/*
 * @Author: qun.chao
 * @Date: 2021-02-19 17:38:51
 */
namespace FAT
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using EL;
    using Merge;
    using fat.rawdata;
    using System.Linq.Expressions;

    public class BoardViewManager : Singleton<BoardViewManager>
    {
        private int width = 0;
        private int height = 0;

        public MatchChecker checker { get { return mChecker; } }
        public Vector2 inventoryEntryScreenPos { get; private set; }  // 非整数格

        private List<IMergeBoard> mAllCompList = new List<IMergeBoard>();

        private int mSelectedItemId = -1;
        private Item mItemInDrag;
        private Item mItemTryToDrag;
        private MatchChecker mChecker = new MatchChecker();
        private MergeHelper mMergeHelper = new MergeHelper();
        private Dictionary<int, int> mActiveBubbleCache = new Dictionary<int, int>();   //当前活跃的泡泡棋子
        private Dictionary<int, int> mActiveBubbleFrozenCache = new Dictionary<int, int>(); //当前活跃的冰冻棋子
        private Dictionary<int, int> mActiveItemCache = new Dictionary<int, int>();
        private Dictionary<int, int> mInSandItemCache = new Dictionary<int, int>(); // 沙尘中的物品数量记录
        private Dictionary<int, int> mActiveBonusCache = new();
        private Dictionary<int, int> mActiveChestCache = new();
        private Dictionary<int, int> mActiveAutoSourceCache = new();
        private Dictionary<int, int> mActiveTapSourceCache = new();

        public Dictionary<int, int> ActiveBonusCache => mActiveBonusCache;

        public Dictionary<int, int> ActiveChestCache => mActiveChestCache;

        public Dictionary<int, int> ActiveAutoSourceCache => mActiveAutoSourceCache;

        public Dictionary<int, int> ActiveTapSourceCache => mActiveTapSourceCache;

        private int mCDItemNum;
        private bool mIsDirty;
        private bool mIsBgDirty;
        private bool mIsLevelDirty;
        private bool mPauseBoard;

        // private bool mShowOpenBoxEffect = true;
        private UnityEngine.Coroutine mCoAppearEffect = null;

        private Item mCurrentBoardInfoItem { get; set; }
        private float mLastActiveTime;
        private int mOverrideDragItemId;
        private int mOverrideDragItemRuntimeId;
        private MergeHelper.MergeAction mOverrideDragBehaviour;

        #region refactor
        private MBBoardView view;
        public MBBoardView boardView => view;
        public MergeWorld world { get; private set; }
        public Board board { get; private set; }
        public bool IsReady { get { return world != null; } }

        public bool IsFiltering => usable_check_filter != null;
        public System.Func<Item, bool> usable_check_filter { get; private set; }
        public System.Func<Item, bool> ignore_filter { get; private set; }
        public ItemEffectType filterEffectType;
        public RectTransform moveRoot => moveRootOverride != null ? moveRootOverride : view.moveRoot;
        private RectTransform moveRootOverride; // 坐标体系不同于boardRoot 需要单独计算

        public void OnBoardEnter(MergeWorld w, MergeWorldTracer tracer, MBBoardView v)
        {
            mOverrideDragItemId = 0;
            mOverrideDragItemRuntimeId = 0;
            mOverrideDragBehaviour = MergeHelper.MergeAction.None;
            mLastActiveTime = 0;
            Game.Manager.mergeBoardMan.SetCurrentActiveWorld(w);
            Game.Manager.mergeBoardMan.SetCurrentActiveTracer(tracer);
            // // 初次回到棋盘 主动触发棋盘change => 触发订单刷新 => 自动清理过期订单
            // tracer.Invalidate();

            world = w;
            view = v;
            board = w.activeBoard;
            width = board.size.x;
            height = board.size.y;

            _CalcScaleCoe();
            _CalcSize();
            _CalcOrigin();
            BoardUtility.ClearSpawnRequest();

            mIsDirty = true;
            mIsBgDirty = true;
            mIsLevelDirty = true;
            world.onItemEvent += _OnItemEvent;
            board.onItemEnter += _OnItemEnter;
            board.onItemLeave += _OnItemLeave;
            board.onItemMove += _OnItemMove;
            board.onItemStateChange += _OnItemStateChange;
            board.onItemComponentChange += _OnItemComponentChange;

            world.onCollectBonus += _OnCollectBonus;
            world.onCollectTapBonus += _OnCollectTapBonus;
            board.onLackOfEnergy += _OnLackOfEnergy;
            board.onFeatureClicked += _OnFeatureClicked;
            board.onUseTimeSkipper += _OnUseTimeSkipper;
            board.onUseTimeScaleSource += _OnUseTimeScaleSource;
            board.onJumpCDBegin += _OnJumpCDBegin;
            board.onJumpCDEnd += _OnJumpCDEnd;
            board.onTokenMultiBegin += _OnTokenMultiBegin;
            board.onTokenMultiEnd += _OnTokenMultiEnd;
            board.onChoiceBoxWaiting += _OnChoiceBoxWaiting;

            MessageCenter.Get<MSG.GAME_BACKGROUND_BACK>().AddListener(_OnMessageFocused);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().AddListener(_OnMessageLevelChange);
            MessageCenter.Get<MSG.GAME_ORDER_CHANGE>().AddListener(_OnMessageOrderCompleted);
            MessageCenter.Get<MSG.GAME_ORDER_DISPLAY_CHANGE>().AddListener(_OnMessageOrderDisplayChange);
            MessageCenter.Get<MSG.BOARD_FLY_SCORE>().AddListener(_OnMessageFlyScore);

            _BindAllComponent();
            _SetupComponent();

            mChecker.Setup();

            // appear effect
            // _TryShowAppearEffectAfterLoadingFinished();
            DataTracker.TrackLogInfo($"OnBoardEnter worldId = {world?.activeBoard?.boardId ?? -1}");
        }

        public void OnBoardLeave()
        {
            DataTracker.TrackLogInfo($"OnBoardLeave worldId = {world?.activeBoard?.boardId ?? -1}");
            Game.Manager.mergeBoardMan.SetCurrentActiveWorld(null);
            Game.Manager.mergeBoardMan.SetCurrentActiveTracer(null);

            world.onItemEvent -= _OnItemEvent;
            board.onItemEnter -= _OnItemEnter;
            board.onItemLeave -= _OnItemLeave;
            board.onItemMove -= _OnItemMove;
            board.onItemStateChange -= _OnItemStateChange;
            board.onItemComponentChange -= _OnItemComponentChange;

            world.onCollectBonus -= _OnCollectBonus;
            world.onCollectTapBonus -= _OnCollectTapBonus;
            board.onLackOfEnergy -= _OnLackOfEnergy;
            board.onFeatureClicked -= _OnFeatureClicked;
            board.onUseTimeSkipper -= _OnUseTimeSkipper;
            board.onUseTimeScaleSource -= _OnUseTimeScaleSource;
            board.onJumpCDBegin -= _OnJumpCDBegin;
            board.onJumpCDEnd -= _OnJumpCDEnd;
            board.onTokenMultiBegin -= _OnTokenMultiBegin;
            board.onTokenMultiEnd -= _OnTokenMultiEnd;
            board.onChoiceBoxWaiting -= _OnChoiceBoxWaiting;

            MessageCenter.Get<MSG.GAME_BACKGROUND_BACK>().RemoveListener(_OnMessageFocused);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().RemoveListener(_OnMessageLevelChange);
            MessageCenter.Get<MSG.GAME_ORDER_CHANGE>().RemoveListener(_OnMessageOrderCompleted);
            MessageCenter.Get<MSG.GAME_ORDER_DISPLAY_CHANGE>().RemoveListener(_OnMessageOrderDisplayChange);
            MessageCenter.Get<MSG.BOARD_FLY_SCORE>().RemoveListener(_OnMessageFlyScore);
            // 不能再undo
            world.SetSoldItem(null);

            mIsDirty = false;
            mIsBgDirty = false;
            mIsLevelDirty = false;
            mSelectedItemId = -1;
            mItemInDrag = null;

            mChecker.Cleanup();
            _CleanupComponent();

            mActiveItemCache.Clear();
            mInSandItemCache.Clear();
            mActiveBubbleCache.Clear();
            mActiveBubbleFrozenCache.Clear();
            mAllCompList.Clear();
            mActiveBonusCache.Clear();
            mActiveChestCache.Clear();
            mActiveAutoSourceCache.Clear();
            mActiveTapSourceCache.Clear();

            world = null;
            view = null;
            BoardUtility.ClearSpawnRequest();
        }
        #endregion

        #region all sub board

        private void _BindAllComponent()
        {
            mAllCompList.Clear();

            var v = view;
            _RegisterComponent(v.boardBg);
            _RegisterComponent(v.boardFg);
            _RegisterComponent(v.boardInd);
            _RegisterComponent(v.boardDrag);
            _RegisterComponent(v.boardHolder);
            _RegisterComponent(v.boardSelector);
            _RegisterComponent(v.boardEffect);
        }

        private void _RegisterComponent(IMergeBoard board)
        {
            mAllCompList.Add(board);
        }

        private void _SetupComponent()
        {
            foreach (var comp in mAllCompList)
            {
                comp.Setup(width, height);
            }
        }

        private void _CleanupComponent()
        {
            foreach (var comp in mAllCompList)
            {
                comp.Cleanup();
            }
        }

        #endregion

        public void SetMoveRootOverride(RectTransform rt)
        {
            moveRootOverride = rt;
        }

        public void RefreshInventoryEntryScreenPos(Vector2 sp)
        {
            inventoryEntryScreenPos = sp;
        }

        public void OnInventoryClose()
        {
            if (view == null) return;
            if (view.boardHolder.TryResolveInventorySpawnQueue())
            {
                // 背包有物体取出
                Game.Manager.audioMan.TriggerSound("BoardReward");
            }
            mIsDirty = true;
        }

        public void SetPause(bool b)
        {
            mPauseBoard = b;
        }

        public bool IsDragItem()
        {
            return mItemInDrag != null;
        }

        public bool IsItemInDrag(Item item)
        {
            return item != null && item == mItemInDrag;
        }

        public bool IsItemCanPutInInventory()
        {
            if (mItemInDrag == null)
                return false;
            return ItemUtility.CanItemInInventory(mItemInDrag);
        }

        public void OnUserActive()
        {
            mLastActiveTime = Time.time;
            mChecker.StopMatchHintAnim();
        }

        private Vector2 beginScreenOffset = Vector2.zero;
        public void OnBeginDragAtTile(int x, int y, Vector2 pointerScreenPos)
        {
            mItemTryToDrag = null;
            var item = board.GetItemByCoord(x, y);
            if (item == null)
            {
                mItemInDrag = null;
                return;
            }

            _TryGrabItem(x, y, Vector2.zero, pointerScreenPos, item);
        }

        private Vector2 beginDragBoardOffset = Vector2.zero;
        private void _TryGrabItem(int coordX, int coordY, Vector2 offset, Vector2 pointerScreenPos, Item item)
        {
            if (item != null && mOverrideDragItemId > 0 && item.tid != mOverrideDragItemId)
            {
                DebugEx.Info($"[GUIDE] ignore drag item {item?.tid}");
                return;
            }

            if (item != null && mOverrideDragItemRuntimeId > 0 && item.id != mOverrideDragItemRuntimeId)
            {
                DebugEx.Info($"[GUIDE] ignore drag item {item?.id}@{item?.tid}");
                return;
            }

            if (board.GetGridTid(coordX, coordY) == (int)GridState.CantMove)
            {
                return;
            }

            //棋子不可被拖拽时返回
            if (item != null && !item.isMovable)
            {
                return;
            }

            Vector2 beginDragScreenPos;
            if (BoardUtility.snapToFinger)
            {
                beginDragScreenPos = pointerScreenPos;
                beginScreenOffset = Vector2.zero;
                beginDragBoardOffset = offset;
            }
            else
            {
                beginDragScreenPos = BoardUtility.GetScreenPosByCoord(coordX, coordY);
                beginScreenOffset = BoardUtility.GetScreenPosByCoord(coordX, coordY) - pointerScreenPos;
                beginDragBoardOffset = Vector2.zero;
            }

            if (view.boardHolder.GrabItem(item, beginDragScreenPos))
            {
                // 设置为拖拽中的item
                mItemInDrag = item;

                // item有变化则更新信息区
                if (item.id != mSelectedItemId)
                {
                    _RefreshInfo(coordX, coordY);
                }

                // 拖拽中隐藏选择框
                _HideSelector();

                // 应用filter
                if (item.TryGetItemComponent(out ItemSkillComponent skill) && skill.IsNeedTarget())
                {
                    _Setup_Filter_For_Skill(skill);
                    _ApplyFilter();
                }
                else if (_IsEdible(item))
                {
                    _Setup_Filter_For_Feed();
                    _ApplyFilter();
                }

                // 不再需要拖拽尝试
                mItemTryToDrag = null;

                // item拖拽开始
                view.boardBg.Refresh();
                VibrationManager.VibrateLight();
            }
            else
            {
                // 因item状态不在idle drag失败 则先记录等之后尝试触发
                if (item.isActive && !item.isDead && view.boardHolder.FindItemView(item.id) != null)
                    mItemTryToDrag = item;
            }
        }

        private (Item item, MergeHelper.MergeAction act) _CheckDragBehaviour(Vector2 pointerScreenPos)
        {
            var ret = mMergeHelper.CheckDragBehaviour(pointerScreenPos + beginScreenOffset, mItemInDrag);
            if (mOverrideDragBehaviour != MergeHelper.MergeAction.None)
            {
                if (ret.act != mOverrideDragBehaviour)
                {
                    // 不符合预期 覆盖为无效操作
                    ret.act = MergeHelper.MergeAction.None;
                }
            }
            return ret;
        }

        public void OnDrag(Vector2 offset, Vector2 pointerScreenPos)
        {
            if (mItemInDrag == null)
            {
                // 尝试恢复之前未能成功发起的drag
                if (mItemTryToDrag != null)
                {
                    if (view.boardHolder.CanGrabItem(mItemTryToDrag))
                    {
                        _TryGrabItem(mItemTryToDrag.coord.x, mItemTryToDrag.coord.y, offset, pointerScreenPos, mItemTryToDrag);
                    }
                }
                return;
            }
            view.boardHolder.DragItem(mItemInDrag, offset - beginDragBoardOffset);

            var ret = _CheckDragBehaviour(pointerScreenPos);
            if (ret.act == MergeHelper.MergeAction.Inventory)
            {
                _ShowInventoryIndicator();
                _HideMergeHighlight();
            }
            else if (ret.act == MergeHelper.MergeAction.Merge ||
                    ret.act == MergeHelper.MergeAction.Feed ||
                    ret.act == MergeHelper.MergeAction.Consume ||
                    ret.act == MergeHelper.MergeAction.Mix)
            {
                _ShowMergeHighlight(ret.item);
                _HideInventoryIndicator();
            }
            else if (ret.act == MergeHelper.MergeAction.Skill)
            {
                // TODO: 特效专门化
                _ShowMergeHighlight(ret.item);
                _HideInventoryIndicator();
            }
            else if (ret.act == MergeHelper.MergeAction.Custom)
            {
                MessageCenter.Get<MSG.UI_BOARD_DRAG_ITEM_CUSTOM>().Dispatch(pointerScreenPos, mItemInDrag);
            }
            else
            {
                _HideMergeHighlight();
                _HideInventoryIndicator();
            }
        }

        public void OnEndDrag(Vector2 pointerScreenPos)
        {
            var _itemInDrag = mItemInDrag;
            if (_itemInDrag == null)
                return;
            var ret = _CheckDragBehaviour(pointerScreenPos);
            if (ret.act == MergeHelper.MergeAction.Inventory)
            {
                if (_TryPutInInventory(_itemInDrag))
                {
                    _CancelSelect();
                    _ShowInventoryPutInFeedback();
                }
                else
                {
                    view.boardHolder.MoveBack(_itemInDrag);
                }
            }
            else if (ret.act == MergeHelper.MergeAction.Feed)
            {
                if (ItemUtility.FeedItem(ret.item, _itemInDrag) == ItemUseState.Success)
                {
                    // 选中目标
                    _RefreshInfo(ret.item.coord.x, ret.item.coord.y);
                }
            }
            else if (ret.act == MergeHelper.MergeAction.Stack)
            {
                var stackFrom = _itemInDrag;
                if (ItemUtility.StackToTarget(_itemInDrag, ret.item))
                {
                    if (!stackFrom.isDead)
                    {
                        // 未耗尽
                        view.boardHolder.MoveBack(stackFrom);
                    }
                    // 选中目标
                    _RefreshInfo(ret.item.coord.x, ret.item.coord.y);
                }
                else
                {
                    // 无法堆叠 tip
                    view.boardHolder.MoveBack(stackFrom);
                }
            }
            else if (ret.act == MergeHelper.MergeAction.Skill)
            {
                var skillItem = _itemInDrag;
                if (skillItem.TryGetItemComponent(out ItemSkillComponent skill))
                {
                    if (skill.type == SkillType.Degrade)
                    {
                        // 先移回原位
                        view.boardHolder.MoveBack(skillItem);
                        UIUtility.ShowItemUseConfirm(I18N.Text("#SysComDesc343"),
                            () => { _UseSkillToTarget(skillItem, ret.item); },
                            skillItem.tid,
                            ret.item.tid);
                    }
                    else if (skill.type == SkillType.Upgrade)
                    {
                        // 先移回原位
                        view.boardHolder.MoveBack(skillItem);
                        UIUtility.ShowItemUseConfirm(I18N.Text("#SysComDesc342"),
                            () => { _UseSkillToTarget(skillItem, ret.item); },
                            skillItem.tid,
                            ret.item.tid);
                    }
                    else
                    {
                        _UseSkillToTarget(skillItem, ret.item);
                    }
                }
            }
            else if (ret.act == MergeHelper.MergeAction.Merge)
            {
                var coord = ret.item.coord;
                // merge
                board.Merge(_itemInDrag, ret.item);
                // show info
                _RefreshInfo(coord.x, coord.y);
                VibrationManager.VibrateMedium();
            }
            else if (ret.act == MergeHelper.MergeAction.Consume)
            {
                var coord = ret.item.coord;
                board.UseClickItemSourceWithConsume(ret.item, _itemInDrag, out var state);
                if (state != ItemUseState.Success)
                {
                    // 消耗失败
                    view.boardHolder.MoveBack(_itemInDrag);
                    // show info
                    _RefreshInfo(_itemInDrag.coord.x, _itemInDrag.coord.y);
                }
                else
                {
                    if (!ret.item.isDead)
                    {
                        _RefreshInfo(coord.x, coord.y);
                    }
                }
            }
            else if (ret.act == MergeHelper.MergeAction.Mix)
            {
                var coord = ret.item.coord;
                if (board.MixSourceConsume(ret.item, _itemInDrag, out var state))
                {
                    // 消耗成功 直接尝试产出
                    board.MixSourceProduce(ret.item, out state);
                    if (!ret.item.isDead)
                    {
                        _RefreshInfo(coord.x, coord.y);
                    }
                }
                else
                {
                    // 消耗失败
                    ItemUtility.ProcessItemUseState(ret.item, state);

                    view.boardHolder.MoveBack(_itemInDrag);
                    _RefreshInfo(_itemInDrag.coord.x, _itemInDrag.coord.y);
                }
            }
            else if (ret.act == MergeHelper.MergeAction.Custom)
            {
                MessageCenter.Get<MSG.UI_BOARD_DRAG_ITEM_END_CUSTOM>().Dispatch(pointerScreenPos,_itemInDrag);
            }
            else
            {
                var nearestCell = mMergeHelper.GetNearestCell();

                // 主动判断目标格子状态 避免特殊动画流程被打断
                var should_not_move = false;
                var target_item = board.GetItemByCoord(nearestCell.col, nearestCell.row);
                if (target_item != null)
                {
                    var target_view = GetItemView(target_item.id);
                    if (target_view != null && target_view.IsViewCantSwap())
                    {
                        // 避免move操作打断原地spawn | 比如星想事成的spawn耗时会很长 此时进行操作会让画面体验不连贯
                        should_not_move = true;
                    }
                }

                if (should_not_move || !board.MoveItem(_itemInDrag, nearestCell.col, nearestCell.row, out var ms))
                {
                    // 拖拽结束
                    MessageCenter.Get<MSG.UI_BOARD_DRAG_ITEM_END>().Dispatch(nearestCell.screenPos, _itemInDrag);
                    // 有可能触发交订单
                    if (_itemInDrag == null)
                    {
                        // 物品已被收走
                    }
                    else
                    {
                        // 占位失败
                        view.boardHolder.MoveBack(_itemInDrag);
                        // show info
                        _RefreshInfo(_itemInDrag.coord.x, _itemInDrag.coord.y);
                    }
                }
                else
                {
                    // 位置更新
                    _RefreshInfo(_itemInDrag.coord.x, _itemInDrag.coord.y);
                }
            }

            _OnDragFinish();
        }

        private void _OnDragFinish()
        {
            mItemInDrag = null;
            _RemoveFilter();
            _HideMergeHighlight();
            _HideInventoryIndicator();
        }

        public void OnClickAtTile(int x, int y)
        {
            var item = board.GetItemByCoord(x, y);
            if (item != null)
            {
                if (!item.isLocked)
                {
                    if (item.isFrozen)
                    {
                        Game.Manager.commonTipsMan.ShowPopTips(Toast.ItemLocked, BoardUtility.GetWorldPosByCoord(item.coord));
                        Game.Manager.audioMan.TriggerSound("ClickLockHalf");
                    }
                    if (item.id == mSelectedItemId || item.HasComponent(ItemComponentType.FeatureEntry))
                    {
                        view.boardHolder.ClickItem(item);
                    }
                    view.boardHolder.TapItem(item.id);
                }
                else
                {
                    if (!item.isReachBoardLevel)
                    {
                        Game.Manager.commonTipsMan.ShowPopTips(Toast.BoardLevel, BoardUtility.GetWorldPosByCoord(item.coord), item.unLockLevel);
                    }
                    // 播放反馈特效
                    view.boardEffect.ShowTapLockedEffect(item.coord);
                    //Game.Manager.audioMan.TriggerSound(view.);
                    Game.Manager.audioMan.TriggerSound(view.BoardRes.TapLockedSound);
                }
                if (mSelectedItemId != item.id)
                {
                    VibrationManager.VibrateLight();
                    view.boardHolder.SetSelectedItem(item.id);
                }
            }
            _RefreshInfo(x, y);
        }

        private void _UseSkillToTarget(Item skillItem, Item targetItem)
        {
            if (skillItem == null || skillItem.parent == null || targetItem == null || targetItem.parent == null)
                return;
            // UseForTarget只能用于skill组件
            if (!skillItem.TryGetItemComponent(out ItemSkillComponent skill))
                return;

            int sandGlassSeconds_before = skill.sandGlassSeconds;

            if (ItemUtility.UseForTarget(skillItem, targetItem))
            {
                if (skill.type == SkillType.Degrade)
                {
                    // track
                    var catCfg = Env.Instance.GetCategoryByItem(targetItem.tid);
                    var idx = catCfg.Progress.IndexOf(targetItem.tid);
                    var afterItemId = -1;
                    if (idx > 0)
                    {
                        afterItemId = catCfg.Progress[idx - 1];
                    }
                    DataTracker.TrackMergeActionSkillScissor(targetItem, ItemUtility.GetItemLevel(targetItem.tid), skillItem.tid, skill.stackCount, afterItemId);
                }
                else if (skill.type == SkillType.Lightbulb)
                {
                    var pos = CoordToWorldPos(targetItem.coord);
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.Battery, pos, skill.param2[0]);
                    DataTracker.board_active.Track(skillItem.tid, targetItem.tid);
                }
                else if (skill.type == SkillType.Upgrade)
                {
                    // track
                    DataTracker.TrackMergeActionSkillUpgrade(targetItem, ItemUtility.GetItemLevel(targetItem.tid), skillItem.tid, skill.stackCount);
                }
                else if (skill.type == SkillType.SandGlass)
                {
                    // track
                    DataTracker.TrackMergeActionSkillHourGlass(targetItem,
                        ItemUtility.GetItemLevel(targetItem.tid),
                        skillItem.tid,
                        sandGlassSeconds_before,
                        skillItem.isDead ? 0 : skill.sandGlassSeconds);
                }

                if (!skillItem.isDead)
                {
                    view.boardHolder.MoveBack(skillItem);
                    // 选中当前位置
                    _RefreshInfo(skillItem.coord.x, skillItem.coord.y);
                }
                else
                {
                    // 选中目标位置
                    _RefreshInfo(targetItem.coord.x, targetItem.coord.y);
                }
                //使用技能棋子成功时发事件
                MessageCenter.Get<MSG.GAME_BOARD_ITEM_SKILL>().Dispatch(skillItem, skill.type);
            }
        }

        #region filter

        private void _ApplyFilter()
        {
            view.boardHolder.ApplyFilter();
        }

        private void _RemoveFilter()
        {
            if (!IsFiltering) return;
            usable_check_filter = null;
            ignore_filter = null;
            view.boardHolder.RemoveFilter();
        }

        private bool _IsEdible(Item food)
        {
            Item item;
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; j++)
                {
                    item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        if (ItemUtility.CanFeed(food, item))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void _Setup_Filter_For_Skill(ItemSkillComponent skill)
        {
            _Setup_Filter_Skill_Default();
        }

        private void _Setup_Filter_For_Feed()
        {
            _Setup_Filter_Feed();
        }

        private void _Setup_Filter_Feed(ItemEffectType effectType = ItemEffectType.Filter_Feed)
        {
            bool _CanUse(Item _item)
            {
                if (mItemInDrag == null) return false;
                if (ItemUtility.CanFeed(mItemInDrag, _item)) return true;
                return false;
            }

            bool _Ignore(Item _item) { return true; }

            usable_check_filter = _CanUse;
            ignore_filter = _Ignore;
            filterEffectType = effectType;
        }

        private void _Setup_Filter_Skill_Default(ItemEffectType effectType = ItemEffectType.Filter_Scissor)
        {
            bool _CanUse(Item _item)
            {
                if (mItemInDrag == null) return false;
                if (ItemUtility.CanUseForTarget(mItemInDrag, _item, out var state)) return true;
                return false;
            }

            bool _Ignore(Item _item)
            {
                if (mItemInDrag == null) return true;
                if (_item.id == mItemInDrag.id) return true;
                if (board.CanMerge(mItemInDrag, _item)) return true;
                if (ItemUtility.CanStack(mItemInDrag, _item)) return true;
                return false;
            }

            usable_check_filter = _CanUse;
            ignore_filter = _Ignore;
            filterEffectType = effectType;
        }

        #endregion

        private bool _TryPutInInventory(Item item)
        {
            // 奖励类item可能直接使用
            if (item.TryGetItemComponent<ItemBonusCompoent>(out var bonus) && bonus.inventoryAutoUse)
            {
                item.parent.UseBonusItem(item);
                return true;
            }

            if (!ItemUtility.CanItemInInventory(item))
            {
                // 物品类型不符合
                Game.Manager.commonTipsMan.ShowPopTips(Toast.BagIllegal);
                return false;
            }

            // 尝试放入
            if (!board.PutItemInInventory(item))
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.BagFull);
                return false;
            }

            // 已放入
            return true;
        }

        #region misc

        public void OnItemChangeState(Item item, ItemLifecycle state)
        {
            switch (state)
            {
                default:
                    view.boardBg.Refresh();
                    break;
            }
        }

        public void OnItemFlagChange()
        {
            view.boardBg.Refresh();
        }

        public void SetCheckerMask(Vector2Int coord)
        {
            mChecker.MarkCoord(coord.x, coord.y);
            mIsDirty = true;
        }

        public void UnsetCheckerMask(Vector2Int coord)
        {
            mChecker.UnmarkCoord(coord.x, coord.y);
            mIsDirty = true;
        }

        public void SetCurrentBoardInfoItem(Item item)
        {
            mCurrentBoardInfoItem = item;
        }

        public Item GetCurrentBoardInfoItem()
        {
            return mCurrentBoardInfoItem;
        }

        public int GetCurrentBoardInfoItemTid()
        {
            return mCurrentBoardInfoItem == null ? 0 : mCurrentBoardInfoItem.tid;
        }

        public void RefreshInfo(int x, int y)
        {
            _RefreshInfo(x, y);
        }

        public Vector2 ReAnchorItemForDrag(RectTransform itemView, Vector2 targetScreenPos)
        {
            itemView.SetParent(moveRoot);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(moveRoot, targetScreenPos, null, out var lp);
            itemView.anchoredPosition = lp;
            return lp;
        }

        public void ReAnchorItemForMove(Transform itemView)
        {
            // move层可能被外部transform重载 / 坐标体系单独计算
            itemView.SetParent(moveRoot, true);
        }

        public void ReAnchorItemForPair(Transform itemView)
        {
            itemView.SetParent(view.pairRoot, true);
        }

        public void HoldItemIfNotInMoveLayer(Item item)
        {
            var v = view.boardHolder.FindItemView(item.id);
            if (v != null && v.transform.parent != moveRoot)
            {
                view.boardHolder.HoldItem(item.coord.x, item.coord.y, v.transform as RectTransform);
            }
        }

        public void HoldItem(int x, int y, RectTransform itemView)
        {
            view.boardHolder.HoldItem(x, y, itemView);
        }

        // public void AddMatchHint(int id)
        // {
        //     view.boardHolder.AddHintForMatch(id);
        // }

        // public void RemoveMatchHint(int id)
        // {
        //     view.boardHolder.RemoveHintForMatch(id);
        // }

        public void ReleaseItem(int id)
        {
            view.boardHolder.ReleaseItem(id);
        }

        public MBItemView GetItemView(int id)
        {
            return view.boardHolder.FindItemView(id);
        }

        // 接管棋盘item 用于场外使用
        public MBItemView TakeoverItem(int id)
        {
            return view.boardHolder.TakeoverItem(id);
        }

        public void ShowUnlockNormalEffect(Vector2Int coord)
        {
            view.boardEffect.ShowUnlockNormalEffect(coord);
            Game.Manager.audioMan.TriggerSound("BoardSandUnlock");
        }

        public void ShowUnlockLevelEffect(Vector2Int coord, Config.AssetConfig res, int level)
        {
            view.boardEffect.ShowUnlockLevelEffect(coord, level);
        }

        // public void ShowSpeedUpTip(Vector2Int coord)
        // {
        //     view.boardEffect.ShowSpeedUpTip(coord);
        // }

        // public void ShowSellTip(Vector2Int coord)
        // {
        //     view.boardEffect.ShowSellTip(coord);
        // }

        public GameObject ShowInstantEffect(Vector2Int coord, string key, float lifetime)
        {
            return view.boardEffect.ShowInstantEffect(coord, key, lifetime);
        }

        public void AddStateEffect(Vector2Int coord, string eff)
        {
            view.boardEffect.AddStateEffect(coord, eff);
        }

        public void RemoveStateEffect(Vector2Int coord, string eff)
        {
            view.boardEffect.RemoveStateEffect(coord, eff);
        }

        public void ShowSellItemReward(Item item, RewardCommitData reward)
        {
            UIFlyUtility.FlyReward(reward, _CoordToWorldPos(item.coord));
        }

        public MBBoardItemHolder GetBoardItemHolder()
        {
            return view.boardHolder;
        }

        #endregion

        #region eff / indicator

        private void _HideSelector()
        {
            view.boardSelector.Hide();
            view.boardInd.Hide();
        }

        private void _RefreshInfo(int x, int y)
        {
            var item = board.GetItemByCoord(x, y);
            if (item == null)
            {
                _CancelSelect();
                var coord = new Vector2Int(x, y);
                if (BoardUtility.FiilMatchItemList(coord) > 0)
                {
                    view.boardSelector.ForceShowSelector(coord.x, coord.y);
                    MessageCenter.Get<MSG.UI_BOARD_SELECT_CELL>().Dispatch(coord);
                }
            }
            else
            {
                // 不能再undo
                world.SetSoldItem(null);
                _SetSelectItem(item);
            }
        }

        private void _SetSelectItem(Item item)
        {
            _SetDeselectItem();
            mSelectedItemId = item.id;
            MessageCenter.Get<MSG.UI_BOARD_SELECT_ITEM>().Dispatch(item);
            view.boardSelector.Show(item.coord.x, item.coord.y);
            view.boardInd.Show(item.coord.x, item.coord.y);
            view.boardHolder.SetSelectItem(mSelectedItemId);
        }

        private void _SetDeselectItem()
        {
            if (mSelectedItemId >= 0)
                view.boardHolder.SetDeselectItem(mSelectedItemId);
            mSelectedItemId = -1;
        }

        private void _CancelSelect()
        {
            _SetDeselectItem();
            MessageCenter.Get<MSG.UI_BOARD_SELECT_ITEM>().Dispatch(null);
            _HideSelector();
        }

        //供外部主动调用 用于取消选中当前物品 不发消息
        public void CancelSelectCurItem()
        {
            _SetDeselectItem();
            _HideSelector();
            SetCurrentBoardInfoItem(null);
        }

        private void _ShowMergeHighlight(Item item)
        {
            view.boardEffect.ShowHighlight(BoardUtility.GetPosByCoord(item.coord.x, item.coord.y));
        }

        private void _HideMergeHighlight()
        {
            view.boardEffect.HideHighlight();
        }

        private void _ShowInventoryIndicator()
        {
            view.boardEffect.ShowInventoryInd(inventoryEntryScreenPos);
            // 避免放入背包连贯操作时音效嘈杂 不再播放
            // Game.Manager.audioMan.TriggerSound("InventoryInd");
        }

        private void _HideInventoryIndicator()
        {
            view.boardEffect.HideInventoryInd();
        }

        private void _ShowInventoryPutInFeedback()
        {
            view.boardEffect.ShowInventoryPutInEffect(inventoryEntryScreenPos);
            Game.Manager.audioMan.TriggerSound("InventoryPutIn");
            MessageCenter.Get<MSG.UI_INVENTORY_ENTRY_FEEDBACK>().Dispatch();
        }

        #endregion

        #region check
        public void SyncBoard(float delta)
        {
            DebugEx.FormatInfo("BoardViewManager::SyncBoard ----> {0}", delta);
            if (delta > 0)
            {
                world.Update((int)(delta * 1000));
            }
        }

        // TODO: worldTracer里也有类似逻辑 应该省掉无用计算
        private void _CacheBoardItem()
        {
            mActiveBubbleCache.Clear();
            mActiveBubbleFrozenCache.Clear();
            mActiveItemCache.Clear();
            mInSandItemCache.Clear();
            mCDItemNum = 0;

            Item item;
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; j++)
                {
                    item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        if (item.TryGetItemComponent(out ItemBubbleComponent bubble))
                        {
                            var cache = bubble.IsBubbleItem() ? mActiveBubbleCache : mActiveBubbleFrozenCache;
                            if (cache.ContainsKey(item.tid))
                            {
                                cache[item.tid] += 1;
                            }
                            else
                            {
                                cache.Add(item.tid, 1);
                            }
                        }
                        else if (!item.isLocked && item.isFrozen)
                        {
                            if (mInSandItemCache.ContainsKey(item.tid))
                            {
                                mInSandItemCache[item.tid] += 1;
                            }
                            else
                            {
                                mInSandItemCache.Add(item.tid, 1);
                            }
                        }
                        else if (item.isActive)
                        {
                            // 不精确 统计cd状态的物品
                            if (item.HasComponent(ItemComponentType.ClickSouce, true) && !ItemUtility.IsItemReadyToUse(item))
                            {
                                ++mCDItemNum;
                            }
                            if (mActiveItemCache.ContainsKey(item.tid))
                            {
                                mActiveItemCache[item.tid] = mActiveItemCache[item.tid] + 1;
                            }
                            else
                            {
                                mActiveItemCache.Add(item.tid, 1);
                            }
                        }
                    }
                }
            }
        }

        public Item FindItem(int tid, bool isBubble)
        {
            bool found = false;
            Item target = null;
            board.WalkAllItem((item) =>
            {
                if (!found && item.tid == tid)
                {
                    if (item.isActive)
                    {
                        if (isBubble == item.HasComponent(ItemComponentType.Bubble))
                        {
                            found = true;
                            target = item;
                        }
                    }
                }
            });

            if (found)
            {
                return target;
            }
            return null;
        }

        public Item FindBoostItem()
        {
            bool found = false;
            Item target = null;
            board.WalkAllItem((item) =>
            {
                if (!found)
                {
                    if (item.isActive)
                    {
                        var config = Env.Instance.GetItemComConfig(item.tid)?.clickSourceConfig;
                        if (config != null)
                        {
                            if (config.IsBoostable)
                            {
                                found = true;
                                target = item;
                            }
                        }
                    }
                }
            });
            if (found)
                return target;
            return null;
        }

        public bool HasBubbleItem(ItemBubbleType type, int tid, int num)
        {
            var dict = type == ItemBubbleType.Bubble ? mActiveBubbleCache : mActiveBubbleFrozenCache;
            //tid>0时：表示某个具体id的且含有type类型组件的棋子，在棋盘上的数量是否满足num
            if (tid > 0)
            {
                if (num <= 0)
                {
                    // 精确要求0个
                    if (dict.TryGetValue(tid, out var value))
                    {
                        return value <= 0;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    // 要求数量足够num个
                    if (dict.TryGetValue(tid, out var value))
                    {
                        return num <= value;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            //tid<=0时：表示任意含有type类型组件的棋子，在棋盘上的数量是否满足num
            else
            {
                var totalCount = 0;
                foreach (var itemCount in dict.Values)
                {
                    totalCount += itemCount;
                }

                if (num <= 0)
                    return totalCount <= 0;
                else
                    return num <= totalCount;
            }
        }

        //获取当前棋盘上冰冻棋子的数量
        public int GetBubbleFrozenItemCount()
        {
            var count = 0;
            foreach (var num in mActiveBubbleFrozenCache.Values)
            {
                count += num;
            }
            return count;
        }

        public int GetFirstFrozenItemId()
        {
            foreach (var info in mActiveBubbleFrozenCache)
            {
                if (info.Value > 0)
                    return info.Key;
            }
            return 0;
        }

        public bool HasInSandItem(int tid, int num)
        {
            var dict = mInSandItemCache;
            dict.TryGetValue(tid, out var cur);
            // 0 表示精确到有0个
            // 非0 表示只是有num个
            if (num <= 0)
            {
                return cur <= 0;
            }
            else
            {
                return cur >= num;
            }
        }

        public bool HasActiveItem(int tid, int num)
        {
            var dict = mActiveItemCache;
            if (num <= 0)
            {
                // 精确要求0个
                if (dict.ContainsKey(tid))
                {
                    return dict[tid] <= 0;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (dict.ContainsKey(tid))
                {
                    return num <= dict[tid];
                }
                else
                {
                    return false;
                }
            }
        }

        // 判断是否有点击生成器正在cd状态 (不包括短cd)
        public bool HasClickSourceReviving(int tid)
        {
            var dict = mActiveItemCache;
            if (dict.ContainsKey(tid))
            {
                Item item;
                for (int i = 0; i < width; ++i)
                {
                    for (int j = 0; j < height; j++)
                    {
                        item = board.GetItemByCoord(i, j);
                        if (item != null && item.tid == tid && item.isActive)
                        {
                            if (item.TryGetItemComponent(out ItemClickSourceComponent source))
                            {
                                if (ItemUtility.IsClickSourceReviving(source))
                                {
                                    // 可以speedup
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        // 判断是否有点击生成器可以产出
        public bool HasClickSourceCanOutput(int tid)
        {
            var dict = mActiveItemCache;
            if (dict.ContainsKey(tid))
            {
                Item item;
                for (int i = 0; i < width; ++i)
                {
                    for (int j = 0; j < height; j++)
                    {
                        item = board.GetItemByCoord(i, j);
                        if (item != null && item.tid == tid && item.isActive)
                        {
                            if (item.TryGetItemComponent(out ItemClickSourceComponent click))
                            {
                                if (click.itemCount > 0)
                                    return true;
                            }
                            else if (item.TryGetItemComponent(out ItemEatSourceComponent eat))
                            {
                                if (eat.state == ItemEatSourceComponent.Status.Output)
                                    return true;
                            }
                        }
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// 自动引导使用，更新缓存的各种棋子列表
        /// </summary>
        /// <param name="merge">mergeChest列表</param>
        /// <param name="auto">autoSource列表</param>
        /// <param name="bonus">mergeBonus列表</param>
        /// <param name="tap">tapSource列表</param>>
        public void AllAutoGuideNeedItem(List<Item> merge, List<Item> auto, List<Item> bonus, List<Item> tap)
        {
            merge.Clear();
            auto.Clear();
            bonus.Clear();
            for (var i = 0; i < width; ++i)
                for (var j = 0; j < height; j++)
                {
                    var item = board.GetItemByCoord(i, j);
                    if (item != null && item.isActive)
                    {
                        if (item.TryGetItemComponent(out ItemAutoSourceComponent autoSource))
                            if (autoSource.itemCount > 0)
                                auto.Add(item);
                        if (item.TryGetItemComponent(out ItemChestComponent mergeSource))
                            if (!mergeSource.isWaiting)
                                merge.Add(item);
                        if (item.TryGetItemComponent(out ItemBonusCompoent bonusSource)) bonus.Add(item);
                        if (item.TryGetItemComponent(out ItemClickSourceComponent tapSource)) tap.Add(item);
                    }
                }
        }



        public bool CheckCDItemNumEnough(int num)
        {
            return mCDItemNum >= num;
        }

        #endregion

        public void Update(float dt)
        {
            if (world == null)
                return;

            if (!mPauseBoard)
                _SyncBoard((int)(dt * 1000));

            if (mIsBgDirty)
            {
                mIsBgDirty = false;
                // 主动刷新cache
                BoardViewWrapper.ValidateOrderDisplayCache();
                // 根据cache显示底板
                view.boardBg.Refresh();
            }

            if (mIsDirty)
            {
                mIsDirty = false;
                // 合成提示
                mChecker.CheckHint(false);
                // 统计
                _CacheBoardItem();
                GuideUtility.TriggerGuide();
                MessageCenter.Get<MSG.UI_BOARD_DIRTY>().Dispatch();
            }

            if (mIsLevelDirty)
            {
                // 检查区格解锁
                mIsLevelDirty = false;
                // 由UI发起解锁
                board.TriggerLevelUnlock();
            }

            if ((Time.time > mLastActiveTime + 1f) &&
                !view.boardDrag.IsDraging &&
                mChecker.ShouldPlayMatchHintAnim())
            {
                mChecker.PlayMatchHintAnim();
            }
        }

        private void _SyncBoard(int milliSec)
        {
            world?.Update(milliSec);
        }

        private void _CalcScaleCoe()
        {
            var scale = view.transform.lossyScale.x;
            BoardUtility.SetCanvasToScreenCoe(scale);
        }

        public void OverrideScaleCoe(float scale)
        {
            BoardUtility.SetCanvasToScreenCoe(scale);
        }

        public void CalcScaleCoe()
        {
            _CalcScaleCoe();
        }

        private void _CalcSize()
        {
            var cellSize = view.cellSize;
            BoardUtility.SetCellSize(cellSize);
            view.boardRoot.sizeDelta = new Vector2(cellSize * width, cellSize * height);

            LayoutRebuilder.ForceRebuildLayoutImmediate(view.boardRoot);
        }

        public void CalcSize()
        {
            _CalcSize();
        }

        private void _CalcOrigin()
        {
            var origin = RectTransformUtility.WorldToScreenPoint(null, view.pairRoot.position);
            BoardUtility.SetOriginPos(origin);
        }

        public void CalcOrigin()
        {
            _CalcOrigin();
        }

        #region handler

        private void _OnItemStateChange(Item item, ItemStateChangeContext context)
        {
            mIsDirty = true;
            mIsBgDirty = true;
        }

        private void _OnItemEnter(Item item)
        {
            mIsDirty = true;
            mIsBgDirty = true;
        }

        private void _OnItemLeave(Item item)
        {
            mIsDirty = true;
            mIsBgDirty = true;
            if (mSelectedItemId == item.id)
            {
                var undoItem = world.undoItem;
                if (undoItem == null || undoItem.id != item.id)
                {
                    // 如果leave的不是正在卖出的物品 才进行取消选择
                    _CancelSelect();
                }
                else
                {
                    // leave的item是正在卖出的物品 仅隐藏选框
                    mSelectedItemId = -1;
                    _HideSelector();
                }
            }

            //若棋子在拖拽过程中死亡了，则走一遍_OnDragFinish，用于清理拖拽过程中可能产生的各种效果
            if (mItemInDrag != null && mItemInDrag == item)
            {
                _OnDragFinish();
            }
        }

        private void _OnItemEvent(Item item, ItemEventType eventType)
        {
            switch (eventType)
            {
                case ItemEventType.ItemBubbleUnleash:
                case ItemEventType.ItemBubbleBreak:
                    Game.Manager.audioMan.TriggerSound("BubbleBreak");
                    break;
                case ItemEventType.ItemBubbleFrozenBreak:
                    //播放冰冻棋子破碎特效
                    Game.Manager.audioMan.TriggerSound("FrozenItemBreak");
                    break;
                case ItemEventType.ItemEventSpeedUp:
                    Game.Manager.audioMan.TriggerSound("SpeedUp");
                    break;
            }
        }

        private void _OnItemMove(Item item)
        {
            mIsDirty = true;
            mIsBgDirty = true;
        }

        private void _OnItemComponentChange(Item item)
        {
            mIsBgDirty = true;
        }

        private void _OnLackOfEnergy()
        {
            BoardUtility.OnLackOfEnergy();
        }

        private void _OnFeatureClicked(Item item, FeatureEntry entry)
        {
            if (item == null) return;
            switch (entry)
            {
                case FeatureEntry.FeatureMiniBoardMulti:
                    Game.Manager.miniBoardMultiMan.TryOpenUIEnterNextRoundTips();
                    break;
            }
        }

        private void _OnChoiceBoxWaiting(Item item, List<int> choices, System.Action<int> onConfirm)
        {
            // 展示UI
            UIManager.Instance.OpenWindow(UIConfig.UIChoiceBox, item, choices, onConfirm);
        }

        private void _OnCollectBonus(MergeWorld.BonusClaimRewardData context)
        {
            var item = context.item;
            var reward = context.GrabReward();
            if (item.parent != null)
            {
                if (item.TryGetItemComponent<ItemBonusCompoent>(out var bonus) && bonus.autoUse)
                {
                    // 自动使用且需要弹窗展示
                    BoardViewWrapper.ShowModalReward(reward);
                }
                else
                {
                    var itemView = GetItemView(item.id);
                    if (itemView.IsViewIdle())
                    {
                        // 比如物体正在拖动中 则不在格子处展示反馈特效
                        view.boardEffect.ShowCollectFeedback(item.coord);
                    }
                    if (reward != null)
                    {
                        if (reward.rewardId == Constant.kMergeInfinateEnergyObjId)
                        { }
                        else
                        {
                            UIFlyUtility.FlyReward(reward, itemView.transform.position);
                        }
                    }
                }
            }
            else
            {
                if (reward != null)
                {
                    Game.Manager.rewardMan.CommitReward(reward);
                }
            }

            // track
            DataTracker.TrackMergeActionCollect(item, ItemUtility.GetItemLevel(item.tid));
        }

        private void _OnCollectTapBonus(MergeWorld.BonusClaimRewardData context)
        {
            var item = context.item;
            var reward = context.GrabReward();
            if (item.parent != null)
            {
                view.boardEffect.ShowCollectFeedback(item.coord);
                if (reward != null)
                {
                    if (reward.rewardId == Constant.kMergeInfinateEnergyObjId)
                    { }
                    else
                    {
                        //tapBonus有自己专门的target，如果没找到的话 就默认使用棋子通用的target
                        UIFlyUtility.FlyRewardSetType(reward, _CoordToWorldPos(item.coord), FlyType.TapBonus);
                    }
                }
            }
            else
            {
                if (reward != null)
                {
                    Game.Manager.rewardMan.CommitReward(reward);
                }
            }

            // track
            DataTracker.TrackMergeActionTapBonus(item.tid, reward?.rewardId ?? 0);
        }

        private Vector3 _CoordToWorldPos(Vector2Int coord)
        {
            var cellSise = BoardUtility.cellSize;
            Vector2 localPos = new Vector2(coord.x * cellSise + cellSise * 0.5f, -coord.y * cellSise - cellSise * 0.5f);
            return view.boardHolder.transform.GetChild(0).TransformPoint(localPos);
        }

        public Vector3 CoordToWorldPos(Vector2Int coord)
        {
            var cellSise = BoardUtility.cellSize;
            Vector2 localPos = new Vector2(coord.x * cellSise + cellSise * 0.5f, -coord.y * cellSise - cellSise * 0.5f);
            return view.boardHolder.transform.GetChild(0).TransformPoint(localPos);
        }

        private int _CdItemNum()
        {
            int cdItemNum = 0;
            board.WalkAllItem((it) =>
            {
                if (it.isActive)
                {
                    // 是手动类生成器 且 当前还不可用 -> 物品在冷却中
                    if (it.HasComponent(ItemComponentType.ClickSouce, true) && !ItemUtility.IsItemReadyToUse(it))
                    {
                        ++cdItemNum;
                    }
                }
            });
            return cdItemNum;
        }

        private void _OnUseTimeSkipper(Item item)
        {
            // TrackData
            DataTracker.TrackMergeActionSkillTimeSkip(item, ItemUtility.GetItemLevel(item.tid), _CdItemNum());
            view.boardEffect.UseTimeSkipper(item.coord);
        }

        private void _OnUseTimeScaleSource(Item item)
        {
            // TrackData
            DataTracker.TrackMergeActionSkillTesla(item, ItemUtility.GetItemLevel(item.tid), _CdItemNum());
        }

        private void _OnJumpCDBegin(Item source)
        {
            var sourceItem = BoardViewManager.Instance.GetItemView(source.id);
            sourceItem.RefreshJumpCdState();

            // 棋子可能正在从礼物盒拿出 效果延迟到棋子落地再处理
            float delay = 0f;
            if (!sourceItem.IsViewIdle())
            {
                delay = 0.7f;
            }

            Item item;
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; j++)
                {
                    item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        if (ItemUtility.CheckSourceCanJumpCD(item))
                        {
                            view.boardEffect.ShowJumpCDEffect(source.coord, item.coord, item, delay);
                        }
                    }
                }
            }
        }

        private void _OnJumpCDEnd()
        {
            Item item;
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; j++)
                {
                    item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        var itemView = BoardViewManager.Instance.GetItemView(item.id);
                        if (itemView != null)
                        {
                            itemView.RefreshJumpCdState();
                        }
                    }
                }
            }
        }
        
        private void _OnTokenMultiBegin(Item source)
        {
            if (!source.TryGetItemComponent<ItemTokenMultiComponent>(out var tokenMultiComp))
                return;
            var sourceItem = BoardViewManager.Instance.GetItemView(source.id);
            sourceItem.RefreshTokenMultiState();

            // 棋子可能正在从礼物盒拿出 效果延迟到棋子落地再处理
            float delay = 0f;
            if (!sourceItem.IsViewIdle())
            {
                delay = 0.7f;
            }
            //延迟播放首次生效时的背光特效
            view.boardEffect.ShowTokenMultiStartEffect(sourceItem, delay);
            Item item;
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; j++)
                {
                    item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        //检测棋盘上的棋子是否含有ActivityToken组件，且持有的tokenId可以被翻倍
                        if (ItemUtility.CheckSourceCanTokenMulti(tokenMultiComp, item))
                        {
                            view.boardEffect.ShowTokenMultiEffect(source.coord, item.coord, item, delay);
                        }
                    }
                }
            }
        }

        private void _OnTokenMultiEnd()
        {
            Item item;
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; j++)
                {
                    item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        var itemView = BoardViewManager.Instance.GetItemView(item.id);
                        if (itemView != null)
                        {
                            itemView.RefreshTokenMultiState();
                            itemView.RefreshActivityTokenState();
                        }
                    }
                }
            }
        }

        #endregion

        public void Guide_OverrideDraggableItemBehaviour(int tid = 0, int runtimeId = 0, MergeHelper.MergeAction act = MergeHelper.MergeAction.None)
        {
            mOverrideDragItemId = tid;
            mOverrideDragItemRuntimeId = runtimeId;
            mOverrideDragBehaviour = act;
        }

        private void _OnMessageFocused(float deltaSec)
        {
            _SyncBoard((int)deltaSec * 1000);
        }

        private void _OnMessageLevelChange(int oldLevel)
        {
            mIsLevelDirty = true;
        }

        private void _OnMessageOrderCompleted(List<IOrderData> changedOrders, List<IOrderData> newlyAddedOrders)
        {
            // 订单刷新可能触发在棋盘没有改变的时候，需要主动处理
            mIsBgDirty = true;
        }

        private void _OnMessageOrderDisplayChange()
        {
            mIsBgDirty = true;
        }

        private void _OnMessageFlyScore((Item item, ScoreEntity.ScoreFlyRewardData r, string prefab) data)
        {
            if (string.IsNullOrEmpty(data.prefab)) return;
            Game.Manager.audioMan.TriggerSound("PurpleBall");
            view.boardEffect.ShowScoreAnim(data.item.coord, data.prefab, data.r);
        }

        public void RegisterBonusCache(int id, int tid)
        {
            mActiveBonusCache.TryAdd(id, tid);
        }

        public void UnregisterBonusCache(int id)
        {
            mActiveBonusCache.Remove(id);
        }

        public void RegisterChestCache(int id, int tid)
        {
            mActiveChestCache.TryAdd(id, tid);
        }

        public void UnregisterChestCache(int id)
        {
            mActiveChestCache.Remove(id);
        }

        public void RegisterAutoSourceCache(int id, int tid)
        {
            mActiveAutoSourceCache.TryAdd(id, tid);
        }

        public void UnregisterAutoSourceCache(int id)
        {
            mActiveAutoSourceCache.Remove(id);
        }

        public void RegisterTapSourceCache(int id, int tid)
        {
            mActiveTapSourceCache.TryAdd(id, tid);
        }

        public void UnregisterTapSourceCache(int id)
        {
            mActiveTapSourceCache.Remove(id);
        }
    }
}