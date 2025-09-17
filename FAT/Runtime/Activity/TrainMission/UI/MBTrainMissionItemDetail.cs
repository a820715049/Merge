// ==================================================
// // File: MBTrainMissionItemDetail.cs
// // Author: liyueran
// // Date: 2025-08-08 14:08:22
// // Desc: $
// // ==================================================

using UnityEngine;

namespace FAT
{
    public class MBTrainMissionItemDetail : MBBoardItemDetail
    {
        private int BoostNameColorId() => 4;

        private Color _nameColor = new Color(115f / 255f, 67f / 255f, 41f / 255f);
        private int BoostDescColorId() => 6;

        private Color _descColor = new Color(173f / 255f, 116f / 255f, 85f / 255f);

        public override void InitOnPreOpen()
        {
            base.InitOnPreOpen();
            var actInst = Game.Manager.activity.LookupAny(fat.rawdata.EventType.TrainMission) as TrainMissionActivity;
            if (actInst != null)
            {
                // 获得配置的颜色
                if (actInst.VisualMain.visual.Theme.StyleInfo.TryGetValue("name", out var nameStyle))
                {
                    ColorUtility.TryParseHtmlString(nameStyle, out _nameColor);
                }

                if (actInst.VisualMain.visual.Theme.StyleInfo.TryGetValue("desc", out var descStyle))
                {
                    ColorUtility.TryParseHtmlString(descStyle, out _descColor);
                }
            }
        }

        protected override string BuildTextFormat(bool boost)
        {
            var config1 = FontMaterialRes.Instance.GetFontMatResConf(BoostNameColorId());
            var config2 = FontMaterialRes.Instance.GetFontMatResConf(BoostDescColorId());
            var col1 = ColorUtility.ToHtmlStringRGB(boost ? config1.color : _nameColor);
            var col2 = ColorUtility.ToHtmlStringRGB(boost ? config2.color : _descColor);
            return $"<color=#{col1}>{{0}}: </color><color=#{col2}>{{1}}</color>";
        }
    }
}