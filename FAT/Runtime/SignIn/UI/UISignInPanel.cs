using System.Collections.Generic;
using EL;

namespace FAT
{
    public class UISignInPanel : UIBase
    {
        public List<MBSignReward> signRewards = new List<MBSignReward>();
        public MBSignProgress signProgress;
        protected override void OnCreate()
        {
            transform.AddButton("Content/Bg/CloseBtn", ClickClose);
        }

        protected override void OnPreOpen()
        {
            signProgress.Setup();
            var i = 0;
            var rewards = Game.Manager.loginSignMan.ConsecutiveSignInRewards;
            foreach (var item in signRewards)
            {
                item.SetUp(rewards.Count > i ? rewards[i] : 0, 0, ++i);
                item.InitState(rewards.Count > i);
            }
        }

        private void ClickClose()
        {
            if (Game.Manager.loginSignMan.SignInReward != null)
            {
                signRewards[Game.Manager.loginSignMan.ConsecutiveSignInDay - 1].OnClickGet();
            }
            else
            {
                Close();
            }
        }

        protected override void OnPreClose()
        {
            if (Game.Manager.loginSignMan.SignInReward != null)
            {
                Game.Manager.rewardMan.CommitReward(Game.Manager.loginSignMan.SignInReward);
                Game.Manager.loginSignMan.ClearConsecutiveReward();
            }
        }

    }

}