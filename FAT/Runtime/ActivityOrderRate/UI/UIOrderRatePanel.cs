using EL;
using TMPro;

namespace FAT
{
    public class UIOrderRatePanel : UIBase
    {
        private TextMeshProUGUI _cd;
        private ActivityOrderRate _act;
        private MBOrderRateProgress _progress;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Bg/ConfirmBtn", Close);
            transform.AddButton("Content/Bg/CloseBtn", Close);
            transform.Access("Content/Bg/RewardBg", out _progress);
            transform.Access("Content/Bg/_cd/text", out _cd);
        }
        protected override void OnParse(params object[] items)
        {
            if (items[0] is ActivityOrderRate act)
            {
                _act = act;
                _progress.SetReward(_act);
                _progress.SetProgress(_act.phase, _act.Reward3.Item2, _act);
                UpdateCD();
            }
        }
        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(UpdateCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(UpdateCD);
        }

        private void UpdateCD()
        {
            if (_act == null)
            {
                UIUtility.CountDownFormat(_cd, 0);
            }
            else
            {
                UIUtility.CountDownFormat(_cd, _act.Countdown);
                if (_act.Countdown <= 0)
                {
                    Close();
                }
            }
        }
    }
}