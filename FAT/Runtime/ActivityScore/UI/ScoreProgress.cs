/*
 * @Author: pengjian.zhang
 * @Description: 积分活动进度条- 当获取积分后 划入界面内的进度条组件 ue和本身在棋盘内的一致
 * @Date: 2024-2-28 15:13:44
 */

using UnityEngine;
using TMPro;
using EL;
using System;
using System.Collections;
using Config;
using DG.Tweening;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class ScoreProgress : MonoBehaviour
    {
        public GameObject group;
        public Transform info;
        public TMP_Text addNum;
        public TMP_Text cd;
        public UIImageRes cdBg;
        public MBRewardProgress progress;
        public UIImageRes progressBg;
        public UIImageRes progressValueImage;
        public MBRewardIcon reward;
        public UIImageRes scoreIcon;
        public UIImageRes bg;
        public Animator progressAnimator;
        public Animator scoreIconAnimator;
        public RectTransform rect;
        public float width;
        public Animator addNumAnimator;
        public float speed;
        public float duration = 1.2f;
        private Action whenCd;
        private ActivityScore activityScore;
        private int targetV;
        private float currentV;
        private bool addScoreByShop;
        private bool willSlide;
        private DG.Tweening.Core.TweenerCore<Vector2, Vector2, DG.Tweening.Plugins.Options.VectorOptions> doTween;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) 
                return;
            var root = transform.Find("group");
            group = root.gameObject;
            rect = (RectTransform)root;
            width = rect.rect.width;
            cd = root.FindEx<TextMeshProUGUI>("cdBg/cd");
            progressBg = root.FindEx<UIImageRes>("progress/back");
            cdBg = root.FindEx<UIImageRes>("cdBg");
            progress = root.FindEx<MBRewardProgress>("progress");
            addNum = root.FindEx<TextMeshProUGUI>("icon/addNum");
            progressAnimator = root.FindEx<Animator>("progress/mask");
            progressValueImage = root.FindEx<UIImageRes>("progress/mask/fore");
            scoreIconAnimator = root.FindEx<Animator>("icon/ScoreIconRoot/scoreIcon");
            addNumAnimator = root.FindEx<Animator>("icon");
            reward = root.FindEx<MBRewardIcon>("rewardbg");
            scoreIcon = root.FindEx<UIImageRes>("icon/ScoreIconRoot/scoreIcon");
            bg = root.FindEx<UIImageRes>("detailBg");
            info = root.FindEx<Transform>("rewardbg/info");
        }
#endif

        public void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_SCORE_GET_PROGRESS_BOARD>().RemoveListener(ShowProgress);
            MessageCenter.Get<MSG.GAME_SCORE_GET_PROGRESS_SHOP>().RemoveListener(ShowShopProgress);
            MessageCenter.Get<MSG.BOARD_AREA_ADAPTER_COMPLETE>().RemoveListener(RefreshAdapter);
            MessageCenter.Get<MSG.SCORE_ENTRY_POSITION_REFRESH>().RemoveListener(RefreshPosition);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(ScoreIconScaleAnimate);
            MessageCenter.Get<MSG.SCORE_PROGRESS_ANIMATE>().RemoveListener(BeginProgressAnimate);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(whenCd);
        }

        public void OnEnable()
        {
            Visible(false);
            whenCd ??= RefreshCD;
            MessageCenter.Get<MSG.GAME_SCORE_GET_PROGRESS_BOARD>().AddListener(ShowProgress);
            MessageCenter.Get<MSG.GAME_SCORE_GET_PROGRESS_SHOP>().AddListener(ShowShopProgress);
            MessageCenter.Get<MSG.BOARD_AREA_ADAPTER_COMPLETE>().AddListener(RefreshAdapter);
            MessageCenter.Get<MSG.SCORE_ENTRY_POSITION_REFRESH>().AddListener(RefreshPosition);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(ScoreIconScaleAnimate);
            MessageCenter.Get<MSG.SCORE_PROGRESS_ANIMATE>().AddListener(BeginProgressAnimate);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(whenCd);
        }

        private void RefreshAdapter(float scale)
        {
            group.transform.localScale = Vector3.one * scale;
        }

        private void RefreshPosition(Vector3 pos)
        {
            var position = group.transform.position;
            position = new Vector3(position.x, pos.y, position.z);
            group.transform.position = position;
        }

        //这个消息承接到是
        //1 棋盘入口 被划到屏幕外 悬浮进度条划出
        //2 切到meta界面 悬浮进度条划出
        private void ShowProgress(int currentValue, int targetValue)
        {
            if (!CanShowAnim())
            {
                return;
            }
            willSlide = true;
            if (doTween != null)
                doTween.Kill();
            Refresh(currentValue, targetValue);
            Game.Instance.StartCoroutineGlobal(CoScoreProgressMove());   
        }

        private IEnumerator CoScoreProgressMove()
        {
            //等待进度条表演 无论成功与否 2s后自动划出界面外 避免残留
            yield return new WaitForSeconds(3f);
            doTween = rect.DOAnchorPosX(-width * group.transform.localScale.x, 0.5f).OnComplete(
                () =>
                {
                    willSlide = false;
                    Visible(false);
                }
            );
        }

        private void ShowShopProgress(int currentValue, int targetValue)
        {
            if (!CanShowAnim())
            {
                return;
            }
            willSlide = true;
            addScoreByShop = true;
            if (doTween != null)
                doTween.Kill();
            Refresh(currentValue, targetValue);
            Game.Instance.StartCoroutineGlobal(CoScoreProgressMove());   
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
        }

        private void Refresh(int currentValue, int targetValue)
        {
            Game.Manager.activity.LookupAny(EventType.Score, out var activity);
            if (activity == null)
            {
                Visible(false);
                return;
            }

            targetV = targetValue;
            currentV = currentValue;
            //由于订单区域适配做了scale缩放 导致动画播完会残留 把scale影响算到动画偏移里
            rect.anchoredPosition = new(-width * group.transform.localScale.x, rect.anchoredPosition.y);

            CheckSpeed();
            Visible(true);
            activityScore = (ActivityScore)activity;
            var next = activityScore.MilestoneNext((int)currentV);
            var (node, prev) = Node(next, 1);
            Progress(node.value, prev);
            //不能直接偏移到0 因为订单区域所有元素适配后进行了缩放
            rect.DOAnchorPosX(width * group.transform.localScale.x - width, 0.5f).OnComplete((() =>
            {
                //进度条划入的两个场景：1商店购买2棋盘内入口被划出屏幕
                //商店内购买时 只有进度条的表演 即直接Animate 棋盘内也需要同步表演 （不再等待飞积分等等表现结束 直接发表演事件）
                //正常情况下 发表演事件时机为 棋盘内其他表现结束后
                if (addScoreByShop)
                {
                    MessageCenter.Get<MSG.SCORE_PROGRESS_ANIMATE>().Dispatch();
                    BeginProgressAnimate();
                }
            }));
            addNum.text = (targetValue - currentValue).ToString();
            RefreshTheme();
            scoreIcon.SetImage(Game.Manager.objectMan.GetBasicConfig(activityScore.ConfD.RequireCoinId).Icon);
            RefreshCD();
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

        private (ActivityScore.Node, int) Node(int v_, int count)
        {
            var list = activityScore.ListM;
            var finalScore = activityScore.ConfDetail.FinalMilestoneScore;
            var finalValue = activityScore.GetCyclePrevScore(currentV) + finalScore;
            ActivityScore.Node node;
            if (v_ < 0 || v_ >= list.Count)
            {
                var r = new RewardConfig
                {
                    Id = activityScore.RecordFinalMileStoneRewardId,
                    Count = activityScore.RecordFinalMileStoneRewardCount
                };
                node = new ActivityScore.Node() { reward = r, value = finalValue };
            }
            else
                node = list[v_];

            var prev = v_ > -1 && v_ < list.Count
                ? (v_ > 0 ? list[v_ - 1].value : 0)
                : activityScore.GetCyclePrevScore(currentV);
            //刷新里程碑奖励是否可以查看详情
            var cfg = Game.Manager.objectMan.GetBasicConfig(node.reward.Id);
            if (cfg != null)
            {
                var showTips = UIItemUtility.ItemTipsInfoValid(cfg.Id);
                info.gameObject.SetActive(showTips);
            }

            reward.Refresh(node.reward);
            return (node, prev);
        }
        
        private void Progress(int v_, int p_)
        {
            progress.RefreshSegment((int)Math.Floor(currentV), v_, p_, 0, p_);
        }
        
        private IEnumerator Animate()
        {
            Game.Manager.activity.LookupAny(EventType.Score, out var activity);
            if (activity == null)
            {
                Visible(false);
            }
            activityScore = (ActivityScore)activity;
            var next = activityScore.MilestoneNext((int)currentV);
            var (node, prev) = Node(next, 1);
            Progress(node.value, prev);
            // yield return new WaitForSeconds(0.5f);
            addNum.gameObject.SetActive(true);
            addNumAnimator.SetTrigger("Punch");
            ProgressEffect();
            while (currentV < targetV)
            {
                currentV += speed * Time.deltaTime;
                if (currentV >= node.value)
                {
                    Progress(node.value, prev);
                    var milestoneScore = node.value - prev;
                    progress.text.text = $"{milestoneScore}/{milestoneScore}";
                    yield return new WaitForSeconds(1.5f);
                    if (next >= 0)
                        ++next;
                    else
                    {
                        activityScore.SetCycleScoreCount();
                    }
                    // if (next >= list.Count) break;
                    (node, prev) = Node(next, 1);
                }

                Progress(node.value, prev);
                yield return null;
            }

            currentV = targetV;
            progress.Refresh(activityScore.CurShowScore, activityScore.CurMileStoneScore);
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

        private void BeginProgressAnimate()
        {
            if (!willSlide)
            {
                return;
            }
            if (!CanShowAnim())
            {
                return;
            }
            if (currentV > 0 || addScoreByShop)
            {
                Game.Instance.StartCoroutineGlobal(Animate());
                addScoreByShop = false;
            }
        }

        private bool CanShowAnim()
        {
            if (activityScore == null)
            {
                activityScore = Game.Manager.activity.LookupAny(EventType.Score) as ActivityScore;
                if (!activityScore.HasCycleMilestone())
                {
                    Visible(false);
                    MessageCenter.Get<MSG.ACTIVITY_ENTRY_LAYOUT_REFRESH>().Dispatch();
                    return false;
                }
            }
            return true;
        }
    }
}