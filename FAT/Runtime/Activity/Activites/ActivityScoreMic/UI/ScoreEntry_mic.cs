using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using System.Collections.Generic;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class ScoreEntry_mic : MonoBehaviour, IActivityBoardEntry
    {
        public LayoutElement element;
        public GameObject group;
        public UIImageRes bg;
        public UIImageRes scoreIcon;
        public UIImageRes progressBg;
        public UIImageRes progressValueImage;
        public TMP_Text addNum;
        public TMP_Text addNumShow;
        public TMP_Text cd;
        public UIImageRes cdBg;
        public MBRewardProgress progress;
        public Animator animator;
        public Animator progressAnimator;
        public Animator scoreIconAnimator;
        public Animator addNumAnimator;
        public float speed;
        public float duration = 0.5f;

        private int showAddNum;
        private Action<int, int> WhenUpdate;
        private Action WhenCD;
        private ActivityScoreMic activity;
        private int tipOffset = 4;
        private int targetV;
        private float currentV;
        private Coroutine routine;
        private List<string> listC = new();

        public void Start()
        {
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        public void OnEnable()
        {
            WhenUpdate ??= RefreshData;
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.SCORE_MIC_NUM_ADD>().AddListener(WhenUpdate);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(ScoreIconScaleAnimate);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(OnActEnd);
            MessageCenter.Get<MSG.SCORE_ADD_DEBUG>().AddListener(OnDebugAddScore);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.SCORE_MIC_NUM_ADD>().RemoveListener(WhenUpdate);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(ScoreIconScaleAnimate);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(OnActEnd);
            MessageCenter.Get<MSG.SCORE_ADD_DEBUG>().RemoveListener(OnDebugAddScore);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
            if (routine != null)
            {
                Game.Instance.StopCoroutineGlobal(routine);
                routine = null;
            }
            listC.Clear();
            //如果主棋盘的活动入口因为进入其他棋盘或场景导致被隐藏，则尝试弹一下可能存在的奖励弹窗
            activity?.TryPopupLevelUp();
        }

        private IEnumerator OnAdapterComplete()
        {
            yield return null;
            MessageCenter.Get<MSG.SCORE_ENTRY_POSITION_REFRESH>().Dispatch(group.transform.position);
        }

        private void RefreshData(int currValue, int targetValue)
        {
            listC.Add(currValue + "/" + targetValue);
            var change = targetValue - currValue;
            showAddNum += change;
            addNumShow.text = showAddNum > 0 ? "+" + showAddNum : "";
            addNumShow.gameObject.SetActive(true);
            addNum.gameObject.SetActive(false);
            if (listC.Count <= 1)
            {
                Refresh(currValue, targetValue);
            }
        }

        private void Refresh(int currValue, int targetValue)
        {
            RefreshProgress(currValue, targetValue);
        }

        /// <summary>
        /// 积分活动入口
        /// </summary>
        public void RefreshEntry(ActivityLike activity)
        {
            if (activity == null)
            {
                Visible(false);
                return;
            }
            if (activity is not ActivityScoreMic)
                return;
            this.activity = (ActivityScoreMic)activity;
            var valid = !(this.activity is { Valid: false } || this.activity.IsComplete());
            Visible(valid);
            if (!valid) return;
            RefreshTheme();
            Game.Instance.StartCoroutineGlobal(OnAdapterComplete());
            //刷新倒计时
            RefreshCD();
            //刷新进度条
            progress.Refresh(this.activity.CurMilestoneNum, this.activity.GetCurMilestoneNumMax(this.activity.CurMilestoneLevel));
            addNum.gameObject.SetActive(false);
            addNumShow.gameObject.SetActive(false);
            showAddNum = 0;
            //刷新当前积分活动 积分图标
            scoreIcon.SetImage(Game.Manager.objectMan.GetBasicConfig(this.activity.Conf.Token).Icon);
        }

        private void RefreshTheme()
        {
            activity.Visual.Refresh(bg, "bgImage");
            activity.Visual.Refresh(cdBg, "cdBg");
            activity.Visual.Refresh(progressBg, "bar1");
            activity.Visual.Refresh(progressValueImage, "bar2");
            
            activity.Visual.Refresh(progress.text, "num");
            activity.Visual.Refresh(addNum, "num");
            activity.Visual.RefreshStyle(cd, "cdColor");

        }

        private void RefreshProgress(int currentValue, int targetValue)
        {
            Game.Manager.activity.LookupAny(EventType.MicMilestone, out var activity);
            if (activity == null)
            {
                Visible(false);
                return;
            }

            targetV = targetValue;
            if (routine == null)
            {
                currentV = currentValue;
            }
            else
            {
                Game.Instance.StopCoroutineGlobal(routine);
            }

            CheckSpeed();
        }
        
        private int MilestonePrev(float totalScore)
        {
            var ids = activity.GetCurDetailConfig().MilestoneGroup;
            var sum = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                var conf = Game.Manager.configMan.GetMicMilestoneGroupConfig(ids[i]);
                sum += conf.MilestoneScore;
                if (totalScore < sum)
                {
                    return sum - conf.MilestoneScore;
                }
            }
            return -1;
        }

        private IEnumerator Animate()
        {
            ProgressEffect();

            var shouldPopup = activity.LastMilestoneLevel < activity.CurMilestoneLevel;
            var endMilestoneIndex = activity.LastMilestoneLevel;
            var endShowScore = activity.LastMilestoneNum;
            var prev = MilestonePrev(currentV);
            var endMilestoneScore = activity.GetCurMilestoneNumMax(activity.LastMilestoneLevel);
            
            Progress(prev + endMilestoneScore, prev);
            while (currentV < targetV)
            {
                currentV = Mathf.Min(currentV + speed * Time.deltaTime, targetV);
                if (currentV >= prev + endMilestoneScore)
                {
                    OnMileStoneChanged();
                    Progress(prev + endMilestoneScore, prev);

                    // 第一个里程碑达成时检查是否需要弹窗
                    if (shouldPopup)
                    {
                        activity.TryPopupLevelUp();
                    }

                    yield return new WaitForSeconds(1.5f);
                    if (activity.IsComplete())
                    {
                        MessageCenter.Get<MSG.ACTIVITY_ENTRY_LAYOUT_REFRESH>().Dispatch();
                        Visible(false);
                        yield break;
                    }

                    prev = MilestonePrev(targetV);
                    endMilestoneScore = activity.GetCurMilestoneNumMax(activity.CurMilestoneLevel);
                }

                Progress(prev + endMilestoneScore, prev);

                yield return null;
            }

            currentV = targetV;
            progress.Refresh(activity.CurMilestoneNum, activity.GetCurMilestoneNumMax(activity.CurMilestoneLevel));
            routine = null;
            if (listC.Count > 0)
            {
                listC.RemoveAt(0);
            }
            if (listC.Count > 0)
            {
                var a = listC[0].Split("/");
                var cur = targetV;
                var tar = int.Parse(a[1]);
                Refresh(cur, tar);
                routine = Game.Instance.StartCoroutineGlobal(Animate());
            }
        }

        private void Progress(int v_, int p_)
        {
            progress.RefreshSegment((int)Math.Floor(currentV), v_, p_, 0, p_);
        }

        private void CheckSpeed()
        {
            speed = (targetV - currentV) / duration;
        }

        private void RefreshCD()
        {
            if (!group.activeSelf)
                return;
            var v = activity.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
                Visible(false);
        }

        private void OnMileStoneChanged()
        {
            animator.SetTrigger("Punch");
        }

        private void ScoreIconScaleAnimate(FlyType type)
        {
            if (type == FlyType.ScoreMicToken)
            {
                scoreIconAnimator.SetTrigger("Punch");
                Game.Manager.audioMan.TriggerSound("WhiteBall");
                BeginProgressAnimate();
            }
        }

        private void ProgressEffect()
        {
            if (showAddNum <= 0) return;
            
            progressAnimator.SetTrigger("Punch");
            addNumAnimator.SetTrigger("Punch");
            addNumShow.gameObject.SetActive(false);
            addNum.text = showAddNum > 0 ? "+" + showAddNum : "";
            addNum.gameObject.SetActive(true);
            showAddNum = 0;
            UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureEntryFeedback);
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
            element.ignoreLayout = !v_;
        }

        private void EntryClick()
        {
            activity.Open();
        }

        private void OnActEnd(ActivityLike act, bool expire)
        {
            if (act != activity)
                return;
            Visible(false);
        }

        private void BeginProgressAnimate()
        {
            //如果需要弹窗领奖，这里要播进度条动画，而不是直接消失，消失会在进度条动画中处理
            var shouldPopup = activity.LastMilestoneLevel < activity.CurMilestoneLevel;
            if (!shouldPopup)
            {
                if (activity.IsComplete())
                {
                    Visible(false);
                    MessageCenter.Get<MSG.ACTIVITY_ENTRY_LAYOUT_REFRESH>().Dispatch();
                    return;
                }
            }

            if (routine != null)
                return;
            routine = Game.Instance.StartCoroutineGlobal(Animate());
        }

        private void OnDebugAddScore()
        {
            Game.Manager.activity.LookupAny(EventType.MicMilestone, out var activity);
            this.activity = (ActivityScoreMic)activity;
            RefreshEntry(this.activity);
        }
    }
}