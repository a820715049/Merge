using System.Collections;
using System;
using UnityEngine;
using EL.Resource;
using Config;
using Spine.Unity;
using System.Collections.Generic;
using DG.Tweening;
using EL;

namespace FAT {
    public class MapArea {
        public MapAreaInfo info;
        public MapAsset cloud;
        public MapAssetGroup ground;

        public MapArea() {
            cloud = new();
            ground = new();
        }

        public IEnumerator WaitSetup() {
            if (info.flex) DebugEx.Info($"{nameof(MapScene)} loading area {info.id}");
            yield return cloud.WaitLoadInfo(info.cloud);
            yield return ground.WaitLoad(info.assetGround);
        }

        public bool WillSetup(int id_) => !info.flex || info.id == id_;

        public void RefreshCloud(int id_, bool cloud_) {
            if (info.flex) info.gameObject.SetActive(info.id == id_);
            var o = info.cloud.wrap;
            if (o == null) return;
            o.SetActive(info.id != id_ || cloud_);
        }
    }
}