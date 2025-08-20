/*
 * @Author: qun.chao
 * @Date: 2020-10-29 11:06:23
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using EL;
using FAT.MSG;
using fat.rawdata;
using UnityEditor;
using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class BingoTaskHelper : EditorWindow
    {
        Vector2 scrollPos;

        #region keep
        [SerializeField] private bool misc_fold_gm = true;
        #endregion

        [MenuItem("Tools/Activity Helper/BingoTask")]
        public static void ShowWindow()
        {
            GetWindow<BingoTaskHelper>("BingoTask Helper");
        }

        void OnGUI()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, false);

            _UpdateGM();

            GUILayout.Space(10);
            if (GUILayout.Button("Get Selected Object Path"))
            {
                GetSelectedObjectPath();
            }

            GUILayout.EndScrollView();
        }

        #region GM
        private void _UpdateGM()
        {
            EditorGUILayout.BeginVertical("Button");
            misc_fold_gm = EditorGUILayout.Foldout(misc_fold_gm, "GM");
            if (!misc_fold_gm)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            if (GUILayout.Button("open act"))
            {
                Game.Manager.activity.DebugActivate("990943");
            }

            EditorGUILayout.BeginVertical("Box");


            if (GUILayout.Button("change date to start"))
            {
                _OnBtnSetNetBias("2025-07-17 10:00:00");
                Game.Manager.decorateMan.ClearUnlock();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
                Game.Manager.accountMan.NextRefreshPlayDayUtc10 = 0;
            }


            EditorGUILayout.BeginHorizontal("Box");

            if (GUILayout.Button("add hour"))
            {
                _SetBiasOffset("1", 3600);
            }

            if (GUILayout.Button("add day"))
            {
                _SetBiasOffset("1", 3600 * 24);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("Box");
            if (GUILayout.Button("coin 10000"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCoin, 10000);
                }
            }

            if (GUILayout.Button("order 5"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskOrder, 5);
                }
            }

            if (GUILayout.Button("open card 5"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCardPack, 5);
                }
            }

            if (GUILayout.Button("merge 200"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskMerge, 200);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("Box");
            if (GUILayout.Button("bubble 2"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskBubble, 2);
                }
            }

            if (GUILayout.Button("get token 100"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskTokenGet, 100);
                }
            }

            if (GUILayout.Button("cost token 100"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskTokenCost, 100);
                }
            }

            if (GUILayout.Button("like 5"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskOrderLike, 5);
                }
            }

            if (GUILayout.Button("get card 5"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCardNum, 5);
                }
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            if (GUILayout.Button("task all"))
            {
                if (Game.Manager.activity.LookupAny(EventType.BingoTask, out var like) &&
                    like is ActivityBingoTask _activity)
                {
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCoin, 100000);
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskOrder, 50);
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCardPack, 50);
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskCardNum, 50);
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskMerge, 1000);
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskBubble, 50);
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskTokenGet, 500);
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskTokenCost, 500);
                    _activity.bingoTaskMap.UpdateCellScoreAll((int)EventBingoTaskType.BingoTaskOrderLike, 50);
                }
            }

            if (GUILayout.Button("end act"))
            {
                Game.Manager.activity.LookupAny(EventType.BingoTask, out var activity);
                if (activity == null || !(activity is ActivityBingoTask _activity))
                {
                    return;
                }

                Game.Manager.activity.EndImmediate(_activity, false);
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        private void _OnBtnAddItem(string str, string strCount)
        {
            if (strCount.StartsWith("m")) strCount = strCount.Substring(1);

            if (!int.TryParse(strCount, out var count)) count = 1;

            var isNumber = ulong.TryParse(str, out var id);
            if (isNumber)
            {
                var rewards = new List<RewardCommitData>()
                    { Game.Manager.rewardMan.BeginReward((int)id, count, ReasonString.cheat) };
                UIFlyUtility.FlyRewardList(rewards, Vector3.zero);
            }
        }

        private void _OnBtnSetNetBias(string str)
        {
            if (!long.TryParse(str, out var bias))
            {
                // 按照日期解析
                if (DateTime.TryParse(str, out var dt))
                {
                    var tar = TimeUtility.GetSecondsSinceEpoch(dt.Ticks);
                    bias = tar - Game.Instance.GetTimestampSeconds() + Game.Manager.networkMan.networkBias;
                }
                else
                {
                    return;
                }
            }

            Game.Manager.networkMan.DebugSetNetworkBias(bias);
        }

        private void _SetBiasOffset(string strCount, long offset)
        {
            if (!long.TryParse(strCount, out var count))
                // 数量1
                count = 1;

            var bias = Game.Manager.networkMan.debugBias;
            Game.Manager.networkMan.DebugSetNetworkBias(bias + offset * count);
        }

        private void GetSelectedObjectPath()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("No object selected!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (var obj in Selection.gameObjects)
            {
                string path = GetGameObjectPath(obj);
                sb.AppendLine($"Object: {obj.name}");
                sb.AppendLine($"Path: {path}");
                sb.AppendLine("-------------------");
            }

            Debug.LogError(sb.ToString());
        }

        private string GetGameObjectPath(GameObject obj)
        {
            StringBuilder path = new StringBuilder();
            Transform current = obj.transform;

            while (current != null)
            {
                if (path.Length > 0)
                {
                    path.Insert(0, "/");
                }

                path.Insert(0, current.name);
                current = current.parent;
            }

            return path.ToString();
        }
    }
}