/*
 * @Author: tang.yan
 * @Description: 随机宝箱管理器
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/ev8gk5lizqglu3fc
 * @Date: 2023-11-30 16:11:25
 */

using System.Collections.Generic;
using EL;
using UnityEngine;
using fat.rawdata;

namespace FAT
{
    public class RandomBoxMan : IGameModule
    {
        //随机宝箱数据
        public class RandomBoxData
        {
            public int RandomBoxId;
            public int BoxIndex;    //唯一自增id 用于识别多个id相同的随机宝箱
            public List<RewardCommitData> Reward; 
            public RandomBoxState State;
            public ReasonString ProduceFrom; //记录宝箱产物的来源 
        }
        
        public enum RandomBoxState
        {
            Begin,  //开始领取
            Ready,  //准备领取
            Finish  //完成领取
        }
        
        //当前缓存的随机宝箱队列
        private List<RandomBoxData> _cacheRandomBoxList = new List<RandomBoxData>();
        private static int _boxIndex = 0;
        
        public void TryOpenRandomBoxTips(int itemId, Vector3 startWorldPos, float offset)
        {
            var boxConfig = Game.Manager.objectMan.GetRandomBoxConfig(itemId);
            if (boxConfig == null)
                return;
            var tipsPrefab = boxConfig.SpecialTipsPrefab;
            if (string.IsNullOrEmpty(tipsPrefab))
            {
                UIManager.Instance.OpenWindow(UIConfig.UIRandomBoxTips, startWorldPos, offset, itemId);
            }
            else
            {
                UIConfig.UIRandomBoxSpecialTips.Replace(tipsPrefab.ConvertToAssetConfig());
                UIManager.Instance.OpenWindow(UIConfig.UIRandomBoxSpecialTips, startWorldPos, offset);
            }
        }

        /// <summary>
        /// 解析指定随机宝箱中的奖励
        /// </summary>
        /// <param name="randomBoxId">随机宝箱id ObjRandomChest id</param>
        /// <param name="boxNum">一次性发放的该宝箱的数量</param>
        public void TryBeginRandomBox(int randomBoxId, int boxNum, ReasonString reason = null)
        {
            DebugEx.FormatInfo("RandomBoxMan::TryBeginRandomBox  boxId = {0}, boxNum = {1}", randomBoxId, boxNum);
            //用之前清理
            _ClearFinishBoxData();
            var randomBoxConfig = Game.Manager.objectMan.GetRandomBoxConfig(randomBoxId);
            if (randomBoxConfig == null)
            {
                DebugEx.FormatError("RandomBoxMan::TryBeginRandomBox : error randomBoxId = {0} !", randomBoxId);
                return;
            }
            //构造随机id权重列表 放在最外面避免重复创建
            using var _ = ObjectPool<List<(int, int)>>.GlobalPool.AllocStub(out var randomList);
            var configMan = Game.Manager.configMan;
            foreach (var randomRewardId in randomBoxConfig.RandomReward)
            {
                var randomRewardConfig = configMan.GetRandomRewardConfigById(randomRewardId);
                if (randomRewardConfig != null)
                {
                    randomList.Add((randomRewardId, randomRewardConfig.Weight));
                }
            }
            //构造宝箱数据
            for (int i = 0; i < boxNum; i++)
            {
                var boxData = new RandomBoxData()
                {
                    RandomBoxId = randomBoxId,
                    BoxIndex = _boxIndex++,
                    Reward = new List<RewardCommitData>(),
                    State = RandomBoxState.Begin
                };
                //通过充值获得的随机宝箱，其产物的from为rand_chest_iap
                if (reason != null && reason == ReasonString.purchase)
                    boxData.ProduceFrom = ReasonString.rand_chest_iap;
                else
                    boxData.ProduceFrom = ReasonString.random_chest;
                //填充万能卡
                _TryAddCardJoker(randomBoxConfig, boxData);
                //填充普通奖励 分固定奖励和随机奖励
                _TryAddNormalReward(randomBoxConfig, boxData, randomList);
                //宝箱数据加入list
                _cacheRandomBoxList.Add(boxData);
            }
        }
        
        /// <summary>
        /// 标记指定随机宝箱为可领取
        /// </summary>
        public void TryReadyRandomBox(int randomBoxId, int boxNum)
        {
            int count = 0;
            foreach (var randomBoxData in _cacheRandomBoxList)
            {
                if (count < boxNum)
                {
                    if (randomBoxData.RandomBoxId == randomBoxId && randomBoxData.State == RandomBoxState.Begin)
                    {
                        randomBoxData.State = RandomBoxState.Ready;
                        count++;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 依照当前的队列 依次领取随机宝箱奖励(界面表现)
        /// </summary>
        public void TryClaimRandomBox()
        {
            //界面正在开着时 无视调用请求
            if (UIManager.Instance.IsShow(UIConfig.UIRandomBox))
                return;
            var canClaim = CheckCanClaimRandomBox();
            //当前没有可领取宝箱时 无视调用请求
            if (!canClaim)
            {
                return;
            }
            UIManager.Instance.OpenWindow(UIConfig.UIRandomBox);
        }

        public bool CheckCanClaimRandomBox()
        {
            bool canClaim = false;
            foreach (var boxData in _cacheRandomBoxList)
            {
                if (boxData.State == RandomBoxState.Ready)
                {
                    canClaim = true;
                    break;
                }
            }
            return canClaim;
        }

        //获取当前最近的可以领取的奖励宝箱数据 UI界面OnPreOpen中调用
        public RandomBoxData TryGetCanClaimBoxData()
        {
            foreach (var boxData in _cacheRandomBoxList)
            {
                if (boxData.State == RandomBoxState.Ready)
                {
                    return boxData;
                }
            }
            return null;
        }

        //关闭界面时完成对应宝箱的领取
        public void TryFinishRandomBoxData(RandomBoxData randomBoxData)
        {
            if (randomBoxData == null)
                return;
            foreach (var boxData in _cacheRandomBoxList)
            {
                if (randomBoxData.RandomBoxId == boxData.RandomBoxId && 
                    randomBoxData.BoxIndex == boxData.BoxIndex &&
                    boxData.State == RandomBoxState.Ready)
                {
                    DebugEx.FormatInfo("RandomBoxMan::TryFinishRandomBoxData boxId = {0}, boxIndex = {1}", boxData.RandomBoxId, boxData.BoxIndex);
                    boxData.State = RandomBoxState.Finish;
                    Game.Manager.specialRewardMan.TryFinishSpecialReward(ObjConfigType.RandomBox, boxData.RandomBoxId);
                    break;
                }
            }
        }

        private void _ClearFinishBoxData()
        {
            for (int i = _cacheRandomBoxList.Count - 1; i >= 0; i--)
            {
                var boxData = _cacheRandomBoxList[i];
                if (boxData.State == RandomBoxState.Finish)
                {
                    _cacheRandomBoxList.RemoveAt(i);
                }
            }
        }
        
        private void _ClearReadyBoxData()
        {
            for (int i = _cacheRandomBoxList.Count - 1; i >= 0; i--)
            {
                var boxData = _cacheRandomBoxList[i];
                if (boxData.State == RandomBoxState.Ready)
                {
                    _cacheRandomBoxList.RemoveAt(i);
                }
            }
        }

        public void Reset()
        {
            _boxIndex = 0;
            _ClearFinishBoxData();
            //reset时清理所有已经ready的随机宝箱
            _ClearReadyBoxData();
        }

        public void LoadConfig() { }

        public void Startup() { }

        private System.Random _random = new System.Random();
        private void _TryAddCardJoker(ObjRandomChest boxConfig, RandomBoxData boxData)
        {
            var jokerId = boxConfig?.JokerReward ?? 0;
            if (jokerId <= 0 || !Game.Manager.objectMan.IsType(jokerId, ObjConfigType.CardJoker))
                return;
            var prob = boxConfig?.JokerProb ?? 0;
            if (prob <= 0)
                return;
            //Random.Next会等概率随机出[0, prob - 1]区间的数 默认等于0时即为命中
            if (_random.Next(prob) == 0)
            {
                boxData.Reward.Add(Game.Manager.rewardMan.BeginReward(jokerId, 1, boxData.ProduceFrom));
            }
        }

        private void _TryAddNormalReward(ObjRandomChest boxConfig, RandomBoxData boxData, List<(int, int)> randomList)
        {
            var hasFixReward = boxConfig.FixedReward.Count > 0;
            if (hasFixReward && boxConfig.IsFixedFirst)
            {
                _TryAddFixReward(boxConfig, boxData);
                _TryAddRandomReward(boxData, randomList);
            }
            else if (hasFixReward && !boxConfig.IsFixedFirst)
            {
                _TryAddRandomReward(boxData, randomList);
                _TryAddFixReward(boxConfig, boxData);
            }
            else
            {
                _TryAddRandomReward(boxData, randomList);
            }
        }

        private void _TryAddFixReward(ObjRandomChest boxConfig, RandomBoxData boxData)
        {
            foreach (var reward in boxConfig.FixedReward)
            {
                var r = reward.ConvertToRewardConfig();
                if (r != null)
                {
                    boxData.Reward.Add(Game.Manager.rewardMan.BeginReward(r.Id, r.Count, boxData.ProduceFrom));
                }
            }
        }

        private void _TryAddRandomReward(RandomBoxData boxData, List<(int, int)> randomList)
        {
            //宝箱没有配置随机奖励时返回
            if (randomList.Count <= 0) return;
            var configMan = Game.Manager.configMan;
            var rewardMan = Game.Manager.rewardMan;
            var mergeLevelMan = Game.Manager.mergeLevelMan;
            //根据权重随机 多个相同的宝箱 也要每次都随机一次
            var resultId = randomList.RandomChooseByWeight((e) => e.Item2).Item1;
            var rewardList = configMan.GetRandomRewardConfigById(resultId)?.Reward;
            if (rewardList != null)
            {
                foreach (var reward in rewardList)
                {
                    //随机宝箱发奖时支持按参数类型计算
                    var (_cfg_id, _cfg_count, _param) = reward.ConvertToInt3();
                    var (_id, _count) = rewardMan.CalcDynamicReward(_cfg_id, _cfg_count, mergeLevelMan.GetCurrentLevelRate(), 0, _param);
                    boxData.Reward.Add(rewardMan.BeginReward(_id, _count, boxData.ProduceFrom));
                }
            }
            else
            {
                DebugEx.FormatError("RandomBoxMan._TryAddRandomReward : error resultId = {0}, randomBoxId = {1} !", resultId, boxData.RandomBoxId);
            }
        }
    }
}
