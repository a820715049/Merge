using System.Collections.Generic;
using UnityEngine;
using fat.rawdata;
using static fat.conf.Data;
using UnityEngine.Rendering;

namespace FAT {
    public class MapBuildingForEvent : IMapBuilding {
        public EventDecorateInfo confBuilding;
        public CostRecord cost = new();
        public float CostPercent { get; private set; }
        public int CostDone { get; private set; }
        public bool CostReady { get; private set; }

        public bool Valid => confBuilding != null;
        public int Id => info.id;
        public string Name => confBuilding.Name;
        public int Level { get; private set;}
        public int Phase { get; private set;}
        public bool Unlocked => Level > 0;
        public bool Placed => Phase > 0;
        public Bounds bounds;
        public int sortingOrder;

        public MapAsset asset;
        public MapAsset assetUpgrade;
        public MapAsset cloud;

        public MapBuildingForEvent() {
            asset = new() { WhenAssetReady = AssetReady };
            assetUpgrade = new() { WhenAssetReady = UpgradeReady };
            cloud = new() { WhenAssetReady = CloudReady };
        }

        public static bool ReadyToCreate(int id_, out EventDecorateInfo conf_) {
            foreach(var (_, c) in GetEventDecorateInfoMap()) {
                if (c.BuildId == id_) {
                    conf_ = c;
                    return true;
                }
            }
            conf_ = null;
            return false;
        }

        public void Setup(EventDecorateInfo conf_, fat.gamekitdata.MapBuilding data_) {
            confBuilding = conf_;
            SetupCost(confBuilding);
            SetupState();
            Refresh();
        }

        public void TrySetupAsset(int areaId_) {
            if (areaId_ != info.area) return;
            asset.Load(confBuilding?.PrefabName);
        }

        public void SetupCost(EventDecorateInfo conf_) {
            cost.id = conf_.TokenID;
            cost.require = conf_.Price;
        }

        private void AssetReady(MapAsset asset_) {
            if (info == null) {
                Debug.LogError($"{nameof(MapBuilding)} info {info?.id} missing");
                asset.SetActive(false);
                return;
            }
            var root = asset_.asset.transform;
            root.SetParent(info.transform, false);
            var count = info.levelInfo.Count;
            var level = Level switch {
                >= 0 when Level < count => info.levelInfo[Level],
                >= 0 when count > 0 => info.levelInfo[^1],
                < 0 when count > 0 => info.levelInfo[0],
                _ => default,
            };
            var offset = level.offset;
            bounds = level.bounds;
            if (asset_.sort != null) sortingOrder = asset_.sort.sortingOrder;
            else {
                var spr = root.GetComponentInChildren<SpriteRenderer>();
                if (spr != null) sortingOrder = spr.sortingOrder;
            }
            root.localPosition = offset;
            cloud.Sorting(sortingOrder + 10);
            RefreshState();
        }

        private void UpgradeReady(MapAsset asset_) {
            asset_.SetActive(false);
        }

        private void CloudReady(MapAsset asset_) {
            var root = asset_.asset.transform;
            root.SetParent(info.transform, false);
            root.position = info.cloud;
        }

        public bool Refresh() => Refresh(Game.Manager.decorateMan.Score);
        public bool Refresh(int score_) {
            var changed = false;
            if (score_ != cost.current) {
                cost.current = score_;
                changed = true;
            }
            var percent = Mathf.Clamp01((float)score_ / cost.require);
            CostReady = cost.Done;
            CostPercent = Mathf.Clamp01(percent);
            // if (changed) {
            //      MessageCenter.Get<MSG.MAP_BUILDING_UPDATE>().Dispatch(this);
            // }
            return changed;
        }

        public void SetupState() {
            var deco = Game.Manager.decorateMan;
            var area = deco.GetAreaID();
            TrySetupAsset(area);
            var unlock = deco.CheckDecorationUnlock(confBuilding.Id);
            Phase = unlock ? 1 : 0;
            Level = Phase;
        }

        public void RefreshState() {
            asset.SetActive(Placed);
            asset.PlayAny("ans_dyn_idle", true);
            asset.Preview(false);
        }

        public void OverviewState() {
            var deco = Game.Manager.decorateMan;
            var area = deco.GetAreaID();
            TrySetupAsset(area);
            asset.SetActive(true);
            asset.PlayAny("ans_dyn_idle", true);
            asset.Preview(false);
        }

        public override void OpenBanner() {
            ClearUI();
        }

        public override MapSceneUI WhenSelect() {
            var ui = Game.Manager.mapSceneMan.ui;
            var n = 0;
            MapUI.UIAsset target = n switch {
                _ when !Unlocked => ui.buildingPlace,
                _ => ui.buildingPlace,//error proof
            };
            if (target == null || !ui.Open(target, this)) return null;
            uiActive = (BuildingUI)target.UIRef;
            return uiActive;
        }

        public bool TryUpgrade() {
            if (!CostReady) return false;
            var deco = Game.Manager.decorateMan;
            if (!deco.TryUnlock(confBuilding.Id)) return false;
            TryUpgradeOnce();
            return true;
        }

        private void TryUpgradeOnce() {
            Level = 1;
            Phase = 1;
            Refresh();
        }

        internal void PrepareUpgrade() {
            assetUpgrade.Load(confBuilding.PrefabName);
        }

        internal void ConfirmUpgrade() {
            asset.Take(assetUpgrade);
            var obj = asset.asset;
            AssetReady(asset);
            obj.SetActive(true);
        }
    }
}