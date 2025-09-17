// ================================================
// File: GuideActImpHandProTrainSpawner.cs
// Author: yueran.li
// Date: 2025/08/22 16:14:38 星期五
// Desc: 火车新手引导 手指生成器
// ================================================

using fat.conf;
using UnityEngine;
using FAT.Merge;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class GuideActImpHandProTrainSpawner : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            int findId = -1;

            // 活动开启
            if (Game.Manager.activity.LookupAny(EventType.TrainMission) is not TrainMissionActivity _activity)
            {
                mIsWaiting = false;
                return;
            }

            // 打开主界面
            var mainConfig = _activity.VisualMain.res.ActiveR ?? UIConfig.UITrainMissionMain;
            var ui = UIManager.Instance.TryGetUI(mainConfig);
            if (ui == null || ui is not UITrainMissionMain main)
            {
                mIsWaiting = false;
                return;
            }

            var challengeIDList = TrainGroupDetailVisitor.Get(_activity.groupDetailID)?.IncludeChallenge;
            if (challengeIDList == null)
            {
                mIsWaiting = false;
                return;
            }

            // 刷新生成器
            foreach (var challengeID in challengeIDList)
            {
                var challenge = TrainChallengeVisitor.Get(challengeID);
                if (challenge == null)
                {
                    continue;
                }

                foreach (var linkId in challenge.ConnectSpawner)
                {
                    var spawnerId = BoardActivityUtility.GetHighestLevelItemIdInCategory(linkId, 1);
                    if (TrainMissionUtility.HasActiveItemInMainBoard(spawnerId))
                    {
                        findId = spawnerId;
                        break;
                    }
                }
            }


            Item item = BoardViewManager.Instance.FindItem(findId, false);
            var target = BoardViewManager.Instance.boardView.boardHolder.FindItemView(item.id);
            var trans = target != null ? target.transform : null;

            float.TryParse(param[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var angle);
            bool block = param[1].Contains("true");
            bool mask = param[2].Contains("true");
            float.TryParse(param[3], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var offset);

            if (trans != null)
            {
                Game.Manager.guideMan.ActiveGuideContext?.ShowPointerPro(trans, block, mask, _StopWait, offset);
                Game.Manager.guideMan.ActiveGuideContext?.SetAngleOffset(angle);
            }
            else
            {
                _StopWait();
                Debug.LogError("[GUIDE] hand_pro path fail");
            }
        }
    }
}