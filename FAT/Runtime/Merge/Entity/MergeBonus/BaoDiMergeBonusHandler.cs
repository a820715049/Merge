/**
 * @Author: handong.liu
 * @Date: 2023-02-23 15:18:17
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    //保底型合成奖励
    public class BaoDiMergeBonusHandler : IMergeBonusHandler
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
            public IList<int> weightBonusId;
            public IList<int> weight;
            public IList<int> weightAuto;
            public IList<int> baodiBonus;
            public IList<int> baodiCounts;
            public IList<int> nonOutputItems;
            public bool checkAuto;
            public override string ToString()
            {
                return $"key:{debugKey}, outputs:{weightBonusId.ToStringEx()}, outputWeight:{weight.ToStringEx()}, baodiId:{baodiBonus.ToStringEx()}, baodiCounts:{baodiCounts.ToStringEx()}, nooutput:{nonOutputItems.ToStringEx()}";
            }
        }
        [System.Serializable]
        public class BaoDiMergeBonusDebugInfo
        {
            public int totalMerge;
            public int energyMerge;
            public int nonEnergyMerge;
            public int totalEnergyUse;
            public int lv1PointCount;
            public int[] claimedPointByLevel = new int[0];
        }
        public int priority;
        private string mDebugKey;
        int IMergeBonusHandler.priority => priority;        //越小越先出
        private BaoDiMergeBonusDebugInfo mDebugInfo = null;
        private Dictionary<int, int> mWeightTable = new Dictionary<int, int>();
        private Dictionary<int, int> mWeightTableAuto = new Dictionary<int, int>();
        private HashSet<int> mNonOutputItem = new HashSet<int>();           //合成这些东西不出奖励
        private Dictionary<int, Range> mBaoDiTable = new Dictionary<int, Range>();
        private Dictionary<int, int> mNonOutputCounter = new Dictionary<int, int>();
        private HashSet<int> mCategory = new HashSet<int>();       //产出物所属的category
        private bool checkAuto;


        public override string ToString()
        {
            return $"debugKey:{mDebugKey}, debug:{JsonUtility.ToJson(mDebugInfo)}, weightTable:{mWeightTable.ToStringEx()}, nonOutputItem:{mNonOutputItem.ToStringEx()}, baodiTable:{mBaoDiTable.ToStringEx()}, counter:{mNonOutputCounter.ToStringEx()}, cate:{mCategory.ToStringEx()}";
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
            mDebugInfo = new BaoDiMergeBonusDebugInfo();
            SaveDebugInfo();
        }

        public void InitConfig(CreateParam param)
        {
            mWeightTable.Clear();
            mWeightTableAuto.Clear();
            mNonOutputItem.Clear();
            mBaoDiTable.Clear();
            mNonOutputCounter.Clear();
            mCategory.Clear();
            mDebugKey = param.debugKey;
            checkAuto = param.checkAuto;
            for(int i = 0; i < param.weightBonusId.Count && i < param.weight.Count; i++)
            {
                mWeightTable[param.weightBonusId[i]] = param.weight[i];
                mWeightTableAuto[param.weightBonusId[i]] = param.weightAuto[i];
                var category = Env.Instance.GetCategoryByItem(param.weightBonusId[i]);
                if(category != null)
                {
                    mCategory.Add(category.Id);
                }
            }

            for(int i = 0, j = 0; i < param.baodiBonus.Count && j + 1 < param.baodiCounts.Count; i ++, j += 2)
            {
                mBaoDiTable[param.baodiBonus[i]] = new Range() {
                    lower = param.baodiCounts[j],
                    upper = param.baodiCounts[j+1]
                };
                var category = Env.Instance.GetCategoryByItem(param.baodiBonus[i]);
                if(category != null)
                {
                    mCategory.Add(category.Id);
                }
            }

            foreach(var item in param.nonOutputItems)
            {
                mNonOutputItem.Add(item);
            }

            DebugEx.FormatTrace("BaodiMergeBonusHandler::InitConfig ----> for {0}, I'am {1}", param, this);
        }

        void IMergeBonusHandler.OnRegister()
        {
            LoadAndGetDebugInfo();
        }

        void IMergeBonusHandler.OnUnRegister()
        {
            MessageCenter.Get<MSG.GAME_MERGE_ENERGY_CHANGE>().RemoveListener(_OnEnergyChange);
            MessageCenter.Get<MSG.GAME_MERGE_ITEM_EVENT>().RemoveListener(_OnItemEvent);
        }

        public BaoDiMergeBonusHandler()
        {
        } 

        private void _OnEnergyChange(int count)
        {
            if(mDebugInfo == null)
            {
                return;
            }
        }

        private void _OnItemEvent(Merge.Item item, ItemEventType ev)
        {
            if(mDebugInfo != null && ev == ItemEventType.ItemEventClaimBonus && item.TryGetItemComponent<ItemBonusCompoent>(out var bonus) 
                && mCategory.Contains(Env.Instance.GetCategoryByItem(item.tid)?.Id ?? 0))
            {
                if(mDebugInfo.claimedPointByLevel == null)
                {
                    mDebugInfo.claimedPointByLevel = new int[0];
                }
                var level = ItemUtility.GetItemLevel(item.tid) - 1;
                if(level >= 0)
                {
                    if(level >= mDebugInfo.claimedPointByLevel.Length)
                    {
                        System.Array.Resize(ref mDebugInfo.claimedPointByLevel, level + 1);
                    }
                    mDebugInfo.claimedPointByLevel[level] ++;
                }
            }
        }

        public BaoDiMergeBonusDebugInfo LoadAndGetDebugInfo()
        {
            // FAT_TODO
            // if(mDebugInfo == null && GameSwitchManager.Instance.isDebugMode)
            // {
            //     mDebugInfo = new BaoDiMergeBonusDebugInfo();
            //     var key = string.Format("BaoDiDbg{0}", mDebugKey);
            //     if(Game.Instance.accountMan.TryGetClientStorage(key, out var infoStr))
            //     {
            //         JsonUtility.FromJsonOverwrite(infoStr, mDebugInfo);
            //     }
            //     MessageCenter.Get<MSG.GAME_MERGE_ENERGY_CHANGE>().AddListener(_OnEnergyChange);
            //     MessageCenter.Get<MSG.GAME_MERGE_ITEM_EVENT>().AddListener(_OnItemEvent);
            // }
            return mDebugInfo;
        }

        public void SaveDebugInfo()
        {
            // FAT_TODO
            // if(mDebugInfo != null)
            // {
            //     var key = string.Format("BaoDiDbg{0}", mDebugKey);
            //     Game.Instance.accountMan.SetClientStorage(key, JsonUtility.ToJson(mDebugInfo));
            // }
        }

        public int Simulate(MergeBonusContext context, bool dryrun)
        {
            int output = 0;
            var result = context.result;
            var debugInfo = LoadAndGetDebugInfo();
            if(debugInfo != null)
            {
                debugInfo.totalMerge++;
                // FAT_TODO
                // if(Game.Instance.mergeItemMan.IsItemCostEnergy(context.result.tid))
                // {
                //     debugInfo.energyMerge++;
                // }
                // else
                // {
                //     debugInfo.nonEnergyMerge++;
                // }
            }
            if(mNonOutputItem.Contains(result.tid))
            {
                return 0;
            }
            //如果保底触发，则直接产出
            foreach(var entry in mNonOutputCounter)
            {
                if(entry.Value <= 0)
                {
                    //该出了
                    output = entry.Key;
                    DebugEx.FormatTrace("BaoDiMergeBonusHandler::Process ----> baodi {0}", entry.Key);
                    break;
                }
            }
            if(output <= 0)
            {
                var id = context.srcId;
                var fromAuto = checkAuto && ItemUtility.IsFromAuto(id);
                DebugEx.Info($"try get merge bonus for {id} (from auto? {fromAuto})");
                output = fromAuto
                    ? mWeightTableAuto.Keys.RandomChooseByWeight((e) => mWeightTableAuto[e])
                    : mWeightTable.Keys.RandomChooseByWeight((e) => mWeightTable[e]);
            }
            if(output > 0)
            {
                if(dryrun || null != context.world.activeBoard.SpawnItemMustWithReason(output, ItemSpawnContext.CreateWithSource(result, ItemSpawnContext.SpawnType.None), result.coord.x, result.coord.y, false, false))
                {
                    if(mDebugInfo != null)
                    {
                        if(ItemUtility.GetItemLevel(output) == 1)
                        {
                            mDebugInfo.lv1PointCount++;
                        }
                    }
                    // FAT_TODO
                    // DataTracker.TrackBPItemDrop(output, mNonOutputCounter.GetDefault(output, 0));
                    mNonOutputCounter.Remove(output);
                }
                else
                {
                    output = 0;
                }
            }

            
            foreach(var entry in mBaoDiTable)
            {
                if(!mNonOutputCounter.TryGetValue(entry.Key, out var count))
                {
                    var val = Random.Range(entry.Value.lower, entry.Value.upper);
                    mNonOutputCounter.Add(entry.Key, val);
                    DebugEx.FormatTrace("BaoDiMergeBonusHandler::Process ----> schedule baodi {0}:{1}", entry.Key, val);
                }
                else if(output != entry.Key)
                {
                    mNonOutputCounter[entry.Key] = count - 1;         //这次又没出，保底-1
                }
            }

            
            SaveDebugInfo();
            return output;
        }

        void IMergeBonusHandler.Process(MergeBonusContext context)
        {
            Simulate(context, false);
        }
    }
}