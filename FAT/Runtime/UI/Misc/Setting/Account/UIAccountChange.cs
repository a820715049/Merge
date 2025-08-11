using FAT.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace FAT {
    public class UIAccountChange : UIBase {
        public AccountBindList list = new();
        internal UIRectState rect;

        protected override void OnCreate() {
            transform.Access("Content/Panel", out Transform root);
            root.Access(out rect);
            root.Access("close", out MapButton close);
            close.WhenClick = Close;
            var sdk = PlatformSDK.Instance.binding;
            list.Init(root.Find("list"), false, t_ => sdk.TryBind(t_, switch_:true), sdk.ToGuest);
        }

        protected override void OnPreOpen() {
            RefreshInfo();
        }

        public void RefreshInfo() {
            list.Refresh();
            rect.SelectNear(list.Count() - 1);
        }
    }
}