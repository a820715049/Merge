/*
 * @Author: qun.chao
 * @Date: 2021-07-19 15:02:56
 */
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EL;
using fat.gamekitdata;
using TMPro;
using System;

namespace FAT
{
    public class UIMailDetailBase : UIBase
    {
        [SerializeField] protected UIMailDetailCompReward compReward;
        [SerializeField] private TextMeshProUGUI MailTitle;
        [SerializeField] private TextMeshProUGUI MailDesc;
        [SerializeField] private UIImageRes preImg;
        [SerializeField] private UIImageRes postImg;
        [SerializeField] private RectTransform contentNode;
        protected Mail mMail;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Panel/Top/BtnClose", base.Close);
            compReward?.SetCallback(_OnBtnClaim);
        }

        protected override void OnParse(params object[] items)
        {
            mMail = items[0] as Mail;
        }

        protected override void OnPreOpen()
        {
            // mark read
            if (!mMail.IsRead)
                Game.Manager.mailMan.SetMailRead(mMail.Id);
            _RefreshClaimBtn();
            _ShowReward(mMail.Rewards);
            ShowDetail();
            if (contentNode != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentNode);
            }
        }

        protected override void OnPostClose()
        {
            _ClearReward();
            ClearDetail();
        }

        protected virtual void ShowDetail()
        {
            if (mMail.IsRaw)
            {
                MailTitle.text = mMail.Title;
                MailDesc.text = mMail.Content;
            }
            else
            {
                MailTitle.text = I18N.Text(mMail.Title);
                MailDesc.text = I18N.Text(mMail.Content);
            }

            if (mMail != null)
            {
                SetImageByUrl();
            }
        }

        protected virtual void ClearDetail()
        { }

        private void _ShowReward(IDictionary<int, int> dict)
        {
            if (dict.Count == 0)
            {
                compReward.SetDataEmpty();
            }
            else
            {
                var rewards = new List<RewardValue>();
                foreach (var item in dict)
                {
                    rewards.Add(new RewardValue { id = item.Key, count = item.Value });
                }
                rewards.Sort((a, b) => a.id - b.id);

                compReward.SetData(rewards);
            }
        }

        private void _ClearReward()
        {
            compReward?.ClearData();
        }

        private void _RefreshClaimBtn()
        {
            compReward?.SetClaimState(!mMail.IsClaimed);
        }

        private void _OnBtnClaim()
        {
            IEnumerator Routine()
            {
                UIManager.Instance.Block(true);
                var task = Game.Manager.mailMan.RequestMailReward(mMail.Id);
                while (task.keepWaiting) yield return null;
                _RequestRewardCallback(task);
                UIManager.Instance.Block(false);
            }
            Game.Instance.StartCoroutineGlobal(Routine());
        }

        private void _RequestRewardCallback(AsyncTaskBase task)
        {
            if (task.isSuccess)
            {
                // 立即不允许再点击
                _RefreshClaimBtn();
                var resp = task as SimpleResultedAsyncTask<List<RewardCommitData>>;
                UIFlyUtility.FlyRewardList(resp.result, compReward.transform.position);
            }
            else
            {
                Game.Manager.commonTipsMan.ShowClientTips($"code:{task.errorCode}\n{task.error}");
            }
        }

        private void SetImageByUrl()
        {
            preImg.gameObject.SetActive(false);
            postImg.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(mMail.ImageUrl))
            {
                if (mMail.ImagePosition == MailImagePositionType.MailImagePositionTop)
                {
                    preImg.gameObject.SetActive(true);
                    preImg.SetUrl(mMail.ImageUrl);
                }
                else if (mMail.ImagePosition == MailImagePositionType.MailImagePositionBottom)
                {
                    postImg.gameObject.SetActive(true);
                    postImg.SetUrl(mMail.ImageUrl);
                }
            }
        }
    }
}