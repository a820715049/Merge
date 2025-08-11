using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT {
    using static BuildingUI;

    public class BuildCostGroup : MonoBehaviour {
        public bool redirect;
        public float redirectDelay;
        public TextMeshProUGUI title;
        public List<BuildCost> costList = new();
        public MapButton confirm;
        public MapButton info;
        public Transform fromSingle;
        public Transform fromMulti;
        public bool lockVisual;

        private Action WhenConfirm;
        private MapBuilding target;
        
        #if UNITY_EDITOR

        public void OnValidate() {
            if (Application.isPlaying) return;
            var root = transform;
            title = root.FindEx<TextMeshProUGUI>("title");
            costList.Clear();
            costList.Add(root.FindEx<BuildCost>("list/entry"));
            confirm = root.FindEx<MapButton>("confirm");
            info = root.FindEx<MapButton>("info");
        }

        #endif

        public void Init(Action WhenConfirm_ = null) {
            confirm.WithClickScale().FixPivot().WhenClick = ConfirmClick;
            info.WithClickScale().FixPivot().WhenClick = InfoClick;
            WhenConfirm = WhenConfirm_;
        }

        public void Refresh(MapBuilding target_) {
            if (lockVisual) return;
            target = target_;
            var cList = target_.costList;
            var singleCost = target.PhaseCount == 1;
            if (singleCost) {
                title.text = I18N.Text("#SysComDesc21");
            }
            else {
                title.text = I18N.FormatText("#SysComDesc26", target.PhaseVisual);
            }
            var template = costList[0];
            for (var n = costList.Count; n < cList.Count; ++n) {
                var obj = GameObject.Instantiate(template.gameObject);
                obj.transform.SetParent(template.transform.parent, false);
                var v = obj.GetComponent<BuildCost>();
                costList.Add(v);
            }
            for (var n = 0; n < cList.Count; ++n) {
                var cost = cList[n];
                var cell = costList[n];
                cell.gameObject.SetActive(true);
                cell.Refresh(cost.id, cost.current, cost.require, n == 0);
            }
            for (var n = cList.Count; n < costList.Count; ++n) {
                costList[n].gameObject.SetActive(false);
            }
            confirm.State(target_.CostReady, I18N.Text("#SysComBtn10"));
        }

        private void ConfirmClick() {
            if (redirect) {
                IEnumerator DelayRedirect() {
                    UIManager.Instance.Block(true);
                    yield return new WaitForSeconds(redirectDelay);
                    UIManager.Instance.Block(false);
                    var target = Game.Manager.mapSceneMan.ui.buildingUpgrade.ui.costGroup;
                    target.ConfirmClick();
                }
                Game.Instance.StartCoroutineGlobal(DelayRedirect());
                goto end;
            }
            var pos = confirm.transform.position;
            static bool Valid(Transform t_) => t_ != null && t_.gameObject.activeInHierarchy;
            var posL = Valid(fromSingle) ? fromSingle.position :
                       Valid(fromMulti) ? fromMulti.position :
                       pos;
            lockVisual = true;
            var count = target.costList.Count;
            if (!TryUpgrade(target, pos, posL, 152f)) goto end1;
            for(var n = 0; n < count; ++n) {
                costList[n].Tick(true);
            }
            confirm.State(false, "...");
        end:
            WhenConfirm?.Invoke();
        end1:
            lockVisual = false;
        }

        private void InfoClick() {
            UIManager.Instance.OpenWindow(UIConfig.UIMapSceneHelp);
        }
    }
}