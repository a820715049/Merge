// ==================================================
// // File: MBBingoTaskProgress.cs
// // Author: liyueran
// // Date: 2025-07-16 16:07:59
// // Desc: bingoTask 进度条
// // ==================================================

using System;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBBingoTaskProgress : UIModuleBase
    {
        private UIBingoTaskMain _main;
        private GameObject _boxPrefab;

        private RectTransform _mask;
        private RectTransform _boxRoot;
        private TextMeshProUGUI _indexText;

        private ActivityBingoTask _activity;
        private string boxKey = "pool_bingo_task_box";
        private List<MBBingoTaskBoxItem> _boxes = new(5);
        private Sequence _sequence;

        #region module
        public MBBingoTaskProgress(Transform root) : base(root)
        {
        }

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            ModuleRoot.Access("mask", out _mask);
            ModuleRoot.Access("boxRoot", out _boxRoot);
            ModuleRoot.Access("taskIcon/indexBg/index", out _indexText);
            _boxPrefab = ModuleRoot.Find("boxRoot").GetChild(0).gameObject;
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
            PreparePool();

            var milestoneCount = _activity.detail.MilstoneScore.Count;
            for (var i = 0; i < milestoneCount; i++)
            {
                var index = _activity.detail.MilstoneScore[i];
                var (id, count, _) = _activity.detail.MilestoneReward[i].ConvertToInt3();
                var obj = GameObjectPoolManager.Instance.CreateObject(boxKey, _boxRoot);
                var rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = i == milestoneCount - 1
                    ? new Vector2(128, 128)
                    : new Vector2(108, 108);
                var item = obj.GetComponent<MBBingoTaskBoxItem>();
                item.Init(_activity, id, count, index);
                _boxes.Add(item);
            }

            SetProgress(_activity.score);
        }

        protected override void OnHide()
        {
            foreach (var item in _boxes)
            {
                GameObjectPoolManager.Instance.ReleaseObject(boxKey, item.gameObject);
            }

            _boxes.Clear();
        }


        protected override void OnAddListener()
        {
            MessageCenter.Get<UI_BINGO_CLOSE>().AddListener(OnBingoClose);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<UI_BINGO_CLOSE>().RemoveListener(OnBingoClose);
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


        #region 事件
        private void OnBingoClose()
        {
            // 发放进度条奖励
            var commitList = _activity.GetWaitCommit();

            var to = CalculateRight(_activity.score);
            ProgressAnim(to, commitList, () =>
            {
                foreach (var reward in commitList)
                {
                    UIFlyUtility.FlyReward(reward, Vector3.zero);
                }
            });
        }
        #endregion

        private void PreparePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(boxKey))
                return;
            GameObjectPoolManager.Instance.PreparePool(boxKey, _boxPrefab);
        }

        private float CalculateRight(int score)
        {
            var maskRect = _mask.parent.GetComponent<RectTransform>();
            var max = maskRect.rect.width;
            var milestoneScore = _activity.detail.MilstoneScore;

            if (score <= 0)
            {
                return 0;
            }

            if (score >= milestoneScore[^1])
            {
                return max;
            }

            var getCount = 0;
            var baseScoreIndex = 0;
            for (var i = 0; i < milestoneScore.Count - 1; i++)
            {
                if (score >= milestoneScore[i])
                {
                    getCount += 1;
                    baseScoreIndex = i;
                }
            }

            // 计算基础得分
            var baseScore = milestoneScore[baseScoreIndex];

            // 基础进度
            var percent = getCount * 1f / (milestoneScore.Count);

            // 分段进度
            var delta = milestoneScore[baseScoreIndex + 1] - milestoneScore[baseScoreIndex];
            var diff = (score - baseScore) * 1f / delta;
            percent += diff * 1f / (milestoneScore.Count);

            return max * ( percent);
        }

        private void SetProgress(int index)
        {
            _indexText.SetText(index.ToString());
            var to = CalculateRight(index);
            _mask.sizeDelta = new Vector2(to, _mask.sizeDelta.y);
        }

        private void ProgressAnim(float to, List<RewardCommitData> rewardList, Action onComplete = null)
        {
            // 进度条动画只能单向改变
            if (to <= _mask.sizeDelta.x)
            {
                onComplete?.Invoke();
                return;
            }

            _sequence?.Kill();
            _sequence = DOTween.Sequence();
            _sequence.AppendCallback(() => { _main.SetBlock(true); });
            _sequence.Append(
                DOTween.To(() => _mask.sizeDelta, x => _mask.sizeDelta = x,
                    new Vector2(to, _mask.sizeDelta.y), 1f)
            );

            // 对勾动画
            if (rewardList != null && rewardList.Count > 0)
            {
                var score = _activity.score;
                _sequence.AppendCallback(() =>
                {
                    foreach (var box in _boxes)
                    {
                        if (box._index <= score)
                        {
                            box.ShowCheck();
                        }
                    }
                });

                _sequence.AppendInterval(0.6f);
            }

            _sequence.OnComplete(() =>
            {
                _main.SetBlock(false);
                onComplete?.Invoke();
                _indexText.SetText(_activity.score.ToString());

                // 判断是否完成全部bingo
                var milestoneScore = _activity.detail.MilstoneScore;
                if (_activity.score == milestoneScore[^1])
                {
                    var ui = UIManager.Instance.TryGetUI(_activity.MainPopUp.res.ActiveR);
                    if (ui != null && ui is UIBingoTaskMain main)
                    {
                        main.Close();
                    }
                }
            });
        }
    }
}