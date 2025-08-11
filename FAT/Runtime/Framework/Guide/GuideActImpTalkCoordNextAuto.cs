using System;
using UnityEngine;

namespace FAT
{
    public class GuideActImpTalkCoordNextAuto : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            mIsWaiting = true;
            Action callback = () =>
            {
                mIsWaiting = false;
                Game.Manager.audioMan.TriggerSound("CardActivityAwesome");
            };

            if (param.Length > 3)
            {
                float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x);
                float.TryParse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y);
                Game.Manager.guideMan.ActiveGuideContext?.ShowTalkByCoordWithBtn
                (
                    EL.I18N.Text(param[2]),
                    new Vector2(x, y),
                    EL.I18N.Text(param[3]),
                    callback,
                    true
                );
                if (param.Length >= 5)
                    Game.Manager.guideMan.ActiveGuideContext?.SetHeadImage(param[4]);
                else
                    Game.Manager.guideMan.ActiveGuideContext?.SetHeadImage(null);
                if (param.Length >= 6)
                    Game.Manager.guideMan.ActiveGuideContext?.SetBgImage(param[5]);
                else
                    Game.Manager.guideMan.ActiveGuideContext?.SetBgImage(null);
                if (param.Length >= 7 && ColorUtility.TryParseHtmlString(param[6], out var color))
                    Game.Manager.guideMan.ActiveGuideContext?.SetTextColor(color);
                else
                    Game.Manager.guideMan.ActiveGuideContext?.SetTextColor(Color.clear);
            }
        }
    }
}