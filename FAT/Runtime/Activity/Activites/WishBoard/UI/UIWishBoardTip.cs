using System.Linq;
using EL;
using TMPro;
using UnityEngine.UI;

namespace FAT
{
    public class UIWishBoardTip : UIBase
    {
        public UIImageRes bg;
        public UIImageRes titleBg;
        public TextMeshProUGUI title;
        public TextMeshProUGUI desc;
        private WishBoardActivity _activity;

        public void OnCreate()
        {
            transform.AddButton("Content/root/confirm", Close);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2) { return; }
            _activity = items[0] as WishBoardActivity;
            desc.text = items[1] as string;
        }

        protected override void OnPreOpen()
        {
            var visual = _activity.VisualUITip;
            visual.visual.Refresh(bg, "bgImage");
            visual.visual.Refresh(titleBg, "titleImage");
            visual.visual.Refresh(title, "mainTitle");
            visual.visual.Refresh(desc, "desc");
        }
    }
}