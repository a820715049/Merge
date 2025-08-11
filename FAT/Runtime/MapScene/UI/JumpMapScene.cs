using System;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;
using fat.rawdata;

namespace FAT {
    public class JumpMapScene : MonoBehaviour {
        public bool filterBuild;
        public LayoutElement element;
        public GameObject group;
        public GameObject dot;
        public TextMeshProUGUI text;
        private Action WhenUpdate;
        private MapBuilding focus;

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            element = transform.GetComponent<LayoutElement>();
            var root = transform.Find("group");
            group = root.gameObject;
            dot = root.Find("dot")?.gameObject;
            text = root.FindEx<TextMeshProUGUI>("text");
        }
        #endif

        public void Start() {
            var button = group.GetComponent<MapButton>().WithClickScale().FixPivot();
            button.WhenClick = () => GameProcedure.MergeToScene(focus);
        }

        public void OnEnable() {
            Game.Manager.mapSceneMan.RefreshCost();
            Refresh();
            WhenUpdate ??= Refresh;
            MessageCenter.Get<MSG.MAP_BUILDING_UPDATE_ANY>().AddListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().AddListener(WhenUpdate);
            MessageCenter.Get<MSG.SCENE_LOAD_FINISH>().AddListener(WhenUpdate);
        }

        public void OnDisable() {
            MessageCenter.Get<MSG.MAP_BUILDING_UPDATE_ANY>().RemoveListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().RemoveListener(WhenUpdate);
            MessageCenter.Get<MSG.SCENE_LOAD_FINISH>().RemoveListener(WhenUpdate);
        }

        private void Refresh() {
            var feature = Game.Manager.featureUnlockMan;
            if (!feature.IsFeatureEntryUnlocked(FeatureEntry.FeatureMapJump)) {
                Visible(false);
                return;
            }
            var map = Game.Manager.mapSceneMan;
            var (count, next) = map.CountReadyToBuild(filterBuild);
            var ready = next != null;
            focus = next;
            Visible(ready);
            if (!ready) return;
            text.text = $"{count}";
        }

        private void Visible(bool v_) {
            group.SetActive(v_);
            element.ignoreLayout = !v_;
        }
    }
}