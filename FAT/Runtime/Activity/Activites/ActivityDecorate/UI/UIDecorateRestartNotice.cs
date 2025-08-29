/*
 *@Author:chaoran.zhang
 *@Desc:装饰区新一轮活动组开启
 *@Created Time:2024.05.27 星期一 11:28:07
 */

using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIDecorateRestartNotice : UIBase
    {
        private UIImageRes _bg;
        private UIImageRes _titleBg;
        private TextProOnACircle _title;
        private TextMeshProUGUI _leftTime;
        private DecorateActivity _activity;
        private TextMeshProUGUI _desc;
        private TextMeshProUGUI _desc2;

        protected override void OnCreate()
        {
            _bg = transform.Find("Content/Panel/bg").GetComponent<UIImageRes>();
            _titleBg = transform.Find("Content/Panel/bg1").GetComponent<UIImageRes>();
            _title = transform.Find("Content/Panel/bg1/title").GetComponent<TextProOnACircle>();
            _leftTime = transform.Find("Content/Panel/_cd/text").GetComponent<TextMeshProUGUI>();
            _desc = transform.Find("Content/Panel/bg2/desc2").GetComponent<TextMeshProUGUI>();
            _desc2 = transform.Find("Content/Panel/desc1").GetComponent<TextMeshProUGUI>();
            transform.AddButton("Content/Panel/confirm", () =>
            {
                Game.Manager.decorateMan.Activity.ChangePopState(false);
                Close();
                GameProcedure.MergeToSceneArea(Game.Manager.decorateMan.Activity.CurArea,
                    () => UIManager.Instance.OpenWindow(Game.Manager.decorateMan.Panel));
            });
        }

        protected override void OnPreOpen()
        {
            _activity = Game.Manager.decorateMan.Activity;
            Game.Manager.decorateMan.SetCloudState(true);
            RefreshTheme();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        private void RefreshTheme()
        {
            var visual = _activity.ReStartVisual;
            visual.Refresh(_bg, "bgimage");
            visual.Refresh(_titleBg, "titleImage");
            visual.Refresh(_title, "mainTitle");
            visual.Refresh(_desc2, "desc2");
            _desc.text = I18N.FormatText("#SysComDesc402", "<size=120>" + (_activity.phase + 1) + "</size>");
        }

        protected override void OnPostClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.DECORATE_REFRESH>().Dispatch();
        }

        private void RefreshCD()
        {
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, Game.Manager.decorateMan.Activity.endTS - t);
            UIUtility.CountDownFormat(_leftTime, diff);
            if (diff == 0) Close();
        }
    }
}