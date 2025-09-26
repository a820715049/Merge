/*
 * @Author: qun.chao
 * @Date: 2023-10-16 17:51:21
 */

using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using Config;
using fat.rawdata;
using conf = fat.conf;

namespace FAT
{
    public partial class ConfigMan : IGameModule, IConfigProvider
    {
        public IEnumerable<ObjCoin> GetCoinConfigs()
        {
            return conf.Data.GetObjCoinMap().Values;
        }

        public ObjItemConfig[] GetItemConfigs()
        {
            return new ObjItemConfig[0]; //conf.Data.GetObjItemConfigMap().Values;
        }

        public IEnumerable<ObjRandomChest> GetRandomBoxConfigs()
        {
            return conf.Data.GetObjRandomChestMap().Values;
        }

        public RandomReward GetRandomRewardConfigById(int randomRewardId)
        {
            return conf.Data.GetRandomReward(randomRewardId);
        }

        public Global GetGlobalConfig()
        {
            return conf.Data.GetGlobalByIndex(0);
        }

        public ToastConfig GetToastConfig(Toast toastType)
        {
            return conf.Data.GetOneToastConfigByFilter(x => x.ToastId == toastType);
        }

        public IEnumerable<ObjBasic> GetObjBasicConfigs()
        {
            return conf.Data.GetObjBasicMap().Values;
        }

        public IEnumerable<ObjSeasonItem> GetObjSeasonItemConfigs()
        {
            return conf.Data.GetObjSeasonItemMap().Values;
        }

        public IEnumerable<ObjMergeItem> GetObjMergeItemConfigs(int targetVersion, out int realVersion)
        {
            return _ChooseVersionAndFilterConfig(conf.Data.GetObjMergeItemMap().Values, targetVersion,
                (config) => config.ConfigVersion, out realVersion);
        }

        public IEnumerable<GuideMerge> GetGuideMergeConfigs()
        {
            return conf.Data.GetGuideMergeMap().Values;
        }

        public IEnumerable<GuideMergeAction> GetGuideMergeActionConfigs()
        {
            return conf.Data.GetGuideMergeActionMap().Values;
        }

        public IEnumerable<FaqConfig> GetFaqConfigs()
        {
            return conf.Data.GetFaqConfigMap().Values;
        }

        public IEnumerable<FeatureUnlock> GetFeatureUnlockConfigs()
        {
            return conf.Data.GetFeatureUnlockSlice();
        }

        public IEnumerable<IAPProduct> GetIAPProductConfigs()
        {
            return conf.Data.GetIAPProductMap().Values;
        }

        public bool TryGetIapFreeConfig(int freeId, out IAPFree freeConf)
        {
            freeConf = conf.Data.GetIAPFree(freeId);
            return freeConf != null;
        }

        public IEnumerable<MergeLevel> GetMergeLevelConfigs()
        {
            return conf.Data.GetMergeLevelSlice();
        }

        public IEnumerable<MergeItemCategory> GetMergeCategoryConfigs(int targetVersion, out int realVersion)
        {
            return _ChooseVersionAndFilterConfig(conf.Data.GetMergeItemCategoryMap().Values, targetVersion,
                (config) => config.ConfigVersion, out realVersion);
        }

        public IEnumerable<MergeFixedOutput> GetMergeFixedOutputConfigs()
        {
            return conf.Data.GetMergeFixedOutputSlice();
        }

        public IEnumerable<MergeFixedItem> GetMergeFixedOutputByItemConfigs()
        {
            return conf.Data.GetMergeFixedItemSlice();
        }

        public IDictionary<int, ObjTool> GetObjToolMap()
        {
            return conf.Data.GetObjToolMap();
        }

        public IDictionary<int, ObjMergeTool> GetObjMergeToolMap()
        {
            return conf.Data.GetObjMergeToolMap();
        }

        public IDictionary<int, MergeMixCost> GetMergeMixCostMap()
        {
            return conf.Data.GetMergeMixCostMap();
        }

        public IDictionary<int, MergeTapCost> GetMergeTapCostMap()
        {
            return conf.Data.GetMergeTapCostMap();
        }

        public IDictionary<int, OrderBoxDetail> GetOrderBoxDetailMap()
        {
            return conf.Data.GetOrderBoxDetailMap();
        }

        public IDictionary<int, GallerySpecial> GetGallerySpecialMap()
        {
            return conf.Data.GetGallerySpecialMap();
        }

        public IDictionary<int, ItemReplace> GetItemReplaceMap()
        {
            return conf.Data.GetItemReplaceMap();
        }

        public IEnumerable<MergeRule> GetMergeRuleConfigs()
        {
            return conf.Data.GetMergeRuleMap().Values;
        }

        public IEnumerable<ComMergeBox> GetComMergeBoxConfigs()
        {
            return conf.Data.GetComMergeBoxMap().Values;
        }

        public IEnumerable<ComMergeSkill> GetComMergeSkillConfigs()
        {
            return conf.Data.GetComMergeSkillMap().Values;
        }

        public IEnumerable<ComMergeBonus> GetComMergeBonusConfigs()
        {
            return conf.Data.GetComMergeBonusMap().Values;
        }

        public IEnumerable<ComTapBonus> GetComTapBonusConfigs()
        {
            return conf.Data.GetComTapBonusMap().Values;
        }

        public IEnumerable<ComMergeChest> GetComMergeChestConfigs(int targetVersion, out int realVersion)
        {
            return _ChooseVersionAndFilterConfig(conf.Data.GetComMergeChestMap().Values, targetVersion,
                (config) => config.ConfigVersion, out realVersion);
        }

        public IEnumerable<ComMergeTapSource> GetComMergeClickSourceConfigs(int targetVersion, out int realVersion)
        {
            return _ChooseVersionAndFilterConfig(conf.Data.GetComMergeTapSourceMap().Values, targetVersion,
                (config) => config.ConfigVersion, out realVersion);
        }

        public IEnumerable<ComMergeEatSource> GetComMergeEatSourceConfigs()
        {
            return conf.Data.GetComMergeEatSourceMap().Values;
        }

        public IEnumerable<ComMergeAutoSource> GetComMergeAutoSourceConfigs()
        {
            return conf.Data.GetComMergeAutoSourceMap().Values;
        }

        public IDictionary<int, ComMergeAutoSource> GetComMergeAutoSourceMap()
        {
            return conf.Data.GetComMergeAutoSourceMap();
        }

        public IEnumerable<ComMergeDying> GetComMergeDyingConfigs()
        {
            return conf.Data.GetComMergeDyingMap().Values;
        }

        public IEnumerable<ComMergeTimeSkip> GetComMergeTimeSkipConfigs()
        {
            return conf.Data.GetComMergeTimeSkipMap().Values;
        }

        public IEnumerable<ComMergeFeature> GetComMergeFeatureConfigs()
        {
            return conf.Data.GetComMergeFeatureMap().Values;
        }

        public IEnumerable<ComMergeEat> GetComMergeEatConfigs()
        {
            return conf.Data.GetComMergeEatMap().Values;
        }

        public IEnumerable<ComMergeToolSource> GetComMergeToolSourceConfigs()
        {
            return conf.Data.GetComMergeToolSourceMap().Values;
        }

        public IEnumerable<ComMergeOrderBox> GetComMergeOrderBoxConfigs()
        {
            return conf.Data.GetComMergeOrderBoxMap().Values;
        }

        public IEnumerable<ComMergeJumpCD> GetComMergeJumpCDConfigs()
        {
            return conf.Data.GetComMergeJumpCDMap().Values;
        }

        public IEnumerable<ComMergeSpecialBox> GetComMergeSpecialBoxConfigs()
        {
            return conf.Data.GetComMergeSpecialBoxMap().Values;
        }

        public IEnumerable<ComMergeChoiceBox> GetComMergeChoiceBoxConfigs()
        {
            return conf.Data.GetComMergeChoiceBoxMap().Values;
        }

        public IEnumerable<ComMergeMixSource> GetComMergeMixSourceConfigs()
        {
            return conf.Data.GetComMergeMixSourceMap().Values;
        }

        public IEnumerable<ComTrigAutoSource> GetComTrigAutoSourceConfigs()
        {
            return conf.Data.GetComTrigAutoSourceMap().Values;
        }

        public IDictionary<int, ComTrigAutoDetail> GetComTrigAutoDetailConfigs()
        {
            return conf.Data.GetComTrigAutoDetailMap();
        }

        public IEnumerable<ComMergeActiveSource> GetComMergeActiveSourceConfigs()
        {
            return conf.Data.GetComMergeActiveSourceMap().Values;
        }

        public IDictionary<int, GalleryCategory> GetGalleryCategoryConfigs()
        {
            return conf.Data.GetGalleryCategoryMap();
        }

        public IDictionary<int, MergeBoard> GetMergeBoardConfigs()
        {
            return conf.Data.GetMergeBoardMap();
        }

        #region 冰冻棋子相关

        public FrozenItem GetFrozenItemConfig(int id)
        {
            return conf.Data.GetFrozenItem(id);
        }
        
        public FrozenItemDetail GetFrozenItemDetailConfig(int id)
        {
            return conf.Data.GetFrozenItemDetail(id);
        }

        #endregion

        public IDictionary<int, AdSetting> GetAdSettingMap()
        {
            return conf.Data.GetAdSettingMap();
        }

        public AdSetting GetAdSettingById(int adsId)
        {
            return conf.Data.GetAdSetting(adsId);
        }

        public IDictionary<int, DropLimitItem> GetDropLimitItemMap()
        {
            return conf.Data.GetDropLimitItemMap();
        }

        #region 背包配置相关

        public IDictionary<int, InventoryItem> GetInventoryItemConfig()
        {
            return conf.Data.GetInventoryItemMap();
        }

        public InventoryItem GetInventoryItemConfigById(int bagGirdId)
        {
            return conf.Data.GetInventoryItem(bagGirdId);
        }

        public IDictionary<int, InventoryProducer> GetInventoryProducerConfig()
        {
            return conf.Data.GetInventoryProducerMap();
        }

        public InventoryProducer GetInventoryProducerConfigById(int bagGirdId)
        {
            return conf.Data.GetInventoryProducer(bagGirdId);
        }

        public IDictionary<int, InventoryTool> GetInventoryToolConfig()
        {
            return conf.Data.GetInventoryToolMap();
        }

        #endregion

        #region 商店配置相关

        public MarketCommondity GetMarketCommondityConfigById(int goodsId)
        {
            return conf.Data.GetMarketCommondity(goodsId);
        }

        public IDictionary<int, MarketIAP> GetMarketIAPConfig()
        {
            return conf.Data.GetMarketIAPMap();
        }

        public IDictionary<int, MarketIncrease> GetMarketIncreaseConfig()
        {
            return conf.Data.GetMarketIncreaseMap();
        }

        public MarketIncrease GetOneMarketIncreaseConfigByFilter(Func<MarketIncrease, bool> filterFunc, string tag = "")
        {
            return conf.Data.GetOneMarketIncreaseByFilter(filterFunc, tag);
        }

        public IDictionary<int, MarketWeight> GetMarketWeightConfig()
        {
            return conf.Data.GetMarketWeightMap();
        }

        public MarketWeight GetOneMarketWeightConfigByFilter(Func<MarketWeight, bool> filterFunc, string tag = "")
        {
            return conf.Data.GetOneMarketWeightByFilter(filterFunc, tag);
        }

        public IDictionary<int, MarketDifficulty> GetMarketDifficultyConfig()
        {
            return conf.Data.GetMarketDifficultyMap();
        }

        public MarketDifficulty GetOneMarketDifficultyConfigByFilter(Func<MarketDifficulty, bool> filterFunc,
            string tag = "")
        {
            return conf.Data.GetOneMarketDifficultyByFilter(filterFunc, tag);
        }

        public MarketIgnore GetMarketIgnoreConfigByFilter(Func<MarketIgnore, bool> filterFunc)
        {
            return conf.Data.GetOneMarketIgnoreByFilter(filterFunc);
        }

        #endregion

        #region reward scale

        public IDictionary<int, MergeLevelRate> GetMergeLevelRateConfigMap()
        {
            return conf.Data.GetMergeLevelRateMap();
        }

        public IEnumerable<RoundCoin> GetRoundCoinConfig()
        {
            return conf.Data.GetRoundCoinSlice();
        }

        public IEnumerable<RoundTool> GetRoundToolConfig()
        {
            return conf.Data.GetRoundToolSlice();
        }

        public IEnumerable<RoundLifeTime> GetRoundLifeTimeConfig()
        {
            return conf.Data.GetRoundLifeTimeSlice();
        }

        public IEnumerable<RoundScore> GetRoundScoreConfig()
        {
            return conf.Data.GetRoundScoreSlice();
        }

        #endregion

        #region order

        public IDictionary<int, MergeDifficulty> GetMergeItemDifficultyConfigMap()
        {
            return conf.Data.GetMergeDifficultyMap();
        }

        public IEnumerable<OrderCommon> GetOrderCommonConfigByFilter(Func<OrderCommon, bool> filterFunc,
            string tag = "")
        {
            return conf.Data.GetOrderCommonByFilter(filterFunc, tag);
        }

        public IEnumerable<OrderDetector> GetOrderDetectorConfigByFilter(Func<OrderDetector, bool> filterFunc,
            string tag = "")
        {
            return conf.Data.GetOrderDetectorByFilter(filterFunc, tag);
        }

        public IEnumerable<OrderRandomer> GetOrderRandomerConfigByFilter(Func<OrderRandomer, bool> filterFunc,
            string tag = "")
        {
            return conf.Data.GetOrderRandomerByFilter(filterFunc, tag);
        }

        public OrderRandomer GetOrderRandomerConf(int id)
        {
            return conf.Data.GetOrderRandomer(id);
        }

        public IEnumerable<OrderCategory> GetOrderCategoryConfigByFilter(Func<OrderCategory, bool> filterFunc,
            string tag = "")
        {
            return conf.Data.GetOrderCategoryByFilter(filterFunc, tag);
        }


        public IDictionary<int, OrderCategory> GetOrderCategoryMap()
        {
            return conf.Data.GetOrderCategoryMap();
        }

        public IDictionary<string, OrderApiWhitelist> GetOrderApiWhiteListMap()
        {
            return conf.Data.GetOrderApiWhitelistMap();
        }

        public IDictionary<int, OrderIgnore> GetOrderIgnoreConfigMap()
        {
            return conf.Data.GetOrderIgnoreMap();
        }

        public MergeBoardOrder GetOneMergeBoardOrderByFilter(Func<MergeBoardOrder, bool> filterFunc, string tag = "")
        {
            return conf.Data.GetOneMergeBoardOrderByFilter(filterFunc, tag);
        }

        public IDictionary<int, OrderDiff> GetOrderDiffConfigMap()
        {
            return conf.Data.GetOrderDiffMap();
        }

        public IDictionary<int, OrderReward> GetOrderRewardConfigMap()
        {
            return conf.Data.GetOrderRewardMap();
        }

        #endregion

        public IDictionary<int, NpcConfig> GetNpcConfigMap()
        {
            return conf.Data.GetNpcConfigMap();
        }

        public IEnumerable<MergeCloud> GetMergeCloudConfigs()
        {
            return conf.Data.GetMergeCloudSlice();
        }

        public IDictionary<int, MergeBoardGrp> GetMergeBoardGrpConfigs()
        {
            return conf.Data.GetMergeBoardGrpMap();
        }

        public IEnumerable<BubbleSpawn> GetBubbleSpawnConfigs()
        {
            return conf.Data.GetBubbleSpawnSlice();
        }

        public AdsFeature GetAdsFeatureConfig(FeatureEntry featureType)
        {
            foreach (var f in conf.Data.GetAdsFeatureSlice())
                if (f.Entry == featureType)
                    return f;
            return null;
        }

        public IList<Language> GetLanguageConfigs()
        {
            return conf.Data.GetLanguageSlice();
        }

        public IEnumerable<PlayerGroup> GetPlayerGroupConfigs()
        {
            return conf.Data.GetPlayerGroupMap().Values;
        }

        public IDictionary<int, MergeRuledOutput> GetMergeRuledOutputConfigs()
        {
            return conf.Data.GetMergeRuledOutputMap();
        }

        public IEnumerable<MergeGrid> GetMergeGridConfigs()
        {
            return conf.Data.GetMergeGridMap().Values;
        }

        public IEnumerable<MergeGridArea> GetMergeGridAreaConfigs()
        {
            return conf.Data.GetMergeGridAreaSlice();
        }

        public IDictionary<int, ObjToken> GetObjTokenConfigs()
        {
            return conf.Data.GetObjTokenMap();
        }

        public IEnumerable<PlayerGroupRule> GetPlayerGroupRuleConfigs()
        {
            return conf.Data.GetPlayerGroupRuleSlice();
        }

        public IList<GameDiff> GetGameDiffConfigs()
        {
            return conf.Data.GetGameDiffSlice();
        }

        public EventTime GetEventTimeConfig(int eventId)
        {
            return conf.Data.GetEventTime(eventId);
        }

        //传入活动类型 获取所有该类型的配置数据
        public IEnumerable<EventTime> GetEventTimeConfigsByType(EventType eventType)
        {
            return conf.Data.GetEventTimeByFilter(x => x.EventType == eventType);
        }

        public Popup GetPopupConfig(int popupId)
        {
            return conf.Data.GetPopup(popupId);
        }

        public ObjTool GetObjToolConfig(int toolId)
        {
            return conf.Data.GetObjTool(toolId);
        }
        
        //传入objBasicId 获取对应的震动配置 配置中决定是否在资源飞向资源栏时触发震动效果
        public Shake GetShakeConfig(int objBasicId)
        {
            return conf.Data.GetShake(objBasicId);
        }

        #region 卡册相关

        public EventCardRound GetEventCardRoundConfig(int id)
        {
            return conf.Data.GetEventCardRound(id);
        }

        public EventCardAlbum GetEventCardAlbumConfig(int id)
        {
            return conf.Data.GetEventCardAlbum(id);
        }

        /// <summary>
        /// 获取卡组配置
        /// </summary>
        /// <param name="id">配置id</param>
        /// <returns></returns>
        public CardGroup GetCardGroupConfig(int id)
        {
            return conf.Data.GetCardGroup(id);
        }

        public IEnumerable<ObjCardPack> GetCardPackConfigs()
        {
            return conf.Data.GetObjCardPackMap().Values;
        }

        public IEnumerable<ObjCard> GetCardConfigs()
        {
            return conf.Data.GetObjCardMap().Values;
        }

        public RandomStar GetCardRandomStarConfig(int id)
        {
            return conf.Data.GetRandomStar(id);
        }

        public CardLimit GetCardLimitConfig(int id)
        {
            return conf.Data.GetCardLimit(id);
        }

        public IEnumerable<ObjCardJoker> GetCardJokerConfigs()
        {
            return conf.Data.GetObjCardJokerMap().Values;
        }

        public StarExchange GetStarExchangeConfig(int id)
        {
            return conf.Data.GetStarExchange(id);
        }

        #endregion

        #region 限时订单

        public EventFlashOrder GetEventFlashOrderConfig(int id)
        {
            return conf.Data.GetEventFlashOrder(id);
        }

        #endregion

        #region 额外奖励订单

        public EventOrderExtra GetEventOrderExtraConfig(int id)
        {
            return conf.Data.GetEventOrderExtra(id);
        }

        #endregion

        #region 寻宝活动

        public EventTreasure GetEventTreasureConfig(int id)
        {
            return conf.Data.GetEventTreasure(id);
        }

        public EventTreasureGroupDetail GetEventTreasureGroupDetailConfig(int id)
        {
            return conf.Data.GetEventTreasureGroupDetail(id);
        }

        public IEnumerable<SettingsCommunity> GetSettingsCommunity()
        {
            return conf.Data.GetSettingsCommunityMap().Values;
        }

        public EventTreasureGroup GetEventTreasureGroupConfig(int groupId)
        {
            return conf.Data.GetEventTreasureGroup(groupId);
        }

        public EventTreasureLevel GetEventTreasureLevelConfig(int levelId)
        {
            return conf.Data.GetEventTreasureLevel(levelId);
        }

        public EventTreasureReward GetEventTreasureRewardConfig(int rewardId)
        {
            return conf.Data.GetEventTreasureReward(rewardId);
        }

        #endregion

        #region 积分活动

        public EventScore GetEventScoreConfig(int id)
        {
            return conf.Data.GetEventScore(id);
        }

        public EventExtraScore GetEventExtraScoreConfig(int id)
        {
            return conf.Data.GetEventExtraScore(id);
        }

        public EventScoreDetail GetEventScoreDetail(int id)
        {
            return conf.Data.GetEventScoreDetail(id);
        }

        #endregion

        #region 用户分层相关

        public UserGradeGroup GetUserGradeGroupConfig(int id)
        {
            return conf.Data.GetUserGradeGroup(id);
        }

        public GradeIndexMapping GetGradeIndexMappingConfig(int id)
        {
            return conf.Data.GetGradeIndexMapping(id);
        }

        public UserGrade GetUserGradeConfig(int id)
        {
            return conf.Data.GetUserGrade(id);
        }

        public IEnumerable<UserGrade> GetUserGradeConfigs()
        {
            return conf.Data.GetUserGradeMap().Values;
        }

        #endregion

        #region 装饰区活动

        public EventDecorate GetEventDecorateConfig(int id)
        {
            return conf.Data.GetEventDecorate(id);
        }

        public EventDecorateGroup GetEventDecorateGroupConfig(int id)
        {
            return conf.Data.GetEventDecorateGroup(id);
        }

        public EventDecorateLevel GetEventDecorateLevelConfig(int id)
        {
            return conf.Data.GetEventDecorateLevel(id);
        }

        public EventDecorateInfo GetEventDecorateInfo(int id)
        {
            return conf.Data.GetEventDecorateInfo(id);
        }

        #endregion

        #region 迷你棋盘

        public EventMiniBoard GetEventMiniBoardConfig(int id)
        {
            return conf.Data.GetEventMiniBoard(id);
        }

        public EventMiniBoardDetail GetEventMiniBoardDetailConfig(int id)
        {
            return conf.Data.GetEventMiniBoardDetail(id);
        }

        public EventMiniBoardDrop GetEventMiniBoardDropConfig(int id)
        {
            return conf.Data.GetEventMiniBoardDrop(id);
        }

        #endregion

        #region 多轮迷你棋盘

        public EventMiniBoardMulti GetEventMiniBoardMultiConfig(int id)
        {
            return conf.Data.GetEventMiniBoardMulti(id);
        }

        public EventMiniBoardMultiGroup GetEventMiniBoardMultiGroupConfig(int id)
        {
            return conf.Data.GetEventMiniBoardMultiGroup(id);
        }

        public EventMiniBoardMultiInfo GetEventMiniBoardMultiInfoConfig(int id)
        {
            return conf.Data.GetEventMiniBoardMultiInfo(id);
        }

        public IEnumerable<EventMiniBoardMultiInfo> GetEventMiniBoardMultiInfoMapConfig()
        {
            return conf.Data.GetEventMiniBoardMultiInfoMap().Values;
        }

        public EventMiniBoardMultiDrop GetEventMiniBoardMultiDropConfig(int id)
        {
            return conf.Data.GetEventMiniBoardMultiDrop(id);
        }

        #endregion

        #region 挖矿棋盘

        public EventMine GetEventMineConfig(int id)
        {
            return conf.Data.GetEventMine(id);
        }

        public EventMineGroup GetEventMineGroupConfig(int id)
        {
            return conf.Data.GetEventMineGroup(id);
        }

        public EventMineReward GetEventMineRewardConfig(int id)
        {
            return conf.Data.GetEventMineReward(id);
        }

        public EventMineBoardDetail GetEventMineBoardDetail(int id)
        {
            return conf.Data.GetEventMineBoardDetail(id);
        }

        public EventMineBoardRow GetEventMineBoardRow(int id)
        {
            return conf.Data.GetEventMineBoardRow(id);
        }

        #endregion
        
        #region 矿车棋盘

        public EventMineCart GetEventMineCartConfig(int id)
        {
            return conf.Data.GetEventMineCart(id);
        }

        public EventMineCartDetail GetEventMineCartDetailConfig(int id)
        {
            return conf.Data.GetEventMineCartDetail(id);
        }
        
        public EventMineCartRowGrp GetEventMineCartRowGrpConfig(int id)
        {
            return conf.Data.GetEventMineCartRowGrp(id);
        }
        
        public EventMineCartRound GetEventMineCartRoundConfig(int id)
        {
            return conf.Data.GetEventMineCartRound(id);
        }
        
        public EventMineCartReward GetEventMineCartRewardConfig(int id)
        {
            return conf.Data.GetEventMineCartReward(id);
        }
        
        public EventMineCartDrop GetEventMineCartDropConfig(int id)
        {
            return conf.Data.GetEventMineCartDrop(id);
        }

        public EventMineCartOrderItem GetEventMineCartOrderItemConfig(int id)
        {
            return conf.Data.GetEventMineCartOrderItem(id);
        }
        
        public EventMineCartRow GetEventMineCartRowConfig(int id)
        {
            return conf.Data.GetEventMineCartRow(id);
        }

        #endregion

        #region 农场棋盘

        public EventFarmBoard GetEventFarmBoardConfig(int id)
        {
            return conf.Data.GetEventFarmBoard(id);
        }

        public EventFarmBoardGroup GetEventFarmBoardGroupConfig(int id)
        {
            return conf.Data.GetEventFarmBoardGroup(id);
        }

        public EventFarmBoardDetail GetEventFarmBoardDetailConfig(int id)
        {
            return conf.Data.GetEventFarmBoardDetail(id);
        }

        public EventFarmRow GetEventFarmRowConfig(int id)
        {
            return conf.Data.GetEventFarmRow(id);
        }

        public EventFarmDrop GetEventFarmDropConfig(int id)
        {
            return conf.Data.GetEventFarmDrop(id);
        }

        public EventFarmBoardAnimal GetEventFarmBoardAnimalConfig(int id)
        {
            return conf.Data.GetEventFarmBoardAnimal(id);
        }

        public EventFarmBoardFarm GetEventFarmBoardFarmConfig(int id)
        {
            return conf.Data.GetEventFarmBoardFarm(id);
        }

        #endregion

        #region 连续限时订单活动

        public EventZeroQuest GetEventZeroQuestConfig(int id)
        {
            return conf.Data.GetEventZeroQuest(id);
        }

        public EventZeroQuestGroup GetEventZeroQuestGroupConfig(int id)
        {
            return conf.Data.GetEventZeroQuestGroup(id);
        }

        public EventZeroQuestRandom GetEventZeroQuestRandomConfig(int id)
        {
            return conf.Data.GetEventZeroQuestRandom(id);
        }

        #endregion

        #region 挖沙活动

        public EventDiggingRound GetEventDiggingRoundConfig(int id)
        {
            return conf.Data.GetEventDiggingRound(id);
        }

        public EventDigging GetEventDiggingConfig(int id)
        {
            return conf.Data.GetEventDigging(id);
        }

        public EventDiggingDetail GetEventDiggingDetail(int id)
        {
            return conf.Data.GetEventDiggingDetail(id);
        }

        public EventDiggingLevel GetEventDiggingLevel(int id)
        {
            return conf.Data.GetEventDiggingLevel(id);
        }

        public EventDiggingBoard GetEventDiggingBoard(int id)
        {
            return conf.Data.GetEventDiggingBoard(id);
        }

        public EventDiggingItem GetEventDiggingItem(int id)
        {
            return conf.Data.GetEventDiggingItem(id);
        }

        #endregion

        #region 小游戏

        public IEnumerable<MiniGameSheet> GetMiniGameMap()
        {
            return conf.Data.GetMiniGameSheetMap().Values;
        }

        public MiniGameBeadsLevel GetBeadsLevelConf(int level)
        {
            return conf.Data.GetMiniGameBeadsLevel(level);
        }

        public IEnumerable<MiniGameBeadsLevel> GetBeadsLevels()
        {
            return conf.Data.GetMiniGameBeadsLevelMap().Values;
        }

        public MiniGameSlideMergeLevel GetSlideMergeLevel(int level)
        {
            return conf.Data.GetMiniGameSlideMergeLevel(level);
        }

        #endregion

        #region 弹珠游戏

        public EventPachinkoRound GetPachinkoRoundByID(int id)
        {
            return conf.Data.GetEventPachinkoRound(id);
        }

        public EventPachinko GetEventPachinkoByID(int id)
        {
            return conf.Data.GetEventPachinko(id);
        }

        public EventPachinkoDetail GetEventPachinkoDetailByID(int id)
        {
            return conf.Data.GetEventPachinkoDetail(id);
        }

        public DropInfo GetPachinkoDropInfoByID(int id)
        {
            return conf.Data.GetDropInfo(id);
        }

        public EventPachinkoMilestone GetPachinkoMilestoneByID(int id)
        {
            return conf.Data.GetEventPachinkoMilestone(id);
        }

        public BumperInfo GetPachinkoBumperInfoByID(int id)
        {
            return conf.Data.GetBumperInfo(id);
        }

        public List<PachinkoMultiple> GetPachinkoMultipleList()
        {
            return conf.Data.GetPachinkoMultipleMap().Values.ToList();
        }

        public PachinkoMultiple GetPachinkoMultipleByID(int id)
        {
            return conf.Data.GetPachinkoMultiple(id);
        }

        #endregion

        #region 盖章活动

        public EventStamp GetEventStampConfig(int id)
        {
            return conf.Data.GetEventStamp(id);
        }

        public EventStampRound GetEventStampRoundConfig(int id)
        {
            return conf.Data.GetEventStampRound(id);
        }

        #endregion
        #region Bingo活动
        public EventItemBingoRound GetEventItemBingoRoundConfig(int id)
        {
            return conf.Data.GetEventItemBingoRound(id);
        }
        public EventItemBingo GetEventItemBingoConfig(int id)
        {
            return conf.Data.GetEventItemBingo(id);
        }
        public EventItemBingoDetail GetEventItemBingoDetailConfig(int id)
        {
            return conf.Data.GetEventItemBingoDetail(id);
        }

        public LevelGroups GetLevelGroupConfig(int id)
        {
            return conf.Data.GetLevelGroups(id);
        }

        public GroupDetail GetGroupDetailConfig(int id)
        {
            return conf.Data.GetGroupDetail(id);
        }

        public ItemBingoBoard GetItemBingoBoardConfig(int id)
        {
            return conf.Data.GetItemBingoBoard(id);
        }
        #endregion
        #region 1v1活动

        public EventScoreDuel GetEventScoreDuelConfig(int id)
        {
            return conf.Data.GetEventScoreDuel(id);
        }

        public EventScoreDuelDetail GetEventScoreDuelDetailConfig(int id)
        {
            return conf.Data.GetEventScoreDuelDetail(id);
        }

        public EventScoreDuelSTG GetEventScoreDuelSTGConfig(int id)
        {
            return conf.Data.GetEventScoreDuelSTG(id);
        }
        #endregion

        #region OrderRate活动
        public EventOrderRate GetEventOrderRateConfig(int id)
        {
            return conf.Data.GetEventOrderRate(id);
        }
        public EventOrderRateDetail GetEventOrderRateDetailConfig(int id)
        {
            return conf.Data.GetEventOrderRateDetail(id);
        }

        public EventOrderRateBox GetEventOrderRateBoxConfig(int id)
        {
            return conf.Data.GetEventOrderRateBox(id);
        }

        public IEnumerable<EventOrderRateRandom> GetEventOrderRateRandomConfig()
        {
            return conf.Data.GetEventOrderRateRandomMap().Values;
        }
        #endregion

        #region 签到
        public LoginSign GetLoginSignConfig()
        {
            return conf.Data.GetLoginSignByIndex(0);
        }

        public LoginSignPool GetLoginSignPoolConfig(int id)
        {
            return conf.Data.GetLoginSignPool(id);
        }

        public LoginSignTotal GetLoginSignTotalConfig(int id)
        {
            return conf.Data.GetLoginSignTotal(id);
        }
        #endregion

        #region 签到抽奖
        public EventWeeklyRaffleGrp GetEventWeeklyRaffleGroupConfig(int groupId)
        {
            return conf.Data.GetEventWeeklyRaffleGrp(groupId);
        }
        #endregion

        #region 三日签到

        public EventThreeSign GetEventThreeSignConfig(int id)
        {
            return conf.Data.GetEventThreeSign(id);
        }
        
        public EventThreeSignPool GetEventThreeSignPoolConfig(int id)
        {
            return conf.Data.GetEventThreeSignPool(id);
        }

        #endregion

        #region 订单助力
        public EventOrderBonus GetEventOrderBonus(int id)
        {
            return conf.Data.GetEventOrderBonus(id);
        }

        public EventOrderBonusGroup GetEventOrderBonusGroup(int id)
        {
            return conf.Data.GetEventOrderBonusGroup(id);
        }

        public EventOrderBonusDetail GetEventOrderBonusDetail(int id)
        {
            return conf.Data.GetEventOrderBonusDetail(id);
        }
        #endregion

        #region 兑换商店活动

        public EventRedeem GetEventRedeemConfig(int id)
        {
            return conf.Data.GetEventRedeem(id);
        }

        public EventRedeemDetail GetEventRedeemDetailConfig(int id)
        {
            return conf.Data.GetEventRedeemDetail(id);
        }

        public EventRedeemMilestone GetEventRedeemMilestone(int id)
        {
            return conf.Data.GetEventRedeemMilestone(id);
        }

        public EventRedeemGrp GetEventRedeemGrp(int id)
        {
            return conf.Data.GetEventRedeemGrp(id);
        }

        public EventRedeemReward GetEventRedeemReward(int id)
        {
            return conf.Data.GetEventRedeemReward(id);
        }

        #endregion

        #region 通行证 BP BattlePass

        public EventBp GetEventBpConfig(int id)
        {
            return conf.Data.GetEventBp(id);
        }
        
        public BpDetail GetBpDetailConfig(int id)
        {
            return conf.Data.GetBpDetail(id);
        }
        
        public BpMilestone GetBpMilestoneConfig(int id)
        {
            return conf.Data.GetBpMilestone(id);
        }
        
        public BpPackInfo GetBpPackInfoConfig(int id)
        {
            return conf.Data.GetBpPackInfo(id);
        }
        
        public BpTask GetBpTaskConfig(int id)
        {
            return conf.Data.GetBpTask(id);
        }

        #endregion

        #region 打怪棋盘
        public EventFight GetEventFightById(int id)
        {
            return conf.Data.GetEventFight(id);
        }

        public EventFightDetail GetEventFightDetailById(int id)
        {
            return conf.Data.GetEventFightDetail(id);
        }

        public EventFightLevel GetEventFightLevelById(int id)
        {
            return conf.Data.GetEventFightLevel(id);
        }

        public Monster GetMonsterById(int id)
        {
            return conf.Data.GetMonster(id);
        }

        public MonsterTalk GetMonsterTalkById(int id)
        {
            return conf.Data.GetMonsterTalk(id);
        }
        #endregion

        #region 许愿棋盘
        public EventWishBoard GetEventWishBoardConfig(int id)
        {
            return conf.Data.GetEventWishBoard(id);
        }

        public EventWishBoardGroup GetEventWishBoardGroupConfig(int id)
        {
            return conf.Data.GetEventWishBoardGroup(id);
        }

        public EventWishBoardDetail GetEventWishBoardDetailConfig(int id)
        {
            return conf.Data.GetEventWishBoardDetail(id);
        }

        public EventWishRow GetEventWishRowConfig(int id)
        {
            return conf.Data.GetEventWishRow(id);
        }

        public EventWishMilestone GetEventWishMilestone(int id)
        {
            return conf.Data.GetEventWishMilestone(id);
        }

        public EventWishDrop GetEventWishDropConfig(int id)
        {
            return conf.Data.GetEventWishDrop(id);
        }

        public EventWishBoardGroup GetEventWishBoardGroup(int id)
        {
            return conf.Data.GetEventWishBoardGroup(id);
        }
        public EventWishBarReward GetCurWishBarRewardById(int id)
        {
            return conf.Data.GetEventWishBarReward(id);
        }
        #endregion

        #region 能量加倍
        public EnergyBoost GetEnergyBoostConfig(int id)
        {
            return conf.Data.GetEnergyBoost(id);
        }
        #endregion

        private IEnumerable<T> _ChooseVersionAndFilterConfig<T>(IEnumerable<T> rawData, int targetVersion,
            Func<T, int> configVersionExtractor, out int realVersion)
        {
            realVersion = _ChooseConfigVersion(rawData, targetVersion, configVersionExtractor);
            return _FilterConfigByVersion(rawData, realVersion, configVersionExtractor);
        }

        private int _ChooseConfigVersion<T>(IEnumerable<T> rawData, int targetVersion,
            Func<T, int> configVersionExtractor)
        {
            //find the largest version number LEQ targetVersion
            var version = 0;
            foreach (var data in rawData)
            {
                var v = configVersionExtractor(data);
                if (v <= targetVersion && v > version) version = v;
                if (version == targetVersion) break;
            }

            DebugEx.FormatInfo("ConfigMan::_ChooseConfigVersion ----> type {0}, targetVersion {1}, realVersion {2}",
                typeof(T).Name, targetVersion, version);
            return version;
        }

        private IEnumerable<T> _FilterConfigByVersion<T>(IEnumerable<T> rawData, int realVersion,
            Func<T, int> configVersionExtractor)
        {
            foreach (var data in rawData)
                if (configVersionExtractor(data) == realVersion)
                    yield return data;
        }
    }
}
