/**
 * @Author: zhangpengjian
 * @Date: 2024/11/7 15:50:36
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/11/7 15:50:36
 * Description: 连续限时订单入口
 */


using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;

namespace FAT
{
    public class UIOrderChallengeEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private TMP_Text cd;
        [SerializeField] private Button btn;
        [SerializeField] private Button btnGo;
        [SerializeField] private GameObject group;
        [SerializeField] private LayoutElement element;

        private ActivityOrderChallenge activityOrderChallenge;
        private Action WhenCD;

        public void Start()
        {
            btn.onClick.AddListener(EntryClick);
            btnGo.onClick.AddListener(EntryClick);
        }

        private void EntryClick()
        {
            activityOrderChallenge?.StartRes.ActiveR.Open(activityOrderChallenge);
        }

        public void RefreshEntry(ActivityLike _)
        {
            Visible(true);
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
            MessageCenter.Get<MSG.ORDER_CHALLENGE_BEGIN>().AddListener(WhenChallenge);
            MessageCenter.Get<MSG.ORDER_CHALLENGE_EXPIRE>().AddListener(WhenExpire);
            MessageCenter.Get<MSG.ORDER_CHALLENGE_VICTORY>().AddListener(WhenVictory);
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.ZeroQuest, out var activity);
            if (activity == null)
            {
                Visible(false);
                return;
            }
            activityOrderChallenge = (ActivityOrderChallenge)activity;
            if (activityOrderChallenge.IsOver())
            {
                Visible(false);
                return;
            }
            if (activityOrderChallenge.IsChallenge)
            {
                Visible(false);
                return;
            }
            UIUtility.CountDownFormat(cd, activityOrderChallenge.Countdown);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.ORDER_CHALLENGE_BEGIN>().RemoveListener(WhenChallenge);
            MessageCenter.Get<MSG.ORDER_CHALLENGE_EXPIRE>().RemoveListener(WhenExpire);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
            MessageCenter.Get<MSG.ORDER_CHALLENGE_VICTORY>().RemoveListener(WhenVictory);
        }

        private void WhenVictory()
        {
            Visible(true);
        }

        private void WhenExpire()
        {
            if (!activityOrderChallenge.IsOver())
                Visible(true);
        }

        private void WhenChallenge()
        {
            Visible(false);
        }

        private void RefreshCD()
        {
            if (!group.gameObject.activeSelf)
                return;
            var v = activityOrderChallenge.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0 || activityOrderChallenge.IsOver())
                Visible(false);
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
            element.ignoreLayout = !v_;
        }
    }
}