using UnityEngine;

namespace FAT
{
    public class GuideActImpTalkCoordAuto : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (param.Length > 2)
            {
                float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x);
                float.TryParse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y);
                Game.Manager.guideMan.ActiveGuideContext?.ShowTalkByCoord
                (
                    EL.I18N.Text(param[2]),
                    new Vector2(x, y),
                    true
                );
                if (param.Length >= 4)
                    Game.Manager.guideMan.ActiveGuideContext?.SetHeadImage(param[3]);
                else
                    Game.Manager.guideMan.ActiveGuideContext?.SetHeadImage(null);
                if (param.Length >= 5)
                    Game.Manager.guideMan.ActiveGuideContext?.SetBgImage(param[4]);
                else
                    Game.Manager.guideMan.ActiveGuideContext?.SetBgImage(null);
                if (param.Length >= 6 && ColorUtility.TryParseHtmlString(param[5], out var color))
                    Game.Manager.guideMan.ActiveGuideContext?.SetTextColor(color);
                else
                    Game.Manager.guideMan.ActiveGuideContext?.SetTextColor(Color.clear);
            }
            mIsWaiting = false;
        }
    }
}