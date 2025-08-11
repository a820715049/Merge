/*
 * @Author: tang.yan
 * @Description: 图鉴界面链条棋子cell 
 * @Date: 2023-11-17 11:11:29
 */
using UnityEngine;
using UnityEngine.UI;
using Spine;
using Spine.Unity;
using System.Collections;

namespace FAT
{
    public class UIHandbookItemCell : UIGenericItemBase<int>
    {
        [SerializeField] private GameObject normalGo;
        [SerializeField] private GameObject normalMaxGo;
        [SerializeField] private GameObject previewGo;
        [SerializeField] private SkeletonGraphic previewSpine;
        [SerializeField] private GameObject lockGo;
        [SerializeField] private UIImageRes icon;
        [SerializeField] private Button rewardBtn;
        [SerializeField] private float delayAnimTime;
        private bool _isPlaySpineAnim = false;
        private Coroutine _coPlayAnim;
        
        protected override void InitComponents()
        {
            _Reset();
            rewardBtn.onClick.AddListener(_OnBtnClaim);
        }
        
        protected override void UpdateOnDataChange()
        {
            _Reset();
            _Refresh();
        }

        protected override void UpdateOnForce()
        {
            _Refresh();
        }

        protected override void UpdateOnDataClear()
        {
            _Reset();
        }

        private void _Refresh()
        {
            if (_isPlaySpineAnim)
                return;
            int itemId = mData;
            var mgr = Game.Manager.handbookMan;
            bool isLast = Game.Manager.mergeItemMan.IsLastItemInChain(itemId);
            normalGo.SetActive(!isLast);
            normalMaxGo.SetActive(isLast);
            previewGo.SetActive(mgr.IsItemPreview(itemId));
            previewSpine.gameObject.SetActive(mgr.IsItemCanClaim(itemId));
            lockGo.SetActive(mgr.IsItemLock(itemId));
            icon.gameObject.SetActive(mgr.IsItemReceived(itemId) || mgr.IsItemPreview(itemId));
            rewardBtn.gameObject.SetActive(mgr.IsItemCanClaim(itemId));
            var image = Game.Manager.objectMan.GetBasicConfig(itemId)?.Icon.ConvertToAssetConfig();
            if (image != null)
            {
                icon.SetImage(image.Group, image.Asset);
                Color color = Color.white;
                color.a = !mgr.IsItemPreview(itemId) ? 1f : 0.5f;
                icon.color = color;
            }
        }

        private void _OnBtnClaim()
        {
            if (!_isPlaySpineAnim && Game.Manager.handbookMan.IsItemCanClaim(mData))
            {
                _isPlaySpineAnim = true;
                bool isSucc = Game.Manager.handbookMan.TryClaimHandbookReward(mData, rewardBtn.transform.position);
                if (isSucc)
                {
                    rewardBtn.gameObject.SetActive(false);
                    icon.gameObject.SetActive(true);
                    _coPlayAnim = StartCoroutine(_CoPlaySpineAnim());
                }
            }
        }
        
        private IEnumerator _CoPlaySpineAnim()
        {
            yield return new WaitForSeconds(delayAnimTime);
            previewSpine.AnimationState.SetAnimation(0, "activate", false)
                .Complete += delegate(TrackEntry entry)
            {
                _isPlaySpineAnim = false;
                _Refresh();
            };
        }

        private void _Reset()
        {
            _isPlaySpineAnim = false;
            if (previewSpine != null)
            {
                previewSpine.AnimationState.SetAnimation(0, "normal", false);
            }
            if (_coPlayAnim != null)
            {
                StopCoroutine(_coPlayAnim);
                _coPlayAnim = null;
            }
        }
    }
}