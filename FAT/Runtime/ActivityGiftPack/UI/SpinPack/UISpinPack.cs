using System;
using System.Collections.Generic;
using EL;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UISpinPack : UIBase
    {
        public GameObject info;
        public TextMeshProUGUI title;
        public TextMeshProUGUI cd;
        public SkeletonGraphic skeleton;
        public MBSpinLayout layout;
        public GameObject effect;
        public MBSpinBuyButton button;
        private PackSpin _pack;


        protected override void OnCreate()
        {
            transform.AddButton("Content/Bg/CloseBtn", Close);
            transform.AddButton("Content/Bg/ConfirmBtn", _ClickBuy);
            transform.AddButton("Content/Bg/InfoBtn", () => UIManager.Instance.OpenWindow(UIConfig.UISpinPackTip, info.transform.position, 0f, _pack));
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshCD);
        }

        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(cd, _pack.Countdown);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) { return; }
            _pack = items[0] as PackSpin;
            _RefreshCD();
            info.SetActive(!_pack.HasFreeCount);
            button.Refresh(_pack);
            _pack.Visual.Refresh(title, "title");
        }

        protected override void OnPreOpen()
        {
            skeleton.AnimationState.SetAnimation(0, "idle", true);
            if (_pack == null) { return; }
            layout.RefreshLayout(_pack);
        }

        private void _ClickBuy()
        {
            _pack.TryBuyPack(_PlayRewardAnim);
        }

        private void _PlayRewardAnim()
        {
            UIManager.Instance.Block(true);
            VibrationManager.VibrateLight();
            Game.Manager.audioMan.TriggerSound("SpinPackSpinning");
            effect.SetActive(true);
            skeleton.AnimationState.SetAnimation(0, "idletodj", false).Complete += delegate (TrackEntry entry)
            {
                skeleton.AnimationState.SetAnimation(0, "dj", true);
            };
            layout.PlayRewardAnim(_EndRewardAnim);
        }

        private void _EndRewardAnim()
        {
            Game.Manager.audioMan.TriggerSound("SpinPackWin");
            VibrationManager.VibrateHeavy();
            skeleton.AnimationState.SetAnimation(0, "djtoidle", false).Complete += delegate (TrackEntry entry)
            {
                skeleton.AnimationState.SetAnimation(0, "idle", true);
                button.Refresh(_pack);
                info.SetActive(!_pack.HasFreeCount);
                UIManager.Instance.Block(false);
                effect.SetActive(false);
                Action action = delegate ()
                {
                    layout.PlayEndAnim();
                    _pack.rewardWaitCommit.Clear();
                    _pack.extraWaitCommit.Clear();
                };
                UIManager.Instance.OpenWindow(UIConfig.UISpinRewardPanel, action, _pack.rewardWaitCommit, _pack.extraWaitCommit);
            };
        }
    }
}