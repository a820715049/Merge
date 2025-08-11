/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡册相关数据 
 * @Date: 2024-10-18 10:10:29
 */

using System.Collections.Generic;
using fat.rawdata;
using EL;
using fat.gamekitdata;

namespace FAT
{
    //卡册相关数据
    public class CardAlbumData
    {
        public int CardAlbumId;         //卡册模板id(EventCardAlbum.id)
        public int BelongRoundId;       //卡册所属集卡轮次id(EventCardRound.id)
        public int CardLimitTempId;     //本卡册针对卡片限制条件信息(CardLimit)要统一使用的模板Id
        public ulong TotalCostEnergy;   //活动期间累计消耗的能量值
        public int StartTotalIAP = -1;  //活动开始时该玩家已经累计充值过的金额(以服务器为准单位美分) 默认初始为-1 代表还没有收到服务器发来的IAP值
        public bool IsRecAlbumReward = false;  //是否已领取整个卡册集齐后的最终奖励
        private Dictionary<int, CardGroupData> _cardGroupDataMap = new Dictionary<int, CardGroupData>();  //本卡册涉及到的所有的卡组信息 key groupId
        private Dictionary<int, CardData> _cardDataMap = new Dictionary<int, CardData>();   //本卡册涉及到的所有的卡片信息 key cardId
        
        //获取本卡册对应的配置表数据
        public EventCardAlbum GetConfig()
        {
            return Game.Manager.configMan.GetEventCardAlbumConfig(CardAlbumId);
        }

        //传入卡组id 返回本卡册中对应的卡组数据 可能为空
        public CardGroupData TryGetCardGroupData(int groupId)
        {
            return _cardGroupDataMap.TryGetValue(groupId, out var groupData) ? groupData : null;
        }

        public Dictionary<int, CardGroupData> GetAllGroupDataMap()
        {
            return _cardGroupDataMap;
        }
        
        public Dictionary<int, CardData> GetAllCardDataMap()
        {
            return _cardDataMap;
        }

        //传入卡牌id 返回本卡册中对应的卡牌数据 可能为空
        public CardData TryGetCardData(int cardId)
        {
            return _cardDataMap.TryGetValue(cardId, out var cardData) ? cardData : null;
        }

        #region 数据初始化
        
        //isFirstOpen 控制是否是第一次初始化 进而决定是否随机随机本次卡册活动要使用的模板id和其中各个卡片的LimitId
        public void InitConfigData(EventCardAlbum albumConf, bool isFirstOpen = false)
        {
            var configMan = Game.Manager.configMan;
            //根据配置数据构造本卡册中的相关卡组卡片数据
            if (albumConf == null) return;
            CardAlbumId = albumConf.Id;
            _cardGroupDataMap.Clear();
            _cardDataMap.Clear();
            foreach (var groupId in albumConf.GroupInfo)
            {
                var groupConf = configMan.GetCardGroupConfig(groupId);
                //如果groupId对应的配置没有或者没有配卡片 则不初始化
                if (groupConf == null || groupConf.CardInfo.Count <= 0) continue;
                var groupData = new CardGroupData()
                {
                    CardGroupId = groupId,
                    BelongAlbumId = CardAlbumId
                };
                _cardGroupDataMap.Add(groupId, groupData);
                foreach (var cardId in groupConf.CardInfo)
                {
                    var cardData = new CardData()
                    {
                        CardId = cardId,
                        BelongAlbumId = CardAlbumId,
                        BelongGroupId = groupId
                    };
                    _cardDataMap.Add(cardId, cardData);
                }
            }
            if (isFirstOpen)
            {
                //第一次开启时随机相关信息
                _RandomCardLimitTempId();
                _RandomCardLimitId();
                //第一次开启时设置此刻玩家的累充金额
                TryInitStartTotalIAP();
            }
        }

        //根据存档初始化数据 11.0版本新加数据结构CardAlbumInfo 不再使用CardActivityInfo
        public void SetData(CardAlbumInfo info)
        {
            CardLimitTempId = info.CardLimitTempId;
            TotalCostEnergy = info.TotalCostEnergy;
            StartTotalIAP = info.StartTotalIAP;
            IsRecAlbumReward = info.IsRecAlbumReward;
            //设置卡组相关的存档数据
            foreach (var id in info.RecRewardGroupIds)
            {
                if (_cardGroupDataMap.TryGetValue(id, out var groupData))
                {
                    groupData.IsRecReward = true;
                }
            }
            //设置卡包相关的存档数据
            foreach (var kv in info.CardInfoMap)
            {
                if (_cardDataMap.TryGetValue(kv.Key, out var cardData))
                {
                    cardData.SetData(kv.Value);
                    cardData.RefreshPassInfo(CardLimitTempId);
                }
            }
            //检查卡组集齐状态
            CheckIsCollectAll();
        }

        public CardAlbumInfo FillData()
        {
            var data = new CardAlbumInfo();
            data.CardLimitTempId = CardLimitTempId;
            data.TotalCostEnergy = TotalCostEnergy;
            data.StartTotalIAP = StartTotalIAP;
            data.IsRecAlbumReward = IsRecAlbumReward;
            //设置卡组相关的存档数据
            foreach (var groupData in _cardGroupDataMap.Values)
            {
                if (groupData.IsRecReward)
                {
                    data.RecRewardGroupIds.Add(groupData.CardGroupId);
                }
            }
            //设置卡包相关的存档数据
            foreach (var cardData in _cardDataMap.Values)
            {
                data.CardInfoMap[cardData.CardId] = cardData.FillData();
            }
            return data;
        }

        //等概率随机本次卡册活动要使用的模板id, 只应该在活动第一次开启时调用
        private void _RandomCardLimitTempId()
        {
            var config = GetConfig();
            CardLimitTempId = config.TempId.RandomChooseByWeight();
        }

        //等概率随机本次卡册活动中各个卡片的LimitId, 只应该在活动第一次开启时调用
        private void _RandomCardLimitId()
        {
            //创建一个table 用于存储目前已经使用了的LimitId
            var usedLimitInfoIdList = new List<int>();
            //用于配了多个LimitId的卡片随机最终限制信息
            var randomList = new List<int>();
            foreach (var cardData in _cardDataMap.Values)
            {
                cardData.RandomLimitId(usedLimitInfoIdList, randomList);
                cardData.RefreshPassInfo(CardLimitTempId);
            }
            usedLimitInfoIdList.Clear();
        }

        //尝试构建假的全收集的卡册数据
        public void InitFakeAlbumData(EventCardAlbum albumConf)
        {
            InitConfigData(albumConf);
            CardLimitTempId = 0;
            TotalCostEnergy = 0;
            StartTotalIAP = 0;
            IsRecAlbumReward = true;    //默认奖励全领取
            //设置卡册相关数据
            foreach (var groupData in _cardGroupDataMap.Values)
            {
                //默认卡组全集齐 奖励全领取
                groupData.IsCollectAll = true;
                groupData.IsRecReward = true;
            }
            //设置卡包相关数据
            foreach (var cardData in _cardDataMap.Values)
            {
                cardData.LimitId = 0;
                cardData.EnergyPassNum = -1;
                cardData.PayPassNum = -1;
                cardData.MinEryPassNum = -1;
                cardData.DayPassNum = -1;
                cardData.IsOwn = true;
                cardData.IsSee = true;
                cardData.OwnCount = 1;
            }
        }

        #endregion

        #region 对外基本方法
        
        public void TryInitStartTotalIAP()
        {
            if (StartTotalIAP != -1) return; //不为-1说明已经设置过了
            var iap = Game.Manager.iap;
            StartTotalIAP = iap.DataReady ? iap.TotalIAPServer : -1;
        }

        //消耗能量时 累计消耗的能量
        public void SetTotalCostEnergy(int energy)
        {
            TotalCostEnergy += (ulong)energy;
        }
        
        //检查卡组是否集齐
        public void CheckIsCollectAll()
        {
            foreach (var groupData in _cardGroupDataMap.Values)
            {
                var config = groupData.GetConfig();
                if (config == null || config.CardInfo.Count <= 0)
                {
                    groupData.IsCollectAll = false;
                    continue;
                }
                bool isCollectAll = true;
                foreach (var cardId in config.CardInfo)
                {
                    if (_cardDataMap.TryGetValue(cardId, out var cardData) && !cardData.IsOwn)
                    {
                        isCollectAll = false;
                        break;
                    }
                }
                groupData.IsCollectAll = isCollectAll;
            }
        }

        //获取指定卡组的收集进度
        public void GetCollectProgress(int groupId, out int ownCount, out int allCount)
        {
            ownCount = 0;
            allCount = 0;
            if (!_cardGroupDataMap.TryGetValue(groupId, out var groupData)) return;
            var groupConfig = groupData.GetConfig();
            if (groupConfig == null || groupConfig.CardInfo.Count <= 0) return;
            foreach (var cardId in groupConfig.CardInfo)
            {
                allCount++;
                if (_cardDataMap.TryGetValue(cardId, out var cardData) && cardData.IsOwn)
                {
                    ownCount++;
                }
            }
        }
        
        //获取所有卡组的收集进度
        public void GetAllCollectProgress(out int ownCount, out int allCount)
        {
            ownCount = 0;
            allCount = _cardDataMap.Count;
            foreach (var cardData in _cardDataMap.Values)
            {
                if (cardData.IsOwn)
                    ownCount++;
            }
        }
        
        //获取指定卡组有无新卡
        public int CheckNewCardCount(int groupId)
        {
            if (!_cardGroupDataMap.TryGetValue(groupId, out var groupData)) return 0;
            var groupConfig = groupData.GetConfig();
            if (groupConfig == null || groupConfig.CardInfo.Count <= 0) return 0;
            int newCount = 0;
            foreach (var cardId in groupConfig.CardInfo)
            {
                if (_cardDataMap.TryGetValue(cardId, out var cardData) && cardData.CheckIsNew())
                {
                    newCount++;
                }
            }
            return newCount;
        }
        
        //设置指定卡组中所有卡片为已查看
        public void SetGroupCardSee(int groupId)
        {
            if (!_cardGroupDataMap.TryGetValue(groupId, out var groupData)) return;
            var groupConfig = groupData.GetConfig();
            if (groupConfig == null || groupConfig.CardInfo.Count <= 0) return;
            foreach (var cardId in groupConfig.CardInfo)
            {
                if (_cardDataMap.TryGetValue(cardId, out var cardData) && cardData.CheckIsNew())
                {
                    cardData.SetCardSee();
                }
            }
        }

        //设置指定卡片为已查看
        public void SetCardSee(int cardId)
        {
            if (_cardDataMap.TryGetValue(cardId, out var cardData) && cardData.CheckIsNew())
            {
                cardData.SetCardSee();
            }
        }

        //获取当前已完成卡组的数量
        public void CheckFinishGroupNum(out int finishGroupNum)
        {
            finishGroupNum = 0;
            foreach (var groupData in _cardGroupDataMap.Values)
            {
                if (groupData.IsCollectAll)
                    finishGroupNum++;
            }
        }
        
        //获取本卡册中所有卡片累加起来的虚拟星星库存
        public int GetTotalVirtualStarNum()
        {
            var totalNum = 0;
            foreach (var cardDara in _cardDataMap.Values)
            {
                totalNum += cardDara.GetVirtualStarNum();
            }
            return totalNum;
        }
        
        #endregion
        
    }
}

