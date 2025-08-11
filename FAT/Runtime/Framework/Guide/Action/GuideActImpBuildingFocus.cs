/*
 * @Author: qun.chao
 * @Date: 2023-11-30 11:23:12
 */
using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class GuideActImpBuildingFocus : GuideActImpBase
    {
        // private void _OnBuildingSeleted()
        // {
        //     mIsWaiting = false;
        //     EL.MessageCenter.Get<MSG.MAP_FOCUS_POPUP>().RemoveListener(_OnBuildingSeleted);
        // }

        public override void Play(string[] param)
        {
            int bid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            float camSize = 0f;
            if (param.Length > 1)
            {
                float.TryParse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out camSize);
            }
            var mgr = Game.Manager.mapSceneMan;

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
                mgr.viewport.Focus(ret, camSize, 0f);
            }

            // 目前仅用于首次购买建筑1
            // 此action不打算通用
            // 所以此处顺便禁止建筑自动弹窗
            Game.Manager.mapSceneMan.interact.SkipFocusPopupOnce = true;

            mIsWaiting = false;
        }
    }
}