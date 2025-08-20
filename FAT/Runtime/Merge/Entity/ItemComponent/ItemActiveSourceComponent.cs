/*
 * @Author: qun.chao
 * @Date: 2025-04-07 17:50:15
 */
using fat.gamekitdata;
using fat.rawdata;

namespace FAT.Merge
{
    public class ItemActiveSourceComponent : ItemComponentBase
    {
        public ComMergeActiveSource Config => _config;
        public bool CanOutput => _itemCount > 0;
        public bool WillDead => _itemCount <= 1;
        public int DropCount => _dropCount;
        private ComMergeActiveSource _config;
        private int _itemCount;
        private int _dropCount;

        public void Consume()
        {
            _itemCount--;
        }

        public static bool Validate(ItemComConfig config)
        {
            return config?.activeSourceConfig != null;
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComActiveSource = new ComActiveSource
            {
                ItemCount = _itemCount
            };
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if (itemData.ComActiveSource != null)
            {
                _itemCount = itemData.ComActiveSource.ItemCount;
            }
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            _config = Env.Instance.GetItemComConfig(item.tid).activeSourceConfig;
            _itemCount = _config.Limit;
            _dropCount = _config.Drop;
        }
    }
}