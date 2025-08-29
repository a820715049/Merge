/**
 * @Author: zhangpengjian
 * @Date: 2025/7/28 15:52:10
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/28 15:52:10
 * Description: 耗体自选活动奖励cell
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using System.Collections.Generic;
using Config;

namespace FAT
{
    public class UIActivityWishUponCell : MonoBehaviour
    {
        [SerializeField] private UICommonItem[] rewardItems;
        [SerializeField] private Button claimBtn;
        [SerializeField] private UITextState claimBtnText;

        private ActivityWishUpon _activity;
        private int _index;

        /// <summary>
        /// 获取领取按钮引用，供UIActivityWishUpon管理事件
        /// </summary>
        public Button GetClaimButton()
        {
            return claimBtn;
        }

        /// <summary>
        /// 播放奖励飞行效果
        /// </summary>
        public void PlayFlyReward(List<RewardCommitData> rewards)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                UIFlyUtility.FlyReward(rewards[i], rewardItems[i].transform.position);
            }
        }

        public void UpdateContent(List<RewardConfig> rewards, ActivityWishUpon activity, int index, int energyCost)
        {
            _activity = activity;
            _index = index;

            for (int i = 0; i < rewardItems.Length; i++)
            {
                rewardItems[i].gameObject.SetActive(false);
                rewardItems[i].transform.GetChild(0).gameObject.SetActive(false);
            }
            claimBtn.transform.GetChild(1).gameObject.SetActive(false);

            for (int i = 0; i < rewards.Count; i++)
            {
                rewardItems[i].gameObject.SetActive(true);
                rewardItems[i].Refresh(rewards[i]);
                if (_activity.EnergyCost >= _activity.confD.Score && energyCost == _activity.EnergyCost)
                {
                    rewardItems[i].transform.GetChild(0).gameObject.SetActive(true);
                }
                else
                {
                    rewardItems[i].transform.GetChild(0).gameObject.SetActive(false);
                }
            }


            if (_activity.EnergyCost >= _activity.confD.Score && energyCost == _activity.EnergyCost)
            {
                GameUIUtility.SetDefaultShader(claimBtn.image);
                claimBtnText.Select(0);
                claimBtn.interactable = true;
            }
            else
            {
                GameUIUtility.SetGrayShader(claimBtn.image);
                claimBtnText.Select(1);
                claimBtn.interactable = false;
            }
        }

        public void PlayAnim()
        {
            StartCoroutine(PlayAnimCoroutine());
        }

        private IEnumerator PlayAnimCoroutine()
        {
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < rewardItems.Length; i++)
            {
                rewardItems[i].transform.GetChild(0).gameObject.SetActive(true);
            }
            claimBtn.transform.GetChild(1).gameObject.SetActive(true);
            GameUIUtility.SetDefaultShader(claimBtn.image);
            claimBtnText.Select(0);
            claimBtn.interactable = true;
        }
    }
}