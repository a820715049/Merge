using System.Collections.Generic;
using UnityEngine;
using fat.rawdata;
using static fat.conf.Data;
using EL;
using System.Collections;

namespace FAT {
    public class MapScene {
        private fat.gamekitdata.MapScene data;
        public BuildingScene confScene;
        public int Id => confScene.Id;
        public readonly List<MapArea> area = new();
        public readonly List<MapBuilding> building = new();
        public readonly List<MapBuildingForEvent> buildingForEvent = new();
        public readonly HashSet<int> built = new();

        public MapAsset asset;
        public MapSceneInfo info;

        public BuildingPurchase buildingPurchase;

        public bool Active => asset.Active;
        public bool Ready => asset.Valid && viewReady && building.Count > 0;
        public bool viewReady;
        private bool visible;

        public MapScene() {
            asset = new();
        }
        
        public void Setup(int id) {
            confScene = GetBuildingScene(id);
            Game.Instance.StartCoroutineGlobal(WaitSetup());
        }

        public IEnumerator WaitSetup() {
            viewReady = false;
            var str = confScene.PrefabAsset;
            var conf = str.ConvertToAssetConfig();
            yield return asset.WaitLoad(conf.Group, conf.Asset);
            if (!asset.Valid) {
                DebugEx.Error($"scene asset not found: {confScene.PrefabAsset}");
                yield break;
            }
            Visible(visible);
            var root = asset.asset.transform;
            root.SetParent(MapSceneMan.AssetRoot, false);
            info = root.GetComponent<MapSceneInfo>();
            var assetT = new MapAsset();
            yield return assetT.WaitLoadInfo(info.templateBanner);
            assetT.asset.transform.SetParent(MapSceneMan.AssetRoot, false);
            info.templateBanner.obj = assetT.asset;
            Game.Manager.mapSceneMan.SetupView(info);
            viewReady = true;
            SetupArea();
            TrySetup();
            RefreshAreaSetup();
            RefreshStateForEvent();
            MessageCenter.Get<MSG.MAP_SETUP_FINISHED>().Dispatch();
        }

        internal void FillData(fat.gamekitdata.MapScene data_) {
            var bMap = data_.Building;
            if (!Ready) {
                bMap.Add(data.Building);
                return;
            }
            foreach (var b in building) {
                if (!b.Valid || bMap.ContainsKey(b.Id)) continue;
                bMap[b.Id] = new() {
                    Level = b.Level,
                    Phase = b.Phase,
                };
            }
            foreach (var b in buildingForEvent) {
                if (!b.Valid || bMap.ContainsKey(b.Id)) continue;
                bMap[b.Id] = new() {
                    Level = b.Level,
                    Phase = b.Phase,
                };
            }
        }

        internal void DataReady(fat.gamekitdata.MapScene data_) {
            data = data_;
            TrySetup();
        }

        private void TrySetup() {
            if (data != null && asset.Valid) {
                RefreshArea();
                SetupBuilding();
                MessageCenter.Get<MSG.SCENE_LOAD_FINISH>().Dispatch();
            }
        }

        private void SetupArea() {
            foreach (var a in info.areaList) {
                var aa = new MapArea() { info = a };
                area.Add(aa);
            }
        }

        private void SetupBuilding() {
            static fat.gamekitdata.MapBuilding Find(fat.gamekitdata.MapScene data, int id) {
                if (data == null) return null;
                if (data.Building.TryGetValue(id, out var d)) return d;
                return null;
            }
            if (building.Count > 0) DebugEx.Warning($"building list dirty");
            if (buildingForEvent.Count > 0) DebugEx.Warning($"event building list dirty");
            var map = Game.Manager.mapSceneMan;
            var ui = map.ui;
            foreach(var bInfo in info.buildingList) {
                if (!MapBuilding.ReadyToCreate(bInfo.id, out var conf, out var rs)) {
                    DebugEx.Warning($"building {bInfo.id} setup fail reason:{rs}");
                    continue;
                }
                var b = new MapBuilding() {
                    info = bInfo,
                    banner = ui.CreateBanner(bInfo),
                };
                bInfo.buildingRef = b;
                b.Setup(conf, Find(data, bInfo.id));
                building.Add(b);
                if (b.Level > 0) map.RecordBuilt(b);
            }
            foreach(var bInfo in info.buildingForEvent) {
                if (!MapBuildingForEvent.ReadyToCreate(bInfo.id, out var conf)) {
                    DebugEx.Warning($"decorate info for building {bInfo.id} not found");
                    continue;
                }
                var b = new MapBuildingForEvent() {
                    info = bInfo,
                };
                bInfo.buildingRef = b;
                b.Setup(conf, Find(data, bInfo.id));
                buildingForEvent.Add(b);
            }
            foreach(var b in building) {
                b.RefreshVisible();
            }
        }

        internal void Visible(bool v_) {
            visible = v_;
            asset.SetActive(v_);
        }

        internal void Enter() {
            Visible(true);
            Refresh(enter_:true);
        }

        internal void Exit() {
            Visible(false);
        }

        internal IMapBuilding Find(int area_, int id_) {
            foreach(var b in buildingForEvent) {
                if (b.info.area == area_ && b.Id == id_) return b;
            }
            foreach(var b in building) {
                if (b.info.area == area_ && b.Id == id_) return b;
            }
            return null;
        }

        internal (bool, MapArea) FindArea(int area_) {
            foreach(var a in area) {
                if (a.info.id == area_) return (true, a);
            }
            return default;
        }

        internal void CollectUnlocked(IList<MapBuilding> cache_) {
            foreach (var b in building) {
                if (b.UnlockAvailable) {
                    cache_.Add(b);
                }
            }
        }

        internal void Refresh(bool enter_ = false) {
            var anyUpdate = false;
            foreach (var b in building) {
                anyUpdate = b.Refresh(enter_:enter_) || anyUpdate;
            }
            if (anyUpdate) {
                MessageCenter.Get<MSG.MAP_BUILDING_UPDATE_ANY>().Dispatch();
            }
        }
        
        internal void RefreshForEvent() {
            var score = Game.Manager.decorateMan.Score;
            foreach (var b in buildingForEvent) {
                b.Refresh(score);
            }
        }

        internal void RefreshStateForEvent() {
            // if (data == null) return;
            foreach (var b in buildingForEvent) {
                b.SetupState();
                b.RefreshState();
            }
        }

        internal void OverviewStateForEvent() {
            foreach (var b in buildingForEvent) {
                b.OverviewState();
            }
        }

        internal void RefreshAreaSetup() {
            // if (data == null) return;
            var mgr = Game.Manager.decorateMan;
            var active = mgr.GetCloudState();
            var id = mgr.GetAreaID();
            foreach(var a in area) {
                if (a.WillSetup(id)) {
                    Game.Instance.StartCoroutineGlobal(a.WaitSetup());
                }
                a.RefreshCloud(id, active);
            }
        }

        internal void RefreshArea() {
            var mgr = Game.Manager.decorateMan;
            var active = mgr.GetCloudState();
            var id = mgr.GetAreaID();
            foreach(var a in area) {
                a.RefreshCloud(id, active);
            }
        }

        internal void ResetClear() {
            GameObject.DestroyImmediate(asset.asset);
            building.Clear();
            buildingForEvent.Clear();
        }
    }
}