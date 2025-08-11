/**
 * @Author: zhangpengjian
 * @Date: 2025/1/14 10:45:18
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/1/14 10:45:18
 * Description: 卡册印章cell
 */

using Config;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UICardStampCell : MonoBehaviour
    {
        [SerializeField] private Animation animator;
        [SerializeField] private TextMeshProUGUI txtNum;
        [SerializeField] private GameObject imgIcon;
        [SerializeField] private GameObject objIcon;
        [SerializeField] private UIImageRes rewardIcon;

        public void Setup(int num, bool isShowStamp, bool hasExtraReward, AssetConfig icon = null)
        {
            animator.gameObject.SetActive(false);
            txtNum.text = num.ToString();
            if (hasExtraReward && icon != null)
            {
                rewardIcon.SetImage(icon);
            }
            objIcon.gameObject.SetActive(hasExtraReward && !isShowStamp);
            imgIcon.gameObject.SetActive(isShowStamp);
        }

        public void PlayAnim()
        {
            objIcon.gameObject.SetActive(false);
            transform.SetAsLastSibling();
            animator.gameObject.SetActive(true);
            animator.Play();
            Game.Manager.audioMan.TriggerSound("StampCardCover");
        }
    }
}