using EL;
using TMPro;

namespace FAT {
    public class BuildingPlace : BuildingUI {
        private TextMeshProUGUI title;
        private BuildCost cost;
        private MapButton confirm;
        private new MapBuildingForEvent target;

        public override void Init() {
            var root = transform;
            root.Access("title", out title);
            root.Access("cost", out cost);
            root.Access("confirm", out confirm);
            root.Access("close", out MapButton close);
            confirm.WithClickScale().FixPivot().WhenClick = ConfirmClick;
            close.WithClickScale().FixPivot().WhenClick = Clear;
        }

        internal override void WillOpen() {
            Block(true);
        }

        internal override void WhenOpen() {
            Block(false);
        }

        internal override void WillClose() {
            base.WillClose();
            Game.Manager.mapSceneMan.interact.ClearSelect(target);
            target.asset.Preview(false);
            target.asset.PauseSpineAnim(false);
        }

        public override void RefreshView(IMapBuilding target_) {
            Scale(target_.info.zoom, Game.Manager.mapSceneMan.viewport.setting.zoomFocus);
        }

        public override void Refresh(IMapBuilding target_) {
            target = (MapBuildingForEvent)target_;
            target.asset.SetActive(true);
            target.asset.Preview(true);
            target.asset.PauseSpineAnim(true);
            Game.Manager.audioMan.TriggerSound("DecoratePreview");
            title.text = I18N.Text(target.Name);
            var cost0 = target.cost;
            cost.Refresh(cost0.id, cost0.current, cost0.require, true);
            confirm.State(target.CostReady);
        }

        private void ConfirmClick() {
            if (!TryPlace(target) || !TryConclude()) return;
            Game.Manager.audioMan.TriggerSound("DecoratePlaceClick");
            target.asset.Preview(false);
            target.asset.PauseSpineAnim(false);
            confirm.State(false, "...");
        }

        public bool TryPlace(MapBuildingForEvent target_) {
            if (!target_.TryUpgrade()) return false;
            var mgr = Game.Manager.mapSceneMan;
            var pos = confirm.transform.position;
            Game.Instance.StartCoroutineGlobal(mgr.PlaceVisual(target_, pos));
            return true;
        }

        public override void Clear() {
            if (!TryConclude()) return;
            Close();
            target.RefreshState();
            var uiMgr = UIManager.Instance;
            uiMgr.Block(true);
            Game.Manager.mapSceneMan.TryFocus(target.info.area, WhenFocus_: () => {
                uiMgr.Block(false);
                UIManager.Instance.Visible(Game.Manager.decorateMan.Panel, true);
            });
        }
    }
}