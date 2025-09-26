using fat.conf;
using fat.rawdata;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBSevenDayMilestone : MonoBehaviour
    {
        public Animator animator;
        public UICommonItem reward;
        public TextMeshProUGUI milestoneCount;
        public Transform complete;

        public void RefreshData(int id)
        {
            var config = SevenDayTaskRwdVisitor.Get(id);
            if (config == null) { return; }
            reward.Refresh(config.Reward[0].ConvertToRewardConfig());
            milestoneCount.text = config.Points.ToString();
        }

        public void RefreshState(bool hasComplete)
        {
            complete.gameObject.SetActive(hasComplete);
            transform.Find("UICommonItem/Count").gameObject.SetActive(!hasComplete);
            if (!hasComplete) { animator.SetTrigger("Num"); }
        }

        public void PlayComplete()
        {
            complete.gameObject.SetActive(true);
            animator.SetTrigger("Punch");
        }
    }
}