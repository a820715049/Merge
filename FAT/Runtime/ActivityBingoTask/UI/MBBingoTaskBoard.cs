// ==================================================
// // File: MBBingoTaskBoard.cs
// // Author: liyueran
// // Date: 2025-07-16 16:07:34
// // Desc: bingoTask 棋盘
// // ==================================================

using System;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT.MSG;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class MBBingoTaskBoard : UIModuleBase
    {
        private GameObject _itemPrefab;
        private GameObject _effPrefab;

        private UIBingoTaskMain _main;
        private RectTransform _itemRoot;
        private RectTransform _effectRoot;

        private ActivityBingoTask _activity;

        public MBBingoTaskBoard(Transform root) : base(root)
        {
        }

        private string pool_key_item => $"bingo_task_board_item_{_activity.Id}";
        private string pool_key_item_eff => $"bingo_task_item_eff_{_activity.Id}";

        private Dictionary<int, MBBingoTaskItem> _itemMap = new();
        public Dictionary<int, MBBingoTaskItem> ItemMap => _itemMap;

        public UIBingoTaskMain Main
        {
            get
            {
                if (_main != null)
                {
                    return _main;
                }

                var ui = UIManager.Instance.TryGetUI(_activity.MainPopUp.res.ActiveR);
                if (ui != null && ui is UIBingoTaskMain main)
                {
                    return main;
                }

                return null;
            }
        }

        #region module
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            ModuleRoot.Access<RectTransform>("itemRoot", out _itemRoot);
            ModuleRoot.Access<RectTransform>("effectRoot", out _effectRoot);

            _itemPrefab = ModuleRoot.Find("itemRoot").GetChild(0).gameObject;
            _effPrefab = ModuleRoot.Find("effectRoot").GetChild(0).gameObject;
        }

        private void AddButton()
        {
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (ActivityBingoTask)items[0];
            _main = (UIBingoTaskMain)items[1];
        }

        protected override void OnShow()
        {
            EnsurePool();

            // index排序
            var indexList = _activity.bingoTaskMap.bingoDic.Keys.ToList();
            indexList.Sort();

            // 初始化棋盘
            foreach (var index in indexList)
            {
                if (_activity.bingoTaskMap.TryGetBingoTaskCell(index, out var cell))
                {
                    var obj = GameObjectPoolManager.Instance.CreateObject(pool_key_item, _itemRoot);
                    obj.transform.localScale = Vector3.one;

                    var item = obj.GetComponent<MBBingoTaskItem>();
                    item.Init(_activity, this, cell);
                    obj.SetActive(true);
                    _itemMap.Add(index, item);
                }
            }
        }

        private void EnsurePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(pool_key_item))
                return;
            GameObjectPoolManager.Instance.PreparePool(pool_key_item, _itemPrefab);
            GameObjectPoolManager.Instance.PreparePool(pool_key_item_eff, _effPrefab);
        }

        public void OnPostOpen()
        {
            foreach (var item in _itemMap.Values)
            {
                item.OnPostOpen();
            }
        }

        protected override void OnHide()
        {
            foreach (var item in _itemMap.Values)
            {
                GameObjectPoolManager.Instance.ReleaseObject(pool_key_item, item.gameObject);
            }

            _itemMap.Clear();
            
            // 清理特效
            for (var i = _effectRoot.childCount - 1; i >= 0; --i)
            {
                var item = _effectRoot.GetChild(i);
                GameObjectPoolManager.Instance.ReleaseObject(pool_key_item_eff, item.gameObject);
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<UI_BINGO_TASK_COMPLETE_ITEM>().AddListener(OnBingoTaskComplete);
            MessageCenter.Get<BINGO_TASK_QUIT_SPECIAL>().AddListener(OnBingoTaskUnlock);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<UI_BINGO_TASK_COMPLETE_ITEM>().RemoveListener(OnBingoTaskComplete);
            MessageCenter.Get<BINGO_TASK_QUIT_SPECIAL>().RemoveListener(OnBingoTaskUnlock);
        }

        protected override void OnAddDynamicListener()
        {
        }

        protected override void OnRemoveDynamicListener()
        {
        }

        protected override void OnClose()
        {
        }
        #endregion


        #region Effect
        private void Effect_Glow(Vector3 pos)
        {
            var obj = GameObjectPoolManager.Instance.CreateObject(pool_key_item_eff, _effectRoot);
            obj.transform.localScale = Vector3.one;
            obj.transform.position = pos;

            var eff = obj.GetComponent<MBBingoTaskEffect>();
            eff.glow.SetActive(true);
            eff.coverBroke.SetActive(false);
            eff.coverClick.SetActive(false);

            obj.SetActive(true);
            
            Game.Manager.audioMan.TriggerSound("BingoTaskGrid");

            obj.GetOrAddComponent<MBAutoRelease>().Setup(pool_key_item_eff, 2f);
        }

        private void Effect_CoverBroke(MBBingoTaskItem unlockItem)
        {
            var seq = DOTween.Sequence();
            seq.AppendInterval(Main.brokeWaitTime);
            seq.AppendCallback(() =>
            {
                unlockItem.RefreshView();
                CoverBroke(unlockItem.transform.position);
            });
            seq.Play();
        }

        private void CoverBroke(Vector3 pos)
        {
            var obj = GameObjectPoolManager.Instance.CreateObject(pool_key_item_eff, _effectRoot);
            obj.transform.localScale = Vector3.one;
            obj.transform.position = pos;

            var eff = obj.GetComponent<MBBingoTaskEffect>();
            eff.glow.SetActive(false);
            eff.coverBroke.SetActive(true);
            eff.coverClick.SetActive(false);

            obj.SetActive(true);
            obj.GetOrAddComponent<MBAutoRelease>().Setup(pool_key_item_eff, 2f);
        }

        public void Effect_CoverClick(Vector3 pos)
        {
            var obj = GameObjectPoolManager.Instance.CreateObject(pool_key_item_eff, _effectRoot);
            obj.transform.localScale = Vector3.one;
            obj.transform.position = pos;

            var eff = obj.GetComponent<MBBingoTaskEffect>();
            eff.glow.SetActive(false);
            eff.coverBroke.SetActive(false);
            eff.coverClick.SetActive(true);

            obj.SetActive(true);
            obj.GetOrAddComponent<MBAutoRelease>().Setup(pool_key_item_eff, 2f);
        }
        #endregion

        private Sequence _bingoSeq;

        #region 事件
        private void OnBingoTaskUnlock(int index)
        {
            if (_itemMap.TryGetValue(index, out var unlockItem))
            {
                Game.Manager.audioMan.TriggerSound("BingoTaskUnlock");
                Effect_CoverBroke(unlockItem);
            }
        }

        private void OnBingoTaskComplete(BingoResult result, int index)
        {
            if (result == BingoResult.None)
            {
                return;
            }

            _bingoSeq?.Kill();
            _bingoSeq = DOTween.Sequence();
            _bingoSeq.AppendCallback(() => { Main.SetBlock(true); });

            // 全部bingo
            if (_activity.WhetherAllBingo())
            {
                var allList = new List<int>();
                allList.AddRange(_itemMap.Keys);
                var sortDict = SortByIndex(index, allList);
                var seq = ListBingoAnim(sortDict);
                _bingoSeq.Join(seq);
                _bingoSeq.OnComplete(() =>
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIBingoTaskBingo, _activity, result);
                });
            }
            // 只完成自己 不涉及bingo
            else if (result == BingoResult.Completed)
            {
                if (_itemMap.TryGetValue(index, out var item))
                {
                    item.PlayCommitAnim(_bingoSeq, () => { Effect_Glow(item.transform.position); });
                }
            }
            // 一行bingo
            else
            {
                // 横行
                if (result.HasFlag(BingoResult.RowBingo))
                {
                    var rowList = new List<int>();
                    _activity.FillSameRowCellList(index, rowList);
                    var sortDict = SortByIndex(index, rowList);
                    var seq = ListBingoAnim(sortDict);

                    _bingoSeq.Join(seq);
                }

                // 纵行
                if (result.HasFlag(BingoResult.ColumnBingo))
                {
                    var columnList = new List<int>();
                    _activity.FillSameColumnCellList(index, columnList);
                    var sortDict = SortByIndex(index, columnList);
                    var seq = ListBingoAnim(sortDict);

                    _bingoSeq.Join(seq);
                }

                // 主对角 左下到右上
                if (result.HasFlag(BingoResult.MainDiagonalBingo))
                {
                    var mainList = new List<int>();
                    _activity.FillMainDiagonalCellList(index, mainList);
                    var sortDict = SortByIndex(index, mainList);
                    var seq = ListBingoAnim(sortDict);

                    _bingoSeq.Join(seq);
                }

                // 副对角 右下到左上
                if (result.HasFlag(BingoResult.AntiDiagonalBingo))
                {
                    var antiList = new List<int>();
                    _activity.FillAntiDiagonalCellList(index, antiList);
                    var sortDict = SortByIndex(index, antiList);
                    var seq = ListBingoAnim(sortDict);

                    _bingoSeq.Join(seq);
                }

                _bingoSeq.OnComplete(() =>
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIBingoTaskBingo, _activity, result);
                });
            }

            _bingoSeq.OnKill(() => { Main.SetBlock(false); });
            _bingoSeq.Play();
        }
        #endregion

        #region 动画控制
        private Sequence ListBingoAnim(Dictionary<int, List<int>> sortDict)
        {
            var seq = DOTween.Sequence();

            // 根据index的差 差越小越先播放
            var keys = sortDict.Keys.ToList();
            keys.Sort();

            foreach (var diff in keys)
            {
                var sameDiffSeq = DOTween.Sequence();
                foreach (var index in sortDict[diff])
                {
                    // 差相同的同时播放
                    if (_itemMap.TryGetValue(index, out var item))
                    {
                        var bingoSeq = item.PlayBingoAnim(
                            null, () => { Effect_Glow(item.transform.position); },
                            0f, Main.bingoSpreadTime);
                        sameDiffSeq.Join(bingoSeq);
                    }
                }

                seq.Append(sameDiffSeq);
            }

            return seq;
        }
        #endregion

        // 获取坐标
        private (int, int) GetCoord(int index)
        {
            var row = (index) / 4; // 0-3行
            var col = (index) % 4; // 0-3列
            return (row, col);
        }

        // 根据格子距离的差 排序
        private Dictionary<int, List<int>> SortByIndex(int index, List<int> list)
        {
            var sortDict = new Dictionary<int, List<int>>();
            // 根据index,计算格子距离的差 差相同的放在一起
            foreach (var item in list)
            {
                var (row, col) = GetCoord(item);
                var (row2, col2) = GetCoord(index);
                var diff = Math.Abs(row - row2) + Math.Abs(col - col2);
                if (sortDict.ContainsKey(diff))
                {
                    sortDict[diff].Add(item);
                }
                else
                {
                    sortDict.Add(diff, new List<int> { item });
                }
            }

            return sortDict;
        }
    }
}