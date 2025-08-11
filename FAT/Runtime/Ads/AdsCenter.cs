/*
 * @Author: tang.yan
 * @Description: 广告中心 用于支持广告的加载和播放
 * @Date: 2023-12-04 11:12:10
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using FAT.Platform;

namespace FAT
{
    public class AdsCenter : Singleton<AdsCenter>
    {
        //广告加载、展示等各种原因失败后 重新进行请求的最大次数 超过后不再进行广告请求
        private const int MaxAdsRetry = 6;
        //广告位数据类
        private class AdsUnitData
        {
            public string UnitId;   //广告位id(由平台提供)
            public int FailCount;   //此广告位因加载、展示等各种原因失败的次数 加载成功后失败次数重置为0
            public bool IsLoadReady;//当前广告位中是否已经填充好广告
            public override string ToString()
            {
                return $"AdsUnitData : UnitId = {UnitId}, FailCount = {FailCount}, IsLoadReady = {IsLoadReady}";
            }
        }
        
        //广告位数据List
        private List<AdsUnitData> _adsUnitDataList = new List<AdsUnitData>();
        //是否开启广告自动加载
        private bool _isAutoLoadAds = false;
        //当前是否有任意广告位中的广告准备完成 有的话说明就可以播广告  实际播广告会从准备好的广告位中选择靠前的播放
        private bool _isAdsLoadReady = false;
        //每隔一段时间检查当前已填充完成的广告是否还有效，无效的话需要重新加载
        private float _checkTimeCounter = 0f;
        
        //广告加载间隔时间计数
        private float _loadTimeCounter = 0f;
        //当前广告加载任务
        private AsyncTaskBase _curAdsLoadTask = null;
        //记录上次尝试加载的广告位id 用于识别广告加载成功任务中对应的广告位，以及保证不同广告位可以轮流加载广告
        private string _lastLoadAdsUnitId = "";
        
        //当前广告播放任务
        private AsyncTaskBase _curAdsPlayTask = null;

        //根据平台给的广告id初始化广告位 目前只配了一个广告位
        public void ResetWithAdsUnit(IEnumerable<string> unitIds)
        {
            DebugEx.FormatInfo("AdsCenter::ResetWithAdsUnit {0}", unitIds);
            _adsUnitDataList.Clear();
            foreach(var unitId in unitIds)
            {
                var unitData = new AdsUnitData()
                {
                    UnitId = unitId,
                    FailCount = 0,
                    IsLoadReady = false
                };
                _adsUnitDataList.Add(unitData);
                PlatformSDK.Instance.Adapter.SetDisableB2BAdUnitId(unitId);
            }
            _isAdsLoadReady = false;
            _CheckAllAdsUnitReadyState();
        }
        
        public void ChangeAutoLoadAdsState(bool state)
        {
            DebugEx.FormatInfo("AdsCenter::ChangeAutoLoadAdsState ----> change auto load state to {0}", state);
            _isAutoLoadAds = state;
        }
        
        public bool GetIsAdsLoadReady()
        {
            return _isAdsLoadReady;
        }

        //检查是否正在播广告
        public bool CheckIsPlayingAds()
        {
            return _curAdsPlayTask != null;
        }

        /// <summary>
        /// 外部尝试请求播放广告
        /// </summary>
        /// <param name="bonusAdsType">枚举类型 BonusAdsType.ToString</param>
        /// <returns></returns>
        public AsyncTaskBase TryPlayAdsVideo(string bonusAdsType)
        {
            if(_curAdsPlayTask != null)
            {
                DebugEx.FormatWarning("AdsCenter.TryPlayAdsVideo ----> one already in play");
                return SimpleAsyncTask.AlwaysFail;
            }
            else
            {
                AdsUnitData targetUnit = null;
                foreach (var unitData in _adsUnitDataList)
                {
                    if(unitData.IsLoadReady)
                    {
                        targetUnit = unitData;
                        break;
                    }
                }
                if(targetUnit != null)
                {
                    DebugEx.FormatInfo("AdsCenter.TryPlayAdsVideo ----> begin to play");
                    var ret = _curAdsPlayTask = PlatformSDK.Instance.PlayRewardAds(targetUnit.UnitId, bonusAdsType);
                    Game.Instance.StartCoroutineGlobal(_CoShowAdsGuard(ret));
                    //开始播广告时关闭自动加载广告
                    ChangeAutoLoadAdsState(false);
                    return ret;
                }
                else
                {
                    DebugEx.FormatWarning("AdsCenter.TryPlayAdsVideo ----> no ads ready");
                    return SimpleAsyncTask.AlwaysFail;
                }
            }
        }

        //Tick
        public void AdCenterUpdate(float dt)
        {
            //自动加载广告
            _AutoLoadAds(dt);
            //检查广告播放任务是否完成(结果为成功/失败)
            _CheckAdPlayTaskFinish();
            //每隔一段时间检查当前已填充完成的广告是否还有效，无效的话需要重新加载
            _CheckAdsActive(dt);
        }

        //刷新所有广告位的准备情况
        private void _CheckAllAdsUnitReadyState()
        {
            bool hasChange = false;
            foreach(var unitData in _adsUnitDataList)
            {
                hasChange = hasChange || _CheckAdsUnitReadyState(unitData);
            }
            if (hasChange)
                _CheckAnyAdsLoadReadyState();
        }

        //检查并刷新单个广告位的准备情况
        private bool _CheckAdsUnitReadyState(AdsUnitData unitData)
        {
            bool hasChange = false;
            bool unitReady = PlatformSDK.Instance.IsAdsInited() && PlatformSDK.Instance.IsRewardAdsReady(unitData.UnitId);
            if(unitData.IsLoadReady != unitReady)
            {
                DebugEx.FormatInfo("AdsCenter::_CheckAdsUnitReadyState ----> set unit availability {0}:{1}", unitData.UnitId, unitReady);
                unitData.IsLoadReady = unitReady;
                hasChange = true;
            }
            return hasChange;
        }
        
        //检查并刷新当前是否可以播放任意广告位的广告
        private void _CheckAnyAdsLoadReadyState()
        {
            bool newIsReady = false;
            foreach(var unit in _adsUnitDataList)
            {
                newIsReady = unit.IsLoadReady || newIsReady;
            }
            if(newIsReady != _isAdsLoadReady)
            {
                _isAdsLoadReady = newIsReady;
                DebugEx.FormatInfo("AdsCenter::_CheckWholeRewardVideoStatus ----> set whole availability {0}", _isAdsLoadReady);
                MessageCenter.Get<MSG.AD_READY_ANY>().Dispatch();
            }
        }

        //自动加载广告
        private void _AutoLoadAds(float dt)
        {
            //不允许自动加广告或者当前没有可用广告位时 返回
            if (!_isAutoLoadAds || _adsUnitDataList.Count <= 0)
                return;
            //检查广告加载任务是否完成(结果为成功/失败)
            _CheckAdLoadTaskFinish();
            //检查是否可以创建广告加载任务
            _CheckCanStartAdLoadTask(dt);
        }

        //检查广告加载任务是否完成(结果为成功/失败)
        private void _CheckAdLoadTaskFinish()
        {
            //如果没有加载任务 或 加载任务正在进行中 则返回
            if (_curAdsLoadTask == null || _curAdsLoadTask.keepWaiting)
                return;
            var task = _curAdsLoadTask;
            _curAdsLoadTask = null;
            AdsUnitData unit = null;
            //加载任务结束回调时 根据记录的unitId 尝试找到对应的广告位 并判断是否加载完成
            if(_lastLoadAdsUnitId != "")
            {
                unit = _adsUnitDataList.FindEx((e)=>e.UnitId == _lastLoadAdsUnitId);
            }
            bool isLoadSucc = false;
            if(unit != null)
            {
                //刷新此广告位的广告加载状态
                bool hasChange = _CheckAdsUnitReadyState(unit);
                if (hasChange)
                    _CheckAnyAdsLoadReadyState();
                if(!unit.IsLoadReady)
                {
                    //如果加载失败 累计失败次数
                    DebugEx.FormatWarning("AdsCenter::_CheckAdLoadTaskFinish ----> load ad fail, unitId = {0}, error = {1}", _lastLoadAdsUnitId,  task.error);
                    unit.FailCount ++;
                    //同时设置下次加载广告的加载时间 按平台要求间隔时间依次为 2/4/8/16/32/64s 失败次数超过6次就不再请求广告
                    _loadTimeCounter = Mathf.Pow(2f, Mathf.Min(MaxAdsRetry, unit.FailCount));
                }
                else
                {
                    //如果加载成功 累计的失败次数清0
                    DebugEx.FormatInfo("AdsCenter::_CheckAdLoadTaskFinish ----> load ad success, unitId = {0} ", _lastLoadAdsUnitId);
                    unit.FailCount = 0;
                    isLoadSucc = true;
                }
            }
            else
            {
                _loadTimeCounter = 20f; //如果没找到广告位 设置20秒后再次尝试加载  作为一个保底机制
            }
            DebugEx.FormatInfo("AdsCenter::_CheckAdLoadTaskFinish ----> load ad finish, state = {0}, next load after {1} seconds", isLoadSucc, _loadTimeCounter);
        }
        
        //检查是否可以创建广告加载任务
        private void _CheckCanStartAdLoadTask(float dt)
        {
            //如果当前有正在执行的加载任务 或者 当前广告已经准备完毕 则返回
            if (_curAdsLoadTask != null || _isAdsLoadReady)
                return;
            //检查是否处于间隔时间内 如果是 则返回
            _loadTimeCounter -= dt;
            if (_loadTimeCounter >= 0)
                return;
            //尝试执行加载任务
            //找出本次可以执行加载任务的广告位
            int loadUnitIdx = 0;
            if(_lastLoadAdsUnitId != "")
            {
                for(int i = 0; i < _adsUnitDataList.Count - 1; i ++)
                {
                    if(_adsUnitDataList[i].UnitId == _lastLoadAdsUnitId)
                    {
                        loadUnitIdx = i + 1;
                        break;
                    }
                }
            }
            AdsUnitData loadUnit = null;
            for(int i = 0; i < _adsUnitDataList.Count; i++)
            {
                int idx = (i + loadUnitIdx) % _adsUnitDataList.Count;
                //如果广告位的加载失败次数超过上限 则不再尝试进行加载
                if(_adsUnitDataList[idx].FailCount < MaxAdsRetry)
                {
                    loadUnit = _adsUnitDataList[idx];
                    break;
                }
            }
            //如果最终没有合适的广告位 则报错 并停止广告加载逻辑
            if(loadUnit == null)
            {
                DebugEx.FormatError("AdsCenter::_CheckCanStartAdLoadTask ----> all ads unit is out of retry count, ads unit num = {0}", _adsUnitDataList.Count);
                ChangeAutoLoadAdsState(false);
            }
            else
            {
                var unitId = loadUnit.UnitId;
                _lastLoadAdsUnitId = unitId;
                _curAdsLoadTask = PlatformSDK.Instance.LoadRewardAds(unitId);
            }
        }
        
        //检查广告播放任务是否完成(结果为成功/失败)
        private void _CheckAdPlayTaskFinish()
        {
            //如果没有播放任务 或 播放任务正在进行中 则返回
            if (_curAdsPlayTask == null || _curAdsPlayTask.keepWaiting)
                return;
            if(_curAdsPlayTask.isSuccess)
            {
                DebugEx.FormatInfo("AdsCenter ----> successfully watched a ads");
                DataTracker.TraceUser().TotalADViewTimes(1).Apply();
            }
            _curAdsPlayTask = null;
            //播放完广告允许立刻重新loading下一个广告
            _loadTimeCounter = 0.01f;
            //打开自动加载广告
            ChangeAutoLoadAdsState(true);
            //刷新所有广告位的准备情况
            _CheckAllAdsUnitReadyState();
        }
        
        //每隔一段时间检查当前已填充完成的广告是否还有效，无效的话需要重新加载
        private void _CheckAdsActive(float dt)
        {
            //如果当前广告没有加载完 则无需检查
            if (!_isAdsLoadReady)
            {
                if (_checkTimeCounter > 0f)
                    _checkTimeCounter = 0f;
                return;
            }
            _checkTimeCounter += dt;
            if(_checkTimeCounter >= 60f)    //每隔60s检查一次 如果检查时正在播广告也没关系 _isAutoLoadAds字段会限制是否自动加载广告
            {
                bool hasChange = false;
                foreach(var unitData in _adsUnitDataList)
                {
                    if (unitData.IsLoadReady)
                    {
                        hasChange = hasChange || _CheckAdsUnitReadyState(unitData);
                    }
                }
                if (hasChange)
                    _CheckAnyAdsLoadReadyState();
                _checkTimeCounter = 0;
            }
        }
        
        //开一个保护的界面 防止广告播完后因各种原因卡死导致广告界面关不掉
        private IEnumerator _CoShowAdsGuard(AsyncTaskBase task)
        {
            yield return new WaitForSeconds(1);
            if(!task.keepWaiting)           //task already finished, we close
            {
                yield break;
            }
            UIManager.Instance.OpenWindow(UIConfig.UIAdsWaitResolve, task, 
                new System.Action(() => { PlatformSDK.Instance.AdsEvent_RVClose(true); }));
            yield return task;
        }
    }
}