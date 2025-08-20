/*
 * @Author: tang.yan
 * @Description: 主棋盘BP任务信息tips 
 * @Date: 2025-07-08 15:07:57
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EL;
using static FAT.BPActivity;

namespace FAT
{
    public class MBBPTaskTips : MonoBehaviour, IBoardActivityTipsMono
    {
        public MBBPTaskTipsItem[] tipCells; // 最大并发2个

        private Queue<BPTaskUpdateInfo> _queue = new Queue<BPTaskUpdateInfo>();
        private HashSet<MBBPTaskTipsItem> _activeCells = new HashSet<MBBPTaskTipsItem>();
        private List<BPTaskUpdateInfo> _tempList = new List<BPTaskUpdateInfo>();

        private BPActivity _bpActivity;
        void IBoardActivityTipsMono.BindActivity(ActivityLike activity)
        {
            _bpActivity = activity as BPActivity;
            foreach (var cell in tipCells)
            {
                cell.BindActivity(_bpActivity);
            }
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_BP_TASK_UPDATED>().AddListener(OnTaskUpdated);
            MessageCenter.Get<FAT.MSG.UI_SIMPLE_ANIM_FINISH>().AddListener(_OnAnimPlayEnd);
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_BP_TASK_UPDATED>().RemoveListener(OnTaskUpdated);
            MessageCenter.Get<FAT.MSG.UI_SIMPLE_ANIM_FINISH>().RemoveListener(_OnAnimPlayEnd);
            //界面关闭时立即隐藏cell
            foreach (var cell in tipCells)
            {
                cell.HideImmediately();
            }
            _queue.Clear();
            _activeCells.Clear();
            _tempList.Clear();
        }

        private void OnTaskUpdated(List<BPTaskUpdateInfo> rawUpdates)
        {
            if (_bpActivity == null || !_bpActivity.Active)
                return;
            
            PreprocessUpdates(rawUpdates);
            foreach (var u in _tempList)
            {
                // 如果有同类型 Show 中的 Cell，且该cell不是完成状态，则直接更新数据并 Punch
                var sameCell = tipCells.FirstOrDefault(c => c.BoundType == u.TaskType && c.IsInShowState && c.ChangeType != BPTaskChangeType.Completed);
                if (sameCell != null)
                {
                    sameCell.UpdateData(u.TaskData, u.ChangeType);
                    sameCell.Punch();
                    continue;
                }

                // 只对 Progress 去重
                if (u.ChangeType == BPTaskChangeType.Progress)
                {
                    if (_queue.Any(x => x.TaskType == u.TaskType && x.ChangeType == u.ChangeType))
                        continue;
                }
                _queue.Enqueue(u);
            }
            TryPlayNext();
        }

        private void PreprocessUpdates(List<BPTaskUpdateInfo> updates)
        {
            _tempList.Clear();
            foreach (var group in updates.GroupBy(u => u.TaskType))
            {
                var comps = group.Where(x => x.ChangeType == BPTaskChangeType.Completed)
                                 .OrderBy(x => x.TaskData.Priority);
                if (comps.Any())
                {
                    _tempList.AddRange(comps);
                }
                else
                {
                    var prog = group.Where(x => x.ChangeType == BPTaskChangeType.Progress)
                                    .OrderBy(x => x.TaskData.Priority)
                                    .FirstOrDefault();
                    if (prog.TaskData != null)
                        _tempList.Add(prog);
                }
            }
            // 不同类型按 ID 全局排序
            _tempList.OrderBy(x => x.TaskData.Id);
        }

        private void TryPlayNext()
        {
            if (_queue.Count == 0)
                return;
            var freeCell = tipCells.FirstOrDefault(c => !_activeCells.Contains(c));
            if (freeCell == null)
                return;
            var queueList = Enumerable.ToList(_queue);
            int idx = queueList.FindIndex(u => !_activeCells.Select(c => c.BoundType).Contains(u.TaskType));
            if (idx < 0)
                return;

            var next = queueList[idx];
            queueList.RemoveAt(idx);
            _queue.Clear();
            foreach (var t in queueList)
                _queue.Enqueue(t);
            
            _activeCells.Add(freeCell);
            freeCell.Play(next.TaskData, next.ChangeType, cell =>
            {
                _activeCells.Remove(cell);
                TryPlayNext();
            });
        }
        
        private void _OnAnimPlayEnd(AnimatorStateInfo stateInfo)
        {
            if (stateInfo.IsName("UIBPTaskTipsItemShow"))
            {
                foreach (var cell in tipCells)
                {
                    cell.OnShowComplete();
                }
            }
            else if (stateInfo.IsName("UIBPTaskTipsItemHide"))
            {
                foreach (var cell in tipCells)
                {
                    cell.OnHideComplete();
                }
            }
        }
    }
}