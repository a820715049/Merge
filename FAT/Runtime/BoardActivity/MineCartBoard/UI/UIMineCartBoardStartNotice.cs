
using System;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
namespace FAT
{
    public class UIMineCartBoardStartNotice : UIBase
    {
        // Image字段
        [SerializeField] private UIImageRes _Bg;
        [SerializeField] private UIImageRes _TitleBg;
        [SerializeField] private UIImageRes _frame;

        // Text字段
        [SerializeField] private TextProOnACircle _Title;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private TextMeshProUGUI _SubTitle;

        private MineCartActivity _activity;
        protected override void OnCreate()
        {
            base.OnCreate();
            transform.AddButton("Content/Confirm", ClickBtn);
            transform.AddButton("Content/Close", Close);
        }

        protected override void OnParse(params object[] items)
        {
            // 活动数据
            if (items.Length < 1) return;
            _activity = items[0] as MineCartActivity;
        }

        protected override void OnPreOpen()
        {
            // 活动数据
            if (_activity == null) return;
            _activity.StartPopup.visual.Refresh(_Title, "mainTitle");
            _activity.StartPopup.visual.Refresh(_SubTitle, "subTitle");

            _activity.StartPopup.visual.Refresh(_TitleBg, "titleBg");
            _activity.StartPopup.visual.Refresh(_Bg, "bg");
            _activity.StartPopup.visual.Refresh(_frame, "frame");
            RefreshCD();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        private void RefreshCD()
        {
            if (_activity == null) return;
            var v = _activity.Countdown;
            if (v <= 0)
            {
                Close();
                return;
            }
            UIUtility.CountDownFormat(_cd, v);
        }

        private void ClickBtn()
        {
            Close();
            _activity?.Open();
        }
        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is MineCartActivity)
            {
                Close();
            }
        }
    }
}
