/*
 * @Author: tang.yan
 * @Description: 商城数据类定义 
 * @Date: 2023-11-08 11:11:34
 */

using System.Collections.Generic;
using fat.rawdata;
using Config;
using EL;
using fat.gamekitdata;
using FAT.Merge;
using System;
using Cysharp.Text;

namespace FAT
{
    //商店页签类型
    public enum ShopTabType
    {
        None = 0,   //空
        Gem = 1,    //钻石页签(所有商店中共用该页签的数据)
        Energy = 2, //能量页签(不同商店中该页签数据不同)
        Chess = 3,  //棋子页签(不同商店中该页签数据不同)
    }
    
    //商店数据基类
    public class ShopBaseData
    {
        //此商店绑定的棋盘id(游戏中会根据不同的棋盘id生成对应不同的商店数据 不同棋盘一定对应不同商店)
        public int BindBoardId;
        //商店中各个页签对应的数据类
        public Dictionary<ShopTabType, ShopTabBaseData> ShopTabDataMap = new Dictionary<ShopTabType, ShopTabBaseData>();
        //商店是否解锁
        public bool IsUnlock => _CheckIsUnlock();

        //TODO 后续开多个棋盘的时候 需要将Gem页签数据设为只有主棋盘才会初始化 
        //TODO Gem商店在游戏中是各个棋盘通用的 但结构上还是归属于主棋盘，其他棋盘的商店界面如果要显示的话 就自己单独拿一下主棋盘Gem数据来展示
        public ShopBaseData(int bindBoardId)
        {
            BindBoardId = bindBoardId;
            ShopTabDataMap.Add(ShopTabType.Gem, new ShopTabGemData(BindBoardId));
            ShopTabDataMap.Add(ShopTabType.Energy, new ShopTabEnergyData(BindBoardId));
            ShopTabDataMap.Add(ShopTabType.Chess, new ShopTabChessData(BindBoardId));
        }

        public void SetData(ShopData shopData)
        {
            BindBoardId = shopData.BoardId;
            foreach (var tabData in ShopTabDataMap.Values)
            {
                tabData.SetData(shopData);
            }
        }

        public void FillData(ShopData shopData)
        {
            shopData.BoardId = BindBoardId;
            foreach (var tabData in ShopTabDataMap.Values)
            {
                tabData.FillData(shopData);
            }
        }

        private bool _CheckIsUnlock()
        {
            bool isUnlock = false;
            foreach (var data in ShopTabDataMap.Values)
            {
                isUnlock = isUnlock || data.IsUnlock;
            }
            return isUnlock;
        }
    }

    //商城页签数据基类
    public class ShopTabBaseData
    {
        public int BindBoardId;
        public ShopTabType TabType;
        public bool IsUnlock => _CheckIsUnlock();

        protected ShopTabBaseData(int bindBoardId)
        {
            BindBoardId = bindBoardId;
        }

        //初始化相关配置
        protected virtual void _PrepareData() { }
        //设置存档数据
        public virtual void SetData(ShopData shopData) { }
        //读取存档数据
        public virtual void FillData(ShopData shopData) { }
        //检查是否解锁
        protected virtual bool _CheckIsUnlock() { return false; }
        //秒级更新 返回值表示是否触发了数据刷新
        public virtual bool OnSecondUpdate() { return false; }
    }

    #region 钻石商店相关

    //钻石商品数据
    public class ShopGemData
    {
        public int GirdId;      //商品对应的格子Id
        public int Sequence;    //商品显示排序权重 越小越靠前
        public int ConfPackId;
        public int PackId => Reward.packId;      //IAP商品packId IAPPack.id
        public int IapId => Reward.iapId;       //IAP商品id IAPProduct.id
        public BonusReward Reward; //奖励信息
        public AssetConfig Image;   //奖励图标信息
        public string Name;     //奖励名称
        public int LabelId;     //折扣标签 默认为0表示不显示标签
    }
    
    public class ShopTabGemData : ShopTabBaseData
    {
        public List<ShopGemData> GemDataList = new List<ShopGemData>();
        public ShopTabGemData(int bindBoardId) : base(bindBoardId)
        {
            _PrepareData();
        }

        protected sealed override void _PrepareData()
        {
            var objMan = Game.Manager.objectMan;
            var configs = Game.Manager.configMan.GetMarketIAPConfig();
            foreach (var config in configs.Values)
            {
                ShopGemData data = new ShopGemData()
                {
                    GirdId = config.Id,
                    Sequence = config.Sequence,
                    Name = config.Name,
                    Image = config.Image.ConvertToAssetConfig(),
                    LabelId = config.Label,
                    ConfPackId = config.PackId,
                    Reward = new()
                };
                data.Reward.RefreshShop(data.GirdId, data.ConfPackId);
                GemDataList.Add(data);
            }
            GemDataList.Sort((a, b) => a.Sequence - b.Sequence);
        }

        protected override bool _CheckIsUnlock()
        {
            return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureShopGem);
        }
    }

    #endregion

    #region 能量商店相关

    //能量商品数据
    public class ShopEnergyData
    {
        public int BelongBoardId; //对应的棋盘Id
        public int GirdId;      //商品对应的格子Id
        public int Sequence;    //商品显示排序权重 越小越靠前
        public int BuyCount;    //本日该商品的购买次数
        public List<int> IncludeGoods = new List<int>();  //包含的递增商品id 按本日购买次数决定用第几个超过配置的个数的话，固定选最后一个
        public MarketCommondity CurSellGoodsConfig;   //当前正在显示出售的商品
        public void OnBuyGoodsSuccess()
        {
            BuyCount++;
            RefreshCurSellGoodsConfig();
        }

        public void ResetBuyCount()
        {
            BuyCount = 0;
            RefreshCurSellGoodsConfig();
        }

        public void RefreshCurSellGoodsConfig()
        {
            int goodsCount = IncludeGoods.Count;
            if (goodsCount <= 0)
                return;
            int index = BuyCount >= goodsCount ? goodsCount - 1 : BuyCount;
            CurSellGoodsConfig = Game.Manager.configMan.GetMarketCommondityConfigById(IncludeGoods[index]);
        }

        public AssetConfig GetCurSellGoodsImage()
        {
            if (CurSellGoodsConfig == null)
                return null;
            if (CurSellGoodsConfig.Image != "")
            {
                return CurSellGoodsConfig.Image.ConvertToAssetConfig();
            }
            else
            {
                var reward = CurSellGoodsConfig.Reward.ConvertToRewardConfig();
                if (reward == null)
                    return null;
                return Game.Manager.rewardMan.GetShopRewardIcon(reward.Id, reward.Count);
            }
        }

        public string GetCurSellGoodsName()
        {
            if (CurSellGoodsConfig == null)
                return "";
            if (CurSellGoodsConfig.Name != "")
            {
                return I18N.Text(CurSellGoodsConfig.Name);
            }
            else
            {
                var reward = CurSellGoodsConfig.Reward.ConvertToRewardConfig();
                if (reward == null)
                    return "";
                return Game.Manager.rewardMan.GetRewardName(reward.Id);
            }
        }
        
        public string GetCurSellGoodsNumStr()
        {
            if (CurSellGoodsConfig == null)
                return "";
            var reward = CurSellGoodsConfig.Reward.ConvertToRewardConfig();
            if (reward == null)
                return "";
            if (reward.Count <= 1)
                return "";
            else
                return reward.Count.ToString();
        }
    }

    public class ShopTabEnergyData : ShopTabBaseData
    {
        public List<ShopEnergyData> EnergyDataList = new List<ShopEnergyData>();
        public long NextRefreshEnergyTs = 0;    //下次刷新的时间 会存档
        
        public ShopTabEnergyData(int bindBoardId) : base(bindBoardId)
        {
            _PrepareData();
        }
        
        protected sealed override void _PrepareData()
        {
            var configs = Game.Manager.configMan.GetMarketIncreaseConfig();
            foreach (var config in configs.Values)
            {
                //只找到和当前棋盘id一致的配置
                if (BindBoardId == config.BoardId)
                {
                    ShopEnergyData data = new ShopEnergyData()
                    {
                        BelongBoardId = BindBoardId,
                        GirdId = config.Id,
                        Sequence = config.Sequence,
                        BuyCount = 0,   //创建时默认为0 后续会读存档来设置
                    };
                    foreach (var id in config.IncludeCommondity)
                    {
                        data.IncludeGoods.Add(id);
                    }
                    data.RefreshCurSellGoodsConfig();
                    EnergyDataList.Add(data);
                }
            }
            EnergyDataList.Sort((a, b) => a.Sequence - b.Sequence);
        }

        public override void SetData(ShopData shopData)
        {
            NextRefreshEnergyTs = shopData.EnergyShopResetTime;
            foreach (var energyShopGoodsData in shopData.EnergyGoodsData)
            {
                foreach (var energyData in EnergyDataList)
                {
                    if (energyShopGoodsData.GirdId == energyData.GirdId)
                    {
                        energyData.BuyCount = energyShopGoodsData.BuyCount;
                        energyData.RefreshCurSellGoodsConfig();
                    }
                }
            }
            _CheckCanRefreshEnergy();
        }

        public override void FillData(ShopData shopData)
        {
            shopData.EnergyShopResetTime = NextRefreshEnergyTs;
            foreach (var energyData in EnergyDataList)
            {
                var energyShopGoodsData = new EnergyShopGoodsData();
                energyShopGoodsData.GirdId = energyData.GirdId;
                energyShopGoodsData.BuyCount = energyData.BuyCount;
                shopData.EnergyGoodsData.Add(energyShopGoodsData);
            }
        }

        protected override bool _CheckIsUnlock()
        {
            return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureShopEnergy);
        }

        public override bool OnSecondUpdate()
        {
            return _CheckCanRefreshEnergy();
        }

        private bool _CheckCanRefreshEnergy()
        {
            var now = Game.Instance.GetTimestampSeconds();
            if(now >= NextRefreshEnergyTs)
            {
                int offsetHour = Game.Manager.configMan.globalConfig.MarketUtcClock;
                NextRefreshEnergyTs = ((now - offsetHour * 3600) / Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay + offsetHour * 3600;
                foreach (var energyData in EnergyDataList)
                {
                    energyData.ResetBuyCount();
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    #endregion

    #region 棋子商店相关

    //权重随机棋子商品数据
    public class ShopChessRandomData
    {
        public int BelongBoardId; //对应的棋盘Id
        public int GirdId;      //商品对应的格子Id
        public int Sequence;    //商品显示排序权重 越小越靠前
        public int BuyCount;    //该商品目前的购买次数 刷新后该次数重置
        public int Storage;     //一次刷新期间 该格子上商品的最大库存
        public bool IsHighlight;  //该格子边框是否高亮
        public List<(int, int)> RandomGoodsList = new List<(int, int)>();   //商品随机列表 每次刷新时会根据权重随机一个售卖
        public MarketCommondity CurSellGoodsConfig;   //当前正在显示出售的商品

        //当前购买次数小于库存时可以购买
        public bool CheckCanBuy()
        {
            return BuyCount < Storage;
        }
        
        public void OnBuyGoodsSuccess()
        {
            BuyCount++;
        }

        public void ResetBuyCount()
        {
            BuyCount = 0;
            RefreshCurSellGoodsConfig();
        }

        public void RefreshCurSellGoodsConfig(int goodsId = 0)
        {
            if (goodsId <= 0)
            {
                var result = RandomGoodsList.RandomChooseByWeight((e) => e.Item2);
                goodsId = result.Item1;
            }
            CurSellGoodsConfig = Game.Manager.configMan.GetMarketCommondityConfigById(goodsId);
        }
        
        public AssetConfig GetCurSellGoodsImage()
        {
            if (CurSellGoodsConfig == null)
                return null;
            if (CurSellGoodsConfig.Image != "")
            {
                return CurSellGoodsConfig.Image.ConvertToAssetConfig();
            }
            else
            {
                var reward = CurSellGoodsConfig.Reward.ConvertToRewardConfig();
                if (reward == null)
                    return null;
                return Game.Manager.rewardMan.GetShopRewardIcon(reward.Id, reward.Count);
            }
        }

        public string GetCurSellGoodsName()
        {
            if (CurSellGoodsConfig == null)
                return "";
            if (CurSellGoodsConfig.Name != "")
            {
                return I18N.Text(CurSellGoodsConfig.Name);
            }
            else
            {
                var reward = CurSellGoodsConfig.Reward.ConvertToRewardConfig();
                if (reward == null)
                    return "";
                return Game.Manager.rewardMan.GetRewardName(reward.Id);
            }
        }

        public int GetStockNum()
        {
            int stock = Storage - BuyCount;
            return stock > 0 ? stock : 0;
        }
    }
    
    //订单随机棋子商品数据
    public class ShopChessOrderData
    {
        public int BelongBoardId; //对应的棋盘Id
        public int GirdId;      //商品对应的格子Id
        public int Sequence;    //商品显示排序权重 越小越靠前
        public int BuyCount;    //该商品目前的购买次数 刷新后该次数重置
        public int MinDifficulty;   //商品目标难度随机区间最小值
        public int MaxDifficulty;   //商品目标难度随机区间最大值
        public int TargetDifficulty;  //商品的目标难度 作为从当前订单列表中选择商品的依据
        public float PriceRate;   //售价难度系数 价格=订单实际难度*PriceRate 结果四舍五入 最小为1
        public int Storage;     //一次刷新期间 该格子上商品的最大库存
        public int CurSellGoodsId;      //当前正在显示出售的商品id (棋子obj basic id)
        public int CurSellGoodsPrice;   //目前出售商品的价格

        //当前购买次数小于库存时可以购买
        public bool CheckCanBuy()
        {
            return BuyCount < Storage;
        }
        
        public void OnBuyGoodsSuccess()
        {
            BuyCount++;
        }

        public void ResetBuyCount()
        {
            BuyCount = 0;
        }

        public AssetConfig GetCurSellGoodsImage()
        {
            return Game.Manager.rewardMan.GetShopRewardIcon(CurSellGoodsId, 1);
        }

        public string GetCurSellGoodsName()
        {
            return Game.Manager.rewardMan.GetRewardName(CurSellGoodsId);
        }

        public int GetStockNum()
        {
            int stock = Storage - BuyCount;
            return stock > 0 ? stock : 0;
        }

        public bool CheckIsNeedInOrder()
        {
            using (ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var allOrderDataList))
            {
                BoardViewWrapper.FillBoardOrder(allOrderDataList);
                foreach (var orderData in allOrderDataList)
                {
                    foreach (var itemInfo in orderData.Requires)
                    {
                        if (CurSellGoodsId == itemInfo.Id)
                            return true;
                    }
                }
            }
            return false;
        }

        public void RandomTargetDifficulty(Random random)
        {
            TargetDifficulty = random.Next(MinDifficulty, MaxDifficulty + 1);
        }

        //根据规则找出当前格子要出售的商品及其价格
        //resultOrderIdDict key:记录本次随机已经用过的订单id, value:记录商品格子中已经出售了的棋子id,用过的后续就不再使用了
        public void FindCurSellGoodsId(List<IOrderData> allOrderDataList, Dictionary<int, int> resultOrderIdDict)
        {
            int resultItemId = 0;
            int itemDifficulty = 0;
            var mergeItemDifficultyMan = Game.Manager.mergeItemDifficultyMan;
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var itemProgressList))    //符合条件的棋子合成链上的id list
            {
                foreach (var orderData in allOrderDataList)
                {
                    int orderId = orderData.Id;
                    if (!resultOrderIdDict.ContainsKey(orderId))
                    {
                        //找到该订单中 所有的符合条件的棋子id
                        using (ObjectPool<List<(int, int)>>.GlobalPool.AllocStub(out var targetItemIdList))
                        {
                            foreach (var item in orderData.Requires)
                            {
                                int itemId = item.Id;
                                //找到订单中符合要求的棋子 添加棋子id和对应难度
                                if (itemId > 0)
                                {
                                    int difficulty = mergeItemDifficultyMan.GetItemAvgDifficulty(itemId);
                                    //找不到棋子的平均难度配置时 不加入检查列表
                                    if (difficulty != -1)
                                    {
                                        targetItemIdList.Add((itemId, difficulty));
                                    }
                                }
                            }
                            targetItemIdList.Sort((a, b) => - (a.Item2 - b.Item2));
                            
                            //遍历符合条件的棋子list
                            foreach (var target in targetItemIdList)
                            {
                                int targetItemId = target.Item1;
                                //找到目标棋子对应的合成链  去掉该合成链中等级大于该棋子的 以及 被其他商品格子占用了的 棋子
                                itemProgressList.Clear();
                                var catCfg = Env.Instance.GetCategoryByItem(targetItemId);
                                var progress = catCfg.Progress;
                                int idx = progress.IndexOf(targetItemId);
                                for (int i = 0; i <= idx; i++)
                                {
                                    if (!resultOrderIdDict.ContainsValue(progress[i]))
                                    {
                                        itemProgressList.Add(progress[i]);
                                    }
                                }
                                //如果合成链上有合适的棋子 则寻找和商品格子目标难度,差值的绝对值最小的棋子,作为该格子的售卖棋子
                                if (itemProgressList.Count > 0)
                                {
                                    resultItemId = 0;
                                    itemDifficulty = 0;
                                    int minAbs = 0;
                                    foreach (var itemId in itemProgressList)
                                    {
                                        //获取棋子平均难度 计算出差值的绝对值最小的棋子
                                        int difficulty = mergeItemDifficultyMan.GetItemAvgDifficulty(itemId);
                                        //找不到棋子的平均难度配置时 不进行后续计算
                                        if (difficulty != -1)
                                        {
                                            int curAbs = Math.Abs(TargetDifficulty - difficulty);
                                            if (minAbs == 0 || minAbs > curAbs)
                                            {
                                                minAbs = curAbs;
                                                resultItemId = itemId;
                                                itemDifficulty = difficulty;
                                            }
                                        }
                                    }
                                    //如果找到了最终售卖棋子id 则退出循环 否则就继续
                                    if (resultItemId > 0)
                                    {
                                        resultOrderIdDict.Add(orderId, resultItemId);
                                        break;
                                    }
                                }
                            }
                            
                            if (resultItemId > 0)
                                break;
                        }
                    }
                }
            }
            CurSellGoodsId = resultItemId;
            float price = itemDifficulty * PriceRate / 100;
            price = price < 1 ? 1 : price;  //价格最小为1
            CurSellGoodsPrice = (int)Math.Floor(price + 0.5f);
        }
    }

    public class ShopTabChessData : ShopTabBaseData
    {
        private List<ShopChessRandomData> _chessRandomDataList = new List<ShopChessRandomData>();
        private List<ShopChessOrderData> _chessOrderDataList = new List<ShopChessOrderData>();
        private long _nextRefreshChessTs = 0;    //下次刷新的时间 会存档 单位秒
        private Random _random = new Random();

        public ShopTabChessData(int bindBoardId) : base(bindBoardId)
        {
            _PrepareData();
        }
        
        protected sealed override void _PrepareData()
        {
            //权重随机棋子
            var configs = Game.Manager.configMan.GetMarketWeightConfig();
            foreach (var config in configs.Values)
            {
                //只找到和当前棋盘id一致的配置
                if (BindBoardId == config.BoardId)
                {
                    ShopChessRandomData data = new ShopChessRandomData()
                    {
                        BelongBoardId = BindBoardId,
                        GirdId = config.Id,
                        Sequence = config.Sequence,
                        BuyCount = 0,   //创建时默认为0 后续会读存档来设置
                        Storage = config.Storage,
                        IsHighlight = config.IsHighlight,
                    };
                    foreach (var randomInfo in config.RandomCommondity)
                    {
                        string[] info = randomInfo.Split(':');
                        int goodsId = info.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                        int weight = info.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                        data.RandomGoodsList.Add((goodsId, weight));
                    }
                    data.RefreshCurSellGoodsConfig();
                    _chessRandomDataList.Add(data);
                }
            }
            _chessRandomDataList.Sort((a, b) => a.Sequence - b.Sequence);
            //订单随机棋子
            var orderConfigs = Game.Manager.configMan.GetMarketDifficultyConfig();
            foreach (var config in orderConfigs.Values)
            {
                //只找到和当前棋盘id一致的配置
                if (BindBoardId == config.BoardId)
                {
                    ShopChessOrderData data = new ShopChessOrderData()
                    {
                        BelongBoardId = BindBoardId,
                        GirdId = config.Id,
                        Sequence = config.Sequence,
                        BuyCount = 0,   //创建时默认为0 后续会读存档来设置
                        Storage = config.Storage,
                        PriceRate = config.PriceRate
                    };
                    if (config.TargetDifficulty.Count > 1)
                    {
                        data.MinDifficulty = config.TargetDifficulty[0];
                        data.MaxDifficulty = config.TargetDifficulty[1];
                    }
                    else
                    {
                        data.MinDifficulty = 0;
                        data.MaxDifficulty = 0;
                    }
                    _chessOrderDataList.Add(data);
                }
            }
            _chessOrderDataList.Sort((a, b) => a.Sequence - b.Sequence);
            _RefreshCurSellOrderGoods();
        }

        public override void SetData(ShopData shopData)
        {
            _nextRefreshChessTs = shopData.ChessShopResetTime;
            foreach (var randomChessGoodsData in shopData.RandomChessGoodsData)
            {
                foreach (var chessData in _chessRandomDataList)
                {
                    if (randomChessGoodsData.GirdId == chessData.GirdId)
                    {
                        chessData.BuyCount = randomChessGoodsData.BuyCount;
                        chessData.RefreshCurSellGoodsConfig(randomChessGoodsData.SellGoodsId);
                    }
                }
            }
            foreach (var orderChessGoodsData in shopData.OrderChessGoodsData)
            {
                foreach (var chessData in _chessOrderDataList)
                {
                    if (orderChessGoodsData.GirdId == chessData.GirdId)
                    {
                        chessData.BuyCount = orderChessGoodsData.BuyCount;
                        chessData.CurSellGoodsId = orderChessGoodsData.SellGoodsId;
                        chessData.CurSellGoodsPrice = orderChessGoodsData.SellGoodsPrice;
                    }
                }
            }
            _CheckCanRefreshChess();
        }

        public override void FillData(ShopData shopData)
        {
            shopData.ChessShopResetTime = _nextRefreshChessTs;
            foreach (var chessData in _chessRandomDataList)
            {
                var randomChessGoodsData = new RandomChessShopGoodsData();
                randomChessGoodsData.GirdId = chessData.GirdId;
                randomChessGoodsData.BuyCount = chessData.BuyCount;
                if (chessData.CurSellGoodsConfig != null)
                {
                    randomChessGoodsData.SellGoodsId = chessData.CurSellGoodsConfig.Id;
                }
                shopData.RandomChessGoodsData.Add(randomChessGoodsData);
            }
            foreach (var chessData in _chessOrderDataList)
            {
                var orderChessGoodsData = new OrderChessShopGoodsData();
                orderChessGoodsData.GirdId = chessData.GirdId;
                orderChessGoodsData.BuyCount = chessData.BuyCount;
                orderChessGoodsData.SellGoodsId = chessData.CurSellGoodsId;
                orderChessGoodsData.SellGoodsPrice = chessData.CurSellGoodsPrice;
                shopData.OrderChessGoodsData.Add(orderChessGoodsData);
            }
        }

        public bool GetChessRandomData(int index, out ShopChessRandomData data)
        {
            return _chessRandomDataList.TryGetByIndex(index, out data);
        }
        
        public bool GetChessOrderData(int index, out ShopChessOrderData data)
        {
            return _chessOrderDataList.TryGetByIndex(index, out data);
        }
        
        public ShopChessOrderData GetChessOrderData(int sellItemId)
        {
            foreach (var orderData in _chessOrderDataList)
            {
                if (sellItemId == orderData.CurSellGoodsId)
                    return orderData;
            }
            return null;
        }
        
        //外部主动刷新当前出售的商品
        public void RefreshCurSellGoods()
        {
            //外部主动刷新时只刷订单随机棋子 不刷权重随机棋子  如果是cd时间到了 则都刷
            foreach (var orderData in _chessOrderDataList)
            {
                orderData.ResetBuyCount();
            }
            //刷新订单随机棋子商品
            _RefreshCurSellOrderGoods();
        }

        public long GetRefreshRemainTime()
        {
            var now = Game.Instance.GetTimestampSeconds();
            return _nextRefreshChessTs - now;
        }

        public override bool OnSecondUpdate()
        {
            return _CheckCanRefreshChess();
        }
        
        protected override bool _CheckIsUnlock()
        {
            return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureShopChess);
        }
        
        private bool _CheckCanRefreshChess()
        {
            var now = Game.Instance.GetTimestampSeconds();
            if(IsUnlock && now >= _nextRefreshChessTs)
            {
                //下次刷新时间=当前时间+cd
                _nextRefreshChessTs = now + Game.Manager.configMan.globalConfig.MarketRefresh;
                foreach (var randomData in _chessRandomDataList)
                {
                    randomData.ResetBuyCount();
                }
                foreach (var orderData in _chessOrderDataList)
                {
                    orderData.ResetBuyCount();
                }
                //刷新订单随机棋子商品
                _RefreshCurSellOrderGoods();
                return true;
            }
            else
            {
                return false;
            }
        }

        //刷新当前订单随机各个商品
        private void _RefreshCurSellOrderGoods()
        {
            //用之前根据配置的难度区间随一个目标难度值
            foreach (var chessOrderData in _chessOrderDataList)
            {
                chessOrderData.RandomTargetDifficulty(_random);
            }
            //用之前按预设难度由大到小排序 用于数据查找
            _chessOrderDataList.Sort((a, b) => - (a.TargetDifficulty - b.TargetDifficulty));
            //填充当前各个格子的商品信息
            using (ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var allOrderDataList))
            {
                //todo 这里需要按照绑定的棋盘id来找到对应棋盘上的所有订单  目前默认取的都是主棋盘的订单
                
                //获取当前除了Detector类型的所有订单
                BoardViewWrapper.FillBoardOrderExcept(allOrderDataList, OrderProviderType.Detector.ToIntMask());
                var configMan = Game.Manager.configMan;
                for (int i = allOrderDataList.Count - 1; i >= 0; i--)
                {
                    var orderData = allOrderDataList[i];
                    var ignoreOrderConf = configMan.GetMarketIgnoreConfigByFilter(x => x.SlotId == orderData.Id 
                                                                                && x.OrderType == orderData.ProviderType);
                    //移除忽略的订单
                    if (ignoreOrderConf != null)
                    {
                        allOrderDataList.RemoveAt(i);
                    }
                }
                //剩下的订单按照难度由大到小排序
                allOrderDataList.Sort(_SortByDifficulty);
                using (ObjectPool<Dictionary<int, int> >.GlobalPool.AllocStub(out var resultOrderIdDict))
                {
                    //填充各个订单随机商品格子
                    foreach (var chessOrderData in _chessOrderDataList)
                    {
                        chessOrderData.FindCurSellGoodsId(allOrderDataList, resultOrderIdDict);
                    }
                }
            }
            //用完后按权重排序 用于界面显示
            _chessOrderDataList.Sort((a, b) => a.Sequence - b.Sequence);
            //刷新完后打点
            using var sb = ZString.CreateStringBuilder();
            foreach (var data in _chessOrderDataList)
            {
                if (sb.Length > 0) sb.Append(",");  // 只有在不是第一个元素时才加逗号
                sb.Append(data.CurSellGoodsId.ToString());
            }
            DataTracker.market_refresh.Track(BindBoardId, sb.ToString());
        }
        
        //按订单难度由大到小排序 难度相同时 按ProviderType类型 randomer ＞ common, 类型相同时id由小到大
        private int _SortByDifficulty(IOrderData a, IOrderData b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            OrderUtility.CalOrderDifficulty(a, out var avgA, out _);
            OrderUtility.CalOrderDifficulty(b, out var avgB, out _);
            if (avgA == avgB)
            {
                if (a.ProviderType == b.ProviderType)
                {
                    return a.Id - b.Id;
                }
                //randomer ＞ common
                return b.ProviderType - a.ProviderType;
            }
            //难度由大到小
            return avgB - avgA;
        }
    }

    #endregion
}
