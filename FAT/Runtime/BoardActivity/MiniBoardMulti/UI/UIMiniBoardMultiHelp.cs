using System;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIMiniBoardMultiHelp : UIBase
    {
        private UIImageRes _img1;
        private UIImageRes _img2;
        private UIImageRes _img3;
        private TextMeshProUGUI _desc;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Close", Close);
            transform.Access("Content/Img1", out _img1);
            transform.Access("Content/Img2", out _img2);
            transform.Access("Content/Img3", out _img3);
            transform.Access("Content/Desc", out _desc);
        }

        protected override void OnPreOpen()
        {
            if (Game.Manager.miniBoardMultiMan.GetCurInfoConfig() != null)
            {
                var cfg = Game.Manager.objectMan.GetBasicConfig(Game.Manager.miniBoardMultiMan.GetCurInfoConfig()
                    .LevelItem[0]);
                var sprite = "<sprite name=\"" + cfg.Icon.ConvertToAssetConfig().Asset + "\">";
                _desc.text = I18N.FormatText("#SysComDesc484", sprite);
            }
        }
    }
}