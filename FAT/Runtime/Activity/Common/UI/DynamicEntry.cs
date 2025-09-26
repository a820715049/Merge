// ===================================================
// Author: mengqc
// Date: 2025/09/23
// ===================================================

using System;
using Config;
using EL;
using UnityEngine;

namespace FAT
{
    public class DynamicEntry<A, T> : ListActivity.IEntrySetup where A : ActivityLike where T : MonoBehaviour
    {
        protected ListActivity.Entry _entry;
        protected A _activity;
        protected T _metaEntry; //meta入口自定义元素
        protected string _metaEntryPoolKey; //meta入口自定义元素对象池key
        protected Coroutine _metaEntryCoroutine;

        public DynamicEntry(ListActivity.Entry entry, A act)
        {
            _entry = entry;
            _activity = act;
            StopMetaEntryCoroutine();
            ReleaseEntry();
            _metaEntryCoroutine = Game.StartCoroutine(CreateMetaEntryElement());
        }

        public override string TextCD(long diff)
        {
            return UIUtility.CountDownFormat(diff);
        }

        public override void Clear(ListActivity.Entry entry)
        {
            StopMetaEntryCoroutine();
            ReleaseEntry();
        }

        protected virtual string GetMetaEntryAsset()
        {
            throw new NotImplementedException();
        }

        protected virtual void UpdateMetaEntry(T metaEntry)
        {
            throw new NotImplementedException();
        }

        protected virtual string GetMetaEntryPoolKey(AssetConfig asset)
        {
            var entryName = asset.Asset.Split(".");
            return $"{_activity.Type.ToString()}_{entryName[0]}_{_activity.Id}";
        }

        private void StopMetaEntryCoroutine()
        {
            if (_metaEntryCoroutine != null)
            {
                Game.Instance.StopCoroutineGlobal(_metaEntryCoroutine);
                _metaEntryCoroutine = null;
            }
        }

        private System.Collections.IEnumerator CreateMetaEntryElement()
        {
            var prefabName = GetMetaEntryAsset();
            if (string.IsNullOrEmpty(prefabName))
            {
                yield break;
            }

            var asset = prefabName.ConvertToAssetConfig();
            var poolKey = GetMetaEntryPoolKey(asset);
            if (poolKey != _metaEntryPoolKey)
            {
                ReleaseEntry();
                _metaEntryPoolKey = poolKey;
            }

            if (!GameObjectPoolManager.Instance.HasPool(_metaEntryPoolKey))
            {
                var loader = EL.Resource.ResManager.LoadAsset<GameObject>(asset.Group, asset.Asset);
                yield return loader;
                if (!loader.isSuccess)
                {
                    DebugEx.Error($"SeaRaceEntry::CreateMetaEntry ----> loading res error {loader.error}");
                    yield break;
                }

                GameObjectPoolManager.Instance.PreparePool(_metaEntryPoolKey, loader.asset as GameObject);
            }

            if (_metaEntry)
            {
                ReleaseEntry();
            }

            var metaEntryObj = GameObjectPoolManager.Instance.CreateObject(_metaEntryPoolKey);
            _metaEntry = metaEntryObj.GetComponent<T>();
            _metaEntry.transform.SetParent(_entry.icon.transform);
            _metaEntry.transform.localPosition = Vector3.zero;
            _metaEntry.transform.localScale = Vector3.one;
            _metaEntry.name = prefabName;
            _metaEntry.gameObject.SetActive(true);
            _metaEntry.transform.SetAsFirstSibling();
            UpdateMetaEntry(_metaEntry);
            _metaEntryCoroutine = null;
        }

        private void ReleaseEntry()
        {
            if (!string.IsNullOrEmpty(_metaEntryPoolKey) && _metaEntry)
            {
                _metaEntry.gameObject.SetActive(false);
                GameObjectPoolManager.Instance.ReleaseObject(_metaEntryPoolKey, _metaEntry.gameObject);
                _metaEntry = null;
            }
        }
    }
}