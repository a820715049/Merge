/*
 *@Author:chaoran.zhang
 *@Desc:Pachinko里程碑最终奖励
 *@Created Time:2024.12.16 星期一 20:06:32
 */

using System.Collections;
using System.Linq;
using EL;
using UnityEngine;

namespace FAT
{
    public class UIPachinkoLevelReward : UIBase
    {
        private Transform _rewardNode;
        private UIImageRes _icon;

        protected override void OnCreate()
        {
            transform.Access("Content/Icon", out _icon);
            transform.AddButton("Content/BtnClaim", ClickClaim);
            _rewardNode = transform.Find("Content/ItemRoot");
        }

        protected override void OnPreOpen()
        {
            UIUtility.CommonItemSetup(_rewardNode);
            var last = Game.Manager.pachinkoMan.GetMilestone().LastOrDefault();
            if (last == null) return;
            var rewardList = last.MilestoneReward.Select(info => info.ConvertToRewardConfig());
            UIUtility.CommonItemRefresh(_rewardNode, Enumerable.ToList(rewardList));
            _icon.SetImage(last.MilestoneRewardIcon2.ConvertToAssetConfig());
            Game.Manager.audioMan.TriggerSound("PachinkoClaimReward");
        }

        private void ClickClaim()
        {
            var commit = Game.Manager.pachinkoMan.GetFinalMilestoneRewardCommitData();
            var i = 0;
            foreach (var data in commit) UIFlyUtility.FlyReward(data, _rewardNode.GetChild(i++).position);
            Close();
        }

        protected override void OnPostClose()
        {
            Game.Instance.StartCoroutineGlobal(Enumerator());

            IEnumerator Enumerator()
            {
                yield return new WaitUntil(() => !Game.Manager.specialRewardMan.IsBusy());
                UIManager.Instance.Block(true);
                yield return new WaitForSeconds(1.5f);
                UIManager.Instance.Block(false);
                Game.Manager.pachinkoMan.ExitMainScene();
                Game.Manager.pachinkoMan.GetNextRoundData();
            }
        }
    }
}