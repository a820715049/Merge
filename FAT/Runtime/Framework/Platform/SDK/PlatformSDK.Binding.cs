
using System;
using System.Collections;
using System.Diagnostics;
using EL;
using fat.rawdata;

namespace FAT.Platform {
    public partial class PlatformSDK {
        public AccountBinding binding = new();
    }

    public class AccountBinding {
        public void TryBind(AccountLoginType type_, bool switch_ = false, Action<bool> WhenComplete_ = null) {
            var switchV = switch_ && true;
            var tip = Game.Manager.commonTipsMan;
            var sdk = PlatformSDK.Instance.Adapter;
            if (!switch_ && sdk.LoginType == type_) {
                tip.ShowClientTips(I18N.Text("#SysComDesc506"));
                return;
            }
            var ui = UIManager.Instance;
            IEnumerator Routine() {
                var sw = new Stopwatch();
                var token = new WaitToken();
                sdk.GetBindInfo((_, _) => { token.Cancel(); });
                //wait GetBindInfo
                yield return token.Wait(15, sw);
                if (!sdk.BindInit) {
                    tip.ShowClientTips(I18N.Text("#SysComDesc1636"));
                    yield break;
                }
                var map = sdk.BindInfo;
                if (!switch_ && map.TryGetValue(type_, out var n)) {
                    tip.ShowClientTips(I18N.Text("#SysComDesc506"));
                    yield break;
                }
                if (switch_ && map.Count == 0) {//not bound to anything
                    var confirm = false;
                    token.Reset();
                    tip.ShowMessageTips(
                        I18N.Text("#SysComDesc512"), I18N.Text("#SysComDesc507"),
                        () => { confirm = false; token.Cancel(); },
                        () => { confirm = true; token.Cancel(); }
                    );
                    //wait user confirm
                    yield return token.Wait(ui_:false, block_:false);
                    if (!confirm) yield break;
                }
                DataTracker.AccountBinding(0, switch_, null, type_);
                var bR = false;
                SDKError bE = null;
                token.Reset();
                sdk.BindAccount(type_, (r, e) => {
                    token.Cancel();
                    (bR, bE) = (r, e);
                }, switch_: switchV);
                //wait BindAccount
                yield return token.Wait();
                if (token.Timeout()) yield break;
                if (bR) {
                    token.Reset();
                    sdk.GetBindInfo((_, _) => { token.Cancel(); });
                    //wait GetBindInfo
                    yield return token.Wait(10, sw);
                }
                map = sdk.BindInfo;
                map.TryGetValue(type_, out var pN);
                DataTracker.AccountBinding(1, switch_, pN.id, type_, bR, bE);
                if (bR) {
                    if (switchV) {
                        sdk.LoginType = sdk.LoginCache;
                        sdk.LogoutSkip = true;
                    }
                    GameProcedure.RestartGame();
                    yield break;
                }
                else ErrorToast((int)bE.ErrorCode, bE.Message);
                WhenComplete_?.Invoke(bR);
            }
            Game.Instance.StartCoroutineGlobal(Routine());
        }

        public void ToGuest() {
            var sdk = PlatformSDK.Instance.Adapter;
            IEnumerator R() {
                var ui = UIManager.Instance;
                ui.OpenWindow(UIConfig.UIWait);
                ui.Block(true);
                yield return sdk.Logout();
                ui.CloseWindow(UIConfig.UIWait);
                ui.Block(false);
                if (sdk.LoginId1 != sdk.SessionId) {
                    sdk.LoginType = AccountLoginType.Guest;
                }
                GameProcedure.RestartGame();
            }
            Game.Instance.StartCoroutineGlobal(R());
        }

        public bool CheckBind(AccountLoginType type_, bool openUI = true)
        {
            var sdk = PlatformSDK.Instance.Adapter;
            if (sdk.BindInfo.ContainsKey(type_) && sdk.LoginType == type_)
            {
                return true;
            }
            else
            {
                if (!openUI) 
                    return false;
                switch (type_)
                {
                    case AccountLoginType.Facebook:
                        UIConfig.UIFacebookBindNotice.Open();
                        break;
                }
                return false;
            }
        }

        public void ErrorToast(int code_, string msg_) {
            var ui = code_ switch {
                //already bound
                1109 or
                2106 or 2403 or 2501 or 1206 or
                2903 or 2800 or 2410
                    => UIConfig.UIAccountBindExist,
                _ => null,
            };
            if (ui != null) {
                UIManager.Instance.Visible(UIConfig.UIAccountBind, false);
                UIManager.Instance.OpenWindow(ui);
                return;
            }
            if (msg_ != null) DebugEx.Warning(msg_);
            var tip = Game.Manager.commonTipsMan;
            var msg = code_ switch {
                //cancel
                2104 => "#SysComDesc502",//facebook
                2803 => "#SysComDesc503",//apple
                2406 or 2700 => "#SysComDesc504",//google
                _ => null,
            };
            if (msg != null) {
                tip.ShowClientTips(I18N.FormatText(msg, code_));
                return;
            }
            var toast = code_ switch {
                //bind fail
                1109 or 1124 or 1133 or 1163 or
                2109 or 2622 or 2623 or
                2102 or 2105 or 2107 or 2108 or 2603 or 2604 or 2607 or 2608 or
                2804 or 2805 or 2806 or 2807 or 2808 or 2810 or 1210
                    => Toast.BindError,
                //unknown
                _ => Toast.UnknownError,
            };
            DebugEx.Warning($"binding error code:{code_} toast:{toast}");
            tip.ShowPopTips(toast, code_);
        }
    }
}