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
            compReward?.SetLinkCallback(_OnBtnLink);
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
        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.APP_ENTER_FOREGROUND_EVENT>().AddListener(RefreshCommunityLinkReward);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.APP_ENTER_FOREGROUND_EVENT>().RemoveListener(RefreshCommunityLinkReward);
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
                    rewards.Add(new RewardValue { id = item.Key, count = item.Value, isClaimed = mMail.IsClaimed });
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
            if (mMail.LinkType == MailLinkType.MailExternalLink)
            {
                compReward?.SetLinkBtnState(mMail.LinkType == MailLinkType.MailExternalLink);
            }
            else
            {
                compReward?.SetClaimState(!mMail.IsClaimed);
            }
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
                MessageCenter.Get<MSG.MAIL_ITEM_REFRESH>().Dispatch();
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

        private void _OnBtnLink()
        {
            if (string.IsNullOrEmpty(mMail.Link))
            {
                return;
            }
            UIBridgeUtility.OpenURL(mMail.Link);
            DebugEx.Info("mMail.Link:" + mMail.Link);
            Game.Manager.mailMan.RecordClickLinkMail = mMail;
            var mailMan = Game.Manager.mailMan;
            DataTracker.mail_link.Track(mMail.Type.ToString(), mMail.FromUid.ToString(), mMail.Title, mMail.Rewards, mailMan.SingleMailHasReward(mMail), mailMan.IsLinkMail(mMail));
        }

        private void RefreshCommunityLinkReward()
        {
            var mail = Game.Manager.mailMan.RecordClickLinkMail;
            if (!IsGetLinkReward(mail))
            {
                return;
            }
            if (mail.LinkType == MailLinkType.MailExternalLink)
            {
                //返回进行领取奖励流程
                _OnBtnClaim();
                Game.Manager.mailMan.RecordClickLinkMail = null;
            }
        }

        private bool IsGetLinkReward(Mail mail)
        {
            if (mail == null)
            {
                DebugEx.Info("RefreshCommunityLinkReward: mail is null");
                return false;
            }
            if (mail.IsClaimed)
            {
                DebugEx.Info("RefreshCommunityLinkReward: mail is claimed");
                return false;
            }
            return true;
        }
    }
}