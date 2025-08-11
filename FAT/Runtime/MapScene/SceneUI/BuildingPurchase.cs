using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT {
    public class BuildingPurchase : BuildingUI {
        private TextMeshProUGUI title;
        private TextMeshProUGUI desc;
        private Image imgFrame;
        private UIImageRes imgIcon;
        private MapButton confirm;
        public float heightWithReward;
        public float heightWithoutReward;
        private bool anyReward;

        public override void Init() {
            var root = transform;
            root.Access("title", out title);
            root.Access("desc", out desc);
            root.Access("frame", out imgFrame);
            root.Access("icon", out imgIcon);
            root.Access("confirm", out confirm);
            root.Access("close", out MapButton close);
            confirm.WithClickScale().FixPivot().WhenClick = ConfirmClick;
            close.WithClickScale().FixPivot().WhenClick = Close;
        }

        public override void Refresh(IMapBuilding target_) {
            target = (MapBuilding)target_;
            title.text = I18N.Text(target.Name);
            var objMan = Game.Manager.objectMan;
            var rList = target.rewardList;
            anyReward = rList.Count > 0;
            imgFrame.enabled = anyReward;
            imgIcon.Hide();
            if (anyReward) {
                Resize(heightWithReward);
                var r = rList[0];
                var id = r.Id;
                var bConf = objMan.GetBasicConfig(id);
                imgIcon.SetImage(bConf.Icon);
                desc.text = I18N.Text($"#SysComDesc23");
            }
            else {
                Resize(heightWithoutReward);
                desc.text = I18N.Text($"#SysComDesc22");
            }
            var cList = target.costList;
            var cost0 = cList[0];
            var text = $"{cost0.require}{TextSprite.CoinL}";
            confirm.State(target.CostReady, text);
        }

        private void ConfirmClick() {
            var pos = confirm.transform.position;
            var posL = imgIcon.transform.position;
            if (!TryUpgrade(target, pos, posL, 220f)) return;
            confirm.State(false, "...");
        }
    }
}