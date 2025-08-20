/*
 *@Author:chaoran.zhang
 *@Desc:热气球活动结束弹板
 *@Created Time:2024.07.09 星期二 10:20:20
 */

using EL;

namespace FAT
{
    public class UIScoreFinish_piece : UIBase
    {
        protected override void OnCreate()
        {
            transform.AddButton("Content/ConfirmBtn", Close);
        }
    }
}
