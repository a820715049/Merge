/*
 *@Author:chaoran.zhang
 *@Desc:
 *@Created Time:2024.02.19 星期一 16:21:02
 */

using EL;
using TMPro;

namespace FAT
{
    public class UIGuideBoost : UIBase
    {
        protected override void OnCreate()
        {
            transform.AddButton("Content/BtnConfirm", Close);
        }
    }
}