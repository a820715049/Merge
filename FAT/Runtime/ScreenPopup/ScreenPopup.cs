using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using EL;
using fat.gamekitdata;
using fat.rawdata;

namespace FAT
{
    public class ScreenPopup : IGameModule, IUserDataHolder
    {
        public bool QueueReady => !blockIgnore;
        public List<IScreenPopup> list = new();
        public readonly Stack<(List<IScreenPopup>, PopupType, int)> cache = new();
        private readonly Dictionary<PopupType, bool> state = new();
        public IScreenPopup active;
        public UIResource wait;
        public PopupType query;
        public bool changed;
        public Dictionary<PopupType, long> queryTS = new();
        internal readonly Dictionary<int, int> record = new();
        internal long refreshTS;
        private readonly Comparison<IScreenPopup> WeightSort;
        private int popupCount;
        private int popupLimit;
        public PopupType PopupNone => (PopupType)(-1);
        private bool blockIgnore;
        private bool blockDelay;

        public ScreenPopup()
        {
            WeightSort = (a_, b_) => b_.PopupWeight - a_.PopupWeight;
        }

        public void DebugReset()
        {
            refreshTS = 0;
            Reset();
            record.Clear();
            WhenEnterGame();
        }

        //检查目前是否有弹脸
        public bool HasPopup()
        {
            return list.Count > 0;
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var game = archive.ClientData.PlayerGameData;
            var data = game.ScreenPopup ??= new();
            data.RefreshTS = refreshTS;
            foreach (var (k, v) in record)
            {
                data.Record[k] = v;
            }
        }

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.ScreenPopup;
            if (data == null) return;
            refreshTS = data.RefreshTS;
            foreach (var (k, v) in data.Record)
            {
                record[k] = v;
            }
        }

        public void Reset()
        {
            cache.Clear();
            list.Clear();
            record.Clear();
            active = null;
            query = 0;
        }

        public void Startup()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListenerUnique(CheckTick);
            CheckRefresh();
        }

        public void LoadConfig()
        {
            popupLimit = Game.Manager.configMan.globalConfig.PopupLimit;
        }

        public void Block(bool delay_ = false, bool ignore_ = false)
        {
            DebugEx.Info($"{nameof(ScreenPopup)} delay:{delay_} ignore_:{ignore_}");
            blockIgnore = ignore_;
            blockDelay = delay_;
        }

        public void CheckTick()
        {
            //弹脸系统如果在loading过程中则不检查
            if (!GameProcedure.IsInGame)
                return;
            CheckRefresh();
            CheckClose();
            if (list.Count == 0 || active != null) return;
            Check();
        }

        public void CheckRefresh()
        {
            var t = Game.TimestampNow();
            if (t < refreshTS) return;
            record.Clear();
            var global = Game.Manager.configMan.globalConfig;
            var tt = Game.NextTimeOfDay(global.PopupRefresh);
            refreshTS = Game.Timestamp(tt);
        }

        public void CheckClose()
        {
            var ui = wait ?? active?.PopupRes;
            if (ui == null) return;
            if (!UIManager.Instance.IsValid(ui))
            {
                DebugEx.Warning($"{nameof(ScreenPopup)} active failed to show:{ui.prefabPath}");
                Next();
                return;
            }
            if (UIManager.Instance.IsClosed(ui))
            {
                DebugEx.Warning($"{nameof(ScreenPopup)} undetected closed active:{ui.prefabPath}");
                Next();
                return;
            }
        }

        public void Check(int c_ = 1)
        {
            if (blockDelay)
            {
                CheckIgnoreDelay(c_);
                return;
            }
        next:
            if (list.Count == 0)
            {
                if (CheckState(query))
                {
                    EndClear();
                    DropCache();
                }
                if (TryResumeCache())
                {
                    c_ = 0;
                    goto next;
                }
                return;
            }
            if (changed)
            {
                changed = false;
                list.Sort(WeightSort);
            }
            var peek = list[0];
            if (peek.option.delay || !peek.Ready()) return;
            active = peek;
            list.RemoveAt(0);
            if (!active.CheckValid(out var rs))
            {
                DebugEx.Error($"{nameof(ScreenPopup)} reject active popup {active} reason={rs}");
                goto next;
            }
            var id = active.PopupId;
            record.TryGetValue(id, out var v);
            var limit = active.QueueState == PopupNone ? 100 : popupLimit;
            DebugEx.Info($"{nameof(ScreenPopup)} try popup {id} {v}<{active.PopupLimit} {popupCount}<{limit}");
            if (active.PopupLimit >= 0 && v >= active.PopupLimit) goto next;
            var ui = active.PopupRes;
            if (ui == null)
            {
                DebugEx.Error($"{nameof(ScreenPopup)} no ui to popup, please check:{active}");
                goto next;
            }
            ui.ActivePopup = true;
            if (!active.OpenPopup()) goto next;
            record[id] = v + c_;
            Game.Manager.archiveMan.SendImmediately(true);
            popupCount += c_;
            if (popupCount >= limit && list.Count > 0)
            {
                DebugEx.Info($"{nameof(ScreenPopup)} popup limit {limit} reached, clear {list.Count}");
                LimitClear();
            }
        }

        public void CheckIgnoreDelay(int c_)
        {
            if (list.Count == 0 || !(Game.Manager.mergeBoardMan.activeWorld?.isEquivalentToMain ?? false)) { return; }
            if (changed)
            {
                changed = false;
                list.Sort(WeightSort);
            }
            while (list.FirstOrDefault(x => x.option.ignoreDelay == true) != null)
            {
                var peek = list.FirstOrDefault(x => x.option.ignoreDelay == true);
                if (peek.option.delay || !peek.Ready()) return;
                active = peek;
                list.Remove(active);
                if (changed)
                {
                    changed = false;
                    list.Sort(WeightSort);
                }
                if (!active.CheckValid(out var rs))
                {
                    DebugEx.Error($"{nameof(ScreenPopup)} reject active popup {active} reason={rs}");
                    continue;
                }
                var id = active.PopupId;
                record.TryGetValue(id, out var v);
                var limit = active.QueueState == PopupNone ? 100 : popupLimit;
                DebugEx.Info($"{nameof(ScreenPopup)} try popup {id} {v}<{active.PopupLimit} {popupCount}<{limit}");
                if (active.PopupLimit >= 0 && v >= active.PopupLimit) { continue; }
                ;
                var ui = active.PopupRes;
                if (ui == null)
                {
                    DebugEx.Error($"{nameof(ScreenPopup)} no ui to popup, please check:{active}");
                    continue;
                }
                ui.ActivePopup = true;
                if (!active.OpenPopup()) { continue; }
                record[id] = v + c_;
                Game.Manager.archiveMan.SendImmediately(true);
                popupCount += c_;
                if (popupCount >= limit && list.Count > 0)
                {
                    DebugEx.Info($"{nameof(ScreenPopup)} popup limit {limit} reached, clear {list.Count}");
                    LimitClear();
                }
            }
            if (list.Count == 0)
            {
                if (CheckState(query))
                {
                    EndClear();
                    DropCache();
                }
                if (TryResumeCache())
                {
                    c_ = 0;
                }
            }
        }

        public void TryQueue(IScreenPopup target_, PopupType state_, object custom_ = null)
        {
            if (target_ == null || !target_.StateValid(state_) || (target_.option.delay && !Game.Manager.iap.DataReady)) return;
            static string M(IScreenPopup target_, string rs_) => $"{nameof(ScreenPopup)} reject popup {target_} reason={rs_}";
            if (!target_.CheckValid(out var rs))
            {
                DebugEx.Error(M(target_, rs));
                return;
            }
            if (list.Contains(target_))
            {
                DebugEx.Warning(M(target_, "duplicate"));
                return;
            }
            if (blockIgnore)
            {
                DebugEx.Warning(M(target_, "block"));
                return;
            }
            target_.PopupState = query;
            target_.QueueState = state_;
            target_.Custom = custom_;
            DebugEx.Info($"{nameof(ScreenPopup)} queue {target_} {query} {state_}");
            list.Add(target_);
            changed = true;
        }
        public void Queue(IScreenPopup target_, object custom_ = null) => TryQueue(target_, PopupNone, custom_);

        public void WhenClose(UIResource ui_)
        {
            if (ui_ == wait)
            {
                DebugEx.Info($"{nameof(ScreenPopup)} wait {wait.prefabPath} end");
                wait = null;
                goto next;
            }
            if (wait != null || !ui_.ActivePopup) return;
            ui_.ActivePopup = false;
            var ui = active?.PopupRes;
            if (ui != null && ui_ != ui)
            {
                DebugEx.Warning($"{nameof(ScreenPopup)} closing/active mismatch, closing:{ui_.prefabPath} active:{ui.prefabPath}");
            }
        next:
            Next();
        }

        public void Next()
        {
            active = null;
            wait = null;
            Check();
        }

        private void LimitClear()
        {
            var n = -1;
            for (var k = 0; k < list.Count; ++k)
            {
                if (list[k].option.ignoreLimit || list[k].IgnoreLimit) list[++n] = list[k];
            }
            ++n;
            list.RemoveRange(n, list.Count - n);
        }

        private void EndClear()
        {
            list.Clear();
            query = PopupNone;
            active = null;
            wait = null;
            popupCount = 0;
        }

        public void Clear()
        {
            var ui = active?.PopupRes;
            DebugEx.Info($"{nameof(ScreenPopup)} clear {list.Count} {ui?.prefabPath}");
            EndClear();
            if (ui != null) UIManager.Instance.CloseWindow(ui);
        }

        public void ResetState(PopupType state_)
        {
            DebugEx.Info($"{nameof(ScreenPopup)} reset {state_}");
            state[state_] = false;
        }

        public bool CheckState(PopupType state_)
        {
            return state_ < 0 || state.TryGetValue(state_, out var v) && v;
        }

        public void Wait(UIResource ui_)
        {
            DebugEx.Info($"{nameof(ScreenPopup)} wait {ui_.prefabPath}");
            wait = ui_;
        }

        public void Query(PopupType state_)
        {
            DebugEx.Info($"{nameof(ScreenPopup)} query {state_}");
            popupCount = 0;
            state[state_] = true;
            query = state_;
            queryTS[state_] = Game.TimestampNow();
            MessageCenter.Get<MSG.SCREEN_POPUP_QUERY>().Dispatch(this, state_);
            Check();
        }

        public void TryQuery(PopupType state_)
        {
            if (query != state_ && (active != null || list.Count > 0))
            {
                var list1 = new List<IScreenPopup>() { active };
                list1.AddRange(list);
                var c = (list1, query, popupCount);
                DebugEx.Info($"{nameof(ScreenPopup)} cache {query} {list1.Count} {popupCount}");
                Clear();
                cache.Push(c);
            }
            Query(state_);
        }

        public bool TryResumeCache()
        {
            if (cache.Count == 0) return false;
            static bool CheckEnergy() => Game.Manager.mergeEnergyMan.EnergyAfterFly <= 0;
            var (list1, query1, popupCount1) = cache.Pop();
            var valid = query1 switch
            {
                PopupType.Energy => CheckEnergy(),
                _ => true,
            };
            if (!valid)
            {
                DebugEx.Info($"{nameof(ScreenPopup)} cache skip {query1} {list1.Count} {popupCount1}");
                return false;
            }
            DebugEx.Info($"{nameof(ScreenPopup)} cache resume {query1} {list1.Count} {popupCount1} <= {query} {list.Count == 0} {popupCount}");
            query = query1;
            popupCount = popupCount1;
            list.AddRange(list1);
            return true;
        }

        public void DropCache()
        {
            if (cache.Count == 0) return;
            DebugEx.Info($"{nameof(ScreenPopup)} drop cache {cache.Count}");
            cache.Clear();
        }

        public void WhenEnterGame() => TryQuery(PopupType.Login);
        public void WhenOutOfEnergy() => TryQuery(PopupType.Energy);
        public void WhenOutOfDiamond() => TryQuery(PopupType.Diamond);

        public async UniTask Wait(int delay_ = 1000)
        {
            var ui = UIManager.Instance;
            while (!QueueReady || !ui.CheckUIIsIdleState()) await Task.Delay(delay_);
        }
    }
}
