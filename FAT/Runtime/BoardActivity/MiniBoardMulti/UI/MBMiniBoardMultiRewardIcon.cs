using UnityEngine;

namespace FAT
{
    public class MBMiniBoardMultiRewardIcon : MBRewardIcon
    {
        public GameObject lockImg;

        public override void Refresh(int id_, int count_ = 1)
        {
            lockImg.SetActive(count_ == -1);
            if (count_ == -1)
            {
                var count = Game.Manager.miniBoardMultiMan.World?.rewardCount ?? 0;
                base.Refresh(id_, count);
            }
            else
            {
                base.Refresh(id_, count_);
            }
        }
    }
}