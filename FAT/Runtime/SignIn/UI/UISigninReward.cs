using System.Collections.Generic;
using Config;
using EL;
using UnityEngine;

namespace FAT
{
    public class UISigninReward : UIBase
    {

        private Transform _rewardNode;
        private List<RewardCommitData> _reward;

        protected override void OnCreate()
        {
            transform.AddButton("Content/BtnClaim", ClickClaim);
            _rewardNode = transform.Find("Content/ItemRoot");
        }

        protected override void OnParse(params object[] items)
        {
            _reward = Game.Manager.loginSignMan.SignInRewards;
            var rewardConfigList = new List<RewardConfig>();
            if (_reward == null) return;
            foreach (var reward in _reward)
            {
                var config = new RewardConfig();
                config.Id = reward.rewardId;
                config.Count = reward.rewardCount;
                rewardConfigList.Add(config);
            }
            UIUtility.CommonItemSetup(_rewardNode);
            UIUtility.CommonItemRefresh(_rewardNode, rewardConfigList);
        }

        private void ClickClaim()
        {
            var i = 0;
            if (_reward != null)
                foreach (var data in _reward) UIFlyUtility.FlyReward(data, _rewardNode.GetChild(i++).position);
            Close();
        }

        protected override void OnPostClose()
        {
            _reward = null;
            Game.Manager.loginSignMan.ClearTotalReward();
            UIManager.Instance.CloseWindow(UIConfig.UISignInpanel);
        }
    }
}