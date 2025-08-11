/**
 * @Author: handong.liu
 * @Date: 2020-07-08 19:49:41
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EL
{
    public static class DebugEx
    {
        public enum LogLevel
        {
            All,        //all log
            Trace,      //higher or equal to trace
            Info,       //higher or equal to Info
            Warning,     //higher or equal to warning
            Error
        }
        public static void SetMaxSingleLogLength(int length)
        {
            sMaxSingleLogLength = length;
        }
        public static void SetLogLevel(LogLevel level)
        {
            sLogLevel = (int)level;
        }
        // Start is called before the first frame update
        public static void Info(string content)
        {
            if (sLogLevel > (int)LogLevel.Info)
            {
                return;
            }
            _OutputPossibleLongLog(LogLevel.Info, content);
        }
        public static void Warning(string content)
        {
            if (sLogLevel > (int)LogLevel.Warning)
            {
                return;
            }
            _OutputPossibleLongLog(LogLevel.Warning, content);
        }
        public static void Error(string content)
        {
            _OutputPossibleLongLog(LogLevel.Error, content);
        }
        public static void Trace(string content)
        {
            if (sLogLevel > (int)LogLevel.Trace)
            {
                return;
            }
            _OutputPossibleLongLog(LogLevel.Trace, string.Format("[DEBUG]{0}", content));
        }
        public static void FormatInfo(string content, params object[] items)
        {
            if (sLogLevel > (int)LogLevel.Info)
            {
                return;
            }
            _ProcessParam(items);
            _OutputPossibleLongLog(LogLevel.Info, string.Format(content, items));
        }
        public static void FormatWarning(string content, params object[] items)
        {
            if (sLogLevel > (int)LogLevel.Warning)
            {
                return;
            }
            _ProcessParam(items);
            _OutputPossibleLongLog(LogLevel.Warning, string.Format(content, items));
        }
        public static void FormatError(string content, params object[] items)
        {
            _ProcessParam(items);
            _OutputPossibleLongLog(LogLevel.Error, string.Format(content, items));
        }
        public static void FormatTrace(string content, params object[] items)
        {
            if (sLogLevel > (int)LogLevel.Trace)
            {
                return;
            }
            _ProcessParam(items);
            _OutputPossibleLongLog(LogLevel.Trace, string.Format("[DEBUG]" + content, items));
        }
        private static void _ProcessParam(params object[] items)
        {
            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    var obj = items[i];
                    var unityObj = obj as UnityEngine.Object;
                    if (unityObj != null)
                    {
                        items[i] = unityObj.GetInstanceID();
                        var transform = obj as Transform;
                        if (transform != null)
                        {
                            items[i] = items[i] + transform.name;
                        }
                        continue;
                    }
                    var enumerable = obj as IEnumerable;
                    if (enumerable != null)
                    {
                        items[i] = enumerable.ToStringEx();
                        continue;
                    }
                }
            }
        }
        private static int sMaxSingleLogLength = -1;
        private static void _OutputPossibleLongLog(LogLevel level, string content)
        {
            if (sMaxSingleLogLength > 0 && content.Length > sMaxSingleLogLength)
            {
                for (int i = sMaxSingleLogLength, j = 0; j < content.Length; j += sMaxSingleLogLength, i += sMaxSingleLogLength)
                {
                    if (i > content.Length)
                    {
                        i = content.Length;
                    }
                    _OutputLog(level, content.Substring(j, i - j));
                }
            }
            else
            {
                _OutputLog(level, content);
            }
        }
        private static void _OutputLog(LogLevel level, string content)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    Debug.Log(content);
                    break;
                case LogLevel.Info:
                    Debug.Log(content);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(content);
                    break;
                case LogLevel.Error:
                    Debug.LogError(content);
                    break;
            }
        }
        private static int sLogLevel = (int)LogLevel.All;
    }
}