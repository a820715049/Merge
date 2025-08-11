using TMPro;

namespace FAT {
    public class BuildingMystery : BuildingUI {
        private TextMeshProUGUI title;
        private TextMeshProUGUI desc;
        private UIImageRes imgIcon;

        public override void Init() {
            var root = transform;
            root.Access("title", out title);
            root.Access("desc", out desc);
            root.Access("icon", out imgIcon);
            root.Access("close", out MapButton close);
            close.WithClickScale().FixPivot().WhenClick = Close;
        }

        public override void Refresh(IMapBuilding target_) {
            target = (MapBuilding)target_;
        }
    }
}