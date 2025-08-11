using System;
using System.Collections;
using System.Diagnostics;

namespace FAT {
    public class BlockToken {
        public bool block;
        public bool wait;

        public void Enter(bool block_ = true, bool wait_ = true) {
            var ui = UIManager.Instance;
            if (block_ && !block) ui.Block(true);
            if (wait_) ui.OpenWindow(UIConfig.UIWait);
            block = block_;
            wait = wait_;
        }

        public void Exit() {
            var ui = UIManager.Instance;
            if (block) ui.Block(false);
            if (wait) ui.CloseWindow(UIConfig.UIWait);
            block = false;
            wait = false;
        }
    }
}