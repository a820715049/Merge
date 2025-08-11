/*
 * @Author: qun.chao
 * @Date: 2023-09-26 19:41:54
 */
using fat.gamekitdata;

namespace FAT.Merge
{
    public class ItemActivityComponent : ItemComponentBase
    {
        public int activityId { get; private set; }
        public int activityEnergy { get; private set; }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComActivity = new ComActivity
            {
                ActivityId = activityId,
                ActivityEnergy = activityEnergy
            };

        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if (itemData.ComActivity != null)
            {
                activityId = itemData.ComActivity.ActivityId;
                activityEnergy = itemData.ComActivity.ActivityEnergy;
            }
        }

        public void SetActivityEnergy(int actId, int energy)
        {
            activityId = actId;
            activityEnergy = energy;
        }
    }
}