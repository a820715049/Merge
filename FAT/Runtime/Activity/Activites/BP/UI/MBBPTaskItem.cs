// ================================================
// File: MBBPTaskItem.cs
// Author: yueran.li
// Date: 2025/06/18 17:05:58 星期三
// Desc: Desc
// ================================================

using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBBPTaskItem : MonoBehaviour
    {
        [SerializeField] private GameObject check;
        [SerializeField] private GameObject cycleCheck;
        private RectMask2D _mask2D;
        private Animator _animator;
        private RectTransform _progress;

        private TextMeshProUGUI _title; // 标题文本
        private TextMeshProUGUI _exp; // 经验值文本
        private TextMeshProUGUI _percent; // 进度条文本
        private TextMeshProUGUI _cd; // 倒计时文本

        private UIImageRes _progressBg; // 进度条背景
        private UIImageRes _progressFill; // 进度条填充
        private UIImageRes _bgRes; // 底板
        private UIImageRes _orderBg; // 底板
        private UIImageRes _iconRes; // 图标

        private BPActivity _activity;
        private BPActivity.BPTaskData _taskData;

        public void Awake()
        {
            RegisterComp();
        }

        private void RegisterComp()
        {
            transform.Access("", out _animator);
            transform.Access("bg", out _bgRes);
            transform.Access("bg/orderBg", out _orderBg);
            transform.Access("bg/icon", out _iconRes);
            transform.Access("bg/Title", out _title);
            transform.Access("bg/cd", out _cd);
            transform.Access("bg/exp", out _exp);
            transform.Access("bg/progress", out _progress);
            transform.Access("bg/progress/mask", out _mask2D);
            transform.Access("bg/progress/mask", out _progressBg);
            transform.Access("bg/progress/mask/fill", out _progressFill);
            transform.Access("bg/progress/percent", out _percent);
        }


        public void OnEnable()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<UI_BP_TASK_COMPLETE>().AddListener(OnCompleteTask);
        }

        public void OnDisable()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<UI_BP_TASK_COMPLETE>().RemoveListener(OnCompleteTask);
        }


        public void Init(BPActivity activity, BPActivity.BPTaskData taskData)
        {
            this._activity = activity;
            this._taskData = taskData;
        }

        // 根据数据刷新表现
        public void RefreshView(bool checkCycle = false)
        {
            RefreshTheme(); // 换皮

            _bgRes.enabled = !_taskData.IsCycle;
            _bgRes.gameObject.GetComponent<Image>().enabled = !_taskData.IsCycle;
            _orderBg.gameObject.SetActive(_taskData.IsCycle);


            _iconRes.SetImage(_taskData.Conf.TaskIcon); // icon
            _title.SetText(I18N.Text(_taskData.Conf.TaskDesc)); // 标题
            _cd.gameObject.SetActive(!_taskData.IsCycle);
            _progress.gameObject.SetActive(true);


            // 是否完成
            if (_taskData.State != BPActivity.BPTaskState.UnFinish)
            {
                // 已完成
                check.SetActive(true);
                _exp.gameObject.SetActive(false);
                _cd.gameObject.SetActive(false);

                // 进度条
                _percent.SetText($"{_taskData.RequireCount}/{_taskData.RequireCount}");
            }
            else
            {
                // 未完成
                check.SetActive(false);
                _exp.gameObject.SetActive(true);
                _cd.gameObject.SetActive(!_taskData.IsCycle);

                // 经验
                var (id, count, _) = _taskData.Conf.Reward.ConvertToInt3();
                var tokenIcon = UIUtility.FormatTMPString(id);
                _exp.SetText($"{tokenIcon}{count}");

                // 进度条
                _percent.SetText(_taskData.ProgressValue <= _taskData.RequireCount
                    ? $"{_taskData.ProgressValue}/{_taskData.RequireCount}"
                    : $"{_taskData.RequireCount}/{_taskData.RequireCount}");
            }

            // 进度条
            _percent.SetText(_taskData.ProgressValue <= _taskData.RequireCount
                ? $"{_taskData.ProgressValue}/{_taskData.RequireCount}"
                : $"{_taskData.RequireCount}/{_taskData.RequireCount}");

            var progress = CalculateProgress();
            _mask2D.padding = new Vector4(0, 0, progress, 0);

            // 循环任务 在 任务完成界面的特殊显示
            if (checkCycle && _taskData.IsCycle && _taskData.State == BPActivity.BPTaskState.UnFinish)
            {
                check.SetActive(false);
                if (_activity.UnClaimCycleTaskCount > 0)
                {
                    _progress.gameObject.SetActive(false);
                    cycleCheck.SetActive(true);
                    _exp.gameObject.SetActive(false);
                    _cd.gameObject.SetActive(false);
                }
            }
            else
            {
                cycleCheck.SetActive(false);
            }

            // cd
            RefreshCd();
        }

        public void PlayTaskRefreshAnim()
        {
            _animator.SetTrigger("Refresh");
        }

        private float CalculateProgress()
        {
            var progress = 1 - _taskData.ProgressValue / (float)_taskData.RequireCount;

            // 已完成
            if (_taskData.State != BPActivity.BPTaskState.UnFinish)
            {
                progress = 0;
            }

            var max = _mask2D.rectTransform.sizeDelta.x;
            return progress * max;
        }

        // 完成任务
        private void OnCompleteTask(int id)
        {
            // 判断完成的任务 是否是自己
            if (id != _taskData.Conf.Id)
            {
                return;
            }

            // 刷新表现
            RefreshView();
        }

        private void RefreshCd()
        {
            // cd刷新时间
            var refresh = _activity.TaskRefreshTs;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, refresh - t);
            _cd.SetCountDown(diff);
        }

        private void RefreshTheme()
        {
            if (_activity == null)
            {
                return;
            }

            // 进度条背景图
            if (_taskData.IsCycle)
            {
                if (_activity.VisualTaskItem.AssetMap.TryGetValue("cycleProgressFill", out var cycleProgressBgKey))
                {
                    _progressBg.SetImage(cycleProgressBgKey);
                }
            }
            else
            {
                if (_activity.VisualTaskItem.AssetMap.TryGetValue("progressBg", out var progressBgKey))
                {
                    _progressBg.SetImage(progressBgKey);
                }
            }

            // 进度条填充图
            if (_activity.VisualTaskItem.AssetMap.TryGetValue("progressFill", out var progressFillKey))
            {
                _progressFill.SetImage(progressFillKey);
            }

            // 进度条字体颜色
            _activity.VisualTaskItem.Refresh(_percent, "progress");

            // 倒计时字体颜色
            _activity.VisualTaskItem.Refresh(_cd, "cd");


            // 根据任务类型 获得 对应的key
            if (_taskData.IsCycle)
            {
                // 任务底板图
                if (_activity.VisualTaskItem.AssetMap.TryGetValue("cycleBg", out var cycleBgKey))
                {
                    _bgRes.SetImage(cycleBgKey);
                    _orderBg.SetImage(cycleBgKey);
                }

                // 标题字体颜色
                _activity.VisualTaskItem.Refresh(_title, "cycleTitle");


                // 经验值字体颜色
                _activity.VisualTaskItem.Refresh(_exp, "cycleExp");
            }
            else
            {
                // 任务底板图
                if (_activity.VisualTaskItem.AssetMap.TryGetValue("dailyBg", out var dailyBgKey))
                {
                    _bgRes.SetImage(dailyBgKey);
                }

                // 标题字体颜色
                _activity.VisualTaskItem.Refresh(_title, "dailyTitle");


                // 经验值字体颜色
                _activity.VisualTaskItem.Refresh(_exp, "dailyExp");
            }
        }
    }
}