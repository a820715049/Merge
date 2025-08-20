/*
 * @Author: qun.chao
 * @Date: 2021-07-20 10:54:55
 */
using EL;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMailDetailCompReward : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rewardTitle;
        [SerializeField] private GameObject item;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private Button btnClaim;
        [SerializeField] private Button btnClaimUnable;
        [SerializeField] private Button btnLink;
        public void SetCallback(UnityEngine.Events.UnityAction cb)
        {
            btnClaim.onClick.AddListener(cb);
            ButtonExt.TryAddClickScale(btnClaim);
        }

        public void SetClaimState(bool b)
        {
            btnClaim.gameObject.SetActive(b);
            btnClaimUnable.gameObject.SetActive(!b);
            btnLink.gameObject.SetActive(false);
        }

        public void SetData(IList<RewardValue> list)
        {
            rewardTitle.gameObject.SetActive(true);
            //rewardTitle.text = I18N.Text();
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MAIL_DETAIL_REWARD_ITEM, item);
            UIUtility.CreateGenericPooItem(itemRoot, PoolItemType.MAIL_DETAIL_REWARD_ITEM, list);
        }

        //邮箱中没有reward时调用
        public void SetDataEmpty()
        {
            rewardTitle.gameObject.SetActive(false);
            btnClaim.gameObject.SetActive(false);
        }

        public void ClearData()
        {
            UIUtility.ReleaseClearableItem(itemRoot, PoolItemType.MAIL_DETAIL_REWARD_ITEM);
        }

        public void SetLinkBtnState(bool b)
        {
            btnClaim.gameObject.SetActive(!b);
            btnClaimUnable.gameObject.SetActive(!b);
            btnLink.gameObject.SetActive(b);
        }

        public void SetLinkCallback(UnityEngine.Events.UnityAction cb)
        {
            btnLink.onClick.AddListener(cb);
            ButtonExt.TryAddClickScale(btnLink);
        }
    }
}