using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;
using DG.Tweening;
using System.Collections;
using Spine.Unity;

namespace FAT {
    using static MessageCenter;
    using static PoolMapping;

    public class UIActivityDuelMain : UIBase {
        public struct Player {
            public TextMeshProUGUI name;
            public TextMeshProUGUI score;
            public RectTransform avatar;
            public SkeletonGraphic spine;
            public UIImageState icon;
            public CanvasGroup crown;
            public Animator crownA;
            public CanvasGroup bar;
            public Animator barA;
        }

        internal TextMeshProUGUI cd;
        internal MBRewardIcon prize;
        internal ActivityDuelMilestone milestoneA;
        internal ActivityDuelMilestone milestoneN;
        internal UIStateGroup state;
        internal Player playerL;
        internal Player playerR;
        internal SlideIcon slideR;
        internal float startY, finishY, rangeY;
        internal float xL, xR;
        public float speed = 100;
        public float endDelay = 1.2f;
        public float winDelay = 0.5f;
        public AnimationCurve moveCurve;
        public UIVisualGroup visualGroup;
        public bool closeOnInactive;

        private ActivityDuel activity;
        private Action WhenTick;
        private Action<int, int> ScoreUpdate;
        private BlockToken block = new();

        private void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            var root = transform.Find("Content");
            visualGroup.Prepare(root.Access<UIImageRes>("bg", try_:true), "bgPrefab");
            visualGroup.Prepare(root.Access<UIImageRes>("bg1"), "titleBg");
            visualGroup.Prepare(root.Access<UIImageRes>("finish"), "bg2");
            visualGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("active/desc"), "subTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("next/desc1"), "desc1");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("active/_milestone/desc"), "desc2");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("active/_milestone/index"), "round");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("next/_milestone/index"), "round");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
            visualGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("next/duel/text"), "duel");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("next/later/text"), "later");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("prize/count"), "prize");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("playerL/bar/score"), "scoreL");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("playerL/bar/name"), "nameL");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("playerR/bar/score"), "scoreR");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("playerR/bar/name"), "nameR");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate() {
            var root = transform.Find("Content");
            root.Access(out state);
            root.Access("prize", out prize);
            root.Access("_cd/text", out cd);
            root.Access("playerL/bar/name", out playerL.name);
            root.Access("playerL/bar/score", out playerL.score);
            root.Access("playerL/crown", out playerL.crown);
            root.Access("playerL/crown", out playerL.crownA);
            root.Access("playerL/bar", out playerL.bar);
            root.Access("playerL", out playerL.barA);
            root.Access("playerR/bar/name", out playerR.name);
            root.Access("playerR/bar/score", out playerR.score);
            root.Access("playerR/crown", out playerR.crown);
            root.Access("playerR/crown", out playerR.crownA);
            root.Access("playerR/bar", out playerR.bar);
            root.Access("playerR", out playerR.barA);
            root.Access("playerR/icon", out playerR.icon);
            root.Access("playerR/slide", out slideR);
            root.Access("active/avatarL", out playerL.avatar);
            root.Access("active/avatarR", out playerR.avatar);
            root.Access("active/avatarL/pf_ufo/pf_ufo", out playerL.spine);
            root.Access("active/avatarR/pf_ufo/pf_ufo", out playerR.spine);
            root.Access("active/_milestone", out milestoneA);
            root.Access("next/_milestone", out milestoneN);
            root.Access("start", out RectTransform start);
            root.Access("finish", out RectTransform finish);
            startY = start.anchoredPosition.y;
            finishY = finish.anchoredPosition.y;
            xL = playerL.avatar.anchoredPosition.x;
            xR = playerR.avatar.anchoredPosition.x;
            root.Access("close", out MapButton close);
            root.Access("info", out MapButton info);
            close.WhenClick = Close;
            info.WhenClick = Info;
            root.Access("next/duel", out MapButton duel);
            root.Access("next/later", out MapButton later);
            duel.WhenClick = Duel;
            later.WhenClick = Later;
            WhenTick ??= RefreshCD;
            ScoreUpdate ??= (_, _) => RefreshPlayer();
            playerL.crown.gameObject.SetActive(false);
            playerR.crown.gameObject.SetActive(false);
            playerL.bar.gameObject.SetActive(false);
            playerR.bar.gameObject.SetActive(false);
            slideR.gameObject.SetActive(false);
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivityDuel)items[0];
        }

        protected override void OnPreOpen() {
            RefreshTheme();
            RefreshRound();
            RefreshPlayer();
            RefreshCD();
            Get<ACTIVITY_DUEL_SCORE>().AddListener(ScoreUpdate);
            Get<ACTIVITY_DUEL_ROBOT_SCORE>().AddListener(ScoreUpdate);
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPreClose() {
            Get<ACTIVITY_DUEL_SCORE>().RemoveListener(ScoreUpdate);
            Get<ACTIVITY_DUEL_ROBOT_SCORE>().RemoveListener(ScoreUpdate);
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public void Update() {
            if (!closeOnInactive) return;
            if (activity.IsComplete()) return;
            if (!activity.Active) Close();
        }

        public void RefreshTheme() {
            var visual = activity.VisualMain;
            visual.Refresh(visualGroup);
            playerL.name.text = I18N.Text("#SysComDesc459");
            playerR.name.text = I18N.FormatText("#SysComDesc431", 1);
        }

        public void RefreshRound() {
            var r = activity.RoundReward;
            prize.Refresh(r);
            state.Enabled(activity.VisualActive);
            milestoneA.Refresh(activity);
            milestoneN.Refresh(activity);
            var visual = activity.VisualMain;
            visual.visual.RefreshText(visualGroup, "subTitle", activity.visualTargetScore, UIUtility.FormatTMPString(activity.Conf.TokenId));
        }

        public void RefreshPlayer() {
            var sL = activity.visualScore;
            var sR = activity.visualRobotScore;
            playerR.icon.Select(activity.robotIcon);
            var sN = (float)activity.visualTargetScore;
            playerL.avatar.anchoredPosition = new(xL, PosY(sL / sN));
            playerR.avatar.anchoredPosition = new(xR, PosY(sR / sN));
            var nL = activity.GetPlayerScore();
            var nR = activity.GetRobotScore();
            var active = activity.VisualActive;
            A(playerL.bar, playerL.barA, active);
            A(playerR.bar, playerR.barA, active);
            playerR.icon.gameObject.SetActive(true);
            slideR.gameObject.SetActive(false);
            Animate(playerL, "idle", refresh_:false);
            Animate(playerR, "idle", refresh_:false);
            RefreshPlayerS(nL, nR);
            if (nL != sL || nR != sR) {
                Animate();
            }
        }

        public static void A(CanvasGroup g_, Animator a_, bool b_) {
            var a = g_.gameObject.activeSelf && g_.alpha > 0;
            if (a == b_) {
                a_.Play("Common_Panel_Idle");
                g_.gameObject.SetActive(b_);
                return;
            }
            g_.gameObject.SetActive(true);
            var anim = b_ ? "Common_Panel_MiddleShow" : "Common_Panel_MiddleHide";
            a_.Play(anim);
        }

        public void RefreshPlayerS(int sL, int sR) {
            var active = activity.VisualActive;
            playerL.score.text = $"{sL}";
            A(playerL.crown, playerL.crownA, active && sL > 0 && sL >= sR);
            playerR.score.text = $"{sR}";
            A(playerR.crown, playerR.crownA, active && sR > 0 && sR > sL);
        }

        public void RefreshNextRound() {
            activity.SyncScore();
            RefreshRound();
            RefreshPlayer();
            if (!activity.Active || !activity.RoundValid) Close();
        }

        public void RefreshCD() {
            var t = Game.TimestampNow();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
        }

        public void Animate(Player player_, string anim_, bool loop_ = true, bool refresh_ = true) {
            var s = player_.spine.AnimationState;
            var t = s.GetCurrent(0);
            if (t != null && !refresh_) {
                t.Loop = false;
                s.AddAnimation(0, anim_, loop_, 0);
            }
            else {
                s.SetAnimation(0, anim_, loop_);
            }
        }

        public float Remain(Player player_) {
            var track = player_.spine.AnimationState.GetCurrent(0);
            return track.AnimationEnd - track.AnimationTime;
        }

        public void Animate() {
            var sL = activity.visualScore;
            var sR = activity.visualRobotScore;
            var nL = activity.GetPlayerScore();
            var nR = activity.GetRobotScore();
            var pA = nL >= nR;
            var sN = (float)activity.visualTargetScore;
            DOTween.Kill(playerL.avatar, complete:true);
            DOTween.Kill(playerR.avatar, complete:true);
            if (sN == 0) return;
            var audio = Game.Manager.audioMan;
            var aL = 0;
            void M(Player playerA, Player playerB, int nA) {
                block.Enter(wait_:false);
                Animate(playerA, "move");
                audio.TriggerSound("DuelRise");
                ++aL;
                playerA.avatar.DOAnchorPosY(PosY(nA / sN), speed).SetSpeedBased().SetEase(moveCurve).OnComplete(() => {
                    if (--aL <= 0) audio.StopLoopSound();
                    if (pA && nA >= sN) {
                        DOVirtual.DelayedCall(Remain(playerA), () => {
                            Animate(playerA, "right", false);
                            Animate(playerB, "fall", false);
                        });
                    }
                    else {
                        block.Exit();
                        DOVirtual.DelayedCall(Remain(playerA), () => {
                            Animate(playerA, "idle");
                        });
                    }
                    if (nA >= sN) {
                        DOVirtual.DelayedCall(winDelay, () => {
                            RoundEnd(playerA.avatar == playerL.avatar);
                        });
                    }
                });
            }
            if (nL != sL) {
                M(playerL, playerR, nL);
            }
            if (nR != sR) {
                M(playerR, playerL, nR);
            }
            if (nR < sN && nL < sN) {
                activity.SyncScore();
            }
        }

        public void RoundEnd(bool win_) {
            void R() {
                block.Exit();
                if (win_ && activity.ScoreCommitReward == null) return;
                var ui = activity.VisualResult.res.ActiveR;
                ui.Open(activity, win_, (Action)RefreshNextRound);
            }
            DOVirtual.DelayedCall(endDelay, R);
        }

        public float PosY(float p_) => Mathf.Lerp(startY, finishY, p_);

        internal void Info() {
            UIManager.Instance.OpenWindow(activity.VisualHelp.res.ActiveR);
        }

        public void AnimateR() {
            IEnumerator R() {
                slideR.gameObject.SetActive(true);
                playerR.icon.gameObject.SetActive(false);
                slideR.Preview(activity.robotIcon);
                Game.Manager.audioMan.TriggerSound("DuelRoll");
                while (slideR.preview) yield return null;
                slideR.gameObject.SetActive(false);
                playerR.icon.gameObject.SetActive(true);
            }
            StartCoroutine(R());
        }

        internal void Duel() {
            activity.SetRoundStart();
            RefreshNextRound();
            AnimateR();
        }

        internal void Later() {
            Close();
        }
    }
}