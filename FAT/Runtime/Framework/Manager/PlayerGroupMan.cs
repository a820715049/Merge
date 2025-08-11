/*
 * @Author: tang.yan
 * @Description: AB测试/用户分组测试 管理器
 * @Date: 2024-02-28 15:02:04
 */
using System.Collections.Generic;
using System.Linq;
using fat.gamekitdata;
using fat.rawdata;
using EL;
using UnityEngine;

namespace FAT
{
    public class PlayerGroupMan : IGameModule, IUserDataHolder, IPreSetUserDataListener
    {
        //当前玩家在每个测试组中的分组情况
        //list中的 index+1代表测试组id(配置表中id从1开始递增)  各个index对应的值代表位于该测试组的小组编号 默认0代表还没有小组
        private List<int> _playerGroups = new List<int>();

        public void Reset() { }

        public void LoadConfig() { }

        public void Startup() { }
        
        public void OnPreSetUserData(LocalSaveData archive)
        {
            var baseData = archive.PlayerBaseData;
            _playerGroups.Clear();
            _playerGroups.AddRange(baseData.Groups);
            //检查分组
            _CheckAssignGroups(archive);
            //应用tag
            _AdjustAbTestConfig();
        }
        
        public void SetData(LocalSaveData archive)
        {
        }

        public void FillData(LocalSaveData archive)
        {
            var baseData = archive.PlayerBaseData;
            baseData.Groups.Clear();
            baseData.Groups.AddRange(_playerGroups);
        }
        
        /// 传入测试组id 返回玩家位于该测试组中的哪个小组中(小组编号) 默认为0代表还没在这个测试组中有小组编号
        public int GetPlayerGroup(int id)
        {
            return _playerGroups.GetElementEx(id - 1, ArrayExt.OverflowBehaviour.Default);
        }

        public string DebugABTestInfo()
        {
            using(ObjectPool<System.Text.StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                for(int i = 0; i < _playerGroups.Count; i++)
                {
                    sb.AppendFormat("group{0}[{1}], ", i + 1, _playerGroups[i]);
                }
                return sb.ToString();
            }
        }

        //debug面板中修改指定测试ID的分组情况
        public void DebugChangeGroup(int testId, int groupIndex)
        {
            if (groupIndex <= 0)
                groupIndex = 1;
            _playerGroups[testId - 1] = groupIndex;
            Game.Manager.archiveMan.SendImmediately(true);
            Game.Instance.AbortRestart("Change success! \nConfirm to Restart!", 0);
        }
        
        private void _CheckAssignGroups(LocalSaveData archive)
        {
            var groups = Game.Manager.configMan.GetPlayerGroupConfigs();
            bool hasNoGroup = false;
            foreach(var group in groups)
            {
                //0代表还没分组
                if(GetPlayerGroup(group.Id) == 0)
                {
                    _AssignGroup(group, archive);
                    hasNoGroup = true;
                }
            }
            //如果触发了分组逻辑则打点
            if (hasNoGroup)
            {
                if (_playerGroups != null && _playerGroups.Count > 0)
                {
                    var dictionary = _playerGroups
                        .Select((value, index) => new { index, value })
                        .ToDictionary(item => (item.index + 1), item => item.value);
                    var jsonStr = HSMiniJSON.Json.Serialize(dictionary);
                    DataTracker.TrackABChange(jsonStr);
                }
                DataTracker.TraceUser().auto_group().Apply();
            }
        }

        private void _AdjustAbTestConfig()
        {
            var playerGroupConfigs = Game.Manager.configMan.GetPlayerGroupConfigs();
            List<string> tags = new List<string>();
            foreach(var cfg in playerGroupConfigs)
            {
                if(cfg.ConfigGroups.TryGetValue(GetPlayerGroup(cfg.Id), out var tag))
                {
                    tags.Add(tag);
                }
            }
            tags.Reverse();         //playergroup表里，id越大优先级越大，越要排在前面
            Game.Manager.configMan.SetAbTags(tags);
        }

        private void _AssignGroup(PlayerGroup config, LocalSaveData archive)
        {
            var g = 0;
            //如果是老用户且有指定分组 则直接指定
            if(!Game.Manager.archiveMan.isNewUser && config.OldUserGroup > 0)
            {
                DebugEx.FormatInfo("PlayerGroupMan::_CalculateGroup ----> use old user group {0}", config.OldUserGroup);
                g = config.OldUserGroup;
            }
            //如果是新用户或者没有指定老用户分组 则走分组逻辑
            else
            {
                var rule = _GetRuleForGroup(config.Id, _GetOrCreatePlayerInfo());
                DebugEx.FormatInfo("PlayerGroupMan::_CalculateGroup ----> calculate with {0}, rule: {1}", config.Id, rule);
                g = _CalculateGroup(config, archive, rule);
            }
            //最后得出来的分组编号大于0说明有效 将该编号填入对应测试组中
            if(g > 0)
            {
                DebugEx.FormatInfo("PlayerGroupMan::_AssignGroup ----> group {0} set as {1}", config.Id, g);
                //避免配置没有从1开始
                while(_playerGroups.Count < config.Id)
                {
                    _playerGroups.Add(0);
                }
                //将得到的分组编号填入对应测试组
                _playerGroups[config.Id - 1] = g;
            }
        }

        private class PlayerInfo
        {
            public string platform;
            public int channel;
        }

        private PlayerInfo mPlayerInfo;

        private PlayerInfo _GetOrCreatePlayerInfo()
        {
            if(mPlayerInfo != null)
            {
                return mPlayerInfo;
            }
            var ret = mPlayerInfo = new PlayerInfo();
            ret.channel = _GetChannel();
            ret.platform = _GetPlatform();
            DebugEx.FormatInfo("PlayerGroupMan::_CreatePlayerInfo ----> {0}", JsonUtility.ToJson(ret));
            return ret;
        }

        private int _GetChannel()
        {
            return Platform.PlatformSDK.Instance.GetSDKChannelId();
        }

        private string _GetPlatform()
        {
            return 
            #if UNITY_EDITOR
            "editor"
            #elif UNITY_ANDROID
            "android"
            #elif UNITY_IOS
            "ios"
            #endif
            ;
        }
        
        private PlayerGroupRule _GetRuleForGroup(int groupId, PlayerInfo info)
        {
            var rules = Game.Manager.configMan.GetPlayerGroupRuleConfigs();
            foreach(var rule in rules)
            {
                if(rule.GroupId != groupId)
                {
                    continue;
                }
                if(rule.Platform.Count > 0 && !rule.Platform.Contains(info.platform))
                {
                    continue;
                }
                if(rule.Channel > 0 && rule.Channel != info.channel)
                {
                    continue;
                }
                return rule;
            }
            return null;
        }

        private DeterministicRandom mRandom = new DeterministicRandom();
        private int _CalculateGroup(PlayerGroup config, LocalSaveData archive, PlayerGroupRule rule)
        {
            var id = archive.PlayerBaseData.Uid;
            int total = 0;
            var define = config.GroupDefine;
            if(rule != null)
            {
                define = rule.GroupDefine;
            }
            DebugEx.FormatInfo("PlayerGroupMan::_CalculateGroup ----> use group define {0}", define.ToString());
            foreach(var entry in define)
            {
                if(entry.Value <= 0)
                {
                    continue;
                }
                total += entry.Value;
            }
            if(total <= 0)
            {
                return 0;
            }
            mRandom.ResetWithSeed((int)(id % (ulong)int.MaxValue), 11 + config.Id);
            var idValue = mRandom.Next % total;
            DebugEx.FormatInfo("PlayerGroupMan::_CalculateGroup ----> total {0}, id {1}, val {2}", total, id, idValue);
            foreach(var entry in define)
            {
                if(entry.Value <= 0)
                {
                    continue;
                }
                if(entry.Value > idValue)
                {
                    return entry.Key;
                }
                else
                {
                    idValue -= entry.Value;
                }
            }
            DebugEx.FormatError("PlayerGroupMan::_CalculateGroup ---> error when calculate");
            return 0;
        }
        
        // 缓存用于划分ab打点分组的排序信息
        private List<(int, string)> divideInfoList;
        
        //传入id返回该id所属ab打点分组名称
        public string GetDivideTrackKey(int value, out bool isFirst)
        {
            isFirst = false;
            if (divideInfoList == null)
            {
                divideInfoList = new List<(int, string)>();
                var dict = Game.Manager.configMan.GetGlobalConfig()?.AbInfoAttributeDivide;
                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        divideInfoList.Add((kv.Key, kv.Value));
                    }
                    divideInfoList.Sort((a, b) => a.Item1 - b.Item1);
                }
            }
            // 如果数值小于所有区间的起始值，则返回 null 或者自行指定默认值
            if (divideInfoList.Count <= 0)
                return null;
            var firstValue = divideInfoList.First().Item1;
            if (value < firstValue)
                return null;
            string result = null;
            // 遍历缓存的排序键，直到当前键大于给定数值
            foreach (var info in divideInfoList)
            {
                var orderValue = info.Item1;
                if (value < orderValue)
                    break;
                result = info.Item2;
                isFirst = (firstValue == orderValue);
            }
            return result;
        }
    }
}