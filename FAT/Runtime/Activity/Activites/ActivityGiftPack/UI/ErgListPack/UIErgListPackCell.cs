/*
 * @Author: tang.yan
 * @Description: 体力列表礼包界面 Cell 
 * @Date: 2025-04-14 10:04:48
 */

using DG.Tweening;
using EL;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class UIErgListPackCell : UIGenericItemBase<PackErgList.ErgTaskData>
    {
        [SerializeField] private RectTransform root;
        //未完成
        [SerializeField] private GameObject unFinishGo;
        [SerializeField] private TMP_Text taskDesc;
        [SerializeField] private UICommonProgressBar progressBar;
        //已完成未领取
        [SerializeField] private GameObject unClaimGo;
        [SerializeField] private Button claimBtn;
        //已完成已领取
        [SerializeField] private GameObject claimedGo;
        [SerializeField] private Animator receiveAnim;
        //奖励
        [SerializeField] private UICommonItem reward;
        //特效动画
        [SerializeField] private Animation lockAnim;

        private Sequence _tweenSeq;

        protected override void InitComponents()
        {
            claimBtn.WithClickScale().FixPivot().onClick.AddListener(_OnClickBtnClaim);
        }
        
        protected override void UpdateOnDataChange()
        {
            _Refresh();
        }

        protected override void UpdateOnDataClear()
        {
            _ClearTween();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            MessageCenter.Get<MSG.GAME_ERG_LIST_PACK_BUY_SUCC>().AddListener(_OnBuySuccess);
        }

        protected override void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ERG_LIST_PACK_BUY_SUCC>().AddListener(_OnBuySuccess);
        }

        private void _Refresh()
        {
            if (mData == null)
                return;
            root.localScale = Vector3.one;
            var taskState = mData.State;
            unFinishGo.SetActive(taskState == 0);
            unClaimGo.SetActive(taskState == 1);
            claimedGo.SetActive(taskState == 2);
            var fontStyle = 9;
            var isGray = false;
            if (taskState == 0)
            {
                if (mData.Conf != null)
                {
                    taskDesc.text = I18N.FormatText(mData.Conf.Desc, mData.RequireCount, UIUtility.FormatTMPString(mData.Conf.IconShow));
                }
                progressBar.ForceSetup(0, mData.RequireCount, mData.ProgressValue);
            }
            else if (taskState == 1)
            {
                fontStyle = 17;
            }
            else if (taskState == 2)
            {
                fontStyle = 29;
                isGray = true;
            }
            reward.Refresh(mData.Reward, fontStyle);
            reward.SetGray(isGray);
            lockAnim.gameObject.SetActive(!mData.HasBuy);
            if (!mData.HasBuy)
            {
                lockAnim.Play("eff_lock_close");
            }
        }

        private void _OnClickBtnClaim()
        {
            if (mData == null)
                return;
            if (Game.Manager.activity.Lookup(mData.BelongActId, out var act))
            {
                var pack = act as PackErgList;
                pack?.TryClaimTaskReward(mData, reward.transform.position);
            }
        }

        private void _OnBuySuccess()
        {
            if (mData == null)
                return;
            if (mData.HasBuy)
            {
                lockAnim.Play("eff_lock_openpack");
            }
        }

        public void OnClaimSuccess(int mDataId)
        {
            if (mData == null || mData.Id != mDataId)
                return;
            claimedGo.SetActive(true);
            receiveAnim.ResetTrigger("Punch");
            receiveAnim.SetTrigger("Punch");
            _ClearTween();
            _tweenSeq = DOTween.Sequence();
            _tweenSeq.AppendInterval(0.7f);
            _tweenSeq.Append(root.DOScale(Vector3.zero, 0.2f).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                root.localScale = Vector3.zero;
            }));
        }

        private void _ClearTween()
        {
            _tweenSeq?.Kill();
            _tweenSeq = null;
        }
    }
}