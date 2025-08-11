/*
 * @Author: tang.yan
 * @Description: 集卡活动处理器 用于控制活动的创建及弹脸等相关逻辑 作为中间桥梁 解藕集卡活动逻辑(CardActivity)和集卡核心逻辑(CardMan)
 * @Date: 2024-01-16 10:01:07
 */
using fat.rawdata;
using fat.gamekitdata;
using EL;

namespace FAT {
    public class CardActivityHandler : ActivityGroup {

        //在检测到目前处理卡册活动可以开启的时间内时 尝试添加卡册活动数据
        public override (bool, string) TryAdd(Activity activity_, (int, int) id_, EventType type_, ActivityInstance data_, in Option option_) {
            //若活动正在开启中 说明已经初始化过数据了 直接返回true
            if (activity_.IsActive(id_)) return (true, "active");
            //活动数据非法或者已经过期(开过一次)则直接返回false
            if (activity_.IsInvalid(id_, out var rsi) || activity_.IsExpire(id_)) return (false, rsi ?? "expire");
            //如果没有配置 返回false
            var (r, rs) = ActivityLite.ReadyToCreate(id_, option_, out var lite);
            if (!r) return activity_.Invalid(id_, $"{id_} not available reason:{rs}");
            //创建活动数据
            (r, rs) = _TryCreateCardActivity(lite, type_, out var cardActivity);
            if (r)
            {
                if (!cardActivity.Valid) return activity_.Invalid(id_, $"failed to create instance for {id_}");
                cardActivity.LoadData(data_);
                option_.Apply(cardActivity);
                activity_.AddActive(id_, cardActivity, new_: data_ == null);
                _TryCreateCardData(cardActivity.Id);
                return (true, null);
            }
            return (false, rs);
        }

        private (bool, string) _TryCreateCardActivity(LiteInfo lite_, EventType type_, out CardActivity cardActivity)
        {
            cardActivity = null;
            var cardMan = Game.Manager.cardMan;
            if (!cardMan.IsUnlock)
                return (false, "not unlock");
            var cardRoundConfig = Game.Manager.configMan.GetEventCardRoundConfig(lite_.param);
            if (cardRoundConfig == null || cardRoundConfig.IncludeAlbumId.Count < 1)
                return (false, "round config failed");
            //创建活动前 先确认一下有没有对应活动id的卡册轮次数据 有的话获取其中的index
            var index = Game.Manager.cardMan.TryGetCurOpenAlbumIndex(lite_.id);
            //使用IncludeAlbumId中对应index的卡册作为活动展示相关的配置
            var cardAlbumConfig = Game.Manager.configMan.GetEventCardAlbumConfig(cardRoundConfig.IncludeAlbumId[index]);
            if (cardAlbumConfig == null)
                return (false, "album config failed");
            var (r, rs) = ActivityLite.TryCreate(lite_, type_, out var lite);
            if (!r) return (r, rs);
            cardActivity = new CardActivity();
            cardActivity.Setup(lite, cardAlbumConfig);
            return (true, null);
        }

        private void _TryCreateCardData(int actId)
        {
            var cardMan = Game.Manager.cardMan;
            //通知man 当前开启的活动id 并初始化数据
            cardMan.CurCardActId = actId;
            cardMan.InitCurCardActData();
            //尝试预加载本次卡册活动的相关动态资源
            cardMan.TryPreLoadAsset();
        }

        public void RefreshCardActivity(CardActivity cardActivity)
        {
            if (cardActivity == null)
                return;
            cardActivity.RefreshAlbumConfig(Game.Manager.cardMan.GetCardAlbumConfig());
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().Dispatch();
        }
        
        public override void End(Activity activity_, ActivityLike acti_, bool expire_)
        {
            //通知cardMan  acti_ 结束了  清空活动id
            var cardMan = Game.Manager.cardMan;
            if (cardMan.CurCardActId == acti_.Id)
            {
                cardMan.TryClearExpireItem(acti_.Id);
                cardMan.TrackCardAlbumEnd(acti_.Id);
                cardMan.CurCardActId = 0;
            }
        }
    }
}