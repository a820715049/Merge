/*
 * @Author: qun.chao
 * @Date: 2020-11-19 14:35:29
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using fat.gamekitdata;
using System.Collections;
using TMPro;

namespace FAT
{
    public class UIMailBox : UIBase
    {
        [SerializeField] private GameObject goEmptyTip;
        [SerializeField] private UIMailBoxScrollRect scrollRect;
        private Button mBtnClaimAll;
        private GameObject mTextClaimAll;
        private GameObject mTextClaimAll_unable;
        private List<Mail> mCache = new();

        protected override void OnCreate()
        {
            transform.Find("Mask").GetComponent<Button>().onClick.AddListener(Close);
            transform.AddButton("Content/Panel/Top/BtnClose", base.Close);
            mBtnClaimAll = transform.AddButton("Content/Panel/Bottom/BtnClaimAll", _OnBtnClaimAll);
            mTextClaimAll = mBtnClaimAll.transform.Find("TextClaimAll").gameObject;
            mTextClaimAll_unable = mBtnClaimAll.transform.Find("TextClaimAll_unable").gameObject;

            scrollRect.InitLayout();
        }

        protected override void OnPreOpen()
        {
            _RefreshList();
            _RefreshClaimAllBtn();
            MessageCenter.Get<MSG.GAME_MAIL_STATE_CHANGE>().AddListener(_OnMessageMailStateChange);
            MessageCenter.Get<MSG.GAME_MAIL_LIST_CHANGE>().AddListener(_OnMessageMailListChange);
        }

        protected override void OnPostClose()
        {
            MessageCenter.Get<MSG.GAME_MAIL_STATE_CHANGE>().RemoveListener(_OnMessageMailStateChange);
            MessageCenter.Get<MSG.GAME_MAIL_LIST_CHANGE>().RemoveListener(_OnMessageMailListChange);
        }

        private void _RefreshList()
        {
            mCache.Clear();
            Game.Manager.mailMan.FillMailList(mCache);
            scrollRect.UpdateData(mCache);

            goEmptyTip.gameObject.SetActive(mCache.Count < 1);
            scrollRect.gameObject.SetActive(mCache.Count > 0);
        }

        private void _RefreshClaimAllBtn()
        {
            if(Game.Manager.mailMan.HasReward())
            {
                mBtnClaimAll.GetComponent<UIImageState>().Select(0);
                mBtnClaimAll.interactable = true;
                mTextClaimAll.gameObject.SetActive(true);
                mTextClaimAll_unable.gameObject.SetActive(false);
            }
            else
            {
                mBtnClaimAll.GetComponent<UIImageState>().Select(1);
                mBtnClaimAll.interactable= false;
                mTextClaimAll.gameObject.SetActive(false);
                mTextClaimAll_unable.gameObject.SetActive(true);
            }
        }

        private void _OnBtnClaimAll()
        {
            if (!Game.Manager.mailMan.HasReward())
                return;
            IEnumerator Routine() {
                UIManager.Instance.Block(true);
                var task = Game.Manager.mailMan.RequestAllMailReward();
                while (task.keepWaiting) yield return null;
                _OnClaimAllResp(task);
                UIManager.Instance.Block(false);
            }
            Game.Instance.StartCoroutineGlobal(Routine());
        }

        private void _OnClaimAllResp(AsyncTaskBase task)
        {
            if (task.isSuccess)
            {
                _RefreshClaimAllBtn();
                var resp = task as SimpleResultedAsyncTask<List<RewardCommitData>>;
                UIFlyUtility.FlyRewardList(resp.result, mBtnClaimAll.transform.position);
            }
            else
            {
                Game.Manager.commonTipsMan.ShowClientTips($"code:{task.errorCode}\n{task.error}");
            }
        }

        private void _OnMessageMailListChange()
        {
            _RefreshList();
        }

        private void _OnMessageMailStateChange()
        {
            _RefreshClaimAllBtn();
        }
    }
}