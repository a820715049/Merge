using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;
using System.Collections;
using DG.Tweening;

namespace FAT {
    public class UIActivityDuelResult : UIBase {
        public struct Player {
            public Transform root;
            public GameObject crown;
            public TextMeshProUGUI name;
            public UIImageState icon;
            public GameObject effect;
        }
        
        internal Player playerL;
        internal Player playerR;
        internal MBRewardIcon prize;
        internal ActivityDuelMilestone milestone;
        public UIVisualGroup visualGroup;
        public Animator animator;
        public float wait = 0.8f;
        public float waitNode = 1.4f;
        public float winnerScale = 1.4f;
        public float winnerScaleDuration = 0.8f;

        private ActivityDuel activity;
        internal bool result;
        internal Action WhenRefresh;
        internal BlockToken block = new();

        private void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out animator);
            transform.Access(out visualGroup);
            var root = transform.Find("Content");
            visualGroup.Prepare(root.Access<TextProOnACircle>("Title_scale/title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("playerL/group/name"), "name");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("playerR/group/name"), "name");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("confirm/text"), "confirm");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("reward/count"), "prize");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_milestone/index"), "round");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate() {
            var root = transform.Find("Content");
            root.Access("playerL/group", out playerL.root);
            root.Access("playerR/group", out playerR.root);
            root.Access("playerL/group/name", out playerL.name);
            root.Access("playerR/group/name", out playerR.name);
            playerL.crown = root.TryFind("playerL/group/crown");
            playerR.crown = root.TryFind("playerR/group/crown");
            playerL.effect = root.TryFind("playerL/effect");
            playerR.effect = root.TryFind("playerR/effect");
            root.Access("playerR/group/icon", out playerR.icon);
            root.Access("reward", out prize);
            root.Access("_milestone", out milestone);
            root.Access("confirm", out MapButton confirm);
            confirm.WhenClick = Confirm;
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivityDuel)items[0];
            result = (bool)items[1];
            WhenRefresh = (Action)items[2];
        }

        protected override void OnPreOpen() {
            UIUtility.FadeIn(this, animator);
            Game.Manager.audioMan.TriggerSound("DuelReward");
            RefreshTheme();
            RefreshReward();
            RefreshState();
        }

        protected override void OnPreClose() {
            animator.SetTrigger("Hide");
            activity.SyncScore();
            WhenRefresh?.Invoke();
            WhenRefresh = null;
            MessageCenter.Get<ACTIVITY_REFRESH>().Dispatch(activity);
        }

        public void RefreshTheme() {
            var visual = activity.VisualResult;
            var map = visual.visual.TextMap;
            if (result) {
                map.Replace("mainTitle", "#SysComDesc925");
                map.Replace("subTitle", "#SysComDesc926");
            }
            else {
                map.Replace("mainTitle", "#SysComDesc928");
                map.Replace("subTitle", "#SysComDesc929");
            }
            visual.Refresh(visualGroup);
            playerL.name.text = I18N.Text("#SysComDesc459");
            playerR.name.text = I18N.FormatText("#SysComDesc431", 1);
        }

        public void RefreshReward() {
            var r = activity.LastReward;
            prize.gameObject.SetActive(r != null);
            if (r == null) return;
            prize.Refresh(r);
            prize.gameObject.SetActive(result);
        }

        public void RefreshState() {
            var (rH, rN) = result ? (playerL.root, playerR.root) : (playerR.root, playerL.root);
            rH.localScale = Vector3.one;
            rN.localScale = Vector3.one;
            rH.DOScale(winnerScale, winnerScaleDuration).SetEase(Ease.OutElastic);
            playerR.icon.Select(activity.robotIcon);
            var wL = result;
            var wR = !result;
            playerL.crown.SetActive(wL);
            playerR.crown.SetActive(wR);
            playerL.effect.SetActive(wL);
            playerR.effect.SetActive(wR);
            milestone.gameObject.SetActive(false);
            if (result) {
                StartCoroutine(Animate());
            }
        }

        public IEnumerator Animate() {
            var tokenId = activity.MilestoneTokenId;
            var rMan = Game.Manager.rewardMan;
            var data = rMan.BeginReward(tokenId, 1, ReasonString.duel);
            UIFlyUtility.FlyReward(data, playerL.root.position);
            milestone.gameObject.SetActive(true);
            milestone.animator.Play("Common_Panel_MiddleShow");
            var dF1 = milestone.animator.GetCurrentAnimatorStateInfo(0).length;
            var offsetN = activity.MilestoneCommitReward != null ? -1 : 0;
            milestone.Refresh(activity, offsetN, -1);
            block.Enter(wait_:false);
            yield return new WaitForSeconds(dF1);
            var v = activity.Round;
            milestone.Target(v, speed_:2);
            var mR = false;
            while(!milestone.Stable) {
                var rr = milestone.TargetStep();
                mR = mR || rr;
                yield return null;
            }
            var d = wait;
            if (mR) d += waitNode;
            yield return new WaitForSeconds(d);
            milestone.animator.Play("Common_Panel_MiddleHide");
            var dF2 = milestone.animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(dF2);
            milestone.gameObject.SetActive(false);
            block.Exit();
        }

        internal void Confirm() {
            UIUtility.FadeOut(this, animator);
            var r = activity.ScoreCommitReward;
            activity.ScoreCommitReward = null;
            if (r != null) {
                UIFlyUtility.FlyReward(r, prize.transform.position);
            }
            var data = activity.MilestoneCommitReward;
            if (data != null && Game.Manager.specialRewardMan.IsBusy()) {
                UIFlyUtility.FlyReward(data, prize.transform.position);
                activity.MilestoneCommitReward = null;
            }
        }
    }
}