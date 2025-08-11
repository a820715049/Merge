
using EL;
using TMPro;
using UnityEngine;
namespace FAT
{
    public class UIMineBoardStartNotice : UIBase
    {
        // Image字段
        private UIImageRes _Bg;
        private UIImageRes _TitleBg;
        private UIImageRes _frame;

        // Text字段
        private TextProOnACircle _Title;
        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _SubTitle;

        private MineBoardActivity _activity;
        protected override void OnCreate()
        {
            base.OnCreate();
            // Image绑定
            transform.Access("Content/Bg_img", out _Bg);
            transform.Access("Content/TitleBg_img", out _TitleBg);
            transform.Access("Content/cd/frame_img", out _frame);
            // Text绑定
            transform.Access("Content/TitleBg_img/Title_txt", out _Title);
            transform.Access("Content/cd/cd_txt", out _cd);
            transform.Access("Content/SubTitle_txt", out _SubTitle);
            transform.AddButton("Content/Confirm", ClickBtn);
        }

        protected override void OnParse(params object[] items)
        {
            // 活动数据
            if (items.Length < 1) return;
            _activity = items[0] as MineBoardActivity;
        }

        protected override void OnPreOpen()
        {
            // 活动数据
            if (_activity == null) return;
            _activity.StartTheme.Refresh(_Title, "mainTitle");
            RefreshCD();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(_cd, _activity?.Countdown ?? 0);
        }

        private void ClickBtn()
        {
            Close();
            Game.Manager.mineBoardMan.EnterMineBoard();
        }
    }
}
