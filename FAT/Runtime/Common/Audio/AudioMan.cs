/*
 * @Author: qun.chao
 * @Date: 2024-10-15 11:34:05
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using EL;
using EL.Resource;
using System.Threading;
using Config;
using Cysharp.Threading.Tasks;
using fat.conf;
using Random = UnityEngine.Random;

namespace FAT
{
    public class AudioMan : IGameModule
    {
        public readonly string default_group = "common_audio";
        public readonly string default_bgm_asset = "fat_bgm.ogg";
        public readonly string default_bgm_name = "fat_bgm";

        private readonly List<string> _mergeSoundPerLevel = new ();
        private readonly Dictionary<string, string> _commonAudioMap = new();
        private Dictionary<string, int> _recentAudioRequestMap = new();
        // private CancellationTokenSource _cts_loading;
        // private CancellationTokenSource _cts_loop_audio;
        private AudioSource _sourceBgm;
        public AudioSource SourceBgm => _sourceBgm;
        private AudioSource _sourceSfx;
        private AudioSource _sourceSfx_loop;

        // Bgm配置
        private readonly List<AssetConfig> _bgmPlaylist = new(); // BGM 播放列表
        public List<AssetConfig> BgmPlaylist => _bgmPlaylist;

        private PlayListMode _playlistMode { get; set; } // 播放模式
        public PlayListMode PlaylistMode => _playlistMode;

        private readonly List<int> _playedBgmIndex = new(); // 已播放的 BGM 索引

        public enum PlayListMode
        {
            Shuffle = 0, // 随机
            Array = 1, // 列表
        }

        // Bgm状态
        private int _currentBgmIndex = -1; // 当前播放的 BGM 索引
        private float _currentBgmTime = 0f; // 当前 BGM 的播放时间点
        private CancellationTokenSource _cts_bgmPlaylist;
        private bool _restoreState; // 判断是否恢复播放状态

        public void SetBgmVolume(float v) => _sourceBgm.volume = v;
        public void SetSfxVolume(float v)
        {
            _sourceSfx.volume = v;
            _sourceSfx_loop.volume = v;
        }

        public void InitWithConfig(MergeAudioConfig cfg)
        {
            Cancel();
            _mergeSoundPerLevel.Clear();
            _commonAudioMap.Clear();
            _recentAudioRequestMap.Clear();

            _mergeSoundPerLevel.AddRange(cfg.mergeSounds);
            foreach (var item in cfg.soundEventConfig.eventDatas)
            {
                if (item.clipNames.Length > 0)
                {
                    if (!_commonAudioMap.TryAdd(item.eventName, item.clipNames[0]))
                        Error($"duplicated audio cfg {item.eventName}");
                }
            }

            var isMute = PlayerPrefs.GetInt(SettingManager.GameMuteMusicKeyName, 0) == 1;
            SetBgmVolume(isMute ? 0f : 1f);
            isMute = PlayerPrefs.GetInt(SettingManager.GameMuteSoundKeyName, 0) == 1;
            SetSfxVolume(isMute ? 0f : 1f);
        }

        public void TurnOff()
        {
            _sourceBgm.enabled = false;
            _sourceSfx.enabled = false;
            _sourceSfx_loop.enabled = false;
        }

        public void TurnOn()
        {
            _sourceBgm.enabled = true;
            _sourceSfx.enabled = true;
            _sourceSfx_loop.enabled = true;
            RestoreAudio();
        }

        // ref: https://discussions.unity.com/t/ironsource-levelplay-ios-audio-is-muted-after-ad-is-played/912955/27
        private static void RestoreAudio()
        {
            AudioSettings.Reset(AudioSettings.GetConfiguration());
        }

        private void _OnConfigLoaded()
        {
            _bgmPlaylist.Clear();

            var bgmConfig = Data.GetBGMSlice()[0];
            _playlistMode = bgmConfig.Mode == false ? PlayListMode.Shuffle : PlayListMode.Array;
            foreach (var bgm in bgmConfig.Asset)
            {
                var res = bgm.ConvertToAssetConfig();
                _bgmPlaylist.Add(res);
            }

            _currentBgmIndex = -1;
            _currentBgmTime = 0;

            // 配置加载完毕后 播放bgm
            PlayDefaultBgm();
        }

        public void TriggerSound(string soundKey)
        {
            if (string.IsNullOrEmpty(soundKey))
                return;
            if (_commonAudioMap.TryGetValue(soundKey, out var asset))
            {
                PlaySound(default_group, asset);
            }
            else
            {
                var sound = Data.GetSound(soundKey);
                if (sound != null)
                {
                    var res = sound.Asset.ConvertToAssetConfig();
                    if (sound.Loop)
                    {
                        PlayLoopSound(res.Group, res.Asset);
                    }
                    else
                    {
                        PlaySound(res.Group, res.Asset);
                    }
                }
            }
        }

        public void PlayDefaultBgm()
        {
            PlayNextTrack().Forget();
        }

        public void PlayBgm(string bgmKey)
        {
            if (string.IsNullOrEmpty(bgmKey))
                return;
            var bgm = Data.GetSound(bgmKey);
            var res = bgm?.Asset.ConvertToAssetConfig();
            if (res != null)
            {
                StopLoopBgm();
                PlayMainTheme(res.Group, res.Asset).Forget();
            }
        }

        #region 轮播BGM

        // 停止播放
        private void StopLoopBgm()
        {
            if (_sourceBgm != null && _sourceBgm.isPlaying)
            {
                // 存储播放状态 记录播放时间
                _currentBgmTime = _sourceBgm.time;
                _sourceBgm.Stop();
                _sourceBgm.clip = null;
            }

            _cts_bgmPlaylist?.Cancel();
            _cts_bgmPlaylist?.Dispose();
            _cts_bgmPlaylist = null;
        }

        private void RefreshBgmIndex()
        {
            switch (_playlistMode)
            {
                case PlayListMode.Array:
                    if (_currentBgmIndex < _bgmPlaylist.Count - 1 && _currentBgmIndex >= 0)
                    {
                        _currentBgmIndex += 1;
                    }
                    else
                    {
                        _currentBgmIndex = 0;
                    }
                    break;
                case PlayListMode.Shuffle:
                    if (_playedBgmIndex.Count == _bgmPlaylist.Count)
                    {
                        _playedBgmIndex.Clear();
                        _currentBgmIndex = Random.Range(0, _bgmPlaylist.Count);
                    }
                    else
                    {
                        // 从未播放的 BGM 中随机选择一个
                        List<int> unPlayedIndices = new List<int>();

                        // 遍历播放列表，找到未播放的索引
                        for (int i = 0; i < _bgmPlaylist.Count; i++)
                        {
                            if (!_playedBgmIndex.Contains(i))
                            {
                                unPlayedIndices.Add(i);
                            }
                        }

                        // 从未播放的索引中随机选择一个
                        int randomIndex = Random.Range(0, unPlayedIndices.Count);
                        _currentBgmIndex = unPlayedIndices[randomIndex];
                    }
                    break;
                default:
                    _currentBgmIndex = 0;
                    break;
            }
        }

        private async UniTaskVoid PlayNextTrack()
        {
            if (_bgmPlaylist == null || _bgmPlaylist.Count == 0 || _sourceBgm == null)
                return;

            // 创建新的 CancellationTokenSource
            _cts_bgmPlaylist?.Dispose();
            _cts_bgmPlaylist = new CancellationTokenSource();
            var token = _cts_bgmPlaylist.Token;

            // 判断是否恢复索引
            if (!_restoreState)
            {
                RefreshBgmIndex();
            }
            else
            {
                _restoreState = false;
            }

            // 记录播放的索引
            if (!_playedBgmIndex.Contains(_currentBgmIndex))
            {
                _playedBgmIndex.Add(_currentBgmIndex);
            }

            // 获取当前 BGM 的 asset 名称
            var bgmAsset = _bgmPlaylist[_currentBgmIndex];
            var clip = await LoadAudioClip(bgmAsset.Group, bgmAsset.Asset);
            if (clip != null)
            {
                _sourceBgm.clip = clip;
                _sourceBgm.time = _currentBgmTime; // 设置播放时间点
                _sourceBgm.loop = false; // 单曲循环由播放列表控制
                _sourceBgm.Play();

                // 等待当前 BGM 播放完毕
                WaitForBgmToFinish(token).Forget();
            }
        }

        private async UniTaskVoid WaitForBgmToFinish(CancellationToken token)
        {
            try
            {
                // 在播放或者未启用(广告暂停)
                await UniTask.WaitWhile(() =>
                    _sourceBgm != null && (_sourceBgm.isPlaying || !_sourceBgm.enabled),
                    cancellationToken: token);

                _currentBgmTime = 0;

                // 播放下一首
                PlayNextTrack().Forget();
            }
            catch (OperationCanceledException)
            {
                // 操作被取消
                _restoreState = true;

                DebugEx.Info("BGM 播放被取消");
            }
        }

        #endregion
        
        public void PlayMergeSound(int level)
        {
            var asset = _mergeSoundPerLevel.GetElementEx(level - 2, ArrayExt.OverflowBehaviour.Clamp);
            PlaySound(default_group, asset);
        }

        public void PlaySound(string group, string asset)
        {
            if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(asset)) return;
            if (!TryRequest(group, asset)) return;
            PlayOneShot(group, asset).Forget();
        }

        public void PlayLoopSound(string group, string asset)
        {
            if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(asset)) return;
            if (!TryRequest(group, asset)) return;
            PlayLoop(group, asset).Forget();
        }

        public void Pause()
        {
            _sourceBgm.Pause();
            _sourceSfx.Pause();
            _sourceSfx_loop.Pause();
        }

        public void UnPause()
        {
            _sourceBgm.UnPause();
            _sourceSfx.UnPause();
            _sourceSfx_loop.UnPause();
        }

        public void StopLoopSound()
        {
            _sourceSfx_loop.Stop();
            _sourceSfx_loop.clip = null;
        }


        private async UniTaskVoid PlayMainTheme(string group, string asset)
        {
            var clip = await LoadAudioClip(group, asset);
            if (clip != null)
            {
                Info($"play {asset}");
                _sourceBgm.clip = clip;
                _sourceBgm.loop = true;
                _sourceBgm.Play();
            }
        }

        private async UniTaskVoid PlayOneShot(string group, string asset)
        {
            var clip = await LoadAudioClip(group, asset);
            if (clip != null)
            {
                Info($"play {asset}");
                _sourceSfx.PlayOneShot(clip);
            }
        }

        private async UniTaskVoid PlayLoop(string group, string asset)
        {
            var clip = await LoadAudioClip(group, asset);
            if (clip != null)
            {
                Info($"play {asset}");
                _sourceSfx_loop.clip = clip;
                _sourceSfx_loop.loop = true;
                _sourceSfx_loop.Play();
            }
        }

        private async UniTask<AudioClip> LoadAudioClip(string group, string asset)
        {
            var task = ResManager.LoadAsset<AudioClip>(group, asset);
            if (task.keepWaiting)
                await UniTask.WaitWhile(() => task.keepWaiting && !task.isCanceling);
            if (task.isSuccess)
                return task.asset as AudioClip;
            return null;
        }

        private bool TryRequest(string group, string asset)
        {
            return TryRequest($"{group}:{asset}");
        }

        private bool TryRequest(string res)
        {
            if (_recentAudioRequestMap.TryGetValue(res, out var lastFrameCount))
            {
                if (Time.frameCount < lastFrameCount + 2)
                {
                    return false;
                }
            }
            _recentAudioRequestMap[res] = Time.frameCount;
            return true;
        }

        private void Cancel()
        {
            // _cts_loading?.Cancel();
            // _cts_loading?.Dispose();
            // _cts_loading = new();
        }

        private void Info(string info) => DebugEx.Info($"[AUDIO] {info}");
        private void Error(string err) => DebugEx.Error($"[AUDIO] {err}");

        void IGameModule.Reset()
        {
            if (_sourceBgm == null) _sourceBgm = Camera.main.gameObject.AddComponent<AudioSource>();
            if (_sourceSfx == null) _sourceSfx = Camera.main.gameObject.AddComponent<AudioSource>();
            if (_sourceSfx_loop == null) _sourceSfx_loop = Camera.main.gameObject.AddComponent<AudioSource>();
            StopLoopSound();
        }

        void IGameModule.LoadConfig()
        {
            _OnConfigLoaded();
        }

        void IGameModule.Startup()
        { }
    }
}