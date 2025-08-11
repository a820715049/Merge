/*
 * @Author: tang.yan
 * @Description: 触发式产棋子组件 有点击次数,每次点击都会换一下棋子图片,次数点满后死亡同时爆奖励,若棋盘空间不够则直接发往奖励箱
 * @Date: 2025-03-19 18:03:07
 */

using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;
using UnityEngine;

namespace FAT.Merge
{
    //触发式产棋子组件 有点击次数,每次点击都会换一下棋子图片,次数点满后死亡同时爆奖励,若棋盘空间不够则直接发往奖励箱
    public class ItemTrigAutoSourceComponent : ItemComponentBase
    {
        public ComTrigAutoSource Config => mConfig;
        public int CurTriggerCount => _curTriggerCount;
        public int TotalTriggerCount => _totalTriggerCount;
        
        private ComTrigAutoSource mConfig;
        private int _curTriggerCount = 0;   //当前已经触发的次数 初始为0 走存档
        private int _totalTriggerCount = 0; //总共可以触发的次数

        public static bool Validate(ItemComConfig config)
        {
            return config?.trigAutoSourceConfig != null;
        }
        
        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).trigAutoSourceConfig;
            _totalTriggerCount = mConfig.TriggerInfo.Count;
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            var data = new ComTrigSource
            {
                TriggerCount = _curTriggerCount
            };
            itemData.ComTrigSource = data;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            var data = itemData.ComTrigSource;
            if (data != null)
            {
                _curTriggerCount = data.TriggerCount;
            }
        }

        //检查目前是否还有触发次数 没有的话就要死了
        public bool HasTriggerCount()
        {
            return _curTriggerCount < _totalTriggerCount;
        }
        
        //检查是否再点一下就会死
        public bool CheckWillDead()
        {
            return _curTriggerCount == _totalTriggerCount - 1;
        }

        public int GetDieIntoItemId()
        {
            return mConfig?.DieInto ?? 0;
        }

        //尝试消耗一次触发次数 若成功则+1 
        public bool TryUseTriggerCount(out int triggerInfoId)
        {
            triggerInfoId = 0;
            if (mConfig == null || !HasTriggerCount())
                return false;
            if (mConfig.FeatureEntry == FeatureEntry.FeatureMine)
            {
                if (!Game.Manager.mineBoardMan.TryUseToken(mConfig.CostTokenId, mConfig.CostTokenNum, ReasonString.trig_auto_source))
                {
                    return false;
                }
                triggerInfoId = mConfig.TriggerInfo[_curTriggerCount];
                _curTriggerCount++;
                //打点
                DataTracker.board_trigauto.Track(item.parent.boardId, item.coord.ToString(), item.tid, _curTriggerCount);
                return true;
            }
            return false;
        }

        //获取随机打乱后的奖励List
        public bool GetRandomOutputList(int triggerInfoId, PoolMapping.Ref<List<int>> container)
        {
            if (!Game.Manager.mergeItemMan.TryGetTrigAutoDetailConfig(triggerInfoId, out var trigInfoConf))
                return false;
            var randomList = container.obj;
            randomList.AddRange(trigInfoConf.Outputs);
            var n = randomList.Count;
            for (var i = n - 1; i > 0; i--)
            {
                // 注意：对于整数，Random.Range 的上界是排除的，所以这里用 i+1
                var j = Random.Range(0, i + 1);
                //基于元组实现 替代传统的交换元素写法
                (randomList[i], randomList[j]) = (randomList[j], randomList[i]);
            }
            return true;
        }
    }
}