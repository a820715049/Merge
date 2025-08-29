/**
 * @Author: zhangpengjian
 * @Date: 2025/4/24 18:18:54
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/4/24 18:18:54
 * Description: 周任务item
 */

using EL;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI.Extensions;
using System;

namespace FAT
{
    public class UIActivityWeeklyTaskItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI taskName;
        [SerializeField]
        private TextMeshProUGUI taskNameFinish;
        [SerializeField]
        private UIImageRes taskIcon;
        [SerializeField]
        private MBRewardProgress taskProgress;
        [SerializeField]
        private UIImageRes tokenIcon;
        [SerializeField]
        private Transform complete;
        [SerializeField]
        private Transform tokenRoot;
        [SerializeField]
        private Animator animator;

        public void UpdateContent(ActivityWeeklyTask.WeeklyTask task)
        {
            complete.gameObject.SetActive(task.complete);
            tokenRoot.gameObject.SetActive(!task.complete);
            taskName.text = I18N.FormatText(task.conf.Desc, task.require);
            taskNameFinish.text = I18N.FormatText(task.conf.Desc, task.require);
            taskIcon.SetImage(task.conf.IconShow);
            taskProgress.Refresh(task.value, task.require);
            var token = task.conf.TaskReward.ConvertToRewardConfig().Id;
            var tokenConf = fat.conf.Data.GetObjBasic(token);
            tokenIcon.SetImage(tokenConf.Icon);
        }

        public void PlayAnim(ActivityWeeklyTask.WeeklyTask task, bool notFlyToken, Action onComplete, int pre)
        {
            animator.gameObject.SetActive(false);
            taskProgress.Refresh(pre, task.require);

            // 创建序列来处理延时操作
            DOTween.Sequence()
                .AppendCallback(() => taskProgress.Refresh(task.value, task.require, 0.5f))
                .AppendInterval(0.5f)  // 等待进度条动画完成
                .AppendCallback(() =>
                {
                    animator.gameObject.SetActive(true);
                    animator.SetTrigger("Punch");
                })
                .AppendInterval(0.3f)  // 继续等待0.3秒
                .AppendCallback(() =>
                {
                    if (!notFlyToken)
                    {
                        var reward = task.conf.TaskReward.ConvertToRewardConfig();
                        var r = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.weekly_task);
                        UIFlyUtility.FlyReward(r, tokenIcon.transform.position);
                    }
                });

            // 获取或添加CanvasGroup组件
            var completeGroup = complete.gameObject.GetOrAddComponent<CanvasGroup>();
            var t = transform.gameObject.GetOrAddComponent<CanvasGroup>();

            // 设置初始状态
            completeGroup.alpha = 0;
            t.alpha = 1;
            complete.gameObject.SetActive(true);

            // 执行渐现动画，然后是渐隐动画
            completeGroup.DOFade(1, 0.5f).OnComplete(() =>
            {
                // 添加渐隐动画
                t.DOFade(0, 0.5f).SetDelay(1.8f).OnComplete(() =>
                {
                    onComplete?.Invoke();
                });
            });
        }
    }
}