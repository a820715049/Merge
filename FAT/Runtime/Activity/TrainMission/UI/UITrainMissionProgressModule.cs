// ==================================================
// // File: UITrainMissionProgress.cs
// // Author: liyueran
// // Date: 2025-07-29 15:07:28
// // Desc: $火车棋盘进度条模块
// // ==================================================

using System;
using System.Collections.Generic;
using Cysharp.Text;
using DG.Tweening;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UITrainMissionProgressModule : UIModuleBase
    {
        private RectTransform _content;
        private RectTransform _mask;
        public RectTransform headIcon;
        private GameObject _itemPrefab;
        private TextMeshProUGUI _progressText;
        private TextMeshProUGUI _roundText;
        private Animator _animator;

        private TrainMissionActivity _activity;
        private UITrainMissionMain _main;
        private readonly float _width = 10;

        private string PoolKeyItem => $"train_mission_progress_item";

        private Dictionary<int, MBTrainMissionProgressItem> _itemMap = new();

        public UITrainMissionProgressModule(Transform root) : base(root)
        {
        }

        #region module
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            ModuleRoot.Access("", out _animator);
            ModuleRoot.Access("Content", out _content);
            ModuleRoot.Access("bg/mask", out _mask);
            ModuleRoot.Access("icon/proBg/prog", out _progressText);
            ModuleRoot.Access("icon", out headIcon);
            ModuleRoot.Access("Round/txt", out _roundText);

            _itemPrefab = _content.GetChild(0).gameObject;
        }

        private void AddButton()
        {
            ModuleRoot.AddButton("Round", OnClickRoundInfo).WithClickScale().FixPivot();
            ModuleRoot.AddButton("Round/info", OnClickRoundInfo).WithClickScale().FixPivot();
            ModuleRoot.AddButton("icon", OnClickRoundInfo).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (TrainMissionActivity)items[0];
            _main = (UITrainMissionMain)items[1];
        }

        protected override void OnShow()
        {
            EnsurePool();

            var maxLv = _activity.GetCurMilestoneTotal();

            // 初始化进度条item
            for (var i = 0; i < maxLv; i++)
            {
                var index = i + 1;
                var obj = GameObjectPoolManager.Instance.CreateObject(PoolKeyItem, _content);
                obj.transform.localScale = Vector3.one;

                var item = obj.GetComponent<MBTrainMissionProgressItem>();
                item.Init(_activity, this, index);

                obj.SetActive(true);
                _itemMap.Add(index, item);
            }

            SetProgress(CalProgressValue());

            SetProgressText();
        }

        private void EnsurePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(PoolKeyItem))
                return;
            GameObjectPoolManager.Instance.PreparePool(PoolKeyItem, _itemPrefab);
        }

        protected override void OnHide()
        {
            foreach (var item in _itemMap.Values)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolKeyItem, item.gameObject);
            }

            _itemMap.Clear();
            _progressAnimSeq?.Kill();
        }

        protected override void OnAddListener()
        {
        }

        protected override void OnRemoveListener()
        {
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

        #region 进度条动画
        private void ClearProgress()
        {
            _mask.sizeDelta = new Vector2(0, _mask.sizeDelta.y);
        }

        public float CalProgressValue()
        {
            // 获取mask相对于canvas的缩放
            var maskRect = _mask.parent.GetComponent<RectTransform>();
            var max = maskRect.rect.width;

            var curLv = _activity.GetCurMilestoneProgress();
            var maxLv = _activity.GetCurMilestoneTotal();
            var progress = (float)curLv / maxLv;

            return max * progress - _width;
        }

        private void SetProgress(float to)
        {
            _mask.sizeDelta = new Vector2(to, _mask.sizeDelta.y);
            _proAnimTo = to;
        }

        private Sequence _progressAnimSeq;

        private float _proAnimTo = 0f;

        public void ProgressAnim(float to)
        {
            // 获取待提交的里程碑奖励
            var milestoneReward = _activity.GetMilestoneWaitCommitReward();
            var delay = milestoneReward == null ? 0f : 0.5f;

            // 进度条动画只能单向改变
            if (to <= _mask.sizeDelta.x || to <= _proAnimTo)
            {
                return;
            }
            
            _proAnimTo = to;

            _progressAnimSeq?.Kill();
            _progressAnimSeq = DOTween.Sequence();
            _progressAnimSeq.Append(
                
                // 进度条动画
                DOTween.To(() => _mask.sizeDelta, x => _mask.sizeDelta = x, new Vector2(to, _mask.sizeDelta.y), 1f)
                    .OnComplete(() =>
                    {
                        var curLv = _activity.GetCurMilestoneProgress();
                        foreach (var item in _itemMap)
                        {
                            if (item.Key <= curLv)
                            {
                                item.Value.Punch();
                            }
                        }

                        SetProgressText();
                    }));

            _progressAnimSeq.AppendInterval(delay);

            _progressAnimSeq.OnComplete(() =>
            {
                // 获取待提交的里程碑奖励
                if (milestoneReward != null)
                {
                    // 打开发奖界面
                    UIManager.Instance.OpenWindow(_activity.VisualReward.res.ActiveR, _activity, _main,
                        milestoneReward);
                }
                else
                {
                    // 判断是否有下一轮
                    if (_activity.waitEnterNextChallenge && !_activity.waitRecycle)
                    {
                        UIManager.Instance.OpenWindow(_activity.VisualComplete.res.ActiveR, _activity, _main);
                    }
                    else if (_activity.waitRecycle)
                    {
                        _main.StartRecycle();
                    }
                }
            });
        }
        #endregion

        #region 事件
        private void OnClickRoundInfo()
        {
            // 打开预览界面
            UIManager.Instance.OpenWindow(_activity.VisualPreview.res.ActiveR, _activity);
        }
        #endregion

        // 车Icon动画
        public void TrainPunch()
        {
            _animator.SetTrigger("Punch");
        }


        // 设置进度值
        private void SetProgressText()
        {
            var maxLv = _activity.GetCurMilestoneTotal();
            var curLv = _activity.GetCurMilestoneProgress();
            _progressText.SetText($"{curLv}/{maxLv}");
            _roundText.SetText(I18N.FormatText("#SysComDesc1544", _activity.challengeIndex + 1));
        }
    }
}