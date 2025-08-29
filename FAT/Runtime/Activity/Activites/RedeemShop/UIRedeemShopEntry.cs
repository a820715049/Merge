/*
 * @Author: yanfuxing
 * @Date: 2025-05-08 14:20:01
 */
using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using EL;
using fat.rawdata;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    /// <summary>
    /// 兑换商店活动入口
    /// </summary>
    public class UIRedeemShopEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _group;
        [SerializeField] private GameObject _redDot;
        [SerializeField] private LayoutElement _element;
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _addNum;
        [SerializeField] private TextMeshProUGUI _addNumShow;
        [SerializeField] private TextMeshProUGUI _cdText;
        [SerializeField] private MBRewardIcon _mbReward;
        [SerializeField] private MBRewardProgress _mbProgress;
        [SerializeField] private Animator _animator;
        [SerializeField] public Animator progressAnimator;
        [SerializeField] public Animator scoreIconAnimator;
        [SerializeField] public Animator addNumAnimator;
        private Action _whenTick;
        private ActivityRedeemShopLike _activityRedeemShopLike;
        private (int, int) _curProgress;
        private float _progressPlayAnimTime = 0.75f; //进度条动画表现时间

        public float speed;
        public float duration = 0.5f;
        private int targetV;
        private float currentV;
        private int showAddNum;
        private Coroutine routine;
        private int _isPopupStageReward = -1;
        private int _stageNum = -1;
        void OnEnable()
        {
            _isPopupStageReward = -1;
            _button.onClick.AddListener(OnEntryClick);
            _whenTick = RefreshCD;
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(_whenTick);
            MessageCenter.Get<REDEEMSHOP_SCORE_UPDATE>().AddListener(UpdateScore);
            MessageCenter.Get<REDEEMSHOP_ENTRY_REFRESH_RED_DOT>().AddListener(RefreshRedDot);
            MessageCenter.Get<UI_REWARD_FEEDBACK>().AddListener(RewardFlyFinish);
            MessageCenter.Get<REDEEMSHOP_DATA_CHANGE>().AddListener(RefreshRedDot);
            MessageCenter.Get<REDEEMSHOP_REDPOINT_REFRESH>().AddListener(LookRedPointRefresh);
        }

        #region 方法

        #region 入口刷新
        public void RefreshEntry(ActivityLike activity)
        {
            if (activity is not ActivityRedeemShopLike)
                return;
            _activityRedeemShopLike = (ActivityRedeemShopLike)activity;
            var isActivityValid = _activityRedeemShopLike is { Valid: true, IsUnlock: true };
            IsShowActiveEntry(isActivityValid);
            if (!isActivityValid) return;
            RefreshTheme();
            RefreshCD();
            RefreshRedDot();
            _curProgress = _activityRedeemShopLike.GetCurEntryStageScoreProgress();
            _mbProgress.Refresh(_curProgress.Item1, _curProgress.Item2);

            //刷新当前里程碑奖励
            var rewardDagta = _activityRedeemShopLike.GetTargetRewardConfig();
            _mbReward.Refresh(rewardDagta);
            _addNum.gameObject.SetActive(false);
            _addNumShow.gameObject.SetActive(false);
            _stageNum = -1;
        }
        #endregion

        #region 刷新主题
        public void RefreshTheme()
        {
            var visual = _activityRedeemShopLike.Visual;
        }

        #endregion

        #region 是否显示活动入口
        private void IsShowActiveEntry(bool isActive)
        {
            _group.SetActive(isActive);
            _element.ignoreLayout = !isActive;
        }

        #endregion

        #region 刷新红点
        private void RefreshRedDot()
        {
            if (!_activityRedeemShopLike.Valid) return;
            var isHasReward = _activityRedeemShopLike.IsHasCanRedeemReward();
            _redDot.SetActive(isHasReward && !_activityRedeemShopLike.IsLookRedPoint);
            if (_activityRedeemShopLike != null)
            {
                if (_isPopupStageReward != -1)
                {
                    _activityRedeemShopLike.ShowPopup(_isPopupStageReward);
                    _isPopupStageReward = -1;
                }
            }
        }

        #endregion

        #region 刷新倒计时
        private void RefreshCD()
        {
            if (!_group.activeSelf)
                return;
            var time = _activityRedeemShopLike.Countdown;
            UIUtility.CountDownFormat(_cdText, time);
            if (time <= 0)
            {
                IsShowActiveEntry(false);
            }
        }

        #endregion


        #region  更新入口处数量显示
        private void UpdateScore(int oV_, int nV_)
        {
            if (_activityRedeemShopLike == null)
                return;
            targetV = nV_;
            IsShowActiveEntry(true);
            var change = nV_ - oV_;
            showAddNum += change;
            _addNumShow.text = "+" + showAddNum;
            _addNumShow.gameObject.SetActive(true);
            _addNum.gameObject.SetActive(false);
            currentV = nV_ - showAddNum;
            CheckSpeed();
#if UNITY_EDITOR
            var (curV, tarV) = _activityRedeemShopLike.GetCurEntryStageScoreProgress();
            Debug.Log($"redeemEntry score = {curV}/{tarV}");
#endif
            if (routine != null)
            {
                StopCoroutine(routine);
            }
            routine = StartCoroutine(Animate());
        }

       
        private IEnumerator Animate()
        {
            yield return new WaitForSeconds(0.5f);
            UIManager.Instance.Block(true);
            ProgressEffect();
            _curProgress.Item1 += showAddNum;
            showAddNum = 0;
            _mbProgress.Refresh(_curProgress.Item1, _curProgress.Item2, duration);

            var waitRewardScale = new WaitForSeconds(1.5f);
            _stageNum = -1;
            if (_activityRedeemShopLike.ClonMilestoneNodeList.Count > 0)
            {
                while (_activityRedeemShopLike.ClonMilestoneNodeList.Count > 0)
                {
                    var node = _activityRedeemShopLike.ClonMilestoneNodeList[0];

                    var rewardNode = new RewardConfig
                    {
                        Id = node.milestoneRewardId,
                        Count = node.milestoneRewardCount,
                    };
                    var rewardData = _activityRedeemShopLike.TryGetCommitReward(rewardNode);
                    if (rewardData != null)
                    {
                        // 弹出奖励
                        OnScoreRewardChanged(rewardData);
                    }
                    if (node.IsLast)
                    {
                        _stageNum = node.StageNum;
                    }

                    yield return waitRewardScale;  //等待展示完毕

                    // 获取并显示下一个节点的信息
                    int index = _activityRedeemShopLike.ClonMilestoneNodeList.IndexOf(node);
                    var rewardInfo = GetNextRewardInfo(index);
                    if (rewardInfo != null)
                    {
                        _mbReward.Refresh(rewardInfo);

                        _mbProgress.Refresh(0, node.milestoneScore);
                        _mbProgress.Refresh(_curProgress.Item1, node.milestoneScore, duration);
                    }
                    else
                    {
                        // 如果没有下一个节点，显示当前节点的信息
                        var r = _activityRedeemShopLike.GetScoreShowReward();
                        _mbReward.Refresh(r);

                        //代表没有需要涨满进度条的数据了，截止了
                        var item = _activityRedeemShopLike.GetNextScore();
                        _mbProgress.Refresh(0, node.milestoneScore);
                        _mbProgress.Refresh(item.Item1, item.Item2, duration);
                    }
                    if (_activityRedeemShopLike.ClonMilestoneNodeList.Count == 1)
                    {
                        if (_stageNum > 0)
                        {
                            _isPopupStageReward = _stageNum;
                        }
                    }
                    _activityRedeemShopLike.ClonMilestoneNodeList.RemoveAt(0);
                }
            }
            else
            {
                _curProgress = _activityRedeemShopLike.GetCurEntryStageScoreProgress();
                _mbProgress.Refresh(_curProgress.Item1, _curProgress.Item2, duration);
            }
            UIManager.Instance.Block(false);
        }

        private RewardConfig GetNextRewardInfo(int currentIndex)
        {
            if (_activityRedeemShopLike.ClonMilestoneNodeList.Count > 0)
            {
                // 获取当前节点的下一个节点
                int nextIndex = currentIndex + 1;
                if (nextIndex < _activityRedeemShopLike.ClonMilestoneNodeList.Count)
                {
                    var nextNode = _activityRedeemShopLike.ClonMilestoneNodeList[nextIndex];
                    return new RewardConfig
                    {
                        Id = nextNode.milestoneRewardId,
                        Count = nextNode.milestoneRewardCount
                    };
                }
            }
            return null;
        }

        private void CheckSpeed()
        {
            speed = (targetV - currentV) / duration;
        }

        private void ProgressEffect()
        {
            progressAnimator.SetTrigger("Punch");
            scoreIconAnimator.SetTrigger("Punch");
            addNumAnimator.SetTrigger("Punch");
            _addNumShow.gameObject.SetActive(false);
            _addNum.text = "+" + showAddNum;
            _addNum.gameObject.SetActive(true);
            //showAddNum = 0;
        }

        #endregion

        #region 奖励改变触发
        private void OnScoreRewardChanged(RewardCommitData r)
        {
            //_animator.SetTrigger("Punch");
            _mbReward.icon.transform.DOScale(1.5f, 0.2f).OnComplete(() =>
            {
                MessageCenter.Get<SCORE_FLY_REWARD_CENTER>().Dispatch((_mbReward.icon.transform.position, r, _activityRedeemShopLike));
                _mbReward.icon.transform.localScale = Vector3.one;
            });
        }

        private void RewardFlyFinish(FlyType ft)
        {
            if (ft == FlyType.RedeemCoindEntry)
            {
                _animator.SetTrigger("Punch");
                ActivityRedeemShopLike.PlaySound(AudioEffect.RedeemAccept);
            }
        }
        #endregion

        #endregion

        #region 事件
        private void OnEntryClick()
        {
            if (!_activityRedeemShopLike.Valid) return;
            _activityRedeemShopLike.Open();
        }


        private void LookRedPointRefresh(bool obj)
        {
            if (!_activityRedeemShopLike.Valid) return;
            var isHasReward = _activityRedeemShopLike.IsHasCanRedeemReward();
            _redDot.SetActive(isHasReward && !_activityRedeemShopLike.IsLookRedPoint);
        }

        #endregion

        public void OnDisable()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(_whenTick);
            MessageCenter.Get<REDEEMSHOP_SCORE_UPDATE>().RemoveListener(UpdateScore);
            MessageCenter.Get<REDEEMSHOP_ENTRY_REFRESH_RED_DOT>().RemoveListener(RefreshRedDot);
            MessageCenter.Get<UI_REWARD_FEEDBACK>().RemoveListener(RewardFlyFinish);
            MessageCenter.Get<REDEEMSHOP_DATA_CHANGE>().RemoveListener(RefreshRedDot);
            MessageCenter.Get<REDEEMSHOP_REDPOINT_REFRESH>().RemoveListener(LookRedPointRefresh);
        }


    }
}

