/*
 * @Author: qun.chao
 * @Date: 2024-01-04 20:34:48
 */
using System.Collections.Generic;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    /*
    订单礼盒
    doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/ggrpxbp79nsyvxul
    */
    public class OrderBox
    {
        public bool hasActiveOrderBox => mOrderBoxDurationMilli > 0;
        public int orderBoxDurationMilli => mOrderBoxDurationMilli;
        public int orderBoxLifeCountMilli => mOrderBoxLifeCountMilli;

        private int mOrderBoxLifeCountMilli;
        private int mActivatedOrderBoxId;

        // key: random订单id
        // value: output配置
        private Dictionary<int, OrderBoxDetail> mOrderBoxDetailMap = new();
        private int mOrderBoxDurationMilli;
        private MergeWorld mWorld;

        public OrderBox(MergeWorld world)
        {
            mWorld = world;
        }

        public void Deserialize(fat.gamekitdata.Merge data)
        {
            if (data.OrderBox != null)
            {
                var tid = data.OrderBox.OrderBoxItemTid;
                if (tid > 0 && TryActivateOrderBox(tid, true))
                {
                    mOrderBoxLifeCountMilli = data.OrderBox.LifeCounter;
                }
            }
        }

        public void Serialize(fat.gamekitdata.Merge data)
        {
            data.OrderBox ??= new fat.gamekitdata.OrderBox();
            data.OrderBox.LifeCounter = mOrderBoxLifeCountMilli;
            data.OrderBox.OrderBoxItemTid = mActivatedOrderBoxId;
        }

        public void Update(int milli)
        {
            if (mActivatedOrderBoxId <= 0)
                return;
            mOrderBoxLifeCountMilli += milli;
            if (mOrderBoxLifeCountMilli >= mOrderBoxDurationMilli)
            {
                _RemoveCurrentOrderBox();
                _TryActivateNextOrderBox();
            }
        }

        public OrderBoxDetail GetOrderBoxDetailByRandomerId(int id)
        {
            mOrderBoxDetailMap.TryGetValue(id, out var cfg);
            return cfg;
        }

        public bool TryActivateOrderBox(int itemTid, bool isDeserialize = false)
        {
            var cfg = Env.Instance.GetItemComConfig(itemTid).orderBoxConfig;
            if (cfg == null)
                return false;
            _Reset();
            mActivatedOrderBoxId = itemTid;
            mOrderBoxDurationMilli = cfg.Time;
            _RefreshOrderBoxDetail(itemTid);
            if (!isDeserialize)
            {
                DataTracker.board_active.Track(itemTid);
            }
            return true;
        }

        private void _RefreshOrderBoxDetail(int tid)
        {
            var cfg = Env.Instance.GetItemComConfig(tid).orderBoxConfig;
            if (cfg != null)
            {
                foreach (var bid in cfg.BoxDetailId)
                {
                    var detail = Env.Instance.GetOrderBoxDetailConfig(bid);
                    mOrderBoxDetailMap[detail.RandomerId] = detail;
                }
            }
        }

        private void _Reset()
        {
            mActivatedOrderBoxId = 0;
            mOrderBoxLifeCountMilli = 0;
            mOrderBoxDurationMilli = 0;
            mOrderBoxDetailMap.Clear();
        }

        private void _RemoveCurrentOrderBox()
        {
            DebugEx.Info($"OrderBox::_RemoveCurrentOrderBox expired {mOrderBoxDurationMilli}@{mActivatedOrderBoxId}");
            _Reset();
            MessageCenter.Get<MSG.GAME_ORDER_ORDERBOX_END>().Dispatch();
        }

        private bool _TryActivateNextOrderBox()
        {
            _Reset();
            var item = mWorld.activeBoard.FindAnyNormalItemByComponent<ItemOrderBoxComponent>();
            if (item != null && mWorld.activeBoard.UseOrderBox(item))
            {
                DebugEx.Info($"OrderBox::_TryActivateNextOrderBox activated {mOrderBoxDurationMilli}@{mActivatedOrderBoxId}");
                return true;
            }
            return false;
        }
    }
}