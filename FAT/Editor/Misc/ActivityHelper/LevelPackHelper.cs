/*
 * @Author: qun.chao
 * @Date: 2020-10-29 11:06:23
 */

using System;
using System.Collections;
using System.Text;
using FAT.Merge;
using UnityEditor;
using UnityEngine;
using EventType = fat.rawdata.EventType;
using Object = UnityEngine.Object;

namespace FAT
{
    public class LevelPackHelper : EditorWindow
    {
        Vector2 scrollPos;

        #region keep
        [SerializeField] private bool misc_fold_gm = true;
        #endregion

        [MenuItem("Tools/Activity Helper/LevelPack")]
        public static void ShowWindow()
        {
            GetWindow<LevelPackHelper>("LevelPack Helper");
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
                var path = "Assets/Bundle/event/bundle_levelpack_s001/prefab";
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
            }

            if (GUILayout.Button("open act"))
            {
                Game.Manager.activity.DebugActivate("991023");
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

            if (GUILayout.Button("add 30 day"))
            {
                _SetBiasOffset("1", 3600 * 24 * 30);
            }

            EditorGUILayout.EndHorizontal();
            #endregion

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }
        #endregion


        private void _OnBtnAddItem(int rewardID)
        {
            var board = Game.Manager.mergeBoardMan.activeWorld;
            if (board != null)
                board.activeBoard.SpawnItemMustWithReason(rewardID,
                    ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Cheat), 0, 0, false, false);
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