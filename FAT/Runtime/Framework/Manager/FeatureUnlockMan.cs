/*
 * @Author: qun.chao
 * @Date: 2021-04-16 11:53:07
 */
using System.Collections.Generic;
using fat.rawdata;

namespace FAT
{
    public class FeatureUnlockMan : IGameModule
    {
        private Dictionary<FeatureEntry, FeatureUnlock> mFeatureUnlockDict = new Dictionary<FeatureEntry, FeatureUnlock>();
        private Dictionary<FeatureEntry, System.Func<bool, bool>> mFeatureUnlockHandler = new Dictionary<FeatureEntry, System.Func<bool, bool>>();

        public void OnConfigLoaded()
        {
            mFeatureUnlockDict.Clear();
            var featureCfgs = Game.Manager.configMan.GetFeatureUnlockConfigs();
            foreach (var item in featureCfgs)
            {
                mFeatureUnlockDict.Add(item.Entry, item);
            }
        }

        public void SetFeatureUnlockHandler(FeatureEntry entry, System.Func<bool, bool> handler)
        {
            mFeatureUnlockHandler[entry] = handler;
        }

        public FeatureUnlock GetFeatureConfig(FeatureEntry entry)
        {
            if (mFeatureUnlockDict.ContainsKey(entry))
                return mFeatureUnlockDict[entry];
            return null;
        }

        public bool IsFeatureEntryShow(FeatureEntry entry)
        {
            if (!mFeatureUnlockDict.ContainsKey(entry))
                return true;
            var cfg = mFeatureUnlockDict[entry];
            if (Game.Manager.mergeLevelMan.displayLevel < cfg.DisplayLevel)
                return false;
            return true;
        }

        public bool IsFeatureEntryUnlocked(FeatureEntry entry)
        {
            var result = _IsFeatureEntryUnlockedInner(entry);
            if (mFeatureUnlockHandler.TryGetValue(entry, out var func))
            {
                result = func(result);
            }
            return result;
        }

        private bool _IsFeatureEntryUnlockedInner(FeatureEntry entry)
        {
            if (IsFeatureEntrySuspended(entry))
                return false;
            return IsEntryMatchRequire(entry);
        }

        public bool IsEntryMatchRequire(FeatureEntry entry)
        {
            if (!mFeatureUnlockDict.ContainsKey(entry))
                return true;

            var mgr = Game.Manager.mainOrderMan;
            var cfg = mFeatureUnlockDict[entry];

            if (cfg.Guide > 0)
            {
                if (Game.Manager.guideMan.IsGuideFinished(cfg.Guide))
                {
                    // guide达成则无视其他条件直接解锁
                    return true;
                }
            }

            var level = Game.Manager.mergeLevelMan.displayLevel;
            if (level < cfg.DisplayLevel)
                return false;
            if (level < cfg.Level)
                return false;

            // 订单 任何一项满足
            bool orderReady = !(cfg.OrderId.Count > 0);
            foreach (var orderId in cfg.OrderId)
            {
                if (mgr.IsOrderCompleted(orderId))
                {
                    orderReady = true;
                    break;
                }
            }
            if (!orderReady) return false;

            return true;
        }

        public bool IsFeatureEntrySuspended(FeatureEntry entry)
        {
            if (!mFeatureUnlockDict.ContainsKey(entry))
                return false;
            return mFeatureUnlockDict[entry].FeatureMaintain == 1;
        }

        public int GetUnlockLevel(FeatureEntry entry)
        {
            if (!mFeatureUnlockDict.ContainsKey(entry))
                return 0;
            var cfg = mFeatureUnlockDict[entry];
            return cfg.Level;
        }

        public void OnMergeLevelChange() { _FeatureEntryStatusRefresh(); }
        public void OnMainOrderFinished() { _FeatureEntryStatusRefresh(); }
        public void OnGuideFinished() { _FeatureEntryStatusRefresh(); }

        private void _FeatureEntryStatusRefresh()
        {
            EL.MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().Dispatch();
        }

        void IGameModule.Reset()
        {
            mFeatureUnlockDict.Clear();
            mFeatureUnlockHandler.Clear();
        }
        void IGameModule.LoadConfig() { OnConfigLoaded(); }
        void IGameModule.Startup() { }
    }
}