/**
 * @Author: handong.liu
 * @Date: 2021-02-25 15:00:03
 */

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;
using FAT.Platform;

namespace FAT
{
    public class MergeLevelMan : IGameModule, IUserDataHolder
    {
        public bool canLevelup => mNextLevel != null && exp >= mNextLevel.Exp;
        public bool canLevelupAfterFly => mNextLevel != null && mExp >= mNextLevel.Exp;
        public MergeLevel nextLevelConfig => mNextLevel;
        public int displayLevel => mLevel;
        public int level => mLevel;
        public int realExp => mExp;
        public int exp => Mathf.Max(0, mExp - mFlyExp);     // 存在debt时 exp可能没有预先addFlyExp
        private EncryptInt mExp = new EncryptInt().SetValue(0);
        private int mFlyExp = 0;
        private EncryptInt mLevel = new EncryptInt().SetValue(1);
        private EncryptInt mExpDebt = new EncryptInt().SetValue(1);
        private MergeLevel mNextLevel = null;

        private IDictionary<int, MergeLevelRate> mLevelRateMap;
        private List<RewardCommitData> _levelUpReward = new List<RewardCommitData>();  //升级奖励需要缓存到玩家主动点击时表现发放过程
        private readonly Dictionary<int, long> record = new();

        #region imp
        void IGameModule.Reset()
        { }

        void IGameModule.LoadConfig()
        {
            _OnConfigLoaded();
        }

        void IGameModule.Startup() { }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var data = archive.PlayerBaseData;
            if (data != null)
            {
                data.ExpDebt = mExpDebt;
                data.Exp = mExp;
                data.Level = mLevel;
            }
            var dataR = archive.ClientData.PlayerGameData.Record ??= new();
            foreach(var (lv, ts) in record) {
                dataR.Level[lv] = ts;
            }
        }

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            var data = archive.PlayerBaseData;
            mExp.SetValue(data.Exp);
            mExpDebt.SetValue(data.ExpDebt);
            _RefreshLevel(Mathf.Max(1, data.Level));
            var dataR = archive.ClientData.PlayerGameData.Record;
            if (dataR != null) {
                foreach(var (lv, ts) in dataR.Level) {
                    record[lv] = ts;
                }
            }
        }

        #endregion

        private void _OnConfigLoaded()
        {
            mLevelRateMap = Game.Manager.configMan.GetMergeLevelRateConfigMap();
            _RefreshLevel(mLevel);
        }

        public void DebugReset() {
            mExp.SetValue(0);
            mExpDebt.SetValue(0);
            var target = 1;
            _RefreshLevel(target);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().Dispatch(target);
        }

        public long RecordOf(int lv) {
            record.TryGetValue(lv, out var ts);
            return ts;
        }

        public bool TryLevelup(List<RewardCommitData> rewards)
        {
            if (canLevelup)
            {
                DebugEx.FormatInfo("MergeLevelMan ----> level up to level:{0}, exp:{1}, current level:{2}", mNextLevel.Level, mExp, mLevel);
                var config = mNextLevel;
                mExp.SetValue(mExp - config.Exp);
                record[mLevel + 1] = Game.TimestampNow();
                _RefreshLevel(mLevel + 1);
                Game.Manager.rewardMan.PushContext(new RewardContext() { targetWorld = Game.Manager.mainMergeMan.world });
                if (config.Rewards != null)
                {
                    foreach (var reward in config.Rewards)
                    {
                        rewards.Add(Game.Manager.rewardMan.BeginReward(reward.ConvertToRewardConfig().Id, reward.ConvertToRewardConfig().Count, ReasonString.levelup));
                    }
                }
                Game.Manager.rewardMan.PopContext();
                //统一处理升级后的业务逻辑
                _OnMergeLevelChange(config);
                //Dispatch升级事件
                MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().Dispatch(mLevel - 1);
                PlatformSDK.Instance.UpdateGameUserInfo();
                return true;
            }
            else
            {
                var nextExp = mNextLevel != null ? mNextLevel.Exp : 0;
                DebugEx.Warning($"MergeLevelMan ----> level up fail level:{mLevel}, exp:{mExp}, debt:{mExpDebt}, nextLevelExp:{nextExp}");
                return false;
            }
        }

        private void _OnMergeLevelChange(MergeLevel levelConfig)
        {
            //升级时TGA打点
            DataTracker.TrackMergeLevelUp();
            //升级时Adjust打点
#if UNITY_ANDROID
            AdjustTracker.TrackLevelEvent(levelConfig.Level, levelConfig.AndToken);
#elif UNITY_IOS
            AdjustTracker.TrackLevelEvent(levelConfig.Level, levelConfig.IosToken);
#endif
            Game.Manager.featureUnlockMan.OnMergeLevelChange();
            Game.Manager.npcMan.OnMergeLevelChange();
            Game.Manager.bagMan.OnMergeLevelChange();
            Game.Manager.mainOrderMan.OnMergeLevelChange();
            GuideUtility.OnMergeLevelChange();
            Game.Manager.miniGameDataMan.OnMergeLevelChange();
        }

        public void AddFlyExp(int addCount, ReasonString reason)
        {
            var change = false;
            if (mExpDebt > 0)
            {
                // 优先偿还预支的exp
                if (mExpDebt >= addCount)
                {
                    mExpDebt.SetValue(mExpDebt - addCount);
                }
                else
                {
                    // 债务清零
                    mExp.SetValue(mExp + (addCount - mExpDebt));
                    mExpDebt.SetValue(0);
                    change = true;
                }
            }
            else
            {
                mExp.SetValue(mExp + addCount);
                change = true;
            }

            mFlyExp += addCount;
            DebugEx.FormatInfo("MergeLevelMan::AddFlyExp ----> add exp {0}:{1}", addCount, reason);
            if (change)
            {
                DataTracker.exp_change.Track(reason, addCount);
            }
        }

        public void FinishFlyExp(int addCount, bool check = true)
        {
            if (addCount < 0) addCount = mFlyExp;
            mFlyExp -= addCount;
            DebugEx.FormatInfo("MergeLevelMan::FinishFly ----> finish fly exp {0}", addCount);
            MessageCenter.Get<MSG.GAME_MERGE_EXP_CHANGE>().Dispatch(addCount);
            if (check) CheckLevelup();
        }

        public void CheckLevelup() {
            if (canLevelup && !UIManager.Instance.IsOpen(UIConfig.UILevelUp)) {
                _levelUpReward.Clear();
                if (TryLevelup(_levelUpReward)) {
                    UIManager.Instance.OpenWindow(UIConfig.UILevelUp, _levelUpReward, (Action)CheckLevelup);
                }
            }
        }

        public void AddExp(int addCount, ReasonString reason)
        {
            AddFlyExp(addCount, reason);
            FinishFlyExp(addCount, check:false);
        }

        public int GetCurrentLevelRate() 
        {
            TryGetLevelRate(level, out var rate);
            return rate;
        }

        public bool TryGetLevelRate(int level, out int rate)
        {
            if (mLevelRateMap.TryGetValue(level, out var cfg))
            {
                rate = cfg.Rate;
                return true;
            }
            rate = 1;
            return false;
        }

        private void _RefreshLevel(int level)
        {
            mLevel.SetValue(level);
            mNextLevel = null;
            var nextLevels = Game.Manager.configMan.GetMergeLevelConfigs();
            var nextLevel = mLevel + 1;
            foreach (var config in nextLevels)
            {
                if (config.Level == nextLevel)
                {
                    mNextLevel = config;
                    break;
                }
            }
        }
    }
}