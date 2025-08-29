/*
 *@Author:chaoran.zhang
 *@Desc:热气球活动结束弹板
 *@Created Time:2024.07.09 星期二 10:20:20
 */

using EL;
using TMPro;

namespace FAT
{
    public class UIRaceEnd : UIBase
    {
        private TextMeshProUGUI _title;
        protected override void OnCreate()
        {
            transform.AddButton("Content/ConfirmBtn", Close);
            transform.Access("Content/TitleBg/Title", out _title);
        }

        protected override void OnParse(params object[] items)
        {
            var activity = items[0] as ActivityRace;
            activity.EndVisual.Refresh(_title, "mainTitle");
        }
    }
}