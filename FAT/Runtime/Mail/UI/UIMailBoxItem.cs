/*
 * @Author: qun.chao
 * @Date: 2020-11-19 14:35:56
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using GameNet;
using EL;
using fat.gamekitdata;
using TMPro;
using CenturyGame.AppUpdaterLib.Runtime;

namespace FAT
{
    public class UIMailBoxItem : FancyScrollRectCell<Mail, UICommonScrollRectDefaultContext>
    {

        [SerializeField] private Transform newRoot;
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private TextMeshProUGUI textTitle;
        [SerializeField] private TextMeshProUGUI textLeftTime;
        [SerializeField] private RectTransform textNode;

        private Mail mMail;

        private void Awake()
        {
            transform.AddButton(null, _OnBtnClick);
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_MAIL_STATE_CHANGE>().AddListener(_OnMessageMailChange);
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_MAIL_STATE_CHANGE>().RemoveListener(_OnMessageMailChange);
        }

        public override void UpdateContent(Mail mail)
        {
            mMail = mail;

            if (mMail.IsRaw)
            {
                textTitle.text = mail.Title;
            }
            else
            {
                textTitle.text = I18N.Text(mail.Title);
            }

            _RefreshStatus();
            _RefreshExpire();

            IEnumerator rebuild()
            {
                yield return null;
                LayoutRebuilder.MarkLayoutForRebuild(textNode);
            }

            StartCoroutine(rebuild());

            // 强制刷新布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(textNode);
        }

        private void _RefreshStatus()
        {
            newRoot.gameObject.SetActive(!mMail.IsRead);
            rewardRoot.gameObject.SetActive(_CheckHasReward());
        }

        private void _RefreshExpire()
        {
            textLeftTime.gameObject.SetActive(mMail.ExpireTime > 0);
            var left = mMail.ExpireTime - Game.Instance.GetTimestampSeconds();
            UIUtility.CountDownFormat(textLeftTime, left);
            textLeftTime.text = string.Format("{0} {1}", I18N.Text("#SysComDesc147"), textLeftTime.text);
        }

        private bool _CheckHasReward()
        {
            return mMail.IsClaimed == false && mMail.Rewards.Count > 0;
        }

        private void _OnBtnClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIMailDetailSystem, mMail);

            //switch (mMail.Type)
            //{
            //    case MailType.System:
            //    case MailType.WithNoParam:
            //        UIManager.Instance.OpenWindow(UIConfig.UIMailDetailSystem, mMail);
            //        break;
            //}
        }

        private void _OnMessageMailChange()
        {
            _RefreshStatus();
        }

        private void _OnSecondPass()
        {
            _RefreshExpire();
        }
    }
}