/**
 * @Author: yingbo.li
 * @Date: 2023-12-21
 */
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using FAT;
using Merge = FAT.Merge;
using fat.rawdata;
using System.Text;

public static partial class DataTracker
{
    #region 奖励箱

    [Serializable] internal class gift_box_change : MergeCommonData {
        public bool is_in;
        public int item_id;
        public int target_board_id;     //is_in为true时此值有效 代表真正要把奖励发到的目标棋盘id

        public static void Track(int itemId_, bool isIn_, int targetBoardId_ = 0) {
            var data = _GetTrackData<gift_box_change>();
            data.is_in = isIn_;
            data.item_id = itemId_;
            data.target_board_id = targetBoardId_;
            _TrackData(data);
        }
    }

    #endregion 奖励箱

    #region 合成棋盘

    [Serializable]
    private class MergeActionCommonData : MergeCommonData
    {
        public int item_id;
        public int item_index;
        public int item_level;
        public int board_space;
        public void FillMergeActionCommonData(Merge.Item item, int level)
        {
            item_id = item.tid;
            item_index = item.id;
            item_level = level;
            board_space = FAT.BoardViewManager.Instance.board.emptyGridCount;
        }
    }

    [Serializable]
    private class board_spawn : MergeActionCommonData
    {
        // 生成来源的id
        public int producer_id;
        // 是否处于能量道具/灯泡状态
        public bool is_boostitem;
        public board_spawn Fill(Merge.Item item, int item_level, int gen_id, bool is_boostitem)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.producer_id = gen_id;
            this.is_boostitem = is_boostitem;
            return this;
        }
    }
    public static void TrackMergeActionSpawn(Merge.Item item, int item_level, int gen_id, bool is_boostitem)
    {
        _TrackData(_GetTrackData<board_spawn>().Fill(item, item_level, gen_id, is_boostitem));
    }

    [Serializable]
    private class board_spawn_wish : MergeActionCommonData
    {
        // 生成来源的id
        public int producer_id;
        // 是否处于能量道具/灯泡状态
        public bool is_boostitem;
        public board_spawn_wish Fill(Merge.Item item, int item_level, int gen_id, bool is_boostitem)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.producer_id = gen_id;
            this.is_boostitem = is_boostitem;
            return this;
        }
    }
    public static void TrackMergeActionSpawnWish(Merge.Item item, int item_level, int gen_id, bool is_boostitem)
    {
        _TrackData(_GetTrackData<board_spawn_wish>().Fill(item, item_level, gen_id, is_boostitem));
    }

    [Serializable]
    private class board_die : MergeActionCommonData
    {
        public board_die Fill(Merge.Item item, int item_level)
        {
            base.FillMergeActionCommonData(item, item_level);
            return this;
        }
    }
    public static void TrackMergeActionDieInto(Merge.Item item, int item_level)
    {
        _TrackData(_GetTrackData<board_die>().Fill(item, item_level));
    }

    [Serializable]
    private class board_merge : MergeActionCommonData
    {
        public bool is_web;
        public bool is_joker;
        public board_merge Fill(Merge.Item item, int item_level, bool isWeb, bool isJoker)
        {
            base.FillMergeActionCommonData(item, item_level);
            is_web = isWeb;
            is_joker = isJoker;
            return this;
        }
    }
    public static void TrackMergeActionMerge(Merge.Item item, int item_level, bool isWeb)
    {
        _TrackData(_GetTrackData<board_merge>().Fill(item, item_level, isWeb, false));
    }

    [Serializable]
    private class board_clear : MergeActionCommonData
    {
        public string from;

        public board_clear Fill(Merge.Item item, int item_level, string act)
        {
            base.FillMergeActionCommonData(item, item_level);
            from = act;
            return this;
        }
    }
    public static void TrackMergeActionClear(Merge.Item item, int item_level, string act)
    {
        _TrackData(_GetTrackData<board_clear>().Fill(item, item_level, act));
    }

    [Serializable]
    private class board_bonus : MergeActionCommonData
    {
        public board_bonus Fill(Merge.Item item, int item_level)
        {
            base.FillMergeActionCommonData(item, item_level);
            return this;
        }
    }
    public static void TrackMergeActionCollect(Merge.Item item, int item_level)
    {
        _TrackData(_GetTrackData<board_bonus>().Fill(item, item_level));
    }
    
    [Serializable]
    private class board_tapbonus : MergeCommonData
    {
        public int producer_id; //使用前棋子id
        public int item_id;     //使用后棋子id
        public board_tapbonus Fill(int producerId, int itemId)
        {
            producer_id = producerId;
            item_id = itemId;
            return this;
        }
    }
    public static void TrackMergeActionTapBonus(int producerId, int itemId)
    {
        _TrackData(_GetTrackData<board_tapbonus>().Fill(producerId, itemId));
    }

    [Serializable]
    private class board_delete : MergeActionCommonData
    {
        public board_delete Fill(Merge.Item item, int item_level)
        {
            base.FillMergeActionCommonData(item, item_level);
            return this;
        }
    }
    public static void TrackMergeActionSell(Merge.Item item, int item_level)
    {
        _TrackData(_GetTrackData<board_delete>().Fill(item, item_level));
    }

    [Serializable]
    private class board_unlock : MergeActionCommonData
    {
        public string from;

        public board_unlock Fill(Merge.Item item, int item_level, string act)
        {
            base.FillMergeActionCommonData(item, item_level);
            from = act;
            return this;
        }
    }
    public static void TrackMergeActionUnlock(Merge.Item item, int item_level, string act)
    {
        _TrackData(_GetTrackData<board_unlock>().Fill(item, item_level, act));
    }

    [Serializable]
    private class board_bubble : MergeActionCommonData
    {
        public bool is_ad;
        public bool is_free;

        public board_bubble Fill(Merge.Item item, int item_level, bool isAd, bool isFree)
        {
            base.FillMergeActionCommonData(item, item_level);
            is_ad = isAd;
            is_free = isFree;
            return this;
        }
    }
    public static void TrackMergeActionBubble(Merge.Item item, int item_level, bool is_ad, bool is_free)
    {
        _TrackData(_GetTrackData<board_bubble>().Fill(item, item_level, is_ad, is_free));
    }

    // [Serializable]
    // private class board_bubble : MergeActionCommonData
    // {
    //     public bool is_ad;

    //     public board_bubble Fill(Merge.Item item, int item_level, bool isAd)
    //     {
    //         base.FillMergeActionCommonData(item, item_level);
    //         is_ad = isAd;
    //         return this;
    //     }
    // }
    // public static void TrackMergeActionBubble(Merge.Item item, int item_level, bool is_ad)
    // {
    //     _TrackData(_GetTrackData<board_bubble>().Fill(item, item_level, is_ad));
    // }

    [Serializable]
    private class board_tap_cd : MergeActionCommonData
    {
        public int cd_num;

        public board_tap_cd Fill(Merge.Item item, int item_level, int cd)
        {
            base.FillMergeActionCommonData(item, item_level);
            cd_num = cd;
            return this;
        }
    }
    public static void TrackMergeActionTapSourceCD(Merge.Item item, int item_level, int cd)
    {
        _TrackData(_GetTrackData<board_tap_cd>().Fill(item, item_level, cd));
    }

    [Serializable]
    private class board_cd : MergeActionCommonData
    {
        public board_cd Fill(Merge.Item item, int item_level)
        {
            base.FillMergeActionCommonData(item, item_level);
            return this;
        }
    }
    public static void TrackMergeActionSourceCD(Merge.Item item, int item_level)
    {
        _TrackData(_GetTrackData<board_cd>().Fill(item, item_level));
    }

    [Serializable]
    private class board_eat : MergeActionCommonData
    {
        public int eat_id;
        public int eat_left;

        public board_eat Fill(Merge.Item item, int item_level, int eat_id, int need_num_after_feed)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.eat_id = eat_id;
            this.eat_left = need_num_after_feed;
            return this;
        }
    }
    public static void TrackMergeActionEat(Merge.Item item, int item_level, int eat_id, int need_num_after_feed)
    {
        _TrackData(_GetTrackData<board_eat>().Fill(item, item_level, eat_id, need_num_after_feed));
    }

    [Serializable]
    private class board_undo : MergeActionCommonData
    {
        public board_undo Fill(Merge.Item item, int item_level)
        {
            base.FillMergeActionCommonData(item, item_level);
            return this;
        }
    }
    public static void TrackMergeActionUndo(Merge.Item item, int item_level)
    {
        _TrackData(_GetTrackData<board_undo>().Fill(item, item_level));
    }

    [Serializable]
    private class board_remind : MergeCommonData
    {
        public string remind_item;
        public board_remind Fill(Merge.Item itemA, Merge.Item itemB)
        {
            remind_item = $"{itemA.tid}, {itemB.tid}";
            return this;
        }
    }
    public static void TrackMergeActionHint(Merge.Item itemA, Merge.Item itemB)
    {
        _TrackData(_GetTrackData<board_remind>().Fill(itemA, itemB));
    }

    [Serializable]
    private class board_choicebox : MergeCommonData
    {
        public int producer_id;
        public int item_id; // 最终选择的id
        public string option;
        public board_choicebox Fill(int spawner_id, int result_id, List<int> choices)
        {
            producer_id = spawner_id;
            item_id = result_id;
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                for (var i = 0; i < choices.Count; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(",");
                    }
                    sb.Append(choices[i]);
                }
                option = sb.ToString();
            }
            return this;
        }
    }
    public static void TrackMergeActionChoiceBox(int spawner_id, int result_id, List<int> choices)
    {
        _TrackData(_GetTrackData<board_choicebox>().Fill(spawner_id, result_id, choices));
    }

    [Serializable]
    private class skill_time_skip : MergeActionCommonData
    {
        public int cd_item_num;
        public skill_time_skip Fill(Merge.Item item, int item_level, int num)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.cd_item_num = num;
            return this;
        }
    }
    public static void TrackMergeActionSkillTimeSkip(Merge.Item item, int item_level, int cd_item_num)
    {
        _TrackData(_GetTrackData<skill_time_skip>().Fill(item, item_level, cd_item_num));
    }

    [Serializable]
    private class skill_tesla : MergeActionCommonData
    {
        public int cd_item_num;
        public skill_tesla Fill(Merge.Item item, int item_level, int num)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.cd_item_num = num;
            return this;
        }
    }
    public static void TrackMergeActionSkillTesla(Merge.Item item, int item_level, int cd_item_num)
    {
        _TrackData(_GetTrackData<skill_tesla>().Fill(item, item_level, cd_item_num));
    }

    [Serializable]
    private class skill_unlimit_energy : MergeActionCommonData
    {
        public int cd_item_num;
        public skill_unlimit_energy Fill(Merge.Item item, int item_level, int num)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.cd_item_num = num;
            return this;
        }
    }
    public static void TrackMergeActionSkillUnlimitEnergy(Merge.Item item, int item_level, int cd_item_num)
    {
        _TrackData(_GetTrackData<skill_unlimit_energy>().Fill(item, item_level, cd_item_num));
    }

    [Serializable]
    private class skill_tap_nocd : MergeActionCommonData
    {
        public int cd_item_num;
        public int target_id;
        public skill_tap_nocd Fill(Merge.Item item, int item_level, int targetId, int num)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.cd_item_num = num;
            this.target_id = targetId;
            return this;
        }
    }
    public static void TrackMergeActionSkillTapNoCD(Merge.Item item, int item_level, int targetId, int cd_item_num)
    {
        _TrackData(_GetTrackData<skill_tap_nocd>().Fill(item, item_level, targetId, cd_item_num));
    }

    [Serializable]
    private class skill_auto_output : MergeActionCommonData
    {
        public int cd_item_num;
        public int target_id;
        public skill_auto_output Fill(Merge.Item item, int item_level, int targetId, int num)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.cd_item_num = num;
            this.target_id = targetId;
            return this;
        }
    }
    public static void TrackMergeActionSkillAutoOutput(Merge.Item item, int item_level, int targetId, int cd_item_num)
    {
        _TrackData(_GetTrackData<skill_auto_output>().Fill(item, item_level, targetId, cd_item_num));
    }

    [Serializable]
    private class board_split : MergeActionCommonData
    {
        public int skill_item_id;
        public int stack_count;
        public int after_item_id;
        public int target_id;
        public board_split Fill(Merge.Item targetItem, int item_level, int _skill_item_id, int _stack_count, int _after_item_id)
        {
            this.skill_item_id = _skill_item_id;
            this.stack_count = _stack_count;
            this.after_item_id = _after_item_id;
            this.target_id = targetItem.tid;
            base.FillMergeActionCommonData(targetItem, item_level);
            return this;
        }
    }
    public static void TrackMergeActionSkillScissor(Merge.Item targetItem, int item_level, int skill_item_id, int stack_count, int after_item_id)
    {
        _TrackData(_GetTrackData<board_split>().Fill(targetItem, item_level, skill_item_id, stack_count, after_item_id));
    }

    [Serializable]
    private class skill_hourglass : MergeActionCommonData
    {
        public int skill_item_id;
        public int time_before;
        public int time_after;

        public skill_hourglass Fill(Merge.Item item, int item_level, int skill_item_id, int before, int after)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.skill_item_id = skill_item_id;
            this.time_before = before;
            this.time_after = after;
            return this;
        }
    }
    public static void TrackMergeActionSkillHourGlass(Merge.Item item, int item_level, int skill_item_id, int time_before, int time_after)
    {
        _TrackData(_GetTrackData<skill_hourglass>().Fill(item, item_level, skill_item_id, time_before, time_after));
    }

    [Serializable]
    private class skill_lightbulb : MergeActionCommonData
    {
        public int skill_item_id;
        public int stack_count;
        public skill_lightbulb Fill(Merge.Item item, int item_level, int skill_item_id, int stack_count)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.skill_item_id = skill_item_id;
            this.stack_count = stack_count;
            return this;
        }
    }

    [Serializable]
    private class skill_upgrade : MergeActionCommonData
    {
        public int skill_item_id;
        public int stack_count;
        public skill_upgrade Fill(Merge.Item item, int item_level, int skill_id, int count)
        {
            base.FillMergeActionCommonData(item, item_level);
            this.skill_item_id = skill_id;
            this.stack_count = count;
            return this;
        }
    }
    public static void TrackMergeActionSkillUpgrade(Merge.Item item, int item_level, int skill_item_id, int stack_count)
    {
        _TrackData(_GetTrackData<skill_upgrade>().Fill(item, item_level, skill_item_id, stack_count));
        _TrackData(_GetTrackData<board_merge>().Fill(item, item_level, isWeb:false, isJoker:true));
    }

    public static void TrackMergeActionSkillLightbulb(Merge.Item item, int item_level, int skill_item_id, int stack_count)
    {
        _TrackData(_GetTrackData<skill_lightbulb>().Fill(item, item_level, skill_item_id, stack_count));
        _TrackData(_GetTrackData<board_merge>().Fill(item, item_level, isWeb:false, isJoker:true));
    }

    [Serializable]
    private class item_replace : MergeCommonData
    {
        public int item_id;
        public int replace_id;
        public item_replace Fill(int from_id, int to_id)
        {
            item_id = from_id;
            replace_id = to_id;
            return this;
        }
    }
    public static void TrackItemReplace(int from_id, int to_id)
    {
        _TrackData(_GetTrackData<item_replace>().Fill(from_id, to_id));
    }

    [Serializable]
    internal class board_active : MergeCommonData
    {
        public int item_id;
        public int target_id;
        public static void Track(int tid, int target_id = 0)
        {
            var data = _GetTrackData<board_active>();
            data.item_id = tid;
            data.target_id = target_id;
            _TrackData(data);
        }
    }
    
    //TrigAuto棋子被触发消耗时
    [Serializable]
    internal class board_trigauto : MergeCommonData
    {
        public int board_id;    //来源于棋盘（MergeBoard.id）
        public string board_xy;    //棋子：行，列(行：需要结合挖矿棋盘深度结合推断)
        public int item_id; //消耗的棋子 id
        public int item_queue;  //消耗棋子第几次
        
        public static void Track(int boardId, string coord, int itemId, int itemQueue)
        {
            var data = _GetTrackData<board_trigauto>();
            data.board_id = boardId;
            data.board_xy = coord;
            data.item_id = itemId;
            data.item_queue = itemQueue;
            _TrackData(data);
        }
    }

    #endregion 合成棋盘

    #region 订单

    [Serializable]
    internal class board_order : MergeCommonData
    {
        public int item_id;
        public int item_level;
        public int board_space;

        public static void Track(Merge.Item item)
        {
            var data = _GetTrackData<board_order>();
            data.board_space = BoardViewManager.Instance.board.emptyGridCount;
            data.item_id = item.tid;
            data.item_level = Merge.ItemUtility.GetItemLevel(data.item_id);
            _TrackData(data);
        }
    }

    [Serializable] internal class order_start : MergeCommonData {
        public int order_type;
        public int order_id;
        public string order_require;
        public int difficulty;
        public int pay_difficulty;
        public bool is_api_request;
        public long start_timestamp;
        public string diff_type;
        public string model_version;

        public static void Track(IOrderData order_) {
            var data = _GetTrackData<order_start>();
            data.order_type = order_.ProviderType;
            data.order_id = order_.Id;
            data.order_require = Require(order_);
            // OrderUtility.CalOrderDifficulty(order_, out _, out data.difficulty);
            data.pay_difficulty = order_.PayDifficulty;
            data.difficulty = order_.ActDifficulty;
            // data.is_api = order_.IsApiOrder > 0;
            data.is_api_request = order_.ApiStatus != OrderApiStatus.None;
            data.start_timestamp = (order_ as OrderData).Record.CreatedAt;
            data.diff_type = $"{(OrderProviderRandom.CtrlDffyType)order_.DffyStrategy}";
            data.model_version = (order_ as OrderData).Record.ModelVersion;
            _TrackData(data);
        }

        public static string Require(IOrderData order_) {
            if (order_.Requires.Count < 1)
                return string.Empty;
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var builder))
            {
                foreach (var r in order_.Requires) {
                    builder.Append(r.Id).Append(':').Append(r.TargetCount).Append(',');
                }
                return builder.ToString();
            }
        }
    }

    [Serializable] internal class order_end : MergeCommonData {
        public int order_type;
        public int order_id;
        public string order_require;
        public int difficulty;
        public int pay_difficulty;
        public bool is_api;
        public long start_timestamp;
        public string diff_type;
        public string model_version;
        public static void Track(IOrderData order_) {
            var data = _GetTrackData<order_end>();
            data.order_type = order_.ProviderType;
            data.order_id = order_.Id;
            data.order_require = order_start.Require(order_);
            // OrderUtility.CalOrderDifficulty(order_, out _, out data.difficulty);
            data.pay_difficulty = order_.PayDifficulty;
            data.difficulty = order_.ActDifficulty;
            data.is_api = order_.IsApiOrder;
            data.start_timestamp = (order_ as OrderData).Record.CreatedAt;
            data.diff_type = $"{(OrderProviderRandom.CtrlDffyType)order_.DffyStrategy}";
            data.model_version = (order_ as OrderData).Record.ModelVersion;
            _TrackData(data);
            TraceUser().TotalOrder(order_, 1)?.Apply(); ;
        }
    }

    [Serializable] internal class order_show : MergeCommonData {
        public int order_type;
        public int order_id;
        public string order_require;
        public int difficulty;
        public int pay_difficulty;
        public bool is_api;
        public long start_timestamp;
        public string diff_type;
        public string model_version;
        public static void Track(IOrderData order_) {
            var data = _GetTrackData<order_show>();
            data.order_type = order_.ProviderType;
            data.order_id = order_.Id;
            data.order_require = order_start.Require(order_);
            // OrderUtility.CalOrderDifficulty(order_, out _, out data.difficulty);
            data.pay_difficulty = order_.PayDifficulty;
            data.difficulty = order_.ActDifficulty;
            data.is_api = order_.IsApiOrder;
            data.start_timestamp = (order_ as OrderData).Record.CreatedAt;
            data.diff_type = $"{(OrderProviderRandom.CtrlDffyType)order_.DffyStrategy}";
            data.model_version = (order_ as OrderData).Record.ModelVersion;
            _TrackData(data);
            TraceUser().TotalOrder(order_, 1)?.Apply(); ;
        }
    }

    #endregion 订单

    #region 星想事成

    [Serializable]
    internal class wishing_reward : MergeCommonData
    {
        public int event_id;
        public int event_from; //活动来源（0：EventTime，1：EventTrigger）
        public int event_param;
        public int order_id;
        public int item_id;
        public int reward_difficulty;
        public static void Track(int _event_id, int _event_param, int _order_id, int _item_id)
        {
            var data = _GetTrackData<wishing_reward>();
            data.event_id = _event_id;
            data.event_from = 0;
            data.event_param = _event_param;
            data.order_id = _order_id;
            data.item_id = _item_id;
            Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(_item_id, out _, out data.reward_difficulty);
            _TrackData(data);
        }
    }

    #endregion
}