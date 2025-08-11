/*
*@Author:chaoran.zhang
*@Desc:集卡功能结算弹窗
*@Created Time:2024.01.22 星期一 15:01
*/

using System;
using EL;

namespace FAT
{
    public class UIGuideCardUnlock: UIBase
    {
        private Action _callBack;
        
        protected override void OnCreate()
        {
            transform.AddButton("Content/Panel/BtnConfirm", Close);
        }

        protected override void OnParse(params object[] items)
        {
            if(items.Length > 0)
                _callBack = items[0] as Action;
        }

        protected override void OnPostClose()
        {
            _callBack?.Invoke();
        }
    }
}