/*
 * @Author: qun.chao
 * @Date: 2023-10-11 20:22:55
 */
using System.Collections.Generic;
using UnityEngine;
using EL;
using System;

namespace FAT
{
    public class MBGlobalLoading : MonoSingleton<MBGlobalLoading>, IGameLoading
    {
        [SerializeField] private MBGlobalLoadingBg mLoadingBg;
        [SerializeField] private uTools.PlayTween mBgLoaderTween;
        [SerializeField] private MBGlobalLoadingImp mLoadingImp;

        public MBGlobalLoadingBg loadingBg => mLoadingBg;
        private GameObject loadingImp => mLoadingImp.gameObject;
        private UICommonProgressBar progressBar => mLoadingImp.progressBar;
        private TMPro.TextMeshProUGUI textFpid => mLoadingImp.textFpid;
        private UnityEngine.UI.Button btnContact => mLoadingImp.btnContact;
        private Transform contactRoot => mLoadingImp.contactRoot;
        private long progressMax = 1000;
        private Func<float> progressReporter;
        private float progressFrom;
        private float progressTo;
        // 记录目标进度 用于避免进度倒退
        private long targetProgress;

        private void Awake()
        {
            btnContact.onClick.AddListener(_OnBtnContact);
        }

        private void Update()
        {
            if (progressReporter != null)
            {
                RefreshProgress(progressFrom + progressReporter.Invoke() * (progressTo - progressFrom));
            }
        }

        public void WhenBgReady()
        {
            mBgLoaderTween.Play();
        }

        public void InstallLoadingImp()
        {
            _SetupSafeArea();
            MessageCenter.Get<MSG.GAME_ACCOUNT_CHANGED>().AddListenerUnique(_OnMessageGameAccountChanged);
        }

        private void _RefreshFpid()
        {
            var fpid = Platform.PlatformSDK.Instance?.GetUserId();
            if (string.IsNullOrEmpty(fpid))
            {
                textFpid.gameObject.SetActive(false);
            }
            else
            {
                textFpid.text = fpid;
                textFpid.gameObject.SetActive(true);
            }
        }

        private void _OnBtnContact()
        {
            if (!Platform.PlatformSDK.Instance.SDKInit) return;
            Platform.PlatformSDK.Instance.ShowCustomService(true);
        }

        private void _OnMessageGameAccountChanged()
        {
            _RefreshFpid();
        }

        private void RefreshProgress(float p)
        {
            var progress = (long)(p * progressMax);
            if (progress > targetProgress)
            {
                targetProgress = progress;
                progressBar.SetProgress(targetProgress);
            }
        }

        #region imp

        public void ShowDefault()
        {
            targetProgress = 0;
            _RefreshFpid();
            loadingBg.Show = true;
            loadingImp.SetActive(true);
            progressBar.SetFormatter(UICommonProgressBar.FormatterPercentFloorToInt);
            progressBar.ForceSetup(0, progressMax, 0);
        }

        bool IGameLoading.HasFadeIn()
        {
            return loadingImp.activeSelf;
        }

        bool IGameLoading.HasFadeOut()
        {
            return !loadingImp.activeSelf;
        }

        void IGameLoading.OnPostFadeIn()
        { }

        void IGameLoading.OnPostFadeOut()
        { }

        void IGameLoading.OnPreFadeIn()
        {
            ShowDefault();
        }

        void IGameLoading.OnPreFadeOut()
        {
            loadingBg.Show = false;
            loadingImp.SetActive(false);
        }

        void IGameLoading.SetProgress(float p)
        {
            progressReporter = null;
            RefreshProgress(p);
        }

        void IGameLoading.SetProgress(float from, float to, Func<float> reporter)
        {
            progressReporter = reporter;
            progressFrom = from;
            progressTo = to;
        }

        #endregion
        
        /// <summary>
        /// loading界面客服按钮 安全区适配
        /// </summary>
        private void _SetupSafeArea()
        {
            var width = Screen.width;
            var height = Screen.height;
            var curSafeArea = Screen.safeArea;
            var anchorMin = curSafeArea.position;
            var anchorMax = curSafeArea.position + curSafeArea.size;
            anchorMin.x /= width;
            anchorMin.y /= height;
            anchorMax.x /= width;
            anchorMax.y /= height;
            if (anchorMin.x >= 0 && anchorMin.y >= 0 && anchorMax.x >= 0 && anchorMax.y >= 0)
            {
                var content = contactRoot as RectTransform;
                if (content != null)
                {
                    content.anchorMin = anchorMin;
                    content.anchorMax = anchorMax;
                }
            }
        }
    }
}