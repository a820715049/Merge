using System.Collections;
using System;
using UnityEngine;
using EL.Resource;
using Config;
using Spine.Unity;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace FAT {
    [Serializable]
    public struct AssetInfo {
        #if UNITY_EDITOR
        public string name;
        #endif
        public string bundle;
        public string asset;
        public readonly bool Ready => obj != null;
        public GameObject wrap;
        public GameObject obj;
    }

    [Serializable]
    public struct AssetGroup {
        public Transform root;
        public List<AssetInfo> list;
    }

    public class MapAsset {
        public (string, string) assetId;
        public GameObject asset;
        public MapAnimation anim = new();
        public SortingGroup sort;
        public Action<bool> WhenAssetResult;
        public Action<MapAsset> WhenAssetReady;
        private ResourceAsyncTask task;
        private bool cancel;
        public bool Busy => task != null;
        public bool Valid => asset != null;
        public bool Active => Valid && asset.activeSelf;
        public bool Visible => Valid && asset.activeInHierarchy;
        public bool useRaw;

        public void Load(string conf) => Load(conf?.ConvertToAssetConfig());
        public void Load(AssetConfig conf) => Load(conf?.Group, conf?.Asset);
        public void Load(string group_, string asset_) {
            var wait = WaitLoad(group_, asset_);
            if (wait != null) Game.Instance.StartCoroutineGlobal(wait);
        }
        public IEnumerator WaitLoad(string group_, string asset_) {
            if (task != null || string.IsNullOrEmpty(asset_)) return null;
            if (Valid) {
                if (assetId == (group_, asset_)) return null;
                else Unload();
            }
            assetId = (group_, asset_);
            return WaitTask(group_, asset_);
        }
        public void Load(AssetInfo info_) {
            var wait = WaitLoadInfo(info_);
            if (wait != null) Game.Instance.StartCoroutineGlobal(wait);
        }
        public IEnumerator WaitLoadInfo(AssetInfo info_) {
            var wait = WaitLoad(info_.bundle, info_.asset);
            if (wait == null) yield break;
            yield return wait;
            SetupBy(info_);
        }

        private IEnumerator WaitTask(string group_, string asset_) {
            cancel = false;
            task = ResManager.LoadAsset<GameObject>(group_, asset_);
            while(task.keepWaiting) {
                if (cancel) break;
                yield return null;
            }
            WhenAssetResult?.Invoke(task.isSuccess);
            if (cancel || assetId != (group_, asset_)) yield break;
            if (task.isSuccess) {
                asset = (GameObject)task.asset;
                if(!useRaw) asset = GameObject.Instantiate(asset);
                anim.Setup(asset);
                asset.TryGetComponent(out sort);
                WhenAssetReady?.Invoke(this);
            }
            task = null;
        }

        public void Unload() {
            if (task != null) {
                cancel = true;
            }
            if (asset == null) return;
            GameObject.Destroy(asset);
            Clear();
        }

        internal void Clear() {
            asset = null;
            anim.Clear();
            sort = null;
            assetId = default;
        }

        public GameObject Instantiate() => GameObject.Instantiate(asset);

        public void Take(MapAsset other_) {
            if (assetId != other_.assetId) {
                Unload();
                assetId = other_.assetId;
                asset = other_.asset;
                anim = other_.anim;
                sort = other_.sort;
                other_.Clear();
            }
            other_.Unload();
        }

        public void SetActive(bool v_) {
            asset?.SetActive(v_);
        }

        public float PlayAny(string name_, bool loop_) {
            return anim.Play(name_, loop_);
        }

        public void Preview(bool v_) {
            anim.Preview(v_);
        }

        //暂停或恢复当前所有的spine动画
        public void PauseSpineAnim(bool isPause)
        {
            anim.PauseSpineAnim(isPause);
        }

        public void SetupBy(AssetInfo info_) {
            if (!Valid || info_.wrap == null) {
                SetActive(false);
                return;
            }
            var t = asset.transform;
            t.SetParent(info_.wrap.transform);
            t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            t.localScale = Vector3.one;
        }

        public void Sorting(int v_) {
            if (sort == null) return;
            sort.sortingOrder = v_;
        }
    }

    public class MapAssetGroup {
        public readonly List<MapAsset> list = new();
        public Action<bool> WhenAssetResult;
        public Action<MapAsset> WhenAssetReady;
        
        public void Load(AssetGroup group_) {
            Game.Instance.StartCoroutineGlobal(WaitLoad(group_));
        }

        public IEnumerator WaitLoad(AssetGroup group_) {
            Clear();
            var gList = group_.list;
            foreach(var a in gList) {
                var asset = new MapAsset() {
                    WhenAssetResult = WhenAssetResult,
                    WhenAssetReady = WhenAssetReady,
                };
                list.Add(asset);
                yield return asset.WaitLoad(a.bundle, a.asset);
                if (!asset.Valid) continue;
                asset.SetupBy(a);
            }
        }

        public void Clear() {
            foreach(var a in list) {
                a.Unload();
            }
            list.Clear();
        }
    }
}