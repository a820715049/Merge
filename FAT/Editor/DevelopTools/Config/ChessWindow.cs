using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using EL.Resource;
using UnityEngine.UI;
using FAT;
using FAT.Merge;
using System.Collections;

namespace DevelopTools
{
    public class ChessWindow : EditorWindow
    {
        [MenuItem("Tools/Develop/Chess Window", false, 10020)]
        public static void ShowWindow()
        {
            try
            {
                var window = EditorWindow.GetWindow(typeof(ChessWindow));
                window.titleContent = new GUIContent("Chess Window");
                window.position = new Rect(window.position.x, window.position.y, 500, 600);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.Log(ex.Message);
            }
        }

        [Serializable]
        public class ChessJson
        {
            public Dictionary<string, ChessRowData> ObjBasicMap;
        }

        [Serializable]
        public class ChessRowData
        {
            public string id;
            public string icon;

            public string GetIcon()
            {
                var arr = icon.Split(':');
                return arr[1];
            }

            /* public IEnumerator GetIconFromAB()
            {
                var res = icon.ConvertToAssetConfig();
                var task = ResManager.LoadAsset<Texture2D>(res.Group, res.Asset);
                yield return task;
                var texture = task.asset as Texture2D;
                if (texture != null)
                {
                    
                }
            } */

            public string GetABName()
            {
                var arr = icon.Split(':');
                return arr[0];
            }
        }


        private ChessJson m_ChessJson;
        private Dictionary<string, List<string>> m_AssetsToIds = new();
        private Dictionary<string, Texture2D> m_AssetsToSprite = new();
        private Vector2 scrollPosition = Vector2.zero;


        private static string GetChessJsonPath()
        {
            return ConfigUtils.GetConfigRootPath() + "gen/rawdata/all/ObjBasicConf.json";
        }

        private void OnFocus()
        {
        }

        private void Refresh()
        {
            var jsonPath = GetChessJsonPath();
            if (!File.Exists(jsonPath))
            {
                return;
            }

            var contentStr = File.ReadAllText(jsonPath);
            if (string.IsNullOrEmpty(contentStr))
            {
                return;
            }

            m_AssetsToIds.Clear();
            m_AssetsToSprite.Clear();

            m_ChessJson = Newtonsoft.Json.JsonConvert.DeserializeObject<ChessJson>(contentStr);
            foreach (var row in m_ChessJson.ObjBasicMap)
            {
                List<string> list = m_AssetsToIds.ContainsKey(row.Value.icon) ? m_AssetsToIds[row.Value.icon] : new List<string>();
                if (list.Count == 0)
                {
                    m_AssetsToIds.Add(row.Value.icon, list);
                }
                list.Add(row.Value.id);
            }
        }

        private void OnGUI()
        {
            if (GUILayout.Button("采集视图"))
            {
                Refresh();
                RefreshChess();
            }
            if (m_ChessJson == null)
            {
                return;
            }
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 计算窗口宽度，决定每行显示多少个Item
            float itemWidth = 60f;
            float itemHeight = 80f;
            float padding = 5;
            float windowWidth = EditorGUIUtility.currentViewWidth;
            int itemsPerRow = Mathf.Max(1, Mathf.FloorToInt(windowWidth / (itemWidth + padding)));
            int index = 0;
            bool hasStart = false;
            foreach (var icon in m_AssetsToSprite)
            {
                if (m_AssetsToIds.TryGetValue(icon.Key, out var ids) && ids.Count > 0)
                {
                    foreach (var id in ids)
                    {
                        var sprite = icon.Value;
                        if (index % itemsPerRow == 0)
                        {
                            if (hasStart)
                            {
                                EditorGUILayout.EndHorizontal();
                                GUILayout.Space(padding);
                                hasStart = false;
                            }
                            EditorGUILayout.BeginHorizontal();
                            hasStart = true;
                        }
                        index++;

                        EditorGUILayout.BeginVertical(GUILayout.Width(itemWidth), GUILayout.Height(itemHeight));
                        GUILayout.Space(5);

                        // 图片
                        Color oldColor = GUI.backgroundColor;
                        if (ids.Count > 1) GUI.backgroundColor = Color.green;
                        GUILayout.Box(sprite, GUILayout.Width(itemWidth - 10), GUILayout.Height(itemWidth - 10));
                        GUI.backgroundColor = oldColor;

                        // id文本
                        if (GUILayout.Button(id, EditorStyles.miniBoldLabel)) GUIUtility.systemCopyBuffer = id;

                        // 按钮
                        if (Application.isPlaying)
                        {
                            if (GUILayout.Button("Add", GUILayout.Width(itemWidth - 10)))
                            {
                                if (int.TryParse(id, out var chessId))
                                {
                                    var board = Game.Manager.mergeBoardMan.activeWorld;
                                    if (board != null)
                                    {
                                        board.activeBoard.SpawnItemMustWithReason(chessId, ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Cheat), 0, 0, false, false);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("复制Id", GUILayout.Width(itemWidth - 10)))
                            {
                                GUIUtility.systemCopyBuffer = id;
                            }
                        }

                        EditorGUILayout.EndVertical();
                    }
                }
            }

            if (hasStart)
            {
                EditorGUILayout.EndHorizontal();
                hasStart = false;
            }


            EditorGUILayout.EndScrollView();
        }

        private void RefreshChess()
        {
            var gameObj = GameObject.Find("Root_UI");
            if (gameObj == null)
            {
                return;
            }
            var ress = gameObj.GetComponentsInChildren<UIImageRes>();
            foreach (var res in ress)
            {
                if (res.CacheImage.Target is Image image)
                {
                    var sprite = image.sprite;
                    if (sprite == null || res.AssetConfig == null || !m_AssetsToIds.ContainsKey(res.AssetConfig) || m_AssetsToSprite.ContainsKey(res.AssetConfig))
                    {
                        continue;
                    }
                    m_AssetsToSprite.Add(res.AssetConfig, sprite.texture);
                }
            }
        }
    }
}