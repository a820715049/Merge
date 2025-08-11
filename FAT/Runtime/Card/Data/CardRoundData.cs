/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡册轮次相关数据 
 * @Date: 2024-10-18 10:10:22
 */

using System.Collections.Generic;
using System.Linq;
using fat.rawdata;
using EL;
using fat.gamekitdata;

namespace FAT
{
    //卡册轮次相关数据
    public class CardRoundData
    {
        public int CardActId;           //集卡活动id
        public int CardRoundId;         //本次集卡活动使用的轮次id(EventCardRound.id)
        private int _curOpenAlbumIndex;   //当前正在开启的卡册序号,默认从0开始,需要使用此序号去EventCardRound.includeAlbumId中去索引
        private CardAlbumData _curOpenAlbumData;  //当前正在开启的卡册数据
        private List<CardJokerData> _cardJokerDataList = new List<CardJokerData>();         //本期卡牌活动涉及到的所有的万能卡片信息 已过期的/已经用完的卡会被移除
        //使用本轮次中序号为0的卡册配置构建一个假的卡册数据，里面所有卡和奖励都为已收集状态，用于典藏版开启时切换查看普通版本的卡册
        private CardAlbumData _fakeNormalData;
        
        //获取本卡册轮次对应的配置表数据
        public EventCardRound GetConfig()
        {
            return Game.Manager.configMan.GetEventCardRoundConfig(CardRoundId);
        }
        
        //needFake 是否需要假的卡册数据 默认false
        public CardAlbumData TryGetCardAlbumData(bool needFake = false)
        {
            return !needFake ? _curOpenAlbumData : _fakeNormalData;
        }

        public int GetCurOpenAlbumIndex()
        {
            return _curOpenAlbumIndex;
        }

        //检查当前轮次中是否还有下一个卡册id
        public bool CheckHasNextAlbum()
        {
            var include = GetConfig()?.IncludeAlbumId;
            return include != null && _curOpenAlbumIndex + 1 < include.Count;
        }
        
        //检查当前正在开启的卡册是否已领取全部奖励
        public bool CheckCollectAllReward()
        {
            return TryGetCardAlbumData()?.IsRecAlbumReward ?? false;
        }
        
        //检查当前正在开启的卡册是否为典藏版
        public bool CheckIsRestartAlbum()
        {
            return TryGetCardAlbumData()?.GetConfig()?.IsPremium ?? false;
        }

        //尝试重置卡册数据
        public void TryRestartAlbum()
        {
            if (!CheckHasNextAlbum())
                return;
            var targetIndex = _curOpenAlbumIndex + 1;
            if (GetConfig().IncludeAlbumId.TryGetByIndex(targetIndex, out var cardAlbumId))
            {
                if (_InitCurOpenAlbumData(cardAlbumId, true, true))
                {
                    //在重置成功后 index++前 打点
                    var from = Game.Manager.cardMan.GetCardActivity()?.From ?? 0;
                    DataTracker.TrackCardAlbumRestart(CardActId, from, _curOpenAlbumData.CardAlbumId, _curOpenAlbumData.CardLimitTempId, 
                        _curOpenAlbumData.StartTotalIAP, GetCurRoundNum());
                    InitCurOpenAlbumIndex(targetIndex);
                }
            }
        }
        
        //检查当前轮次是否已经集齐所有普通卡
        public bool CheckIsCollectAllNormalCard()
        {
            if (_curOpenAlbumData == null)
                return false;
            foreach (var cardData in _curOpenAlbumData.GetAllCardDataMap().Values)
            {
                //金卡不检查
                if (cardData.GetConfig().IsGold)
                    continue;
                //非金卡如果没有拥有或者拥有数量不足1 则说明没有此卡片
                if (!cardData.IsOwn || cardData.OwnCount < 1)
                    return false;
            }
            return true;
        }
        
        //检查当前轮次是否已经集齐所有卡片(白卡+金卡)
        public bool CheckIsCollectAllCard()
        {
            if (_curOpenAlbumData == null)
                return false;
            foreach (var groupData in _curOpenAlbumData.GetAllGroupDataMap().Values)
            {
                if (!groupData.IsCollectAll)
                    return false;
            }
            return true;
        }
        
        //检查当前轮次是否有任意重复的普通卡
        public bool CheckHasRepeatNormalCard()
        {
            if (_curOpenAlbumData == null)
                return false;
            foreach (var cardData in _curOpenAlbumData.GetAllCardDataMap().Values)
            {
                //金卡不检查
                if (cardData.GetConfig().IsGold)
                    continue;
                //非金卡如果拥有且拥有数量大于1 则说明此卡片重复
                if (cardData.IsOwn && cardData.OwnCount > 1)
                    return true;
            }
            return false;
        }

        #region 数据初始化
        
        public void InitCurOpenAlbumIndex(int index)
        {
            _curOpenAlbumIndex = index;
        }
        
        //isFirstOpen 控制是否是第一次初始化 进而决定是否随机随机本次卡册活动要使用的模板id和其中各个卡片的LimitId
        public void InitRoundConfigData(int cardActId, bool isFirstOpen = false)
        {
            var configMan = Game.Manager.configMan;
            CardActId = cardActId;
            //根据配置构造数据
            var eventTimeConf = configMan.GetEventTimeConfig(CardActId);
            var roundConf = configMan.GetEventCardRoundConfig(eventTimeConf?.EventParam ?? 0);
            if (roundConf == null) return;
            CardRoundId = roundConf.Id;
            //初始化当前开启的卡册数据 
            if (roundConf.IncludeAlbumId.TryGetByIndex(_curOpenAlbumIndex, out var cardAlbumId))
            {
                _InitCurOpenAlbumData(cardAlbumId, isFirstOpen, false);
            }
            //初始化卡片星星兑换功能相关数据
            _InitStarExchangeData();
        }

        //初始化当前开启的卡册数据 
        private bool _InitCurOpenAlbumData(int cardAlbumId, bool isFirstOpen = false, bool needStarExchange = false)
        {
            var albumConf = Game.Manager.configMan.GetEventCardAlbumConfig(cardAlbumId);
            if (albumConf == null) 
                return false;
            //在创建新的卡册数据之前尝试将目前剩余的重复卡(如果有)转化为卡片星星固定库存
            if (needStarExchange)
                _TryConvertFixedStarNum();
            //创建当前卡册数据
            _curOpenAlbumData = new CardAlbumData()
            {
                BelongRoundId = CardRoundId
            };
            _curOpenAlbumData.InitConfigData(albumConf, isFirstOpen);
            //尝试创建假的卡册数据 一旦有了就不再重复创建
            _TryGenFakeAlbumData(isFirstOpen);
            return true;
        }
        
        //根据存档初始化数据
        public void SetData(CardRoundInfo info)
        {
            if (info.CurOpenAlbumInfo != null)
            {
                //根据存档设置卡册数据
                _curOpenAlbumData?.SetData(info.CurOpenAlbumInfo);
            }
            if (info.CardJokerInfoList != null)
            {
                //根据存档设置万能卡数据
                _SetDataCardJoker(info.CardJokerInfoList);
            }
            if (info.StarExchangeInfo != null)
            {
                //根据存档设置卡片星星兑换相关数据
                _SetDataStarExchange(info.StarExchangeInfo);
            }
        }

        public CardRoundInfo FillData()
        {
            var data = new CardRoundInfo();
            data.CurOpenAlbumIndex = _curOpenAlbumIndex;
            //设置卡册相关的存档数据
            if (_curOpenAlbumData != null)
            {
                data.CurOpenAlbumInfo = _curOpenAlbumData.FillData();
            }
            //设置万能卡相关信息
            foreach (var cardJokerData in _cardJokerDataList)
            {
                data.CardJokerInfoList.Add(cardJokerData.FillData());
            }
            //设置卡片星星兑换相关信息
            var exchangeInfo = new CardStarExchangeInfo();
            data.StarExchangeInfo = exchangeInfo;
            exchangeInfo.TotalFixedStarNum = _totalFixedStarNum;
            exchangeInfo.TotalUsedStarNum = _totalUsedStarNum;
            foreach (var cardStarExchangeData in _starExchangeDataList)
            {
                exchangeInfo.StarExchangeCdList.Add(cardStarExchangeData.NextCanExchangeTime);
            }
            return data;
        }

        private void _SetDataCardJoker(IEnumerable<CardJokerInfo> cardJokerInfos)
        {
            //读取存档数据时用CheckIsExpire()筛选一下 直接把过期的万能卡pass掉 不进入list中
            foreach (var cardJokerInfo in cardJokerInfos)
            {
                var cardJokerData = new CardJokerData()
                {
                    CardJokerId = cardJokerInfo.JokerId,
                    BelongRoundId = CardRoundId,
                };
                cardJokerData.SetData(cardJokerInfo);
                var config = cardJokerData.GetConfig();
                if (config != null)
                {
                    cardJokerData.IsGoldCard = config.IsOnlyNormal ? 0 : 1;
                    _cardJokerDataList.Add(cardJokerData);
                }
            }
            //排序
            _cardJokerDataList.Sort(_JokerSortFunc);
        }

        //尝试构建假的全收集的卡册数据
        private void _TryGenFakeAlbumData(bool isFirstOpen = false)
        {
            //如果当前卡册不是典藏版 则不构建假的数据
            if (!TryGetCardAlbumData().GetConfig().IsPremium)
                return;
            //生成假的卡册数据时使用index+1生成,目的为保持当前普通版和典藏版的卡组id一致
            var index = isFirstOpen ? _curOpenAlbumIndex + 1 : _curOpenAlbumIndex;
            if (!GetConfig().IncludeAlbumId.TryGetByIndex(index, out var cardAlbumId))
                return;
            var albumConf = Game.Manager.configMan.GetEventCardAlbumConfig(cardAlbumId);
            if (albumConf == null) 
                return;
            //创建假的卡册数据
            _fakeNormalData = new CardAlbumData()
            {
                BelongRoundId = CardRoundId
            };
            _fakeNormalData.InitFakeAlbumData(albumConf);
        }
        
        #endregion
        
        #region 万能卡相关方法

        private int _curShowJokerIndex = 0;
        private int _processTimes = 0;    //记录递进次数 保证所有万能卡每次都会被玩家依次看到
        private bool _checkUseful = false;  //当前展示万能卡的流程是否只展示值得被使用的万能卡

        //用户关闭一张万能卡的操作界面后 递进index到下一张万能卡 循环index
        //返回是否是最后一张万能卡
        public bool ProcessNextJokerIndex()
        {
            //递进次数已到达目前万能卡总张数 则停止递进
            if (_processTimes >= _cardJokerDataList.Count - 1)
            {
                _processTimes = 0;
                _curShowJokerIndex = 0;
                _checkUseful = false;   //停止递进时置为false
                return true;
            }
            //递进次数+1
            _processTimes++;
            //index循环递增
            if (_curShowJokerIndex >= _cardJokerDataList.Count - 1)
            {
                //当前是最后一张时 index归0
                _curShowJokerIndex = 0;
            }
            else
            {
                _curShowJokerIndex ++;
            }
            if (_checkUseful)
            {
                var isUseful = CheckIsUsefulJokerData(GetCurIndexJokerData());
                //如果当前卡片值得被使用 则显示
                if (isUseful)
                {
                    return false;
                }
                //如果当前卡片不值得被使用 则继续向后查找 直到找到或者递进次数达到上限
                else
                {
                    return ProcessNextJokerIndex();
                }
            }
            return false;
        }

        public void SetCurShowJokerIndex(int index, bool checkUseful = false)
        {
            if (index >= 0 && index < _cardJokerDataList.Count)
                _curShowJokerIndex = index;
            _checkUseful = checkUseful;
        }
        
        //获取第一张万能卡数据 没有则返回空
        public CardJokerData GetFirstJokerData()
        {
            return _cardJokerDataList?.FirstOrDefault();
        }
        
        //获取当前Index对应的万能卡数据
        public CardJokerData GetCurIndexJokerData()
        {
            return GetJokerData(_curShowJokerIndex);
        }
        
        //获取指定index序号的万能卡数据
        public CardJokerData GetJokerData(int index)
        {
            return _cardJokerDataList.TryGetByIndex(index, out var jokerData) ? jokerData : null;
        }
        
        public void OnGetCardJoker(int cardJokerId, int jokerNum, long actEndTime)
        {
            var curTime = Game.Instance.GetTimestampSeconds();
            for (var i = 0; i < jokerNum; i++)
            {
                var cardJokerData = new CardJokerData()
                {
                    CardJokerId = cardJokerId,
                    BelongRoundId = CardRoundId,
                };
                var config = cardJokerData.GetConfig();
                if (config != null)
                {
                    cardJokerData.IsGoldCard = config.IsOnlyNormal ? 0 : 1;
                    var configExpireTime = curTime + config.Lifetime;
                    cardJokerData.ExpireTs = configExpireTime < actEndTime ? configExpireTime : actEndTime;
                    _cardJokerDataList.Add(cardJokerData);
                }
            }
            _cardJokerDataList.Sort(_JokerSortFunc);
            SetCurShowJokerIndex(_cardJokerDataList.Count - 1);
        }

        private int _JokerSortFunc(CardJokerData a, CardJokerData b)
        {
            var compareA = a.ExpireTs.CompareTo(b.ExpireTs);
            if (compareA != 0)
                return compareA;
            var compareB = a.IsGoldCard.CompareTo(b.IsGoldCard);
            return compareB != 0 ? compareB : a.CardJokerId.CompareTo(b.CardJokerId);
        }

        //每秒检查是否有过期的万能卡 如果有的话 强制弹出选卡界面
        public bool CheckCardJokerExpire()
        {
            foreach (var jokerData in _cardJokerDataList)
            {
                if (jokerData.CheckIsExpire())
                {
                    return true;
                }
            }
            return false;
        }

        //检查目前所有万能卡中是否有过期了的且值得被使用的卡 如果没有则返回
        //值得被使用的卡定义为：普通万能卡是否可以兑换目前轮次还没有的白卡；金色万能卡是否可以兑换目前轮次还没有的任意卡，如果都没有则认为不值得被使用
        public bool CheckHasExpireUsefulJokerCard(out int firstUsefulIndex)
        {
            firstUsefulIndex = 0;
            var index = 0;
            foreach (var jokerData in _cardJokerDataList)
            {
                if (jokerData.CheckIsExpire() && CheckIsUsefulJokerData(jokerData))
                {
                    firstUsefulIndex = index;
                    return true;
                }
                index++;
            }
            return false;
        }

        //检查指定卡是否值得被使用
        public bool CheckIsUsefulJokerData(CardJokerData jokerData)
        {
            if (jokerData == null) return false;
            var isGold = jokerData.IsGoldCard == 1;
            if (isGold)
                return !CheckIsCollectAllCard();
            else
                return !CheckIsCollectAllNormalCard();
        }

        //根据万能卡数据index来删除卡，因为可能会有多张一样id的卡
        public void OnUseCardJokerSucc()
        {
            _cardJokerDataList?.RemoveAt(_curShowJokerIndex);
            //每次用完后 都归0 简单处理 
            _processTimes = 0;
            _curShowJokerIndex = 0;
            //万能卡使用成功后 若当前只展示万值得被使用的卡 则递进一下index
            if (_checkUseful)
                ProcessNextJokerIndex();
        }

        //活动结束尝试结算所有未使用的万能卡
        public void TryClearJokerCard()
        {
            var hasExpire = false;
            foreach (var jokerData in _cardJokerDataList)
            {
                var conf = jokerData.GetConfig();
                //获取转换成的奖励
                var r = conf?.ExpireItem.ConvertToRewardConfig();
                if (r == null) continue;
                //发奖励
                var expireReward = Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.expire);
                UIFlyUtility.FlyReward(expireReward, UnityEngine.Vector3.zero);
                //万能卡过期时打点
                DataTracker.expire.Track(jokerData.CardJokerId, 1);
                hasExpire = true;
            }
            //弹提示
            if (hasExpire) Game.Manager.commonTipsMan.ShowPopTips(Toast.EventEndTrans);
            //清空所有万能卡
            _cardJokerDataList.Clear();
        }

        #endregion

        #region 打点相关方法

        //打点时使用 获取当前是活动期间的第几轮卡册(从1开始)
        public int GetCurRoundNum()
        {
            return GetCurOpenAlbumIndex() + 1;
        }

        //打点时使用 获取当前累计完成了几轮卡册(从0开始)
        public int GetCurFinishNum()
        {
            var albumData = TryGetCardAlbumData();
            if (albumData == null)
                return 0;
            var finishNum = 0;
            var curIndex = GetCurOpenAlbumIndex();
            if (curIndex > 0)   //大于0说明重开过
                finishNum = albumData.IsRecAlbumReward ? curIndex + 1 : curIndex;
            else
                finishNum = albumData.IsRecAlbumReward ? 1 : 0;
            return finishNum;
        }

        #endregion

        #region 卡片兑换相关方法
        
        //卡片兑换文档：https://centurygames.yuque.com/ywqzgn/ne0fhm/eopuffcrlitdttbt#mLlgD

        //记录本次卡册活动中玩家累计转化获得的星星固定库存
        private int _totalFixedStarNum;
        //记录本次卡册活动中玩家通过兑换累计消耗的星星数量
        private int _totalUsedStarNum;
        //本次集卡活动星星兑换功能中的兑换条目数据List
        private List<CardStarExchangeData> _starExchangeDataList = new List<CardStarExchangeData>();

        public int GetTotalFixedStarNum()
        {
            return _totalFixedStarNum;
        }
        
        public int GetTotalUsedStarNum()
        {
            return _totalUsedStarNum;
        }
        
        public List<CardStarExchangeData> GetAllStarExchangeData()
        {
            return _starExchangeDataList;
        }

        public CardStarExchangeData GetStarExchangeData(int exchangeId)
        {
            return _starExchangeDataList.FindEx(x => x.StarExchangeId == exchangeId);
        }
        
        public bool IsOpenStarExchange()
        {
            //配置决定在第几轮中开启兑换 值≤0时不开启
            var openIndex = GetConfig()?.StartExchange ?? 0;
            return openIndex > 0 && GetCurOpenAlbumIndex() + 1 >= openIndex;
        }

        private void _InitStarExchangeData()
        {
            _totalFixedStarNum = 0;
            _totalUsedStarNum = 0;
            _starExchangeDataList.Clear();
            var exchangeIdList = GetConfig()?.IncludeExchangeId;
            if (exchangeIdList == null)
                return;
            foreach (var id in exchangeIdList)
            {
                var data = new CardStarExchangeData()
                {
                    StarExchangeId = id,
                    NextCanExchangeTime = 0,
                };
                _starExchangeDataList.Add(data);
            }
        }

        private void _SetDataStarExchange(CardStarExchangeInfo info)
        {
            _totalFixedStarNum = info.TotalFixedStarNum;
            _totalUsedStarNum = info.TotalUsedStarNum;
            int index = 0;
            foreach (var cdTime in info.StarExchangeCdList)
            {
                if (_starExchangeDataList.TryGetByIndex(index, out var data))
                {
                    data.NextCanExchangeTime = cdTime;
                }
                index++;
            }
        }

        //卡册重开时尝试将目前剩余的重复卡转化为卡片星星固定库存
        private void _TryConvertFixedStarNum()
        {
            if (!IsOpenStarExchange())
                return;
            var cardDataMap = TryGetCardAlbumData()?.GetAllCardDataMap();
            if (cardDataMap == null) return;
            var totalVirtualStarNum = 0;
            foreach (var cardData in cardDataMap.Values)
            {
                var virtualNum = cardData.GetVirtualStarNum();
                totalVirtualStarNum += virtualNum;
                if (virtualNum > 0)
                {
                    cardData.ReduceCardCount(cardData.OwnCount - 1, 1, 0);
                }
            }
            //增加固定库存
            _AddTotalFixedStarNum(totalVirtualStarNum);
        }
        
        //卡册重开以及星星结余时会增加固定库存
        private void _AddTotalFixedStarNum(int starNum)
        {
            if (starNum < 0)
            {
                DebugEx.FormatError("[CardData._AddTotalFixedStarNum] error : totalNum = {0}, addNum = {1}", _totalFixedStarNum, starNum);
                return;
            }
            _totalFixedStarNum += starNum;
            DebugEx.FormatInfo("[CardData._AddTotalFixedStarNum] : totalNum = {0}, addNum = {1}, totalUsedNum = {2}", _totalFixedStarNum, starNum, _totalUsedStarNum);
        }
        
        //星星兑换奖励时会减少固定库存(如果有)
        private void _ReduceTotalFixedStarNum(int starNum)
        {
            if (starNum < 0)
            {
                DebugEx.FormatError("[CardData._ReduceTotalFixedStarNum] error : totalNum = {0}, reduceNum = {1}", _totalFixedStarNum, starNum);
                return;
            }

            if (_totalFixedStarNum < starNum)
            {
                DebugEx.FormatError("[CardData._ReduceTotalFixedStarNum] error : totalNum = {0}, reduceNum = {1}", _totalFixedStarNum, starNum);
                _totalFixedStarNum = 0;
            }
            else
            {
                _totalFixedStarNum -= starNum;
                DebugEx.FormatInfo("[CardData._ReduceTotalFixedStarNum] : totalNum = {0}, reduceNum = {1}, totalUsedNum = {2}", _totalFixedStarNum, starNum, _totalUsedStarNum);
            }
        }
        
        //当卡片星星兑换奖励成功时刷新相关数据 内部业务逻辑使用
        public void OnCardStarExchangeSuccess(int exchangeId, int useTotalStarNum, int useFixedStarNum, int addFixedStarNum)
        {
            //刷新下次可兑换时间
            var data = GetStarExchangeData(exchangeId);
            data?.RefreshCd();
            //累计消耗的星星总数
            _totalUsedStarNum += useTotalStarNum;
            //减少固定库存值
            _ReduceTotalFixedStarNum(useFixedStarNum);
            //增加固定库存值(如果有的话 一般来自于消耗卡片时的溢出值)
            if (addFixedStarNum > 0)
                _AddTotalFixedStarNum(addFixedStarNum);
        }

        public void DebugChangeFixedStarNum(int fixedStarNum, bool isAdd)
        {
            if (isAdd)
            {
                _AddTotalFixedStarNum(fixedStarNum);
            }
            else
            {
                _ReduceTotalFixedStarNum(fixedStarNum);
            }
        }

        public void DebugResetExchangeCd()
        {
            foreach (var exchangeData in _starExchangeDataList)
            {
                exchangeData.NextCanExchangeTime = 0;
            }
        }

        #endregion
    }
}

