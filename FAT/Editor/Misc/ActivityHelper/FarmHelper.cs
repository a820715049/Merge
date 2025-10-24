/*
 * @Author: qun.chao
 * @Date: 2020-10-29 11:06:23
 */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using EventType = fat.rawdata.EventType;
using Object = UnityEngine.Object;

namespace FAT
{
    public class FarmHelper : EditorWindow
    {
        Vector2 scrollPos;

        #region keep
        [SerializeField] private bool misc_fold_gm = true;
        #endregion

        [MenuItem("Tools/Activity Helper/Farm")]
        public static void ShowWindow()
        {
            GetWindow<FarmHelper>("Farm Helper");
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

            if (GUILayout.Button("ping prefab"))
            {
                var path = "Assets/Bundle/event/bundle_farmboard_s002/UI";
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
            }

            if (GUILayout.Button("change date to start"))
            {
                _OnBtnSetNetBias("2026-07-28 10:00:00");
                Game.Manager.decorateMan.ClearUnlock();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
            }

            if (GUILayout.Button("open act s001"))
            {
                Game.Manager.activity.DebugActivate("8179");
            }

            if (GUILayout.Button("open act s002"))
            {
                Game.Manager.activity.DebugActivate("991025");
            }

            if (GUILayout.Button("open act s002 pack"))
            {
                Game.Manager.activity.DebugActivate("991026");
            }

            if (GUILayout.Button("add token"))
            {
                _OnBtnAddItem("66", "500");
                _OnBtnAddItem("87", "500");
            }
            
            if (GUILayout.Button("cloud"))
            {
                Game.Manager.activity.LookupAny(fat.rawdata.EventType.FarmBoard, out var activity);
                if (activity == null || !(activity is FarmBoardActivity _activity))
                {
                    return;
                }

                var ui = UIManager.Instance.TryGetUI(_activity.VisualBoard.res.ActiveR);
                if (ui != null && ui is UIFarmBoardMain main)
                {
                    var nextCloud = main.CloudHolder.GetNextCloud();

                    var idList = _activity.GetAllItemIdList();
                    var id = idList[nextCloud.UnlockLevel - 1];
                    _OnBtnAddItem(id.ToString(), "1");
                }
            }

            EditorGUILayout.BeginVertical("Box");

            #region time
            EditorGUILayout.BeginHorizontal("Box");

            if (GUILayout.Button("add hour"))
            {
                _SetBiasOffset("1", 3600);
            }

            if (GUILayout.Button("add day"))
            {
                _SetBiasOffset("1", 3600 * 24);
            }

            if (GUILayout.Button("add 7 day"))
            {
                _SetBiasOffset("1", 3600 * 24 * 7);
            }

            EditorGUILayout.EndHorizontal();
            #endregion


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
    }
}