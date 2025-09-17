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
    using static RecordStateHelper;

    //保底型合成奖励
    public class OutputSpawnBonusHandler : ISpawnBonusHandler
    {
        public int priority;
        int ISpawnBonusHandler.priority => priority;        //越小越先出
        public bool Valid => weight != null;
        public int id;
        private IDictionary<int, int> weight;
        private IDictionary<int, int> weight2;
        private IDictionary<int, int> weight4;
        private int target;
        private int target2;
        private int target4;
        private (int lower, int upper) range;
        private int counter;
        private readonly HashSet<int> skip = new();           //合成这些东西不出奖励

        public void Serialize(IList<AnyState> any_, int offset_)
        {
            any_.Add(ToRecord(offset_, counter));
        }

        public void Deserialize(IList<AnyState> any_, int offset_)
        {
            counter = ReadInt(offset_, any_);
        }

        public void Init(int id_, IDictionary<int, int> weight_, IDictionary<int, int> weight2_, IDictionary<int, int> weight4_, int target_, int target2_, int target4_, IList<int> range_, IList<int> skip_)
        {
            id = id_;
            weight = weight_;
            weight2 = weight2_;
            weight4 = weight4_;
            target = target_;
            target2 = target2_;
            target4 = target4_;
            range = (range_[0], range_[1]);
            ResetCounter();
            if (skip_ != null)
            {
                foreach (var v in skip_)
                {
                    skip.Add(v);
                }
            }
            DebugEx.Info($"{nameof(OutputSpawnBonusHandler)} {id} init {target} {target2} {target4} {counter} {range}");
        }

        void ISpawnBonusHandler.OnRegister()
        {

        }

        void ISpawnBonusHandler.OnUnRegister()
        {

        }

        public void ResetCounter()
        {
            counter = Random.Range(range.lower, range.upper);
        }

        public int Simulate(SpawnBonusContext context, bool dryrun)
        {
            var from = context.from;
            if (from is null || context.energyCost <= 0 || skip.Contains(from.tid))
            {
                return 0;
            }
            var output = 0;
            var boostValid = from.TryGetItemComponent<ItemClickSourceComponent>(out var comp, true)
                && comp.config.IsBoostable;
            // 耗体不同
            var state = Env.Instance.GetEnergyBoostState();
            if (counter <= 0)
            {
                // 保底触发则直接产出
                output = !boostValid ? target :
                        state switch { EnergyBoostState.X2 => target2, EnergyBoostState.X4 => target4, _ => target };
                DebugEx.Info($"{nameof(OutputSpawnBonusHandler)} {id} output fixed: {output} {state}");
            }
            if (output <= 0)
            {
                var w = !boostValid ? weight :
                        state switch { EnergyBoostState.X2 => weight2, EnergyBoostState.X4 => weight4, _ => weight };
                output = w.Keys.RandomChooseByWeight(e => w[e]);
                DebugEx.Info($"{nameof(OutputSpawnBonusHandler)} {id} output weighted: {output} {state}");
            }
            if (dryrun || output <= 0)
            {
                goto end;
            }
            var result = context.result;
            if (Game.Manager.mergeBoardMan.activeWorld.isEquivalentToMain ||
                null == context.world.activeBoard.SpawnItemMustWithReason(output, ItemSpawnContext.CreateWithSource(from, ItemSpawnContext.SpawnType.None), from.coord.x, from.coord.y, false, false))
            {
                var d = Game.Manager.rewardMan.BeginReward(output, 1, ReasonString.step);
                var pos = BoardUtility.GetWorldPosByCoord(from.coord);
                UIFlyUtility.FlyReward(d, pos);
            }
        end:
            if (output == target || output == target2 || output == target4)
            {
                ResetCounter();
            }
            else
            {
                --counter;
            }
            DebugEx.Info($"{nameof(OutputSpawnBonusHandler)} {id} output counter: {counter}");
            return output;
        }

        void ISpawnBonusHandler.Process(SpawnBonusContext context)
        {
            if (!Valid || context.reason != ItemSpawnReason.ClickSource) return;
            Simulate(context, false);
        }
    }
}