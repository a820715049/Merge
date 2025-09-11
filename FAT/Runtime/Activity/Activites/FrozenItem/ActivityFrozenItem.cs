/*
 * @Author: tang.yan
 * @Description: 冰冻棋子活动类
 * @Doc: https://centurygames.feishu.cn/wiki/QhU3wACXIi4ExJkOdfccOQq9nxb
 * @Date: 2025-08-14 16:08:51
 */

using EL;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public class ActivityFrozenItem : ActivityLike
    {
        public override bool Valid => Lite.Valid && Conf != null;
        public FrozenItem Conf { get; private set; }
        //用户分层 对应FrozenItemDetail.id
        public int GroupId { get; private set; }  
        
        public int TapCount { get; private set; }          // T1：累计点击耗体次数
        public int EnergyConsumed { get; private set; }    // E1：累计消耗体力
        public bool Guarantee { get; private set; }         // 保底标记：命中几率且候选空 -> true；产出成功 -> false

        //外部调用需判空
        public FrozenItemDetail GetCurGroupConfig()
        {
            return Game.Manager.configMan.GetFrozenItemDetailConfig(GroupId);
        }
        
        #region 活动内部方法

        public ActivityFrozenItem(ActivityLite lite_)
        {
            Lite = lite_;
            Conf = Game.Manager.configMan.GetFrozenItemConfig(lite_.Param);
        }
        
        // 活动首次初始化 | 此时不走读档流程 不会调用LoadSetup
        public override void SetupFresh()
        {
            GroupId = Game.Manager.userGradeMan.GetTargetConfigDataId(Conf.EventGroup);
            //刷新合成回调模块
            _RefreshMergeBonusHandler();
            //刷新产棋子回调模块
            _RefreshSpawnBonusHandler();
        }
        
        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, GroupId));
            any.Add(ToRecord(1, TapCount));
            any.Add(ToRecord(2, EnergyConsumed));
            any.Add(ToRecord(3, Guarantee));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            GroupId = ReadInt(0, any);
            TapCount = ReadInt(1, any);
            EnergyConsumed = ReadInt(2, any);
            Guarantee = ReadBool(3, any);
            //刷新合成回调模块
            _RefreshMergeBonusHandler();
            //刷新产棋子回调模块
            _RefreshSpawnBonusHandler();
        }

        public override void Open()
        {
            //本活动没有界面
        }

        public override void WhenEnd()
        {
            //清理合成回调模块
            _ClearMergeBonusHandler();
            //清理产棋子回调模块
            _ClearSpawnBonusHandler();
        }

        #endregion

        #region 冰冻棋子相关逻辑

        #region FrozenItemMergeBonusHandler

        private FrozenItemMergeBonusHandler mergeBonusHandler;

        private void _RefreshMergeBonusHandler()
        {
            mergeBonusHandler ??= new FrozenItemMergeBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalMergeBonusHandler(mergeBonusHandler);
        }
        
        private void _ClearMergeBonusHandler()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalMergeBonusHandler(mergeBonusHandler);
            mergeBonusHandler = null;
        }
        
        #endregion
        
        #region FrozenItemSpawnBonusHandler

        private FrozenItemSpawnBonusHandler spawnBonusHandler;

        private void _RefreshSpawnBonusHandler()
        {
            spawnBonusHandler ??= new FrozenItemSpawnBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(spawnBonusHandler);
        }
        
        private void _ClearSpawnBonusHandler()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(spawnBonusHandler);
            spawnBonusHandler = null;
        }

        #endregion
        
        // 由 SpawnBonusHandler 在“有效耗体”时调用（只认主棋盘 + firstCost 严格匹配 + ClickSource/DieOutput）
        public void OnEnergySpent(int energy)
        {
            if (energy <= 0) return;
            TapCount += 1;
            EnergyConsumed += energy;
#if UNITY_EDITOR
            DebugEx.Info($"ActivityFrozenItem.OnEnergySpent: TapCount = {TapCount}, EnergyConsumed = {EnergyConsumed}, Guarantee = {Guarantee}, curUseEnergyNum = {energy}");
#endif
        }

        // 成功产出后调用：清空T1/E1与保底标记
        public void ResetAfterSpawn()
        {
            TapCount = 0;
            EnergyConsumed = 0;
            Guarantee = false;
        }

        public void SetGuarantee(bool on) => Guarantee = on;

        // 达到门槛（≥ ergConsumeTimes）
        public bool ThresholdReached(FrozenItemDetail d)
        {
            if (d == null) return false;
            return TapCount >= d.ErgConsumeTimes;
        }
        
        //值配的10，那就代表是千分之十，也就是百分之一，换成小数是0.01
        //这里将千分比转换成百分比 所以除10
        private static float PerMilleToPercent(float v) => v / 10f;
        
        // 计算当前掉落概率（不含 guarantee 覆盖）
        public float CalcDropProb(FrozenItemDetail d)
        {
            if (d == null) return 0f;
            var stepCnt = Mathf.Max(0, (int)(TapCount - d.ErgConsumeTimes));
            var p = PerMilleToPercent(d.BaseProb) + PerMilleToPercent(d.StepProb) * stepCnt;
            // 体力过低的额外加成（与当前概率叠加）
            var curEnergy = Game.Manager.mergeEnergyMan.EnergyAfterFly;
            if (curEnergy < d.RateUpEnergy)
            {
                p += PerMilleToPercent(d.RateUpProb);
            }
            p = Mathf.Clamp(p, 0f, 100f);
            DebugEx.Info($"ActivityFrozenItem.CalcDropProb: Prob = {p}, TapCount = {TapCount}, curEnergy = {curEnergy}, conf = {d}");
            return p;
        }

        #endregion

        #region Debug工具

        public void DebugSetTapCount(int tapCount)
        {
            TapCount = tapCount;
        }
        
        public void DebugSetEnergyConsumed(int energyConsumed)
        {
            EnergyConsumed = energyConsumed;
        }

        public bool MustSpawnFrozenItem = false;

        public void DebugMustSpawnFrozenItem()
        {
            MustSpawnFrozenItem = !MustSpawnFrozenItem;
        }

        #endregion
    }
}