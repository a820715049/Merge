using System.Linq;
using Cysharp.Text;
using EL;
using TMPro;

namespace FAT
{
    public class UIPachinkoStart : UIBase
    {
        private UIImageRes _bg;
        private UIImageRes _titleBg;
        private TextProOnACircle _title;
        private UIImageRes _cdFrame;
        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _desc;
        private ActivityPachinko _activity;
        private MBRewardLayout _group;

        protected override void OnCreate()
        {
            transform.Access("Content/Panel/bg", out _bg);
            transform.Access("Content/Panel/bg1", out _titleBg);
            transform.Access("Content/Panel/bg1/title", out _title);
            transform.Access("Content/Panel/_cd/frame", out _cdFrame);
            transform.Access("Content/Panel/_cd/text", out _cd);
            transform.Access("Content/Panel/desc1", out _desc);
            transform.Access("Content/Panel/_group", out _group);
            transform.AddButton("Content/Panel/confirm", OnClick);
            transform.AddButton("Content/Panel/close", Close);
        }

        protected override void OnPreOpen()
        {
            _activity = Game.Manager.pachinkoMan.GetActivity();
            if (_activity == null) return;
            RefreshTheme();
            RefreshReward();
            UIUtility.CountDownFormat(_cd, _activity?.Countdown ?? 0);
        }

        /// <summary>
        /// 刷新UI换皮
        /// </summary>
        private void RefreshTheme()
        {
            _activity.StartVisual.Refresh(_bg, "bg");
            _activity.StartVisual.Refresh(_titleBg, "titleBg");
            _activity.StartVisual.Refresh(_cdFrame, "time");
            _activity.StartVisual.Refresh(_desc, "desc");
            _activity.StartVisual.Refresh(_title, "mainTitle");
            _activity.StartVisual.Refresh(_cd, "time");
            _desc.SetTextFormat(I18N.Text("#SysComDesc721"),
                ZString.Format("<sprite name=\"{0}\">", Game.Manager.pachinkoMan.GetTokenIcon()));
            _title.SetText(I18N.Text(Game.Manager.pachinkoMan.GetActivity().Conf.Name));
        }

        private void RefreshReward()
        {
            var info = Game.Manager.pachinkoMan.GetMilestone().LastOrDefault();
            if (info == null) return;
            var reward = Enumerable.ToList(info.MilestoneReward
                .Select(info => info.ConvertToRewardConfig()));
            _group.Refresh(reward);
        }


        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void RefreshCD()
        {
            var left = _activity?.Countdown ?? 0;
            if (left <= 0)
            {
                Close();
                return;
            }

            UIUtility.CountDownFormat(_cd, left);
        }

        private void OnClick()
        {
            Close();
            if (Game.Manager.pachinkoMan.GetCoinCount() > 0)
                Game.Manager.pachinkoMan.EnterMainScene();
        }
    }
}
