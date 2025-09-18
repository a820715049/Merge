using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UILevelPack : UIBase
    {
        // Text字段
        private TextProOnACircle _title;
        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _subtitle;
        private TextMeshProUGUI _tip;

        private TextMeshProUGUI _beforePrice;
        private TextMeshProUGUI _currentPrice;

        private RectTransform _best;
        private TextMeshProUGUI _bestDiscount;

        private MBRewardLayout _group;
        private MapButton _confirm;

        private NonDrawingGraphic _block;

        // 活动实例
        private PackLevel _pack;


        #region UIBase
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("Content/Bg/best", out _best);
            transform.Access("Content/Bg/best/discount", out _bestDiscount);
            transform.Access("Content/Bg/TitleBg/Title", out _title);
            transform.Access("Content/Bg/subTitle", out _subtitle);
            transform.Access("Content/Bg/_group", out _group);
            transform.Access("Content/Bg/_cd/text", out _cd);
            transform.Access("Content/Bg/tip", out _tip);

            transform.Access("Content/Bg/confirm/price/before", out _beforePrice);
            transform.Access("Content/Bg/confirm/price/cur", out _currentPrice);
            transform.Access("Content/Bg/confirm", out _confirm);
            transform.Access("block", out _block);
        }

        private void AddButton()
        {
            transform.AddButton("Content/Bg/CloseBtn", OnClickClose).WithClickScale().FixPivot();
            _confirm.WithClickScale().FixPivot().WhenClick = OnClickBuy;
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _pack = (PackLevel)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
            RefreshCd();
            RefreshPack();
            RefreshPrice();

            if (IsBlock)
            {
                SetBlock(false);
            }
        }


        protected override void OnAddListener()
        {
            MessageCenter.Get<IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<ACTIVITY_REFRESH>().AddListener(WhenRefresh);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<ACTIVITY_REFRESH>().RemoveListener(WhenRefresh);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        protected override void OnPostOpen()
        {
            _group.Refresh(_pack.Goods.reward);
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
        }
        #endregion

        #region 事件
        private void WhenInit()
        {
            RefreshPrice();
        }

        private void WhenTick()
        {
            RefreshCd();
        }

        private void WhenRefresh(ActivityLike act)
        {
            if (act is not PackLevel pack)
            {
                return;
            }

            RefreshPack();
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            Close();
        }

        private void OnPurchase(IList<RewardCommitData> list)
        {
            SetBlock(true);

            _pack.ResetPopup();
            for (var n = 0; n < list.Count; ++n)
            {
                var pos = _group.list[n].icon.transform.position;
                UIFlyUtility.FlyReward(list[n], pos);
            }

            IEnumerator DelayClose()
            {
                yield return new WaitForSeconds(1.2f);
                Close();
            }

            StartCoroutine(DelayClose());
        }

        private void OnClickBuy()
        {
            _pack.TryPurchase(OnPurchase);
        }

        private void OnClickClose()
        {
            Close();
        }
        #endregion

        private void RefreshPrice()
        {
            var (before, price) = _pack.GetPrice();
            _beforePrice.SetText(before);
            _currentPrice.SetText(price);

            _best.anchoredPosition = new Vector2(_best.anchoredPosition.x, _pack.Goods.reward.Count > 5 ? 150 : 75);
            _bestDiscount.SetText(I18N.FormatText("#SysComDesc913", _pack.ConfDetail.Label)); // {0}%折扣
        }

        private void RefreshPack()
        {
            _group.Refresh(_pack.Goods.reward);
        }

        private void RefreshTheme()
        {
            if (_pack == null)
            {
                return;
            }


            // 等级
            _title.SetText(I18N.FormatText("#SysComDesc1637", _pack.GetCurConfLv())); // {0}级礼包
            _subtitle.SetText(I18N.FormatText("#SysComDesc1638", _pack.GetCurConfLv())); // 恭喜达到{0}级，特别为您准备专属礼包！

            var diff = _pack.GetNextLevelDiff();
            if (diff != -1)
            {
                _tip.SetText(I18N.FormatText("#SysComDesc1640", diff)); // 距离下一个礼包还差{0}级
            }
            else
            {
                _tip.gameObject.SetActive(false);
            }
        }


        private void RefreshCd()
        {
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _pack.endTS - t);
            UIUtility.CountDownFormat(_cd, diff);
        }

        private bool IsBlock => _block.raycastTarget;

        private void SetBlock(bool block)
        {
            _block.raycastTarget = block;
        }
    }
}