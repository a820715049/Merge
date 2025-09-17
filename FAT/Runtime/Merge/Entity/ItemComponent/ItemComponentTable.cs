/**
 * @Author: handong.liu
 * @Date: 2021-02-20 12:33:48
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;

namespace FAT.Merge
{
    public interface IItemComponentPool
    {
        void Free(ItemComponentBase com);
        ItemComponentBase AllocByType();
    }
    public class ItemComponentPool<T> : IItemComponentPool where T: ItemComponentBase, new()
    {
        public static ItemComponentPool<T> Instance;
        private List<T> mAllCom = new List<T>();

        public ItemComponentBase AllocByType()
        {
            return Alloc();
        }

        public T Alloc()
        {
            var newCom = new T();
            mAllCom.Add(newCom);
            DebugEx.FormatTrace("ItemComponentPool<{0}> add one: {1}", typeof(T).FullName, mAllCom.Count);
            return newCom;
        }
        public void Free(ItemComponentBase com)
        {
            mAllCom.Remove(com as T);
            DebugEx.FormatTrace("ItemComponentPool<{0}> remove one: {1}", typeof(T).FullName, mAllCom.Count);
        }
    }

    public static class ItemComponentTable
    {
        //注意：和时间相关的量，需要保存两个：counter和starttime，当parent == null时不在棋盘上，此时服务器保存counter,当parent!= null时在棋盘上，此时服务器保存starttime, 如此可减少数据改变频率
        //return whether changed
        //remove not changed field in GameNet.MergeItem
        public delegate bool SerializeDeltaFunc(MergeItem newData, MergeItem oldData);
        public delegate bool ValidateFunc(ItemComConfig config);
        private static Dictionary<System.Type, ItemComponentType> mTypeEnumMap = new Dictionary<System.Type, ItemComponentType>();
        private static Dictionary<ItemComponentType, System.Type> mEnumTypeMap = new Dictionary<ItemComponentType, System.Type>();
        private static Dictionary<ItemComponentType, SerializeDeltaFunc> mSerializeDeltaFunc = new Dictionary<ItemComponentType, SerializeDeltaFunc>();
        private static Dictionary<ItemComponentType, ValidateFunc> mValidateFuncc = new Dictionary<ItemComponentType, ValidateFunc>();
        private static Dictionary<ItemComponentType, IItemComponentPool> mItemPools = new Dictionary<ItemComponentType, IItemComponentPool>();

        static ItemComponentTable()
        {
            _Init();
        }

        private static void _Init()
        {
            _RegisterItemComponent<ItemAutoSourceComponent>(ItemComponentType.AutoSouce, ItemAutoSourceComponent.SerializeDelta, ItemAutoSourceComponent.Validate);
            _RegisterItemComponent<ItemBonusCompoent>(ItemComponentType.Bonus, null, ItemBonusCompoent.Validate);
            _RegisterItemComponent<ItemTapBonusComponent>(ItemComponentType.TapBonus, null, ItemTapBonusComponent.Validate);
            _RegisterItemComponent<ItemBubbleComponent>(ItemComponentType.Bubble, null, null);
            _RegisterItemComponent<ItemChestComponent>(ItemComponentType.Chest, ItemChestComponent.SerializeDelta, ItemChestComponent.Validate);
            _RegisterItemComponent<ItemClickSourceComponent>(ItemComponentType.ClickSouce, ItemClickSourceComponent.SerializeDelta, ItemClickSourceComponent.Validate);
            _RegisterItemComponent<ItemEatSourceComponent>(ItemComponentType.EatSource, ItemEatSourceComponent.SerializeDelta, ItemEatSourceComponent.Validate);
            _RegisterItemComponent<ItemDyingComponent>(ItemComponentType.Dying, ItemDyingComponent.SerializeDelta, ItemDyingComponent.Validate);
            _RegisterItemComponent<ItemMergeComponent>(ItemComponentType.Merge, null, null);
            _RegisterItemComponent<ItemTimeSkipCompoent>(ItemComponentType.TimeSkipper, null, ItemTimeSkipCompoent.Validate);
            _RegisterItemComponent<ItemFrozenOverrideComponent>(ItemComponentType.FrozenOverride, null, null);
            _RegisterItemComponent<ItemBoxComponent>(ItemComponentType.Box, ItemBoxComponent.SerializeDelta, ItemBoxComponent.Validate);
            _RegisterItemComponent<ItemSkillComponent>(ItemComponentType.Skill, null, ItemSkillComponent.Validate);
            _RegisterItemComponent<ItemFeatureComponent>(ItemComponentType.FeatureEntry, null, (config) => config?.featureConfig != null);
            _RegisterItemComponent<ItemEatComponent>(ItemComponentType.Eat, null, ItemEatComponent.Validate);
            _RegisterItemComponent<ItemActivityComponent>(ItemComponentType.Activity, null, null);
            _RegisterItemComponent<ItemToolSourceComponent>(ItemComponentType.ToolSouce, null, ItemToolSourceComponent.Validate);
            _RegisterItemComponent<ItemOrderBoxComponent>(ItemComponentType.OrderBox, null, ItemOrderBoxComponent.Validate);
            _RegisterItemComponent<ItemJumpCDComponent>(ItemComponentType.JumpCD, null, ItemJumpCDComponent.Validate);
            _RegisterItemComponent<ItemSpecialBoxComponent>(ItemComponentType.SpecialBox, null, ItemSpecialBoxComponent.Validate);
            _RegisterItemComponent<ItemChoiceBoxComponent>(ItemComponentType.ChoiceBox, null, ItemChoiceBoxComponent.Validate);
            _RegisterItemComponent<ItemMixSourceComponent>(ItemComponentType.MixSource, null, ItemMixSourceComponent.Validate);
            _RegisterItemComponent<ItemTrigAutoSourceComponent>(ItemComponentType.TrigAutoSource, null, ItemTrigAutoSourceComponent.Validate);
            _RegisterItemComponent<ItemActiveSourceComponent>(ItemComponentType.ActiveSource, null, ItemActiveSourceComponent.Validate);
            _RegisterItemComponent<ItemActivityTokenComponent>(ItemComponentType.ActivityToken, null, null);
            _RegisterItemComponent<ItemTokenMultiComponent>(ItemComponentType.TokenMulti, null, ItemTokenMultiComponent.Validate);
        }

        public static ItemComponentType GetEnumByType(System.Type type)
        {
            return mTypeEnumMap.GetDefault(type, ItemComponentType.Count);
        }

        public static T CreateComponent<T>() where T: ItemComponentBase, new()
        {
            return _GetPool<T>().Alloc();
        }

        public static ItemComponentBase CreateComponentByType(ItemComponentType type)
        {
            var pool = mItemPools.GetDefault(type, null);
            if(pool == null)
            {
                DebugEx.FormatError("ItemComponentTable.CreaetComponentByType ----> no pool for type {0}", type);
                return null;
            }
            else
            {
                return pool.AllocByType();
            }
        }
        
        public static ItemComponentBase ValidateAndAddComponent(Item item, ItemComConfig comConfig, ItemComponentType type)
        {
            if(mValidateFuncc.TryGetValue(type, out var validateFunc) && validateFunc != null && !validateFunc(comConfig))
            {
                return null;
            }
            return item.AddComponent(type);
        }

        public static bool CalculateSerializeDelta(MergeItem newData, MergeItem oldData)
        {
            bool changed = false;
            var com = newData.Com;
            foreach(var entry in mSerializeDeltaFunc)
            {
                if(entry.Value != null && (com & (1 << (int)entry.Key)) > 0)
                {
                    changed = entry.Value.Invoke(newData, oldData) || changed;
                }
            }
            return changed;
        }

        //判断是否是生成器
        public static bool IsComponentItemSource(ItemComponentType type)
        {
            return type == ItemComponentType.AutoSouce ||
                type == ItemComponentType.ClickSouce ||
                type == ItemComponentType.Dying ||
                type == ItemComponentType.EatSource ||
                type == ItemComponentType.Eat ||
                type == ItemComponentType.ToolSouce;
        }

        public static void Free(ItemComponentType type, ItemComponentBase com)
        {
            if(mItemPools.TryGetValue(type, out var pool))
            {
                pool.Free(com);
            }
            else
            {
                DebugEx.FormatError("ItemComponentTable.Free ----> no such pool for type {0}", type);
            }
        }

        private static ItemComponentPool<T> _GetPool<T>() where T: ItemComponentBase, new()
        {
            return ItemComponentPool<T>.Instance;
        }

        private static void _RegisterItemComponent<T>(ItemComponentType type, SerializeDeltaFunc serializeDeltaFunc, ValidateFunc validateFunction) where T: ItemComponentBase, new()
        {
            var ret = new ItemComponentPool<T>();
            ItemComponentPool<T>.Instance = ret;
            mItemPools.Add(type, ret);
            mEnumTypeMap.Add(type, typeof(T));
            mTypeEnumMap.Add(typeof(T), type);
            mSerializeDeltaFunc.Add(type, serializeDeltaFunc);
            mValidateFuncc.Add(type, validateFunction);
        }
    }
}