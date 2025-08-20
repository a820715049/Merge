using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using EL;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using DG.Tweening;
using FAT.MSG;

namespace FAT
{
    using static MessageCenter;
    using static PoolMapping;

    public class MapSceneMan : IGameModule, IUserDataHolder
    {
        public static Transform AssetRoot { get; set; }
        public MapViewport viewport;
        public MapInteract interact;
        public MapUI ui;
        public MapScene scene;
        public MapTransition transition = new();
        public MapBuilding upgrading;
        public MapEffectForBuilding MapEffectForBuilding;
        public bool Active => scene.Active;
        public bool Focus => interact.selected != null;
        public int CostCount { get; set; }
        public int BuildingIdMax { get; set; }
        public MetaPopup popup = new();

        private Action<CoinType> OnCoinChange;
        private Action<MapBuilding> OnBuilt;
        private Action<DecorateActivity, int> OnTokenChange;
        private Action OnDecorateRefresh;
        private Action OnDecorateAreaRefresh;
        private readonly List<CostInfo> costInfo = new();
        private bool willLevelup;

        public void Reset()
        {
            scene?.ResetClear();
            ui?.ResetClear();
            transition?.ResetClear();
            scene = new();
            var cam = Camera.main;
            interact = new(cam);
            viewport = new(cam, interact);
            ui = new();
            AssetRoot = cam.transform.parent;
            ClearListener();
            MapEffectForBuilding?.ResetClear();
            MapEffectForBuilding = new MapEffectForBuilding(cam);
        }

        public void LoadConfig()
        {
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var game = archive.ClientData.PlayerGameData;
            var data = game.MapScene ??= new();
            scene.FillData(data);
            var any = data.AnyState;
            any.Add(ToRecord(1, CostCount));
        }

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.MapScene;
            scene.DataReady(data ?? new());
            if (data == null) return;
            var any = data.AnyState;
            CostCount = ReadInt(1, any);
        }

        public void Startup()
        {
            SetupListener();
            ui.PrepareUI();
            scene.Setup(1);
            transition.Load();
        }

        public void SetupView(MapSceneInfo info_)
        {
            viewport.SetupCamera(info_.setting);
            viewport.SetupControl(info_.control);
            viewport.SetupCanvas(info_.canvas, info_.root);
            ui.SetupCanvas(info_, viewport.camera);
        }

        private void SetupListener()
        {
            OnCoinChange ??= _ => RefreshCost();
            OnBuilt ??= RecordBuilt;
            OnTokenChange ??= (_, _) => scene.RefreshForEvent();
            OnDecorateRefresh ??= () => scene.RefreshStateForEvent();
            OnDecorateAreaRefresh ??= () => scene.RefreshAreaSetup();
            Get<GAME_COIN_CHANGE>().AddListener(OnCoinChange);
            Get<MAP_BUILDING_BUILT>().AddListener(OnBuilt);
            Get<DECORATE_SCORE_UPDATE>().AddListener(OnTokenChange);
            Get<DECORATE_REFRESH>().AddListener(OnDecorateRefresh);
            Get<DECORATE_AREA_REFRESH>().AddListener(OnDecorateAreaRefresh);
        }

        private void ClearListener()
        {
            Get<GAME_COIN_CHANGE>().RemoveListener(OnCoinChange);
            Get<MAP_BUILDING_BUILT>().RemoveListener(OnBuilt);
            Get<DECORATE_SCORE_UPDATE>().RemoveListener(OnTokenChange);
            Get<DECORATE_REFRESH>().RemoveListener(OnDecorateRefresh);
            Get<DECORATE_AREA_REFRESH>().RemoveListener(OnDecorateAreaRefresh);
        }

        public void Enter(MapBuilding focus_)
        {
            Enter();
            TryFocus(focus_);
        }

        public void Enter()
        {
            UIConfig.UISceneHud.Open();
            scene.Enter();
            DataTracker.TrackShowMeta();
            Get<MapState>().Dispatch(true);
        }

        public void Exit()
        {
            UIConfig.UISceneHud.Close();
            interact.Clear();
            ui.Clear();
            scene.Exit();
            Get<MapState>().Dispatch(false);
        }

        internal void DebugReset()
        {
            interact.ClearSelect();
            scene.built.Clear();
            foreach (var b in scene.building)
            {
                b.storyList.Clear();
                b.SetupByLevel(0, 0);
                b.Refresh(enter_: true);
                b.RefreshVisible();
            }

            Get<MAP_BUILDING_UPDATE_ANY>().Dispatch();
        }

        public void Select(MapBuilding target_) => interact.Select(viewport, target_);

        public bool Select(int area_, int id_)
        {
            var b = scene.Find(area_, id_);
            if (b == null) return false;
            interact.Select(viewport, b, duration_: 0.5f);
            return true;
        }

        public void TryFocus(MapBuilding focus_)
        {
            var focus = focus_ ?? NextBuildingToFocus();
            if (focus == null) return;
            if (focus.CostReady)
            {
                interact.Select(viewport, focus);
            }
            else
            {
                viewport.Focus(focus);
            }
        }

        public void TryFocus(int area_, float duration_ = 0.4f, float speed_ = 0f, TweenCallback WhenFocus_ = null, float zoom = -1f, bool overview_ = false)
        {
            var (r, aa) = scene.FindArea(area_);
            if (r)
            {
                var a = aa.info;
                var v = WhenFocus_;
                var c = a.center;
                var z = zoom < 0f ? a.zoom : zoom;
                if (overview_) {
                    UIManager.Instance.Visible(UIConfig.UISceneHud, false);
                    Get<GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
                    scene.OverviewStateForEvent();
                    v = () => {
                        viewport.SetupAs(a.overview);
                        WhenFocus_?.Invoke();
                    };
                    c = a.overview.cameraPos;
                    z = a.overview.cameraRest;
                }
                viewport.Focus(c, v_:z, duration_:duration_, speed_:speed_, WhenFocus_:v);
            }
            else
            {
                DebugEx.Warning($"area to focus {area_} not found");
                WhenFocus_?.Invoke();
            }
        }

        public void ExitOverview() {
            Get<GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
            if (viewport.setting == scene.info.setting) return;
            viewport.SetupAs(scene.info.setting);
            scene.RefreshStateForEvent();
            if (!Active) return;
            UIManager.Instance.Visible(UIConfig.UISceneHud, true);
        }
        
        public void SetBuildingLayer(MapBuildingInfo info, string layerName, out int oldLayer)
        {
            oldLayer = 0;
            if (info == null || string.IsNullOrEmpty(layerName))
                return;
            var layer = LayerMask.NameToLayer(layerName);
            var trans = info.transform;
            oldLayer = trans.gameObject.layer;
            _SetBuildingLayer(trans, layer);
        }
        
        public void SetBuildingLayer(MapBuildingInfo info, int layer)
        {
            if (info == null)
                return;
            var trans = info.transform;
            _SetBuildingLayer(trans, layer);
        }
        
        private void _SetBuildingLayer(Transform trans, int layer)
        {
            // 包括 root 本身和所有子物体；参数 true 表示也包含 inactive 的子物体
            var transforms = trans.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                t.gameObject.layer = layer;
            }
        }

        internal void RefreshCost()
        {
            scene.Refresh();
        }

        internal void RefreshVisible() {
            foreach(var b in scene.building) {
                b.RefreshVisible();
            }
        }

        internal void RecordBuilt(MapBuilding b_) {
            var built = scene.built;
            if (!built.Contains(b_.Id)) built.Add(b_.Id);
            BuildingIdMax = Mathf.Max(b_.Id, BuildingIdMax);
        }

        internal void RefreshLocked()
        {
            var token = PoolMappingAccess.Take<List<MapBuilding>>(out var list);
            scene.CollectUnlocked(list);
            if (list.Count == 0)
            {
                TryContinueUpgradeVisual();
                return;
            }

            Get<MAP_BUILDING_UPDATE_ANY>().Dispatch();
            Game.Instance.StartCoroutineGlobal(UnlockVisual(token));
        }

        private IEnumerator UnlockVisual(Ref<List<MapBuilding>> token_)
        {
            var list = token_.obj;
            var uiMgr = UIManager.Instance;
            uiMgr.Block(true);
            var d1 = 0.5f;
            var d2 = 0.8f;
            var wait1 = new WaitForSeconds(d1);
            var wait2 = new WaitForSeconds(d2);
            for (var n = 0; n < list.Count; ++n)
            {
                var b = list[n];
                b.banner.Close();
                viewport.Focus(b, duration_: d1);
                yield return wait1;
                b.RefreshUnlock();
                b.OpenBanner();
                yield return wait2;
            }
            token_.Free();
            uiMgr.Block(false);
            TryContinueUpgradeVisual();
        }

        internal IEnumerator UpgradeVisual(MapBuilding target_, Ref<List<RewardCommitData>> list_,
            Ref<List<RewardCommitData>> listL_, Vector3 from_, Vector3 fromL_, bool upgrade_, float size) {
            var __ = list_.ToAuto();
            var _ = listL_.ToAuto();
            var uiMgr = UIManager.Instance;
            uiMgr.Block(true);
            var level = Game.Manager.mergeLevelMan;
            willLevelup = level.canLevelupAfterFly;
            upgrading = target_;
            var d1 = 2f;
            var d2 = 0.8f;
            var wait1 = new WaitForSeconds(d1);
            var wait2 = new WaitForSeconds(d2);
            if (upgrade_) target_.PrepareUpgrade();
            viewport.Focus(target_, v1_: upgrade_ ? 2 : 0, duration_: d2);
            var (upgradeIn, upgradeOut, sold) = upgrade_ ? transition.AnimateUpgrade(target_) : default;
            if (upgrade_ && !sold) Game.Instance.StartCoroutineGlobal(upgradeIn);
            var cost = target_.LastCoinCost;
            UIFlyUtility.FlyCost(cost.id, cost.require, from_);
            yield return wait1;
            UIFlyUtility.FlyRewardList(list_.obj, from_);
            UIFlyUtility.FlyRewardList(listL_.obj, fromL_, null, size);
            yield return wait2;
            target_.ClearUI(suppress_: true);
            if (!upgrade_) {
                yield return new WaitForSeconds(0.2f);
                goto end;
            }
            if (upgrade_ && sold) yield return upgradeIn;
            viewport.Focus(target_, v_: 12f, duration_: d2);
            while (!target_.AssetUpgradeReady) yield return null;
            target_.ConfirmUpgrade();
            if (upgrade_ && !sold) yield return upgradeOut;
            yield return null;
            end:
            uiMgr.Block(false);
            if (!willLevelup) RefreshLocked();
        }

        internal void TryContinueUpgradeVisual() {
            if (upgrading != null) UpgradeFocus(upgrading);
        }

        internal void UpgradeFocus(MapBuilding b_) {
            interact.selected = null;
            void S() {
                if (!willLevelup || !popup.Notify()) {
                    BuildingUI.TryOpenStoryBySetting(b_, null);
                }
                upgrading = null;
            }
            if (b_.Visible) interact.Select(viewport, b_, delayPopup_: true, WhenPopup_: S);
            else S();
        }

        internal IEnumerator PlaceVisual(MapBuildingForEvent target_, Vector3 from_)
        {
            if (target_ == null || !target_.Valid)
                yield break;
            var uiMgr = UIManager.Instance;
            var mapSceneMan = Game.Manager.mapSceneMan;
            var asset = target_.asset;
            var info = scene.info.decorate;
            var buildingEffect = mapSceneMan.MapEffectForBuilding;
            // (1).建造前的准备工作
            uiMgr.Block(true);
            asset.SetActive(true);
            target_.ClearUI(suppress_: true);
            Get<DECORATE_RES_UI_STATE_CHANGE>().Dispatch(false);
            //将建筑的层级设成Building
            mapSceneMan.SetBuildingLayer(target_.info, "Building", out var oldLayer);
            //打开特效相机 开启渲染RT图
            buildingEffect?.SetCameraEnable(true);

            // (2).开始建筑建造
            //播放建筑建造动画
            var d1 = asset.PlayAny("ans_dyn_show", false);
            var d11 = info.placeSoundDelay;
            yield return new WaitForSeconds(d11);
            //等待一段时间播建筑建造音效
            Game.Manager.audioMan.TriggerSound("DecoratePlaceConfirm");
            yield return new WaitForSeconds(d1 - d11);
            //建筑建造动画播完后  直接播建筑的idle动画
            asset.PlayAny("ans_dyn_idle", true);
            //开启针对RT图的后处理 同时会触发建筑建造的一系列表现
            buildingEffect?.StartPostProcess();
            //等待一段播建造完成效果的时间
            yield return new WaitForSeconds(info.buildingFinishDelay);
            
            // (3).建筑建造结束
            //将建筑的层级设回原层级
            mapSceneMan.SetBuildingLayer(target_.info, oldLayer);
            //关闭特效相机 停止渲染RT图
            buildingEffect?.SetCameraEnable(false);
            var d2 = info.panOut;   //镜头移回的时间
            var d21 = info.panOutDelay; //镜头移回前要等待的时间
            yield return new WaitForSeconds(d21);
            //等待一段时间后将镜头移回 聚焦到整个装饰区
            TryFocus(target_.info.area, duration_: d2);
            yield return new WaitForSeconds(d2);
            //UI关闭block
            uiMgr.Block(false);
            //通知活动做界面表现
            Game.Manager.decorateMan.AfterUnlock();
        }

        internal IEnumerator AreaCompleteVisual(int area_)
        {
            var zV1 = viewport.Zoom;
            var zV2 = zV1 + 4.2f;
            var uiMgr = UIManager.Instance;
            uiMgr.Block(true);
            var d1 = 0f;
            TryFocus(area_, duration_: d1, 0f, null, zV2);
            viewport.ZoomTo(zV2, 0f, free_:true);
            yield return new WaitForSeconds(d1);
            foreach (var b in scene.buildingForEvent)
            {
                if (b.info.area != area_) continue;
                b.asset.SetActive(true);
                var d = b.asset.PlayAny("ans_dyn_idle", false);
            }
            Game.Manager.audioMan.TriggerSound("DecoratePlaceConfirm");
            uiMgr.Block(false);
            uiMgr.OpenWindow(UIConfig.UIDecorateComplete);
        }

        public IEnumerator AreaCloudVisual(int area_, Action callback = null)
        {
            var uiMgr = UIManager.Instance;
            uiMgr.Block(true);
            var (r, target) = scene.FindArea(area_);
            if (!r) goto end;
            var targetC = target.cloud;
            if (!targetC.Valid) {
                var t1 = 0.5f;
                var wait1 = new WaitForSeconds(t1);
                var w = 0;
                var wN = (int)(10 / t1);
                while (!targetC.Valid && ++w < wN) yield return wait1;
                if (!targetC.Valid) goto end;
            }
            var cloud = targetC.asset;
            var anim = cloud.GetComponentInChildren<Animation>();
            if (anim == null) goto end;
            cloud.SetActive(true);
            anim.Play();
            var d = anim.clip.length;
            yield return new WaitForSeconds(d);
            cloud.SetActive(false);
            end:
            callback?.Invoke();
            uiMgr.Block(false);
        }

        internal (int, MapBuilding) CountReadyToBuild(bool filterBuild_)
        {
            MapBuilding ret = null;
            var c = 0;
            var priority = int.MaxValue;
            foreach (var b in scene.building)
            {
                if (b.UpgradeCostReady && b.WillBuild == filterBuild_)
                {
                    ++c;
                    if (b.Priority < priority)
                    {
                        ret = b;
                        priority = b.Priority;
                    }
                }
            }

            return (c, ret);
        }

        //filterBuild_: true=build, false=upgrade
        //return -1=not found, 0=not ready, 1=ready, 2=complete
        public int BuildingState(int id_, int level_, bool filterBuild_)
        {
            foreach (var b in scene.building)
            {
                if (b.Id != id_) continue;
                if (b.Level >= level_) return 2;
                return b.UpgradeCostReady ? 1 : 0;
            }

            return -1;
        }

        //return -1=not found, 0=banner, 1=ui active
        public int BuildingUIState(int id_)
        {
            foreach (var b in scene.building)
            {
                if (b.Id != id_) continue;
                return b.uiActive != null && b.uiActive.Active ? 1 : 0;
            }

            return -1;
        }

        internal MapBuilding NextBuildingToFocus()
        {
            MapBuilding ret = null;
            var priority = int.MaxValue;
            foreach (var b in scene.building)
            {
                if (b.UpgradeCostReady && b.Priority < priority)
                {
                    ret = b;
                    priority = b.Priority;
                }
            }

            if (ret != null) return ret;
            priority = int.MaxValue;
            foreach (var b in scene.building)
            {
                if (b.UpgradeAvailable && b.Priority < priority)
                {
                    ret = b;
                    priority = b.Priority;
                }
            }

            return ret;
        }

        internal MapBuilding NextBuilding()
        {
            MapBuilding ret = null;
            var priority = int.MaxValue;
            foreach (var b in scene.building)
            {
                if (!b.Maxed && b.Visible && b.Priority < priority)
                {
                    ret = b;
                    priority = b.Priority;
                }
            }
            return ret;
        }

        public List<CostInfo> CollectCostRecord(Dictionary<int, int> potentialScore, int targetCount_ = 0)
        {
            costInfo.Clear();
            using var _ = ObjectPool<Dictionary<int, int>>.GlobalPool.AllocStub(out var scoreM);

            bool CheckCost(BuildingCost cost_)
            {
                if (cost_ == null || !cost_.IsCostTool) return false;
                var list = cost_.Cost;
                for (var n = 1; n < list.Count; ++n)
                {
                    var c = list[n].ConvertToRewardConfig();
                    var id = c.Id;
                    potentialScore.TryGetValue(id, out var v);
                    scoreM.TryGetValue(id, out var vM);
                    v -= vM;
                    var confT = GetObjTool(id);
                    var vv = v / confT.ToolScore;
                    var r = c.Count - vv;
                    if (r > 0) costInfo.Add(new() { id = id, require = r, target = cost_.Id });
                    scoreM[id] = vM + c.Count * confT.ToolScore;
                }
                return targetCount_ > 0 && costInfo.Count >= targetCount_;
            }

            bool CheckLevel(BuildingLevel confL, int from_)
            {
                var list = confL.CostInfo;
                for (var n = from_; n < list.Count; ++n)
                {
                    var cost = GetBuildingCost(list[n]);
                    if (CheckCost(cost)) return true;
                }
                return false;
            }

            bool CheckBuilding(MapBuilding b_)
            {
                var confB = b_.confBuilding;
                var list = confB.LevelInfo;
                var p = b_.Phase;
                for (var l = b_.Level; l < b_.MaxLevel; ++l)
                {
                    var confL = GetBuildingLevel(list[l]);
                    if (CheckLevel(confL, p)) return true;
                    p = 0;
                }
                return false;
            }

            scoreM.Clear();
            foreach (var b in scene.building)
            {
                if (CheckBuilding(b)) goto end;
            }

            end:
            return costInfo;
        }
    }
}