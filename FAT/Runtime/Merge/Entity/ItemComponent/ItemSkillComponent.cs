/**
 * @Author: handong.liu
 * @Date: 2021-06-16 11:38:45
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public enum ItemSkillStateType
    {
        Normal,     //正常技能使用
        UpdateChest       //时间相关需要作用到Chest上
    }
    public struct ItemSkillState
    {
        public ItemSkillStateType type;
        public long useCount;           //使用了多少量，对SandGlass来说表示skip掉多少时间(毫秒)
    }
    public class ItemSkillComponent: ItemComponentBase
    {
        public SkillType type => mConfig.Type;
        public IList<int> param => mConfig.Params;
        public IList<int> param2 => mConfig.Param2;
        public IList<int> param3 => mConfig.Param3;
        public IList<string> descList => mConfig.Desc2;
        public int sandGlassSeconds => mBuffCounter;
        private ComMergeSkill mConfig;

        public bool teslaActive => type == SkillType.Tesla && mBuffCounter > 0;
        public int teslaTotalLife => param[0] * 1000;
        public int teslaLeftMilli => teslaTotalLife - mBuffCounter;
        private int mBuffCounter;  //对SandGlass, 表示能过多少时间(秒)，对tesla，表示走了多少毫秒

        public int stackCount => mStackNum;
        private int mStackNum;

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).skillConfig;
            switch(mConfig.Type)
            {
                case SkillType.SandGlass:
                    mBuffCounter = mConfig.Params[0];
                break;
            }
            _SetupStack();
        }

        public static bool SerializeDelta(MergeItem newData, MergeItem oldData)
        {
            if (oldData.ComSkill!= null && oldData.ComSkill.Equals(newData.ComSkill))
            {
                newData.ComSkill = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        public int MaxStackNum()
        {
            if (CanStack())
                return mConfig.Param2[0];
            return 0;
        }

        public int StackBy(int num)
        {
            var max = MaxStackNum();
            if (mStackNum + num <= max)
            {
                mStackNum += num;
                return 0;
            }
            else
            {
                var overflow = mStackNum + num - max;
                mStackNum = max;
                return overflow;
            }
        }

        public bool CanStack()
        {
            switch (mConfig.Type)
            {
                case SkillType.Degrade:
                case SkillType.Upgrade:
                    return true;
                    break;
            }
            return false;
        }

        public void MultiplyBy(int count)
        {
            switch(mConfig.Type)
            {
                case SkillType.SandGlass:
                    DebugEx.FormatInfo("ItemSkillComponent::MultiplyBy ----> {0} to {1}", mBuffCounter, count);
                    mBuffCounter *= count;
                    break;
            }
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComSkill = new ComSkill();
            itemData.ComSkill.BuffCounter = mBuffCounter;
            itemData.ComSkill.StackCounter = mStackNum;
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if (itemData.ComSkill != null)
            {
                mBuffCounter = itemData.ComSkill.BuffCounter;
                mStackNum = itemData.ComSkill.StackCounter;
                if (mStackNum < 1) _SetupStack();
            }
        }

        public static bool Validate(ItemComConfig config)
        {
            return config?.skillConfig != null;
        }

        public static string ProcessDesc(string baseString, SkillType skillType, IList<int> paramList)
        {
            switch(skillType)
            {
                case SkillType.Degrade:
                case SkillType.Upgrade:
                //do nothing
                break;
                case SkillType.TimeSkip:
                case SkillType.SandGlass:
                baseString = baseString.Replace("{SP1}", (paramList[0] / 3600).ToString());
                break;
                default:
                baseString = baseString.Replace("{SP1}", paramList[0].ToString());
                break;
            }
            return baseString;
        }

        public bool IsNeedTarget()
        {
            return type != SkillType.TimeSkip && type != SkillType.Tesla;           //timeskip don't need target
        }

        protected override void OnPostMerge(Item src, Item dst)
        {
            base.OnPostMerge(src, dst);
            if(src.TryGetItemComponent<ItemSkillComponent>(out var skillSrc) && dst.TryGetItemComponent<ItemSkillComponent>(out var skillDst)
                && skillSrc.type == SkillType.SandGlass && skillDst.type == SkillType.SandGlass)
            {
                long newCounter = skillSrc.mBuffCounter + skillDst.mBuffCounter;
                if(newCounter > int.MaxValue)           //防止溢出
                {
                    newCounter = int.MaxValue;
                }
                else if(newCounter < 0)
                {
                    newCounter = 0;
                }
                mBuffCounter = (int)newCounter;
                DebugEx.FormatInfo("ItemSKillComponent::OnPostMerge ----> SandGlass get time from merge {0}, {1}, {2}", skillSrc.mBuffCounter, skillDst.mBuffCounter, mBuffCounter); 
            }
        }

        public bool CanUseForTarget(Item target, out ItemSkillState state)
        {
            state = default;
            if(target.isLocked || target.parent == null || target.isDead || target.isUnderCloud)
            {
                return false;
            }
            if (ItemUtility.IsInBubble(target))
            {
                return false;
            }
            if(ItemUtility.CanMerge(item, target))          //if can merge, won't use
            {
                return false;
            }
            if(type == SkillType.SkillDustClear)
            {
                return target.isFrozen;                     //only frozen item can use dust clear
            }
            // 允许万能棋子和网内棋子合成
            if (target.isFrozen && type != SkillType.Upgrade)
            {
                return false;
            }
            switch (type)
            {
                case SkillType.NoCd:
                return target.TryGetItemComponent<ItemClickSourceComponent>(out var com) && !com.isNoCD;
                case SkillType.InstantOutput:
                return target.HasComponent(ItemComponentType.ClickSouce);
                case SkillType.Degrade:
                {
                    if(!target.isActive)                //active condition
                    {
                        return false;
                    }
                    var cfg = Env.Instance.GetItemMergeConfig(target.tid);
                    return cfg.IsDegradable;
                }
                case SkillType.Upgrade:
                {
                    var cfg = Env.Instance.GetItemMergeConfig(target.tid);
                    return cfg.IsJokerable;
                }
                case SkillType.Lightbulb:
                {
                    if(target.TryGetItemComponent<ItemClickSourceComponent>(out var click))
                    {
                        return click.config.IsBoostItem;
                    }
                    return false;
                }
                case SkillType.SandGlass:
                {
                    if(!target.isActive)                //active condition
                    {
                        return false;
                    }
                    if(target.HasComponent(ItemComponentType.Bubble))
                    {
                        return false;
                    }
                    long milliToConsume = 0;
                    if(target.TryGetItemComponent<ItemClickSourceComponent>(out var click) && click.itemCount <= 0)
                    {
                        if(click.isOutputing)
                        {
                            //小cd
                            _SelectMinAndPositive(ref milliToConsume, click.config.OutputTime * 1000 - click.outputMilli);
                        }
                        if(click.isReviving)
                        {
                            //大cd
                            _SelectMinAndPositive(ref milliToConsume, click.reviveTotalMilli - click.reviveMilli);
                        }
                    }
                    if(target.TryGetItemComponent<ItemAutoSourceComponent>(out var auto) && auto.isOutputing)
                    {
                        _SelectMinAndPositive(ref milliToConsume, auto.outputWholeMilli - auto.outputMilli);
                    }
                    if(target.TryGetItemComponent<ItemChestComponent>(out var chest) && chest.isWaiting)
                    {
                        _SelectMinAndPositive(ref milliToConsume, chest.openWaitLeftMilli);
                        state.type = ItemSkillStateType.UpdateChest;
                    }
                    if(milliToConsume < 0)
                    {
                        milliToConsume = 0;
                    }
                    else if(milliToConsume > sandGlassSeconds * 1000)
                    {
                        milliToConsume = sandGlassSeconds * 1000;
                    }
                    state.useCount = milliToConsume;
                    return state.useCount > 0;
                }
            }
            return false;
        }

        private void _SelectMinAndPositive(ref long a, long b)
        {
            if(b > 0 && (b < a || a <= 0))
            {
                a = b;
            }
        }

        public bool StackToTarget(Item target)
        {
            bool used = false;
            bool deadAfterUse = false;
            switch (type)
            {
                case SkillType.Degrade:
                case SkillType.Upgrade:
                    {
                        if (!target.TryGetItemComponent(out ItemSkillComponent skill_tar) || skill_tar == null || !skill_tar.CanStack())
                        {
                            DebugEx.FormatError("ItemSkillComponent::StackToTarget ----> bad target {0} {1}", target.tid, target.id);
                            return false;
                        }
                        if (skill_tar.stackCount >= skill_tar.MaxStackNum())
                        {
                            DebugEx.FormatInfo("ItemSkillComponent::StackToTarget ----> already max stack {0} {1}", skill_tar.stackCount, skill_tar.MaxStackNum());
                            return false;
                        }
                        mStackNum = skill_tar.StackBy(mStackNum);
                        if (mStackNum < 1)
                        {
                            // 耗尽
                            item.parent.DisposeItem(item);
                            deadAfterUse = true;
                        }
                        used = true;
                    }
                    break;
            }
            if (used && deadAfterUse)
            {
                item.parent.DisposeItem(item);
            }
            return used;
        }

        public bool Use()
        {
            DebugEx.FormatInfo("ItemSkillComponent::Use ----> {0},{1}", item.id, item.tid);
            bool used = false;
            bool disposeOnUse = false;
            switch(type)
            {
                case SkillType.TimeSkip:
                {
                    if(item.world == null)
                    {
                        DebugEx.FormatWarning("ItemSkillComponent::Use ----> {0},{1} fail not in world", item.id, item.tid);
                        return false;
                    }
                    item.world.SetSkipSeconds(param[0]);
                    item.world.activeBoard?.TriggerUseTimeSkipper(item);
                    used = true;
                    disposeOnUse = true;
                }
                break;
                case SkillType.Tesla:
                {
                    if (!teslaActive)
                    {
                        mBuffCounter = 1;
                        item.world.activeBoard?.TriggerUseTimeScaleSource(item);
                        used = true;
                    }
                }
                break;
            }

            if(used && disposeOnUse)
            {
                item.parent.DisposeItem(item);
            }
            return used;
        }

        public bool UseForTarget(Item target)
        {
            bool used = false;
            bool deadAfterUse = true;
            switch(type)
            {
                case SkillType.NoCd:
                if(target.TryGetItemComponent<ItemClickSourceComponent>(out var com) && !com.isNoCD)
                {
                    com.StartNoCD(param[0]);
                    used = true;
                }
                break;
                case SkillType.InstantOutput:
                if(target.TryGetItemComponent<ItemClickSourceComponent>(out var clickSource))
                {
                    clickSource.StartInstantOutput(param[0]);
                    used = true;
                }
                break;
                case SkillType.Degrade:
                {
                    var catConfig = Env.Instance.GetCategoryByItem(target.tid);
                    int idx = catConfig.Progress.IndexOf(target.tid);
                    if(idx > 0)
                    {
                        used = true;
                        deadAfterUse = false;

                        int newItemId = catConfig.Progress[idx - 1];
                        DebugEx.FormatInfo("ItemSkillComponent::UseForTarget ----> degrade item {0} for {1} x 2", target.tid, newItemId);
                        // item.parent.DisposeItem(item);
                        --mStackNum;
                        if (mStackNum <= 0)
                        {
                            // 立即销毁 给目标产物留出空间
                            item.parent.DisposeItem(item);
                        }
                        var parent = target.parent;
                        var coord = target.coord;
                        item.parent.DisposeItem(target);
                        var sameItem = parent.SpawnItemMustWithReason(newItemId, ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Degrade), coord.x, coord.y, false, false);
                        var otherItem = parent.SpawnItemMustWithReason(newItemId, ItemSpawnContext.CreateWithSource(sameItem, ItemSpawnContext.SpawnType.Degrade), coord.x, coord.y, false, false);
                    }
                }
                break;
                case SkillType.Lightbulb:
                {
                    if (target.TryGetItemComponent<ItemClickSourceComponent>(out var click) && click.config.IsBoostItem)
                    {
                        click.StartBoostItem(param2[0]);
                        used = true;
                    }
                }
                break;
                case SkillType.Upgrade:
                {
                    var catConfig = Env.Instance.GetCategoryByItem(target.tid);
                    int idx = catConfig.Progress.IndexOf(target.tid);
                    if(idx < catConfig.Progress.Count - 1)
                    {
                        int newItemId = catConfig.Progress[idx + 1];
                        DebugEx.FormatInfo("ItemSkillComponent::UseForTarget ----> upgrade item {0} to {1}", target.tid, newItemId);
                        // item.parent.DisposeItem(item);
                        --mStackNum;
                        if (mStackNum > 0)
                        {
                            // 堆叠未耗尽 不销毁
                            deadAfterUse = false;
                        }
                        var parent = target.parent;
                        var coord = target.coord;
                        item.parent.DisposeItem(target);
                        var cxt = ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Upgrade);
                        cxt.from1 = target;
                        var result = parent.SpawnItemMustWithReason(newItemId, cxt, coord.x, coord.y, false, false);
                        used = true;
                    }
                }
                break;
                case SkillType.SkillDustClear:
                {
                    target.parent?.UnfrozenItem(target);
                    used = true;
                }
                break;
                case SkillType.SandGlass:
                {
                    if(CanUseForTarget(target, out var state))
                    {
                        mBuffCounter = (int)(((long)mBuffCounter * 1000L - state.useCount) / 1000L);
                        if(mBuffCounter < 0)
                        {
                            mBuffCounter = 0;
                        }
                        switch(state.type)
                        {
                            case ItemSkillStateType.Normal:
                                {
                                    int skipTime = int.MaxValue / 2;            //保证不溢出
                                    if(state.useCount < skipTime)
                                    {
                                        skipTime = (int)state.useCount;
                                    }
                                    target.Update(skipTime);
                                }
                                break;
                            case ItemSkillStateType.UpdateChest:
                                {
                                    int skipTime = int.MaxValue / 2;            //保证不溢出
                                    if(state.useCount < skipTime)
                                    {
                                        skipTime = (int)state.useCount;
                                    }
                                    target.Update(skipTime);
                                    target.parent.UpdateChest(skipTime);
                                }
                                break;
                        }
                        _CheckDead();       
                        used = true;
                        deadAfterUse = false; //可能不死，所以外部不处理死亡
                    }
                }
                break;
            }

            if(used && deadAfterUse)
            {
                item.parent.DisposeItem(item);
            }
            return used;
        }

        protected override void OnUpdate(int dt)
        {
            base.OnUpdate(dt);
            if (item.parent != null)
            {
                switch (type)
                {
                    case SkillType.Tesla:
                        {
                            if (teslaActive)
                            {
                                mBuffCounter += dt;
                            }
                        }
                        break;
                }
                _CheckDead();
            }
        }

        private void _SetupStack()
        {
            mStackNum = 1;
        }

        private void _CheckDead()
        {
            bool shouldDead = false;
            switch (type)
            {
                case SkillType.Tesla:
                    {
                        shouldDead = mBuffCounter >= teslaTotalLife;
                    }
                    break;
                case SkillType.SandGlass:
                    {
                        shouldDead = mBuffCounter <= 0;
                    }
                    break;
            }
            if(shouldDead)
            {
                item.parent.DisposeItem(item);
            }
        }
    }
}