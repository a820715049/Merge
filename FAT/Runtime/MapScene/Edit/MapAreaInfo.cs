using System.Collections.Generic;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FAT {
    public class MapAreaInfo : MonoBehaviour {
        public int id;
        public float zoom;
        public bool flex;
        public Vector3 center;
        public AssetInfo cloud;
        public AssetGroup assetGround;
        public MapSetting overview;
    }
}