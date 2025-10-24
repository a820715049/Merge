/*
 * @Author: tang.yan
 * @Description: 棋子泡泡组件 兼容泡泡和冰冻类型的棋子 
 * @Date: 2025-08-18 14:08:35
 */
using UnityEngine;
using fat.gamekitdata;

namespace FAT.Merge
{
    public enum ItemBubbleType
    {
        None = 0,   //默认
        Bubble = 1, //泡泡棋子
        Frozen = 2, //冰冻棋子
    }
    
    public class ItemBubbleComponent : ItemComponentBase
    {
        //打破气泡需要花费的钻石数量
        public int BreakCost => Env.Instance.IsBubbleGuidePassed() ? _breakCost : 0;
        //棋子剩余存在的时间 单位毫秒
        public long LifeRemainTime => (long)Mathf.Max(0, _lifeTime - _lifeCounter);
        //棋子类型 默认为泡泡棋子 会进存档
        private ItemBubbleType _bubbleType = ItemBubbleType.None;
        //棋子可以存在的时间(单位毫秒) 会进存档 借助ComBubble.Start字段存储
        private long _lifeTime = 0;
        //棋子已存在的时间(单位毫秒) 会进存档
        private int _lifeCounter;
        //是否正在看广告
        private bool _isWatchingAds;
        //泡泡打破需要花费的钻石数
        private int _breakCost;
        
        public void InitItemBubbleType(ItemBubbleType type, long lifeTime = 0, int lifeCounter = 0)
        {
            _bubbleType = type;
            _OnInit(lifeTime, lifeCounter);
        }

        public bool IsBubbleItem()
        {
            return _bubbleType == ItemBubbleType.Bubble;
        }

        public bool IsFrozenItem()
        {
            return _bubbleType == ItemBubbleType.Frozen;
        }

        #region 看广告

        public void OnWatchBubbleAd_Prepare()
        {
            _isWatchingAds = true;
        }

        public void OnWatchBubbleAd_Finish(bool suc)
        {
            _isWatchingAds = false;
            if (suc)
            {
                // 观看成功 解锁bubble
                item.parent?.UnleashBubbleItem(item);
                // track
                DataTracker.TrackMergeActionBubble(item, ItemUtility.GetItemLevel(item.tid), true, false);
            }
        }

        #endregion

        #region 内部方法

        private void _OnInit(long lifeTime = 0, int lifeCounter = 0)
        {
            switch (_bubbleType)
            {
                case ItemBubbleType.Bubble:
                    _InitBubble(lifeCounter);
                    break;
                case ItemBubbleType.Frozen:
                    _InitFrozen(lifeTime, lifeCounter);
                    break;
            }
        }

        private void _InitBubble(int lifeCounter = 0)
        {
            var config = Env.Instance.GetItemMergeConfig(item.tid);
            _breakCost = config?.BubblePrice ?? 0;
            _lifeTime = Game.Manager.configMan.globalConfig.BubbleExpired;
            _lifeCounter = lifeCounter;
        }

        private void _InitFrozen(long lifeTime = 0, int lifeCounter = 0)
        {
            _breakCost = 0;
            _lifeTime = lifeTime;
            _lifeCounter = lifeCounter;
        }
        
        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComBubble = new ComBubble();
            itemData.ComBubble.Life = _lifeCounter;
            itemData.ComBubble.Start = _lifeTime;
            itemData.ComBubble.Type = (int)_bubbleType;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            var data = itemData.ComBubble;
            if (data != null)
            {
                _lifeCounter = data.Life;
                _lifeTime = data.Start;
                //解析存档时 如果Type值<=0 则默认为Bubble类型 (应对老玩家在存档中含有泡泡棋子时 更新到冰冻棋子相关代码的情况)
                var type = data.Type;
                var bubbleType = type > 0 ? (ItemBubbleType)type : ItemBubbleType.Bubble;
                _bubbleType = bubbleType;
                _OnInit(_lifeTime, _lifeCounter);
            }
        }

        protected override void OnUpdate(int dt)
        {
            base.OnUpdate(dt);
            if (item.parent == null)
                return;
            if (IsBubbleItem())
            {
                //只有泡泡棋子会在看广告时锁一下
                if (_isWatchingAds)
                    return;
                if (Env.Instance.IsBubbleGuidePassed())
                {
                    _lifeCounter += dt;
                    if (_lifeCounter >= _lifeTime && !_IsInteracting())
                    {
                        item.parent.KillBubbleItem(item, _bubbleType, out _);
                    }
                }
                else
                {
                    _lifeCounter = 0;           //keep bubble 0 life
                }
            }
            else if (IsFrozenItem())
            {
                //冰冻棋子到点后直接kill 不会因为选中而延长死亡
                _lifeCounter += dt;
                if (_lifeCounter >= _lifeTime)
                {
                    item.parent.KillBubbleItem(item, _bubbleType, out var transItemId);
                    //冰冻棋子死亡时打点
                    DataTracker.event_frozen_item_expire.Track(item.id, item.tid, ItemUtility.GetItemLevel(item.tid), $"{transItemId}:1");
                }
            }
        }

        private bool _IsInteracting()
        {
            return Game.Manager.mergeBoardMan.recentActiveItem == item;
        }

        #endregion
    }
}
