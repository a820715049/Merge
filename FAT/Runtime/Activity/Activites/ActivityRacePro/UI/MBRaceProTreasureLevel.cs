using System;
using UnityEngine;
using EL;

namespace FAT
{
    public class MBRaceProTreasureLevel : MonoBehaviour
    {
        public GameObject duigou;
        public bool hasReward;
        private int _index = -1;
        private int state = 0;
        
        private ActivityRaceExtend _activityRace;

        public void InitState(ActivityRaceExtend activityRace, int round)
        {
            _activityRace = activityRace;
            _index = int.Parse(name);
            UpdateState(round);
            if (hasReward)
            {
                InitClick();
            }
        }

        public void UpdateState(int round)
        {
            if (_index > round) { state = 1; }
            else if (_index == round) { state = 2; }
            else { state = 3; }
            duigou.gameObject.SetActive(state == 3);
        }

        public void InitClick()
        {
            transform.AddButton("info", OnClick);
        }

        private void OnClick()
        {
            var round = _activityRace.GetRoundConfigByIndex(_index);
            if (round.MilestoneRwd == 0)
            {
                Debug.LogError($"not milestoneRwd on {_index} from id: {round.Id}");
            }
            var rewardID = fat.conf.Data.GetEventRaceRound(round.MilestoneRwd).RaceGetGift[0];
            var config = fat.conf.Data.GetRaceExtendReward(rewardID);
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, transform.Find("info/info").position, 50f, config.Reward);
        }
    }
}