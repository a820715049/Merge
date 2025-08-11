/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏-选关界面 scroll
 * @Date: 2024-10-05 11:35:48
 */

using UnityEngine;

namespace MiniGame
{
    public class UIBeadsSelectScrollRect : UICommonScrollGrid<(int, int), UICommonScrollGridDefaultContext>
    {
        class CellGroup : DefaultCellGroup { }
        [SerializeField] UIBeadsSelectCell cellPrefab = default;
        protected override void SetupCellTemplate() => Setup<CellGroup>(cellPrefab);
    }
}