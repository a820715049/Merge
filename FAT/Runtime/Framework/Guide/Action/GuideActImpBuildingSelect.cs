/*
 * @Author: qun.chao
 * @Date: 2023-12-06 11:18:06
 */
using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class GuideActImpBuildingSelect : GuideActImpBase
    {
        private void _OnBuildingSeleted()
        {
            mIsWaiting = false;
            EL.MessageCenter.Get<MSG.MAP_FOCUS_POPUP>().RemoveListener(_OnBuildingSeleted);
        }

        public override void Play(string[] param)
        {
            int bid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            var mgr = Game.Manager.mapSceneMan;
            var camMoveDuration = 0.8f;
            if (param.Length > 1)
            {
                float.TryParse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out camMoveDuration);
            }

            MapBuilding ret = null;
            foreach (var b in mgr.scene.building)
            {
                if (b.Id == bid)
                {
                    ret = b;
                    break;
                }
            }

            if (ret != null)
            {
                EL.MessageCenter.Get<MSG.MAP_FOCUS_POPUP>().AddListener(_OnBuildingSeleted);
                mgr.interact.Select(mgr.viewport, ret, -1, camMoveDuration, true, true);
            }
            else
            {
                mIsWaiting = false;
            }
        }
    }
}