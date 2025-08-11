/*
 * @Author: qun.chao
 * @Date: 2021-09-14 11:12:37
 */
using System.Collections.Generic;
using UnityEngine;
using EL;
using FAT;
using FAT.Merge;

public class SettingManager : Singleton<SettingManager>
{
    public bool SoundIsOn { get; private set; }
    public bool MusicIsOn { get; private set; }
    public bool VibrationIsOn { get; private set; }
    public bool PlotIsOn { get; private set; }
    public string Lang { get; private set; }
    public int EnergyBoostState { get; private set; }
    public bool EnergyBoostIsOn => EnergyBoostUtility.IsBoost(EnergyBoostState);

    //记录用户静音状态 用于在游戏登录界面还没有玩家存档时 读取本地存储数据决策是否静音
    public const string GameMuteMusicKeyName = "__GameMuteMusicKey__";      //背景音
    public const string GameMuteSoundKeyName = "__GameMuteSoundKey__";      //音效
    private static readonly string kMusicStateKey = "setting_music_state";
    private static readonly string kSoundStateKey = "setting_sound_state";
    private static readonly string kVibrationStateKey = "setting_vibration_state";
    private static readonly string kLanguageKey = "setting_lang";
    private static readonly string kEnergyBoostStateKey = "setting_energy_boost_state";
    private static readonly string kPlotStateKey = "setting_plot_state";
    private float musicMaxVolume;
    private float soundMaxVolume;

    // 初始化时机在login以后 配置已完备
    public void Initialize()
    {
        _SyncSetting();
        _ApplySetting();
    }

    public void OnSwitchMusicState()
    {
        MusicIsOn = !MusicIsOn;
        DataTracker.TraceUser().Sound(MusicIsOn, SoundIsOn).Apply();
        _ApplySetting();
        _SaveSetting();
    }

    public void OnSwitchSoundState()
    {
        SoundIsOn = !SoundIsOn;
        DataTracker.TraceUser().Sound(MusicIsOn, SoundIsOn).Apply();
        _ApplySetting();
        _SaveSetting();
    }

    public void OnSwitchVibrationState()
    {
        VibrationIsOn = !VibrationIsOn;
        _SaveSetting();
    }
    
    public void OnSwitchPlotState()
    {
        PlotIsOn = !PlotIsOn;
        _SaveSetting();
        DataTracker.SettingDialog(PlotIsOn);
    }

    public void SetLanguage(string _lang)
    {
        Lang = _lang;
        _ApplySetting();
        _SaveSetting();
    }
    
    public void OnSwitchEnergyBoostState()
    {
        _SetEnergyBoostState(EnergyBoostUtility.SwitchBetState(EnergyBoostState));
    }

    public void OnLoginRefreshEnergyBoostState()
    {
        if (EnergyBoostUtility.OnLoginAdjustBetState(EnergyBoostState, out var state))
        {
            _SetEnergyBoostState(state);
        }
    }

    private void _SetEnergyBoostState(int state)
    {
        EnergyBoostState = state;
        _SaveSetting();
        DataTracker.boost.Track(EnergyBoostIsOn, EnergyBoostUtility.GetEnergyRate(state));
    }

    private void _SaveSetting()
    {
        _SetValue(kMusicStateKey, MusicIsOn ? 1 : 0);
        _SetValue(kSoundStateKey, SoundIsOn ? 1 : 0);
        _SetValue(kVibrationStateKey, VibrationIsOn ? 1 : 0);
        _SetValue(kPlotStateKey, PlotIsOn ? 1 : 0);
        _SetValue(kLanguageKey, Lang);
        _SetValue(kEnergyBoostStateKey, EnergyBoostState);
    }

    private void _SyncSetting()
    {
        musicMaxVolume = 1f;
        soundMaxVolume = 1f;

        MusicIsOn = _GetValue(kMusicStateKey, 1) == 1;
        SoundIsOn = _GetValue(kSoundStateKey, 1) == 1;
#if UNITY_ANDROID
        VibrationIsOn = _GetValue(kVibrationStateKey, 0) == 1;
#elif UNITY_IOS
        VibrationIsOn = _GetValue(kVibrationStateKey, 1) == 1;
#endif
        PlotIsOn = _GetValue(kPlotStateKey, 1) == 1;
        Lang = _GetValue(kLanguageKey, I18N.GetLanguage());
        EnergyBoostState = _GetValue(kEnergyBoostStateKey, 0);
        //用户上线时同步一下TGA开关状态属性
        DataTracker.TraceUser()
            .Boost(EnergyBoostState)
            .Dialog(PlotIsOn)
            .Sound(MusicIsOn, SoundIsOn)
            .Apply();
    }

    private void _ApplySetting()
    {
        Game.Manager.audioMan.SetBgmVolume(MusicIsOn ? musicMaxVolume : 0f);
        Game.Manager.audioMan.SetSfxVolume(SoundIsOn ? soundMaxVolume : 0f);

        // 设置mutekey 在游戏启动时决策是否静音
        PlayerPrefs.SetInt(GameMuteMusicKeyName, MusicIsOn ? 0 : 1);
        PlayerPrefs.SetInt(GameMuteSoundKeyName, SoundIsOn ? 0 : 1);

        // 设置game语言
        // I18N.SetLanguage(Lang);
        // 设置平台语言
        // FAT.Platform.PlatformSDK.Instance.SetLanguage(Lang);

        // msg
        MessageCenter.Get<FAT.MSG.GAME_SETTING_APPLY>().Dispatch();
    }

    #region storage

    private string _GetValue(string key, string _default)
    {
        var mgr = Game.Manager.accountMan;
        if (mgr.TryGetClientStorage(key, out var ret))
        {
            return ret;
        }
        return _default;
    }

    private void _SetValue(string key, string value)
    {
        Game.Manager.accountMan.SetClientStorage(key, value);
    }

    private int _GetValue(string key, int _default)
    {
        var mgr = Game.Manager.accountMan;
        if (mgr.TryGetClientStorage(key, out var ret))
        {
            if (!int.TryParse(ret, out _default))
            {
                Debug.LogErrorFormat("[SETTING] bad storage value. {0} {1}", key, ret);
            }
        }
        return _default;
    }

    private void _SetValue(string key, int value)
    {
        Game.Manager.accountMan.SetClientStorage(key, value.ToString());
    }

    #endregion
}