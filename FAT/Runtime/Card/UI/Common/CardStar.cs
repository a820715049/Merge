using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class CardStar : MonoBehaviour {
        public List<GameObject> list = new();

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            var root = transform;
            list.Clear();
            for (var k = 0; k < root.childCount; ++k) {
                list.Add(root.GetChild(k).gameObject);
            }
        }
        #endif

        public void Setup(int n_) {
            var m = Mathf.Min(n_, list.Count);
            for (var k = 0; k < m; ++k) list[k].SetActive(true);
            for (var k = m; k < list.Count; ++k) list[k].SetActive(false);
        }
    }
}