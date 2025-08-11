using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT {
    public class MapTransition {
        public MapAsset assetUpgrade;
        public TransitionUpgrade upgrade;
        
        public MapTransition() {
            assetUpgrade = new() { WhenAssetReady = AssetReady };
        }

        private void AssetReady(MapAsset asset_) {
            var obj = asset_.asset;
            var root = obj.transform;
            root.SetParent(MapSceneMan.AssetRoot, false);
            upgrade = obj.GetComponent<TransitionUpgrade>();
        }

        public void Load() {
            assetUpgrade.Load("fat_map", "transition_upgrade.prefab");
        }

        public void ResetClear() {
            assetUpgrade.Unload();
        }

        public (IEnumerator, IEnumerator, bool) AnimateUpgrade(MapBuilding target_) {
            var sold = target_.sold;
            if (sold != null) {
                return (upgrade.AnimateSold(sold), null, true);
            }
            upgrade.Resize(target_.bounds, target_.sortingOrder);
            return (upgrade.AnimateIn(), upgrade.AnimateOut(), false);
        }
    }
}