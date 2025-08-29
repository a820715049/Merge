using UnityEngine;
using EL;
namespace FAT
{
    public class MBRaceTreasureLevel : MonoBehaviour
    {
        public Animator animator;
        private int _index = -1;
        private int state = 0;
        private bool isInitialized = false;
        private int lastRound = 0;

        public void InitState(int index, int round)
        {
            if (!isInitialized)
            {
                _index = index;
                if (_index > round) { state = 1; }
                else if (_index == round) { state = 2; }
                else { state = 3; }
                InitAnim();
                if (index == 5)
                {
                    InitClick();
                }
            }
            else
            {
                if (lastRound <= round)
                {
                    animator.SetTrigger("State" + state);
                }
                else
                {
                    if (_index > round) { state = 1; }
                    else if (_index == round) { state = 2; }
                    else { state = 3; }
                    InitAnim();
                }
            }
            lastRound = round;
        }

        private void InitAnim()
        {
            if (state == 1)
            {
                animator.SetTrigger("State" + state);
            }
            else if (state == 2)
            {
                animator.SetTrigger("State1");
                animator.SetTrigger("Unlock");
            }
            else if (state == 3)
            {
                animator.SetTrigger("State" + state);
            }
            isInitialized = true;
        }

        public void UpdateState(int round)
        {
            if (state == 1 && _index == round)
            {
                state = 2;
                animator.SetTrigger("Unlock");
            }
            else if (state == 2 && _index < round)
            {
                state = 3;
                animator.SetTrigger("Complete");
            }
            else if (state == 2 && _index > round)
            {
                state = 1;
                animator.SetTrigger("State" + state);
            }
            else if (state == 3 && _index > round)
            {
                state = 1;
                animator.SetTrigger("State" + state);
            }
            else if (state == 3 && _index == round)
            {
                state = 2;
                animator.SetTrigger("State" + state);
            }

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
