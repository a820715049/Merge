using EL;
using UnityEngine;

namespace FAT
{
    public class GuideActImpFindDecorateEntry : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            if (param.Length < 3)
            {
                _StopWait();
                return;
            }

            float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var angle);
            bool block = param[1].Contains("true");
            bool mask = param[2].Contains("true");
            float.TryParse(param[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var offset);

            Transform trans = null;

            void StartGuide()
            {
                var act = Game.Manager.decorateMan.Activity.Entry?.Entry.obj.activeInHierarchy;
                if (act == null)
                    Game.Manager.guideMan.DropGuide();
                else if (act == false)
                    Game.Manager.guideMan.DropGuide();
                trans = Game.Manager.decorateMan.Activity.Entry?.Entry.obj.transform;
                Game.Manager.guideMan.ActiveGuideContext?.ShowPointerPro(trans, block, mask, _StopWait, offset);
                Game.Manager.guideMan.ActiveGuideContext?.SetAngleOffset(angle);
                MessageCenter.Get<MSG.DECORATE_GUIDE_READY>().RemoveListener(StartGuide);
            }
            if(Game.Manager.decorateMan.GetCloudState())
                MessageCenter.Get<MSG.DECORATE_GUIDE_READY>().AddListener(StartGuide);
            else
            {
                StartGuide();
            }
        }
    }
}