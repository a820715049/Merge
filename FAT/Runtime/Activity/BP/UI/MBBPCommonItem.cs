// ==================================================
// // File: MBBPCommonItem.cs
// // Author: liyueran
// // Date: 2025-07-14 10:07:48
// // Desc: $UIBP commonItem 封装
// // ==================================================

using Config;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBBPCommonItem : MonoBehaviour
    {
        public UICommonItem item;
        public LayoutElement layout;

        public UIImageRes bg;
        private BPActivity _activity;

        public void Setup( BPActivity activity, float preferWidth, float preferHeight, bool hideBg = false)
        {
            item.Setup();
            this._activity = activity;

            if (hideBg)
            {
                bg.gameObject.SetActive(false);

                layout.preferredWidth = preferWidth;
                layout.preferredHeight = preferHeight;
            }
            else
            {
                var endVisual = _activity.EndPopup.visual;
                if (endVisual.Theme.AssetInfo.TryGetValue("itembg",out var bgValue))
                {
                    bg.SetImage(bgValue);
                    bg.gameObject.SetActive(true);
                }
            }
        }
    }
}