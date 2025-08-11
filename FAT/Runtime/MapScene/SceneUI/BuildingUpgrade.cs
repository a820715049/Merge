using UnityEngine;
using EL;
using TMPro;
using DG.Tweening;

namespace FAT {
    public class BuildingUpgrade : BuildingUI {
        public float progressDuration = 1.2f;
        private TextMeshProUGUI title;
        private TextMeshProUGUI level;
        private TextMeshProUGUI desc;
        private GameObject groupSingle;
        private GameObject groupMulti;
        private UIImageRes imgIconSingle;
        private UIImageRes imgIconMulti;
        private RectTransform bar;
        private float barSize;
        private TextMeshProUGUI barText;
        internal BuildCostGroup costGroup;
        private MapButton story;
        public float heightWithReward;
        public float heightWithRewardBar;
        public float heightWithoutReward;

        private int phase;
        private int phaseCount;

        public override void Init() {
            var root = transform;
            root.Access("title", out title);
            root.Access("level", out level);
            root.Access("desc", out desc);
            root.Access("story", out story);
            root.Access("close", out MapButton close);
            groupSingle = root.TryFind("single");
            groupMulti = root.TryFind("multi");
            root.Access("_cost", out costGroup);
            root = groupSingle.transform;
            root.Access("icon", out imgIconSingle);
            root = groupMulti.transform;
            root.Access("icon", out imgIconMulti);
            root.Access("progress", out bar);
            barSize = bar.sizeDelta.x;
            root.Access("progress_text", out barText);
            story.WithClickScale().FixPivot().WhenClick = StoryClick;
            close.WithClickScale().FixPivot().WhenClick = Close;
            costGroup.Init(WhenConfirm);
        }

        public override void Refresh(IMapBuilding target_) {
            target = (MapBuilding)target_;
            title.text = I18N.Text(target.Name);
            level.text = I18N.FormatText("#SysComDesc18", $"{target.DisplayLevel}/{target.MaxLevel}");
            var objMan = Game.Manager.objectMan;
            var cList = target.costList;
            phaseCount = target.PhaseCount;
            var singleCost = phaseCount == 1;
            groupSingle.SetActive(singleCost);
            groupMulti.SetActive(!singleCost);
            var rList = target.rewardList;
            var anyReward = cList.Count > 0;
            string rewardIcon = null;
            if (anyReward) {
                Resize(singleCost ? heightWithReward : heightWithRewardBar);
                var reward0 = rList[0];
                var bConf = objMan.GetBasicConfig(reward0.Id);
                rewardIcon = bConf.Icon;
            }
            else {
                Resize(heightWithoutReward);
            }
            if (singleCost) {
                desc.text = I18N.Text("#SysComDesc24");
                imgIconSingle.Hide();
                if (rewardIcon != null) imgIconSingle.SetImage(rewardIcon);
            }
            else {
                desc.text = I18N.FormatText("#SysComDesc25", target.PhaseVisual);
                imgIconMulti.Hide();
                if (rewardIcon != null) imgIconMulti.SetImage(rewardIcon);
                phase = target.Phase;
                RefreshProgress(phase, phaseCount);
                RefreshProgressText(phase, phaseCount);
            }
            costGroup.Refresh(target);
            story.image.Enabled(target.AnyStory);
        }

        private void RefreshProgress(float v, float max, float duration = 0, TweenCallback WhenComplete_ = null) {
            var size = bar.sizeDelta;
            var target = new Vector2(Mathf.Clamp01(v / max) * barSize, size.y);
            if (duration > 0) bar.DOSizeDelta(target, duration).OnComplete(WhenComplete_);
            else bar.sizeDelta = target;
        }

        private void RefreshProgressText(int v, int max) {
            barText.text = $"{v}/{max}";
        }

        private void WhenConfirm() {
            if (phaseCount <= 1) return;
            ++phase;
            RefreshProgress(phase, phaseCount, progressDuration, () => RefreshProgressText(phase, phaseCount));
        }
    }
}