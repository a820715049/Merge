/**
 * @Author: zhangpengjian
 * @Date: 2025/7/24 14:57:06
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/24 14:57:06
 * Description: 体力消耗自选活动
 */

using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using EL;
using UnityEngine;
using Google.Protobuf.Collections;
using Config;
using Cysharp.Text;

namespace FAT
{
    public class ActivityWishUpon : ActivityLike, IBoardEntry
    {
        public EventWishUpon conf;
        public EventWishUponDetail confD;
        public override bool Valid => conf != null;
        public VisualPopup VisualMain { get; } = new(UIConfig.UIActivityWishUponMain);
        public override ActivityVisual Visual => VisualMain.visual;
        public int EnergyCost => _energyCost;
        public int EnergyShow => _energyShow;

        private int _energyCost;
        private int _energyShow;
        private List<List<RewardConfig>> _rewardList = new();

        public ActivityWishUpon(ActivityLite lite_)
        {
            Lite = lite_;
            conf = GetEventWishUpon(lite_.Param);
            if (conf == null) return;
            VisualMain.Setup(conf.ThemeId, this, active_: false);
            MessageCenter.Get<MSG.GAME_MERGE_ENERGY_CHANGE>().AddListener(_OnMessageEnergyChange);
        }

        public override void WhenReset()
        {
            MessageCenter.Get<MSG.GAME_MERGE_ENERGY_CHANGE>().RemoveListener(_OnMessageEnergyChange);
        }

        public override void WhenEnd()
        {
            if (_energyCost >= confD.Score && !UIManager.Instance.IsShow(VisualMain.res.ActiveR))
            {
                Game.Manager.screenPopup.TryQueue(VisualMain.popup, PopupType.Login, true);
                DataTracker.event_wishupon_popup.Track(this, 2);
            }
            MessageCenter.Get<MSG.GAME_MERGE_ENERGY_CHANGE>().RemoveListener(_OnMessageEnergyChange);
        }

        private void _OnMessageEnergyChange(int e)
        {
            if (e > 0) return;
            if (_energyCost >= confD.Score) return;
            var prev = _energyCost;
            _energyCost += Mathf.Abs(e);
            _energyCost = Mathf.Min(confD.Score, _energyCost);
            if (_energyCost >= confD.Score)
            {
                DataTracker.event_wishupon_complete.Track(this, confD.Id);
            }
            MessageCenter.Get<MSG.WISH_UPON_ENERGY_UPDATE>().Dispatch(prev, _energyCost);
        }

        public void SetEnergyShow(int energyShow)
        {
            _energyShow = energyShow;
            Game.Manager.archiveMan.SendImmediately(true);
        }
    
        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            if (confD != null)
            {
                any.Add(ToRecord(1, confD.Id));
            }
            any.Add(ToRecord(2, _energyCost));
            any.Add(ToRecord(3, _energyShow));

            var i = 4;
            for (int j = 0; j < _rewardList.Count; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    if (_rewardList[j].Count > k && _rewardList[j][k] != null)
                    {
                        any.Add(ToRecord(i++, _rewardList[j][k].Id));
                        any.Add(ToRecord(i++, _rewardList[j][k].Count));
                    }
                    else
                    {
                        any.Add(ToRecord(i++, 0));
                        any.Add(ToRecord(i++, 0));
                    }
                }
            }
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var gId = ReadInt(1, any);
            _energyCost = ReadInt(2, any);
            _energyShow = ReadInt(3, any);
            SetupDetail(gId);
            var i = 4;
            _rewardList.Clear();
            for (int j = 0; j < 3; j++)
            {
                var rewardList = new List<RewardConfig>();
                for (int k = 0; k < 2; k++)
                {
                    var id = ReadInt(i++, any);
                    var count = ReadInt(i++, any);
                    if (id != 0 && count != 0)
                    {
                        var reward = new RewardConfig() { Id = id, Count = count };
                        rewardList.Add(reward);
                    }
                }
                _rewardList.Add(rewardList);
            }
        }

        public override void SetupFresh()
        {
            var gId = Game.Manager.userGradeMan.GetTargetConfigDataId(conf.GradeId);
            SetupDetail(gId);
            
            RandomCountAndReward();
            
            Game.Manager.screenPopup.TryQueue(VisualMain.popup, PopupType.Login);
            DataTracker.event_wishupon_popup.Track(this, 1);
        }

        public void RandomCountAndReward()
        {
            if (confD == null) return;
            _rewardList.Clear();
            
            // 处理三个奖励位置
            var rewardConfigs = new[]
            {
                (confD.RewardOneNum, confD.RewardOne),
                (confD.RewardTwoNum, confD.RewardTwo), 
                (confD.RewardThreeNum, confD.RewardThree)
            };
            
            foreach (var (numConfig, rewardConfig) in rewardConfigs)
            {
                var rewardList = new List<RewardConfig>();
                
                // 根据权重随机选择奖励数量
                var selectedCount = RandomChooseCountByWeight(numConfig);
                
                if (selectedCount > 0 && rewardConfig.Count > 0)
                {
                    // 解析所有可选的奖励
                    var availableRewards = new List<RewardConfig>();
                    foreach (var rewardStr in rewardConfig)
                    {
                        var reward = rewardStr.ConvertToRewardConfig();
                        if (reward != null)
                        {
                            availableRewards.Add(reward);
                        }
                    }
                    
                    // 根据选择的数量随机选择不重复的奖励
                    if (selectedCount == 1)
                    {
                        // 数量为1时，随机选择一个奖励
                        if (availableRewards.Count > 0)
                        {
                            var randomIndex = UnityEngine.Random.Range(0, availableRewards.Count);
                            var reward = new RewardConfig() { Id = availableRewards[randomIndex].Id, Count = availableRewards[randomIndex].Count };
                            rewardList.Add(reward);
                        }
                    }
                    else if (selectedCount == 2)
                    {
                        // 数量为2时，随机选择两个不重复的奖励
                        if (availableRewards.Count >= 2)
                        {
                            var shuffledRewards = new List<RewardConfig>();
                            foreach (var reward_ in availableRewards)
                            {
                                shuffledRewards.Add(new RewardConfig() { Id = reward_.Id, Count = reward_.Count });
                            }
                            // 洗牌算法
                            for (int i = shuffledRewards.Count - 1; i > 0; i--)
                            {
                                int j = UnityEngine.Random.Range(0, i + 1);
                                var temp = shuffledRewards[i];
                                shuffledRewards[i] = shuffledRewards[j];
                                shuffledRewards[j] = temp;
                            }
                            
                            // 取前两个
                            var reward = new RewardConfig() { Id = shuffledRewards[0].Id, Count = shuffledRewards[0].Count };
                            rewardList.Add(reward);
                            reward = new RewardConfig() { Id = shuffledRewards[1].Id, Count = shuffledRewards[1].Count };
                            rewardList.Add(reward); 
                        }
                        else if (availableRewards.Count == 1)
                        {
                            // 只有一个奖励时，添加两次
                            var reward = new RewardConfig() { Id = availableRewards[0].Id, Count = availableRewards[0].Count };
                            rewardList.Add(reward);
                            rewardList.Add(reward);
                        }
                    }
                }
                
                _rewardList.Add(rewardList);
            }
        }
        
        /// <summary>
        /// 根据权重随机选择数量
        /// </summary>
        /// <param name="numConfig">数量配置，格式为 数量:权重</param>
        /// <returns>选择的数量</returns>
        private int RandomChooseCountByWeight(MapField<int, int> numConfig)
        {
            if (numConfig == null || numConfig.Count == 0) return 0;
            
            // 计算总权重
            int totalWeight = 0;
            foreach (var kvp in numConfig)
            {
                totalWeight += kvp.Value;
            }
            
            if (totalWeight <= 0) return 0;
            
            // 根据权重随机选择
            int randomWeight = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;
            
            foreach (var kvp in numConfig)
            {
                currentWeight += kvp.Value;
                if (randomWeight < currentWeight)
                {
                    return kvp.Key;
                }
            }
            
            return 0;
        }
        
        /// <summary>
        /// 获取随机生成的奖励列表
        /// </summary>
        /// <returns>奖励列表，每个元素是一个(id, count)元组</returns>
        public List<List<RewardConfig>> GetRewardList()
        {
            return _rewardList;
        }
        
        /// <summary>
        /// 获取指定位置的奖励列表
        /// </summary>
        /// <param name="position">奖励位置 (0-2)</param>
        /// <returns>指定位置的奖励列表</returns>
        public List<RewardConfig> GetRewardListAtPosition(int position)
        {
            if (position >= 0 && position < _rewardList.Count)
            {
                return _rewardList[position];
            }
            return new List<RewardConfig>();
        }
        
        /// <summary>
        /// 应用指定位置的奖励
        /// </summary>
        /// <param name="position">奖励位置 (0-2)</param>
        /// <returns>奖励数据列表</returns>
        public List<RewardCommitData> BeginRewardByIndex(int position, bool isComplete)
        {
            var rewards = new List<RewardCommitData>();
            var rewardList = GetRewardListAtPosition(position);
            string rewardStr = "";
            if (rewardList.Count > 0)
            {
                var rewardMan = Game.Manager.rewardMan;
                var rewardIds = new List<int>();
                for (int i = 0; i < rewardList.Count; i++)
                {
                    var reward = rewardList[i];
                    var rewardData = rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.wish_upon_reward);
                    rewards.Add(rewardData);
                    rewardIds.Add(reward.Id);
                }
                rewardStr = ZString.Join(",", rewardIds);
                Game.Manager.archiveMan.SendImmediately(true);
            }
            DataTracker.event_wishupon_rwd.Track(this, position + 1, rewardList.Count, confD.Id, rewardStr, isComplete ? 2 : 1);
            VisualMain.res.ActiveR.Close();
            Game.Manager.activity.EndImmediate(this, false);
            return rewards;
        }

        public void SetupDetail(int gId)
        {
            confD = GetEventWishUponDetail(gId);
        }

        public override (long, long) SetupTS(long sTS_, long eTS_)
        {
            if (sTS_ > 0) return (sTS_, eTS_);
            var sts = Game.TimestampNow();
            var ets = Math.Min(sts + conf.EventTime, Lite.EndTS);
            return (sts, ets);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            popup_.TryQueue(VisualMain.popup, state_);
        }

        public override void Open() => Open(VisualMain.res);

        public string BoardEntryAsset()
        {
            Visual.Theme.AssetInfo.TryGetValue("boardEntry", out var s);
            return s;
        }
    }
}