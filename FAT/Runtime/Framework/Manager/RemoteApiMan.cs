/*
 * @Author: qun.chao
 * @Date: 2024-11-15 12:18:11
 */
using EL;
using fat.gamekitdata;

namespace FAT
{
    // https://centurygames.yuque.com/ywqzgn/ne0fhm/zy0lqdbgxpgg5ey3
    // 专门维护一些在api需求中必须记入存档的数据
    public class RemoteApiMan : IGameModule, IUserDataHolder
    {
        // 最后一个有生成意愿的生成器ID
        public int last_wish_producer { get; private set; }
        // 今天的登入次数 对标session_start 但是进游戏不设置这个数据 最小值为1
        public int today_session_count => CheckDailyValue(_lastSessionActiveTS, _lastSessionActiveCount);
        // 今天第几次调用订单API接口 首次上报时应该传1(计数更新后)
        public int today_order_api_count => CheckDailyValue(_lastOrderApiTS, _lastOrderApiCount);

        private int _lastSessionActiveTS;
        private int _lastSessionActiveCount;
        private int _lastOrderApiTS;
        private int _lastOrderApiCount;

        // 准备发送订单请求
        public void OnPreSendOrderRequest()
        {
            // 确保session记录从1开始
            if (today_session_count == 0)
            {
                OnUserSessionActive();
            }
            // 当前调用次数需要更新
            OnUseOrderApi();
        }

        public void OnWishProducerChange(int itemId)
        {
            last_wish_producer = itemId;
        }

        public void ToForeground()
        {
            OnUserSessionActive();
        }

        private void OnUseOrderApi()
        {
            UpdateDailyValue(ref _lastOrderApiTS, ref _lastOrderApiCount);
        }

        private void OnUserSessionActive()
        {
            UpdateDailyValue(ref _lastSessionActiveTS, ref _lastSessionActiveCount);
        }

        private void UpdateDailyValue(ref int last_ts, ref int count)
        {
            var day_diff = EvaluateEventTrigger.LT(last_ts);
            if (day_diff != 0)
            {
                // 上次不是今天
                last_ts = (int)Game.Instance.GetTimestampSeconds();
                count = 1;
            }
            else
            {
                ++count;
            }
        }

        private int CheckDailyValue(int last_ts, int count)
        {
            return EvaluateEventTrigger.LT(last_ts) != 0 ? 0 : count;
        }

        void IGameModule.LoadConfig() { }

        void IGameModule.Reset() { }

        void IGameModule.Startup()
        {
        }

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.RemoteApiData;
            if (data != null)
            {
                last_wish_producer = data.LastWishProducer;
                _lastSessionActiveTS = data.SessionCountTS;
                _lastSessionActiveCount = data.SessionCountNum;
                _lastOrderApiTS = data.OrderApiCountTS;
                _lastOrderApiCount = data.OrderApiCountNum;
            }
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var data = new RemoteApiData()
            {
                LastWishProducer = last_wish_producer,
                SessionCountTS = _lastSessionActiveTS,
                SessionCountNum = _lastSessionActiveCount,
                OrderApiCountTS = _lastOrderApiTS,
                OrderApiCountNum = _lastOrderApiCount,
            };
            archive.ClientData.PlayerGameData.RemoteApiData = data;
        }
    }
}