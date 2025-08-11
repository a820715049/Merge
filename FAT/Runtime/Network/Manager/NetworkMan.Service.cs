
using System.Collections.Generic;
using fat.msg;
using fat.rawdata;
using fat.service;
using FAT;
using FAT.Platform;
using Google.Protobuf;

namespace GameNet {
    using NetTask = SimpleResultedAsyncTask<IMessage>;
    using static FoundationWrapper;

    public partial class NetworkMan: IRTMListener, IGameModule, IUpdate {
        #region info
        
        public NetTask LoginProfile(AccountProfile profile_) {
            if (profile_.type != AccountLoginType.Facebook) return null;
            var req = new SavePlayerOpenInfoReq() { FacebookInfo = new() };
            var fb = req.FacebookInfo;
            fb.Id = profile_.id;
            fb.Name = profile_.name;
            fb.Avatar = profile_.pic;
            return PostMessage(req, PlayerOpenInfoService_SavePlayerOpenInfo.QueryPath, PlayerOpenInfoService_SavePlayerOpenInfo.URIRequest);
        }

        #endregion info
        #region IAP

        public NetTask IAPPurchase(int productId, PayContext context)
            => PostMessage(new MakeThroughCargoReq () {
                ChargeId = productId,
                PayContext = context
            }, PlatformService_MakeThroughCargo.QueryPath, PlatformService_MakeThroughCargo.URIRequest);

        public NetTask IAPDeliveryCheck()
            => PostMessage(new DeliverCargoReq(), PlatformService_DeliverCargo.QueryPath, PlatformService_DeliverCargo.URIRequest);

        #endregion IAP

        #region mail

        public NetTask FetchMail()
            => PostMessage(new GetMailReq(), MailService_GetMail.QueryPath, MailService_GetMail.URIRequest);

        public NetTask ReadMail(ulong mid)
            => PostMessage(new ReadMailReq() { Id = mid }, MailService_ReadMail.QueryPath, MailService_ReadMail.URIRequest);

        public NetTask FetchMailReward(ulong mid)
            => PostMessage(new GetMailRewardReq() { Id = mid }, MailService_ClaimMailReward.QueryPath, MailService_ClaimMailReward.URIRequest);

        public NetTask FetchAllMailReward()
            => PostMessage(new ClaimAllMailRewardReq(), MailService_ClaimAllReward.QueryPath, MailService_ClaimAllReward.URIRequest);

        #endregion mail

        #region deeplink

        public NetTask DeeplinkFullfill(string referer_, long ts_)
            => PostMessage(new DeeplinkInviteReq() {
                InvitorFpId = referer_,
                InvitorActivityBeginTs = ts_
            }, DeeplinkService_DeeplinkInvite.QueryPath, DeeplinkService_DeeplinkInvite.URIRequest);

        public NetTask DeeplinkStat(long ts_)
            => PostMessage(new GetInviteeStatReq() {
                InvitorActivityBeginTs = ts_,
            }, DeeplinkService_GetInviteeStat.QueryPath, DeeplinkService_GetInviteeStat.URIRequest);

        #endregion deeplink

        #region ranking

        public NetTask RequestRanking(ActivityRanking e_, RankingType type_)
            => PostMessage(new GetRankingInfoReq() {
                ID = e_.IdR,
                SubId = e_.SubId,
                RankType = type_,
            }, RankingService_GetRankingInfo.QueryPath, RankingService_GetRankingInfo.URIRequest);

        #endregion ranking
        
        #region playerinfo 获取玩家信息

        //通过玩家uid获取玩家信息 服务器返回PlayerOpenInfosMessage
        public NetTask GetPlayerOpenInfosByUidReq(IEnumerable<ulong> playerUidList)
        {
            var req = new GetPlayerOpenInfosByUidReq();
            req.Uids.AddRange(playerUidList);
            return PostMessage(req, 
                PlayerOpenInfoService_GetPlayerOpenInfosByUid.QueryPath, 
                PlayerOpenInfoService_GetPlayerOpenInfosByUid.URIRequest);
        }
        
        //通过facebook id获取玩家信息 服务器返回PlayerOpenInfosMessage
        public NetTask GetPlayerOpenInfosByFacebookIDReq(IEnumerable<string> facebookIdList)
        {
            var req = new GetPlayerOpenInfosByFacebookIDReq();
            req.FacebookIds.AddRange(facebookIdList);
            return PostMessage(req, 
                PlayerOpenInfoService_GetPlayerOpenInfosByFacebookID.QueryPath, 
                PlayerOpenInfoService_GetPlayerOpenInfosByFacebookID.URIRequest);
        }
        
        #endregion

        #region exchange 玩家之间通过中转站赠送/领取物品

        /// <summary>
        /// 通用给好友发送东西请求, 无需关注协议回复的数据内容，只关心NetTask是否成功即可
        /// </summary>
        /// <param name="toUid">接收者uid</param>
        /// <param name="sendTs">发送时间</param>
        /// <param name="expireTs">过期时间</param>
        /// <param name="items">key 道具id value 数量</param>
        /// <param name="type">交换类型</param>
        /// <returns></returns>
        public NetTask SendItemsToOtherReq(ulong toUid, long sendTs, long expireTs, IDictionary<int, int> items, ExchangeType type)
        {
            var req = new SendItemsToOtherReq()
            {
                ToUid = toUid,
                SendTs = sendTs,
                ExpireTs = expireTs,
                ExchangeType = type,
            };
            req.Items.Add(items);
            return PostMessage(req, 
                ExchangeService_SendItemsToOther.QueryPath,
                ExchangeService_SendItemsToOther.URIRequest);
        }

        /// <summary>
        /// 通用获取好友发送东西请求  返回GetItemsFromOthersResp 数据结构ItemsFromOther
        /// </summary>
        /// <param name="type">交换类型</param>
        /// <returns></returns>
        public NetTask GetItemsFromOthersReq(ExchangeType type)
        {
            return PostMessage(new GetItemsFromOthersReq() { ExchangeType = type },
                ExchangeService_GetItemsFromOthers.QueryPath, 
                ExchangeService_GetItemsFromOthers.URIRequest);
        }

        /// <summary>
        /// 领取好友发送的东西请求, 无需关注协议回复的数据内容，只关心NetTask是否成功即可
        /// </summary>
        /// <param name="recordId">服务器发来的发送记录id</param>
        /// <returns></returns>
        public NetTask ReceiveItemsFromOtherReq(ulong recordId)
        {
            return PostMessage(new ReceiveItemsFromOtherReq() { Id = recordId },
                ExchangeService_ReceiveItemsFromOther.QueryPath, 
                ExchangeService_ReceiveItemsFromOther.URIRequest);
        }

        #endregion
    }
}