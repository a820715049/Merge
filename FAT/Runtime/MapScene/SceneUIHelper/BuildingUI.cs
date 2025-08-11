using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;
using fat.rawdata;

namespace FAT {
    using static PoolMapping;

    public abstract class BuildingUI : MapSceneUI {
        internal MapBuilding target;

        public void Relocate(Vector3 pos_) {
            transform.position = pos_;
        }

        public virtual void RefreshView(IMapBuilding target_) {}
        public abstract void Refresh(IMapBuilding target_);

        internal override void WillOpen() {
            MessageCenter.Get<MSG.MAP_BUILDING_UPDATE>().AddListener(OnBuildingUpdate);
        }

        internal override void WillClose() {
            MessageCenter.Get<MSG.MAP_BUILDING_UPDATE>().RemoveListener(OnBuildingUpdate);
            var interact = Game.Manager.mapSceneMan.interact;
            if (target != null) {
                interact.ClearSelect(target);
                //TODO delayed open banner
                var banner = target.banner;
                banner.Activate(target);
                banner.Refresh(target);
                target = null;
            }
        }

        private void OnBuildingUpdate(MapBuilding target_) {
            if (target != target_ || target.Upgrading) return;
            Refresh(target);
        }

        internal void ClearTarget() {
            target = null;
        }

        public void Resize(float sizeY_) {
            var root = (RectTransform)transform;
            var size = root.sizeDelta;
            size.y = sizeY_;
            root.sizeDelta = size;
        }

        public void StoryClick() {
            TryOpenStory(target, null);
        }

        public static void TryOpenStory(MapBuilding target_, Action WhenClose_) {
            if (target_ == null || !target_.AnyStory) {
                WhenClose_?.Invoke();
                return;
            }
            UIManager.Instance.OpenWindow(UIConfig.UIMapSceneStory, target_, WhenClose_);
        }
        
        public static void TryOpenStoryBySetting(MapBuilding target_, Action WhenClose_) {
            if (!SettingManager.Instance.PlotIsOn) {
                AccountMan.TryRate(target_.storyPending, target_);
                target_.ConfirmStory();
                WhenClose_?.Invoke();
                return;
            }
            TryOpenStory(target_, WhenClose_);
        }

        public static bool TryUpgrade(MapBuilding target_, Vector3 from_, Vector3 fromL_, float size) {
            var list = PoolMappingAccess.Take<List<RewardCommitData>>();
            var listL = PoolMappingAccess.Take<List<RewardCommitData>>();
            var upgrade = target_.PhaseUpgrade;
            if (!target_.TryUpgrade(list.obj, listL.obj)) {
                list.Free();
                listL.Free();
                return false;
            }
            var mgr = Game.Manager.mapSceneMan;
            var pos = mgr.viewport.SceneToCanvasWorld(from_);
            var posL = mgr.viewport.SceneToCanvasWorld(fromL_);
            Game.Instance.StartCoroutineGlobal(mgr.UpgradeVisual(target_, list, listL, pos, posL, upgrade, size));
            return true;
        }
    }
}