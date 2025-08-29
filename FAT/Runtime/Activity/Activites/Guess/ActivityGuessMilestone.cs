using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using EL;
using static fat.conf.Data;
using DG.Tweening;
using Coffee.UIExtensions;
using FAT.MSG;

namespace FAT {
    using static MessageCenter;

    public class ActivityGuessMilestone : RewardBar {
        public int preview = 5;
        public float barDuration = 1.6f;
        public float barSpeed;
        public float rewardPunch = 0.1f;
        public float rewardPunchDuration = 0.5f;
        public float durationMove = 1.2f;
        public float durationFade = 0.5f;
        public float moveDelay = 0.3f;
        public float rewardDelay = 0.6f;
        public float iconPunch = 0.1f;
        public float iconPunchDuration = 0.2f;
        public float rewardTargetFade = 0.8f;
        public float rewardTargetPunch = -0.1f;
        public float rewardTargetDuration = 0.1f;
        public float exitDelay = 0.8f;
        internal TextMeshProUGUI index;
        internal MBRewardIcon reward;
        internal HelpInfo info;
        internal MBRewardLayout prize;
        internal UIParticle iconPS;
        internal GameObject rewardTarget;
        internal readonly List<NodeInfo> segment = new();
        internal NodeOption option;
        internal float rangeT;
        internal int endIndex;
        internal int i;
        internal int lastV;
        private ActivityGuess activity;
        private Action<FlyableItemSlice> WhenFlyFeedback;
        internal float milestoneDelay;
        internal BlockToken block;

        internal override void Awake() {
            base.Awake();
            var root = transform.Find("list");
            root.Access("index", out index);
            root.Access("reward", out reward);
            root.Access("fx_energy_collect", out iconPS);
            root = transform.parent;
            root.Access("preview", out info);
            root.Access("preview/group", out prize);
            rewardTarget = root.TryFind("target");
            WhenFlyFeedback ??= RewardFeedback;
        }

        internal void OnEnable() {
            rewardTarget.SetActive(false);
            Get<FLY_ICON_FEED_BACK>().AddListener(WhenFlyFeedback);
        }

        internal void OnDisable() {
            Get<FLY_ICON_FEED_BACK>().RemoveListener(WhenFlyFeedback);
        }

        public void Refresh(ActivityGuess acti_, BlockToken block_) {
            activity = acti_;
            block = block_;
            var iConf = GetObjBasic(acti_.confD.MilestoneScoreId);
            icon.SetImage(iConf.Icon);
            var mList = activity.prize;
            rangeT = mList[preview - 1].Value;
            var maxV = mList[^1].Value;
            var endV = 0;
            var k = mList.Count;
            while (k > 0 && endV <= rangeT) {
                var n = mList[--k];
                endV = maxV - n.Value;
            }
            endIndex = k + 1;
            bar.Init();
            Refresh();
            RefreshReward();
        }

        public void Refresh() {
            Refresh(activity.milestoneIndex, activity.Milestone);
            RefreshIndex();
        }

        public void RefreshIndex() {
            index.text = $"{I18N.Text("#SysComDesc722")} {activity.milestoneIndex}/{activity.prize.Count}";
        }

        public void Refresh(int next_, int value_) {
            static void F(IList<NodeInfo> l_, GuessMilestone m_, int value_, int preV_) {
                var v = m_.Value;
                l_.Add(new() { reward = m_.reward[0], value = v, pos = v - preV_, complete = value_ >= v });
            }
            var mList = activity.prize;
            i = Mathf.Min(next_, endIndex);
            segment.Clear();
            var preV = i > 0 ? mList[i - 1].Value : 0;
            var rangeV = preV + rangeT;
            option = new() { value = preV, pos = 0, range = rangeT };
            var nextV = 0;
            var ii = 0;
            while(nextV < rangeV && i + ii < mList.Count) {
                var m = mList[i + ii];
                nextV = m.Value;
                F(segment, m, value_, preV);
                ++ii;
            }
            if (segment.Count == 0) {
                DebugEx.Warning($"{nameof(ActivityGuess)} milestone segment contains no element");
                return;
            }
            DebugEx.Info($"{nameof(ActivityGuess)} milestone preview {i},{i + ii - 1} {ii}");
            RefreshList(segment, value_, next_ - i, option);
            var iii = ii - 1;
            list[iii].obj.SetActive(false);
            lastV = mList[iii].Value;
            preV = next_ > 0 ? mList[next_ - 1].Value : 0;
            nextV = next_ < mList.Count ? mList[next_].Value : 0;
            barSpeed = (nextV - preV) / barDuration;
        }

        public void RefreshReward() {
            var p = activity.Prize;
            Func<int, object, bool> Click = p.icon == null ? null : (_, _) => {
                info.Active();
                return true;
            };
            reward.Refresh(p.icon, Click);
            prize.Refresh(p.reward);
        }

        public void EnterWait() {
            Pause = true;
            block.Enter(wait_:false);
        }

        public void ExitWait() {
            Pause = false;
            block.Exit();
        }

        public void Reward(int n_) {
            EnterWait();
            var e = list[n_];
            var map = activity.prizeCache;
            var nn = i + next;
            if (!map.TryGetValue(nn, out var listT)) {
                DebugEx.Warning($"{nameof(ActivityGuess)} milestone reward cache not found id:{nn}");
                goto end;
            }
            map.Remove(nn);
            var conf = activity.prize[nn - 1];
            if (conf.icon != null) {
                reward.icon.GetComponent<Animator>().Play("UIActivityGuess_RewardItem_Punch");
                Action Next = () => {
                    block.Enter(wait_:false);
                    DOVirtual.DelayedCall(exitDelay, () => {
                        block.Exit();
                        NextNode(n_);
                    });
                };
                ExitWait();
                rewardTarget.SetActive(true);
                UIManager.Instance.OpenWindow(activity.VisualReward.res.ActiveR, activity, listT, Next);
                return;
            }
            DOVirtual.DelayedCall(rewardDelay, () => {
                FlyReward(listT.obj, e.root.position);
                listT.Free();
            });
            end:
            e.icon.icon.GetComponent<Animator>().Play("UIActivityGuess_RewardItem_Punch");
            e.tick.GetComponent<Animator>().Play("UIActivityGuessReward_Right_Show");
            NextNode(n_);
        }

        public void TargetM(int n_, float delay_ = 0) {
            if (milestoneDelay <= 0) milestoneDelay = delay_;
            Target(n_, barSpeed);
        }

        public bool TargetStep() {
            var delta = Time.deltaTime;
            milestoneDelay -= delta;
            if (milestoneDelay > 0) return false;
            else milestoneDelay = 0;
            var pass = TargetStep(segment, delta, stopAtNext_:true, option);
            if (pass) {
                Reward(0);
                RefreshIndex();
            }
            return pass;
        }

        public void NextNode(int n_) {
            activity.TryNextRound();
            AnimateTo(n_, moveDelay);
        }

        public void AnimateTo(int n_, float delay_) {
            if (i >= endIndex) {
                ExitWait();
                return;
            }
            DOTween.Kill(bar, complete:true);
            DOTween.Kill(this, complete:true);
            var rect = bar.bar.rectTransform;
            var size = rect.sizeDelta;
            var moveX = list[n_].root.anchoredPosition.x;
            for (var k = 0; k < active; ++k) {
                var r = list[k].root;
                r.DOAnchorPosX(r.anchoredPosition.x - moveX, durationMove)
                    .SetDelay(delay_)
                    .SetId(this);
            }
            size.x -= moveX;
            bar.bar.rectTransform.DOSizeDelta(size, durationMove)
                .SetDelay(delay_)
                .SetId(bar);
            var e1 = list[0];
            DOVirtual.Float(0, 1, durationFade, v_ => e1.color.MixTo(0, v_))
                .SetDelay(durationMove - durationFade + delay_)
                .SetId(bar);
            if (lastV - preV <= rangeT) {
                var e2 = list[segment.Count - 1];
                e2.obj.SetActive(true);
                e2.color.MixTo(0, 1);
                DOVirtual.Float(1, 0, durationFade, v_ => e2.color.MixTo(0, v_))
                    .SetDelay(delay_)
                    .SetId(bar);
            }
            DOVirtual.DelayedCall(durationMove + delay_ + 0.05f, () => {
                ExitWait();
                var targetV = target;
                DOTween.Kill(bar, complete:true);
                Refresh(i + next, (int)value);
                TargetM((int)targetV);
            }).SetId(this);
        }

        public void FlyReward(List<RewardCommitData> list_, Vector3 pos_) {
            foreach(var r in list_) {
                var fly = UIFlyFactory.ResolveFlyType(r.rewardId);
                if (fly != FlyType.GuessMilestone) {
                    rewardTarget.SetActive(true);
                }
            }
            UIFlyUtility.FlyRewardList(list_, pos_);
        }

        public void RewardFeedback(FlyableItemSlice s_) {
            void Other(FlyableItemSlice s_) {
                DOTween.Kill(rewardTarget, complete:true);
                rewardTarget.SetActive(true);
                rewardTarget.transform.DOPunchScale(Vector3.one * rewardTargetPunch, rewardTargetDuration, vibrato:0).SetId(rewardTarget);
                DOVirtual.DelayedCall(rewardTargetFade, () => {
                    rewardTarget.SetActive(false);
                }).SetId(rewardTarget);
            }
            void Icon(FlyableItemSlice s_) {
                DOTween.Kill(icon, complete:true);
                icon.transform.DOPunchScale(Vector3.one * rewardTargetPunch, rewardTargetDuration, vibrato:0).SetId(icon);
                UIUtility.ManuallyEmitParticle(iconPS);
            }
            switch (s_.FlyType) {
                case FlyType.GuessMilestone: Icon(s_); break;
                default: Other(s_); break;
            }
        }
    }
}