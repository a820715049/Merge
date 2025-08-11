using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace FAT {
    public class MapUI {
        public abstract class UIAsset {
            public MapAsset asset;
            public abstract MapSceneUI UIRef { get; }
            public abstract void Init();
        }
        public class UIAsset<T> : UIAsset where T : MapSceneUI {
            public T ui;
            public override MapSceneUI UIRef => ui;

            public override void Init() {
                ui = asset.asset.GetComponent<T>();
                ui.Init();
            }
        }

        public Canvas canvas;
        public Graphic block;
        public Transform root;
        public Transform rootAsset;
        public Transform rootBanner;

        public MapSceneInfo info;

        private readonly List<UIAsset> assetList = new();
        public UIAsset<BuildingPurchase> buildingPurchase;
        public UIAsset<BuildingUpgrade> buildingUpgrade;
        public UIAsset<BuildingMaxed> buildingMaxed;
        public UIAsset<BuildingMystery> buildingMystery;
        public UIAsset<BuildingPlace> buildingPlace;
        private GameObject templateBanner;
        private readonly List<BannerBuilding> bannerList = new();

        public bool Ready { get; private set; }

        public void SetupCanvas(MapSceneInfo info_, Camera camera_) {
            info = info_;
            templateBanner = info_.templateBanner.obj;
            canvas = info_.canvas;
            canvas.worldCamera = camera_;
            canvas.transform.Find("block").TryGetComponent(out block);
            block.raycastTarget = false;
            root = info_.root;
            root.gameObject.SetActive(true);
            static Transform AddChild(Transform t_, string name_) {
                var rt = new GameObject(name_).AddComponent<RectTransform>();
                rt.SetParent(t_);
                rt.localScale = Vector3.one;
                rt.localPosition = Vector3.zero;
                return rt;
            }
            rootBanner = AddChild(root, "banner");
            rootAsset = AddChild(root, "asset");
            AttachAsset();
        }

        public void PrepareUI() {
            Ready = false;
            var map = new HashSet<UIAsset>();
            assetList.Clear();
            void Load(UIAsset asset_, string path_) {
                map.Add(asset_);
                var asset = asset_.asset;
                asset.WhenAssetResult = _ => AssetResult(asset_);
                asset.WhenAssetReady = _ => AssetReady(asset_);
                asset.Load(path_);
            }
            void AssetResult(UIAsset asset_) {
                map.Remove(asset_);
                assetList.Add(asset_);
            }
            void AssetReady(UIAsset asset_) {
                asset_.Init();
                if (map.Count == 0) {
                    AttachAsset();
                    Ready = true;
                }
            }
            UIAsset Create<T>(out UIAsset<T> asset_) where T : MapSceneUI {
                asset_ = new() {
                    asset = new(),
                };
                return asset_;
            }
            Load(Create(out buildingPurchase), "fat_map:building_purchase.prefab");
            Load(Create(out buildingUpgrade), "fat_map:building_upgrade.prefab");
            Load(Create(out buildingMaxed), "fat_map:building_maxed.prefab");
            Load(Create(out buildingMystery), "fat_map:building_mystery.prefab");
            Load(Create(out buildingPlace), "fat_map:building_place.prefab");
        }

        private void AttachAsset() {
            foreach(var e in assetList) {
                var obj = e.asset.asset;
                if (obj == null) continue;
                var root = obj.transform;
                obj.SetActive(false);
                AttachSceneUI(root, Vector3.zero, to_:rootAsset);
            }
        }

        public void AttachSceneUI(Transform root_, Vector3 pos_, Transform to_ = null) {
            to_ ??= root;
            root_.position = pos_;
            root_.SetParent(to_, true);
            root_.localScale = Vector3.one;
        }

        public BannerBuilding CreateBanner(MapBuildingInfo target_) {
            var obj = GameObject.Instantiate(templateBanner);
            obj.name = $"{templateBanner.name}{target_.id}";
            obj.SetActive(true);
            AttachSceneUI(obj.transform, target_.banner, to_:rootBanner);
            var banner = obj.GetComponent<BannerBuilding>();
            bannerList.Add(banner);
            banner.Init(target_);
            return banner;
        }

        public void Block(bool v_) {
            block.raycastTarget = v_;
        }

        public bool Open(UIAsset asset_, IMapBuilding b_) {
            var ui = asset_?.UIRef as BuildingUI;
            if (ui == null) return false;
            ui.Relocate(b_.info.banner);
            ui.RefreshView(b_);
            ui.Open();
            ui.Refresh(b_);
            return true;
        }

        public void Clear() {
            foreach(var e in assetList) {
                e.UIRef.Close();
            }
        }

        public void ResetClear() {
            assetList.Clear();
        }
    }
}