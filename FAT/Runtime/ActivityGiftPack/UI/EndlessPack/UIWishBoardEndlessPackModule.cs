/*
 * @Author: yanfuxing
 * @Date: 2025-06-16 15:59:00
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    public class UIWishBoardEndlessPackModule
    {
        //根节点
        private Transform _moduleRoot;
        //当前要显示的数据对应的index
        private int _curIndex = -1;
        //活动数据
        private PackEndlessWishBoard pack;
        //当前要显示的数据Data
        private PackEndlessWishBoard.EndlessPkgData _curRewardData;

        //UI相关节点
        private RectTransform _rootRectTrans;    //根节点rect
        private GameObject _bgFreeGo;   //免费奖励背景图
        private GameObject _bgPayGo;    //付费奖励背景图
        private GameObject _curEffectGo;//付费奖励背景图
        private Transform _label;//折扣标签节点
        private List<UICommonItem> _rewardItemList = new List<UICommonItem>(); //奖励Item List
        private Button buyBtn;
        private GameObject freeGo;
        private Animation lockAnim;
        private GameObject iapGo;
        private UITextState iapPriceState;

        //位移tween
        private TweenerCore<Vector2, Vector2, VectorOptions> _tweenPos;
        //缩放tween
        private TweenerCore<Vector3, Vector3, VectorOptions> _tweenScale;
        //位移加缩放tween sequence
        private Sequence _tweenSeq;
        //变化值
        private Vector2 _deltaV2 = Vector2.zero;
        private Vector3 _deltaV3 = Vector3.zero;
        //tween动画结束回调
        private Action _tweenFinishCb;
        private readonly UIIAPLabel iapLabel = new();
        //字体样式
        private int _freeFontStyle = 0;
        private int _payFontStyle = 0;

        /// <summary>
        /// 绑定module根节点transform
        /// </summary>
        /// <param name="transform">根节点transform</param>
        public void BindModuleRoot(Transform transform)
        {
            if (transform != null)
                _moduleRoot = transform;
        }

        public void OnModuleCreate(int freeFontStyle = 0, int payFontStyle = 0)
        {
            if (_moduleRoot == null)
                return;
            _rootRectTrans = _moduleRoot as RectTransform;
            //背景图
            _moduleRoot.FindEx("BgFree", out _bgFreeGo);
            _moduleRoot.FindEx("BgPay", out _bgPayGo);
            _moduleRoot.FindEx("CurEffect", out _curEffectGo);
            _moduleRoot.FindEx("Label", out _label);
            //奖励Item 界面最多显示2个
            for (int i = 0; i < 2; i++)
            {
                var icon = _moduleRoot.FindEx<UICommonItem>("Item/Item" + i);
                _rewardItemList.Add(icon);
            }
            buyBtn = _moduleRoot.FindEx<Button>("BuyBtn");
            _moduleRoot.FindEx("BuyBtn/Free", out freeGo);
            _moduleRoot.FindEx("BuyBtn/IAP", out iapGo);
            lockAnim = _moduleRoot.FindEx<Animation>("BuyBtn/eff_lock_pack/ani");
            iapPriceState = _moduleRoot.FindEx<UITextState>("BuyBtn/IAP/Num");
            buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnBuyBtnClick);
            //字体样式
            _freeFontStyle = freeFontStyle;
            _payFontStyle = payFontStyle;
        }

        public void OnModulePreOpen(PackEndlessWishBoard _pack)
        {
            pack = _pack;
        }

        public void OnModulePostClose()
        {
            _curIndex = -1;
            _curRewardData = null;
            _ResetTween();
            iapLabel.Clear();
        }

        public void OnMessageInitIAP()
        {
            _RefreshBuyBtn();
        }

        public void SetModuleActive(bool isActive)
        {
            if (_moduleRoot != null)
                _moduleRoot.gameObject.SetActive(isActive);
        }

        public void SetModuleAnchorPos(Vector2 anchorPos)
        {
            if (_rootRectTrans != null)
                _rootRectTrans.anchoredPosition = anchorPos;
        }

        public void SetModuleScale(Vector3 scale)
        {
            if (_rootRectTrans != null)
                _rootRectTrans.localScale = scale;
        }

        public void UpdateRewardDataAndUI(int newDataIndex)
        {
            if (pack == null)
                return;
            _curIndex = newDataIndex;
            _curRewardData = pack.GetRewardDataByIndex(_curIndex);

            _RefreshRewardItemList();
            _RefreshBuyBtn();
            _RefreshUnlockState();
            _RefreshCurEffect();
            _RefreshLabel();
        }

        private void _RefreshLabel()
        {
            if (_moduleRoot == null || _curIndex < 0 || _curRewardData == null)
                return;
            var iapId = _curRewardData.IapId;
            bool isFree = iapId == 0;
            bool isValid = iapId >= 0;
            _bgFreeGo.gameObject.SetActive(isFree);
            _bgPayGo.gameObject.SetActive(!isFree);
            if (isFree)
            {
                _label.gameObject.SetActive(false);
            }
            else if (isValid)
            {
                _label.gameObject.SetActive(true);
                var pkgConf = GetOneEventWishEndlessInfoByFilter(x => x.Id == _curRewardData.BelongPkgId);
                iapLabel.Clear();
                iapLabel.Setup(_label, pkgConf.Label, _curRewardData.PackId);
            }
            else
            {
                _label.gameObject.SetActive(false);
            }
        }

        //刷新奖励UI
        private void _RefreshRewardItemList()
        {
            if (_moduleRoot == null || _curIndex < 0 || _curRewardData == null)
                return;
            var iapId = _curRewardData.IapId;
            bool isFree = iapId == 0;
            bool isValid = iapId >= 0;
            _bgFreeGo.gameObject.SetActive(isFree);
            _bgPayGo.gameObject.SetActive(!isFree);
            if (isFree)
            {
                _rewardItemList[0].gameObject.SetActive(true);
                _rewardItemList[0].Refresh(_curRewardData.FreeRewardInfo, _freeFontStyle == 0 ? 9 : _freeFontStyle);
                _rewardItemList[1].gameObject.SetActive(false);
            }
            else if (isValid)
            {
                var payInfo = _curRewardData.PayRewardInfo;
                for (int i = 0; i < _rewardItemList.Count; i++)
                {
                    if (i < payInfo.Count)
                    {
                        _rewardItemList[i].gameObject.SetActive(true);
                        _rewardItemList[i].Refresh(payInfo[i], _payFontStyle == 0 ? 17 : _payFontStyle);
                    }
                    else
                    {
                        _rewardItemList[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        //刷新购买按钮
        private void _RefreshBuyBtn()
        {
            if (_moduleRoot == null || _curIndex < 0 || _curRewardData == null)
                return;
            var iapId = _curRewardData.IapId;
            bool isFree = iapId == 0;
            bool isValid = iapId >= 0;
            if (isFree)
            {
                freeGo.SetActive(true);
                iapGo.SetActive(false);
            }
            else if (isValid)
            {
                freeGo.SetActive(false);
                iapGo.SetActive(true);
                var iap = Game.Manager.iap;
                var valid = iap.Initialized;
                iapPriceState.Enabled(valid, iap.PriceInfo(_curRewardData.IapId));
                if (valid)
                {
                    buyBtn.interactable = true;
                    GameUIUtility.SetDefaultShader(buyBtn.image);
                }
                else
                {
                    buyBtn.interactable = false;
                    GameUIUtility.SetGrayShader(buyBtn.image);
                }
            }
            //数据非法时按钮不可交互
            else
            {
                buyBtn.interactable = false;
                freeGo.SetActive(false);
                iapGo.SetActive(false);
            }
        }

        //刷新锁定状态
        private void _RefreshUnlockState(bool isRefresh = false)
        {
            if (_moduleRoot == null || _curIndex < 0 || _curRewardData == null || pack == null)
                return;
            bool canGet = pack.CheckCanGetReward(_curIndex);
            if (!canGet)
            {
                lockAnim.gameObject.SetActive(true);
                lockAnim.Play("eff_lock_close");
            }
            else
            {
                if (isRefresh)
                {
                    lockAnim.gameObject.SetActive(true);
                    lockAnim.Play("eff_lock_openpack");
                    //解锁音效
                    Game.Manager.audioMan.TriggerSound("FreeComplete");
                }
                else
                {
                    lockAnim.gameObject.SetActive(false);
                }
            }
        }

        private void _RefreshCurEffect()
        {
            if (_moduleRoot == null || _curIndex < 0 || _curRewardData == null || pack == null)
                return;
            bool canGet = pack.CheckCanGetReward(_curIndex);
            _curEffectGo.SetActive(canGet);
        }

        private void _OnBuyBtnClick()
        {
            if (_moduleRoot == null || _curIndex < 0 || _curRewardData == null || pack == null)
                return;
            bool canGet = pack.CheckCanGetReward(_curIndex);
            if (!canGet)
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.EndlessClaim);
                return;
            }
            pack.TryGetReward(_curIndex);
        }

        public void OnPurchaseComplete(int targetIndex, IList<RewardCommitData> list_)
        {
            if (targetIndex != _curIndex)
                return;
            for (var n = 0; n < list_.Count; ++n)
            {
                var pos = _rewardItemList[n].transform.position;
                UIFlyUtility.FlyReward(list_[n], pos);
            }
        }

        #region tween动画表现相关

        private void _ResetTween()
        {
            _tweenPos?.Kill();
            _tweenPos = null;
            _tweenScale?.Kill();
            _tweenScale = null;
            _tweenSeq?.Kill();
            _tweenSeq = null;
            _tweenFinishCb = null;
        }

        public Tween StartTweenMove(float targetX, float targetY, float duration, Action finishCb = null)
        {
            _ResetTween();
            _deltaV2.Set(targetX, targetY);
            _tweenFinishCb = finishCb;
            _tweenPos = _rootRectTrans.DOAnchorPos(_deltaV2, duration).SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    SetModuleAnchorPos(_deltaV2);
                    if (_tweenFinishCb != null)
                        _tweenFinishCb();
                });
            return _tweenPos;
        }

        public Tween StartTweenScale(float scale, float duration, Action finishCb = null)
        {
            _ResetTween();
            _deltaV3.Set(scale, scale, scale);
            _tweenFinishCb = finishCb;
            _tweenScale = _rootRectTrans.DOScale(_deltaV3, duration).SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    SetModuleScale(_deltaV3);
                    if (_tweenFinishCb != null)
                        _tweenFinishCb();
                });
            return _tweenScale;
        }

        public Tween StartTweenMoveAndScale(float targetX, float targetY, float scale, float duration, Action finishCb = null)
        {
            _ResetTween();
            _deltaV2.Set(targetX, targetY);
            _deltaV3.Set(scale, scale, scale);
            _tweenFinishCb = finishCb;
            _tweenSeq = DOTween.Sequence();
            _tweenSeq.Append(_rootRectTrans.DOAnchorPos(_deltaV2, duration).SetEase(Ease.OutCubic));
            _tweenSeq.Join(_rootRectTrans.DOScale(_deltaV3, duration).SetEase(Ease.OutCubic));
            _tweenSeq.OnComplete(() =>
            {
                SetModuleAnchorPos(_deltaV2);
                SetModuleScale(_deltaV3);
                if (_tweenFinishCb != null)
                    _tweenFinishCb();
            });
            _tweenSeq.Play();
            return _tweenSeq;
        }

        public void PlayUnlockEffect()
        {
            _RefreshUnlockState(true);
        }

        public void ShowCurEffect()
        {
            _RefreshCurEffect();
        }

        #endregion
    }
}

