/*
 * @Author: yanfuxing
 * @Date: 2025-6-12 16:13:36
 */
using EL;
using fat.conf;
using fat.rawdata;

namespace FAT
{
    public class IAPMailReshipment
    {
        public const int MailHasRewardReshipmentPopupId = 130;
        public const int MailNotRewardReshipmentPopupId = 131;

        /// <summary>
        /// 补单弹窗
        /// </summary>
        /// <param name="delivery"></param>
        public static void LateDelivery(IAPLateDelivery delivery)
        {
            if (delivery == null)
            {
                DebugEx.Info("mailReshipment info is null");
                return;
            }
            //根据from从EventType配置表中获取对应的配置信息
            var (valid, id, from, type, sub) = ActivityLite.InfoUnwrap(delivery.context.ProductName);
            fat.rawdata.EventType eventType = type;
            DebugEx.Info($"mailReshipment:eventType=>{eventType} from {delivery.from}");
            int reshipmentModuleId = -1;
            if (delivery.from == IAPFrom.ShopMan)
            {
                //约定商店固定使用模板1
                reshipmentModuleId = (int)MailReshipmentMoudleType.HasReward;
            }
            else
            {
                var eventTypeInfo = EventTypeInfoVisitor.GetOneByFilter(info => info.EventType == eventType);
                if (eventTypeInfo != null)
                {
                    reshipmentModuleId = eventTypeInfo.ReshipmentType;
                    if (eventTypeInfo.ReshipmentType == 0)
                    {
                        reshipmentModuleId = (int)MailReshipmentMoudleType.HasReward;
                    }
                    DebugEx.Info($"mailReshipment:reshipmentModuleId=>{reshipmentModuleId}");
                }
                else
                {
                    DebugEx.Info($"mailReshipment:EventTypeInfo not has this =>{eventType} packId:{delivery.context.PayStoreId}");
                    //未在EventTypeInfo这个表中配置的
                    reshipmentModuleId = (int)MailReshipmentMoudleType.HasReward;
                }
            }
            if (MailReshipmentMoudleType.HasReward == (MailReshipmentMoudleType)reshipmentModuleId)
            {
                var data = new ReshipmentData
                {
                    Delivery = delivery,
                    ReshipmentId = reshipmentModuleId
                };
                UIResAlt MailHasRewardReshipmentResAlt = new UIResAlt(UIConfig.UIMailHasRewardReshipment);
                var MailHasRewardReshipmentPopup = new MailReshipmentPopup(MailHasRewardReshipmentPopupId, MailHasRewardReshipmentResAlt);
                Game.Manager.screenPopup.TryQueue(MailHasRewardReshipmentPopup, PopupType.Login, data);
            }
            else if (MailReshipmentMoudleType.NotHasReward == (MailReshipmentMoudleType)reshipmentModuleId)
            {
                var data = new ReshipmentData
                {
                    Delivery = delivery,
                    ReshipmentId = reshipmentModuleId
                };
                UIResAlt MailNotRewardReshipmentResAlt = new UIResAlt(UIConfig.UIMailNotRewardReshipment);
                var MailNotRewardReshipmentPopup = new MailReshipmentPopup(MailNotRewardReshipmentPopupId, MailNotRewardReshipmentResAlt);
                Game.Manager.screenPopup.TryQueue(MailNotRewardReshipmentPopup, PopupType.Login, data);
            }
        }
    }

    public class ReshipmentData
    {
        public IAPLateDelivery Delivery;
        public int ReshipmentId;
    }
}
