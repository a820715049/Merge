/*
 * @Author: tang.yan
 * @Description: 强更弹窗
 * @Date: 2023-12-23 19:12:29
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

namespace FAT
{
    public class UINewVersion : UIBase
    {
        [SerializeField] private TMP_Text textVersion;
        [SerializeField] private TMP_Text textDetail;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnAppUrl;
        [SerializeField] private GameObject goBlock;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(_OnBtnClose);
            btnAppUrl.onClick.AddListener(_OnBtnOpenDownloadUrl);
            // clear
            textDetail.text = string.Empty;
            textVersion.text = string.Empty;
        }

        protected override void OnPreOpen()
        {
            var useKey = true;
            if (useKey)
                _ShowInfoWithKey();
            else
                _ShowOnlineVersionInfo();

            //重置位置
            var trans = (textDetail.transform.parent as RectTransform);
            trans.anchoredPosition = Vector2.zero;
        }

        private void _ShowOnlineVersionInfo()
        {
            _TryResolveVersionInfo();
        }

        private void _ShowInfoWithKey()
        {
            textVersion.text = EL.I18N.Text("#SysComDesc619");
            textDetail.text = EL.I18N.Text("#SysComDesc620");
        }

        private void _OnBtnClose()
        {
            base.Close();
            UIBridgeUtility.OpenAppStore();
            // GameProcedure.QuitGame();
            CommonUtility.QuitApp();
        }

        private void _OnBtnOpenDownloadUrl()
        {
            UIBridgeUtility.OpenAppStore();
            // GameProcedure.QuitGame();
            CommonUtility.QuitApp();
        }

        #region download
        
        private void _TryResolveVersionInfo()
        {
            goBlock.SetActive(true);
            StartCoroutine(_CoGetUpdateInfo((suc, title, desc, packageUrl, md5) =>
            {
                goBlock.SetActive(false);
                if (suc)
                {
                    textVersion.text = title;
                    desc += "\n\n\n"; //最后加点换行 避免显示不全
                    textDetail.text = desc;
                }
            }));
        }

        private string _GetCdnSuffix()
        {
            var tm = Game.Instance.GetTimestampSeconds() / 60;
            return $"?t={tm}&v={tm}";
        }

        private IEnumerator _CoGetUpdateInfo(Action<bool, string, string, string, string> cb)
        {
            var mgr = GameUpdateManager.Instance;
            bool _ret = true;
            string _packageUrl = string.Empty;
            string _md5 = string.Empty;
            string _desc = string.Empty;
            string _title = string.Empty;
            EL.SimpleJSON.JSONNode _root = null;

            var r1 = StartCoroutine(_CoGetWebResult(mgr.jsonUrl + _GetCdnSuffix(), (suc, json) =>
            {
                _ret = _ret && suc;
                if (suc)
                {
                    Debug.Log($"[Force Update Download] json = {json}");
                    try
                    {
                        _root = EL.SimpleJSON.JSON.Parse(json);
                        _title = _root["title"];
                        _desc = _root["content"];
                        // _packageUrl = _root["url_package"];
                    }
                    catch (Exception e)
                    {
                        _ret = false;
                        Debug.LogError($"[Force Update Download] parse json error : {e.Message}");
                    }
                }
            }));
            yield return r1;
            cb?.Invoke(_ret, _title, _desc, _packageUrl, _md5);
        }

        private IEnumerator _CoGetWebResult(string url, Action<bool, string> cb)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                Debug.Log($"[Force Update Download] _CoGetWebResult url {url}");
                yield return request.SendWebRequest();
                Debug.Log($"[Force Update Download] _CoGetWebResult done {request.isDone} {request.error}");
                if (request.isHttpError)
                {
                    Debug.Log($"[Force Update Download] _CoGetWebResult isHttpError true: {request.responseCode}");
                    cb?.Invoke(false, null);
                }
                else if (!string.IsNullOrEmpty(request.error))
                {
                    Debug.Log($"[Force Update Download] _CoGetWebResult isNetworkError true: {request.error}");
                    cb?.Invoke(false, null);
                }
                else
                {
                    cb?.Invoke(true, request.downloadHandler.text);
                }
            }
        }

        #endregion
    }
}