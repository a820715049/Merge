/*
 * @Author: qun.chao
 * @Date: 2023-10-25 12:13:36
 */
using Config;
using UnityEngine;

namespace FAT
{
    public class MBBoardOrderRewardItem : MonoBehaviour
    {
        [SerializeField] private MBCommonItem item;

        public void SetData(RewardConfig reward)
        {
            SetData(reward.Id, reward.Count);
        }

        public void SetData(int id, int count)
        {
            var res = Game.Manager.rewardMan.GetRewardImage(id, count);
            item.ShowItemFromRes(res, id, count);
        }
    }
}