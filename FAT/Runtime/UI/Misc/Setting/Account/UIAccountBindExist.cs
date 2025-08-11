using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using centurygame;
using EL;
using FAT.Platform;
using UnityEngine;

namespace FAT {
    public class UIAccountBindExist : UIBase {
        protected override void OnCreate() {
            transform.Access("Content/Panel", out Transform root);
            root.Access("close", out MapButton close);
            root.Access("change", out MapButton change);
            root.Access("help", out MapButton help);
            close.WhenClick = Close;
            change.WhenClick = () => UIManager.Instance.OpenWindow(UIConfig.UIAccountChange);
            help.WhenClick = () => PlatformSDK.Instance.ShowCustomService();
        }

        protected override void OnPreOpen() {
            
        }

        protected override void OnPostClose() {
            UIManager.Instance.Visible(UIConfig.UIAccountBind, true);
        }
    }
}