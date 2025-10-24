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
    public class ActivityWindow : EditorWindow
    {

        [MenuItem("Tools/Develop/Activity Window", false, 10010)]
        public static void ShowWindow()
        {
            try
            {
                var window = EditorWindow.GetWindow(typeof(ActivityWindow));
                window.titleContent = new GUIContent("Activity Window");
                window.position = new Rect(window.position.x, window.position.y, 500, 600);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.Log(ex.Message);
            }
        }


        // private ExcelData m_ExcelData;
        private Vector2 m_ScrollPosition = Vector2.zero;
        private string m_InputStr = "";
        // private List<string> m_Ids;

        [Serializable]
        public class EventTimeJson
        {
            public Dictionary<string, EventTimeRowData> EventTimeMap;
        }

        [Serializable]
        public class EventTimeRowData
        {
            public string id;
            public int EventType;
            public long StartTime;
            public long EndTime;

            public EventType GetEventType()
            {
                return (EventType)EventType;
            }
        }

        private EventTimeJson m_EventTimeJson;
        private Dictionary<int, string> m_EventTypeMap = new();
        private Dictionary<int, string> m_EventTypeDescMap = new();

        private string GetEventTimeJsonPath()
        {
            return ConfigUtils.GetConfigRootPath() + "gen/rawdata/all/EventTimeConf.json";
        }

        private string GetEventTypeProtoPath()
        {
            return ConfigUtils.GetConfigRootPath() + "protos/rawdata_client/EventType.proto";
        }


        private void OnFocus()
        {
            Refresh();
        }

        private void Refresh()
        {
            /* if (m_ExcelData == null)
            {
                m_ExcelData = new ExcelData("FAT商业化配置总表", "EventTime@design", true);
            }
            else
            {
                m_ExcelData.ReadExcel();
            }
            m_Ids = m_ExcelData.GetIds(); */

            var jsonPath = GetEventTimeJsonPath();
            if (!File.Exists(jsonPath))
            {
                return;
            }

            var contentStr = File.ReadAllText(jsonPath);
            if (string.IsNullOrEmpty(contentStr))
            {
                return;
            }

            m_EventTimeJson = Newtonsoft.Json.JsonConvert.DeserializeObject<EventTimeJson>(contentStr);

            var protoPath = GetEventTypeProtoPath();
            if (!File.Exists(protoPath))
            {
                return;
            }

            var protoContentStr = File.ReadAllLines(protoPath);
            m_EventTypeMap.Clear();
            m_EventTypeDescMap.Clear();

            foreach (var line in protoContentStr)
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"^\s*(\w+)\s*=\s*(\d+);\s*//\s*(.*)$");
                if (match.Success)
                {
                    string eventType = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    string comment = match.Groups[3].Value;
                    // 这里可以根据需要处理 eventType, value, comment
                    // 例如：Debug.Log($"{eventType} = {value}; // {comment}");
                    var id = int.Parse(value);
                    m_EventTypeMap.Add(id, eventType);
                    m_EventTypeDescMap.Add(id, comment);
                }
            }

        }

        private Dictionary<int, bool> m_Temp = new();

        private void OnGUI()
        {
            /* if (GUILayout.Button("刷 新"))
            {
                Refresh();
            } */

            if (m_EventTimeJson == null)
            {
                return;
            }

            /* if (m_ExcelData == null)
            {
                return;
            } */



            #region 显示列表

            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            m_InputStr = GUILayout.TextField(m_InputStr, GUILayout.Width(position.width - 5));
            GUILayout.EndHorizontal();

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            m_Temp.Clear();

            foreach (var row in m_EventTimeJson.EventTimeMap)
            {
                m_EventTypeMap.TryGetValue(row.Value.EventType, out var eventType);
                m_EventTypeDescMap.TryGetValue(row.Value.EventType, out var desc);
                if (m_InputStr == "" && m_Temp.ContainsKey(row.Value.EventType))
                {
                    continue;
                }
                else if ((desc.IndexOf(m_InputStr, System.StringComparison.OrdinalIgnoreCase) == -1 && eventType.IndexOf(m_InputStr, System.StringComparison.OrdinalIgnoreCase) == -1))
                {
                    continue;
                }

                //不搜索的时候每种类型展示一个
                if (m_InputStr == "")
                {
                    m_Temp.Add(row.Value.EventType, true);
                }


                GUILayout.BeginHorizontal();

                // if (GUILayout.Button(row.Value.id, EditorStyles.boldLabel, GUILayout.Width(70), GUILayout.ExpandWidth(false))) GUIUtility.systemCopyBuffer = row.Value.id;
                // if (GUILayout.Button(eventType, EditorStyles.label, GUILayout.ExpandWidth(true))) GUIUtility.systemCopyBuffer = eventType;
                // if (GUILayout.Button(desc, EditorStyles.boldLabel, GUILayout.ExpandWidth(true))) GUIUtility.systemCopyBuffer = desc;

                GUILayout.Label(row.Value.id, EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(eventType, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label(desc, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                var formattedTime_start = DateTimeOffset.FromUnixTimeSeconds(row.Value.StartTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                var formattedTime_end = DateTimeOffset.FromUnixTimeSeconds(row.Value.EndTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                var timeStr = string.Format("{0} - {1}", formattedTime_start, formattedTime_end);

                GUILayout.FlexibleSpace();

                if (Application.isPlaying)
                {
                    bool hasClick = false;
                    // 添加活动
                    if (GUILayout.Button("添加", GUILayout.Width(40)))
                    {
                        hasClick = true;
                        Game.Manager.activity.DebugActivate(row.Value.id);
                    }
                    // 调试时间
                    if (GUILayout.Button("开始", GUILayout.Width(40)))
                    {
                        hasClick = true;
                        var startTime = row.Value.StartTime - 5;
                        var bias = startTime - Game.Instance.GetTimestampSeconds() + Game.Manager.networkMan.networkBias;
                        Game.Manager.networkMan.DebugSetNetworkBias(bias);
                    }
                    // 结束
                    if (GUILayout.Button("结束", GUILayout.Width(40)))
                    {
                        hasClick = true;
                        var endTime = row.Value.EndTime - 5;
                        var bias = endTime - Game.Instance.GetTimestampSeconds() + Game.Manager.networkMan.networkBias;
                        Game.Manager.networkMan.DebugSetNetworkBias(bias);
                    }

                    if (hasClick)
                    {
                        Debug.LogWarning("活动时间: " + timeStr);
                    }
                }
                else
                {
                    //运行中不操作，显示时间
                    GUILayout.Label(timeStr, EditorStyles.miniLabel);
                }


                GUILayout.EndHorizontal();

                // GUILayout.BeginHorizontal();
                // GUILayout.Label(timeStr, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                // GUILayout.EndHorizontal();

            }
            EditorGUILayout.EndScrollView();

            /* GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            m_InputStr = GUILayout.TextField(m_InputStr, GUILayout.Width(position.width - 5));
            GUILayout.EndHorizontal();

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            foreach (string id in m_Ids)
            {
                var desc = m_ExcelData.GetValue(id, "desc1@pm");
                if (m_InputStr == "" || desc.IndexOf(m_InputStr, System.StringComparison.OrdinalIgnoreCase) == -1)
                {
                    continue;
                }

                GUILayout.BeginHorizontal();

                GUILayout.Label(id, EditorStyles.boldLabel, GUILayout.Width(70), GUILayout.ExpandWidth(false));
                GUILayout.Label(desc, EditorStyles.label, GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("激活", GUILayout.Width(40)))
                {
                    Game.Manager.activity.DebugActivate(id);
                }

                GUILayout.EndHorizontal();

            }
            EditorGUILayout.EndScrollView(); */
            #endregion
        }

    }
}