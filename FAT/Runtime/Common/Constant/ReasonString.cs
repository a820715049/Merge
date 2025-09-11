/**
 * @Author: handong.liu
 * @Date: 2020-11-02 16:19:00
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT
{
    public partial class ReasonString
    {
        public string value;

        public ReasonString(string v_) => value = v_;
        public static implicit operator string(ReasonString v_) => v_?.value;
        public void UpdateReason(string v_) => value = v_;
        public override string ToString() => value;

        //resource_change.from + get_item.from
        public static readonly ReasonString init = new("default");
        public static readonly ReasonString purchase = new(nameof(purchase));
        public static readonly ReasonString use_item = new(nameof(use_item));
        public static readonly ReasonString missing_item = new(nameof(missing_item));
        public static readonly ReasonString merge_item = new(nameof(merge_item));
        public static readonly ReasonString trig_auto_source = new(nameof(trig_auto_source)); //trig棋子
        public static readonly ReasonString handbook = new(nameof(handbook));
        public static readonly ReasonString random_chest = new(nameof(random_chest));
        public static readonly ReasonString rand_chest_iap = new(nameof(rand_chest_iap));
        public static readonly ReasonString market_energy = new(nameof(market_energy));
        public static readonly ReasonString market_boost = new(nameof(market_boost));
        public static readonly ReasonString market_item = new(nameof(market_item));
        public static readonly ReasonString skip = new(nameof(skip));
        public static readonly ReasonString bubble = new(nameof(bubble));
        public static readonly ReasonString inventory = new(nameof(inventory));
        public static readonly ReasonString order = new(nameof(order));
        public static readonly ReasonString daily_event = new(nameof(daily_event));
        public static readonly ReasonString meta = new(nameof(meta));
        public static readonly ReasonString reset = new(nameof(reset));
        public static readonly ReasonString recover_online = new(nameof(recover_online));
        public static readonly ReasonString recover_offline = new(nameof(recover_offline));
        public static readonly ReasonString ad = new(nameof(ad));
        public static readonly ReasonString produce = new(nameof(produce));
        public static readonly ReasonString produce_2x = new(nameof(produce_2x));
        public static readonly ReasonString produce_4x = new(nameof(produce_4x));
        public static readonly ReasonString cheat = new(nameof(cheat));
        public static readonly ReasonString free = new(nameof(free));
        public static readonly ReasonString levelup = new(nameof(levelup));
        public static readonly ReasonString mail = new(nameof(mail));
        public static readonly ReasonString initialize = new(nameof(initialize));
        public static readonly ReasonString sell_item = new(nameof(sell_item));
        public static readonly ReasonString undo_sell_item = new(nameof(undo_sell_item));
        public static readonly ReasonString order_box = new(nameof(order_box)); // 订单礼盒
        public static readonly ReasonString tool_exchange = new(nameof(tool_exchange));
        public static readonly ReasonString card = new(nameof(card));       //集卡
        public static readonly ReasonString star_exchange = new(nameof(star_exchange));  //集卡星星兑换
        public static readonly ReasonString expire = new(nameof(expire));   //物品过期
        public static readonly ReasonString score = new(nameof(score));   //积分活动
        public static readonly ReasonString step = new(nameof(step));//阶梯活动
        public static readonly ReasonString login_gift = new ReasonString(nameof(login_gift));
        public static readonly ReasonString treasure = new(nameof(treasure));   //寻宝活动
        public static readonly ReasonString treasure_key_by_open_box = new(nameof(treasure_key_by_open_box));   //开宝箱获得钥匙
        public static readonly ReasonString treasure_milestone = new(nameof(treasure_milestone));   //寻宝活动积分来源：积分奖励（里程碑
        public static readonly ReasonString treasure_chest = new(nameof(treasure_chest));   //寻宝活动积分来源：开宝箱
        public static readonly ReasonString treasure_reward = new(nameof(treasure_reward));   //寻宝活动积分来源：获得奖励
        public static readonly ReasonString total_reward = new(nameof(total_reward));   //UITotalRewardPanel使用
        public static readonly ReasonString decorate_reward = new(nameof(decorate_reward)); //装饰区活动奖励
        public static readonly ReasonString survey = new(nameof(survey));
        public static readonly ReasonString race_reward = new(nameof(race_reward)); //热气球活动奖励
        public static readonly ReasonString miniboard_start = new(nameof(miniboard_start));   //迷你棋盘活动中发放初始棋子时
        public static readonly ReasonString miniboard_getitem = new(nameof(miniboard_getitem));   //从主棋盘产棋子发到迷你棋盘奖励箱时
        public static readonly ReasonString miniboard_multi_start = new(nameof(miniboard_multi_start));   //多轮迷你棋盘活动中发放初始棋子时
        public static readonly ReasonString miniboard_multi_getitem = new(nameof(miniboard_multi_getitem));   //从主棋盘产棋子发到多轮迷你棋盘奖励箱时
        public static readonly ReasonString miniboard_multi_inherititem = new(nameof(miniboard_multi_inherititem));   //多轮迷你棋盘进入下一轮时继承棋盘上棋子
        public static readonly ReasonString miniboard_multi_gift_box_item = new(nameof(miniboard_multi_gift_box_item));   //多轮迷你棋盘进入下一轮时继承奖励箱内的棋子
        public static readonly ReasonString ranking = new(nameof(ranking));
        public static readonly ReasonString invite = new(nameof(invite));
        public static readonly ReasonString stamp = new(nameof(stamp));
        public static readonly ReasonString endless_progress = new(nameof(endless_progress));   //6格无限礼包进度条奖励
        public static readonly ReasonString endless_three_progress = new(nameof(endless_three_progress)); //3格无限礼包进度条奖励
        public static readonly ReasonString mine_start = new(nameof(mine_start)); //挖矿棋盘获得初始代币
        public static readonly ReasonString mine_order_gettoken = new(nameof(mine_order_gettoken)); //从主订单产代币到挖矿活动
        public static readonly ReasonString mine_milestone_reward = new(nameof(mine_milestone_reward)); //挖矿活动中获得里程碑奖励时
        public static readonly ReasonString mine_end_token_energy = new(nameof(mine_end_token_energy)); //挖矿活动结束代币转化体力
        public static readonly ReasonString wish_upon_reward = new(nameof(wish_upon_reward)); //耗体自选活动获得奖励
        

        #region 农场棋盘

        public static readonly ReasonString farm_start = new(nameof(farm_start)); //农场棋盘获得初始代币
        public static readonly ReasonString farm_order = new(nameof(farm_order)); //从主订单产代币到农场棋盘活动
        public static readonly ReasonString farm_merge = new(nameof(farm_merge)); //从主棋盘通过耗体行为产代币到农场棋盘活动
        public static readonly ReasonString farm_use_token = new(nameof(farm_use_token)); //农场棋盘消耗代币
        public static readonly ReasonString farm_end_token_energy = new(nameof(farm_end_token_energy)); //农场棋盘活动结束代币转化体力

        #endregion
        
        #region  矿车棋盘
        
        public static readonly ReasonString mine_cart_start = new(nameof(mine_cart_start)); //矿车棋盘获得初始棋子
        public static readonly ReasonString mine_cart_tap = new(nameof(mine_cart_tap));  //从主棋盘通过点击耗体行为产棋子到矿车棋盘活动
        public static readonly ReasonString mine_cart_order = new(nameof(mine_cart_order));  //从订单产活动棋子到矿车棋盘活动
        public static readonly ReasonString mine_cart_use_item = new(nameof(mine_cart_use_item));  //使用活动棋子获得奖励，因棋盘满飞到奖励箱时
        public static readonly ReasonString mine_cart_milestone_reward = new(nameof(mine_cart_milestone_reward));  //矿车棋盘里程碑阶段奖励，因棋盘满飞到奖励箱时
        public static readonly ReasonString mine_cart_round_reward = new(nameof(mine_cart_round_reward));  //矿车棋盘回合大奖

        #endregion

        #region BP-通行证

        public static readonly ReasonString bp_task = new(nameof(bp_task)); //bp任务奖励
        public static readonly ReasonString bp_milestone = new(nameof(bp_milestone)); //bp手动领取里程碑免费奖励
        public static readonly ReasonString bp_milestone_purchase = new(nameof(bp_milestone_purchase)); //bp手动领取里程碑付费奖励
        public static readonly ReasonString bp_cycle_reward = new(nameof(bp_cycle_reward)); //bp循环奖励(付费)
        public static readonly ReasonString bp_lastpurchase = new(nameof(bp_lastpurchase)); //bp结束补买奖励（花钱：包含里程碑+循环奖励）
        public static readonly ReasonString bp_end = new(nameof(bp_end)); //bp结束自动领取免费奖励
        public static readonly ReasonString bp_end_purchase = new(nameof(bp_end_purchase)); //bp结束自动领取付费奖励
        #endregion

        #region 沙堡里程碑
        public static readonly ReasonString castle_convert = new(nameof(castle_convert));
        public static readonly ReasonString castle_milestone = new(nameof(castle_milestone));
        #endregion

        #region 挖沙
        public static readonly ReasonString digging_start = new(nameof(digging_start));// 挖沙活动参与时
        public static readonly ReasonString digging_end = new(nameof(digging_end));// 挖沙活动结束时
        public static readonly ReasonString digging_level_complete = new(nameof(digging_level_complete));//挖沙关卡完成时
        public static readonly ReasonString digging_level_reward = new(nameof(digging_level_reward));//挖沙关卡奖励领取时
        public static readonly ReasonString digging_restart = new(nameof(digging_restart));//挖沙活动重开时
        public static readonly ReasonString digging_level = new(nameof(digging_level));
        public static readonly ReasonString digging_milestone = new(nameof(digging_milestone));
        public static readonly ReasonString digging_random = new(nameof(digging_random));
        #endregion
        #region 连续限时订单活动
        public static readonly ReasonString order_challenge = new(nameof(order_challenge));

        #endregion
        public static readonly ReasonString guide_energy4x = new(nameof(guide_energy4x));

        #region 弹珠掉落

        public static readonly ReasonString pachinko_end = new(nameof(pachinko_end));
        public static readonly ReasonString pachinko_energy = new(nameof(pachinko_energy));
        public static readonly ReasonString pachinko_bumper = new(nameof(pachinko_bumper));
        public static readonly ReasonString pachinko_milestone = new(nameof(pachinko_milestone));
        public static readonly ReasonString pachinko_use = new(nameof(pachinko_use));


        #region 连续限时订单活动
        public static readonly ReasonString order_streak = new(nameof(order_streak));
        #endregion

        #region 拼图活动
        public static readonly ReasonString puzzle_end = new(nameof(puzzle_end));
        public static readonly ReasonString puzzle_token = new(nameof(puzzle_token));
        public static readonly ReasonString puzzle_milestone = new(nameof(puzzle_milestone));
        #endregion

        #endregion

        #region Bingo
        public static readonly ReasonString bingo_reward = new(nameof(bingo_reward));
        #endregion

        #region OrderLike
        public static readonly ReasonString order_like = new(nameof(order_like));
        #endregion
        #region OrderRate
        public static readonly ReasonString order_rate = new(nameof(order_rate));
        #endregion
        #region ClawOrder
        public static readonly ReasonString order_claw = new(nameof(order_claw));
        #endregion

        #region 钓鱼棋盘
        public static readonly ReasonString fish_getitem = new(nameof(fish_getitem));
        public static readonly ReasonString fish_start = new(nameof(fish_start));
        public static readonly ReasonString fish_milestone = new(nameof(fish_milestone));
        public static readonly ReasonString fish_convert = new(nameof(fish_convert));
        #endregion

        #region 签到抽奖
        // TODO
        public static readonly ReasonString weekly_raffle_draw = new(nameof(weekly_raffle_draw));   // 签到抽奖 奖励
        public static readonly ReasonString weekly_raffle_end_token_energy = new(nameof(weekly_raffle_end_token_energy)); // 签到抽奖活动结束代币转化体力
        public static readonly ReasonString weekly_raffle_reward = new(nameof(weekly_raffle_reward)); // 签到抽奖 钻石买token
        #endregion

        #region 订单助力
        public static readonly ReasonString order_bonus = new(nameof(order_bonus));
        #endregion

        #region 每周任务
        public static readonly ReasonString weekly_task = new(nameof(weekly_task));
        #endregion

        #region 打怪棋盘
        public static readonly ReasonString fight_getitem = new(nameof(fight_getitem));
        public static readonly ReasonString fight_attack = new(nameof(fight_attack));
        public static readonly ReasonString fight_milestone = new(nameof(fight_milestone));
        #endregion

        #region CDKey / 礼品码
        public static readonly ReasonString gift_code = new(nameof(gift_code));
        #endregion

        //legacy?
        public static readonly ReasonString unfrozen = new(nameof(unfrozen));
        public static readonly ReasonString SignIn = new(nameof(SignIn));
        //三日签到
        public static readonly ReasonString three_sign = new(nameof(three_sign));

        #region 积分活动变种(麦克风版)

        public static readonly ReasonString score_mic = new(nameof(score_mic));

        #endregion

        #region 兑换商店
        public static readonly ReasonString redeem_Coin_change = new(nameof(redeem_Coin_change));  //兑换币变化
        public static readonly ReasonString redeem_token_energy = new(nameof(redeem_token_energy));  //兑换商店活动结束转化体力

        public static readonly ReasonString redeem_reward = new(nameof(redeem_reward));  //兑换商店兑换物品

        #endregion

        #region  许愿棋盘
        public static readonly ReasonString wish_tap = new(nameof(wish_tap));
        public static readonly ReasonString wish_order = new(nameof(wish_order));
        public static readonly ReasonString wish_start = new(nameof(wish_start));
        public static readonly ReasonString wish_bar_reward = new(nameof(wish_bar_reward));
        #endregion

        #region 跑马灯礼包
        public static readonly ReasonString spin_pack = new(nameof(spin_pack));  //兑换商店兑换物品
        #endregion

        #region 社区计划
        public static readonly ReasonString community_reward = new(nameof(community_reward));  //社群关注奖励
        public static readonly ReasonString giftLink_reward = new(nameof(giftLink_reward));  //礼物链接奖励
        #endregion

        #region bingoTask
        public static readonly ReasonString bingo_task_milestone = new(nameof(bingo_task_milestone));
        #endregion

        #region 每日任务路径主题
        public static readonly ReasonString landmark_reward = new(nameof(landmark_reward));
        #endregion
        
        #region 倍率排行榜
        public static readonly ReasonString multi_ranking_token = new(nameof(multi_ranking_token));
        #endregion
    }
}
