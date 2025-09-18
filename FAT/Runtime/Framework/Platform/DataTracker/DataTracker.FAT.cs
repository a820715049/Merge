/*
 * @Author: qun.chao
 * @Date: 2023-12-20 16:46:56
 */

using System;
using System.Collections.Generic;
using FAT;
using fat.rawdata;
using UnityEngine;
using EL;
using EventType = fat.rawdata.EventType;
using FAT.Platform;
using EL.SimpleJSON;
using System.Text;
using static FAT.ActivityRanking;
using fat.conf;

/**
 *  注意，由于object复用，所有Fill方法必须保证赋值了每个字段，否则可能保留上个event的字段
 **/
public partial class DataTracker
{
    public static Dictionary<Type, object> cache;
    public static JSONObject cacheJson;
    public static StringBuilder builder;
    public static bool TrackEnable = true;

    public static void Track(string name_, string data_)
    {
        if (GameSwitchManager.Instance.isDataTracking && TrackEnable)
            PlatformSDK.Instance.TraceEvent(name_, data_);
        else
            DebugEx.FormatTrace("PlatformSDK.TraceEvent ----> {0}:{1}", name_, data_);
    }

    private static void _TrackData(object data_, string name_ = null)
    {
        if (data_ == null) return;
        if (data_ is DataTrackBase common) common.FillCommonData();
        Track(name_ ?? data_.GetType().Name, JsonUtility.ToJson(data_));
    }

    private static void TrackObject(JSONObject data_, string name_, bool common_ = true)
    {
        if (data_ == null) return;
        if (common_) MergeCommonData.Fill(data_);
        builder ??= new StringBuilder();
        builder.Clear();
        data_.WriteToStringBuilder(builder, 0, 0, JSONTextMode.Compact);
        Track(name_, builder.ToString());
    }

    private static T _GetTrackData<T>() where T : new()
    {
        return (T)_GetTrackData(typeof(T));
    }

    private static object _GetTrackData(Type type_)
    {
        cache ??= new Dictionary<Type, object>();
        if (cache.TryGetValue(type_, out var r)) return r;
        r = Activator.CreateInstance(type_);
        cache[type_] = r;
        return r;
    }

    private static JSONObject BorrowTrackObject()
    {
        cacheJson ??= new JSONObject();
        cacheJson.Clear();
        return cacheJson;
    }

    public static void FillActivity(JSONObject data_, ActivityLike acti_)
    {
        (data_["event_id"], data_["event_from"], data_["event_param"]) = acti_.Info3;
    }

    [Serializable]
    internal class DataTrackBase
    {
        public string app_version_detail;
        public string version_code;

        public virtual void FillCommonData()
        {
            if (Game.Instance.appSettings != null)
            {
                app_version_detail = Game.Instance.appSettings.version;
                version_code = Game.Instance.appSettings.versionCode;
            }
        }

        public static void Fill(JSONObject target_)
        {
            if (Game.Instance.appSettings != null)
            {
                target_["app_version_detail"] = Game.Instance.appSettings.version;
                target_["version_code"] = Game.Instance.appSettings.versionCode;
            }
        }
    }

    [Serializable]
    internal class MergeCommonData : DataTrackBase
    {
        // 需要区分棋盘
        public int board_id;
        public int board_version;
        public int task_num;
        public int diamond_num;
        public int energy_num;
        public int coin_num;
        public int wood_num;
        public int stone_num;
        public int ceramic_num;
        public int tile_num;
        public int payer_tag;
        public bool is_paid_user;
        public int current_level;

        public override void FillCommonData()
        {
            base.FillCommonData();
            var coinMan = Game.Manager.coinMan;
            var currentWorld = Game.Manager.mergeBoardMan.activeWorld;
            board_id = currentWorld?.activeBoard?.boardId ?? 0;
            board_version = Game.Manager.mainMergeMan.world.configVersion;
            task_num = Game.Manager.mapSceneMan.CostCount;
            diamond_num = coinMan.GetDisplayCoin(CoinType.Gem);
            energy_num = Game.Manager.mergeEnergyMan.Energy;
            coin_num = coinMan.GetDisplayCoin(CoinType.MergeCoin);
            wood_num = coinMan.GetDisplayCoin(CoinType.ToolWood);
            stone_num = coinMan.GetDisplayCoin(CoinType.ToolStone);
            ceramic_num = coinMan.GetDisplayCoin(CoinType.ToolCeramics);
            tile_num = coinMan.GetDisplayCoin(CoinType.ToolTile);
            is_paid_user = Game.Manager.iap.TotalIAPServer > 0;
            current_level = Game.Manager.mergeLevelMan.level;
        }

        public new static void Fill(JSONObject target_)
        {
            DataTrackBase.Fill(target_);
            var coinMan = Game.Manager.coinMan;
            var currentWorld = Game.Manager.mergeBoardMan.activeWorld;
            target_["board_id"] = currentWorld?.activeBoard?.boardId ?? 0;
            target_["board_version"] = Game.Manager.mainMergeMan?.world?.configVersion;
            target_["task_num"] = Game.Manager.mapSceneMan?.CostCount;
            target_["diamond_num"] = (int)coinMan.GetDisplayCoin(CoinType.Gem);
            target_["energy_num"] = Game.Manager.mergeEnergyMan.Energy;
            target_["coin_num"] = (int)coinMan.GetDisplayCoin(CoinType.MergeCoin);
            target_["wood_num"] = (int)coinMan.GetDisplayCoin(CoinType.ToolWood);
            target_["stone_num"] = (int)coinMan.GetDisplayCoin(CoinType.ToolStone);
            target_["ceramic_num"] = (int)coinMan.GetDisplayCoin(CoinType.ToolCeramics);
            target_["tile_num"] = (int)coinMan.GetDisplayCoin(CoinType.ToolTile);
            target_["is_paid_user"] = Game.Manager.iap.TotalIAPServer > 0;
            target_["current_level"] = Game.Manager.mergeLevelMan.level;
        }
    }

    [Serializable]
    private class loading : DataTrackBase
    {
        public int loading_step;
        public string loading_name;

        public loading Fill(int loadingStep, string loadingName)
        {
            loading_step = loadingStep;
            loading_name = loadingName;
            return this;
        }
    }

    public static void TrackLoading(GameProcedure.LoadingPhase phase)
    {
        _TrackData(_GetTrackData<loading>().Fill((int)phase, phase.ToString()));
    }

    [Serializable]
    internal class game_start : MergeCommonData
    {
        public string user_platform_id;
        public int user_platform_type;

        public static void Track()
        {
            var data = _GetTrackData<game_start>();
            var sdk = PlatformSDK.Instance.Adapter;
            var p = sdk.profile;
            data.user_platform_id = p.id;
            data.user_platform_type = p.type switch
            {
                AccountLoginType.Facebook => 1,
                AccountLoginType.Apple => 2,
                AccountLoginType.Google => 3,
                _ => -(int)p.type
            };
            _TrackData(data);
        }
    }

    [Serializable]
    internal class version_update : DataTrackBase
    {
        public int old_version;
        public int new_version;
        public string old_data;
        public string new_data;

        public static void Track(int oldV, int newV, fat.gamekitdata.ClientData oldData,
            fat.gamekitdata.ClientData newData)
        {
            var data = _GetTrackData<version_update>();
            data.old_version = oldV;
            data.new_version = newV;
            data.old_data = oldData.ToString();
            data.new_data = newData.ToString();
            _TrackData(data);
        }
    }

    //玩家触发ab分组逻辑时打点
    [Serializable]
    private class ab_change : DataTrackBase
    {
        public string ab_group_info;

        public ab_change Fill(string dictJson)
        {
            ab_group_info = dictJson;
            return this;
        }
    }

    public static void TrackABChange(string dictJson)
    {
        _TrackData(_GetTrackData<ab_change>().Fill(dictJson));
    }

    [Serializable]
    private class merge_level_up : MergeCommonData
    {
        public int level;
        public int exp_left;

        public merge_level_up Fill()
        {
            level = Game.Manager.mergeLevelMan.level;
            exp_left = Game.Manager.mergeLevelMan.exp;
            return this;
        }
    }

    public static void TrackMergeLevelUp()
    {
        _TrackData(_GetTrackData<merge_level_up>().Fill());
        TraceUser().CurrentLevel().Apply();
    }

    [Serializable]
    internal class get_item : MergeCommonData
    {
        public int item_id;
        public int num;
        public string from;

        public static void Track(int _id, int _num, ReasonString reason)
        {
            var data = _GetTrackData<get_item>();
            data.item_id = _id;
            data.num = _num;
            data.from = reason;
            _TrackData(data);
        }
    }

    // 生成器丢失
    [Serializable]
    internal class missing_item : MergeCommonData
    {
        public List<int> category_list;
        public string from;

        public static void Track(List<int> _missList, ReasonString reason)
        {
            var data = _GetTrackData<missing_item>();
            data.category_list = _missList;
            data.from = reason;
            _TrackData(data);
        }
    }

    //物品过期转化
    [Serializable]
    internal class expire : MergeCommonData
    {
        public int item_id;
        public int num;

        public static void Track(int id, int num)
        {
            var data = _GetTrackData<expire>();
            data.item_id = id;
            data.num = num;
            _TrackData(data);
        }
    }

    #region 冰冻棋子

    //通过合成行为，生成冰冻棋子时
    [Serializable]
    internal class event_frozen_item_get : MergeCommonData
    {
        public int event_id;   //活动ID（EventTime.id）
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param; //活动参数（EventTime.eventParam）
        public int milestone_difficulty;   //本轮活动难度
        public int pay_difficulty;   //棋子付出难度
        public int item_unique_id;   //冰冻棋子的唯一id
        public int item_id;    //获得的冰冻棋子id
        public int item_level;   //获得的冰冻棋子等级

        public static void Track(ActivityLike acti_, int diff, int payDiff, int itemUniqueId, int itemId, int itemLevel)
        {
            var data = _GetTrackData<event_frozen_item_get>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_difficulty = diff;
            data.pay_difficulty = payDiff;
            data.item_unique_id = itemUniqueId;
            data.item_id = itemId;
            data.item_level = itemLevel;
            _TrackData(data);
        }
    }

    //把冰冻棋子和其他棋子合成时
    [Serializable]
    internal class event_frozen_item_merge : MergeCommonData
    {
        public int item_unique_id;   //冰冻棋子的唯一id
        public int item_id;    //被合成的冰冻棋子id
        public int item_level;   //被合成的冰冻棋子等级

        public static void Track(int itemUniqueId, int itemId, int itemLevel)
        {
            var data = _GetTrackData<event_frozen_item_merge>();
            data.item_unique_id = itemUniqueId;
            data.item_id = itemId;
            data.item_level = itemLevel;
            _TrackData(data);
        }
    }

    //冰冻棋子过期时
    [Serializable]
    internal class event_frozen_item_expire : MergeCommonData
    {
        public int item_unique_id;   //冰冻棋子的唯一id
        public int item_id;    //过期的冰冻棋子id
        public int item_level;   //过期的冰冻棋子等级
        public string reward_map;   //过期物品转化（奖励ID:数量）

        public static void Track(int itemUniqueId, int itemId, int itemLevel, string rewardInfo)
        {
            var data = _GetTrackData<event_frozen_item_expire>();
            data.item_unique_id = itemUniqueId;
            data.item_id = itemId;
            data.item_level = itemLevel;
            data.reward_map = rewardInfo;
            _TrackData(data);
        }
    }

    #endregion

    #region guide

    [Serializable]
    private class tutorial : DataTrackBase
    {
        public int tutorial_step;

        public tutorial Fill(int guide_full_step, string name)
        {
            tutorial_step = guide_full_step;
            return this;
        }
    }

    public static void TrackTutorial(int guide_full_step, string name)
    {
        _TrackData(_GetTrackData<tutorial>().Fill(guide_full_step, name));
    }

    [Serializable]
    private class tutorial_debug : DataTrackBase
    {
        public string info;

        public tutorial_debug Fill(string s)
        {
            info = s;
            return this;
        }
    }

    public static void TrackTutorialForDebug(string info)
    {
        _TrackData(_GetTrackData<tutorial_debug>().Fill(info));
    }

    #endregion guide

    #region 游戏货币

    [Serializable]
    internal class IntCoinChangeBase : MergeCommonData
    {
        public string from;
        public bool is_add;
        public long amount_change;
        public long amount_after;

        public IntCoinChangeBase Fill(CoinType type, ReasonString reason, bool isAdd, long amountChange)
        {
            from = reason;
            is_add = isAdd;
            amount_change = amountChange;
            amount_after = Game.Manager.coinMan.GetCoin(type);
            return this;
        }
    }

    [Serializable]
    private class diamond_change : IntCoinChangeBase
    {
    }

    [Serializable]
    private class coin_change : IntCoinChangeBase
    {
    }

    public static void TrackIntCoinChange(CoinType type, ReasonString reason, bool isAdd, long amountChange)
    {
        switch (type)
        {
            case CoinType.MergeCoin:
                _TrackData(_GetTrackData<coin_change>().Fill(type, reason, isAdd, amountChange));
                break;
            case CoinType.Gem:
                _TrackData(_GetTrackData<diamond_change>().Fill(type, reason, isAdd, amountChange));
                break;
            default:
                {
                    if (type >= CoinType.ToolTile && type <= CoinType.ToolWrench)
                        tool_change.Track(type, reason, isAdd, amountChange);
                }
                break;
        }

        TraceUser().Coin(type)?.Apply();
    }

    public static class energy_change_total
    {
        public static long totalAdd;
        public static long totalConsume;

        public static void Track(bool isAdd, long change)
        {
            if (isAdd)
                totalAdd += change;
            else
                totalConsume += change;
        }

        public static int CommitTotalConsume()
        {
            var total = totalConsume;
            totalConsume = 0;
            if (total > int.MaxValue)
                return int.MaxValue;
            return (int)total;
        }
    }

    [Serializable]
    internal class energy_change : IntCoinChangeBase
    {
        public static void Track(ReasonString reason, bool isAdd, long amountChange)
        {
            // 累计
            energy_change_total.Track(isAdd, amountChange);

            var data = _GetTrackData<energy_change>();
            data.from = reason;
            data.is_add = isAdd;
            data.amount_change = amountChange;
            data.amount_after = Game.Manager.mergeEnergyMan.EnergyAfterFly;
            _TrackData(data);
            TraceUser().Energy().Apply();
        }
    }

    [Serializable]
    internal class energy_error : MergeCommonData
    {
        public static void Track()
        {
            _TrackData(_GetTrackData<energy_error>());
        }
    }

    [Serializable]
    internal class exp_change : IntCoinChangeBase
    {
        public static void Track(ReasonString reason, long amountChange)
        {
            var data = _GetTrackData<exp_change>();
            data.from = reason;
            data.is_add = amountChange > 0;
            data.amount_change = amountChange;
            data.amount_after = Game.Manager.mergeLevelMan.realExp;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class tool_change : IntCoinChangeBase
    {
        public int id;

        public static void Track(CoinType type, ReasonString reason, bool isAdd, long amountChange)
        {
            var data = _GetTrackData<tool_change>();
            var coinMan = Game.Manager.coinMan;
            data.id = coinMan.GetIdByCoinType(type);
            data.Fill(type, reason, isAdd, amountChange);
            _TrackData(data);
        }
    }

    [Serializable]
    internal class token_change : IntCoinChangeBase
    {
        public int id;

        public static void Track(int id_, int change_, int after_, ReasonString reason)
        {
            var data = _GetTrackData<token_change>();
            data.id = id_;
            data.from = reason;
            data.is_add = change_ > 0;
            data.amount_change = change_;
            data.amount_after = after_;
            _TrackData(data);
        }
    }

    #endregion 游戏货币

    #region metagame

    [Serializable]
    internal class meta_cost : MergeCommonData
    {
        public int scene_id;
        public int base_id;
        public int level_id;
        public int level_queue;
        public int cost_id;
        public int cost_queue;

        public static void Track(MapBuilding b_)
        {
            var data = _GetTrackData<meta_cost>();
            data.scene_id = Game.Manager.mapSceneMan.scene.Id;
            data.base_id = b_.confBuilding.Id;
            data.level_id = b_.confLevel.Id;
            data.level_queue = b_.Level;
            data.cost_id = b_.confCost.Id;
            data.cost_queue = b_.PhaseVisual;
            _TrackData(data);
        }
    }

    #endregion metagame

    #region 商城

    [Serializable]
    internal class market_buy : MergeCommonData
    {
        public int market_type; //商店类型（1:IAP/2:increase/3:weight/4:difficulty）
        public int board_id;
        public int slot_id;
        public int commodity_id;
        public int item_id;

        public static void Track(int type_, int board_, int slot_, int commodity_, int item_)
        {
            var data = _GetTrackData<market_buy>();
            data.market_type = type_;
            data.board_id = board_;
            data.slot_id = slot_;
            data.commodity_id = commodity_;
            data.item_id = item_;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class market_refresh : MergeCommonData
    {
        public int board_id;
        public string item_list;    //刷新结果 itemId 用逗号分隔

        public static void Track(int board_, string itemList)
        {
            var data = _GetTrackData<market_refresh>();
            data.board_id = board_;
            data.item_list = itemList;
            _TrackData(data);
        }
    }

    #endregion 商城

    #region 每日任务

    [Serializable]
    internal class daily_task : MergeCommonData
    {
        public int task_id;
        public int task_group_id;
        public int task_list_id;
        public int task_queue;
        public int task_difficulty;
        public bool is_final_normal;
        public bool is_final_golden;
        public int num_normal;
        public int num_golden;
        public int event_id;
        public int event_from;
        public int event_param;

        public static void Track(ActivityDE acti_, DailyEvent.Task t_)
        {
            var data = _GetTrackData<daily_task>();
            var de = Game.Manager.dailyEvent;
            var lastTask = t_ == de.list[^1];
            data.task_id = t_.Id;
            data.task_group_id = de.group.Id;
            data.task_list_id = de.active.Id;
            data.task_queue = de.list.IndexOf(t_) + 1;
            data.task_difficulty = t_.conf.Diff;
            data.is_final_normal = lastTask && de.groupIndex == 0;
            data.is_final_golden = lastTask && de.groupIndex > 0 && de.group.Id == de.groupRef.IncludeGroupId[^1];
            data.num_normal = de.taskCountN;
            data.num_golden = de.taskCountG;
            (data.event_id, data.event_from, data.event_param) = acti_?.Info3 ?? default;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class daily_task_group : MergeCommonData
    {
        public int task_group_id;
        public int task_list_id;

        public static void Track()
        {
            var data = _GetTrackData<daily_task_group>();
            var de = Game.Manager.dailyEvent;
            data.task_group_id = de.group.Id;
            data.task_list_id = de.active.Id;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class daily_task_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_id;
        public int milestone_queue;
        public int milestone_difficulty;
        public int milestone_num;
        public int round_num;
        public bool is_final;

        public static void Track(ActivityDEM acti_, int index_, int count_, int diff_)
        {
            var data = _GetTrackData<daily_task_milestone>();
            var de = Game.Manager.dailyEvent;
            (data.event_id, data.event_from, data.event_param) = acti_?.Info3 ?? default;
            data.milestone_id = de.milestone.Id;
            data.milestone_queue = index_ + 1;
            data.milestone_difficulty = diff_;
            data.milestone_num = count_;
            data.round_num = 1;
            data.is_final = index_ == count_ - 1;
            _TrackData(data);
        }
    }

    #endregion 每日任务

    #region 图鉴

    [Serializable]
    internal class handbook : MergeCommonData
    {
        public int item_id;

        public static void Track(int itemId_)
        {
            var data = _GetTrackData<handbook>();
            data.item_id = itemId_;
            _TrackData(data);
        }
    }

    #endregion 图鉴

    #region 背包

    [Serializable]
    internal class bag_item_unlock : MergeCommonData
    {
        public int slot_id;

        public static void Track(int slotId_)
        {
            var data = _GetTrackData<bag_item_unlock>();
            data.slot_id = slotId_;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class bag_change : MergeCommonData
    {
        public int is_in; //1 放入 0取出
        public int bag_id;
        public int item_id;

        public static void Track(int bagId_, int itemId_, bool isIn_)
        {
            var data = _GetTrackData<bag_change>();
            data.is_in = isIn_ ? 1 : 0;
            data.bag_id = bagId_;
            data.item_id = itemId_;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class bag_remind : MergeCommonData
    {
        public int item_id;

        public static void Track(int item)
        {
            var data = _GetTrackData<bag_remind>();
            data.item_id = item;
            _TrackData(data);
        }
    }

    #endregion 背包

    #region 评价

    [Serializable]
    internal class rate_start : MergeCommonData
    {
        public static void Track()
        {
            var data = _GetTrackData<rate_start>();
            _TrackData(data);
        }
    }

    [Serializable]
    internal class rate_end : MergeCommonData
    {
        public int result;

        public static void Track(int result_)
        {
            var data = _GetTrackData<rate_end>();
            data.result = result_;
            _TrackData(data);
        }
    }

    #endregion

    #region 积分活动

    [Serializable]
    internal class event_score_milestone : MergeCommonData
    {
        public int event_id;
        public int event_param;
        public int event_from;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public bool is_final;
        public bool is_cycle;
        public int round_num;

        public static void Track(int event_id_, int event_param_, int milestone_queue_, int event_from_, int milestone_num_, int milestone_difficulty_, bool is_final_, bool is_cycle_, int round_num_ = 1)
        {
            var data = _GetTrackData<event_score_milestone>();
            data.event_id = event_id_;
            data.event_param = event_param_;
            data.event_from = event_from_;
            data.milestone_queue = milestone_queue_;
            data.milestone_num = milestone_num_;
            data.milestone_difficulty = milestone_difficulty_;
            data.is_final = is_final_;
            data.is_cycle = is_cycle_;
            data.round_num = round_num_;
            _TrackData(data);
        }
    }

    #endregion
    
    #region 积分活动变种(麦克风版)

    //玩家成功领取里程碑奖励时记录
    [Serializable]
    internal class event_mic_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //该奖励位于里程碑奖励的序数（从1开始，本次计算在内）
        public int milestone_num;   //本轮里程碑的总个数
        public int milestone_difficulty;   //本轮活动难度
        public int round_num;   //活动期间的第几轮（固定为1）
        public bool is_final;   //是否是本轮最后一个里程碑

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, bool isFinal)
        {
            var data = _GetTrackData<event_mic_milestone>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.round_num = 1;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }
    
    //玩家积分增长时记录
    [Serializable]
    internal class event_mic_token : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int id; //获得token的id
        public string from;   //积分来源
        public bool is_add;   //是否是增加
        public int amount_change;   //获得积分数量（按基础积分换算后的值）
        public bool is_double;   //是否是双倍

        public static void Track(ActivityLike acti_, int id, ReasonString reason, bool isAdd, int num, bool isDouble)
        {
            var data = _GetTrackData<event_mic_token>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.id = id;
            data.from = reason;
            data.is_add = isAdd;
            data.amount_change = num;
            data.is_double = isDouble;
            _TrackData(data);
        }
    }
    
    #endregion

    #region 寻宝活动

    [Serializable]
    internal class event_treasure_milestone : MergeCommonData
    {
        public int event_id;
        public int event_param;
        public int event_from;
        public int treasure_group_id;
        public int round_num;
        public int milestone_num;
        public int milestone_queue;
        public int milestone_difficulty;
        public bool is_final;

        public static void Track(int event_id_, int event_param_, int treasure_group_id_, int event_from_, int rount_num_, int milestone_num_, int milestone_queue_, int milestone_difficulty_, bool is_final_)
        {
            var data = _GetTrackData<event_treasure_milestone>();
            data.event_id = event_id_;
            data.event_param = event_param_;
            data.event_from = event_from_;
            data.treasure_group_id = treasure_group_id_;
            data.round_num = rount_num_;
            data.milestone_num = milestone_num_;
            data.milestone_queue = milestone_queue_;
            data.milestone_difficulty = milestone_difficulty_;
            data.is_final = is_final_;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class event_treasure_bonus : MergeCommonData
    {
        public int event_id;
        public int event_param;
        public int event_from;
        public int milestone_stage;
        public int milestone_num;
        public int milestone_difficulty;
        public bool is_final;
        public int round_num;

        public static void Track(int event_id_, int event_param_, int event_from_, int milestone_stage_, int milestone_num_, int milestone_difficulty_, bool is_final_, int round_num_)
        {
            var data = _GetTrackData<event_treasure_bonus>();
            data.event_id = event_id_;
            data.event_param = event_param_;
            data.event_from = event_from_;
            data.milestone_stage = milestone_stage_;
            data.milestone_num = milestone_num_;
            data.milestone_difficulty = milestone_difficulty_;
            data.is_final = is_final_;
            data.round_num = round_num_;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class event_treasure_score : MergeCommonData
    {
        public int event_id;
        public int event_param;
        public int event_from;
        public int milestone_queue;
        public int round_num;

        public static void Track(int event_id_, int event_param_, int milestone_queue_, int event_from_, int round_num_)
        {
            var data = _GetTrackData<event_treasure_score>();
            data.event_id = event_id_;
            data.event_param = event_param_;
            data.event_from = event_from_;
            data.milestone_queue = milestone_queue_;
            data.round_num = round_num_;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class treasure_bag_change : MergeCommonData
    {
        public int is_in;
        public int item_id;
        public int item_num;

        public static void Track(int is_in_, int item_id_, int item_num_)
        {
            var data = _GetTrackData<treasure_bag_change>();
            data.is_in = is_in_;
            data.item_id = item_id_;
            data.item_num = item_num_;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class event_treasure : MergeCommonData
    {
        public int event_id;
        public int event_param;
        public int event_from;
        public int treasure_group_id;
        public int treasure_level_id;
        public int treasure_reward_id;
        public int is_treasure;
        public int round_num;
        public int level_queue;
        public int reward_queue;

        public static void Track(int event_id_, int event_param_, int treasure_group_id_, int treasure_level_id_,
            int treasure_reward_id_, int is_treasure_, int event_from_, int round_num_, int level_queue_, int reward_queue_)
        {
            var data = _GetTrackData<event_treasure>();
            data.event_id = event_id_;
            data.event_param = event_param_;
            data.event_from = event_from_;
            data.treasure_group_id = treasure_group_id_;
            data.treasure_level_id = treasure_level_id_;
            data.treasure_reward_id = treasure_reward_id_;
            data.is_treasure = is_treasure_;
            data.round_num = round_num_;
            data.level_queue = level_queue_;
            data.reward_queue = reward_queue_;
            _TrackData(data);
        }
    }

    #endregion

    #region 装饰区活动

    [Serializable]
    internal class event_decorate : MergeCommonData
    {
        public int event_id;
        public int event_param;
        public int event_from;
        public int round_num;
        public int decorate_level_id;
        public int decorate_id;

        public static void Track(int event_id, int event_param, int round_num, int decorate_level_id, int decorate_id,
            int event_from)
        {
            var data = _GetTrackData<event_decorate>();
            data.event_id = event_id;
            data.event_param = event_param;
            data.event_from = event_from;
            data.round_num = round_num;
            data.decorate_level_id = decorate_level_id;
            data.decorate_id = decorate_id;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class event_decorate_levelcomp : MergeCommonData
    {
        public int event_id;
        public int event_param;
        public int event_from;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public bool is_final;
        public int decorate_levelcomp_queue;

        public static void Track(int id, int param, int from, int round, int level, int milestone_queue, int milestone_num, int milestone_difficulty, bool is_final)
        {
            var data = _GetTrackData<event_decorate_levelcomp>();
            data.event_id = id;
            data.event_param = param;
            data.event_from = from;
            data.round_num = round;
            data.decorate_levelcomp_queue = level;
            data.milestone_queue = milestone_queue;
            data.milestone_num = milestone_num;
            data.milestone_difficulty = milestone_difficulty;
            data.is_final = is_final;
            _TrackData(data);
        }
    }

    #endregion

    #region 活动

    [Serializable]
    internal class event_active : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public EventType event_type;
        public int event_param;
        public string local_time;

        public static void Track(ActivityLike acti_)
        {
            var data = _GetTrackData<event_active>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.event_type = acti_.Type;
            var rH = Game.Manager.configMan.globalConfig.RequireTypeUtcClock;
            data.local_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            _TrackData(data);
        }
    }

    [Serializable]
    internal class event_popup : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public EventType event_type;
        public int event_param;
        public string local_time;

        public static void Track(ActivityLike acti_)
        {
            var data = _GetTrackData<event_popup>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.event_type = acti_.Type;
            var rH = Game.Manager.configMan.globalConfig.RequireTypeUtcClock;
            data.local_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            _TrackData(data);
        }
    }

    [Serializable]
    internal class event_step_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public bool is_final;

        public static void Track(ActivityLike acti_, int diff_)
        {
            var data = _GetTrackData<event_step_milestone>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.milestone_queue = 1;
            data.milestone_num = 1;
            data.milestone_difficulty = diff_;
            data.round_num = 1;
            data.is_final = true;
            _TrackData(data);
        }
    }

    // LandMark: 获得活动 token 时
    [Serializable]
    internal class event_landmark_token : MergeCommonData
    {
        public int event_id;                 // 活动ID（EventTime.id）
        public int event_from;               // 活动来源（0：EventTime；1：EventTrigger）
        public int event_param;              // 活动模版ID
        public int milestone_difficulty;     // 活动难度（对应RFM分层）
        public int milestone_num;            // 一共需要的token数
        public int milestone_queue;          // 已获得token数
        public int round_num;                // 里程碑轮数（固定为1）
        public bool is_final;                // 在获得最后一个token时为true

        public static void Track(ActivityLike act, int round, bool isFinal, int milestoneQueue, int milestoneNum, int milestoneDifficulty)
        {
            var data = _GetTrackData<event_landmark_token>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_num = round;
            data.is_final = isFinal;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = milestoneDifficulty;
            _TrackData(data);
        }
    }

    //无限礼包领取奖励
    [Serializable]
    internal class endless_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int reward_queue;
        public bool is_free;

        public static void Track(ActivityLike acti_, int index, bool isFree)
        {
            var data = _GetTrackData<endless_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.reward_queue = index;
            data.is_free = isFree;
            _TrackData(data);
        }
    }

    //无限礼包领取进度条奖励
    [Serializable]
    internal class endless_progreward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //当前进度条奖励在配置中对应的序号 从1开始
        public bool is_final;       //是否是最后一个奖励

        public static void Track(ActivityLike acti_, int index, bool isFinal)
        {
            var data = _GetTrackData<endless_progreward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }

    //1+1礼包领取奖励
    [Serializable]
    internal class oneplusone_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public bool is_free;

        public static void Track(ActivityLike acti_, bool isFree)
        {
            var data = _GetTrackData<oneplusone_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.is_free = isFree;
            _TrackData(data);
        }
    }

    //无限礼包三格版领取奖励
    [Serializable]
    internal class endless_three_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int reward_queue;
        public bool is_free;

        public static void Track(ActivityLike acti_, int index, bool isFree)
        {
            var data = _GetTrackData<endless_three_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.reward_queue = index;
            data.is_free = isFree;
            _TrackData(data);
        }
    }

    //无限礼包三格版领取进度条奖励
    [Serializable]
    internal class endless_three_progreward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //当前进度条奖励在配置中对应的序号 从1开始
        public bool is_final;       //是否是最后一个奖励

        public static void Track(ActivityLike acti_, int index, bool isFinal)
        {
            var data = _GetTrackData<endless_three_progreward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }

    //钻石无限礼包三格版领取奖励
    [Serializable]
    internal class gem_endless_three_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int reward_queue;
        public bool is_free;
        public int pack_id; //礼包ID（CurrencyPack.id）
        public string tga_name; //礼包name（CurrencyPack.tgaName）

        public static void Track(ActivityLike acti_, int index, bool isFree, int packId = 0, string tgaName = "")
        {
            var data = _GetTrackData<gem_endless_three_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.reward_queue = index;
            data.is_free = isFree;
            data.pack_id = packId;
            data.tga_name = tgaName;
            _TrackData(data);
        }
    }

    //钻石三选一领取奖励
    [Serializable]
    internal class gem_threeforone_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int pack_id;
        public string tga_name;

        public static void Track(ActivityLike acti_, int pack_id_, string tga_name_)
        {
            var data = _GetTrackData<gem_threeforone_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.pack_id = pack_id_;
            data.tga_name = tga_name_;
            _TrackData(data);
        }
    }

    //体力多档礼包
    [Serializable]
    internal class energymultipack_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;

        public static void Track(ActivityLike acti_)
        {
            var data = _GetTrackData<energymultipack_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            _TrackData(data);
        }
    }

    //1+2礼包领取奖励
    [Serializable]
    internal class oneplustwo_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int reward_queue;
        public bool is_free;

        //index = 1付费奖励 =2第一列免费奖励 =3第二列免费奖励
        public static void Track(ActivityLike acti_, int index, bool isFree)
        {
            var data = _GetTrackData<oneplustwo_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.reward_queue = index;
            data.is_free = isFree;
            _TrackData(data);
        }
    }

    //等级礼包领取奖励
    [Serializable]
    internal class level_pack_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int pack_id;
        public int level_default;

        //index = 1付费奖励 =2第一列免费奖励 =3第二列免费奖励
        public static void Track(ActivityLike acti_, int packId, int levelDefault)
        {
            var data = _GetTrackData<level_pack_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.pack_id = packId;
            data.level_default = levelDefault;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class threeforone_reward : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;

        public static void Track(ActivityLike acti_)
        {
            var data = _GetTrackData<threeforone_reward>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            _TrackData(data);
        }
    }

    //进阶礼包
    [Serializable]
    internal class progresspack_reward : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;

        public static void Track(ActivityLike acti_)
        {
            var data = _GetTrackData<progresspack_reward>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            _TrackData(data);
        }
    }

    //闪卡必得礼包
    [Serializable]
    internal class shinnyguarpack_reward : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public string show_group_id;
        public string show_card_id;
        public string card_is_new;

        public static void Track(ActivityLike acti_, string show_group_id, string show_card_id, string card_is_new)
        {
            var data = _GetTrackData<shinnyguarpack_reward>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.show_group_id = show_group_id;
            data.show_card_id = show_card_id;
            data.card_is_new = card_is_new;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class shinnyguarpack_show : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public string show_group_id;
        public string show_card_id;
        public string card_is_new;

        public static void Track(ActivityLike acti_, string show_group_id, string show_card_id, string card_is_new)
        {
            var data = _GetTrackData<shinnyguarpack_show>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.show_group_id = show_group_id;
            data.show_card_id = show_card_id;
            data.card_is_new = card_is_new;
            _TrackData(data);
        }
    }

    //付费留存礼包
    [Serializable]
    internal class retentionpack_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int reward_queue;
        public bool is_free;

        //index = 1付费奖励 =2第二行的免费奖励 =3第三行的免费奖励
        public static void Track(ActivityLike acti_, int index, bool isFree)
        {
            var data = _GetTrackData<retentionpack_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.reward_queue = index;
            data.is_free = isFree;
            _TrackData(data);
        }
    }


    //商店留存礼包领取奖励
    [Serializable]
    internal class marketslidepack_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int event_packid; //礼包详情id（MarketSlidePackDetail.id）

        public static void Track(ActivityLike acti_, int detailId)
        {
            var data = _GetTrackData<marketslidepack_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.event_packid = detailId;
            _TrackData(data);
        }
    }

    internal class event_spinpack_claim : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int reward_id;
        public bool is_free;
        public bool is_final;
        public static void Track(ActivityLike activity, int queue, int num, int diff, int id, bool free, bool final)
        {
            var data = _GetTrackData<event_spinpack_claim>();
            (data.event_id, data.event_from, data.event_param) = activity.Info3;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.milestone_difficulty = diff;
            data.reward_id = id;
            data.is_free = free;
            data.is_final = final;
            _TrackData(data);
        }
    }

    #region 印章活动

    //盖章成功时
    [Serializable]
    internal class event_stamp_collect : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int level;   //盖章的层数(从1开始)
        public int milestone_queue; //本轮第几次盖章（从1开始，换轮次时重新计数）
        public int milestone_num; //本轮需要盖章几次
        public int milestone_difficulty; //本轮活动难度（固定为1）
        public int round_num;   //活动期间的第几轮
        public bool is_final; //是否是本轮最后一个里程碑

        public static void Track(ActivityLike acti_, int finishIndex, int roundIndex, int totalNum, bool isFinal)
        {
            var data = _GetTrackData<event_stamp_collect>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.level = finishIndex;
            data.milestone_queue = finishIndex;
            data.milestone_num = totalNum;
            data.milestone_difficulty = 1;
            data.round_num = roundIndex;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }

    //活动结束时
    [Serializable]
    internal class event_stamp_end : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue;   //盖章的层数(从1开始)
        public int round_num;   //当前是第几轮

        public static void Track(ActivityLike acti_, int finishIndex, int roundIndex)
        {
            var data = _GetTrackData<event_stamp_end>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = finishIndex;
            data.round_num = roundIndex;
            _TrackData(data);
        }
    }

    //领取盖章奖励时
    [Serializable]
    internal class event_stamp_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int reward_from;  //奖励来源(0:小奖 1:大奖)
        public string item_info;   //获得的道具(id:数量 逗号隔开)

        public static void Track(ActivityLike acti_, int from, string itemInfo)
        {
            var data = _GetTrackData<event_stamp_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.reward_from = from;
            data.item_info = itemInfo;
            _TrackData(data);
        }
    }

    #endregion

    #region 体力列表礼包

    //完成任务时
    [Serializable]
    internal class event_erglist_complete : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //第几个任务(从1开始)
        public int milestone_num; //本轮任务总数
        public int milestone_difficulty; //活动难度(ErgListDetail.diff)
        public bool is_buy;   //是否已购买
        public bool is_final; //是否为最后一条任务

        public static void Track(ActivityLike acti_, int index, int total, int diff, bool isBuy, bool isFinal)
        {
            var data = _GetTrackData<event_erglist_complete>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = total;
            data.milestone_difficulty = diff;
            data.is_buy = isBuy;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }

    //购买时
    [Serializable]
    internal class event_erglist_purchase : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_difficulty; //活动难度(ErgListDetail.diff)

        public static void Track(ActivityLike acti_, int diff)
        {
            var data = _GetTrackData<event_erglist_purchase>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_difficulty = diff;
            _TrackData(data);
        }
    }

    //领取奖励时
    [Serializable]
    internal class event_erglist_rwd : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int reward_queue; //本轮领取的第几个奖励（累加值，从1开始）
        public bool is_final; //是否为本轮最后一个奖励（领取行为而非奖励排序）

        public static void Track(ActivityLike acti_, int index, bool isFinal)
        {
            var data = _GetTrackData<event_erglist_rwd>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.reward_queue = index;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }


    #endregion

    #endregion 活动

    #region mail

    [Serializable]
    internal class mail_read : MergeCommonData
    {
        public string type;
        public string from_uid;
        public bool is_treasure;
        public bool is_link;

        public static void Track(string type, string fromUid, bool isTreasure, bool isLink)
        {
            var data = _GetTrackData<mail_read>();
            data.type = type;
            data.from_uid = fromUid;
            data.is_treasure = isTreasure;
            data.is_link = isLink;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class mail_reward : MergeCommonData
    {
        public string type;
        public string from_uid;
        public string json;
        public string extInfo;
        public bool is_treasure;
        public bool is_link;

        public static void Track(string type, string fromUid, string titleOrKey, IDictionary<int, int> rewards, bool isTreasure, bool isLink)
        {
            var data = _GetTrackData<mail_reward>();
            data.type = type;
            data.from_uid = fromUid;
            data.extInfo = titleOrKey;
            data.is_treasure = isTreasure;
            data.is_link = isLink;
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                var count = 0;
                sb.Append("{");
                foreach (var kv in rewards)
                {
                    ++count;
                    sb.Append($"\"{kv.Key}\":{kv.Value}");
                    if (count < rewards.Count) sb.Append(",");
                }

                sb.Append("}");
                data.json = sb.ToString();
            }

            _TrackData(data);
        }
    }


    [Serializable]
    internal class mail_receive : MergeCommonData
    {
        public string type;
        public string from_uid;
        public bool is_treasure;
        public string json;
        public string extInfo;
        public bool is_link;

        public static void Track(string type, string fromUid, bool isTreasure, string titleOrKey, IDictionary<int, int> rewards, bool isLink)
        {
            var data = _GetTrackData<mail_receive>();
            data.type = type;
            data.from_uid = fromUid;
            data.is_treasure = isTreasure;
            data.extInfo = titleOrKey;
            data.is_link = isLink;
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                var count = 0;
                sb.Append("{");
                foreach (var kv in rewards)
                {
                    ++count;
                    sb.Append($"\"{kv.Key}\":{kv.Value}");
                    if (count < rewards.Count) sb.Append(",");
                }

                sb.Append("}");
                data.json = sb.ToString();
            }

            _TrackData(data);
        }
    }

    [Serializable]
    internal class mail_link : MergeCommonData
    {
        public string type;
        public string from_uid;
        public string json;
        public string extInfo;
        public bool is_treasure;
        public bool is_link;

        public static void Track(string type, string fromUid, string titleOrKey, IDictionary<int, int> rewards, bool isTreasure, bool isLink)
        {
            var data = _GetTrackData<mail_link>();
            data.type = type;
            data.from_uid = fromUid;
            data.extInfo = titleOrKey;
            data.is_treasure = isTreasure;
            data.is_link = isLink;
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                var count = 0;
                sb.Append("{");
                foreach (var kv in rewards)
                {
                    ++count;
                    sb.Append($"\"{kv.Key}\":{kv.Value}");
                    if (count < rewards.Count) sb.Append(",");
                }

                sb.Append("}");
                data.json = sb.ToString();
            }
            _TrackData(data);
        }
    }

    #endregion

    #region 开关

    [Serializable]
    internal class notification : MergeCommonData
    {
        public bool is_on;

        public static void Track(bool v_)
        {
            var data = _GetTrackData<notification>();
            data.is_on = v_;
            _TrackData(data);
            TraceUser().Notification(v_).Apply();
        }
    }

    [Serializable]
    internal class notification_popup : MergeCommonData
    {
        public string type_id;
        public static void Track(string type_id_)
        {
            var data = _GetTrackData<notification_popup>();
            data.type_id = type_id_;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class notification_button : MergeCommonData
    {
        public string type_id;
        public static void Track(string type_id_)
        {
            var data = _GetTrackData<notification_button>();
            data.type_id = type_id_;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class user_notifi : DataTrackBase
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public string event_type;
        public int id;
        public static void Track(int event_id_, int event_from_, int event_param_, string event_type_, int notice_detail_id_)
        {
            var data = _GetTrackData<user_notifi>();
            data.event_id = event_id_;
            data.event_from = event_from_;
            data.event_param = event_param_;
            data.event_type = event_type_;
            data.id = notice_detail_id_;
            _TrackData(data);
        }
    }

    //能量加倍开关打点
    [Serializable]
    internal class boost : MergeCommonData
    {
        public bool is_on;
        public int bet;

        public static void Track(bool v_, int bet_)
        {
            var data = _GetTrackData<boost>();
            data.is_on = v_;
            data.bet = bet_;
            _TrackData(data);
            TraceUser().Boost(bet_).Apply();
        }
    }

    public static void SettingDialog(bool v_)
    {
        var data = BorrowTrackObject();
        data["is_on"] = v_;
        TrackObject(data, "dialog");
        TraceUser().Dialog(v_).Apply();
    }

    #endregion 开关

    #region 问卷

    [Serializable]
    internal class Survey : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;

        public static void TrackEnter(ActivityLike acti_)
        {
            var data = (Survey)_GetTrackData(typeof(Survey));
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            _TrackData(data, "survey_enter");
        }

        public static void TrackReward(ActivityLike acti_)
        {
            var data = _GetTrackData<Survey>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            _TrackData(data, "survey_reward");
        }
    }

    #endregion 问卷

    #region 社区跳转

    [Serializable]
    internal class settings_community : MergeCommonData
    {
        public int type_id;

        public static void Track(int type_id)
        {
            var data = _GetTrackData<settings_community>();
            data.type_id = type_id;
            _TrackData(data);
        }
    }

    #endregion 社区跳转

    #region 热气球

    [Serializable]
    internal class event_race : MergeCommonData
    {
        public int event_id;
        public int event_param;
        public int event_from;
        public int round_queue;
        public bool is_robot;
        public int robot_id;
        public int race_num;
        public int race_score_num;
        public int race_score_time;
        public bool is_race_num;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public bool is_final;
        public bool is_cycle;

        public static void Track(int event_id, int event_param, int event_from, int round_queue, bool is_robot,
            int robot_id,
            int race_num, int race_score_num, int race_score_time, bool is_race_num, int milestone_queue, int milestone_num,
            int milestone_difficulty, int round_num, bool is_final, bool is_cycle)
        {
            var data = _GetTrackData<event_race>();
            data.event_id = event_id;
            data.event_param = event_param;
            data.event_from = event_from;
            data.round_queue = round_queue;
            data.is_robot = is_robot;
            data.robot_id = robot_id;
            data.race_num = race_num;
            data.race_score_num = race_score_num;
            data.race_score_time = race_score_time;
            data.is_race_num = is_race_num;
            data.milestone_queue = milestone_queue;
            data.milestone_num = milestone_num;
            data.milestone_difficulty = milestone_difficulty;
            data.round_num = round_num;
            data.is_final = is_final;
            data.is_cycle = is_cycle;
            _TrackData(data);
        }
    }

    #endregion

    #region 迷你棋盘

    //解锁棋子阶段奖励时
    [Serializable]
    internal class event_miniboard_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //该奖励位于里程碑奖励的序数（从1开始，本次计算在内）
        public int milestone_num;   //本轮目标棋子个数
        public int milestone_difficulty;   //本轮活动难度（EventMiniBoardDetail.diff）
        public int round_num;   //活动期间的第几轮（固定为1）
        public bool is_final;   //是否是本轮最后一个里程碑

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, bool isFinal)
        {
            var data = _GetTrackData<event_miniboard_milestone>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.round_num = 1;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }

    //活动结束有未收集规定范围的奖励棋子时
    [Serializable]
    internal class event_miniboard_end_collect : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int item_id; //回收的棋子id
        public int num; //回收的棋子数量

        public static void Track(ActivityLike acti_, int itemId, int itemNum)
        {
            var data = _GetTrackData<event_miniboard_end_collect>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.item_id = itemId;
            data.num = itemNum;
            _TrackData(data);
        }
    }

    //获得迷你棋盘棋子时
    [Serializable]
    internal class event_miniboard_getitem : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int board_id; //来源棋盘id
        public int item_id; //生成的棋子id
        public int item_num; //生成的棋子数量
        public int item_level; //棋子在所属合成链中的等级

        public static void Track(ActivityLike acti_, int boardId, int itemId, int itemNum, int itemLevel)
        {
            var data = _GetTrackData<event_miniboard_getitem>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.board_id = boardId;
            data.item_id = itemId;
            data.item_num = itemNum;
            data.item_level = itemLevel;
            _TrackData(data);
        }
    }

    #endregion

    #region 多轮迷你棋盘

    //解锁棋子阶段奖励时
    [Serializable]
    internal class event_miniboard_multi_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //本轮本棋盘第几个目标棋子（从1开始，换轮次或换棋盘时都重新计数）
        public int milestone_num; //本轮本棋盘目标棋子个数
        public int milestone_difficulty; //本轮活动难度（EventMiniBoardMultiGroup.diff）
        public int round_num; //活动期间的第几轮（固定为1）
        public bool is_final; //是否是本轮最后一个里程碑
        public int board_queue; //本轮的第几个棋盘
        public int board_id;    //来源于棋盘（MergeBoard.id）
        public int board_num;   //棋盘序号（从1开始，本次计算在内）

        public static void Track(ActivityLike acti_, int index, int boardId, int curRoundIndex, int milestoneNum, int diff, bool isFinal)
        {
            var data = _GetTrackData<event_miniboard_multi_milestone>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.round_num = 1;
            data.is_final = isFinal;
            data.board_queue = curRoundIndex;
            data.board_id = boardId;
            data.board_num = curRoundIndex;
            _TrackData(data);
        }
    }

    //活动结束有未收集规定范围的奖励棋子时
    [Serializable]
    internal class event_miniboard_multi_end_collect : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public string itemsinfo; //回收的棋子信息 id:数量

        public static void Track(ActivityLike acti_, string itemsInfo)
        {
            var data = _GetTrackData<event_miniboard_multi_end_collect>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.itemsinfo = itemsInfo;
            _TrackData(data);
        }
    }

    //获得迷你棋盘棋子时
    [Serializable]
    internal class event_miniboard_multi_getitem : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int board_id; //来源棋盘id
        public int board_num;   //棋盘序号（从1开始，本次计算在内）
        public int item_id; //生成的棋子id
        public int item_num; //生成的棋子数量
        public int item_level; //棋子在所属合成链中的等级

        public static void Track(ActivityLike acti_, int boardId, int boardNum, int itemId, int itemNum, int itemLevel)
        {
            var data = _GetTrackData<event_miniboard_multi_getitem>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.board_id = boardId;
            data.board_num = boardNum;
            data.item_id = itemId;
            data.item_num = itemNum;
            data.item_level = itemLevel;
            _TrackData(data);
        }
    }

    //进入新棋盘时
    [Serializable]
    internal class event_miniboard_multi_newboard : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int board_id; //来源棋盘id
        public int board_num;   //棋盘序号（从1开始，本次计算在内）
        public string bonus_itemsinfo; //bonus和tap bonus棋子id:数量:等级
        public string inherit_itemsinfo; //继承棋子id:数量:等级
        public string gift_box_itemsinfo; //奖励箱内棋子id:数量:等级

        public static void Track(ActivityLike acti_, int boardId, int boardNum, string bonusItemsInfo, string inheritItemsInfo, string giftBoxItemsInfo)
        {
            var data = _GetTrackData<event_miniboard_multi_newboard>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.board_id = boardId;
            data.board_num = boardNum;
            data.bonus_itemsinfo = bonusItemsInfo;
            data.inherit_itemsinfo = inheritItemsInfo;
            data.gift_box_itemsinfo = giftBoxItemsInfo;
            _TrackData(data);
        }
    }

    #endregion

    #region 挖矿棋盘

    //完成里程碑时
    [Serializable]
    internal class event_mine_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //这轮的第几个里程碑（从1开始）
        public int milestone_num;   //本轮里程碑个数
        public int milestone_difficulty;   //本轮活动难度（EventMineGroup.diff）
        public int round_num;   //活动期间的第几轮（固定为1）
        public bool is_final;   //是否是本轮最后一个里程碑
        public int board_id;    //来源于棋盘（MergeBoard.id）
        public int board_row;   //当时棋盘深度m
        public string milestone_reward; //奖励内容

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, bool isFinal, int boardId, int depth, string reward)
        {
            var data = _GetTrackData<event_mine_milestone>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.round_num = 1;
            data.is_final = isFinal;
            data.board_id = boardId;
            data.board_row = depth;
            data.milestone_reward = reward;
            _TrackData(data);
        }
    }

    //完成棋子图鉴时
    [Serializable]
    internal class event_mine_gallery_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //本轮本棋盘第几个目标棋子（从1开始，换轮次或换棋盘时都重新计数）
        public int milestone_num; //本轮本棋盘目标棋子个数
        public int milestone_difficulty; //本轮活动难度（EventMineGroup.diff）
        public int round_num; //活动期间的第几轮（固定为1）
        public bool is_final; //是否是本轮最后一个图鉴
        public int board_id;    //来源于棋盘（MergeBoard.id）
        public int board_row;   //当时棋盘深度m

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, bool isFinal, int boardId, int depth)
        {
            var data = _GetTrackData<event_mine_gallery_milestone>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.round_num = 1;
            data.is_final = isFinal;
            data.board_id = boardId;
            data.board_row = depth;
            _TrackData(data);
        }
    }

    //活动结束领取遗留奖励时
    [Serializable]
    internal class event_mine_end_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public string itemsinfo; //回收的棋子信息 id:数量

        public static void Track(ActivityLike acti_, string itemsInfo)
        {
            var data = _GetTrackData<event_mine_end_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.itemsinfo = itemsInfo;
            _TrackData(data);
        }
    }

    //挖矿1+1礼包领取奖励
    [Serializable]
    internal class oneplusone_mine_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public bool is_free;

        public static void Track(ActivityLike acti_, bool isFree)
        {
            var data = _GetTrackData<oneplusone_mine_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.is_free = isFree;
            _TrackData(data);
        }
    }

    #endregion

    #region 矿车棋盘

    //使用最高级进度棋子时
    [Serializable]
    internal class event_minecart_foward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //活动期间第几个里程碑（从1开始，累加）
        public int milestone_num;   //活动里程碑总数（1轮总数，不计算循环里程碑）
        public int milestone_difficulty;   //活动难度(EventMineCartDetail.diff)
        public int board_id;    //来源棋盘（MergeBoard.id）
        public int board_row;   //当前棋盘深度m（类似挖矿逻辑）
        public int round_num; //本次活动第几轮（从1开始，递增）
        public int score_num; //当前里程碑进度值（本次增加分数后）

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, int boardId, int depth, int roundNum, int scoreNum)
        {
            var data = _GetTrackData<event_minecart_foward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.board_id = boardId;
            data.board_row = depth;
            data.round_num = roundNum;
            data.score_num = scoreNum;
            _TrackData(data);
        }
    }

    //获得回合奖励时（大奖）
    [Serializable]
    internal class event_minecart_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //第几个回合奖励
        public int milestone_num;   //回合总数（EventMineCartDetail.rounId数量）
        public int milestone_difficulty;   //活动难度(EventMineCartDetail.diff)
        public bool is_final;   //是否为本轮最后一个里程碑
        public int milestone_id;    //里程碑id（EventMineCartRound.id）
        public int board_id;    //来源棋盘（MergeBoard.id）
        public int board_row;   //当前棋盘深度m（类似挖矿逻辑）
        public int round_num; //本次活动第几轮（从1开始，递增）

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, bool isFinal, int milestoneId, int boardId, int depth, int roundNum)
        {
            var data = _GetTrackData<event_minecart_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.is_final = isFinal;
            data.milestone_id = milestoneId;
            data.board_id = boardId;
            data.board_row = depth;
            data.round_num = roundNum;
            _TrackData(data);
        }
    }

    //完成图鉴时
    [Serializable]
    internal class event_minecart_gallery : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //第几个目标棋子（从1开始，EventMineCart.handBook）
        public int milestone_num;   //目标棋子总数（EventMineCart.handBook）
        public int milestone_difficulty;   //该里程碑认定难度(EventMineCartDetail.diff)
        public bool is_final;   //是否为最后一个目标棋子
        public int board_id;    //来源棋盘（MergeBoard.id）
        public int board_row;   //当前棋盘深度m（类似挖矿逻辑）
        public int round_num; //本次活动第几轮（从1开始，递增）

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, bool isFinal, int boardId, int depth, int roundNum)
        {
            var data = _GetTrackData<event_minecart_gallery>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.is_final = isFinal;
            data.board_id = boardId;
            data.board_row = depth;
            data.round_num = roundNum;
            _TrackData(data);
        }
    }

    //提交随机订单获得棋子时
    [Serializable]
    internal class event_minecart_getitem_order : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_difficulty;   //活动难度(EventMineCartDetail.diff)
        public int board_id;    //来源棋盘（MergeBoard.id）
        public int board_row;   //当前棋盘深度m（类似挖矿逻辑）
        public int round_num; //本次活动第几轮（从1开始，递增）
        public int item_id; //生成的棋子id
        public int item_level; //生成的棋子在合成链内的等级
        public int item_num; //生成的棋子数量
        public int pay_difficulty;  //订单的付出难度

        public static void Track(ActivityLike acti_, int diff, int boardId, int depth, int roundNum, int itemId, int itemLevel, int itemNum, int payDifficulty)
        {
            var data = _GetTrackData<event_minecart_getitem_order>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_difficulty = diff;
            data.board_id = boardId;
            data.board_row = depth;
            data.round_num = roundNum;
            data.item_id = itemId;
            data.item_level = itemLevel;
            data.item_num = itemNum;
            data.pay_difficulty = payDifficulty;
            _TrackData(data);
        }
    }

    //点击耗体生成器获得棋子时
    [Serializable]
    internal class event_minecart_getitem_tap : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_difficulty;   //活动难度(EventMineCartDetail.diff)
        public int board_id;    //来源棋盘（MergeBoard.id）
        public int board_row;   //当前棋盘深度m（类似挖矿逻辑）
        public int round_num; //本次活动第几轮（从1开始，递增）
        public int item_id; //生成的棋子id
        public int item_level; //生成的棋子在合成链内的等级
        public int item_num; //生成的棋子数量

        public static void Track(ActivityLike acti_, int diff, int boardId, int depth, int roundNum, int itemId, int itemLevel, int itemNum)
        {
            var data = _GetTrackData<event_minecart_getitem_tap>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_difficulty = diff;
            data.board_id = boardId;
            data.board_row = depth;
            data.round_num = roundNum;
            data.item_id = itemId;
            data.item_level = itemLevel;
            data.item_num = itemNum;
            _TrackData(data);
        }
    }

    //活动结束领取遗留奖励时
    [Serializable]
    internal class event_minecart_end_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public string rewardinfo; //回收的棋子信息 id:数量

        public static void Track(ActivityLike acti_, string itemsInfo)
        {
            var data = _GetTrackData<event_minecart_end_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.rewardinfo = itemsInfo;
            _TrackData(data);
        }
    }

    //矿车 1+1 领取奖励时
    [Serializable]
    internal class oneplusone_minecart_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public bool is_free;

        public static void Track(ActivityLike acti_, bool isFree)
        {
            var data = _GetTrackData<oneplusone_minecart_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.is_free = isFree;
            _TrackData(data);
        }
    }

    #endregion

    #region 第三方绑定

    public static void AccountBinding(int state_, bool switch_, string pId_, AccountLoginType pType_,
        bool result_ = true, SDKError e_ = null)
    {
        var nT = switch_ ? "change" : "binding";
        var nR = (state_, result_) switch
        {
            (0, _) => "start",
            (1, true) => "succeed",
            (1, false) => "failed",
            _ => null
        };
        if (nR == null) return;
        var name = $"account_{nT}_{nR}";
        var data = BorrowTrackObject();
        data["user_platform_id"] = pId_;
        data["user_platform_type"] = pType_ switch
        {
            AccountLoginType.Facebook => 0,
            AccountLoginType.Apple => 1,
            AccountLoginType.Google => 2,
            _ => -(int)pType_
        };
        data["code"] = e_?.ErrorCode ?? 0;
        data["message"] = e_?.Message;
        TrackObject(data, name);
        if (nR == "succeed") TraceUser().Binding(pType_, pId_).Apply();
    }

    #endregion 第三方绑定

    #region deeplink

    public static void DeeplinkShare(ActivityInvite acti_, int type_, bool result_ = true, SDKError e_ = null)
    {
        var nR = result_ ? "succeed" : "failed";
        var name = $"event_share_{nR}";
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_from"]) = acti_.Info3;
        data["share_type"] = type_;
        data["code"] = e_?.ErrorCode ?? 0;
        data["message"] = e_?.Message;
        TrackObject(data, name);
    }

    public static void DeeplinkFullfill(AccountMan.Referrer referrer_)
    {
        var name = $"event_share_newuser";
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_from"]) = referrer_.info3;
        data["invite_fpid"] = referrer_.id;
        data["newuser_from_type"] = referrer_.type;
        TrackObject(data, name);
    }

    #endregion deeplink

    #region restart

    public static void TrackRestart()
    {
        TrackObject(BorrowTrackObject(), "restart");
    }

    #endregion restart

    #region 棋盘 / meta / ..

    public static void TrackShowBoard()
    {
        TrackObject(BorrowTrackObject(), "show_board");
    }

    public static void TrackShowMeta()
    {
        TrackObject(BorrowTrackObject(), "show_meta");
    }

    #endregion

    #region 挖沙
    [Serializable]
    internal class digging_token_use : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int level_num;
        public int digging_board_id;
        public int digging_item;
        public string item_info;

        public static void Track(ActivityLike acti_, int level_num, int digging_board_id, int digging_item, string item_info)
        {
            var data = _GetTrackData<digging_token_use>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.level_num = level_num;
            data.digging_board_id = digging_board_id;
            data.digging_item = digging_item;
            data.item_info = item_info;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class digging_random_reward : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int level_num;
        public int digging_board_id;
        public int item_id;
        public int item_num;

        public static void Track(ActivityLike acti_, int level_num, int digging_board_id, int item_id, int item_num)
        {
            var data = _GetTrackData<digging_random_reward>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.level_num = level_num;
            data.digging_board_id = digging_board_id;
            data.item_id = item_id;
            data.item_num = item_num;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class digging_boom : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int level_num;
        public int digging_board_id;
        public int num;

        public static void Track(ActivityLike acti_, int level_num, int digging_board_id, int num)
        {
            var data = _GetTrackData<digging_boom>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.level_num = level_num;
            data.digging_board_id = digging_board_id;
            data.num = num;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class digging_restart : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_num;

        public static void Track(ActivityLike acti_, int round_num)
        {
            var data = _GetTrackData<digging_restart>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.round_num = round_num;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class digging_end : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_total;
        public int digging_token_get_num;
        public int digging_token_use_num;
        public int digging_token_expire;

        public static void Track(ActivityLike acti_, int round_total, int digging_token_get_num,
            int digging_token_use_num, int digging_token_expire)
        {
            var data = _GetTrackData<digging_end>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.round_total = round_total;
            data.digging_token_get_num = digging_token_get_num;
            data.digging_token_use_num = digging_token_use_num;
            data.digging_token_expire = digging_token_expire;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class digging_level_complete : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_num;
        public int level_num;
        public int digging_board_id;
        public int dig_num;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public bool is_final;

        public static void Track(ActivityLike acti_, int round_num, int level_num, int digging_board_id, int dig_num, int milestone_queue, int milestone_num, int milestone_difficulty, bool is_final)
        {
            var data = _GetTrackData<digging_level_complete>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.round_num = round_num;
            data.level_num = level_num;
            data.digging_board_id = digging_board_id;
            data.dig_num = dig_num;
            data.milestone_queue = milestone_queue;
            data.milestone_num = milestone_num;
            data.milestone_difficulty = milestone_difficulty;
            data.is_final = is_final;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class digging_level_random : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int level_num;
        public int digging_board_id;

        public static void Track(ActivityLike acti_, int level_num, int digging_board_id)
        {
            var data = _GetTrackData<digging_level_random>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.level_num = level_num;
            data.digging_board_id = digging_board_id;
            _TrackData(data);
        }
    }
    #endregion

    #region 耗体自选活动
    [Serializable]
    internal class event_wishupon_rwd : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public string item_info;
        public int reward_from;

        public static void Track(ActivityLike acti_, int milestone_queue, int milestone_num, int milestone_difficulty, string reward_str, int reward_from)
        {
            var data = _GetTrackData<event_wishupon_rwd>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.milestone_queue = milestone_queue;
            data.milestone_num = milestone_num;
            data.milestone_difficulty = milestone_difficulty;
            data.item_info = reward_str;
            data.reward_from = reward_from;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class event_wishupon_complete : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_difficulty;

        public static void Track(ActivityLike acti_, int milestone_difficulty)
        {
            var data = _GetTrackData<event_wishupon_complete>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.milestone_difficulty = milestone_difficulty;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class event_wishupon_popup : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int from;

        public static void Track(ActivityLike acti_, int from)
        {
            var data = _GetTrackData<event_wishupon_popup>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.from = from;
            _TrackData(data);
        }
    }
    #endregion

    #region 连续订单
    [Serializable]
    internal class event_orderstreak_complete : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_num;
        public int milestone_difficulty;
        public int order_id;
        public int milestone_sequence;
        public bool is_final;

        public static void Track(ActivityLike acti_, int milestone_num, int milestone_difficulty, int order_id, int milestone_sequence, bool is_final)
        {
            var data = _GetTrackData<event_orderstreak_complete>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.milestone_num = milestone_num;
            data.milestone_difficulty = milestone_difficulty;
            data.order_id = order_id;
            data.milestone_sequence = milestone_sequence;
            data.is_final = is_final;
            _TrackData(data);
        }
    }
    #endregion

    #region 连续限时订单，零度挑战
    [Serializable]
    internal class event_zero_enter : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int challenge_num;
        public int group;
        public int group_id;

        public static void Track(ActivityLike acti_, int challenge_num, int group, int group_id)
        {
            var data = _GetTrackData<event_zero_enter>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.challenge_num = challenge_num;
            data.group = group;
            data.group_id = group_id;
            _TrackData(data);
        }
    }

    internal class event_zero_end : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int challenge_num;
        public int group;
        public int group_id;
        public int end_reason;
        public int order_num;

        public static void Track(ActivityLike acti_, int challenge_num, int group, int end_reason, int order_num, int group_id)
        {
            var data = _GetTrackData<event_zero_end>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.challenge_num = challenge_num;
            data.group = group;
            data.end_reason = end_reason;
            data.order_num = order_num;
            data.group_id = group_id;
            _TrackData(data);
        }
    }

    internal class event_zero_success : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int challenge_num;
        public int group;
        public int group_id;
        public int reward_num;
        public int order_num;
        public int people_num;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public bool is_final;
        public int round_num;

        public static void Track(ActivityLike acti_, int challenge_num, int group, int reward_num, int order_num, int group_id, int people_num, int milestone_queue, int milestone_num, int milestone_difficulty, bool is_final, int round_num)
        {
            var data = _GetTrackData<event_zero_success>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.challenge_num = challenge_num;
            data.group = group;
            data.reward_num = reward_num;
            data.order_num = order_num;
            data.group_id = group_id;
            data.people_num = people_num;
            data.milestone_queue = milestone_queue;
            data.milestone_num = milestone_num;
            data.milestone_difficulty = milestone_difficulty;
            data.is_final = is_final;
            data.round_num = round_num;
            _TrackData(data);
        }
    }

    internal class event_zero_order_fail : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int challenge_num;
        public int group;
        public int group_id;
        public int order_num;
        public int diff;

        public static void Track(ActivityLike acti_, int challenge_num, int group, int order_num, int diff, int group_id)
        {
            var data = _GetTrackData<event_zero_order_fail>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.challenge_num = challenge_num;
            data.group = group;
            data.order_num = order_num;
            data.diff = diff;
            data.group_id = group_id;
            _TrackData(data);
        }
    }

    internal class event_zero_order_end : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int challenge_num;
        public int group;
        public int group_id;
        public int order_num;
        public int diff;
        public int time;
        public int diamond;
        public int out_num;

        public static void Track(ActivityLike acti_, int challenge_num, int group, int order_num, int diff, int time, int diamond, int out_num, int group_id)
        {
            var data = _GetTrackData<event_zero_order_end>();
            (data.event_id, data.event_from, data.event_param) = acti_.Info3;
            data.challenge_num = challenge_num;
            data.group = group;
            data.order_num = order_num;
            data.diff = diff;
            data.time = time;
            data.diamond = diamond;
            data.out_num = out_num;
            data.group_id = group_id;
            _TrackData(data);
        }
    }
    #endregion

    #region 自动手指引导

    internal class auto_finger : MergeCommonData
    {
        public AutoFinger finger_type;
        public int board_id;
        public int item_id;

        public static void Track(AutoFinger type, int board, int item)
        {
            var data = _GetTrackData<auto_finger>();
            data.finger_type = type;
            data.board_id = board;
            data.item_id = item;
            _TrackData(data);
        }
    }

    #endregion

    #region ranking

    public static void RankingStart(ActivityRanking acti_)
    {
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_param"]) = acti_.Info3;
        TrackObject(data, "event_rank_start");
    }

    public static void RankingEnd(ActivityRanking acti_, bool reward_)
    {
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_param"]) = acti_.Info3;
        data["rank"] = acti_.Cache.Data?.Me?.RankingOrder ?? 0;
        data["score_num"] = acti_.RankingScore;
        data["is_getreward"] = reward_;
        TrackObject(data, "event_rank_end");
    }

    public static void RankingEndUI1(ActivityRanking acti_)
    {
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_param"]) = acti_.Info3;
        TrackObject(data, "event_rank_end_rank");
    }

    public static void RankingEndUI2(ActivityRanking acti_)
    {
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_param"]) = acti_.Info3;
        TrackObject(data, "event_rank_end_reward");
    }

    public static void RankingRecord(ActivityRanking acti_)
    {
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_param"]) = acti_.Info3;
        data["rank_record"] = acti_.Cache.Data?.Me?.RankingOrder ?? 0;
        TrackObject(data, "event_rank_online");
    }

    public static void RankingGetMelistoneReward(ActivityRanking acti_, RankMelistoneTrackData milestonData)
    {
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_param"]) = acti_.Info3;
        data["milestone_queue"] = milestonData.MelistoneNum;
        data["milestone_num"] = milestonData.MelistoneAllCount;
        data["rank"] = milestonData.RankOrderNum;
        data["score_num"] = milestonData.CollectScoreNum;
        TrackObject(data, "event_rank_milestone");
    }


    #endregion ranking

    #region api_pack

    public static void APIPackSend(ActivityLike acti_)
    {
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_param"]) = acti_?.Info3 ?? default;
        data["event_type"] = (int)acti_.Type;
        TrackObject(data, "api_pack_send");
    }

    public static void APIPackReturn(ActivityLike acti_, int packId_, bool use_, bool timeout_)
    {
        var data = BorrowTrackObject();
        (data["event_id"], data["event_from"], data["event_param"]) = acti_?.Info3 ?? default;
        data["event_type"] = (int)acti_.Type;
        data["pack_id"] = packId_;
        data["is_api_use"] = use_;
        data["is_timeout"] = timeout_;
        TrackObject(data, "api_pack_return");
    }

    #endregion api_pack

    #region 难度api

    //向难度API发送请求时
    [Serializable]
    internal class api_diff_send : MergeCommonData
    {
        public static void Track()
        {
            var data = _GetTrackData<api_diff_send>();
            _TrackData(data);
        }
    }

    //收到难度API回调时
    [Serializable]
    internal class api_diff_return : MergeCommonData
    {
        public int difficulty;   //难度API返回的难度值
        public bool is_timeout; //是否超时
        public int event_diff;  //RFM的难度值

        public static void Track(int diff, bool isTimeout, int eventDiff)
        {
            var data = _GetTrackData<api_diff_return>();
            data.difficulty = diff;
            data.is_timeout = isTimeout;
            data.event_diff = eventDiff;
            _TrackData(data);
        }
    }

    #endregion

    #region 弹珠掉落

    internal class event_pachinko_drop : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int multiple;
        public int drop_num;
        public int drop_range;
        public int hit_bumper_num;
        public int drop_score;

        public static void Track(ActivityLike act, int mul, int drop_num, int drop_range, int hit_bumper_num,
            int drop_score)
        {
            var data = _GetTrackData<event_pachinko_drop>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.multiple = mul;
            data.drop_num = drop_num;
            data.drop_range = drop_range;
            data.hit_bumper_num = hit_bumper_num;
            data.drop_score = drop_score;
            _TrackData(data);
        }
    }

    internal class event_pachinko_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int event_round;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public bool is_final;

        public static void Track(ActivityLike act, int milestone_queue, int event_round, int milestone_num, int milestone_difficulty, int round_num, bool is_final)
        {
            var data = _GetTrackData<event_pachinko_milestone>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = milestone_queue;
            data.event_round = event_round;
            data.milestone_num = milestone_num;
            data.milestone_difficulty = milestone_difficulty;
            data.round_num = round_num;
            data.is_final = is_final;
            _TrackData(data);
        }
    }

    internal class event_pachinko_restart : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_num;

        public static void Track(ActivityLike act, int round_num)
        {
            var data = _GetTrackData<event_pachinko_restart>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_num = round_num;
            _TrackData(data);
        }
    }

    internal class event_pachinko_end : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_total;
        public int token_expire_num;

        public static void Track(ActivityLike act, int round_total, int expire)
        {
            var data = _GetTrackData<event_pachinko_end>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_total = round_total;
            data.token_expire_num = expire;
            _TrackData(data);
        }
    }

    internal class event_pachinko_bumper_reward : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int bumper_id;
        public int multiple;

        public static void Track(ActivityLike act, int bumper_id, int multiple)
        {
            var data = _GetTrackData<event_pachinko_bumper_reward>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.bumper_id = bumper_id;
            data.multiple = multiple;
            _TrackData(data);
        }
    }

    #endregion

    #region 砍价礼包

    internal class event_discountpack_reward : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int discount_stage;

        public static void Track(ActivityLike act, int discount_stage)
        {
            var data = _GetTrackData<event_discountpack_reward>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.discount_stage = discount_stage;
            _TrackData(data);
        }
    }

    internal class event_discountpack_stage : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int current_stage;
        public bool is_final;

        public static void Track(ActivityLike act, int current_stage, bool is_final)
        {
            var data = _GetTrackData<event_discountpack_stage>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.current_stage = current_stage;
            data.is_final = is_final;
            _TrackData(data);
        }
    }

    #endregion
    #region 1v1竞赛
    internal class event_duel_roundstart : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int event_diff;
        public int round_total;
        public int round_queue;
        public int round_times;
        public int strategy_id;

        public static void Track(ActivityLike act, int eventDiff, int roundTotal, int roundQueue, int roundTimes, int strategyId)
        {
            var data = _GetTrackData<event_duel_roundstart>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.event_diff = eventDiff;
            data.round_total = roundTotal;
            data.round_queue = roundQueue;
            data.round_times = roundTimes;
            data.strategy_id = strategyId;
            _TrackData(data);
        }
    }

    internal class event_duel_roundwin : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int event_diff;

        public int round_total;
        public int round_queue;
        public int round_times;
        public int last_time;
        public int strategy_id;
        public static void Track(ActivityLike act, int eventDiff, int roundTotal, int roundQueue, int roundTimes, int lastTime, int strategyId)
        {
            var data = _GetTrackData<event_duel_roundwin>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.event_diff = eventDiff;
            data.round_total = roundTotal;
            data.round_queue = roundQueue;
            data.round_times = roundTimes;
            data.last_time = lastTime;
            data.strategy_id = strategyId;
            _TrackData(data);
        }
    }

    internal class event_duel_roundlose : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int event_diff;
        public int round_total;
        public int round_queue;
        public int round_times;
        public int strategy_id;
        public static void Track(ActivityLike act, int eventDiff, int roundTotal, int roundQueue, int roundTimes, int strategyId)
        {
            var data = _GetTrackData<event_duel_roundlose>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.event_diff = eventDiff;
            data.round_total = roundTotal;
            data.round_queue = roundQueue;
            data.round_times = roundTimes;
            data.strategy_id = strategyId;
            _TrackData(data);
        }
    }

    internal class event_duel_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int event_diff;
        public int rwd_queue;
        public bool is_final;
        public static void Track(ActivityLike act, int eventDiff, int rwdQueue, bool isFinal)
        {
            var data = _GetTrackData<event_duel_milestone>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.event_diff = eventDiff;
            data.rwd_queue = rwdQueue;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }

    #endregion

    #region 周任务
    internal class event_weeklytask_task : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_stage;
        public int milestone_queue;
        public int task_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public bool is_final;
        public bool is_final_all;

        public static void Track(ActivityLike act, int milestoneStage, int milestoneQueue, int taskQueue, int milestoneNum, int milestoneDifficulty, bool isFinal, bool isFinalAll)
        {
            var data = _GetTrackData<event_weeklytask_task>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_stage = milestoneStage;
            data.milestone_queue = milestoneQueue;
            data.task_queue = taskQueue;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = milestoneDifficulty;
            data.is_final = isFinal;
            data.is_final_all = isFinalAll;
            _TrackData(data);
        }
    }

    internal class event_weeklytask_stage : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public bool is_final;

        public static void Track(ActivityLike act, int milestoneQueue, int milestoneNum, int milestoneDifficulty, int roundNum, bool isFinal)
        {
            var data = _GetTrackData<event_weeklytask_stage>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = milestoneDifficulty;
            data.round_num = roundNum;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }

    internal class event_weeklytask_final : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;

        public static void Track(ActivityLike act, int milestoneNum, int milestoneDifficulty, int roundNum)
        {
            var data = _GetTrackData<event_weeklytask_final>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = milestoneDifficulty;
            data.round_num = roundNum;
            _TrackData(data);
        }
    }
    #endregion

    #region bingo
    internal class event_bingo_submit : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public string item_info;
        public string reward;
        public bool is_bingo;
        public bool is_level_complete;
        public int milestone_num;
        public int milestone_queue;
        public int level_queue;
        public int round_num;
        public static void Track(ActivityLike act, string itemInfo, string reward, bool isBingo, bool isLevelComplete, int milestoneNum, int milestoneQueue, int levelQueue, int roundNum)
        {
            var data = _GetTrackData<event_bingo_submit>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.item_info = itemInfo;
            data.reward = reward;
            data.is_bingo = isBingo;
            data.is_level_complete = isLevelComplete;
            data.milestone_num = milestoneNum;
            data.milestone_queue = milestoneQueue;
            data.level_queue = levelQueue;
            data.round_num = roundNum;
            _TrackData(data);
        }
    }

    internal class event_bingo_complete : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public bool is_bingo_straight;
        public bool is_bingo_slash;
        public bool is_bingo_all;
        public int level_queue;
        public int milestone_queue;
        public int round_num;
        public bool is_level_complete;
        public static void Track(ActivityLike act, bool isBingoStraight, bool isBingoSlash, bool isBingoAll, int levelQueue, int milestoneQueue, int roundNum, bool isLevelComplete)
        {
            var data = _GetTrackData<event_bingo_complete>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.is_bingo_straight = isBingoStraight;
            data.is_bingo_slash = isBingoSlash;
            data.is_bingo_all = isBingoAll;
            data.level_queue = levelQueue;
            data.milestone_queue = milestoneQueue;
            data.round_num = roundNum;
            data.is_level_complete = isLevelComplete;
            _TrackData(data);
        }
    }

    internal class event_bingo_level_complete : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int level_id;
        public int level_queue;
        public int round_num;
        public static void Track(ActivityLike act, int levelId, int levelQueue, int roundNum)
        {
            var data = _GetTrackData<event_bingo_level_complete>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.level_id = levelId;
            data.level_queue = levelQueue;
            data.round_num = roundNum;
            _TrackData(data);
        }
    }

    internal class event_bingo_restart : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_num;
        public static void Track(ActivityLike act, int roundNum)
        {
            var data = _GetTrackData<event_bingo_restart>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_num = roundNum;
            _TrackData(data);
        }
    }

    internal class event_bingo_end : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_total;
        public int level_total;
        public static void Track(ActivityLike act, int roundTotal, int levelTotal)
        {
            var data = _GetTrackData<event_bingo_end>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_total = roundTotal;
            data.level_total = levelTotal;
            _TrackData(data);
        }
    }
    #endregion

    #region 沙堡里程碑
    internal class event_castle_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public bool is_final;
        public static void Track(ActivityLike act, int milestoneQueue, int milestoneNum, int milestoneDifficulty, int roundNum, bool isFinal)
        {
            var data = _GetTrackData<event_castle_milestone>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = milestoneDifficulty;
            data.round_num = roundNum;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }
    #endregion

    #region 钓鱼棋盘
    internal class event_fish_get_fish : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int item_id;         // 捕获的鱼id
        public int milestone_queue; // 此id的鱼捕获数量
        public int milestone_num;   // 总捕获鱼数量
        public bool is_expire;   // 是否转换奖励
        public static void Track(ActivityLike act, int fishId, int count, int totalCount, bool isConvert)
        {
            var data = _GetTrackData<event_fish_get_fish>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.item_id = fishId;
            data.milestone_queue = count;
            data.milestone_num = totalCount;
            data.is_expire = isConvert;
            _TrackData(data);
        }
    }

    internal class event_fish_star : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int item_id;         // 捕获的鱼id
        public int milestone_queue; // 此id的鱼第几个星星(从1开始)
        public static void Track(ActivityLike act, int fishId, int nowStar)
        {
            var data = _GetTrackData<event_fish_star>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.item_id = fishId;
            data.milestone_queue = nowStar;
            _TrackData(data);
        }
    }

    // 里程碑进度满时触发
    internal class event_fish_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue; // 第几个里程碑(从1开始)
        public int milestone_num;   // 共有几个里程碑
        public int milestone_difficulty; // 当前group的难度
        public int round_num;   // 活动期间的第几轮(无用 固定1)
        public bool is_final;   // 是否是最后一个里程碑
        public static void Track(ActivityLike act, int curMilestoneIdx, int totalMilestoneCount, int difficulty)
        {
            var data = _GetTrackData<event_fish_milestone>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = curMilestoneIdx + 1;
            data.milestone_num = totalMilestoneCount;
            data.milestone_difficulty = difficulty;
            data.round_num = 1;
            data.is_final = (curMilestoneIdx + 1) >= totalMilestoneCount;
            _TrackData(data);
        }
    }

    internal class event_fish_end_collect : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public string itemsinfo;
        public static void Track(ActivityLike act, string itemsinfo)
        {
            var data = _GetTrackData<event_fish_end_collect>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.itemsinfo = itemsinfo;
            _TrackData(data);
        }
    }

    internal class event_fish_get_item : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int item_id;
        public int item_num;
        public int item_level;
        public static void Track(ActivityLike act, int itemId, int itemNum, int itemLevel)
        {
            var data = _GetTrackData<event_fish_get_item>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.item_id = itemId;
            data.item_num = itemNum;
            data.item_level = itemLevel;
            _TrackData(data);
        }
    }
    #endregion

    #region 农场棋盘

    //完成里程碑时
    [Serializable]
    internal class event_farm_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //这轮的第几个里程碑（从1开始）
        public int milestone_num;   //本轮里程碑个数
        public int milestone_difficulty;   //本轮活动难度（EventFarmBoardGroup.diff）
        public int board_id;    //来源于棋盘（MergeBoard.id）
        public int round_num;   //活动期间的第几轮（固定为1）
        public int board_row;   //当时棋盘深度m（EventFarmRow 从1开始）
        public int field_num;   //解锁第几个地块（EventFarmBoardFarm 从1开始）
        public bool is_final;   //是否是本轮最后一个里程碑

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, int boardId, int depth, int fieldNum, bool isFinal)
        {
            var data = _GetTrackData<event_farm_milestone>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.board_id = boardId;
            data.round_num = 1;
            data.board_row = depth;
            data.field_num = fieldNum;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }

    //使用 token 时
    [Serializable]
    internal class event_farm_use_token : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //这轮的第几个里程碑（从1开始）
        public int milestone_num;   //本轮里程碑个数
        public int milestone_difficulty;   //本轮活动难度（EventFarmBoardGroup.diff）
        public int board_id;    //来源于棋盘（MergeBoard.id）
        public int round_num;   //活动期间的第几轮（固定为1）
        public int board_row;   //当时棋盘深度m（EventFarmRow 从1开始）
        public int field_num;   //解锁第几个地块（EventFarmBoardFarm 从1开始）
        public string item_info;     //农田产出棋子 id:num

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, int boardId, int depth, int fieldNum, string itemInfo)
        {
            var data = _GetTrackData<event_farm_use_token>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.board_id = boardId;
            data.round_num = 1;
            data.board_row = depth;
            data.field_num = fieldNum;
            data.item_info = itemInfo;
            _TrackData(data);
        }
    }

    //活动结束领取遗留奖励时
    [Serializable]
    internal class event_farm_end_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public string rewardinfo; //回收的棋子信息 id:数量

        public static void Track(ActivityLike acti_, string itemsInfo)
        {
            var data = _GetTrackData<event_farm_end_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.rewardinfo = itemsInfo;
            _TrackData(data);
        }
    }

    //领取农场无限礼包奖励
    [Serializable]
    internal class farm_endless_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int reward_queue;
        public bool is_free;

        public static void Track(ActivityLike acti_, int index, bool isFree)
        {
            var data = _GetTrackData<farm_endless_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.reward_queue = index;
            data.is_free = isFree;
            _TrackData(data);
        }
    }

    #endregion

    #region BP-通行证

    //成功购买通行证时
    [Serializable]
    internal class bp_purchase : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int event_diff;  //活动难度（对应RFM分层）
        public int iap_product_name;  //属于哪一档付费（1：付费1档，2：付费2档，3：升级礼包，4：结束礼包）
        public bool is_late;    //是否是补单

        public static void Track(ActivityLike acti_, int diff, int productName, bool isLate)
        {
            var data = _GetTrackData<bp_purchase>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.event_diff = diff;
            data.iap_product_name = productName;
            data.is_late = isLate;
            _TrackData(data);
        }
    }

    //任务领奖时（每个任务打1个点，循环任务每1个循环打1次点）
    [Serializable]
    internal class bp_task_complete : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int event_diff;  //活动难度（对应RFM分层）
        public int task_id;  //任务id（BpTask.id）
        public int type_id;  //bp激活状态（0：免费，1：付费1档-补领也算，2：付费2档-升级也算）
        public int type;  //任务领取方式（1：手动领取，2：刷新时刻系统领取）
        public int reward_num;  //奖励数量（bp积分数量）

        public static void Track(ActivityLike acti_, int diff, int taskId, int typeId, int type, int rewardNum)
        {
            var data = _GetTrackData<bp_task_complete>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.event_diff = diff;
            data.task_id = taskId;
            data.type_id = typeId;
            data.type = type;
            data.reward_num = rewardNum;
            _TrackData(data);
        }
    }

    //达成里程碑（每达成1档，打一个点）
    [Serializable]
    internal class bp_milestone_complete : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int event_diff;  //活动难度（对应RFM分层）
        public int milestone_queue;  //第几档里程碑（从1开始）
        public int milestone_num;  //本轮里程碑个数
        public bool is_final;   //是否是最后一档里程碑（不包括循环）
        public int level_num;  //里程碑id（BpMilestone.id）
        public int type_id;  //bp激活状态（0：免费，1：付费1档-补领也算，2：付费2档-升级也算）
        public int level_queue;  //里程碑累计计数（从1开始。包含循环档位，循环档位每完成1次+1）

        public static void Track(ActivityLike acti_, int diff, int queue, int num, bool isFinal, int levelNum, int typeId, int levelQueue)
        {
            var data = _GetTrackData<bp_milestone_complete>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.event_diff = diff;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.is_final = isFinal;
            data.level_num = levelNum;
            data.type_id = typeId;
            data.level_queue = levelQueue;
            _TrackData(data);
        }
    }

    //领取里程碑奖励时（每领取1档，打一个点）
    [Serializable]
    internal class bp_milestone_claim : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int event_diff;  //活动难度（对应RFM分层）
        public int milestone_queue;  //第几档里程碑（从1开始）
        public int milestone_num;  //本轮里程碑个数
        public int level_num;  //里程碑id（BpMilestone.id）
        public bool is_free;  //是否是免费奖励（bool）
        public int type_id;  //bp激活状态（0：免费，1：付费1档-补领也算，2：付费2档-升级也算）

        public static void Track(ActivityLike acti_, int diff, int queue, int num, int levelNum, bool isFree, int typeId)
        {
            var data = _GetTrackData<bp_milestone_claim>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.event_diff = diff;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.level_num = levelNum;
            data.is_free = isFree;
            data.type_id = typeId;
            _TrackData(data);
        }
    }

    //达成循环奖励时
    [Serializable]
    internal class bp_cycle_complete : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int event_diff;  //活动难度（对应RFM分层）
        public int num;  //可领取次数
        public int level_num;  //已领取次数
        public int level_queue;  //里程碑累计计数（从1开始。包含循环档位，循环档位每完成1次+1）

        public static void Track(ActivityLike acti_, int diff, int num, int levelNum, int levelQueue)
        {
            var data = _GetTrackData<bp_cycle_complete>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.event_diff = diff;
            data.num = num;
            data.level_num = levelNum;
            data.level_queue = levelQueue;
            _TrackData(data);
        }
    }

    //领取循环奖励时
    [Serializable]
    internal class bp_cycle_claim : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int event_diff;  //活动难度（对应RFM分层）
        public int num;  //本次共领取了几个循环奖励
        public int level_num;    //里程碑id（BpMilestone.id）

        public static void Track(ActivityLike acti_, int diff, int num, int levelNum)
        {
            var data = _GetTrackData<bp_cycle_claim>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.event_diff = diff;
            data.num = num;
            data.level_num = levelNum;
            _TrackData(data);
        }
    }

    //活动结束结算
    [Serializable]
    internal class bp_end_settle : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int event_diff;  //活动难度（对应RFM分层）
        public int type_id;  //bp激活状态（0：免费，1：付费1档，2：付费2档-升级也算）
        public int level_queue;    //里程碑累计计数（从1开始。包含循环档位，循环档位每完成1次+1）

        public static void Track(ActivityLike acti_, int diff, int typeId, int levelQueue)
        {
            var data = _GetTrackData<bp_end_settle>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.event_diff = diff;
            data.type_id = typeId;
            data.level_queue = levelQueue;
            _TrackData(data);
        }
    }

    #endregion

    #region 拼图

    internal class event_puzzle_rwd : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public bool is_final;
        public bool is_cycle;

        public static void Track(ActivityLike act, int queue, int num, int diff, int round, bool isFinal, bool isCycle)
        {
            var data = _GetTrackData<event_puzzle_rwd>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.milestone_difficulty = diff;
            data.round_num = round;
            data.is_final = isFinal;
            data.is_cycle = isCycle;
            _TrackData(data);
        }
    }

    #endregion

    #region 好评订单

    internal class event_orderlike_start : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_num;   // 当前第几轮
        public static void Track(ActivityLike act, int round)
        {
            var data = _GetTrackData<event_orderlike_start>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_num = round;
            _TrackData(data);
        }
    }

    internal class event_orderlike_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_num;       // 当前第几轮
        public int order_id;
        public int order_num;       // 本轮完成的第几个好评订单
        public bool is_final;        // 是否是本轮最后一个订单
        public int milestone_queue; // 进度 cur
        public int milestone_num;   // 进度 max
        public static void Track(ActivityLike act, int round, int orderId, int orderNum, bool isFinal, int milestoneQueue, int milestoneNum)
        {
            var data = _GetTrackData<event_orderlike_milestone>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_num = round;
            data.order_id = orderId;
            data.order_num = orderNum;
            data.is_final = isFinal;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            _TrackData(data);
        }
    }

    internal class event_orderlike_rwd : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_num;       // 当前第几轮
        public int difficulty;      // 实际难度总和
        public string reward_info;  // 奖励信息
        public static void Track(ActivityLike act, int round, int difficulty, string rewardInfo)
        {
            var data = _GetTrackData<event_orderlike_rwd>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_num = round;
            data.difficulty = difficulty;
            data.reward_info = rewardInfo;
            _TrackData(data);
        }
    }

    #endregion
    #region OrderRate
    internal class event_orderrate_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_num;       // 当前第几轮
        public bool is_final;        // 是否是本轮最后一个订单
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public static void Track(ActivityLike act, int round, bool isFinal, int milestoneQueue, int milestoneNum, int milestoneDifficulty)
        {
            var data = _GetTrackData<event_orderrate_milestone>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_num = round;
            data.is_final = isFinal;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = milestoneDifficulty;
            _TrackData(data);
        }
    }

    internal class event_orderrate_rwd : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int round_num;       // 当前第几轮
        public int itemId;      // 实际难度总和
        public int orderId;
        public int milestone_num;
        public int milestone_queue;
        public int milestone_difficulty;
        public int reward_difficulty;
        public bool is_final;
        public string reward_info;  // 奖励信息
        public static void Track(ActivityLike act, int round, int itemId, int orderId, int milestoneNum, int milestoneQueue, int milestoneDifficulty, int rewardDifficulty, bool isFinal)
        {
            var data = _GetTrackData<event_orderrate_rwd>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.round_num = round;
            data.itemId = itemId;
            data.orderId = orderId;
            data.milestone_num = milestoneNum;
            data.milestone_queue = milestoneQueue;
            data.milestone_difficulty = milestoneDifficulty;
            data.reward_difficulty = rewardDifficulty;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }
    #endregion

    #region 签到

    internal class loginsign_rwd : MergeCommonData
    {
        public int today_num;
        public int past_num;
        public bool is_break;
        public int milestone_queue;
        public int milestone_num;
        public int round_num;
        public bool is_final;
        public static void Track(int today, int past, bool isbreak, int queue, int num, int round, bool isfinal)
        {
            var data = _GetTrackData<loginsign_rwd>();
            data.today_num = today;
            data.past_num = past;
            data.is_break = isbreak;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.round_num = round;
            data.is_final = isfinal;
            _TrackData(data);
        }
    }

    #endregion

    #region 签到抽奖
    internal class event_weeklyraffle_tokenget : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue; // 获得第几天的token
        public int milestone_num; // 获得数量
        public int is_buy; // 是否为购买获得
        public int milestone_difficulty; // 活动难度(EventWeeklyRaffleDetail.diff)

        public static void Track(ActivityLike act, int queue, int num, int is_buy, int mile_diff)
        {
            var data = _GetTrackData<event_weeklyraffle_tokenget>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.is_buy = is_buy;
            data.milestone_difficulty = mile_diff;
            _TrackData(data);
        }
    }

    internal class event_weeklyraffle_raffle : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_difficulty;
        public int reward_queue; // 第几次抽奖
        public int is_treasure; // 是否为大奖
        public int is_final; // 是否为最后一次抽奖

        public static void Track(ActivityLike act, int mile_diff, int queue, int treasure, int final)
        {
            var data = _GetTrackData<event_weeklyraffle_raffle>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_difficulty = mile_diff;
            data.reward_queue = queue;
            data.is_treasure = treasure;
            data.is_final = final;

            _TrackData(data);
        }
    }
    #endregion

    #region 三日签到

    //领取签到奖励时发送
    [Serializable]
    internal class threesign_rwd : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int event_revive;    //当前Trigger.id的开启次数，默认为1，之后每次重复开启+1
        public int milestone_queue; //3日登入签到中，当前签到的天数（1,2,3）
        public bool is_final;       //是否是本轮最后一个签到

        public static void Track(ActivityLike act, int queue, bool isFinal)
        {
            var data = _GetTrackData<threesign_rwd>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.event_revive = act.OpenCount;
            data.milestone_queue = queue;
            data.is_final = isFinal;
            _TrackData(data);
        }
    }

    //三天签到结束时发送
    [Serializable]
    internal class threesign_finish : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int event_revive;    //当前Trigger.id的开启次数，默认为1，之后每次重复开启+1
        public int milestone_queue; //3日登入签到中，当前签到的天数（1,2,3）
        public bool is_break;       //3日登入签到中，是否断签（本次活动结束时是否签满3天）

        public static void Track(ActivityLike act, int queue, bool isBreak)
        {
            var data = _GetTrackData<threesign_finish>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.event_revive = act.OpenCount;
            data.milestone_queue = queue;
            data.is_break = isBreak;
            _TrackData(data);
        }
    }

    #endregion

    #region 订单助力
    internal class event_orderbonus_pick : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public int challenge_num;
        public int pay_difficulty;
        public int order_id;
        public int milestone_id;
        public static void Track(ActivityLike act, int queue, int num, int mileDiff, int round, int challenge, int payDiff, int order, int id)
        {
            var data = _GetTrackData<event_orderbonus_pick>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.milestone_difficulty = mileDiff;
            data.round_num = round;
            data.challenge_num = challenge;
            data.pay_difficulty = payDiff;
            data.order_id = order;
            data.milestone_id = id;
            _TrackData(data);
        }
    }
    internal class event_orderbonus_success : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public int challenge_num;
        public int pay_difficulty;
        public int time;
        public string rewardInfo;
        public int order_id;
        public int milestone_id;
        public static void Track(ActivityLike act, int queue, int num, int mileDiff, int round, int challenge, int payDiff, int time, string rewardinfo, int order, int id)
        {
            var data = _GetTrackData<event_orderbonus_success>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.milestone_difficulty = mileDiff;
            data.round_num = round;
            data.challenge_num = challenge;
            data.pay_difficulty = payDiff;
            data.time = time;
            data.rewardInfo = rewardinfo;
            data.order_id = order;
            data.milestone_id = id;
            _TrackData(data);
        }
    }
    internal class event_orderbonus_fail : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public int challenge_num;
        public int pay_difficulty;
        public int order_id;
        public int milestone_id;
        public static void Track(ActivityLike act, int queue, int num, int mileDiff, int round, int challenge, int payDiff, int order, int id)
        {
            var data = _GetTrackData<event_orderbonus_fail>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.milestone_difficulty = mileDiff;
            data.round_num = round;
            data.challenge_num = challenge;
            data.pay_difficulty = payDiff;
            data.order_id = order;
            data.milestone_id = id;
            _TrackData(data);
        }
    }
    internal class event_orderbonus_trigger : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int pay_difficulty;
        public int round_num;
        public static void Track(ActivityLike act, int queue, int num, int mileDiff, int payDiff, int order)
        {
            var data = _GetTrackData<event_orderbonus_trigger>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.milestone_difficulty = mileDiff;
            data.pay_difficulty = payDiff;
            data.round_num = order;
            _TrackData(data);
        }
    }
    #endregion

    #region 兑换商店
    internal class event_redeem_complete : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_stage; //第几个阶段（从1开始）
        public int milestone_queue; //本阶段第几个里程碑节点（从1开始）
        public int milestone_num;  //本阶段里程碑总节点数
        public int milestone_difficulty; //活动难度
        public bool is_final; //是否最后一个里程碑节点
        public int round_num; //本次活动的第几轮里程碑

        public static void Track(ActivityLike act, int milestoneStage, int milestoneQueue, int milestoneNum, int milestoneDifficulty, bool isFinal, int roundNum)
        {
            var data = _GetTrackData<event_redeem_complete>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_stage = milestoneStage;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = milestoneDifficulty;
            data.is_final = isFinal;
            data.round_num = roundNum;
            _TrackData(data);
        }
    }

    internal class event_redeem_reward : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int reward_from;
        public int reward_queue;
        public int milestone_difficulty;
        public bool is_final; // 是否是本轮最后一个奖励(最后一个点击的商品)
        public int reward_num; //本轮第几次奖励，每轮重置从1开始
        public int round_num;


        public static void Track(ActivityLike act, int rewardFrom, int rewardQueue, int milestoneDifficulty, int roundNum, bool isFinal, int rewardNum)
        {
            var data = _GetTrackData<event_redeem_reward>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.reward_from = rewardFrom;
            data.reward_queue = rewardQueue;
            data.milestone_difficulty = milestoneDifficulty;
            data.is_final = isFinal;
            data.reward_num = rewardNum;
            data.round_num = roundNum;
            _TrackData(data);
        }
    }

    internal class event_fight_attack : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int item_id;
        public int num;
        public int critical_num;
        public int milestone_queue;
        public int milestone_num;
        public int round_num;
        public static void Track(ActivityLike act, int _itemId, int _num, int _critical, int _milestone_queue, int _milestone_num)
        {
            var data = _GetTrackData<event_fight_attack>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.item_id = _itemId;
            data.num = _num;
            data.critical_num = _critical;
            data.milestone_queue = _milestone_queue;
            data.milestone_num = _milestone_num;
            data.round_num = 1;
            _TrackData(data);
        }
    }

    internal class event_fight_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int round_num;
        public bool is_final;
        public int token_use;
        public int reward_queue;
        public int level_num;
        public static void Track(ActivityLike act, int _milestonequeue, int _milesonenum, int _difficulty, int _round, bool isfinal, int token, int reward, int level)
        {
            var data = _GetTrackData<event_fight_milestone>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.milestone_queue = _milestonequeue;
            data.milestone_num = _milesonenum;
            data.milestone_difficulty = _difficulty;
            data.round_num = _round;
            data.is_final = isfinal;
            data.token_use = token;
            data.reward_queue = reward;
            data.level_num = level;
            _TrackData(data);
        }
    }

    internal class event_fight_end_collect : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public string itemsinfo;
        public static void Track(ActivityLike act, string itemsinfo)
        {
            var data = _GetTrackData<event_fight_end_collect>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.itemsinfo = itemsinfo;
            _TrackData(data);
        }
    }

    internal class event_fight_get_item : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int board_id;
        public int item_id;
        public int item_level;
        public int item_num;
        public static void Track(ActivityLike act, int board, int id, int level, int num)
        {
            var data = _GetTrackData<event_fight_get_item>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.board_id = board;
            data.item_id = id;
            data.item_level = level;
            data.item_num = num;
            _TrackData(data);
        }
    }


    #endregion
    #region 倍率排行榜

    internal class event_multiranking_start : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_difficulty;
        public int robot_group;

        public static void Track(ActivityLike activity, int diff, int group)
        {
            var data = _GetTrackData<event_multiranking_start>();
            (data.event_id, data.event_from, data.event_param) = activity.Info3;
            data.milestone_difficulty = diff;
            data.robot_group = group;
            _TrackData(data);
        }
    }

    internal class event_multiranking_multiplier : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_difficulty;
        public int multiplier_num;
        public int multiplier_before;
        public bool is_reset;
        public static void Track(ActivityLike activityLike, int diff, int current, int previous, bool reset)
        {
            var data = _GetTrackData<event_multiranking_multiplier>();
            (data.event_id, data.event_from, data.event_param) = activityLike.Info3;
            data.milestone_difficulty = diff;
            data.multiplier_num = current;
            data.multiplier_before = previous;
            data.is_reset = reset;
            _TrackData(data);
        }
    }

    internal class event_multiranking_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_difficulty;
        public int milestone_queue;
        public int milestone_num;
        public int round_num;
        public bool is_final;

        public static void Track(ActivityLike activityLike, int diff, int queue, int num, bool final)
        {
            var data = _GetTrackData<event_multiranking_milestone>();
            (data.event_id, data.event_from, data.event_param) = activityLike.Info3;
            data.milestone_difficulty = diff;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.round_num = 1;
            data.is_final = final;
            _TrackData(data);
        }
    }

    internal class event_multiranking_complete : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_difficulty;
        public int rank;
        public int score_num;
        public int energy_cost;
        public string bot_score;
        public static void Track(ActivityLike activityLike, int diff, int rank, int score, int cost, string botscore)
        {
            var data = _GetTrackData<event_multiranking_complete>();
            (data.event_id, data.event_from, data.event_param) = activityLike.Info3;
            data.milestone_difficulty = diff;
            data.rank = rank;
            data.score_num = score;
            data.energy_cost = cost;
            data.bot_score = botscore;
            _TrackData(data);
        }

    }
    #endregion

    #region 火车任务
    internal class event_train_choose_group : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int group_id;
        public static void Track(ActivityLike act, int group)
        {
            var data = _GetTrackData<event_train_choose_group>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.group_id = group;
            _TrackData(data);
        }

    }

    internal class event_train_complete_item : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int group_id;
        public int challenge_id;
        public int challenge_queue;
        public int train_id;
        public int item_id;
        public string reward_info;
        public bool is_limit;
        public string limit_reward_info;
        public int limit_item_queue;
        public int round_num;

        public static void Track(ActivityLike act, int group, int challengeID, int challengeQueue, int trainID, int itemID, string rewardInfo, bool isLimit, string limitReward, int limitQueue, int round)
        {
            var data = _GetTrackData<event_train_complete_item>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.group_id = group;
            data.challenge_id = challengeID;
            data.challenge_queue = challengeQueue;
            data.train_id = trainID;
            data.item_id = itemID;
            data.reward_info = rewardInfo;
            data.is_limit = isLimit;
            data.limit_reward_info = limitReward;
            data.limit_item_queue = limitQueue;
            data.round_num = round;
            _TrackData(data);
        }

    }

    internal class event_train_complete_train : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int group_id;
        public int challenge_id;
        public int challenge_queue;
        public int train_id;
        public string reward_info;
        public int train_queue;
        public int round_num;
        public static void Track(ActivityLike act, int group, int challengeID, int challengeQueue, int train, string reward, int trainQueue, int round)
        {
            var data = _GetTrackData<event_train_complete_train>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.group_id = group;
            data.challenge_id = challengeID;
            data.challenge_queue = challengeQueue;
            data.train_id = train;
            data.reward_info = reward;
            data.train_queue = trainQueue;
            data.round_num = round;
            _TrackData(data);
        }

    }

    internal class event_train_complete_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int group_id;
        public int challenge_id;
        public int milestone_id;
        public int milestone_queue;
        public string reward_info;
        public bool is_final;
        public int round_num;
        public static void Track(ActivityLike act, int group, int challengeID, int milestoneID, int milestoneQueue, string reward, bool isFinal, int round)
        {
            var data = _GetTrackData<event_train_complete_milestone>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.group_id = group;
            data.challenge_id = challengeID;
            data.milestone_id = milestoneID;
            data.milestone_queue = milestoneQueue;
            data.reward_info = reward;
            data.is_final = isFinal;
            data.round_num = round;
            _TrackData(data);
        }

    }

    internal class event_train_challenge_complete : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int group_id;
        public int challenge_id;
        public int challenge_queue;
        public bool is_final;
        public int round_num;
        public static void Track(ActivityLike act, int group, int challengeID, int challengeQueue, bool isFinal, int round)
        {
            var data = _GetTrackData<event_train_challenge_complete>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.group_id = group;
            data.challenge_id = challengeID;
            data.challenge_queue = challengeQueue;
            data.is_final = isFinal;
            data.round_num = round;
            _TrackData(data);
        }

    }

    internal class event_train_end : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int group_id;
        public int challenge_id;
        public int challenge_queue;
        public string item_info;
        public int item_diff;
        public int round_num;
        public string reward;
        public static void Track(ActivityLike act, int group, int challengeID, int challengeQueue, string itemInfo, int diff, int round, string reward)
        {
            var data = _GetTrackData<event_train_end>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.group_id = group;
            data.challenge_id = challengeID;
            data.challenge_queue = challengeQueue;
            data.item_info = itemInfo;
            data.item_diff = diff;
            data.round_num = round;
            data.reward = reward;
            _TrackData(data);
        }

    }

    internal class event_train_restart : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int rounud_total;
        public static void Track(ActivityLike act, int round)
        {
            var data = _GetTrackData<event_train_restart>();
            (data.event_id, data.event_from, data.event_param) = act.Info3;
            data.rounud_total = round;
            _TrackData(data);
        }

    }
    #endregion

    #region 许愿棋盘
    /// <summary>
    /// 许愿棋盘完成里程碑时
    /// </summary>
    internal class event_wish_bar : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //这轮的第几个里程碑（从1开始）
        public int milestone_num;   //本轮里程碑个数
        public int milestone_difficulty;   //本轮活动难度（EventMineGroup.diff）
        public bool is_final;   //是否是本轮最后一个里程碑
        public int board_id;    //来源于棋盘（MergeBoard.id）
        public int round_num;   //活动期间的第几轮（固定为1）
        public int board_row;   //当时棋盘深度m

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, bool isFinal, int boardId, int depth)
        {
            var data = _GetTrackData<event_wish_bar>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.is_final = isFinal;
            data.board_id = boardId;
            data.round_num = 1;
            data.board_row = depth;
            _TrackData(data);
        }
    }

    /// <summary>
    /// 许愿棋盘完成棋子图鉴时
    /// </summary>
    internal class event_wish_gallery_milestone : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue; //本轮本棋盘第几个目标棋子（从1开始，换轮次或换棋盘时都重新计数）
        public int milestone_num; //本轮本棋盘目标棋子个数
        public int milestone_difficulty; //本轮活动难度（EventMineGroup.diff）
        public int board_id;    //来源于棋盘（MergeBoard.id）
        public int round_num; //活动期间的第几轮（固定为1）
        public bool is_final; //是否是本轮最后一个图鉴
        public int board_row;   //当时棋盘深度m

        public static void Track(ActivityLike acti_, int index, int milestoneNum, int diff, bool isFinal, int boardId, int depth)
        {
            var data = _GetTrackData<event_wish_gallery_milestone>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = index;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = diff;
            data.board_id = boardId;
            data.round_num = 1;
            data.is_final = isFinal;
            data.board_row = depth;
            _TrackData(data);
        }
    }

    /// <summary>
    /// 许愿棋盘活动结束领取遗留奖励时
    /// </summary>
    internal class event_wish_end_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public string itemsinfo; //回收的棋子信息 id:数量

        public static void Track(ActivityLike acti_, string itemsInfo)
        {
            var data = _GetTrackData<event_wish_end_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.itemsinfo = itemsInfo;
            _TrackData(data);
        }
    }

    /// <summary>
    /// 许愿棋盘1+1礼包领取奖励
    /// </summary>
    internal class wish_endless_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int reward_queue;
        public bool is_free;

        public static void Track(ActivityLike acti_, int rewardQueue, bool isFree)
        {
            var data = _GetTrackData<wish_endless_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.reward_queue = rewardQueue;
            data.is_free = isFree;
            _TrackData(data);
        }
    }

    internal class event_wish_getitem_order : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int board_id;
        public int round_num;
        public int board_row;
        public int item_id;
        public int item_level;
        public int item_num;
        public int pay_difficulty;

        public static void Track(ActivityLike acti_, int milestoneQueue, int milestoneNum, int milestoneDifficulty, int boardId, int roundNum, int boardRow, int itemID, int itemLevel, int itemNum, int payDifficulty)
        {
            var data = _GetTrackData<event_wish_getitem_order>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = milestoneDifficulty;
            data.board_id = boardId;
            data.round_num = roundNum;
            data.board_row = boardRow;
            data.item_id = itemID;
            data.item_level = itemLevel;
            data.item_num = itemNum;
            data.pay_difficulty = payDifficulty;
            _TrackData(data);
        }
    }

    internal class event_wish_getitem_tap : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public int board_id;
        public int round_num;
        public int board_row;
        public int item_id;
        public int item_level;
        public int item_num;

        public static void Track(ActivityLike acti_, int milestoneQueue, int milestoneNum, int milestoneDifficulty, int boardId, int roundNum, int boardRow, int itemID, int itemLevel, int itemNum)
        {
            var data = _GetTrackData<event_wish_getitem_tap>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.milestone_difficulty = milestoneDifficulty;
            data.board_id = boardId;
            data.round_num = roundNum;
            data.board_row = boardRow;
            data.item_id = itemID;
            data.item_level = itemLevel;
            data.item_num = itemNum;
            _TrackData(data);
        }
    }

    #endregion

    #region 抓宝大师

    /// <summary>
    /// 成功指定订单时
    /// </summary>
    internal class event_claworder_pick_success : MergeCommonData
    {
        // 活动ID（EventTime.id）
        public int event_id;
        // 活动来源（0：EventTime，1：EventTrigger）
        public int event_from;
        // 活动参数（EventTime.eventParam）
        public int event_param;
        // 活动期间的第几轮（固定为1）
        public int round_num = 1;
        // 本轮活动难度（EventClawOrderGroup.diff）
        public int milestone_difficulty;
        // 本轮的进度条第几个token里程碑（EventClawOrderGroup.tokenMilestone加工）
        public int milestone_queue;
        // 本轮进度条token里程碑总个数
        public int milestone_num;
        // 累计收集 token数量（EventClawOrderToken.tokenCollectCount）
        public int token_num;
        // 本轮第几个获得抽奖机会（EventClawOrderGroup.drawMilestone加工）
        public int draw_queue;
        // 本轮获得抽奖机会的里程碑总个数
        public int draw_num;
        // 累计获得抽奖次数（EventClawOrderDraw.drawCount）
        public int draw_count;
        // 指定订单目标总付出难度
        public int pay_difficulty;
        // 上次提交订单 至 指定成功时 时间差s（第一次指定为0）
        public int time;
        // 挂在哪个槽位
        public int order_id;

        public static void Track(ActivityLike acti_, int milestoneDifficulty, int milestoneQueue, int milestoneNum, int tokenNum,
            int drawQueue, int drawNum, int drawCount, int payDifficulty, int time, int orderId)
        {
            var data = _GetTrackData<event_claworder_pick_success>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_difficulty = milestoneDifficulty;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.token_num = tokenNum;
            data.draw_queue = drawQueue;
            data.draw_num = drawNum;
            data.draw_count = drawCount;
            data.pay_difficulty = payDifficulty;
            data.time = time;
            data.order_id = orderId;
            _TrackData(data);
        }
    }

    /// <summary>
    /// 提交指定订单时
    /// </summary>
    internal class event_claworder_submit : MergeCommonData
    {
        // 活动ID（EventTime.id）
        public int event_id;
        // 活动来源（0：EventTime，1：EventTrigger）
        public int event_from;
        // 活动参数（EventTime.eventParam）
        public int event_param;
        // 活动期间的第几轮（固定为1）
        public int round_num = 1;
        // 本轮活动难度（EventClawOrderGroup.diff）
        public int milestone_difficulty;
        // 本轮的进度条第几个token里程碑（EventClawOrderGroup.tokenMilestone加工）
        public int milestone_queue;
        // 本轮进度条token里程碑总个数
        public int milestone_num;
        // 累计收集 token数量（EventClawOrderToken.tokenCollectCount）
        public int token_num;
        // 本轮第几个获得抽奖机会（EventClawOrderGroup.drawMilestone加工）
        public int draw_queue;
        // 本轮获得抽奖机会的里程碑总个数
        public int draw_num;
        // 累计获得抽奖次数（EventClawOrderDraw.drawCount）
        public int draw_count;
        // 提交订单目标总付出难度
        public int pay_difficulty;
        // 挂在哪个槽位
        public int order_id;
        // 是否是最后一次获得token
        public bool is_final;
        // 订单指定开始时到提交订单时，时间差s
        public int time;
        // tokenId:数量
        public string reward_map;

        public static void Track(ActivityLike acti_, int milestoneDifficulty, int milestoneQueue, int milestoneNum, int tokenNum,
            int drawQueue, int drawNum, int drawCount, int payDifficulty, int time, int orderId, bool isFinal, string rewardMap)
        {
            var data = _GetTrackData<event_claworder_submit>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_difficulty = milestoneDifficulty;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.token_num = tokenNum;
            data.draw_queue = drawQueue;
            data.draw_num = drawNum;
            data.draw_count = drawCount;
            data.pay_difficulty = payDifficulty;
            data.order_id = orderId;
            data.is_final = isFinal;
            data.time = time;
            data.reward_map = rewardMap;
            _TrackData(data);
        }
    }

    /// <summary>
    /// 消耗抽奖次数, 获得棋子时
    /// </summary>
    internal class event_claworder_drawitem_spawn : MergeCommonData
    {
        // 活动ID（EventTime.id）
        public int event_id;
        // 活动来源（0：EventTime，1：EventTrigger）
        public int event_from;
        // 活动参数（EventTime.eventParam）
        public int event_param;
        // 活动期间的第几轮（固定为1）
        public int round_num = 1;
        // 本轮活动难度（EventClawOrderGroup.diff）
        public int milestone_difficulty;
        // 本轮的进度条第几个token里程碑（EventClawOrderGroup.tokenMilestone）
        public int milestone_queue;
        // 本轮进度条token里程碑总个数
        public int milestone_num;
        // 累计收集 token数量（EventClawOrderToken.tokenCollectCount）
        public int token_num;
        // 本轮第几个获得抽奖机会（EventClawOrderGroup.drawMilestone）
        public int draw_queue;
        // 本轮获得抽奖机会的里程碑总个数
        public int draw_num;
        // 累计获得抽奖次数（EventClawOrderDraw.drawCount）
        public int draw_count;
        // 本轮第几个抽奖区间（EventClawOrderGroup.rewardDiffMilestone）
        public int use_draw_queue;
        // 本轮抽奖区间里程碑总个数
        public int use_draw_num;
        // 累计消耗抽奖次数
        public int use_draw_count;
        // 是否是最后一次抽棋子
        public bool is_final;
        // 选择棋子时 随机的实际难度（EventClawOrderReDiff.rewardDiffRange)
        public string draw_pay_difficulty;
        // 生成的棋子ID（ObjBasic.id）
        public int item_id;
        // 生成的物品的难度
        public int item_diff;
        // 生成的棋子等在合成链内的等级
        public int item_level;
        // 是否无法找到订单，默认区间产出棋子
        public bool is_default;

        public static void Track(ActivityLike acti_, int milestoneDifficulty, int milestoneQueue, int milestoneNum, int tokenNum,
            int drawQueue, int drawNum, int drawCount,
            int useDrawQueue, int useDrawNum, int useDrawCount,
            bool isFinal, string drawPayDifficulty, int itemId, int itemDiff, int itemLevel, bool isDefault)
        {
            var data = _GetTrackData<event_claworder_drawitem_spawn>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.milestone_difficulty = milestoneDifficulty;
            data.milestone_queue = milestoneQueue;
            data.milestone_num = milestoneNum;
            data.token_num = tokenNum;
            data.draw_queue = drawQueue;
            data.draw_num = drawNum;
            data.draw_count = drawCount;
            data.use_draw_queue = useDrawQueue;
            data.use_draw_num = useDrawNum;
            data.use_draw_count = useDrawCount;
            data.is_final = isFinal;
            data.draw_pay_difficulty = drawPayDifficulty;
            data.item_id = itemId;
            data.item_diff = itemDiff;
            data.item_level = itemLevel;
            data.is_default = isDefault;
            _TrackData(data);
        }
    }

    /// <summary>
    /// 活动结束, 存在剩余抽奖次数转化时
    /// </summary>
    internal class event_claworder_end_reward : MergeCommonData
    {
        // 活动ID（EventTime.id）
        public int event_id;
        // 活动来源（0：EventTime，1：EventTrigger）
        public int event_from;
        // 活动模板ID
        public int event_param;
        // 剩余抽奖次数
        public int remain_draw_count;
        // 奖励ID:数量（EventClawOrderExpire.expireItem)
        public string reward_map;

        public static void Track(ActivityLike acti_, int remainDrawCount, string rewardMap)
        {
            var data = _GetTrackData<event_claworder_end_reward>();
            data.event_id = acti_.Id;
            data.event_from = acti_.From;
            data.event_param = acti_.Param;
            data.remain_draw_count = remainDrawCount;
            data.reward_map = rewardMap;
            _TrackData(data);
        }
    }

    #endregion

    #region 补单弹窗

    [Serializable]
    internal class iap_reshipment_popup : MergeCommonData
    {
        public int pkg_id;
        public string product_id;
        public string iap_name;

        public static void Track(int pkgId, string productId, string iapName)
        {
            var data = _GetTrackData<iap_reshipment_popup>();
            data.pkg_id = pkgId;
            data.product_id = productId;
            data.iap_name = iapName;
            _TrackData(data);
        }
    }

    [Serializable]
    internal class iap_reshipment_claim : MergeCommonData
    {
        public int pkg_id;
        public string product_id;
        public string iap_name;

        public static void Track(int pkgId, string productId, string iapName)
        {
            var data = _GetTrackData<iap_reshipment_claim>();
            data.pkg_id = pkgId;
            data.product_id = productId;
            data.iap_name = iapName;
            _TrackData(data);
        }
    }

    #endregion

    #region 社区链接

    [Serializable]
    internal class community_link : MergeCommonData
    {
        public int id; //跳转条目(linkId)
        public int type; //跳转入口类型(1:设置页 2:商店 3:社区弹窗)
        public bool get_reward; //本次跳转是否发放关注奖励

        public static void Track(int id, int type, bool getReward)
        {
            var data = _GetTrackData<community_link>();
            data.id = id;
            data.type = type;
            data.get_reward = getReward;
            _TrackData(data);
        }
    }

    #endregion

    #region 社区礼物外链

    [Serializable]
    internal class gift_link : MergeCommonData
    {
        public string key; //礼包码
        public string local_time; //客户端认为的活动时间
        public bool get_reward; //本次是否成功领取礼物奖励
        public static void Track(string key, bool getReward)
        {
            var data = _GetTrackData<gift_link>();
            data.key = key;
            data.local_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            data.get_reward = getReward;
        }
    }
    #endregion

    #region 钻石二次确认弹窗

    [Serializable]
    internal class gem_tips : MergeCommonData
    {
        public int amount_change;
        public string from;
        public bool is_buy;
        public static void Track(int costAmount, ReasonString reason, bool isBuy)
        {
            var data = _GetTrackData<gem_tips>();
            data.amount_change = costAmount;
            data.from = reason.ToString();
            data.is_buy = isBuy;
            _TrackData(data);
        }
    }
    #endregion

    #region hotfix

    internal class hotfix : DataTrackBase
    {
        // 配置版本发生变动后的配置版本号（没有进行配置变更时，记作“not change”）
        public string version_change_data;
        // 资源版本发生变动后的资源版本号（没有进行资源热更时，记作“not change”）
        public string version_change_res;
        public static void Track(string versionChangeData, string versionChangeRes)
        {
            var data = _GetTrackData<hotfix>();
            data.version_change_data = versionChangeData;
            data.version_change_res = versionChangeRes;
            _TrackData(data);
        }
    }

    internal class hotfix_popup : MergeCommonData
    {
        public static void Track()
        {
            _TrackData(_GetTrackData<hotfix_popup>());
        }
    }

    #endregion

    internal class board_info : DataTrackBase
    {
        public int board_id;
        public string cell_list;
        public static void Track(FAT.Merge.Board board)
        {
            var data = _GetTrackData<board_info>();
            var width = board.size.x;
            var height = board.size.y;
            using var sb = Cysharp.Text.ZString.CreateStringBuilder();
            sb.Append("[");
            for (var j = 0; j < height; ++j)
            {
                for (var i = 0; i < width; ++i)
                {
                    if (i != 0 && j != 0)
                    {
                        sb.Append(",");
                    }
                    var tid = board.GetItemByCoord(i, j)?.tid ?? 0;
                    sb.Append(@$"{{""x"":{i},");
                    sb.Append(@$"""y"":{j},");
                    if (tid > 0)
                        sb.Append(@$"""id"":""{tid}""}}");
                    else
                        sb.Append(@$"""id"":""""}}");
                }
            }
            sb.Append("]");
            data.board_id = board.boardId;
            data.cell_list = sb.ToString();
#if UNITY_EDITOR
            DebugEx.Info($"board_info: {data.board_id} {data.cell_list}");
#endif
            _TrackData(data);
        }
    }

    #region bingo任务
    internal class event_bingotask_complete : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int task_id;
        public int milestone_num;
        public int milestone_difficulty;
        public bool is_bingo;
        public bool is_final;
        public int round_num;
        public static void Track(ActivityLike activity, int queue, int id, int num, int diff, bool bingo, bool final, int round)
        {
            var data = _GetTrackData<event_bingotask_complete>();
            (data.event_id, data.event_from, data.event_param) = activity.Info3;
            data.milestone_queue = queue;
            data.task_id = id;
            data.milestone_num = num;
            data.milestone_difficulty = diff;
            data.is_bingo = bingo;
            data.is_final = final;
            data.round_num = round;
            _TrackData(data);
        }
    }

    internal class event_bingotask_reward : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_num;
        public int milestone_difficulty;
        public bool is_final;
        public int round_num;
        public static void Track(ActivityLike activity, int queue, int num, int diff, bool final, int round)
        {
            var data = _GetTrackData<event_bingotask_reward>();
            (data.event_id, data.event_from, data.event_param) = activity.Info3;
            data.milestone_queue = queue;
            data.milestone_num = num;
            data.milestone_difficulty = diff;
            data.is_final = final;
            data.round_num = round;
            _TrackData(data);
        }
    }

    internal class event_bingotask_bingo : MergeCommonData
    {
        public int event_id;
        public int event_from;
        public int event_param;
        public int milestone_queue;
        public int milestone_difficulty;
        public int bingo_line;
        public bool is_final;
        public int round_num;
        public static void Track(ActivityLike activity, int queue, int diff, int line, bool final, int round)
        {
            var data = _GetTrackData<event_bingotask_bingo>();
            (data.event_id, data.event_from, data.event_param) = activity.Info3;
            data.milestone_queue = queue;
            data.milestone_difficulty = diff;
            data.bingo_line = line;
            data.is_final = final;
            data.round_num = round;
            _TrackData(data);
        }
    }
    #endregion
}
