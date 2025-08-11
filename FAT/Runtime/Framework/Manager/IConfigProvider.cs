/*
 * @Author: qun.chao
 * @Date: 2023-10-16 17:30:11
 */
using System.Collections;
using System.Collections.Generic;
using fat.rawdata;

namespace FAT
{
    public interface IConfigProvider
    {
        ToastConfig GetToastConfig(Toast toastType);

        #region merge

        IEnumerable<MergeFixedOutput> GetMergeFixedOutputConfigs();
        IEnumerable<MergeFixedItem> GetMergeFixedOutputByItemConfigs();
        IDictionary<int, ObjTool> GetObjToolMap();
        IDictionary<int, ObjMergeTool> GetObjMergeToolMap();
        IDictionary<int, MergeMixCost> GetMergeMixCostMap();
        IDictionary<int, MergeTapCost> GetMergeTapCostMap();
        IDictionary<int, OrderBoxDetail> GetOrderBoxDetailMap();
        IDictionary<int, GallerySpecial> GetGallerySpecialMap();
        IDictionary<int, ItemReplace> GetItemReplaceMap();
        IDictionary<int, DropLimitItem> GetDropLimitItemMap();

        #endregion

        #region 背包配置相关
        IDictionary<int, InventoryItem> GetInventoryItemConfig();
        InventoryItem GetInventoryItemConfigById(int bagGirdId);
        IDictionary<int, InventoryProducer> GetInventoryProducerConfig();
        InventoryProducer GetInventoryProducerConfigById(int bagGirdId);
        IDictionary<int, InventoryTool> GetInventoryToolConfig();
        #endregion

        #region 商店配置相关
        public MarketCommondity GetMarketCommondityConfigById(int goodsId);
        public IDictionary<int, MarketIAP> GetMarketIAPConfig();
        public IDictionary<int, MarketIncrease> GetMarketIncreaseConfig();
        public MarketIncrease GetOneMarketIncreaseConfigByFilter(System.Func<MarketIncrease, bool> filterFunc, string tag = "");
        public IDictionary<int, MarketWeight> GetMarketWeightConfig();
        public MarketWeight GetOneMarketWeightConfigByFilter(System.Func<MarketWeight, bool> filterFunc, string tag = "");
        public IDictionary<int, MarketDifficulty> GetMarketDifficultyConfig();
        public MarketDifficulty GetOneMarketDifficultyConfigByFilter(System.Func<MarketDifficulty, bool> filterFunc, string tag = "");
        public MarketIgnore GetMarketIgnoreConfigByFilter(System.Func<MarketIgnore, bool> filterFunc);
        
        #endregion

        #region reward scale
        IDictionary<int, MergeLevelRate> GetMergeLevelRateConfigMap();
        IEnumerable<RoundCoin> GetRoundCoinConfig();
        IEnumerable<RoundTool> GetRoundToolConfig();
        IEnumerable<RoundLifeTime> GetRoundLifeTimeConfig();
        #endregion

        #region order
        IDictionary<int, MergeDifficulty> GetMergeItemDifficultyConfigMap();
        IEnumerable<OrderCommon> GetOrderCommonConfigByFilter(System.Func<OrderCommon, bool> filterFunc, string tag = "");
        IEnumerable<OrderDetector> GetOrderDetectorConfigByFilter(System.Func<OrderDetector, bool> filterFunc, string tag = "");
        IEnumerable<OrderRandomer> GetOrderRandomerConfigByFilter(System.Func<OrderRandomer, bool> filterFunc, string tag = "");
        IEnumerable<OrderCategory> GetOrderCategoryConfigByFilter(System.Func<OrderCategory, bool> filterFunc, string tag = "");
        IDictionary<int, OrderCategory> GetOrderCategoryMap();
        IDictionary<string, OrderApiWhitelist> GetOrderApiWhiteListMap();
        IDictionary<int, OrderIgnore> GetOrderIgnoreConfigMap();
        MergeBoardOrder GetOneMergeBoardOrderByFilter(System.Func<MergeBoardOrder, bool> filterFunc, string tag = "");
        IDictionary<int, OrderDiff> GetOrderDiffConfigMap();
        IDictionary<int, OrderReward> GetOrderRewardConfigMap();
        #endregion

        #region 随机宝箱

        public IEnumerable<ObjRandomChest> GetRandomBoxConfigs();
        public RandomReward GetRandomRewardConfigById(int randomRewardId);

        #endregion

        public bool TryGetIapFreeConfig(int freeId, out IAPFree freeConf);
        
        public IDictionary<int, AdSetting> GetAdSettingMap();
        public AdSetting GetAdSettingById(int adsId);
        
        IDictionary<int, NpcConfig> GetNpcConfigMap();

        IEnumerable<PlayerGroup> GetPlayerGroupConfigs();
        IDictionary<int, MergeRuledOutput> GetMergeRuledOutputConfigs();

        IEnumerable<MergeGridArea> GetMergeGridAreaConfigs();
        IEnumerable<MergeGrid> GetMergeGridConfigs();

        IEnumerable<PlayerGroupRule> GetPlayerGroupRuleConfigs();
        IList<GameDiff> GetGameDiffConfigs();

        public EventTime GetEventTimeConfig(int eventId);
        public Popup GetPopupConfig(int popupId);
        
        #region 卡册相关
        
        public EventCardRound GetEventCardRoundConfig(int id);
        public EventCardAlbum GetEventCardAlbumConfig(int id);
        public CardGroup GetCardGroupConfig(int id);
        public IEnumerable<ObjCardPack> GetCardPackConfigs();
        public IEnumerable<ObjCard> GetCardConfigs();
        public RandomStar GetCardRandomStarConfig(int id);
        public CardLimit GetCardLimitConfig(int id);

        #endregion

        #region 限时订单
        public EventFlashOrder GetEventFlashOrderConfig(int id);
        #endregion

        #region 用户分层

        public UserGradeGroup GetUserGradeGroupConfig(int id);
        public GradeIndexMapping GetGradeIndexMappingConfig(int id);
        public UserGrade GetUserGradeConfig(int id);

        #endregion
    }
}