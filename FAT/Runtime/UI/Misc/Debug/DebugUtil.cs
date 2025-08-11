using System;
using System.Collections.Generic;
using System.Reflection;
using Config;
using EL;
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    /// <summary>
    /// 程序开发Debug工具 if分支内代码勿传 方便本地使用
    /// </summary>
    public static class DebugUtil
    {
        private static int a = 5;

#if UNITY_EDITOR
        public static void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Game.Manager.activity.LookupAny(fat.rawdata.EventType.ZeroQuest, out var activity);
                if (activity == null)
                {
                    return;
                }
                var activityTreasure = (ActivityOrderChallenge)activity;
                activityTreasure.Challenge();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                MessageCenter.Get<MSG.GAME_CARD_DRAW_FINISH>().Dispatch();
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                Game.Manager.activity.LookupAny(fat.rawdata.EventType.Digging, out var activity);
                if (activity == null)
                {
                    return;
                }

                var activityDigging = (ActivityDigging)activity;
                var field = typeof(ActivityDigging).GetField("currentLevelIndex", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(activityDigging, activityDigging.detailConfig.Includelevel.Count - 1);
                }
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                UIManager.Instance.OpenWindow(UIConfig.UIDebugPanelProMax);
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                var mergeBoardMan = Game.Manager.mergeBoardMan;
                var detailParam = Game.Manager.mergeBoardMan
                    .GetBoardConfig(mergeBoardMan.activeWorld.activeBoard.boardId)?.DetailParam ?? 0;
                using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
                {
                    // 获得从第8个索引开始 之后两行的棋子数据
                    if (Game.Manager.mineBoardMan.FillBoardRowConfStr(detailParam, rowItems, 8, 2))
                    {
                        // 从第二行开始 向上生成两行棋子
                        mergeBoardMan.CreateNewBoardItemFromRowToTop(mergeBoardMan.activeWorld.activeBoard, rowItems, 2);
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.F6))
            {
                // 棋盘内棋子向下移动
                var mergeBoardMan = Game.Manager.mergeBoardMan;
                mergeBoardMan.MoveDownBoardItem(mergeBoardMan.activeWorld.activeBoard, 2);
                BoardViewManager.Instance.boardView.boardHolder.ReFillItem();
            }
        }
#endif
    }

    //棋盘快速合成工具类
    public static class BoardQuickMergeTool
    {
        private static List<Item> _cacheItemList = new List<Item>();
        private static List<Tuple<Item, Item>> _canMergeItemList = new List<Tuple<Item, Item>>();
        
        private enum MergeStep
        {
            None,           //空状态
            CollectItems,   //收集当前棋盘上所有符合条件的棋子并排序
            FindMergePairs, //找出所有可以合成的棋子对
            ExecuteMerge    //执行合成操作
        }
        private static bool _isInAutoMerge = false;         // 是否处于自动合成状态，由外部控制
        private static int _mergeStepDelayFrame = 3;        // 每步之间的延迟帧数，可配置
        private static int _mergeStepFrameCounter = 0;      // 当前帧计数器
        private static MergeStep _currentMergeStep = MergeStep.None; // 当前状态阶段

        //只执行一次 此操作会停止auto merge
        public static void QuickMergeOnce()
        {
            Reset();
            _AutoCollectItems();
            _AutoFindMergePairs();
            _AutoExecuteMerge();
        }

        public static bool SwitchAutoQuickMerge()
        {
            Reset(!_isInAutoMerge);
            return _isInAutoMerge;
        }
        
        public static void Update()
        {
            if (!_isInAutoMerge)
                return;

            _mergeStepFrameCounter++;
            if (_mergeStepFrameCounter < _mergeStepDelayFrame)
                return;

            _mergeStepFrameCounter = 0;

            switch (_currentMergeStep)
            {
                case MergeStep.None:
                    _currentMergeStep = MergeStep.CollectItems;
                    break;

                case MergeStep.CollectItems:
                    _AutoCollectItems();
                    _currentMergeStep = MergeStep.FindMergePairs;
                    break;

                case MergeStep.FindMergePairs:
                    _AutoFindMergePairs();
                    _currentMergeStep = MergeStep.ExecuteMerge;
                    break;

                case MergeStep.ExecuteMerge:
                    _AutoExecuteMerge();
                    _currentMergeStep = MergeStep.CollectItems;
                    break;
            }
        }

        public static void Reset(bool isOpen = false)
        {
            _isInAutoMerge = isOpen;
            _mergeStepFrameCounter = 0;
            _currentMergeStep = MergeStep.None;
            _cacheItemList.Clear();
            _canMergeItemList.Clear();
        }
        
        private static void _AutoCollectItems()
        {
            var curBoard = Game.Manager.mergeBoardMan.activeWorld?.activeBoard;
            if (curBoard == null)
                return;
            
            _cacheItemList.Clear();

            curBoard.WalkAllItem((item) =>
            {
                if (item.parent != null &&
                    !item.isDead &&
                    !item.HasComponent(ItemComponentType.Bubble))
                {
                    _cacheItemList.Add(item);
                }
            });
            //排个序 保证isActive为false的棋子在前面，尽量模拟玩家会先处理状态为蜘蛛网的棋子
            _cacheItemList.Sort((a, b) => a.isActive.CompareTo(b.isActive));
        }

        private static void _AutoFindMergePairs()
        {
            _canMergeItemList.Clear();

            if (_cacheItemList.Count <= 0)
                return;
            var curBoard = Game.Manager.mergeBoardMan.activeWorld?.activeBoard;
            if (curBoard == null)
                return;

            while (true)
            {
                bool foundPair = false;
                for (int i = 0; i < _cacheItemList.Count; i++)
                {
                    var first = _cacheItemList[i];
                    for (int j = i + 1; j < _cacheItemList.Count; j++)
                    {
                        var second = _cacheItemList[j];
                        //当前查找的棋子分别作为主棋子和副棋子进行检查，看看是否能合成，这里主要考虑其中一个棋子是蜘蛛网的情况，主棋子不能是蜘蛛网
                        if (ItemUtility.CanMerge(first, second))
                        {
                            _canMergeItemList.Add(Tuple.Create(first, second));
                            _cacheItemList.Remove(first);
                            _cacheItemList.Remove(second);
                            foundPair = true;
                            break;
                        }
                        else if (ItemUtility.CanMerge(second, first))
                        {
                            _canMergeItemList.Add(Tuple.Create(second, first));
                            _cacheItemList.Remove(second);
                            _cacheItemList.Remove(first);
                            foundPair = true;
                            break;
                        }
                    }
                    if (foundPair)
                        break;
                }
                if (!foundPair)
                    break;
            }
        }

        private static void _AutoExecuteMerge()
        {
            if (_canMergeItemList.Count <= 0)
                return;
            
            var curBoard = Game.Manager.mergeBoardMan.activeWorld?.activeBoard;
            if (curBoard == null)
                return;

            foreach (var pair in _canMergeItemList)
            {
                curBoard.Merge(pair.Item1, pair.Item2);
            }

            _canMergeItemList.Clear();
        }

    }
}