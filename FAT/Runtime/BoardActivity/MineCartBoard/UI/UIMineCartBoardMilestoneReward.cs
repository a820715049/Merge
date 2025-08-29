
using System.Collections.Generic;
using Config;
using EL;
using UnityEngine;
using static EL.PoolMapping;


namespace FAT
{
    public class UIMineCartBoardMilestoneReward : UIBase
    {
        [SerializeField] private Transform _rewardNode;
        [SerializeField] private UIImageRes _icon;
        private Ref<List<RewardCommitData>> _reward;

        protected override void OnCreate()
        {
            base.OnCreate();
            transform.AddButton("Content/BtnClaim", ClickClaim);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 2)
            {
                _reward = (Ref<List<RewardCommitData>>)items[0];
                _icon.SetImage(items[1] as string);

                var rewardConfigList = new List<RewardConfig>();
                if (_reward.Valid)
                {
                    foreach (var reward in _reward.obj)
                    {
                        var config = new RewardConfig();
                        config.Id = reward.rewardId;
                        config.Count = reward.rewardCount;
                        rewardConfigList.Add(config);
                    }
                }
                UIUtility.CommonItemSetup(_rewardNode);
                UIUtility.CommonItemRefresh(_rewardNode, rewardConfigList);
            }
        }
        private void ClickClaim()
        {
            if (_reward.Valid)
            {
                for (int i = 0; i < _reward.obj.Count; i++)
                {
                    UIFlyUtility.FlyReward(_reward.obj[i], _rewardNode.GetChild(i).position);
                }
            }
            Close();
        }
        protected override void OnPostClose()
        {
            base.OnPostClose();
            if (_reward.Valid)
            {
                _reward.Free();
            }
        }
    }
}
