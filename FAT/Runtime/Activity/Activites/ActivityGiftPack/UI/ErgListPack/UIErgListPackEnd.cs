/*
 * @Author: tang.yan
 * @Description: 体力列表礼包结算界面 
 * @Date: 2025-04-16 18:04:33
 */
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class UIErgListPackEnd : UIBase
    {
        [SerializeField] private Button claimBtn;
        [SerializeField] private UICommonItem reward;

        private PackErgList pack;
        private RewardCommitData rewardData;
        
        protected override void OnCreate()
        {
            claimBtn.WithClickScale().FixPivot().onClick.AddListener(_OnClickBtnClaim);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 1)
            {
                pack = (PackErgList)items[0];
                rewardData = (RewardCommitData)items[1]; 
            }
        }

        protected override void OnPreOpen()
        {
            if (rewardData != null)
            {
                reward.gameObject.SetActive(true);
                reward.Refresh(rewardData.rewardId, rewardData.rewardCount);
            }
            else
                reward.gameObject.SetActive(false);
        }

        private void _OnClickBtnClaim()
        {
            if (rewardData != null)
                UIFlyUtility.FlyReward(rewardData, reward.transform.position);
            Close();
        }
    }
}