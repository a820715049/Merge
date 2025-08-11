/*
 * @Author: qun.chao
 * @Date: 2021-05-27 18:42:40
 */
namespace FAT
{
    using System.Collections.Generic;
    using UnityEngine;
    using Config;
    using Merge;

    public class MergeHelper
    {
        public enum MergeAction
        {
            None,
            Merge,
            Consume,
            Mix,
            Inventory,
            Feed,
            Skill,
            Stack,  // 堆叠
            Custom, // 业务自己处理
        }

        public class AffectedCell
        {
            public int idx;
            public int col;
            public int row;
            public bool valid;
            public float squaredDist;
            public Vector2 screenPos;
        }

        private List<AffectedCell> _affectedCellList;
        private List<AffectedCell> affectedCellList
        {
            get
            {
                if (_affectedCellList == null)
                {
                    _affectedCellList = new List<AffectedCell>();
                    for (int i = 0; i < 4; ++i)
                    {
                        _affectedCellList.Add(new AffectedCell());
                    }
                }
                return _affectedCellList;
            }
        }

        private int width;
        private int height;

        public (Item item, MergeAction act) CheckDragBehaviour(Vector2 pos, Item itemInDrag)
        {
            var bagTriggerDist = BoardUtility.screenCellSize * 0.5;
            var bagSqrDist = bagTriggerDist * bagTriggerDist;
            var bagCoord = BoardViewManager.Instance.inventoryEntryScreenPos;
            var bagValid = Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(fat.rawdata.FeatureEntry.FeatureBagItem) &&
                BoardViewWrapper.IsMainBoard();
            var board = BoardViewManager.Instance.board;
            width = board.size.x;
            height = board.size.y;

            _FillAffectedArea(pos);

            var isSkill = itemInDrag.HasComponent(ItemComponentType.Skill);

            for (int i = 0; i < affectedCellList.Count; ++i)
            {
                var cell = affectedCellList[i];
                if (!cell.valid)
                {
                    if (bagValid && (cell.screenPos - bagCoord).sqrMagnitude < bagSqrDist)
                    {
                        return (null, MergeAction.Inventory);
                    }
                    continue;
                }
                var tar = board.GetItemByCoord(cell.col, cell.row);
                if (tar == null)
                    continue;
                if (tar.id == itemInDrag.id)
                    continue;

                if (board.CanMerge(itemInDrag, tar))
                {
                    // merge
                    return (tar, MergeAction.Merge);
                }
                else if (ItemUtility.CanConsume(itemInDrag, tar))
                {
                    // consume
                    return (tar, MergeAction.Consume);
                }
                else if (ItemUtility.CanMix(itemInDrag, tar))
                {
                    // mix
                    return (tar, MergeAction.Mix);
                }
                else if (ItemUtility.CanFeed(itemInDrag, tar))
                {
                    // feed
                    return (tar, MergeAction.Feed);
                }
                else if (isSkill && ItemUtility.CanStack(itemInDrag, tar))
                {
                    // stack
                    return (tar, MergeAction.Stack);
                }
                else if (isSkill && ItemUtility.CanUseForTarget(itemInDrag, tar, out var state))
                {
                    // skill
                    return (tar, MergeAction.Skill);
                }
            }

            if (CustomDragBehaviourCtrl.CheckDragBehaviour(pos))
            {
                return (null,MergeAction.Custom);
            }

            return (null, MergeAction.None);
        }

        public AffectedCell GetNearestCell()
        {
            return affectedCellList[0];
        }

        private void _FillAffectedArea(Vector2 pos)
        {
            var affectRangeCoe = 0.25f;
            var cellSize = BoardUtility.screenCellSize;
            var halfCell = cellSize * 0.5f * affectRangeCoe;

            // topleft
            _FillCell(affectedCellList[0], 0, ref pos, -halfCell, halfCell);
            // topright
            _FillCell(affectedCellList[1], 1, ref pos, halfCell, halfCell);
            // bottomleft
            _FillCell(affectedCellList[2], 2, ref pos, -halfCell, -halfCell);
            // bottomright
            _FillCell(affectedCellList[3], 3, ref pos, halfCell, -halfCell);

            affectedCellList.Sort(_SortCell);
        }

        private void _FillCell(AffectedCell cell, int idx, ref Vector2 anchor, float offset_x, float offset_y)
        {
            cell.idx = idx;
            cell.screenPos.x = anchor.x + offset_x;
            cell.screenPos.y = anchor.y + offset_y;
            cell.col = Mathf.FloorToInt((cell.screenPos.x - BoardUtility.originPosInScreenSpace.x) / BoardUtility.screenCellSize);
            cell.row = -Mathf.CeilToInt((cell.screenPos.y - BoardUtility.originPosInScreenSpace.y) / BoardUtility.screenCellSize);
            cell.valid = cell.col >= 0 && cell.col < width && cell.row >= 0 && cell.row < height;
            if (cell.valid)
            {
                var halfCell = BoardUtility.screenCellSize * 0.5f;
                var dist_x = BoardUtility.originPosInScreenSpace.x + BoardUtility.screenCellSize * cell.col + halfCell - anchor.x;
                var dist_y = BoardUtility.originPosInScreenSpace.y - BoardUtility.screenCellSize * cell.row - halfCell - anchor.y;
                cell.squaredDist = dist_x * dist_x + dist_y * dist_y;
            }
        }

        private int _SortCell(AffectedCell a, AffectedCell b)
        {
            if (a.valid && !b.valid)
            {
                return -1;
            }
            else if (!a.valid && b.valid)
            {
                return 1;
            }
            else if (!a.valid && !b.valid)
            {
                return a.idx - b.idx;
            }
            else
            {
                if (a.squaredDist > b.squaredDist)
                    return 1;
                else if (a.squaredDist < b.squaredDist)
                    return -1;
                else
                    return 0;
            }
        }
    }
}
