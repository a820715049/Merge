/*
 * @Author: pengjian.zhang
 * @Description: 折扣标签组件
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/sp3945n1tp4oyi9o
 * @Date: 2024-07-23 18:25:58
 */

using System.Collections;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIIAPLabel
    {
        private GameObject labelPrefab;

        public void Setup(Transform root, int label, int packId)
        {
            if (label <= 0)
                return;
            Game.Manager.iap.IAPLabel(label, packId, out var confLabel, out var txt);
            if (txt == null || confLabel == null)
            {
                root.gameObject.SetActive(false);
                return;
            }
            root.gameObject.SetActive(true);
            Game.Instance.StartCoroutineGlobal(_CoLoad(root, confLabel, txt));
        }

        private IEnumerator _CoLoad(Transform parent, Label labelConf, string txt)
        {
            var asset = labelConf.Prefab.ConvertToAssetConfig();
            var loader = EL.Resource.ResManager.LoadAsset<GameObject>(asset.Group, asset.Asset);
            yield return loader;
            if (!loader.isSuccess)
            {
                DebugEx.Error($"UIIAPLabel::_CoLoad ----> loading res error {loader.error}");
            }
            labelPrefab = GameObject.Instantiate(loader.asset as GameObject, parent);
            if (labelPrefab == null)
                yield break;
            labelPrefab.transform.SetParent(parent);
            labelPrefab.transform.localPosition = Vector3.zero;
            labelPrefab.transform.localScale = Vector3.one;
            var scale = labelConf.Zoom;
            if (scale > 0)
                labelPrefab.transform.localScale = new Vector3(scale, scale, scale);
            labelPrefab.name = loader.asset.name;
            labelPrefab.gameObject.SetActive(true);
            labelPrefab.transform.GetChild(0).GetComponent<UIImageRes>().SetImage(labelConf.Image);
            var tmp = labelPrefab.transform.GetChild(1).GetComponent<TMP_Text>();
            tmp.SetText(txt);
            var c = FontMaterialRes.Instance.GetFontMatResConf(labelConf.Style);
            c?.ApplyFontMatResConfig(tmp);
        }

        public void Clear()
        {
            if (labelPrefab != null)
            {
                GameObject.Destroy(labelPrefab);
                labelPrefab = null;
            }
        }
    }
}