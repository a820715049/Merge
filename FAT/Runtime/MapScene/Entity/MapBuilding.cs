using System.Collections.Generic;
using UnityEngine;
using fat.rawdata;
using static fat.conf.Data;
using Config;
using EL;
using UnityEngine.Rendering;

namespace FAT {
    using static MessageCenter;

    public abstract class IMapBuilding {
        public MapBuildingInfo info;
        public BuildingUI uiActive;
        public virtual bool WillSelect => true;

        public abstract MapSceneUI WhenSelect();
        public abstract void OpenBanner();

        internal void ClearUI(bool suppress_ = false) {
            if (uiActive == null) return;
            if (suppress_) {
                uiActive.ClearTarget();
                uiActive.Close();
            }
            else {
                uiActive.Clear();
            }
            uiActive = null;
        }
    }

    public class CostRecord {
        public int id;
        public CoinType coin;
        public int current;
        public int require;
        public bool Done => current >= require;
    }

    public struct CostInfo {
        public int id;
        public int require;
        public int target;
    }

    public class MapBuilding : IMapBuilding {
        public BuildingBase confBuilding;
        public BuildingLevel confLevel;
        public List<RewardConfig> rewardList = new();
        public BuildingCost confCost;
        public List<CostRecord> costList = new();
        public List<int> storyList = new();
        public int storyPending;
        public float CoinPercent { get; private set; }
        public float CostPercent { get; private set; }
        public int CostDone { get; private set; }
        public bool CostReady { get; private set; }
        public CostRecord LastCoinCost { get; private set; }

        public bool Valid => confBuilding != null;
        public int Id => info.id;
        public string Name => confBuilding.Name;
        public string StoryIcon => confBuilding.StoryImage;
        public int DisplayLevel => confLevel.DisplayLevel;
        public int UnlockLevel => confBuilding.UnlockLevel;
        public bool Mystery => confBuilding.IsSecret;
        public int Priority => confBuilding.Sequence;
        public bool Locked { get; private set; }
        public bool LockedCheck => Game.Manager.mergeLevelMan.displayLevel < UnlockLevel;
        public bool Visible { get; private set; }
        public bool VisibleCheck {
            get {
                var built = Game.Manager.mapSceneMan.scene.built;
                var pre = confBuilding.Pre;
                return pre == 0 || built.Contains(pre);
            }
        }
        public int Level { get; private set; }
        public int Phase { get; private set; }
        public int MaxLevel => confBuilding.LevelInfo.Count - 1;
        public int MaxPhase => confLevel.CostInfo.Count - 1;
        public int PhaseVisual => Phase + 1;
        public int PhaseCount => confLevel.CostInfo.Count;
        public bool PhaseUpgrade => Phase >= MaxPhase;
        public bool Maxed => Level >= MaxLevel;
        public override bool WillSelect => !Locked;
        public bool WillUnlock => Locked && !LockedCheck;
        public bool WillBuild => confLevel.IsBuy;
        public bool WillUpgrade => confCost != null;
        public bool Upgrading => assetUpgrade.Busy || assetUpgrade.Valid;
        public bool UpgradeCostReady => !Maxed && Visible && WillSelect && CostReady;
        public bool UpgradeAvailable => !Maxed && Visible && WillSelect && WillUpgrade;
        public bool UnlockAvailable => WillUnlock;
        public bool AssetUpgradeReady => assetUpgrade.Valid;
        public bool AnyStory => storyList.Count > 0;
        public Animation sold;
        public Bounds bounds;
        public int sortingOrder;

        public MapAsset asset;
        public MapAsset assetUpgrade;
        public MapAsset cloud;
        public BannerBuilding banner;

        public MapBuilding() {
            asset = new() { WhenAssetReady = AssetReady };
            assetUpgrade = new() { WhenAssetReady = UpgradeReady };
            cloud = new() { WhenAssetReady = CloudReady };
        }

        public static bool ReadyToCreate(int id_, out BuildingBase conf_, out string rs_) {
            conf_ = GetBuildingBase(id_);
            if (conf_ == null) { rs_ = "config not found"; return false; }
            if (conf_.LevelInfo.Count == 0) { rs_ = "level info empty"; return false; }
            rs_ = null;
            return true;
        }

        public void Setup(BuildingBase conf_, fat.gamekitdata.MapBuilding data_) {
            confBuilding = conf_;
            if (data_ == null) {
                SetupByLevel(0, 0);
            }
            else {
                SetupByLevel(data_.Level, data_.Phase);
            }
            Refresh();
            SetupStory();
        }

        public void SetupByLevel(int level_, int phase_, bool upgrade_ = false) {
            Level = Mathf.Clamp(level_, 0, MaxLevel);
            var id = confBuilding.LevelInfo[Level];
            confLevel = GetBuildingLevel(id);
            rewardList.Clear();
            foreach (var r in confLevel.LevelReward) {
                rewardList.Add(r.ConvertToRewardConfig());
            }
            if (costList.Count > 0) LastCoinCost = costList[0];
            costList.Clear();
            if (confLevel.CostInfo.Count == 0) {
                Phase = 0;
                confCost = null;
            }
            else {
                Phase = Mathf.Clamp(phase_, 0, MaxPhase);
                var coinMan = Game.Manager.coinMan;
                var costId = confLevel.CostInfo[Phase];
                confCost = GetBuildingCost(costId);
                foreach(var c in confCost.Cost) {
                    var r = c.ConvertToRewardConfig();
                    costList.Add(new() {
                        id = r.Id,
                        coin = coinMan.GetCoinTypeById(r.Id),
                        require = r.Count,
                    });
                }
            }
            Visible = VisibleCheck;
            Locked = LockedCheck;
            if (Locked) {
                //setup cloud
                cloud.Load(confBuilding.LockedCloudAsset);
                if (confBuilding.IsPreShow) asset.Load(confLevel.BuildingAsset);
            }
            else {
                cloud.Unload();
                //setup building
                if(!upgrade_) asset.Load(confLevel.BuildingAsset);
            }
        }

        public void SetupStory() {
            var levelList = confBuilding.LevelInfo;
            void AppendStory(BuildingLevel lConf, int p_) {
                var cList = lConf.CostInfo;
                var p = Mathf.Min(cList.Count, p_);
                for (var c = 0; c < p; ++c) {
                    var cConf = GetBuildingCost(cList[c]);
                    if (cConf.StoryId > 0) storyList.Add(cConf.StoryId);
                }
            }
            for (var l = 0; l < Level; ++l) {
                AppendStory(GetBuildingLevel(levelList[l]), int.MaxValue);
            }
        }

        public void NextStory(int id_) {
            storyPending = id_;
            if (id_ > 0) storyList.Add(id_);
        }

        public void ConfirmStory() {
            storyPending = 0;
        }

        public void RefreshVisible() {
            Visible = VisibleCheck;
            info.select.enabled = Visible;
            banner.Activate(this);
            if (Visible) banner.Refresh(this);
        }

        private void AssetReady(MapAsset asset_) {
            var root = asset_.asset.transform;
            root.SetParent(info.transform, false);
            var rootSold = root.Find("sold");
            sold = rootSold?.GetComponentInChildren<Animation>();
            var level = Level < info.levelInfo.Count ? info.levelInfo[Level] : default;
            var offset = level.offset;
            bounds = level.bounds;
            if (asset_.sort != null) sortingOrder = asset_.sort.sortingOrder;
            else {
                var spr = root.GetComponentInChildren<SpriteRenderer>();
                if (spr != null) sortingOrder = spr.sortingOrder;
            }
            root.localPosition = offset;
            cloud.Sorting(sortingOrder + 10);
        }

        private void UpgradeReady(MapAsset asset_) {
            asset_.SetActive(false);
        }

        private void CloudReady(MapAsset asset_) {
            var root = asset_.asset.transform;
            root.SetParent(info.transform, false);
            root.position = info.cloud;
        }

        internal void RefreshUnlock() {
            SetupByLevel(Level, Phase);
            Refresh(unlock_: true);
        }

        public bool Refresh(bool enter_ = false, bool unlock_ = false, bool upgrade_ = false) {
            var coinMan = Game.Manager.coinMan;
            var changed = false;
            var percent = 0f;
            var done = 0;
            var costCount = costList.Count;
            for(var i = 0; i < costCount; ++i) {
                var cost = costList[i];
                var v = coinMan.GetCoin(cost.coin);
                if (v != cost.current) {
                    cost.current = v;
                    changed = true;
                }
                if (cost.Done) ++done;
                var p = Mathf.Clamp01((float)v / cost.require);
                if (cost.coin == CoinType.MergeCoin) CoinPercent = p;
                percent += p;
            }
            CostDone = done;
            CostReady = costCount > 0 && CostDone >= costCount;
            CostPercent = Mathf.Clamp01(percent / Mathf.Max(1, costCount));
            var popup = enter_ || unlock_ && !upgrade_;
            if (changed && !upgrade_) {
                Get<MSG.MAP_BUILDING_UPDATE>().Dispatch(this);
            }
            if (Maxed) {
                banner.CloseImmediate();
            }
            else if (popup && !Locked) {
                OpenBanner();
            }
            if (changed || popup) {
                banner.Refresh(this);
            }
            return changed;
        }

        public override void OpenBanner() {
            ClearUI();
            if (banner.Active) return;
            banner.Activate(this);
        }

        public override MapSceneUI WhenSelect() {
            var ui = Game.Manager.mapSceneMan.ui;
            var n = 0;
            MapUI.UIAsset target = n switch {
                _ when Locked => null,
                _ when Maxed && Mystery => ui.buildingMystery,
                _ when Maxed => ui.buildingMaxed,
                _ when WillBuild => ui.buildingPurchase,
                _ when WillUpgrade => ui.buildingUpgrade,
                _ => null,
            };
            if (target == null || !ui.Open(target, this)) return null;
            banner.Close();
            uiActive = (BuildingUI)target.UIRef;
            return uiActive;
        }

        public bool TryUpgrade(IList<RewardCommitData> reward_, IList<RewardCommitData> rewardL_) {
            if (confCost == null) return false;
            var coinMan = Game.Manager.coinMan;
            foreach(var c in costList) {
                if (!c.Done) return false;
            }
            foreach(var c in costList) {
                var r = coinMan.UseCoin(c.coin, c.require, ReasonString.meta);
                if (!r) {
                    DebugEx.Error($"failed to use coin {c.id} {c.coin} {c.require}");
                }
            }
            UpgradeReward(reward_, rewardL_);
            TryUpgradeOnce();
            return true;
        }

        private void UpgradeReward(IList<RewardCommitData> reward_, IList<RewardCommitData> rewardL_) {
            var rewardMan = Game.Manager.rewardMan;
            if (PhaseUpgrade) {
                foreach (var r in rewardList) {
                    var data = rewardMan.BeginReward(r.Id, r.Count, ReasonString.meta);
                    rewardL_.Add(data);
                }
            }
            foreach (var rConf in confCost.CostReward) {
                var r = rConf.ConvertToRewardConfig();
                var data = rewardMan.BeginReward(r.Id, r.Count, ReasonString.meta);
                reward_.Add(data);
            }
        }

        private void TryUpgradeOnce() {
            ++Game.Manager.mapSceneMan.CostCount;
            DataTracker.meta_cost.Track(this);
            NextStory(confCost.StoryId);
            var level = Level;
            var phase = Phase + 1;
            if (phase > MaxPhase) {
                ++level;
                phase = 0;
                if (level == 1) Get<MSG.MAP_BUILDING_BUILT>().Dispatch(this);
            }
            SetupByLevel(level, phase, upgrade_: true);
            DataTracker.TraceUser().MetaUpdate().Apply();
            Refresh(upgrade_: true);
        }

        internal void PrepareUpgrade() {
            assetUpgrade.Load(confLevel.BuildingAsset);
        }

        internal void ConfirmUpgrade() {
            asset.Take(assetUpgrade);
            var obj = asset.asset;
            AssetReady(asset);
            obj.SetActive(true);
        }
    }
}