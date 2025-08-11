/*
 * @Author: tang.yan
 * @Description: 图鉴系统 
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/klghl22357ib4tq4
 * @Date: 2023-11-15 11:11:07
 */

using System.Collections.Generic;
using EL;
using UnityEngine;
using fat.gamekitdata;
using fat.rawdata;
using static DataTracker;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    //图鉴系统棋子状态
    public enum HandbookItemState
    {
        Lock,       //锁定状态
        Preview,    //预显示状态
        Unlock,     //已解锁未领奖(如果没有配奖励 直接设为Received)
        Received,   //已解锁已领奖
    }

    public class HandbookMan : IGameModule, IUserDataHolder, ISecondUpdate
    {
        //图鉴入口是否显示红点
        public bool HandbookHasRP = false;

        //记录各图鉴大类中所有已解锁的子项 采用bitmap的形式 节省存储空间
        private Bitmap64 _handbookUnlockMask = new Bitmap64(Constant.kMergeItemIdBase);
        //记录各图鉴大类中所有已领奖的子项 采用bitmap的形式 节省存储空间
        private Bitmap64 _handbookReceivedMask = new Bitmap64(Constant.kMergeItemIdBase);
        //记录所有图鉴棋子的状态数据 key:棋子id
        private Dictionary<int, HandbookItemState> _itemStateMap = new Dictionary<int, HandbookItemState>();
        //缓存同一时间内解锁的图鉴 在统一时间点处理解锁的图鉴相关逻辑
        private List<int> _cacheNewUnlockItemIdList = new List<int>();
        //飞奖励起始位置
        private static Vector3 _rewardFromPos = Vector3.zero;

        public bool IsHandbookOpen()
        {
            return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureHandbook);
        }

        public void OpenUIHandbook()
        {
            if (!IsHandbookOpen())
                return;
            int groupId = GetNextRewardableGroupId();
            UIManager.Instance.OpenWindow(UIConfig.UIHandbook, groupId);
        }

        //解锁指定itemId对应的图鉴 refreshNow:是否立即刷新 默认为每秒统一刷新数据
        public void UnlockHandbookItem(int itemId, bool refreshNow = false)
        {
            _TrySetItemUnlock(itemId, refreshNow);
        }

        //锁定指定itemIdList对应的图鉴
        public void LockHandbookItem(IList<int> itemIdList = null)
        {
            _TrySetItemLock(itemIdList);
        }

        //尝试领取指定图鉴奖励
        public bool TryClaimHandbookReward(int itemId, Vector3 flyFromPos)
        {
            _rewardFromPos = flyFromPos;
            return _TryClaimHandbookReward(itemId);
        }

        //获取排序最靠前的可领奖的分组页签id
        public int GetNextRewardableGroupId()
        {
            int resultGroupId = 0;
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var groupIdList))
            {
                Game.Manager.mergeItemMan.FillCollectionCategoryOrdered(groupIdList);
                foreach (var groupId in groupIdList)
                {
                    int seriesId = GetNextRewardableSeriesId(groupId);
                    if (seriesId > 0)
                    {
                        resultGroupId = groupId;
                        break;
                    }
                }
            }
            return resultGroupId;
        }

        //获取指定分组页签下面 排序最靠前的可领奖的链条id
        public int GetNextRewardableSeriesId(int groupId)
        {
            int seriesId = -1;
            var mgr = Game.Manager.mergeItemMan;
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var seriesIdList))
            {
                mgr.FillSeriesInCategoryOrdered(groupId, seriesIdList, false);
                foreach (var id in seriesIdList)
                {
                    var config = mgr.GetCategoryConfig(id);
                    if (config != null)
                    {
                        foreach (var itemId in config.Progress)
                        {
                            if (IsItemCanClaim(itemId))
                            {
                                seriesId = id;
                                return seriesId;
                            }
                        }
                    }
                }
            }
            return seriesId;
        }

        //检查指定棋子对应链条中的其他棋子是否有可以领奖的
        public bool CheckHasRewardInChain(int itemId)
        {
            var categoryConfig = Game.Manager.mergeItemMan.GetCategoryConfigByItemId(itemId);
            if (categoryConfig == null)
                return false;
            foreach (var id in categoryConfig.Progress)
            {
                if (IsItemCanClaim(id))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsItemLock(int itemId)
        {
            return _itemStateMap.TryGetValue(itemId, out var state) &&
                   state is HandbookItemState.Lock;
        }

        public bool IsItemPreview(int itemId)
        {
            return _itemStateMap.TryGetValue(itemId, out var state) &&
                   state is HandbookItemState.Preview;
        }

        //图鉴棋子是否解锁
        public bool IsItemUnlocked(int itemId)
        {
            return _itemStateMap.TryGetValue(itemId, out var state) &&
                   state is HandbookItemState.Unlock or HandbookItemState.Received;
        }

        //图鉴棋子是否在缓存解锁List中
        public bool IsItemUnlockedInList(int itemId)
        {
            for (int i = 0; i < _cacheNewUnlockItemIdList.Count; i++)
            {
                if (itemId == _cacheNewUnlockItemIdList[i])
                    return true;
            }
            return false;
        }

        //图鉴棋子是否可领取对应奖励
        public bool IsItemCanClaim(int itemId)
        {
            return _itemStateMap.TryGetValue(itemId, out var state) &&
                   state is HandbookItemState.Unlock;
        }

        //图鉴棋子是否解锁且已领取奖励
        public bool IsItemReceived(int itemId)
        {
            return _itemStateMap.TryGetValue(itemId, out var state) &&
                   state is HandbookItemState.Received;
        }

        #region 数据处理相关

        public void Reset()
        {
            _handbookUnlockMask.Clear();
            _handbookReceivedMask.Clear();
            _itemStateMap.Clear();
            _cacheNewUnlockItemIdList.Clear();
        }

        public void LoadConfig() { }

        public void Startup() { }

        public void SetData(LocalSaveData archive)
        {
            _handbookUnlockMask.Clear();
            _handbookReceivedMask.Clear();
            var data = archive.ClientData.PlayerGameData.HandbookData;
            if (data != null)
            {
                _handbookUnlockMask.Reset(data.MergeItemUnlock);
                _handbookReceivedMask.Reset(data.MergeItemReward);
            }
            _InitItemStateMap();
        }

        public void FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.HandbookData ??= new HandbookData();
            data.HandbookData.MergeItemUnlock.AddRange(_handbookUnlockMask.data);
            data.HandbookData.MergeItemReward.AddRange(_handbookReceivedMask.data);
        }

        public void SecondUpdate(float dt)
        {
            _CheckCacheItemUnlockState();
        }

        //初始化所有棋子状态Map
        private void _InitItemStateMap()
        {
            _itemStateMap.Clear();
            var replaceMap = Game.Manager.configMan.GetItemReplaceMap();
            foreach (var info in replaceMap.Values)
            {
                //判断checkItemId对应的图鉴是否解锁 若已解锁则同步直接解锁replaceItemId
                var checkItemId = info.Id;
                var replaceItemId = info.ReplaceInto;
                if (_handbookUnlockMask.ContainsId(checkItemId))
                {
                    if (!_handbookUnlockMask.ContainsId(replaceItemId))
                        _handbookUnlockMask.AddId(replaceItemId);
                }
                if (_handbookReceivedMask.ContainsId(checkItemId))
                {
                    if (!_handbookReceivedMask.ContainsId(replaceItemId))
                        _handbookReceivedMask.AddId(replaceItemId);
                }
            }
            //已配置的全量数据
            var allItemCategoryMap = Game.Manager.mergeItemMan.GetItemCategoryMap();
            foreach (var map in allItemCategoryMap)
            {
                int itemId = map.Key;
                _CheckItemState(itemId, out var state);
                _itemStateMap.TryAdd(itemId, state);
            }
            //预告状态只会在其他棋子刷新成最终状态后才会刷新
            _RefreshAllItemPreviewState();
            //刷新红点状态
            _RefreshRedPointState();
        }

        //刷新所有棋子的状态 全量数据 只改不加
        //refreshItemIdList 只刷新指定的道具
        private void _RefreshAllItemState(IList<int> refreshItemIdList = null)
        {
            if (refreshItemIdList != null)
            {
                foreach (var itemId in refreshItemIdList)
                {
                    if (_itemStateMap.ContainsKey(itemId))
                    {
                        _CheckItemState(itemId, out var state);
                        _itemStateMap[itemId] = state;
                    }
                }
            }
            else
            {
                foreach (var itemId in _itemStateMap.Keys.ToList())
                {
                    _CheckItemState(itemId, out var state);
                    _itemStateMap[itemId] = state;
                }
            }
        }

        private void _RefreshItemState(int itemId)
        {
            if (_itemStateMap.ContainsKey(itemId))
            {
                _CheckItemState(itemId, out var state);
                _itemStateMap[itemId] = state;
            }
        }

        //刷新棋子的可预告状态 全量数据 只改不加  预告状态只会在其他棋子刷新成最终状态后才会刷新
        //refreshItemIdList 只刷新指定的道具
        private void _RefreshAllItemPreviewState(List<int> refreshItemIdList = null)
        {
            if (refreshItemIdList != null)
            {
                foreach (var itemId in refreshItemIdList)
                {
                    if (IsItemUnlocked(itemId))
                    {
                        _RefreshItemPreviewState(itemId);
                    }
                }
            }
            else
            {
                foreach (var map in _itemStateMap.ToList())
                {
                    HandbookItemState state = map.Value;
                    if (state is HandbookItemState.Unlock or HandbookItemState.Received)
                    {
                        _RefreshItemPreviewState(map.Key);
                    }
                }
            }
        }

        //检查单个棋子的状态
        private void _CheckItemState(int itemId, out HandbookItemState state)
        {
            //已解锁
            if (_handbookUnlockMask.ContainsId(itemId))
            {
                if (_handbookReceivedMask.ContainsId(itemId))
                {
                    //已解锁已领奖
                    state = HandbookItemState.Received;
                }
                else
                {
                    var itemConfig = Game.Manager.objectMan.GetMergeItemConfig(itemId);
                    if (itemConfig != null && itemConfig.Reward != "")
                    {
                        //已解锁 有配置的奖励 未领奖 
                        state = HandbookItemState.Unlock;
                    }
                    else
                    {
                        //已解锁 无配置的奖励
                        state = HandbookItemState.Received;
                    }
                }
            }
            //未解锁
            else
            {
                state = IsItemPreview(itemId) ? HandbookItemState.Preview : HandbookItemState.Lock;
            }
        }

        //刷新单个棋子可预告状态
        private void _RefreshItemPreviewState(int itemId)
        {
            var clickSourceConfig = Game.Manager.mergeItemMan.GetItemComConfig(itemId).clickSourceConfig;
            var autoSourceConfig = Game.Manager.mergeItemMan.GetItemComConfig(itemId).autoSourceConfig;
            //尝试刷新棋子的可预告状态 只有棋子当前为Lock状态时才会刷成Preview状态
            if (clickSourceConfig != null)
            {
                foreach (var costId in clickSourceConfig.CostId)
                {
                    var tapCost = Game.Manager.mergeItemMan.GetMergeTapCostConfig(costId);
                    if (tapCost != null)
                    {
                        foreach (var id in tapCost.Outputs.Keys)
                        {
                            if (IsItemLock(id))
                            {
                                _itemStateMap[id] = HandbookItemState.Preview;
                            }
                        }
                    }
                }
                foreach (var id in clickSourceConfig.OutputsFixed)
                {
                    if (IsItemLock(id))
                    {
                        _itemStateMap[id] = HandbookItemState.Preview;
                    }
                }
            }
            else if (autoSourceConfig != null)
            {
                foreach (var id in autoSourceConfig.Outputs)
                {
                    if (IsItemLock(id))
                    {
                        _itemStateMap[id] = HandbookItemState.Preview;
                    }
                }
            }
        }

        //设置棋子的解锁状态
        private void _TrySetItemUnlock(int itemId, bool refreshNow = false)
        {
            //如果记录成功 则缓存该id
            if (_handbookUnlockMask.AddId(itemId))
            {
                _cacheNewUnlockItemIdList.AddIfAbsent(itemId);
                if (refreshNow)
                    _CheckCacheItemUnlockState();
            }
        }

        //设置棋子的锁定状态
        private void _TrySetItemLock(IList<int> itemIdList = null)
        {
            if (itemIdList == null || itemIdList.Count < 1)
                return;
            //从mask记录中移除解锁和已领奖状态
            foreach (var itemId in itemIdList)
            {
                _handbookUnlockMask.RemoveId(itemId);
                _handbookReceivedMask.RemoveId(itemId);
            }
            //刷新棋子状态 这里只会把已解锁的刷成未解锁  但是已经为preview状态的并不会刷新
            _RefreshAllItemState(itemIdList);
            //刷新红点状态
            _RefreshRedPointState();
        }

        //每秒检测一下当前缓存的新解锁的棋子图鉴  避免同一时间内多个图鉴解锁 造成相同的处理逻辑走多遍
        private void _CheckCacheItemUnlockState()
        {
            if (_cacheNewUnlockItemIdList.Count > 0)
            {
                //刷新指定棋子状态
                _RefreshAllItemState(_cacheNewUnlockItemIdList);
                //预告状态只会在其他棋子刷新成最终状态后才会刷新
                _RefreshAllItemPreviewState(_cacheNewUnlockItemIdList);
                //刷新红点状态
                _RefreshRedPointState();
                //通知有需要的管理器
                _OnNewItemUnlock();
                //dispatch事件
                MessageCenter.Get<MSG.GAME_HANDBOOK_UNLOCK_ITEM>().Dispatch();
                _cacheNewUnlockItemIdList.Clear();
            }
        }

        //刷新红点状态
        private void _RefreshRedPointState()
        {
            bool hasRP = false;
            foreach (var map in _itemStateMap)
            {
                if (map.Value == HandbookItemState.Unlock)
                {
                    hasRP = true;
                    break;
                }
            }
            HandbookHasRP = hasRP;
        }

        private void _OnNewItemUnlock()
        {
            Game.Manager.bagMan.OnGalleryUnlock();
            Game.Manager.mainOrderMan.SetDirty();
            Game.Manager.miniBoardMan.OnNewItemUnlock();
            Game.Manager.miniBoardMultiMan.OnNewItemUnlock();
            Game.Manager.mineBoardMan.OnNewItemUnlock();
            if (Game.Manager.activity.LookupAny(EventType.FarmBoard, out var act1) && act1 is FarmBoardActivity farm)
            {
                farm.OnNewItemUnlock();
            }
            if (Game.Manager.activity.LookupAny(EventType.WishBoard, out var act2) && act2 is WishBoardActivity wish)
            {
                wish.OnNewItemUnlock();
            }
        }

        //尝试领取指定图鉴奖励
        private bool _TryClaimHandbookReward(int itemId)
        {
            if (!IsItemCanClaim(itemId))
                return false;
            //如果记录成功 则缓存该id
            if (_handbookReceivedMask.AddId(itemId))
            {
                //刷新状态
                _RefreshItemState(itemId);
                //打点
                handbook.Track(itemId);
                //发奖励
                var reward = Game.Manager.objectMan.GetMergeItemConfig(itemId)?.Reward.ConvertToRewardConfig();
                if (reward != null)
                {
                    using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
                    {
                        rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.handbook));
                        foreach (var kv in rewards) UIFlyUtility.FlyRewardSetType(kv, _rewardFromPos, FlyType.Handbook);
                    }
                }
                //刷新红点状态
                _RefreshRedPointState();
                MessageCenter.Get<MSG.GAME_HANDBOOK_REWARD>().Dispatch(itemId);
                return true;
            }
            return false;
        }

        #endregion
    }
}