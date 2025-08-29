/**
 * @Author: zhangpengjian
 * @Date: 2024/10/28 18:36:40
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/28 18:36:40
 * Description: 连续限时订单开启
 */

using System;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIOrderChallengeStart : UIBase
    {
        private TextProOnACircle title;
        private TextMeshProUGUI leftTime;
        private TextMeshProUGUI rewardCount;
        private TextMeshProUGUI tip;
        private Action whenTick;
        private UIImageRes bg;
        private UIImageRes titleBg;
        private UIImageRes rewardIcon;
        private ActivityOrderChallenge activityOrderChallenge;
        [SerializeField] private GameObject btnRoot1;
        [SerializeField] private GameObject btnRoot2;

        protected override void OnCreate()
        {
            title = transform.Find("Content/Panel/TitleBg/Title").GetComponent<TextProOnACircle>();
            rewardCount = transform.Find("Content/Panel/_group/anchor/entry/count").GetComponent<TextMeshProUGUI>();
            leftTime = transform.Find("Content/Panel/_cd/text").GetComponent<TextMeshProUGUI>();
            tip = transform.Find("Content/Panel/Desc").GetComponent<TextMeshProUGUI>();
            transform.AddButton("Content/Panel/1/BtnConfirm", _ClickConfirm);
            transform.AddButton("Content/Panel/2/BtnConfirm", _ClickConfirm);
            transform.AddButton("Content/Panel/2/BtnClose", _ClickClose);
            bg = transform.Find("Content/Panel/Bg").GetComponent<UIImageRes>();
            rewardIcon = transform.Find("Content/Panel/_group/anchor/entry/icon").GetComponent<UIImageRes>();
            titleBg = transform.Find("Content/Panel/TitleBg").GetComponent<UIImageRes>();
        }

        protected override void OnParse(params object[] items)
        {
            activityOrderChallenge = (ActivityOrderChallenge)items[0];
        }

        protected override void OnPreOpen()
        {
            btnRoot1.SetActive(!activityOrderChallenge.IsWait);
            btnRoot2.SetActive(activityOrderChallenge.IsWait);
            whenTick ??= _RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(whenTick);
            activityOrderChallenge.VisualStart.Refresh(title, "mainTitle");
            activityOrderChallenge.VisualStart.Refresh(bg, "bgImage");
            activityOrderChallenge.VisualStart.Refresh(titleBg, "titleImage");
            activityOrderChallenge.VisualStart.Refresh(tip, "tip");
            var r = activityOrderChallenge.TotalReward;
            rewardIcon.SetImage(Game.Manager.rewardMan.GetRewardIcon(r.Id, r.Count));
            rewardCount.SetText(r.Count.ToString());
            _RefreshCD();
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(whenTick);
        }

        private void _RefreshCD()
        {
            var v = activityOrderChallenge?.Countdown ?? 0;
            UIUtility.CountDownFormat(leftTime, v);
            if (v <= 0)
            {
                Close();
            }
        }

        private void _ClickConfirm()
        {
            if (activityOrderChallenge != null)
            {
                activityOrderChallenge.Challenge();
                activityOrderChallenge.Res.ActiveR.Open(activityOrderChallenge, true, true);
                var active = Game.Manager.mapSceneMan.scene.Active;
                if (active)
                    GameProcedure.SceneToMerge();
            }
            Close();
        }

        private void _ClickClose()
        {
            Close();
        }
    }
}