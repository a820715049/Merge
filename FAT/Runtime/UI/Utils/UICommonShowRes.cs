/**
 * @Author: zhangpengjian
 * @Date: 2024/9/2 17:17:01
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/9/2 17:17:01
 * Description: 用于做表现的资源栏
 */

using System.Collections;
using DG.Tweening;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UICommonShowRes : UIBase
    {
        [SerializeField] private GameObject resNode;
        //体力
        [SerializeField] private TMP_Text energyNumTmp;

        //金币
        [SerializeField] private TMP_Text coinNumTmp;

        //宝石
        [SerializeField] private TMP_Text gemNumTmp;

        //反馈动画
        [Tooltip("0.Energy 1.Coin 2.Gem")] [SerializeField]
        private Animator[] feedbackAnimators;

        [SerializeField] private Coffee.UIExtensions.UIParticle[] feedbackEffs;
        [SerializeField] private GameObject boardNode;
        //出入场动画
        private Animator _animator;
        
        private enum ResType
        {
            Energy,
            Coin,
            Gem,
            Token
        }

        protected override void OnCreate()
        {
            boardNode = transform.Find("Content/Target/boardNode").gameObject;
            _animator = transform.FindEx<Animator>("Content");
        }

        protected override void OnPreOpen()
        {
            _animator.SetTrigger(UIManager.OpenAnimTrigger);
            resNode.gameObject.SetActive(false);
            _RefreshAllStatus();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_MERGE_ENERGY_CHANGE>().AddListener(_OnMessageEnergyChange);
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().AddListener(_OnMessageCoinChange);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(_OnMessageClaimRewardFeedback);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_MERGE_ENERGY_CHANGE>().RemoveListener(_OnMessageEnergyChange);
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().RemoveListener(_OnMessageCoinChange);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(_OnMessageClaimRewardFeedback);
        }

        public void ShowGameNode()
        {
            var seq = DOTween.Sequence();
            seq.Append(boardNode.GetComponent<Image>().DOFade(1, 0.7f));
            seq.Append(boardNode.GetComponent<Image>().DOFade(0, 0.5f));
        }
        
        protected override void OnPreClose()
        {
            _animator.SetTrigger(UIManager.CloseAnimTrigger);
        }

        private void _RefreshAllStatus()
        {
            _RefreshEnergy();
            _RefreshCoin();
            _RefreshGem();
        }

        private void _RefreshEnergy()
        {
            energyNumTmp.text = Game.Manager.mergeEnergyMan.Energy.ToString();
        }

        private void _RefreshCoin()
        {
            var coin = Game.Manager.coinMan.GetDisplayCoin(CoinType.MergeCoin);
            coinNumTmp.text = coin.ToString();
        }

        private void _RefreshGem()
        {
            var coin = Game.Manager.coinMan.GetDisplayCoin(CoinType.Gem);
            gemNumTmp.text = coin.ToString();
        }

        private void _FeedbackEffect(ResType rt)
        {
            feedbackAnimators[(int)rt]?.SetTrigger("Punch");
            var eff = feedbackEffs[(int)rt];
            foreach (var p in eff.particles)
            {
                if (p.emission.burstCount > 0)
                {
                    var bst = p.emission.GetBurst(0);
                    var count = bst.count.mode == ParticleSystemCurveMode.Constant
                        ? (short)bst.count.constant
                        : (bst.minCount + bst.maxCount) / 2;
                    if (count > 0)
                    {
                        p.Emit(count);
                    }
                }
            }
        }

        private void _OnMessageEnergyChange(int e)
        {
            _RefreshEnergy();
        }

        private void _OnMessageCoinChange(CoinType ct)
        {
            if (ct == CoinType.MergeCoin)
            {
                _RefreshCoin();
            }
            else if (ct == CoinType.Gem)
            {
                _RefreshGem();
            }
        }

        private void _OnMessageClaimRewardFeedback(FlyType flyType)
        {
            switch (flyType)
            {
                case FlyType.Energy:
                    _FeedbackEffect(ResType.Energy);
                    break;
                case FlyType.Coin:
                    _FeedbackEffect(ResType.Coin);
                    break;
                case FlyType.Gem:
                    _FeedbackEffect(ResType.Gem);
                    break;
            }
        }

        public void ShowOtherRes()
        {
            resNode.SetActive(true);
            IEnumerator enumerator()
            {
                yield return new WaitForSeconds(2f);
                resNode.SetActive(false);
            }

            Game.Instance.StartCoroutineGlobal(enumerator());
        }
        
        private void _OnShowStateChange(bool isShow)
        {
            _animator.ResetTrigger(UIManager.IdleShowAnimTrigger);
            _animator.ResetTrigger(UIManager.IdleHideAnimTrigger);
            _animator.ResetTrigger(UIManager.OpenAnimTrigger);
            _animator.ResetTrigger(UIManager.CloseAnimTrigger);
            if (isShow)
            {
                _animator.SetTrigger(UIManager.OpenAnimTrigger);
            }
            else
            {
                _animator.SetTrigger(UIManager.CloseAnimTrigger);
            }
        }
    }
}