/*
 * @Author: qun.chao
 * @Date: 2023-11-23 12:23:49
 */

using System.Collections.Generic;
using Config;
using fat.rawdata;
using EL;
using EventType = fat.rawdata.EventType;
using System;

namespace FAT
{
    public class GuideRequireChecker
    {
        public enum UIState
        {
            BoardMain = 0, // 主棋盘
            SceneMain = 1, // 主场景
            SceneBuilding = 2, // 主场景 且 特定建筑ui显示时
            OutOfEnergy = 3, // 体力不足
            MainShop = 4, // 主商店
            Bag = 5, // 背包
            CardPack = 6, // 卡包
            CardAlbum = 7, // 卡册
            CardSet = 8, //卡组
            DailyEvent = 9, //每日任务里程碑
            DecoratePick = 10, //装饰选择界面
            MiniBoard = 11, //迷你棋盘界面
            Digging = 12, //挖沙活动UI
            Pachinko = 13, //弹珠界面
            MiniBoardMulti = 14,
            Guess = 15,
            Bingo = 16, //bingo活动
            MineBoard = 17, //矿洞活动
            FishBoard = 18, //钓鱼活动
            FarmBoard = 19, //农场棋盘
            Fight = 20,
            WishBoard = 21,
            DecorateStart = 22,
            WeeklyRaffle = 23, //签到抽奖
            Boost4XGuide = 24, //是否满足4倍加速引导弹出条件
            MultiRanking = 25,
            MineCartBoard = 26, //矿车棋盘
        }

        private UIManager uiMan => UIManager.Instance;
        private int mLastCollectedItemTid;

        public void Reset()
        {
            mLastCollectedItemTid = 0;
        }

        public bool IsMatchRequirement(IList<string> requires)
        {
            return _IsMatchRequirement(requires);
        }

        private bool _IsMatchRequirement(IList<string> requires)
        {
            for (var i = 0; i < requires.Count; i++)
                if (!_CheckRequire(requires[i].ConvertToGuideMergeRequire()))
                    return false;

            return true;
        }

        private bool _CheckRequire(GuideMergeRequire require)
        {
            var value = require.Value;
            var extra = require.Extra;
            switch (require.Type)
            {
                case GuideMergeRequireType.Uistate:
                    return _IsRequireUIState(value, extra);
                case GuideMergeRequireType.BoardItem:
                    return _IsRequireBoardItem(value, require.Extra);
                case GuideMergeRequireType.BoardSandItem:
                    return _IsRequireBoardSandItem(value, require.Extra);
                case GuideMergeRequireType.BoardBubbleItem:
                    return _IsRequireBubbleItem(value, require.Extra);
                case GuideMergeRequireType.BoardOrderIng:
                    return _IsRequireOnGoingOrderNum(value);
                case GuideMergeRequireType.OrderCommittable:
                case GuideMergeRequireType.TaskCommittable:
                    return _IsRequireOrderCommittable(value);
                case GuideMergeRequireType.OrderComplete:
                case GuideMergeRequireType.TaskComplete:
                    return _IsRequireOrderComplete(value);
                case GuideMergeRequireType.OrderUncomplete:
                    return _IsRequireOrderUncomplete(value);
                case GuideMergeRequireType.CanLevelUp:
                    return _IsRequireCanLevelUp(value);
                case GuideMergeRequireType.Level:
                    return _IsRequireLevel(value);
                case GuideMergeRequireType.BoardReward:
                    return _IsRequireBoardReward(value);

                case GuideMergeRequireType.SelectItem:
                    return _IsRequireSelectItem(value);
                case GuideMergeRequireType.BubbleSelected:
                    return _IsRequireSelectBubble(value);

                case GuideMergeRequireType.CollectItem:
                    return _IsRequireCollectItem(value);
                case GuideMergeRequireType.Cditem:
                    return _IsRequireBoardItemCoolDown(value);
                case GuideMergeRequireType.CanOutput:
                    return _IsRequireBoardItemOutput(value);
                case GuideMergeRequireType.GiftBoxItem:
                    return _IsRequireGiftBoxFirstItem(value);
                case GuideMergeRequireType.BoardRestPlace:
                    return _IsRequireMainBoardEmptyGridNum(value);

                case GuideMergeRequireType.BoardNoMatch:
                    return _IsRequireBoardNoMatch();
                case GuideMergeRequireType.BagEmptyGridNum:
                    return _IsRequireBagEmptyGridNum(value);
                case GuideMergeRequireType.BagInUseGridNum:
                    return _IsRequireBagInUseGridNum(value);

                case GuideMergeRequireType.BuildingCanBuy:
                    return _IsRequireBuildingCanBuy(value);
                case GuideMergeRequireType.BuildingCanUpgrade:
                    return _IsRequireBuildingCanUpgrade(value, extra);
                case GuideMergeRequireType.BuildingCompleted:
                    return _IsRequireBuildingLevel(value, extra);

                case GuideMergeRequireType.EventTypeActive:
                    return _IsRequireEventTypeActive(value);
                case GuideMergeRequireType.NotNewUserSession:
                    return _IsRequireNotNewUserActivity();
                case GuideMergeRequireType.Energy:
                    return _isOnLackOfEnergy(value);
                case GuideMergeRequireType.MiniGiftBoxNum:
                    return _CheckMiniBoardBoxNum(value);
                case GuideMergeRequireType.MiniBoardItemSame:
                    return _CheckMiniBoardItemSame();
                case GuideMergeRequireType.DiggingTokenNum:
                    return _CheckDiggingTokenNum(value);
                case GuideMergeRequireType.SceneReady:
                    return _CheckSceneReady();
                case GuideMergeRequireType.PachinkoTokenNum:
                    return _CheckPachinkoToken(value);
                case GuideMergeRequireType.ItemCanBoost:
                    return _CheckItemCanBoost();
                case GuideMergeRequireType.MiniMultiGiftBoxNum:
                    return _CheckMiniBoardMultiBoxNum(value);
                case GuideMergeRequireType.MiniMultiBoardItemSame:
                    return _CheckMiniBoardMultiItemSame();
                case GuideMergeRequireType.MiniMultiDoorOpen:
                    return _CheckMiniBoardMultiEnterNext();
                case GuideMergeRequireType.GuessTokenNum:
                    return _GuessColorTokenNum(value);
                case GuideMergeRequireType.GuessItemRight:
                    return _GuessTotalRightItem(value);
                case GuideMergeRequireType.GuessPutRepeatedItem:
                    return _GuessPutRepeatedItem();
                case GuideMergeRequireType.BingoCompleteNum:
                    return _CheckBingoCompleteNum(value);
                case GuideMergeRequireType.MineBonusItemMax:
                    return _CheckMineBonusItemMax(value);
                case GuideMergeRequireType.FirstFishUnlock:
                    return _CheckFirstFishUnlock(value);
                case GuideMergeRequireType.DecoratePreview:
                    return _CheckCanPreview();
                case GuideMergeRequireType.StockItem:
                    return _CheckStockItem(value, extra);
                case GuideMergeRequireType.ClawOrderPickSuccess:
                    return _CheckClawOrderPickSuccess();
                case GuideMergeRequireType.LevelCanBoost:
                    return _CheckLevelCanBoost(value);
                case GuideMergeRequireType.MineCartRoundComplete:
                    return _CheckMineCartBoardRoundFinish();
            }

            return false;
        }



        public bool IsMatchUIState(int state, int extra)
        {
            return _IsRequireUIState(state, extra);
        }

        private bool _IsLayerEmpty(UILayer layer)
        {
            return uiMan.GetLayerRootByType(layer).childCount < 1;
        }

        private bool _IsBoardReady(UIResource res)
        {
            if (uiMan.IsOpen(UIConfig.UIGuide))
                // 如果guide已经在显示 则不用考虑PopUpList
                // 1.UIGuide加载前, 满足条件, 尝试加载UI并播放guide
                // 2.UIGuide异步加载后, popUpList=0的条件可能不再满足, 导致guide不能继续执行
                // 解决方案: 当UIGuide已经在显示时, 可以不考虑popUpList
                return uiMan.IsOpen(res) &&
                       !uiMan.IsPause(res) &&
                       _IsLayerEmpty(UILayer.AboveStatus) &&
                       _IsLayerEmpty(UILayer.Loading) &&
                       Game.Manager.mergeBoardMan.activeWorld == Game.Manager.mainMergeMan.world;

            return uiMan.IsOpen(res) &&
                   !uiMan.IsPause(res) &&
                   _IsLayerEmpty(UILayer.AboveStatus) &&
                   _IsLayerEmpty(UILayer.Loading) &&
                   Game.Manager.mergeBoardMan.activeWorld == Game.Manager.mainMergeMan.world &&
                   Game.Manager.screenPopup.list.Count == 0;
        }

        private bool _IsFishBoardReady()
        {
            var fish = Game.Manager.activity.LookupAny(EventType.Fish) as ActivityFishing;
            var res = fish?.VisualBoard.res.ActiveR ?? UIConfig.UIActivityFishMain;
            var mainBoardOpen = UIManager.Instance.IsOpen(res);
            return mainBoardOpen && !UIManager.Instance.IsBlocked && UIManager.Instance.LoadingCount == 0;
        }

        private bool _IsFightBoardReady()
        {
            var fight = Game.Manager.activity.LookupAny(EventType.Fight) as FightBoardActivity;
            var res = fight?.BoardRes.res.ActiveR ?? UIConfig.UIActivityFightMain;
            var mainBoardOpen = UIManager.Instance.IsOpen(res);
            return mainBoardOpen && !UIManager.Instance.IsBlocked && UIManager.Instance.LoadingCount == 0;
        }

        private bool _IsSceneReady()
        {
            var from = (int)UILayer.BelowStatus;
            var to = (int)UILayer.Modal;
            for (var i = from; i <= to; i++)
            {
                if (i == (int)UILayer.Status || i == (int)UILayer.Top) // status 和 guide 所在层
                    continue;
                var root = uiMan.GetLayerRootByType((UILayer)i);
                if (root.childCount > 0)
                    return false;
            }

            return true;
        }

        private bool _IsSceneBuildingPopUp(int id)
        {
            var state = Game.Manager.mapSceneMan.BuildingUIState(id);
            DebugEx.Warning($"[GUIDE] check building state {state}@{id}");
            return state == 1;
        }

        private bool _IsRequireUIState(int value, int extra)
        {
            if (UIManager.Instance.LoadingCount != 0)
                return false;
            switch ((UIState)value)
            {
                case UIState.BoardMain:
                    return _IsBoardReady(UIConfig.UIMergeBoardMain);
                case UIState.SceneMain:
                    return _IsSceneReady();
                case UIState.SceneBuilding:
                    return _IsSceneReady() && _IsSceneBuildingPopUp(extra);
                case UIState.OutOfEnergy:
                    return UIManager.Instance.IsOpen(UIConfig.UIOutOfEnergy);
                case UIState.MainShop:
                    return UIManager.Instance.IsOpen(UIConfig.UIShop);
                case UIState.Bag:
                    return UIManager.Instance.IsOpen(UIConfig.UIBag);
                case UIState.CardPack:
                    return UIManager.Instance.IsOpen(UIConfig.UICardPackOpen);
                case UIState.CardAlbum:
                    return UIManager.Instance.IsOpen(UIConfig.UICardAlbum) && _isCardAlbumPanel();
                case UIState.CardSet:
                    return UIManager.Instance.IsOpen(UIConfig.UICardAlbum) && _isCardGroupInfoPanel();
                case UIState.DailyEvent:
                    return UIManager.Instance.IsOpen(Game.Manager.dailyEvent.ActivityD?.TaskRes.ActiveR ??
                                                     UIConfig.UIDailyEvent);
                case UIState.DecoratePick:
                    return Game.Manager.decorateMan.GuideRequireCheck(extra);
                case UIState.MiniBoard:
                    return Game.Manager.miniBoardMan.CheckMiniBoardOpen();
                case UIState.Digging:
                    var actInst = Game.Manager.activity.LookupAny(EventType.Digging) as ActivityDigging;
                    return UIManager.Instance.IsOpen(actInst?.Res.ActiveR ?? UIConfig.UIDiggingMain);
                case UIState.Pachinko:
                    return UIManager.Instance.IsOpen(Game.Manager.pachinkoMan.GetActivity()?.MainResAlt.ActiveR ??
                                                     UIConfig.UIPachinkoMain);
                case UIState.MiniBoardMulti:
                    return Game.Manager.miniBoardMultiMan.CheckMiniBoardOpen();
                case UIState.Guess:
                    var guess = Game.Manager.activity.LookupAny(EventType.Guess) as ActivityGuess;
                    return UIManager.Instance.IsOpen(guess?.VisualMain.res.ActiveR ?? UIConfig.UIActivityGuess);
                case UIState.Bingo:
                    return _CheckBingoUIState();
                case UIState.MineBoard:
                    var mine = Game.Manager.activity.LookupAny(EventType.Mine) as MineBoardActivity;
                    return UIManager.Instance.IsOpen(mine?.BoardResAlt.ActiveR ?? UIConfig.UIMineBoardMain);
                case UIState.FishBoard:
                    return _IsFishBoardReady();
                case UIState.FarmBoard:
                    var farm = Game.Manager.activity.LookupAny(EventType.FarmBoard) as FarmBoardActivity;
                    return UIManager.Instance.IsOpen(farm?.VisualBoard.res.ActiveR ?? UIConfig.UIFarmBoardMain);
                case UIState.Fight:
                    return _IsFightBoardReady();
                case UIState.WishBoard:
                    var wish = Game.Manager.activity.LookupAny(EventType.WishBoard) as WishBoardActivity;
                    return UIManager.Instance.IsOpen(wish?.VisualUIBoardMain.res.ActiveR ?? UIConfig.UIWishBoardMain);
                case UIState.DecorateStart:
                    return Game.Manager.decorateMan.CheckGuideStart();
                case UIState.WeeklyRaffle:
                    var raffle = Game.Manager.activity.LookupAny(EventType.WeeklyRaffle) as ActivityWeeklyRaffle;
                    return UIManager.Instance.IsOpen(raffle?.MainPopUp.res.ActiveR ?? UIConfig.UIActivityWeeklyRaffleMain);
                case UIState.Boost4XGuide:
                    // 这里描述的是一个约束引导打开的条件，目的是限制引导不要意外打断其他UI
                    // 后续迷你棋盘也需要弹出这个引导的时候，在下面if里补个或的逻辑就可以了
                    bool isSuccess = false;
                    if (_IsBoardReady(UIConfig.UIMergeBoardMain))
                    {
                        isSuccess = true;
                    }
                    return isSuccess;
                case UIState.MineCartBoard:
                    var mineCart = Game.Manager.activity.LookupAny(EventType.MineCart) as MineCartActivity;
                    return UIManager.Instance.IsOpen(mineCart?.VisualBoard.res.ActiveR ?? UIConfig.UIMineCartBoardMain);
                case UIState.MultiRanking:
                    var multi = Game.Manager.activity.LookupAny(EventType.MultiplierRanking) as ActivityMultiplierRanking;
                    return UIManager.Instance.IsOpen(multi?.VisualUIRankingMain.res.ActiveR ?? UIConfig.UIMultiplyRankingMain);
            }

            return false;
        }

        private bool _IsRequireBubbleItem(int id, int num)
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            return BoardViewManager.Instance.HasBubbleItem(id, num);
        }

        private bool _IsRequireBoardItem(int id, int num)
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            return BoardViewManager.Instance.HasActiveItem(id, num);
        }

        private bool _IsRequireBoardSandItem(int id, int num)
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            return BoardViewManager.Instance.HasInSandItem(id, num);
        }

        /// <summary>
        /// 0表示要求为0个 / 非0表示至少有num个
        /// </summary>
        private bool _IsRequireOnGoingOrderNum(int num)
        {
            var cur = Game.Manager.mainOrderMan.GetActiveOrderNum();
            if (num == 0)
                return cur == 0;
            else
                return cur >= num;
        }

        private bool _IsRequireOrderCommittable(int orderId)
        {
            var order = Game.Manager.mainOrderMan.GetActiveCommonOrderById(orderId);
            if (order != null) return order.State == OrderState.Finished && order.Displayed;

            return false;
        }

        private bool _IsRequireOrderComplete(int orderId)
        {
            return Game.Manager.mainOrderMan.IsOrderCompleted(orderId);
        }

        private bool _IsRequireOrderUncomplete(int orderId)
        {
            return !_IsRequireOrderComplete(orderId);
        }

        private bool _IsRequireCanLevelUp(int level)
        {
            var mgr = Game.Manager.mergeLevelMan;
            if (mgr.canLevelup && level == mgr.displayLevel + 1) return true;

            return false;
        }

        private bool _IsRequireLevel(int level)
        {
            return Game.Manager.mergeLevelMan.level >= level;
        }

        private bool _IsRequireBoardReward(int tid)
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            return BoardViewManager.Instance.world.nextReward == tid;
        }

        private bool _IsRequireSelectItem(int tid)
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            return BoardViewManager.Instance.GetCurrentBoardInfoItemTid() == tid;
        }

        private bool _IsRequireSelectBubble(int tid)
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            var curItem = BoardViewManager.Instance.GetCurrentBoardInfoItem();
            if (curItem == null)
                return false;
            if (!curItem.HasComponent(Merge.ItemComponentType.Bubble))
                return false;
            if (tid > 0 && curItem.tid != tid)
                return false;
            return true;
        }

        private bool _IsRequireCollectItem(int tid)
        {
            return tid == mLastCollectedItemTid;
        }

        private bool _IsRequireBoardItemCoolDown(int tid)
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            return BoardViewManager.Instance.HasClickSourceReviving(tid);
        }

        private bool _IsRequireBoardItemOutput(int tid)
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            return BoardViewManager.Instance.HasClickSourceCanOutput(tid);
        }

        private bool _IsRequireGiftBoxFirstItem(int tid)
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            var world = BoardViewWrapper.GetCurrentWorld();
            if (world.rewardCount > 0 && world.nextReward == tid)
                return true;
            return false;
        }

        private bool _IsRequireMainBoardEmptyGridNum(int num)
        {
            if (num == 0)
                return Game.Manager.mainMergeMan.world.activeBoard.emptyGridCount == num;
            return Game.Manager.mainMergeMan.world.activeBoard.emptyGridCount >= num;
        }

        private bool _IsRequireBoardNoMatch()
        {
            if (!BoardViewManager.Instance.IsReady)
                return false;
            var checker = BoardViewManager.Instance.checker;
            return !checker.HasMatchPair();
        }

        private bool _IsRequireBagEmptyGridNum(int num)
        {
            if (num == 0)
                return Game.Manager.bagMan.ItemBagEmptyGirdNum == num;
            return Game.Manager.bagMan.ItemBagEmptyGirdNum >= num;
        }

        private bool _IsRequireBagInUseGridNum(int num)
        {
            var mgr = Game.Manager.bagMan;
            var inUseNum = mgr.CurItemBagUnlockId - mgr.ItemBagEmptyGirdNum;
            if (num == 0)
                return inUseNum == 0;
            return inUseNum >= num;
        }

        private bool _IsRequireBuildingCanBuy(int id)
        {
            // 筛选可购买的建筑
            var ret = Game.Manager.mapSceneMan.BuildingState(id, 1, true);
            if (ret == 1) return true;

            return false;
        }

        private bool _IsRequireBuildingCanUpgrade(int id, int level)
        {
            var ret = Game.Manager.mapSceneMan.BuildingState(id, level, false);
            if (ret == 1) return true;

            return false;
        }

        private bool _IsRequireBuildingLevel(int id, int level)
        {
            var ret = Game.Manager.mapSceneMan.BuildingState(id, level, false);
            if (ret == 2) return true;

            return false;
        }

        private bool _IsRequireEventTypeActive(int typeId)
        {
            return Game.Manager.activity.IsActive((EventType)typeId);
        }

        private bool _IsRequireNotNewUserActivity()
        {
            if (Game.Manager.activity.IsActive(EventType.NewUser))
                return false;

            return true;
        }

        private bool _isCardAlbumPanel()
        {
            if (UIManager.Instance.TryGetUI(UIConfig.UICardAlbum) as UICardAlbum != null)
                return (UIManager.Instance.TryGetUI(UIConfig.UICardAlbum) as UICardAlbum).IsOverViewPanel();
            else
                return false;
        }

        private bool _isCardGroupInfoPanel()
        {
            if (UIManager.Instance.TryGetUI(UIConfig.UICardAlbum) as UICardAlbum != null)
                return (UIManager.Instance.TryGetUI(UIConfig.UICardAlbum) as UICardAlbum).IsGroupInfoPanel();
            else
                return false;
        }

        private bool _isOnLackOfEnergy(int num)
        {
            return Game.Manager.mergeEnergyMan.EnergyAfterFly <= num;
        }

        private bool _CheckMiniBoardBoxNum(int num)
        {
            if (!Game.Manager.miniBoardMan.IsValid)
                return false;
            var world = Game.Manager.miniBoardMan.World;
            return world.rewardCount > num;
        }

        private bool _CheckMiniBoardMultiBoxNum(int num)
        {
            if (!Game.Manager.miniBoardMultiMan.IsValid)
                return false;
            var world = Game.Manager.miniBoardMultiMan.World;
            return world?.rewardCount > num;
        }

        private bool _CheckMiniBoardItemSame()
        {
            if (!Game.Manager.miniBoardMan.IsValid)
                return false;
            return BoardViewManager.Instance.checker.HasMatchPair();
        }

        private bool _CheckMiniBoardMultiItemSame()
        {
            if (!Game.Manager.miniBoardMultiMan.IsValid)
                return false;
            return BoardViewManager.Instance.checker.HasMatchPair();
        }

        private bool _CheckMiniBoardMultiEnterNext()
        {
            if (!Game.Manager.miniBoardMultiMan.IsValid)
                return false;
            return Game.Manager.miniBoardMultiMan.CheckHasNextBoard() &&
                   Game.Manager.miniBoardMultiMan.CheckCanEnterNextRound();
        }

        private bool _CheckDiggingTokenNum(int num)
        {
            Game.Manager.activity.LookupActive(EventType.Digging).TryGetByIndex(0, out var act);
            if (act == null)
                return false;
            return (act as ActivityDigging).GetKeyNum() >= num;
        }

        private bool _CheckSceneReady()
        {
            return Game.Manager.mapSceneMan.scene?.Ready ?? false;
        }

        private bool _CheckPachinkoToken(int num)
        {
            return Game.Manager.pachinkoMan.GetCoinCount() >= num;
        }

        private bool _CheckItemCanBoost()
        {
            var item = BoardViewManager.Instance.GetCurrentBoardInfoItem();
            if (item != null && item.TryGetItemComponent(out Merge.ItemClickSourceComponent com))
                return com.config.IsBoostable;
            return false;
        }

        private bool _GuessColorTokenNum(int num)
        {
            if (!Game.Manager.activity.LookupAny(EventType.Guess, out var acti))
                return false;
            return (acti as ActivityGuess)?.Token >= num;
        }

        private bool _GuessTotalRightItem(int num)
        {
            if (!Game.Manager.activity.LookupAny(EventType.Guess, out var acti) || acti is not ActivityGuess a)
                return false;
            return a.FirstCheck && a.CorrectCount >= num;
        }

        private bool _GuessPutRepeatedItem()
        {
            if (!Game.Manager.activity.LookupAny(EventType.Guess, out var acti))
                return false;
            return (acti as ActivityGuess)?.PutRepeatedItem ?? false;
        }

        private bool _CheckBingoCompleteNum(int num)
        {
            if (!Game.Manager.activity.LookupAny(EventType.ItemBingo, out var acti) || acti is not ActivityBingo a)
                return false;
            return a.CheckBingoComplete();
        }

        private bool _CheckBingoUIState()
        {
            var act = Game.Manager.activity.LookupAny(EventType.ItemBingo) as ActivityBingo;
            if (act == null)
                return false;
            return UIManager.Instance.IsOpen(act.MainRes.ActiveR) && act.CheckGroupStart() && act.IsMain &&
                UIManager.Instance.GetLayerRootByType(UILayer.SubStatus).childCount < 1;
        }

        private bool _CheckMineBonusItemMax(int num)
        {
            if (!Game.Manager.activity.LookupAny(EventType.Mine, out var acti) || acti is not MineBoardActivity a)
                return false;
            foreach (var item in a.ConfD.BonusItemMax)
            {
                if (Game.Manager.mergeBoardMan.activeTracer.GetCurrentActiveBoardItemCount().ContainsKey(item))
                {
                    return true;
                }
            }
            return false;
        }

        private bool _CheckFirstFishUnlock(int num)
        {
            if (!Game.Manager.activity.LookupAny(EventType.Fish, out var acti) || acti is not ActivityFishing a)
            {
                return false;
            }

            // 判断是否捕获到鱼
            foreach (var fishInfo in a.FishInfoList)
            {
                if (a.IsFishUnlocked(fishInfo.Id)) return true;
            }

            return false;
        }

        private bool _CheckCanPreview()
        {
            return Game.Manager.decorateMan.CheckCanPreview();
        }

        private bool _CheckClawOrderPickSuccess()
        {
            if (Game.Manager.activity.LookupAny(EventType.ClawOrder) is not ActivityClawOrder act)
                return false;
            return act.SelectedOrderId > 0;
        }

        private bool _CheckStockItem(int id, int num)
        {
            var world = Game.Manager.mergeBoardMan.activeWorld;
            return world.FindRewardCount(id) >= num;
        }

        private bool _CheckLevelCanBoost(int state)
        {
            var cfg = Game.Manager.configMan.GetEnergyBoostConfig(state);
            if (cfg != null)
            {
                return Game.Manager.mergeLevelMan.level >= cfg.ActiveLv;
            }
            return false;
        }
        private bool _CheckMineCartBoardRoundFinish()
        {
            if (!Game.Manager.activity.LookupAny(EventType.MineCart, out var acti) || acti is not MineCartActivity a)
                return false;
            return a.CanPlayFinishRoundGuide;
        }
    }
}
