/*
 * @Author: qun.chao
 * @Date: 2024-03-11 17:36:11
 */
using UnityEngine;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    public class MBItemUsageSpeedUpFree : MBItemUsageBase
    {
        [SerializeField] private TMPro.TMP_Text txtDesc;
        [SerializeField] private TMPro.TMP_Text txtCost;
        private int mCost;

        public override void Initialize()
        {
            base.Initialize();
            txtDesc.text = I18N.Text("#SysComBtn8");
        }

        public override void SetData(Item item)
        {
            base.SetData(item);
            mCost = -1;
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
            ItemUtility.TrySpeedUpEmptyItem(mItem, () =>
            {
                // // 加速表现效果
                // if (_item != null)
                //     BoardViewManager.Instance.ShowSpeedUpTip(_item.coord);
            });
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
