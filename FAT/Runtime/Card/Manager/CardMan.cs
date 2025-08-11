/*
 * @Author: tang.yan
 * @Description: 集卡系统管理器
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/wz88ugv6graz2uq0#BQkOq
 * @Date: 2024-01-15 17:01:37
 */

using System;
using System.Collections.Generic;
using Config;
using Cysharp.Text;
using fat.rawdata;
using fat.gamekitdata;
using EL;
using EL.Resource;
using UnityEngine;

namespace FAT
{
    public partial class CardMan : IGameModule, IUserDataHolder, ISecondUpdate
    {
        //集卡活动是否满足解锁条件
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureCardAlbum);
        //记录当前正在进行的活动id 若不在活动时间内会自动置为0
        public int CurCardActId { get; set; }
        //卡牌活动处理器 用于创建活动 默认跟随卡牌管理器创建
        public CardActivityHandler CardActHandler = new CardActivityHandler();
        //入口红点状态
        public bool CanShowEntryRP => _CheckEntryRP();
        //目前是否在开卡包的流程中
        public bool IsInOpenPackState { get; private set; }
        //目前是否在等待打开万能卡入口界面
        private bool _isWaitOpenJoker = false;
        //目前是否在使用万能卡的流程中
        private bool _isInUseJokerState;
        //1/N闪卡必得卡包打开时的卡池堆栈
        private Stack<(int, string)> _specialPackRewardPool = new Stack<(int, string)>();

        public bool CheckValid()
        {
            return IsUnlock && GetCardActivity() != null && GetCardAlbumData() != null;
        }

        //检查集卡活动入口是否可以显示
        //同时满足以下条件时，出现预告入口：
        //1: 当前EventTime中有集卡活动正在开启（EventType@enum.EventTypeCardAlbum）
        //2: 玩家等级 >= 集卡功能提前展示等级
        public bool CheckCardEntryShow()
        {
            var isInCardActivity = false;
            var curTime = Game.Instance.GetTimestampSeconds();
            var confList = Game.Manager.configMan.GetEventTimeConfigsByType(fat.rawdata.EventType.CardAlbum);
            foreach (var conf in confList)
            {
                if (curTime >= conf.StartTime && curTime < conf.EndTime)
                {
                    isInCardActivity = true;
                    break;
                }
            }
            var isEntryShow = Game.Manager.featureUnlockMan.IsFeatureEntryShow(FeatureEntry.FeatureCardAlbum);
            return isInCardActivity && isEntryShow;
        }
        
        //检查目前是否为卡册可以重玩的状态，检查时机为：当玩家领取完卡册集齐奖励后检查并弹出； 每次上线时利用popup系统检测弹出
        //需满足两个条件：1、当前卡册所有奖励都已领取 2、EventCardRound.includeAlbumId中，有下一个可以开启的EventCardAlbum.id
        //不过不满足条件 则维持现状 无事发生
        //如果满足条件，则会触发卡册重开流程
        public bool CheckCanRestartAlbum()
        {
            if (!CheckValid())
                return false;
            var roundData = GetCardRoundData();
            return roundData != null && roundData.CheckHasNextAlbum() && roundData.CheckCollectAllReward();
        }

        //检查目前是否为重玩的卡册(典藏版卡册 读配置确定)
        public bool CheckIsRestartAlbum()
        {
            if (!CheckValid())
                return false;
            return GetCardRoundData()?.CheckIsRestartAlbum() ?? false;
        }

        //卡册重玩界面中调用 尝试真正重置卡册
        public void TryRestartAlbum()
        {
            if (!CheckValid() || !CheckCanRestartAlbum())
                return;
            //尝试开启新一轮的集卡 相关数据都重新随机
            GetCardRoundData()?.TryRestartAlbum();
            //根据随机后的卡册数据 刷新一下集卡活动相关数据
            CardActHandler?.RefreshCardActivity(GetCardActivity());
            //立即存档
            Game.Manager.archiveMan.SendImmediately(true);
            //重置后打开新一轮集卡界面
            OpenCardAlbumUI();
            //如果是第一次重玩 则算作积极行为
            if (GetCardRoundData()?.GetCurOpenAlbumIndex() == 1)
            {
                MessageCenter.Get<MSG.ACTIVITY_SUCCESS>().Dispatch(GetCardActivity());
            }
        }
        
        //获取当前卡牌活动 可能为空 外部调用时需要判空
        public CardActivity GetCardActivity()
        {
            return CurCardActId > 0 ? Game.Manager.activity.Lookup(CurCardActId) as CardActivity : null;
        }

        //获取当前卡牌活动对应的集卡轮次数据
        public CardRoundData GetCardRoundData()
        {
            return _allCardRoundDataDict.TryGetValue(CurCardActId, out var roundData) ? roundData : null;
        }

        //传入活动id 尝试获取对应卡册轮次中正在开启的卡册对应的index 没有时为0
        public int TryGetCurOpenAlbumIndex(int cardActId)
        {
            return _allCardRoundDataDict.TryGetValue(cardActId, out var roundData) ? roundData.GetCurOpenAlbumIndex() : 0;
        }
        
        //获取当前集卡轮次配置
        public EventCardRound GetCardRoundConfig()
        {
            return GetCardRoundData()?.GetConfig();
        }

        //获取当前卡牌活动中当前正在进行的卡册数据
        public CardAlbumData GetCardAlbumData(bool needFake = false)
        {
            return GetCardRoundData()?.TryGetCardAlbumData(needFake);
        }

        //获取当前卡册配置
        public EventCardAlbum GetCardAlbumConfig(bool needFake = false)
        {
            return GetCardAlbumData(needFake)?.GetConfig();
        }
        
        //传入卡组id 返回当前卡册中对应的卡组数据
        public CardGroupData GetCardGroupData(int groupId)
        {
            return GetCardAlbumData()?.TryGetCardGroupData(groupId);
        }

        //传入卡牌id 返回本卡册中对应的卡牌数据
        public CardData GetCardData(int cardId, bool needFake = false)
        {
            return GetCardAlbumData(needFake)?.TryGetCardData(cardId);
        }

        //当使用能量时调用此方法 传入本次使用的能量值
        public void OnUseEnergy(int amount)
        {
            if (CurCardActId > 0 && _allCardRoundDataDict.TryGetValue(CurCardActId, out var roundData))
            {
                var albumData = roundData.TryGetCardAlbumData();
                //记录配置指定的能量类型
                if (albumData != null && albumData.GetConfig()?.RecordErg == Constant.kMergeEnergyObjId)
                {
                    albumData.SetTotalCostEnergy(amount);
                }
            }
        }
        
        //传入cardPackId尝试开启卡包，返回开卡包是否成功
        //后续如果有打点需求 bool值可以换成Enum用于表示结果类型 
        public bool TryOpenCardPack(int cardPackId, Vector3 fromPos)
        {
            //判断是否在开卡包流程中 是的话返回
            if (IsInOpenPackState)
                return false;
            //开卡包时启用标记位 在所有可能存在的界面表现结束后 标记位置为false
            IsInOpenPackState = true;
            //活动/数据过期 这时如果有卡包且尝试开启 不再展示卡包结果，而是将卡包转化为指定的物品
            if (!CheckValid())
            {
                IsInOpenPackState = false;
                var cardPackConfig = Game.Manager.objectMan.GetCardPackConfig(cardPackId);
                if (cardPackConfig == null)
                {
                    DebugEx.FormatError("[CardMan.TryOpenCardPack]: cardPackConfig is null , cardPackId = {0}, CurCardActId = {1}", cardPackId, CurCardActId);
                    return false;
                }
                //卡包过期后再使用 转换成的奖励
                var r = cardPackConfig.ExpireItem.ConvertToRewardConfig();
                if (r != null)
                {
                    //发奖励
                    var expireReward = Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.expire);
                    UIFlyUtility.FlyReward(expireReward, fromPos);
                    //弹提示
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.EventEndTrans);
                }
                //拆开卡包时如果此时不在活动时间内 则打点
                DataTracker.TrackCardPackOpen(cardPackId, false, true, GetCardRoundData()?.GetCurRoundNum() ?? 0, "");
                return false;
            }
            //走开卡包逻辑
            OpenCardPack(cardPackId);
            //种种原因导致开卡包出来的结果为空 则开卡失败 没有相关表现
            if (_curCardPackResult == null || _curCardPackResult.Count < 1)
            {
                IsInOpenPackState = false;
                _openPackDisplayCb.Clear();
                return false;
            }
            //尝试执行开卡包流程相关界面表现
            TryOpenPackDisplay();
            return true;
        }
        
        //尝试执行开卡包流程相关界面表现
        public void TryOpenPackDisplay()
        {
            if (_openPackDisplayCb == null || _openPackDisplayCb.Count < 1)
            {
                //所有界面表现结束后 置为false
                IsInOpenPackState = false;
                _isInUseJokerState = false;
                _openPackDisplayCb?.Clear();
                return;
            }
            //每次都取第一个回调执行 并从list中移除
            var first = _openPackDisplayCb[0];
            _openPackDisplayCb.RemoveAt(0);
            first.Item1?.Invoke(first.Item2, first.Item3);
        }

        //传入卡包id 执行开卡包相关逻辑流程
        public List<int> OpenCardPack(int cardPackId)
        {
            _OpenCardPack(cardPackId);
            return _curCardPackResult;
        }

        public void OpenCardAlbumUI()
        {
            //保底逻辑 正常流程中不会出现 使用debug工具后可能会出现
            //尝试打开卡册界面时 检查是否可以重玩卡册 如果可以的话 则打开重玩界面
            if (CheckCanRestartAlbum())
            {
                UIManager.Instance.OpenWindow(UIConfig.UICardAlbumRestart);
                return;
            }
            if (CheckValid())
            {
                UIManager.Instance.OpenWindow(UIConfig.UICardAlbum);
                DataTracker.event_popup.Track(GetCardActivity());
                //打开卡册界面时 如果有万能卡 则也打开万能卡入口界面
                var jokerData = GetCardRoundData()?.GetFirstJokerData();
                if (jokerData != null && !UIManager.Instance.IsShow(UIConfig.UICardJokerEntrance))
                {
                    GetCardRoundData().SetCurShowJokerIndex(0);
                    jokerData.SetCurSelectCardId(0);    //开启入口界面时重置一下当前万能卡选择的卡片id
                    UIManager.Instance.OpenWindow(UIConfig.UICardJokerEntrance);
                }
            }
        }
        
        public void OpenJokerEntranceUI()
        {
            if (!CheckValid())
                return;
            //尝试打开万能卡入口界面时 检查是否可以重玩卡册 如果可以的话 则return
            if (CheckCanRestartAlbum())
                return;
            var jokerData = GetCardRoundData()?.GetCurIndexJokerData();
            if (jokerData == null)
                return;
            //在尝试打开万能卡界面时 如果此时没打开卡册界面 则也一同打开
            if (!UIManager.Instance.IsShow(UIConfig.UICardAlbum))
            {
                UIManager.Instance.OpenWindow(UIConfig.UICardAlbum);
            }
            jokerData.SetCurSelectCardId(0);    //开启入口界面时重置一下当前万能卡选择的卡片id
            UIManager.Instance.OpenWindow(UIConfig.UICardJokerEntrance);
        }

        //清理对应集卡活动相关的过期物品
        public void TryClearExpireItem(int actId)
        {
            if (actId <= 0) return;
            if (!_allCardRoundDataDict.TryGetValue(actId, out var roundData)) return;
            //卡包过期逻辑
            var expireItem = roundData.TryGetCardAlbumData()?.GetConfig()?.ExpireItem;
            if (expireItem != null)
            {
                var world = Game.Manager.mainMergeMan.world;
                foreach (var (f, t) in expireItem) {
                    var count = world.ConvertItem(f, t);
                    if (count > 0)
                    {
                        DebugEx.Info($"TryClearExpireItem : actId = {actId}, clear {count} item, expireItemId = {f}, newItemId = {t}");
                    }
                }
            }
            //万能卡过期逻辑
            roundData.TryClearJokerCard();
            //活动结束后清理当前缓存卡池
            _specialPackRewardPool.Clear();
        }
        
        //集卡活动结束时打点
        public void TrackCardAlbumEnd(int actId)
        {
            if (actId <= 0) return;
            if (_allCardRoundDataDict.TryGetValue(actId, out var roundData))
            {
                var albumData = roundData.TryGetCardAlbumData();
                if (albumData != null)
                {
                    albumData.GetAllCollectProgress(out var ownCount, out _);
                    albumData.CheckFinishGroupNum(out var finishGroupNum);
                    DataTracker.TrackCardAlbumEnd(roundData.CardActId, GetCardActivity()?.From ?? 0, albumData.CardAlbumId, albumData.CardLimitTempId, 
                        ownCount, finishGroupNum, albumData.StartTotalIAP, roundData.GetCurFinishNum());
                }
            }
        }
        
        //卡片数量减少时打点
        //reduceType为1代表新一轮转化为星星  2代表系统兑换 3代表好友卡片交换
        public void TrackCardReduction(int cardId, int reduceNum, int reduceType, int reduceParam)
        {
            var activity = GetCardActivity();
            if (activity == null) return;
            var roundData = GetCardRoundData();
            var albumData = roundData?.TryGetCardAlbumData();
            if (albumData == null) return;
            var cardData = GetCardData(cardId);
            var cardConf = cardData?.GetConfig();
            if (cardConf == null) return;
            var exchangeId = reduceType == 2 ? reduceParam : 0; //reduceType为2时表示星星兑换减少 此时reduceParam表示StarExchange.id
            DataTracker.TrackCardReduction(CurCardActId, activity.From, albumData.CardAlbumId, roundData.GetCurRoundNum(),
                cardId, cardConf.Star, cardConf.IsGold, cardData.OwnCount, reduceNum, reduceType, exchangeId);
        }
        
        #region 典藏版/普通版卡册UI切换逻辑

        //只供外部UI使用 用于决定目前是否使用假的卡册数据刷新界面
        public bool IsNeedFakeAlbumData => CheckIsRestartAlbum() && !_isShowGoldAlbum;
        //记录典藏版和普通版卡册UI切换状态 默认false则显示普通版
        private bool _isShowGoldAlbum = false;  
        
        //设置典藏版和普通版卡册UI切换状态 默认false则显示普通版
        public void SwitchShowGoldAlbumState()
        {
            _isShowGoldAlbum = !_isShowGoldAlbum;
        }

        public void ResetShowGoldAlbumState()
        {
            _isShowGoldAlbum = CheckIsRestartAlbum();
        }

        #endregion

        #region 内部基础数据构造逻辑
        
        //累计参与集卡活动的次数
        private int _totalJoinCount;
        //存储最近两次的集卡活动数据 key为卡牌活动id 若当前已经存了两期活动数据，如果有新一期活动要开，会删除存档中最老的活动数据
        private Dictionary<int, CardRoundData> _allCardRoundDataDict = new Dictionary<int, CardRoundData>();

        public void Reset()
        {
            _isWaitOpenJoker = false;
            ResetShowGoldAlbumState();
            _isWaitFriendsInfo = false;
        }

        public void LoadConfig() { }

        public void Startup()
        {
            MessageCenter.Get<MSG.IAP_DATA_READY>().AddListenerUnique(_OnIAPTotalAmountUpdate);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListenerUnique(_OnSpecialRewardFinish);
        }

        private void _OnSpecialRewardFinish()
        {
            //如果在开卡包的流程中 优先开卡包
            if (IsInOpenPackState)
            {
                TryOpenPackDisplay();
            }
            //否则尝试开万能卡入口界面
            else if (_isWaitOpenJoker)
            {
                //注册当界面环境idle时打开万能卡入口界面的回调
                UIManager.Instance.RegisterIdleAction("ui_idle_card_joker", 99999, () => OpenJokerEntranceUI());
                _isWaitOpenJoker = false;
            }
            else if (_isInUseJokerState)
            {
                TryOpenPackDisplay();
            }
        }

        //11.0版本新增字段CardRoundInfoMap，不再使用CardActInfoMap
        //遇到修改线上存档结构的做法：SetData时需要区分新老存档，FillData时直接使用新字段进行存储
        public void SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.CardActivity;
            if (data == null) return;
            //存档有数据时初始化相关数据
            _totalJoinCount = data.TotalJoinCount;
            _allCardRoundDataDict.Clear();
            foreach (var info in data.CardRoundInfoMap)
            {
                var actId = info.Key;
                //这里初始化完配置数据后，完全使用存档数据 不随机模板id和卡片的LimitId
                var roundData = new CardRoundData();
                roundData.InitCurOpenAlbumIndex(info.Value.CurOpenAlbumIndex); //使用存档中的卡册index
                roundData.InitRoundConfigData(actId);
                roundData.SetData(info.Value);
                _allCardRoundDataDict.Add(actId, roundData);
            }
            NextRefreshGiveCardTs = data.NextRefreshGiveCardTs;
            CurGiveCardNum = data.CurGiveCardNum;
            _specialPackRewardPool.Clear();
            //倒序遍历 靠后的元素先入栈
            if (data.SpecialPackInfoList != null)
            {
                for (var i = data.SpecialPackInfoList.Count - 1; i >= 0 ; i--)
                {
                    var packInfo = data.SpecialPackInfoList[i];
                    _specialPackRewardPool.Push((packInfo.PackId, packInfo.CardIdPool));
                }
            }
            //卡册星星兑换入口红点刷新时间
            _nextShowExchangeRPTs = data.NextShowExchangeRPTs; 
        }
        
        public void FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.CardActivity ??= new fat.gamekitdata.CardActivity();
            data.CardActivity.CardActInfoMap.Clear();   //老字段不再存数据
            data.CardActivity.TotalJoinCount = _totalJoinCount;
            var roundInfoMap = data.CardActivity.CardRoundInfoMap;
            roundInfoMap.Clear();
            foreach (var roundData in _allCardRoundDataDict.Values)
            {
                roundInfoMap[roundData.CardActId] = roundData.FillData();
            }
            data.CardActivity.NextRefreshGiveCardTs = NextRefreshGiveCardTs;
            data.CardActivity.CurGiveCardNum = CurGiveCardNum;
            var packInfoList = data.CardActivity.SpecialPackInfoList;
            packInfoList.Clear();
            //从栈顶到栈底遍历塞入list
            foreach (var info in _specialPackRewardPool)
            {
                var packInfo = new CardSpecialPackInfo
                {
                    PackId = info.Item1,
                    CardIdPool = info.Item2
                };
                packInfoList.Add(packInfo);
            }
            //卡册星星兑换入口红点刷新时间
            data.CardActivity.NextShowExchangeRPTs = _nextShowExchangeRPTs;
        }
        
        public void SecondUpdate(float dt)
        {
            if (!CheckValid())
                return;
            var curTs = Game.Instance.GetTimestampSeconds();
            _SecondUpdateJoker();
            _SecondUpdateTrade(curTs);
        }

        //每秒检查是否有过期的万能卡 如果有的话 尝试强制打开万能卡选卡界面，此时玩家必须选择 关不了界面
        private void _SecondUpdateJoker()
        {
            var roundData = GetCardRoundData();
            if (roundData == null) return;
            //如果没有过期的卡 则返回
            var hasExpire = roundData.CheckCardJokerExpire();
            if (!hasExpire) return;
            if (_openPackDisplayCb?.Count >= 1) return;
            //入口界面开着时返回
            if (UIManager.Instance.IsShow(UIConfig.UICardJokerEntrance)) return;
            // ui不处于idle状态时返回
            if (!UIManager.Instance.CheckUIIsIdleState()) return;
            // 如果目前既不在主棋盘也不在meta场景 则返回
            if (!UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain) && !Game.Manager.mapSceneMan.scene.Active) return;
            //检查目前所有万能卡中是否有过期了的且值得被使用的卡 如果没有则返回
            if (!roundData.CheckHasExpireUsefulJokerCard(out var firstUsefulIndex)) return;
            //从firstUsefulIndex开始展示  本次展示流程只展示值得被使用的万能卡 直到整个流程结束
            roundData.SetCurShowJokerIndex(firstUsefulIndex, true);
            OpenJokerEntranceUI();
        }

        public void InitCurCardActData()
        {
            //集卡活动开始时 尝试初始化数据 如果存档中已经有了对应活动id的数据 则无事发生
            //如果没有对应数据 则创建，并且还要检测一下当前存档中的集卡活动数据是否超过2期，如果超过了删除最老的那一期
            //这么做的意义主要是为了应对线上玩家对于已经结束的卡册活动有各种疑问的情况，便于QA策划查看该玩家上期活动的存档数据
            if (CurCardActId > 0 && !_allCardRoundDataDict.ContainsKey(CurCardActId))
            {
                //尝试删除老活动卡册的相关数据
                _TryDelOldAlbumData();
                //这里初始化完配置数据后，因为是第一次创建该活动id对应的数据 所以要随机模板id和卡片的LimitId
                var roundData = new CardRoundData();
                roundData.InitCurOpenAlbumIndex(0); //默认活动刚开始时卡册index从0开始
                roundData.InitRoundConfigData(CurCardActId, true);
                _allCardRoundDataDict.Add(CurCardActId, roundData);
                //活动真正开启后 累计开启次数
                _totalJoinCount++;
                //集卡活动开启时打点
                var albumData = roundData.TryGetCardAlbumData();
                if (albumData != null)
                {
                    DataTracker.TrackCardAlbumStart(roundData.CardActId, GetCardActivity()?.From ?? 0, albumData.CardAlbumId, albumData.CardLimitTempId, albumData.StartTotalIAP);  
                }
            }
        }

        //尝试找到最老的存档数据并删掉
        private void _TryDelOldAlbumData()
        {
            if (_allCardRoundDataDict.Count >= 2)
            {
                var activity = Game.Manager.activity;
                int oldActId = 0;
                long oldActEndTime = 0;
                foreach (var actId in _allCardRoundDataDict.Keys)
                {
                    var cur = activity.RecordOf(actId);
                    if (cur > 0)
                    {
                        if (oldActId == 0 || oldActEndTime > cur)
                        {
                            oldActId = actId;
                            oldActEndTime = cur;
                        }
                    }
                }
                //移除当前最老的卡牌活动相关数据
                _allCardRoundDataDict.Remove(oldActId);
            }
        }

        //当用户累充金额发生更新时 尝试设置活动的起始累充金额
        private void _OnIAPTotalAmountUpdate()
        {
            if (CurCardActId > 0 && _allCardRoundDataDict.TryGetValue(CurCardActId, out var roundData))
            {
                roundData.TryGetCardAlbumData()?.TryInitStartTotalIAP();
            }
        }

        //尝试预加载本次卡册活动的相关动态资源
        public void TryPreLoadAsset()
        {
            var albumImage = GetCardAlbumData()?.GetConfig()?.Image;
            if (albumImage != null)
            {
                var asset = albumImage.ConvertToAssetConfig();
                if (!ResManager.IsGroupLoaded(asset.Group))
                {
                    ResManager.LoadGroup(asset.Group);
                }
            }
        }
        
        #endregion

        #region 开卡包相关逻辑

        private List<int> _curCardPackResult = new List<int>(); //当前卡包开的结果
        
        private void _OpenCardPack(int cardPackId)
        {
            _curCardPackResult.Clear();
            _openPackDisplayCb.Clear();
            var curAlbumData = GetCardAlbumData();
            if (curAlbumData == null)
            {
                DebugEx.FormatError("[CardMan.OpenCardPack]: curAlbumData is null , cardPackId = {0}, CurCardActId = {1}", cardPackId, CurCardActId);
                return;
            }
            var cardPackConfig = Game.Manager.objectMan.GetCardPackConfig(cardPackId);
            if (cardPackConfig == null)
            {
                DebugEx.FormatError("[CardMan.OpenCardPack]: cardPackConfig is null , cardPackId = {0}, CurCardActId = {1}", cardPackId, CurCardActId);
                return;
            }
            string drawStr = "";
            string errorLog = "";
            //根据配置判断是否是闪卡必得卡包，如果是的话 有单独的开包流程
            if (cardPackConfig.IsShinnyGuar)
            {
                _SpecialDrawCard(cardPackConfig, curAlbumData, ref drawStr, ref errorLog);
            }
            //如果不是闪卡必得卡包，则走之前通用的开包流程
            else
            {
                _NormalDrawCard(cardPackConfig, curAlbumData, ref drawStr, ref errorLog);
            }
            //根据抽卡结果记录相关数据
            var hasNewCard = _UpdateCardData();
            //抽卡结果转成字符串
            var resultStr = "";
            foreach (var cardId in _curCardPackResult) { resultStr = string.Concat(resultStr, cardId, " "); }
            //拆开卡包时打点
            DataTracker.TrackCardPackOpen(cardPackId, hasNewCard, false, GetCardRoundData().GetCurRoundNum(), resultStr);
            //打印抽卡结果
            _PrintDrawResult(resultStr, drawStr, errorLog);
            //检查是否有卡组/卡册的集齐奖励 构建表现回调
            _BuildOpenCardDisplayCb(cardPackId);
            //立即存档
            Game.Manager.archiveMan.SendImmediately(true);
            //抽卡结束后广播事件
            MessageCenter.Get<MSG.GAME_CARD_DRAW_FINISH>().Dispatch();
        }

        private void _NormalDrawCard(ObjCardPack cardPackConfig, CardAlbumData curAlbumData, ref string drawStr, ref string errorLog)
        {
            var cardPackId = cardPackConfig.Id;
            //根据ObjCardPack.randomStarInfo，按权重随机获取一个RandomStar.id
            int randomStarId = _GetRandomStarId(cardPackConfig);
            if (randomStarId <= 0)
            {
                DebugEx.FormatError("[CardMan.OpenCardPack]: randomStarId is error , randomStarId = {0}, cardPackId = {1}, CurCardActId = {2}", randomStarId, cardPackId, CurCardActId);
                return;
            }
            int totalCostIap = 0;
#if !UNITY_EDITOR
            //计算累充金额 StartTotalIAP大于等于0时有效
            if (curAlbumData.StartTotalIAP >= 0)
            {
                totalCostIap = Game.Manager.iap.TotalIAPServer - curAlbumData.StartTotalIAP;
                totalCostIap = totalCostIap <= 0 ? 0 : totalCostIap;
            }
#else
            totalCostIap = Game.Manager.iap.TotalIAPServer - curAlbumData.StartTotalIAP;
            totalCostIap = totalCostIap <= 0 ? 0 : totalCostIap;
#endif
            //将本卡册中的所有卡片按所处的状态以及对应配置进行分组
            _DivideCardGroup(curAlbumData, cardPackConfig, totalCostIap);
            //执行抽卡流程
            _ExecuteDrawCard(randomStarId, cardPackConfig.GoldNewLimit, cardPackConfig.GoldLimit, ref errorLog);
            //构造抽卡结果日志
            drawStr = $"[CardMan.OpenCardPack Draw Normal Card Result]: cardPackId = {cardPackId}, randomStarId = {randomStarId}, totalCostEnergy = {curAlbumData.TotalCostEnergy}, totalCostIap = {totalCostIap}";
        }

        private void _SpecialDrawCard(ObjCardPack cardPackConfig, CardAlbumData curAlbumData, ref string drawStr, ref string errorLog)
        {
            errorLog = "";
            var cardPackId = cardPackConfig.Id;
            var needCount = cardPackConfig.CardNum; //这里表示最终给出几张卡片
            //有对应卡池 则以卡池为准给出结果
            if (_specialPackRewardPool.TryPop(out var cacheResult) && cacheResult.Item1 == cardPackId)
            {
                var strList = cacheResult.Item2.Split(",");
                var result = new List<int>();
                foreach (var str in strList)
                {
                    if (int.TryParse(str, out var cardId))
                    {
                        result.Add(cardId);
                    }
                }
                var count = 0;
                //遍历查找把未拥有的卡片加入最终结果
                foreach (var cardId in result)
                {
                    if (count >= needCount)
                        break;
                    var cardData = GetCardData(cardId);
                    if (cardData == null || cardData.IsOwn) //卡片数据为空或卡片已拥有时跳过
                        continue;
                    _curCardPackResult.Add(cardId);
                    count++;
                }
                //如果遍历完之后 还未达到需要的数量 则重新遍历 按顺序把不在_curCardPackResult中的cardId加进去
                if (count < needCount)
                {
                    foreach (var cardId in result)
                    {
                        if (count >= needCount)
                            break;
                        if (_curCardPackResult.Contains(cardId)) continue;
                        var cardData = GetCardData(cardId);
                        if (cardData == null) continue;
                        _curCardPackResult.Add(cardId);
                        count++;
                    }
                }
            }
            //否则就以当前持卡情况查找
            else
            {
                var result = _SpecialSortGoldCard(curAlbumData);
                var count = 0;
                foreach (var cardId in result)
                {
                    if (count >= needCount)
                        break;
                    _curCardPackResult.Add(cardId);
                    count++;
                }
            }
            //构造抽卡结果日志
            drawStr = $"[CardMan.OpenCardPack Draw Special Card Result]: cardPackId = {cardPackId}, totalCostEnergy = {curAlbumData.TotalCostEnergy}, needCount = {needCount}";
        }
        
        //1/n金卡必得礼包中用于显示的卡片id, needCount外部指定要显示的卡片数量  cardIdList卡片id信息
        public void FillShowCardIdList(int needCount, List<int> cardIdList)
        {
            if (!CheckValid() || needCount <= 0 || cardIdList == null)
                return;
            var curAlbumData = GetCardAlbumData();
            if (curAlbumData == null) 
                return;
            var result = _SpecialSortGoldCard(curAlbumData);
            var count = 0;
            foreach (var cardId in result)
            {
                if (count >= needCount)
                    break;
                cardIdList.Add(cardId);
                count++;
            }
            cardIdList.Sort(_ShowCardIdSortFunc);
        }

        private List<int> _cacheCardIdList = new List<int>(); 
        public void OnGetSpecialCardPack(int cardPackId)
        {
            _cacheCardIdList.Clear();
            if (!CheckValid()) return;
            //找到1/N闪卡必得活动实例，根据其配置的卡片展示数量来决定卡池里缓存多少个卡片id  默认缓存3个
            var needCount = 3;
            if (Game.Manager.activity.LookupAny(fat.rawdata.EventType.ShinnyGuarPack, out var activity))
            {
                var act = (PackShinnyGuar)activity;
                needCount = act.confD?.CardDisplayNum ?? 3;
            }
            //填充卡池
            FillShowCardIdList(needCount, _cacheCardIdList);
            var result = "";
            foreach (var id in _cacheCardIdList)
            {
                result = ZString.Concat(result, id, ",");
            }
            _specialPackRewardPool.Push((cardPackId, result));
        }

        private int _ShowCardIdSortFunc(int idA, int idB)
        {
            var groupIdA = GetCardData(idA)?.BelongGroupId ?? 0;
            var groupIdB = GetCardData(idB)?.BelongGroupId ?? 0;
            return groupIdA != groupIdB ? groupIdA.CompareTo(groupIdB) : idA.CompareTo(idB);
        }

        #region 开卡包逻辑执行完后的收尾逻辑 包括存档 执行表现等

        //根据抽卡结果记录相关数据
        private bool _UpdateCardData(int jokerType = 0)
        {
            bool hasNewCard = false;
            foreach (var cardId in _curCardPackResult)
            {
                var cardData = GetCardData(cardId);
                if (cardData != null)
                {
                    bool isNew = false;
                    bool oldOwnState = cardData.IsOwn;
                    cardData.ChangeCardCount();
                    //新获得的卡
                    if (!oldOwnState && cardData.IsOwn)
                    {
                        hasNewCard = true;
                        isNew = true;
                    }
                    //获得卡片时打点
                    var conf = cardData.GetConfig();
                    DataTracker.TrackCardChange(cardId, cardData.OwnCount, isNew, conf?.Star ?? 0, conf?.IsGold ?? false, jokerType, GetCardRoundData().GetCurRoundNum(), false);
                }
            }
            GetCardAlbumData()?.CheckIsCollectAll();
            return hasNewCard;
        }

        //打印抽卡结果
        private void _PrintDrawResult(string resultStr, string drawStr, string errorLog)
        {
            if (errorLog == "")
            {
                drawStr += $"\nResult Card Id = {resultStr}";
                DebugEx.Info(drawStr);
            }
            else
            {
                drawStr += $"\nErrorLog = {errorLog}\nResult Card Id = {resultStr}";
                DebugEx.Error(drawStr);
            }
        }

        //每次抽卡时构建相关界面表现回调 存储和传递的是BeginReward方法返回的RewardCommitData
        private List<(Action<int, List<RewardCommitData>>, int, List<RewardCommitData>)> _openPackDisplayCb = new();
        //构建抽卡流程表现回调
        private void _BuildOpenCardDisplayCb(int cardPackId)
        {
            //卡包结果为空 则不执行任何界面表现
            if (_curCardPackResult == null || _curCardPackResult.Count < 1) return;
            //如果结果不为空 则构建开卡包回调
            _openPackDisplayCb.Add((_OpenUICardPackOpen, cardPackId, null));
            //检查是否有卡组/卡册的集齐奖励 构建表现回调
            _CheckCollectReward();
            //检查是否有盖章活动(卡包版) 构建表现回调
            _CheckStampActivity(cardPackId);
        }

        //检查是否有卡组/卡册的集齐奖励 构建表现回调
        private void _CheckCollectReward()
        {
            var curAlbumData = GetCardAlbumData();
            if (curAlbumData == null) return;
            //先检查是否有卡组集齐(可能有多个)
            var rewardMan = Game.Manager.rewardMan;
            var allGroupInfo = curAlbumData.GetAllGroupDataMap().Values;
            var index = 1;
            foreach (var groupData in allGroupInfo)
            {
                //检查所有卡组是否有已集齐未领奖的 有的就加入队列 等待后续执行相关表现
                if (groupData.IsCollectAll && !groupData.IsRecReward)
                {
                    var rewardConfig = groupData.GetConfig()?.Reward;
                    if (rewardConfig == null) continue;
                    List<RewardCommitData> rewardList = new List<RewardCommitData>();
                    foreach (var r in rewardConfig)
                    {
                        var reward = r.ConvertToRewardConfig();
                        if (reward != null)
                        {
                            rewardList.Add(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.card));
                        }
                    }
                    _openPackDisplayCb.Add((_OpenUICardGroupReward, groupData.CardGroupId, rewardList));
                    groupData.IsRecReward = true;
                    //领取卡组奖励时打点
                    DataTracker.TrackCardGroupComplete(CurCardActId, GetCardActivity()?.From ?? 0, curAlbumData.CardAlbumId, curAlbumData.CardLimitTempId, 
                        groupData.CardGroupId, curAlbumData.StartTotalIAP, GetCardRoundData().GetCurRoundNum(), index, allGroupInfo.Count);
                }
                index++;
            }
            //再检查是否卡册整个集齐
            curAlbumData.GetAllCollectProgress(out var ownCount, out var allCount);
            bool isCollectAll = ownCount == allCount;
            //如果卡册已集齐但还没领取奖励 则加入回调队列
            if (isCollectAll && !curAlbumData.IsRecAlbumReward)
            {
                var rewardConfig = curAlbumData.GetConfig()?.Reward;
                if (rewardConfig != null)
                {
                    List<RewardCommitData> rewardList = new List<RewardCommitData>();
                    foreach (var r in rewardConfig)
                    {
                        var reward = r.ConvertToRewardConfig();
                        if (reward != null)
                        {
                            rewardList.Add(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.card));
                        }
                    }
                    _openPackDisplayCb.Add((_OpenUICardAlbumReward, curAlbumData.CardAlbumId, rewardList));
                    curAlbumData.IsRecAlbumReward = true;
                    //领取卡册奖励时打点
                    DataTracker.TrackCardAlbumComplete(CurCardActId, GetCardActivity()?.From ?? 0, curAlbumData.CardAlbumId, curAlbumData.CardLimitTempId, curAlbumData.StartTotalIAP, GetCardRoundData().GetCurRoundNum());
                }
            }
            //最后检查是否可以重玩卡册 如果可以的话 则把重开确认界面加入回调队列
            if (CheckCanRestartAlbum())
            {
                _openPackDisplayCb.Add((_OpenUICardAlbumRestart, 0, null));
            }
        }

        //检查是否有盖章活动(卡包版) 尝试使用idle action进行后续表现
        private void _CheckStampActivity(int cardPackId)
        {
            //活动没有开启时返回
            if (!Game.Manager.activity.LookupAny(fat.rawdata.EventType.Stamp, out var activity))
                return;
            var stampActivity = (ActivityStamp)activity;
            //执行cost逻辑 
            stampActivity.TryExecuteCost(cardPackId);
        }

        private void _OpenUICardPackOpen(int cardPackId, List<RewardCommitData> empty = null)
        {
            if (cardPackId <= 0) return;
            UIManager.Instance.OpenWindow(UIConfig.UICardPackOpen, _curCardPackResult, cardPackId);
        }

        private void _OpenUICardGroupReward(int groupId, List<RewardCommitData> rewards)
        {
            if (groupId <= 0) return;
            UIManager.Instance.OpenWindow(UIConfig.UICardGroupReward, groupId, rewards);
        }
        
        private void _OpenUICardAlbumReward(int albumId, List<RewardCommitData> rewards)
        {
            if (albumId <= 0) return;
            UIManager.Instance.OpenWindow(UIConfig.UICardAlbumReward, albumId, rewards);
        }
        
        private void _OpenUICardAlbumRestart(int id = 0, List<RewardCommitData> empty = null)
        {
            TryOpenPackDisplay();
            if (!CheckCanRestartAlbum()) return;
            UIManager.Instance.CloseWindow(UIConfig.UICardAlbum);
            UIManager.Instance.OpenWindow(UIConfig.UICardAlbumRestart);
        }
        
        #endregion

        #region 开卡包核心逻辑
        
        private int _GetRandomStarId(ObjCardPack cardPackConfig)
        {
            var configMan = Game.Manager.configMan;
            int result;
            using (ObjectPool<List<(int, int)>>.GlobalPool.AllocStub(out var randomList))
            {
                //构造随机id权重列表
                foreach (var randomStarId in cardPackConfig.RandomStarInfo)
                {
                    var randomStarConfig = configMan.GetCardRandomStarConfig(randomStarId);
                    if (randomStarConfig != null)
                    {
                        randomList.Add((randomStarId, randomStarConfig.Weight));
                    }
                }
                result = randomList.RandomChooseByWeight((e) => e.Item2).Item1;
            }
            return result;
        }

        //将本期卡片数据进行分组 (int, int, int)第一个表示卡牌id 第二个表示对应卡牌的星级 第三个表示对应卡牌的LimitId
        private List<(int, int, int)> _oldWhiteList = new List<(int, int, int)>();  //已获得的白卡
        private List<(int, int, int)> _oldGoldList = new List<(int, int, int)>();  //已获得的金卡
        private List<(int, int, int)> _newWhiteList = new List<(int, int, int)>();  //可获得的白卡
        private List<(int, int, int)> _newGoldList = new List<(int, int, int)>();  //可获得的金卡
        private List<(int, int, int)> _tempList = new List<(int, int, int)>();     //查找过程中用到的list
        private void _DivideCardGroup(CardAlbumData curAlbumData, ObjCardPack cardPackConfig, int totalCostIap)
        {
            _oldWhiteList.Clear();
            _oldGoldList.Clear();
            _newWhiteList.Clear();
            _newGoldList.Clear();
            _tempList.Clear();
            var goldPayPass = 0;
            if (cardPackConfig.GoldPayPass.TryGetValue(curAlbumData.CardLimitTempId, out var payPass))
                goldPayPass = payPass;
            int payRate = curAlbumData.GetConfig().PayRate;//累计充值对ergPass的加成系数（百分数）
            ulong totalCostEnergy = curAlbumData.TotalCostEnergy;
            var curTs = Game.Instance.GetTimestampSeconds();
            var actStartTs = GetCardActivity()?.startTS ?? 0;
            foreach (var cardData in curAlbumData.GetAllCardDataMap().Values)
            {
                int cardId = cardData.CardId;
                if (cardId <= 0) continue;
                //卡牌自行检查属于哪个组
                int groupId = cardData.CheckBelongGroup(totalCostEnergy, totalCostIap, goldPayPass, payRate, curTs, actStartTs);
                int cardStar = cardData.GetConfig().Star;
                int limitId = cardData.LimitId;
                switch (groupId)
                {
                    case 1:
                        _oldWhiteList.Add((cardId, cardStar, limitId));
                        break;
                    case 2:
                        _oldGoldList.Add((cardId, cardStar, limitId));
                        break;
                    case 3:
                        _newWhiteList.Add((cardId, cardStar, limitId));
                        break;
                    case 4:
                        _newGoldList.Add((cardId, cardStar, limitId));
                        break;
                }
            }
            //按照Item3(LimitId)从小到大排序
            _newGoldList.Sort((a, b) => a.Item3 - b.Item3);
        }
        
        //卡包星级信息 使用RewardConfig处理 Id代表需要的卡片星级 Count代表需要的数量 当数量变为0时 本信息失效
        //配置上，pendingGoldPriority是pendingStarReward的子集
        private List<RewardConfig> _pendingGoldPriority = new List<RewardConfig>();
        private List<RewardConfig> _pendingStarReward = new List<RewardConfig>();
        
        //执行抽卡流程
        private void _ExecuteDrawCard(int randomStarId, int goldLimitNew, int goldLimit, ref string errorLog)
        {
            var randomStarConfig = Game.Manager.configMan.GetCardRandomStarConfig(randomStarId);
            if (randomStarConfig == null)
                return;
            //构造需要的数据
            int doneGold = 0;       //记录抽卡结果中已有的金卡数量
            int doneGoldNew = 0;    //记录抽卡结果中已有的金卡新卡数量
            _pendingGoldPriority.Clear();   //待处理的金卡优先星级（有序）
            _pendingStarReward.Clear();   //待处理的卡片星级（有序）
            foreach (var starInfo in randomStarConfig.GoldPriority)
            {
                //这里没有直接用starInfo.ConvertToRewardConfig() 是因为这样转出来的数据会被cache起来，而抽卡逻辑是要修改这个数据的，所以每次抽卡都要用新的
                var r = new RewardConfig();
                string[] split = starInfo.Split(':');
                r.Id = split.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                r.Count = split.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                _pendingGoldPriority.Add(r);
            }
            foreach (var starInfo in randomStarConfig.StarReward)
            {
                //这里没有直接用starInfo.ConvertToRewardConfig() 是因为这样转出来的数据会被cache起来，而抽卡逻辑是要修改这个数据的，所以每次抽卡都要用新的
                var r = new RewardConfig();
                string[] split = starInfo.Split(':');
                r.Id = split.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                r.Count = split.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                _pendingStarReward.Add(r);
            }
            //处理金卡优先逻辑
            _ExecuteGoldDraw(goldLimitNew, goldLimit, ref doneGold, ref doneGoldNew, ref errorLog);
            //处理正常抽卡逻辑
            _ExecuteNormalDraw(goldLimitNew, goldLimit, ref doneGold, ref doneGoldNew, ref errorLog);
        }

        private void _ExecuteGoldDraw(int goldLimitNew, int goldLimit, ref int doneGold, ref int doneGoldNew, ref string errorLog)
        {
            //找到list中数量大于0且还有效的第一个元素 如果找不到说明没有要找的金卡优先信息了
            //Id代表需要的卡片星级 Count代表需要的数量 当数量变为0时 本信息失效
            var target = _pendingGoldPriority.FindEx(x => x.Id > 0 && x.Count > 0);
            if (target == null) return;
            int targetCardStar = target.Id;
            //本次寻找的结果卡片 Item1表示卡牌id Item2表示对应卡牌星级 Item3表示对应卡牌的LimitId
            (int, int, int) resultCardInfo;
            if (doneGoldNew < goldLimitNew) //需要出金卡，可以出新的金卡
            {
                //先从newGold中找（策划需要 _newGoldList查找时要先排序 这在前面已经排好了）
                resultCardInfo = _DrawInNewGoldList(targetCardStar);
                if (resultCardInfo.Item1 > 0)   //大于0说明在newGold组中找到了
                {
                    //将抽到的卡片id加入本次抽卡结果中
                    _curCardPackResult.Add(resultCardInfo.Item1);
                    //记录本次抽卡已获得的金卡
                    doneGold++;
                    //记录本次抽卡已获得的第一次得到的金卡
                    doneGoldNew++;
                    //将找到的卡从newGold组中移除
                    _newGoldList.Remove(resultCardInfo);
                    //将找到的卡加入oldGold组
                    _oldGoldList.Add(resultCardInfo);
                    //将target中需要的数量-1
                    target.Count--;
                    //查找pendingStarReward中和本次target一样的星级信息，对应次数也减1
                    //配置上，pendingGoldPriority是pendingStarReward的子集，如果找到了满足前者条件的卡片，则也需要在后者对应数据处-1
                    var otherTarget = _pendingStarReward.FindEx(x => x.Id == targetCardStar);
                    if (otherTarget != null)
                        otherTarget.Count--;
                    else
                        errorLog = "[_ExecuteGoldDraw]: 配置错误! RandomStar表中goldPriority不是starReward的子集！";
                }
                else if (doneGold < goldLimit)  //没找到时 如果此时出的金卡还没到限制数量 就要尝试去oldGold中出
                {
                    resultCardInfo = _DrawInOldGoldList(targetCardStar);
                    if (resultCardInfo.Item1 > 0)   //大于0说明在oldGold组中找到了
                    {
                        //将抽到的卡片id加入本次抽卡结果中
                        _curCardPackResult.Add(resultCardInfo.Item1);
                        //记录本次抽卡已获得的金卡
                        doneGold++;
                        //将target中需要的数量-1
                        target.Count--;
                        //查找pendingStarReward中和本次target一样的星级信息，对应次数也减1
                        //配置上，pendingGoldPriority是pendingStarReward的子集，如果找到了满足前者条件的卡片，则也需要在后者对应数据处-1
                        var otherTarget = _pendingStarReward.FindEx(x => x.Id == targetCardStar);
                        if (otherTarget != null) 
                            otherTarget.Count--;
                        else
                            errorLog = "[_ExecuteGoldDraw]: 配置错误! RandomStar表中goldPriority不是starReward的子集！";
                    }
                    else    //如果连oldGold组中都找不到金卡 说明配错了
                    {
                        errorLog = "[_ExecuteGoldDraw]: 配置错误! [需要出金卡且可以出新的金卡] 这种情况下没有找到任何符合条件的新/旧金卡 可能是当前账号没有满足任何金卡的限制条件!";
                        return;
                    }
                }
                else //此时出的金卡数量已经到限制数量 则报错
                {
                    errorLog = $"[_ExecuteGoldDraw]: 配置错误! [需要出金卡且可以出新的金卡] 这种情况下，能出的金卡数量已达限制. doneGold = {doneGold}, goldLimit = {goldLimit}, doneGoldNew = {doneGoldNew}, goldLimitNew = {goldLimitNew}";
                    return;
                }
            }
            else
            {
                if (doneGold < goldLimit) //需要出金卡. 可以出旧的金卡
                {
                    resultCardInfo = _DrawInOldGoldList(targetCardStar);
                    if (resultCardInfo.Item1 > 0)   //大于0说明在oldGold组中找到了
                    {
                        //将抽到的卡片id加入本次抽卡结果中
                        _curCardPackResult.Add(resultCardInfo.Item1);
                        //记录本次抽卡已获得的金卡
                        doneGold++;
                        //将target中需要的数量-1
                        target.Count--;
                        //查找pendingStarReward中和本次target一样的星级信息，对应次数也减1
                        //配置上，pendingGoldPriority是pendingStarReward的子集，如果找到了满足前者条件的卡片，则也需要在后者对应数据处-1
                        var otherTarget = _pendingStarReward.FindEx(x => x.Id == targetCardStar);
                        if (otherTarget != null) 
                            otherTarget.Count--;
                        else
                            errorLog = "[_ExecuteGoldDraw]: 配置错误! RandomStar表中goldPriority不是starReward的子集！";
                    }
                    else
                    {
                        errorLog = "[_ExecuteGoldDraw]: 配置错误! [需要出金卡且可以出旧的金卡] 这种情况下没有找到任何符合条件的旧金卡 可能是当前账号没有满足任何金卡的限制条件!";
                        return;
                    }
                }
                else
                {
                    //需要出金卡，不能出任何金卡 (配置避免这种情况)
                    errorLog = $"[_ExecuteGoldDraw]: 配置错误! 需要出金卡但配置上不允许出任何金卡, doneGold = {doneGold}, goldLimit = {goldLimit}, doneGoldNew = {doneGoldNew}, goldLimitNew = {goldLimitNew}";
                    return;
                }
            }
            //最后找到结果了 就继续找 否则结束查找
            if (resultCardInfo.Item1 > 0)
            {
                _ExecuteGoldDraw(goldLimitNew, goldLimit, ref doneGold, ref doneGoldNew, ref errorLog);
            }
        }
        
        private void _ExecuteNormalDraw(int goldLimitNew, int goldLimit, ref int doneGold, ref int doneGoldNew, ref string errorLog)
        {
            //找到list中数量大于0且还有效的第一个元素 如果找不到说明没有要找的星级信息了
            //Id代表需要的卡片星级 Count代表需要的数量 当数量变为0时 本信息失效
            var target = _pendingStarReward.FindEx(x => x.Id > 0 && x.Count > 0);
            if (target == null) return;
            int targetCardStar = target.Id;
            //本次寻找的结果卡片 Item1表示卡牌id Item2表示对应卡牌星级 Item3表示对应卡牌的LimitId
            (int, int, int) resultCardInfo;
            if (doneGoldNew < goldLimitNew) //需要出卡片，可以出新的金卡
            {
                resultCardInfo = _DrawInNewGoldWhiteList(targetCardStar);
                if (resultCardInfo.Item1 > 0)   //大于0说明在_newGoldList+_newWhiteList组中找到了
                {
                    //将抽到的卡片id加入本次抽卡结果中
                    _curCardPackResult.Add(resultCardInfo.Item1);
                    //判断抽出来的卡是否是金卡
                    var cardData = GetCardData(resultCardInfo.Item1);
                    bool isGold = cardData?.GetConfig()?.IsGold ?? false;   
                    //是新的金卡
                    if (isGold)
                    {
                        //记录本次抽卡已获得的金卡
                        doneGold++;
                        //记录本次抽卡已获得的第一次得到的金卡
                        doneGoldNew++;
                        //将找到的卡从newGold组中移除
                        _newGoldList.Remove(resultCardInfo);
                        //将找到的卡加入oldGold组
                        _oldGoldList.Add(resultCardInfo);
                    }
                    //是新的白卡
                    else
                    {
                        //将找到的卡从_newWhiteList组中移除
                        _newWhiteList.Remove(resultCardInfo);
                        //将找到的卡加入_oldWhiteList组
                        _oldWhiteList.Add(resultCardInfo);
                    }
                    //将target中需要的数量-1
                    target.Count--;
                }
                else if (doneGold < goldLimit)  //没找到时 如果此时出的金卡还没到限制数量 就要尝试去old gold white中出
                {
                    resultCardInfo = _DrawInOldGoldWhiteList(targetCardStar);
                    if (resultCardInfo.Item1 > 0) //大于0说明在_oldGoldList+_oldWhiteList组中找到了
                    {
                        //将抽到的卡片id加入本次抽卡结果中
                        _curCardPackResult.Add(resultCardInfo.Item1);
                        //判断抽出来的卡是否是金卡
                        var cardData = GetCardData(resultCardInfo.Item1);
                        bool isGold = cardData?.GetConfig()?.IsGold ?? false;   
                        //是旧的金卡
                        if (isGold)
                        {
                            //记录本次抽卡已获得的金卡
                            doneGold++;
                        }
                        //是旧的白卡
                        else
                        {
                            //不做其他处理
                        }
                        //将target中需要的数量-1
                        target.Count--;
                    }
                    else
                    {
                        errorLog = "[_ExecuteNormalDraw]: 配置错误! [需要出卡片且可以出新的金卡,但没找到新金卡且金卡还没到限制数量] 这种情况下没有找到任何符合条件的旧金旧白卡!";
                        return;
                    }
                }
                else if (doneGold >= goldLimit)//如果达到了限制 就尝试去old white中出
                {
                    resultCardInfo = _DrawInOldWhiteList(targetCardStar);
                    if (resultCardInfo.Item1 > 0) //大于0说明在_oldWhiteList组中找到了
                    {
                        //将抽到的卡片id加入本次抽卡结果中
                        _curCardPackResult.Add(resultCardInfo.Item1);
                        //将target中需要的数量-1
                        target.Count--;
                    }
                    else
                    {
                        errorLog = "[_ExecuteNormalDraw]: 配置错误! [需要出卡片且可以出新的金卡，但没有找到新金卡且金卡到了限制数量] 这种情况下没有找到任何符合条件的旧白卡!";
                        return;
                    }
                }
            }
            else
            {
                if (doneGold < goldLimit) //需要出卡片. 可以出旧的金卡
                {
                    resultCardInfo = _DrawInNewWhiteList(targetCardStar);
                    if (resultCardInfo.Item1 > 0) //大于0说明在_newWhiteList组中找到了
                    {
                        //将抽到的卡片id加入本次抽卡结果中
                        _curCardPackResult.Add(resultCardInfo.Item1);
                        //将找到的卡从_newWhiteList组中移除
                        _newWhiteList.Remove(resultCardInfo);
                        //将找到的卡加入_oldWhiteList组
                        _oldWhiteList.Add(resultCardInfo);
                        //将target中需要的数量-1
                        target.Count--;
                    }
                    else
                    {
                        resultCardInfo = _DrawInOldGoldWhiteList(targetCardStar);
                        if (resultCardInfo.Item1 > 0) //大于0说明在_oldGoldList+_oldWhiteList组中找到了
                        {
                            //将抽到的卡片id加入本次抽卡结果中
                            _curCardPackResult.Add(resultCardInfo.Item1);
                            //判断抽出来的卡是否是金卡
                            var cardData = GetCardData(resultCardInfo.Item1);
                            bool isGold = cardData?.GetConfig()?.IsGold ?? false;   
                            //是旧的金卡
                            if (isGold)
                            {
                                //记录本次抽卡已获得的金卡
                                doneGold++;
                            }
                            //是旧的白卡
                            else
                            {
                                //不做其他处理
                            }
                            //将target中需要的数量-1
                            target.Count--;
                        }
                        else
                        {
                            errorLog = "[_ExecuteNormalDraw]: 配置错误! [需要出卡片且可以出旧的金卡] 这种情况下没有找到任何符合条件的新白旧金旧白卡!";
                            return;
                        }
                    }
                }
                else  //需要出卡片，不能出任何金卡
                {
                    resultCardInfo = _DrawInNewWhiteList(targetCardStar);
                    if (resultCardInfo.Item1 > 0) //大于0说明在_newWhiteList组中找到了
                    {
                        //将抽到的卡片id加入本次抽卡结果中
                        _curCardPackResult.Add(resultCardInfo.Item1);
                        //将找到的卡从_newWhiteList组中移除
                        _newWhiteList.Remove(resultCardInfo);
                        //将找到的卡加入_oldWhiteList组
                        _oldWhiteList.Add(resultCardInfo);
                        //将target中需要的数量-1
                        target.Count--;
                    }
                    else
                    {
                        resultCardInfo = _DrawInOldWhiteList(targetCardStar);
                        if (resultCardInfo.Item1 > 0) //大于0说明在_oldWhiteList组中找到了
                        {
                            //将抽到的卡片id加入本次抽卡结果中
                            _curCardPackResult.Add(resultCardInfo.Item1);
                            //将target中需要的数量-1
                            target.Count--;
                        }
                        else
                        {
                            errorLog = "[_ExecuteNormalDraw]: 配置错误! [需要出卡片且不能出任何金卡] 这种情况下没有找到任何符合条件的新白旧白卡!";
                            return;
                        }
                    }
                }
            }
            //最后找到结果了 就继续找 否则结束查找
            if (resultCardInfo.Item1 > 0)
            {
                _ExecuteNormalDraw(goldLimitNew, goldLimit, ref doneGold, ref doneGoldNew, ref errorLog);
            }
        }
        
        //指定targetCardStar在_newGoldList中查找
        private (int, int, int) _DrawInNewGoldList(int targetCardStar)
        {
            //找到_newGoldList中卡牌id大于0且星级和targetCardStar一致的目标卡
            return _newGoldList.FindEx(x => x.Item1 > 0 && x.Item2 == targetCardStar);
        }

        //指定targetCardStar在_oldGoldList随机
        private (int, int, int) _DrawInOldGoldList(int targetCardStar)
        {
            return _DrawInTargetList(targetCardStar, 2);
        }
        
        //指定targetCardStar在_newGoldList+_newWhiteList中随机
        private (int, int, int) _DrawInNewGoldWhiteList(int targetCardStar)
        {
            return _DrawInTargetList(targetCardStar, 4, 3);
        }
        
        //指定targetCardStar在_oldGoldList+_oldWhiteList中随机
        private (int, int, int) _DrawInOldGoldWhiteList(int targetCardStar)
        {
            return _DrawInTargetList(targetCardStar, 2, 1);
        }
        
        //指定targetCardStar在_newWhiteList中随机
        private (int, int, int) _DrawInNewWhiteList(int targetCardStar)
        {
            return _DrawInTargetList(targetCardStar, 3);
        }
        
        //指定targetCardStar在_oldWhiteList中随机
        private (int, int, int) _DrawInOldWhiteList(int targetCardStar)
        {
            return _DrawInTargetList(targetCardStar, 1);
        }
        
        private (int, int, int) _DrawInTargetList(int targetCardStar, params int[] typeList)
        {
            _tempList.Clear();
            foreach (var groupId in typeList)
            {
                var targetGroup = groupId switch
                {
                    1 => _oldWhiteList,
                    2 => _oldGoldList,
                    3 => _newWhiteList,
                    4 => _newGoldList,
                    _ => null
                };
                if (targetGroup == null) continue;
                //从targetGroup中筛选出指定星级且不在本次开卡结果中的卡  加入_tempList中  用于后续随机
                foreach (var info in targetGroup)
                {
                    if (info.Item1 > 0 && info.Item2 == targetCardStar && !_curCardPackResult.Contains(info.Item1))
                    {
                        _tempList.Add(info);
                    }
                }
            }
            //除了_newGoldList以外的其他list 在查找时都是等概率随机
            return _tempList.Count > 0 ? _tempList.RandomChooseByWeight() : default;
        }
        
        //特殊开卡包逻辑中获取排序好的所有金卡List
        private List<int> _SpecialSortGoldCard(CardAlbumData curAlbumData)
        {
            //各个卡组的未获得卡片数量信息统计 key卡组id value未获得的卡片数量
            var cardGroupCountDict = new Dictionary<int, int>();
            foreach (var groupId in curAlbumData.GetAllGroupDataMap().Keys)
            {
                curAlbumData.GetCollectProgress(groupId, out var ownCount, out var allCount);
                var noHaveCount = allCount - ownCount;
                if (noHaveCount <= 0) noHaveCount = 0;
                cardGroupCountDict.Add(groupId, noHaveCount);
            }
            //遍历筛选分组卡片
            var notOwn_hasErgPass_list = new List<(int, int, int)>();    //未拥有且有能量坑深   id、所在卡组未获得的卡片数量、energyPassNum
            var notOwn_noErgPass_list = new List<(int, int, int)>();     //未拥有且没有能量坑深  id、所在卡组未获得的卡片数量、payPassNum
            var own_list = new List<(int, int)>();                      //已拥有 id、所在卡组未获得的卡片数量
            foreach (var cardData in curAlbumData.GetAllCardDataMap().Values)
            {
                int cardId = cardData.CardId;
                if (cardId <= 0) continue;
                //非金卡不检查
                if (!cardData.GetConfig().IsGold)
                    continue;
                cardGroupCountDict.TryGetValue(cardData.BelongGroupId, out var groupNoHaveCount);
                //卡片已拥有
                if (cardData.IsOwn && cardData.OwnCount >= 1)
                {
                    own_list.Add((cardId, groupNoHaveCount));
                }
                //卡片未拥有
                else
                {
                    if (cardData.EnergyPassNum == -1)
                        notOwn_noErgPass_list.Add((cardId, groupNoHaveCount, cardData.PayPassNum));
                    else
                        notOwn_hasErgPass_list.Add((cardId, groupNoHaveCount, cardData.EnergyPassNum));
                }
            }
            //按照案子里写的方式排序
            notOwn_hasErgPass_list.Sort((a, b) =>
            {
                // 按 Item2 升序排序
                int result = a.Item2.CompareTo(b.Item2);
                if (result != 0) return result;
                // 按 Item3 升序排序
                result = a.Item3.CompareTo(b.Item3);
                if (result != 0) return result;
                // 按 Item1 升序排序
                return a.Item1.CompareTo(b.Item1);
            });
            notOwn_noErgPass_list.Sort((a, b) =>
            {
                // 按 Item3 升序排序
                int result = a.Item3.CompareTo(b.Item3);
                if (result != 0) return result;
                // 按 Item2 升序排序
                result = a.Item2.CompareTo(b.Item2);
                if (result != 0) return result;
                // 按 Item1 升序排序
                return a.Item1.CompareTo(b.Item1);
            });
            own_list.Sort((a, b) =>
            {
                // 按 Item2 降序排序
                int result = b.Item2.CompareTo(a.Item2);
                if (result != 0) return result;
                // 按 Item1 升序排序
                return a.Item1.CompareTo(b.Item1);
            });
            //最终结果 取各个list中的item1(id) 
            var result = new List<int>();
            foreach (var info in notOwn_hasErgPass_list)
                result.Add(info.Item1);
            foreach (var info in notOwn_noErgPass_list)
                result.Add(info.Item1);
            foreach (var info in own_list)
                result.Add(info.Item1);
            return result;
        }

        #endregion
        
        #endregion

        #region 红点相关逻辑

        //设置指定卡组中所有卡片为已查看
        public void SetGroupCardSee(int groupId)
        {
            GetCardAlbumData()?.SetGroupCardSee(groupId);
            MessageCenter.Get<MSG.GAME_CARD_REDPOINT_UPDATE>().Dispatch(0);
        }
        
        //设置指定卡片为已查看
        public void SetCardSee(int cardId)
        {
            GetCardAlbumData()?.SetCardSee(cardId);
            MessageCenter.Get<MSG.GAME_CARD_REDPOINT_UPDATE>().Dispatch(cardId);
        }

        private bool _CheckEntryRP()
        {
            if (!CheckValid())
                return false;
            //有新卡时入口处显示红点
            foreach (var cardData in GetCardAlbumData().GetAllCardDataMap().Values)
            {
                if (cardData.CheckIsNew())
                    return true;
            }
            //有待收取的卡片时入口处显示红点
            if (CheckHasPendingCards())
                return true;
            return false;
        }

        #endregion

        #region 万能卡相关逻辑

        /// <summary>
        /// 当通过奖励系统获取到万能卡 关联配置表ObjCardJoker
        /// </summary>
        /// <param name="cardJokerId">万能卡 ObjectBasic.id </param>
        /// <param name="jokerNum">本次获得的万能卡数量</param>
        public bool OnGetCardJoker(int cardJokerId, int jokerNum)
        {
            //活动/数据过期 这时如果获取了万能卡 不再存储万能卡数据信息，而是将万能卡转化为资源物品
            if (!CheckValid())
            {
                var cardJokerConfig = Game.Manager.objectMan.GetCardJokerConfig(cardJokerId);
                if (cardJokerConfig == null)
                {
                    DebugEx.FormatError("[CardMan.OnGetCardJoker]: cardJokerConfig is null , cardJokerId = {0}, CurCardActId = {1}", cardJokerId, CurCardActId);
                    return false;
                }
                //获取转换成的奖励
                var r = cardJokerConfig.ExpireItem.ConvertToRewardConfig();
                if (r != null)
                {
                    for (int i = 0; i < jokerNum; i++)
                    {
                        //发奖励
                        var expireReward = Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.expire);
                        UIFlyUtility.FlyReward(expireReward, Vector3.zero);
                    }
                    //弹提示
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.EventEndTrans);
                }
                return false;
            }
            //将万能卡放进当前卡册中
            GetCardRoundData().OnGetCardJoker(cardJokerId, jokerNum, GetCardActivity().endTS);
            //当获取到万能卡时广播事件
            MessageCenter.Get<MSG.GAME_CARD_JOKER_GET>().Dispatch();
            //等待展示万能卡获得界面
            _isWaitOpenJoker = true;
            return true;
        }

        public void TryOpenJokerGet()
        {
            if (!CheckValid())
                return;
            UIManager.Instance.OpenWindow(UIConfig.UICardJokerGet);
        }

        //传入卡片id 使用当前万能卡兑换目标卡片
        public bool TryUseCardJoker(int targetCardId)
        {
            //活动结束时使用万能卡 会失败false 并直接关闭相关界面
            if (!CheckValid())
                return false;
            //判断是否在开卡包流程中 是的话返回false
            if (IsInOpenPackState)
                return false;
            //没有万能卡数据 或者检查到万能卡过期时 返回false
            var jokerData = GetCardRoundData().GetCurIndexJokerData();
            if (jokerData == null || jokerData.CheckIsExpire())
                return false;
            //没有指定的卡片数据时 返回false
            var cardData = GetCardData(targetCardId);
            if (cardData == null)
                return false;
            //万能白卡只能兑换白卡 万能金卡可以兑换白卡或金卡 ，检查是否符合 不符合则返回false
            if (jokerData.IsGoldCard == 0 && cardData.GetConfig().IsGold)
                return false;
            _isInUseJokerState = true;
            var jokerCardId = jokerData.CardJokerId;
            //借用之前的result结构 尽量让获得卡片的流程统一
            _curCardPackResult.Clear();
            _openPackDisplayCb.Clear();
            _curCardPackResult.Add(targetCardId);
            //构建表现回调 1、打开万能卡兑换成界面 2、检查是否有卡组/卡册的集齐奖励 
            _BuildJokerCardDisplayCb(jokerData, targetCardId);
            DebugEx.Info($"[CardMan.TryUseCardJoker]: targetCardId = {targetCardId}, jokerCardId = {jokerCardId}");
            //立即存档
            Game.Manager.archiveMan.SendImmediately(true);
            //使用万能卡兑换卡片结束后广播事件
            MessageCenter.Get<MSG.GAME_CARD_JOKER_USE>().Dispatch();
            //尝试执行开卡包流程相关界面表现
            TryOpenPackDisplay();
            return true;
        }

        private void _BuildJokerCardDisplayCb(CardJokerData jokerData, int targetCardId)
        {
            //卡包结果为空 则不执行任何界面表现
            if (_curCardPackResult == null || _curCardPackResult.Count < 1) return;
            //构建打开万能卡兑换卡片成功界面回调 需要使用数据更新前的卡牌数据
            _BuildStartDisplayCb(targetCardId);
            
            //刷新卡片相关数据 打印日志 并构建表现流程
            _UpdateCardData(jokerData.IsGoldCard == 0 ? 1 : 2); //打点万能卡类型 1代表白卡万能卡 2代表金卡万能卡
            //通知数据层万能卡使用成功
            GetCardRoundData().OnUseCardJokerSucc();
            
            //构建展示获得的卡片icon飞向所在卡组对应位置的表现回调
            _openPackDisplayCb.Add((_DisplayFlyToTargetCard, targetCardId, null));
            //检查是否有卡组/卡册的集齐奖励 构建表现回调
            _CheckCollectReward();
            //奖励发完后尝试继续下一个万能卡
            _openPackDisplayCb.Add((_DisplayNextJokerCard, 0, null));
        }

        private void _BuildStartDisplayCb(int targetCardId)
        {
            var cardData = GetCardData(targetCardId);
            if (cardData == null) return;
            var isOwn = cardData.IsOwn;
            GetCardAlbumData().GetCollectProgress(cardData.BelongGroupId, out var ownCount, out var allCount);
            var isComplete = !cardData.IsOwn && ownCount == allCount - 1;
            var cardCount = cardData.OwnCount;
            _openPackDisplayCb.Add((
                (id, empty) => {
                    UIManager.Instance.CloseWindow(UIConfig.UICardJokerSelect);
                    UIManager.Instance.OpenWindow(UIConfig.UICardJokerSet, targetCardId, isOwn, isComplete, cardCount); 
                }, 0, null));
        }

        private void _DisplayFlyToTargetCard(int targetCardId, List<RewardCommitData> empty = null)
        {
            MessageCenter.Get<MSG.UI_JUMP_TO_SELECT_CARD>().Dispatch(targetCardId, TryOpenPackDisplay);
        }

        private void _DisplayNextJokerCard(int id = 0, List<RewardCommitData> empty = null)
        {
            TryOpenPackDisplay();
            OpenJokerEntranceUI();
        }

        #endregion
    }
}


