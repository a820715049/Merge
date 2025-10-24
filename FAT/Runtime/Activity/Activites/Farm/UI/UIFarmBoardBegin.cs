// ================================================
// File: UIFarmBoardBegin.cs
// Author: yueran.li
// Date: 2025/04/29 14:46:54 星期二
// Desc: 农场活动开启界面
// ================================================


using EL;
using FAT.MSG;
using TMPro;

namespace FAT
{
    public class UIFarmBoardBegin : UIBase
    {
        // 活动实例
        private FarmBoardActivity _activity;

        // UI
        private TextMeshProUGUI leftTime;
        private TextMeshProUGUI desc;
        private TextProOnACircle title;


        #region UI
        protected override void OnCreate()
        {
            transform.Access("Content/Panel/TitleBg/Title", out title);
            transform.Access("Content/Panel/Desc3", out desc);
            transform.Access("Content/Panel/_cd/text", out leftTime);
            transform.AddButton("Content/Panel/BtnConfirm", _ClickConfirm);
            transform.AddButton("Content/Panel/BtnClose", Close);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            // items[0]: activity;    items[1]: Custom
            _activity = (FarmBoardActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            if (_activity == null) return;
            _RefreshCD();
            RefreshTheme();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(_RefreshCD);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }


        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshCD);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }
        #endregion

        #region Listener
        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(leftTime, _activity?.Countdown ?? 0);
        }

        private void _ClickConfirm()
        {
            Close();

            // 进入农场棋盘
            _activity?.Open();
        }
        #endregion

        private void RefreshTheme()
        {
            _activity.StartPopup.visual.Refresh(title,"mainTitle");
            _activity.StartPopup.visual.Refresh(desc,"subTitle");
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