/*
 * @Author: qun.chao
 * @Date: 2022-04-01 11:37:13
 */
using System.Collections;
using UnityEngine;

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace FAT
{
    public static class UIBridgeUtility
    {
#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void _OpenURL(string url);
#endif

        public static void OpenURL(string url)
        {
#if UNITY_IOS
            _OpenURL(url);
#else
            Application.OpenURL(url);
#endif
        }

        public static void OpenAppStore()
        {
#if FAT_PIONEER
            return;
#endif
            OpenURL(GameUpdateManager.Instance.appUrl);
        }

        //loading过程中尝试打开界面 不依赖UIManager
        public static void OpenWindowInLoading(UIResource res, params object[] items)
        {
            Game.Instance.StartCoroutineGlobal(_LoadRes(res, items));
        }

        //这里的弹窗发生在loading期间 字体相关资源可能没准备好 需要主动加载一次
        private static IEnumerator _LoadRes(UIResource res, params object[] items)
        {
            // 加载字体材质Asset配置信息
            yield return GameProcedure.LoadingJob_CoLoadFontMatRes();
            // 根据当前语言加载对应字体Asset资源
            yield return GameI18NHelper.GetOrCreate().CoLoadFontAsset();
            // 刷新当前语言对应的配置文本
            EL.I18N.RefreshI18N();

            var task = EL.Resource.ResManager.LoadAsset(res.prefabGroup, res.prefabPath);
            yield return task;
            if (task.isSuccess && task.asset != null)
            {
                var _canvas = GameObject.Find("Root_UI/Canvas_Global").transform;
                if (_canvas != null)
                {
                    _canvas.gameObject.SetActive(true);
                    var go = GameObject.Instantiate(task.asset, _canvas) as GameObject;
                    go.transform.localScale = Vector3.one;
                    go.transform.localPosition = Vector3.zero;
                    go.SetActive(true);

                    var ui = go.GetComponent<UIBase>();
                    ui.OnLoaded();
                    ui.PreOpen(items);
                    ui.MarkAutoDestroy(true);
                    yield return null;
                    ui.PostOpen();
                }
            }
            else
            {
                //loading过程中资源加载失败 直接退游戏
                GameProcedure.QuitGame();
            }
        }

        public static void ForceUpdate()
        {
            OpenWindowInLoading(UIConfig.UIUpdate, true);
        }
    }
}