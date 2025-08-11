/**
 * @Author: zhangpengjian
 * @Date: 2024/9/7 11:54:26
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/9/7 11:54:26
 * Description: 积分活动入口
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class ScoreEntry : MonoBehaviour, IActivityBoardEntry
    {
        public LayoutElement element;
        public GameObject group;
        public UIImageRes rewardIcon;
        public UIImageRes bg;
        public UIImageRes scoreIcon;
        public UIImageRes progressBg;
        public UIImageRes progressValueImage;
        public TMP_Text addNum;
        public TMP_Text cd;
        public UIImageRes cdBg;
        public MBRewardProgress progress;
        public MBRewardIcon reward;
        public Image info;
        public Button btnInfo;
        public Button rewardIconBtn;
        public Animator animator;
        public Animator progressAnimator;
        public Animator scoreIconAnimator;
        public Animator addNumAnimator;
        public float speed;
        public float duration = 0.5f;

        private Action<int, int> WhenUpdate;
        private Action WhenCD;
        private ActivityScore activityScore;
        private int tipOffset = 4;
        private int targetV;
        private float currentV;
        private Coroutine routine;
        private List<string> listC = new();
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) 
                return;
            var root = transform.Find("group");
            group = root.gameObject;
            rewardIcon = root.FindEx<UIImageRes>("rewardbg/icon");
            element = transform.GetComponent<LayoutElement>();
            scoreIcon = root.FindEx<UIImageRes>("icon/ScoreIconRoot/scoreIcon");
            bg = root.FindEx<UIImageRes>("detailBg");
            cd = root.FindEx<TextMeshProUGUI>("cdBg/cd");
            cdBg = root.FindEx<UIImageRes>("cdBg");
            progressBg = root.FindEx<UIImageRes>("progress/back");
            progressValueImage = root.FindEx<UIImageRes>("progress/mask/fore");
            progress = root.FindEx<MBRewardProgress>("progress");
            reward = root.FindEx<MBRewardIcon>("rewardbg");
            info = root.FindEx<Image>("rewardbg/info");
            btnInfo = root.FindEx<Button>("btnInfo");
            addNum = root.FindEx<TextMeshProUGUI>("icon/addNum");
            rewardIconBtn = root.FindEx<Button>("rewardbg");
            progressAnimator = root.FindEx<Animator>("progress/mask");
            scoreIconAnimator = root.FindEx<Animator>("icon/ScoreIconRoot/scoreIcon");
            addNumAnimator = root.FindEx<Animator>("icon");
            animator = transform.GetComponent<Animator>();
        }
#endif

        public void Start()
        {
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
            btnInfo.onClick.AddListener(EntryClick);
            rewardIconBtn.onClick.AddListener(_OnTipsBtnClick);
        }

        public void OnEnable()
        {
            WhenUpdate ??= RefreshData;
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.SCORE_DATA_UPDATE>().AddListener(WhenUpdate);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(ScoreIconScaleAnimate);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(OnActEnd);
            MessageCenter.Get<MSG.SCORE_PROGRESS_ANIMATE>().AddListener(BeginProgressAnimate);
            MessageCenter.Get<MSG.SCORE_ADD_DEBUG>().AddListener(OnDebugAddScore);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
            MessageCenter.Get<MSG.SCORE_DATA_UPDATE>().AddListener(CheckScrollPos);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.SCORE_DATA_UPDATE>().RemoveListener(WhenUpdate);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(ScoreIconScaleAnimate);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(OnActEnd);
            MessageCenter.Get<MSG.SCORE_PROGRESS_ANIMATE>().RemoveListener(BeginProgressAnimate);
            MessageCenter.Get<MSG.SCORE_ADD_DEBUG>().RemoveListener(OnDebugAddScore);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
            MessageCenter.Get<MSG.SCORE_DATA_UPDATE>().RemoveListener(CheckScrollPos);
            if (routine != null)
            {
                Game.Instance.StopCoroutineGlobal(routine);
                routine = null;
            }
            listC.Clear();
            //当玩家离开棋盘时 尝试commit奖励 确保数据和显示正确
            if (activityScore != null)
            {
                activityScore.TryCommitReward();
                activityScore.PrevFinalMileStoneRewardId = 0;
                activityScore.PrevFinalMileStoneRewardCount = 0;
            }
        }

        private IEnumerator OnAdapterComplete()
        {
            yield return null;
            MessageCenter.Get<MSG.SCORE_ENTRY_POSITION_REFRESH>().Dispatch(group.transform.position);
        }

        private void RefreshData(int currValue, int targetValue)
        {
            listC.Add(currValue + "/" + targetValue);
            if (listC.Count <= 1)
            {
                Refresh(currValue, targetValue);
                addNum.text = (targetValue - currValue).ToString();
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
            if (activity is not ActivityScore)
                return;
            activityScore = (ActivityScore)activity;
            var valid = activityScore is { Valid: true, IsUnlock: true } && activityScore.HasCycleMilestone();
            Visible(valid);
            if (!valid) return;
            if (!UIManager.Instance.IsShow(UIConfig.UIScoreProgress))
                UIManager.Instance.OpenWindow(UIConfig.UIScoreProgress);
            RefreshTheme();
            Game.Instance.StartCoroutineGlobal(OnAdapterComplete());
            //刷新倒计时
            RefreshCD();
            //刷新进度条
            progress.Refresh(activityScore.CurShowScore, activityScore.CurMileStoneScore);
            //刷新当前里程碑奖励
            var r = activityScore.GetCurMileStoneReward();
            reward.Refresh(r);
            //刷新里程碑奖励是否可以查看详情
            var cfg = Game.Manager.objectMan.GetBasicConfig(r.Id);
            if (cfg != null)
            {
                var showTips = UIItemUtility.ItemTipsInfoValid(cfg.Id);
                info.gameObject.SetActive(showTips);
            }
            rewardIcon.transform.localScale = Vector3.one;
            //刷新当前积分活动 积分图标
            scoreIcon.SetImage(Game.Manager.objectMan.GetBasicConfig(activityScore.ConfD.RequireCoinId).Icon);
        }

        private void RefreshTheme()
        {
            activityScore.Visual.Refresh(bg, "bgImage");
            activityScore.Visual.Refresh(cdBg, "cdBg");
            activityScore.Visual.Refresh(progressBg, "bar1");
            activityScore.Visual.Refresh(progressValueImage, "bar2");

            activityScore.Visual.Refresh(reward.count, "rewardNum");
            activityScore.Visual.Refresh(progress.text, "num");
            activityScore.Visual.Refresh(addNum, "num");
            activityScore.Visual.RefreshStyle(cd, "cdColor");

        }

        private void RefreshProgress(int currentValue, int targetValue)
        {
            Game.Manager.activity.LookupAny(EventType.Score, out var activity);
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

        private IEnumerator Animate()
        {
            var list = activityScore.ListM;
            var next = activityScore.MilestoneNext((int)currentV);

            (ActivityScore.Node, int) Node(int v_, int count)
            {
                var finalScore = activityScore.ConfDetail.FinalMilestoneScore;
                var finalValue = activityScore.GetCyclePrevScore(currentV) + finalScore;
                ActivityScore.Node node;
                if (v_ < 0 || v_ >= list.Count)
                {
                    var r = new RewardConfig
                    {
                        Id = activityScore.PrevFinalMileStoneRewardId > 0
                            ? activityScore.PrevFinalMileStoneRewardId
                            : activityScore.RecordFinalMileStoneRewardId,
                        Count = activityScore.PrevFinalMileStoneRewardCount > 0
                            ? activityScore.PrevFinalMileStoneRewardCount
                            : activityScore.RecordFinalMileStoneRewardCount
                    };
                    node = new ActivityScore.Node() { reward = r, value = finalValue };
                }
                else
                    node = list[v_];

                var prev = next > -1 && v_ < list.Count
                    ? (next > 0 ? list[v_ - 1].value : 0)
                    : activityScore.GetCyclePrevScore(currentV);
                reward.Refresh(node.reward);
                return (node, prev);
            }

            ProgressEffect();
            addNum.gameObject.SetActive(true);
            addNumAnimator.SetTrigger("Punch");
            var (node, prev) = Node(next, 1);
            Progress(node.value, prev);
            while (currentV < targetV)
            {
                currentV += speed * Time.deltaTime;
                if (currentV >= node.value)
                {
                    var r = node.reward;
                    var commitData = activityScore.TryGetCommitReward(r);
                    OnMileStoneChanged(commitData);
                    Progress(node.value, prev);
                    var milestoneScore = node.value - prev;
                    progress.text.text = $"{milestoneScore}/{milestoneScore}";
                    yield return new WaitForSeconds(1.5f);
                    if (!activityScore.HasCycleMilestone())
                    {
                        MessageCenter.Get<MSG.ACTIVITY_ENTRY_LAYOUT_REFRESH>().Dispatch();
                        Visible(false);
                        yield break;
                    }
                    if (next >= 0)
                        ++next;
                    else
                    {
                        activityScore.SetCycleScoreCount();
                    }

                    if (next < 0)
                    {
                        //todo@eric 
                        activityScore.PrevFinalMileStoneRewardId = 0;
                        activityScore.PrevFinalMileStoneRewardCount = 0;
                    }

                    (node, prev) = Node(next, 1);
                }

                Progress(node.value, prev);
                var cfg = Game.Manager.objectMan.GetBasicConfig(node.reward.Id);
                if (cfg != null)
                {
                    bool showTips = UIItemUtility.ItemTipsInfoValid(cfg.Id);
                    info.gameObject.SetActive(showTips);
                }

                yield return null;
            }

            currentV = targetV;
            progress.Refresh(activityScore.CurShowScore, activityScore.CurMileStoneScore);
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
                addNum.text = (tar - cur).ToString();
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
            var v = activityScore.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
                Visible(false);
        }

        private void OnMileStoneChanged(RewardCommitData r)
        {
            animator.SetTrigger("Punch");
            rewardIcon.transform.DOScale(1.5f, 0.2f).OnComplete(() =>
            {
                rewardIcon.transform.localScale = Vector3.one;
                MessageCenter.Get<MSG.SCORE_FLY_REWARD_CENTER>().Dispatch((rewardIcon.transform.position, r,
                    activityScore));
            });
        }

        private void ScoreIconScaleAnimate(FlyType type)
        {
            if (type == FlyType.EventScore)
            {
                scoreIconAnimator.SetTrigger("Punch");
                Game.Manager.audioMan.TriggerSound("WhiteBall");
            }
        }

        private void ProgressEffect()
        {
            progressAnimator.SetTrigger("Punch");
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
            element.ignoreLayout = !v_;
        }

        private void EntryClick()
        {
            activityScore.Open();
        }

        private void _OnTipsBtnClick()
        {
            var r = activityScore.GetCurMileStoneReward();
            if (!UIItemUtility.ItemTipsInfoValid(r.Id))
            {
                return;
            }

            var icon = rewardIcon.image;
            var root = icon.rectTransform;
            UIItemUtility.ShowItemTipsInfo(r.Id, root.position, tipOffset + root.rect.size.y * 0.5f);
        }

        private void OnActEnd(ActivityLike act, bool expire)
        {
            if (act != activityScore)
                return;
            Visible(false);
        }

        private void BeginProgressAnimate()
        {
            if (!activityScore.HasCycleMilestone())
            {
                Visible(false);
                MessageCenter.Get<MSG.ACTIVITY_ENTRY_LAYOUT_REFRESH>().Dispatch();
                return;
            }
            if (routine != null)
                return;
            routine = Game.Instance.StartCoroutineGlobal(Animate());
        }

        private void OnDebugAddScore()
        {
            Game.Manager.activity.LookupAny(EventType.Score, out var activity);
            activityScore = (ActivityScore)activity;
            RefreshEntry(activityScore);
        }
        
        /// <summary>
        /// 在棋盘内时，只有在积分活动入口被划出屏幕外，划入积分活动进度条
        /// </summary>
        /// <param name="prevScore">增加前的积分</param>
        /// <param name="totalScore">增加后的积分</param>
        private void CheckScrollPos(int prevScore, int totalScore)
        {
            var trans = transform as RectTransform;
            var p = new Vector3(transform.position.x + (trans.rect.width / 2),
                transform.position.y,
                transform.position.z);
            var vp = Camera.main.ScreenToViewportPoint(p);
            // var screenPos = RectTransformUtility.WorldToScreenPoint(null, trans.position);
            if (vp.x <= 0)
                MessageCenter.Get<MSG.GAME_SCORE_GET_PROGRESS_BOARD>().Dispatch(prevScore, totalScore);
        }
    }
}