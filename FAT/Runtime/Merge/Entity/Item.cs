/**
 * @Author: handong.liu
 * @Date: 2021-02-19 14:31:45
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public enum ItemUseState
    {
        Success,
        UnknownError,
        CoolingDown,
        NotEnoughSpace,
        NotEnoughSpaceForDying,
        NotEnoughEnergy,
        NotEnoughCost,
    }
    public class Item
    {
        public static bool SerializeDelta(MergeItem newItem, MergeItem oldItem)
        {
            if(oldItem == null)
            {
                return true;
            }
            bool compnentChanged = ItemComponentTable.CalculateSerializeDelta(newItem, oldItem);
            if(compnentChanged)
            {
                return true;
            }
            return newItem.State != oldItem.State ||
                newItem.Tid != oldItem.Tid ||
                newItem.X != oldItem.X ||
                newItem.Y != oldItem.Y ||
                newItem.Com != oldItem.Com;
        }
        public ObjMergeItem config => mConfig;
        public Board parent => mParent;
        public bool isLocked => mIsLocked;
        public IMergeGrid grid => mGrid;
        public bool isFrozen => mIsFrozen;
        public bool isDead => mIsDead;
        public bool isActive => !isFrozen && !isLocked && !isUnderCloud;
        public bool isMovable => isActive && isDraggable;    //是否可移动:没有蜘蛛网和纸箱且配置上允许 
        public bool isDraggable => !(mConfig?.IsNondrag ?? true);   //是否可被拖拽移动
        public bool isUnderCloud => parent != null && parent.HasCloud(mCoord.x, mCoord.y);
        // TODO: 4表示等级锁 避免到处写数字
        public bool isReachBoardLevel => mStateConfType != 4 || Env.Instance.GetBoardLevel() >= mStateConfParam;
        public int unLockLevel => mStateConfType == 4 ? mStateConfParam : 0;
        public int stateConfParam => mStateConfParam;
        public Vector2Int coord => mCoord;
        public MergeWorld world => mWorld;
        public int id => mId;
        public int tid => mTId;
        public int timeScale => mTimeScale;
        private int mId;
        private int mTId;
        private int mTimeScale = 1;
        private bool mIsDead = false;
        private bool mIsLocked = false;
        private bool mIsFrozen = false;
        private int mStateConfType = 0; // 初始状态
        private int mStateConfParam = 0; // 初始状态参数
        private Board mParent;
        private Vector2Int mCoord;
        private MergeGrid mGrid;
        private ObjMergeItem mConfig;
        private Dictionary<ItemComponentType, ItemComponentBase> mComponents = new Dictionary<ItemComponentType, ItemComponentBase>();
        private bool mIsIniting = false;
        private List<SpeedEffect> mEffects = new List<SpeedEffect>();
        private MergeWorld mWorld;
        private List<System.Action> mExecuteAfterUpdate = new List<System.Action>();

        public Item(int id, MergeWorld world)
        {
            mWorld = world;
            mId = id;
        }

        public void BeginDispose()
        {
            mIsDead = true;
        }

        public void EndDispose()
        {
            _ClearAllComponent();
        }

        public void SetParent(Board pa, MergeGrid grid)
        {
            mParent = pa;
            mGrid = grid;
            if(mParent == null)
            {
                mGrid = null;
            }
            foreach(var c in mComponents)
            {
                c.Value.OnPositionChange();
            }
            if(isActive)
            {
                _RefreshEffectList();
            }
        }

        public void SetState(bool locked, bool frozen)
        {
            mIsLocked = locked;
            mIsFrozen = frozen;
            if(isActive)
            {
                _RefreshEffectList();
            }
            else
            {
                mEffects.Clear();
            }
        }

        // 记录棋子初始化时的状态参数
        public void SetStateConfParam(int type, int param)
        {
            mStateConfType = type;
            mStateConfParam = param;
        }

        public void GetStateConfParam(out int type, out int param)
        {
            type = mStateConfType;
            param = mStateConfParam;
        }

        private int _SerializeStateConfParam()
        {
            return (mStateConfType << 16) | (mStateConfParam);
        }

        private void _DeserializeStateConfParam(int val)
        {
            mStateConfType = val >> 16;
            mStateConfParam = val & 0xffff;
        }

        private static int _StateToSerializedState(bool isLocked, bool isFrozen)
        {
            if(isFrozen)
            {
                if(isLocked)
                {
                    return 2;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                if(isLocked)
                {
                    return 3;
                }
                else
                {
                    return 0;
                }
            }
        }

        //之所以不用位，是为了前向兼容，以前的版本没有单独的locked，所以locked+frozen是2
        private static void _SerailizedStateToState(int state, out bool isLocked, out bool isFrozen)
        {
            switch(state)
            {
                case 0:
                isLocked = false;
                isFrozen = false;
                break;
                case 1:
                isLocked = false;
                isFrozen = true;
                break;
                case 2:
                isLocked = true;
                isFrozen = true;
                break;
                case 3:
                isLocked = true;
                isFrozen = false;
                break;
                default:
                isLocked = true;
                isFrozen = true;
                break;
            }
        }

        public void OnStart()
        {
            foreach(var c in mComponents.Values)
            {
                c.OnStart();
            }
        }

        public void Serialize(MergeItem data)           //return whether changed
        {
            data.Id = mId;
            data.Tid = mTId;
            data.State = _StateToSerializedState(mIsLocked, mIsFrozen);
            data.StateConf = _SerializeStateConfParam();
            data.X = mParent==null?-1:mCoord.x;
            data.Y = mParent==null?-1:mCoord.y;
            data.Com = 0;
            foreach(var c in mComponents.Keys)
            {
                data.Com |= (uint)1<<(int)c;
            }
            foreach(var c in mComponents.Values)
            {
                c.Serialize(data);
            }
        }

        // 仅解析棋子的状态信息 / 不处理组件
        public void DeserializeStateOnly(MergeItem data)
        {
            _SerailizedStateToState(data.State, out mIsLocked, out mIsFrozen);
            _DeserializeStateConfParam(data.StateConf);
            mIsDead = false;
            mCoord.Set(data.X, data.Y);
        }

        public void Deserialize(MergeItem data)
        {
            mId = data.Id;
            mTId = data.Tid;
            mConfig = Env.Instance.GetItemMergeConfig(mTId);
            _SerailizedStateToState(data.State, out mIsLocked, out mIsFrozen);
            _DeserializeStateConfParam(data.StateConf);
            mIsDead = false;
            mCoord.Set(data.X, data.Y);
            int type = 0;
            uint mask = data.Com;
            int comTypeCount = (int)ItemComponentType.Count;
            var comConfig = Env.Instance.GetItemComConfig(mTId);
            while(mask > 0 && type < comTypeCount)
            {
                if((mask & 1) > 0)
                {
                    var ret = ItemComponentTable.ValidateAndAddComponent(this, comConfig, (ItemComponentType)type);
                    if(ret == null)
                    {
                        DebugEx.FormatWarning("Item::Deserialize ----> component {0} no longer exists!", (ItemComponentType)type);
                    }
                }
                type++;
                mask = mask >> 1;
            }
            _FillNeverDestroyNormalItemComponent(comConfig);
            foreach(var c in mComponents.Values)
            {
                c.Deserialize(data);
            }
        }

        public void FillAllEffects<T>(List<T> container)
        {
            foreach(var e in mEffects)
            {
                if(e is T t)
                {
                    container.Add(t);
                }
            }
        }

        // public void WalkEffects(System.Action<SpeedEffect> cb)
        // {
        //     foreach(var e in mEffects)
        //     {
        //         cb?.Invoke(e);
        //     }
        // }

        public void SetEffectDirty()
        {
            if(isActive)
            {
                _RefreshEffectList();
            }
        }

        public override string ToString()
        {
            return string.Format("{0}({1})", tid, id);
        }

        public void ProcessPostMerge(Item src, Item dst)
        {
            foreach(var c in mComponents.Values)
            {
                c.TriggerPostMerge(src, dst);
            }
        }

        public void ProcessPostSpawn(ItemSpawnContext cxt)
        {
            foreach(var c in mComponents.Values)
            {
                c.TriggerPostSpawn(cxt);
            }
        }

        public void InitWithBubbleItem(int itemConfId, ItemBubbleType type, long lifeTime = 0, int lifeCounter = 0)
        {
            if(mTId == itemConfId)
            {
                return;
            }
            mIsIniting = true;
            mTId = itemConfId;
            mConfig = Env.Instance.GetItemMergeConfig(itemConfId);
            _ClearAllComponent();

            var bubbleComp = _AddItemComponent<ItemBubbleComponent>();
            bubbleComp?.InitItemBubbleType(type, lifeTime, lifeCounter);
            //冰冻棋子额外具有merge组件
            if (type == ItemBubbleType.Frozen)
            {
                var comConfig = Env.Instance.GetItemComConfig(itemConfId);
                ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.Merge);
            }
            mIsIniting = false;
            OnStart();
        }

        public void AppendWithActivityComponent()
        {
            var com = GetItemComponent<ItemActivityComponent>();
            if (com == null)
            {
                com = _AddItemComponent<ItemActivityComponent>();
            }
        }

        public void SetNoCoinUnfrozen()
        {
            var com = GetItemComponent<ItemFrozenOverrideComponent>();
            if(com == null)
            {
                com = _AddItemComponent<ItemFrozenOverrideComponent>();
            }
        }

        public void InitWithNormalItem(int id)
        {
            if(mTId == id)
            {
                return;
            }
            mIsIniting = true;
            mTId = id;
            mConfig = Env.Instance.GetItemMergeConfig(id);
            _ClearAllComponent();

            var env = Env.Instance;
            var comConfig = env.GetItemComConfig(id);
            
            _FillNeverDestroyNormalItemComponent(comConfig);
            mIsIniting = false;
            OnStart();
        }

        //dummy 物品只提供一个tid，没有任何component
        public void InitWithDummyItem(int id)
        {
            mTId = id;
        }

        public void SetPosition(int col, int row, MergeGrid g)
        {
            mCoord = new Vector2Int(col, row);
            mGrid = g;
            foreach(var c in mComponents)
            {
                c.Value.OnPositionChange();
            }
            if(isActive)
            {
                _RefreshEffectList();
            }
        }

        public bool HasComponent(ItemComponentType type, bool includeDisable = false)
        {
            return GetItemComponent(type) != null;
        }

        public bool TryGetItemComponent<T>(out T ret, bool includeDisable = false) where T: ItemComponentBase
        {
            var e = ItemComponentTable.GetEnumByType(typeof(T));
            var item = GetItemComponent(e, includeDisable);
            ret = item as T;
            return ret != null;
        }

        public ItemComponentBase GetItemComponent(ItemComponentType type, bool includeDisable = false)
        {
            ItemComponentBase ret = mComponents.GetDefault(type, null);
            if(ret != null && (!includeDisable && !ret.enabled))
            {
                ret = null;
            }
            return ret;
        }

        public void WalkAllComponents<T>(System.Action<T> walker) where T:class
        {
            foreach(var c in mComponents.Values)
            {
                var t = c as T;
                if(t != null)
                {
                    walker?.Invoke(t);
                }
            }
        }

        public T GetItemComponent<T>(bool includeDisable = false) where T: ItemComponentBase
        {
            TryGetItemComponent<T>(out var ret, includeDisable);
            return ret;
        }

        public T RemoveItemComponent<T>() where T: ItemComponentBase
        {
            var e = ItemComponentTable.GetEnumByType(typeof(T));
            var ret = RemoveItemComponent(e);
            return ret as T;
        }

        public ItemComponentBase RemoveItemComponent(ItemComponentType type)
        {
            var ret = mComponents.GetDefault(type, null);
            ret.Attach(null);
            mComponents.Remove(type);
            OnComponentChanged(ret);
            return ret;
        }

        public void UpdateInactive(int milli)
        {
            foreach(var c in mComponents.Values)
            {
                int m = c.CalculateUpdateMilli(milli);
                if(m < milli)
                {
                    DebugEx.FormatInfo("Merge::Item::UpdateInactive ----> {0} component {1} cause update time from {2} to {3}", this, c.GetType().Name, milli, m);
                    milli = m;
                }
            }

            foreach(var c in mComponents.Values)
            {
                c.UpdateInactive(milli);
            }
        }

        public void ExecuteAfterUpdate(System.Action act)
        {
            mExecuteAfterUpdate.Add(act);
        }

        public void Update(int milli)
        {
            mTimeScale = parent.CalcTimeScale(this);
            milli *= mTimeScale;

            foreach(var c in mComponents.Values)
            {
                int m = c.CalculateUpdateMilli(milli);
                if(m < milli)
                {
                    DebugEx.FormatInfo("Merge::Item::Update ----> {0} component {1} cause update time from {2} to {3}", this, c.GetType().Name, milli, m);
                    milli = m;
                }
            }

            foreach(var c in mComponents.Values)
            {
                c.Update(milli);
            }

            if(mExecuteAfterUpdate.Count > 0)
            {
                for(var i = 0; i < mExecuteAfterUpdate.Count; i++)
                {
                    mExecuteAfterUpdate[i]?.Invoke();
                }
                mExecuteAfterUpdate.Clear();
            }
        }

        public ItemComponentBase AddComponent(ItemComponentType type)
        {
            var com = GetItemComponent(type);
            if(com != null)
            {
                return com;
            }
            else
            {
                return _AddItemComponent(type);
            }
        }

        public void OnComponentChanged(ItemComponentBase com)
        {
            if(!mIsIniting)
            {
                if(parent != null)
                {
                    parent.TriggerItemComponentChange(this);
                }
            }
        }

        //本棋子解锁后 是否可以进一步尝试解锁周围棋子
        public bool CanUnlockAround()
        {
            //没有蜘蛛网 且 不是触发式棋子
            return !isFrozen && !HasComponent(ItemComponentType.TrigAutoSource);
        }

        private void _FillNeverDestroyNormalItemComponent(ItemComConfig comConfig)
        {
            if(HasComponent(ItemComponentType.Bubble))          //he has bubble
            {
                return;
            }
            var nextId = ItemUtility.GetNextItem(tid);
            if(nextId > 0 || (comConfig.skillConfig != null && comConfig.skillConfig.Type == SkillType.SandGlass))
            {
                ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.Merge);
            }
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.Chest);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.ClickSouce);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.EatSource);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.Bonus);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.TapBonus);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.Dying);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.AutoSouce);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.TimeSkipper);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.Box);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.Skill);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.FeatureEntry);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.Eat);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.ToolSouce);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.OrderBox);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.JumpCD);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.SpecialBox);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.ChoiceBox);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.MixSource);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.TrigAutoSource);
            ItemComponentTable.ValidateAndAddComponent(this, comConfig, ItemComponentType.ActiveSource);
        }

        private void _RefreshEffectList()
        {
            mEffects.Clear();
            if(parent != null)
            {
                parent.WalkEffects((e)=>{
                    bool receive = false;
                    if(e.IsGridAffected(coord))
                    {
                        foreach(var c in mComponents.Values)
                        {
                            var receiver = c as IEffectReceiver;
                            if(receiver != null && receiver.WillReceiveEffect(e))
                            {
                                receive = true;
                                break;
                            }
                        }
                    }
                    if(receive)
                    {
                        mEffects.Add(e);
                    }
                });
            }
        }

        private void _ClearAllComponent()
        {
            foreach(var entry in mComponents)
            {
                entry.Value.Attach(null);
                ItemComponentTable.Free(entry.Key, entry.Value);
            }
            mComponents.Clear();
        }

        private T _AddItemComponent<T>() where T: ItemComponentBase, new()
        {
            var typeEnum = ItemComponentTable.GetEnumByType(typeof(T));
            var com = GetItemComponent(typeEnum, true);
            if(com == null)
            {
                com = ItemComponentTable.CreateComponent<T>();
                com.Attach(this);
                mComponents.Add(typeEnum, com);
                OnComponentChanged(com);
                mWorld.PostProcessItemComponent(typeEnum, com);
            }
            return com as T;
        }

        private ItemComponentBase _AddItemComponent(ItemComponentType type)
        {
            var com = GetItemComponent(type, true);
            if(com == null)
            {
                com = ItemComponentTable.CreateComponentByType(type);
                if(com != null)
                {
                    com.Attach(this);
                    mComponents.Add(type, com);
                    OnComponentChanged(com);
                    mWorld.PostProcessItemComponent(type, com);
                }
            }
            return com;
        }
    }
}