/*
 * @Author: qun.chao
 * @Date: 2020-10-29 11:06:23
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class WeeklyRaffleHelper : EditorWindow
    {
        Vector2 scrollPos;

        #region keep
        [SerializeField] private bool misc_fold_gm = true;
        #endregion

        [MenuItem("Tools/Activity Helper/WeeklyRaffle")]
        public static void ShowWindow()
        {
            GetWindow<WeeklyRaffleHelper>("WeeklyRaffle Helper");
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
            //
            // if (GUILayout.Button("open act"))
            // {
            //     Game.Manager.activity.DebugActivate("990467");
            // }

            EditorGUILayout.BeginVertical("Box");

            if (GUILayout.Button("change date to start"))
            {
                _OnBtnSetNetBias("2026-08-22 10:00:00");
                Game.Manager.decorateMan.ClearUnlock();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
                Game.Manager.accountMan.NextRefreshPlayDayUtc10 = 0;
                // GameProcedure.RestartGame();
            }
            
            if (GUILayout.Button("change date to close 5"))
            {
                _OnBtnSetNetBias("2026-08-29 09:59:55");
                Game.Manager.decorateMan.ClearUnlock();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
            }
            
            if (GUILayout.Button("change date to close 20"))
            {
                _OnBtnSetNetBias("2026-08-29 09:59:40");
                Game.Manager.decorateMan.ClearUnlock();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
            }


            EditorGUILayout.BeginHorizontal("Box");

            if (GUILayout.Button("add hour"))
            {
                _SetBiasOffset("1", 3600);
            }

            if (GUILayout.Button("change to 10"))
            {
                Game.Manager.activity.LookupAny(EventType.WeeklyRaffle, out var activity);
                if (activity == null || !(activity is ActivityWeeklyRaffle _activity))
                {
                    return;
                }

                var day = _activity.GetUtcOffsetTime();

                var utcTime = day.AddSeconds(3600 * 24 - 10);
                _OnBtnSetNetBias(utcTime.ToString(CultureInfo.InvariantCulture));
            }

            if (GUILayout.Button("add week"))
            {
                _SetBiasOffset("1", 3600 * 24 * 7);
            }

            if (GUILayout.Button("add month"))
            {
                _SetBiasOffset("1", 3600 * 24 * 30);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("Box");

            if (GUILayout.Button("add less day"))
            {
                _SetBiasOffset("1", 3600 * 24 - 5);
            }

            if (GUILayout.Button("add day"))
            {
                _SetBiasOffset("1", 3600 * 24);
            }

            if (GUILayout.Button("add 2 day"))
            {
                _SetBiasOffset("1", 3600 * 24 * 2);
            }

            if (GUILayout.Button("add 5 day"))
            {
                _SetBiasOffset("1", 3600 * 24 * 5);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("get today"))
            {
                Game.Manager.activity.LookupAny(EventType.WeeklyRaffle, out var activity);
                if (activity == null || !(activity is ActivityWeeklyRaffle _activity))
                {
                    return;
                }

                Debug.LogError(_activity.GetOffsetDay());
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();


            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal("Box");
            if (GUILayout.Button("log sign"))
            {
                Game.Manager.activity.LookupAny(EventType.WeeklyRaffle, out var activity);
                if (activity == null || !(activity is ActivityWeeklyRaffle _activity))
                {
                    return;
                }

                int count = _activity.GetSignCount();
                Debug.LogError($"sign count: {count}");

                int total = _activity.GetConfigSignDayCount();
                for (int i = 0; i < total; i++)
                {
                    if (_activity.HasSign(i))
                    {
                        Debug.LogError($"day: {i} , sign: true");
                    }
                }
            }

            if (GUILayout.Button("log box"))
            {
                Game.Manager.activity.LookupAny(EventType.WeeklyRaffle, out var activity);
                if (activity == null || !(activity is ActivityWeeklyRaffle _activity))
                {
                    return;
                }

                int count = _activity.GetBoxOpenCount();
                Debug.LogError($"open count: {count}");

                int total = _activity.GetConfigSignDayCount();
                for (int i = 0; i < total; i++)
                {
                    if (_activity.HasOpen(i))
                    {
                        Debug.LogError($"open: {i},");
                    }
                }
            }

            if (GUILayout.Button("log raffle"))
            {
                Game.Manager.activity.LookupAny(EventType.WeeklyRaffle, out var activity);
                if (activity == null || !(activity is ActivityWeeklyRaffle _activity))
                {
                    return;
                }

                int count = _activity.GetBoxOpenCount();
                Debug.LogError($"open count: {count}");

                var rewards = _activity.GetRaffleRewards();
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (_activity.HasRaffle(rewards[i].Id))
                    {
                        Debug.LogError($"reward: {rewards[i].Id},");
                    }
                }
            }

            if (GUILayout.Button("log level appear"))
            {
                Game.Manager.activity.LookupAny(EventType.WeeklyRaffle, out var activity);
                if (activity == null || !(activity is ActivityWeeklyRaffle _activity))
                {
                    return;
                }

                Debug.LogError($"LevelAppear: {_activity.LevelAppear}");
                Debug.LogError($"LevelNoAppear: {_activity.LevelNoAppear}");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();


            if (GUILayout.Button("open main"))
            {
                Game.Manager.activity.LookupAny(EventType.WeeklyRaffle, out var activity);
                if (activity == null || !(activity is ActivityWeeklyRaffle _activity))
                {
                    return;
                }

                _activity.Open();
            }

            if (GUILayout.Button("end act"))
            {
                Game.Manager.activity.LookupAny(EventType.WeeklyRaffle, out var activity);
                if (activity == null || !(activity is ActivityWeeklyRaffle _activity))
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