/*
 * @Author: qun.chao
 * @Date: 2021-02-19 14:25:42
 */
namespace FAT.Merge
{
    using System.Collections.Generic;
    using UnityEngine;
    using FAT;
    using EL;

    public class ItemInteractContext
    {
        public MBItemView src;
        public MBItemView dst;
    }

    public class MBItemView : MonoBehaviour
    {
        [SerializeField] private UIImageRes iconRes;
        [SerializeField] private MBItemContent contentCtrl;
        [SerializeField] private MBItemCharge chargeCtrl;
        [SerializeField] private MBItemAnimation animCtrl;
        [SerializeField] private MBItemEffect effectCtrl;
        [SerializeField] private MBItemIndicator indicatorCtrl;
        [SerializeField] private MBItemActivityToken activityTokenCtrl;

        public ItemLifecycle currentStateType { get; private set; }
        public Item data { get; private set; }
        public ItemSpawnContext spawnContext { get; private set; }
        public ItemInteractContext interactContext { get; private set; }
        public ItemStateChangeContext stateChangeContext { get; private set; }
        public Transform tapCostComp => chargeCtrl.tapCostComp;

        public bool isInBox => contentCtrl.isInBox;
        public bool hasNewTip => contentCtrl.hasNewTip;
        public bool hasFlag => indicatorCtrl.HasFlag;

        private Dictionary<ItemLifecycle, MergeItemBaseState> mStates = new Dictionary<ItemLifecycle, MergeItemBaseState>();

        private string debugName = null;

#if UNITY_EDITOR
        void Awake()
        {
            MBDebug.SetDebugAction(gameObject, "SpawnBubble", () =>
            {
                if (data == null)
                {
                    return;
                }
                var method = typeof(Merge.Board).GetMethod("_CheckSpawnBubble", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method.Invoke(data.parent, new object[] { data, false });
            });
        }
#endif

        #region combo
        private int combo = 0;
        public int Combo()
        {
            ++combo;
            return combo;
        }
        #endregion

        public void SetData(Item data)
        {
            this.data = data;

            combo = 0;

            contentCtrl.SetData(this);
            chargeCtrl.SetData(this);
            effectCtrl.SetData(this);
            animCtrl.SetData(this);
            indicatorCtrl.SetData(this);
            activityTokenCtrl.SetData(this);

            currentStateType = ItemLifecycle.None;

            TryApplyFilter();
        }

        public void ClearData()
        {
            if (currentStateType != ItemLifecycle.None)
                mStates[currentStateType].OnLeave();
            currentStateType = ItemLifecycle.None;

            contentCtrl.ClearData();
            chargeCtrl.ClearData();
            effectCtrl.ClearData();
            animCtrl.ClearData();
            indicatorCtrl.ClearData();
            activityTokenCtrl.ClearData();

            this.data = null;
            debugName = null;

            RemoveFilter();

            interactContext = null;
            stateChangeContext = null;
            spawnContext = null;
        }

        public void RefreshOnComponentChange()
        {
            contentCtrl.ClearData();
            chargeCtrl.ClearData();
            effectCtrl.ClearData();
            animCtrl.ClearData();
            indicatorCtrl.ClearData();
            activityTokenCtrl.ClearData();

            contentCtrl.SetData(this);
            chargeCtrl.SetData(this);
            effectCtrl.SetData(this);
            animCtrl.SetData(this);
            indicatorCtrl.SetData(this);
            activityTokenCtrl.SetData(this);

            TryApplyFilter();
        }

        public void RefreshJumpCdState()
        {
            effectCtrl.TryRefreshJumpCDState();
        }
        
        public void RefreshTokenMultiState()
        {
            effectCtrl.TryRefreshTokenMultiState();
        }
        
        public void AddTokenMultiEffect()
        {
            effectCtrl.AddTokenMultiEffect();
        }

        public void RefreshActivityTokenState()
        {
            activityTokenCtrl.RefreshActivityTokenState();
        }
        
        public MBItemActivityToken GetActivityTokenCtrl()
        {
            return activityTokenCtrl;
        }

        public bool IsDragging()
        {
            return currentStateType == ItemLifecycle.Drag;
        }

        public bool IsViewIdle()
        {
            return currentStateType == ItemLifecycle.Idle;
        }

        public bool IsViewDraggable()
        {
            return IsViewIdle() || currentStateType == ItemLifecycle.SpawnMerge;
        }

        public bool IsViewCantSwap()
        {
            return currentStateType == ItemLifecycle.SpawnWait;
        }

        private void LateUpdate()
        {
            if (data == null)
                return;
            _UpdateLifecycle();

            animCtrl.UpdateEx();
            effectCtrl.UpdateEx();
            indicatorCtrl.UpdateEx();

            if (data.isDead || currentStateType == ItemLifecycle.DelayUnlock)
            {
                return;
            }

            // 计时器相关组件不能在dead或DelayUnlock状态后后调用
            chargeCtrl.UpdateEx();
        }

        #region effect & anim 

        public void TryResolveNewItemTip()
        {
            if (contentCtrl.hasNewTip)
                contentCtrl.ResolveNewItemTip();
        }

        public void PlayTap()
        {
            animCtrl.PlayTap();
        }

        public void PlaySpawn()
        {
            animCtrl.PlaySpawn();
        }

        public void PlayDropToGround()
        {
            animCtrl.PlayDropToGround();
        }

        public void AddHintForConsumeEnergy()
        {
            effectCtrl.AddHintForConsumeEnergy();
        }

        public void RemoveHintForConsumeEnergy()
        {
            effectCtrl.RemoveHintForConsumeEnergy();
        }

        public void AddHintForConsumeBoostEnergy()
        {
            effectCtrl.AddHintForConsumeBoostEnergy();
        }

        public void RemoveHintForConsumeBoostEnergy()
        {
            effectCtrl.RemoveHintForConsumeBoostEnergy();
        }

        public void AddHintForLightbulb()
        {
            effectCtrl.AddHintForLightbulb();
        }

        public void RemoveHintForLightbulb()
        {
            effectCtrl.RemoveHintForLightbulb();
        }

        public void AddHintForReadyToUse()
        {
            animCtrl.AddHintType(ItemHintState.ReadyToUse);
            effectCtrl.AddReadyToUseEffect();
        }

        public void RemoveHintForReadyToUse()
        {
            animCtrl.RemoveHintType(ItemHintState.ReadyToUse);
            effectCtrl.RemoveReadyToUseEffect();
        }

        public void AddOnBoardEffect()
        {
            effectCtrl.AddOnBoardEffect();
        }

        public void AddOnBoardEffect4X()
        {
            effectCtrl.AddOnBoardEffect4X();
        }

        public void AddOnBoardEffectForBubble()
        {
            effectCtrl.AddOnBoardEffectForBubble();
        }

        public void AddOutOfInventoryEffect()
        {
            effectCtrl.AddOutOfInventoryEffect();
        }

        // public void AddHintForMatch()
        // {
        //     animCtrl.AddHintType(ItemHintState.Match);
        // }

        // public void RemoveHintForMatch()
        // {
        //     animCtrl.RemoveHintType(ItemHintState.Match);
        // }

        #endregion

        #region indicator

        public void SetSelect()
        {
            indicatorCtrl.OnSelect();
        }

        public void SetDeselect()
        {
            indicatorCtrl.OnDeselect();
        }

        public void RefreshChestTip()
        {
            indicatorCtrl.TryRefreshChestTip();
            effectCtrl.TryRefreshOpenChestTip();
        }

        public void SetOrderTipDirty()
        {
            effectCtrl.SetOrderTipDirty();
        }

        public void RefreshActivityIndicator()
        {
            indicatorCtrl.RefreshActivityIndicator();
        }

        #endregion

        #region state

        public void SetBorn()
        {
            _ChangeState(ItemLifecycle.Born);
        }

        public void SetDrag()
        {
            _ChangeState(ItemLifecycle.Drag);
        }

        public void SetMixOutput()
        {
            _ChangeState(ItemLifecycle.MixOutput);
        }

        public void SetIdle()
        {
            _ChangeState(ItemLifecycle.Idle);
        }

        public void SetMove()
        {
            // move不被自身打断 | 已经消亡的不用移动 ｜ 已经在别处处理的不用移动
            if (currentStateType == ItemLifecycle.Move || currentStateType == ItemLifecycle.Die || currentStateType == ItemLifecycle.None)
                return;
            _ChangeState(ItemLifecycle.Move);
        }

        public void SetRewardListPop()
        {
            _ChangeState(ItemLifecycle.SpawnReward);
            contentCtrl.SetBornFromRewardList();
        }

        public void SetSpawnFromInventory()
        {
            _ChangeState(ItemLifecycle.SpawnReward);
        }

        public void ResolveSpawnWait()
        {
            if (currentStateType == ItemLifecycle.SpawnWait)
            {
                _ChangeState(ItemLifecycle.Spawn);
            }
        }

        public void SetSpawn(ItemSpawnContext context)
        {
            spawnContext = context;

            if (context.type == ItemSpawnContext.SpawnType.Eat)
            {
                _ChangeState(ItemLifecycle.Reset);
            }
            else
            {
                if (context.spawner != null)
                {
                    _ChangeState(ItemLifecycle.SpawnPop);
                }
                else
                {
                    if (context.type == ItemSpawnContext.SpawnType.MagicHour)
                    {
                        _ChangeState(ItemLifecycle.SpawnWait);
                    }
                    else if (context.type == ItemSpawnContext.SpawnType.OrderLike ||
                             context.type == ItemSpawnContext.SpawnType.Fishing ||
                             context.type == ItemSpawnContext.SpawnType.OrderRate ||
                             context.type == ItemSpawnContext.SpawnType.Farm ||
                             context.type == ItemSpawnContext.SpawnType.Fight ||
                             context.type == ItemSpawnContext.SpawnType.WishBoard ||
                             context.type == ItemSpawnContext.SpawnType.MineCart)
                    {
                        _ChangeState(ItemLifecycle.SpawnReward);
                    }
                    else
                    {
                        _ChangeState(ItemLifecycle.Spawn);
                    }
                }
            }
        }

        public void SetDelayUnlock(ItemStateChangeContext context)
        {
            if (context == null || context.reason != ItemStateChangeContext.ChangeReason.TrigAutoSourceDead)
                return;
            stateChangeContext = context;
            _ChangeState(ItemLifecycle.DelayUnlock);
        }

        public void SetMerge(ItemInteractContext context)
        {
            interactContext = context;
            _ChangeState(ItemLifecycle.SpawnMerge);
        }

        public void SetConsume(ItemInteractContext context)
        {
            interactContext = context;
            _ChangeState(ItemLifecycle.Consume);
        }

        public void SetDead()
        {
            if (currentStateType == ItemLifecycle.Die)
                return;
            _ChangeState(ItemLifecycle.Die);
        }

        public void SetEmpty()
        {
            if (currentStateType == ItemLifecycle.None)
                return;
            mStates[currentStateType].OnLeave();
            currentStateType = ItemLifecycle.None;
        }

        // // 重刷资源
        // public void SetResStateChange(Item item)
        // {
        //     contentCtrl.RefreshRes(item);
        // }

        public void SetResAction(System.Action<GameObject> act)
        {
            contentCtrl.SetResAction(act);
        }

        public void SetFeedStateChange()
        {
            indicatorCtrl.TryRefreshFeedProgress();
        }

        //设置移到奖励箱状态
        public void SetMoveToRewardBox()
        {
            _ChangeState(ItemLifecycle.MoveToRewardBox);
        }

        // 礼物队列现在使用完整item做显示 非图片
        // 礼物队列的物品没有parent
        public void TryApplyFilter()
        {
            if (!BoardViewManager.Instance.IsFiltering || data.parent == null)
                return;
            if (BoardViewManager.Instance.usable_check_filter(data))
            {
                contentCtrl.ApplyFilter(true);
                effectCtrl.TryAddFilterEffect();
            }
            else
            {
                if (BoardViewManager.Instance.ignore_filter(data))
                {
                    contentCtrl.ApplyFilter(true);
                }
                else
                {
                    contentCtrl.ApplyFilter(false);
                }
            }
        }

        public void RemoveFilter()
        {
            contentCtrl.RemoveFilter();
            effectCtrl.RemoveFilterEffect();
        }

        private void _UpdateLifecycle()
        {
            if (currentStateType == ItemLifecycle.None)
                return;

            var st = mStates[currentStateType].Update(Time.deltaTime);
            if (st != ItemLifecycle.None)
            {
                _ChangeState(st);
            }
        }

        private void _ChangeState(ItemLifecycle type)
        {
            if (currentStateType != ItemLifecycle.None)
                mStates[currentStateType].OnLeave();
            _EnsureState(type);
            currentStateType = type;
            mStates[type].OnEnter();
            BoardViewManager.Instance.OnItemChangeState(this.data, type);
        }

        private void _EnsureState(ItemLifecycle type)
        {
            if (!mStates.ContainsKey(type))
            {
                mStates.Add(type, _CreateState(type));
            }
        }

        private MergeItemBaseState _CreateState(ItemLifecycle type)
        {
            MergeItemBaseState state = null;
            switch (type)
            {
                case ItemLifecycle.Born:
                    state = new MergeItemBornState(this);
                    break;
                case ItemLifecycle.Drag:
                    state = new MergeItemDragState(this);
                    break;
                case ItemLifecycle.Idle:
                    state = new MergeItemIdleState(this);
                    break;
                case ItemLifecycle.Reset:
                    state = new ResetState(this);
                    break;
                case ItemLifecycle.Move:
                    state = new MergeItemMoveState(this);
                    break;
                case ItemLifecycle.Consume:
                    state = new MergeItemConsumeState(this);
                    break;
                case ItemLifecycle.SpawnWait:
                    state = new MergeItemSpawnWaitState(this);
                    break;
                case ItemLifecycle.Spawn:
                    state = new MergeItemSpawnState(this);
                    break;
                case ItemLifecycle.SpawnPop:
                    state = new MergeItemSpawnPopStateEx(this);
                    break;
                case ItemLifecycle.SpawnMerge:
                    state = new MergeItemMergeState(this);
                    break;
                case ItemLifecycle.SpawnReward:
                    state = new MergeItemSpawnRewardState(this);
                    break;
                case ItemLifecycle.Die:
                    state = new MergeItemDieState(this);
                    break;
                case ItemLifecycle.MixOutput:
                    state = new MergeItemMixOutputState(this);
                    break;
                case ItemLifecycle.DelayUnlock:
                    state = new DelayUnlockState(this);
                    break;
                case ItemLifecycle.MoveToRewardBox:
                    state = new MoveToRewardBoxState(this);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
            return state;
        }

        #endregion

        #region debug

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (BoardUtility.debugShow && data != null)
            {
                UnityEditor.Handles.color = Color.red;
                if (debugName == null)
                    // debugName = $"{data.id}_{ItemUtility.GetItemShortName(data.tid)}";
                    // debugName = $"{data.tid}_{ItemUtility.GetItemShortName(data.tid)}";
                    debugName = $"{data.tid}";
                UnityEditor.Handles.Label(transform.position, debugName);
            }
#endif
        }

        #endregion

        public MBResHolderBase GetResHolder()
        {
            return contentCtrl.Holder.ResHolder;
        }

        public void TweenSetAlpha(float a)
        {
            contentCtrl.TweenSetAlpha(a);
        }

        public float GetCurIconAlpha()
        {
            return contentCtrl.CurAlpha;
        }
    }
}