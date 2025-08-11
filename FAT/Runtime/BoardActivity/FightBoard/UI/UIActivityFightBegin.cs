/**
 * @Author: ang.cai
 * @Date: 2025/5/16 10:32:09
 * @LastEditors: ang.cai
 * @LastEditTime: 2025/5/20 18:32:09
 * Description: 打怪棋盘开始界面
 */

using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIActivityFightBegin : UIBase
    {
        // 活动实例
        private FightBoardActivity fightBoardActivity;
        // UI
        private TextMeshProUGUI leftTime;
        private TextProOnACircle roundText;
        private UIImageRes _bg;
        private UIImageRes _titlebg;
        private TMP_Text mainTitle;
        private TMP_Text subTitle;
        private TMP_Text desc3;
        private TMP_Text buttonText;

        #region UI
        protected override void OnCreate()
        {
            transform.Access("Content/Panel/TitleBg/Title", out roundText);
            transform.Access("Content/Panel/_cd/text", out leftTime);
            transform.Access("Content/Panel/TitleBg", out _titlebg);
            transform.Access("Content/Panel/Bg", out _bg);

            transform.Access("Content/Panel/TitleBg/Title", out mainTitle);
            transform.Access("Content/Panel/SubTitleBg/SubTitle", out subTitle);
            transform.Access("Content/Panel/Desc3", out desc3);
            transform.Access("Content/Panel/BtnConfirm/TextConfirm", out buttonText);

            transform.AddButton("Content/Panel/BtnConfirm", _ClickConfirm);
            transform.AddButton("Content/Panel/BtnClose", Close);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            // items[0]: activity;    items[1]: Custom
            fightBoardActivity = (FightBoardActivity)items[0];
        }
        protected override void OnPreOpen()
        {
            if (fightBoardActivity == null) return;
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
            UIUtility.CountDownFormat(leftTime, fightBoardActivity?.Countdown ?? 0);
        }
        private void _ClickConfirm()
        {
            Close();

            // 进入打怪棋盘
            fightBoardActivity?.Open();
        }
        #endregion

        private void RefreshTheme()
        {
            fightBoardActivity.StartPopup.visual.Refresh(_bg, "bgImage");
            fightBoardActivity.StartPopup.visual.Refresh(_titlebg, "titlebgImage");

            fightBoardActivity.StartPopup.visual.RefreshText(mainTitle, "mainTitle", roundText);
            fightBoardActivity.StartPopup.visual.RefreshText(subTitle, "subTitle", null);
            fightBoardActivity.StartPopup.visual.RefreshText(desc3, "desc3", null);
            fightBoardActivity.StartPopup.visual.RefreshText(buttonText, "button", null);

            fightBoardActivity.StartPopup.visual.RefreshStyle(mainTitle, "mainTitle");
            fightBoardActivity.StartPopup.visual.RefreshStyle(subTitle, "subTitle");
            fightBoardActivity.StartPopup.visual.RefreshStyle(desc3, "desc3");
            fightBoardActivity.StartPopup.visual.RefreshStyle(buttonText, "button");

        }
    }
}