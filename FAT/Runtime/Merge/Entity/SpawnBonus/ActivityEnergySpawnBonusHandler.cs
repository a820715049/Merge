/**
 * @Author: handong.liu
 * @Date: 2023-02-23 15:18:17
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;

namespace FAT.Merge
{
    //保底型活动体力奖励生成
    public class ActivityEnergySpawnBonusHandler : ISpawnBonusHandler
    {
        public struct Range
        {
            public int lower;
            public int upper;
            public override string ToString()
            {
                return $"[{lower}, {upper})";
            }
        }
        public class CreateParam
        {
            public string debugKey;
            public int activityId;
            public IList<int> weightBonusId;
            public IList<int> weight;
            public IList<int> outputCount;
            public IList<int> baodiBonus;
            public IList<int> baodiCounts;
            public override string ToString()
            {
                return $"key:{debugKey}, outputs:{weightBonusId.ToStringEx()}, outputWeight:{weight.ToStringEx()}, baodiId:{baodiBonus.ToStringEx()}, baodiCounts:{baodiCounts.ToStringEx()}";
            }
        }
        [System.Serializable]
        public class ActivityEnergySpawnBonusDebugInfo
        {
            public int totalSpawn;
            public int energySpawn;
            public int nonEnergySpawn;
            public int totalEnergyUse;
        }
        public int priority;
        private string mDebugKey;
        int ISpawnBonusHandler.priority => priority;        //越小越先出
        private ActivityEnergySpawnBonusDebugInfo mDebugInfo = null;
        private Dictionary<int, int> mWeightTable = new Dictionary<int, int>();
        private Dictionary<int, int> countTable = new Dictionary<int, int>();
        private Dictionary<int, Range> mBaoDiTable = new Dictionary<int, Range>();
        private Dictionary<int, int> mNonOutputCounter = new Dictionary<int, int>();
        private int activityId;

        public override string ToString()
        {
            return $"debugKey:{mDebugKey}, debug:{JsonUtility.ToJson(mDebugInfo)}, weightTable:{mWeightTable.ToStringEx()}, baodiTable:{mBaoDiTable.ToStringEx()}, counter:{mNonOutputCounter.ToStringEx()}";
        }

        public void Serialize(RandomParam param)
        {
            param.Type = (int)RandomMethodType.BpbaoDi;
            param.MapParam1.Clear();
            foreach(var counter in mNonOutputCounter)
            {
                param.MapParam1.Add(counter.Key, counter.Value);
            }
        }

        public void Deserialize(RandomParam param)
        {
            mNonOutputCounter.Clear();
            if(param.Type == (int)RandomMethodType.BpbaoDi)
            {
                foreach(var counter in param.MapParam1)
                {
                    mNonOutputCounter[counter.Key] = counter.Value;
                }
            }
        }

        public void ClearDebugInfo()
        {
            mDebugInfo = new ActivityEnergySpawnBonusDebugInfo();
            SaveDebugInfo();
        }

        public void InitConfig(CreateParam param)
        {
            mWeightTable.Clear();
            mBaoDiTable.Clear();
            mNonOutputCounter.Clear();
            mDebugKey = param.debugKey;
            activityId = param.activityId;
            for(int i = 0; i < param.weightBonusId.Count && i < param.weight.Count; i++)
            {
                var id = param.weightBonusId[i];
                mWeightTable[id] = param.weight[i];
                countTable[id] = param.outputCount[i];
            }

            for(int i = 0, j = 0; i < param.baodiBonus.Count && j + 1 < param.baodiCounts.Count; i ++, j += 2)
            {
                mBaoDiTable[param.baodiBonus[i]] = new Range() {
                    lower = param.baodiCounts[j],
                    upper = param.baodiCounts[j+1]
                };
            }

            DebugEx.FormatTrace("ActivityEnergySpawnBonusHandler::InitConfig ----> for {0}, I'am {1}", param, this);
        }

        void ISpawnBonusHandler.OnRegister()
        {
            LoadAndGetDebugInfo();
        }

        void ISpawnBonusHandler.OnUnRegister()
        {

        }

        public ActivityEnergySpawnBonusHandler()
        {
        } 


        public ActivityEnergySpawnBonusDebugInfo LoadAndGetDebugInfo()
        {
            return mDebugInfo;
        }

        public void SaveDebugInfo()
        {
            // if(mDebugInfo != null)
            // {
            //     var key = string.Format("BaoDiDbg{0}", mDebugKey);
            //     Game.Instance.accountMan.SetClientStorage(key, JsonUtility.ToJson(mDebugInfo));
            // }
        }

        public int Simulate(SpawnBonusContext context, bool dryrun)
        {
            var output = 0;
            // var value = 0;
            // var result = context.result;
            // var debugInfo = LoadAndGetDebugInfo();
            // var costEnergy = Game.Instance.mergeItemMan.IsItemCostEnergy(context.result.tid);
            // if(debugInfo != null)
            // {
            //     debugInfo.totalSpawn++;
            //     if(costEnergy)
            //     {
            //         debugInfo.energySpawn++;
            //     }
            //     else
            //     {
            //         debugInfo.nonEnergySpawn++;
            //     }
            // }
            // //如果保底触发，则直接产出
            // foreach(var entry in mNonOutputCounter)
            // {
            //     if(entry.Value <= 0)
            //     {
            //         //该出了
            //         output = entry.Key;
            //         value = 1;
            //         DebugEx.FormatTrace("ActivityEnergySpawnBonusHandler::Process ----> baodi {0}", entry.Key);
            //         break;
            //     }
            // }
            // if(output <= 0)
            // {
            //     var id = context.srcId;
            //     output = mWeightTable.Keys.RandomChooseByWeight((e) => mWeightTable[e]);
            // }
            // if(output > 0)
            // {
            //     value = countTable[output];
            //     if(!dryrun)
            //     {
            //         _SetActivityEnergy(context, value);
            //     }
            //     // DataTracker.TrackBPItemDrop(output, mNonOutputCounter.GetDefault(output, 0));
            //     mNonOutputCounter.Remove(output);
            // }

            
            // foreach(var entry in mBaoDiTable)
            // {
            //     if(!mNonOutputCounter.TryGetValue(entry.Key, out var count))
            //     {
            //         var val = Random.Range(entry.Value.lower, entry.Value.upper);
            //         mNonOutputCounter.Add(entry.Key, val);
            //         DebugEx.FormatTrace("ActivityEnergySpawnBonusHandler::Process ----> schedule baodi {0}:{1}", entry.Key, val);
            //     }
            //     else if(output != entry.Key)
            //     {
            //         mNonOutputCounter[entry.Key] = count - 1;         //这次又没出，保底-1
            //     }
            // }

            
            // SaveDebugInfo();
            return output;
        }

        void ISpawnBonusHandler.Process(SpawnBonusContext context)
        {
            // // doc: https://centurygames.yuque.com/ywqzgn/lhbdgc/xkyhcf51nyfoxoom
            // // TODO: 需要进过活动棋盘以后此handler才能生效

            // if (context.isBubble)
            // {
            //     // 作用棋盘 : 主棋盘 和 活动棋盘
            //     if (_TryGetPvpEvent(out var act))
            //     {
            //         if (act.Id == activityId &&
            //             (context.world == Game.Instance.mainMergeMan.world || context.world == act.World))
            //         {
            //             // 按bubble售价计算附加体力
            //             var cfg = Env.Instance.GetItemMergeConfig(context.result.tid);
            //             var actEnergy = Mathf.CeilToInt(cfg.BubblePrice * 1.0f / act.PvpEnergyConfig.PvpEnergyBubbleFactor);
            //             _SetActivityEnergy(context, actEnergy);
            //         }
            //     }
            // }
            // else if (context.reason != ItemSpawnReason.None)
            // {
            //     // 作用棋盘 : 主棋盘
            //     // 必须为耗体产生
            //     if (_TryGetPvpEvent(out var act) && act.Id == activityId)
            //     {
            //         if (context.energyCost > 0 && context.world == Game.Instance.mainMergeMan.world) {
            //             //drop bonus
            //             Simulate(context, false);
            //         }
            //         if (context.energyCost > 0 || context.reason == ItemSpawnReason.Merge) {
            //             //drop token
            //             act.TryRewardToken(context);
            //         }
            //     }
            // }
        }

        // private bool _TryGetPvpEvent(out DreamMerge.PvpEvent act)
        // {
        //     act = Game.Instance.activityMan.PvpEvent;
        //     return act != null && act.TaskVisible;
        // }

        private void _SetActivityEnergy(SpawnBonusContext context, int energy)
        {
            context.result.AppendWithActivityComponent();
            if (context.result.TryGetItemComponent(out ItemActivityComponent actCom))
            {
                actCom.SetActivityEnergy(activityId, energy);
            }
        }
    }
}