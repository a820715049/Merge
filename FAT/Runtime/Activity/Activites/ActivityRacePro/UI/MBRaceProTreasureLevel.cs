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

        public void InitState(int round)
        {
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
            var round = RaceManager.GetInstance().Race.ConfD.NormalRoundId[5];
            var group = fat.conf.Data.GetEventRaceGroup(round);
            var rewardID = fat.conf.Data.GetEventRaceRound(Game.Manager.userGradeMan.GetTargetConfigDataId(group.IncludeRoundGrpId)).RaceGetGift[0];
            var config = fat.conf.Data.GetEventRaceReward(rewardID);
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, transform.Find("info/info").position, 50f, config.Reward);
        }
    }
}