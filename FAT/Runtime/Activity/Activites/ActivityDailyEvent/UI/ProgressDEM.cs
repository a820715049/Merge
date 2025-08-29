using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using System.Collections.Generic;
using fat.rawdata;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using Config;
using System.Threading.Tasks;

namespace FAT {
    public class ProgressDEM : MonoBehaviour {
        public RectTransform borderRef;
        public MBBoardFly fly;
        public float stay = 2.5f;
        private GameObject group;
        private RectTransform rect;
        private float width;
        private MBRewardIcon reward;
        private MBRewardProgress progress;
        private UIImageRes icon;
        private Action<int, int> WhenUpdate;
        private Action<RewardCommitData> WhenCommit;
        private Coroutine routine;
        private int targetV;
        private int commitV;
        private float currentV;
        public float duration = 1.2f;
        public float interval = 1.0f;
        internal float speed;
        public Vector3 posL;
        private float border;

        #if UNITY_EDITOR

        public void OnValidate() {
            if (Application.isPlaying) return;
            posL = transform.localPosition;
        }

        #endif

        public void Awake() {
            var root = transform.Find("group");
            group = root.gameObject;
            rect = (RectTransform)group.transform;
            width = rect.rect.width;
            root.Access("reward", out reward);
            root.Access("progress", out progress);
            root.Access("icon", out icon);
            Visible(false);
            WhenUpdate ??= RefreshV;
            WhenCommit ??= RefreshC;
            MessageCenter.Get<MSG.DAILY_EVENT_MILESTONE_PROGRESS>().AddListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().AddListener(WhenCommit);
        }

        public void OnDestroy() {
            MessageCenter.Get<MSG.DAILY_EVENT_MILESTONE_PROGRESS>().RemoveListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().RemoveListener(WhenCommit);
        }

        public void OnEnable() {
            if (borderRef != null) {
                border = borderRef.anchoredPosition.x - borderRef.rect.width / 2;
            }
        }

        public void Update() {
            if (!group.activeSelf || borderRef == null) return;
            var posR = transform.parent.TransformPoint(posL);
            var posB = borderRef.TransformPoint(border, 0, 0);
            if (posR.x < posB.x) {
                posR = new(posB.x, posR.y, posR.z);
            }
            if (transform.position != posR) {
                transform.position = posR;
            }
        }

        public void Refresh(int oV_, int nV_) {
            var de = Game.Manager.dailyEvent;
            if (oV_ >= de.valueMax) return;
            var game = Game.Instance;
            targetV = nV_;
            if (routine == null) {
                currentV = oV_;
                commitV = oV_;
                rect.anchoredPosition = new(-width, 0);
            }
            else {
                game.StopCoroutineGlobal(routine);
            }
            CheckSpeed();
            Visible(true);
            routine = game.StartCoroutineGlobal(Animate());
        }

        public void RefreshV(int oV_, int nV_) => Refresh(oV_, nV_);

        public void RefreshC(RewardCommitData r_) {
            var de = Game.Manager.dailyEvent;
            if (r_.rewardId != (de.milestone?.RequireCoinId ?? 0)) return;
            commitV += r_.rewardCount;
        }

        public void Visible() => Visible(routine != null);
        public void Visible(bool v_) {
            group.SetActive(v_);
        }

        private void CheckSpeed() {
            speed = (targetV - currentV) / duration;
        }

        private IEnumerator Animate() {
            var de = Game.Manager.dailyEvent;
            var list = de.listM;
            var next = de.MilestoneNext((int)currentV);
            (RewardBar.NodeInfo, int) Node(int v_) {
                var node = list[v_];
                var prev = next > 0 ? list[v_ - 1].value : 0;
                reward.Refresh(node.reward);
                return (node, prev);
            }
            void Progress(int v_, int p_) {
                progress.RefreshSegment((int)currentV, v_, p_);
            }
            var (node, prev) = Node(next);
            Progress(node.value, prev);
            var w = rect.anchoredPosition.x;
            var d1 = 0.8f;
            var d11 = 0.8f * Mathf.Abs(w) / width;
            var d2 = stay;
            var d3 = 1.2f;
            var wait1 = new WaitForSeconds(d1);
            var wait11 = new WaitForSeconds(d11);
            var wait2 = new WaitForSeconds(d2);
            var wait3 = new WaitForSeconds(d3);
            rect.DOAnchorPosX(0, d11);
            yield return wait11;
            to_commit:
            while (currentV < commitV) {
                currentV += speed * Time.deltaTime;
                if (currentV >= node.value) {
                    ++next;
                    var r = node.reward;
                    FlyReward(r, next >= list.Count);
                    if (next >= list.Count) break;
                    (node, prev) = Node(next);
                    if (currentV < commitV) yield return wait3;
                }
                Progress(node.value, prev);
                yield return null;
            }
            currentV = commitV;
            if (currentV < targetV) {
                var waitC = Time.time + 4;
                while (currentV >= commitV && Time.time < waitC) {
                    yield return null;
                }
                if (currentV < commitV) goto to_commit;
            }
            Progress(node.value, prev);
            yield return wait2;
            rect.DOAnchorPosX(-width, d1);
            yield return wait1;
            Visible(false);
            routine = null;
        }

        private void FlyReward(Config.RewardConfig r, bool last_) {
            static async UniTask E(RewardCommitData r_) {
                var ui = UIManager.Instance;
                while (!ui.CheckUIIsIdleState()) await Task.Delay(500);
                ui.OpenWindow(UIConfig.UIDailyEvent);
                await UniTask.WaitUntil(() => ui.IsOpen(UIConfig.UIDailyEvent));
                // 因为MBRewardIcon延迟了0.2s弹出tips 为了避免被tips遮挡 此处延迟弹出
                await UniTask.Delay(250);
                ui.OpenWindow(UIConfig.UIDEMReward, r_);
            }
            var pos = reward.icon.transform.position;
            var list = Game.Manager.dailyEvent.MilestoneReward;
            next:
            if (list.Count > 0) {
                var d = list[0];
                list.RemoveAt(0);
                if (d.rewardId != r.Id) {
                    DebugEx.Warning($"reward data mismatch {r.Id} {d.rewardId}");
                    goto next;
                }
                if (last_) _ = E(d);
                else {
                    // fly.ShowFlyCenterReward((pos, d, null));
                    UIFlyUtility.FlyReward(d, pos);
                }
                return;
            }
            if (UIFlyFactory.CheckNeedFlyIcon(r.Id)) {
                var ft = UIFlyFactory.ResolveFlyType(r.Id);
                var to = UIFlyFactory.ResolveFlyTarget(ft);
                UIFlyUtility.FlyCustom(r.Id, r.Count, pos, to, FlyStyle.Reward, ft);
            }
        }
    }
}