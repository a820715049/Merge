using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace FAT {
    public class WaitToken {
        public bool wait;

        public void Reset() => wait = true;
        public void Cancel() => wait = false;

        public WaitToken() {
            Reset();
        }

        public IEnumerator Wait(float s_, Stopwatch sw_) {
            var n = this;
            if (!n.wait) yield break;
            var ui = UIManager.Instance;
            sw_.Restart();
            ui.OpenWindow(UIConfig.UIWait);
            ui.Block(true);
            while (n.wait && sw_.ElapsedMilliseconds < s_ * 1000) yield return null;
            ui.CloseWindow(UIConfig.UIWait);
            ui.Block(false);
        }

        public IEnumerator Wait(bool ui_ = true, bool block_ = true) {
            var n = this;
            if (!n.wait) yield break;
            var ui = UIManager.Instance;
            if (ui_) ui.OpenWindow(UIConfig.UIWait);
            if (block_) ui.Block(true);
            while (n.wait) yield return null;
            if (ui_) ui.CloseWindow(UIConfig.UIWait);
            if (block_) ui.Block(false);
        }

        public async UniTask Wait(int i_, int m_ = 0, bool ui_ = true, bool block_ = true) {
            var n = this;
            if (!n.wait) return;
            var ui = UIManager.Instance;
            if (ui_) ui.OpenWindow(UIConfig.UIWait);
            if (block_) ui.Block(true);
            var t = 0;
            while (n.wait && (m_ <= 0 || t < m_)) {
                t += i_;
                await Task.Delay(i_);
            }
            if (ui_) ui.CloseWindow(UIConfig.UIWait);
            if (block_) ui.Block(false);
        }

        public bool Timeout(string msg_ = null) {
            if (!wait) return false;
            Game.Manager.commonTipsMan.ShowClientTips(EL.I18N.Text(msg_ ?? "#Timeout"));
            return true;
        }
    }
}