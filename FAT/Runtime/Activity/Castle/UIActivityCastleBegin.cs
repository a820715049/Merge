/**
 * @Author: zhangpengjian
 * @Date: 2025/7/11 11:00:00
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/11 11:00:00
 * Description: 沙堡里程碑活动开始界面
 */

using EL;
using FAT.MSG;
using TMPro;

namespace FAT
{
    public class UIActivityCastleBegin : UIBase
    {
        // 活动实例
        private ActivityCastle _activity;

        // UI
        private TextMeshProUGUI leftTime;
        private TextProOnACircle title;


        #region UI
        protected override void OnCreate()
        {
            transform.Access("Content/Panel/TitleBg/Title", out title);
            transform.Access("Content/Panel/_cd/text", out leftTime);
            transform.AddButton("Content/Panel/BtnConfirm", _ClickConfirm);
            transform.AddButton("Content/Panel/BtnClose", _ClickConfirm);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            // items[0]: activity;    items[1]: Custom
            _activity = (ActivityCastle)items[0];
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
        }   


        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshCD);
        }
        #endregion

        #region Listener
        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(leftTime, _activity?.Countdown ?? 0);
        }

        private void _ClickConfirm()
        {
            _activity.Open();
            Close();
        }
        #endregion

        private void RefreshTheme()
        {
        }
    }
}