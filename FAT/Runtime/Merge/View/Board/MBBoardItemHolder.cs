/*
 * @Author: qun.chao
 * @Date: 2021-02-19 11:00:34
 */
namespace FAT
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using EL;
    using FAT.Merge;

    public class MBBoardItemHolder : MonoBehaviour, IMergeBoard
    {
        [SerializeField] private GameObject itemViewPrefab;
        [SerializeField] private GameObject itemHolderPrefab;

        private int width;
        private int height;

        private List<RectTransform> mCellList = new List<RectTransform>();
        private RectTransform mRoot;
        private Vector2 mItemInDragBeginPos;
        private int mLastUseFrameCount;
        private int mUseItemMinIntervalFrame = 3;
        private float mLastUseTime;
        private float mUseItemMinIntervalTime = 0.05f;

        private Dictionary<int, MBItemView> mItemViewDict = new Dictionary<int, MBItemView>();
        private List<Item> mInventorySpawnQueue = new List<Item>();
        private bool mIsClearing = false;
        private int mRecentSelectedItemId { get; set; }
        private string poolKey { get; set; }

        void IMergeBoard.Init()
        {
            mRoot = transform.GetChild(0) as RectTransform;
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MERGE_ITEM_HOLDER, itemHolderPrefab);
        }

        void IMergeBoard.Setup(int w, int h)
        {
            width = w;
            height = h;

            var board = BoardViewManager.Instance.board;
            var world = BoardViewManager.Instance.world;
            poolKey = $"{PoolItemType.MERGE_ITEM_VIEW}_{world.activeBoard.boardId}";
            GameObjectPoolManager.Instance.PreparePool(poolKey, itemViewPrefab);

            board.onItemMove += _OnItemMove;
            board.onItemSpawn += _OnItemSpawn;
            board.onItemSpawnFly += _OnItemSpawnFly;
            board.onItemMerge += _OnItemMerge;
            board.onItemDead += _OnItemDead;
            board.onItemEat += _OnItemEat;
            board.onItemConsume += _OnItemConsume;
            board.onItemToInventory += _OnItemPutIntoInventory;
            board.onItemFromInventory += _OnItemTakeOutFromInventory;
            board.onItemStateChange += _OnItemStateChange;
            board.onItemSell += _OnItemSell;
            board.onItemComponentChange += _OnItemComponentChange;
            board.onUseTimeScaleSource += _OnUseTimeScaleSource;
            world.onChestWaitStart += _OnChestWaitStart;
            world.onChestWaitFinish += _OnChestWaitFinish;
            world.onItemEvent += _OnItemEvent;

            mLastUseFrameCount = Time.frameCount;
            mInventorySpawnQueue.Clear();
            mRecentSelectedItemId = -1;

            _PrepareGrid();
            _FillItem();
        }

        void IMergeBoard.Cleanup()
        {
            var board = BoardViewManager.Instance.board;
            var world = BoardViewManager.Instance.world;

            board.onItemMove -= _OnItemMove;
            board.onItemSpawn -= _OnItemSpawn;
            board.onItemSpawnFly -= _OnItemSpawnFly;
            board.onItemMerge -= _OnItemMerge;
            board.onItemDead -= _OnItemDead;
            board.onItemEat -= _OnItemEat;
            board.onItemConsume -= _OnItemConsume;
            board.onItemToInventory -= _OnItemPutIntoInventory;
            board.onItemFromInventory -= _OnItemTakeOutFromInventory;
            board.onItemStateChange -= _OnItemStateChange;
            board.onItemSell -= _OnItemSell;
            board.onItemComponentChange -= _OnItemComponentChange;
            board.onUseTimeScaleSource -= _OnUseTimeScaleSource;
            world.onChestWaitStart -= _OnChestWaitStart;
            world.onChestWaitFinish -= _OnChestWaitFinish;
            world.onItemEvent -= _OnItemEvent;

            mInventorySpawnQueue.Clear();
            _ClearItem();
            _ReleaseGrid();
            poolKey = null;
        }

        public bool CanGrabItem(Item item)
        {
            if (!item.isActive || item.isDead)
                return false;
            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                if (v.IsViewDraggable())
                {
                    return true;
                }
            }
            return false;
        }

        public bool GrabItem(Item item, Vector2 screenPos)
        {
            if (!item.isActive || item.isDead)
                return false;
            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                if (v.IsViewDraggable())
                {
                    var localPos = BoardViewManager.Instance.ReAnchorItemForDrag(v.transform as RectTransform, screenPos);
                    mItemInDragBeginPos = localPos;
                    v.SetDrag();
                    return true;
                }
            }
            return false;
        }

        public void DragItem(Item item, Vector2 offset)
        {
            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                var trans = v.transform as RectTransform;
                trans.anchoredPosition = mItemInDragBeginPos + offset;
            }
        }

        public bool ClickItem(Item item)
        {
            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                //点击棋子的冷却时间 避免连点 默认0.05f
                var clickCd = mUseItemMinIntervalTime;
                //如果棋子带有MBResHolderTrig则使用其配置的点击冷却时间
                var resHolderTrig = v.GetResHolder() as MBResHolderTrig;
                if (resHolderTrig != null)
                {
                    clickCd = resHolderTrig.ClickDelayTime;
                }
                if (Time.time > mLastUseTime + clickCd && _UseItem(item))
                {
                    mLastUseTime = Time.time;
                    return true;
                }
            }
            return false;
        }

        public void ApplyFilter()
        {
            foreach (var view in mItemViewDict.Values)
            {
                view.TryApplyFilter();
            }
        }

        public void RemoveFilter()
        {
            foreach (var view in mItemViewDict.Values)
            {
                view.RemoveFilter();
            }
        }

        public void SetSelectedItem(int itemId)
        {
            mRecentSelectedItemId = itemId;
        }

        private bool _UseItem(Item item)
        {
            var ret = BoardUtility.UseItemOnBoard(item, UserMergeOperation.DoubleClickItem);
            if (!ret)
            {
                if (ItemUtility.CanUseInOrder(item))
                {
                    ret = BoardViewWrapper.TryFinishOrderByItem(item);
                }
                if (!ret)
                {
                    MessageCenter.Get<MSG.UI_BOARD_USE_ITEM>().Dispatch(item);
                }
            }
            return ret;
        }

        public void MoveBack(Item item)
        {
            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                v.SetMove();
            }
        }

        public void HoldItem(int x, int y, RectTransform view)
        {
            view.SetParent(_Grid(x, y));
            view.anchoredPosition = Vector2.zero;
            view.localScale = Vector3.one;
        }

        public void ReleaseItem(int id)
        {
            if (mIsClearing)
                return;
            if (mItemViewDict.TryGetValue(id, out MBItemView view))
            {
                view.ClearData();
                GameObjectPoolManager.Instance.ReleaseObject(poolKey, view.gameObject);
                mItemViewDict.Remove(id);
            }
        }

        public MBItemView FindItemView(int id)
        {
            if (mItemViewDict.TryGetValue(id, out MBItemView v))
            {
                return v;
            }
            else
            {
                return null;
            }
        }

        public MBItemView TakeoverItem(int id)
        {
            if (mIsClearing)
                return null;
            if (mItemViewDict.TryGetValue(id, out MBItemView view))
            {
                mItemViewDict.Remove(id);
                return view;
            }
            return null;
        }

        public void TapItem(int id, bool isDelayHigh = false)
        {
            if (mItemViewDict.TryGetValue(id, out MBItemView view))
            {
                if (!isDelayHigh)
                    view.PlayTap();
                else
                    view.PlayTapDelayHigh();
            }
        }

        public void TimeSkipItem(int id)
        {
            // TODO: 做独特动画
            TapItem(id);
        }

        public bool IsItemIdle(int id)
        {
            if (mItemViewDict.TryGetValue(id, out MBItemView view))
            {
                return view.IsViewDraggable();
            }
            return false;
        }

        public void SetSelectItem(int id)
        {
            if (mItemViewDict.TryGetValue(id, out MBItemView view))
            {
                view.SetSelect();
            }
        }

        public void SetDeselectItem(int id)
        {
            if (mItemViewDict.TryGetValue(id, out MBItemView view))
            {
                view.SetDeselect();
            }
        }

        public bool TryResolveInventorySpawnQueue()
        {
            var suc = mInventorySpawnQueue.Count > 0;
            foreach (var item in mInventorySpawnQueue)
            {
                if (item.isDead)
                    continue;
                if (!mItemViewDict.TryGetValue(item.id, out var v))
                {
                    v = _CreateView(item);
                }
                v.SetSpawnFromInventory();
            }
            mInventorySpawnQueue.Clear();
            return suc;
        }

        private void _ClearItem()
        {
            mIsClearing = true;

            foreach (var view in mItemViewDict.Values)
            {
                view.ClearData();
                GameObjectPoolManager.Instance.ReleaseObject(poolKey, view.gameObject);
            }
            mItemViewDict.Clear();

            mIsClearing = false;
        }

        private void _FillItem()
        {
            var board = BoardViewManager.Instance.board;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        var view = _CreateView(item);
                        var trans = view.transform as RectTransform;
                        trans.SetParent(_Grid(i, j));
                        trans.localPosition = Vector3.zero;
                        trans.localScale = Vector3.one;
                    }
                }
            }
        }

        public void ReFillItem()
        {
            var board = BoardViewManager.Instance.board;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        if (mItemViewDict.TryGetValue(item.id, out MBItemView view))
                        {
                            var trans = view.transform as RectTransform;
                            trans.SetParent(_Grid(i, j));
                            trans.localPosition = Vector3.zero;
                            trans.localScale = Vector3.one;
                        }
                        else
                        {
                            var _view = _CreateView(item);
                            var trans = _view.transform as RectTransform;
                            trans.SetParent(_Grid(i, j));
                            trans.localPosition = Vector3.zero;
                            trans.localScale = Vector3.one;
                        }
                    }
                }
            }
        }

        private MBItemView _CreateView(Item item)
        {
            var obj = GameObjectPoolManager.Instance.CreateObject(poolKey);
            var view = obj.GetComponent<MBItemView>();
            view.gameObject.SetActive(true);
            view.SetData(item);
            view.SetBorn();
            mItemViewDict.Add(item.id, view);
            return view;
        }

        // topleft -> bottomright
        private void _PrepareGrid()
        {
            float cellSize = BoardUtility.cellSize;
            float halfSize = cellSize * 0.5f;

            GameObject go;
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    go = GameObjectPoolManager.Instance.CreateObject(PoolItemType.MERGE_ITEM_HOLDER, mRoot);
                    var trans = go.transform as RectTransform;
                    // var trans = _CreateCell(i, j);
                    trans.anchoredPosition = new Vector2(i * cellSize + halfSize, -j * cellSize - halfSize);
                    mCellList.Add(trans);
                }
            }
        }

        private void _ReleaseGrid()
        {
            for (int i = 0; i < mCellList.Count; i++)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.MERGE_ITEM_HOLDER, mCellList[i].gameObject);
            }
            mCellList.Clear();
        }

        // private RectTransform _CreateCell(int x, int y)
        // {
        //     var go = new GameObject($"{x}_{y}", typeof(RectTransform));
        //     go.transform.SetParent(mRoot);
        //     go.transform.localScale = Vector3.one;
        //     return go.transform as RectTransform;
        // }

        private RectTransform _Grid(int x, int y)
        {
            if (width * y + x >= mCellList.Count)
                return mCellList[0];
            return mCellList[width * y + x];
        }

        #region handler

        private void _OnItemEvent(Item item, ItemEventType eventType)
        {
            DebugEx.FormatInfo("TestMerge._OnItemEvent ----> {0}, {1}", item.id, eventType.ToString());
            if (eventType == ItemEventType.ItemEventRewardListOut)
            {
                // item可能取出后直接消耗
                if (item.parent != null)
                {
                    var view = _CreateView(item);
                    view.SetRewardListPop();
                }
            }
            else if (eventType == ItemEventType.ItemEventInventoryConsumeForOrder)
            {
                // 背包物品被订单消耗
                var sp = BoardViewManager.Instance.inventoryEntryScreenPos;
                RectTransformUtility.ScreenPointToWorldPointInRectangle(transform as RectTransform, sp, null, out var wp);
                MessageCenter.Get<MSG.UI_ON_ORDER_ITEM_CONSUMED>().Dispatch(item.tid, wp);
            }
            else if (eventType == ItemEventType.ItemEventTrigAutoSource)
            {
                var itemView = FindItemView(item.id);
                if (itemView != null)
                {
                    //播特效
                    var effRoot = BoardViewManager.Instance.boardView.topEffectRoot;
                    var effType = BoardUtility.EffTypeToPoolType(ItemEffectType.TrigAutoSource).ToString();
                    var effectGo = GameObjectPoolManager.Instance.CreateObject(effType, effRoot);
                    effectGo.transform.position = BoardUtility.GetWorldPosByCoord(item.coord);
                    BoardUtility.AddAutoReleaseComponent(effectGo, 2f, effType);
                    effectGo.SetActive(true);
                    //刷新图片
                    var trigHolder = itemView.GetResHolder() as MBResHolderTrig;
                    if (trigHolder != null)
                    {
                        trigHolder.OnTrigAutoSourceSucc();
                    }
                }
            }
            else if (eventType == ItemEventType.ItemEventMoveToRewardBox)
            {
                var itemView = FindItemView(item.id);
                if (itemView != null)
                {
                    itemView.SetMoveToRewardBox();
                }
            }
        }

        private void _OnItemMove(Item item)
        {
            DebugEx.Info($"TestMerge._OnItemMove ----> {item.id}@{item.tid}");

            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                v.SetMove();
                // 可能要播放音效
                var _cfg = Game.Manager.mergeBoardMan.GetMergeGridConfig(item.grid.gridTid);
                if (!string.IsNullOrEmpty(_cfg?.SndMoveIn))
                {
                    Game.Manager.audioMan.PlaySound(Game.Manager.audioMan.default_group, _cfg.SndMoveIn);
                }
            }
        }

        private void _OnItemMerge(Item src, Item dst, Item result)
        {
            DebugEx.FormatInfo("TestMerge._OnItemMerge ----> ({0}, {1}), ({2}, {3})", src.coord.x, src.coord.y, dst.coord.x, dst.coord.y);
            DataTracker.TrackMergeActionMerge(result, ItemUtility.GetItemLevel(result.tid), dst.isFrozen);

            GuideUtility.OnItemMerge(src, dst, result);

            var boardEffect = BoardViewManager.Instance.boardView.boardEffect;
            // 正常合成时播放的特效
            boardEffect.ShowMergeEffect(result.coord);
            // 假如当前有冰冻棋子参与合成 则播放冰块破碎特效 顺带打点
            var srcIsFrozenItem = ItemUtility.IsFrozenItem(src);
            var dstIsFrozenItem = ItemUtility.IsFrozenItem(dst);
            if (srcIsFrozenItem || dstIsFrozenItem)
            {
                boardEffect.ShowFrozenMergeEffect(result.coord);
                //打点
                var frozenItem = srcIsFrozenItem ? src : dst;
                DataTracker.event_frozen_item_merge.Track(frozenItem.id, frozenItem.tid, ItemUtility.GetItemLevel(frozenItem.tid));
            }

            if (mItemViewDict.TryGetValue(src.id, out MBItemView srcView) && mItemViewDict.TryGetValue(dst.id, out MBItemView dstView))
            {
                srcView.SetEmpty();
                dstView.SetEmpty();

                var context = new ItemInteractContext
                {
                    src = srcView,
                    dst = dstView,
                };

                var view = _CreateView(result);
                view.SetMerge(context);
                Game.Manager.audioMan.PlayMergeSound(ItemUtility.GetItemLevel(result.tid));
            }
        }

        private void _OnItemSpawn(ItemSpawnContext context, Item item)
        {
            DebugEx.FormatInfo("TestMerge._OnItemSpawn ----> {0}", item.id);

            if (!mItemViewDict.TryGetValue(item.id, out MBItemView view))
            {
                view = _CreateView(item);
            }
            view.SetSpawn(context);

            if (context.spawner != null)
            {
                // track
                var sp = context.spawner;
                if (context.type == ItemSpawnContext.SpawnType.TapSource)
                {
                    if (sp.TryGetItemComponent(out ItemClickSourceComponent click))
                    {
                        if (click.itemCount == 0 && click.isReviving)
                        {
                            DataTracker.TrackMergeActionSourceCD(sp, ItemUtility.GetItemLevel(sp.tid));
                        }
                    }
                }
                else if (context.type == ItemSpawnContext.SpawnType.MixSource)
                {
                    if (sp.TryGetItemComponent(out ItemMixSourceComponent com))
                    {
                        var mixer = FindItemView(sp.id);
                        if (mixer != null)
                            mixer.SetMixOutput();
                        if (com.itemCount == 0 && com.isReviving)
                        {
                            DataTracker.TrackMergeActionSourceCD(sp, ItemUtility.GetItemLevel(sp.tid));
                        }
                    }
                }
                else if (context.type == ItemSpawnContext.SpawnType.AutoSource)
                {
                    DataTracker.TrackMergeActionSourceCD(sp, ItemUtility.GetItemLevel(sp.tid));
                }
                sp.TryGetItemComponent<ItemClickSourceComponent>(out var c);
                if (mRecentSelectedItemId > 0)
                {
                    if (mRecentSelectedItemId == context.spawner.id)
                    {
                        // 目的是仅在首次spawn时记录一些信息
                        DataTracker.TrackMergeActionSpawnWish(item, ItemUtility.GetItemLevel(item.tid), context.spawner.tid, c?.isBoostItem ?? false);
                        Game.Manager.remoteApiMan.OnWishProducerChange(item.tid);
                    }
                    mRecentSelectedItemId = -1;
                }
                var isBoost = false;
                if (c != null)
                {
                    if (!c.isBoostItem)
                    {
                        isBoost = c.WasBoostItem();
                    }
                    else
                    {
                        isBoost = c.isBoostItem;
                    }
                }
                DataTracker.TrackMergeActionSpawn(item, ItemUtility.GetItemLevel(item.tid), context.spawner.tid, isBoost);
            }
            else if (context.type == ItemSpawnContext.SpawnType.Upgrade)
            {
                var score = item.config.MergeScore;
                if (score > 0)
                {
                    MessageCenter.Get<MSG.ON_USE_JOKER_ITEM_UPGRADE>().Dispatch(item, score);
                }
            }
            else if (context.type == ItemSpawnContext.SpawnType.DieInto)
            {
                DataTracker.TrackMergeActionDieInto(item, ItemUtility.GetItemLevel(item.tid));
            }
            else if (context.type == ItemSpawnContext.SpawnType.Undo)
            {
                DataTracker.TrackMergeActionUndo(item, ItemUtility.GetItemLevel(item.tid));
            }

            if (item.HasComponent(ItemComponentType.Bubble))
            {
                var bubbleComp = item.GetItemComponent<ItemBubbleComponent>();
                if (bubbleComp.IsBubbleItem())
                {
                    Game.Manager.audioMan.TriggerSound("BubbleBorn");
                }
                else if (bubbleComp.IsFrozenItem())
                {
                    //播冰冻棋子出生音效
                    Game.Manager.audioMan.TriggerSound("FrozenItemBorn");
                }
            }
            else if (context.spawner != null)
            {
                var snd = ItemUtility.GetSourceSpawnSound(context.spawner.tid);
                if (!string.IsNullOrEmpty(snd.eventName))           //null is ok, null means use config value
                {
                    if (string.IsNullOrEmpty(snd.audioName))
                    {
                        // play key
                        Game.Manager.audioMan.TriggerSound(snd.eventName);
                    }
                    else
                    {
                        // res
                        Game.Manager.audioMan.PlaySound(Game.Manager.audioMan.default_group, snd.audioName);
                    }
                }
            }
        }

        private void _OnItemSpawnFly(Item item, List<RewardCommitData> rewardList)
        {
            var pos = BoardUtility.GetWorldPosByCoord(item.coord);
            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                //如果棋子带有MBResHolderTrig则使用其自定义的飞图标时机
                var resHolderTrig = v.GetResHolder() as MBResHolderTrig;
                if (resHolderTrig != null)
                {
                    resHolderTrig.DelayFlyRewardList(rewardList, pos);
                    return;
                }
            }
            UIFlyUtility.FlyRewardList(rewardList, pos);
        }

        private void _OnItemDead(Item item, ItemDeadType type)          //TODO
        {
            DebugEx.FormatInfo("TestMerge._OnItemDead ----> {0}", item.id);

            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                if (type == ItemDeadType.Order)
                {
                    MessageCenter.Get<MSG.UI_ON_ORDER_ITEM_CONSUMED>().Dispatch(item.tid, v.transform.position);
                    ReleaseItem(item.id);
                    return;
                }
                else if (type == ItemDeadType.ClickOut)
                {
                    if (item.TryGetItemComponent(out ItemClickSourceComponent click) && click.config.IsSkipDieAnime)
                    {
                        // 点击消亡 且跳过死亡动画
                        ReleaseItem(item.id);
                        return;
                    }
                }
                else if (type == ItemDeadType.Eat || type == ItemDeadType.Bonus || type == ItemDeadType.TapBonus)
                {
                    // 特指ItemEatCompont转变的item || bonus奖励收集掉的item || tap bonus奖励收集掉的item
                    ReleaseItem(item.id);
                    return;
                }
                else if (type == ItemDeadType.OrderBoxOpen)
                {
                    // 订单礼盒开启
                    BoardViewManager.Instance.boardView.boardEffect.ShowOrderBoxDieEffect(item.coord, item.tid);
                    ReleaseItem(item.id);
                    return;
                }
                v.SetDead();
            }
        }

        private void _OnItemEat(Item item, Item food)
        {
            DebugEx.FormatInfo("TestMerge._OnItemEat ----> {0} {1}", item.id, food.id);
            Game.Manager.audioMan.TriggerSound("Spawn");

            if (mItemViewDict.TryGetValue(item.id, out MBItemView eater))
            {
                eater.SetFeedStateChange();
            }
            if (mItemViewDict.TryGetValue(food.id, out MBItemView v))
            {
                // food消亡
                v.SetDead();
            }
            if (eater != null && v != null)
            {
                // track
                if (item.TryGetItemComponent(out ItemEatSourceComponent es))
                {
                    int eat_id = food.tid;
                    var num = es.GetItemCountInStomach(v.data.tid);
                    es.eatItemNeeded.TryGetValue(eat_id, out var total);
                    DataTracker.TrackMergeActionEat(item, ItemUtility.GetItemLevel(item.tid), eat_id, total - num);
                }
                else if (item.TryGetItemComponent(out ItemEatComponent eat))
                {
                    int eat_id = food.tid;
                    var num = eat.GetItemCountInStomach(v.data.tid);
                    eat.GetEatItemNeeded(0).TryGetValue(eat_id, out var total);
                    DataTracker.TrackMergeActionEat(item, ItemUtility.GetItemLevel(item.tid), eat_id, total - num);
                }
            }
        }

        private void _OnItemConsume(Item item, Item consumeTarget)
        {
            DebugEx.FormatInfo("TestMerge._OnItemConsume ----> {0} {1}", item.id, consumeTarget.id);
            GuideUtility.OnItemConsume(item, consumeTarget);
            if (mItemViewDict.TryGetValue(consumeTarget.id, out var src))
            {
                mItemViewDict.TryGetValue(item.id, out var dst);
                var context = new ItemInteractContext()
                {
                    src = src,
                    dst = dst,
                };
                src.SetConsume(context);
            }
        }

        private void _OnItemStateChange(Item item, ItemStateChangeContext context)
        {
            DebugEx.FormatInfo("TestMerge._OnItemStateChange ----> {0}", item.id);

            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                if (context != null && context.reason == ItemStateChangeContext.ChangeReason.TrigAutoSourceDead)
                {
                    if (context.from != null && mItemViewDict.TryGetValue(context.from.id, out var fromView))
                        context.SetFromView(fromView);
                    v.SetDelayUnlock(context);
                }
                else
                {
                    var isInBoxBefore = v.isInBox;
                    v.RefreshOnComponentChange();
                    // 开箱
                    if (isInBoxBefore != v.isInBox)
                    {
                        if (item.unLockLevel > 0)
                        {
                            // 等级解锁
                            var res = BoardUtility.GetLevelLockBg();
                            BoardViewManager.Instance.ShowUnlockLevelEffect(item.coord, res, item.unLockLevel);
                        }
                        else
                        {
                            // 自然解锁
                            BoardViewManager.Instance.ShowUnlockNormalEffect(item.coord);
                        }
                    }
                }
            }
        }

        private void _CalcInventory(out int itemNum, out int spaceNum)
        {
            var inv = BoardViewManager.Instance.world.inventory;
            inv.CalcInventoryMetric(out itemNum, out spaceNum);
        }

        private void _OnItemPutIntoInventory(Item item)
        {
            DebugEx.FormatInfo("TestMerge._OnItemPutIntoInventory ----> {0}", item.id);

            GuideUtility.OnItemPutIntoInventory(item);

            // track
            // _CalcInventory(out var itemNum, out var spaceNum);
            // DataTracker.TrackBagIn(item.tid, itemNum, spaceNum);

            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                v.SetDead();
            }
        }

        private void _OnItemTakeOutFromInventory(Item item)
        {
            DebugEx.FormatInfo("TestMerge._OnItemTakeOutFromInventory ----> {0}", item.id);
            // track
            _CalcInventory(out var itemNum, out var spaceNum);
            // DataTracker.TrackBagOut(item.tid, itemNum, spaceNum);

            mInventorySpawnQueue.Add(item);

            // 直接刷新选中
            BoardViewManager.Instance.RefreshInfo(item.coord.x, item.coord.y);

            // if (!mItemViewDict.TryGetValue(item.id, out MBItemView v))
            // {
            //     var view = _CreateView(item);
            //     view.SetSpawnFromInventory();
            // }
            // else
            // {
            //     mItemViewDict[item.id].SetSpawnFromInventory();
            // }
        }

        private void _OnItemSell(Item item, RewardCommitData reward)
        {
            DebugEx.FormatInfo("TestMerge._OnItemSell ----> {0}", item.id);

            if (reward.rewardCount > 0)
            {
                BoardViewManager.Instance.ShowSellItemReward(item, reward);
            }

            // 播放死亡动画
            if (mItemViewDict.TryGetValue(item.id, out var v))
            {
                v.SetDead();
            }

            // track
            DataTracker.TrackMergeActionSell(item, ItemUtility.GetItemLevel(item.tid));
        }

        private void _OnChestWaitStart(Item item)
        {
            DebugEx.FormatInfo("TestMerge._OnChestWaitStart ----> {0}", item.id);
            if (mItemViewDict.TryGetValue(item.id, out var v))
            {
                v.RefreshChestTip();
            }
        }

        private void _OnChestWaitFinish(Item item)
        {
            DebugEx.FormatInfo("TestMerge._OnChestWaitFinish ----> {0}", item.id);
            if (mItemViewDict.TryGetValue(item.id, out var v))
            {
                v.RefreshChestTip();
            }
        }

        private void _OnItemComponentChange(Item item)
        {
            DebugEx.FormatInfo("TestMerge._OnItemComponentChange ----> {0}", item.id);

            // TODO: 极端情况
            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                v.RefreshOnComponentChange();
            }
        }

        private void _OnUseTimeScaleSource(Item item)
        {
            DebugEx.FormatInfo("TestMerge._OnUseTimeScaleSource ----> {0}", item.id);

            if (mItemViewDict.TryGetValue(item.id, out MBItemView v))
            {
                v.RefreshOnComponentChange();
            }
        }

        #endregion
    }
}