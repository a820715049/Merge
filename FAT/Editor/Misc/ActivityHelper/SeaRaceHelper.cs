/*
 * @Author: qun.chao
 * @Date: 2020-10-29 11:06:23
 * ai review
 */

using System;
using System.Collections.Generic;
using System.Text;
using EL;
using FAT.MSG;
using fat.rawdata;
using UnityEditor;
using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class SeaRaceHelper : EditorWindow
    {
        Vector2 scrollPos;

        #region keep
        [SerializeField] private bool misc_fold_gm = true;
        #endregion

        [MenuItem("Tools/Activity Helper/SeaRace")]
        public static void ShowWindow()
        {
            GetWindow<SeaRaceHelper>("SeaRace Helper");
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

            if (GUILayout.Button("change date to start"))
            {
                _OnBtnSetNetBias("2027-07-15 10:00:00");
                Game.Manager.decorateMan.ClearUnlock();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
            }

            if (GUILayout.Button("open act"))
            {
                Game.Manager.activity.DebugActivate("991036");
            }


            EditorGUILayout.BeginVertical("Box");

            #region time
            EditorGUILayout.BeginHorizontal("Box");

            if (GUILayout.Button("add 5 min"))
            {
                _SetBiasOffset("1", 300);
            }

            if (GUILayout.Button("add 10 min"))
            {
                _SetBiasOffset("1", 600);
            }

            if (GUILayout.Button("add hour"))
            {
                _SetBiasOffset("1", 3600);
            }

            if (GUILayout.Button("add day"))
            {
                _SetBiasOffset("1", 3600 * 24);
            }

            EditorGUILayout.EndHorizontal();
            #endregion


            EditorGUILayout.BeginHorizontal("Box");

            if (GUILayout.Button("log"))
            {
                var act = Game.Manager.activity.LookupAny(EventType.SeaRace);
                if (act != null && act is ActivitySeaRace seaRace)
                {
                    seaRace.LogState();
                }
            }

            if (GUILayout.Button("log cache"))
            {
                var act = Game.Manager.activity.LookupAny(EventType.SeaRace);
                if (act != null && act is ActivitySeaRace seaRace)
                {
                    seaRace.LogCache();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal("Box");

            if (GUILayout.Button("Start"))
            {
                var act = Game.Manager.activity.LookupAny(EventType.SeaRace);
                if (act != null && act is ActivitySeaRace seaRace)
                {
                    seaRace.DebugStart();
                }
            }

            if (GUILayout.Button("refresh cache"))
            {
                var act = Game.Manager.activity.LookupAny(EventType.SeaRace);
                if (act != null && act is ActivitySeaRace seaRace)
                {
                    seaRace.DebugRefreshCache();
                }
            }

            if (GUILayout.Button("add coin"))
            {
                var act = Game.Manager.activity.LookupAny(EventType.SeaRace);
                if (act != null && act is ActivitySeaRace seaRace)
                {
                    _OnBtnAddCoin(CoinType.MergeCoin, 4000);
                }
            }
            
            
            if (GUILayout.Button("add round"))
            {
                var act = Game.Manager.activity.LookupAny(EventType.SeaRace);
                if (act != null && act is ActivitySeaRace seaRace)
                {
                    seaRace.AddRound();
                }
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space();


            EditorGUILayout.EndVertical();
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

        private void _OnBtnAddCoin(CoinType ct, int count)
        {
            Game.Manager.coinMan.AddCoin(ct, count, ReasonString.cheat);
        }
    }
}