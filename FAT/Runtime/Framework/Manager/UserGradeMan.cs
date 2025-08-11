/*
 * @Author: tang.yan
 * @Description: 用户分层数据管理器 
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/kdkr7xvap4wff3d5
 * @Date: 2024-03-01 14:03:29
 */
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using UnityEngine;
using FAT.RemoteAnalysis;
using fat.rawdata;

namespace FAT
{
    public class UserGradeMan : IGameModule, IUserDataHolder
    {
        //标记目前tag标签信息是否已过期，如果过期则需要重新请求服务器最新的tag
        //如果业务逻辑需要受制于此标记位，则当标记位显示tag已过期时，需要自行控制不走时间刷新相关的逻辑
        public bool IsTagExpire => _CheckTagExpire() && _CheckApiExpire();
        private bool _isTagExpire = true;   //使用标记位标记一下 便于在自定义时机锁住更新逻辑
        private bool _isNetSync = false;    //记录网络目前的同步状态 如果没同步 则标签认为也是过期状态
        
        //用户不同分层维度上的档位信息 key:UserGrade.id  value:服务器发来的UserGrade对应的档位值
        private Dictionary<int, int> _userGradeValueDict = new Dictionary<int, int>();
        //目前服务器发来的tag信息，其真正被计算更新时的时间戳，客户端用这个时间戳和当前时间戳比较，判断两个时间戳是否位于同一天(会基于UTC10偏移)
        //如果位于同一天 说明tag信息未过期 如果不在同一天，说明客户端此时的tag信息过期了，需要向服务器请求新的tag。
        //在请求新的tag信息到真正协议数据回来的这段期间内，客户端会使用IsTagExpire这个标记位，锁住相关业务逻辑的时间刷新。
        //避免tag标签还没刷成最新的，但业务逻辑却已经处理完跨天刷新逻辑了
        private long _curTagUpdateTs = 0;
        private long _tagExpireTs = 0;      //基于服务器发来的时间戳计算出来的此tag的过期时间

        public void Reset()
        {
            _isTagExpire = true;
            _isNetSync = false;
            _userGradeValueDict.Clear();
            _curTagUpdateTs = 0;
            _tagExpireTs = 0;
            _curAPIReqTs = 0;
            _apiExpireTs = 0;
            _curAPIReqWaitTs = 0;
        }

        public void LoadConfig()
        {
            _InitAPIReqInfo();
        }

        public void Startup() { }
        
        public void SetData(LocalSaveData archive)
        {
            // 登录时服务器LoginResp协议中会有最新的tag信息 用于赋值UserTagData
            
            //读取存档中难度API相关内容
            _TryUseAPIArchiveData(archive);
        }

        public void FillData(LocalSaveData archive)
        {
            var userTagData = new UserTagData();
            foreach (var tagData in _userGradeValueDict)
            {
                userTagData.Data[tagData.Key] = tagData.Value;
            }
            userTagData.UpdateTS = _curTagUpdateTs;
            archive.PlayerBaseData.UserTagData = userTagData;
            //难度API相关存档
            var userTagApiData = new UserTagApiData();
            if (_userGradeAPIDict != null)
            {
                foreach (var apiData in _userGradeAPIDict)
                {
                    userTagApiData.Data[apiData.Key] = apiData.Value.tagValue;
                }
            }
            userTagApiData.ReqTS = _curAPIReqTs;
            archive.ClientData.PlayerGameData.UserTagApiData = userTagApiData;
        }

        //网络状态发生变动时，也记录一下
        public void MarkTagExpire(bool isNetSync)
        {
            _isNetSync = isNetSync;
        }

        //接收到服务器发来的用户不同分层维度上的档位信息数据
        public void OnReceiveUserTagInfo(UserTagData userTagData)
        {
            TryApplyUserTagInfo(userTagData);
            //尝试应用debug信息
            _TryApplyUseTagDebugInfo();
            //目前暂定在服务器的RFM信息返回后才开始尝试请求API  若后续策划对请求时机有要求 则再修改 不过最大限制还是在于post消息时的延迟
            _TryReqAPIInfo();
        }

        private void TryApplyUserTagInfo(UserTagData userTagData)
        {
            if (userTagData != null && userTagData.Data.Count > 0)
            {
                _isTagExpire = false;
                _userGradeValueDict.Clear();
                foreach (var tagData in userTagData.Data)
                {
                    _userGradeValueDict[tagData.Key] = tagData.Value;
                }
                //刷新tag过期时间
                _RefreshTagExpireTs(userTagData.UpdateTS);
            }
            else
            {
                _isTagExpire = true;
                DebugEx.FormatError("UserGradeMan.OnReceiveUserTagInfo : userTagData is null !");
            }
        }

        public int GetUserGradeValue(int gradeId)
        {
            //debug模式下返回debug dict
            if (IsIgnoreTagExpire)
            {
                return _GetDebugGetUserGradeValue(gradeId);
            }
            //否则返回服务器发来的对应信息
            if (_userGradeValueDict.TryGetValue(gradeId, out var value))
            {
                //获取UserGrade时尝试使用难度API获取到的值进行替换
                if (_CheckApiUsable() && _userGradeAPIDict.TryGetValue(gradeId, out var info) && info.conf.IsApiUse)
                {
                    return info.tagValue;
                }
                //找不到时返回原值
                return value;
            }
            else
            {
                //找不到时 读取配置中的默认值 保证游戏逻辑不会出错  实际情况下不会出现这种情况
                return Game.Manager.configMan.GetUserGradeConfig(gradeId)?.DefaultGradeValue ?? 0;
            }
        }

        //传入具体业务逻辑关联配置表中xxxGrpId字段对应的值，接口会返回"目标表格中的id"。
        //调用方自行确定目标表格到底指向哪个具体表格，直接拿id去目标表格中查找对应数据，拿到的数据自行处理
        //注意：这里的逻辑永远都是根据目前最新的tag标签进行的，即tag标签可能随时会变，具体业务逻辑如果有一旦设置了tag标签就不能改变的需求，则需要自行处理(存档)
        //没有找到时默认返回0
        public int GetTargetConfigDataId(int mapId)
        {
            var gradeMapConf = Game.Manager.configMan.GetGradeIndexMappingConfig(mapId);
            if (gradeMapConf == null)
                return 0;
            var gradeGroupConf = Game.Manager.configMan.GetUserGradeGroupConfig(gradeMapConf.UserGradeGroupId);
            if (gradeGroupConf == null)
                return 0;
            var targetKey = "";
            var userGrades = gradeGroupConf.UserGradeId;
            var length = userGrades.Count;
            for (var i = 0; i < length; i++)
            {
                var value = GetUserGradeValue(userGrades[i]);
                targetKey = i == 0 ? string.Concat(targetKey, value) : string.Concat(targetKey, "_", value);
            }
            if (gradeGroupConf.UserGradeGroupValue.TryGetValue(targetKey, out var targetValue))
            {
                return gradeMapConf.IndexInfo.TryGetValue(targetValue, out var targetId) ? targetId : 0;
            }
            else
            {
                return 0;
            }
        }

        private void _RefreshTagExpireTs(long tagUpdateTs)
        {
            _curTagUpdateTs = tagUpdateTs;
            //根据服务器发来的tag的生成时间，计算当前tag的过期时间
            var offsetHour = Game.Manager.configMan.globalConfig.UserRecordRefreshUtc;
            _tagExpireTs = ((_curTagUpdateTs - offsetHour * 3600) / Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay + offsetHour * 3600;
        }
        
        private bool _CheckTagExpire()
        {
            //如果debug中设置忽略tag 则认为tag永远没过期
            if (IsIgnoreTagExpire)
                return false;
            //网络不同步时 认为标签过期
            if (!_isNetSync)
            {
                return true;
            }
            //如果tag没过期 则检查一下tag过期时间
            if (!_isTagExpire)
            {
                _isTagExpire = Game.Instance.GetTimestampSeconds() >= _tagExpireTs;
            }
            return _isTagExpire;
        }

        #region Debug相关

        //debug面板中自定义是否忽略服务器发来的tag信息
        //若为true(忽略) : 1、不会检查tag过期状态，依赖tag的活动会正常触发。 2、tag标签使用默认配置，也可以在debug面板中修改
        //若为false(不忽略) : 1、会检查tag过期信息，直到收到最新tag前都不会触发依赖tag的活动. 2、tag标签使用服务器发来的信息，debug面板中修改无效
        public bool IsIgnoreTagExpire {
#if UNITY_EDITOR
            get => PlayerPrefs.GetInt(nameof(IsIgnoreTagExpire), 0) > 0;
            private set => PlayerPrefs.SetInt(nameof(IsIgnoreTagExpire), value ? 1 : 0);
#else
            get; private set;
#endif
        }
        //只在debug期间使用 用户不同分层维度上的档位信息 key:UserGrade.id  value:服务器发来的UserGrade对应的档位值
        private Dictionary<int, int> _debugTagValueDict = new Dictionary<int, int>();
        
        //debug面板中自定义是否忽略服务器发来的tag信息的过期状态
        public void DebugSetIgnoreTagExpire()
        {
            IsIgnoreTagExpire = !IsIgnoreTagExpire;
        }

        //debug面板中修改指定测试ID的分组情况
        public void DebugChangeUserTag(int userGradeId, int userGradeValue)
        {
            if (userGradeValue <= 0)
                userGradeValue = 1;
            if (_debugTagValueDict.ContainsKey(userGradeId))
                _debugTagValueDict[userGradeId] = userGradeValue;
        }
        
        public string DebugUserTagInfo()
        {
            using(ObjectPool<System.Text.StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                if (IsIgnoreTagExpire)
                {
                    foreach (var info in _debugTagValueDict)
                    {
                        sb.AppendFormat("userGrade[{0}]:[{1}], ", info.Key, info.Value);
                    }
                }
                else
                {
                    foreach (var info in _userGradeValueDict)
                    {
                        sb.AppendFormat("userGrade[{0}]:[{1}], ", info.Key, info.Value);
                    }
                }
                return sb.ToString();
            }
        }
        
        //尝试应用debug信息
        private void _TryApplyUseTagDebugInfo()
        {
            if (!IsIgnoreTagExpire) return;
            //只在dict为空时 使用服务器信息 否则全部使用自定义的
            if (_debugTagValueDict.Count <= 0)
            {
                foreach (var info in _userGradeValueDict)
                {
                    _debugTagValueDict.Add(info.Key, info.Value);
                }
            }
        }

        private int _GetDebugGetUserGradeValue(int gradeId)
        {
            if (_debugTagValueDict.TryGetValue(gradeId, out var value))
            {
                return value;
            }
            else
            {
                return Game.Manager.configMan.GetUserGradeConfig(gradeId)?.DefaultGradeValue ?? 0;
            }
        }

        #endregion

        #region 难度API相关

        private class UserGardeAPIData
        {
            public UserGrade conf;
            public int tagValue;
        }
        //API相关
        private Dictionary<int, UserGardeAPIData> _userGradeAPIDict;
        private UserGradeRemoteWrapper _wrapper;
        private long _curAPIReqTs = 0;      //记录当前难度API发起请求的时间戳
        private long _curAPIReqWaitTs = 0;  //基于难度API发起请求的时间计算出来的等待API请求的时间
        private long _apiExpireTs = 0;      //基于难度API发起请求的时间计算出来的过期时间

        private void _InitAPIReqInfo()
        {
            _userGradeAPIDict?.Clear();
            var userGardeConf = Game.Manager.configMan.GetUserGradeConfigs();
            if (userGardeConf == null) return;
            _userGradeAPIDict ??= new Dictionary<int, UserGardeAPIData>();
            foreach (var conf in userGardeConf)
            {
                if (conf.IsApiSend)
                {
                    var data = new UserGardeAPIData
                    {
                        conf = conf,
                        tagValue = 0,
                    };
                    _userGradeAPIDict.Add(conf.Id, data);
                }
            }
            if (_userGradeAPIDict.Count > 0)
                _wrapper ??= new UserGradeRemoteWrapper();
        }
        
        private void _TryReqAPIInfo()
        {
            //配置上不可以请求API时 返回
            if (_userGradeAPIDict == null || _userGradeAPIDict.Count <= 0 || _wrapper == null) return;
            //刷新过期时间
            _RefreshAPIExpireTs();
        }
        
        private void _RefreshAPIExpireTs()
        {
            var isExpire = false;
            var curTime = Game.Instance.GetTimestampSeconds();
            if (_curAPIReqTs <= 0)
            {
                //存档中从未记录过时 认为过期 请求一次API
                isExpire = true;
            }
            else if (curTime >= _apiExpireTs)
            {
                //当前时间大于过期时间时 请求API
                isExpire = true;
            }
            //如果过期了 则请求新的
            if (isExpire)
            {
                //记录当前时间为请求时间
                _curAPIReqTs = curTime;
                //计算过期时间
                _CalAPIExpireTs();
                //过期时 计算等待API返回值的时间 并请求难度API
                _curAPIReqWaitTs = curTime + Game.Manager.configMan.globalConfig.DiffApiTimeout / 1000;
                //请求API时打点
                DataTracker.api_diff_send.Track();
                foreach (var info in _userGradeAPIDict)
                {
                    _wrapper.SendRequest(info.Value.conf.ModelVersion, _OnReqAPISuccess);
                }
            }
        }
        
        //根据配置计算过期时间
        private void _CalAPIExpireTs()
        {
            //根据服务器发来的tag的生成时间，计算当前tag的过期时间
            var offsetHour = Game.Manager.configMan.globalConfig.UserRecordRefreshUtc;
            var apiRefreshType = 0;
            foreach (var info in _userGradeAPIDict)
            {
                var conf = info.Value.conf;
                if (conf.ApiRefreshType > 0)
                {
                    apiRefreshType = conf.ApiRefreshType;
                    break;
                }
            }
            if (apiRefreshType <= 0)
                return;
            //每天的UTC-0时区的10点刷新
            if (apiRefreshType == 1)
            {
                _apiExpireTs = ((_curAPIReqTs - offsetHour * 3600) / Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay + offsetHour * 3600;
            }
            //每周一的UTC-0时区的10点刷新
            else if (apiRefreshType == 2)
            {
                // 1. 计算从 Unix 纪元算起的“整天数”
                long daysSinceEpoch = _curAPIReqTs / Constant.kSecondsPerDay;

                // 2. 根据 Unix 纪元（1970-01-01 是周四）推算今天是周几
                //    DayOfWeek: Sunday=0, Monday=1, … Thursday=4
                const int EpochThursday = 4;
                int currentDow = (int)((daysSinceEpoch + EpochThursday) % 7);

                // 3. 计算到下一个周一还要几天
                int daysUntilMonday = (1 - currentDow + 7) % 7;
                if (daysUntilMonday == 0)
                {
                    // 如果今天就是周一，则下周一要 +7 天
                    daysUntilMonday = 7;
                }

                // 4. 计算下周一的“整天数”及对应的秒级起点
                long nextMondayDayCount = daysSinceEpoch + daysUntilMonday;
                long nextMondayBaseTs  = nextMondayDayCount * Constant.kSecondsPerDay;

                // 5. 加上 offsetHour 小时后得到最终到期时间戳
                _apiExpireTs = nextMondayBaseTs + offsetHour * 3600L;
            }
        }

        private void _TryUseAPIArchiveData(LocalSaveData archive)
        {
            if (_userGradeAPIDict == null || _userGradeAPIDict.Count <= 0)
                return;
            var userTagApiData = archive?.ClientData?.PlayerGameData?.UserTagApiData;
            if (userTagApiData != null && userTagApiData.Data.Count > 0)
            {
                foreach (var apiData in userTagApiData.Data)
                {
                    var key = apiData.Key;
                    if (_userGradeAPIDict.TryGetValue(key, out var value))
                    {
                        value.tagValue = apiData.Value;
                    }
                }
                _curAPIReqTs = userTagApiData.ReqTS;
                _CalAPIExpireTs();
            }
        }

        private void _OnReqAPISuccess(int diff)
        {
            var curTime = Game.Instance.GetTimestampSeconds();
            var canUse = curTime <= _curAPIReqWaitTs;
            //获取到API结果后置0
            _curAPIReqWaitTs = 0;
            //收到API结果时若已经超时 则不再使用此结果
            if (canUse && _userGradeAPIDict != null)
            {
                foreach (var data in _userGradeAPIDict)
                {
                    //检查收到的API难度值是否在配置上被允许
                    var isConfAllow = false;
                    var conf = data.Value.conf;
                    if (conf != null)
                    {
                        foreach (var value in conf.UserGradeValue.Values)
                        {
                            if (value == diff)
                            {
                                isConfAllow = true;
                                break;
                            }
                        }
                    }
                    //如果允许则可用；如果不允许则弃掉这个值，同时按超时处理
                    if (isConfAllow)
                    {
                        data.Value.tagValue = diff;
                    }
                    else
                    {
                        canUse = false;
                        break;
                    }
                }
                //api返回值可用时立即存档
                if (canUse)
                {
                    Game.Manager.archiveMan.SendImmediately(true);
                }
            }
            //收到结果时打点
            var eventDiff = GetUserGradeValue(Game.Manager.configMan.globalConfig.OrderApiLiveopsGrade);
            DataTracker.api_diff_return.Track(diff, !canUse, eventDiff);
        }

        //检查难度API是否过期
        private bool _CheckApiExpire()
        {
            //配置上不可以请求API时 认为永远没过期
            if (_userGradeAPIDict == null || _userGradeAPIDict.Count <= 0)
                return false;
            var curTime = Game.Instance.GetTimestampSeconds();
            //若正在请求 且请求超时 则认为没过期
            if (_curAPIReqWaitTs > 0 && curTime > _curAPIReqWaitTs)
                return false;
            var isExpire = curTime >= _apiExpireTs; //过期
            var isWait = curTime <= _curAPIReqWaitTs;   //正在等待结果
            return isExpire || isWait;
        }
        
        //检查难度API是否可用
        private bool _CheckApiUsable()
        {
            //配置上不可以请求API时 认为不可用
            if (_userGradeAPIDict == null || _userGradeAPIDict.Count <= 0)
                return false;
            var curTime = Game.Instance.GetTimestampSeconds();
            //若正在请求 且请求超时 则认为不可用
            if (_curAPIReqWaitTs > 0 && curTime > _curAPIReqWaitTs)
                return false;
            var isExpire = curTime >= _apiExpireTs; //过期
            var isWait = curTime <= _curAPIReqWaitTs;   //正在等待结果
            //过期或者正在请求时 不可用
            return !isExpire && !isWait;
        }

        public void DebugResetApiExpireTs()
        {
            _apiExpireTs = 0;
        }

        public float DebugDelayTime { private set; get; }
        public void DebugSetAPIDelayTime(float delayTime)
        {
            if (delayTime <= 0) return;
            DebugDelayTime = delayTime;
        }

        #endregion
    }
}

