/*
 * @Author: qun.chao
 * @Date: 2024-01-02 17:50:07
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using EL;

namespace FAT
{
    public class UIFlyManager : EL.Singleton<UIFlyManager>
    {
        public const PoolItemType ItemType = PoolItemType.COMMON_COLLECT_ITEM;
        private readonly Queue<FlyableItemSlice> _itemPool = new();
        public UIFlyConfig conf;

        public void Init()
        {
            if (!GameObjectPoolManager.Instance.HasPool($"{ItemType}"))
                _SetupPool();
        }

        private void _SetupPool()
        {
            var go = GameObject.Instantiate(UIFlyConfig.Instance.icon);
            GameObjectPoolManager.Instance.PreparePool(ItemType, go);
            GameObjectPoolManager.Instance.ReleaseObject(ItemType, go);
        }

        public void TryCollectReward(FlyableItemSlice item)
        {
            if (item.CurIdx >= item.SplitNum)
            {
                Game.Manager.rewardMan.CommitReward(item.Reward);
                TryFeedbackOnClaimResource(item);
            }
            else
            {
                Game.Manager.rewardMan.CommitSplitReward(item.Reward, item.Reward.rewardCount / item.SplitNum);
                TryFeedbackOnClaimResource(item);
            }
        }

        public void TryFeedbackOnClaimResource(FlyableItemSlice item)
        {
            if (item.Style == FlyStyle.Cost)
                MessageCenter.Get<MSG.UI_COST_FEEDBACK>().Dispatch(item.FlyType);
            else
                MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().Dispatch(item.FlyType);

            //播音效
            UIUtility.CommonResFeedBackSoundEffect(item.FlyType);
            //播震动
            UIUtility.CommonResFeedBackShakeEffect(item.ID);
        }

        public void TryFeedbackFlyable(FlyableItemSlice flyableItemSlice)
        {
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().Dispatch(flyableItemSlice);
        }

        public void OnCollected(FlyableItemSlice item, RectTransform trans = null)
        {
            if (trans != null)
            {
                item.Transforms.Remove(trans);
                GameObjectPoolManager.Instance.ReleaseObject(ItemType, trans.gameObject);
            }

            ++item.CurIdx;
            if (item.CurIdx >= item.SplitNum)
            {
                item.OnCollectedWholly?.Invoke(item);
                TryFeedbackFlyable(item);
                Free(item);
            }
            else
            {
                item.OnCollectedPartially?.Invoke(item);
                TryFeedbackFlyable(item);
            }
        }

        public FlyableItemSlice Alloc()
        {
            FlyableItemSlice item = null;
            if (_itemPool.Count > 0)
            {
                item = _itemPool.Dequeue();
            }

            item ??= new();
            return item;
        }

        public void Free(FlyableItemSlice item)
        {
            item.Reset();
            foreach (var trans in item.Transforms)
            {
                GameObjectPoolManager.Instance.ReleaseObject(ItemType, trans.gameObject);
            }

            item.Transforms.Clear();
            _itemPool.Enqueue(item);
        }
    }
}