// ==================================================
// // File: TrainMissionUtility.cs
// // Author: liyueran
// // Date: 2025-07-29 10:07:25
// // Desc: $火车棋盘工具类
// // ==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using EL;
using fat.conf;
using fat.rawdata;
using FAT.Merge;

namespace FAT
{
    public class TrainMissionUtility
    {
        public static bool IsLoading { get; private set; }
        private static TrainMissionActivity actInst;
        private static bool isEnterFromMerge;

        public static bool TryGetEventInst(out TrainMissionActivity act)
        {
            act = actInst;
            if (act == null)
            {
                DebugEx.Error($"[TrainMission] activity inst not found");
            }

            return act != null;
        }


        public static bool IsEventActive()
        {
            return actInst != null && actInst.Active;
        }

        private static void _RegisterActivityInstance()
        {
            actInst = Game.Manager.activity.LookupAny(fat.rawdata.EventType.TrainMission) as TrainMissionActivity;
        }

        private static void _UnregisterActivityInstance()
        {
            actInst = null;
        }

        #region 界面
        public static void EnterActivity()
        {
            _RegisterActivityInstance();
            if (!TryGetEventInst(out var act))
                return;
            if (!IsEventActive())
                return;

            Game.Manager.screenPopup.Block(delay_: true);
            UIManager.Instance.ChangeIdleActionState(false);
            Game.Instance.StartCoroutineGlobal(_CoLoading(_MergeToActivity, () =>
            {
                var ui = UIManager.Instance.TryGetUI(actInst.VisualMain.res.ActiveR);
                if (ui != null && ui is UITrainMissionMain main)
                {
                    if (actInst.waitEnterNextChallenge)
                    {
                        UIManager.Instance.OpenWindow(actInst.VisualComplete.res.ActiveR, actInst, main);
                    }

                    if (actInst.waitRecycle)
                    {
                        main.StartRecycle();
                    }
                }
            }));
        }

        public static void LeaveActivity()
        {
            Game.Instance.StartCoroutineGlobal(_CoLoading(_ActivityToMerge));
        }

        public static void EnterNextChallenge()
        {
            _RegisterActivityInstance();
            if (!TryGetEventInst(out var act))
                return;
            if (!IsEventActive())
                return;
            Game.Instance.StartCoroutineGlobal(_CoLoading(_EnterNextChallenge));
        }

        private static void _EnterNextChallenge()
        {
            actInst.EnterNextChallenge();

            actInst.VisualMain.res.ActiveR.Close();
            actInst.VisualMain.res.ActiveR.Open(actInst);
        }

        private static void _MergeToActivity()
        {
            isEnterFromMerge = UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain);


            if (isEnterFromMerge)
            {
                UIConfig.UIMergeBoardMain.Close();
            }
            else
            {
                Game.Manager.mapSceneMan.Exit();
            }

            actInst.VisualMain.res.ActiveR.Open(actInst);
        }

        private static void _ActivityToMerge()
        {
            actInst.VisualMain.res.ActiveR.Close();

            if (isEnterFromMerge)
            {
                UIConfig.UIMergeBoardMain.Open();
            }
            else
            {
                GameProcedure.SceneToMerge(); // 默认返回主棋盘
            }

            if (!TryGetEventInst(out var act)) return;

            Game.Manager.screenPopup.Block(false, false);
            UIManager.Instance.ChangeIdleActionState(true);
            _UnregisterActivityInstance();
        }

        private static IEnumerator _CoLoading(Action afterFadeIn = null, Action afterFadeOut = null)
        {
            IsLoading = true;

            var waitFadeInEnd = new SimpleAsyncTask();
            var waitFadeOutEnd = new SimpleAsyncTask();
            var waitLoadingJobFinish = new SimpleAsyncTask();

            //复用寻宝loading音效
            Game.Manager.audioMan.TriggerSound("UnderseaTreasure");

            actInst.VisualLoading.res.ActiveR.Open(waitLoadingJobFinish, waitFadeInEnd, waitFadeOutEnd);

            yield return waitFadeInEnd;

            afterFadeIn?.Invoke();

            waitLoadingJobFinish.ResolveTaskSuccess();

            yield return waitFadeOutEnd;

            afterFadeOut?.Invoke();

            IsLoading = false;
        }
        #endregion

        public static bool HasBoardItem()
        {
            var hasItem = false;
            var cat1 = Game.Manager.mergeItemMan.GetCategoryConfig(actInst.trainChallenge.ConnectSpawner[0]);
            var cat2 = Game.Manager.mergeItemMan.GetCategoryConfig(actInst.trainChallenge.ConnectSpawner[1]);
            actInst.World.activeBoard.WalkAllItem((Item item) =>
            {
                if (!cat1.Progress.Contains(item.tid) && !cat2.Progress.Contains(item.tid))
                {
                    hasItem = true;
                }
            });
            return hasItem;
        }

        /// <summary>
        /// 判断主棋盘存在item
        /// </summary>
        public static bool HasActiveItemInMainBoard(int itemId)
        {
            _RegisterActivityInstance();
            if (!TryGetEventInst(out var _))
            {
                return false;
            }

            return actInst.WorldTracer.GetCurrentActiveBoardItemCount().ContainsKey(itemId);
        }

        /// <summary>
        /// 判断主棋盘和背包存在item
        /// </summary>
        public static bool HasActiveItemInMainBoardAndInventory(int itemId)
        {
            _RegisterActivityInstance();
            if (!TryGetEventInst(out var _))
            {
                return false;
            }

            return actInst.WorldTracer.GetCurrentActiveBoardAndInventoryItemCount().ContainsKey(itemId);
        }
    }

    public class TrainMissionOrder
    {
        public int orderID;
        public int complete;
        public List<TrainMissionItemInfo> ItemInfos = new();
        public List<RewardConfig> rewardConfigs = new();
        public List<SpecialMissionInfo> specialMissionInfos = new();
        public TrainMission config;
        public int startTime;

        public void SetData(int id, int complete, int startTime)
        {
            orderID = id;
            this.complete = complete;
            config = TrainMissionVisitor.Get(id);
            this.startTime = startTime;
            SetItemInfos();
            SetRewardConfigs();
            SetSpecialInfos();
        }

        public void SetItemInfos()
        {
            if (config == null)
            {
                return;
            }

            ItemInfos.Clear();
            foreach (var info in config.ItemInfo)
            {
                ItemInfos.Add(new TrainMissionItemInfo(info.ConvertToInt3()));
            }
        }

        public void SetRewardConfigs()
        {
            if (config == null)
            {
                return;
            }

            rewardConfigs.Clear();
            foreach (var info in config.Reward)
            {
                rewardConfigs.Add(info.ConvertToRewardConfig());
            }
        }

        public void SetSpecialInfos()
        {
            if (config == null)
            {
                return;
            }

            specialMissionInfos.Clear();
            foreach (var info in config.SpecialMissionInfo)
            {
                var item = info.ConvertToInt4();
                specialMissionInfos.Add(new SpecialMissionInfo()
                {
                    orderIndex = item.Item1, rewardID = item.Item2, rewardCount = item.Item3, duration = item.Item4
                });
            }
        }

        public void Resetorder()
        {
            orderID = 0;
            complete = 0;
            ItemInfos.Clear();
            rewardConfigs = null;
        }

        public bool CheckAllFinish()
        {
            for (var index = 0; index < ItemInfos.Count; index++)
            {
                if ((complete & 1 << index) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryGetSpecialMission(int index, out int endTime, out SpecialMissionInfo info)
        {
            info = specialMissionInfos.FirstOrDefault(e => e.orderIndex - 1 == index);
            endTime = startTime + info?.duration ?? 0;
            return info != null;
        }
    }

    public class TrainMissionItemInfo
    {
        public int itemID;
        public int rewardID;
        public int rewardCount;

        public TrainMissionItemInfo((int, int, int) info)
        {
            (itemID, rewardID, rewardCount) = info;
        }
    }

    public class SpecialMissionInfo
    {
        public int orderIndex;
        public int rewardID;
        public int rewardCount;
        public int duration;
    }
}