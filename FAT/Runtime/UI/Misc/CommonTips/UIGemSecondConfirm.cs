/*
 * @Author: ange.shentu
 * @Description: 大额钻石二次确认框UI
 * @Date: 2025-07-07 16:10:17
 */
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;
using System;
using FAT.Merge;
using fat.rawdata;

namespace FAT
{
    public class UIGemSecondConfirm : UIBase, INavBack
    {
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnCancel;
        [SerializeField] private TMP_Text infoText;
        private Action m_successEvent;              //扣款成功后的自定义事件
        private Item m_targetItem;                  //用于计算动态钻石价格的棋盘物件
        private int m_currentCost = -1;             //减少刷新次数用的临时变量
        private bool m_isSpeedUpOperation = false;  //当前是否为加速或装填弹窗
        private CoinType m_coinType;                //理论上只会有钻石，为了和上层方法参数统一，也方便未来拓展
        private int m_initialAmount;                //静态费用
        private ReasonString m_reason;              //消费归因
        private bool m_dynamicPrice = false;        //是否动态价格
        private bool m_closeWhenShopRefresh = false;//是否在商店刷新时关闭
        private ActivityLite m_sourceActivity = null;// 记录来源Activity信息

        protected override void OnCreate()
        {
            btnCancel.onClick.AddListener(_OnBtnCancel);
            btnConfirm.onClick.AddListener(_OnBtnConfirm);
        }

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            if (items[0] is CoinType coinType)
            {
                m_coinType = coinType;
            }

            if (items[1] is int amount)
            {
                m_initialAmount = amount;
                m_currentCost = amount; // 初始费用
            }

            if (items[2] is ReasonString reason)
            {
                m_reason = reason;
            }

            if (items[3] is bool dynamicPrice)
            {
                m_dynamicPrice = dynamicPrice;
            }

            if (items[4] is ActivityLite sourceActivity)
            {
                m_sourceActivity = sourceActivity;
            }

            if (items[5] is bool closeWhenShopRefresh)
            {
                m_closeWhenShopRefresh = closeWhenShopRefresh;
            }

            if (items[6] is Action successEvent)
            {
                m_successEvent = successEvent;
            }
        }


        protected override void OnPreOpen()
        {
            // 获取当前活跃的Item
            m_targetItem = Game.Manager.mergeBoardMan.activeItem;

            // 只有在有activeItem且是SpeedUp操作时才启用动态费用更新
            m_isSpeedUpOperation = m_dynamicPrice && m_targetItem != null && _IsSpeedUpOperation(m_targetItem);

            if (m_isSpeedUpOperation)
            {
                _RefreshCost(true);
            }
            else
            {
                _UpdateUI();
            }
        }

        protected override void OnAddListener()
        {
            // 添加定时器来更新费用和检查来源Activity
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondUpdate);
            if (m_closeWhenShopRefresh)
            {
                MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().AddListener(Close);
            }
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondUpdate);
            if (m_closeWhenShopRefresh)
            {
                MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().RemoveListener(Close);
            }
        }

        protected override void OnPostClose()
        {
            base.OnPostClose();
            m_successEvent = null;
            m_targetItem = null;
            m_currentCost = -1;
            m_isSpeedUpOperation = false;
            m_coinType = CoinType.NoneCoin;
            m_initialAmount = 0;
            m_reason = ReasonString.skip;
            m_dynamicPrice = false;
            m_sourceActivity = null;
            m_closeWhenShopRefresh = false;
        }

        private void _OnSecondUpdate()
        {
            // 检查来源Activity是否已关闭
            if (m_sourceActivity != null)
            {
                if (!m_sourceActivity.Valid)
                {
                    Close();
                    return;
                }
            }

            // 只在SpeedUp操作时更新费用
            if (m_isSpeedUpOperation)
            {
                _RefreshCost(false);
            }
        }

        private void _RefreshCost(bool forceRefresh = false)
        {
            if (m_isSpeedUpOperation && m_targetItem != null)
            {
                if (ItemUtility.TryGetItemSpeedUpInfo(m_targetItem, out var _, out var _, out var cost))
                {
                    if (forceRefresh || m_currentCost != cost)
                    {
                        m_currentCost = cost;
                        _UpdateUI();
                    }
                }
                //如果归零,是获取不到SpeedUpInfo的,此时需要关闭弹窗
                else
                {
                    m_currentCost = 0;
                    Close();
                }
            }
        }

        /// <summary>
        /// 检查是否为SpeedUp操作
        /// </summary>
        private bool _IsSpeedUpOperation(Item item)
        {
            return ItemUtility.TryGetItemSpeedUpInfo(item, out var _, out var _, out var _);
        }

        /// <summary>
        /// 更新UI显示
        /// </summary>
        private void _UpdateUI()
        {
            if (infoText != null)
            {
                infoText.text = I18N.FormatText("#SysComDesc1383", m_currentCost);
            }

            // 如果费用低于全局配置里的值，自动关闭弹窗
            if (m_currentCost < Game.Manager.configMan.globalConfig.SpdGemTips)
            {
                Close();
            }
        }

        private void _OnBtnCancel()
        {
            // 埋点：点击取消按钮
            int costAmount = m_isSpeedUpOperation && m_targetItem != null ? m_currentCost : m_initialAmount;
            DataTracker.gem_tips.Track(costAmount, m_reason, false);
            Close();
        }

        private void _OnBtnConfirm()
        {
            // 埋点：点击确认按钮
            int costAmount = m_isSpeedUpOperation && m_targetItem != null ? m_currentCost : m_initialAmount;
            DataTracker.gem_tips.Track(costAmount, m_reason, true);

            // 执行成功回调
            m_successEvent?.Invoke();

            // 执行实际的消费逻辑
            if (m_isSpeedUpOperation && m_targetItem != null)
            {
                // 对于SpeedUp操作，使用最新的费用
                _RefreshCost(false);
                if (m_currentCost > 0)
                {
                    Game.Manager.coinMan.PerformCoinConsumption(m_coinType, m_currentCost, m_reason);
                }
            }
            else
            {
                // 对于其他操作，使用初始费用
                Game.Manager.coinMan.PerformCoinConsumption(m_coinType, m_initialAmount, m_reason);
            }
            Close();
        }

        void INavBack.OnNavBack()
        {
            Close();
        }
    }
}
