/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡册界面 卡组scroll
 * @Date: 2024-01-25 15:01:11
 */

using System;
using UnityEngine;

namespace FAT
{
    public class UICardGroupScrollRect : UICommonScrollGrid<int, UICommonScrollGridDefaultContext>
    {
        class CellGroup : DefaultCellGroup { }
        [SerializeField] UICardGroupCell cellPrefab = default;
        protected override void SetupCellTemplate() => Setup<CellGroup>(cellPrefab);
        
        public void SetupCellClickCb(Action<int> cb)
        {
            if (cellPrefab != null)
            {
                cellPrefab.SetupCellClickCb(cb);
            }
        }
    }
}