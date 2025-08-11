/*
 * @Author: qun.chao
 * @Date: 2025-03-25 17:18:31
 */
using System;
using UnityEngine;
using EL;

namespace FAT
{
    public class MBBoardOrderAttachment
    {
        public bool HasLoaded => objLoaded != null;
        public GameObject AttachedObject => objLoaded;

        private Transform attachRoot;
        private string poolKey { get; set; }
        private string resLoadRequestId;
        private GameObject objLoaded;

        public void Clear()
        {
            if (objLoaded != null)
            {
                GameObjectPoolManager.Instance.ReleaseObject(poolKey, objLoaded);
                objLoaded = null;
            }
            poolKey = null;
            resLoadRequestId = null;
        }

        private void OnPrefabLoaded(GameObject obj, string curResId, Action<GameObject> act)
        {
            // 避免重复触发时加载到旧的资源
            if (!string.Equals(resLoadRequestId, curResId))
            {
                DebugEx.Warning($"order attachment dup [{resLoadRequestId}] [{curResId}]");
                GameObjectPoolManager.Instance.ReleaseObject(curResId, obj);
                return;
            }
            obj.transform.localPosition = Vector3.zero;
            obj.SetActive(true);
            poolKey = curResId;
            resLoadRequestId = null;
            objLoaded = obj;
            act?.Invoke(obj);
        }

        public void RefreshAttachment(string res, Transform root, Action<GameObject> act)
        {
            if (string.Equals(poolKey, res))
            {
                // 资源一致 无需重新加载
                act?.Invoke(objLoaded);
                return;
            }
            else
            {
                attachRoot = root;
                // 尝试加载资源并且避免重复加载
                if (!string.Equals(resLoadRequestId, res))
                {
                    Clear();
                    resLoadRequestId = res;
                    GameObjectPoolManager.Instance.CreateObject(res, attachRoot, (obj) => OnPrefabLoaded(obj, res, act));
                }
            }
        }
    }
}