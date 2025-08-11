/*
 * @Author: qun.chao
 * @Date: 2022-10-25 15:01:24
 */
using UnityEngine;
using UnityEngine.UI;
using Config;

namespace FAT
{
    // TODO: 需要简化 更通用 or 弃用
    public class MBCommonItem : MonoBehaviour
    {
        [SerializeField] private UIImageRes iconRes;
        [SerializeField] private Text textNum;
        [SerializeField] private TMPro.TMP_Text tmpNum;
        [SerializeField] private Button btnInfo;

        public void ShowRewardCommitData(RewardCommitData reward)
        {
            ShowItemNormal(reward.rewardId, reward.rewardCount);
        }

        public void ShowRewardConfig(RewardConfig reward)
        {
            ShowItemNormal(reward.Id, reward.Count);
        }

        public void ShowItemFromRes(AssetConfig res, int id, int count)
        {
            if (iconRes != null)
            {
                UIItemUtility.NormalRes(iconRes, res);
            }
            if (textNum != null)
            {
                UIItemUtility.CountString(textNum, id, count);
            }
            if (tmpNum != null)
            {
                UIItemUtility.CountString(tmpNum, id, count);
            }
            if (btnInfo != null)
            {
                UIItemUtility.InfoForReward(btnInfo, id);
            }
        }

        /// <summary>
        /// NormalIcon / CountString / info
        /// </summary>
        public void ShowItemNormal(int id, int count)
        {
            if (iconRes != null)
            {
                UIItemUtility.NormalIcon(iconRes, id, count);
            }
            if (textNum != null)
            {
                UIItemUtility.CountString(textNum, id, count);
            }
            if (tmpNum != null)
            {
                UIItemUtility.CountString(tmpNum, id, count);
            }
            if (btnInfo != null)
            {
                UIItemUtility.Info(btnInfo, id);
            }
        }

        public void ShowItemReward(int id, int count)
        {
            if (iconRes != null)
            {
                UIItemUtility.NormalIcon(iconRes, id, count);
            }
            if (textNum != null)
            {
                UIItemUtility.CountString(textNum, id, count);
            }
            if (tmpNum != null)
            {
                UIItemUtility.CountString(tmpNum, id, count);
            }
            if (btnInfo != null)
            {
                UIItemUtility.InfoForReward(btnInfo, id);
            }
        }
    }
}