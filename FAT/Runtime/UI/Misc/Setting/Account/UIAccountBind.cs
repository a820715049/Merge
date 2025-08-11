using FAT.Platform;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace FAT {
    public class UIAccountBind : UIBase {
        public AccountBindList list = new();
        internal UIRectState rect;
        private Action WhenBind;

        protected override void OnCreate() {
            transform.Access("Content/Panel", out Transform root);
            root.Access(out rect);
            root.Access("close", out MapButton close);
            root.Access("change", out MapButton change);
            close.WhenClick = Close;
            change.WhenClick = () => UIManager.Instance.OpenWindow(UIConfig.UIAccountChange);
            var sdk = PlatformSDK.Instance.binding;
            void R(bool r_) {
                if (r_) RefreshInfo();
            }
            list.Init(root.Find("list"), true, t_ => sdk.TryBind(t_, WhenComplete_:R), null);
        }

        protected override void OnPreOpen() {
            WhenBind ??= RefreshInfo;
            RefreshInfo();
        }

        public void RefreshInfo() {
            list.Refresh();
            rect.SelectNear(list.Count() - 1);
        }
    }
}