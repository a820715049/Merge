/**
 * @Author: handong.liu
 * @Date: 2021-02-20 16:15:45
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using fat.rawdata;

namespace FAT.Merge
{
    public interface IMergeEnvironment
    {
        long GetTimestamp();
        // MergeWorld GetWorld();
        bool IsFeatureEnable(MergeFeatureType feature);
        #region  order
        bool CanInventoryItemUseForOrder => true;
        bool IsOrderItem(int id);
        #endregion
        #region item global state

        IDictionary<int, int> GetFixedItemOutputDB();
        IDictionary<int, int> GetFixedCategoryOutputDB();

        void OnItemShowInView(int tid);
        #endregion
        #region  config
        Global GetGlobalConfig();
        ObjBasic GetItemConfig(int id);
        ObjMergeItem GetItemMergeConfig(int id);
        ItemComConfig GetItemComConfig(int id);
        MergeItemCategory GetCategoryByItem(int id);
        MergeFixedOutput GetFixedOutputConfig(int categoryId);
        MergeFixedItem GetFixedOutputByItemConfig(int itemId);
        MergeRuledOutput GetRuledOutputConfig(int itemId);
        MergeRule GetMergeRuleByItem(int tid);
        fat.rawdata.MergeGrid GetMergeGridConfig(int tid);
        bool TryGetDropLimitItemConfig(int tid, out DropLimitItem cfg);
        #endregion
        #region item add
        RewardCommitData CollectBonus(int bonusId, int bonusCount);
        RewardCommitData SellItem(int id, int count);
        // RewardCommitData SellBubble(int coinCount);
        #endregion
        #region energy
        bool CanUseEnergy(int cost);
        bool UseEnergy(int energy, ReasonString reason);
        void SwitchEnergyBoostState();  //切换能量加倍开关状态
        bool IsInEnergyBoost(); //目前是否处于能量加倍状态
        EnergyBoostState GetEnergyBoostState(); //返回能量倍率状态
        int GetNextLevelItemId(int curItemId, int nextLevel); //传入物品id 返回其所在合成链的下一等级的物品id
        #endregion
        #region diamond
        bool CanUseGem(int cost);
        bool UseGem(int gem, ReasonString reason);
        #endregion
        #region coin
        bool CanUseCoin(int cost);
        bool UseCoin(int coin, ReasonString reason);
        #endregion
        #region misc
        int GetBoardLevel();    // 主棋盘: 合成等级 / 活动棋盘: 活动进度等
        bool TryIncMergeTestSpawnBubbleCount(int tid);         //试图一次固定产出泡泡逻辑，返回是否应该产出泡泡，同时尝试次数+1
        bool IsBubbleGuidePassed();
        bool IsSpeedupGuidePassed();
        bool CanMergeProduceCoin();
        MergeMixCost GetMergeMixCostConfig(int costId);
        MergeTapCost GetMergeTapCostConfig(int costId);
        MergeTapCost FindPossibleCost(IList<int> costIdList);                 // 服务于MergeTapCost
        MergeTapCost FindCostByItem(IList<int> costIdList, Item item);  // 服务于MergeTapCost
        OrderBoxDetail GetOrderBoxDetailConfig(int boxId);
        int GetMergeLevel();
        fat.gamekitdata.MergeGlobal GetGlobalData();
        int GetPlayerTestGroup(int groupId);
        void FillGlobalMergeBonusHandlers(List<Merge.IMergeBonusHandler> container);
        void FillGlobalSpawnBonusHandlers(List<Merge.ISpawnBonusHandler> container);
        void FillGlobalDisposeBonusHandlers(List<Merge.IDisposeBonusHandler> container);
        #endregion
        #region  notify
        void NotifyItemMerge(Item newItem);
        void NotifyItemUse(Item newItem, ItemComponentType usedComponent);
        void NotifyItemEvent(Item newItem, Merge.ItemEventType ev);
        #endregion
        #region post card
        int CalculatePostCardPercentage(Item item);
        #endregion
    }
}