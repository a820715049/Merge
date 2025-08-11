// ================================================
// File: UIFarmBoardTips.cs
// Author: yueran.li
// Date: 2025/04/29 15:04:01 星期二
// Desc: 农场活动 点击Animal Tip
// ================================================

using EL;
using FAT.Merge;
using FAT.MSG;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIFarmBoardAnimalTips : UITipsBase
    {
        private UIImageRes icon;
        private int id;
        public NonDrawingGraphic panelGraphic;
        public UIOutsideCheckDown checkDown;

        private FarmBoardActivity _activity;

        protected override void OnCreate()
        {
            transform.Access("Panel/Icon", out icon);
            transform.Access("Panel", out panelGraphic);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            // items[0] Vector3 位置
            // items[1] float 偏移参数
            _SetTipsPosInfo(items);

            if (items.Length > 2)
            {
                id = (int)items[2];
            }

            _activity = (FarmBoardActivity)(items[3]);
        }

        protected override void OnPreOpen()
        {
            // 刷新tips位置
            _RefreshTipsPos(18);
            var cfg = Env.Instance.GetItemConfig(id);
            icon.SetImage(cfg.Icon);
        }

        protected override void OnAddListener()
        {
            base.OnAddListener();
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            base.OnRemoveListener();
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        protected override void OnPostOpen()
        {
            if (_activity.CheckHasConsumeItem())
            {
                panelGraphic.raycastTarget = false;
                checkDown.enabled = false;
            }
        }

        protected override void OnPreClose()
        {
            panelGraphic.raycastTarget = true;
            checkDown.enabled = true;
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is FarmBoardActivity)
            {
                Close();
            }
        }
    }
}