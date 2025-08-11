using EL;
using TMPro;

namespace FAT {
    public class BuildingMaxed : BuildingUI {
        private TextMeshProUGUI title;
        private TextMeshProUGUI desc;
        private MapButton story;

        public override void Init() {
            var root = transform;
            root.Access("title", out title);
            root.Access("desc", out desc);
            root.Access("story", out story);
            root.Access("close", out MapButton close);
            story.WithClickScale().FixPivot().WhenClick = StoryClick;
            close.WithClickScale().FixPivot().WhenClick = Close;
        }

        public override void Refresh(IMapBuilding target_) {
            target = (MapBuilding)target_;
            title.text = I18N.Text(target.Name);
            desc.text = I18N.Text("#SysComDesc27");
            story.image.Enabled(target.AnyStory);
        }
    }
}