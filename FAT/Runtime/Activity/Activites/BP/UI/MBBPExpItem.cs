 // ================================================
// File: MBBPExpItem.cs
// Author: yueran.li
// Date: 2025/07/10 14:40:18 星期四
// Desc: 界面经验值item显示
// ================================================


using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class MBBPExpItem : MonoBehaviour
    {
        public UIImageRes tokenImg;

        private void OnEnable()
        {
            if (Game.Manager.activity.LookupAny(EventType.Bp, out var activity) && activity is BPActivity bp)
            {
                var token = fat.conf.Data.GetObjBasic(bp.ConfD.ScoreId);
                tokenImg.SetImage(token.Icon);
            }
        }
    }
}
