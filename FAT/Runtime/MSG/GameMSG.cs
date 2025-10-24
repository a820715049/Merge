/*
 * @Author: qun.chao
 * @Date: 2023-10-12 11:06:40
 */
using UnityEngine;
using System;
using System.Collections.Generic;
using EL;
using FAT.Merge;
using fat.rawdata;
using static EL.PoolMapping;

namespace FAT
{
    namespace MSG
    {
        #region  FAT
        public class GAME_ORDER_CHANGE : MessageBase<List<IOrderData>, List<IOrderData>> { }  // channged， newlyAdded
        public class GAME_ORDER_COMPLETED : MessageBase<IOrderData> { }
        public class GAME_ORDER_REFRESH : MessageBase<IOrderData> { }
        public class GAME_ORDER_TRY_FINISH_FROM_UI : MessageBase<IOrderData, bool> { }  // param1: order / param2: 是否需要确认消耗
        public class GAME_ORDER_ORDERBOX_BEGIN : MessageBase<Merge.Item> { }
        public class GAME_ORDER_ORDERBOX_END : MessageBase { }
        public class GAME_ORDER_MAGICHOUR_REWARD_BEGIN : MessageBase<(IOrderData, IOrderData, Item)> { }
        public class GAME_ORDER_MAGICHOUR_REWARD_END : MessageBase<IOrderData, Item> { }
        public class GAME_ORDER_DISPLAY_CHANGE : MessageBase { }
        public class UI_BOARD_ORDER_TRY_RELEASE : MessageBase<MBBoardOrder> { }
        public class UI_BOARD_ORDER_ANIMATING : MessageBase { }
        public class UI_BOARD_ORDER_RELOAD : MessageBase<IOrderData, string> { }    // 订单数据 订单theme
        public class UI_NEWLY_FINISHED_ORDER_SHOW : MessageBase<Transform> { } // 订单transform
        public class UI_ORDER_REQUEST_SCROLL : MessageBase<Transform> { } // 订单transform
        public class BOARD_AREA_ADAPTER_COMPLETE : MessageBase<float> { }
        public class GAME_ORDER_TOKEN_MULTI_BEGIN : MessageBase<Merge.Item> { }
        public class GAME_ORDER_TOKEN_MULTI_END : MessageBase { }

        #endregion

        public class GAME_ONE_SECOND_DRIVER : MessageBase { }       // 每秒通知事件
        public class TIME_BIAS : MessageBase<long> { }
        public class GAME_DAY_CHANGE : MessageBase { }              // 日期变更消息
        public class GAME_DAY_CHANGE_TEN : MessageBase { }              // 日期变更消息 10点
        public class GAME_LOCAL_DAY_CHANGE : MessageBase { }              // 日期变更消息
        public class GAME_LOGIN_FIRST_SYNC : MessageBase { }
        public class GAME_SWITCH_REFRESH : MessageBase { }
        public class GAME_COMMON_CLOSE_CUR_TIPS : MessageBase { } //关闭当前显示的通用消息提示框
        public class GAME_ACTIVITY_REWARD_CLOSE : MessageBase { } //关闭活动奖励界面
        public class GAME_ACTIVITY_REWARD_CLICK_CLAIM : MessageBase { } //点击活动奖励界面领取按钮
        // add coin
        public class GAME_COIN_CHANGE : MessageBase<fat.rawdata.CoinType> { }            //param: coinType
        public class GAME_COIN_USE : MessageBase<CoinChange> { }
        public class GAME_COIN_ADD : MessageBase<CoinChange> { }
        public class GAME_RES_UPDATE_FINISH : MessageBase { }                         //param:hot update finish
        public class GAME_MAIL_LIST_CHANGE : MessageBase { }
        public class GAME_MAIL_STATE_CHANGE : MessageBase { }
        public class GAME_MERGE_MAIN_BOARD_CLEAR : MessageBase { }
        public class GAME_MERGE_ENERGY_FREE_SLOT_UPDATE : MessageBase { }
        public class GAME_MERGE_ENERGY_CHANGE : MessageBase<int> { }                 //param: delta, reason
        public class GAME_MERGE_ENERGY_INFINATE_REFRESH : MessageBase<int> { }                 //param: delta seconds (0 means not add)
        public class GAME_MERGE_PVP_ENERGY_CHANGE : MessageBase<int> { } //param: delta, reason
        public class GAME_MERGE_EXP_CHANGE : MessageBase<int> { }                    //param: delta
        public class GAME_MERGE_LEVEL_CHANGE : MessageBase<int> { }                  //param: oldLevel

        public class GAME_MERGE_PRE_BEGIN_REWARD : MessageBase<RewardCommitData> { }
        public class GAME_MERGE_POST_COMMIT_REWARD : MessageBase<RewardCommitData> { }

        public class GAME_FEATURE_STATUS_CHANGE : MessageBase { }                  //param: feature 入口显示or解锁
        public class GAME_MERGE_WORLD_CLEAR : MessageBase<Merge.MergeWorld> { }                  //when board is cleared, mergeworld is the world that cleared
        public class GAME_HANDBOOK_UNLOCK_ITEM : MessageBase { }      //param: items that is unlocked
        public class GAME_HANDBOOK_DISPLAY_ITEM_UPDATE : MessageBase { }
        public class GAME_HANDBOOK_REWARD : MessageBase<int> { }      //param: items that is rewarded
        public class GAME_MERGE_ITEM_EVENT : MessageBase<Merge.Item, Merge.ItemEventType> { }
        public class GAME_BOARD_ITEM_MERGE : MessageBase<Merge.Item> { }
        public class GAME_BOARD_ITEM_SKILL : MessageBase<Merge.Item, SkillType> { }    //使用技能棋子成功时发事件 Item为技能棋子本身 SkillType为技能棋子类型
        public class GAME_BAG_ITEM_INFO_CHANGE : MessageBase { }    //背包
        public class GAME_SHOP_ENTRY_STATE_CHANGE : MessageBase<bool> { }  //控制顶部商城入口按钮显隐
        public class GAME_LEVEL_GO_STATE_CHANGE : MessageBase<bool> { }  //控制顶部等级Icon显隐
        public class UI_STATUS_ADD_BTN_CHANGE : MessageBase<bool> { }  //控制顶部加号按钮显隐
        public class GAME_SHOP_ITEM_INFO_CHANGE : MessageBase { }    //商城
        public class GAME_BACKGROUND_BACK : MessageBase<float> { }                //param: seconds passed during background
        public class GAME_ACCOUNT_CHANGED : MessageBase { }
        public class GAME_WALLPAPER_UNLOCK : MessageBase<int> { }        //param: wallpaperid
        public class GAME_WALLPAPER_UNREAD_CHANGE : MessageBase { }
        public class GAME_WALLPAPER_SWITCH : MessageBase<int> { }        //param: target wallpaperid
        public class GAME_BOARD_TOUCH : MessageBase { }
        public class MERGE_TO_SCENE : MessageBase { }
        public class SCENE_TO_MERGE : MessageBase { }
        public class GAME_NETWORK_WEAK : MessageBase<bool> { }

        public class FLY_ICON_START : MessageBase<FlyableItemSlice> { }
        public class FLY_ICON_FEED_BACK : MessageBase<FlyableItemSlice> { }
        #region setting
        public class NOTIFICATION_STATE : MessageBase<bool> { }
        public class GAME_SETTING_APPLY : MessageBase { }
        #endregion
        #region map
        public class MapState : MessageBase<bool> { }
        public class MAP_SETUP_FINISHED : MessageBase { }
        public class MAP_BUILDING_UPDATE : MessageBase<MapBuilding> { }
        public class MAP_BUILDING_UPDATE_ANY : MessageBase { }
        public class MAP_BUILDING_BUILT : MessageBase<MapBuilding> { }
        public class MAP_FOCUS_CHANGE : MessageBase<IMapBuilding> { }
        public class MAP_FOCUS_POPUP : MessageBase { }
        public class MAP_EFFECT_RT_READ_BACK : MessageBase<Unity.Collections.NativeArray<Color32>, int, int> { }
        #endregion
        #region daily event
        public class DAILY_EVENT_TASK_UPDATE : MessageBase<DailyEvent.Task> { }
        public class DAILY_EVENT_TASK_UPDATE_ANY : MessageBase { }
        public class DAILY_EVENT_TASK_COMPLETE : MessageBase<(DailyEvent.Task, Ref<List<RewardCommitData>>, Ref<List<RewardCommitData>>)> { }
        public class DAILY_EVENT_MILESTONE_UPDATE : MessageBase { }
        public class DAILY_EVENT_MILESTONE_PROGRESS : MessageBase<int, int> { }
        public class DAILY_EVENT_MILESTONE_REWARD : MessageBase<RewardCommitData> { }
        #endregion
        #region activity
        public class ACTIVITY_ACTIVE : MessageBase<ActivityLike, bool> { }
        public class ACTIVITY_END : MessageBase<ActivityLike, bool> { }
        public class ACTIVITY_STATE : MessageBase<ActivityLike> { }
        public class ACTIVITY_REFRESH : MessageBase<ActivityLike> { }
        public class ACTIVITY_UPDATE : MessageBase { }
        public class ACTIVITY_SUCCESS : MessageBase<ActivityLike> { }
        public class ACTIVITY_TS_SYNC : MessageBase<ActivityLike> { }
        public class ACTIVITY_RANKING_DATA : MessageBase<ActivityRanking, RankingType> { }
        public class ACTIVITY_QUERY_ENTRY : MessageBase<ActivityLike, Action<Transform>> { }

        #endregion activity
        #region IAP
        public class IAP_INIT : MessageBase { }
        public class IAP_LATE_DELIVERY : MessageBase<IAPLateDelivery> { }
        public class IAP_REWARD_CHECK : MessageBase { }
        public class IAP_DATA_READY : MessageBase { }  //累充金额数据更新
        #endregion IAP
        #region AD
        public class AD_READY_ANY : MessageBase { }
        #endregion AD
        #region ui

        public class UI_SPLASH_SCREEN_STATE : MessageBase<bool> { }
        public class SCREEN_POPUP_QUERY : MessageBase<ScreenPopup, PopupType> { }

        public class UI_CLOSE_LAYER : MessageBase<UILayer> { }

        #region guide
        public class GUIDE_CHECK : MessageBase { }
        public class GUIDE_FINISH : MessageBase<int> { }     //param: guide id
        public class GUIDE_QUIT : MessageBase { }
        public class GUIDE_OPEN : MessageBase { }
        public class GUIDE_CLOSE : MessageBase { }
        public class UI_GUIDE_BOARD_SHOW_DIALOG : MessageBase<string, string> { }
        public class UI_GUIDE_BOARD_HIDE_DIALOG : MessageBase { }
        public class UI_GUIDE_TOP_SHOW_DIALOG : MessageBase<string, string> { }
        public class UI_GUIDE_TOP_HIDE_DIALOG : MessageBase { }
        public class UI_GUIDE_FINGER_TAP : MessageBase<Merge.Item> { }
        public class UI_GUIDE_FINGER_DRAG : MessageBase<Merge.Item, Merge.Item> { }
        public class UI_GUIDE_FINGER_DRAG_POS : MessageBase<Merge.Item, Vector2> { }
        public class UI_GUIDE_FINGER_MATCH : MessageBase { }
        public class UI_GUIDE_FINGER_Hide : MessageBase { } // 手指引导隐藏
        #endregion

        // 主界面UI显隐状态改变 参数true为通知主界面显示 false为通知主界面隐藏
        public class GAME_MAIN_UI_STATE_CHANGE : MessageBase<bool> { };
        // 资源栏UI显隐状态改变 参数true为通知资源栏显示 false为通知资源栏隐藏
        public class GAME_STATUS_UI_STATE_CHANGE : MessageBase<bool> { }
        //主棋盘显隐状态改变
        public class GAME_MAIN_BOARD_STATE_CHANGE : MessageBase<bool> { }

        //当AboveStatus层级第一次有界面打开时
        public class UI_ABOVE_STATUE_HAS_CHILD : MessageBase { }
        //当AboveStatus层级第一次没有界面时
        public class UI_ABOVE_STATUE_NO_CHILD : MessageBase { }

        // UI界面开启动画播完事件
        public class UI_OPEN_ANIM_FINISH : MessageBase<UIResource> { }
        // UI界面关闭动画播完事件
        public class UI_CLOSE_ANIM_FINISH : MessageBase<UIResource> { }
        // UI界面Pause动画播完事件
        public class UI_PAUSE_ANIM_FINISH : MessageBase<UIResource> { }
        // UI界面Resume动画播完事件
        public class UI_RESUME_ANIM_FINISH : MessageBase<UIResource> { }
        // UI界面中监听动画播放完成
        public class UI_SIMPLE_ANIM_FINISH : MessageBase<AnimatorStateInfo> { }

        public class UI_TOP_BAR_PUSH_STATE : MessageBase<UIStatus.LayerState> { }
        public class UI_TOP_BAR_POP_STATE : MessageBase { }

        //主棋盘界面打开PreOpen
        public class UI_MERGE_BOARD_MAIN_OPEN : MessageBase { }
        //主棋盘界面关闭PreClose
        public class UI_MERGE_BOARD_MAIN_CLOSE : MessageBase { }

        // 棋盘选择item / null表示取消选择
        public class UI_BOARD_SELECT_ITEM : MessageBase<Merge.Item> { }
        public class UI_BOARD_SELECT_CELL : MessageBase<Vector2Int> { }
        public class UI_BOARD_USE_ITEM : MessageBase<Merge.Item> { }
        public class UI_BOARD_DRAG_ITEM_END : MessageBase<Vector2, Merge.Item> { } // ScreenPos, item
        public class UI_BOARD_DRAG_ITEM_CUSTOM : MessageBase<Vector2, Merge.Item> { } // 拖拽中 判断为业务自己的逻辑 / 屏幕坐标 拖拽的item
        public class UI_BOARD_DRAG_ITEM_END_CUSTOM : MessageBase<Vector2, Merge.Item> { } // 拖拽结束 判断为业务自己的逻辑 / 屏幕坐标 拖拽的item
        public class UI_BOARD_ITEM_SPEEDUP_TIP : MessageBase<Merge.Item> { }
        public class UI_BOARD_DIRTY : MessageBase { }
        public class UI_ON_ORDER_ITEM_CONSUMED : MessageBase<int, Vector3> { }
        public class UI_REWARD_FEEDBACK : MessageBase<FlyType> { }
        public class UI_COST_FEEDBACK : MessageBase<FlyType> { }
        public class UI_SINGLE_REWARD_CLOSE_FEEDBACK : MessageBase<int> { }
        public class UI_SHOW_LEVEL_UP_TIP : MessageBase { }
        public class UI_ZODIAC_LEVEL_LOCK_TIP : MessageBase<Transform> { }
        public class UI_ZODIAC_MERGE_ENTRY_GUIDE : MessageBase<bool> { }
        public class UI_SPECIAL_REWARD_FINISH : MessageBase { } //当前所有特殊奖励已领取完成(表现上也完成 如随机宝箱)
        public class GAME_ENDLESS_PGK_REC_SUCC : MessageBase<int, IList<RewardCommitData>, RewardCommitData> { }
        public class GAME_ENDLESS_PGK_REC_FAIL : MessageBase { }
        //3格无限进度条表现事件 param1:最终进度值 param2:进度条完成时的奖励 param3:进度条完成时对应的进度最大值
        public class GAME_ENDLESS_PROG_CHANGE : MessageBase<int, RewardCommitData, int> { }
        public class GAME_ENDLESS_THREE_PGK_REC_SUCC : MessageBase<int, IList<RewardCommitData>, RewardCommitData> { }
        public class GAME_ENDLESS_THREE_PGK_REC_FAIL : MessageBase { }
        //3格无限进度条表现事件 param1:最终进度值 param2:进度条完成时的奖励 param3:进度条完成时对应的进度最大值
        public class GAME_ENDLESS_THREE_PROG_CHANGE : MessageBase<int, RewardCommitData, int> { }
        public class GAME_GEM_ENDLESS_THREE_PGK_REC_SUCC : MessageBase<int, IList<RewardCommitData>> { }
        public class GAME_GEM_ENDLESS_THREE_PGK_REC_FAIL : MessageBase { }
        public class GAME_MARKET_SLIDE_TRY_BUY : MessageBase { }
        public class GAME_MARKET_SLIDE_PGK_REC_SUCC : MessageBase<int, IList<RewardCommitData>> { }
        public class GAME_ERG_LIST_PACK_BUY_SUCC : MessageBase { }  //体力列表礼包购买成功
        public class GAME_ERG_LIST_PACK_CLAIM_SUCC : MessageBase<PackErgList.ErgTaskData> { }  //体力列表礼包领取成功

        public class UI_ORDER_ADJUST_SCROLL : MessageBase<float, float> { }  // param1: posX, param2: duration
        public class UI_ORDER_QUERY_RANDOMER_TRANSFORM : MessageBase<int, Action<Transform>> { }  // orderId, callback
        public class UI_ORDER_QUERY_COMMON_FINISHED_TRANSFORM : MessageBase<int, Action<Transform>> { }  // orderId, callback
        public class UI_ORDER_QUERY_TRANSFORM_BY_ORDER : MessageBase<IOrderData, Action<Transform>> { }  // order, callback
        public class UI_ORDER_BOX_TIPS_POSITION_REFRESH : MessageBase<Vector3, Vector3> { }

        public class UI_TREASURE_FLY_FEEDBACK : MessageBase { }
        public class UI_TREASURE_REWARD_FLY_FEEDBACK : MessageBase<FlyType> { }
        public class UI_TREASURE_LEVEL_CLEAR : MessageBase { }
        public class UI_TREASURE_LEVEL_GROUP_CLEAR : MessageBase { }

        public class UI_INVENTORY_ENTRY_FEEDBACK : MessageBase { }
        public class UI_INVENTORY_REFRESH_RED_POINT : MessageBase { }
        #endregion
        #region card

        public class GAME_CARD_DRAW_FINISH : MessageBase { }    //抽卡逻辑执行完毕
        public class GAME_CARD_REDPOINT_UPDATE : MessageBase<int> { } //卡册红点状态刷新 参数为发生状态变化的卡片id 默认0代表刷新全部卡片
        public class GAME_CARD_JOKER_GET : MessageBase { }    //当获取到万能卡时
        public class GAME_CARD_JOKER_USE : MessageBase { }    //当使用完万能卡时
        public class GAME_CARD_JOKER_SELECT : MessageBase { }    //当界面中选中万能卡时
        public class UI_JUMP_TO_SELECT_CARD : MessageBase<int, System.Action> { }    //特殊方式获得卡片成功时通知卡册界面跳转到对应卡组界面
        public class UI_CARD_GIFTING_SELECT : MessageBase { }    //好友赠卡界面选中某个好友时
        public class UI_JUMP_TO_ALBUM_MAIN_VIEW : MessageBase { }    //通知卡册界面返回主视图
        public class UI_GIVE_CARD_SUCCESS : MessageBase { }     //赠送卡片成功时
        public class UI_PULL_PENDING_CARD_INFO_SUCCESS : MessageBase { }   //拉取服务器发来的待收取卡片信息成功
        public class GAME_CARD_ADD : MessageBase<CardData> { }

        #endregion
        public class GAME_HELP_GUIDE_DRAG_END : MessageBase<int> { }    //帮助引导界面拖拽结束

        #region wish upon
        public class WISH_UPON_ENERGY_UPDATE : MessageBase<int, int> { }
        #endregion

        #region score
        public class GAME_SCORE_GET_PROGRESS_BOARD : MessageBase<int, int> { }    //积分活动- 获得积分后 通知棋盘积分详情组件划入
        public class GAME_SCORE_GET_PROGRESS_SHOP : MessageBase<int, int> { }    //积分活动- 获得积分后 通知商店积分详情组件划入
        public class SCORE_ENTITY_ADD_COMPLETE : MessageBase<(int, int, int)> { }    //活动积分加分结束
        public class SCORE_DATA_UPDATE : MessageBase<int, int> { }    //积分活动数据更新
        public class BOARD_FLY_SCORE : MessageBase<(Item, ScoreEntity.ScoreFlyRewardData, string)> { }    //棋牌内飞积分事件
        public class ON_USE_JOKER_ITEM_UPGRADE : MessageBase<Item, int> { }    //使用万能卡升级
        public class ON_USE_SPEED_UP_ITEM : MessageBase<Item, int> { }    //使用钻石解锁bubble且此bubble有积分
        public class ON_USE_SPEED_UP_ITEM_SUCCESS : MessageBase { }    //解锁bubble成功(广告/钻石)
        public class ON_BUY_SHOP_ITEM : MessageBase<int, int> { }    //购买商店商品
        public class ON_MERGE_HAS_SCORE_ITEM : MessageBase<Item, int> { }    //合并成功有积分的棋子
        public class ON_COMMIT_ORDER : MessageBase<int> { }    //提交订单尝试添加积分
        public class ON_COMMIT_ORDER_BR : MessageBase<int> { }    //提交订单尝试添加右下角积分
        public class SCORE_FLY_REWARD_CENTER : MessageBase<(Vector3 from, RewardCommitData re, ActivityLike act)> { }    //展示奖励或积分飞屏幕中间
        public class ORDER_SCORE_ANIM_COMPLETE : MessageBase<Vector3, bool> { }    //订单积分动画结束  bool 是否是左下角积分
        public class SCORE_ENTRY_POSITION_REFRESH : MessageBase<Vector3> { }    //积分活动入口位置刷新
        public class SCORE_PROGRESS_ANIMATE : MessageBase { }    //积分活动进度条开始表演
        public class SCORE_ADD_DEBUG : MessageBase { }    //积分活动debug gm add score
        public class ACTIVITY_ENTRY_LAYOUT_REFRESH : MessageBase { }    //棋盘活动入口布局刷新
        public class BOARD_FLY_TEXT : MessageBase<Item, int, string> { }    //灯泡/特殊道具 飘字
        public class ACTIVITY_SCORE_ROUND_CHANGE : MessageBase { }
        #endregion

        #region 积分活动变种(麦克风版)

        public class SCORE_MIC_NUM_ADD : MessageBase<int, int> { }    // 积分数量增加

        #endregion

        #region 寻宝

        public class TREASURE_SCORE_UPDATE : MessageBase<int, int> { }    //寻宝积分更新
        public class TREASURE_KEY_UPDATE : MessageBase<int> { }    //寻宝钥匙更新
        public class TREASURE_OPENBOX_UPDATE : MessageBase { }    //寻宝开宝箱更新
        public class TREASURE_LEVEL_UPDATE : MessageBase { }    //寻宝关卡更新
        public class TREASURE_HELP_DRAG_END : MessageBase<int> { }    //寻宝教学拖拽结束
        public class TREASURE_HELP_REFRESH_RED_DOT : MessageBase { }    //寻宝积分奖励飞完奖励刷新红点
        public class TREASURE_OPENBOX : MessageBase<Vector3, int> { }    //寻宝开宝箱


        #endregion

        #region 沙堡里程碑
        public class CASTLE_MILESTONE_CHANGE : MessageBase<int, List<RewardCommitData>, CastleMilestoneGroup> { }
        #endregion

        #region decorate
        public class DECORATE_SCORE_UPDATE : MessageBase<DecorateActivity, int> { }
        public class DECORATE_REFRESH : MessageBase { }
        public class DECORATE_AREA_REFRESH : MessageBase { }
        public class DECORATE_ANIM_END : MessageBase { }
        public class DECORATE_SCORE_FLY : MessageBase { }
        public class DECORATE_GUIDE_READY : MessageBase { }
        public class DECORATE_RES_UI_STATE_CHANGE : MessageBase<bool> { }

        #endregion decorate

        #region AutoGuide

        public class GAME_REMIND_MERGE_START : MessageBase { }
        public class GAME_REMIND_MERGE_END : MessageBase { }
        public class GAME_CLAIM_REWARD : MessageBase { }

        public class GAME_ITEM_INFO_SHOW : MessageBase { }
        public class GAME_ITEM_INFO_END : MessageBase { }

        public class GAME_CARD_PACK_OPEN : MessageBase { }
        public class GAME_CARD_PACK_OPEN_END : MessageBase { }
        public class SCENE_LOAD_FINISH : MessageBase { }
        public class ORDER_FINISH : MessageBase { }
        public class ORDER_FINISH_DATA : MessageBase<IOrderData> { }

        #endregion

        #region race

        public class RACE_ROUND_START : MessageBase<bool> { } //是否进行中状态切换时发送消息
        public class RACE_REWARD_END : MessageBase { } //是否进行中状态切换时发送消息

        #endregion

        #region 活动类棋盘通用事件

        //棋盘准备进行移动时，通知界面做准备。 移动几行本事件就发几次，每次会对应传递要收集飞到奖励箱的棋子
        public class UI_ACTIVITY_BOARD_MOVE_COLLECT : MessageBase<List<Item>> { }
        //棋盘开始上升 通知界面
        public class UI_ACTIVITY_BOARD_MOVE_START : MessageBase { }
        //棋盘发生卡死情况时，处理过程中需要通知界面打开block
        public class UI_ACTIVITY_BOARD_EXTREME : MessageBase { }

        #endregion

        #region MiniBoard

        public class UI_MINI_BOARD_UNLOCK_ITEM : MessageBase<Merge.Item> { } //迷你棋盘棋子在图鉴中第一次解锁时发消息(用于界面表现)
        #endregion

        #region 多轮迷你棋盘

        public class UI_MINI_BOARD_MULTI_UNLOCK_ITEM : MessageBase<Item>
        {
        } //多轮迷你棋盘棋子在图鉴中第一次解锁时发消息(用于界面表现)

        public class UI_MINI_BOARD_MULTI_INHERIT_ITEM : MessageBase<Dictionary<int, int>>
        {
        } //多轮迷你棋盘通知界面层目前要继承的棋子id

        public class UI_MINI_BOARD_MULTI_FINISH : MessageBase
        {
        }

        public class UI_MINI_BOARD_MULTI_COLLECT : MessageBase
        {
        }

        public class UI_CLICK_LOCK_REWARD : MessageBase { }
        public class UI_CLICK_LOCK_DOOR : MessageBase { }
        public class CHECK_MINI_BOARD_MULTI_ENTRY_RED_POINT : MessageBase<MiniBoardMultiActivity> { }
        public class UI_MINI_BOARD_SHOW_END : MessageBase { }

        #endregion

        #region 挖矿棋盘

        public class UI_MINE_BOARD_UNLOCK_ITEM : MessageBase<Item> { } //挖矿盘棋子在图鉴中第一次解锁时发消息(用于界面表现)
        public class UI_MINE_BOARD_MOVE_UP_READY : MessageBase { }   //挖矿棋盘准备进行上升前 通知界面 做预处理
        public class UI_MINE_BOARD_MOVE_UP_COLLECT : MessageBase<List<Item>, int> { }   //挖矿棋盘开始上升并收集棋子到奖励箱时 通知界面 做棋子飞行表现
        public class UI_MINE_BOARD_MOVE_UP_FINISH : MessageBase<int> { } //挖矿棋盘处理完整个上升流程时 通知界面 做棋盘上升表现
        //挖矿棋盘进度条表现事件 param1:最终进度值 param2:进度条完成时的奖励，用完时记得回收 param3:进度条完成时对应的进度最大值
        public class GAME_MINE_BOARD_PROG_CHANGE : MessageBase<int, Ref<List<RewardCommitData>>, int> { }
        public class GAME_MINE_BOARD_TOKEN_CHANGE : MessageBase<int, int> { }

        #endregion
        #region 矿车棋盘

        public class UI_MINECART_BOARD_UNLOCK_ITEM : MessageBase<Item> { } //矿车盘最高级棋子在图鉴中第一次解锁时发消息(用于界面表现)
        public class UI_MINECART_BOARD_MOVE_UP : MessageBase<List<Item>> { }   //矿车棋盘准备进行上升前 通知界面 按行传递要飞行的棋子
        public class UI_MINECART_BOARD_MOVE_START : MessageBase { }   //矿车棋盘开始上升 通知界面

        //矿车棋盘进度条表现事件
        //param1:最终进度值
        //param2:本次进度条变化达成的阶段id，需要再把此值回传给数据层，默认-1表示没有达成阶段奖励
        //param3:进度条完成时的回合大奖，默认传default表示不是大奖，用完时记得回收
        public class GAME_MINECART_BOARD_PROG_CHANGE : MessageBase<int, int, Ref<List<RewardCommitData>>> { }

        #endregion
        #region 付费留存礼包
        public class PACK_RETENTION_EXPIRE : MessageBase { } //付费玩家活动过期后领取免费没领取的免费奖励
        #endregion

        #region 集卡活动交换

        public class CARD_STAR_EXCHANGE : MessageBase
        {
        }

        #endregion

        #region 挖沙
        public class DIGGING_SCORE_UPDATE : MessageBase<int, int> { }    //挖沙积分更新
        public class DIGGING_KEY_UPDATE : MessageBase<int> { }    //挖沙钥匙更新
        public class DIGGING_ENTRY_REFRESH_RED_DOT : MessageBase { }    //挖沙积分奖励飞完奖励刷新红点
        public class DIGGING_LEVE_CLEAR : MessageBase { }
        public class DIGGING_LEVE_ROUND_CLEAR : MessageBase { }
        public class DIGGING_PROGRESS_REFRESH : MessageBase { }
        public class DIGGING_REWARD_FLY_FEEDBACK : MessageBase<FlyType> { }

        #endregion
        #region 连续限时订单
        public class ORDER_CHALLENGE_BEGIN : MessageBase { }
        public class ORDER_CHALLENGE_EXPIRE : MessageBase { }
        public class ORDER_CHALLENGE_VICTORY : MessageBase { }

        #endregion
        public class BOARD_ORDER_SCROLL_RESET : MessageBase { }
        public class BOARD_ORDER_SCROLL_SETTARGET : MessageBase<Transform> { }
        public class BOARD_FLY_START : MessageBase { }

        public class MINIGAME_UNLOCK_LEVEL : MessageBase
        {
        }

        #region 弹珠游戏相关

        public class GAME_DEBUG_PACHINKO_INFO : MessageBase<int, string> { }
        public class PACHINKO_SCORE_UPDATE : MessageBase<int> { }
        public class UI_PACHINKO_LOADING_END : MessageBase { }  //弹珠活动loading动画结束

        #endregion

        #region 4倍能量解锁
        public class UI_ENERGY_BOOST_UNLOCK_FLY_FEEDBACK : MessageBase { }
        #endregion

        #region bingo
        public class BINGO_PROGRESS_UPDATE : MessageBase { }
        public class BINGO_ITEM_COMPLETE_DIRTY : MessageBase { }
        public class BINGO_ENTER_NEXT_ROUND : MessageBase { }
        public class BINGO_ITEM_MAP_UPDATE : MessageBase { }
        #endregion

        #region bingo task
        public class UI_BINGO_TASK_COMPLETE_ITEM : MessageBase<BingoResult, int> { }
        public class UI_BINGO_CLOSE : MessageBase { }
        public class BINGO_TASK_QUIT_SPECIAL : MessageBase<int> { }
        #endregion

        #region orderlike | 好评订单
        public class ORDERLIKE_TOKEN_CHANGE : MessageBase { }
        public class ORDERLIKE_ROUND_CHANGE : MessageBase { }
        #endregion

        #region claworder | 抓宝大师
        public class CLAWORDER_TOKEN_COMMIT : MessageBase { }
        public class CLAWORDER_CHANGE : MessageBase { }
        #endregion

        #region 周任务
        public class ACTIVITY_WEEKLY_TASK_END : MessageBase { }
        #endregion

        #region 火车任务
        public class UI_TRAIN_MISSION_SCROLLIN : MessageBase<MBTrainMissionTrain.TrainType> { } // 火车驶入
        public class UI_TRAIN_MISSION_SCROLLSTOP : MessageBase<MBTrainMissionTrain.TrainType> { } // 火车停止
        public class UI_TRAIN_MISSION_SCROLLOUT : MessageBase<MBTrainMissionTrain.TrainType> { } // 火车驶出
        public class UI_TRAIN_MISSION_COMPLETE_TRAIN_MISSION : MessageBase<MBTrainMissionTrain.TrainType, int> { } // 完成火车所有任务
        #endregion

        #region 钓鱼棋盘
        public class FISHING_FISH_CAUGHT : MessageBase<ActivityFishing.FishCaughtInfo> { }      // 捕获鱼
        public class FISHING_FISH_CAUGHT_CLOSE : MessageBase<ActivityFishing.FishCaughtInfo> { }      // 捕获鱼界面关闭
        public class FISHING_FISH_COLLECT_CLOSE : MessageBase<ActivityFishing.FishCaughtInfo> { }      // 集齐鱼界面关闭
        public class FISHING_FISH_CAUGHT_REPEAT : MessageBase { }      // 捕获鱼界面转换为体力关闭时
        public class FISHING_FISH_CONVERT : MessageBase { }     // 转换鱼
        public class FISHING_MILESTONE_TOKEN_CHANGE : MessageBase { }     // 里程碑积分更新
        public class FISHING_MILESTONE_REWARD_CHANGE : MessageBase { }    // 里程碑奖励更新
        public class FISHING_MILESTONE_REWARD_CLOSE : MessageBase { }    // 里程碑奖励关闭
        public class FISHING_FISH_BOARD_SPAWN_ITEM : MessageBase { }    // 钓鱼棋盘点击生成器生成每个棋子时
        #endregion
        #region 签到
        public class GAME_SIGN_IN_CLICK : MessageBase { }
        #endregion

        #region 七天任务
        public class SEVEN_DAY_TASK_UPDATE : MessageBase { }
        #endregion

        #region 农场棋盘
        public class FARM_BOARD_TOKEN_CHANGE : MessageBase { }    // 农场棋盘Token数量改变
        public class UI_FARM_BOARD_UNLOCK_ITEM : MessageBase<Item> { } // 农场盘棋子在图鉴中第一次解锁时发消息(用于界面表现)
        public class FARM_BOARD_SEED_CLOSE : MessageBase { } // 农场棋盘种子包界面关闭
        public class UI_FARM_BOARD_MOVE_UP_READY : MessageBase { }   //农场棋盘准备进行上升前 通知界面 做预处理
        public class UI_FARM_BOARD_MOVE_UP_COLLECT : MessageBase<List<Item>, int> { }   //农场棋盘开始上升并收集棋子到奖励箱时 通知界面 做棋子飞行表现
        public class UI_FARM_BOARD_MOVE_UP_FINISH : MessageBase<int> { } //农场棋盘处理完整个上升流程时 通知界面 做棋盘上升表现
        public class UI_FARM_EXTREME_CASE_BLOCK : MessageBase { }    // 农场棋盘发生卡死情况时，处理过程中打开界面block
        #endregion

        #region 签到抽奖
        public class WEEKLYRAFFLE_TOKEN_CHANGE : MessageBase<bool> { } // 签到抽奖 token改变
        public class WEEKLYRAFFLE_REFILL : MessageBase<int, bool> { } // 签到抽奖 补签 int-哪一天 bool-是否全部补签
        public class WEEKLYRAFFLE_RAFFLE_END : MessageBase<Ref<List<RewardCommitData>>, int, int> { } // 签到抽奖 抽奖结束 <rewardData boxID rewardID>
        #endregion

        #region 订单助力
        public class ORDER_BONUS_PHASE_CHANGE : MessageBase { }
        public class ROCKET_ANIM_COMPLETE : MessageBase { }
        public class CLEAR_BONUS : MessageBase<int> { }
        #endregion

        #region BP
        public class UI_BP_TASK_COMPLETE : MessageBase<int> { } // 任务完成 int:任务id
        //BP里程碑进度条表现事件 param1:最终进度值 param2:进度条涨满时对应的进度最大值 -1表示直接使用数据层最新的进度最大值
        public class UI_BP_MILESTONE_CHANGE : MessageBase<int, int> { }
        public class UI_BP_MILESTONECELL_PLAY_UP : MessageBase { } // 里程碑格子播放升级动画
        public class UI_BP_MILESTONECELL_PLAY_PROGRESS : MessageBase<int, float, int, int> { } // 里程碑格子播放进度动画
        public class UI_BP_OPEN_CYCLE_TIP : MessageBase { } // 打开循环奖励提示
        //BP购买成功
        public class GAME_BP_BUY_SUCCESS : MessageBase<BPActivity.BPPurchaseType, Ref<List<RewardCommitData>>, bool> { }
        //BP任务状态发生改变 用于界面监听刷新红点等
        public class GAME_BP_TASK_STATE_CHANGE : MessageBase { }
        //BP任务刷新 用于主棋盘展示任务进度或状态发生变化时的Tips
        public class GAME_BP_TASK_UPDATED : MessageBase<List<BPActivity.BPTaskUpdateInfo>> { }

        #endregion

        #region 兑换商店
        public class REDEEMSHOP_SCORE_UPDATE : MessageBase<int, int> { }
        public class REDEEMSHOP_ENTRY_REFRESH_RED_DOT : MessageBase { }    //兑换商店积分奖励飞完奖励刷新红点
        public class REDEEMSHOP_BUY_REFRESH : MessageBase<int, int, int> { }
        public class REDEEMSHOP_PANEL_REFRESH : MessageBase<bool> { }
        public class REDEEMSHOP_DATA_CHANGE : MessageBase { }
        public class REDEEMSHOP_REDPOINT_REFRESH : MessageBase<bool> { }
        #endregion
        #region 打怪棋盘
        public class FIGHT_RECEIVE_ATTACK : MessageBase<AttackInfo> { }
        public class FIGHT_ATTACK_REWARD : MessageBase<List<RewardCommitData>> { } //获得攻击奖励时的事件
        public class FIGHT_LEVEL_REWARD : MessageBase<List<RewardCommitData>, EventFightLevel> { }   //获得里程碑奖励时的事件
        #endregion

        #region 许愿棋盘
        public class WISH_BOARD_TOKEN_CHANGE : MessageBase { }    // 农场棋盘Token数量改变
        public class UI_WISH_BOARD_UNLOCK_ITEM : MessageBase<Item> { } // 农场盘棋子在图鉴中第一次解锁时发消息(用于界面表现)
        public class WISH_BOARD_SEED_CLOSE : MessageBase { } // 农场棋盘种子包界面关闭
        public class UI_WISH_BOARD_MOVE_UP_READY : MessageBase { }   //农场棋盘准备进行上升前 通知界面 做预处理
        public class UI_WISH_BOARD_MOVE_UP_FINISH : MessageBase<int> { } //农场棋盘处理完整个上升流程时 通知界面 做棋盘上升表现
        public class UI_WISH_EXTREME_CASE_BLOCK : MessageBase { }    // 农场棋盘发生卡死情况时，处理过程中打开界面block
        public class UI_WISH_PROGRESS_CHANGE : MessageBase<List<RewardCommitData>, string, int> { } //进度条更新
        #endregion

        #region 社区引流
        public class APP_ENTER_FOREGROUND_EVENT : MessageBase { }
        public class COMMUNITY_LINK_REFRESH_RED_DOT : MessageBase { }
        public class MAIL_ITEM_REFRESH : MessageBase { }
        #endregion

        #region 每日任务路径主题
        public class LANDMARK_TOKEN_CHANGE : MessageBase { } // LandMark 活动专用：token 数量变更
        #endregion

        #region 倍率排行
        public class MULTIPLIER_RANKING_SLOTS_CHANGE : MessageBase<int> { }    // 倍率排行转盘槽位发生变化
        public class MULTIPLY_RANKING_ENTRY_REFRESH_RED_DOT : MessageBase { }    // 倍率排行入口刷新红点
        public class MULTIPLY_RANKING_BLOCK_ENTRY_UPDATE : MessageBase { }
        public class MULTIPLY_RANKING_RANKING_CHANGE : MessageBase { }
        #endregion

        #region 任务统计
        public class TASK_UPDATE : MessageBase<TaskType, int> { }
        public class TASK_PAY_SUCCESS : MessageBase { }
        public class TASK_BUILD_UPGRADE : MessageBase { }
        public class TASK_COMPLETE_DAILY_TASK : MessageBase { }
        public class TASK_RACE_WIN : MessageBase { }
        public class TASK_SCORE_DUEL_WIN : MessageBase { }
        public class TASK_FARM_TOKEN_COST : MessageBase { }
        public class TASK_ACTIVITY_TOKEN_USE : MessageBase<int, int> { }
        #endregion

        #region 海上竞速
        public class UI_SEA_RACE_SCORE_CHANGE : MessageBase { } // 海上竞速分数变化
        public class UI_SEA_RACE_ENTRY_UPDATE : MessageBase { } // 海上竞速入口更新
        public class SEA_RACE_ROBOT_ADD_ONLINE_SCORE : MessageBase { } // 海上竞速 机器人在线加分
        #endregion
        
        #region 飞跃藤蔓

        public class VINELEAP_STEP_START : MessageBase { }    // 当前Step开始
        public class VINELEAP_STEP_END : MessageBase<bool> { }    // 当前Step结束

        #endregion
    }
}
