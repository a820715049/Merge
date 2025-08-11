using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using DG.Tweening;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static EL.PoolMapping;

namespace FAT
{
    public class MBMineProgress : MonoBehaviour
    {
        public float ProgressAnimTime;
        public float RewardScale;
        public float RewardAnimTime;
        public Transform RewardFlyNode;
        private TextMeshProUGUI _progressTxt;
        private TextMeshProUGUI _level;
        private UIImageRes _progressReward;
        private RectMask2D _progressMask;
        private Animator _animatorLevel;
        private readonly List<ProgressInfo> _progressList = new();
        private MineBoardActivity _activity;
        private string _rewardIcon;
        public class ProgressInfo
        {
            public int curProgress;
            public int maxProgress;
            public List<RewardCommitData> rewardList;
            public ProgressInfo(int curProgress, int maxProgress, List<RewardCommitData> rewardList)
            {
                this.curProgress = curProgress;
                this.maxProgress = maxProgress;
                this.rewardList = rewardList;
            }
        }
        /// <summary>
        /// 初始化组件
        /// </summary>
        public void Setup()
        {
            transform.Access("MilestoneNode", out _animatorLevel);
            transform.Access("text", out _progressTxt);
            transform.Access("MilestoneNode/Milestone/level", out _level);
            transform.Access("RewardNode/RewardIcon", out _progressReward);
            transform.Access("Mask", out _progressMask);
            transform.AddButton("RewardNode/RewardBtn", ClickReward);
            transform.AddButton("MilestoneNode/Milestone", ClickMilestone);
        }
        /// <summary>
        /// 初始化界面信息，界面刚打开时调用一次即可
        /// </summary>
        /// <param name="mineboard"></param>
        public void Refresh(MineBoardActivity mineboard)
        {
            _activity = mineboard;
            _rewardIcon = _activity.GetProgressInfo(_activity.GetCurProgressPhase())?.RewardIcon ?? string.Empty;
            if (_rewardIcon == string.Empty)
            {
                _progressTxt.text = I18N.Text("#SysComDesc890");
                _level.text = _activity.GetCurProgressPhase().ToString();
                _progressMask.padding = new Vector4(0, 0, 0, 0);
                return;
            }
            _level.text = (_activity.GetCurProgressPhase() + 1).ToString();
            _progressTxt.text = ZString.Format("{0}/{1}", _activity.GetCurProgressNum(), _activity.GetProgressInfo(_activity.GetCurProgressPhase()).Milestone);
            _progressReward.SetImage(_rewardIcon);
            var width = (_progressMask.transform as RectTransform).rect.width;
            _progressMask.padding = new Vector4(0, 0, width * (1 - _activity.GetCurProgressNum() / (float)_activity.GetProgressInfo(_activity.GetCurProgressPhase()).Milestone), 0);
        }
        private void ClickReward()
        {
            if (_isPlaying) return;
            var list = Enumerable.ToList(_activity.GetProgressInfo(_activity.GetCurProgressPhase()).MilestoneReward.Select(s => s.ConvertToRewardConfig()));
            UIManager.Instance.OpenWindow(UIConfig.UIMineRewardTips, _progressReward.transform.position, 35f, list);
        }

        private void ClickMilestone()
        {
            if (_isPlaying) return;
            UIManager.Instance.OpenWindow(_activity.MilestoneResAlt.ActiveR, _activity);
        }

        /// <summary>
        /// 注册进度条动画信息
        /// </summary>
        /// <param name="curProgress"></param>
        /// <param name="rewardList"></param>
        /// <param name="maxProgress"></param>
        public void RegiestProgressInfo(int curProgress, Ref<List<RewardCommitData>> rewardList, int maxProgress)
        {
            _progressList.Add(new ProgressInfo(curProgress, maxProgress, rewardList.obj));
        }
        #region 进度条动画
        /// <summary>
        /// 进度条代币飞到后调用，后接表现流程
        /// </summary>
        public void StartProgressAnim(FlyableItemSlice slice)
        {
            if (slice.FlyType != FlyType.MineScore) return;
            if (slice.CurIdx != 1) return;
            _animatorLevel.SetTrigger("Punch");
            Game.Manager.audioMan.TriggerSound("MineFlyMilestoneToken");
            PlayProgressAnim();
        }
        private bool _isPlaying;
        /// <summary>
        /// 播放进度条动画
        /// </summary>
        private void PlayProgressAnim()
        {
            if (_isPlaying) return;
            if (_progressList.Count == 0) return;
            _isPlaying = true;
            if (_progressList.First().maxProgress == -1)
            {
                NormalAnim(_progressList[0]);
                _progressList.RemoveAt(0);
            }
            else
            {
                CompleteAnim(_progressList[0]);
                _progressList.RemoveAt(0);
            }
        }

        /// <summary>
        /// 进度条没有完成的时候播放的动画
        /// </summary>
        /// <param name="info"></param>
        private void NormalAnim(ProgressInfo info)
        {
            var width = (_progressMask.transform as RectTransform).rect.width;
            var startScore = int.Parse(_progressTxt.text.Split('/')[0]);
            var endScore = int.Parse(_progressTxt.text.Split('/')[1]);
            DOTween.To(value =>
            {
                _progressTxt.text = ZString.Format("{0}/{1}", (int)value, endScore);
                _progressMask.padding = new Vector4(0, 0, width * (1 - value / endScore), 0);
            }, startScore, info.curProgress, ProgressAnimTime).OnComplete(() =>
            {
                _isPlaying = false;
                PlayProgressAnim();
            });
        }
        /// <summary>
        /// 进度条完成的时候播放的动画
        /// </summary>
        /// <param name="info"></param>
        private void CompleteAnim(ProgressInfo info)
        {
            var width = (_progressMask.transform as RectTransform).rect.width;
            var startScore = int.Parse(_progressTxt.text.Split('/')[0]);
            var seq = DOTween.Sequence();
            seq.Append(DOTween.To(value =>
            {
                _progressTxt.text = ZString.Format("{0}/{1}", (int)value, info.maxProgress);
                _progressMask.padding = new Vector4(0, 0, width * (1 - value / info.maxProgress), 0);
            }, startScore, info.maxProgress, ProgressAnimTime / 2).OnComplete(() =>
            {
                RewardAnim(info.rewardList);
            }));
            if (_activity.GetProgressInfo(_activity.GetCurProgressPhase()) == null)
            {
                return;
            }
            seq.Append(DOTween.To(value =>
            {
                _progressTxt.text = ZString.Format("{0}/{1}", (int)value, _activity.GetProgressInfo(_activity.GetCurProgressPhase()).Milestone);
                _progressMask.padding = new Vector4(0, 0, width * (1 - value / _activity.GetProgressInfo(_activity.GetCurProgressPhase()).Milestone), 0);
            }, 0, info.curProgress, ProgressAnimTime / 2).OnComplete(() =>
            {
                _isPlaying = false;
                PlayProgressAnim();
            }));
        }
        /// <summary>
        /// 获得阶段奖励的动画
        /// </summary>
        private void RewardAnim(List<RewardCommitData> rewardList)
        {
            _level.text = (_activity.GetCurProgressPhase() + 1).ToString();
            var seq = DOTween.Sequence();
            seq.Append(_progressReward.transform.DOMove(RewardFlyNode.position, RewardAnimTime).SetEase(Ease.OutSine));
            seq.Join(_progressReward.transform.DOScale(Vector3.one * RewardScale, RewardAnimTime).SetEase(Ease.OutSine));
            seq.Join(_progressReward.transform.DOLocalRotate(Vector3.zero, RewardAnimTime - 0.1f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                UIManager.Instance.OpenWindow(UIConfig.UIMineBoardMilestoneReward, rewardList, _rewardIcon);
            }));
            seq.AppendInterval(0.5f).OnComplete(() =>
            {
                _progressReward.transform.localPosition = Vector3.zero;
                _progressReward.transform.localScale = Vector3.one;
                _rewardIcon = _activity.GetProgressInfo(_activity.GetCurProgressPhase())?.RewardIcon ?? string.Empty;
                if (_rewardIcon == string.Empty)
                {
                    _progressReward.Clear();
                    _progressTxt.text = I18N.Text("#SysComDesc890");
                    _progressMask.padding = new Vector4(0, 0, 0, 0);
                    _level.text = _activity.GetCurProgressPhase().ToString();
                }
                else
                {
                    _progressReward.SetImage(_rewardIcon);
                }
                _isPlaying = false;
            });
        }

        public void DebugShow()
        {
            RewardAnim(null);
        }
        #endregion
    }
}