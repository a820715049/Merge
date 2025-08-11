/*
 * @Author: qun.chao
 * @Date: 2022-12-19 12:45:09
 */

using System.Linq;
using UnityEngine;

namespace FAT
{
    public class GuideActImpMaskRectShow : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            float.TryParse(param[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var size);
            var trans = Game.Manager.guideMan.FindByPath(param.Skip(1).ToList());
            if (trans != null)
            {
                Game.Manager.guideMan.ActionShowRectMask(trans, size);
            }

            mIsWaiting = false;
        }
    }
}