using Cysharp.Text;
using EL;
using TMPro;

namespace FAT
{
    public class UIPachinkoRestart : UIBase
    {
        private UIImageRes _bg;
        private UIImageRes _titleBg;
        private TextProOnACircle _title;
        private UIImageRes _cdFrame;
        private TextMeshProUGUI _subTitle;
        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _desc;
        private ActivityPachinko _activity;

        protected override void OnCreate()
        {
            transform.Access("Content/Panel/bg", out _bg);
            transform.Access("Content/Panel/bg1", out _titleBg);
            transform.Access("Content/Panel/bg1/title", out _title);
            transform.Access("Content/Panel/_cd/frame", out _cdFrame);
            transform.Access("Content/Panel/_cd/text", out _cd);
            transform.Access("Content/Panel/desc1", out _desc);
            transform.Access("Content/Panel/bg2/desc2", out _subTitle);
            transform.AddButton("Content/Panel/confirm", OnClick);
        }

        protected override void OnPreOpen()
        {
            _activity = Game.Manager.pachinkoMan.GetActivity();
            if (_activity == null) return;
            _activity.RestartVisual.Refresh(_bg, "bg");
            _activity.RestartVisual.Refresh(_titleBg, "titleBg");
            _activity.RestartVisual.Refresh(_cdFrame, "time");
            _activity.RestartVisual.Refresh(_desc, "desc");
            _activity.RestartVisual.Refresh(_title, "mainTitle");
            _activity.RestartVisual.Refresh(_cd, "time");
            _activity.RestartVisual.Refresh(_subTitle, "subTitle");
            UIUtility.CountDownFormat(_cd, _activity?.Countdown ?? 0);
            _title.SetText(I18N.Text(Game.Manager.pachinkoMan.GetActivity().Conf.Name));
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
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