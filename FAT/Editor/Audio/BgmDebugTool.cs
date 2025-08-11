using UnityEditor;
using UnityEngine;


namespace FAT
{
    public class BgmDebugTool : EditorWindow
    {
        [MenuItem("Tools/BgmDebugTool")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(BgmDebugTool));
            window.titleContent = new GUIContent(nameof(BgmDebugTool));
        }

        private void OnGUI()
        {
            if (Game.Manager == null || Game.Manager.audioMan == null || Game.Manager.audioMan.SourceBgm == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal("box");
            // 获得audioMan的PlayListMode
            EditorGUILayout.LabelField("PlayListMode");
            EditorGUILayout.LabelField(Game.Manager.audioMan.PlaylistMode.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal("box");
            // 获得audioMan的PlayList
            EditorGUILayout.LabelField("PlayList");
            EditorGUILayout.BeginVertical();
            // 这里假设PlayList是一个List<string>，如果是其他类型需要调整显示方式
            foreach (var item in Game.Manager.audioMan.BgmPlaylist)
            {
                EditorGUILayout.LabelField(item.Asset);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal("box");
            // 获得audioMan的PlayListMode
            EditorGUILayout.LabelField("Current");
            if (Game.Manager.audioMan.SourceBgm.clip != null)
            {
                EditorGUILayout.LabelField(Game.Manager.audioMan.SourceBgm.clip.name);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("play Next"))
            {
                Game.Manager.audioMan.PlayDefaultBgm();
                Repaint();
            }

            EditorGUILayout.Space();
            
            if (GUILayout.Button("play activity guess"))
            {
                Game.Manager.audioMan.PlayBgm("GuessBgm");
                Repaint();
            }

            if (GUILayout.Button("play activity PachinkoBgm"))
            {
                Game.Manager.audioMan.PlayBgm("PachinkoBgm");
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }
    }
}