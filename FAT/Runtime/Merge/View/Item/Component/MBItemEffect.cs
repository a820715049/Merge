/*
 * @Author: qun.chao
 * @Date: 2021-02-24 14:27:00
 */
namespace FAT
{
    using System.Collections.Generic;
    using UnityEngine;
    using Merge;
    using fat.rawdata;

    public enum ItemEffectType
    {
        [Tooltip("状态 可消耗能量")]
        Energy,
        [Tooltip("状态 可产出")]
        Spawnable,
        [Tooltip("状态 顶级棋子")]
        TopLevel,
        [Tooltip("瞬时 登场表现")]
        OnBoard,
        [Tooltip("瞬时 合成表现")]
        OnMerge,
        [Tooltip("瞬时 收集表现")]
        OnCollect,
        [Tooltip("瞬时 解锁格子")]
        UnlockNormal,
        [Tooltip("瞬时 解锁格子 带等级")]
        UnlockLevel,
        [Tooltip("瞬时 解锁网")]
        UnFrozen,
        [Tooltip("状态 订单礼盒拖尾")]
        OrderBoxTrail,
        [Tooltip("瞬时 订单礼盒从棋盘上消耗")]
        OrderBoxOpen,
        [Tooltip("状态 可加倍消耗能量")]
        BoostEnergy,
        [Tooltip("瞬时 棋子飞到订单时爆炸")]
        OrderItemConsumed,
        [Tooltip("瞬时 点击沙滩格")]
        TapLocked,
        [Tooltip("瞬时 跳过冷却 冷却消失效果")]
        JumpCDDisappear,
        [Tooltip("瞬时 跳过冷却 飞行轨迹")]
        JumpCDTrail,
        [Tooltip("状态 跳过冷却背光效果")]
        JumpCDBg,
        [Tooltip("状态 订单可提交")]
        OrderCanFinish,
        [Tooltip("瞬时 4倍体力Max产出")]
        EnergyBoostBg4X,
        [Tooltip("状态 星想事成拖尾")]
        MagicHourTrail,
        [Tooltip("瞬时 星想事成反馈")]
        MagicHourHit,
        [Tooltip("瞬时 时间加速器生效")]
        TimeSkip,
        [Tooltip("瞬时 触发式产棋子组件特效")]
        TrigAutoSource,
        [Tooltip("状态 灯泡特殊道具")]
        Lightbulb,
        [Tooltip("瞬时 冰冻棋子合成及消失时要播的特效")]
        FrozenItem,

        //以下部分实际没用到
        TeslaSource,
        TeslaBuff,
        Filter_Scissor,
        Filter_Feed,
        SpeedUp_Tip,
    }

    public class MBItemEffect : MonoBehaviour
    {
        [SerializeField] private Transform backRoot;
        [SerializeField] private Transform frontRoot;
        [SerializeField] private Transform filterRoot;

        private MBItemView mView;
        private Dictionary<ItemEffectType, GameObject> mEffectDict = new Dictionary<ItemEffectType, GameObject>();
        private bool mIsShowTeslaBuff;
        private bool mCanAffectedByTimeScale;
        private bool mIsOrderFlagDirty;

        public void SetData(MBItemView view)
        {
            mView = view;

            mIsShowTeslaBuff = false;
            mCanAffectedByTimeScale = false;
            mIsOrderFlagDirty = false;

            // 未激活的item不显示奖励等效果
            if (!view.data.isActive)
                return;

            if (view.data.config?.IsTopEffect == true)
            {
                AddTopLevelEffect();
            }

            if (view.data.TryGetItemComponent(out ItemSkillComponent skill))
            {
                if (skill.teslaActive)
                    _TryRefreshTimeScaleSourceEffect();
            }
            else
            {
                TryRefreshOpenChestTip();
                TryRefreshJumpCDState();
            }

            // 初始化时判断一次是否可用特斯拉
            _RefreshTimeScaleBuffUsability();
        }

        public void ClearData()
        {
            mView = null;
            _ClearAllEffect();
            frontRoot.localScale = Vector3.one;
        }

        public void UpdateEx()
        {
            if (!mView.data.isActive)
                return;

            if (mIsShowTeslaBuff != _ShouldShowTeslaBuff())
            {
                _TryRefreshTimeScaleBuffEffect();
                mIsShowTeslaBuff = mEffectDict.ContainsKey(ItemEffectType.TeslaBuff);
            }

            if (mIsOrderFlagDirty)
            {
                mIsOrderFlagDirty = false;
                TryRefreshOrderTip();
            }
        }

        public void TryAddFilterEffect()
        {
            if (!BoardViewManager.Instance.IsFiltering)
                return;
            if (mView.data.TryGetItemComponent<ItemClickSourceComponent>(out var click))
            {
                if (click.config.IsBoostItem)
                {
                    return;
                }
            }
            _ApplyFilter();
        }

        public void RemoveFilterEffect()
        {
            _RemoveFilter();
        }

        private void AddHintForOrderCanFinish()
        {
            _AddEffect(ItemEffectType.OrderCanFinish);
        }

        private void RemoveHintForOrderCanFinish()
        {
            _RemoveEffect(ItemEffectType.OrderCanFinish);
        }

        public void AddHintForConsumeEnergy()
        {
            _AddEffect(ItemEffectType.Energy);
        }

        public void RemoveHintForConsumeEnergy()
        {
            _RemoveEffect(ItemEffectType.Energy);
        }
        
        public void AddHintForConsumeBoostEnergy()
        {
            _AddEffect(ItemEffectType.BoostEnergy);
        }

        public void RemoveHintForConsumeBoostEnergy()
        {
            _RemoveEffect(ItemEffectType.BoostEnergy);
        }

        public void AddHintForLightbulb()
        {
            _AddEffect(ItemEffectType.Lightbulb);
        }

        public void RemoveHintForLightbulb()
        {
            _RemoveEffect(ItemEffectType.Lightbulb);
        }

        public void AddReadyToUseEffect()
        {
            _AddEffect(ItemEffectType.Spawnable);
        }

        public void RemoveReadyToUseEffect()
        {
            _RemoveEffect(ItemEffectType.Spawnable);
        }

        public void AddTopLevelEffect()
        {
            _AddEffect(ItemEffectType.TopLevel);
        }

        public void TryRefreshJumpCDState()
        {
            if (BoardViewManager.Instance.world.jumpCD.hasActiveJumpCD)
            {
                if ((mView.data.TryGetItemComponent(out ItemClickSourceComponent click) && click.config.IsJumpable) ||
                    (mView.data.TryGetItemComponent(out ItemJumpCDComponent jump) && jump.item.id == BoardViewManager.Instance.world.jumpCD.activeJumpCDId))
                {
                    _AddEffect(ItemEffectType.JumpCDBg);
                }
                else
                {
                    _RemoveEffect(ItemEffectType.JumpCDBg);
                }
            }
            else
            {
                _RemoveEffect(ItemEffectType.JumpCDBg);
            }
        }

        public void TryRefreshOpenChestTip()
        {
            if (!mView.data.TryGetItemComponent<ItemChestComponent>(out var chestComp)) return;
            
            if (chestComp.canUse)
            {
                AddReadyToUseEffect();
            }
            if (chestComp.isWaiting)
            {
                _SetTimeScaleBuffUsable(true);
            }
            else
            {
                _SetTimeScaleBuffUsable(false);
            }
        }

        public void SetOrderTipDirty()
        {
            mIsOrderFlagDirty = true;
        }

        public void TryRefreshOrderTip()
        {
            if (!UIUtility.ABTest_OrderItemChecker())
                return;
            var state = BoardViewWrapper.GetItemRequireState(mView.data);
            if (state == 1 && mView.IsViewDraggable())
            {
                AddHintForOrderCanFinish();
            }
            else
            {
                RemoveHintForOrderCanFinish();
            }
        }

        private void _RefreshTimeScaleBuffUsability()
        {
            var item = mView.data;
            if (item.HasComponent(ItemComponentType.ClickSouce) ||
                item.HasComponent(ItemComponentType.AutoSouce) ||
                item.HasComponent(ItemComponentType.Dying) ||
                item.HasComponent(ItemComponentType.EatSource) ||

                // // 正在吃的组件
                // (item.TryGetItemComponent(out ItemEatSourceComponent eat) && eat.eatLeftMilli > 0) ||

                // 正在等待开启的宝箱
                (item.TryGetItemComponent(out ItemChestComponent chest) && chest.isWaiting) ||
                // bubble
                item.HasComponent(ItemComponentType.Bubble))
            {
                _SetTimeScaleBuffUsable(true);
            }
            else
            {
                _SetTimeScaleBuffUsable(false);
            }
        }

        private void _SetTimeScaleBuffUsable(bool b)
        {
            mCanAffectedByTimeScale = b;
        }

        // 特斯拉发生器
        private void _TryRefreshTimeScaleSourceEffect()
        {
            _AddEffect(ItemEffectType.TeslaSource);
        }

        // 受特斯拉影响的物品
        private void _TryRefreshTimeScaleBuffEffect()
        {
            if (_ShouldShowTeslaBuff())
            {
                _AddEffect(ItemEffectType.TeslaBuff);
            }
            else
            {
                _RemoveEffect(ItemEffectType.TeslaBuff);
            }
        }

        private bool _ShouldShowTeslaBuff()
        {
            return mView.data.timeScale != 1 && mCanAffectedByTimeScale;
        }

        #region once effect

        public void AddOnBoardEffect(float duration = 1f)
        {
            PoolItemType type;
            type = BoardUtility.EffTypeToPoolType(ItemEffectType.OnBoard);
            var go = GameObjectPoolManager.Instance.CreateObject(type, backRoot);
            go.transform.localPosition = Vector3.zero;
            BoardUtility.AddAutoReleaseComponent(go, duration, type);
        }

        public void AddOnBoardEffect4X(float duration = 1f)
        {
            PoolItemType type;
            type = BoardUtility.EffTypeToPoolType(ItemEffectType.EnergyBoostBg4X);
            var go = GameObjectPoolManager.Instance.CreateObject(type, backRoot);
            go.transform.localPosition = Vector3.zero;
            BoardUtility.AddAutoReleaseComponent(go, duration, type);
        }

        public void AddOnBoardEffectForBubble()
        {
            AddOnBoardEffect();
        }

        public void AddOutOfInventoryEffect()
        {
            AddOnBoardEffect(3f);
        }

        #endregion

        private bool _IsTapGuideFinished()
        {
            return false;
            // return Game.Instance.schoolMan.IsTaskCompleted(3);
        }

        private void _AddEffect(ItemEffectType type)
        {
            if (mEffectDict.ContainsKey(type))
                return;
            var go = _CreateEffect(type);
            switch (type)
            {
                case ItemEffectType.Energy:
                case ItemEffectType.Spawnable:
                case ItemEffectType.TopLevel:
                case ItemEffectType.TeslaSource:
                case ItemEffectType.TeslaBuff:
                case ItemEffectType.Lightbulb:
                    go.transform.SetParent(frontRoot);
                    break;
                case ItemEffectType.BoostEnergy:
                    go.transform.SetParent(frontRoot);
                    break;
                case ItemEffectType.OrderCanFinish:
                    go.transform.SetParent(frontRoot);
                    // 延迟一帧动画才能正确播放 或者手动处理一下第一帧的缩放
                    go.transform.GetChild(0).localScale = Vector3.zero;
                    break;
                case ItemEffectType.TimeSkip:
                case ItemEffectType.Filter_Feed:
                    go.transform.SetParent(backRoot);
                    break;
                case ItemEffectType.Filter_Scissor:
                    go.transform.SetParent(filterRoot);
                    break;
                default:
                    go.transform.SetParent(backRoot);
                    break;
            }
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            mEffectDict.Add(type, go);
        }

        private void _RemoveEffect(ItemEffectType type)
        {
            if (!mEffectDict.ContainsKey(type))
                return;
            _ReleaseEffect(type, mEffectDict[type]);
            mEffectDict.Remove(type);
        }

        private GameObject _CreateEffect(ItemEffectType type)
        {
            return GameObjectPoolManager.Instance.CreateObject(BoardUtility.EffTypeToPoolType(type));
        }

        private void _ReleaseEffect(ItemEffectType type, GameObject obj)
        {
            GameObjectPoolManager.Instance.ReleaseObject(BoardUtility.EffTypeToPoolType(type), obj);
        }

        private void _ClearAllEffect()
        {
            foreach (var eff in mEffectDict)
            {
                _ReleaseEffect(eff.Key, eff.Value);
            }
            mEffectDict.Clear();

            BoardUtility.ReleaseAutoPoolItemFromChildren(backRoot);
            BoardUtility.ReleaseAutoPoolItemFromChildren(frontRoot);
        }

        private void _ApplyFilter()
        {
            frontRoot.localScale = Vector3.zero;
        }

        private void _RemoveFilter()
        {
            frontRoot.localScale = Vector3.one;
        }
    }
}