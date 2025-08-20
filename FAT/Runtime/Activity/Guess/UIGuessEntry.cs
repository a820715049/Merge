/**
 * @Author: zhangpengjian
 * @Date: 2025/2/11 11:58:29
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/2/11 11:58:29
 * Description: 猜颜色棋盘入口
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using DG.Tweening;

namespace FAT
{
    public class UIGuessEntry : MonoBehaviour, IActivityBoardEntry
    {
        public GameObject group;
        public TMP_Text cd;
        public GameObject redGo;
        public TMP_Text redNum;
        public MBRewardProgress progress;
        public MBRewardIcon reward;
        public TMP_Text addNum;
        public TMP_Text addNumShow;
        public Animator animator;
        public Animator progressAnimator;
        public Animator scoreIconAnimator;
        public Animator addNumAnimator;
        public Animator redDotAnimator;
        public float duration = 0.75f;
        private int showAddNum;
        private Coroutine routine;
        private (int, int) curProgress;

        private Action WhenCD;
        private ActivityGuess activityGuess;

        public void Start()
        {
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        public void OnEnable()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.ACTIVITY_GUESS_SCORE>().AddListener(Refresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(PlayRedDotAnimator);
            MessageCenter.Get<MSG.ACTIVITY_GUESS_ENTRY_REFRESH_RED_DOT>().AddListener(RefreshRedDot);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.ACTIVITY_GUESS_SCORE>().RemoveListener(Refresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(PlayRedDotAnimator);
            MessageCenter.Get<MSG.ACTIVITY_GUESS_ENTRY_REFRESH_RED_DOT>().RemoveListener(RefreshRedDot);
        }

        /// <summary>
        /// 寻宝活动入口
        /// </summary>
        public void RefreshEntry(ActivityLike activity)
        {
            if (activity == null)
            {
                Visible(false);
                return;
            }
            if (activity is not ActivityGuess)
            {
                Visible(false);
                return;
            }
            activityGuess = (ActivityGuess)activity;
            var valid = activityGuess is { Valid: true, IsUnlock: true } && activityGuess.HasNextRound();
            Visible(valid);
            if (!valid) return;
            //刷新倒计时
            RefreshCD();
            //刷新红点
            RefreshRedDot();
            //刷新进度条
            curProgress = activityGuess.GetScoreShowNum();
            progress.Refresh(curProgress.Item1, curProgress.Item2);
            //刷新当前里程碑奖励
            var r = activityGuess.GetScoreShowReward();
            reward.Refresh(r);
            addNum.gameObject.SetActive(false);
            addNumShow.gameObject.SetActive(false);
            showAddNum = 0;
        }

        private void RefreshCD()
        {
            if (!group.activeSelf)
                return;
            var v = activityGuess.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
                Visible(false);
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }

        private void EntryClick()
        {
            activityGuess.Open();
        }

        private void Refresh(int last, int cur)
        {
            Visible(true);
            var change = cur - last;
            showAddNum += change;
            addNumShow.text = "+" + showAddNum;
            addNumShow.gameObject.SetActive(true);
            addNum.gameObject.SetActive(false);
            if (routine != null)
            {
                StopCoroutine(routine);
            }
            routine = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            yield return new WaitForSeconds(0.5f);
            ProgressEffect();
            curProgress.Item1 += showAddNum;
            showAddNum = 0;
            progress.Refresh(curProgress.Item1, curProgress.Item2, duration);
            if (activityGuess.scoreCommit != null)
            {
                OnScoreRewardChanged(activityGuess.scoreCommit);
                curProgress = activityGuess.GetScoreShowNum();
                activityGuess.SetScoreCommit(null);
            }
        }

        /// <summary>
        /// 当积分进度条满了之后要做的表现
        /// </summary>
        /// <param name="r"></param>
        private void OnScoreRewardChanged(RewardCommitData r)
        {
            animator.SetTrigger("Punch");
            reward.icon.transform.DOScale(1.5f, 0.2f).OnComplete(() =>
            {
                MessageCenter.Get<MSG.SCORE_FLY_REWARD_CENTER>().Dispatch((reward.icon.transform.position, r, activityGuess));
                reward.icon.transform.localScale = Vector3.one;
            });
        }


        private void ProgressEffect()
        {
            progressAnimator.SetTrigger("Punch");
            scoreIconAnimator.SetTrigger("Punch");
            addNumAnimator.SetTrigger("Punch");
            addNumShow.gameObject.SetActive(false);
            addNum.text = "+" + showAddNum;
            addNum.gameObject.SetActive(true);
            //UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.DiggingEntryFeedback);
        }

        private void RefreshRedDot()
        {
            var token = activityGuess.Token;
            redNum.SetRedPoint(token);
            redGo.SetActive(token > 0);
        }

        private void PlayRedDotAnimator(FlyType ft)
        {
            if (ft == FlyType.GuessToken)
            {
                //UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.DiggingDotGrowth);
                redDotAnimator.SetTrigger("Punch");
                progress.Refresh(0, curProgress.Item2);
                activityGuess.SetScoreCommit(null);
                progress.Refresh(curProgress.Item1, curProgress.Item2, duration);
                var r = activityGuess.GetScoreShowReward();
                reward.Refresh(r);
            }
        }
    }
}