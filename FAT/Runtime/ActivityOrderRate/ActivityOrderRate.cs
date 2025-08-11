using System.Collections.Generic;
using static FAT.RecordStateHelper;
using static fat.conf.Data;
using fat.gamekitdata;
using fat.rawdata;
using System;
using FAT.Merge;
using EL;
using DG.Tweening;
using UnityEngine.UI.Extensions;
using System.Linq;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FAT
{
    public struct OrderRateParamProvider : IParamProvider
    {
        public int orderId;
    }
    public class ActivityOrderRate : ActivityLike, IBoardEntry, IActivityOrderHandler, ISpawnEffectWithTrail
    {
        #region 运行时字段
        public EventOrderRate Conf;
        private readonly List<RoundCoin> _roundCoins = new();
        public (int, int) Reward1 => (1, _milestoneList[0]);
        public (int, int) Reward2 => (2, _milestoneList[1]);
        public (int, int) Reward3 => (3, _milestoneList[2]);

        // 开启界面
        public ActivityVisual MainTheme = new();
        public UIResAlt MainRes = new(UIConfig.UIOrderRateMain);
        public PopupActivity MainPopup = new();

        // 领奖界面
        public ActivityVisual StartTheme = new();
        public UIResAlt StartRes = new(UIConfig.UIOrderRateStart);
        public PopupActivity StartPopup = new();

        public override ActivityVisual Visual => MainTheme;
        public int curShowPhase = 0;
        private int _curPhase = 0;
        private bool hasPop = false;

        #endregion
        #region 存档字段
        private int _detailID = 0;
        private readonly List<int> _milestoneList = new();
        #endregion

        public ActivityOrderRate(ActivityLite lite_)
        {
            Lite = lite_;
            Conf = Game.Manager.configMan.GetEventOrderRateConfig(lite_.Param);
            MessageCenter.Get<MSG.GAME_MERGE_PRE_BEGIN_REWARD>().AddListener(TryAddPhase);
            if (MainTheme.Setup(Conf.EventThemeId, MainRes))
                MainPopup = new(this, MainTheme, MainRes, false);
            if (StartTheme.Setup(Conf.StartThemeId, StartRes))
                StartPopup = new(this, StartTheme, StartRes, false);
        }

        public string BoardEntryAsset()
        {
            //return MainTheme.AssetMap.TryGetValue("boardEntry", out var res) ? res : null;
            return "event_orderrate_default:UIOrderRateEntry.prefab";
        }


        public override void SetupFresh()
        {
            _detailID = Game.Manager.userGradeMan.GetTargetConfigDataId(Conf.GradeId);
            GetMilestoneList(_detailID);
            InitPhase();
            Game.Manager.screenPopup.TryQueue(MainPopup, PopupType.Login);
            hasPop = true;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (hasPop) return;
            Game.Manager.screenPopup.TryQueue(MainPopup, PopupType.Login);
            hasPop = true;
        }
        public override void LoadSetup(ActivityInstance instance)
        {
            var i = 0;
            var data = instance.AnyState;
            _detailID = ReadInt(i++, data);
            while (i < data.Count)
            {
                _milestoneList.Add(ReadInt(i++, data));
            }
            InitPhase();
        }

        public override void Open()
        {
            UIManager.Instance.OpenWindow(MainRes.ActiveR, this);
        }

        public override void SaveSetup(ActivityInstance instance)
        {
            var i = 0;
            var data = instance.AnyState;
            data.Clear();
            data.Add(ToRecord(i++, _detailID));
            foreach (var milestone in _milestoneList)
            {
                data.Add(ToRecord(i++, milestone));
            }
        }

        public override void WhenEnd()
        {
            MessageCenter.Get<MSG.GAME_MERGE_PRE_BEGIN_REWARD>().RemoveListener(TryAddPhase);
        }

        public override void WhenReset()
        {
            MessageCenter.Get<MSG.GAME_MERGE_PRE_BEGIN_REWARD>().RemoveListener(TryAddPhase);
        }

        #region 对外接口
        public void FillMilestoneList(List<int> list) { list.AddRange(_milestoneList); }
        public string GetTitle()
        {
            switch (_curPhase)
            {
                case 1:
                    return Game.Manager.configMan.GetEventOrderRateBoxConfig(Reward1.Item1).StartTitleKey;
                case 2:
                    return Game.Manager.configMan.GetEventOrderRateBoxConfig(Reward2.Item1).StartTitleKey;
                case 3:
                    return Game.Manager.configMan.GetEventOrderRateBoxConfig(Reward3.Item1).StartTitleKey;
                default:
                    return string.Empty;
            }
        }
        public int GetCurReward()
        {
            switch (_curPhase)
            {
                case 1:
                    return Reward1.Item1;
                case 2:
                    return Reward2.Item1;
                case 3:
                    return Reward3.Item1;
                default:
                    return 0;
            }
        }

        public (int, int) GetCurRewardInfo()
        {
            switch (_curPhase)
            {
                case 0:
                    return Reward1;
                case 1:
                    return Reward2;
                case 2:
                    return Reward3;
                default:
                    return (0, 0);
            }
        }

        public int GetLastMile()
        {
            switch (_curPhase)
            {
                case 0:
                    return 0;
                case 1:
                    return Reward1.Item2;
                case 2:
                    return Reward2.Item2;
            }
            return 0;
        }

        public bool TryClaimReward(int order, int reward, Vector3 OrderPos)
        {
            var all = Game.Manager.configMan.GetEventOrderRateRandomConfig();
            var detail = Game.Manager.configMan.GetEventOrderRateDetailConfig(_detailID);
            var conf = all.Where(x => detail.RandomId.Contains(x.Id));
            var confDetail = conf.FirstOrDefault(x => x.RandomerId == order && x.BoxInfo == reward);
            if (confDetail == null)
            {
                return false;
            }
            var diffyMin = confDetail.DiffLeft;
            var diffyMax = confDetail.DiffRight;
            var board = Game.Manager.mergeBoardMan.activeWorld.activeBoard;

            var context = ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.OrderRate);
            context.spawnEffect = this;
            var itemId = Game.Manager.mergeItemDifficultyMan.CalcSpecialBoxOutput(diffyMin, diffyMax);
            if (itemId <= 0)
            {
                // 无法生成奖励
                DebugEx.Error("[ActivityOrderLike] specialbox output failed");
                return false;
            }
            // 注册出生位置
            BoardUtility.RegisterSpawnRequest(itemId, OrderPos, 1f);
            var item = board.TrySpawnItem(itemId, ItemSpawnReason.OrderRate, context);
            Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(itemId, out var avg, out var real);
            DataTracker.event_orderrate_rwd.Track(this, 1, itemId, order, Reward3.Item2, reward, _detailID, real, _curPhase == 3);
            if (item != null)
            {
                // 已发往棋盘
                return true;
            }
            else
            {
                // 不再需要发放到棋盘 取消预填充的SpawnRequest
                BoardUtility.PopSpawnRequest();
                // 需要放入奖励队列 走飞图标机制
                var re = Game.Manager.rewardMan.BeginReward(itemId, 1, ReasonString.order_rate);
                UIFlyUtility.FlyReward(re, OrderPos);
            }
            return true;
        }
        #endregion
        private void GetMilestoneList(int detail)
        {
            var conf = Game.Manager.configMan.GetEventOrderRateDetailConfig(detail);
            if (conf == null) return;
            foreach (var levelScore in conf.LevelScore)
            {

                var item1 = levelScore.Split(':')[0].ConvertToInt();
                var item2 = levelScore.Split(':')[1].ConvertToInt();
                var baseCount = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(item1, item2);
                _milestoneList.Add(FindBoundCount(baseCount));
            }
        }

        private int FindBoundCount(int count)
        {
            _roundCoins.AddRange(Game.Manager.configMan.GetRoundCoinConfig());
            var left = 0;
            var right = _roundCoins.Count - 1;
            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                if (_roundCoins[mid].From < count)
                {
                    left = mid + 1;
                }
                else if (_roundCoins[mid].From > count)
                {
                    right = mid - 1;
                }
                else if (_roundCoins[mid].From == count)
                {
                    right = mid;
                    break;
                }
            }
            return (int)Math.Ceiling((double)count / _roundCoins[right].RoundBy) * _roundCoins[right].RoundBy;
        }

        #region IActivityOrderHandler

        public static string GetExtraRewardMiniThemeRes(int eventId, int paramId)
        {
            if (paramId == 0)
            {
                var _cfg = GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == fat.rawdata.EventType.OrderRate);
                paramId = _cfg?.EventParam ?? 0;
            }
            if (paramId == 0)
            {
                return string.Empty;
            }
            var cfg = GetEventOrderRate(paramId);
            var theme = GetEventTheme(cfg.EventThemeId);
            //theme.AssetInfo.TryGetValue("orderRate", out var res);
            //return res;
            return "event_orderrate_default:OrderRewardItem_Rate.prefab";
        }

        bool IActivityOrderHandler.IsValidForBoard(int boardId) => boardId == Game.Manager.mainMergeMan.world.activeBoard.boardId;

        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            var changed = false;
            if (order.OrderType != (int)OrderType.Normal)
            {
                return changed;
            }
            if (OrderAttachmentUtility.slot_extra_tr.HasData(order))
            {
                if (OrderAttachmentUtility.slot_extra_tr.IsMatchEventId(order, Id))
                {
                    if (order.GetValue(OrderParamType.ExtraSlot_TR_RewardId) == GetCurReward())
                    {
                        return changed;
                    }
                    else
                    {

                        OrderAttachmentUtility.slot_extra_tr.UpdateEventData(order, Id, Param, GetCurReward(), 1);
                        changed = true;
                    }
                }
                else
                {
                    // 不是同一期 这里顺便删除
                    OrderAttachmentUtility.slot_extra_tr.ClearData(order);
                    changed = true;
                }
            }
            if (GetCurReward() > 0)
            {
                OrderAttachmentUtility.slot_extra_tr.UpdateEventData(order, Id, Param, GetCurReward(), 1);
                changed = true;
            }
            return changed;
        }

        #endregion
        #region ISpawnEffectWithTrail

        public string trail_res_key = "event_orderrate_default:fx_orderrate_trail.prefab";
        public string order_trail_key = "fat_guide:fx_common_trail.prefab";

        void ISpawnEffectWithTrail.AddTrail(MBItemView view, Tween tween)
        {
            var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.MiddleStatus);
            view.transform.Find("Root/Content/Icon").GetComponent<Image>().color = Color.clear;
            GameObjectPoolManager.Instance.CreateObject(trail_res_key, effRoot, trail =>
            {
                trail.SetActive(false);
                trail.transform.position = view.transform.position;
                trail.SetActive(true);
                var script = trail.GetOrAddComponent<MBAutoRelease>();
                var animator = trail.transform.Find("Icon").GetComponent<Animator>();
                animator.SetInteger("State", 0);
                var icon = trail.transform.Find("Icon").GetComponent<UIImageRes>();
                icon.SetImage(Game.Manager.configMan.GetEventOrderRateBoxConfig(curShowPhase).OrderInfo);
                script.Setup(trail_res_key, 3f);
                trail.transform.Find("particle").gameObject.SetActive(false);

                IEnumerator par()
                {
                    yield return new WaitForSeconds(1);
                    trail.transform.Find("particle").gameObject.SetActive(true);
                }
                Game.Instance.StartCoroutineGlobal(par());
                // 显示head粒子
                animator.transform.localScale = Vector3.zero;
                if (tween.IsPlaying())
                {
                    var act = tween.onUpdate;
                    tween.OnUpdate(() =>
                    {
                        act?.Invoke();
                        animator.transform.localScale = view.transform.localScale;
                        trail.transform.position = view.transform.position;
                    });
                    var act_complete = tween.onComplete;
                    tween.OnComplete(() =>
                    {
                        act_complete?.Invoke();
                        trail.transform.position = BoardUtility.GetWorldPosByCoord(view.data.coord);
                        IEnumerator item()
                        {
                            yield return new WaitForSeconds(UIFlyConfig.Instance.durationShowItemOrderRate);
                            view.transform.Find("Root/Content/Icon").GetComponent<Image>().color = Color.white;
                        }
                        Game.Instance.StartCoroutineGlobal(item());
                        if (trail != null)
                        {
                            // 隐藏head粒子
                            trail.transform.Find("particle").gameObject.SetActive(false);
                        }
                        animator.SetInteger("State", _curPhase);
                    });
                }
            });
        }
        #endregion
        private void TryAddPhase(RewardCommitData data)
        {
            if (data.rewardType != ObjConfigType.Coin) return;
            if (data.reason == ReasonString.sell_item) return;
            phase += data.rewardCount;
            CheckMilestone();
        }

        private bool waitCheck = false;
        private void CheckMilestone()
        {
            var i = 0;
            foreach (var milestone in _milestoneList)
            {
                if (phase >= milestone)
                {
                    i++;
                }
            }
            if (i > _curPhase)
            {
                _curPhase = i;
                if (!waitCheck)
                {
                    waitCheck = true;
                    IEnumerator delay()
                    {
                        yield return new WaitForSeconds(1.5f);
                        Game.Manager.screenPopup.TryQueue(StartPopup, PopupType.Login);
                        curShowPhase = _curPhase;
                        waitCheck = false;
                    }

                    Game.Instance.StartCoroutineGlobal(delay());
                }
                DataTracker.event_orderrate_milestone.Track(this, 1, _curPhase == _milestoneList.Count, _curPhase, Reward3.Item2, _detailID);
            }
        }

        private void InitPhase()
        {
            var i = 0;
            foreach (var milestone in _milestoneList)
            {
                if (phase >= milestone)
                {
                    i++;
                }
            }
            _curPhase = i;
            curShowPhase = _curPhase;
        }
    }
}
