using System;
using UnityEngine;
using UnityEngine.UI;
using EL;
using fat.rawdata;
using System.Collections;

namespace FAT {
    public class EntryMapScene : MonoBehaviour {
        public float guideDelay = 1.8f;
        public UIImageState icon;
        public GameObject dot;
        public GameObject guide;
        public Animation notice;
        public RectTransform root;
        public Vector2 size;
        public Vector2 pos;
        private Action WhenUpdate;

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            icon = GetComponent<UIImageState>();
            dot = transform.Find("dot")?.gameObject;
            guide = transform.Find("eff_guide_target")?.gameObject;
            notice = GetComponent<Animation>();
            root = (RectTransform)transform;
            size = root.sizeDelta;
            pos = root.anchoredPosition;
        }
        #endif

        public void Start() {
            var button = GetComponent<MapButton>().WithClickScale().FixPivot();
            button.WhenClick = () => GameProcedure.MergeToScene();
        }

        public void OnEnable() {
            Game.Manager.mapSceneMan.RefreshCost();
            Refresh(open_: true);
            WhenUpdate ??= () => Refresh(open_: false);
            MessageCenter.Get<MSG.MAP_BUILDING_UPDATE_ANY>().AddListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().AddListener(WhenUpdate);
            MessageCenter.Get<MSG.SCENE_LOAD_FINISH>().AddListener(WhenUpdate);
        }

        public void OnDisable() {
            MessageCenter.Get<MSG.MAP_BUILDING_UPDATE_ANY>().RemoveListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().RemoveListener(WhenUpdate);
            MessageCenter.Get<MSG.SCENE_LOAD_FINISH>().RemoveListener(WhenUpdate);
        }

        private void Refresh(bool open_) {
            var feature = Game.Manager.featureUnlockMan;
            if (!feature.IsFeatureEntryUnlocked(FeatureEntry.FeatureMapBuild)) {
                guide.SetActive(false);
                dot.SetActive(false);
                icon.Setup(3);
                return;
            }
            var next = Game.Manager.mapSceneMan.NextBuildingToFocus();
            var ready = next != null && next.UpgradeCostReady;
            var readyO = dot.activeSelf;
            dot.SetActive(ready);
            icon.Setup(ready switch {
                false => 0,
                true when next.WillBuild => 1,
                _ => 2,
            });
            var animate = (open_ || ready != readyO) && ready;
            if (animate) {
                notice.Play();
            }
            else {
                root.sizeDelta = size;
                root.anchoredPosition = pos;
            }
            var level = Game.Manager.mergeLevelMan.displayLevel;
            var global = Game.Manager.configMan.globalConfig;
            var levelMin = global.MapBuildStartAim;
            var levelMax = global.MapBuildStopAim;
            var visible = ready && (level < levelMax && level >= levelMin);
            var animate1 = animate && visible;
            if (animate1) {
                IEnumerator AnimateGuide() {
                    yield return new WaitForSeconds(guideDelay);
                    guide.SetActive(true);
                }
                if (!guide.activeSelf) StartCoroutine(AnimateGuide());
            }
            else if (!visible) {
                guide.SetActive(false);
            }
        }
    }
}