/*
 * @Author: tang.yan
 * @Description: 集卡系统管理器-卡片交易相关逻辑
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/xcgdf85d4z2lzeyz
 * @Date: 2024-10-18 10:10:30
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EL;
using FAT.Platform;
using fat.rawdata;
using UnityEngine;
using fat.gamekitdata;
using fat.msg;

namespace FAT
{
    //卡片交易相关逻辑
    public partial class CardMan
    {
        //卡片交易是否解锁
        public bool IsCardTradeUnlock => IsCardTradeUnlockInner || IsInWhiteList;
        private bool IsCardTradeUnlockInner => IsUnlock &&
                                        Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureGiveCard);
        //今日已赠卡次数 会存档
        public int CurGiveCardNum { get; private set; }
        //下次赠卡次数重置时间 会存档
        public long NextRefreshGiveCardTs { get; private set; }
        //白名单内
        private bool IsInWhiteList => fat.conf.Data.GetExchangeCardWhitelist(Game.Manager.networkMan.fpId) != null;

        //检查是否有待收取的卡片
        public bool CheckHasPendingCards()
        {
            return _pendingCardInfoList.Count > 0;
        }
        
        //尝试打开赠卡界面
        public void TryOpenUICardGifting(int cardId)
        {
            if (!DebugIsIgnoreFbBind)
            {
                //检查是否绑定了fb
                var isBindFb = PlatformSDK.Instance.binding.CheckBind(AccountLoginType.Facebook);
                if (!isBindFb) return;
            }
            //若绑定了直接打开赠卡界面
            UIManager.Instance.OpenWindow(UIConfig.UICardGifting, cardId);
        }
        
        //获取目前可以赠送卡片的好友信息列表
        public List<PlayerOpenInfo> GetFriendInfoList()
        {
            return _friendInfoList;
        }

        public PlayerOpenInfo TryGetFriendInfo(int index)
        {
            return _friendInfoList.TryGetByIndex(index, out var info) ? info : null;
        }
        
        //传入uid获取目前送给过自己卡片的好友信息
        public PlayerOpenInfo GetSentCardFriendInfo(ulong uid)
        {
            return _sentCardFriendInfoList.TryGetValue(uid, out var info) ? info : null;
        }
        
        //获取目前送给自己的卡片信息列表 里面存了赠送人的uid 可以结合_sentCardFriendInfoList使用
        public List<ItemsFromOther> GetPendingCardInfoList()
        {
            return _pendingCardInfoList;
        }
        
        //打开卡册概览界面时打点
        public void TrackOpenCardOverview()
        {
            var activity = GetCardActivity();
            if (activity == null) return;
            var roundData = GetCardRoundData();
            var albumData = roundData?.TryGetCardAlbumData();
            if (albumData == null) return;
            DataTracker.TrackOpenCardOverview(CurCardActId, activity.From, albumData.CardAlbumId, roundData.GetCurRoundNum());
        }
        
        //跳转到换卡社群同时打点
        public void JumpTradingGroup(int buttonType)
        {
            //跳转
            UIBridgeUtility.OpenURL(Game.Manager.configMan.globalConfig.CardTradeGroupLink);
            //打点
            var activity = GetCardActivity();
            if (activity == null) return;
            var roundData = GetCardRoundData();
            var albumData = roundData?.TryGetCardAlbumData();
            if (albumData == null) return;
            DataTracker.TrackJumpTradingGroup(CurCardActId, activity.From, albumData.CardAlbumId, roundData.GetCurRoundNum(), buttonType);
        }

        //请求facebook好友信息的cd时间(单位秒) 游戏内重连时不会重置cd时间，杀进程重进后会重置为-1, -1代表本次游戏期间还没有请求过好友信息
        private int _pullFriendInfoCd = -1;
        private bool _isWaitFriendsInfo = false;
        private List<List<string>> _fbFriendIdList = new();
        private List<PlayerOpenInfo> _friendInfoList = new();
        
        //界面调用 协程方式刷新赠卡界面 分多步
        public SimpleAsyncTask RefreshUICardGiftingTask;
        public void TryRefreshUICardGifting()
        {
            //如果task没在执行 则执行协程 
            //起task的目的是不允许外部调用随意打断可能正在执行的请求任务
            if (RefreshUICardGiftingTask == null)
            {
                Game.Instance.StartCoroutineGlobal(_TryRefreshUICardGifting());
            }
        }

        public void ClearRefreshUICardGiftingTask()
        {
            RefreshUICardGiftingTask = null;
        }
        
        //目前选中的cell Index -1代表啥也没选 正常是从0开始
        public int CurSelectFriendIndex { get; set; }

        #region IOS专用 好友列表刷新方法

        //检查是否需要在好友送卡界面显示主动刷新按钮
        //仅在IOS环境下检查，如果玩家没给ATT权限则显示刷新按钮。其余情况默认都不显示
        public bool CheckNeedShowRefreshBtn()
        {
#if !UNITY_EDITOR && UNITY_IOS
            return PlatformSDK.Instance.Adapter.IsNeedLimitedLogin();
#else
            return false;
#endif
        }

        //主动重新登录以让SDK层面重新向Facebook请求好友信息，以便下次调用GetGameFriends时获取到最新好友信息
        public void ReLoginToRefreshFriend()
        {
#if !UNITY_EDITOR && UNITY_IOS
            PlatformSDK.Instance.binding.TryBind(AccountLoginType.Facebook, true);
#endif
        }

        #endregion
        
        private IEnumerator _TryRefreshUICardGifting()
        {
            if (!IsCardTradeUnlock || !CheckValid())
                yield break;
            //起task供外部感知任务是否完成
            RefreshUICardGiftingTask = new SimpleAsyncTask();
            //1. 首先调用sdk接口拉取到facebook好友信息列表
            //这里加个cd限制 避免多次请求facebook api被封 以及有人故意频繁请求，在cd时间内打开界面时直接使用上次的缓存信息
            //-1代表本次游戏期间还没有请求过好友信息
            var waitTime = Game.Manager.configMan.globalConfig.PullFriendInfoWaitTime;
            if (_pullFriendInfoCd != -1 && _pullFriendInfoCd <= waitTime)
            {
                RefreshUICardGiftingTask.ResolveTaskCancel();
                yield break;
            }
            var adapter = PlatformSDK.Instance.Adapter;
            //2.检查目前是否有user_friend权限 有的话放行 没有的话向sdk请求, debug模式下忽略检查
            var friendPermission = centurygame.CGFacebook.FRIENDS_PERMISSION;
            if (!DebugIsIgnoreFbBind && !adapter.HasPermission(friendPermission))
            {
                var isWaitPermission = true;
                var isPermission = false;
                adapter.AskPermission(friendPermission, (permission, isSuccess, _) =>
                {
                    isWaitPermission = false;
                    isPermission = isSuccess && adapter.HasPermission(friendPermission);
                });
                yield return new WaitUntil(() => !isWaitPermission);
                //若最后请求权限失败 则退出整个流程
                if (!isPermission)
                {
                    RefreshUICardGiftingTask.ResolveTaskCancel();
                    yield break;
                }
            }
            _isWaitFriendsInfo = true;
            //只要调用过sdk接口了 就开始cd计时
            _pullFriendInfoCd = 0;
            //debug流程下使用自定义的好友列表
            if (DebugIsIgnoreFbBind && _friendIdList != null)
            {
                _OnGetGameFriendsResult(_friendIdList, true, null);
            }
            else
            {
                adapter.GetGameFriends(_OnGetGameFriendsResult);
            }
            yield return new WaitUntil(() => !_isWaitFriendsInfo);
            //向fb请求到的好友列表为空时返回
            if (_fbFriendIdList.Count < 1 || _fbFriendIdList[0].Count < 1)
            {
                RefreshUICardGiftingTask.ResolveTaskFail();
                yield break;
            }
            //3.使用fb id List向服务器请求真正的和游戏绑定的好友信息
            foreach (var idList in _fbFriendIdList)
            {
                var task = Game.Manager.networkMan.GetPlayerOpenInfosByFacebookIDReq(idList);
                yield return task;
                if (!task.isSuccess || task.result is not fat.msg.PlayerOpenInfosMessage resp) 
                    continue;
                foreach (var playerOpenInfo in resp.Players)
                {
                    //检查服务器返回好友信息的合法性 合法的才显示
                    if (playerOpenInfo.Uid > 0 && playerOpenInfo.FacebookInfo != null && !string.IsNullOrEmpty(playerOpenInfo.FacebookInfo.Id))
                        _friendInfoList.Add(playerOpenInfo);
                }
            }
            RefreshUICardGiftingTask.ResolveTaskSuccess();
        }
        
        private void _OnGetGameFriendsResult(List<string> friends, bool isSuccess, SDKError sdkError)
        {
            _isWaitFriendsInfo = false;
            if (isSuccess)
            {
                //成功请求到fb好友信息后清理上次的缓存
                _fbFriendIdList.Clear();
                _friendInfoList.Clear();
                //将friends按照splitNum划分成多组装进fbFriendIdList里，避免一次性向服务器发送过多数据
                var splitNum = Game.Manager.configMan.globalConfig.MaxPullFriendInfoNum;
                _SplitList(friends, splitNum, _fbFriendIdList);
            }
        }
        
        //界面调用 协程方式向服务器请求赠卡
        //toPlayerInfo 要赠予的玩家信息  cardId要赠的卡片
        public IEnumerator TryGiftCardToFriend(PlayerOpenInfo toPlayerInfo, int cardId)
        {
            if (toPlayerInfo == null || toPlayerInfo.Uid <= 0)
                yield break;
            if (!CheckValid())
                yield break;
            var cardData = GetCardData(cardId);
            var cardConf = cardData?.GetConfig();
            if (cardData == null || cardConf == null)
                yield break;
            //金卡或者拥有张数小于等于1的卡片不允许交换
            if (cardConf.IsGold || cardData.OwnCount <= 1)
                yield break;
            //赠送的卡片过期时间等于本期集卡活动结束时间
            var expireTs = GetCardActivity().endTS;
            //注意这里是直接减少卡片张数 然后立即存档，等确认存档成功后，再打点，以及向服务器发送请求协议，
            cardData.ChangeCardCount(false);
            CurGiveCardNum++;
            var archiveMan = Game.Manager.archiveMan;
            archiveMan.SendImmediately(true);
            yield return new WaitUntil(() => archiveMan.uploadCompleted);
            //打点
            Game.Manager.cardMan.TrackCardReduction(cardId, 1, 3, 0);
            //发协议
            var sendTs = Game.Instance.GetTimestampSeconds();
            var items = new Dictionary<int, int> { { cardId, 1 } };
            var task = Game.Manager.networkMan.SendItemsToOtherReq(toPlayerInfo.Uid, sendTs, expireTs, items, ExchangeType.Card);
            yield return task;
            if (!task.isSuccess)
            {
                //如果服务器返回失败 则撤回数据改动 卡片张数+1 再补个打点-1代表失败
                cardData.ChangeCardCount();
                CurGiveCardNum--;
                archiveMan.SendImmediately(true);
                Game.Manager.cardMan.TrackCardReduction(cardId, -1, 3, 0);
            }
            else
            {
                //服务器返回成功
                //如果返回时活动结束 则不开界面
                if (Game.Instance.GetTimestampSeconds() >= expireTs)
                    yield break;
                //打开赠卡成功界面
                UIManager.Instance.OpenWindow(UIConfig.UICardTradeSuccess, cardId, toPlayerInfo);
                //赠送卡片成功事件
                MessageCenter.Get<MSG.UI_GIVE_CARD_SUCCESS>().Dispatch();
            }
        }
        
        //目前送给过自己卡片的好友信息列表
        private Dictionary<ulong, PlayerOpenInfo> _sentCardFriendInfoList = new();
        //目前送给自己的卡片信息列表
        private List<ItemsFromOther> _pendingCardInfoList = new();

        //向服务器发送请求 获取待领取的卡片相关信息
        //有三种调用方式：
        //1、玩家上线时在合适时机拉取一下 用于红点显示
        //2、打开卡片收取箱界面时配合界面流程拉取
        //3、玩家在线时如果收到RTM相关消息 主动拉取一下
        public void TryPullPendingCardInfo()
        {
            Game.Instance.StartCoroutineGlobal(CoPullPendingCardInfo());
        }
        
        public IEnumerator CoPullPendingCardInfo()
        {
            if (!IsCardTradeUnlock || !CheckValid())
                yield break;
            _sentCardFriendInfoList.Clear();
            _pendingCardInfoList.Clear();
            var expireTs = GetCardActivity().endTS;
            var task = Game.Manager.networkMan.GetItemsFromOthersReq(ExchangeType.Card);
            yield return task;
            if (!task.isSuccess || task.result is not GetItemsFromOthersResp resp)
                yield break;
            var curTime = Game.Instance.GetTimestampSeconds();
            //检查活动是否过期 是的话结束流程 不处理后续组织数据的逻辑
            if (curTime >= expireTs)
                yield break;
            //存储服务器发来的信息
            foreach (var info in resp.PlayerOpenInfos)
            {
                _sentCardFriendInfoList.AddIfAbsent(info);
            }
            //去除收取箱中已经过期或数据不存在的卡片
            foreach (var itemInfo in resp.Items)
            {
                if (curTime >= itemInfo.ExpireTs)
                    continue;
                var cardId = itemInfo.Items.FirstOrDefault().Key;
                var cardData = GetCardData(cardId);
                var cardConf = cardData?.GetConfig();
                if (cardData == null || cardConf == null)
                    continue;
                _pendingCardInfoList.AddIfAbsent(itemInfo);
            }
            _pendingCardInfoList.Sort((a, b) => b.SendTs.CompareTo(a.SendTs));
            //拉取服务器发来的待收取卡片信息成功
            MessageCenter.Get<MSG.UI_PULL_PENDING_CARD_INFO_SUCCESS>().Dispatch();
        }

        //向服务器发送请求 尝试领取卡片收取箱中指定的卡片
        public IEnumerator TryClaimPendingCard(ItemsFromOther itemInfo)
        {
            if (!IsCardTradeUnlock || !CheckValid() || itemInfo == null)
                yield break;
            //先检查一下是否过期
            if (Game.Instance.GetTimestampSeconds() >= itemInfo.ExpireTs)
                yield break;
            //对于卡片收取箱 默认只取Items中的第一个 且卡片数量默认为1
            var cardId = itemInfo.Items.FirstOrDefault().Key;
            var cardData = GetCardData(cardId);
            var cardConf = cardData?.GetConfig();
            if (cardData == null || cardConf == null)
                yield break;
            var expireTs = GetCardActivity().endTS;
            //检查是否是新卡
            bool isNew = false;
            bool oldOwnState = cardData.IsOwn;
            //注意这里是直接增加卡片张数 然后立即存档，等确认存档成功后，再打点，以及向服务器发送请求协议，
            cardData.ChangeCardCount();
            //新获得的卡
            if (!oldOwnState && cardData.IsOwn)
            {
                isNew = true;
            }
            //立即存档
            var archiveMan = Game.Manager.archiveMan;
            archiveMan.SendImmediately(true);
            yield return new WaitUntil(() => archiveMan.uploadCompleted);
            //获得赠送的卡片时打点
            DataTracker.TrackCardChange(cardId, cardData.OwnCount, isNew, cardConf.Star, cardConf.IsGold, 0, GetCardRoundData().GetCurRoundNum(), true);
            //向服务器发送请求协议
            var task = Game.Manager.networkMan.ReceiveItemsFromOtherReq(itemInfo.Id);
            yield return task;
            if (!task.isSuccess)
            {
                //如果服务器返回失败 则撤回数据改动 卡片张数-1
                cardData.ChangeCardCount(false);
                archiveMan.SendImmediately(true);
                //再补个打点ownNum传-1代表失败
                DataTracker.TrackCardChange(cardId, -1, isNew, cardConf.Star, cardConf.IsGold, 0, GetCardRoundData().GetCurRoundNum(), true);
            }
            else
            {
                //服务器返回成功
                //如果返回时活动结束 则不开界面
                if (Game.Instance.GetTimestampSeconds() >= expireTs)
                    yield break;
                //执行获得卡片的通用流程 基本逻辑借用万能卡的
                _isInUseJokerState = true;
                _curCardPackResult.Clear(); //借用之前的result结构 尽量让获得卡片的流程统一
                _openPackDisplayCb.Clear();
                _curCardPackResult.Add(cardId);
                //检查是否收集齐卡组
                GetCardAlbumData()?.CheckIsCollectAll();
                //构建收取卡片表现回调 内含发奖逻辑
                _BuildReceiveCardDisplayCb(cardId);
                //立即存档
                archiveMan.SendImmediately(true);
                yield return new WaitUntil(() => archiveMan.uploadCompleted);
                //打开卡片收取成功界面 界面关闭后执行获得卡片后的通用表现流程
                var friendInfo = GetSentCardFriendInfo(itemInfo.FromUid);
                UIManager.Instance.OpenWindow(UIConfig.UICardReceive, cardId, friendInfo);
            }
        }

        public void TryDisplayReceiveCard(bool showAnim)
        {
            if (!_isInUseJokerState) 
                return;
            //如果不展示动画 则移除第一个cb
            if (!showAnim)
            {
                _openPackDisplayCb.RemoveAt(0);
            }
            TryOpenPackDisplay();
        }
        
        private void _BuildReceiveCardDisplayCb(int targetCardId)
        {
            //卡包结果为空 则不执行任何界面表现
            if (_curCardPackResult == null || _curCardPackResult.Count < 1) return;
            //构建展示获得的卡片icon飞向所在卡组对应位置的表现回调
            _openPackDisplayCb.Add((_DisplayFlyToTargetCard, targetCardId, null));
            //检查是否有卡组/卡册的集齐奖励 构建表现回调
            _CheckCollectReward();
        }

        //每秒检查是否要重置赠卡次数
        private void _SecondUpdateTrade(long curTs)
        {
            //每秒计时cd，-1代表本次游戏期间还没有请求过好友信息
            if (_pullFriendInfoCd != -1)
            {
                _pullFriendInfoCd ++;
                //超过等待时间时重置为-1
                if (_pullFriendInfoCd > Game.Manager.configMan.globalConfig.PullFriendInfoWaitTime)
                    _pullFriendInfoCd = -1;
            }
            //若当前时间大于下次重置时间 则重置
            if (curTs >= NextRefreshGiveCardTs)
            {
                var offsetHour = Game.Manager.configMan.globalConfig.GiveCardRefreshUtc;
                //刷新下次重置时间
                NextRefreshGiveCardTs = ((curTs - offsetHour * 3600) / Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay + offsetHour * 3600;
                //重置今日已赠卡次数
                CurGiveCardNum = 0;
            }
        }
        
        private void _SplitList<T>(List<T> source, int chunkSize, List<List<T>> result)
        {
            for (int i = 0; i < source.Count; i += chunkSize)
            {
                // 使用 GetRange 获取子列表，chunkSize 或最后剩下的元素数
                result.Add(source.GetRange(i, System.Math.Min(chunkSize, source.Count - i)));
            }
        }
    }
}


