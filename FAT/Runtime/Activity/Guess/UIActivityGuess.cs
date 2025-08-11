using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;
using DG.Tweening;
using fat.rawdata;
using Spine.Unity;

namespace FAT {
    using static MessageCenter;

    public class UIActivityGuess : UIBase {
        internal ActivityGuessMilestone milestone;
        internal TextMeshProUGUI cd;
        internal TextMeshProUGUI token;
        internal TextMeshProUGUI reward;
        internal MBRewardLayout answerLayout;
        internal SkeletonGraphic role;
        internal GameObject cheer;
        internal readonly List<GuessAnswerEntry> answer = new();
        internal readonly List<GuessHandEntry> hand = new();
        public readonly List<GuessAnswer> answerC = new();
        public readonly List<GuessHand> handC = new();
        public List<GuessAnswer> answerV;
        public List<GuessHand> handV;
        public UIVisualGroup visualGroup;
        public float handSpacing = -10;
        public float handSpacingE = -5;
        internal Vector3 handOffset;
        internal Vector2 handSize;
        internal float handOffsetR;
        internal float recordOffset;
        public float recordSpacing = -24;
        public float recordPunch = 0.2f;
        public float toHandDuration = 0.8f;
        public AnimationCurve toHandCurve;
        public float toAnswerDuration = 0.8f;
        public AnimationCurve toAnswerCurve;
        public float matchOffset = 0.4f;
        public float cheerOffset = 0f;
        public float recordDuration = 0.3f;
        public float returnDuration1 = 0.4f;
        public float returnDuration2 = 0.3f;
        public float checkDelay = 0.25f;
        public float resumeDelay = 0.4f;
        public float returnDelay = 0.8f;
        public float milestoneDelay = 0.5f;
        public float nextLevelDelay = 0.8f;
        public float completeD1 = 0.8f;
        public float completeD2 = 0.5f;
        public float completeD3 = 1.2f;
        public float incorrectD1 = 2.2f;
        public float incorrectD2 = 1.8f;
        private ActivityGuess activity;
        private AnswerAccess answerList;
        private Action WhenTick;
        private Action<ActivityLike, bool> WhenEnd;
        private Action<int, int> WhenTokenChange;
        private Action<int, int> WhenMilestoneChange;
        private Action<bool> WhenNetworkChange;
        private Action<MBRewardIcon, (int, int), bool> FillAnswerRef;
        private Action WhenRecordClick;
        private readonly BlockToken block = new();
        private string nameHand;

        private void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            var root = transform.Find("Content");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("cd"), "time");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("preview/group/title"), "prize");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate() {
            var root = transform.Find("Content");
            root.Access("cd", out cd);
            root.Access("token/text", out token);
            root.Access("reward/count", out reward);
            root.Access("_milestone", out milestone);
            root.Access("answer", out answerLayout);
            root.Access("pf_guess_color_role/pf_guess_color_role_spine", out role);
            cheer = root.TryFind("Effect04");
            answerLayout.list[0].Access("record/_card", out RectTransform recordT);
            recordOffset = recordT.anchoredPosition.y;
            var template = root.Find("hand/wrap");
            nameHand = template.name;
            template.name = $"{nameHand}0";
            var e = new GuessHandEntry(template);
            hand.Add(e);
            var er = e.anchor;
            handOffset = er.localPosition;
            handSize = er.sizeDelta;
            root.Access("info", out MapButton info);
            info.WhenClick = () => activity.Open(activity.VisualHelp.res);
            root.Access("close", out MapButton close);
            close.WhenClick = Exit;
            WhenTick ??= RefreshCD;
            WhenEnd ??= RefreshEnd;
            WhenTokenChange ??= (o_, n_) => RefreshToken(n_);
            WhenMilestoneChange ??= (o_, n_) => milestone.TargetM(n_, delay_:milestoneDelay);
            WhenNetworkChange ??= v_ => { if(v_) Exit(); };
            FillAnswerRef ??= FillAnswer;
            WhenRecordClick ??= () => Game.Manager.commonTipsMan.ShowPopTips(Toast.GuessRecordTap);
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivityGuess)items[0];
            answerList = new() { list = activity.answer };
        }

        protected override void OnPreOpen() {
            RefreshTheme();
            RefreshCD();
            RefreshToken(activity.Token);
            RefreshMilestone();
            RefreshTable();
            RoleIdle();
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            Get<ACTIVITY_END>().AddListener(WhenEnd);
            Get<ACTIVITY_GUESS_TOKEN>().AddListener(WhenTokenChange);
            Get<ACTIVITY_GUESS_MILESTONE>().AddListener(WhenMilestoneChange);
            Get<GAME_NETWORK_WEAK>().AddListener(WhenNetworkChange);
            Game.Manager.audioMan.PlayBgm("GuessBgm");
        }

        protected override void OnPostOpen() {
            if (!activity.Active) {
                Exit();
                return;
            }
            if (activity.AnswerReady) CheckAnswer();
        }

        protected override void OnPreClose() {
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            Get<ACTIVITY_GUESS_TOKEN>().RemoveListener(WhenTokenChange);
            Get<ACTIVITY_GUESS_MILESTONE>().RemoveListener(WhenMilestoneChange);
            Get<GAME_NETWORK_WEAK>().RemoveListener(WhenNetworkChange);
        }

        protected override void OnPostClose() {
            Game.Manager.audioMan.PlayDefaultBgm();
        }

        internal void Exit() {
            activity.Exit(ResConfig);
        }

        public void Update() {
            milestone.TargetStep();
        }

        internal void RefreshEnd(ActivityLike acti_, bool expire_) {
            if (acti_ != activity || !expire_) return;
            Exit();
        }

        public void RefreshTheme() {
            var visual = activity.Visual;
            visual.Refresh(visualGroup);
            cheer.SetActive(false);
        }

        public void RefreshReward() {
            var r = activity.level.reward[0];
            reward.text = $"{activity.MilestoneSprite}+{r.Count}";
        }

        public void RefreshCD() {
            var diff = (long)Mathf.Max(0, activity.Countdown);
            UIUtility.CountDownFormat(cd, diff);
        }

        public void RefreshToken(int v_) {
            token.text = $"{activity.TokenSprite}{v_}";
        }

        public void RefreshMilestone() {
            milestone.Refresh(activity, block);
        }

        public void RefreshTable() {
            RefreshReward();
            Sync();
            RefreshAnswer();
            RefreshHand();
        }

        public void RefreshTableExceptHand() {
            RefreshReward();
            Sync();
            RefreshAnswer();
        }

        public void RefreshLevel() {
            var (s, e) = (handSpacing, handSpacingE);
            (handSpacing, handSpacingE) = (0, 0);
            RefreshTable();
            (handSpacing, handSpacingE) = (s, e);
            RefreshOffset();
        }

        public void RefreshOffset() {
            handOffsetR = HandOffset(activity.handCount);
        }

        public void Sync() {
            answerV = activity.answer;
            handV = activity.hand;
        }

        public void RefreshAnswer() {
            var count = answerV.Count;
            answer.Clear();
            answerLayout.RefreshActive(count);
            answerLayout.RefreshList(answerList, Refresh_:FillAnswerRef);
            answerLayout.RenameByIndex();
        }

        public void FillAnswer(MBRewardIcon e_, (int index, int) n_, bool delay_) {
            var i = n_.index;
            var e = new GuessAnswerEntry(e_.transform);
            e.button.WhenClick = () => AnswerClick(i);
            e.card.image.raycastTarget = true;
            e.effect.SetActive(false);
            answer.Add(e);
            FillAnswer(i);
            FillRecord(i);
        }

        public void FillAnswer(int i_) {
            var n = answerV[i_];
            var e = answer[i_];
            e.slot.Enabled(activity.AnswerNext == i_);
            e.card.Select(n.bet);
            e.state.Enabled(n.match);
            e.match.Enabled(n.match);
        }

        public void MatchAnswer(int i_) {
            var n = answerV[i_];
            var e = answer[i_];
            e.state.Enabled(true);
            e.match.Enabled(n.match);
        }

        public void FillRecord(int i_) {
            var n = answerV[i_];
            var e = answer[i_];
            var count = answerV.Count;
            e.AlignRecord(count);
            var recordV = n.record;
            var c = 0;
            while (recordV > 0) {
                var r = e.record[c++].image;
                var d = recordV % 10;
                recordV /= 10;
                r.Select(d);
            }
            for (var k = 0; k < c; ++k) {
                var r = e.record[k];
                r.WhenClick = WhenRecordClick;
                var rect = (RectTransform)r.transform;
                var p = c - 1 - k;
                rect.anchoredPosition = new(0, recordOffset + recordSpacing * p);
                rect.SetSiblingIndex(p);
            }
            for (var k = c; k < count; ++k) {
                var r = e.record[k].image;
                r.Select(0);
            }
        }

        public void AnswerClick(int i_) {
            if (!activity.ToHand(i_, out var ii, out var ni)) return;
            FillAnswer(i_);
            if (i_ != ni && ni < answerV.Count) FillAnswer(ni);
            FillHand(ii);
            var n = hand[ii];
            var g = n.card.image;
            var h = n.anchor;
            var a = answer[i_].anchor;
            g.raycastTarget = false;
            void End() {
                g.raycastTarget = true;
            }
            AnimateMove(h, a, h, toHandCurve, toHandDuration, 0, End);
        }

        public void AnswerReturn(int i_, int ii_, float duration_, float delay_, TweenCallback End_) {
            void F() {
                FillAnswer(i_);
                FillHand(ii_);
                DebugEx.Info($"{nameof(ActivityGuess)} return answer {i_} {answerV[i_].value} hand {ii_} {handV[ii_].value}");
                var n = hand[ii_];
                var r = n.root;
                var g = n.card.image;
                var h = n.anchor;
                var a = answer[i_].anchor;
                r.rotation = Quaternion.identity;
                g.raycastTarget = false;
                AnimateMove(h, a, h, toHandCurve, duration_, 0, End_);
            }
            if (delay_ == 0) F();
            else DOVirtual.DelayedCall(delay_, F);
        }

        public void AnswerReturnEnd(int ii_) {
            var h = handV[ii_];
            DebugEx.Info($"{nameof(ActivityGuess)} return end hand {ii_} {h.value}");
            var n = hand[ii_];
            var r = n.root;
            var g = n.card.image;
            var c = n.anchor;
            g.raycastTarget = true;
            DOTween.Kill(c, complete:true);
            r.DORotateQuaternion(HandRotation(h.place), returnDuration2).SetId(c);
        }

        public void AnswerResume(int i_) {
            if (!activity.ToHand(i_, out var ii, out _)) return;
            AnswerReturn(i_, ii, returnDuration1, resumeDelay, () => AnswerReturnEnd(ii));
        }

        public float HandOffset(int count_) {
            var r = -count_ / 2 * handSpacing;
            if (count_ % 2 == 0) r += handSpacingE;
            return r;
        }

        public void RefreshHand() {
            var count = handV.Count;
            RefreshOffset();
            AlignHand(count);
            for (var k = 0; k < count; ++k) {
                var e = hand[k];
                var kk = k;
                e.button.WhenClick = () => HandClick(kk);
                FillHand(k);
            }
        }

        public void AlignHand(int count_) {
            for (var k = hand.Count; k < count_; ++k) {
                var template = hand[0];
                var obj = GameObject.Instantiate(template.root.gameObject, template.root.parent);
                obj.name = $"{nameHand}{k}";
                var e = new GuessHandEntry(obj.transform);
                hand.Add(e);
            }
            for (var k = count_; k < hand.Count; ++k) {
                hand[k].card.Select(0);
            }
        }

        public void FillHand(int k_) {
            var n = handV[k_];
            var e = hand[k_];
            var v = n.value;
            e.card.Select(v);
            e.card.image.raycastTarget = v > 0;
            RevertHand(e, n.place);
        }

        public Quaternion HandRotation(int k_) => Quaternion.Euler(0, 0, handOffsetR + k_ * handSpacing);

        public void RevertHand(GuessHandEntry e_, int k_) {
            var er = e_.anchor;
            er.localPosition = handOffset;
            er.localRotation = Quaternion.identity;
            er.sizeDelta = handSize;
            e_.root.localRotation = HandRotation(k_);
        }

        public void HandClick(int i_) {
            if (!activity.ToAnswer(i_, out var ii, out var ni)) {
                if (!activity.TokenReady) {
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.GuessNoToken, activity.TokenSprite);
                }
                return;
            }
            var n = answer[ii];
            var g = n.card.image;
            var m = hand[i_];
            var eff = n.effect;
            var h = m.anchor;
            var a = n.anchor;
            var next = activity.AnswerNext;
            g.raycastTarget = false;
            eff.SetActive(false);
            void End() {
                FillHand(i_);
                FillAnswer(ii);
                g.raycastTarget = true;
                eff.SetActive(true);
                if (next != activity.AnswerNext) return;
                if (ii != ni && ni < answerLayout.count) FillAnswer(ni);
                if (activity.AnswerReady) CheckAnswer();
            }
            Game.Manager.audioMan.TriggerSound("GuessPutOneCard");
            AnimateMove(h, h, a, toAnswerCurve, toAnswerDuration, 0, End);
        }

        public void CheckAnswer() {
            block.Enter(wait_:false);
            if (!activity.CheckRecord()) {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.GuessDuplicate);
                for (var k = 0; k < answerV.Count; ++k) {
                    if (answerV[k].match) continue;
                    AnswerResume(k);
                }
                var d33 = resumeDelay + returnDuration1 + returnDuration2;
                DOVirtual.DelayedCall(d33, () => {
                    block.Exit();
                });
                goto incorrect;
            }
            var (ready, complete, change, rewardR) = activity.CheckAnswer();
            if (!ready) goto incorrect;
            if (complete) CacheTable(change);
            RefreshOffset();
            var audio = Game.Manager.audioMan;
            var count = change.Count;
            var delay = checkDelay;
            var d11 = RoleCheck();
            var dC = count * delay;
            var d12 = Mathf.Max(d11 + returnDelay, dC);
            var d13 = d12 + returnDuration1;
            for (var k = 0; k < change.Count; ++k) {
                var c = change[k];
                if (!c.match) {
                    var ii = c.from;
                    AnswerReturn(k, ii, returnDuration1, d12, () => AnswerReturnEnd(ii));
                }
                if (c.changeR) AnimateRecord(k, 0, d13);
                if (c.change || !c.match) {
                    AnimateMatch(k, k * delay);
                }
            }
            if (complete) {
                DOVirtual.DelayedCall(completeD1, () => audio.TriggerSound("GuessLevelSuccess"));
                DOVirtual.DelayedCall(completeD2, () => audio.TriggerSound("GuessAmazing"));
                DOVirtual.DelayedCall(completeD3, () => audio.TriggerSound("GuessHandClap"));
                var d21 = d11 + dC + matchOffset;
                var d22 = d21 + nextLevelDelay;
                var d23 = d22 + returnDuration1;
                var d24 = d11 + dC + cheerOffset;
                RoleResult("correct");
                cheer.SetActive(false);
                DOVirtual.DelayedCall(d21, () => {
                    VibrationManager.VibrateMedium();
                    UIFlyUtility.FlyRewardList(rewardR.obj, reward.transform.position);
                    rewardR.Free();
                    CacheComplete(change);
                });
                DOVirtual.DelayedCall(d24, () => {
                    cheer.SetActive(true);
                });
                DOVirtual.DelayedCall(d22, () => {
                    audio.TriggerSound("GuessPutCards");
                    for (var k = 0; k < change.Count; ++k) {
                        var ii = change[k].from;
                        AnswerReturn(k, ii, returnDuration1, 0, null);
                    }
                    RefreshTableExceptHand();
                });
                DOVirtual.DelayedCall(d23, () => {
                    block.Exit();
                    audio.TriggerSound("GuessExpandCards");
                    RefreshLevel();
                    for (var k = 0; k < handV.Count; ++k) {
                        AnswerReturnEnd(k);
                    }
                });
                return;
            }
            else {
                DOVirtual.DelayedCall(incorrectD1, () => audio.TriggerSound("GuessLevelFail"));
                DOVirtual.DelayedCall(incorrectD2, () => audio.TriggerSound("GuessSigh"));
                DOVirtual.DelayedCall(d12, () => {
                    audio.TriggerSound("GuessPutCards");
                });
                DOVirtual.DelayedCall(d13, () => {
                    block.Exit();
                    audio.TriggerSound("GuessExpandCards");
                });
            }
            incorrect:
            RoleResult("error");
        }

        public void CacheTable(List<GuessCheck> change_) {
            var count = change_.Count;
            for (var k = answerC.Count; k < count; ++k) {
                answerC.Add(new());
                handC.Add(new());
            }
            for (var k = 0; k < count; ++k) {
                var ca = answerC[k];
                ca.Copy(change_[k]);
                var ch = handC[k];
                ch.Copy(change_[k]);
            }
            answerV = answerC;
            handV = handC;
        }

        public void CacheComplete(List<GuessCheck> change_) {
            var count = change_.Count;
            for (var k = 0; k < count; ++k) {
                var ca = answerC[k];
                var ch = handC[k];
                ch.value = ca.bet;
                ca.bet = 0;
                ca.match = false;
            }
        }

        public void AnimateMove(RectTransform target_, RectTransform from_, RectTransform to_, AnimationCurve curve_, float duration_, float delay_, TweenCallback End_) {
            var pe = to_.position + (Vector3)to_.rect.center;
            var re = to_.rotation;
            var ps = from_.position + (Vector3)from_.rect.center;
            var rs = from_.rotation;
            var se = to_.sizeDelta;
            var ss = from_.sizeDelta;
            target_.position = ps;
            target_.rotation = rs;
            target_.sizeDelta = ss;
            DOTween.Kill(target_, complete:true);
            target_.DOMove(pe, duration_).SetEase(curve_).SetDelay(delay_).SetId(target_).OnComplete(End_);
            target_.DORotateQuaternion(re, duration_).SetDelay(delay_).SetId(target_);
            target_.DOSizeDelta(se, duration_).SetEase(Ease.InFlash).SetDelay(delay_).SetId(target_);
        }

        public void AnimateMatch(int k_, float d_) {
            var e = answer[k_];
            var s = e.anim;
            DOTween.Kill(s, complete:true);
            DOVirtual.DelayedCall(d_, () => {
                MatchAnswer(k_);
                var n = answerV[k_];
                s.Play("UIActivityGuess_Right_Show");
                var ss = n.match ? "GuessCardRight" : "GuessCardWrong";
                DOVirtual.DelayedCall(0.2f, () => Game.Manager.audioMan.TriggerSound(ss));
                VibrationManager.VibrateLight();
            }).SetId(s);
        }

        public void AnimateRecord(int k_, int v_, float d_) {
            DebugEx.Info($"{nameof(ActivityGuess)} answer {k_} record {v_}");
            var n = answer[k_].record[v_];
            DOVirtual.DelayedCall(d_, () => {
                FillRecord(k_);
                var b = n.transform.GetSiblingIndex();
                n.transform.SetAsLastSibling();
                n.transform.DOPunchScale(Vector3.one * recordPunch, recordDuration, vibrato:0, elasticity:0.8f)
                    .OnComplete(() => {
                        n.transform.SetSiblingIndex(b);
                    });
            });
        }

        public void RoleIdle() {
            role.AnimationState.SetAnimation(0, "idle", true);
        }

        public float RoleCheck() {
            role.AnimationState.SetAnimation(0, "look", false);
            var t = role.AnimationState.GetCurrent(0).AnimationEnd;
            return t;
        }

        public void RoleResult(string anim_) {
            role.AnimationState.AddAnimation(0, anim_, false, 0);
            role.AnimationState.AddAnimation(0, "idle", true, 0);
        }
    }
}