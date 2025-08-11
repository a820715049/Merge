/**
 * @Author: ang.cai
 * @Date: 2025/5/16 15:22:09
 * @LastEditors: ang.cai
 * @LastEditTime: 2025/5/16 15:22:09
 * Description: 打怪棋盘结束界面
 */

using EL;
using fat.rawdata;
using TMPro;

namespace FAT
{
    public class UIActivityFightEnd : UIBase
    {
        private UIImageRes _bg;
        private UIImageRes _titleBg;
        private TextProOnACircle _title;
        private FightBoardActivity fightBoardActivity;
        private TextMeshProUGUI _desc1;
        private TMP_Text buttonText;
        private TMP_Text mainTitle;
        private TMP_Text subTitle;
        private TMP_Text desc1;
        protected override void OnCreate()
        {
            transform.Access("Content/Panel/bg", out _bg);
            transform.Access("Content/Panel/bg1", out _titleBg);
            transform.Access("Content/Panel/bg1/title", out _title);
            transform.Access("Content/Panel/desc1", out _desc1);
            transform.Access("Content/Panel/confirm/text", out buttonText);

            transform.Access("Content/Panel/title", out mainTitle);
            transform.Access("Content/Panel/bg2/desc2", out subTitle);
            transform.Access("Content/Panel/desc1", out desc1);

            transform.AddButton("Content/Panel/confirm", Close);
        }

        protected override void OnParse(params object[] items)
        {
            fightBoardActivity = items[0] as FightBoardActivity;
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
        }

        private void RefreshTheme()
        {
            fightBoardActivity.EndPopup.visual.Refresh(_bg, "bgImage");
            fightBoardActivity.EndPopup.visual.Refresh(_titleBg, "titleImage");
            fightBoardActivity.EndPopup.visual.Refresh(_title, "mainTitle");
            fightBoardActivity.EndPopup.visual.Refresh(_desc1, "desc1");
            fightBoardActivity.EndPopup.visual.RefreshText(buttonText, "button", null);

            fightBoardActivity.EndPopup.visual.RefreshStyle(mainTitle, "mainTitle");
            fightBoardActivity.EndPopup.visual.RefreshStyle(subTitle, "subTitle");
            fightBoardActivity.EndPopup.visual.RefreshStyle(desc1, "desc1");
            fightBoardActivity.EndPopup.visual.RefreshStyle(buttonText, "button");
        }
    }
}