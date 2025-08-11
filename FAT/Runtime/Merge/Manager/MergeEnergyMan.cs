/**
 * @Author: handong.liu
 * @Date: 2021-02-25 14:59:55
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using FAT.MSG;
using System;

namespace FAT {
    public class MergeEnergyMan : IGameModule, IUserDataHolder, IUserDataInitializer {
        public int Energy => vEnergy - flying;
        public int EnergyAfterFly => vEnergy;
        public int RecoverCD => interval - recover;
        public int RecoverMax { get; private set; }

        //free energy claim count by board id
        private readonly Dictionary<int, int> claimCount = new();
        private readonly EncryptInt vEnergy = new EncryptInt().SetValue(0);
        private int recover;
        private long offlineTS;

        private int flying = 0;
        private int interval = 0;

        public void DebugAddEnergy(int amount, ReasonString reason) {
            AddFlyEnergy(amount, reason);
            FinishFlyEnergy(amount);
        }

        void IUserDataHolder.FillData(LocalSaveData archive) {
            var now = Game.TimestampNow();
            var data = archive.ClientData.PlayerGameData.EnergyInfo ??= new();
            data.Energy = vEnergy;
            data.LastAddEnergy = now - recover;
            foreach(var (bId, count) in claimCount) {
                data.ClaimCount.Add(bId, count);
            }
        }

        void IUserDataHolder.SetData(LocalSaveData archive) {
            var data = archive.ClientData.PlayerGameData.EnergyInfo;
            if (data == null) {
                InitUserData();
                return;
            }
            vEnergy.SetValue(data.Energy);
            offlineTS = data.LastAddEnergy;
            claimCount.Clear();
            foreach(var (bId, count) in data.ClaimCount) {
                claimCount.Add(bId, count);
            }
            TryRecoverOffline();
        }

        public void InitUserData() {
            vEnergy.SetValue(Game.Manager.configMan.globalConfig.MergeEnergyAutoMax);
        }

        public void ToBackground() {
            offlineTS = Game.TimestampNow();
        }

        public void ToForeground() {
            TryRecoverOffline();
        }

        public int FullRecoverCD() {
            var delta = RecoverMax - vEnergy;
            if (delta <= 0) return 0;
            return Mathf.Max(0, interval - recover) + (delta - 1) * interval;
        }

        public void TickRecover(int s_) {
            if (vEnergy >= RecoverMax) return;
            recover += s_;
            if (recover >= interval && TryRecover(ReasonString.recover_online, recover / interval, 1)) {
                recover %= interval;
                //能量自然恢复时播音效
                Game.Manager.audioMan.TriggerSound("AddEnergyAuto");
            }
        }

        internal bool TryRecover(ReasonString reason, int count_, int max_) {
            if (count_ > max_) {
                DebugEx.Error($"{nameof(MergeEnergyMan)} trying to update energy above limit {count_}>{max_} {recover}");
            }
            if (count_ <= 0) {
                DebugEx.Error($"{nameof(MergeEnergyMan)} trying to update {count_} energy");
                return false;
            }
            var value = Math.Min(vEnergy + count_, RecoverMax);
            var count = value - vEnergy;
            vEnergy.SetValue(value);
            DataTracker.energy_change.Track(reason, true, count);
            MessageCenter.Get<GAME_MERGE_ENERGY_CHANGE>().Dispatch(count);
            if (count == count_) DebugEx.Info($"{nameof(MergeEnergyMan)} recover count:{count} energy:{vEnergy} reason:{reason}");
            else DebugEx.Info($"{nameof(MergeEnergyMan)} recover count:{count}|{count_} energy:{vEnergy} reason:{reason}");
            return true;
        }

        internal void TryRecoverOffline() {
            if (vEnergy >= RecoverMax || offlineTS <= 0) return;
            var diff = (int)(Game.TimestampNow() - offlineTS);
            offlineTS = 0;
            recover += diff;
            var count = recover / interval;
            Debug.Log($"[energy] {count}={recover}/{interval} diff is {diff} % is {diff % interval}");
            if (count <= 0) return;
            TryRecover(ReasonString.recover_offline, count, count);
            recover = diff % interval;
        }

        public void AddFlyEnergy(int amount, ReasonString reason) {
            vEnergy.SetValue(vEnergy + amount);
            flying += amount;
            DataTracker.energy_change.Track(reason, true, amount);
            DebugEx.Info($"{nameof(MergeEnergyMan)} add fly energy count:{amount} fly:{flying} energy:{vEnergy} reason:{reason}");
        }

        public void FinishFlyEnergy(int amount) {
            flying -= amount;
            MessageCenter.Get<GAME_MERGE_ENERGY_CHANGE>().Dispatch(amount);
            DebugEx.Info($"{nameof(MergeEnergyMan)} finish fly energy count:{amount} fly:{flying} energy:{vEnergy}");
        }

        public bool CanUseEnergy(int amount) {
            return vEnergy >= amount;
        }

        public bool UseEnergy(int amount, ReasonString reason) {
            var cost = amount;
            if (cost < 0) {
                DebugEx.Error($"{nameof(MergeEnergyMan)} trying to cost {cost} energy");
                return false;
            }
            if (vEnergy < cost) {
                DebugEx.Info($"{nameof(MergeEnergyMan)} use energy fail {vEnergy}<{cost} reason:{reason}");
                return false;
            }
            vEnergy.SetValue(vEnergy - cost);
            DataTracker.energy_change.Track(reason, false, cost);
            TryTrackEnergyUnbalance();
            DebugEx.FormatInfo($"{nameof(MergeEnergyMan)} use energy count:{cost} energy:{vEnergy} reason:{reason}");
            MessageCenter.Get<GAME_MERGE_ENERGY_CHANGE>().Dispatch(-cost);
            Game.Manager.cardMan.OnUseEnergy(cost);
            return true;
        }

        public int ClaimCount(int boardId_) {
            if (claimCount.TryGetValue(boardId_, out var v)) return v;
            return 0;
        }

        public void ClaimEnergy(int boardId_) {
            claimCount.TryGetValue(boardId_, out var v);
            claimCount[boardId_] = v + 1;
        }

        // 仅在用户明确显示负数的情况下主动track
        private void TryTrackEnergyUnbalance() {
            // 如果在获得体力的同时快速消耗 有机会短暂产生合法的小额负数
            // 所以这里设置一个阈值 尽量在明确出错后才track
            if (Energy <= -5) {
                DataTracker.energy_error.Track();
            }
        }

        void IGameModule.Reset() {
            // 时间前后切换时recover可能成为负数
            recover = 0;
            // 不再处理之前的flying | RewardMan里也清空了之前没有提交的奖励
            flying = 0;
        }
        
        void IGameModule.LoadConfig() {
            interval = Game.Manager.configMan.globalConfig.MergeEnergyAutoSec;
            RecoverMax = Game.Manager.configMan.globalConfig.MergeEnergyAutoMax;
        }

        void IGameModule.Startup() {
            void Tick() => TickRecover(1);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListenerUnique(Tick);
            TryRecoverOffline();
        }
    }
}