/**
 * @Author: zhangpengjian
 * @Date: 2025/5/27 15:21:00
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/27 15:21:00
 * Description: 朴素的奖励界面 （随机宝箱界面去掉宝箱）
 */

using System;
using UnityEngine;
using System.Collections.Generic;
using EL;
using Config;

namespace FAT
{
    public class UIRewardPanel : UIBase
    {
        [SerializeField] private GameObject rewardGo;
        [SerializeField] private List<UICommonItem> rewardGroup;
        private List<RewardConfig> _rewardList;
        private Action callback;
        private Action flyCallback;
        protected override void OnCreate()
        {
            foreach (var r in rewardGroup)
            {
                r.Setup();
            }
            transform.AddButton("Content/ClaimBtn", _OnBtnClaim);
        }

        protected override void OnParse(params object[] items)
        {
            _rewardList = items[0] as List<RewardConfig>;
            if (items.Length > 1)
            {
                callback = items[1] as Action;
                if (items.Length > 2)
                {
                    flyCallback = items[2] as Action;
                }
            }
        }

        protected override void OnPreOpen()
        {
            _RefreshReward();
        }

        protected override void OnPostOpen()
        {
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
        }

        private void _RefreshReward()
        {
            var index = 0;
            foreach (var uiReward in rewardGroup)
            {
                if (index < _rewardList.Count)
                {
                    uiReward.gameObject.SetActive(true);
                    uiReward.Refresh(_rewardList[index].Id, _rewardList[index].Count);
                }
                else
                {
                    uiReward.gameObject.SetActive(false);
                }
                index++;
            }
        }

        private void _OnBtnClaim()
        {
            callback?.Invoke();
            int index = 0;
            foreach (var reward in _rewardList)
            {
                if (index < rewardGroup.Count)
                {
                    UIFlyFactory.GetFlyTarget(FlyType.TreasureBag, out var to);
                    UIFlyUtility.FlyCustom(reward.Id, reward.Count, rewardGroup[index].transform.position, to, FlyStyle.Show, FlyType.TreasureBag, () =>
                    {
                        flyCallback?.Invoke();
                    }, null, 200f);
                }
                index++;
            }
            //再关界面
            Close();
        }
    }
}