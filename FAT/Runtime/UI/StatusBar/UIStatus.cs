/*
 * @Author: qun.chao
 * @Date: 2023-10-23 12:25:32
 */
using EL;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using fat.rawdata;
using System.Collections.Generic;
using Cysharp.Text;
using Coffee.UIExtensions;

namespace FAT
{
    public class UIStatus : UIBase
    {
        //等级
        [SerializeField] private GameObject levelGo;
        [SerializeField] private TMP_Text levelTmp;
        [SerializeField] private Image levelImg;
        [SerializeField] private Button debugBtn;
        //体力
        [SerializeField] private TMP_Text energyNumTmp;
        [SerializeField] private GameObject energyAddGo;
        [SerializeField] private Button energyAddBtn;
        [SerializeField] private GameObject energyTimeGo;
        [SerializeField] private TMP_Text energyTimeTmp;
        //金币
        [SerializeField] private TMP_Text coinNumTmp;
        //宝石
        [SerializeField] private TMP_Text gemNumTmp;
        [SerializeField] private GameObject gemAddGo;
        [SerializeField] private Button gemAddBtn;
        //商城
        [SerializeField] private GameObject shopGo;
        [SerializeField] private Button shopBtn;
        //反馈动画
        [Tooltip("0.Energy 1.Coin 2.Gem")]
        [SerializeField] private Animator[] feedbackAnimators;
        [SerializeField] private Coffee.UIExtensions.UIParticle[] feedbackEffs;
        //出入场动画
        private Animator _animator;
        
        //界面逻辑相关字段
        private int _energyChargeSec;
        
        enum ResType
        {
            Energy,
            Coin,
            Gem,
        }

        public enum LayerState
        {
            Status, //表示状态栏在UILayer.Status层级
            AboveStatus, //表示状态栏在UILayer.AboveStatus层级
            SubStatus, //表示状态栏在UILayer.SubStatus层级
            Cache,  //表示状态栏在UILayer.Cache层级 (隐藏状态栏)
        }
        private Stack<LayerState> _stateStack = new Stack<LayerState>();

        protected override void OnCreate()
        {
            debugBtn.onClick.AddListener(_OnClickDebugBtn);
            energyAddBtn.onClick.AddListener(_OnClickEnergyAddBtn);
            gemAddBtn.onClick.AddListener(_OnClickGemAddBtn);
            shopBtn.WithClickScale().FixPivot().onClick.AddListener(_OnClickShopBtn);
            _animator = transform.FindEx<Animator>("Content");
        }

        protected override void OnPreOpen()
        {
            //界面打开时根据IsHideMainUI状态播不同动画
            _animator.SetTrigger(UIManager.Instance.IsHideStatusUI ? UIManager.IdleHideAnimTrigger : UIManager.IdleShowAnimTrigger);
            _energyChargeSec = -1;
            _RefreshAllStatus();
            
            _stateStack.Clear();
            _PushState(LayerState.Status);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnOneSecondPass);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().AddListener(_OnMessageLevelChange);
            MessageCenter.Get<MSG.GAME_MERGE_EXP_CHANGE>().AddListener(_OnMessageExpChange);
            MessageCenter.Get<MSG.GAME_MERGE_ENERGY_CHANGE>().AddListener(_OnMessageEnergyChange);
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().AddListener(_OnMessageCoinChange);
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().AddListener(_SwitchShopEntryShow);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().AddListener(_SwitchLevelGoShow);
            MessageCenter.Get<MSG.UI_STATUS_ADD_BTN_CHANGE>().AddListener(_SwitchAddBtnShow);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(_OnMessageClaimRewardFeedback);
            MessageCenter.Get<MSG.UI_TOP_BAR_PUSH_STATE>().AddListener(_OnMessagePushState);
            MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().AddListener(_OnMessagePopState);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().AddListener(_OnShowStateChange);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnOneSecondPass);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().RemoveListener(_OnMessageLevelChange);
            MessageCenter.Get<MSG.GAME_MERGE_EXP_CHANGE>().RemoveListener(_OnMessageExpChange);
            MessageCenter.Get<MSG.GAME_MERGE_ENERGY_CHANGE>().RemoveListener(_OnMessageEnergyChange);
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().RemoveListener(_OnMessageCoinChange);
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().RemoveListener(_SwitchShopEntryShow);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().RemoveListener(_SwitchLevelGoShow);
            MessageCenter.Get<MSG.UI_STATUS_ADD_BTN_CHANGE>().RemoveListener(_SwitchAddBtnShow);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(_OnMessageClaimRewardFeedback);
            MessageCenter.Get<MSG.UI_TOP_BAR_PUSH_STATE>().RemoveListener(_OnMessagePushState);
            MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().RemoveListener(_OnMessagePopState);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().RemoveListener(_OnShowStateChange);
        }

        private void _RefreshAllStatus()
        {
            _RefreshLevel();
            _RefreshEnergy();
            _RefreshEnergyCharge();
            _RefreshCoin();
            _RefreshGem();
            _RefreshAddGo();
        }

        private void _RefreshLevel()
        {
            var mgr = Game.Manager.mergeLevelMan;
            int cur = mgr.exp;
            int max = cur;
            if (mgr.nextLevelConfig != null)
                max = mgr.nextLevelConfig.Exp;
            levelImg.fillAmount = (float)cur / max;
            levelTmp.SetText(mgr.displayLevel);
        }
        
        private void _RefreshEnergy()
        {
            var energy = Game.Manager.mergeEnergyMan.Energy;
            energyNumTmp.SetText(energy < 0 ? 0 : energy);
        }
        
        private void _RefreshEnergyCharge()
        {
            if (Game.Manager.mergeEnergyMan.Energy >= Game.Manager.mergeEnergyMan.RecoverMax)
            {
                energyTimeGo.gameObject.SetActive(false);
            }
            else
            {
                energyTimeGo.gameObject.SetActive(true);
            }

            int leftSec = Game.Manager.mergeEnergyMan.RecoverCD;
            if (leftSec < 0)
            {
                leftSec = 0;
            }

            if (leftSec != _energyChargeSec)
            {
                _energyChargeSec = leftSec;
                var min = leftSec / 60;
                var sec = leftSec % 60;
                const string pattern = "{0:d2}:{1:d2}";
                energyTimeTmp.SetTextFormat(pattern, min, sec);
            }
        }
        
        private void _RefreshCoin()
        {
            int coin = Game.Manager.coinMan.GetDisplayCoin(CoinType.MergeCoin);
            coinNumTmp.SetText(coin);
        }

        private void _RefreshGem()
        {
            int coin = Game.Manager.coinMan.GetDisplayCoin(CoinType.Gem);
            gemNumTmp.SetText(coin);
        }

        private void _RefreshAddGo()
        {
            var mgr = Game.Manager.shopMan;
            energyAddGo.SetActive(mgr.CheckShopTabIsUnlock(ShopTabType.Energy));
            gemAddGo.SetActive(mgr.CheckShopTabIsUnlock(ShopTabType.Gem));
            shopGo.SetActive(mgr.CheckShopIsUnlock());
        }

        private void _FeedbackEffect(ResType rt)
        {
            feedbackAnimators[(int)rt]?.SetTrigger("Punch");
            var eff = feedbackEffs[(int)rt];
            UIUtility.ManuallyEmitParticle(eff);
        }

        private void _OnClickDebugBtn()
        {
            if (GameSwitchManager.Instance.isDebugMode)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIDebugPanelProMax);
            }
        }
        
        private void _OnClickEnergyAddBtn()
        {
            Game.Manager.shopMan.TryOpenUIShop(ShopTabType.Energy);
        }
        
        private void _OnClickGemAddBtn()
        {
            Game.Manager.shopMan.TryOpenUIShop();
        }

        private void _OnClickShopBtn()
        {
            Game.Manager.shopMan.TryOpenUIShop();
        }

        private void _OnOneSecondPass()
        {
            _RefreshEnergyCharge();
        }

        private void _OnMessageExpChange(int add)
        {
            _RefreshLevel();
        }

        private void _OnMessageLevelChange(int lvl)
        {
            _RefreshLevel();
            _RefreshAddGo();
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

        private void _SwitchShopEntryShow(bool isShow)
        {
            shopGo.SetActive(isShow);
        }
        
        private void _SwitchLevelGoShow(bool isShow)
        {
            levelGo.SetActive(isShow);
        }
        
        private void _SwitchAddBtnShow(bool isShow)
        {
            energyAddGo.SetActive(isShow);
            energyAddBtn.interactable = isShow;
            gemAddGo.SetActive(isShow);
            gemAddBtn.interactable = isShow;
        }
        
        #region state

        private void _PushState(LayerState s)
        {
            if (_stateStack.Count < 1 || _stateStack.Peek() != s)
                _ChangeState(s);

            _stateStack.Push(s);
        }

        private void _ChangeState(LayerState s)
        {
            UILayer tempLayer = UILayer.Max; 
            if (s == LayerState.Status)
            {
                tempLayer = UILayer.Status;
            }
            else if (s == LayerState.AboveStatus)
            {
                tempLayer = UILayer.AboveStatus;
            }
            else if (s == LayerState.SubStatus)
            {
                tempLayer = UILayer.SubStatus;
            }
            else if (s == LayerState.Cache)
            {
                tempLayer = UILayer.Cache;
            }
            //层级不合法或不变时 return
            if (tempLayer == UILayer.Max || tempLayer == BelongLayer)
                return;
            UIManager.Instance.TryChangeUILayer(ResConfig, tempLayer);
        }

        private void _OnMessagePushState(LayerState s)
        {
            _PushState(s);
        }

        private void _OnMessagePopState()
        {
            if (_stateStack.Count > 1)
            {
                _stateStack.Pop();
                _ChangeState(_stateStack.Peek());
            }
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

        #endregion

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
    }
}