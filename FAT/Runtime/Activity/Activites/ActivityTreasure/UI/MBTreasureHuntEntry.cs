/**
 * @Author: zhangpengjian
 * @Date: 2024-04-22 16:57:44
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/10 14:47:09
 * Description: 寻宝活动入口
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
    public class MBTreasureHuntEntry : MonoBehaviour, IActivityBoardEntry
    {
        public LayoutElement element;
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
        private ActivityTreasure activityTreasure;

        public void Start()
        {
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        public void OnEnable()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(OnActEnd);
            MessageCenter.Get<MSG.TREASURE_SCORE_UPDATE>().AddListener(Refresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(PlayRedDotAnimator);
            MessageCenter.Get<MSG.TREASURE_HELP_REFRESH_RED_DOT>().AddListener(RefreshRedDot);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(OnActEnd);
            MessageCenter.Get<MSG.TREASURE_SCORE_UPDATE>().RemoveListener(Refresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(PlayRedDotAnimator);
            MessageCenter.Get<MSG.TREASURE_HELP_REFRESH_RED_DOT>().RemoveListener(RefreshRedDot);
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
            activityTreasure = (ActivityTreasure)activity;
            var valid = activityTreasure is { Valid: true, IsUnlock: true } && activityTreasure.HasNextLevelGroup();
            Visible(valid);
            if (!valid)
                return;
            //刷新倒计时
            RefreshCD();
            //刷新红点
            RefreshRedDot();
            //刷新进度条
            var (curV, tarV) = activityTreasure.GetScoreShowNum();
            progress.Refresh(curV, tarV);
            //刷新当前里程碑奖励
            var r = activityTreasure.GetScoreShowReward();
            reward.Refresh(r);
            addNum.gameObject.SetActive(false);
            addNumShow.gameObject.SetActive(false);
            showAddNum = 0;
        }

        private void RefreshCD()
        {
            if (!group.activeSelf)
                return;
            var v = activityTreasure.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
                Visible(false);
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
            element.ignoreLayout = !v_;
        }

        private void EntryClick()
        {
            UITreasureHuntUtility.EnterActivity();
        }

        private void OnActEnd(ActivityLike act, bool expire)
        {
            if (act != activityTreasure)
                return;
            Visible(false);
        }

        private void Progress(int v_, int p_)
        {
            progress.RefreshSegment((int)currentV, v_, p_);
        }

        private (ActivityTreasure.Node, int) Node(int v_)
        {
            var next = activityTreasure.ScoreRewardNext((int)currentV);
            var list = activityTreasure.ListM;
            ActivityTreasure.Node node;
            if (v_ < 0 || v_ >= list.Count)
            {
                node = new()
                {
                    reward = activityTreasure.GetCycleReward(),
                    value = (activityTreasure.GetCycleScore() * (activityTreasure.GetScoreCycleCount() + 1)) + activityTreasure.GetScoreMax()
                };
            }
            else
            {
                node = list[v_];
            }
            var prev = next > -1 && v_ < list.Count
                ? (next > 0 ? list[v_ - 1].value : 0)
                : activityTreasure.GetCycleScore() * activityTreasure.GetScoreCycleCount() + activityTreasure.GetScoreMax();
            // 音效小组反馈 先不播放获得钥匙奖励音效
            // UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureGetKey);
            reward.Refresh(node.reward);
            return (node, prev);
        }

        private void Refresh(int oV_, int nV_)
        {
            if (activityTreasure == null)
                return;
            targetV = nV_;
            Visible(true);
            var change = nV_ - oV_;
            showAddNum += change;
            addNumShow.text = "+" + showAddNum;
            addNumShow.gameObject.SetActive(true);
            addNum.gameObject.SetActive(false);
            currentV = nV_ - showAddNum;
            CheckSpeed();
#if UNITY_EDITOR
            var (curV, tarV) = activityTreasure.GetScoreShowNum();
            Debug.Log($"treasureEntry score = {curV}/{tarV}");
#endif
            if (routine != null)
            {
                StopCoroutine(routine);
            }
            routine = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            yield return new WaitForSeconds(1f);
            var next = activityTreasure.ScoreRewardNext((int)currentV);
            ProgressEffect();
            var (node, prev) = Node(next);
            Progress(node.value, prev);
            while (currentV < targetV)
            {
                currentV += speed * Time.deltaTime;
                if (currentV >= node.value)
                {
                    var r = node.reward;
                    var data = activityTreasure.TryGetCommitReward(r);
                    if (data != null)
                    {
                        OnScoreRewardChanged(data);
                    }
                    Progress(node.value, prev);
                    yield return new WaitForSeconds(1.5f);
                    if (next >= 0)
                        ++next;
                    else
                        activityTreasure.SetCycleScoreCount();
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
                    activityTreasure));
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
            UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureEntryFeedback);
        }

        private void RefreshRedDot()
        {
            var keyNum = activityTreasure.GetKeyNum();
            redNum.SetRedPoint(keyNum);
            redGo.SetActive(keyNum > 0);
        }

        private void PlayRedDotAnimator(FlyType ft)
        {
            if (ft == FlyType.TreasureKey)
            {
                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureDotGrowth);
                redDotAnimator.SetTrigger("Punch");
            }
        }
    }
}