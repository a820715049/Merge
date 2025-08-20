/*
 *@Author:chaoran.zhang
 *@Desc:装饰区活动开启弹板
 *@Created Time:2024.05.27 星期一 11:09:23
 */

using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIDecorateStartNotice : UIBase
    {
        private UIImageRes _bg;
        private UIImageRes _titleBg;
        private TextProOnACircle _title;
        private DecorateActivity _activity;
        private TextMeshProUGUI _leftTime;
        private TextMeshProUGUI _desc;

        protected override void OnCreate()
        {
            _bg = transform.Find("Content/Panel/bg").GetComponent<UIImageRes>();
            _titleBg = transform.Find("Content/Panel/bg1").GetComponent<UIImageRes>();
            _title = transform.Find("Content/Panel/bg1/title").GetComponent<TextProOnACircle>();
            _leftTime = transform.Find("Content/Panel/_cd/text").GetComponent<TextMeshProUGUI>();
            _desc = transform.Find("Content/Panel/desc1").GetComponent<TextMeshProUGUI>();
            transform.AddButton("Content/Panel/confirm", ClickBtn);
        }

        protected override void OnPreOpen()
        {
            _activity = Game.Manager.decorateMan.Activity;
            RefreshTheme();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            RefreshCD();
        }

        private void RefreshTheme()
        {
            var visual = _activity.StartRemindVisual;
            visual.Refresh(_bg, "bgimage");
            visual.Refresh(_titleBg, "titleImage");
            visual.Refresh(_title, "mainTitle");
            visual.Refresh(_desc, "desc2");
        }

        protected override void OnPostClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void RefreshCD()
        {
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, Game.Manager.decorateMan.Activity.endTS - t);
            UIUtility.CountDownFormat(_leftTime, diff);
        }

        private void ClickBtn()
        {
            if (!Game.Manager.mapSceneMan.scene.Ready) return;
            Game.Manager.decorateMan.Activity.ChangePopState(false);
            Close();
            if (Game.Manager.guideMan.IsGuideFinished(82))
                GameProcedure.MergeToSceneArea(Game.Manager.decorateMan.Activity.CurArea,
                    () => UIManager.Instance.OpenWindow(Game.Manager.decorateMan.Panel));
            else
                GameProcedure.MergeToSceneArea(Game.Manager.decorateMan.Activity.CurArea,
                    MessageCenter.Get<MSG.DECORATE_GUIDE_READY>().Dispatch);
        }
    }
}