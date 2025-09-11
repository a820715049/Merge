/**FileHeader
 * @Author: zhangpengjian
 * @Date: 2025/9/1 15:01:27
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/9/1 15:01:33
 * @Description: 拼图活动 添加拼图碎片奖励
 * @Copyright: Copyright (©)}) 2025 zhangpengjian. All rights reserved.
 * @Email: xxx@xxx.com
 */

using UnityEngine;

namespace FAT
{
    public class GuideActImpAddPuzzlePieceReward : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var objID);
            float.TryParse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var count);
            var data = Game.Manager.rewardMan.BeginReward((int)objID, (int)count, ReasonString.guide_energy4x);
            Game.Manager.rewardMan.CommitReward(data);
            mIsWaiting = false;
        }
    }
}