/**
 * @Author: handong.liu
 * @Date: 2021-02-23 12:15:09
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class ItemBubbleComponent: ItemComponentBase
    {
        public int breakCost => Env.Instance.IsBubbleGuidePassed()?mBreakCost:0;
        public int lifeLeftMilli => Mathf.Max(0, bubbleExpireTime - mLifeCounter);
        private int bubbleExpireTime => Game.Manager.configMan.globalConfig.BubbleExpired;
        private int mLifeCounter;
        private int mBreakCost;
        private bool mIsWatchingAds;

        public static bool SerializeDelta(MergeItem newData, MergeItem oldData)
        {
            if(oldData.ComBubble != null && oldData.ComBubble.Equals(newData.ComBubble))
            {
                newData.ComBubble = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            long lifeStart = 0;
            int lifeCounter = 0;
            // if(item.parent == null)
            // {
            lifeCounter = mLifeCounter;
            // }
            // else
            // {
            //     lifeStart = (item.world.lastTickMilli - mLifeCounter) / 1000;
            // }
            itemData.ComBubble = new ComBubble();
            itemData.ComBubble.Life = lifeCounter;
            itemData.ComBubble.Start = lifeStart;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if(itemData.ComBubble != null)
            {
                // if(itemData.ComBubble.Start > 0)
                // {
                //     mLifeCounter = Mathf.Max(0, (int)(item.world.lastActiveTime * 1000 - itemData.ComBubble.Start * 1000));
                // }
                // else
                // {
                    mLifeCounter = itemData.ComBubble.Life;
                // }
            }
        }

        public bool BreakBubble()
        {
            var env = Env.Instance;
            if(item.parent != null && env.CanUseGem(breakCost))
            {
                env.UseGem(breakCost, ReasonString.bubble);
                item.parent.UnleashBubbleItem(item);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OnWatchBubbleAd_Prepare()
        {
            mIsWatchingAds = true;
        }

        public void OnWatchBubbleAd_Finish(bool suc)
        {
            mIsWatchingAds = false;
            if (suc)
            {
                // 观看成功 解锁bubble
                item.parent?.UnleashBubbleItem(item);
                // track
                DataTracker.TrackMergeActionBubble(item, ItemUtility.GetItemLevel(item.tid), true, false);
            }
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            var config = Env.Instance.GetItemMergeConfig(item.tid);
            if(config == null)
            {
                DebugEx.FormatWarning("ItemBubbleComponent ----> no config for item {0}", item.id);
                mBreakCost = 0;
            }
            else
            {
                mBreakCost = config.BubblePrice;
            }
        }

        protected override void OnUpdate(int dt)
        {
            if (mIsWatchingAds)
                return;

            base.OnUpdate(dt);
            if(item.parent != null)
            {
                if(Env.Instance.IsBubbleGuidePassed())
                {
                    mLifeCounter += dt;
                    if(mLifeCounter >= bubbleExpireTime && !_IsInteracting())
                    {
                        item.parent.KillBubbleItem(item);
                    }
                }
                else
                {
                    mLifeCounter = 0;           //keep bubble 0 life
                }
            }
        }

        private bool _IsInteracting()
        {
            return Game.Manager.mergeBoardMan.recentActiveItem == item;
        }
    }
}