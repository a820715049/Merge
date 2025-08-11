/*
 * @Author: qun.chao
 * @Date: 2021-04-08 14:48:03
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using Config;
using fat.rawdata;

namespace FAT
{
    public class MBFeatureEntryCtrl : MonoBehaviour
    {
        [SerializeField]private AssetConfig[] preloadPrefabs;
        public FeatureEntry entryType;
        public bool useZeroSacleAsDisable;
        private EL.Resource.ResourceAsyncTask[] preloadPrefabsTask;


        private void Awake()
        {
            _Refresh();
            if(preloadPrefabs != null && preloadPrefabs.Length > 0)
            {
                EL.Resource.ResManager.onLoadAsset += _OnResAssetLoad;
            }
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().AddListener(_OnMessageFeatureStatusChange);
        }

        private void OnDestroy()
        {
            EL.Resource.ResManager.onLoadAsset -= _OnResAssetLoad;
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().RemoveListener(_OnMessageFeatureStatusChange);
        }

        private void _OnResAssetLoad(string group, string asset, System.Type type, Object obj)
        {
            var target = preloadPrefabs?.FindEx((e) => e.Group == group && e.Asset == asset);
            if(target != null)
            {
                _Refresh();
            }
        }

        private void _Refresh()
        {
            var shouldShow = Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(entryType) && _CheckResourceReady();
            if (useZeroSacleAsDisable)
            {
                if (shouldShow)
                {
                    transform.localScale = Vector3.one;
                    // Component.Destroy(this);
                    if (gameObject.activeSelf)
                    {
                        LayoutRebuilder.MarkLayoutForRebuild(transform.parent as RectTransform);
                    }
                }
                else
                {
                    transform.localScale = Vector3.zero;
                    if (gameObject.activeSelf)
                    {
                        LayoutRebuilder.MarkLayoutForRebuild(transform.parent as RectTransform);
                    }
                }
            }
            else
            {
                if (shouldShow)
                {
                    gameObject.SetActive(true);
                    // Component.Destroy(this);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private bool _CheckResourceReady()
        {
            if(preloadPrefabs == null || preloadPrefabs.Length == 0)
            {
                return true;
            }

            if(preloadPrefabsTask == null || preloadPrefabsTask.Length != preloadPrefabs.Length)
            {
                preloadPrefabsTask = new EL.Resource.ResourceAsyncTask[preloadPrefabs.Length];
            }

            for(int i = 0; i < preloadPrefabsTask.Length; i++)
            {
                if(preloadPrefabsTask[i] == null || preloadPrefabsTask[i].isFailOrCancel)
                {
                    preloadPrefabsTask[i] = EL.Resource.ResManager.LoadAssetByType(preloadPrefabs[i].Group, preloadPrefabs[i].Asset, typeof(GameObject));
                }
            }
            bool ready = true;
            foreach(var t in preloadPrefabsTask)
            {
                if(!t.isSuccess)
                {
                    ready = false;
                    break;
                }
            }
            return ready;
        }

        // private void _OnMessageOrderChange(IEnumerable<int> orders)
        // {
        //     _Refresh();
        // }

        // private void _OnMessageLevelChange(int lvl)
        // {
        //     _Refresh();
        // }

        private void _OnMessageFeatureStatusChange()
        {
            _Refresh();
        }
    }
}