/*
 * @Author: qun.chao
 * @Date: 2023-11-23 14:13:37
 */
using UnityEngine;
using fat.rawdata;

namespace FAT
{
    public class GuideActionFactory
    {
        public GuideActImpBase CreateGuideAction(GuideMergeAction action)
        {
            GuideActImpBase imp = null;
            switch (action.Act)
            {
                case "add_reward": imp = new GuideActImpAddReward(); break;
                case "add_energy4x_reward": imp = new GuideActImpAddEnergyBoostUnlock4xReward(); break;
                case "block": imp = new GuideActImpBlockCtrl(); break;
                case "bingo_hand_pro": imp = new GuideActImpBingoHandPro(); break;
                case "board_dialog": imp = new GuideActImpBoardDialog(); break;
                case "board_dialog_hide": imp = new GuideActImpBoardDialogHide(); break;
                case "board_drag": imp = new GuideActImpBoardDrag(); break;
                case "board_drag_pos": imp = new GuideActImpBoardDragPos(); break;
                case "board_eat": imp = new GuideActImpBoardEat(); break;
                case "board_match": imp = new GuideActImpBoardMatch(); break;
                case "board_merge": imp = new GuideActImpBoardMerge(); break;
                case "board_select_hand": imp = new GuideActImpBoardSelectHand(); break;
                case "board_select_hand_pos": imp = new GuideActImpBoardSelectHandPos(); break;
                case "board_tap": imp = new GuideActImpBoardTap(); break;
                case "board_tap_boost": imp = new GuideActImpBoardTapBoost(); break;
                case "board_to_bag": imp = new GuideActImpBoardToBag(); break;  // 自带mask显示
                case "board_to_custom": imp = new GuideActImpBoardToCustom(); break;  // 自带mask显示
                case "board_unlock": imp = new GuideActImpBoardUnlock(); break;
                case "board_use_hand": imp = new GuideActImpBoardUseHand(); break;
                case "board_use_hand_crood": imp = new GuideActImpBoardUseHandCrood(); break;
                case "bubble_show": imp = new GuideActImpBubbleShow(); break;
                case "building_focus": imp = new GuideActImpBuildingFocus(); break;
                case "building_select": imp = new GuideActImpBuildingSelect(); break;
                case "find_decorate_entry": imp = new GuideActImpFindDecorateEntry(); break;
                case "finish_guide": imp = new GuideActImpFinishGuide(); break;
                case "first_active_set": imp = new GuideActImpFirstActiveSet(); break;
                case "first_enable_deco": imp = new GuideActImpFirstEnableDeco(); break;
                case "hand": imp = new GuideActImpHand(); break;
                case "hand_offset": imp = new GuideActImpHandOffsetForUI(); break;
                case "hand_free": imp = new GuideActImpHandFree(); break;
                case "hand_hide": imp = new GuideActImpHandHide(); break;
                case "hand_order_finish": imp = new GuideActImpOrderFinishHand(); break;
                case "hand_pro": imp = new GuideActImpHandPro(); break;
                case "hand_mine": imp = new GuideActImpHandMine(); break;
                case "hand_fish": imp = new GuideActImpHandFish(); break;
                case "hand_pro_sceneui": imp = new GuideActImpHandProScene(); break;
                case "hand_guess": imp = new GuideActImpHandGuess(); break;
                case "mask_item_show": imp = new GuideActImpMaskItemShow(); break;
                case "mask_item_hide": imp = new GuideActImpMaskItemHide(); break;
                case "mask_board_show": imp = new GuideActImpMaskShowBoard(); break;
                case "mask_hide": imp = new GuideActImpMaskHide(); break;
                case "mask_rect_hide": imp = new GuideActImpMaskRectHide(); break;
                case "mask_rect_show": imp = new GuideActImpMaskRectShow(); break;
                case "mask_show": imp = new GuideActImpMaskShow(); break;
                case "mask_show_fish": imp = new GuideActImpMaskShowFish(); break;
                case "merge_ctrl": imp = new GuideActImpMergeCtrl(); break;
                case "merge_mini": imp = new GuideActImpMiniBoardMerge(); break;
                case "mini_multi_next_round": imp = new GuideActImpMiniBoardEnterNext(); break;
                case "talk_coord": imp = new GuideActImpTalkCoord(); break;
                case "talk_coord_next": imp = new GuideActImpTalkCoordNext(); break;
                case "talk_coord_auto": imp = new GuideActImpTalkCoordAuto(); break;
                case "talk_coord_next_auto": imp = new GuideActImpTalkCoordNextAuto(); break;
                case "talk_hide": imp = new GuideActImpTalkHide(); break;
                case "open_help_card_trade": imp = new GuideActImpOpenHelpCardTrade(); break;
                case "open_ui_album_unlock": imp = new GuideActImpOpenAlbumUnlock(); break;
                case "open_ui_album_start": imp = new GuideActImpOpenAlbumStart(); break;
                case "open_ui_bingo_guide": imp = new GuideActImpOpenBingoGuide(); break;
                case "open_ui_bingo_help": imp = new GuideActImpOpenBingoHelp(); break;
                case "open_ui_boost": imp = new GuideActImpOpenUIBoost(); break;
                case "open_ui_boost_4x": imp = new GuideActImpOpenEnergyBoostUnlock4X(); break;
                case "open_ui_buy_energy": imp = new GuideActImpOpenUIEnergy(); break;
                case "farm_open_help": imp = new GuideActImpFarmOpenHelp(); break;
                case "open_ui_dem_tutorial": imp = new GuideActImpOpenUIDEMTutorial(); break;
                case "open_ui_digging_help": imp = new GuideActImpOpenUIDiggingHelp(); break;
                case "open_ui_pachinko_help": imp = new GuideActImpOpenUIPachinkoHelp(); break;
                case "open_ui_guess_help": imp = new GuideActImpOpenGuessHelp(); break;
                case "open_ui_multi_help": imp = new GuideActImpUIMultihelp(); break;
                case "open_ui_wish_help": imp = new GuideActImpOpenWishHelp(); break;
                case "plot": imp = new GuideActImpPlot(); break;
                case "save": imp = new GuideActImpSave(); break;
                case "source_ctrl": imp = new GuideActImpSourceCtrl(); break;
                case "top_dialog": imp = new GuideActImpTopDialog(); break;
                case "top_dialog_hide": imp = new GuideActImpTopDialogHide(); break;
                case "wait": imp = new GuideActImpWait(); break;
                case "wait_click": imp = new GuideActImpWaitClick(); break;
                case "wait_item": imp = new GuideActImpWaitItem(); break;
                case "wait_item_pos": imp = new GuideActImpWaitItemPos(); break;
                case "wait_pos_unlock": imp = new GuideActImpWaitPosUnlock(); break;
                case "wait_produce_count": imp = new GuideActImpWaitProduceCount(); break;
                case "wait_select": imp = new GuideActImpWaitSelect(); break;
                case "wait_uistate": imp = new GuideActWaitUIState(); break;
                case "token_mask": imp = new GuideActImpTokenMask(); break; // 签到抽奖活动

                #region sp
                case "claw_order_mask_show": imp = new GuideActImpClawOrderMaskShow(); break;
                case "claw_order_help": imp = new GuideActImpClawOrderHelp(); break;
                case "gameplayhelp": imp = new GuideActImpSpShowGameplayHelp(); break;
                case "merge_entry_hide": imp = new GuideActImpSpMergeEntryHide(); break;
                case "merge_entry_hijack": imp = new GuideActImpSpMergeEntryHijack(); break;
                case "merge_entry_show": imp = new GuideActImpSpMergeEntryShow(); break;
                #endregion
            }
            return imp;
        }

    }
}
