using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using EL;
using static fat.conf.Data;

namespace FAT {
    public class InviteProgress : RewardBar {
        public void Refresh(ActivityInvite acti_) {
            var list = acti_.Node;
            var value = acti_.value;
            var next = Next(list, value);
            RefreshList(list, value, next);
        }
    }
}