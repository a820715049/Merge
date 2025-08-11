/**
 * @Author: handong.liu
 * @Date: 2021-09-29 20:35:56
 */
using System.Collections.Generic;
using Cysharp.Text;
using EL;

namespace FAT.Merge
{
    public class MergeWorldTracer
    {
        public MergeWorld world => mWorld;  //当前绑定追踪的合成世界实体
        private MergeWorld mWorld;
        //当前合成世界中 棋盘上的活跃物品数量
        private System.Action mOnBoardItemDirty;
        // 目前背包改变都伴随着board改变
        // 简单起见 这两个cache共用一个刷新逻辑
        private Dictionary<int, int> mActiveBoardAndInventoryItemCount = new();
        private Dictionary<int, int> mActiveBoardItemCount = new Dictionary<int, int>();
        private bool mActiveBoardItemDirty;
        //当前合成世界中 所有的活跃物品数量
        private System.Action mOnItemDirty;
        private Dictionary<int, int> mActiveItemCount = new Dictionary<int, int>();
        private bool mActiveItemDirty;

        //创建tracer时外部决定是否监听物品数量变化
        public MergeWorldTracer(System.Action boardItemDirtyCB, System.Action itemDirtyCB)
        {
            mOnBoardItemDirty = boardItemDirtyCB;
            mOnItemDirty = itemDirtyCB;
        }

        public void Invalidate()
        {
            _SetBoardActiveItemsDirty();
        }

        //获取棋盘上活跃物品数量
        public IDictionary<int, int> GetCurrentActiveBoardItemCount()
        {
            if (mActiveBoardItemDirty)
            {
                _UpdateActiveBoardItemCount();
            }
            return mActiveBoardItemCount;
        }

        // 获取棋盘以及inventory里的活跃物品数量
        public IDictionary<int, int> GetCurrentActiveBoardAndInventoryItemCount()
        {
            if (mActiveBoardItemDirty)
            {
                _UpdateActiveBoardItemCount();
            }
            return mActiveBoardAndInventoryItemCount;
        }

        //获取整个世界活跃物品数量
        public IDictionary<int, int> GetCurrentActiveItemCount()
        {
            if (mActiveItemDirty)
            {
                _UpdateActiveItemCount();
            }
            return mActiveItemCount;
        }

        public void DebugBoardItemCount()
        {
            DebugInfo("board item count");
        }


        public void DebugAllItemCount()
        {
            DebugInfo("all item count");
        }

        private void DebugInfo(string tag)
        {
            var list = GetCurrentActiveBoardItemCount().ToList();
            list.Sort((a, b) => a.Key - b.Key);

            using var sb = ZString.CreateStringBuilder();
            sb.AppendFormat("[BOARDDEBUG] {0}:", tag);
            sb.AppendLine();
            var idx = 0;
            foreach (var item in list)
            {
                sb.AppendFormat("{0}_{1},", item.Key, item.Value);
                ++idx;
                if (idx % 5 == 0)
                {
                    sb.AppendLine();
                }
            }
#if UNITY_EDITOR
            DebugEx.Info(sb.ToString());
#endif
            DataTracker.TrackLogInfo(sb.ToString());
        }

        private void _UpdateActiveBoardItemCount()
        {
            mActiveBoardItemDirty = false;
            mActiveBoardItemCount.Clear();
            mActiveBoardAndInventoryItemCount.Clear();
            mWorld.activeBoard.WalkAllItem(_UpdateActiveCountFunc_Board);
            mWorld.inventory.WalkAllItem(_UpdateActiveCountFunc_Inventory);
        }

        private void _UpdateActiveCountFunc_Board(Item item)
        {
            if (Merge.ItemUtility.CanUseInTracer(item))
            {
                mActiveBoardItemCount[item.tid] = mActiveBoardItemCount.GetDefault(item.tid, 0) + 1;
                mActiveBoardAndInventoryItemCount[item.tid] = mActiveBoardAndInventoryItemCount.GetDefault(item.tid, 0) + 1;
            }
        }

        private void _UpdateActiveCountFunc_Inventory(Item item)
        {
            if (!item.isDead && item.isActive)
            {
                mActiveBoardAndInventoryItemCount[item.tid] = mActiveBoardAndInventoryItemCount.GetDefault(item.tid, 0) + 1;
            }
        }

        private void _UpdateActiveCountFunc_All(Item item)
        {
            if (Merge.ItemUtility.CanUseInTracer(item))
            {
                mActiveItemCount[item.tid] = mActiveItemCount.GetDefault(item.tid, 0) + 1;
            }
        }

        private void _UpdateActiveItemCount()
        {
            mActiveItemDirty = false;
            mActiveItemCount.Clear();
            mWorld.WalkAllItem(_UpdateActiveCountFunc_All);
        }

        private void _SetBoardActiveItemsDirty()
        {
            mActiveBoardItemDirty = true;
            mOnBoardItemDirty?.Invoke();
            _SetActiveItemsDirty();
        }

        private void _SetActiveItemsDirty()
        {
            mActiveItemDirty = true;
            mOnItemDirty?.Invoke();
        }

        public void Bind(MergeWorld w)
        {
            mWorld = w;
            if (mWorld != null)
            {
                mWorld.activeBoard.onItemEnter += _OnItemEnter;
                mWorld.activeBoard.onItemLeave += _OnItemLeave;
                mWorld.activeBoard.onItemStateChange += _OnItemRefresh;
                mWorld.onRewardListChange += _OnRewardRefresh;
                mWorld.onItemEvent += _OnItemEvent;

                mActiveItemCount.Clear();
                mActiveBoardItemCount.Clear();
                mActiveBoardAndInventoryItemCount.Clear();

                mActiveBoardItemDirty = true;
                mActiveItemDirty = true;
            }
        }

        private void _OnRewardRefresh(bool isAdd)
        {
            _SetActiveItemsDirty();
        }

        private void _OnItemEnter(Item item)
        {
            _OnItemRefresh(item);
        }

        private void _OnItemLeave(Item item)
        {
            _OnItemRefresh(item);
        }

        private void _OnItemEvent(Item item, ItemEventType eventType)
        {
            if (eventType == ItemEventType.ItemEventInventoryConsumeForOrder)
            {
                // 订单直接从背包收走物品时也触发boardItem数量刷新
                // 因为需要重新统计能提交订单的棋子数量 而其他方式改变背包都会走棋盘
                // TODO: 应该有独立的inventory改变事件
                _SetBoardActiveItemsDirty();
            }
        }

        private void _OnItemRefresh(Item item, ItemStateChangeContext context = null)
        {
            _SetBoardActiveItemsDirty();
        }
    }
}