/**
 * @Author: handong.liu
 * @Date: 2021-02-22 11:57:35
 */
using UnityEngine;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;
using System.Linq;

namespace FAT.Merge
{
    public enum ItemEventType
    {
        ItemEventSpeedUp,
        ItemEventOutputItem,
        ItemEventLeaveBoard,
        ItemEventEnterBoard,
        ItemEventNoCDStateChange,
        ItemBubbleBreak,
        ItemBubbleUnleash,
        ItemEventClaimBonus,
        ItemEventRewardListOut,
        ItemEventOrderBoxActivate,
        ItemEventJumpCDActivate,
        ItemEventInventoryConsumeForOrder,
        ItemEventRewardDisappear,   //奖励箱中的物品在点击后直接移出奖励箱并消失(不发往棋盘)
        ItemEventTrigAutoSource,
        ItemEventMoveToRewardBox,   //棋盘棋子移动到奖励箱
        ItemBubbleFrozenBreak,      //冰冻棋子过期时破碎
        ItemEventTokenMultiActivate,//活动token翻倍棋子生效
    }
    public class MergeWorldParam
    {
        public string dataTrackName = "";
    }
    public interface IMergeWorldPrivate
    {
        MergeWorld world { get; }
        //如果返回了，意味着物品会从unusedItem集合删除，不在维护，外部需要负责维护这个item保证它不会神隐
        bool GrabUnusedItem(int itemId, out Item item);
    }

    public class MergeWorld
    {
        public interface IActivityHandler { }

        private class MergeWorldInternal : IMergeWorldPrivate
        {
            MergeWorld IMergeWorldPrivate.world => mWorld;
            public MergeWorld mWorld;
            public Dictionary<int, Item> mUnusedItem = new Dictionary<int, Item>();            //未使用的棋子，当一个棋子没有被棋盘、rewardlist、inventory拥有的时候它就在里面
            bool IMergeWorldPrivate.GrabUnusedItem(int itemId, out Item item)
            {
                item = null;
                if (mUnusedItem.TryGetValue(itemId, out item))
                {
                    mUnusedItem.Remove(itemId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public int rewardListUnreadCount => mRewardListUnreadCount;
        public long lastActiveTime => mLastActiveTime;
        public long lastTickMilli => mLastActiveTime > 0 ? mLastActiveTime * 1000 : mLastTickMilli;
        public HandlerChain<Item, FuncType, int, int> handlerItemFuncUse = new HandlerChain<Item, FuncType, int, int>();
        public event System.Action<BonusClaimRewardData> onCollectBonus;  //when a bonus item is collected
        public event System.Action<BonusClaimRewardData> onCollectTapBonus;  //when a tap bonus item is collected
        public event System.Action<bool> onRewardListChange;         //param: is add
        public event System.Action onRewardListUnreadChange;
        public event System.Action<Item> onChestWaitFinish;
        public event System.Action<Item> onChestWaitStart;
        public event System.Action<Item> onSelledItemChange;
        public event System.Action<Item, ItemEventType> onItemEvent;
        public Inventory inventory => mInventory;
        public int configVersion => mConfigVersion;
        public Board activeBoard => mBoard;
        public OrderBox orderBox => mOrderBox;
        public JumpCD jumpCD => mJumpCD;
        public TokenMulti tokenMulti => mTokenMulti;
        public int currentWaitChest => mWaitChest;
        public int currentWaitChestTime => mWaitChestTime;
        public Item undoItem => mSoldItem;
        public int nextReward => nextRewardItem?.tid ?? 0;
        public Item nextRewardItem => mRewardList.Count > 0 ? mRewardList[mRewardList.Count - 1] : null;
        public int rewardCount => mRewardList.Count;
        public string dataTrackName => mParam.dataTrackName;
        public MergeWorldTracer currentTracer { get; private set; }
        public IOrderHelper currentOrderHelper { get; private set; }
        public IList<IActivityHandler> activityHandlers => mActivityHandlers;
        public bool isGiftboxUsable => !mBoard.GiftBoxUnable;
        public bool isEquivalentToMain => mBoard.EquivalentToMain;

        private MergeWorldInternal mPrivateInterface;
        private List<IMergeBonusHandler> mMergeBonusHandlers = new List<IMergeBonusHandler>();
        private MergeWorldParam mParam = new MergeWorldParam();
        private int mWaitChest;
        private int mWaitChestTime;

        private int mLastItemId = 0;
        private Board mBoard;
        private Inventory mInventory;
        private OrderBox mOrderBox;     // 订单随机礼盒
        private JumpCD mJumpCD;     // 跳过冷却
        private TokenMulti mTokenMulti;     // 活动token翻倍
        private List<Item> mRewardList = new List<Item>();            //注意，越往后越优先
        private HashSet<ItemComponentType> mDisabledComponent = new HashSet<ItemComponentType>();
        private List<Item> mItemsToDispose = new List<Item>();
        private long mLastTickMilli = 0;
        private long mLastActiveTime = 0;
        private Item mSoldItem;
        private int mSkipedSeconds = 0;
        private int mConfigVersion = 0;         //配置版本号
        private int mRewardListUnreadCount = 0;
        private Dictionary<int, InterlaceOutputMethod> mInterlaceOutputMethodById = new Dictionary<int, InterlaceOutputMethod>();
        private Dictionary<int, ItemOutputRandomList> mRandomOutputListById = new Dictionary<int, ItemOutputRandomList>();
        private List<IActivityHandler> mActivityHandlers = new();

        public void BindTracer(MergeWorldTracer tracer)
        {
            currentTracer = tracer;
        }

        public void BindOrderHelper(IOrderHelper helper)
        {
            currentOrderHelper = helper;
        }

        public void FillMergeBonusHandler(List<IMergeBonusHandler> container)
        {
            if (Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureMergeBonus))
                container.AddRange(mMergeBonusHandlers);
        }

        public void RegisterActivityHandler(IActivityHandler handler)
        {
            mActivityHandlers.AddIfAbsent(handler);
        }

        public void UnregisterActivityHandler(IActivityHandler handler)
        {
            mActivityHandlers.Remove(handler);
        }

        public InterlaceOutputMethod GetInterlaceRandomForId(int tid, ItemComponentType tp = ItemComponentType.EatSource)
        {
            InterlaceOutputMethod ret = null;
            var comConfig = Env.Instance.GetItemComConfig(tid);
            if (mInterlaceOutputMethodById.TryGetValue(tid, out ret))
            {
                return ret;
            }
            if (tp == ItemComponentType.EatSource)
            {
                var eatConfig = comConfig.eatConfig;
                if (eatConfig != null)
                {
                    ret = new InterlaceOutputMethod();
                    using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var indexes))
                    {
                        for (int i = 0; i < eatConfig.Eat.Count; i++)
                        {
                            indexes.Add(i);
                        }
                        ret.InitConfig(indexes, eatConfig.Weight);
                        DebugEx.FormatTrace("MergeWorld::GetInterlaceRandomForId ----> create as interlace {0}", ret);
                    }
                }
            }
            if (ret != null)
            {
                mInterlaceOutputMethodById[tid] = ret;
            }
            return ret;
        }

        public ItemOutputRandomList GetRandomListForId(int tid, ItemComponentType tp = ItemComponentType.ClickSouce)
        {
            int randomOutputId = tid;
            if (tp == ItemComponentType.Dying)
            {
                randomOutputId = tid - Constant.kMergeItemIdBase;
            }
            if (mRandomOutputListById.TryGetValue(randomOutputId, out var ret))
            {
                return ret;
            }
            else
            {
                var config = Env.Instance.GetRuledOutputConfig(tid);
                if (config != null)
                {
                    ret = new ItemOutputRandomList(string.Format("RandomOutputById{0}", tid));
                    using (ObjectPool<List<ItemOutputRandomList.OutputConstraitFixCount>>.GlobalPool.AllocStub(out var container))
                    {
                        for (int i = 0; i < config.OutputsFixed.Count; i++)
                        {
                            container.Add(new ItemOutputRandomList.OutputConstraitFixCount()
                            {
                                id = config.OutputsFixed[i],
                                totalCount = config.OutputsFixedTime[2 * i],
                                targetCount = config.OutputsFixedTime[2 * i + 1]
                            });
                        }
                        if (container.Count > 0)
                        {
                            ret.AddConstraitFixCount(container);
                        }
                    }
                }
                if (ret != null)
                {
                    mRandomOutputListById.Add(randomOutputId, ret);
                }
                return ret;
            }
        }

        public MergeWorld()
        {
            mPrivateInterface = new MergeWorldInternal()
            {
                mWorld = this
            };
            mInventory = new Inventory(mPrivateInterface);
            mBoard = new Board(this);
            mOrderBox = new OrderBox(this);
            mJumpCD = new JumpCD(this);
            mTokenMulti = new TokenMulti(this);
            mLastTickMilli = Game.Instance.GetTimestamp();
            mMergeBonusHandlers.Add(new Merge.ConfigMergeBonusHandler());
            mMergeBonusHandlers.Add(new Merge.BubbleMergeBonusHandler());
            foreach (var h in mMergeBonusHandlers)
            {
                h.OnRegister();
            }
        }

        public void UnRegisterConfigMergeBonusHandler()
        {
            var index = 0;
            foreach (var handler in mMergeBonusHandlers)
            {
                if (handler is ConfigMergeBonusHandler)
                {
                    handler.OnUnRegister();
                    index = mMergeBonusHandlers.IndexOf(handler);
                }
            }
            mMergeBonusHandlers.RemoveAt(index);
        }

        public void UnRegisterBubbleMergeBonusHandler()
        {
            var index = 0;
            foreach (var handler in mMergeBonusHandlers)
            {
                if (handler is BubbleMergeBonusHandler)
                {
                    handler.OnUnRegister();
                    index = mMergeBonusHandlers.IndexOf(handler);
                }
            }
            mMergeBonusHandlers.RemoveAt(index);
        }

        public void SetWorldParam(MergeWorldParam p)
        {
            mParam = p;
        }

        public void SetConfigVersion(int version)
        {
            mConfigVersion = version;
        }

        public int ConsumeNextItemId()
        {
            return ++mLastItemId;
        }

        public void FinishDisposeItem(Item item)
        {
            if (jumpCD.activeJumpCDId == item.id)
            {
                // 销毁的是当前激活的<跳过冷却>棋子 需要强行结束当前跳过冷却状态
                mJumpCD.ClearJumpCD();
            }
            else if (tokenMulti.activeTokenMultiId == item.id)
            {
                // 销毁的是当前激活的<token翻倍>棋子 需要强行结束当前翻倍状态
                mTokenMulti.ClearTokenMulti();
            }
            else if (currentWaitChest == item.id)
            {
                DebugEx.FormatInfo("Merge.MergeWorld.DisposeItem ----> chest {0} is disposed, we remove wait chest", item.id);
                SetWaitChest(null);
            }
            item.BeginDispose();
            mItemsToDispose.Add(item);
        }

        public void SetWaitChest(Item item)
        {
            mWaitChest = item == null ? 0 : item.id;
            mWaitChestTime = 0;
            if (item != null)
            {
                onChestWaitStart?.Invoke(item);
            }
        }

        public void SetSoldItem(Item item)
        {
            // ? dispose逻辑不应进入
            // if (mSoldItem != null)
            // {
            //     mSoldItem.BeginDispose();
            //     mSoldItem.EndDispose();
            // }
            mSoldItem = item;
            onSelledItemChange?.Invoke(item);
        }

        public Item GrabSelledItem()
        {
            var costSuc = false;
            var targetItem = mSoldItem;
            if (targetItem != null)
            {
                var (id, num) = ItemUtility.GetSellReward(targetItem.tid);
                if (num > 0)
                {
                    if (id == Constant.kMergeEnergyObjId && Env.Instance.CanUseEnergy(num))
                    {
                        costSuc = Env.Instance.UseEnergy(num, ReasonString.undo_sell_item);
                    }
                    else if (Env.Instance.CanUseCoin(num))
                    {
                        // 配置实际上仅支持coin 非任意reward
                        costSuc = true;
                        Env.Instance.UseCoin(num, ReasonString.undo_sell_item);
                    }
                }
                else
                {
                    // 卖出所得0 无需返回
                    costSuc = true;
                }
                if (costSuc)
                {
                    SetSoldItem(null);
                }
            }
            return costSuc ? targetItem : null;
        }

        public Item GetItem(int itemId)
        {
            return mBoard.FindItemById(itemId);
        }

        public Item AddReward(int tid, bool toFirstPlace = false)
        {
            var item = new Item(ConsumeNextItemId(), this);
            item.InitWithNormalItem(tid);
            item.SetState(false, false);
            DebugEx.FormatInfo("Merge::MergeWorld.AddReward ----> item {0} is spawned", item);
            AddReward(item, toFirstPlace);
            return item;
        }

        public void AddReward(Item rewardItem, bool toFirstPlace = false)
        {
            DebugEx.FormatInfo("Merge::MergeWorld.AddReward ----> item {0} is entering", rewardItem);
            if (toFirstPlace || mRewardList.Count == 0)
            {
                mRewardList.Add(rewardItem);
            }
            else
            {
                mRewardList.Insert(0, rewardItem);
            }
            _OnRewardListChange(true);

            // track 如果是add奖励的话 还要传一下真正要把奖励发到的目标棋盘id
            DataTracker.gift_box_change.Track(rewardItem.tid, true, mBoard?.boardId ?? 0);
        }

        public void SortRewardList()
        {
            mRewardList.Sort((r1, r2) =>
            {
                var cat1 = Env.Instance.GetCategoryByItem(r1.tid);
                var cat2 = Env.Instance.GetCategoryByItem(r2.tid);
                int score1 = r1.tid % Constant.kObjIdCapacity, score2 = r2.tid % Constant.kObjIdCapacity;
                if (cat1 != null)
                {
                    score1 += cat1.Id * Constant.kObjIdCapacity;
                }
                if (cat2 != null)
                {
                    score2 += cat2.Id * Constant.kObjIdCapacity;
                }
                return score1 - score2;
            });
            SetRewardListRead();
        }

        //return the tid of reward
        public int FindRewardIndex(int tid)
        {
            int idx = -1;
            for (int i = 0; i < mRewardList.Count; i++)
            {
                if (mRewardList[i].tid == tid)
                {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0)
            {
                return mRewardList.Count - 1 - idx;
            }
            else
            {
                return -1;
            }
        }

        public int FindRewardCount(int tid)
        {
            return mRewardList.Count(item => item.tid == tid);
        }

        public int PeekRewardByIdx(int idx)
        {
            idx = mRewardList.Count - 1 - idx;          //reverse the order so that 0 is the latest item
            return mRewardList.GetElementEx(idx, ArrayExt.OverflowBehaviour.Default)?.tid ?? 0;
        }

        public Item PeekNextReward()
        {
            var idx = mRewardList.Count - 1 - 0;          //reverse the order so that 0 is the latest item
            return mRewardList.GetElementEx(idx, ArrayExt.OverflowBehaviour.Default);
        }

        public Item ConsumeRewardByIdx(int idx)
        {
            idx = mRewardList.Count - 1 - idx;
            if (idx >= 0 && mRewardList.Count > idx)
            {
                var ret = mRewardList[idx];
                mRewardList.RemoveAt(idx);
                _OnRewardListChange(false);
                return ret;
            }
            else
            {
                return null;
            }
        }

        public int PeekNextFixedCategoryOutputIdx(int categoryId)
        {
            var fixedOutputs = Env.Instance.GetFixedCategoryOutputDB();
            return fixedOutputs == null ? 0 : fixedOutputs.GetDefault(categoryId, 0);
        }

        public int ConsumeNextFixedCategoryOutputIdx(int categoryId)
        {
            var fixedOutputs = Env.Instance.GetFixedCategoryOutputDB();
            if (fixedOutputs != null)
            {
                int ret = fixedOutputs.GetDefault(categoryId, 0);
                fixedOutputs[categoryId] = ret + 1;
                return ret;
            }
            else
            {
                return 0;
            }
        }

        public int PeekNextFixedItemOutpuIdx(int itemId)
        {
            var fixedOutputs = Env.Instance.GetFixedItemOutputDB();
            return fixedOutputs == null ? 0 : fixedOutputs.GetDefault(itemId, 0);
        }

        public int ConsumeNextFixedItemOutputIdx(int itemId)
        {
            var fixedOutputs = Env.Instance.GetFixedItemOutputDB();
            if (fixedOutputs != null)
            {
                int ret = fixedOutputs.GetDefault(itemId, 0);
                fixedOutputs[itemId] = ret + 1;
                return ret;
            }
            else
            {
                return 0;
            }
        }

        public void Update(int milli)
        {
            mLastTickMilli = Env.Instance.GetTimestamp();
            if (mSkipedSeconds > 0)
            {
                milli += mSkipedSeconds * 1000;
                mSkipedSeconds = 0;
            }
            if (mLastActiveTime > 0)
            {
                long nowMilli = Env.Instance.GetTimestamp();
                long offlineMilli = nowMilli - mLastActiveTime * 1000;
                if (offlineMilli < 0)
                {
                    offlineMilli = 0;
                }
                long totalMilli = milli + offlineMilli;
                if (totalMilli > (int.MaxValue >> 1))           //保证不溢出
                {
                    totalMilli = (int.MaxValue >> 1);
                }
                milli = (int)totalMilli;
                DebugEx.FormatInfo("Merge::MergeWorld::Update ----> offlineMilli {0}, totalMilli {1}, lastOffline {2}, now {3}, delta {4}", offlineMilli, totalMilli, mLastActiveTime, nowMilli, milli);
                mLastActiveTime = 0;
            }

            // if(mWaitChest > 0)          //process chest
            // {
            //     var item = GetItem(mWaitChest);
            //     var component = item.GetItemComponent<ItemChestComponent>();
            //     mWaitChestTime += EffectUtility.CalculateMilliBySpeedEffect(component, milli);
            //     if(component.config.WaitTime * 1000 <= mWaitChestTime)
            //     {
            //         component.SetOpen();
            //     }
            // }

            mBoard.Update(milli);
            mInventory.Update(milli);
            mOrderBox.Update(milli);
            mJumpCD.Update(milli);
            mTokenMulti.Update(milli);

            if (mItemsToDispose.Count > 0)
            {
                foreach (var item in mItemsToDispose)
                {
                    item.EndDispose();
                }
                mItemsToDispose.Clear();
            }
        }

        public int ForwardChestWaitTime(int milli)
        {
            mWaitChestTime += milli;
            return mWaitChestTime;
        }

        public void SetCurrentChestOpen()
        {
            var chestId = mWaitChest;
            var item = GetItem(chestId);
            SetWaitChest(null);
            onChestWaitFinish?.Invoke(item);
        }

        public class BonusClaimRewardData
        {
            public Item item;
            public Vector2Int overrideRewardPos;
            public BonusClaimRewardData(Item it, RewardCommitData data)
            {
                item = it;
                mData = data;
            }
            private RewardCommitData mData;
            public RewardCommitData GrabReward()
            {
                var data = mData;
                mData = null;
                return data;
            }
        }
        public bool UseBonusItem(Item item)
        {
            var com = item.GetItemComponent<ItemBonusCompoent>();
            Env.Instance.NotifyItemEvent(item, ItemEventType.ItemEventClaimBonus);
            if (com.funcType == FuncType.Reward || com.funcType == FuncType.Token)
            {
                var reward = Env.Instance.CollectBonus(com.bonusId, com.bonusCount);
                var data = new BonusClaimRewardData(com.item, reward);
                onCollectBonus?.Invoke(data);
                _DisposeItem(item, ItemDeadType.Bonus);
            }
            else
            {
                handlerItemFuncUse.Handler(item, com.funcType, com.bonusId, com.bonusCount);
                _DisposeItem(item, ItemDeadType.None);
            }
            DebugEx.FormatInfo("Merge::World::UseBonusItem ----> claim bonus {0}", item);
            return true;
        }

        public bool UseTapBonusItem(Item item)
        {
            var com = item.GetItemComponent<ItemTapBonusComponent>();
            if (com.funcType == FuncType.Collect)
            {
                var reward = Env.Instance.CollectBonus(com.bonusId, com.bonusCount);
                var data = new BonusClaimRewardData(com.item, reward);
                onCollectTapBonus?.Invoke(data);
                _DisposeItem(item, ItemDeadType.TapBonus);
            }
            else
            {
                handlerItemFuncUse.Handler(item, com.funcType, com.bonusId, com.bonusCount);
                _DisposeItem(item, ItemDeadType.None);
            }
            DebugEx.FormatInfo("Merge::World::UseTapBonusItem ----> claim bonus {0}", item);
            return true;
        }

        public bool UseOrderBoxItem(Item item)
        {
            if (mOrderBox.TryActivateOrderBox(item.tid))
            {
                MessageCenter.Get<MSG.GAME_ORDER_ORDERBOX_BEGIN>().Dispatch(item);
                Env.Instance.NotifyItemEvent(item, ItemEventType.ItemEventOrderBoxActivate);
                _DisposeItem(item, ItemDeadType.OrderBoxOpen);
                DebugEx.FormatInfo("Merge::World::UseOrderBoxItem ----> activate orderbox {0}", item);
                return true;
            }
            return false;
        }

        public bool OnJumpCDItemExpired(int itemId)
        {
            var item = activeBoard.FindItemById(itemId);
            if (item != null)
            {
                _DisposeItem(item, ItemDeadType.JumpCDExpired);
                DebugEx.FormatInfo("Merge::World::OnJumpCDItemExpired ----> dispose jumpcd {0}", item);
                return true;
            }
            return false;
        }

        public bool UseJumpCDItem(Item item)
        {
            if (mJumpCD.TryActivateJumpCD(item))
            {
                DataTracker.board_active.Track(item.tid);
                activeBoard.TriggerJumpCDBegin(item);
                Env.Instance.NotifyItemEvent(item, ItemEventType.ItemEventJumpCDActivate);
                DebugEx.FormatInfo("Merge::World::UseJumpCDItem ----> activate jumpcd {0}", item);
                return true;
            }
            return false;
        }
        
        public bool OnTokenMultiItemExpired(int itemId)
        {
            var item = activeBoard.FindItemById(itemId);
            if (item != null)
            {
                _DisposeItem(item, ItemDeadType.TokenMultiExpired);
                DebugEx.FormatInfo("Merge::World::OnTokenMultiItemExpired ----> dispose tokenMulti {0}", item);
                return true;
            }
            return false;
        }

        public bool UseTokenMultiItem(Item item)
        {
            if (mTokenMulti.TryActivateTokenMulti(item))
            {
                DataTracker.board_active.Track(item.tid);
                activeBoard.TriggerTokenMultiBegin(item);
                Env.Instance.NotifyItemEvent(item, ItemEventType.ItemEventTokenMultiActivate);
                DebugEx.FormatInfo("Merge::World::UseTokenMultiItem ----> activate tokenMulti {0}", item);
                return true;
            }
            return false;
        }

        public BonusClaimRewardData CollectActivityEnergy(ItemActivityComponent com, Item disposeTarget)
        {
            var reward = Env.Instance.CollectBonus(Constant.kMergeEnergyForEventObjId, com.activityEnergy);
            var data = new BonusClaimRewardData(com.item, reward);
            data.overrideRewardPos = disposeTarget.coord;
            onCollectBonus?.Invoke(data);
            DebugEx.FormatInfo("Merge::World::CollectActivityEnergy ----> claim activity energy {0}", com.item);
            return data;
        }

        [System.Flags]
        public enum WalkItemMask
        {
            Board = 1,
            Inventory = 2,
            RewardList = 4,
            NoRewardList = WalkItemMask.Board | WalkItemMask.Inventory,
            NoInventory = WalkItemMask.Board | WalkItemMask.RewardList,
            All = 0x7FFFFFFF
        }
        public void WalkAllItem(System.Action<Item> func, WalkItemMask walkMask = WalkItemMask.All)
        {
            if (walkMask.HasFlag(WalkItemMask.Board))
            {
                mBoard.WalkAllItem(func);
            }
            if (walkMask.HasFlag(WalkItemMask.Inventory))
            {
                mInventory.WalkAllItem(func);
            }
            if (walkMask.HasFlag(WalkItemMask.RewardList))
            {
                foreach (var r in mRewardList)
                {
                    func?.Invoke(r);
                }
            }
        }

        public bool IsComponentDisable(ItemComponentType type)
        {
            return mDisabledComponent.Contains(type);
        }

        public void DisableComponent(ItemComponentType type, bool disable)
        {
            DebugEx.FormatInfo("Merge.MergeWorld.DisableCmponent ----> {0}:{1}", type, disable);
            if (disable)
            {
                mDisabledComponent.Add(type);
            }
            else
            {
                mDisabledComponent.Remove(type);
            }
            WalkAllItem((item) =>
            {
                var com = item.GetItemComponent(type, true);
                if (com != null)
                {
                    com.enabled = !disable;
                }
            });
        }

        public void PostProcessItemComponent(ItemComponentType type, ItemComponentBase com)
        {
            if (mDisabledComponent.Contains(type))
            {
                com.enabled = false;
            }
        }

        private fat.gamekitdata.Merge mCurrentDataSession;
        public void Serialize(fat.gamekitdata.Merge data)
        {
            data.BoardId = mBoard.boardId;
            data.LastActiveTime = lastTickMilli / 1000;// mLastActiveTime > 0?mLastActiveTime:(mLastTickMilli / 1000);
            var currentTime = Env.Instance.GetTimestamp();
            data.RewardListUnreadCount = mRewardListUnreadCount;

            mInventory.Serialize(data);
            mOrderBox.Serialize(data);
            mJumpCD.Serialize(data);
            mTokenMulti.Serialize(data);

            data.WaitChest = mWaitChest;
            data.WaitChestStart = (lastTickMilli - (long)mWaitChestTime) / 1000;
            data.LastItemId = mLastItemId;
            // data.RewardList.AddRange(mRewardList);
            data.ConfigVersion = mConfigVersion;
            //serialize disable component
            if (mDisabledComponent.Count > 0)
            {
                ulong disableComs = 0;
                foreach (var t in mDisabledComponent)
                {
                    disableComs |= 1UL << (int)t;
                }
                data.DisableComs = disableComs;
            }
            //serialize ruled output
            foreach (var entry in mRandomOutputListById)
            {
                data.RandomOutputForId.Add(entry.Key, new RandomOutputParam()
                {
                    RandomNextIdx = entry.Value.randomOutputNextIdx,
                    RandomSeed = entry.Value.randomOutputSeed
                });
            }
            //serialize random output
            foreach (var entry in mInterlaceOutputMethodById)
            {
                var param = new RandomParam();
                entry.Value.Serialize(param);
                data.RandomParamForId.Add(entry.Key, param);
            }
            //serialize all item
            mCurrentDataSession = data;
            WalkAllItem(_SerializeItem);
            foreach (var item in mRewardList)
            {
                data.RewardListItemId.Add(item.id);
            }
            // mBoard.WalkAllItem(_SerializeItem);          //被上面的代码取代
            // for(int i = 0; i < mInventory.capacity; i++)
            // {
            //     var item = mInventory.PeekItem(i);
            //     if(item != null)
            //     {
            //         _SerializeItem(item);
            //     }
            // }
            foreach (var unused in mPrivateInterface.mUnusedItem.Values)
            {
                if (unused != null)
                {
                    _SerializeItem(unused);
                }
            }
        }

        public void TriggerItemEvent(Item item, ItemEventType ev)
        {
            onItemEvent?.Invoke(item, ev);
        }

        public void SetSkipSeconds(int seconds)
        {
            mSkipedSeconds += seconds;
        }

        // 优先消耗的item列表
        private List<Item> mPriorOrderConsumeItems = new();
        public void AddPriorityConsumeItem(Item item) { mPriorOrderConsumeItems.Add(item); }
        public void ClearPriorityConsumeItem() { mPriorOrderConsumeItems.Clear(); }

        public bool TryConsumeOrderItem(IEnumerable<ItemConsumeRequest> itemsToConsume, List<Item> itemsToConfirm, bool dryrun)
        {
            void fill_item_board(IList<Item> container, int tid)
            {
                activeBoard.WalkAllItem((item) =>
                {
                    if (item.tid == tid && Merge.ItemUtility.CanUseInOrder(item))
                        container.Add(item);
                });
            }

            int fill_item_inventory(IList<Item> container, int tid, int max)
            {
                inventory.WalkAllItem((item) =>
                {
                    if (container.Count < max && item.tid == tid && Merge.ItemUtility.CanUseInOrderAllowInventory(item))
                        container.Add(item);
                });
                return container.Count;
            }

            var pool = PoolMapping.PoolMappingAccess;
            using var _ = pool.Borrow<List<Item>>(out var itemToRemove);
            using var __ = pool.Borrow<List<Item>>(out var itemBoardList); // 棋盘上的item
            using var ___ = pool.Borrow<List<Item>>(out var itemInvList); // 背包内的item

            var iter = itemsToConsume.GetEnumerator();
            while (iter.MoveNext())
            {
                itemBoardList.Clear();
                itemInvList.Clear();
                var req = iter.Current;
                var tarTid = req.itemId;

                fill_item_board(itemBoardList, tarTid);
                if (itemBoardList.Count < req.itemCount)
                {
                    if (Env.Instance.CanInventoryItemUseForOrder)
                    {
                        // 尝试从背包里继续扣除
                        var lack = req.itemCount - itemBoardList.Count;
                        if (fill_item_inventory(itemInvList, tarTid, lack) < lack)
                        {
                            DebugEx.Warning($"Merge::MergeWorld::ConsumeItem ----> item {tarTid} not enough, has {itemBoardList.Count}+{itemInvList.Count} vs needed {req.itemCount}");
                            return false;
                        }
                    }
                    else
                    {
                        DebugEx.FormatWarning("Merge::MergeWorld::ConsumeItem ----> item {0} not enough, exists {1} vs needed {2}",
                            req.itemId, itemBoardList.Count, req.itemCount);
                        return false;
                    }
                }
                itemBoardList.Sort(_SortForOrderConsume);
                for (var idx = 0; idx < req.itemCount && idx < itemBoardList.Count; idx++)
                {
                    itemToRemove.Add(itemBoardList[idx]);
                }
                itemToRemove.AddRange(itemInvList);
                itemsToConfirm?.AddRange(itemInvList);
            }

            if (dryrun)
            {
                return true;
            }
            ClearPriorityConsumeItem();

            var invItemCount = 0;
            foreach (var item in itemToRemove)
            {
                if (item.parent != null)
                {
                    activeBoard.DisposeItem(item, ItemDeadType.Order);
                }
                else
                {
                    // 没有parent认为item来自inventory
                    ++invItemCount;
                    inventory.DisposeItem(item, ItemDeadType.Order);
                    TriggerItemEvent(item, ItemEventType.ItemEventInventoryConsumeForOrder);
                }
                // track
                DataTracker.board_order.Track(item);
            }
            if (invItemCount > 0) Game.Manager.bagMan.OnItemLeaveBag();

            return true;
        }

        public int RemoveItem(int targetTid) => ConvertItem(targetTid, 0);
        public int ConvertItem(int targetTid, int toTid)
        {
            var count = 0;
            activeBoard.WalkAllItem((item) =>
            {
                if (item.tid == targetTid)
                {
                    var pos = item.coord;
                    activeBoard.DisposeItem(item, ItemDeadType.Event);
                    if (toTid > 0) activeBoard.SpawnItem(toTid, pos.x, pos.y, false, false);
                    ++count;
                }
            });
            var rCount = mRewardList.RemoveAll(r => r.tid == targetTid);        //TODO:如果需要完整的Dispose流程，这里补一下
            count += rCount;
            if (toTid > 0)
            {
                for (var k = 0; k < rCount; ++k) AddReward(toTid);
            }
            else if (rCount > 0)
            {
                _OnRewardListChange(false);
            }

            var bagCount = 0;
            while (mInventory.GetItemIndexByTid(targetTid, out var itemIdx, out int invId))
            {
                mInventory.DisposeItem(mInventory.PeekItem(itemIdx, invId), ItemDeadType.Event);
                if (toTid > 0) AddReward(toTid);
                ++count;
                ++bagCount;
            }
            if (bagCount > 0) Game.Manager.bagMan.OnItemLeaveBag();
            //物品过期转化打点
            if (count > 0) DataTracker.expire.Track(targetTid, count);
            return count;
        }

        private void _DisposeItem(Item item, ItemDeadType type)
        {
            if (item.parent != null)
            {
                item.parent.DisposeItem(item, type);
            }
            else if (mRewardList.Contains(item))
            {
                mRewardList.Remove(item);
            }
            else
            {
                mInventory.DisposeItem(item, type);
            }
        }

        private int _SortForOrderConsume(Merge.Item a, Merge.Item b)
        {
            // 分值大的优先
            return -(_SortForOrderConsumeScore(a) - _SortForOrderConsumeScore(b));
        }

        private int _SortForOrderConsumeScore(Merge.Item a)
        {
            var itemCount = Merge.ItemUtility.GetItemUsableCount(a);
            var pos = a.parent != null ? 900 - mBoard.CalculateIdxByCoord(a.coord.x, a.coord.y) : 999;
            var priority = 0;
            var idx = mPriorOrderConsumeItems.IndexOf(a);
            if (idx >= 0)
            {
                priority = 1000000 * (idx + 1);
            }
            return priority + itemCount * 1000 + pos;
        }

        private void _SerializeItem(Item item)
        {
            var itemData = new MergeItem();
            item.Serialize(itemData);
            mCurrentDataSession.Items.Add(itemData);
        }

        public void Deserialize(fat.gamekitdata.Merge data, System.Action configCB)
        {
            var objectMan = Game.Manager.objectMan;
            mPrivateInterface.mUnusedItem.Clear();
            //deserialize disable component
            if (data.DisableComs > 0)
            {
                int t = 0;
                ulong disableComs = data.DisableComs;
                while (disableComs > 0)
                {
                    if ((disableComs & 1) == 1)
                    {
                        mDisabledComponent.Add((ItemComponentType)t);
                    }
                    t++;
                    disableComs >>= 1;
                }
            }
            mRewardListUnreadCount = data.RewardListUnreadCount;
            mLastActiveTime = data.LastActiveTime;
            mConfigVersion = data.ConfigVersion;
            configCB?.Invoke();
            var currentTime = Env.Instance.GetTimestamp();
            mWaitChest = data.WaitChest;
            mWaitChestTime = Mathf.Max(1, (int)(data.LastActiveTime * 1000 - data.WaitChestStart * 1000));

            mRandomOutputListById.Clear();
            foreach (var entry in data.RandomOutputForId)
            {
                var random = GetRandomListForId(entry.Key);
                if (random == null)
                {
                    DebugEx.FormatWarning("MergeWorld::Deserialize ----> random list not exists {0}", entry.Key);
                }
                else
                {
                    random.SetParam(entry.Value.RandomSeed, entry.Value.RandomNextIdx);
                }
            }

            mInterlaceOutputMethodById.Clear();
            foreach (var entry in data.RandomParamForId)
            {
                var random = GetInterlaceRandomForId(entry.Key);
                if (random == null)
                {
                    DebugEx.FormatWarning("MergeWorld::Deserialize ----> random list not exists {0}", entry.Key);
                }
                else
                {
                    random.Deserialize(entry.Value);
                }
            }

            var replaceMap = Game.Manager.configMan.GetItemReplaceMap();
            foreach (var itemData in data.Items)
            {
                var conf = objectMan.GetMergeItemConfig(itemData.Tid);
                // 允许卡包出现在礼物盒队列
                if (conf == null && !ItemUtility.IsCardPack(itemData.Tid))
                {
                    string error = string.Format("MergeWorld::Deserialize ----> no item {0}@{1} {2}", itemData.Id, itemData.Tid, data.BoardId);
                    DebugEx.Error(error);
#if UNITY_EDITOR
                    continue;
#else
                    throw new System.Exception(error);
#endif
                }
                // var item = new Item(itemData.Id, this);
                // item.Deserialize(itemData);
                _ReplaceItemFilter(itemData, replaceMap, out var item);
                mPrivateInterface.mUnusedItem[itemData.Id] = item;
            }

            foreach (var item in mPrivateInterface.mUnusedItem)
            {
                item.Value.OnStart();
            }

            mInventory.Deserialize(data, mPrivateInterface.mUnusedItem);
            mOrderBox.Deserialize(data);

            foreach (var itemId in data.RewardListItemId)
            {
                if (!mPrivateInterface.mUnusedItem.TryGetValue(itemId, out var item))
                {
                    string error = string.Format("MergeWorld::Deserialize ----> reward item lost {0} {1}", itemId, data.BoardId);
                    DebugEx.Error(error);
                    throw new System.Exception(error);
                }
                mRewardList.Add(item);
                mPrivateInterface.mUnusedItem.Remove(itemId);
            }
            // mRewardList.AddRange(data.RewardList);
            mLastItemId = data.LastItemId;
            mBoard.Deserialize(mPrivateInterface.mUnusedItem);
            foreach (var itemTid in data.RewardList)
            {
                //migrate old reward id
                var item = AddReward(itemTid, true);
                DebugEx.FormatInfo("MergeWorld::Deserialize ----> migrate old reward {0} -> {1}", itemTid, item);
            }

            // jumpcd依赖棋盘上的item
            mJumpCD.Deserialize(data);
            // TokenMulti依赖棋盘上的item
            mTokenMulti.Deserialize(data);
        }

        public void SetRewardListRead()
        {
            _SetRewardListUnread(0);
        }

        private void _ReplaceItemFilter(MergeItem itemData, IDictionary<int, ItemReplace> replaceDict, out Item item)
        {
            if (replaceDict.TryGetValue(itemData.Tid, out var info))
            {
                DataTracker.TrackItemReplace(itemData.Tid, info.ReplaceInto);

                // 需要替换棋子
                item = new Item(itemData.Id, this);
                item.DeserializeStateOnly(itemData);
                var bubbleData = itemData.ComBubble;
                if (bubbleData != null)
                {
                    var type = bubbleData.Type;
                    //替换棋子时 如果Type值<=0 则默认为Bubble类型
                    var bubbleType = type > 0 ? (ItemBubbleType)type : ItemBubbleType.Bubble;
                    //继承原有棋子的存档 避免其已存在的时间清0
                    item.InitWithBubbleItem(info.ReplaceInto, bubbleType, bubbleData.Start, bubbleData.Life);
                }
                else
                {
                    item.InitWithNormalItem(info.ReplaceInto);
                }
            }
            else
            {
                // 无需替换
                item = new Item(itemData.Id, this);
                item.Deserialize(itemData);
            }
        }

        private void _SetRewardListUnread(int count)
        {
            if (count != mRewardListUnreadCount)
            {
                DebugEx.FormatInfo("MergeWorld::_SetRewardListUnread ----> {0} to {1}", mBoard?.boardId, count);
                mRewardListUnreadCount = count;
                onRewardListUnreadChange?.Invoke();
            }
        }

        private void _OnRewardListChange(bool isAdd)
        {
            if (isAdd)
            {
                _SetRewardListUnread(mRewardListUnreadCount + 1);
            }
            else
            {
                _SetRewardListUnread(Mathf.Max(0, mRewardListUnreadCount - 1));
            }
            onRewardListChange?.Invoke(isAdd);
        }
    }
}
