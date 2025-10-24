/*
 * @Author: tang.yan
 * @Description: 积分活动变种(麦克风版) - 棋子左下角挂积分处理器
 * @Date: 2025-09-12 15:09:02
 */

using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public class ScoreMicSpawnBonusHandler : ISpawnBonusHandler
    {
        public int priority;
        int ISpawnBonusHandler.priority => priority;        //越小越先出

        private bool _isValid => _actInst != null && _actInst.Active;
        //权重随机配置信息 区分耗体1 耗体2 耗体4
        private List<(int itemId, int num, int weight)> _outputsOneInfo = new();
        private List<(int itemId, int num, int weight)> _outputsTwoInfo = new();
        private List<(int itemId, int num, int weight)> _outputsFourInfo = new();

        private bool _isDirty = true;
        private ActivityScoreMic _actInst;

        public ScoreMicSpawnBonusHandler(ActivityScoreMic act)
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
            var curDropConf = _actInst.GetCurMilestoneInfo();
            if (curDropConf == null)
            {
                DebugEx.Info($"ScoreMicSpawnBonusHandler : EnsureOutputMap Error, activity Id = {_actInst?.Id}");
                return;
            }
            _InitOutputs(_outputsOneInfo, curDropConf.OutputsOne);
            _InitOutputs(_outputsTwoInfo, curDropConf.OutputsTwo);
            _InitOutputs(_outputsFourInfo, curDropConf.OutputsFour);
        }

        private void _InitOutputs(IList<(int, int, int)> container, IDictionary<int, int> outputs)
        {
            container.Clear();
            foreach (var c in outputs)
            {
                //默认num为1
                var info = (itemId : c.Key, num : 1, weight : c.Value);
                container.Add(info);
            }
        }

        void ISpawnBonusHandler.OnRegister() { }

        void ISpawnBonusHandler.OnUnRegister() { }
        
        void ISpawnBonusHandler.Process(SpawnBonusContext context)
        {
            if (!_isValid)
                return;
            //限制生产来源只能是ClickSource或DieOutput，BubbleBorn时也会生成
            if (context.reason == ItemSpawnReason.ClickSource || context.reason == ItemSpawnReason.DieOutput)
            {
                SimulateSpawn(context);
            }
            else if (context.reason == ItemSpawnReason.BubbleBorn)
            {
                SimulateBubbleSpawn(context);
            }
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
            //click source组件的消耗id和体力id不一致时返回
            if (_actInst.Conf.Cost != comp.firstCost.Cost)
                return;
            EnsureOutputMap();
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
                _SetActivityToken(context, output.itemId, output.num);
                DebugEx.Info($"ScoreMicSpawnBonusHandler.SimulateSpawn : output ItemId: {output.itemId}, Num = {output.num}, BoostState = {state}");
            }
        }

        private void SimulateBubbleSpawn(SpawnBonusContext context)
        {
            //没有结果或者时返回
            if(context.result == null)
                return;
            var result = context.result;
            //没有泡泡组件时返回
            if (!result.TryGetItemComponent<ItemBubbleComponent>(out var comp, true))
                return;
            //是冰冻棋子时返回
            if (comp.IsFrozenItem())
                return;
            //概率没到时返回
            if (!_actInst.CheckCanSpawnOnBubble())
                return;
            //泡泡算分逻辑
            var tokenId = _actInst.GetTokenIdForBubbleItem(result);
            if (tokenId > 0)
            {
                DebugEx.Info($"ScoreMicSpawnBonusHandler.SimulateBubbleSpawn : bubble ItemId: {result.tid}, tokenId = {tokenId}");
                //默认数量为1
                _SetActivityToken(context, tokenId, 1);
            }
        }
        
        //尝试增加活动token组件 如果已有则刷新数据  
        private void _SetActivityToken(SpawnBonusContext context, int tokenId, int tokenNum)
        {
            var target = context.result;
            target.AppendWithActivityTokenComponent();
            if (target.TryGetItemComponent(out ItemActivityTokenComponent actTokenCom))
            {
                actTokenCom.SetActivityInfo_BL(_actInst.Id, tokenId, tokenNum);
            }
        }
    }
}