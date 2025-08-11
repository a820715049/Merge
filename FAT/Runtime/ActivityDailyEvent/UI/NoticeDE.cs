using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using System.Collections.Generic;
using fat.rawdata;
using DG.Tweening;
using static EL.PoolMapping;
using fat.msg;

namespace FAT {
    public class NoticeDE : MonoBehaviour {
        public RectTransform rectRef;
        public RectTransform borderRef;
        public MBBoardFly fly;
        private Transform anchor;
        private CanvasGroup group;
        private UIDailyEvent.Entry entry;
        private UIDailyEvent.Entry entry1;
        private CanvasGroup entry1Group;
        public float flyIn = 0.4f;
        public float flyOut = 0.4f;
        public float stay1 = 0.5f;
        public float complete = 0.4f;
        public float stay2 = 2.5f;
        private Action<(DailyEvent.Task, Ref<List<RewardCommitData>>, Ref<List<RewardCommitData>>)> WhenUpdate;
        private float border;
        private Coroutine routine;
        private readonly List<(DailyEvent.Task, Ref<List<RewardCommitData>>, Ref<List<RewardCommitData>>)> pending = new();
        private static int tweenId;

        private void Awake() {
            transform.Access("anchor", out anchor); 
            var root = transform.Find("group");
            root.Access(out group);
            entry = UIDailyEvent.ParseEntry(root.Find("_entry_task").gameObject);
            entry1 = UIDailyEvent.ParseEntry(root.Find("_entry_task1").gameObject);
            root.Access("_entry_task1", out entry1Group);
        }

        public void OnEnable() {
            Visible(false);
            WhenUpdate ??= Refresh;
            MessageCenter.Get<MSG.DAILY_EVENT_TASK_COMPLETE>().AddListener(WhenUpdate);
            if (borderRef != null) {
                border = borderRef.rect.width / 2;
            }
            tweenId = GetHashCode();
        }

        public void OnDisable() {
            MessageCenter.Get<MSG.DAILY_EVENT_TASK_COMPLETE>().RemoveListener(WhenUpdate);
        }

        public void Refresh((DailyEvent.Task, Ref<List<RewardCommitData>>, Ref<List<RewardCommitData>>) v_) {
            Visible(true);
            pending.Add(v_);
            if (routine != null && pending.Count == 0) {
                StopCoroutine(routine);
                DOTween.Kill(tweenId);
                routine = null;
            }
            routine ??= StartCoroutine(Animate());
        }

        private void RefreshT(DailyEvent.Task task_) {
            entry.name.text = task_.Name;
            entry.reward.Refresh(task_.reward);
            entry.rewardM.Refresh(task_.rewardM);
            UIDailyEvent.RefreshEntry(entry1, task_, false, false);
        }

        private void Visible(bool v_) {
            group.gameObject.SetActive(v_);
        }

        private Vector3 TargetPos() {
            if (rectRef == null || borderRef == null) return anchor.position;
            var posR = rectRef.position;
            var posB = borderRef.InverseTransformPoint(posR);
            var x = posB.x;
            var xx = Mathf.Clamp(x, -border, border);
            if (x != xx) {
                posB.x = xx;
                return borderRef.TransformPoint(posB);
            }
            return posR;
        }

        private IEnumerator Animate() {
            var d1 = 0.8f;
            var d2 = stay2;
            var d3 = flyIn;
            var d4 = flyOut;
            var d5 = Mathf.Max(d3, d4);
            var d6 = stay1;
            var d7 = complete;
            var (d31, d32) = (d3 * 0.6f, d3 * 0.4f);
            var (d41, d42) = (d4 * 0.4f, d4 * 0.6f);
            var wait1 = new WaitForSeconds(d1);
            var wait2 = new WaitForSeconds(d2 - d5);
            var wait3 = new WaitForSeconds(d5 + d6);
            var wait4 = new WaitForSeconds(d7);
            repeat1:
            var root = group.transform;
            var posT = anchor.position;
            root.position = TargetPos();
            root.DOMove(posT, d3).SetEase(Ease.InOutFlash).SetId(tweenId);
            root.localScale = Vector3.one * 0.2f;
            repeat2:
            var (task, reward, rewardM) = pending[0];
            RefreshT(task);
            root.DOScale(Vector3.one, d31).SetDelay(d32).SetId(tweenId);
            group.alpha = 1;
            entry1Group.alpha = 1;
            yield return wait3;
            entry1Group.DOFade(0, d7);
            yield return wait4;
            var pos = entry.reward.icon.transform.position;
            var posM = entry.rewardM.icon.transform.position;
            // foreach(var r in reward.obj) {
            //     fly.ShowFlyCenterReward((pos, r, null));
            // }
            UIFlyUtility.FlyRewardList(reward.obj, pos);
            UIFlyUtility.FlyRewardList(rewardM.obj, posM);
            reward.Free();
            rewardM.Free();
            yield return wait2;
            pending.RemoveAt(0);
            if (pending.Count > 0) goto repeat2;
            root.DOMove(TargetPos(), d4).SetEase(Ease.OutFlash).SetId(tweenId);
            root.DOScale(Vector3.one * 0.1f, d4).SetId(tweenId);
            group.DOFade(0, d41).SetDelay(d42).SetId(tweenId);
            yield return wait1;
            if (pending.Count > 0) goto repeat1;
            Visible(false);
            routine = null;
        }
    }
}