/*
 * @Author: tang.yan
 * @Description: 商城界面钻石页签scroll 
 * @Date: 2023-11-07 15:11:46
 */

using UnityEngine;

namespace FAT
{
    public class UIShopGemScrollRect : UICommonScrollGrid<ShopGemData, UICommonScrollGridDefaultContext>
    {
        class CellGroup : DefaultCellGroup { }
        [SerializeField] UIShopGemCell cellPrefab = default;
        protected override void SetupCellTemplate() => Setup<CellGroup>(cellPrefab);
    }
}