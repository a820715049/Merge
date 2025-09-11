/**
 * @Author: handong.liu
 * @Date: 2023-02-17 10:30:11
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public class BubbleMergeBonusHandler : IMergeBonusHandler
    {
        int IMergeBonusHandler.priority => 102; //泡泡在冰冻棋子后面生成
        void IMergeBonusHandler.Process(Merge.MergeBonusContext context)
        {
            var world = context.world;
            var result = context.result;
            bool isTutorial = false;
            // if (!Env.Instance.IsBubbleGuidePassed() && target == Env.Instance.GetGlobalConfig().BubbleGuideItemId)
            // {
            //     var cate = Env.Instance.GetCategoryByItem(target);
            //     isTutorial = world.GetItemShowCountInCategory(cate.Id) < ItemUtility.GetItemLevel(target);
            // }
            var bubble = _CheckSpawnBubble(context.boardGrids, result, isTutorial);
        }
        void IMergeBonusHandler.OnRegister()
        {

        }

        void IMergeBonusHandler.OnUnRegister()
        {
            
        }

        private Item _CheckSpawnBubble(MergeGrid[] mGrids, Item srcItem, bool isTutorial)
        {
            if (!Env.Instance.IsFeatureEnable(MergeFeatureType.Bubble))
            {
                DebugEx.FormatInfo("Merge::Board::_CheckSpawnBubble ----> bubble feature not open!");
                return null;
            }
            // if (!isTutorial && !Env.Instance.IsBubbleGuidePassed())          //early exit if bubble tutorial not finished
            // {
            //     DebugEx.FormatInfo("Merge::Board::_CheckSpawnBubble ----> not spawn bubble because guide not passed!");
            //     return null;
            // }
            int targetId = srcItem.tid;
            var itemMergeConfig = Env.Instance.GetItemMergeConfig(targetId);
            int prob = itemMergeConfig.BubbleProb;
            string spawnTypeStr = "normal";

            // 是否有概率
            if (!(prob > 0 && EL.MathUtility.ThrowDiceProbPercent(prob)))
            {
                return null;
            }

            // 同id气泡只能有一个
            int bubbleCount = 0;
            foreach (var grid in mGrids)
            {
                if (grid.item != null && ItemUtility.IsBubbleItem(grid.item))
                {
                    ++bubbleCount;
                    if (srcItem.tid == grid.item.tid)
                    {
                        DebugEx.Info($"Merge::Board::_CheckSpawnBubble ----> because item {grid.item} ({grid.item.coord}) is in bubble, won't generate {targetId}");
                        return null;
                    }
                }
            }

            // bubble总数不能超过max
            if (bubbleCount >= Game.Manager.configMan.globalConfig.BubbleMaxNum)
            {
                DebugEx.Info($"Merge::Board::_CheckSpawnBubble ----> too many bubble ({bubbleCount}), won't generate {targetId}");
                return null;
            }

            DebugEx.FormatInfo("Merge::Board::_CheckSpawnBubble ----> bubble {0} created for item {1}", targetId, srcItem.tid);
            return srcItem.parent.TrySpawnBubbleItem(srcItem, targetId, spawnTypeStr);
        }
    }
}