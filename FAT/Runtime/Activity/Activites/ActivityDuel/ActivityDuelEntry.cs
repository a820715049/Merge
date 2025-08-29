
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using DG.Tweening;
using Spine.Unity;

namespace FAT
{
    public class ActivityDuelEntry : MonoBehaviour, IActivityBoardEntry
    {
        public GameObject group;
        public Animator anim;
        public TMP_Text cd;
        public MBRewardProgress progress;
        public TMP_Text addNum;
        public SkeletonGraphic spine;
        public TextMeshProUGUI scoreL, scoreR;
        public GameObject crownL, crownR, state;
        public MapButton confirm;
        public float duration = 0.75f;
        public float addDelay = 0.8f;
        private Coroutine routine;

        private Action WhenCD;
        private ActivityDuel activity;

        public void Start()
        {
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
            confirm.WithClickScale().FixPivot();
            confirm.WhenClick = EntryClick;
        }

        public void OnEnable()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.ACTIVITY_DUEL_SCORE>().AddListener(Refresh);
            MessageCenter.Get<MSG.ACTIVITY_DUEL_ROBOT_SCORE>().AddListener(RefreshR);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().AddListener(RefreshA);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.ACTIVITY_DUEL_SCORE>().RemoveListener(Refresh);
            MessageCenter.Get<MSG.ACTIVITY_DUEL_ROBOT_SCORE>().RemoveListener(RefreshR);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().RemoveListener(RefreshA);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
        }

        /// <summary>
        /// 寻宝活动入口
        /// </summary>
        public void RefreshEntry(ActivityLike activity_)
        {
            if (activity_ == null)
            {
                Visible(false);
                return;
            }
            if (activity_ is not ActivityDuel)
            {
                Visible(false);
                return;
            }
            activity = (ActivityDuel)activity_;
            var valid = activity is { Valid: true, IsUnlock: true };
            Visible(valid);
            if (!valid) return;
            //刷新倒计时
            RefreshCD();
            RefreshP(false);
            RefreshS();
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

        public void RefreshS()
        {
            var sL = activity.GetPlayerScore();
            var sR = activity.GetRobotScore();
            scoreL.text = $"{sL}";
            scoreR.text = $"{sR}";
            crownL.SetActive(sL >= sR);
            crownR.SetActive(sR > sL);
        }

        public void RefreshP(bool p_) {
            var wait = !activity.VisualActive;
            confirm.gameObject.SetActive(wait);
            progress.gameObject.SetActive(!wait && p_);
            addNum.gameObject.SetActive(!wait && p_);
            state.SetActive(!wait && !p_);
            spine.AnimationState.SetAnimation(0, "idle", true);
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }

        private void EntryClick()
        {
            activity.Open();
        }

        private void Refresh(int last, int cur)
        {
            Visible(true);
            var sT = activity.GetCurTargetScore();
            var change = cur - last;
            if (routine != null)
            {
                StopCoroutine(routine);
            }
            IEnumerator Animate()
            {
                yield return new WaitForSeconds(0.5f);
                spine.AnimationState.SetAnimation(0, "active", false);
                spine.AnimationState.AddAnimation(0, "idle", true, 0);
                progress.gameObject.SetActive(true);
                progress.Refresh(last, activity.visualTargetScore);
                progress.transform.DOPunchScale(Vector3.one * 0.1f, 0.8f, vibrato:0);
                addNum.gameObject.SetActive(true);
                addNum.text = "+" + change;
                yield return new WaitForSeconds(addDelay);
                progress.Refresh(cur, sT, duration);
                yield return new WaitForSeconds(duration);
                RefreshP(false);
                var end = cur >= sT;
                if (end) {
                    anim.Play("UIActivityDuelEntry_Punch");
                }
            }
            routine = StartCoroutine(Animate());
            RefreshS();
        }

        private void RefreshR(int last, int cur) {
            RefreshS();
        }

        private void RefreshA(ActivityLike acti_) {
            if (acti_ != activity) return;
            RefreshS();
            RefreshP(progress.gameObject.activeSelf);
        }
    }
}