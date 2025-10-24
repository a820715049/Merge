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
        private int target;
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

        public void Init(int id_, IDictionary<int, int> weight_, int target_, IList<int> range_, IList<int> skip_)
        {
            id = id_;
            weight = weight_;
            target = target_;
            range = (range_[0], range_[1]);
            ResetCounter();
            if (skip_ != null)
            {
                foreach (var v in skip_)
                {
                    skip.Add(v);
                }
            }
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
            // 统一仅使用 OutputsOne（忽略 Two/Four），不再随倍数切换配置
            var state = Env.Instance.GetEnergyBoostState();
            if (counter <= 0)
            {
                // 保底触发则直接产出（只用 target）
                output = target;
                DebugEx.Info($"{nameof(OutputSpawnBonusHandler)} {id} output fixed: {output} {state}");
            }
            if (output <= 0)
            {
                var w = weight;
                output = w.Keys.RandomChooseByWeight(e => w[e]);
                DebugEx.Info($"{nameof(OutputSpawnBonusHandler)} {id} output weighted: {output} {state}");
            }
            if (dryrun || output <= 0)
            {
                goto end;
            }
            // 根据能量加倍等级对保底产出的棋子进行升级
            var finalOutput = output;
            if (boostValid)
            {
                var level_add = EnergyBoostUtility.GetBoostLevel();
                if (level_add > 0)
                {
                    finalOutput = Env.Instance.GetNextLevelItemId(output, level_add);
                    DebugEx.Info($"[EnergyBoost_opt] 活动Step，原始棋子{output}，升级后棋子{finalOutput}，等级+{level_add}。");
                }
            }
            if (Game.Manager.mergeBoardMan.activeWorld.isEquivalentToMain
            || null == context.world.activeBoard.SpawnItemMustWithReason(finalOutput, ItemSpawnContext.CreateWithSource(from, ItemSpawnContext.SpawnType.None), from.coord.x, from.coord.y, false, false))
            {
                var d = Game.Manager.rewardMan.BeginReward(finalOutput, 1, ReasonString.step);
                var pos = BoardUtility.GetWorldPosByCoord(from.coord);
                UIFlyUtility.FlyReward(d, pos);
            }
        end:
            if (output == target)
            {
                ResetCounter();
            }
            else
            {
                --counter;
            }
            return output;
        }

        void ISpawnBonusHandler.Process(SpawnBonusContext context)
        {
            if (!Valid || context.reason != ItemSpawnReason.ClickSource) return;
            Simulate(context, false);
        }
    }
}
