using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBOrderRateProgress : MonoBehaviour
    {
        private RectMask2D _mask;
        private TextMeshProUGUI _coin;
        private MBOrderRateReward _reward1;
        private MBOrderRateReward _reward2;
        private MBOrderRateReward _reward3;

        private void Awake()
        {
            transform.Access("Progress/Mask", out _mask);
            transform.Access("Progress/Coin/Num", out _coin);
            transform.Access("Progress/Reward1", out _reward1);
            transform.Access("Progress/Reward2", out _reward2);
            transform.Access("Progress/Reward3", out _reward3);
        }

        public void SetReward(ActivityOrderRate activity)
        {
            _reward1.SetReward(1, activity.Reward1.Item2);
            _reward2.SetReward(2, activity.Reward2.Item2);
            _reward3.SetReward(3, activity.Reward3.Item2);
        }
        public void SetProgress(int cur, int max, ActivityOrderRate activity)
        {
            _coin.text = cur.ToString();
            var curReward = activity.GetCurRewardInfo();
            if (curReward.Item1 != 0)
            {
                if (cur < activity.Reward1.Item2)
                {
                    _mask.padding = new Vector4(0, 0, 202 * 2 + 202 * (1 - (float)cur / curReward.Item2), 0);
                    return;
                }
                if (cur < activity.Reward2.Item2)
                {
                    _mask.padding = new Vector4(0, 0, 202 * 1 + 202 * (1 - ((float)cur - activity.Reward1.Item2) / (activity.Reward2.Item2 - activity.Reward1.Item2)), 0);
                    return;
                }
                if (cur < activity.Reward3.Item2)
                {
                    _mask.padding = new Vector4(0, 0, 202 * 0 + 202 * (1 - ((float)cur - activity.Reward2.Item2) / (activity.Reward3.Item2 - activity.Reward2.Item2)), 0);
                    return;
                }
            }
            else
                _mask.padding = new Vector4(0, 0, 0, 0);
        }

        public void Show()
        {
            _reward1.Show();
            _reward2.Show();
            _reward3.Show();
        }

        public void Hide()
        {
            _reward1.Hide();
            _reward2.Hide();
            _reward3.Hide();
        }

        public void SetIdle(int index)
        {
            _reward1.SetIdle(index == 1);
            _reward2.SetIdle(index == 2);
            _reward3.SetIdle(index == 3);
        }

    }
}