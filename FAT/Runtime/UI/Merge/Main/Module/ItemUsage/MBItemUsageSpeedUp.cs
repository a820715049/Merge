/*
 * @Author: qun.chao
 * @Date: 2023-10-26 14:36:03
 */
using UnityEngine;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    public class MBItemUsageSpeedUp : MBItemUsageBase
    {
        [SerializeField] private TMPro.TMP_Text txtDesc;
        [SerializeField] private TMPro.TMP_Text txtCost;
        private int mCost;

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void SetData(Item item)
        {
            base.SetData(item);
            mCost = -1;
            _RefreshText();
        }

        public override void Refresh()
        {
            base.Refresh();
            _Refresh();
        }

        public override void UpdateContent()
        {
            base.UpdateContent();
            _Refresh();
        }

        protected override void OnBtnClick()
        {
            base.OnBtnClick();
            if (UIUtility.ShowMoneyNotEnoughTip(CoinType.Gem, mCost))
                return;
            var _item = mItem;
            if (ItemUtility.SpeedUpEmptyItem(mItem))
            {
                // 气泡加速后可能产生积分
                if (_item.HasComponent(ItemComponentType.Bubble))
                {
                    var score = _item.config.BubblePrice;
                    if (score > 0)
                    {
                        MessageCenter.Get<MSG.ON_USE_SPEED_UP_ITEM>().Dispatch(_item, score);
                    }
                }

                // // 加速表现效果
                // if (_item != null)
                //     BoardViewManager.Instance.ShowSpeedUpTip(_item.coord);
            }
        }

        private void _RefreshText()
        {
            if (mItem != null && mItem.HasComponent(ItemComponentType.Bubble))
                txtDesc.text = I18N.Text("#SysComBtn20");
            else
                txtDesc.text = I18N.Text("#SysComDesc82");
        }

        private void _Refresh()
        {
            ItemUtility.TryGetItemSpeedUpInfo(mItem, out var _, out var _, out var cost);
            if (mCost != cost)
            {
                mCost = cost;
                txtCost.text = $"{cost}";
            }
        }
    }
}