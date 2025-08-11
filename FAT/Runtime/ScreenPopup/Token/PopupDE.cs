using System;
using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT {
    public class PopupDE : PopupActivity {
        public void Setup(ActivityLike acti_, ActivityVisual visual_, UIResAlt ui_)
            => Setup(acti_, visual_, ui_, false);
    }
}