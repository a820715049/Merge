using System;
using System.Collections.Generic;
using EL;
using FAT.Merge;

namespace FAT
{
    public class UICompleteOrderBag : UIBase
    {
        private UICommonItem _item1;
        private UICommonItem _item2;
        private UICommonItem _item3;
        private Action _callBack;

        protected override void OnCreate()
        {
            transform.Access("Content/Panel/Layout/UICommonItem1", out _item1);
            transform.Access("Content/Panel/Layout/UICommonItem2", out _item2);
            transform.Access("Content/Panel/Layout/UICommonItem3", out _item3);
            transform.AddButton("Content/Panel/Button/BtnCancel", Close);
            transform.AddButton("Content/Panel/Button/BtnConfirm", Confirm);
        }

        private void Confirm()
        {
            _callBack?.Invoke();
            Close();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2) return;
            var list = items[0] as List<Item>;
            _callBack = items[1] as Action;

            _item1.gameObject.SetActive(false);
            _item2.gameObject.SetActive(false);
            _item3.gameObject.SetActive(false);
            if (list?.Count >= 1) _item1.Refresh(list[0].tid, 1);
            if (list?.Count >= 2) _item2.Refresh(list[1].tid, 1);
            if (list?.Count >= 3) _item3.Refresh(list[2].tid, 1);
        }

        protected override void OnPostClose()
        {
            _callBack = null;
            Game.Manager.mergeBoardMan.activeWorld.ClearPriorityConsumeItem();
        }
    }
}
