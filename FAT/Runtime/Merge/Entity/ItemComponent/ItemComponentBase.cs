/**
 * @Author: handong.liu
 * @Date: 2021-02-20 11:59:12
 */
using fat.gamekitdata;

namespace FAT.Merge
{

    public class ItemComponentBase
    {
        public bool enabled {
            get 
            {
                return mRuntimeEnable; 
            } 
            set 
            { 
                if(mEnable != value) 
                {
                    mEnable = value;
                    RefreshEnableState();
                }
            }
        }
        public Item item => mItem;
        public bool isGridNotMatch => item.grid != null && (item.config != null && item.config.MergeGrid.Count > 0 && item.grid.gridTid == 0);
        protected bool isNew => mIsNewItem;
        private Item mItem;
        private bool mEnable = true;
        private bool mRuntimeEnable = true;
        private bool mIsNewItem = true;
        public void Attach(Item item)
        {
            if(item != mItem)
            {
                if(mItem != null)
                {
                    OnPreDetach();
                    mItem = null;
                }
                mItem = item;
                if(mItem != null)
                {
                    OnPostAttach();
                }
                RefreshEnableState();
            }
        }

        public void Update(int dt)
        {
            if(mEnable)
            {
                OnUpdate(dt);
            }
        }

        public virtual int CalculateUpdateMilli(int maxMilli)
        {
            return maxMilli;
        }

        public void UpdateInactive(int dt)
        {
            if(mEnable)
            {
                OnUpdateInactive(dt);
            }
        }

        public void Serialize(MergeItem itemData)
        {
            OnSerialize(itemData);
        }

        public void Deserialize(MergeItem itemData)
        {
            mIsNewItem = false;
            OnDeserialize(itemData);
        }

        public virtual void OnSerialize(MergeItem itemData)
        {

        }

        public virtual void OnDeserialize(MergeItem itemData)
        {
            
        }

        public virtual void OnStart()
        {

        }

        public void TriggerPostMerge(Item src, Item dst)
        {
            OnPostMerge(src, dst);
        }

        public void TriggerPostSpawn(ItemSpawnContext cxt)
        {
            OnPostSpawn(cxt);
        }

        protected virtual void OnPostAttach()
        {

        }

        protected virtual void OnPreDetach()
        {

        }

        public virtual void OnPositionChange()
        {

        }

        protected virtual void OnUpdate(int dt)
        {

        }

        protected virtual void OnUpdateInactive(int dt)
        {

        }

        protected virtual void OnPostMerge(Item src, Item dst)
        {

        }

        protected virtual void OnPostSpawn(ItemSpawnContext sp)
        {
            
        }

        protected virtual bool RefreshEnableStateImp(bool enableSetting)
        {
            return enableSetting;
        }

        public void RefreshEnableState()
        {
            bool e = false;
            if(item != null)
            {
                e = RefreshEnableStateImp(mEnable); 
            }
            if(e != mRuntimeEnable)
            {
                mRuntimeEnable = e;
                item?.OnComponentChanged(this);
            }
        }
    }
}