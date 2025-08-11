/*
 *@Author:chaoran.zhang
 *@Desc:集卡活动结束预告界面
 *@Created Time:2024.01.22 星期一 16:39
 */

using System;
using EL;
using fat.conf;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UICardActivityEndNotice : UIBase
    {
        private TextMeshProUGUI _timeLimited;
        private TextProOnACircle _title;
        private TextMeshProUGUI _desc;
        private TextMeshProUGUI _bottomDesc;
        private UIImageRes _bg;
        private UIImageRes _titleBg;

        private long _leftTime = -1;
        private float _curTime = 0f;
        private ActivityVisual _eventTheme = new ActivityVisual();

        protected override void OnCreate()
        {
            transform.AddButton("Content/Panel/BtnConfirm", Close);
            transform.AddButton("Content/Panel/BtnClose", Close);
            _timeLimited = transform.Find("Content/Panel/TxtTimeLimited").GetComponent<TextMeshProUGUI>();
            _title = transform.Find("Content/Panel/Title").GetComponent<TextProOnACircle>();
            _bg = transform.Find("Content/Panel/Bg").GetComponent<UIImageRes>();
            _titleBg = transform.Find("Content/Panel/TitleBg").GetComponent<UIImageRes>();
            _desc = transform.Find("Content/Panel/Desc1").GetComponent<TextMeshProUGUI>();
            _bottomDesc = transform.Find("Content/Panel/Desc3").GetComponent<TextMeshProUGUI>();
        }

        protected override void OnPreOpen()
        {
            var config = Game.Manager.cardMan.GetCardAlbumConfig();
            if (config == null) return;
            _title.SetText(I18N.Text(config.Name));
            
            if (_eventTheme.Setup(config.EndRemindTheme))
            {
                _eventTheme.Refresh(_bg, "bgImage");
                _eventTheme.Refresh(_titleBg, "titleImage");
                _eventTheme.Refresh(_title, "mainTitle");
                _eventTheme.Refresh(_desc, "subTitle");
                _eventTheme.Refresh(_bottomDesc, "bottomDesc");
                _eventTheme.Refresh(_timeLimited, "time");
            }
            _RefreshCD();
        }
        
        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_RefreshCD);
        }

        protected override void OnRemoveListener() {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshCD);
        }

        private void _RefreshCD()
        {
            var v = Game.Manager.cardMan.GetCardActivity()?.Countdown ?? 0;
            UIUtility.CountDownFormat(_timeLimited, v);
            if (v <= 0)
                Close();
        }
    }
}