/*
 * @Author: tang.yan
 * @Description: 多轮迷你棋盘专属棋子生成处理器 (在主棋盘通过耗体生成器生成并发到迷你棋盘) 
 * @Date: 2025-01-07 10:01:27
 */
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    //多轮迷你棋盘专属棋子生成处理器 (在主棋盘通过耗体生成器生成并发到迷你棋盘)
    //没有保底逻辑 触发时可能会生成多个棋子
    public class MiniBoardMultiBonusHandler : ISpawnBonusHandler
    {
        public int priority;
        int ISpawnBonusHandler.priority => priority;        //越小越先出

        private int _configId;
        private bool _isValid => _configId > 0;
        //权重随机配置信息 区分耗体1 耗体2 耗体4
        private List<(int itemId, int num, int weight)> _outputsOneInfo = new();
        private List<(int itemId, int num, int weight)> _outputsTwoInfo = new();
        private List<(int itemId, int num, int weight)> _outputsFourInfo = new();

        //外部调用刷新随机信息
        public void RefreshOutputsInfo(int confId)
        {
            var dropConfig = Game.Manager.configMan.GetEventMiniBoardMultiDropConfig(confId);
            if (dropConfig == null)
                return;
            _configId = confId;
            _InitOutputs(_outputsOneInfo, dropConfig.OutputsOne);
            _InitOutputs(_outputsTwoInfo, dropConfig.OutputsTwo);
            _InitOutputs(_outputsFourInfo, dropConfig.OutputsFour);
            DebugEx.Info($"MiniBoard RefreshOutputsInfo : dropConfig: {dropConfig}");
        }

        private void _InitOutputs(IList<(int, int, int)> container, IList<string> outputs)
        {
            container.Clear();
            foreach (var c in outputs)
            {
                container.Add(c.ConvertToInt3());
            }
        }

        void ISpawnBonusHandler.OnRegister() { }

        void ISpawnBonusHandler.OnUnRegister()
        {
            _configId = 0;
        }
        
        void ISpawnBonusHandler.Process(SpawnBonusContext context)
        {
            if (!_isValid)
                return;
            //限制生产来源只能是ClickSource或DieOutput
            if (context.reason != ItemSpawnReason.ClickSource && context.reason != ItemSpawnReason.DieOutput) 
                return;
            SimulateSpawn(context);
        }
        
        private void SimulateSpawn(SpawnBonusContext context)
        {
            //没有来源或者没有消耗体力时返回
            if(context.from == null || context.energyCost <= 0)
                return;
            var from = context.from;
            //没有click source组件时返回
            if (!from.TryGetItemComponent<ItemClickSourceComponent>(out var comp, true))
                return;
            //click source组件的消耗id和活动配置的消耗id不一致时返回
            var curActivity = Game.Manager.miniBoardMultiMan.CurActivity;
            var actCostId = curActivity?.ConfD?.Cost ?? 0;
            if (actCostId != comp.firstCost.Cost)
                return;
            //根据是否是n倍耗体 使用不同的产出权重配置
            var state = Env.Instance.GetEnergyBoostState();
            var outputsConf = !comp.config.IsBoostable ?
                _outputsOneInfo :
                state switch
                {
                    EnergyBoostState.X2 => _outputsTwoInfo,
                    EnergyBoostState.X4 => _outputsFourInfo,
                    _ => _outputsOneInfo,
                };
            var output = outputsConf.RandomChooseByWeight(e => e.weight);
            //产出有效
            if (output.itemId > 0 && output.num > 0)
            {
                DebugEx.Info($"MiniBoardMulti Spawn : output ItemId: {output.itemId}, Num = {output.num}, config id = {_configId}, BoostState = {state}");
                //往多轮迷你棋盘上发奖励
                var reward = Game.Manager.rewardMan.BeginReward(output.itemId, output.num, ReasonString.miniboard_multi_getitem);
                var pos = BoardUtility.GetWorldPosByCoord(from.coord);
                UIFlyUtility.FlyRewardSetType(reward, pos, FlyType.MiniBoardMulti);
                //打点
                DataTracker.event_miniboard_multi_getitem.Track(curActivity, from.parent.boardId, Game.Manager.miniBoardMultiMan.GetCurRoundIndex() + 1, output.itemId, output.num, ItemUtility.GetItemLevel(output.itemId));
            }
        }
    }
}