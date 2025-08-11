/*
 * @Author: tang.yan
 * @Description: 广告SDK接口 
 * @Doc: https://podd-docs.diandian.info/client/integration/unity/10_advertising.html
 * @Date: 2023-12-04 19:12:56
 */
using UnityEngine;
using System.Collections;
using EL;
using centurygame;

namespace FAT.Platform
{
    public partial class PlatformSDK
    {
        private class AdsTask : SimpleAsyncTask
        {
            public string duty = CGAdvertising.CGAdvertisingDuty.RewardAd.ToString();    //默认CGAdvertisingDuty为RewardAd
            public string type = CGAdvertising.CGAdvertisingType.CGMax.ToString();       //默认CGAdvertisingType为CGMax
            public string watchType;
            public string adsUnitId;
            public bool rewarded = false;
        }

        private AdsTask _curAdsLoadTask;
        private AdsTask _curAdsPlayTask;

        public bool IsAdsInited()
        {
#if UNITY_EDITOR
            return true;
#else
            return Adapter.IsAdsInited(); //mAdsInitSuccess;
#endif
        }

        public bool IsSupportAds()
        {
#if UNITY_EDITOR
            return true;
#else
            return Adapter.IsSupportAds();
#endif
        }

        public AsyncTaskBase PlayRewardAds(string unitId, string watchType)
        {
            if (_curAdsPlayTask != null)
            {
                DebugEx.FormatWarning("PlatformSDK.PlayRewardAds ----> one already in play");
                return SimpleAsyncTask.AlwaysFail;
            }
            else
            {
                var ret = _curAdsPlayTask = new AdsTask()
                {
                    watchType = watchType,
                    adsUnitId = unitId
                };
#if UNITY_EDITOR
                StartCoroutine(_CoTestAds());
#else
                Adapter.PlayRewardAds(unitId);
#endif
                return ret;
            }
        }

        public bool IsRewardAdsReady(string unitId)
        {
#if UNITY_EDITOR
            return true;
#else
            return Adapter.IsRewardAdsReady(unitId);
#endif
        }

        public AsyncTaskBase LoadRewardAds(string unitId)
        {
            if (_curAdsLoadTask != null && _curAdsLoadTask.keepWaiting)
            {
                return _curAdsLoadTask;
            }
            else
            {
                var ret = _curAdsLoadTask = new AdsTask()
                {
                    adsUnitId = unitId,
                    rewarded = false,
                };
                DebugEx.FormatInfo("PlatformSDK::LoadRewardAds ----> start load ads, unitId = {0}", unitId);
                Adapter.LoadRewardAds(unitId);
                return ret;
            }
        }

        public void AdsEvent_RVLoadFinish(bool suc, string msg)
        {
            if (suc)
                _curAdsLoadTask?.ResolveTaskSuccess();
            else
                _curAdsLoadTask?.ResolveTaskFail();

            if (_curAdsLoadTask != null)
            {
                DebugEx.FormatInfo("PlatformSDK::AdsEvent_LoadFinish ----> {0}, {1}, {2}", _curAdsLoadTask.adsUnitId,
                    suc, msg);
                // track
                var task = _curAdsLoadTask;
                if (suc)
                    DataTracker.TrackAdLoadingSuccess(task.adsUnitId, task.duty);
                else
                    DataTracker.TrackAdLoadingFail(task.adsUnitId, task.duty, msg);
            }
        }

        public void AdsEvent_RVClose(bool forcely)
        {
            if (_curAdsPlayTask != null)
            {
                DebugEx.FormatInfo("PlatformSDK::AdsEvent_Close ----> {0}, {1}, {2}", forcely, _curAdsPlayTask.duty, _curAdsPlayTask.watchType);
                DataTracker.TrackAdViewSuccess(_curAdsPlayTask.duty, _curAdsPlayTask.watchType);
                if (_curAdsPlayTask.rewarded)
                {
                    _curAdsPlayTask.ResolveTaskSuccess();
                }
                else
                {
                    _curAdsPlayTask.ResolveTaskCancel();
                }

                _curAdsPlayTask = null;
            }
        }

        public void AdsEvent_RVReward()
        {
            if (_curAdsPlayTask != null)
            {
                DebugEx.FormatInfo("PlatformSDK::AdsEvent_Reward ----> {0}", _curAdsPlayTask.adsUnitId);
                _curAdsPlayTask.rewarded = true;
            }
        }

        public void AdsEvent_RVOpen()
        {
            if (_curAdsPlayTask != null)
            {
                DebugEx.FormatInfo("PlatformSDK::AdsEvent_Open ----> {0}", _curAdsPlayTask.adsUnitId);
                DataTracker.TrackAdOpenAds(_curAdsPlayTask.duty, _curAdsPlayTask.watchType);
            }
        }

        public void AdsEvent_RVOpenFail(long commonError, string msg)
        {
            if (_curAdsPlayTask != null)
            {
                DebugEx.FormatInfo("PlatformSDK::AdsEvent_OpenFail ----> {0}, {1}, {2}", _curAdsPlayTask.adsUnitId,
                    commonError, msg);
                DataTracker.TrackAdShowFail(_curAdsPlayTask.adsUnitId, _curAdsPlayTask.duty,
                    _curAdsPlayTask.watchType, msg);
                _curAdsPlayTask.ResolveTaskFail(commonError, msg);
                _curAdsPlayTask = null;
            }
        }

        private IEnumerator _CoTestAds()
        {
            SimpleAsyncTask testTask = new SimpleAsyncTask();
            UIManager.Instance.OpenWindow(UIConfig.UIAdsEditorTest, testTask);
            yield return testTask;
            if (_curAdsPlayTask != null)
            {
                _curAdsPlayTask.ResolveTaskSuccess();
                _curAdsPlayTask = null;
            }
        }
    }
}