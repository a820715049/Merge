/**
 * @Author: handong.liu
 * @Date: 2021-02-25 20:19:24
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    public class GameMergeEnv : IMergeEnvironment
    {
        #region IMergeEnvironment
        bool IMergeEnvironment.IsFeatureEnable(MergeFeatureType feature)
        {
            switch (feature)
            {
                case MergeFeatureType.Bubble:
                    return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureBubble);
                case MergeFeatureType.EnergyBoost:
                    return EnergyBoostUtility.AnyEnergyBoostFeatureReady();
            }
            return true;
        }
        long IMergeEnvironment.GetTimestamp()
        {
            return Game.Instance.GetTimestamp();
        }
        // MergeWorld IMergeEnvironment.GetWorld()
        // {
        //     return Game.Manager.mergeWorldMan.world;
        // }
        void IMergeEnvironment.OnItemShowInView(int tid)
        {
            Game.Manager.mergeItemMan.OnItemShow(tid);
        }
        IDictionary<int, int> IMergeEnvironment.GetFixedCategoryOutputDB()
        {
            return Game.Manager.mergeItemMan.fixedCategoryOutputDB;
        }
        IDictionary<int, int> IMergeEnvironment.GetFixedItemOutputDB()
        {
            return Game.Manager.mergeItemMan.fixedItemOutputDB;
        }
        Global IMergeEnvironment.GetGlobalConfig()
        {
            return Game.Manager.configMan.globalConfig;
        }
        ObjBasic IMergeEnvironment.GetItemConfig(int id)
        {
            return Game.Manager.objectMan.GetBasicConfig(id);
        }
        ObjMergeItem IMergeEnvironment.GetItemMergeConfig(int id)
        {
            return Game.Manager.objectMan.GetMergeItemConfig(id);
        }
        ItemComConfig IMergeEnvironment.GetItemComConfig(int id)
        {
            return Game.Manager.mergeItemMan.GetItemComConfig(id);
        }
        MergeItemCategory IMergeEnvironment.GetCategoryByItem(int id)
        {
            return Game.Manager.mergeItemMan.GetCategoryConfig(Game.Manager.mergeItemMan.GetItemCategoryId(id));
        }
        MergeFixedOutput IMergeEnvironment.GetFixedOutputConfig(int categoryId)
        {
            return Game.Manager.mergeItemMan.GetFixedOutputConfig(categoryId);
        }
        MergeFixedItem IMergeEnvironment.GetFixedOutputByItemConfig(int itemId)
        {
            return Game.Manager.mergeItemMan.GetFixedOutputByItemConfig(itemId);
        }
        MergeRuledOutput IMergeEnvironment.GetRuledOutputConfig(int itemId)
        {
            return Game.Manager.configMan.GetMergeRuledOutputConfigs()?.GetDefault(itemId);
        }
        MergeRule IMergeEnvironment.GetMergeRuleByItem(int tid)
        {
            return Game.Manager.mergeItemMan.GetMergeRuleByItem(tid);
        }
        fat.rawdata.MergeGrid IMergeEnvironment.GetMergeGridConfig(int tid)
        {
            return Game.Manager.mergeBoardMan.GetMergeGridConfig(tid);
        }
        bool IMergeEnvironment.TryGetDropLimitItemConfig(int tid, out DropLimitItem cfg)
        {
            return Game.Manager.mergeItemMan.TryGetDropLimitItemConfig(tid, out cfg);
        }
        RewardCommitData IMergeEnvironment.CollectBonus(int bonusId, int bonusCount)
        {
            return Game.Manager.rewardMan.BeginReward(bonusId, bonusCount, ReasonString.use_item);
        }
        RewardCommitData IMergeEnvironment.SellItem(int id, int count)
        {
            return Game.Manager.rewardMan.BeginReward(id, count, ReasonString.sell_item);
        }
        // RewardCommitData IMergeEnvironment.SellBubble(int coinCount)
        // {
        //     return Game.Manager.rewardMan.BeginReward(Game.Manager.coinMan.GetIdByCoinType(CoinType.MergeCoin), coinCount, ReasonString.sell_item);
        // }
        bool IMergeEnvironment.CanUseEnergy(int cost)
        {
            return Game.Manager.mergeEnergyMan.CanUseEnergy(cost);
        }
        bool IMergeEnvironment.UseEnergy(int energy, ReasonString reason)
        {
            return Game.Manager.mergeEnergyMan.UseEnergy(energy, reason);
        }
        void IMergeEnvironment.SwitchEnergyBoostState()
        {
            SettingManager.Instance.OnSwitchEnergyBoostState();
        }
        bool IMergeEnvironment.IsInEnergyBoost()
        {
            return SettingManager.Instance.EnergyBoostIsOn && Env.Instance.IsFeatureEnable(MergeFeatureType.EnergyBoost);
        }
        EnergyBoostState IMergeEnvironment.GetEnergyBoostState()
        {
            return (EnergyBoostState)SettingManager.Instance.EnergyBoostState;
        }
        int IMergeEnvironment.GetNextLevelItemId(int curItemId, int nextLevel)
        {
            return Game.Manager.mergeItemMan.GetNextLevelItemId(curItemId, nextLevel);
        }
        bool IMergeEnvironment.CanUseCoin(int cost)
        {
            return Game.Manager.coinMan.GetCoin(CoinType.MergeCoin) >= cost;
        }
        bool IMergeEnvironment.UseCoin(int coin, ReasonString reason)
        {
            return Game.Manager.coinMan.UseCoin(CoinType.MergeCoin, coin, reason);
        }
        bool IMergeEnvironment.CanUseGem(int cost)
        {
            return Game.Manager.coinMan.GetCoin(CoinType.Gem) >= cost;
        }
        bool IMergeEnvironment.UseGem(int gem, ReasonString reason)
        {
            return Game.Manager.coinMan.UseCoin(CoinType.Gem, gem, reason);
        }
        bool IMergeEnvironment.IsOrderItem(int id)
        {
            return false;
            // FAT_TODO
            // return Game.Instance.schoolMan.IsTaskItem(id) || Game.Instance.dailyOrderMan.IsTaskItem(id);
        }
        #endregion
        #region  misc
        int IMergeEnvironment.GetBoardLevel()
        {
            return Game.Manager.mergeLevelMan.level;
        }
        bool IMergeEnvironment.TryIncMergeTestSpawnBubbleCount(int tid)
        {
            return Game.Manager.mergeItemMan.TryIncMergeTestSpawnBubbleCount(tid);
        }
        bool IMergeEnvironment.IsBubbleGuidePassed()
        {
            return true;
            // return GuideMergeManager.Instance.IsGuideFinished(Constant.kMergeBubbleGuideId);
        }
        bool IMergeEnvironment.IsSpeedupGuidePassed()
        {
            return true;
            // return GuideMergeManager.Instance.IsGuideFinished(Constant.kSpeedupGuideId);
        }
        bool IMergeEnvironment.CanMergeProduceCoin()
        {
            return true;
            // FAT_TODO
            // return Game.Instance.schoolMan.IsTaskCompleted(3);
        }
        MergeMixCost IMergeEnvironment.GetMergeMixCostConfig(int costId)
        {
            return Game.Manager.mergeItemMan.GetMergeMixCostConfig(costId);
        }
        MergeTapCost IMergeEnvironment.GetMergeTapCostConfig(int costId)
        {
            return Game.Manager.mergeItemMan.GetMergeTapCostConfig(costId);
        }
        MergeTapCost IMergeEnvironment.FindPossibleCost(IList<int> costIdList)
        {
            var tracer = Game.Manager.mergeBoardMan.activeTracer;
            if (tracer != null)
            {
                var dict = tracer.GetCurrentActiveBoardItemCount();
                foreach (var costCfgId in costIdList)
                {
                    var _cost = Env.Instance.GetMergeTapCostConfig(costCfgId);
                    if (_cost != null)
                    {
                        if (_cost.Cost <= 0 ||
                            _cost.Cost == Constant.kMergeEnergyObjId ||
                            dict.ContainsKey(_cost.Cost))
                        {
                            return _cost;
                        }
                    }
                }
            }
            return null;
        }
        MergeTapCost IMergeEnvironment.FindCostByItem(IList<int> costIdList, Item item)
        {
            foreach (var costCfgId in costIdList)
            {
                var _cost = Env.Instance.GetMergeTapCostConfig(costCfgId);
                if (_cost != null && _cost.Cost == item.tid)
                {
                    return _cost;
                }
            }
            return null;
        }
        OrderBoxDetail IMergeEnvironment.GetOrderBoxDetailConfig(int boxId)
        {
            return Game.Manager.mergeItemMan.GetOrderBoxDetailConfig(boxId);
        }
        int IMergeEnvironment.GetMergeLevel()
        {
            return Game.Manager.mergeLevelMan.level;
        }
        fat.gamekitdata.MergeGlobal IMergeEnvironment.GetGlobalData()
        {
            return Game.Manager.mergeBoardMan.globalData;
        }
        int IMergeEnvironment.GetPlayerTestGroup(int groupId)
        {
            return 0;
            // FAT_TODO
            // return Game.Instance.playerGroupMan.GetGroup(groupId);
        }
        void IMergeEnvironment.FillGlobalMergeBonusHandlers(System.Collections.Generic.List<Merge.IMergeBonusHandler> container)
        {
            Game.Manager.mergeBoardMan.FillGlobalMergeBonusHandler(container);
        }
        void IMergeEnvironment.FillGlobalSpawnBonusHandlers(List<Merge.ISpawnBonusHandler> container)
        {
            Game.Manager.mergeBoardMan.FillGlobalSpawnBonusHandler(container);
        }
        void IMergeEnvironment.FillGlobalDisposeBonusHandlers(List<Merge.IDisposeBonusHandler> container)
        {
            Game.Manager.mergeBoardMan.FillGlobalDisposeBonusHandler(container);
        }
        #endregion
        #region  notify
        void IMergeEnvironment.NotifyItemMerge(Item newItem)
        {
            // FAT_TODO
            // Game.Instance.targetMan.OnMergeItem();
            // Game.Instance.pinataMan.OnItemMerge();
            // Game.Instance.taskMan.OnMerge(newItem);
            MessageCenter.Get<MSG.GAME_BOARD_ITEM_MERGE>().Dispatch(newItem);
        }
        void IMergeEnvironment.NotifyItemUse(Item target, ItemComponentType usedComponent)
        {
            if (usedComponent == ItemComponentType.ClickSouce)
            {
                // FAT_TODO
                // Game.Instance.targetMan.OnTapSource(target);
            }
        }
        void IMergeEnvironment.NotifyItemEvent(Merge.Item newItem, Merge.ItemEventType ev)
        {
            MessageCenter.Get<MSG.GAME_MERGE_ITEM_EVENT>().Dispatch(newItem, ev);
        }
        #endregion
        #region postcard
        int IMergeEnvironment.CalculatePostCardPercentage(Item item)
        {
            // if(item.world != Game.Instance.mergeWorldMan.world)
            {
                return 0;
            }
            // if(!Game.Instance.postCardMan.IsPostCardSystemOpen())
            // {
            //     return 0;
            // }
            // return Game.Instance.postCardMan.CalculatePostCardPercentage(ItemUtility.GetItemLevel(item.tid) - 1);
        }
        #endregion
    }
}