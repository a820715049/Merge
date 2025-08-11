/*
 * @Author: tang.yan
 * @Description: 广告数据层管理器 
 * @Date: 2023-12-05 11:12:01
 */
using System;
using System.Collections;
using System.Collections.Generic;
using fat.gamekitdata;
using FAT.Platform;
using EL;
using fat.rawdata;

namespace FAT
{
    public class AdsMan : IGameModule, IUserDataHolder, IUpdate, ISecondUpdate
    {
        //目前是否支持看广告
        public bool IsAdsOpen => PlatformSDK.Instance.IsSupportAds() && PlatformSDK.Instance.IsAdsInited();
        //目前是否可以看广告
        public bool CanPlayAds => AdsCenter.Instance.GetIsAdsLoadReady() && !AdsCenter.Instance.CheckIsPlayingAds();
        
        //各个广告口对应数据类 对应配置表为AdSetting@design
        private class AdsEntryData
        {
            public int AdsConfigId;     //广告配置id
            public string AdsName;      //广告名称 用于埋点
            public int LimitWatchCount; //每天可以观看广告的次数
            public int CurWatchCount;   //当天已观看广告的次数
            public int ResetHour;       //每天重置次数的时间(UTC-0)
            public long NextResetTime;  //下次广告次数重置时间

            public bool CanWatchAds()
            {
                return CurWatchCount < LimitWatchCount;
            }
            
            public void AddWatchCount()
            {
                CurWatchCount++;
            }

            public void CheckReset()
            {
                var now = Game.Instance.GetTimestampSeconds();
                if(now >= NextResetTime)
                {
                    NextResetTime = ((now - ResetHour * 3600) / Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay + ResetHour * 3600;
                    CurWatchCount = 0;
                }
            }
        }
        //各个广告口数据List
        private List<AdsEntryData> _adsEntryDataList = new List<AdsEntryData>();

        public void Reset()
        {
            _adsEntryDataList.Clear();
        }

        public void LoadConfig()
        {
            _ResetWithAdsUnit();
        }

        public void Startup() { }

        public void SetData(LocalSaveData archive)
        {
            //开启广告自动加载
            if (IsAdsOpen)
            {
                AdsCenter.Instance.ChangeAutoLoadAdsState(true);
            }
            //初始化广告口数据
            _InitAdsEntryDataList();
            //读取存档数据
            var adsDataList = archive.ClientData.PlayerGameData.AdsDataList;
            if (adsDataList != null)
            {
                foreach (var adsData in adsDataList)
                {
                    var data = _GetAdsEntryDataById(adsData.AdsId);
                    if (data != null)
                    {
                        data.CurWatchCount = adsData.WatchCount;
                        data.NextResetTime = adsData.NextResetTime;
                    }
                }
            }
        }

        public void FillData(LocalSaveData archive)
        {
            var adsDataList = new List<AdsData>();
            foreach (var entryData in _adsEntryDataList)
            {
                var adsData = new AdsData
                {
                    AdsId = entryData.AdsConfigId,
                    WatchCount = entryData.CurWatchCount,
                    NextResetTime = entryData.NextResetTime
                };
                adsDataList.Add(adsData);
            }
            archive.ClientData.PlayerGameData.AdsDataList.AddRange(adsDataList);
        }
        
        /// <summary>
        /// 检查是否显示广告口
        /// </summary>
        /// <param name="adsId">广告口id</param>
        public bool CheckCanWatchAds(int adsId)
        {
            var entryData = _GetAdsEntryDataById(adsId);
            if (entryData == null)
                return false;
            return entryData.CanWatchAds() && IsAdsOpen && CanPlayAds;
        }
        
        //获取广告口名称(埋点用)
        public string GetAdsName(int adsId)
        {
            var entryData = _GetAdsEntryDataById(adsId);
            if (entryData == null)
                return "";
            return entryData.AdsName;
        }

        /// <summary>
        /// 传入广告口id 播广告 播完后执行回调方法供外部判断
        /// </summary>
        /// <param name="adsId">AdsEntryData.AdsConfigId</param>
        /// <param name="finishCb">广告播放流程结束回调方法 参数为 广告id 和 是否成功</param>
        public void TryPlayAdsVideo(int adsId, Action<int, bool> finishCb)
        {
            var entryData = _GetAdsEntryDataById(adsId);
            if (entryData == null)
                return;
            if (!IsAdsOpen || !CanPlayAds)
            {
                DebugEx.FormatInfo("AdsMan::TryPlayAdsVideo not play ads! ----> IsAdsOpen = {0}, CanPlayAds = {1} ", IsAdsOpen, CanPlayAds);
                Game.Manager.commonTipsMan.ShowPopTips(Toast.IaaPlayingFail);
                return;
            }
            if (!entryData.CanWatchAds())
            {
                //没有可观看次数
                return;
            }
            Game.Instance.StartAsyncTaskGlobal(_TryPlayAdsVideo(entryData, finishCb));
        }

        //播广告测试接口
        public void TestPlayAds()
        {
            if (!IsAdsOpen || !CanPlayAds)
            {
                DebugEx.FormatInfo("AdsMan::TestPlayAds not play ads! ----> IsAdsOpen = {0}, CanPlayAds = {1} ", IsAdsOpen, CanPlayAds);
                Game.Manager.commonTipsMan.ShowPopTips(Toast.IaaPlayingFail);
                return;
            }
            var entryData = new AdsEntryData()
            {
                AdsName = "AdsTest"
            };
            Game.Instance.StartAsyncTaskGlobal(_TryPlayAdsVideo(entryData, null));
        }
        
        public void Update(float dt)
        {
            AdsCenter.Instance.AdCenterUpdate(dt);
        }

        public void SecondUpdate(float dt)
        {
            if (_adsEntryDataList != null)
            {
                foreach (var entryData in _adsEntryDataList)
                {
                    entryData.CheckReset();
                }
            }
        }

        #region 广告业务层逻辑

        private void _InitAdsEntryDataList()
        {
            _adsEntryDataList.Clear();
            var adsConfig = Game.Manager.configMan.GetAdSettingMap();
            foreach (var config in adsConfig.Values)
            {
                var data = new AdsEntryData()
                {
                    AdsConfigId = config.Id,
                    AdsName = config.Name,
                    LimitWatchCount = config.Limit,
                    CurWatchCount = 0,
                    ResetHour = config.RefreshUtc,
                    NextResetTime = 0
                };
                _adsEntryDataList.Add(data);
            }
        }
        
        private AdsEntryData _GetAdsEntryDataById(int adsId)
        {
            return _adsEntryDataList.FindEx(x => x.AdsConfigId == adsId);
        }

        #endregion
        
        #region 广告加载及播放逻辑
        private IEnumerator _TryPlayAdsVideo(AdsEntryData entryData, Action<int, bool> finishCb)
        {
            var task = new SingleTaskWrapper();
            yield return task;
            string bonusAdsType = entryData.AdsName;
            DebugEx.FormatInfo("AdsMan::_TryPlayAdsVideo ----> start to watch ads, bonusAdsType = {0}", bonusAdsType);
            //播广告前关闭所有音乐
            var isMusicOn = SettingManager.Instance.MusicIsOn;
            if (isMusicOn)
                Game.Manager.audioMan.Pause();
            //播广告
            AsyncTaskBase adsTask = AdsCenter.Instance.TryPlayAdsVideo(bonusAdsType);
            task.SetTask(adsTask);
            yield return adsTask;
            //广告播完后开启音乐
            if (isMusicOn)
                Game.Manager.audioMan.UnPause();
            //检查广告的播放结果
            if(adsTask.isSuccess)
            {
                DebugEx.FormatInfo("AdsMan::_TryPlayAdsVideo ----> play ads Success ! bonusAdsType = {0}", bonusAdsType);
                task.ResolveSuccess();
                //增加广告已观看次数
                entryData.AddWatchCount();
                //执行外部回调
                finishCb?.Invoke(entryData.AdsConfigId, true);
            }
            else
            {
                DebugEx.FormatInfo("AdsMan::_TryPlayAdsVideo ----> play ads Fail ! bonusAdsType = {0}", bonusAdsType);
                task.ResolveError();
                //这里播广告失败弹提示没有区分更多情况 adsTask失败的原因可能是已经有在播的广告了或者广告没加载好或者广告没看完
                Game.Manager.commonTipsMan.ShowPopTips(Toast.IaaNotComplete);
                //执行外部回调
                finishCb?.Invoke(entryData.AdsConfigId, false);
            }
        }

        private void _ResetWithAdsUnit()
        {
            IEnumerable<string> unitIds = null;
            //虽然名字是Admob 但实际底层逻辑用的类型是Max 
#if UNITY_IOS
            unitIds = Game.Manager.configMan.globalConfig.AdIdIosAdmobAsia;
#else
            unitIds = Game.Manager.configMan.globalConfig.AdIdAndroidAdmobAsia;
#endif
            AdsCenter.Instance.ResetWithAdsUnit(unitIds);
        }

        #endregion
    }
}