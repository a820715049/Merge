/**
 * @Author: zhangpengjian
 * @Date: 2025/5/26 16:00:07
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/26 16:00:07
 * Description: 寻宝活动优化-开箱为空变为token 收集另一类token
 */

using UnityEngine;
using TMPro;
using Config;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT
{
    public class MBTreasureHuntMilestone : MonoBehaviour
    {
        [SerializeField] private UICommonItem item;
        [SerializeField] private MBRewardProgress progress;
        [SerializeField] private UIImageRes tokenIcon;
        [SerializeField] private Animator animator;
        [SerializeField] private Animator tokenAnimator;
        private (int, int) curProgress;
        private List<RewardConfig> _rewardList = new();

        public void SetData(RewardConfig rewardConfig, (int, int) value, int tokenId)
        {
            item.Refresh(rewardConfig);
            curProgress = value;
            progress.Refresh(curProgress.Item1, curProgress.Item2);
            tokenIcon.SetImage(Game.Manager.rewardMan.GetRewardIcon(tokenId, 1));
        }

        public void Anim()
        {
            tokenAnimator.SetTrigger("Punch");
            Game.Manager.audioMan.TriggerSound("TreasureBonusAccept");
            StartCoroutine(CoDelayAnim());
        }

        private IEnumerator CoDelayAnim()
        {
            yield return new WaitForSeconds(0.3f);
            if (!UITreasureHuntUtility.TryGetEventInst(out var eventInst)) yield break;
            var value = eventInst.GetBonusTokenShowNum();
            if (value.Item1 <= curProgress.Item1)
            {
                progress.Refresh(curProgress.Item2, curProgress.Item2, 0.5f);
                StartCoroutine(CoAnim());
            }
            else
            {
                progress.Refresh(value.Item1, value.Item2, 0.5f);
                UITreasureHuntUtility.SetBlock(false);
                curProgress = value;
            }
        }

        private IEnumerator CoAnim()
        {
            yield return new WaitForSeconds(0.5f);
            if (UITreasureHuntUtility.TryGetEventInst(out var eventInst))
            {
                _rewardList.Clear();
                if (eventInst.bonusTokenReward != null)
                {
                    _rewardList.Add(eventInst.bonusTokenReward);
                }
                System.Action callback = () =>
                {
                    UITreasureHuntUtility.SetBlock(false);
                };
                System.Action flyCallback = () =>
                {
                    int openFrameCount = Time.frameCount;
                    UITreasureHuntUtility.NotifyTreasureFlyFeedback(FlyType.TreasureBag);
                    // 飞完奖励尝试触发钥匙购买
                    UITreasureHuntUtility.TryResolveGiftShopRequest(openFrameCount);
                    var reward = eventInst.GetBonusTokenShowReward();
                    StartCoroutine(CoDelayChangeReward(reward));
                };
                if (_rewardList.Count > 0)
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIRewardPanel, _rewardList, callback, flyCallback);
                }
                eventInst.SetBonusTokenReward(null);
                var value = eventInst.GetBonusTokenShowNum();
                if (value.Item2 > 0)
                {
                    progress.Refresh(0, value.Item2);
                    progress.Refresh(value.Item1, value.Item2, 0.5f);
                    curProgress = value;
                    UITreasureHuntUtility.SetBlock(false);
                }
                else
                {
                    Visible(false);
                }
            }
        }

        private IEnumerator CoDelayChangeReward(RewardConfig reward)
        {
            yield return new WaitForSeconds(0.4f);
            animator.SetTrigger("Transition");
            Game.Manager.audioMan.TriggerSound("TreasureBonusSwitch");
            item.Refresh(reward);
        }

        public void Visible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}