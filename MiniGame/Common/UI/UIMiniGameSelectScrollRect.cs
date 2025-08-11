/*
 * @Author: qun.chao
 * @Date: 2025-04-28 12:30:49
 */

using UnityEngine;

namespace MiniGame
{
    public class UIMiniGameSelectScrollRect : UICommonScrollGrid<(int, int), UICommonScrollGridDefaultContext>
    {
        class CellGroup : DefaultCellGroup { }
        [SerializeField] UIMiniGameSelectCell cellPrefab = default;
        protected override void SetupCellTemplate() => Setup<CellGroup>(cellPrefab);
    }
}