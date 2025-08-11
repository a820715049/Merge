/**
 * @Author: zhangpengjian
 * @Date: 2025/4/25 18:26:48
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/4/25 18:26:48
 * Description: 周任务条目完成飘条
 */

using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class UIActivityWeeklyTaskNotice : UIBase
    {
        [SerializeField]
        private UIActivityWeeklyTaskItem item;

        private Queue<(ActivityWeeklyTask.WeeklyTask, bool, int)> taskQueue = new Queue<(ActivityWeeklyTask.WeeklyTask, bool, int)>();
        private bool isPlaying;

        protected override void OnParse(params object[] items)
        {
            var task = items[0] as ActivityWeeklyTask.WeeklyTask;
            var notFlyToken = (bool)items[1];
            var pre = (int)items[2];
            taskQueue.Enqueue((task, notFlyToken, pre));
            
            if (!isPlaying)
            {
                PlayNextTask();
            }
        }

        private void PlayNextTask()
        {
            if (taskQueue.Count == 0)
            {
                UIManager.Instance.CloseWindow(UIConfig.UIActivityWeeklyTaskNotice);
                isPlaying = false;
                return;
            }

            isPlaying = true;
            var (task, notFlyToken, pre) = taskQueue.Dequeue();
            item.UpdateContent(task);
            item.PlayAnim(task, notFlyToken, OnAnimComplete, pre);
        }

        private void OnAnimComplete()
        {
            PlayNextTask();
        }
    }
}