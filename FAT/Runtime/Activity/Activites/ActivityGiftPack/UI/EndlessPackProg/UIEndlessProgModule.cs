/*
 * @Author: tang.yan
 * @Description: 六格无限礼包进度条版界面奖励格子module 
 * @Date: 2025-02-19 16:02:21
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
using TMPro;
using static fat.conf.Data;

namespace FAT
{
    public class UIEndlessProgModule
    {
        //根节点
        private Transform _moduleRoot;
        //当前要显示的数据对应的index
        private int _curIndex = -1;
        //活动数据
        private PackEndless pack;
        //当前要显示的数据Data
        private PackEndless.EndlessPkgData _curRewardData;
        
        //UI相关节点
        private RectTransform _rootRectTrans;    //根节点rect
        private GameObject _bgFreeGo;   //免费奖励背景图
        private GameObject _bgPayGo;    //付费奖励背景图
        private GameObject _curEffectGo;//付费奖励背景图
        private List<UICommonItem> _rewardItemList = new List<UICommonItem>(); //奖励Item List
        private Button buyBtn;
        private GameObject freeGo;
        private Animation lockAnim;
        private GameObject iapGo;
        private UITextState iapPriceState;
        //normal
        private GameObject normalBtnGo;
        private Button buyBtn1;
        private GameObject freeGo1;
        private Animation lockAnim1;
        private GameObject iapGo1;
        private UITextState iapPriceState1;
        //token
        private GameObject tokenBtnGo;
        private Button buyBtn2;
        private GameObject freeGo2;
        private Animation lockAnim2;
        private GameObject iapGo2;
        private UITextState iapPriceState2;
        private GameObject tokenGo;
        private UIImageRes tokenIcon;
        private TMP_Text tokenNum;
        
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
            //奖励Item 界面最多显示2个
            for (int i = 0; i < 2; i++)
            {
                var icon = _moduleRoot.FindEx<UICommonItem>("Item/Item" + i);
                _rewardItemList.Add(icon);
            }
            //Normal
            _moduleRoot.FindEx("BuyBtnNormal", out normalBtnGo);
            buyBtn1 = _moduleRoot.FindEx<Button>("BuyBtnNormal");
            _moduleRoot.FindEx("BuyBtnNormal/Free", out freeGo1);
            _moduleRoot.FindEx("BuyBtnNormal/IAP", out iapGo1);
            lockAnim1 = _moduleRoot.FindEx<Animation>("BuyBtnNormal/eff_lock_pack/ani");
            iapPriceState1 = _moduleRoot.FindEx<UITextState>("BuyBtnNormal/IAP/Num");
            buyBtn1.WithClickScale().FixPivot().onClick.AddListener(_OnBuyBtnClick);
            //Token
            _moduleRoot.FindEx("BuyBtnToken", out tokenBtnGo);
            buyBtn2 = _moduleRoot.FindEx<Button>("BuyBtnToken/BuyBtn");
            _moduleRoot.FindEx("BuyBtnToken/BuyBtn/Free", out freeGo2);
            _moduleRoot.FindEx("BuyBtnToken/BuyBtn/IAP", out iapGo2);
            lockAnim2 = _moduleRoot.FindEx<Animation>("BuyBtnToken/BuyBtn/eff_lock_pack/ani");
            iapPriceState2 = _moduleRoot.FindEx<UITextState>("BuyBtnToken/BuyBtn/IAP/Num");
            buyBtn2.WithClickScale().FixPivot().onClick.AddListener(_OnBuyBtnClick);
            _moduleRoot.FindEx("BuyBtnToken/Token", out tokenGo);
            tokenIcon = _moduleRoot.FindEx<UIImageRes>("BuyBtnToken/Token/icon");
            tokenNum = _moduleRoot.FindEx<TMP_Text>("BuyBtnToken/Token/count");
            //字体样式
            _freeFontStyle = freeFontStyle;
            _payFontStyle = payFontStyle;
        }
        
        public void OnModulePreOpen(PackEndless _pack)
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
            _RefreshToken();
            _RefreshRewardItemList();
            _RefreshBuyBtn();
            _RefreshUnlockState();
            _RefreshCurEffect();
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
        
        //刷新token奖励信息
        private void _RefreshToken()
        {
            if (_moduleRoot == null || _curIndex < 0 || _curRewardData == null)
                return;
            var count = _curRewardData.TokenRewardNum;
            normalBtnGo.SetActive(count <= 0);
            tokenBtnGo.SetActive(count > 0);
            buyBtn = count <= 0 ? buyBtn1 : buyBtn2;
            freeGo = count <= 0 ? freeGo1 : freeGo2;
            lockAnim = count <= 0 ? lockAnim1 : lockAnim2;
            iapGo = count <= 0 ? iapGo1 : iapGo2;
            iapPriceState = count <= 0 ? iapPriceState1 : iapPriceState2;
            if (count > 0)
            {
                tokenNum.text = count.ToString();
                var tokenConf = Game.Manager.objectMan.GetBasicConfig(pack?.GetCurTokenId() ?? 0);
                tokenIcon.SetImage(tokenConf?.Icon.ConvertToAssetConfig());
            }
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
            for (var n = 0; n < list_.Count; ++n) {
                var pos = _rewardItemList[n].transform.position;
                UIFlyUtility.FlyReward(list_[n], pos);
            }
        }
        
        //token飞图标时机单独调用
        public void TryFlyTokenReward(int targetIndex, RewardCommitData tokenReward, Action finishCb)
        {
            if (targetIndex != _curIndex || tokenReward == null)
                return;
            UIFlyUtility.FlyReward(tokenReward, tokenGo.transform.position, finishCb);
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