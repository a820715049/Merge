/*
 * @Author: tang.yan
 * @Description: 集卡重玩确认界面
 * @Date: 2024-05-14 18:05:32
 */

using System;
using EL;
using fat.conf;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UICardAlbumRestart : UIBase
    {
        private TextProOnACircle _title;
        private UIImageRes _bg;
        private UIImageRes _titleBg;
        private GameObject _normalGo;
        private TMP_Text _normalDesc1;
        private TMP_Text _normalDesc2;
        private GameObject _transStarGo;
        private TMP_Text _transStarDesc1;
        private TMP_Text _transStarDesc2;
        private TMP_Text _transStarDesc3;
        private TMP_Text _transStarNum;
        private ActivityVisual _eventTheme = new();
        private int _starCollect;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Panel/BtnConfirm", _ConfirmRestart);
            _title = transform.Find("Content/Panel/TitleBg/Title").GetComponent<TextProOnACircle>();
            _bg = transform.Find("Content/Panel/Bg").GetComponent<UIImageRes>();
            _titleBg = transform.Find("Content/Panel/TitleBg").GetComponent<UIImageRes>();
            transform.FindEx("Content/Panel/DescNormal", out _normalGo);
            _normalDesc1 = transform.Find("Content/Panel/DescNormal/Desc1").GetComponent<TMP_Text>();
            _normalDesc2 = transform.Find("Content/Panel/DescNormal/Desc2").GetComponent<TMP_Text>();
            transform.FindEx("Content/Panel/DescTransStar", out _transStarGo);
            _transStarDesc1 = transform.FindEx<TMP_Text>("Content/Panel/DescTransStar/Desc1");
            _transStarDesc2 = transform.FindEx<TMP_Text>("Content/Panel/DescTransStar/Desc2");
            _transStarDesc3 = transform.FindEx<TMP_Text>("Content/Panel/DescTransStar/StarInfo/Desc3");
            _transStarNum = transform.FindEx<TMP_Text>("Content/Panel/DescTransStar/StarInfo/Star/Num");
        }

        protected override void OnPreOpen()
        {
            var config = Game.Manager.cardMan.GetCardAlbumConfig();
            if (config == null) return;
            _title.SetText(I18N.Text(config.Name));
            var isOpenExchange = Game.Manager.cardMan.IsOpenStarExchange();
            _normalGo.SetActive(!isOpenExchange);
            _transStarGo.SetActive(isOpenExchange);
            _starCollect = 0;
            if (isOpenExchange)
            {
                _starCollect = Game.Manager.cardMan.GetTotalVirtualStarNum();
                _transStarNum.text = _starCollect.ToString();
            }

            if (_eventTheme.Setup(config.RestartTheme))
            {
                _eventTheme.Refresh(_bg, "bgImage");
                _eventTheme.Refresh(_titleBg, "titleImage");
                _eventTheme.Refresh(!isOpenExchange ? _normalDesc1 : _transStarDesc1, "desc1");
                _eventTheme.Refresh(!isOpenExchange ? _normalDesc2 : _transStarDesc2, "desc2");
                _eventTheme.Refresh(_title, "mainTitle");
                if (isOpenExchange)
                    _eventTheme.RefreshStyle(_transStarDesc3, "desc1");
            }
        }

        private void _ConfirmRestart()
        {
            if (_starCollect > 0 && Game.Manager.cardMan.IsOpenStarExchange())
            {
                UIManager.Instance.OpenWindow(UIConfig.UICardExchangeStarCollect,
                    _transStarNum.transform.parent.position,
                    _starCollect, (Action)_ExchangeCallBack);
            }
            else
            {
                _ExchangeCallBack();
            }
        }

        private void _ExchangeCallBack()
        {
            Close();
            Game.Manager.cardMan.TryRestartAlbum();
            Game.Manager.screenPopup.Wait(UIConfig.UICardAlbum);
        }
    }
}