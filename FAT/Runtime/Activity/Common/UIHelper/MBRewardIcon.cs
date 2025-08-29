using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using static fat.conf.Data;
using System.Collections;

namespace FAT {
    public class MBRewardIcon : MonoBehaviour {
        [Tooltip("/icon")]
        public Image icon;
        public UIImageRes iconRes;
        public UIImageState state;
        [Tooltip("/icon")]
        public MapButton info;
        [Tooltip("/icon/info")]
        public GameObject objInfo;
        [Tooltip("/count")]
        public TextMeshProUGUI count;
        public string prefix;
        public Func<int, object, bool> WhenClick;
        public Component[] objRef;
        public ObjConfigType infoExclude;
        public ObjConfigType infoAuto;
        public int id;
        public object custom;
        private bool infoValid;
        public int tipOffset = 4;
        public RectTransform tipAnchor;
        public bool useImage;
        public bool skipCount1;
        public bool keepImage;

        #if UNITY_EDITOR

        public bool skipValidate;
        private void OnValidate() {
            if (Application.isPlaying || skipValidate) return;
            var root = transform;
            root.Access("icon", out icon);
            root.Access("icon", out iconRes);
            root.Access("icon", out state, try_:true);
            root.Access("icon", out info, try_:true);
            objInfo = root.TryFind("icon/info");
            root.FindEx("count", out count);
        }
        #endif

        public void Start() {
            SetupInfo();
        }

        public void SetupInfo() {
            if (info == null) return;
            info.WhenClick = () => {
                if (WhenClick != null) {
                    if (!WhenClick(id, custom)) {
                        WhenClick = null;
                        RefreshRaycast();
                    }
                    return;
                }
                OpenInfo(id);
            };
        }

        private bool InfoValid(int id_) => UIItemUtility.ItemTipsInfoAuto(id_, 0)
            || UIItemUtility.ItemTipsInfoValid(id_, (int)infoExclude);

        private void OpenInfo(int id_) {
            var root = tipAnchor == null ? icon.rectTransform : tipAnchor;
            UIItemUtility.ShowItemTipsInfoAuto(id_, root.position, tipOffset + root.rect.size.y * 0.5f);
        }

        public void Refresh(Config.AssetConfig c_, Func<int, object, bool> WhenClick_ = null) {
            WhenClick = WhenClick_;
            RefreshInfo(WhenClick_ != null);
            iconRes.SetImage(c_);
        }

        public void Refresh(Config.RewardConfig c_) {
            var valid = c_ != null;
            gameObject.SetActive(valid);
            if (!valid) return;
            Refresh(c_.Id, c_.Count);
        }
        public void Refresh((int id, int count) v_)
            => Refresh(v_.id, v_.count);
        public virtual void Refresh(int id_, int count_ = 1) {
            var valid = id_ > 0;
            gameObject.SetActive(valid);
            if (!valid) return;
            RefreshIcon(id_);
            RefreshInfo(InfoValid(id_));
            if (!UIUtility.SpecialCountText(id_, count_, out var c)) {
                c = count_ switch {
                    0 => string.Empty,
                    1 when skipCount1 => string.Empty,
                    _ => $"{prefix}{count_}",
                };
            }
            RefreshCount(c);
        }

        public void RefreshEmpty() {
            if(keepImage) return;
            iconRes.Hide();
            RefreshInfo(false);
            RefreshCount(string.Empty);
        }

        public void RefreshIcon(int id_) {
            var conf = GetObjBasic(id_);
            if (conf == null) return;
            var asset = useImage ? conf.Image : conf.Icon;
            if (string.IsNullOrEmpty(asset)) asset = conf.Icon;
            if(!keepImage) iconRes.SetImage(asset);
            id = id_;
        }

        public void RefreshInfo(int id_) {
            id = id_;
            RefreshInfo(true);
        }

        public void RefreshInfo(bool v_) {
            infoValid = v_;
            RefreshRaycast();
            if (objInfo != null) objInfo.SetActive(v_);
            if (v_ && UIItemUtility.ItemTipsInfoAuto(id, (int)infoAuto)) {
                IEnumerator Delay() {
                    yield return new WaitForSeconds(0.2f);
                    OpenInfo(id);
                }
                StartCoroutine(Delay());
            }
        }

        public void RefreshCount(string v_) {
            if (count == null) return;
            count.text = v_;
        }

        public void RefreshClick(Func<int, object, bool> WhenClick_) {
            WhenClick = WhenClick_;
            RefreshRaycast();
        }

        public void RefreshRaycast() {
            var clickValid = WhenClick != null;
            icon.raycastTarget = infoValid || clickValid;
        }

        public void Resize(Vector2 size_) {
            var root = (RectTransform)transform;
            root.sizeDelta = size_;
        }

        public void Hide() => iconRes.Hide();
    }
}