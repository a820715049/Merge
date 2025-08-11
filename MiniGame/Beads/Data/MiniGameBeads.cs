/*
 *@Author:chaoran.zhang
 *@Desc:串珠子小游戏实例类
 *@Created Time:2024.09.24 星期二 17:17:07
 */

using System;
using System.Collections.Generic;
using System.Linq;
using EL;
using FAT;
using fat.rawdata;

namespace MiniGame
{
    public class MiniGameBeads : MiniGameBase
    {
        public List<BeadBaseInfo> BeadsBases => _data.BaseInfos;
        public List<BeadGuide> BeadGuides => _data.GuideInfos;
        public UIResource GamePlayUI => UIConfig.UIBeads;
        private readonly MiniGameBeadsData _data = new();
        private BeadCellInfo _curMove; //当前正在移动的BeadCell
        private bool _hasFinish = false; //已经结束状态
        private int _step; //操作数

        public override void InitData(int index, bool isGuide, int level)
        {
            _hasFinish = false;
            IsGuide = isGuide;
            Type = MiniGameType.MiniGameBeads;
            Index = index;
            LevelID = level;
            _step = 0;
            if (LevelID < 0)
            {
                MiniGameManager.Instance.EndCurMiniGame();
                return;
            }
            _data.Init(LevelID, isGuide);
        }

        public override void DeInit()
        {
            Index = -1;
            LevelID = -1;
            _curMove = null;
            _hasFinish = false;
            _data.DeInit();
        }

        public override void OpenUI()
        {
            UIManager.Instance.OpenWindow(GamePlayUI, IsGuide);
        }

        public override void CloseUI()
        {
            UIManager.Instance.CloseWindow(GamePlayUI);
        }

        /// <summary>
        /// 鼠标抬起时的操作，检测是否可以放到制定位置
        /// </summary>
        /// <param name="baseIndex">鼠标释放时的X坐标</param>
        /// <param name="cellIndex">鼠标释放时的Y坐标</param>
        /// <param name="x">如果可以放置，正在移动的珠子中第一个珠子的目标坐标的X值，否则为移动前位置的X值</param>
        /// <param name="y">如果可以放置，正在移动的珠子中第一个珠子的目标坐标的Y值，否则为移动前位置的Y值</param>
        /// <returns>是否可以放置</returns>
        public bool OnPointerUp(int baseIndex, int cellIndex, out int x, out int y)
        {
            x = _curMove.Belong;
            y = _curMove.Index;
            if (baseIndex >= BeadsBases.Count || baseIndex < 0 || baseIndex == _curMove.Belong) return false;
            var info = BeadsBases[baseIndex];
            if (_hasFinish || info == null) return ClearCurMove();
            if (!CheckCanMove(info, cellIndex)) return ClearCurMove();
            MoveCell(baseIndex);
            x = _curMove.Belong;
            y = _curMove.Index;
            _step++;
            AfterMove();
            return true;
        }

        /// <summary>
        /// 以为各种情况无法放置时，清除当前记录的正在移动的棋子
        /// </summary>
        /// <returns></returns>
        private bool ClearCurMove()
        {
            _curMove = null;
            return false;
        }

        /// <summary>
        /// 检测是否可以放在目标BeadBase下
        /// </summary>
        /// <param name="cellInfo"></param>
        /// <returns></returns>
        private bool CheckCanMove(BeadBaseInfo baseInfo, int index)
        {
            if (baseInfo.Beads.Count > 0)
                return baseInfo.Beads[^1].Type == _curMove.Type && Math.Abs(baseInfo.Beads[^1].Index - index) <= 1;
            return baseInfo.Type == _curMove.Type && Math.Abs(index) <= 1;
        }

        /// <summary>
        /// 移动珠子到另一个链条的逻辑处理
        /// </summary>
        /// <param name="cellInfo"></param>
        private void MoveCell(int index)
        {
            var to = BeadsBases[index];
            var from = BeadsBases[_curMove.Belong];
            var list = from.Beads.Skip(_curMove.Index);
            to.Beads.AddRange(list);
            BeadsBases[_curMove.Belong].Beads.RemoveRange(_curMove.Index, list.Count());
            foreach (var bead in to.Beads)
            {
                bead.Index = to.Beads.IndexOf(bead);
                bead.Belong = index;
            }
        }

        /// <summary>
        /// 移动完成后进行的各种检测操作
        /// </summary>
        private void AfterMove()
        {
            RefreshComplete();
            CheckWin();
            CheckLose();
            _curMove = null;
        }

        /// <summary>
        /// 刷新每一个基座上的complete状态
        /// </summary>
        private void RefreshComplete()
        {
            var unComplete = BeadsBases.Where(info => !info.Complete);
            foreach (var baseInfo in unComplete)
            {
                baseInfo.Complete = _data.CellInfos.Where(cell => cell.Type == baseInfo.Type)
                                        .All(cell => cell.Belong == BeadsBases.IndexOf(baseInfo)) &&
                                    baseInfo.Beads.All(cell => cell.Type == baseInfo.Type);
                if (baseInfo.Complete)
                    MessageCenter.Get<MSG.MINIGAME_BEADS_BASE_COMPLETE>().Dispatch(BeadsBases.IndexOf(baseInfo));
            }
        }

        public override void CheckWin()
        {
            if (_hasFinish) return;
            if (_data.BaseInfos.Any(info => !info.Complete)) return;
            MiniGameManager.Instance.CompleteLevel(Type, Index);
            MessageCenter.Get<MSG.MINIGAME_RESULT>().Dispatch(Index, true);
            MiniGameManager.Instance.TrackGameResult(Type, Index, LevelID, true, _step);
            _hasFinish = true;
        }

        public override void CheckLose()
        {
            if (_hasFinish) return;
            var isAllBeadsTypesConsistent = BeadsBases.All(baseInfo =>
            {
                var firstBeadType = baseInfo.Beads.FirstOrDefault(t => t.Type != BeadsType.Default)?.Type ??
                                    BeadsType.Default;
                return baseInfo.Beads.All(t => t.Type == firstBeadType);
            });

            if (!isAllBeadsTypesConsistent) return;
            MessageCenter.Get<MSG.MINIGAME_RESULT>().Dispatch(Index, false);
            MiniGameManager.Instance.TrackGameResult(Type, Index, LevelID, false, _step);
            _hasFinish = true;
        }

        /// <summary>
        /// 检测从哪一个BeadCell开始移动
        /// </summary>
        /// <param name="info">触发点击的BeadCell</param>
        /// <returns>开始移动的BeadCell所在基座的序号，如果没有可移动的BeadCell返回-1</returns>
        public int OnPointerDown(int baseIndex, int cellIndex)
        {
            if (baseIndex < 0 || cellIndex < 0) return -1;
            if (baseIndex >= BeadsBases.Count || cellIndex >= BeadsBases[baseIndex].Beads.Count) return -1;
            if (_hasFinish) return -1;
            var baseInfo = _data.BaseInfos[baseIndex];
            if (baseInfo.Complete)
                return -1;
            return FindEnableMoveBeadCell(baseInfo.Beads[cellIndex]);
        }

        /// <summary>
        /// 查找可以移动的BeadCell
        /// </summary>
        /// <param name="cellInfo">点击的BeadCell</param>
        /// <returns>同一个基座下，可以移动的BeadCell的序号</returns>
        private int FindEnableMoveBeadCell(BeadCellInfo cellInfo)
        {
            var baseInfo = _data.BaseInfos[cellInfo.Belong];
            if (baseInfo.Complete) return -1;
            var index = baseInfo.Beads.Take(cellInfo.Index).Reverse().FirstOrDefault(x => x.Type != cellInfo.Type)
                ?.Index ?? -1;
            _curMove = baseInfo.Beads[index + 1];
            return index + 1;
        }
    }
}