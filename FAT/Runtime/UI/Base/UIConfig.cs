/*
 * @Author: qun.chao
 * @Date: 2023-10-12 10:48:58
 */
using UnityEngine;

namespace FAT
{
    using static UILayer;

    public partial class UIConfig
    {
        public static UIResource UIMergeBoardMain = new UIResource("UIMergeBoardMain.prefab", UILayer.BelowStatus, "fat_global");
        public static UIResource UIEnergyBoostTips = new UIResource("UIEnergyBoostTips.prefab", UILayer.BelowStatus, "fat_global");
        public static UIResource UIDebugPanelProMax = new UIResource("UIDebugPanelProMax.prefab", UILayer.Top, "fat_global").SupportNavBack();
        public static UIResource UIOrderDebug = new UIResource("UIOrderDebug.prefab", UILayer.Top, "fat_global").SupportNavBack();
        public static UIResource UIGuide = new UIResource("UIGuide.prefab", UILayer.Top, "fat_global");
        public static UIResource UIStatus = new UIResource("UIStatus.prefab", UILayer.Status, "fat_global").IgnoreNavBack();
        public static UIResource UISceneHud = new UIResource("UISceneHud.prefab", UILayer.Hud, "fat_global_ext");
        public static UIResource UIMessageBox = new UIResource("UIMessageBox.prefab", UILayer.Top, "fat_global").SupportNavBack();
        public static UIResource UINewVersion = new UIResource("UINewVersion.prefab", UILayer.Loading, "fat_global");
        public static UIResource UIUpdate = new UIResource("UIUpdate.prefab", UILayer.Loading, "fat_global");
        public static UIResource UIPopTips = new UIResource("UIPopTips.prefab", UILayer.Top, "fat_global");
        public static UIResource UIPopFlyTips = new UIResource("UIPopFlyTips.prefab", UILayer.MiddleStatus, "fat_global");
        public static UIResource UINetWarning = new UIResource("UINetWarning.prefab", UILayer.Top, "fat_global");
        public static UIResource UILevelUp = new UIResource("UILevelUp.prefab", UILayer.SubStatus, "fat_global_ext").CustomSoundEvent("LevelUp", null).AllowHideUI();
        public static UIResource UISetting = new UIResource("UISetting.prefab", UILayer.AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UISettingCommunity = new UIResource("UISettingCommunity.prefab", UILayer.AboveStatus, "fat_global").SupportNavBack();
        //这个界面放在global里是因为会在Setting界面呼出，呼出按钮的显示隐藏是单独的配置，global的级别更高，更安全
        public static UIResource UIProbabilityTips = new UIResource("UIProbabilityTips.prefab", UILayer.SubStatus, "fat_global").SupportNavBack();//概率公示弹窗
        public static UIResource UISupport = new UIResource("UISupport.prefab", UILayer.AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIAccountBind = new UIResource("UIAccountBind.prefab", AboveStatus, "fat_global").SupportNavBack();
        public static UIResource UIFacebookBindNotice = new UIResource("UIFacebookBindNotice.prefab", SubStatus, "fat_global").SupportNavBack();
        public static UIResource UIAccountBindExist = new UIResource("UIAccountBindExist.prefab", SubStatus, "fat_global").SupportNavBack();
        public static UIResource UIAccountChange = new UIResource("UIAccountChange.prefab", SubStatus, "fat_global").SupportNavBack();
        public static UIResource UIAuthenticationPolicy = new UIResource("UIAuthenticationPolicy.prefab", UILayer.AboveStatus, "fat_global");
        public static UIResource UIAuthenticationSelect = new UIResource("UIAuthenticationSelect.prefab", UILayer.AboveStatus, "fat_global");
        public static UIResource UIAuthenticationEmail = new UIResource("UIAuthenticationEmail.prefab", UILayer.AboveStatus, "fat_global");
        public static UIResource UINotificationRedirect = new("UINotificationRedirect.prefab", AboveStatus, "fat_global");
        public static UIResource UINotificationRemind = new("UINotificationRemind.prefab", AboveStatus, "fat_global");
        public static UIResource UINotificationRemindActivity = new("UINotificationRemindActivity.prefab", AboveStatus, "event_common");
        public static UIResource UIBag = new UIResource("UIBag.prefab", UILayer.AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIShop = new UIResource("UIShop.prefab", UILayer.AboveStatus, "fat_global").CustomSoundEvent("ShopIn", "CloseWindow").SupportNavBack().AllowHideUI(true, true);
        public static UIResource UIHandbook = new UIResource("UIHandbook.prefab", UILayer.AboveStatus, "fat_global").SupportNavBack().AllowHideUI(true, true);
        public static UIResource UIItemInfo = new UIResource("UIItemInfo.prefab", UILayer.SubStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIItemInfoTips = new UIResource("UIItemInfoTips.prefab", UILayer.SubStatus, "fat_global").SetMute().SupportNavBack().IsTips();
        public static UIResource UIItemInfoWideTips = new UIResource("UIItemInfoWideTips.prefab", UILayer.SubStatus, "fat_global").SetMute().SupportNavBack().IsTips();
        public static UIResource UIRandomBox = new UIResource("UIRandomBox.prefab", UILayer.SubStatus, "chest_randomchest_common");
        public static UIResource UISingleReward = new UIResource("UISingleReward.prefab", UILayer.SubStatus, "chest_randomchest_common");
        public static UIResource UIRewardPanel = new UIResource("UIRewardPanel.prefab", UILayer.SubStatus, "fat_global");
        public static UIResource UIMultipleReward = new UIResource("UIMultipleReward.prefab", UILayer.SubStatus, "fat_global").SupportNavBack().SetMute();
        public static UIResource UIRandomBoxTips = new UIResource("UIRandomBoxTips.prefab", UILayer.SubStatus, "chest_randomchest_common").SetMute().SupportNavBack().IsTips();
        public static UIResource UIRandomBoxSpecialTips = new UIResource(string.Empty, UILayer.SubStatus, string.Empty).SetMute().SupportNavBack().IsTips();
        public static UIResource UIOrderBoxTips = new UIResource("UIOrderBoxTips.prefab", UILayer.SubStatus, "chest_randomchest_common").SetMute().IsTips();
        public static UIResource UIAdsWaitResolve = new UIResource("UIAdsWaitResolve.prefab", UILayer.Loading, "fat_global");
        public static UIResource UIAdsEditorTest = new UIResource("UIAdsEditorTest.prefab", UILayer.BlockUser, "fat_global");
        public static UIResource UIGameplayHelp = new UIResource("UIGameplayHelp.prefab", AboveStatus, "fat_global").AllowHideUI();
        public static UIResource UIOutOfEnergy = new UIResource("UIOutOfEnergy.prefab", AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIDailyEvent = new UIResource("UIDailyEvent.prefab", AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UINoticeDaily = new UIResource(string.Empty, AboveStatus, "fat_daily").SupportNavBack().AllowHideUI();//dynamic
        public static UIResource UIHelpDEM = new UIResource("UIHelpDEM.prefab", SubStatus, "fat_global").AllowHideUI();
        public static UIResource UIDEReward = new("UIDEReward.prefab", Status, "fat_global");
        public static UIResource UIDEMReward = new("UIDEMReward.prefab", SubStatus, "fat_global");
        public static UIResource UINoticeMIG = new UIResource(string.Empty, AboveStatus, "fat_global").SupportNavBack().AllowHideUI();//dynamic
        public static UIResource UIGiftPack = new UIResource("UIGiftPack.prefab", AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIGiftPackNU = new UIResource("UIGiftPackNU.prefab", AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIToolExchange = new UIResource("UIToolExchange.prefab", AboveStatus, "fat_global_ext").SupportNavBack().AllowHideUI();
        public static UIResource UIActivityStep = new UIResource("UIActivityStep.prefab", AboveStatus, "event_step_common").SupportNavBack().AllowHideUI();
        public static UIResource UIActivityStepComplete = new("UIActivityStepComplete.prefab", AboveStatus, "event_step_common");
        public static UIResource UIActivityStepEnd = new UIResource("UIActivityStepEnd.prefab", AboveStatus, "event_step_common").AllowHideUI();
        public static UIResource UIMailBox = new UIResource("UIMailBoxNew.prefab", AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIMailDetailSystem = new("UIMailDetailNew.prefab", AboveStatus, "fat_global");
        public static UIResource UIMapSceneHelp = new UIResource("UIMapSceneHelp.prefab", AboveStatus, "fat_map").SupportNavBack().AllowHideUI();
        public static UIResource UIMapSceneStory = new UIResource("UIMapSceneStory.prefab", AboveStatus, "fat_map").SupportNavBack().AllowHideUI(true, true);
        public static UIResource UIStorySwitchNotice = new UIResource("UIStorySwitchNotice.prefab", AboveStatus, "fat_map").SupportNavBack().AllowHideUI();
        public static UIResource UIGetToolsHelp = new UIResource("UIGetToolsHelp.prefab", SubStatus, "fat_map").SupportNavBack().AllowHideUI();
        public static UIResource UIBuildingEffect = new UIResource("UIBuildingEffect.prefab", Effect, "fat_map").IgnoreNavBack().SetMute();
        public static UIResource UIEnergyBoxTips = new UIResource("UIEnergyBoxTips.prefab", SubStatus, "fat_global").SupportNavBack().IsTips();
        public static UIResource UIActivityReward = new UIResource("UIActivityReward.prefab", SubStatus, "fat_global");
        public static UIResource UIActivityRewardTips = new UIResource("UIActivityRewardTips.prefab", SubStatus, "fat_global").SupportNavBack().IsTips();
        public static UIResource UIWait = new UIResource("UIWait.prefab", Loading, "fat_launch");
        public static UIResource UIOnePlusOnePack = new UIResource("UIOnePlusOnePack.prefab", AboveStatus, "event_oneplusone_default").SupportNavBack().AllowHideUI();
        public static UIResource UIOnePlusTwoPack = new UIResource("UIOnePlusTwoPack.prefab", AboveStatus, "event_oneplustwo_default").SupportNavBack().AllowHideUI();
        public static UIResource UIEndlessPack = new UIResource("UIEndlessPack.prefab", AboveStatus, "event_endless_default").SupportNavBack().AllowHideUI();
        public static UIResource UIEndlessTokenTips = new UIResource("UIEndlessTokenTips.prefab", SubStatus, "event_endless_common").SupportNavBack().IsTips();
        public static UIResource UIPackProgress = new UIResource("UIPackProgress.prefab", AboveStatus, "event_progress_default").SupportNavBack().AllowHideUI();
        public static UIResource UIPackRetention = new UIResource("UIPackRetention.prefab", AboveStatus, "event_retention_default").SupportNavBack().AllowHideUI();
        public static UIResource UIPackShinnyGuar = new UIResource("UIPackShinnyGuar.prefab", AboveStatus, "event_shinny_guar").SupportNavBack().AllowHideUI();
        public static UIResource UIShinnyGuarPreview = new UIResource("UIShinnyGuarPreview.prefab", SubStatus, "event_shinny_guar").SetMute().SupportNavBack().IsTips();
        public static UIResource UIRate = new UIResource("UIRate.prefab", AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIGuideBoost = new UIResource("UIGuideBoost.prefab", AboveStatus, "fat_global").AllowHideUI();
        public static UIResource UIEnergyBoostUnlock4X = new UIResource("UIEnergyBoostUnlock4X.prefab", AboveStatus, "fat_global").AllowHideUI();
        public static UIResource UITimeBoosterDetails = new UIResource("UITimeBoosterDetails.prefab", SubStatus, "fat_global").SupportNavBack().IsTips();
        public static UIResource UITotalRewardPanel = new UIResource("UITotalRewardPanel.prefab", SubStatus, "fat_global").AllowHideUI();
        public static UIResource UIItemUseConfirm = new UIResource("UIItemUseConfirm.prefab", AboveStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UISpecialBoxInfo = new UIResource("UISpecialBoxInfo.prefab", SubStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIChoiceBox = new UIResource("UIChoiceBox.prefab", SubStatus, "fat_global").SupportNavBack().AllowHideUI();
        public static UIResource UIMixSourceTips = new UIResource("UIMixSourceTips.prefab", SubStatus, "fat_global").SupportNavBack().IsTips();
        public static UIResource UIMixSourceDetail = new UIResource("UIMixSourceDetail.prefab", AboveStatus, "fat_global").SupportNavBack();
        public static UIResource UIActivitySurvey = new UIResource("UIActivitySurvey.prefab", AboveStatus, "event_survey_default").SupportNavBack();
        public static UIResource UIActivitySurveyReward = new("UIActivitySurveyReward.prefab", SubStatus, "event_survey_default");
        public static UIResource UIActivityInvite = new UIResource("UIActivityInvite.prefab", AboveStatus, "event_invite_default").SupportNavBack();
        public static UIResource UIActivityRanking = new UIResource("UIActivityRanking.prefab", AboveStatus, "event_ranking_default").SupportNavBack();
        public static UIResource UIActivityRankingHelp = new UIResource("UIActivityRankingHelp.prefab", AboveStatus, "event_ranking_default").SupportNavBack();
        public static UIResource UIActivityRankingStart = new("UIActivityRankingStart.prefab", AboveStatus, "event_ranking_default");
        public static UIResource UIActivityRankingEnd = new("UIActivityRankingEnd.prefab", AboveStatus, "event_ranking_default");
        public static UIResource UIPackGemThreeForOne = new UIResource("UIPackGemThreeForOne.prefab", AboveStatus, "event_giftpack_gemthreeforone").SupportNavBack();
        public static UIResource UIPackEnergyMultiPack = new UIResource("UIPackEnergyMultiPack.prefab", AboveStatus, "event_giftpack_energymulti").SupportNavBack();
        public static UIResource UICompleteOrderBag = new UIResource("UICompleteOrderBag.prefab", SubStatus, "fat_global");
        public static UIResource UIErgListPack = new UIResource("UIErgListPack.prefab", AboveStatus, "event_erglist_default").SupportNavBack();
        public static UIResource UIErgListPackBuyTips = new UIResource("UIErgListPackBuyTips.prefab", SubStatus, "event_erglist_default");
        public static UIResource UIErgListPackEnd = new UIResource("UIErgListPackEnd.prefab", SubStatus, "event_erglist_default");
        public static UIResource UIGemSecondConfirm = new UIResource("UIGemSecondConfirm.prefab", SubStatus, "fat_global").SupportNavBack();
        public static UIResource UIActivityWishUponMain = new UIResource("UIActivityWishUponMain.prefab", AboveStatus, "event_wishupon_default");
        public static UIResource UIFrozenItemHelp = new UIResource("UIFrozenItemHelp.prefab", AboveStatus, "event_frozen_item");
        #region 卡册系统相关界面

        //debug 模拟抽卡界面
        public static UIResource UIDebugDrawCard = new UIResource("UIDebugDrawCard.prefab", UILayer.Top, "fat_card_common");
        //卡包tips
        public static UIResource UICardPackPreview = new UIResource("UICardPackPreview.prefab", SubStatus, "fat_card_common").SetMute().SupportNavBack().IsTips();
        //卡册系统相关
        public static UIResource UICardAlbum = new UIResource("UICardAlbum.prefab", UILayer.AboveStatus, "fat_card_common").SupportNavBack().AllowHideUI(true, true);
        public static UIResource UICardAlbumRestart = new UIResource("UICardAlbumRestart.prefab", UILayer.AboveStatus, "fat_card_common").AllowHideUI();
        public static UIResource UICardInfo = new UIResource("UICardInfo.prefab", UILayer.SubStatus, "fat_card_common").SupportNavBack();
        public static UIResource UICardGroupReward = new UIResource("UICardGroupReward.prefab", UILayer.SubStatus, "fat_card_common");
        public static UIResource UICardAlbumReward = new UIResource("UICardAlbumReward.prefab", UILayer.SubStatus, "fat_card_common");
        public static UIResource UICardPackOpen = new UIResource("UICardPackOpen.prefab", UILayer.SubStatus, "fat_card_common").SetMute();
        public static UIResource UICardAlbumGuide = new UIResource("UICardAlbumGuide.prefab", UILayer.SubStatus, "fat_card_common").SupportNavBack();
        public static UIResource UIGuideCardUnlock = new UIResource("UIGuideCardUnlock.prefab", UILayer.SubStatus, "fat_card_common").AllowHideUI();

        //卡册活动相关
        public static UIResource UICardActivityEnd = new UIResource("UICardActivityEnd.prefab", UILayer.AboveStatus, "fat_card_common").AllowHideUI();
        public static UIResource UICardActivityEndNotice = new UIResource("UICardActivityEndNotice.prefab", UILayer.AboveStatus, "fat_card_common").SupportNavBack().AllowHideUI();
        public static UIResource UICardActivityStartNotice = new UIResource("UICardActivityStartNotice.prefab", UILayer.AboveStatus, "fat_card_common").SupportNavBack().AllowHideUI();
        public static UIResource UICardActivityTips = new UIResource("UICardActivityTips.prefab", UILayer.SubStatus, "fat_card_common").SetMute().IsTips();
        public static UIResource UICardFinalRewardTips = new UIResource("UICardFinalRewardTips.prefab", UILayer.SubStatus, "fat_card_common").SetMute().IsTips();
        //万能卡相关界面
        public static UIResource UIJokerCardTips = new UIResource("UIJokerCardTips.prefab", UILayer.SubStatus, "fat_card_common").SupportNavBack().IsTips();
        public static UIResource UICardJokerGet = new UIResource("UICardJokerGet.prefab", UILayer.SubStatus, "fat_card_common");
        public static UIResource UICardJokerSet = new UIResource("UICardJokerSet.prefab", UILayer.SubStatus, "fat_card_common");
        public static UIResource UICardJokerEntrance = new UIResource("UICardJokerEntrance.prefab", UILayer.SubStatus, "fat_card_common");
        public static UIResource UICardJokerSelect = new UIResource("UICardJokerSelect.prefab", UILayer.SubStatus, "fat_card_common");
        public static UIResource UICardJokerConfirm = new UIResource("UICardJokerConfirm.prefab", UILayer.SubStatus, "fat_card_common");
        //兑换
        public static UIResource UICardExchangeReward = new UIResource("UICardExchangeReward.prefab", SubStatus, "fat_card_common").SupportNavBack();
        public static UIResource UICardExchangeSelect = new UIResource("UICardExchangeSelect.prefab", SubStatus, "fat_card_common").SupportNavBack();
        public static UIResource UICardExchangeStarCollect = new("UICardExchangeStarCollect.prefab", SubStatus, "fat_card_common");
        //卡片交换相关界面
        public static UIResource UICardGifting = new UIResource("UICardGifting.prefab", SubStatus, "fat_card_common").SupportNavBack();
        public static UIResource UICardPending = new UIResource("UICardPending.prefab", SubStatus, "fat_card_common").SupportNavBack();
        public static UIResource UICardAlbumPreview = new UIResource("UICardAlbumPreview.prefab", SubStatus, "fat_card_common").SupportNavBack();
        public static UIResource UICardTradeSuccess = new("UICardTradeSuccess.prefab", SubStatus, "fat_card_common");
        public static UIResource UICardReceive = new("UICardReceive.prefab", SubStatus, "fat_card_common");
        public static UIResource UICardSendTips = new("CardSendTips.prefab", SubStatus, "fat_card_common");
        #endregion

        #region 沙堡里程碑活动
        public static UIResource UIActivityCastleBegin = new UIResource("UIActivityCastleBegin.prefab", AboveStatus, "event_castle_default");
        public static UIResource UIActivityCastleConvert = new UIResource("UIActivityCastleConvert.prefab", AboveStatus, "event_castle_default");
        public static UIResource UIActivityCastleMain = new UIResource("UIActivityCastleMain.prefab", AboveStatus, "event_castle_default");
        #endregion

        #region 寻宝活动
        public static UIResource UITreasureHuntLoading = new UIResource("UITreasureHuntLoading.prefab", Loading, "event_treasurehunt_default").SetMute();
        public static UIResource UITreasureHuntMain = new UIResource("UITreasureHuntMain.prefab", BelowStatus, "event_treasurehunt_default").SetMute();
        public static UIResource UITreasureHuntLevelReward = new UIResource("UITreasureHuntLevelReward.prefab", AboveStatus, "event_treasurehunt_default");
        public static UIResource UITreasureHuntProgressReward = new UIResource("UITreasureHuntProgressReward.prefab", AboveStatus, "event_treasurehunt_default");
        public static UIResource UITreasureHuntBag = new UIResource("UITreasureHuntBag.prefab", AboveStatus, "event_treasurehunt_common").SupportNavBack();
        public static UIResource UITreasureHuntRewardTips = new UIResource("UITreasureHuntRewardTips.prefab", SubStatus, "event_treasurehunt_common").IsTips();
        public static UIResource UITreasureHuntDebug = new UIResource("UITreasureHuntDebug.prefab", UILayer.Top, "event_treasurehunt_default");
        public static UIResource UITreasureHuntStartNotice = new UIResource("UITreasureHuntStartNotice.prefab", UILayer.AboveStatus, "event_treasurehunt_default");
        public static UIResource UITreasureHuntEnd = new UIResource("UITreasureHuntEnd.prefab", UILayer.AboveStatus, "event_treasurehunt_default");
        public static UIResource UITreasureHuntHelp = new UIResource("UITreasureHuntHelp.prefab", UILayer.AboveStatus, "event_treasurehunt_default").SupportNavBack();
        public static UIResource UITreasureHuntHelpPopup = new UIResource("UITreasureHuntHelpPopup.prefab", UILayer.AboveStatus, "event_treasurehunt_default").AllowHideUI();
        public static UIResource UITreasureHuntGift = new UIResource("UITreasureHuntGift.prefab", AboveStatus, "event_treasurehunt_default").SupportNavBack();
        #endregion

        #region 积分活动
        public static UIResource UIScoreHelp = new UIResource("UIScoreHelp_cook.prefab", AboveStatus, "event_score_cook").SupportNavBack().AllowHideUI();
        public static UIResource UIScoreProgress = new UIResource("UIScoreProgress.prefab", Top, "event_score_common").IgnoreNavBack();
        public static UIResource UIScoreGuide = new UIResource("UIScoreGuide_concert.prefab", AboveStatus, "event_score_concert").SupportNavBack().AllowHideUI();
        public static UIResource UIScoreMilestone = new UIResource("UIScoreMilestone_cook.prefab", AboveStatus, "event_score_cook").SupportNavBack().AllowHideUI();
        public static UIResource UIScoreFinish_Track = new UIResource("UIScoreFinish_track.prefab", AboveStatus, "event_score_track").SupportNavBack();
        public static UIResource UIScoreFinish_Piece = new UIResource("UIScoreFinish_piece.prefab", AboveStatus, "event_score_piece").SupportNavBack();
        #endregion
        
        #region 新积分活动-合成音符
        public static UIResource UIScoreConvert_Mic = new UIResource("UIScoreConvert_Mic.prefab", AboveStatus, "event_score_mic").AllowHideUI();
        public static UIResource UIScore_Mic = new UIResource("UIScore_mic.prefab", UILayer.AboveStatus, "event_score_mic").SupportNavBack().AllowHideUI();
        public static UIResource UIMicInfo = new UIResource("UIMicTips.prefab", SubStatus, "event_score_mic").SupportNavBack().AllowHideUI();
        #endregion

        #region 订单额外奖励活动

        public static UIResource UIOrderExtra = new UIResource("UIOrderExtra.prefab", AboveStatus, "fat_global").SupportNavBack().AllowHideUI();

        #endregion

        #region 登入赠品

        public static UIResource UILoginGift = new UIResource("UILoginGift.prefab", AboveStatus, "fat_global_ext").AllowHideUI();

        #endregion

        #region 装饰区活动

        public static UIResource UIDecorateHelp = new UIResource("UIDecorateHelp.prefab", SubStatus, "event_decorate_common").SupportNavBack();
        public static UIResource UIDecorateComplete = new UIResource("UIDecorateComplete.prefab", AboveStatus, "event_decorate_common").AllowHideUI(true, true);
        public static UIResource UIDecorateEndNotice = new UIResource("UIDecorateEndNotice.prefab", AboveStatus, "event_decorate_common").AllowHideUI();
        public static UIResource UIDecorateConvert = new UIResource("UIDecorateConvert.prefab", AboveStatus, "event_decorate_common").AllowHideUI();
        public static UIResource UIDecoratePanel = new UIResource("UIDecoratePanel.prefab", AboveStatus, "event_decorate_common").AllowHideUI(true, true);
        public static UIResource UIDecorateStartNotice = new UIResource("UIDecorateStartNotice.prefab", AboveStatus, "event_decorate_common").AllowHideUI();
        public static UIResource UIDecorateRestartNotice = new UIResource("UIDecorateRestartNotice.prefab", AboveStatus, "event_decorate_common").AllowHideUI();
        public static UIResource UIDecorateEnd = new UIResource("UIDecorateEnd.prefab", AboveStatus, "event_decorate_common").AllowHideUI();
        public static UIResource UIDecorateRes = new UIResource("UIDecorateRes.prefab", SubStatus, "event_decorate_common").IgnoreNavBack();
        public static UIResource UIDecorateOverview = new UIResource("UIDecorateOverview.prefab", SubStatus, "event_decorate_common");

        #endregion

        #region 热气球活动

        public static UIResource UIRaceStart = new UIResource("UIRaceStart.prefab", AboveStatus, "event_race_default");
        public static UIResource UIRaceEnd = new UIResource("UIRaceEnd.prefab", AboveStatus, "event_race_default");
        public static UIResource UIRacePanel = new UIResource("UIRacePanel.prefab", AboveStatus, "event_race_default").SupportNavBack();
        public static UIResource UIRaceReward = new UIResource("UIRaceReward.prefab", SubStatus, "event_race_default");
        public static UIResource UIRaceHelp = new UIResource("UIRaceHelp.prefab", SubStatus, "event_race_default").SupportNavBack();

        #endregion

        #region 迷你棋盘活动

        public static UIResource UIMiniBoard = new UIResource("UIMiniboard.prefab", MiddleStatus, "event_miniboard_swimminggear");
        public static UIResource UIMiniBoardReward = new UIResource("UIMiniBoardReward.prefab", SubStatus, "event_miniboard_swimminggear");

        #endregion

        #region 多轮迷你棋盘相关界面

        public static UIResource UIMiniBoardMulti =
            new("UIMiniBoardMultiMain_s001.prefab", MiddleStatus, "event_miniboardmulti_s001");

        public static UIResource UIMiniBoardMultiReward =
            new("UIMiniBoardMultiReward.prefab", SubStatus, "event_miniboardmulti_s001");

        public static UIResource UIMiniBoardMultiHelp =
            new("UIMiniBoardMultiHelp.prefab", SubStatus, "event_miniboardmulti_s001");

        public static UIResource UIMiniBoardMultiFly =
            new("UIMiniBoardMultiFly.prefab", AboveStatus, "event_miniboardmulti_s001");

        public static UIResource UIMiniBoardMultiNextRound = new("UIMiniBoardMultiNextRound_s001.prefab",
            SubStatus, "event_miniboardmulti_s001");

        #endregion

        #region 挖沙活动
        public static UIResource UIDiggingMain = new UIResource("UIDiggingMain.prefab", BelowStatus, "event_digging_default").SetMute();
        public static UIResource UIDiggingLevelReward = new UIResource("UIDiggingLevelReward.prefab", AboveStatus, "event_digging_common");
        public static UIResource UIDiggingBegin = new UIResource("UIDiggingBegin.prefab", AboveStatus, "event_digging_default");
        public static UIResource UIDiggingNewRound = new UIResource("UIDiggingNewRound.prefab", AboveStatus, "event_digging_default");
        public static UIResource UIDiggingEnd = new UIResource("UIDiggingEnd.prefab", AboveStatus, "event_digging_default");
        public static UIResource UIDiggingHelp = new UIResource("UIDiggingHelp.prefab", AboveStatus, "event_digging_default").SupportNavBack();
        public static UIResource UIDiggingGift = new UIResource("UIDiggingGift.prefab", AboveStatus, "event_digging_default").SupportNavBack();
        public static UIResource UIDiggingLoading = new UIResource("UIDiggingLoading.prefab", Loading, "event_digging_default").SetMute();
        #endregion

        public static UIResource UICommonRewardTips = new UIResource("UICommonRewardTips.prefab", SubStatus, "fat_global_ext").SupportNavBack().IsTips();
        public static UIResource UICommonShowRes = new UIResource("UICommonShowRes.prefab", Top, "fat_global_ext").IgnoreNavBack();

        #region 小游戏相关界面

        public static UIResource UIBeadsSelect = new UIResource("UIBeadsSelect.prefab", AboveStatus, "minigame_beads").AllowHideUI().SupportNavBack();
        public static UIResource UIBeads = new UIResource("UIBeads.prefab", SubStatus, "minigame_beads").AllowHideUI();
        public static UIResource UIBeadsGuide = new UIResource("UIBeadsGuide.prefab", SubStatus, "minigame_beads").AllowHideUI();
        public static UIResource UIBeadsResult = new UIResource("UIBeadsResult.prefab", SubStatus, "minigame_beads").AllowHideUI();

        public static UIResource UIPachinkoLoading = new UIResource("UIPachinkoLoading.prefab", Loading, "event_pachinko_s001").SetMute();
        public static UIResource UIPachinkoMain = new UIResource("UIPachinkoMain.prefab", UILayer.AboveStatus, "event_pachinko_s001").SetMute();
        public static UIResource UIPachinkoPopFly = new UIResource("UIPachinkoPopFly.prefab", UILayer.SubStatus, "event_pachinko_common");
        public static UIResource UIPachinkoMultipleTips = new UIResource("UIPachinkoMultipleTips.prefab", UILayer.SubStatus, "event_pachinko_common").SetMute().SupportNavBack().IsTips();
        public static UIResource UIPhysics2DTest = new UIResource("UIPhysics2DTest.prefab", UILayer.AboveStatus, "event_pachinko_common");
        public static UIResource UIPachinkoDebug = new UIResource("UIPachinkoDebug.prefab", UILayer.Top, "event_pachinko_common");
        public static UIResource UIPachinkoStartNotice = new UIResource("UIPachinkoStartNotice.prefab", AboveStatus, "event_pachinko_common").AllowHideUI().SupportNavBack();
        public static UIResource UIPachinkoLevelReward = new("UIPachinkoLevelReward.prefab", SubStatus, "event_pachinko_common");
        public static UIResource UIPachinkoRestartNotice = new UIResource("UIPachinkoRestartNotice.prefab", AboveStatus, "event_pachinko_common").AllowHideUI().SupportNavBack();
        public static UIResource UIPachinkoConvert = new UIResource("UIPachinkoConvert.prefab", AboveStatus, "event_pachinko_common").AllowHideUI();
        public static UIResource UIPachinkoHelp = new("UIPachinkoHelp.prefab", AboveStatus, "event_pachinko_s001");
        public static UIResource UINoticeMagicHour = new("UINoticeMagicHour.prefab", AboveStatus, "event_magichour_default");
        public static UIResource UIMagicHourHelp = new("UIMagicHourHelp.prefab", AboveStatus, "event_magichour_default");

        #endregion

        #region 合成大西瓜
        public static UIResource UISlideMergeMain = new UIResource("UISlideMergeMain.prefab", AboveStatus, "minigame_slidemerge");
        public static UIResource UISlideMergeSelect = new UIResource("UISlideMergeSelect.prefab", AboveStatus, "minigame_slidemerge");
        public static UIResource UISlideMergeResult = new UIResource("UISlideMergeResult.prefab", AboveStatus, "minigame_slidemerge");
        #endregion

        #region 连续限时订单活动 （零度挑战）

        public static UIResource UIOrderChallengeStart =
            new("UIOrderChallengeStart.prefab", AboveStatus, "event_orderchallenge_default");

        public static UIResource UIOrderChallengeMain =
            new("UIOrderChallengePanel.prefab", AboveStatus, "event_orderchallenge_default");

        public static UIResource UIOrderChallengeMatch =
            new UIResource("UIOrderChallengeMatch.prefab", SubStatus, "event_orderchallenge_default").SupportNavBack();

        public static UIResource UIOrderChallengeVictory =
            new("UIOrderChallengeVictory.prefab", SubStatus, "event_orderchallenge_default");

        public static UIResource UIOrderChallengeHelp =
            new UIResource("UIOrderChallengeHelp.prefab", SubStatus, "event_orderchallenge_default").SupportNavBack();

        public static UIResource UIOrderChallengeTips =
            new("UIOrderChallengeTips.prefab", SubStatus, "event_orderchallenge_default");

        public static UIResource UIOrderChallengeFail =
            new("UIOrderChallengeFail.prefab", SubStatus, "event_orderchallenge_default");

        #endregion
        #region 盖章活动
        public static UIResource UICardStamp = new UIResource("UICardStamp.prefab", AboveStatus, "event_cardstamp_default");
        #endregion

        #region 砍价礼包
        public static UIResource UIPackDiscount = new UIResource("UIPackDiscount.prefab", AboveStatus, "event_packdiscount_default");
        public static UIResource UIPackDiscountHelp = new UIResource("UIPackDiscountHelp.prefab", AboveStatus, "event_packdiscount_default");
        #endregion
        #region 路径活动（每日任务）
        public static UIResource UILandMark = new UIResource("UILandMark.prefab", AboveStatus, "event_landmark_s001").SupportNavBack();
        #endregion

        #region 拼图活动
        public static UIResource UIActivityPuzzleMain = new UIResource("UIActivityPuzzleMain.prefab", AboveStatus, "event_puzzle_default");
        public static UIResource UIActivityPuzzleConvert = new UIResource("UIActivityPuzzleConvert.prefab", AboveStatus, "event_puzzle_default");
        #endregion

        #region 好评订单活动
        public static UIResource UIOrderLike = new UIResource("UIOrderLike.prefab", AboveStatus, "event_orderlike_default");
        public static UIResource UIOrderLikeStart = new UIResource("UIOrderLikeStart.prefab", AboveStatus, "event_orderlike_default");
        public static UIResource UIOrderLikeHelp = new UIResource("UIOrderLikeHelp.prefab", AboveStatus, "event_orderlike_default");
        public static UIResource UIOrderLikeEntryTips = new UIResource("UIOrderLikeEntryTips.prefab", SubStatus, "event_orderlike_default");
        #endregion

        #region 钓鱼棋盘
        public static UIResource UIActivityFishEnd = new UIResource("UIActivityFishEnd.prefab", AboveStatus, "event_fish_default").SupportNavBack();
        public static UIResource UIActivityFishConvert = new UIResource("UIActivityFishConvert.prefab", AboveStatus, "event_fish_default");
        public static UIResource UIActivityFishHelp = new UIResource("UIActivityFishHelp.prefab", AboveStatus, "event_fish_default").SupportNavBack();
        public static UIResource UIActivityFishMain = new UIResource("UIActivityFishMain.prefab", BelowStatus, "event_fish_default").SupportNavBack();
        public static UIResource UIActivityFishBegin = new UIResource("UIActivityFishBegin.prefab", AboveStatus, "event_fish_default").SupportNavBack();
        public static UIResource UIActivityFishCollect = new UIResource("UIActivityFishCollect.prefab", SubStatus, "event_fish_default").SupportNavBack();
        public static UIResource UIActivityFishGet = new UIResource("UIActivityFishGet.prefab", SubStatus, "event_fish_default").SupportNavBack();
        public static UIResource UIActivityFishMilestone = new UIResource("UIActivityFishMilestone.prefab", SubStatus, "event_fish_default").SupportNavBack();
        public static UIResource UIActivityFishReward = new UIResource("UIActivityFishReward.prefab", SubStatus, "event_fish_default").SupportNavBack();
        public static UIResource UIActivityFishTips = new UIResource("UIActivityFishTips.prefab", SubStatus, "event_fish_common").SupportNavBack();
        public static UIResource UIActivityFishLoading = new UIResource("UIActivityFishLoading.prefab", Loading, "event_fish_default");
        public static UIResource UIFishRewardTips = new UIResource("UIFishRewardTips.prefab", SubStatus, "event_fish_common").SupportNavBack();
        #endregion

        #region 打怪棋盘
        public static UIResource UIActivityFightMain = new UIResource("UIActivityFightMain.prefab", BelowStatus, "event_fight_default").SupportNavBack();
        public static UIResource UIActivityFightReward = new UIResource("UIActivityFightReward.prefab", SubStatus, "event_fight_default").SupportNavBack();
        public static UIResource UIActivityFightLoading = new UIResource("UIActivityFightLoading.prefab", Loading, "event_fight_default");
        public static UIResource UIActivityFightHelp = new UIResource("UIActivityFightHelp.prefab", SubStatus, "event_fight_default").SupportNavBack();
        public static UIResource UIActivityFightMilestone = new UIResource("UIActivityFightMilestone.prefab", SubStatus, "event_fight_default").SupportNavBack();
        public static UIResource UIActivityFightConvert = new UIResource("UIActivityFightConvert.prefab", AboveStatus, "event_fight_default");
        public static UIResource UIActivityFightEnd = new UIResource("UIActivityFightEnd.prefab", AboveStatus, "event_fight_default").SupportNavBack();
        public static UIResource UIActivityFightBegin = new UIResource("UIActivityFightBegin.prefab", AboveStatus, "event_fight_default").SupportNavBack();
        public static UIResource UIActivityFightMilestoneTips = new UIResource("UIActivityFightMilestoneTips.prefab", SubStatus, "event_fight_default").SupportNavBack();
        #endregion


        #region 火车任务
        public static UIResource UITrainMissionMain = new UIResource("UITrainMissionMain_s001.prefab", BelowStatus, "event_trainmission_s001").SupportNavBack();
        public static UIResource UITrainMissionBegin = new UIResource("UITrainMissionBegin_s001.prefab", AboveStatus, "event_trainmission_s001").SupportNavBack();
        public static UIResource UITrainMissionChooseGroup = new UIResource("UITrainMissionChooseGroup_s001.prefab", SubStatus, "event_trainmission_s001").SupportNavBack();
        public static UIResource UITrainMissionComplete = new UIResource("UITrainMissionComplete_s001.prefab", SubStatus, "event_trainmission_s001");
        public static UIResource UITrainMissionEnd = new UIResource("UITrainMissionEnd_s001.prefab", SubStatus, "event_trainmission_s001");
        public static UIResource UITrainMissionHelp = new UIResource("UITrainMissionHelp_s001.prefab", SubStatus, "event_trainmission_s001");
        public static UIResource UITrainMissionLoading = new UIResource("UITrainMissionLoading_s001.prefab", SubStatus, "event_trainmission_s001").SetMute();
        public static UIResource UITrainMissionPreview = new UIResource("UITrainMissionPreview_s001.prefab", SubStatus, "event_trainmission_s001").SupportNavBack();
        public static UIResource UITrainMissionItemInfo = new UIResource("UITrainMissionItemInfo_s001.prefab", SubStatus, "event_trainmission_s001");
        public static UIResource UITrainMissionReward = new UIResource("UITrainMissionReward_s001.prefab", SubStatus, "event_trainmission_s001");
        public static UIResource UITrainMissionBag = new UIResource("UITrainMissionBag.prefab", SubStatus, "event_trainmission_default");
        public static UIResource UITrainMissionCompleteOrderBag = new UIResource("UITrainMissionCompleteOrderBag.prefab", SubStatus, "event_trainmission_default");
        public static UIResource UITrainMissionRecycleReward = new UIResource("UITrainMissionRecycleReward_s001.prefab", SubStatus, "event_trainmission_s001");
        #endregion

        #region 周任务
        public static UIResource UIActivityWeeklyTaskMain = new UIResource("UIActivityWeeklyTaskMain.prefab", AboveStatus, "event_weeklytask_default").SupportNavBack();
        public static UIResource UIActivityWeeklyTaskEnd = new UIResource("UIActivityWeeklyTaskEnd.prefab", AboveStatus, "event_weeklytask_default");
        public static UIResource UIActivityWeeklyTaskHelp = new UIResource("UIActivityWeeklyTaskHelp.prefab", SubStatus, "event_weeklytask_default").SupportNavBack();
        public static UIResource UIActivityWeeklyTaskReward = new UIResource("UIActivityWeeklyTaskReward.prefab", SubStatus, "event_weeklytask_default");
        public static UIResource UIActivityWeeklyTaskRewardTips = new UIResource("UIActivityWeeklyTaskRewardTips.prefab", SubStatus, "event_weeklytask_default").SupportNavBack();
        public static UIResource UIActivityWeeklyTaskNotice = new UIResource("UIActivityWeeklyTaskNotice.prefab", Top, "event_weeklytask_default").SupportNavBack();
        #endregion

        #region 连续订单活动
        public static UIResource UIActivityOrderStreakMain = new UIResource("UIActivityOrderStreakMain.prefab", AboveStatus, "event_orderstreak_default").SupportNavBack();
        public static UIResource UIActivityOrderStreakHelp = new UIResource("UIActivityOrderStreakHelp.prefab", SubStatus, "event_orderstreak_default").SupportNavBack();
        public static UIResource UIActivityOrderStreakConvert = new UIResource("UIActivityOrderStreakConvert.prefab", AboveStatus, "event_orderstreak_default");
        public static UIResource UIActivityOrderStreakRewardTips = new UIResource("UIActivityOrderStreakRewardTips.prefab", SubStatus, "event_orderstreak_default").SupportNavBack();
        #endregion

        #region bingo活动

        public static UIResource UIBingoEnd = new("UIBingoEnd.prefab", AboveStatus, "event_bingo_default");
        public static UIResource UIBingoHelp = new UIResource("UIBingoHelp.prefab", SubStatus, "event_bingo_default").SupportNavBack();
        public static UIResource UIBingoItem = new UIResource("UIBingoItem.prefab", SubStatus, "event_bingo_default").SupportNavBack();
        public static UIResource UIBingoMain = new UIResource("UIBingoMain.prefab", AboveStatus, "event_bingo_default").SupportNavBack();
        public static UIResource UIBingoGuide = new UIResource("UIBingoGuide.prefab", SubStatus, "event_bingo_default").SupportNavBack();
        public static UIResource UIBingoSpawnerInfo = new UIResource("UIBingoSpawnerInfo.prefab", SubStatus, "event_bingo_default").SupportNavBack();

        #endregion

        #region Bingo任务
        public static UIResource UIBingoTaskMain = new UIResource("UIBingoTaskMain_s001.prefab", AboveStatus, "event_bingotask_s001").SupportNavBack();
        public static UIResource UIBingoTaskHelp = new UIResource("UIBingoTaskHelp_s001.prefab", SubStatus, "event_bingotask_s001").SupportNavBack();
        public static UIResource UIBingoTaskBingo = new UIResource("UIBingoTaskBingo.prefab", SubStatus, "event_bingotask_common");
        public static UIResource UIBingoTaskItemTips = new UIResource("UIBingoTaskItemTips.prefab", SubStatus, "event_bingotask_common").IsTips();
        public static UIResource UIBingoTaskEnd = new UIResource("UIBingoTaskEnd_s001.prefab", AboveStatus, "event_bingotask_s001");
        #endregion


        #region 挖矿棋盘
        public static UIResource UIMineBoardMain = new UIResource("UIMineBoardMain.prefab", BelowStatus, "event_mineboard_s001");
        public static UIResource UIMineBoardMilestoneReward = new UIResource("UIMineBoardMilestoneReward.prefab", SubStatus, "event_mineboard_common");
        public static UIResource UIMineBoardStartNotice = new UIResource("UIMineBoardStartNotice.prefab", AboveStatus, "event_mineboard_s001");
        public static UIResource UIMineBoardEndNotice = new UIResource("UIMineBoardEndNotice.prefab", AboveStatus, "event_mineboard_s001");
        public static UIResource UIMineBoardReplacement = new UIResource("UIMineBoardReplacement.prefab", SubStatus, "event_mineboard_s001");
        public static UIResource UIMineHandbook = new UIResource("UIMineHandbook.prefab", SubStatus, "event_mineboard_common").SupportNavBack();
        public static UIResource UIMineBoardMilestone = new UIResource("UIMineBoardMilestone.prefab", SubStatus, "event_mineboard_s001");
        public static UIResource UIMineBoardHelp = new UIResource("UIMineBoardHelp.prefab", SubStatus, "event_mineboard_s001");
        public static UIResource UIMineBoardMilestoneTips = new UIResource("UIMineBoardMilestoneTips.prefab", SubStatus, "event_mineboard_s001");
        public static UIResource UIMineLoading = new UIResource("UIMineLoading.prefab", Loading, "event_mineboard_s001").SetMute();
        public static UIResource UIMineRewardTips = new UIResource("UIMineRewardTips.prefab", SubStatus, "event_mineboard_common");
        public static UIResource UIMineTokenTips = new UIResource("UIMineTokenTips.prefab", SubStatus, "event_mineboard_common");

        #endregion

        #region 农场棋盘
        public static UIResource UIFarmBoardMain = new UIResource("UIFarmBoardMain.prefab", BelowStatus, "event_farmboard_s001");
        public static UIResource UIFarmBoardBegin = new UIResource("UIFarmBoardBegin.prefab", AboveStatus, "event_farmboard_s001");
        public static UIResource UIFarmBoardEnd = new UIResource("UIFarmBoardEnd.prefab", AboveStatus, "event_farmboard_s001").SupportNavBack();
        public static UIResource UIFarmBoardHelp = new UIResource("UIFarmBoardHelp.prefab", AboveStatus, "event_farmboard_s001").SupportNavBack();
        public static UIResource UIFarmBoardConvert = new UIResource("UIFarmBoardConvert.prefab", AboveStatus, "event_farmboard_s001");
        public static UIResource UIFarmBoardComplete = new UIResource("UIFarmBoardComplete.prefab", AboveStatus, "event_farmboard_s001").SupportNavBack();
        public static UIResource UIFarmBoardTokenTips = new UIResource("UIFarmBoardTokenTips.prefab", SubStatus, "event_farmboard_s001").SupportNavBack().IsTips();
        public static UIResource UIFarmBoardAnimalTips = new UIResource("UIFarmBoardAnimalTips.prefab", SubStatus, "event_farmboard_s001").SupportNavBack().IsTips();
        public static UIResource UIFarmBoardGetSeed = new UIResource("UIFarmBoardGetSeed.prefab", SubStatus, "event_farmboard_s001").SupportNavBack();
        public static UIResource UIFarmBoardLoading = new UIResource("UIFarmBoardLoading.prefab", Loading, "event_farmboard_s001");
        #endregion
        #region  进度礼盒
        public static UIResource UIOrderRateMain = new UIResource("UIOrderRatePanel.prefab", AboveStatus, "event_orderrate_default");
        public static UIResource UIOrderRateStart = new UIResource("UIOrderRateReward.prefab", SubStatus, "event_orderrate_default");
        public static UIResource UIOrderRateShow = new UIResource("UIOrderRateShow.prefab", SubStatus, "event_orderrate_default");
        public static UIResource OrderRateTip = new UIResource("OrderRateTip.prefab", SubStatus, "event_orderrate_default");
        public static UIResource UIOrderRateInfo = new UIResource("UIOrderRateInfo.prefab", SubStatus, "event_orderrate_default");
        #endregion

        #region 签到
        public static UIResource UISignInpanel = new UIResource("UISignInpanel.prefab", AboveStatus, "event_signin_default");
        public static UIResource UISignInReward = new UIResource("UISignInReward.prefab", SubStatus, "event_signin_default");

        #endregion

        #region 签到抽奖
        public static UIResource UIActivityWeeklyRaffleMain = new UIResource("UIActivityWeeklyRaffleMain.prefab", AboveStatus, "event_weeklyraffle_default").SupportNavBack();
        public static UIResource UIActivityWeeklyRaffleConvert = new UIResource("UIActivityWeeklyRaffleConvert.prefab", AboveStatus, "event_weeklyraffle_default");
        public static UIResource UIActivityWeeklyRaffleHelp = new UIResource("UIActivityWeeklyRaffleHelp.prefab", SubStatus, "event_weeklyraffle_default").SupportNavBack();
        public static UIResource UIActivityWeeklyRaffleBuyToken = new UIResource("UIActivityWeeklyRaffleBuyToken.prefab", SubStatus, "event_weeklyraffle_default").SupportNavBack();
        public static UIResource UIActivityWeeklyRaffleDraw = new UIResource("UIActivityWeeklyRaffleDraw.prefab", SubStatus, "event_weeklyraffle_default").SupportNavBack();
        public static UIResource UIActivityWeeklyRaffleRewardTips = new UIResource("UIActivityWeeklyRaffleRewardTips.prefab", SubStatus, "event_weeklyraffle_default").SupportNavBack().IsTips();
        #endregion

        #region 三日签到
        public static UIResource UIThreeSign = new UIResource("UIThreeSign.prefab", AboveStatus, "event_threesign_default");
        #endregion

        #region 排行榜里程碑
        public static UIResource UIActivityRankMilestone = new UIResource("UIActivityRankMilestone.prefab", AboveStatus, "event_ranking_default").SupportNavBack().AllowHideUI();
        public static UIResource UIActivityRankMilestoneHelp = new UIResource("UIActivityMilestoneRankingHelp.prefab", AboveStatus, "event_ranking_default").SupportNavBack().AllowHideUI();
        public static UIResource UIActivityRankMilestoneReward = new UIResource("UIActivityRankingMilestoneReward.prefab", AboveStatus, "event_ranking_default").SupportNavBack().AllowHideUI();

        #endregion

        #region 兑换商店
        public static UIResource UIRedeemShopMain = new UIResource("UIRedeemShopMain.prefab", AboveStatus, "event_shopredeem_default").SupportNavBack().AllowHideUI();
        public static UIResource UINoticeRedeemShop = new UIResource("UINoticeRedeemShop.prefab", SubStatus, "event_shopredeem_default");
        public static UIResource UIRedeemShopHelp = new UIResource("UIRedeemShopHelp.prefab", AboveStatus, "event_shopredeem_default").SupportNavBack();
        public static UIResource UIRedeemShopStageReward = new UIResource("UIRedeemShopStageReward.prefab", SubStatus, "event_shopredeem_default").SupportNavBack();
        public static UIResource UIRedeemShopSettlement = new UIResource("UIRedeemShopSettlement.prefab", SubStatus, "event_shopredeem_default");

        #endregion


        #region BP
        public static UIResource UIBPStart = new UIResource("UIBPStart.prefab", SubStatus, "event_bp_common");
        public static UIResource UIBPMain = new UIResource("UIBPMain_s001.prefab", AboveStatus, "event_bp_s001").SupportNavBack();
        public static UIResource UIBPTaskComplete = new UIResource("UIBPTaskComplete.prefab", SubStatus, "event_bp_common").SupportNavBack();
        public static UIResource UIBPEnd = new UIResource("UIBPEnd.prefab", SubStatus, "event_bp_common");
        public static UIResource UIBPBuyBoth = new UIResource("UIBPBuyBoth.prefab", SubStatus, "event_bp_common").SupportNavBack();
        public static UIResource UIBPBuyUpgrade = new UIResource("UIBPBuyUpgrade.prefab", SubStatus, "event_bp_common").SupportNavBack();
        public static UIResource UIBPBuyBothPop = new UIResource("UIBPBuyBothPop.prefab", SubStatus, "event_bp_common").SupportNavBack();
        public static UIResource UIBPBuyOneSuccess = new UIResource("UIBPBuyOneSuccess.prefab", SubStatus, "event_bp_common").SupportNavBack().SetMute();
        public static UIResource UIBPBuyTwoSuccess = new UIResource("UIBPBuyTwoSuccess.prefab", SubStatus, "event_bp_common").SupportNavBack().SetMute();
        public static UIResource UIBPDoubleCheck = new UIResource("UIBPDoubleCheck.prefab", SubStatus, "event_bp_common").SupportNavBack();
        public static UIResource UIBPReward = new UIResource("UIBPReward.prefab", SubStatus, "event_bp_common").SupportNavBack().SetMute();
        public static UIResource UIBPRewardTip = new UIResource("UIBPRewardTip.prefab", SubStatus, "event_bp_common").SupportNavBack().IsTips().SetMute();
        public static UIResource UIBPMileStoneTip = new UIResource("UIBPMileStoneTip.prefab", SubStatus, "event_bp_common").SupportNavBack().IsTips();
        #endregion


        #region 订单助力
        public static UIResource UIOrderBonusReward = new UIResource("UIOrderbonusReward.prefab", SubStatus, "event_orderbonus_default");
        public static UIResource UIOrderBonusPanel = new UIResource("UIOrderBonusPanel.prefab", AboveStatus, "event_orderbonus_default");
        public static UIResource UIOrderBonusTips = new UIResource("UIOrderBonusTips.prefab", SubStatus, "event_orderbonus_default");
        public static UIResource UIOrderBonusRewardTips = new UIResource("UIOrderBonusRewardTips.prefab", SubStatus, "event_orderbonus_default");
        #endregion

        #region 抓宝订单
        public static UIResource UIClawOrderPanel = new UIResource("UIClawOrderPanel.prefab", AboveStatus, "event_claworder_default").SupportNavBack();
        public static UIResource UIClawOrderHelp = new UIResource("UIClawOrderHelp.prefab", AboveStatus, "event_claworder_default").SupportNavBack();
        public static UIResource UIClawOrderEnd = new UIResource("UIClawOrderEnd.prefab", AboveStatus, "event_claworder_default");
        public static UIResource UIClawOrderTips = new UIResource("UIClawOrderTips.prefab", SubStatus, "event_claworder_default").SupportNavBack();
        #endregion

        #region  邮件补单
        public static UIResource UIMailHasRewardReshipment = new UIResource("UIMailHasRewardReshipment.prefab", AboveStatus, "fat_global");
        public static UIResource UIMailNotRewardReshipment = new UIResource("UIMailNotRewardReshipment.prefab", AboveStatus, "fat_global");
        #endregion

        #region 等级礼包
        
        public static UIResource UILevelPackPanel = new UIResource("UILevelPackPanel_s001.prefab", AboveStatus, "event_levelpack_s001").SupportNavBack().AllowHideUI();
        #endregion
        
        #region 许愿棋盘
        public static UIResource UIWishBoardMain = new UIResource("UIWishBoardMain.prefab", BelowStatus, "event_wishboard_default").SupportNavBack();
        public static UIResource UIWishBoardHelp = new UIResource("UIWishBoardHelp.prefab", AboveStatus, "event_wishboard_default").SupportNavBack();
        public static UIResource UIWishBoardHandbook = new UIResource("UIWishBoardHandbook.prefab", AboveStatus, "event_wishboard_default").SupportNavBack();
        public static UIResource UIWishBoardMilestone = new UIResource("UIWishBoardMilestone.prefab", AboveStatus, "event_wishboard_default").SupportNavBack();
        public static UIResource UIWishBoardStartNotice = new UIResource("UIWishBoardStartNotice.prefab", AboveStatus, "event_wishboard_default");
        public static UIResource UIWishBoardEndNotice = new UIResource("UIWishBoardEndNotice.prefab", AboveStatus, "event_wishboard_default").SupportNavBack();
        public static UIResource UIWishBoardConvert = new UIResource("UIWishBoardConvert.prefab", AboveStatus, "event_wishboard_default");
        public static UIResource UIWishBoardHandbookTips = new UIResource("UIWishBoardHandbookTips.prefab", AboveStatus, "event_wishboard_default");
        public static UIResource UIWishBoardLoading = new UIResource("UIWishBoardLoading.prefab", Loading, "event_wishboard_default");
        public static UIResource UIWishBoardMilestoneTips = new UIResource("UIWishBoardMilestoneTips.prefab", SubStatus, "event_wishboard_default").SupportNavBack().IsTips();
        public static UIResource UIWishBoardTip = new UIResource("UIWishBoardTip.prefab", SubStatus, "event_wishboard_default");
        #endregion

        #region 社区计划
        public static UIResource UICommunityPlanReward = new UIResource("UICommunityPlanReward.prefab", SubStatus, "fat_global");
        public static UIResource UICommunityPlanGiftReward = new UIResource("UICommunityPlanGiftReward.prefab", AboveStatus, "fat_global");
        public static UIResource UICommunityMailStartNotice = new UIResource("UICommunityMailStartNotice.prefab", AboveStatus, "fat_global");
        #endregion
        #region 跑马灯抽奖
        public static UIResource UISpinPackPanel = new UIResource("UISpinPackPanel.prefab", AboveStatus, "event_spinpack_s001");
        public static UIResource UISpinPackTip = new UIResource("UISpinPackTips.prefab", SubStatus, "event_spinpack_default");
        public static UIResource UISpinRewardPanel = new UIResource("UISpinRewardPanel.prefab", SubStatus, "event_spinpack_default");
        #endregion

        #region 矿车棋盘
        public static UIResource UIMineCartBoardStartNotice = new UIResource("UIMineCartBoardStartNotice.prefab", AboveStatus, "event_minecartboard_common").SupportNavBack();
        public static UIResource UIMineCartBoardEndNotice = new UIResource("UIMineCartBoardEndNotice.prefab", AboveStatus, "event_minecartboard_common").SupportNavBack();
        public static UIResource UIMineCartRewardTips = new UIResource("UIMineCartRewardTips.prefab", SubStatus, "event_minecartboard_common").IsTips().SupportNavBack();
        public static UIResource UIMineCartBoardMilestoneReward = new UIResource("UIMineCartBoardMilestoneReward.prefab", SubStatus, "event_minecartboard_common").SupportNavBack();
        public static UIResource UIMineCartHandbook = new UIResource("UIMineCartHandbook.prefab", AboveStatus, "event_minecartboard_common").SupportNavBack();
        public static UIResource UIMineCartBoardReplacement = new UIResource("UIMineCartBoardReplacement.prefab", AboveStatus, "event_minecartboard_common").SupportNavBack();
        public static UIResource UIMineCartBoardMain = new UIResource("UIMineCartBoardMain.prefab", BelowStatus, "event_minecartboard_s001");
        public static UIResource UIMineCartLoading = new UIResource("UIMineCartLoading.prefab", Loading, "event_minecartboard_s001");
        public static UIResource UIMineCartBoardHelp = new UIResource("UIMineCartBoardHelp.prefab", SubStatus, "event_minecartboard_common").SupportNavBack();
        public static UIResource UIMineCartBoardBannerTip = new UIResource("UIMineCartBoardBannerTip.prefab", SubStatus, "event_minecartboard_common");
        #endregion

        #region 倍率排行榜
        public static UIResource UIMultiplyRankingMain = new UIResource("UIMultiplyRankingMain.prefab", AboveStatus, "event_multiplyranking_default").SupportNavBack();
        public static UIResource UIMultiplyRankingStart = new UIResource("UIMultiplyRankingStart.prefab", AboveStatus, "event_multiplyranking_default");
        public static UIResource UIMultiplyRankingEnd = new UIResource("UIMultiplyRankingEnd.prefab", AboveStatus, "event_multiplyranking_default");
        public static UIResource UIMultiplyRankingHelp = new UIResource("UIMultiplyRankingHelp.prefab", AboveStatus, "event_multiplyranking_default").SupportNavBack();
        public static UIResource UIMultiplyRankingEndReward = new UIResource("UIMultiplyRankingEndReward.prefab", AboveStatus, "event_multiplyranking_default");
        public static UIResource UIMultiplyRankingMilestone = new UIResource("UIMultiplyRankingMilestone.prefab", SubStatus, "event_multiplyranking_default").SupportNavBack();
        public static UIResource UIRankingEntryTips = new UIResource("UIRankingEntryTips.prefab", SubStatus, "event_multiplyranking_default").SupportNavBack().IsTips();
        public static UIResource UIRankingTurntableTips = new UIResource("UIRankingTurntableTips.prefab", SubStatus, "event_multiplyranking_default").SupportNavBack().IsTips();
        #endregion
    }
}
