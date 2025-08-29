/*
 * @Author: tang.yan
 * @Description: 主棋盘BP任务信息tips Item
 * @Date: 2025-07-09 10:07:27
 */
using System;
using System.Collections;
using UnityEngine;
using fat.rawdata;
using TMPro;
using static FAT.BPActivity;

namespace FAT
{
    public class MBBPTaskTipsItem : MonoBehaviour
    {
        public BpTaskType BoundType { get; private set; }
        public BPTaskChangeType ChangeType { get; private set; }
        public bool IsInShowState => _isShowing || _isIdle;

        [SerializeField] private Animator animator;
        [SerializeField] private float idleDuration = 1f;
        [SerializeField] private UIImageRes taskBg;
        [SerializeField] private UIImageRes taskIcon;
        [SerializeField] private TMP_Text desc;
        [SerializeField] private Animator finishAnim;

        private float _idleTimer = 0f;
        private bool _isShowing = false;
        private bool _isIdle = false;
        private bool _isHiding = false;
        private Action<MBBPTaskTipsItem> _onComplete;
        private Coroutine _finishCo;

        private BPActivity _bpActivity;
        public void BindActivity(ActivityLike activity)
        {
            _bpActivity = activity as BPActivity;
        }
        
        public void Play(BPTaskData data, BPTaskChangeType changeType, Action<MBBPTaskTipsItem> onComplete)
        {
            UpdateData(data, changeType);
            _onComplete = onComplete;
            gameObject.SetActive(true);
            _isShowing = true;
            _isIdle = false;
            _isHiding = false;
            _idleTimer = 0f;

            animator.ResetTrigger("Hide");
            animator.SetTrigger("Show");
        }

        public void UpdateData(BPTaskData data, BPTaskChangeType changeType)
        {
            if (_bpActivity == null || data == null)
                return;
            //更新持有的数据
            BoundType = (BpTaskType)data.TaskType;
            ChangeType = changeType;
            //UI刷新
            taskIcon.SetImage(data.Conf.TaskIcon);
            var theme = _bpActivity.VisualTaskItem;
            if (theme == null)
                return;
            // 根据任务类型 获得 对应的key
            if (data.IsCycle)
            {
                // 任务底板图
                if (theme.AssetMap.TryGetValue("cycleBgBoard", out var cycleBgKey))
                {
                    taskBg.SetImage(cycleBgKey);
                }
                // 字体颜色
                theme.Refresh(desc, "cycleExp");
            }
            else
            {
                // 任务底板图
                if (theme.AssetMap.TryGetValue("dailyBgBoard", out var dailyBgKey))
                {
                    taskBg.SetImage(dailyBgKey);
                }
                // 字体颜色
                theme.Refresh(desc, "dailyExp");
            }
            _StopCoroutine();
            // 进度值
            if (changeType == BPTaskChangeType.Progress)
            {
                desc.text = data.ProgressValue <= data.RequireCount
                    ? $"{data.ProgressValue}/{data.RequireCount}"
                    : $"{data.RequireCount}/{data.RequireCount}";
            }
            else if (changeType == BPTaskChangeType.Completed)
            {
                desc.text = $"{data.RequireCount}/{data.RequireCount}";
                _finishCo = Game.Instance.StartCoroutineGlobal(_CoPlayFinishAnim());
            }
        }

        public void Punch()
        {
            if (!IsInShowState) return;
            animator.SetTrigger("Punch");
            _idleTimer = 0f;
        }

        private void Update()
        {
            if (_isShowing) return; // wait for Show animation to end
            if (_isIdle)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= idleDuration)
                {
                    _isIdle = false;
                    animator.SetTrigger("Hide");
                    _isHiding = true;
                }
            }
        }

        // Called by Animation Event at the end of the Show animation clip
        public void OnShowComplete()
        {
            if (_isShowing)
            {
                _isShowing = false;
                _isIdle = true;
                _idleTimer = 0f;
            }
        }

        // Called by Animation Event at the end of the Hide animation clip
        public void OnHideComplete()
        {
            if (_isHiding)
            {
                _isHiding = false;
                gameObject.SetActive(false);
                _onComplete?.Invoke(this);
            }
        }
        
        public void HideImmediately()
        {
            animator.ResetTrigger("Show");
            animator.ResetTrigger("Punch");
            animator.ResetTrigger("Hide");
            _isShowing = false;
            _isIdle = false;
            _isHiding = false;
            _onComplete = null;
            gameObject.SetActive(false);
            _StopCoroutine();
        }

        private IEnumerator _CoPlayFinishAnim()
        {
            yield return null;
            finishAnim.gameObject.SetActive(true);
            finishAnim.SetTrigger("Show");
        }
        
        private void _StopCoroutine()
        {
            if (_finishCo != null)
                Game.Instance.StopCoroutineGlobal(_finishCo);
            _finishCo = null;
            finishAnim.gameObject.SetActive(false);
            finishAnim.ResetTrigger("Show");
        }
    }
}