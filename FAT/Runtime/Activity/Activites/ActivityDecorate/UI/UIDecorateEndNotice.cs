/*
 *@Author:chaoran.zhang
 *@Desc:装饰活动结束预告界面
 *@Created Time:2024.01.22 星期一 16:39
 */

using System;
using EL;
using fat.conf;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIDecorateEndNotice : UIBase
    {
        public TextMeshProUGUI cd;
        public TextProOnACircle titleV;
        public TextMeshProUGUI desc;
        public TextMeshProUGUI bottomDesc;
        public UIImageRes bg;
        public UIImageRes bg1;

        private DecorateActivity activity;

        public void OnValidate() {
            if (Application.isPlaying) return;
            cd = transform.Find("Content/Panel/cd").GetComponent<TextMeshProUGUI>();
            titleV = transform.Find("Content/Panel/bg1/title").GetComponent<TextProOnACircle>();
            desc = transform.Find("Content/Panel/desc").GetComponent<TextMeshProUGUI>();
            bottomDesc = transform.Find("Content/Panel/desc1").GetComponent<TextMeshProUGUI>();
            bg = transform.Find("Content/Panel/bg").GetComponent<UIImageRes>();
            bg1 = transform.Find("Content/Panel/bg1").GetComponent<UIImageRes>();
        }

        protected override void OnCreate() {
            transform.AddButton("Content/Panel/confirm", () =>
            {
                Close();
                if (!Game.Manager.decorateMan.Activity.EndNoticeJump())
                {
                    GameProcedure.MergeToSceneArea(Game.Manager.decorateMan.Activity.CurArea,
                        () =>
                        {
                            UIManager.Instance.RegisterIdleAction("open_decorate_panel", 202,
                                () => UIManager.Instance.OpenWindow(Game.Manager.decorateMan.Panel));
                        });
                }
            });
            transform.AddButton("Content/Panel/close", Close);
        }

        protected override void OnPreOpen() {
            activity = Game.Manager.decorateMan.Activity;
            RefreshTheme();
            RefreshCD();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        public void RefreshTheme() {
            var visual = activity.EndRemindVisual;
            visual.Refresh(bg, "bgimage");
            visual.Refresh(bg1, "titleImage");
            visual.Refresh(titleV, "mainTitle");
            visual.Refresh(desc, "desc1");
            visual.Refresh(bottomDesc, "desc3");
            visual.Refresh(cd, "desc2");
        }

        public void RefreshCD() {
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
        }
    }
}