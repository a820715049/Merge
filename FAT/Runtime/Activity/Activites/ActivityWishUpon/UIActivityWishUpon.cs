/**
 * @Author: zhangpengjian
 * @Date: 2025/7/28 15:42:28
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/28 15:42:28
 * Description: 耗体自选活动主界面
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FAT
{
    public class UIActivityWishUpon : UIBase
    {
        [SerializeField] private List<GameObject> itemList;
        [SerializeField] private TextMeshProUGUI cd;
        [SerializeField] private Button closeBtn;
        [SerializeField] private MBRewardProgress progress;
        [SerializeField] private GameObject block;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject efx;

        private ActivityWishUpon _activity;
        private bool _isComplete;
        private bool _isClaiming = false; // 防止重复点击标志

        protected override void OnCreate()
        {
            closeBtn.onClick.AddListener(Close);
        }

        protected override void OnParse(params object[] items)
        {
            _activity = (ActivityWishUpon)items[0];
            if (items.Length > 1 && items[1] != null)
            {
                _isComplete = (bool)items[1];
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(OnOneSecond);
            AddButtonListeners();
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnOneSecond);
            RemoveButtonListeners();
        }

        protected override void OnPreOpen()
        {
            var energyCost = _activity.EnergyShow;
            efx.SetActive(false);
            progress.Refresh(_activity.EnergyShow, _activity.confD.Score);
            closeBtn.gameObject.SetActive(true);
            OnOneSecond();
            var rewardList = _activity.GetRewardList();
            for (int i = 0; i < rewardList.Count; i++)
            {
                itemList[i].GetComponent<UIActivityWishUponCell>().UpdateContent(rewardList[i], _activity, i, energyCost);
            }
            if (_activity.EnergyCost > _activity.EnergyShow)
            {
                block.SetActive(true);
                progress.Refresh(_activity.EnergyCost, _activity.confD.Score, 0.5f, ()=>
                {
                    block.SetActive(false);
                    if (_activity.EnergyCost >= _activity.confD.Score)
                    {
                        efx.SetActive(true);
                        animator.SetTrigger("Punch");
                        for (int i = 0; i < itemList.Count; i++)
                        {
                            itemList[i].GetComponent<UIActivityWishUponCell>().PlayAnim();
                        }
                    }
                });
            }
            else
            {
                block.SetActive(false);
            }
            _activity.SetEnergyShow(_activity.EnergyCost);
        }

        protected override void OnPostClose()
        {
            _isComplete = false;
            _isClaiming = false;
        }

        private void OnOneSecond()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            if (diff <= 0 && _isComplete)
            {
                closeBtn.gameObject.SetActive(false);
            }
            cd.SetCountDown(diff);
        }

        private void AddButtonListeners()
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                var cell = itemList[i].GetComponent<UIActivityWishUponCell>();
                var button = cell.GetClaimButton();
                var index = i; // 捕获循环变量
                button.onClick.AddListener(() => OnClaim(index));
            }
        }

        private void RemoveButtonListeners()
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                var cell = itemList[i].GetComponent<UIActivityWishUponCell>();
                var button = cell.GetClaimButton();
                button.onClick.RemoveAllListeners();
            }
        }

        private void OnClaim(int index)
        {
            // 防止重复点击
            if (_isClaiming) return;
            _isClaiming = true;

            var content = I18N.Text("#SysComDesc1471");
            Game.Manager.commonTipsMan.ShowMessageTips(content, 
                rightBtnCb: () => OnClaimOK(index), 
                leftBtnCb: OnClaimCancel);
        }

        private void OnClaimOK(int index)
        {
            var cell = itemList[index].GetComponent<UIActivityWishUponCell>();
            var rewards = _activity.BeginRewardByIndex(index, _isComplete);
            if (rewards.Count > 0)
            {
                cell.PlayFlyReward(rewards);
            }
            _isClaiming = false; // 重置防双击标志
        }

        private void OnClaimCancel()
        {
            _isClaiming = false; // 重置防双击标志
        }
    }
}