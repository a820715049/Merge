/**
 * @Author: handong.liu
 * @Date: 2021-04-21 14:26:04
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using FAT;
using fat.gamekitdata;

public class GameSwitchManager : EL.Singleton<GameSwitchManager>
{
    public bool isUseGuideDebug;
    public bool isDataTracking;
    public bool isDebugMode;
    public bool isOpenShop => !GetSwitch("SwitchClosePay");
    public bool isOpenDeleteAccount => GetSwitch("ShowDelUser");
    public bool isOpenCdKey => GetSwitch("ShowCdKey");
    public bool isShowBlockInfo => GetSwitch("ShowBlockInfo");
    public bool isShowSurvey => GetSwitch("ShowSurvey");

    private Dictionary<string, bool> mSwitches = new Dictionary<string, bool>();

    public void Refresh()
    {
        bool isProdBuild = Game.Instance.appSettings.sdkEnv != AppSettings.SDKEnvironment.Sandbox;
        bool isTestUser = false;
        if (Game.Manager.archiveMan != null)
        {
            isTestUser = Game.Manager.archiveMan.userType == UserType.Test;
        }
        isDebugMode = !isProdBuild || isTestUser;
        isUseGuideDebug = false;//!isProdBuild || isTestUser;
        isDataTracking = isProdBuild;
        DebugEx.SetLogLevel(isDebugMode ? DebugEx.LogLevel.All : DebugEx.LogLevel.Info);
        DebugEx.FormatInfo("GameSwitchManager::Refresh ----> {0}, {1}, {2}, {3}, {4}", isProdBuild, isTestUser, isDebugMode, isUseGuideDebug, isDataTracking);
        MessageCenter.Get<FAT.MSG.GAME_SWITCH_REFRESH>().Dispatch();
    }

    public bool GetSwitch(string key)
    {
        return mSwitches.GetDefault(key, false);
    }

    public void D_Toggle(string key)
    {
        mSwitches[key] = !GetSwitch(key);
        _OnSwitchChanged();
    }

    public bool FillSwitchData(IDictionary<string, bool> dict)
    {
        return _FillSwitchData(dict, true);
    }

    public void LoadLocalData()
    {
        var dict = new Dictionary<string, bool>();
        _LoadSwitchFromLocal(dict);
        _FillSwitchData(dict, false);
    }

    private bool _FillSwitchData(IDictionary<string, bool> dict, bool isRemoteData)
    {
        bool changed = false;
        if (isRemoteData)                //远端给的数据永远是全量的，因此需要把不存在的key变成false
        {
            foreach (var key in mSwitches.Keys)
            {
                if (!dict.ContainsKey(key))
                {
                    dict[key] = false;
                }
            }
        }
        foreach (var key in dict.Keys)
        {
            bool original = GetSwitch(key);
            if (dict[key] != original)
            {
                mSwitches[key] = !original;
                changed = true;
            }
        }
        if (changed && isRemoteData)
        {
            _OnSwitchChanged();
        }
        return changed;
    }

    private void _OnSwitchChanged()
    {
        MessageCenter.Get<FAT.MSG.GAME_SWITCH_REFRESH>().Dispatch();
        _SaveSwitchToLocal();
    }

    private void _SaveSwitchToLocal()
    {
        using (ObjectPool<EL.SimpleJSON.JSONObject>.GlobalPool.AllocStub(out var json))
        using (ObjectPool<System.Text.StringBuilder>.GlobalPool.AllocStub(out var sb))
        {
            foreach (var k in mSwitches)
            {
                json[k.Key] = k.Value;
            }
            json.WriteToStringBuilder(sb, 0, 0, EL.SimpleJSON.JSONTextMode.Compact);
            PlayerPrefs.SetString("_SWITCH_", System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sb.ToString())));
            PlayerPrefs.Save();
        }
    }

    private void _LoadSwitchFromLocal(Dictionary<string, bool> result)
    {
        try
        {
            var jsonStr = PlayerPrefs.GetString("_SWITCH_", "");
            if (!string.IsNullOrEmpty(jsonStr))
            {
                jsonStr = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(jsonStr));
                var j = EL.SimpleJSON.JSON.Parse(jsonStr);
                foreach (var k in j.Keys)
                {
                    result[k] = j[k.Value].AsBool;
                }
            }
        }
        catch (System.Exception ex)
        {
            DebugEx.FormatWarning("GameSwitchManager::_LoadSwitchFromLocal fail {0}:{1}", ex.Message, ex.StackTrace);
        }
    }
}
