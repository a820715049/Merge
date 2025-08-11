using System;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UICardExchangeContext : FancyGridViewContext
    {
        public int SelectedID = -1;
        public Action<int> OnCellClicked;
        public Action<int> OnSelectNumChange;
    }
}