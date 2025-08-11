/*
 * @Author: tang.yan
 * @Description: 三格无限礼包钻石版版界面奖励格子module   
 * @Date: 2024-11-08 14:11:49
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

namespace FAT
{
    public class UIGemEndlessThreePkgModule
    {
        //根节点
        private Transform _moduleRoot;
        //当前要显示的数据对应的index
        private int _curIndex = -1;
        //活动数据
        private PackGemEndlessThree pack;
        //当前要显示的数据Data
        private PackGemEndlessThree.GemEndlessThreePkgData _curRewardData;
        //是否是末尾三个module
        private bool _isLast = false;
        
        //UI相关节点
        private RectTransform _rootRectTrans;    //根节点rect
        private CanvasGroup _canvasGroup;       //根节点canvas group用于tween动画控制显隐
        private List<UICommonItem> _rewardItemList = new List<UICommonItem>(); //奖励Item List
        private Button buyBtn;
        private GameObject normalGo;
        private TMP_Text normalText;
        private UIImageRes normalIcon;
        private GameObject freeGo;  //免费go
        private GameObject finishGo;//已完成go
        private Animation lockAnim; //锁
        private GameObject iapGo;   //购买按钮
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
        private float _deltaAlpha = 0;
        //tween动画结束回调
        private Action _tweenFinishCb;

        /// <summary>
        /// 绑定module根节点transform
        /// </summary>
        /// <param name="transform">根节点transform</param>
        public void BindModuleRoot(Transform transform)
        {
            if (transform != null)
                _moduleRoot = transform;
        }
        
        public void OnModuleCreate()
        {
            if (_moduleRoot == null)
                return;
            _rootRectTrans = _moduleRoot as RectTransform;
            _canvasGroup = _moduleRoot.GetComponent<CanvasGroup>();
            //奖励Item 界面最多显示4个
            for (int i = 0; i < 4; i++)
            {
                var icon = _moduleRoot.FindEx<UICommonItem>("Item/Item" + i);
                _rewardItemList.Add(icon);
            }
            buyBtn = _moduleRoot.FindEx<Button>("BuyBtn");
            _moduleRoot.FindEx("BuyBtn/Normal", out normalGo);
            normalText = _moduleRoot.FindEx<TMP_Text>("BuyBtn/Normal/Num");
            normalIcon = _moduleRoot.FindEx<UIImageRes>("BuyBtn/Normal/Icon");
            _moduleRoot.FindEx("BuyBtn/Free", out freeGo);
            _moduleRoot.FindEx("Finish", out finishGo);
            _moduleRoot.FindEx("BuyBtn/IAP", out iapGo);
            lockAnim = _moduleRoot.FindEx<Animation>("BuyBtn/eff_lock/ani");
            iapPriceState = _moduleRoot.FindEx<UITextState>("BuyBtn/IAP/Num");
            buyBtn.WithClickScale().onClick.AddListener(_OnBuyBtnClick);
        }
        
        public void OnModulePreOpen(PackGemEndlessThree _pack)
        {
            pack = _pack;
        }

        public void OnModulePostClose()
        {
            _curIndex = -1;
            _curRewardData = null;
            _ResetTween();
            _isLast = false;
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
        
        public void SetModuleAlpha(float value)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = value;
        }
        
        public void UpdateRewardDataAndUI(int newDataIndex, bool isLast, bool isRefresh = false)
        {
            if (pack == null)
                return;
            _curIndex = newDataIndex;
            _isLast = isLast;
            if (_curIndex < 0)
                return;
            _curRewardData = pack.GetRewardDataByIndex(_curIndex);
            _RefreshRewardItemList();
            _RefreshBuyBtn();
            _RefreshUnlockState(isRefresh);
        }

        //刷新奖励UI
        private void _RefreshRewardItemList()
        {
            if (_moduleRoot == null || _curIndex < 0 || _curRewardData == null)
                return;
            var packId = _curRewardData.PackId;
            bool isFree = packId == 0;
            bool isValid = packId >= 0;
            if (isFree)
            {
                var freeInfo = _curRewardData.FreeRewardInfo;
                for (int i = 0; i < _rewardItemList.Count; i++)
                {
                    if (i < freeInfo.Count)
                    {
                        _rewardItemList[i].gameObject.SetActive(true);
                        _rewardItemList[i].Refresh(freeInfo[i]);
                    }
                    else
                    {
                        _rewardItemList[i].gameObject.SetActive(false);
                    }
                }
            }
            else if (isValid)
            {
                var payInfo = _curRewardData.PayRewardInfo;
                for (int i = 0; i < _rewardItemList.Count; i++)
                {
                    if (i < payInfo.Count)
                    {
                        _rewardItemList[i].gameObject.SetActive(true);
                        _rewardItemList[i].Refresh(payInfo[i]);
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
            if (_isLast)
            {
                bool hasGet = pack.CheckHasGetReward(_curIndex);
                if (hasGet)
                {
                    buyBtn.gameObject.SetActive(false);
                    finishGo.gameObject.SetActive(true);
                    return;
                }
                buyBtn.gameObject.SetActive(true);
                finishGo.gameObject.SetActive(false);
            }
            var packId = _curRewardData.PackId;
            bool isFree = packId == 0;
            bool isValid = packId >= 0;
            if (isFree)
            {
                freeGo.SetActive(true);
                iapGo.SetActive(false);
                normalGo.SetActive(false);
            }
            else if (isValid)
            {
                freeGo.SetActive(false);
                iapGo.SetActive(false);
                normalGo.SetActive(false);
                if (!Game.Manager.iap.FindCurrencyPack(packId, out var currencyPackConf))
                    return;
                var coinType = currencyPackConf.CoinType;
                if (coinType == CoinType.Gem || coinType == CoinType.MergeCoin)  //花钻石 花金币
                {
                    normalGo.SetActive(true);
                    normalText.text = currencyPackConf.Price.ToString();
                    var conf = Game.Manager.coinMan.GetConfigByCoinType(coinType);
                    if (conf != null) 
                        normalIcon.SetImage(conf.Image);
                }
                else if (currencyPackConf.CoinType == CoinType.Iapcoin)     //花iap
                {
                    iapGo.SetActive(true);
                    var iap = Game.Manager.iap;
                    iap.FindIAPPack(currencyPackConf.Price, out var iapPackConf);
                    var valid = iap.Initialized && iapPackConf != null;
                    iapPriceState.Enabled(valid, iap.PriceInfo(iapPackConf?.IapId ?? 0));
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
            }
            //数据非法时按钮不可交互
            else
            {
                buyBtn.interactable = false;
                freeGo.SetActive(false);
                iapGo.SetActive(false);
                normalGo.SetActive(false);
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
                    lockAnim.Play("eff_lock_open");
                }
                else
                {
                    lockAnim.gameObject.SetActive(false);
                }
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
            for (var n = 0; n < list_.Count && n < _rewardItemList.Count; ++n) {
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
        
        public Tween StartTweenMoveAndAlpha(float targetX, float targetY, float alpha, float duration, Action finishCb = null)
        {
            _ResetTween();
            _deltaV2.Set(targetX, targetY);
            _deltaAlpha = alpha;
            _tweenFinishCb = finishCb;
            _tweenSeq = DOTween.Sequence();
            _tweenSeq.Append(_rootRectTrans.DOAnchorPos(_deltaV2, duration).SetEase(Ease.OutCubic));
            _tweenSeq.Join(_canvasGroup.DOFade(_deltaAlpha, duration).SetEase(Ease.OutCubic));
            _tweenSeq.OnComplete(() =>
            {
                SetModuleAnchorPos(_deltaV2);
                SetModuleAlpha(_deltaAlpha);
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

        #endregion
    }
}