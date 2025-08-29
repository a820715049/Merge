// ==================================================
// // File: UIBingoTaskItemTips.cs
// // Author: liyueran
// // Date: 2025-07-20 20:07:21
// // Desc: bingoTask tip弹窗
// // ==================================================

using EL;
using FAT.Merge;
using FAT.MSG;
using fat.rawdata;
using TMPro;

namespace FAT
{
    public class UIBingoTaskItemTips : UITipsBase
    {
        private TextMeshProUGUI _desc;

        private BingoTaskCell _cell;

        protected override void OnCreate()
        {
            transform.Access("Content/Node/Text", out _desc);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            // items[0] Vector3 位置
            // items[1] float 偏移参数
            _SetTipsPosInfo(items);

            _cell = (BingoTaskCell)(items[2]);
        }

        protected override void OnPreOpen()
        {
            // 刷新tips位置
            _RefreshTipsPos(18);

            _desc.SetText(I18N.FormatText(_cell.taskInfo.Desc, _cell.target));
        }
    }
}