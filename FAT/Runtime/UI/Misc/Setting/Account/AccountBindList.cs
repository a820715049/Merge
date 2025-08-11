using System;
using System.Collections.Generic;
using EL;
using FAT.Platform;
using UnityEngine;

namespace FAT {
    public class AccountBindList {
        public Transform root;
        public MapButton guest;
        public MapButton facebook;
        public MapButton apple;
        public MapButton google;
        public bool info;

        public void Init(Transform root_, bool info_, Action<AccountLoginType> TryBind, Action ToGuest) {
            root = root_;
            root_.Access("facebook", out facebook);
            root_.Access("apple", out apple);
            root_.Access("google", out google);
            if (root_.Access("guest", out guest)) {
                var sdk = PlatformSDK.Instance.Adapter;
                var valid = ToGuest != null && sdk.LoginId1 != sdk.SessionId;
                if (valid) {
                    guest.text.Setup(0);
                    guest.WhenClick = ToGuest;
                }
                guest.gameObject.SetActive(valid);
            }
            facebook.WhenClick = () => TryBind(AccountLoginType.Facebook);
            apple.WhenClick = () => TryBind(AccountLoginType.Apple);
            google.WhenClick = () => TryBind(AccountLoginType.Google);
            info = info_;
        }

        public void Refresh() {
            static void RefreshBind(MapButton target_, AccountLoginType type_, IDictionary<AccountLoginType, AccountBindInfo> map_) {
                if (!target_.gameObject.activeSelf) return;
                var text = target_.text;
                if (map_ == null || !map_.TryGetValue(type_, out var n)) {
                    text.Setup(0);
                }
                else {
                    var t = I18N.FormatText(text.state[1].text, n.name);
                    text.Setup(1, t);
                }
            }
            var sdk = PlatformSDK.Instance.Adapter;
            facebook.gameObject.SetActive(Available(AccountLoginType.Facebook));
            apple.gameObject.SetActive(Available(AccountLoginType.Apple));
            google.gameObject.SetActive(Available(AccountLoginType.Google));
            var map = info ? sdk.BindInfo : null;
            RefreshBind(facebook, AccountLoginType.Facebook, map);
            RefreshBind(apple, AccountLoginType.Apple, map);
            RefreshBind(google, AccountLoginType.Google, map);
        }

        public static bool Available(AccountLoginType type_) {
            var g = Game.Manager.configMan.globalConfig;
            var sdk = PlatformSDK.Instance.Adapter;
            if (!sdk.LoginAvailable(type_)) return false;
            var sA = sdk.GetOSName() == "Android";
            return type_ switch {
                AccountLoginType.Facebook => sA ? g.IsAndFacebook : g.IsIosFacebook,
                AccountLoginType.Apple => !sA && g.IsIosApple,
                AccountLoginType.Google => sA ? g.IsAndGoogle : g.IsIosGoogle,
                _ => true,
            };
        }

        public static bool AnyAvailable() {
            var g = Game.Manager.configMan.globalConfig;
            var sdk = PlatformSDK.Instance.Adapter;
            var sA = sdk.GetOSName() == "Android";
            if (sA) return g.IsAndFacebook || g.IsAndGoogle;
            return g.IsIosFacebook || g.IsIosApple || g.IsIosGoogle;
        }

        public int Count() {
            var n = 0;
            foreach(Transform c in root) {
                if (c.gameObject.activeSelf) ++n;
            }
            return n;
        }
    }
}