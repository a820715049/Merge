using EL;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBSpineReward : MonoBehaviour
    {
        public UICommonItem item1;
        public UICommonItem item2;
        public GameObject select;
        public Animator animator;
        public Animation complete;
        public GameObject mask;
        public UIStateGroup uIStateGroup;
        public void RefreshCellData(int rewardId, bool needMask, float size)
        {
            var conf = fat.conf.Data.GetSpinPackRewardPool(rewardId);
            _RefreshReward(conf);
            _RefreshMask(needMask);
            (item1.transform as RectTransform).sizeDelta = size * Vector2.one;
            (item2.transform as RectTransform).sizeDelta = size * Vector2.one;
        }

        private void _RefreshReward(SpinPackRewardPool reward)
        {
            if (reward.RewardList.TryGetByIndex(0, out var ret1)) { item1.Refresh(ret1.ConvertToRewardConfig()); }
            if (reward.RewardList.TryGetByIndex(1, out var ret2)) { item2.Refresh(ret2.ConvertToRewardConfig()); }
            item1.gameObject.SetActive(!string.IsNullOrEmpty(ret1));
            item2.gameObject.SetActive(!string.IsNullOrEmpty(ret2));
        }

        private void _RefreshMask(bool needMask)
        {
            select.SetActive(false);
            mask.SetActive(needMask);
        }

        public void SetGroup(int id)
        {
            uIStateGroup.Select(id);
        }

        public void PlayStartTrigger()
        {
            animator.SetTrigger("start");
        }
        public void PlayPunchTrigger()
        {
            animator.SetTrigger("punch");
        }

        public void PlayWinTrigger()
        {
            animator.SetTrigger("win");
        }
        public void PlayEndTrigger()
        {
            animator.SetTrigger("end");
            mask.SetActive(true);
            complete.Play();
        }
    }
}