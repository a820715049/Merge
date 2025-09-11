using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using static EL.MessageCenter;
using FAT.MSG;
using System.Text;
using System.Linq;
using Cysharp.Threading.Tasks;
using EL.Resource;
using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    using static ActivityLite;
    using static PoolMapping;

    public class Activity : IGameModule, IUserDataHolder, IPostSetUserDataListener, IUpdate
    {
        public class TypeInfo
        {
            public EventTypeInfo conf;
            public ConditionExpression.Expr expr;
            public bool ready;
            public int count;
            public long countTS;
            public long recordTS;

            public void Record(ActivityLike acti_)
            {
                ++count;
                Sync(acti_);
            }

            public void Sync(ActivityLike acti_)
            {
                var d = Game.TimeOf(acti_.endTS);
                var gConf = Game.Manager.configMan.globalConfig;
                d = Game.NextTimeOfDay(d, hour_: gConf.RequireTypeUtcClock, offset_: 1);
                var ts = Game.Timestamp(d);
                if (ts > countTS)
                {
                    recordTS = countTS;
                    countTS = ts;
                    DebugEx.Info($"{nameof(Activity)} {acti_.Type} type count will only refresh after {d}");
                }
            }

            public void Clear()
            {
                count = 0;
                countTS = 0;
                recordTS = 0;
            }
        }

        public readonly List<EventTime> confR = new();
        public readonly Dictionary<(int, int), ActivityLike> map = new();
        public readonly Dictionary<ActivityLike, (int, int, EventType)> mapR = new();
        public readonly Dictionary<EventType, List<ActivityLike>> index = new();
        public readonly GroupGiftPack giftpack = new();
        public readonly GroupExchange exchange = new();
        public readonly GroupCommon common = new();
        private readonly BoardActivityHandler boardActivityHandler = new();  //棋盘类活动处理器 用于控制活动的创建及结束等逻辑
        private readonly List<ActivityLike> cache = new();
        private bool changed;
        private readonly Dictionary<int, long> record = new();
        private readonly Dictionary<(int, int), string> invalid = new();
        private readonly Dictionary<(int, int), string> fail = new();
        private readonly Dictionary<EventType, TypeInfo> info = new();
        internal readonly Dictionary<(int, int), ActivityLike> pending = new();
        private readonly List<ActivityLike> limbo = new();
        private readonly List<ActivityLike> observer = new();
        private IDictionary<int, EventTime> allEvents;
        private long infoTS;
        private long confTS;
        internal readonly ActivityPopup popup;
        internal readonly ActivityRedirect redirect;
        internal UniTask waitRes;
        //能够给各个活动实例提供帧级Update能力的帮助类
        private readonly ActivityUpdateHelper _updateHelper = new();

        public Activity()
        {
            popup = new(this);
            redirect = new();
        }

        public void Reset()
        {
            foreach (var (_, acti) in map)
            {
                try
                {
                    acti.WhenReset();
                }
                catch (Exception e)
                {
                    DebugEx.Error($"{e.Message}\n{e.StackTrace}");
                }
            }
            map.Clear();
            mapR.Clear();
            index.Clear();
            cache.Clear();
            pending.Clear();
            limbo.Clear();
            observer.Clear();
            invalid.Clear();
            fail.Clear();
            confR.Clear();
            info.Clear();
            record.Clear();
            _updateHelper.Clear();
        }

        public void LoadConfig()
        {
            allEvents = GetEventTimeMap();
            var cond = Game.Manager.activityTrigger.cond;
            foreach (var v in GetEventTypeInfoSlice())
            {
                var (e, _) = cond.Parse(v.EventTimeRequire);
                info[v.EventType] = new() { conf = v, expr = e };
            }
            CheckConfR();
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var game = archive.ClientData.PlayerGameData;
            var data = game.Activity ??= new();
            foreach (var ((id, _), pack) in map)
            {
                data.Active[id] = pack.SaveData();
            }
            foreach (var (id, ts) in record)
            {
                data.Record[id] = ts;
            }
            data.TypeCountTS = infoTS;
            foreach (var (id, t) in info)
            {
                data.TypeInfo[(int)id] = new()
                {
                    Count = t.count,
                    CountTS = t.countTS,
                };
            }
            foreach (var pack in limbo)
            {
                data.Limbo.Add(pack.SaveData());
            }
            (boardActivityHandler as IUserDataHolder).FillData(archive);
        }

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            (boardActivityHandler as IUserDataHolder).SetData(archive);
            var data = archive.ClientData.PlayerGameData.Activity;
            if (data == null) return;
            foreach (var (id, p) in data.Active)
            {
                var type = (EventType)p.ActId;
                var id2 = (id, p.From);
                var (r, rs) = TryAdd(id2, type_: type, filter_: -1, data_: p);
                if (!r) RecordFail(id2, type, rs);
            }
            foreach (var (id, ts) in data.Record)
            {
                record[id] = ts;
            }
            infoTS = data.TypeCountTS;
            foreach (var (id, dataT) in data.TypeInfo)
            {
                if (!info.TryGetValue((EventType)id, out var t)) continue;
                t.count = dataT.Count;
                t.countTS = dataT.CountTS;
                t.recordTS = dataT.CountTS;
            }
            static void R(Activity t_, Action<ActivityLike> Add_, IList<ActivityInstance> list_)
            {
                foreach (var p in list_)
                {
                    var type = (EventType)p.ActId;
                    var id2 = (p.Id, p.From);
                    var (r, rs) = t_.TryAddTo(Add_, id2, type_: type, data_: p);
                    if (!r) t_.RecordFail(id2, type, rs);
                }
            }
            R(this, AddLimbo, data.Limbo);
            PrepareRes();
        }

        public void PrepareRes()
        {
            using var _ = PoolMappingAccess.Borrow(out List<UniTask> list);
            foreach (var (_, a) in map)
            {
                if (a.ResPending()) list.Add(a.Asset.pending);
            }
            foreach (var (_, a) in pending)
            {
                if (a.ResPending()) list.Add(a.Asset.pending);
            }
            static async UniTask W(List<UniTask> list_)
            {
                await UniTask.WhenAll(list_);
                DebugEx.Info($"asset_dep ready for existing activities");
            }
            DebugEx.Info($"asset_dep prepare for existing activities count:{list.Count}");
            waitRes = W(list);
        }

        #region Update

        private void _RegisterUpdate(IActivityUpdate obj) => _updateHelper.Register(obj);
        private void _UnregisterUpdate(IActivityUpdate obj) => _updateHelper.Unregister(obj);

        void IUpdate.Update(float deltaTime)
        {
            _updateHelper.UpdateAll(deltaTime);
        }

        #endregion

        public RankingActivity RankingData()
        {
            var list = LookupActive(EventType.Rank);
            if (list == null || list.Count == 0) return null;
            var data = new RankingActivity();
            var map = data.Data;
            foreach (var e in list.Cast<ActivityRanking>())
            {
                if (!e.RankingValid()) continue;
                if (!map.TryGetValue(e.IdR, out var v))
                {
                    v = new() { Cfg = new() };
                    map[e.IdR] = v;
                    e.Fill(v.Cfg);
                }
                e.Fill(v.RankingType2Data);
            }
            return data;
        }

        public void OnPostSetUserData()
        {
            CheckLimbo();
            CheckRefresh(log_: false);
            Get<GAME_ONE_SECOND_DRIVER>().AddListenerUnique(() => CheckRefresh());
        }

        public void Startup()
        {
            Get<ACTIVITY_TS_SYNC>().AddListenerUnique(SyncRecordTS);
            Get<TIME_BIAS>().AddListenerUnique(_ => CheckConfR());
        }

        public void DebugReset()
        {
            foreach (var data in map)
            {
                data.Value.WhenEnd();
            }
            Reset();
            LoadConfig();
            record.Clear();
            foreach (var (_, infoT) in info)
            {
                infoT.count = 0;
                infoT.countTS = 0;
            }
            infoTS = 0;
            CheckRefresh();
            var popup = Game.Manager.screenPopup;
            var state = PopupType.Login;
            popup.Query(state);
        }

        public void DebugExpire()
        {
            foreach (var (id, p) in map)
            {
                map.Remove(id);
                WhenEnd(p, expire_: true);
                break;
            }
        }

        public void DebugReportReady()
        {
            var b = new StringBuilder();
            b.Append(nameof(Activity)).AppendLine($" ready state: (next refresh at {Game.TimeOf(infoTS)})");
            foreach (var (type, n) in info)
            {
                b.Append(type).Append(' ');
                b.Append(n.ready).Append(' ');
                b.Append(n.count);
                if (n.count > 0) b.Append('(').Append(Game.TimeOf(n.countTS)).Append(')');
                if (n.expr == null) b.Append(" <expr-invalid>");
                b.AppendLine();
            }
            DebugEx.Info(b.ToString());
        }

        internal static void S(ReadOnlySpan<char> s_, out ReadOnlySpan<char> a_, out ReadOnlySpan<char> b_, char l_)
        {
            a_ = s_;
            b_ = ReadOnlySpan<char>.Empty;
            var p = s_.IndexOf(l_);
            if (p > 0)
            {
                a_ = s_[..p];
                b_ = s_[(p + 1)..];
            }
        }

        public void DebugActivate(string s_)
        {
            //<id>[-[a][t]]|[-[type]][,duration]
            //a:(search conf AB)
            //t:(from EventTrigger)
            //type:(EventType value)
            //duration:(seconds)
            S(s_, out var sI, out var sD, ',');
            S(sI, out sI, out var sC, '-');
            if (!int.TryParse(sI, out var id)) throw new Exception($"invalid id format:{sI.ToString()}");
            if (!int.TryParse(sD, out var dur))
            {
                if (sD.Length > 0) DebugEx.Warning($"invalid duration format:{sD.ToString()}");
                dur = 3600;
            }
            var searchAB = sC.Length > 0 && sC.IndexOf('a') == sC.Length - 1;
            if (searchAB) sC = sC[..^1];
            var (from, typeV) = sC switch
            {
                var _ when sC.IsEmpty => (FlexEventTime, ActivityLiteTime.ConfigOf(id, searchAB)?.EventType),
                var _ when sC.IndexOf("t") >= 0 => (FlexEventTrigger, GetEventTrigger(id)?.EventType),
                var _ when int.TryParse(sC, out var cT) => (FromInternal, (EventType)cT),
                _ => throw new Exception($"invalid context format:{sC.ToString()}"),
            };
            if (typeV == null) throw new Exception($"config not found for:{sI.ToString()} {sC.ToString()}");
            var type = typeV.Value;
            var id2 = (id, from);
            record.Remove(id);
            var (r, rs) = TryAdd(id2, type, option_: new(flex_: true, searchAB_: searchAB, Create_: a =>
            {
                var now = Game.TimestampNow();
                a.RefreshTS(now, now + dur);
            }));
            if (!r)
            {
                RecordFail(id2, type, rs);
                return;
            }
        }

        public void DebugInsert(string s_)
        {
            //<id> <type> <param>[ duration]
            S(s_, out var sI, out var s, ' ');
            S(s, out var sT, out s, ' ');
            S(s, out var sP, out var sD, ' ');
            if (!int.TryParse(sI, out var id)) throw new Exception($"invalid id format:{sI.ToString()}");
            if (GetEventTime(id) != null) throw new Exception($"insert id already exists id:{id}");
            EventType type;
            if (!int.TryParse(sT, out var typeV))
            {
                if (!Enum.TryParse(sT.ToString(), ignoreCase: true, out type))
                {
                    throw new Exception($"invalid type format:{sT.ToString()}");
                }
            }
            else type = (EventType)typeV;
            if (!int.TryParse(sP, out var param)) throw new Exception($"invalid param format:{sP.ToString()}");
            if (!int.TryParse(sD, out var dur))
            {
                if (sD.Length > 0) DebugEx.Warning($"invalid duration format:{sD.ToString()}");
                dur = 3600;
            }
            var now = Game.TimestampNow();
            var e = new EventTime
            {
                Id = id,
                EventType = type,
                EventParam = param,
                StartTime = now,
                EndTime = now + dur
            };
            confR.Add(e);
            GetEventTimeMap().Add(id, e);
            record.Remove(id);
            if (info.TryGetValue(type, out var infoV))
            {
                infoV.Clear();
            }
        }

        public void DebugEnd(string s_)
        {
            //[[+|-]<type>]
            var black = false;
            EventType type;
            if (string.IsNullOrEmpty(s_)) type = EventType.Default;
            else
            {
                black = s_.StartsWith("-");
                var s = s_.AsSpan();
                if (!char.IsLetterOrDigit(s[0])) s = s[1..];
                if (!int.TryParse(s, out var typeV))
                {
                    if (!Enum.TryParse(s.ToString(), ignoreCase: true, out type))
                    {
                        throw new Exception($"invalid type format:{s.ToString()}");
                    }
                }
                else type = (EventType)typeV;
            }
            cache.Clear();
            foreach (var (_, a) in map)
            {
                if (type != EventType.Default && (a.Type != type == black)) continue;
                cache.Add(a);
            }
            foreach (var a in cache)
            {
                WhenEnd(a, true);
                RemoveActive(a);
                changed = true;
            }
        }

        public bool IsActive((int id, int from) id_) => map.ContainsKey(id_) || pending.ContainsKey(id_);
        public bool IsActive(EventType type_) => index.TryGetValue(type_, out var list) && list.Count > 0;
        public bool IsExpire((int id, int from) id_) => id_.from == 0 && record.ContainsKey(id_.id);
        public bool IsInvalid((int, int) id_, out string rs_) => invalid.TryGetValue(id_, out rs_);
        public bool IsFirst(EventType type_) => !info.TryGetValue(type_, out var v) || v.recordTS == 0;
        public long RecordOf(int id_)
        {
            record.TryGetValue(id_, out var v);
            return v;
        }

        public List<ActivityLike> LookupActive(EventType type_)
        {
            index.TryGetValue(type_, out var list);
            return list;
        }

        public ActivityLike LookupAny(EventType type_)
        {
            LookupAny(type_, out var v);
            return v;
        }

        public bool LookupConf(int id_, out EventTime conf_) => allEvents.TryGetValue(id_, out conf_);

        public bool LookupAny(EventType type_, out ActivityLike acti_)
        {
            var list = LookupActive(type_);
            if (list != null && list.Count > 0)
            {
                acti_ = list[0];
                return true;
            }
            foreach (var (_, a) in pending)
            {
                if (a.Type == type_)
                {
                    acti_ = a;
                    return true;
                }
            }
            acti_ = null;
            return false;
        }

        //找到任意 type_ param_ 一致的正在开启的活动
        public bool LookupAny(EventType type_, int param_, out ActivityLike acti_)
        {
            if (param_ <= 0)
            {
                acti_ = null;
                return false;
            }
            var list = LookupActive(type_);
            if (list != null && list.Count > 0)
            {
                foreach (var a in list)
                {
                    if (a.Param == param_)
                    {
                        acti_ = a;
                        return true;
                    }
                }
            }
            foreach (var (_, a) in pending)
            {
                if (a.Type == type_ && a.Param == param_)
                {
                    acti_ = a;
                    return true;
                }
            }
            acti_ = null;
            return false;
        }

        public bool Lookup(int id_, out ActivityLike acti_)
        {
            var id = (id_, FromEventTime);
            return map.TryGetValue(id, out acti_) || pending.TryGetValue(id, out acti_);
        }
        public ActivityLike Lookup(int id_, int from_ = FromEventTime)
        {
            var id = (id_, from_);
            var _ = map.TryGetValue(id, out var v) || pending.TryGetValue(id, out v);
            return v;
        }

        public void CheckConfR()
        {
            static int P(EventTime v_)
                => v_.EventType switch
                {
                    EventType.CardAlbum => -1,
                    _ => 0,
                };
            confR.Clear();
            var t = Game.TimestampNow();
            var m = GetEventTimeMap();
            foreach (var (_, r) in m)
            {
                if (r.EndTime < t) continue;
                confR.Add(r);
            }
            confTS = t;
            confR.Sort((a_, b_) =>
            {
                var pa = P(a_);
                var pb = P(b_);
                if (pa != pb) return pa - pb;
                return a_.Id - b_.Id;
            });
            if (confR.Count > 0) DebugEx.Info($"{nameof(Activity)} confR {confR[0].Id}->{confR[^1].Id}");
            else DebugEx.Warning($"{nameof(Activity)} confR came out empty. map_count:{m.Count}");
        }

        public ActivityGroup GroupOf(EventType type_)
            => type_ switch
            {
                EventType.ToolExchange
                    => exchange,
                EventType.Energy or EventType.DailyPop or EventType.OnePlusOne or EventType.MineOnePlusOne or EventType.EndlessPack or EventType.FarmEndlessPack or
                EventType.NewSession or EventType.ThreeForOnePack or EventType.EndlessThreePack or EventType.GemEndlessThree or
                EventType.GemThreeForOne or EventType.OnePlusTwo or EventType.ProgressPack or EventType.RetentionPack or EventType.MarketSlidePack or
                EventType.EnergyMultiPack or EventType.ShinnyGuarPack or EventType.DiscountPack or EventType.ErgListPack or EventType.FightOnePlusOne or EventType.WishEndlessPack or
                EventType.SpinPack or EventType.Bp or EventType.CartOnePlusOne
                    => giftpack,
                EventType.De or EventType.Dem
                    => Game.Manager.dailyEvent.ActivityGroup,
                EventType.CardAlbum
                    => Game.Manager.cardMan.CardActHandler,
                EventType.Decorate
                    => Game.Manager.decorateMan.ActivityGroup,
                EventType.MiniBoard
                    => Game.Manager.miniBoardMan.ActivityHandler,
                EventType.MiniBoardMulti
                    => Game.Manager.miniBoardMultiMan.ActivityHandler,
                EventType.Pachinko
                    => Game.Manager.pachinkoMan.Group,
                EventType.Mine or EventType.Fish or EventType.FarmBoard or EventType.Fight or EventType.WishBoard or EventType.MineCart
                    => boardActivityHandler,
                _ => common,
            };

        public static bool LevelValid(int active_, int shutdown_)
        {
            var level = Game.Manager.mergeLevelMan.displayLevel;
            if (shutdown_ == 0) shutdown_ = int.MaxValue;
            return level >= active_ && level < shutdown_;
        }

        public (bool, string) TryAdd((int, int) id_, EventType type_, int filter_ = 0, ActivityInstance data_ = null, ActivityGroup.Option option_ = default)
        {
            if (option_.flex) goto skip_check;
            if (IsInvalid(id_, out var rsi)) return (false, rsi);
            if (TypeLimit(type_)) return (false, "type limit");
            skip_check:
            var group = GroupOf(type_);
            if (group == null) return Invalid(id_, $"{nameof(Activity)} failed to categorize group for type:{type_} id:{id_}");
            var (r, rs) = group.Filter(filter_, id_, type_);
            if (!r) return (r, rs);
            (r, rs) = group.TryAdd(this, id_, type_, data_, option_);
            return (r, rs);
        }

        public (bool, string) TryAddTo(Action<ActivityLike> Add_, (int, int) id_, EventType type_, ActivityInstance data_ = null, ActivityGroup.Option option_ = default)
        {
            if (IsInvalid(id_, out var rsi)) return (false, rsi);
            var group = GroupOf(type_);
            if (group == null) return Invalid(id_, $"{nameof(Activity)} failed to categorize group for type:{type_} id:{id_}");
            var (r, rs) = group.TryAddTo(Add_, this, id_, type_, data_, option_);
            return (r, rs);
        }

        public void CheckRefresh(int filter_ = 0, bool log_ = true)
        {
            if (map.Count > 0) CheckEnd();
            CheckTypeReady(log_: log_);
            CheckTypeCount();
            CheckEventTrigger();
            CheckEventTime(filter_);
            if (changed)
            {
                changed = false;
                Get<ACTIVITY_UPDATE>().Dispatch();
            }
        }

        public void CheckEventTime(int filter_ = 0)
        {
            static bool Active(EventTime conf_, long t_) => conf_.StartTime <= t_ && t_ < conf_.EndTime;
            static bool WaitTag(TypeInfo t_) => t_ != null && t_.conf.IsGradeLimit;
            var tagCheck = Game.Manager.userGradeMan.IsTagExpire;
            var t = Game.TimestampNow();
            foreach (var p in confR)
            {
                var id2 = (p.Id, FromEventTime);
                if (p.Id == 991037)
                {
                    var a1 = !Active(p, t);
                    var a2 = IsActive(id2);
                    var a3 = (info.TryGetValue(p.EventType, out var infoT1) && !infoT1.ready);
                    var a4 = (info.TryGetValue(p.EventType, out var infoT2) && tagCheck && WaitTag(infoT2));
                }
                if (!Active(p, t) || IsActive(id2)
                    || (info.TryGetValue(p.EventType, out var infoT) && !infoT.ready)
                    || (tagCheck && WaitTag(infoT))) continue;
                var (r, rs) = TryAdd(id2, p.EventType, filter_: filter_);
                if (!r) RecordFail(id2, p.EventType, rs);
            }
        }

        public void CheckEventTrigger()
        {
            Game.Manager.activityTrigger.Check();
        }

        public bool TypeLimit(EventType type_)
        {
            if (!info.TryGetValue(type_, out var infoT) || infoT.conf.LimitNum == 0) return false;
            var conf = infoT.conf;
            if (conf.IsLimitSingleDay)
            {
                return infoT.count >= conf.LimitNum;
            }
            var aC = index.TryGetValue(infoT.conf.EventType, out var list) ? list.Count : 0;
            return aC >= conf.LimitNum;
        }

        public void CheckTypeReady(bool log_ = true)
        {
            var mgr = Game.Manager.activityTrigger;
            var cond = mgr.cond;
            var eval = mgr.eval;
            foreach (var (type, infoT) in info)
            {
                if (infoT.expr == null) continue;
                var (v, valid) = cond.Evaluate(eval, infoT.expr);
                if (!valid) infoT.expr = null;
                if (log_ && infoT.ready != v)
                {
                    DebugEx.Info($"{nameof(Activity)} type {type} ready changed to {v}");
                }
                infoT.ready = v;
            }
        }

        public (bool, bool) CheckTypeReady(EventType type_)
        {
            if (!info.TryGetValue(type_, out var infoT) || infoT.expr == null) return (false, false);
            var mgr = Game.Manager.activityTrigger;
            var cond = mgr.cond;
            var eval = mgr.eval;
            return cond.Evaluate(eval, infoT.expr);
        }

        public void CheckTypeCount()
        {
            var ts = Game.TimestampNow();
            if (ts > infoTS)
            {
                var gConf = Game.Manager.configMan.globalConfig;
                infoTS = Game.Timestamp(Game.NextTimeOfDay(hour_: gConf.RequireTypeUtcClock));
                foreach (var (_, infoT) in info)
                {
                    if (ts >= infoT.countTS)
                    {
                        infoT.count = 0;
                    }
                }
                DebugEx.Info($"{nameof(Activity)} type count reset, next reset at {Game.TimeOf(infoTS)}");
            }
        }

        public void CheckEnd()
        {
            var t = Game.TimestampNow();
            static bool CheckEnd(ActivityLike pack_, long t_)
            {
                return t_ >= pack_.endTS || t_ < pack_.startTS;
            }
            cache.Clear();
            foreach (var (_, acti) in map)
            {
                cache.Add(acti);
            }
            for (var k = 0; k < cache.Count; ++k)
            {
                var acti = cache[k];
                if (CheckEnd(acti, t))
                {
                    WhenEnd(acti, expire_: true);
                    RemoveActive(acti);
                }
            }
            cache.Clear();
        }

        public void AddActive((int id, int from) id_, ActivityLike acti_, bool new_)
        {
            var r = acti_.ResPending();
            if ((new_ && acti_.SetupPending()) || r)
            {
                AddPending(acti_);
                return;
            }
            AcceptActive(id_, acti_, new_);
        }

        public void AcceptActive((int id, int from) id_, ActivityLike acti_, bool new_)
        {
            var type = acti_.Type;
            if (mapR.TryGetValue(acti_, out var r))
            {
                DebugEx.Error($"{nameof(Activity)} record map overwrite {id_} {type} onto {r}");
                var (idR, fromR, typeR) = r;
                RemoveActive(idR, fromR, typeR, acti_);
            }
            map[id_] = acti_;
            mapR[acti_] = (id_.id, id_.from, type);
            if (!index.TryGetValue(type, out var list))
            {
                list = new();
                index[type] = list;
            }
            list.Add(acti_);
            if (info.TryGetValue(type, out var infoT)) infoT.Record(acti_);
            DebugEx.Info($"{nameof(Activity)} {id_} {type} active {Game.TimeOf(acti_.startTS)}->{Game.TimeOf(acti_.endTS)}");
            if (new_)
            {
                DataTracker.event_active.Track(acti_);
            }
            acti_.WhenActive(new_);
            //活动开始时自行注册update
            if (acti_ is IActivityUpdate updater)
                _RegisterUpdate(updater);
            Observe(nameof(ACTIVITY_ACTIVE));
            Get<ACTIVITY_ACTIVE>().Dispatch(acti_, new_);
            Get<ACTIVITY_STATE>().Dispatch(acti_);
            changed = true;
        }

        public void RemoveActive(ActivityLike acti_)
        {
            var validR = mapR.TryGetValue(acti_, out var r);
            var (id, from, type) = validR ? r : (acti_.Id, acti_.From, acti_.Type);
            if (!validR) DebugEx.Warning($"{nameof(Activity)} map record for {id} {type} not found");
            RemoveActive(id, from, type, acti_);
            acti_.SetupClear();
        }

        private void RemoveActive(int id_, int from_, EventType type_, ActivityLike acti_)
        {
            map.Remove((id_, from_));
            mapR.Remove(acti_);
            if (index.TryGetValue(type_, out var list))
            {
                list.Remove(acti_);
            }
            Get<ACTIVITY_STATE>().Dispatch(acti_);
        }

        public void AddPending(ActivityLike acti_)
        {
            if (!acti_.Active)
            {
                DebugEx.Warning($"{nameof(Activity)} try pending inactive activity {acti_.Info3}");
                return;
            }
            DebugEx.Info($"{nameof(Activity)} pending activity {acti_.Info3}");
            pending[acti_.Id2] = acti_;
        }

        public void RemovePending(ActivityLike acti_)
        {
            if (!pending.ContainsKey(acti_.Id2))
            {
                DebugEx.Warning($"{nameof(Activity)} try remove untracked pending activity {acti_.Info3}");
                return;
            }
            DebugEx.Info($"{nameof(Activity)} pending activity {acti_.Info3} remove");
            pending.Remove(acti_.Id2);
        }

        public void AcceptPending(ActivityLike acti_)
        {
            if (!pending.ContainsKey(acti_.Id2))
            {
                DebugEx.Warning($"{nameof(Activity)} try accept untracked pending activity {acti_.Info3}");
                return;
            }
            DebugEx.Info($"{nameof(Activity)} pending activity {acti_.Info3} accepted");
            pending.Remove(acti_.Id2);
            AcceptActive(acti_.Id2, acti_, true);
        }

        internal (bool, string) Invalid((int, int) id_, string rs_)
        {
            DebugEx.Error($"{nameof(Activity)} invalid: {rs_}");
            invalid[id_] = rs_;
            return (false, rs_);
        }

        internal void RecordFail((int, int) id_, EventType type_, string reason_)
        {
            if ((reason_ == "filter")
                || (fail.TryGetValue(id_, out var rs) && rs == reason_)) return;
            fail[id_] = reason_;
            DebugEx.Warning($"{nameof(Activity)} fail for {id_} {type_} reason:{reason_}");
        }

        internal void SyncRecordTS(ActivityLike acti_)
        {
            if (!info.TryGetValue(acti_.Type, out var infoT)) return;
            infoT.Sync(acti_);
        }

        public void EndImmediate(EventType type_)
        {
            if (!index.TryGetValue(type_, out var list)) return;
            foreach (var acti in list.ToArray())
            {
                EndImmediate(acti, true);
            }
        }

        public void EndImmediate(ActivityLike acti_, bool expire_)
        {
            WhenEnd(acti_, expire_);
            RemoveActive(acti_);
            changed = true;
            CheckRefresh(0);
        }

        public void WhenEnd(ActivityLike acti_, bool expire_)
        {
            var recordStr = string.Empty;
            if (acti_.Lite.WillRecord)
            {
                record[acti_.Id] = Game.TimestampNow();
                recordStr = "(record)";
            }
            DebugEx.Info($"{nameof(Activity)} {acti_.Info3} {acti_.Type} end {Game.TimeOf(acti_.endTS)}{recordStr}");
            //活动结束时自行取消注册update
            if (acti_ is IActivityUpdate updater)
                _UnregisterUpdate(updater);
            Get<ACTIVITY_END>().Dispatch(acti_, expire_);
            acti_.WhenEnd();
            changed = true;
            GroupOf(acti_.Type)?.End(this, acti_, expire_);
        }

        public void AddLimbo(ActivityLike acti_)
        {
            if (acti_.Active)
            {
                DebugEx.Warning($"{nameof(Activity)} try limbo active activity {acti_.Info3}");
                return;
            }
            DebugEx.Info($"{nameof(Activity)} limbo activity {acti_.Info3}");
            limbo.Add(acti_);
        }

        public void RemoveLimbo(ActivityLike acti_)
        {
            if (!limbo.Contains(acti_))
            {
                DebugEx.Warning($"{nameof(Activity)} try remove untracked limbo activity {acti_.Info3}");
                return;
            }
            DebugEx.Info($"{nameof(Activity)} limbo activity {acti_.Info3} remove");
            limbo.Remove(acti_);
        }

        public void CheckLimbo()
        {
            cache.Clear();
            cache.AddRange(limbo);
            foreach (var a in cache)
            {
                a.WakeLimbo();
            }
        }

        public void Observe(ActivityLike acti_)
        {
            DebugEx.Info($"{nameof(Activity)} observer activity {acti_.Info3}");
            observer.Add(acti_);
        }

        public void Observe(string e_)
        {
            for (var k = 0; k < observer.Count; ++k)
            {
                if (!observer[k].WhenObserve(e_))
                {
                    DebugEx.Info($"{nameof(Activity)} observer activity {observer[k].Info3} end observing");
                    var n = observer.Count - 1;
                    observer[k] = observer[n];
                    --k;
                    observer.RemoveAt(n);
                }
            }
        }
    }
}