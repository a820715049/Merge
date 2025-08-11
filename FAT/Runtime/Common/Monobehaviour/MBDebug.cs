/**
 * @Author: handong.liu
 * @Date: 2021-01-11 20:19:43
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public class MBDebug : MonoBehaviour
{
    public class ParamedAction
    {
        public string strValue;
        public System.Action<string> act;
    }
    public Dictionary<string, System.Action> acts => mActions;
    public Dictionary<string, string> infos => mInfos;
    public Dictionary<string, UnityEngine.Object> objInfos => mObjInfos;
    public Dictionary<string, ParamedAction> paramActions => mParamActions;
    private Dictionary<string, string> mInfos = new Dictionary<string, string>();
    private Dictionary<string, UnityEngine.Object> mObjInfos = new Dictionary<string, UnityEngine.Object>();
    private Dictionary<string, System.Action> mActions = new Dictionary<string, System.Action>();
    private Dictionary<string, ParamedAction> mParamActions = new Dictionary<string, ParamedAction>();

    public static void SetDebugAction(GameObject go, string key, System.Action val)
    {
#if UNITY_EDITOR
        if(Application.isPlaying)
        {
            MBDebug mono = go.GetComponent<MBDebug>();
            if(mono == null)
            {
                mono = go.AddComponent<MBDebug>();
            }
            mono.mActions[key] = val;
        }
#endif
    }

    public static void SetDebugParamedAction(GameObject go, string key, System.Action<string> val)
    {
#if UNITY_EDITOR
        if(Application.isPlaying)
        {
            MBDebug mono = go.GetComponent<MBDebug>();
            if(mono == null)
            {
                mono = go.AddComponent<MBDebug>();
            }
            mono.mParamActions[key] = new ParamedAction() {act = val, strValue = ""};
        }
#endif
    }

    public static void SetDebugInfo(GameObject go, string key, string val)
    {
#if UNITY_EDITOR
        if(Application.isPlaying)
        {
            MBDebug mono = go.GetComponent<MBDebug>();
            if(mono == null)
            {
                mono = go.AddComponent<MBDebug>();
            }
            mono.mInfos[key] = val;
        }
#endif
    }

    public static void SetDebugInfo(GameObject go, string key, object val)
    {
#if UNITY_EDITOR
        if(Application.isPlaying)
        {
            MBDebug mono = go.GetComponent<MBDebug>();
            if(mono == null)
            {
                mono = go.AddComponent<MBDebug>();
            }
            IEnumerable v = val as IEnumerable;
            if(v != null)
            {
                mono.mInfos[key] = v.ToStringEx();
            }
            else
            {
                mono.mInfos[key] = val.ToString();
            }
        }
#endif
    }

    public static void SetObjDebugInfo(GameObject go, string key, UnityEngine.Object val)
    {
#if UNITY_EDITOR
        if(Application.isPlaying)
        {
            MBDebug mono = go.GetComponent<MBDebug>();
            if(mono == null)
            {
                mono = go.AddComponent<MBDebug>();
            }
            string k = key;
            if(mono.mObjInfos.ContainsKey(k))
            {
                int i = 1;
                k = key + i;
                while(mono.mObjInfos.ContainsKey(k))
                {
                    i++;
                    k = key + i;
                }
            }
            mono.mObjInfos[k] = val;
        }
#endif
    }

    public static void ClearDebugInfo(GameObject go, string key)
    {
#if UNITY_EDITOR
        if(Application.isPlaying)
        {
            MBDebug mono = go.GetComponent<MBDebug>();
            if(mono == null)
            {
                mono = go.AddComponent<MBDebug>();
            }
            List<string> toRemove = new List<string>();
            foreach(var k in mono.mInfos.Keys)
            {
                if(k.StartsWith(key))
                {
                    toRemove.Add(k);
                }
            }
            foreach(var k in toRemove)
            {
                mono.mInfos.Remove(k);
            }

            toRemove.Clear();
            foreach(var k in mono.mObjInfos.Keys)
            {
                if(k.StartsWith(key))
                {
                    toRemove.Add(k);
                }
            }
            foreach(var k in toRemove)
            {
                mono.mObjInfos.Remove(k);
            }
        }
#endif
    }

    public static void ClearAll(GameObject go)
    {
#if UNITY_EDITOR
        if(Application.isPlaying)
        {
            MBDebug mono = go.GetComponent<MBDebug>();
            if(mono != null)
            {
                Object.DestroyImmediate(mono);
            }
        }
#endif
    }
}