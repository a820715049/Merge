/*
 * @Author: qun.chao
 * @Date: 2023-11-12 21:42:37
 */
using System;
using System.Collections.Generic;
using fat.rawdata;
using FAT.Merge;

namespace FAT
{
    /// <summary>
    /// 棋盘物品的获得难度评估系统
    /// </summary>
    public class MergeItemDifficultyMan : IGameModule, IUpdate
    {
        private IDictionary<int, MergeDifficulty> mMergeDifficultyConfigs;

        // 每帧丢弃缓存 兼顾准确度也避免同一帧内的重复计算
        private Dictionary<int, (int avg, int real)> mItemDifficultyCache = new();
        private bool enableCache = true;

        // 魔盒/三选一
        private BoxOutputResolver boxOutputResolver = new();

        #region imp
        void IGameModule.Reset()
        {
            mItemDifficultyCache.Clear();
            boxOutputResolver.Reset();
        }

        void IGameModule.LoadConfig()
        {
            mMergeDifficultyConfigs = Game.Manager.configMan.GetMergeItemDifficultyConfigMap();
        }

        void IGameModule.Startup() { }

        void IUpdate.Update(float dt)
        {
            mItemDifficultyCache.Clear();
        }
        #endregion

        #region SpecialBox / ChoiceBox

        // 魔盒强行关联主棋盘
        public int CalcSpecialBoxOutput(int minDffy, int maxDffy, List<int> candidates = null)
        {
            var tracer = Game.Manager.mainMergeMan.worldTracer;
            var helper = Game.Manager.mainOrderMan.curOrderHelper;
            return CalcSpecialBoxOutput(tracer, helper, minDffy, maxDffy, candidates);
        }

        private int CalcSpecialBoxOutput(MergeWorldTracer tracer, IOrderHelper helper, int minDffy, int maxDffy, List<int> candidates = null)
        {
            boxOutputResolver.BindEnv(tracer, helper, minDffy, maxDffy);
            return boxOutputResolver.CalcSpecialBox(candidates);
        }

        // 三选一强行关联主棋盘
        public void CalcChoiceBoxOutput(int minDffy, int maxDffy, List<int> container, int choiceCount)
        {
            var tracer = Game.Manager.mainMergeMan.worldTracer;
            var helper = Game.Manager.mainOrderMan.curOrderHelper;
            CalcChoiceBoxOutput(tracer, helper, minDffy, maxDffy, container, choiceCount);
        }

        private void CalcChoiceBoxOutput(MergeWorldTracer tracer, IOrderHelper helper, int minDffy, int maxDffy, List<int> container, int choiceCount)
        {
            boxOutputResolver.BindEnv(tracer, helper, minDffy, maxDffy);
            boxOutputResolver.CalcChoiceBox(container, choiceCount);
        }

        #endregion

        #region 星想事成
        public bool CalcMagicHourOutput(MergeWorldTracer tracer, IOrderHelper helper, int minDffy, int maxDffy, out IOrderData targetOrder, out int targetId)
        {
            boxOutputResolver.BindEnv(tracer, helper, minDffy, maxDffy);
            return boxOutputResolver.CalcMagicHourOutput(out targetOrder, out targetId);
        }
        #endregion

        public MergeDifficulty GetConfigByItemId(int itemId)
        {
            mMergeDifficultyConfigs.TryGetValue(itemId, out var config);
            return config;
        }

        public int GetItemAvgDifficulty(int itemId)
        {
            var config = GetConfigByItemId(itemId);
            return config?.AverageDifficulty ?? -1;
        }

        // 主动清除cache
        public void ClearCache()
        {
            mItemDifficultyCache.Clear();
        }

        public bool TryGetItemDifficulty(int itemId, out int avg, out int real)
        {
            avg = 0;
            real = 0;

            if (enableCache)
            {
                if (mItemDifficultyCache.TryGetValue(itemId, out var cache))
                {
                    avg = cache.avg;
                    real = cache.real;
                    return true;
                }
            }

            var config = GetConfigByItemId(itemId);
            if (config == null)
                return false;
            avg = config.AverageDifficulty;
            var lvlIdx = _GetCategoryMaxUnlockLevelIdx(config.ProducerCategoryId);
            if (lvlIdx >= 0 && lvlIdx < config.ActualDifficulty.Count)
            {
                int.TryParse(config.ActualDifficulty[lvlIdx].Split(":")[1], out real);
            }

            if (enableCache)
            {
                mItemDifficultyCache.Add(itemId, (avg, real));
            }

            return true;
        }

        private int _GetCategoryMaxUnlockLevelIdx(int cid)
        {
            var handbook = Game.Manager.handbookMan;
            var cfg = Game.Manager.mergeItemMan.GetCategoryConfig(cid);
            for (int i = cfg.Progress.Count - 1; i >= 0; --i)
            {
                if (handbook.IsItemUnlocked(cfg.Progress[i]))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}