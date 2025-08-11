using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using fat.rawdata;
using TMPro;
using FAT.MSG;
using fat.msg;

namespace FAT
{
    using static MessageCenter;

    public class UIActivityRanking : UIBase
    {
        public struct Rank
        {
            public UIStateGroup state;
            public UIImageState select;
            public TextMeshProUGUI name;
            public TextMeshProUGUI score;
            public UIImageRes token;
            public MBRewardLayout reward;
            public GameObject effect1;
            public GameObject effect2;
            public Animator anim;
        }

        internal UITextState cd;
        internal MapButton confirm;
        internal MBRankingScroll scroll;
        internal Rank rank1;
        internal Rank rank2;
        internal Rank rank3;
        public UIVisualGroup vGroup;
        public float offsetY;

        private ActivityRanking activity;
        private Action<ActivityRanking, RankingType> WhenRefresh;
        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;

        private MBRewardProgress milestoneProgress;   //里程碑奖励进度条
        private MBRewardIcon MilestonReward;   //里程碑奖励图标

        public static Rank ParseRank(Transform root_)
        {
            root_.Access("root", out Transform root);
            var n = new Rank()
            {
                effect1 = root.TryFind("Effect01"),
                effect2 = root.TryFind("Effect02")
            };
            root_.Access(out n.state);
            root.Access("name", out n.name);
            root.Access("score", out n.score);
            root.Access("token", out n.token);
            root.Access("_group", out n.reward);
            root.Access("bgS", out n.select);
            root.Access(out n.anim);
            return n;
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out vGroup);
            var root = transform.Find("Content");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("title"), "mainTitle");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
            vGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("confirm/text"), "confirm");
            vGroup.CollectTrim();
            var list = (RectTransform)root.Find("list");
            offsetY = list.anchoredPosition.y - list.sizeDelta.y;
        }

        protected override void OnCreate()
        {
            var root = transform.Find("Content");
            root.Access("_cd/text", out cd);
            rank1 = ParseRank(root.Find("rank1"));
            rank2 = ParseRank(root.Find("rank2"));
            rank3 = ParseRank(root.Find("rank3"));
            root.Access("list/RankingListScroll", out scroll);
            root.Access("close", out MapButton close);
            root.Access("info", out MapButton info);
            root.Access("confirm", out confirm);
            root.Access("progress", out MapButton progressBtnClick);
            root.FindEx<MBRewardProgress>("progress", out milestoneProgress);
            MilestonReward = root.FindEx<MBRewardIcon>("progress/RightImg/_group/anchor/entry");
            progressBtnClick.WhenClick = ProgressClickEvent;
            close.WhenClick = Close;
            info.WhenClick = InfoClick;
            confirm.WhenClick = Confirm;
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
            WhenRefresh ??= (e_, t_) =>
            {
                if (e_ != activity) return;
                RefreshList();
                if (activity != null)
                {
                    if (activity.IsValidRankMileStoneActivity())
                    {
                        RefreshMilestoneProgress();
                    }
                }
            };
        }

        protected override void OnParse(params object[] items)
        {
            activity = (ActivityRanking)items[0];
            scroll.InitContext(activity);
        }

        protected override void OnPreOpen()
        {
            activity.Sync();
            if (!activity.Active) DataTracker.RankingEndUI1(activity);
            milestoneProgress.gameObject.SetActive(false);
            RefreshTheme();
            RefreshList();
            RefreshCD();
            Get<ACTIVITY_END>().AddListener(WhenEnd);
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            Get<ACTIVITY_RANKING_DATA>().AddListener(WhenRefresh);
        }

        protected override void OnPreClose()
        {
            Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            Get<ACTIVITY_RANKING_DATA>().RemoveListener(WhenRefresh);
        }

        public void RefreshTheme()
        {
            var visual = activity.VisualRanking;
            visual.Refresh(vGroup);
        }

        public void RefreshList()
        {
            RefreshSize();
            RefreshRank(false);
            scroll.UpdateInfoList(activity, RefreshRank);
            var valid = activity.RankingValid();
            milestoneProgress.gameObject.SetActive(activity.IsValidRankMileStoneActivity() && valid);
            if (activity.IsValidRankMileStoneActivity())
            {
                RefreshMilestoneProgress();
            }
        }

        public void RefreshSize()
        {
            var valid = activity.RankingValid();
            confirm.gameObject.SetActive(!valid);
            var rect = (RectTransform)scroll.transform.parent;
            var p = rect.anchoredPosition;
            var d = rect.sizeDelta;
            d.y = p.y - (valid ? 0 : offsetY);
            rect.sizeDelta = d;
        }

        public void RefreshRank(bool effect_)
        {
            var cache = activity.Cache;
            var list = cache.Data?.Players;
            var me = cache.Data?.Me?.RankingOrder ?? 0;
            var r3 = me > 0 && me < 4;
            var rUp = effect_ && activity.CheckRankUp();

            void R(int n_, Rank t_)
            {
                var rList = activity.reward;
                if (n_ >= rList.Count)
                    t_.reward.RefreshEmpty(0);
                else
                    t_.reward.Refresh(rList[n_]);
                var valid = list != null && list.Count > n_;
                t_.state.Select(valid ? 0 : 1);
                if (!valid) return;
                var n = list[n_];
                var m = n.RankingOrder == me;
                t_.name.text = m
                    ? I18N.Text("#SysComDesc459")
                    : I18N.FormatText("#SysComDesc629", n.Player.Fpid);
                t_.score.text = $"{n.Score}";
                m = m && effect_;
                t_.select.Enabled(m);
                t_.effect1.SetActive(m);
                if (r3 && rUp)
                {
                    t_.effect2.SetActive(m);
                    if (m) t_.anim.Play("UIRanking_Punch");
                    else t_.anim.Play("UIRanking_Delay");
                }
                else
                {
                    t_.anim.Play("UIRanking_Idle");
                }
            }

            R(0, rank1);
            R(1, rank2);
            R(2, rank3);
        }

        public void RefreshCD()
        {
            var t = Game.TimestampNow();
            var diff = activity.endTS - t;
            if (diff <= 0) cd.Select(0);
            else cd.text.text = activity.entry.TextCD(diff);
        }

        internal void RefreshEnd(ActivityLike acti_, bool expire_)
        {
            if (acti_ != activity || !expire_) return;
            Close();
        }

        internal void InfoClick()
        {
            activity.OpenHelp();
        }

        internal void Confirm()
        {
            Close();
            GameProcedure.SceneToMerge();
        }


        /// <summary>
        /// 刷新里程碑奖励进度
        /// </summary>
        private void RefreshMilestoneProgress()
        {
            var cache = activity.Cache;
            var playerData = cache.Data?.Me;
            if (playerData == null)
            {
                DebugEx.Info("ranking playerData is null");
                return;
            }

            DebugEx.Info("playerRankingScore " + playerData.Score);
            milestoneProgress.gameObject.SetActive(true);
            int curMilestonScore = activity.GetCurMilestonScore();
            if (curMilestonScore == -1)
            {
                DebugEx.Info("curMilestonScore is Invalid");
                return;
            }
            if (activity.IsCompleteMaxMilestoneScore(playerData.Score))
            {
                DebugEx.Info("finish  Maxmilestone");
                MilestonReward.RefreshEmpty();
                milestoneProgress.Refresh(playerData.Score, curMilestonScore);
                milestoneProgress.text.text = I18N.Text("#SysComDesc1046");
            }
            else
            {
                milestoneProgress.Refresh(playerData.Score, curMilestonScore);
                var curMileStoneReward = activity.GetCurMileStoneReward();
                if (curMileStoneReward == null)
                {
                    DebugEx.Info("curMileStoneReward Data is null");
                    return;
                }
                MilestonReward.Refresh(curMileStoneReward);
            }
        }
        private void ProgressClickEvent()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIActivityRankMilestone, activity);
        }
    }
}