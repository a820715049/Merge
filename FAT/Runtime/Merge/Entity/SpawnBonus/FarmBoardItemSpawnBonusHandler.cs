/*
 * @Author: tang.yan
 * @Description: 农场棋盘代币生成处理器 (在主棋盘通过耗体生成器生成并发到农场棋盘)
 * @Date: 2025-05-19 15:05:42
 */
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    //农场棋盘代币生成处理器 (在主棋盘通过耗体生成器生成并发到农场棋盘)
    //没有保底逻辑 触发时可能会生成多个棋子
    public class FarmBoardItemSpawnBonusHandler : ISpawnBonusHandler
    {
        public int priority;
        int ISpawnBonusHandler.priority => priority;        //越小越先出

        private bool _isValid => _actInst != null && _actInst.Active;
        //权重随机配置信息 区分耗体1 耗体2 耗体4
        private List<(int itemId, int num, int weight)> _outputsOneInfo = new();

        private bool _isDirty = true;
        private FarmBoardActivity _actInst;

        public FarmBoardItemSpawnBonusHandler(FarmBoardActivity act)
        {
            _actInst = act;
            SetDirty();
        }

        public void SetDirty()
        {
            _isDirty = true;
        }

        private void EnsureOutputMap()
        {
            if (!_isDirty)
                return;
            _isDirty = false;
            var curDropConf = _actInst.GetCurDropConf();
            if (curDropConf == null)
            {
                DebugEx.Info($"FarmBoard Spawn : EnsureOutputMap Error, activity Id = {_actInst?.Id}");
                return;
            }
            _InitOutputs(_outputsOneInfo, curDropConf.OutputsOne);
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

        void ISpawnBonusHandler.OnUnRegister() { }

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
            if (context.from == null || context.energyCost <= 0)
                return;
            var from = context.from;
            //没有click source组件时返回
            if (!from.TryGetItemComponent<ItemClickSourceComponent>(out var comp, true))
                return;
            //活动不是耗体产出类型时返回
            if (!_actInst.IsEnergyType())
                return;
            //click source组件的消耗id和体力id不一致时返回
            if (_actInst.ConfD.Cost != comp.firstCost.Cost)
                return;
            EnsureOutputMap();
            //统一从 OutputsOne 读取，根据能量加倍状态调整数量
            var state = Env.Instance.GetEnergyBoostState();
            var outputsConf = _outputsOneInfo;
            var output = outputsConf.RandomChooseByWeight(e => e.weight);
            //产出有效
            if (output.itemId > 0 && output.num > 0)
            {
                var rate = comp.config.IsBoostable ? EnergyBoostUtility.GetEnergyRate() : 1;
                var finalNum = output.num * rate;
                DebugEx.Info($"FarmBoard Spawn : output ItemId: {output.itemId}, Num = {finalNum}, BoostState = {state}");
                DebugEx.Info($"[EnergyBoost_opt] 活动{_actInst?.GetType().Name}，原始产出{output.num}，实际产出{finalNum}个，体力倍数{rate}。");
                //往农场棋盘发代币
                var reward = Game.Manager.rewardMan.BeginReward(output.itemId, finalNum, ReasonString.farm_merge);
                var pos = BoardUtility.GetWorldPosByCoord(from.coord);
                UIFlyUtility.FlyRewardSetType(reward, pos, FlyType.FarmToken);
            }
        }
    }
}
