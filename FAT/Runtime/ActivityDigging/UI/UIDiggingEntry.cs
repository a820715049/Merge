/**
 * @Author: zhangpengjian
 * @Date: 2024/8/19 10:30:22
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/26 16:47:06
 * Description: 挖沙活动入口
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using DG.Tweening;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class UIDiggingEntry : MonoBehaviour, IActivityBoardEntry
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
        public float speed;
        public float duration = 0.5f;
        private int targetV;
        private float currentV;
        private int showAddNum;
        private Coroutine routine;

        private Action WhenCD;
        private ActivityDigging activityDigging;

        public void Start()
        {
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        public void OnEnable()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.DIGGING_SCORE_UPDATE>().AddListener(Refresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(PlayRedDotAnimator);
            MessageCenter.Get<MSG.DIGGING_ENTRY_REFRESH_RED_DOT>().AddListener(RefreshRedDot);

        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.DIGGING_SCORE_UPDATE>().RemoveListener(Refresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(PlayRedDotAnimator);
            MessageCenter.Get<MSG.DIGGING_ENTRY_REFRESH_RED_DOT>().RemoveListener(RefreshRedDot);
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
            if (activity is not ActivityDigging)
            {
                Visible(false);
                return;
            }
            activityDigging = (ActivityDigging)activity;
            var valid = activityDigging is { Valid: true, IsUnlock: true } && activityDigging.HasNextRound();
            Visible(valid);
            if (!valid) return;
            //刷新倒计时
            RefreshCD();
            //刷新红点
            RefreshRedDot();
            //刷新进度条
            var (curV, tarV) = activityDigging.GetScoreShowNum();
            progress.Refresh(curV, tarV);
            //刷新当前里程碑奖励
            var r = activityDigging.GetScoreShowReward();
            reward.Refresh(r);
            addNum.gameObject.SetActive(false);
            addNumShow.gameObject.SetActive(false);
            showAddNum = 0;
        }

        private void RefreshCD()
        {
            if (!group.activeSelf)
                return;
            var v = activityDigging.Countdown;
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
            UIDiggingUtility.EnterActivity();
        }

        private void Progress(int v_, int p_)
        {
            progress.RefreshSegment((int)currentV, v_, p_);
        }

        private (ActivityDigging.Node, int) Node(int v_)
        {
            var next = activityDigging.ScoreRewardNext((int)currentV);
            var list = activityDigging.ListM;
            ActivityDigging.Node node;
            if (v_ < 0 || v_ >= list.Count)
            {
                node = new()
                {
                    reward = activityDigging.detailConfig.CycleLevelToken.ConvertToRewardConfig(),
                    value = (activityDigging.detailConfig.CycleLevelScore * (activityDigging.GetScoreCycleCount((int)currentV) + 1)) + activityDigging.GetScoreMax()
                };
            }
            else
            {
                node = list[v_];
            }
            var prev = next > -1 && v_ < list.Count
                ? (next > 0 ? list[v_ - 1].value : 0)
                : activityDigging.detailConfig.CycleLevelScore * activityDigging.GetScoreCycleCount((int)currentV) + activityDigging.GetScoreMax();
            reward.Refresh(node.reward);
            return (node, prev);
        }

        private void Refresh(int oV_, int nV_)
        {
            Game.Manager.activity.LookupAny(EventType.Digging, out var activity);
            activityDigging = (ActivityDigging)activity;
            targetV = nV_;
            CheckSpeed();
            Visible(true);
            var change = nV_ - oV_;
            showAddNum += change;
            addNumShow.text = "+" + showAddNum;
            addNumShow.gameObject.SetActive(true);
            addNum.gameObject.SetActive(false);
            currentV = nV_ - showAddNum;
#if UNITY_EDITOR
            var (curV, tarV) = activityDigging.GetScoreShowNum();
            var isCycle = activityDigging.GetScore() >= activityDigging.GetScoreMax();
            var str = isCycle ? "cycleLevelScore" : "levelScore";
            Debug.Log($"diggingEntry score = {curV}/{tarV} {str}");
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
            var next = activityDigging.ScoreRewardNext((int)currentV);
            ProgressEffect();
            var (node, prev) = Node(next);
            Progress(node.value, prev);
            var waitRewardScale = new WaitForSeconds(1.5f);
            while (currentV < targetV)
            {
                currentV += speed * Time.deltaTime;
                if (currentV >= node.value)
                {
                    var r = node.reward;
                    var data = activityDigging.TryGetCommitReward(r);
                    if (data != null)
                    {
                        OnScoreRewardChanged(data);
                    }
                    Progress(node.value, prev);
                    yield return waitRewardScale;
                    if (next >= 0)
                        ++next;
                    (node, prev) = Node(next);
                }
                Progress(node.value, prev);
                yield return null;
            }
            currentV = targetV;
            yield return new WaitForSeconds(1.5f);
            routine = null;
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
                MessageCenter.Get<MSG.SCORE_FLY_REWARD_CENTER>().Dispatch((reward.icon.transform.position, r,
                    activityDigging));
                reward.icon.transform.localScale = Vector3.one;
            });
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
            addNumShow.gameObject.SetActive(false);
            addNum.text = "+" + showAddNum;
            addNum.gameObject.SetActive(true);
            showAddNum = 0;
            UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.DiggingEntryFeedback);
        }

        private void RefreshRedDot()
        {
            var keyNum = activityDigging.GetKeyNum();
            redNum.SetRedPoint(keyNum);
            redGo.SetActive(keyNum > 0);
        }

        private void PlayRedDotAnimator(FlyType ft)
        {
            if (ft == FlyType.DiggingShovel)
            {
                UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.DiggingDotGrowth);
                redDotAnimator.SetTrigger("Punch");
            }
        }
    }
}