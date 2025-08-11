/*
 * @Author: qun.chao
 * @Date: 2020-10-29 11:06:23
 */
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using EL;

namespace FAT
{
    public class DevHelper : EditorWindow
    {
        Vector2 scrollPos;

        #region keep
        [SerializeField]
        public List<string> accountList = new List<string>();
        [SerializeField] private bool login_fold_state = false;
        [SerializeField] private bool misc_fold_state = false;
        [SerializeField] private bool misc_fold_gm = false;
        [SerializeField] private bool guide_fold_state = false;
        [SerializeField] private bool language_fold_state = false;
        [SerializeField] private bool encrypt_fold_state = false;
        [SerializeField] private bool ceshi_fold_state = false;
        [SerializeField] private bool sdk_fold_state = false;
        [SerializeField] private int currentGuidePage = 0;
        private const int GUIDES_PER_PAGE = 64; // 8行8列 = 64个按钮
        #endregion

        private string guideId = string.Empty;
        private string debugGuideValue = string.Empty;
        private string playGuideValue = string.Empty;
        private string sensitive_content = string.Empty;
        private string uiResourceName = string.Empty;

        [MenuItem("Tools/DevHelper")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(DevHelper));
            window.titleContent = new GUIContent(nameof(DevHelper));
        }

        void OnGUI()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, false);
            _UpdateLogin();
            _UpdateMisc();
            _UpdateGM();
            _UpdateLanguage();
            _UpdateGuide();
            GUILayout.EndScrollView();
        }

        private string replaceSession = string.Empty;

        private void _UpdateLogin()
        {
            EditorGUILayout.BeginVertical("Button");

            login_fold_state = EditorGUILayout.Foldout(login_fold_state, "Login Tool");
            if (!login_fold_state)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Active Accout");
            GUI.enabled = false;
            GUILayout.TextField(_GetCurrentFPID());
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Session");
            GUI.enabled = false;
            EditorGUILayout.TextField(_GetReplaceSession());
            GUI.enabled = true;
            _Button("Delete", _ClearSession);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            replaceSession = EditorGUILayout.TextField(replaceSession);
            if (GUILayout.Button("设置session"))
            {
                _SetReplaceSession(replaceSession);
            }

            EditorGUILayout.EndHorizontal();

            int removeIdx = -1;
            for (int i = 0; i < accountList.Count; ++i)
            {
                EditorGUILayout.BeginHorizontal();
                accountList[i] = EditorGUILayout.TextField(accountList[i]);
                _Button("Login", () => _Login(i));
                _Button("Remove", () => removeIdx = i);
                // FAT_TODO
                // if(Game.Instance.isRunning)
                // {
                //     _Button("Save", () => {
                //         var uid = CommonUtility.DigestStringToUlong(accountList[i]);
                //         var archive = Game.Instance.archiveMan.SerializeArchive(false);
                //         archive.Uid = uid;
                //         var bytesstring = NetworkManTest.codec.MarshalToByteString(archive);
                //         PlayerPrefs.SetString(string.Format("User{0}", uid), bytesstring.ToBase64());
                //     });
                // }
                EditorGUILayout.EndHorizontal();
            }

            if (removeIdx >= 0)
            {
                accountList.RemoveAt(removeIdx);
            }

            _Button("Add", () => accountList.Add(string.Empty));

            EditorGUILayout.EndVertical();
        }

        private void _UpdateLanguage()
        {
            EditorGUILayout.BeginVertical("Button");
            language_fold_state = EditorGUILayout.Foldout(language_fold_state, "多语言切换");
            if (!language_fold_state)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            int buttonsPerRow = 4;
            int currentRowCount = 0;

            EditorGUILayout.BeginVertical();

            var languages = EditorI18N.languages;
            if (languages.Count <= 0)
            {
                EditorGUILayout.LabelField("目前没有任何多语言相关数据，请先进入游戏以拉取配置");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("点击下方按钮切换成对应语言:");

            for (int i = 0; i < languages.Count; i++)
            {
                if (currentRowCount == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                if (GUILayout.Button(EditorI18N.GetLanguageShowName(languages[i]), GUILayout.Width(100),
                        GUILayout.Height(30)))
                {
                    if (!EditorApplication.isPlaying)
                    {
                        EditorI18N.SwitchTargetLanguage(languages[i]);
                    }
                    else
                    {
                        GameI18NHelper.GetOrCreate().SwitchTargetLanguage(languages[i]);
                    }
                }

                currentRowCount++;

                if (currentRowCount >= buttonsPerRow)
                {
                    EditorGUILayout.EndHorizontal();
                    currentRowCount = 0;
                }
            }

            if (currentRowCount > 0)
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        private void _UpdateGuide()
        {
            EditorGUILayout.BeginVertical("Button");

            guide_fold_state = EditorGUILayout.Foldout(guide_fold_state, "Guide");
            if (!guide_fold_state || !EditorApplication.isPlaying || !Game.Instance.isRunning)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            if (GUILayout.Button("Trigger Guide")) GuideUtility.TriggerGuide();

            _Button("Finish All", _FinishAllGuide);

            var bak = GUI.backgroundColor;

            // 计算总页数
            int totalGuides = 64 * 3; // 每页64个 3页
            int totalPages = Mathf.CeilToInt(totalGuides / (float)GUIDES_PER_PAGE);

            // 显示当前页码
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Page {currentGuidePage + 1}/{totalPages}");
            EditorGUILayout.EndHorizontal();

            // 显示当前页的按钮
            int startIndex = currentGuidePage * GUIDES_PER_PAGE;
            for (int j = 0; j < 8; ++j) // 每页8行
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < 8; ++i) // 每行8个
                {
                    var gid = startIndex + j * 8 + i + 1;
                    if (gid <= totalGuides)
                    {
                        _ShowGuideButton(gid);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            // 重置背景颜色
            GUI.backgroundColor = Color.white;

            // 添加翻页按钮
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = currentGuidePage > 0;
            if (GUILayout.Button("Previous Page"))
            {
                currentGuidePage--;
            }

            GUI.enabled = currentGuidePage < totalPages - 1;
            if (GUILayout.Button("Next Page"))
            {
                currentGuidePage++;
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // 恢复原来的背景颜色
            GUI.backgroundColor = bak;

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Guide"))
            {
                _PlayGuide();
            }

            playGuideValue = GUILayout.TextField(playGuideValue);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Kill Guide"))
            {
                Game.Manager.guideMan.DropGuide();
            }

            EditorGUILayout.EndVertical();
        }

        private void _UpdateMisc()
        {
            EditorGUILayout.BeginVertical("Button");
            misc_fold_state = EditorGUILayout.Foldout(misc_fold_state, "Misc");
            if (!misc_fold_state)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            _NetBias();

            // if (GUILayout.Button("按编辑器分辨率截图")) _Capture();

            // 临时写法
            if (Game.Instance.mainEntry != null)
            {
                GUILayout.Space(5);

                if (GUILayout.Button("Restart")) Game.Instance.RestartGame();

                GUILayout.Space(5);

                if (GUILayout.Button("Open DebugTool")) UIManager.Instance.OpenWindow(UIConfig.UIDebugPanelProMax);
                GUILayout.Space(5);

                if (GUILayout.Button("MergeDebug")) _MergeBoardDebug();

                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                _Button("Open UI", () => _OpenWindow(uiResourceName));
                uiResourceName = EditorGUILayout.TextField(uiResourceName);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private string _GetReplaceSession()
        {
            return PlayerPrefs.GetString(Constant.kPrefKeyDebugSession, string.Empty);
        }

        private void _SetReplaceSession(string session)
        {
            if (string.IsNullOrEmpty(session))
                PlayerPrefs.DeleteKey(Constant.kPrefKeyDebugSession);
            else
                PlayerPrefs.SetString(Constant.kPrefKeyDebugSession, session);
            PlayerPrefs.Save();
        }

        private void _ClearSession()
        {
            PlayerPrefs.DeleteKey(Constant.kPrefKeyDebugSession);
            PlayerPrefs.Save();
        }

        private string _GetCurrentFPID()
        {
            if (PlayerPrefs.HasKey(Constant.kPrefKeyDebugFPID))
            {
                return PlayerPrefs.GetString(Constant.kPrefKeyDebugFPID);
            }

            return string.Empty;
        }

        private void _MergeBoardDebug()
        {
            BoardUtility.debugShow = !BoardUtility.debugShow;
            if (UIImageResHelper.cullingMask == 0)
            {
                // TODO: mask
                UIImageResHelper.cullingMask = 1;
            }
            else
            {
                UIImageResHelper.cullingMask = 0;
            }
        }

        private void _Login(int idx)
        {
            // 使用与GameUpdateManager相同的方法清理本地记录
            // 简化切换帐号流程
            PlayerPrefs.SetString("____GAME_CLOSE", "22vd");
            PlayerPrefs.SetString("____GAME_CLOSE2", "22vd");

            accountList[idx] = accountList[idx].Trim();
            PlayerPrefs.SetString(Constant.kPrefKeyDebugFPID, _FixFPID(accountList[idx]));
            PlayerPrefs.Save();

            if (EditorApplication.isPlaying)
            {
                Game.Instance.RestartGame();
                return;
            }
            else
            {
                EditorApplication.isPlaying = true;
            }
        }

        private string _FixFPID(string input)
        {
            input = input.Trim();
            string pattern = @"^([._\w\d]+)";
            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return input;
        }

        #region guide
        private void _PlayGuide()
        {
            if (int.TryParse(playGuideValue, out var gid))
                Game.Manager.guideMan.PlayGuideById(gid);
        }

        private void _FinishAllGuide()
        {
            Game.Manager.guideMan.SetAllGuideFinished();
        }

        private void _ShowGuideButton(int gid)
        {
            var mgr = Game.Manager.guideMan;
            if (!mgr.IsGuideValid(gid))
            {
                GUI.backgroundColor = Color.white;
                GUI.enabled = false;
                GUILayout.Button(gid.ToString());
                GUI.enabled = true;
            }
            else
            {
                if (mgr.IsGuideFinished(gid))
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.red;
                _Button(gid.ToString(), () => { _SwitchGuide(gid); });
            }
        }

        private void _SwitchGuide(int gid)
        {
            var mgr = Game.Manager.guideMan;
            if (mgr.IsGuideFinished(gid))
            {
                mgr.UnfinishGuideAndRefresh(gid);
            }
            else
            {
                mgr.FinishGuideAndMoveNext(gid);
            }
        }
        #endregion

        #region misc
        private string biasVal = string.Empty;
        private string biasKey => NetTimeSync.local_time_bias_key;

        private void _NetBias()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("本地时间偏移");
            biasVal = EditorGUILayout.TextField(PlayerPrefs.GetString(biasKey, "0"));
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                if (ExpressionEvaluator.Evaluate(biasVal, out long result))
                {
                    biasVal = result.ToString();
                    PlayerPrefs.SetString(biasKey, biasVal);
                }
            }
        }

        private void _OpenWindow(string uiName)
        {
            if (string.IsNullOrEmpty(uiName))
                return;
            var info = typeof(UIConfig).GetField(uiName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (info == null)
                return;
            var obj = info.GetValue(null);
            if (obj is UIResource res)
                UIManager.Instance.OpenWindow(res);
        }

        private void _Capture()
        {
            string fileName = $"sc_{Game.Instance.GetTimestampSeconds()}.png";
            ScreenCapture.CaptureScreenshot(fileName);
        }

        private void _Button(string desc, Action cb)
        {
            if (GUILayout.Button(desc))
            {
                cb?.Invoke();
            }
        }
        #endregion

        #region GM
        private int x = 0;
        private int y = 0;

        private void _UpdateGM()
        {
            EditorGUILayout.BeginVertical("Button");
            misc_fold_gm = EditorGUILayout.Foldout(misc_fold_gm, "GM");
            if (!misc_fold_gm)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            x = EditorGUILayout.IntField("x坐标", x);
            y = EditorGUILayout.IntField("y坐标", y);
            EditorGUILayout.EndVertical();
            if (GUILayout.Button("查询棋子"))
            {
                var board = BoardViewManager.Instance.board;
                var item = board.GetItemByCoord(x, y);
                var boardView = BoardViewManager.Instance.boardView;
                
                if (item != null)
                {
                    var v = boardView.boardHolder.FindItemView(item.id);
                    Selection.activeObject = v.gameObject;
                }
               
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        #endregion
    }
}