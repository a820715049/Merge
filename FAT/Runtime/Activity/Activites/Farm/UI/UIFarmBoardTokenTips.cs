// ================================================
// File: UIFarmBoardTips.cs
// Author: yueran.li
// Date: 2025/04/29 15:04:01 星期二
// Desc: 农场活动 点击Token Tip
// ================================================

using EL;
using FAT.MSG;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIFarmBoardTokenTips : UITipsBase
    {
        private TextMeshProUGUI desc;
        private TextMeshProUGUI title;
        private Button confirmBtn;
        private FarmBoardActivity _activity;

        protected override void OnCreate()
        {
            // transform.Access("Panel/desc", out desc);
            transform.Access("Panel/title", out title);
            transform.Access("Panel/confirm", out confirmBtn);

            confirmBtn.onClick.AddListener(OnClickConfirm);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            // items[0] Vector3 位置
            // items[1] float 偏移参数
            _activity = (FarmBoardActivity)(items[2]);
            _SetTipsPosInfo(items);
        }

        protected override void OnPreOpen()
        {
            // 刷新tips位置
            _RefreshTipsPos(18);

            RefreshTheme();

            // 根据配置 判断显示哪个 
            switch (_activity.OutputType)
            {
                case FarmBoardActivity.TokenOutputType.Order:
                    transform.Find("Panel/desc/score").gameObject.SetActive(false);
                    break;
                case FarmBoardActivity.TokenOutputType.Energy:
                    transform.Find("Panel/desc/order").gameObject.SetActive(false);
                    break;
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != _activity) return;
            Close();
        }

        private void OnClickConfirm()
        {
            var ui = UIManager.Instance.TryGetUI(_activity.VisualBoard.res.ActiveR);
            if (ui != null && ui is UIFarmBoardMain main)
            {
                main.Exit(true); // 退出活动时默认返回主棋盘
                Close();
            }
        }

        // 换皮
        private void RefreshTheme()
        {
            // desc.SetText(I18N.Text(""));
            // title.SetText(I18N.Text(""));
        }
    }
}