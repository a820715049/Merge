/*
 *@Author:chaoran.zhang
 *@Desc:集卡活动开始弹窗
 *@Created Time:2024.01.31 星期三 15:01:14
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using EL;
using fat.conf;
using TMPro;
using UnityEngine.UI;

namespace FAT
{
    public class UICardActivityStartNotice : UIBase
    {
        private TextProOnACircle _title;
        private TextMeshProUGUI _leftTime;
        private MBRewardLayout _rewardLayout;
        private List<RewardConfig> _rewardConfigList = new();
        private UIImageRes _bg;
        private UIImageRes _titleBg;
        private UIImageRes _timeBg;
        private TMP_Text _desc;
        private ActivityVisual _eventTheme = new();
        private UIImageRes _reward;

        protected override void OnCreate()
        {
            _title = transform.Find("Content/Panel/TitleBg/Title").GetComponent<TextProOnACircle>();
            _leftTime = transform.Find("Content/Panel/_cd/text").GetComponent<TextMeshProUGUI>();
            _rewardLayout = transform.Find("Content/Panel/_group").GetComponent<MBRewardLayout>();
            transform.AddButton("Content/Panel/BtnConfirm", _ClickConfirm);
            transform.Find("Mask").GetComponent<Button>().onClick.AddListener(Close);
            transform.AddButton("Content/Panel/BtnClose", Close);
            _bg = transform.Find("Content/Panel/Bg").GetComponent<UIImageRes>();
            _titleBg = transform.Find("Content/Panel/TitleBg").GetComponent<UIImageRes>();
            _timeBg = transform.Find("Content/Panel/_cd/frame").GetComponent<UIImageRes>();
            _desc = transform.Find("Content/Panel/Desc3").GetComponent<TMP_Text>();
            transform.Access("Content/Panel/Bg/Reward", out _reward);
            transform.AddButton("Content/Panel/Bg/Reward/Btn", _ClickReward);
        }

        protected override void OnPreOpen()
        {
            var config = Game.Manager.cardMan.GetCardAlbumConfig();
            if (config == null) return;
            _title.SetText(I18N.Text(config.Name));
            _rewardConfigList.Clear();
            foreach (var reward in config.Reward) _rewardConfigList.Add(reward.ConvertToRewardConfig());
            //_rewardLayout.Refresh(_rewardConfigList);
            if (_eventTheme.Setup(config.StartRemindTheme))
            {
                _eventTheme.Refresh(_bg, "bgImage");
                _eventTheme.Refresh(_titleBg, "titleImage");
                _eventTheme.Refresh(_timeBg, "time");
                _eventTheme.Refresh(_title, "mainTitle");
                _eventTheme.Refresh(_desc, "desc");
                _eventTheme.Refresh(_leftTime, "time");
                _eventTheme.Refresh(_reward, "reward");
            }

            _RefreshCD();
        }

        protected override void OnParse(params object[] items)
        {
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
            var v = Game.Manager.cardMan.GetCardActivity()?.Countdown ?? 0;
            UIUtility.CountDownFormat(_leftTime, v);
            if (v <= 0)
                Close();
        }

        private void _ClickConfirm()
        {
            Game.Manager.cardMan.OpenCardAlbumUI();
            Game.Manager.screenPopup.Wait(UIConfig.UICardAlbum);
            Close();
        }

        private void _ClickReward()
        {
            UIManager.Instance.OpenWindow(UIConfig.UICardFinalRewardTips, _reward.transform.position,
                140f, _rewardConfigList);
        }
    }
}