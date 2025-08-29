using EL;
using TMPro;

namespace FAT
{
    public class UIOrderRateReward : UIBase
    {
        private MBOrderRateProgress _progress;
        private TextMeshProUGUI _title;
        private ActivityOrderRate _act;
        protected override void OnCreate()
        {
            transform.Access("Content/BgRoot/RewardBg", out _progress);
            transform.Access("Content/Title", out _title);
            transform.AddButton("Content/CloseBtn", Close);
        }
        protected override void OnParse(params object[] items)
        {
            if (items[0] is ActivityOrderRate act)
            {
                _act = act;
                _progress.SetReward(_act);
                _progress.SetProgress(_act.phase, _act.Reward3.Item2, _act);
                _progress.SetIdle(_act.GetCurReward());
                _title.text = I18N.Text(_act.GetTitle());
            }
        }

        protected override void OnPreOpen()
        {
            _progress.Show();
        }

        protected override void OnPreClose()
        {
            _progress.Hide();
        }

        protected override void OnPostClose()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIOrderRateShow, _act);
        }
    }
}