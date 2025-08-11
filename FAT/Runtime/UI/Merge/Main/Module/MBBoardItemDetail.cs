/*
 * @Author: qun.chao
 * @Date: 2023-10-26 14:09:53
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using FAT.Merge;
using fat.rawdata;
using Cysharp.Threading.Tasks;
using Cysharp.Text;
using TEXT = TMPro.TextMeshProUGUI;

namespace FAT
{
    // TODO: modulebase
    public class MBBoardItemDetail : MonoBehaviour // : UIModuleBase
    {
        enum UsageType
        {
            Undo = 0,
            SpeedUp,
            SpeedUpFree,
            Sell,
            Delete,
            ChestOpen,
            Ads,

            Max,
        }

        [System.Serializable]
        class BoostGroup
        {
            // 文字颜色是否切换为boost状态
            public bool textColorBoost { get; set; }
            public bool tip_4x_showed { get; set; }

            public Button btnSwitch;
            public GameObject root;
            public GameObject bg;
            public GameObject off;
            public GameObject on_2x;
            public GameObject on_4x;
            public GameObject tip_4x;
            public Animator animator_root;
            public Animator animator_crown;
            public float tipPopDelay = 0.85f;

            public void RefreshBoost(bool canShow, EnergyBoostState state, EnergyBoostState preState)
            {
                root.SetActive(canShow);
                if (canShow)
                {
                    off.SetActive(state == EnergyBoostState.X1);
                    on_2x.SetActive(state == EnergyBoostState.X2);
                    on_4x.SetActive(state == EnergyBoostState.X4);

                    if (state == EnergyBoostState.X1)
                    {
                        // 从4倍切换过来
                        if (preState == EnergyBoostState.X4)
                            animator_crown.SetTrigger("Hide");
                        else
                            animator_crown.SetTrigger("IdleHide");
                    }
                    else if (state == EnergyBoostState.X4)
                    {
                        // 从2倍切换过来
                        if (preState == EnergyBoostState.X2)
                            animator_crown.SetTrigger("Show");
                        else
                            animator_crown.SetTrigger("IdleShow");
                    }
                }

                // 根据能量加倍状态是否开启 设置背景图和文本显示的样式
                var isShow = canShow && EnergyBoostUtility.IsBoost((int)state);
                bg.SetActive(isShow);
                textColorBoost = isShow;

                // 关闭tip
                tip_4x.SetActive(false);
            }

            // 可以显示且没有在4倍状态下 则弹tip进行提示
            public void TryShowTip4x(EnergyBoostState state)
            {
                if (!tip_4x_showed &&
                    tip_4x != null &&
                    state != EnergyBoostState.X4 &&
                    EnergyBoostUtility.CanSwitchToState(EnergyBoostState.X4))
                {
                    tip_4x_showed = true;
                    tip_4x.SetActive(true);
                }
            }

            public async UniTaskVoid Punch()
            {
                animator_root.SetTrigger("Punch");
                await UniTask.WaitForSeconds(tipPopDelay);
                TryShowTip4x(EnergyBoostState.X1);
            }
        }

        [SerializeField] private Transform usageRoot;
        [SerializeField] private TEXT txtDesc;
        [SerializeField] private Button btnInfo;
        [SerializeField] private BoostGroup boostGroup;

        private List<MBItemUsageBase> mUsageTable = new List<MBItemUsageBase>();
        private Item mItem;
        private bool mIsUnsell;
        private int mCurUsageMask;

        private int nameColorId(bool isBoost) => isBoost ? 4 : 3;
        private int descColorId(bool isBoost) => isBoost ? 6 : 5;
        private string text_format_for_boost;
        private string text_format_for_normal;

        public void Setup()
        {
            btnInfo.WithClickScale().onClick.AddListener(_OnBtnInfo);
            boostGroup.btnSwitch.WithClickScale().onClick.AddListener(_OnBtnEnergyBoost);

            for (int i = 0; i < (int)UsageType.Max; ++i)
            {
                var item = usageRoot.Find(((UsageType)i).ToString());
                if (item != null)
                {
                    var usage = item.GetComponent<MBItemUsageBase>();
                    usage.Initialize();
                    mUsageTable.Add(usage);
                }
                else
                {
                    mUsageTable.Add(null);
                    Debug.LogErrorFormat("usage {0} not found", (UsageType)i);
                }
            }
        }

        public void InitOnPreOpen()
        {
            text_format_for_boost = null;
            text_format_for_normal = null;

            BoardViewManager.Instance.world.onSelledItemChange += _OnHandleSoldItemChange;
            BoardViewManager.Instance.world.onChestWaitStart += _OnHandleChestWaitStart;
            BoardViewManager.Instance.world.onChestWaitFinish += _OnHandleChestWaitFinish;
            BoardViewManager.Instance.world.activeBoard.onItemStateChange += _OnHandlerItemStateChange;
            BoardViewManager.Instance.world.activeBoard.onUseTimeScaleSource += _OnHandlerUseTimeScaleSource;
            MessageCenter.Get<MSG.UI_BOARD_SELECT_ITEM>().AddListener(_OnMessageBoardSelectItem);
            MessageCenter.Get<MSG.UI_ENERGY_BOOST_UNLOCK_FLY_FEEDBACK>().AddListener(_OnMessageEnergyBoostUnlockFlyFeedback);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondPass);
            
            _ClearUsage();

            var lastSelectedItem = Game.Manager.mergeBoardMan.recentActiveItem;
            if (lastSelectedItem != null && lastSelectedItem.parent == BoardViewManager.Instance.board)
            {
                // 最近选中的item有效 尝试还原选中状态
                var coord = lastSelectedItem.coord;
                BoardViewManager.Instance.RefreshInfo(coord.x, coord.y);
            }
            else
            {
                _ShowEmptyInfo();
            }

            BoardViewWrapper.SetParam(BoardViewWrapper.ParamType.CompItemInfo, this);
        }

        public void CleanupOnPostClose()
        {
            BoardViewManager.Instance.world.onSelledItemChange -= _OnHandleSoldItemChange;
            BoardViewManager.Instance.world.onChestWaitStart -= _OnHandleChestWaitStart;
            BoardViewManager.Instance.world.onChestWaitFinish -= _OnHandleChestWaitFinish;
            BoardViewManager.Instance.world.activeBoard.onItemStateChange -= _OnHandlerItemStateChange;
            BoardViewManager.Instance.world.activeBoard.onUseTimeScaleSource -= _OnHandlerUseTimeScaleSource;
            MessageCenter.Get<MSG.UI_BOARD_SELECT_ITEM>().RemoveListener(_OnMessageBoardSelectItem);
            MessageCenter.Get<MSG.UI_ENERGY_BOOST_UNLOCK_FLY_FEEDBACK>().RemoveListener(_OnMessageEnergyBoostUnlockFlyFeedback);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondPass);
            _ClearUsage();
            _ShowEmptyInfo();
        }

        #region usage

        private void Update()
        {
            var item = mItem;
            if (item == null)
            {
                if (mCurUsageMask > 0)
                    _ClearUsage();
                return;
            }

            if (!mIsUnsell && (item.isDead || item.parent == null))
            {
                // 不是卖出 且 不在棋盘
                if (mCurUsageMask > 0)
                    _ClearUsage();
                return;
            }

            var _mask = _CheckUsage();
            if (_mask != mCurUsageMask)
            {
                _ApplyUsageMask(_mask, item);
            }

            for (int i = 0; i < mUsageTable.Count; i++)
            {
                var usage = mUsageTable[i];
                if (usage != null && usage.gameObject.activeSelf)
                    usage.UpdateContent();
            }
        }

        private void _ApplyUsageMask(int mask, Item item)
        {
            mCurUsageMask = mask;
            // 最多同时显示2项
            int count = 0;
            for (int i = 0; i < (int)UsageType.Max; i++)
            {
                var usage = mUsageTable[i];
                if (usage == null)
                    continue;
                if (count < 2 && ((1 << i) & mask) != 0)
                {
                    usage.Show();
                    usage.SetData(item);
                    usage.Refresh();
                    ++count;
                }
                else
                {
                    usage.Hide();
                    usage.ClearData();
                }
            }
        }

        private void _ClearUsage()
        {
            mItem = null;
            mCurUsageMask = -1;
            foreach (var act in mUsageTable)
            {
                act?.ClearData();
                act?.Hide();
            }
        }

        private bool _AllowBubbleAds()
        {
            return BoardUtility.CanWatchBubbleAds();
        }

        private bool _AllowSell()
        {
            return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureItemSale);
        }

        private int _GetUsageFlag(UsageType act)
        {
            return 1 << ((int)act);
        }

        private int _CheckUsage()
        {
            int mask = 0;
            var item = mItem;
            var cfg = Env.Instance.GetItemMergeConfig(item.tid);
            if (cfg == null)
            {
                // Debug.LogErrorFormat("config null {0}", item.tid);
                return mask;
            }
            // 撤回
            if (mIsUnsell)
            {
                mask |= _GetUsageFlag(UsageType.Undo);
                return mask;
            }
            // 气泡
            if (item.HasComponent(ItemComponentType.Bubble))
            {
                if (_IsFreeBubble(item))
                    mask |= _GetUsageFlag(UsageType.SpeedUpFree);
                else
                {
                    mask |= _GetUsageFlag(UsageType.SpeedUp);
                    if (cfg.BubbleAd && _AllowBubbleAds())
                        mask |= _GetUsageFlag(UsageType.Ads);
                }
            }
            // 卖出/删除
            if (item.isActive &&
                _AllowSell() &&
                Env.Instance.GetMergeLevel() >= cfg.SellPlayerLv &&
                !ItemUtility.IsInBubble(item))
            {
                if (cfg.SellNum > 0)
                    mask |= _GetUsageFlag(UsageType.Sell);
                else
                    mask |= _GetUsageFlag(UsageType.Delete);
            }
            // 非气泡常规加速
            if (!item.HasComponent(ItemComponentType.Bubble) && ItemUtility.TryGetItemSpeedUpInfo(item, out var _, out var _, out var _))
            {
                mask |= _GetUsageFlag(UsageType.SpeedUp);
            }
            // 宝箱解锁
            if (item.TryGetItemComponent(out ItemChestComponent comChest))
            {
                if (!comChest.canUse)
                {
                    var world = Game.Manager.mergeBoardMan.activeWorld;
                    if (world.currentWaitChest <= 0)
                    {
                        mask |= _GetUsageFlag(UsageType.ChestOpen);
                    }
                }
            }
            return mask;
        }

        private bool _IsFreeBubble(Item item)
        {
            if (ItemUtility.TryGetItemSpeedUpInfo(item, out var _, out var op, out var _))
            {
                return op == ItemUtility.ItemSpeedUpType.FreeBubble;
            }
            return false;
         }

        #endregion

        private void _UpdateItem(Item item)
        {
            mItem = item;
            mCurUsageMask = -1;
            _RefreshItem(item);
        }

        private void _RefreshItem(Item item)
        {
            if (mItem == null || mItem.id != item.id)
                return;
            if (item.isLocked)
            {
                _ShowLockItem(item);
            }
            else if (item.isFrozen)
            {
                _ShowNormalInfo(item);
            }
            else
            {
                _ShowNormalInfo(item);
            }
        }

        private void _SetSelectedItem(Item item)
        {
            BoardViewManager.Instance.SetCurrentBoardInfoItem(item);
            Game.Manager.mergeBoardMan.SetCurrentInteractingItem(item);
        }

        private void _SetNameAndDesc(string _name, string _desc)
        {
            string BuildTextFormat(bool boost)
            {
                var config1 = FontMaterialRes.Instance.GetFontMatResConf(nameColorId(boost));
                var config2 = FontMaterialRes.Instance.GetFontMatResConf(descColorId(boost));
                var col1 = ColorUtility.ToHtmlStringRGB(config1.color);
                var col2 = ColorUtility.ToHtmlStringRGB(config2.color);
                return $"<color=#{col1}>{{0}}: </color><color=#{col2}>{{1}}</color>";
            }

            if (string.IsNullOrEmpty(_name))
            {
                txtDesc.text = string.Empty;
            }
            else
            {
                if (boostGroup.textColorBoost)
                {
                    text_format_for_boost ??= BuildTextFormat(true);
                    txtDesc.SetTextFormat(text_format_for_boost, _name, _desc);
                }
                else
                {
                    text_format_for_normal ??= BuildTextFormat(false);
                    txtDesc.SetTextFormat(text_format_for_normal, _name, _desc);
                }
            }
        }

        private void _SetInfoBtn(Item item)
        {
            var _canShowDetail = item == null ? false : UIUtility.CanPopupMergeItemDetail(item);
            btnInfo.gameObject.SetActive(_canShowDetail);
        }

        private void _ShowEmptyInfo()
        {
            _SetSelectedItem(null);
            _ShowEnergyBoostInfo(false);
            _SetNameAndDesc(string.Empty, string.Empty);
            _SetInfoBtn(null);
        }

        private void _ShowLockItem(Item item)
        {
            _SetSelectedItem(null);
            _ShowEnergyBoostInfo(false);
            _SetNameAndDesc(string.Empty, ItemUtility.GetBoardItemInfo(item));
            _SetInfoBtn(null);
        }

        private void _ShowNormalInfo(Item item)
        {
            _SetSelectedItem(item);
            _ShowEnergyBoostInfo(true);
            _SetNameAndDesc(ItemUtility.GetItemRuntimeShortName(item), ItemUtility.GetBoardItemInfo(item));
            _SetInfoBtn(item);

            // 选中某item时触发guide
            GuideUtility.TriggerGuide();
        }

        private void _ShowUndoInfo(Item item)
        {
            _SetSelectedItem(null);
            _ShowEnergyBoostInfo(false);
            var sold = ItemUtility.GetSellReward(item.tid);
            if (sold.num > 0)
            {
                if (sold.id == Constant.kMergeEnergyObjId)
                {
                    _SetNameAndDesc(ItemUtility.GetItemRuntimeShortName(item), I18N.FormatText("#SysComDesc78", $"{sold.num}{TextSprite.Energy}"));
                }
                else
                {
                    _SetNameAndDesc(ItemUtility.GetItemRuntimeShortName(item), I18N.FormatText("#SysComDesc78", $"{sold.num}{TextSprite.Coin}"));
                }
            }
            else
            {
                // undelete
                _SetNameAndDesc(ItemUtility.GetItemRuntimeShortName(item), I18N.Text("#SysComDesc156"));
            }
            _SetInfoBtn(item);
        }

        private void _ShowEnergyBoostInfo(bool tryShow)
        {
            var canShow = tryShow && _CheckCanShowEnergyBoost();
            var state = Env.Instance.GetEnergyBoostState();
            boostGroup.RefreshBoost(canShow, state, state);

            if (canShow)
            {
                boostGroup.TryShowTip4x(state);
            }
        }
        
        private bool _CheckCanShowEnergyBoost()
        {
            bool canShow = false;
            if (mItem != null)
            {
                var clickSourceConfig = Env.Instance.GetItemComConfig(mItem.tid)?.clickSourceConfig; 
                canShow = clickSourceConfig != null && clickSourceConfig.IsBoostable && Env.Instance.IsFeatureEnable(MergeFeatureType.EnergyBoost);
            }
            return canShow;
        }

        private void _OnBtnInfo()
        {
            if (mItem != null)
            {
                UIItemUtility.ShowItemTipsInfo(mItem.tid);
            }
        }

        private void _OnSecondPass()
        {
            if (mItem != null)
            {
                var cur = BoardViewManager.Instance.world?.currentWaitChest ?? 0;
                if (cur == mItem.id)
                    _RefreshItem(mItem);
            }
        }

        private void _OnMessageBoardSelectItem(Item item)
        {
            if (item != null)
            {
                // 相同item不重复触发选中
                if (mItem == null || mItem.id != item.id)
                {
                    _UpdateItem(item);
                    // // 尝试播放音效
                    // _TryPlaySelectSnd(item);
                }
            }
            else
            {
                if (BoardViewManager.Instance.world.undoItem == null)
                {
                    _ClearUsage();
                    _ShowEmptyInfo();
                }
            }
        }

        private void _OnHandleSoldItemChange(Item item)
        {
            mIsUnsell = item != null;
            if (mIsUnsell)
            {
                mItem = item;
                _ShowUndoInfo(item);
            }
        }

        private void _OnHandleChestWaitStart(Item item)
        {
            _RefreshItem(item);
        }

        private void _OnHandleChestWaitFinish(Item item)
        {
            _RefreshItem(item);
        }

        private void _OnHandlerItemStateChange(Item item, ItemStateChangeContext context)
        {
            _RefreshItem(item);
        }

        private void _OnHandlerUseTimeScaleSource(Item item)
        {
            _RefreshItem(item);
        }

        private void _OnBtnEnergyBoost()
        {
            var preState = Env.Instance.GetEnergyBoostState();
            //发起切换后item不再选中 显示的按钮和文案都和item无关
            Env.Instance.SwitchEnergyBoostState();

            //界面中取消对当前棋子的引用
            _ClearUsage();
            _SetInfoBtn(null);
            //棋盘上也取消选中当前棋子
            BoardViewManager.Instance.CancelSelectCurItem();

            //刷能量按钮
            boostGroup.RefreshBoost(true, Env.Instance.GetEnergyBoostState(), preState);

            //刷文本提示
            var (name_key, desc_key) = EnergyBoostUtility.GetBoardDetailKeyForBoostState();
            _SetNameAndDesc(I18N.Text(name_key), I18N.Text(desc_key));

            if (Env.Instance.IsInEnergyBoost())
            {
                UIManager.Instance.OpenWindow(UIConfig.UIEnergyBoostTips, true);
                // 能量boost模式开启音效
                Game.Manager.audioMan.TriggerSound("EnergyBoost");
                Game.Manager.audioMan.TriggerSound("LightningBig");
            }
            else
            {
                UIManager.Instance.OpenWindow(UIConfig.UIEnergyBoostTips, false);
            }
        }

        private void _OnMessageEnergyBoostUnlockFlyFeedback()
        {
            boostGroup?.Punch();
        }
    }
}