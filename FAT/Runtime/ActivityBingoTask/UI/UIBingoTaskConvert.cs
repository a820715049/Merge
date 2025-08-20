// ==================================================
// // File: UIBingoTaskConvert.cs
// // Author: liyueran
// // Date: 2025-07-18 14:07:00
// // Desc: BingoTask 结算
// // ==================================================

using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIBingoTaskConvert : UIBase
    {
        // Text字段
        private TextMeshProUGUI _count;

        private PoolMapping.Ref<List<RewardCommitData>> list;
        private ActivityBingoTask _activity;


        protected override void OnCreate()
        {
            // Object绑定
            transform.AddButton("Content/root/confirm", ConfirmClick);
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as ActivityBingoTask;
        }


        private void ConfirmClick()
        {
            Close();
        }

        protected override void OnPostClose()
        {
        }
    }
}