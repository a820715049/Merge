/*
 * @Author: qun.chao
 * @Date: 2023-10-12 18:47:25
 */
using System.Collections;
using System.Collections.Generic;
using EL;
using FAT.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIGameStart : UIBase
    {
        [SerializeField] private Button btnStart;
        public GameObject groupLogin;
        public MapButton guest;
        public MapButton facebook;
        public MapButton google;
        public MapButton apple;

        private bool mShouldAutoLogin;
        [SerializeField] private List<GameObject> logoList;

#if UNITY_EDITOR

        private void OnValidate() {
            if (Application.isPlaying) return;
            btnStart = transform.FindEx<Button>("Start");
            var group = transform.Find("LoginSelect/group");
            groupLogin = group.gameObject;
            guest = group.FindEx<MapButton>("BtnGuest");
            facebook = group.FindEx<MapButton>("BtnFacebook");
            google = group.FindEx<MapButton>("BtnGoogle");
            apple = group.FindEx<MapButton>("BtnApple");
        }

#endif

        protected override void OnCreate()
        {
            btnStart.onClick.AddListener(_OnBtnStart);
            guest.WhenClick = () => Login(AccountLoginType.Guest);
            facebook.WhenClick = () => Login(AccountLoginType.Facebook);
            google.WhenClick = () => Login(AccountLoginType.Google);
            apple.WhenClick = () => Login(AccountLoginType.Apple);
        }

        protected override void OnPreOpen()
        {
            // EL.MessageCenter.Get<MSG.UI_START_MENU_READY>().AddListener(_OnMessageStartMenuReady);
            _RefreshLanguageLogo();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
                mShouldAutoLogin = true;
        }

        protected override void OnPostClose()
        {
            // EL.MessageCenter.Get<MSG.UI_START_MENU_READY>().RemoveListener(_OnMessageStartMenuReady);
        }

        protected override void OnPostOpen()
        {
            groupLogin.SetActive(!mShouldAutoLogin);
        }

        private void _OnBtnStart()
        {
            Login(AccountLoginType.Guest);
        }

        // TODO: sdk login / enter game
        private void _StartGame()
        {
            // GameProcedure.EnterGame();
        }

        private void Login(AccountLoginType type_) {
            IEnumerator Routine() {
                UIManager.Instance.Block(true);
                var sdk = PlatformSDK.Instance.Adapter;
                var wait = sdk.Login(type_, null);
                yield return wait;
                UIManager.Instance.Block(false);
                if (sdk.SDKLogin) {
                    _StartGame();
                }
                //TODO login fail
            }
            StartCoroutine(Routine());
        }

        private void _OnMessageStartMenuReady()
        {
            if (mShouldAutoLogin)
                _StartGame();
        }
        
        private void _RefreshLanguageLogo()
        {
            if (logoList == null || logoList.Count < 1)
                return;
            var curLanguage = I18N.GetLanguage();
            var showIndex = curLanguage switch
            {
                "en" => 0,
                "ja" => 1,
                "ko" => 2,
                _ => 0
            };
            for (var i = 0; i < logoList.Count; i++)
            {
                logoList[i].SetActive(i == showIndex);
            }
        }
    }
}