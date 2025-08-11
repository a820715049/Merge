using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using EL;
using fat.rawdata;

namespace FAT.Platform {
    using static PoolMapping;

    public partial class PlatformSDK {
        public ShareLink shareLink = new();
    }

    public class ShareLink {
        public void Test() {
            DebugEx.Info($"invalid payload, expect:fail");
            //{id:\"xxxx\",target:\"nnnn\"}
            ProcessPayload("e2lkOiJ4eHh4Iix0YXJnZXQ6Im5ubm4ifQ==", true, null);
            DebugEx.Info($"malformed payload, expect:fail with error");
            //id:<fpid>,target:0,ts:<now>
            ProcessPayload("aWQ6MTA0MTA1MDYxLHRhcmdldDowLHRzOjE3MjgxMTA1OTU=", true, null);
            DebugEx.Info($"correct payload, expect:success");
            //id:<fpid>,target:0,ts:<now>,tss:<>,ei:<>,ef:<>,ep:<>,st:<>
            ProcessPayload("aWQ6MTA0MTA1MDYxLHRhcmdldDo2LHRzOjE3MjkwNDQ2NDQsdHNzOjE3MjkwNDQ2NDQsZWk6MCxlZjoxMDAsZXA6MCxzdDox", true, null);
        }

        public void TestSend(bool share_) {
            var sdk = PlatformSDK.Instance.Adapter;
            var type = share_ ? 1 : 0;
            var p = $"id:{sdk.SessionId},target:{6},ts:{Game.TimestampNow()},tss:{Game.TimestampNow()},ei:{0},ef:{ActivityLite.FromInternal},ep:{0},st:{type}";
            TrySend(p, null, share_, null);
        }

        public void TrySend(ActivityInvite acti_, bool share_, Action<bool, SDKError> WhenComplete_) {
            var sdk = PlatformSDK.Instance.Adapter;
            var type = share_ ? 1 : 0;
            var p = $"id:{sdk.SessionId},target:{acti_?.confD.Level},ts:{Game.TimestampNow()},tss:{acti_?.startTS},ei:{acti_?.Id},ef:{acti_?.From},ep:{acti_?.Param},st:{type}";
            TrySend(p, acti_, share_, WhenComplete_);
        }

        public void TrySend(string payload_, ActivityInvite acti_, bool share_, Action<bool, SDKError> WhenComplete_) {
            var sdk = PlatformSDK.Instance.Adapter;
            var os = sdk.GetOSName();
            var type = (os, share_) switch {
                ("iOS", true) => 1,
                ("iOS", false) => 2,
                ("Android", true) => 3,
                ("Android", false) => 4,
                _ => 0,
            };
            var b = Encoding.UTF8.GetBytes(payload_);
            var b64 = Convert.ToBase64String(b);
            DebugEx.Info($"deeplink: content:{payload_} payload:{b64}");
            IEnumerator Routine() {
                var sw = new Stopwatch();
                var token = new WaitToken();
                string bL = null;
                var bR = false;
                SDKError bE = null;
                sdk.ShareLink(b64, (l_, r_, e_) => {
                    token.Cancel();
                    (bL, bR, bE) = (l_, r_, e_);
                }, share_:share_);
                yield return token.Wait(15, sw);
                if (token.Timeout("#SysComDesc602")) yield break;
                if (bR && !share_) {
                    token.Reset();
                    sdk.Share(bL, ShareType.Link, (r_, e_) => {
                        token.Cancel();
                        (bR, bE) = (r_, e_);
                    });
                    yield return token.Wait(15, sw);
                }
                if (token.Timeout("#SysComDesc602")) yield break;
                if (acti_ != null) DataTracker.DeeplinkShare(acti_, type, bR, bE);
                if (!bR || type == 1) ResultToast(bR, bE);
                WhenComplete_?.Invoke(bR, bE);
            }
            Game.Instance.StartCoroutineGlobal(Routine());
        }

        public void ResultToast(bool r_, SDKError e_) {
            var msg = e_?.Message;
            var code = e_?.ErrorCode ?? 0;
            var tip = Game.Manager.commonTipsMan;
            if (msg != null) DebugEx.Warning(msg);
            var t = r_ ? I18N.Text("#SysComDesc603") : I18N.FormatText(code switch {
                2104 => "#SysComDesc601",//cancel
                2111 or 2119 => "#SysComDesc617",//not installed
                _ => "#SysComDesc602",//fail
            }, code);
            tip.ShowClientTips(t);
        }

        internal void ProcessPayload(string p_, bool check_, Action<AccountMan.Referrer> WhenComplete_) {
            static bool S(ReadOnlySpan<char> s_, out ReadOnlySpan<char> a_, out ReadOnlySpan<char> b_, char l_) {
                a_ = s_;
                b_ = ReadOnlySpan<char>.Empty;
                var p = s_.IndexOf(l_);
                if (p > 0) {
                    a_ = s_[..p];
                    b_ = s_[(p + 1)..];
                }
                return p > 0;
            }
            static void K(Dictionary<string, string> v_, ReadOnlySpan<char> a_, char l_) {
                if (!S(a_, out var n, out var v, l_)) {
                    DebugEx.Warning($"invalid segment {a_.ToString()}");
                }
                else {
                    v_[n.ToString()] = v.ToString();
                }
            }
            static void M(ReadOnlySpan<char> s_, Dictionary<string, string> v_) {
                var l = ',';
                var l1 = ':';
                while(s_.Length > 0) {
                    S(s_, out var a, out s_, l);
                    K(v_, a, l1);
                }
            }
            try {
                var account = Game.Manager.accountMan;
                var b = Convert.FromBase64String(p_);
                var p = Encoding.UTF8.GetString(b);
                var valid = p.Contains("id:") && p.Contains("target:") && p.Contains("ts:");
                DebugEx.Info($"deeplink payload content:{p} valid:{valid}");
                if (!valid) return;
                using var _ = PoolMappingAccess.Borrow<Dictionary<string, string>>(out var map);
                M(p, map);
                var ts = long.Parse(map["ts"]);
                var target = int.Parse(map["target"]);
                var start = long.Parse(map["tss"]);
                var now = Game.TimestampNow();
                var createTS = account.createAt;
                var level = Game.Manager.mergeLevelMan.level;
                if (check_ && (ts > createTS || (now - createTS) > 48 * 3600 || level > 1)) {
                    DebugEx.Info($"deeplink payload received by old account");
                    return;
                }
                var fullfill = account.TryGetClientStorage(nameof(AccountMan.Referrer), out var _);
                var id = map["id"];
                var ei = int.Parse(map["ei"]);
                var ef = int.Parse(map["ef"]);
                var ep = int.Parse(map["ep"]);
                var st = int.Parse(map["st"]);
                var referrer = new AccountMan.Referrer(id, target, start, ei, ef, ep, st, fullfill);
                DebugEx.Info($"deeplink referer {referrer}");
                WhenComplete_?.Invoke(referrer);
            }
            catch (Exception e) {
                var msg = $"deeplink payload malformed:{p_}\n{e.Message}\n{e.StackTrace}";
                if (check_) DebugEx.Error(msg);
                else DebugEx.Warning(msg);
            }
        }

        public void LinkPayload(Action<AccountMan.Referrer> WhenComplete_) {
            var account = Game.Manager.accountMan;
            var sdk = PlatformSDK.Instance.Adapter;
            sdk.LinkPayload((p_, r_, e_) => {
                DebugEx.Info($"deeplink payload:{p_} result:{r_} error:{e_}");
                if (!r_) return;
                ProcessPayload(p_, true, WhenComplete_);
            });
        }
    }
}