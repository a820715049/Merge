/*
 * @Author: tang.yan
 * @Description: 集卡活动打点相关 
 * @Date: 2024-02-18 14:02:07
 */
using System;
using FAT;

public static partial class DataTracker
{
	[Serializable]
	internal class CardTrackBase : MergeCommonData
	{
		public int event_id;    //活动ID（EventTime.id）
		public int event_from;  //活动ID 活动来源（0：EventTime，1：EventTrigger）
        public int event_param; //活动模板ID（EventCardAlbum.id）
		public int temp_id;     //最终集卡活动坑深ID（EventCardAlbum.tempId）
		public int start_total_revenue;     //活动开始时的累计充值金额
		public CardTrackBase CardTrackBaseFill(int eventId, int eventFrom, int eventParam, int tempId, int startTotalIap)
		{
			event_id = eventId;
            event_from = eventFrom;
			event_param = eventParam;
			temp_id = tempId;
            start_total_revenue = startTotalIap;
			return this;
		}
	}
	
    //集卡活动参与时
    [Serializable]
    private class card_album_start : CardTrackBase
    { 
	    public card_album_start Fill(int eventId, int eventFrom, int eventParam, int tempId, int startTotalIap)
        {
	        CardTrackBaseFill(eventId, eventFrom, eventParam, tempId, startTotalIap);
            return this;
        }
    }
    public static void TrackCardAlbumStart(int eventId, int eventFrom, int eventParam, int tempId, int startTotalIap)
    {
        _TrackData(_GetTrackData<card_album_start>().Fill(eventId, eventFrom, eventParam, tempId, startTotalIap));
    }
    
    //集卡活动结束时
    [Serializable]
    private class card_album_end : CardTrackBase
    {
	    public int card_num;	//活动结束时获得卡片数量
	    public int group_num;	//活动结束时完成卡组数量
        public int round_total; //活动结束时累计完成了几轮（从0开始）
	    public card_album_end Fill(int eventId, int eventFrom, int eventParam, int tempId, int cardNum, int groupNum, int startTotalIap, int roundTotal)
	    {
		    CardTrackBaseFill(eventId, eventFrom, eventParam, tempId, startTotalIap);
		    card_num = cardNum;
		    group_num = groupNum;
            round_total = roundTotal;
		    return this;
	    }
    }
    public static void TrackCardAlbumEnd(int eventId, int eventFrom, int eventParam, int tempId, int cardNum, int groupNum, int startTotalIap, int roundTotal)
    {
	    _TrackData(_GetTrackData<card_album_end>().Fill(eventId, eventFrom, eventParam, tempId, cardNum, groupNum, startTotalIap, roundTotal));
    }
    
    //卡册奖励领取时
    [Serializable]
    private class card_album_complete : CardTrackBase
    {
        public int round_num;   //目前处于活动期间的第几轮（从1开始）
	    public card_album_complete Fill(int eventId, int eventFrom, int eventParam, int tempId, int startTotalIap, int roundNum)
	    {
		    CardTrackBaseFill(eventId, eventFrom, eventParam, tempId, startTotalIap);
            round_num = roundNum;
		    return this;
	    }
    }
    public static void TrackCardAlbumComplete(int eventId, int eventFrom, int eventParam, int tempId, int startTotalIap, int roundNum)
    {
	    _TrackData(_GetTrackData<card_album_complete>().Fill(eventId, eventFrom, eventParam, tempId, startTotalIap, roundNum));
    }
    
    //卡组奖励领取时
    [Serializable]
    private class card_group_complete : CardTrackBase
    {
	    public int group_id;    //卡组ID（CardGroup.id）
        public int round_num;   //目前处于活动期间的第几轮（从1开始）
        public int milestone_queue; //本卡组位于该卡册的序数（从1开始，新一轮时需要重新计数）
        public int milestone_num;   //本轮集卡卡组总数
        public int milestone_difficulty;    //该里程碑认定难度（固定为1）
        
        public card_group_complete Fill(int eventId, int eventFrom, int eventParam, int tempId, int groupId, int startTotalIap, int roundNum, int groupIndex, int totalGroupNum)
	    {
		    CardTrackBaseFill(eventId, eventFrom, eventParam, tempId, startTotalIap);
		    group_id = groupId;
            round_num = roundNum;
            milestone_queue = groupIndex;
            milestone_num = totalGroupNum;
            milestone_difficulty = 1;
            return this;
	    }
    }
    public static void TrackCardGroupComplete(int eventId, int eventFrom, int eventParam, int tempId, int groupId, int startTotalIap, int roundNum, int groupIndex, int totalGroupNum)
    {
	    _TrackData(_GetTrackData<card_group_complete>().Fill(eventId, eventFrom, eventParam, tempId, groupId, startTotalIap, roundNum, groupIndex, totalGroupNum));
    }
    
    //拆开卡包时
    [Serializable]
    private class card_pack_open : MergeCommonData
    {
	    public int item_id;			//拆开的卡包ID（ObjBasic.id）
	    public bool is_include_new;	//是否含有新卡（新卡数量>=1即可）
        public string open_result;  //本次开卡的卡片ID（ObjBasic.id）
	    public bool is_expire;		//是否是非集卡活动期间拆开卡包
        public int round_num;   //目前处于活动期间的第几轮（从1开始）
	    public card_pack_open Fill(int itemId, bool isIncludeNew, bool isExpire, int roundNum, string resultStr)
	    {
		    item_id = itemId;
		    is_include_new = isIncludeNew;
            open_result = resultStr;
		    is_expire = isExpire;
            round_num = roundNum;
		    return this;
	    }
    }
    
    public static void TrackCardPackOpen(int itemId, bool isIncludeNew, bool isExpire, int roundNum, string resultStr)
    {
	    _TrackData(_GetTrackData<card_pack_open>().Fill(itemId, isIncludeNew, isExpire, roundNum, resultStr));
    }
    
    //获得卡片时
    [Serializable]
    private class card_change : MergeCommonData
    {
	    public int item_id;		//获得的卡片ID（ObjBasic.id）
        public int own_num;     //卡片数量变化后的持有所有卡片数量
	    public bool is_new;		//是否是新卡
	    public int star;		//卡片星级
	    public bool is_gold;	//是否是金卡
	    public int joker_type;	//王牌卡类型（0:非joker/1:白joker/2:闪joker）
        public int round_num;   //目前处于活动期间的第几轮（从1开始）
        public bool is_friend_give; //是否是好友赠送
	    public card_change Fill(int itemId, int ownNum, bool isNew, int starNum, bool isGold, int jokerType, int roundNum, bool isFriendGive)
	    {
		    item_id = itemId;
            own_num = ownNum;
		    is_new = isNew;
		    star = starNum;
		    is_gold = isGold;
            joker_type = jokerType;
            round_num = roundNum;
            is_friend_give = isFriendGive;
		    return this;
	    }
    }
    
    public static void TrackCardChange(int itemId, int ownNum, bool isNew, int starNum, bool isGold, int jokerType, int roundNum, bool isFriendGive)
    {
	    _TrackData(_GetTrackData<card_change>().Fill(itemId, ownNum, isNew, starNum, isGold, jokerType, roundNum, isFriendGive));
    }
    
    //集卡活动重开时
    [Serializable]
    private class card_album_restart : CardTrackBase
    {
        public int round_num;   //目前处于活动期间的第几轮（从1开始）
        public card_album_restart Fill(int eventId, int eventFrom, int eventParam, int tempId, int startTotalIap, int roundNum)
        {
            CardTrackBaseFill(eventId, eventFrom, eventParam, tempId, startTotalIap);
            round_num = roundNum;
            return this;
        }
    }
    public static void TrackCardAlbumRestart(int eventId, int eventFrom, int eventParam, int tempId, int startTotalIap, int roundNum)
    {
        _TrackData(_GetTrackData<card_album_restart>().Fill(eventId, eventFrom, eventParam, tempId, startTotalIap, roundNum));
    }
    
    //卡片数量减少时
    [Serializable]
    private class card_reduction : MergeCommonData
    {
        public int event_id;    //活动ID（EventTime.id）
        public int event_from;  //活动ID 活动来源（0：EventTime，1：EventTrigger）
        public int event_param; //活动模板ID（EventCardAlbum.id）
        public int round_num;   //目前处于活动期间的第几轮（从1开始）
        public int item_id;		//减少的卡片ID（ObjBasic.id）
        public int star;		//卡片星级
        public bool is_gold;	//是否是金卡
        public int own_num;         //卡片数量变化后的持有所有卡片数量
        public int reduction_num;   //减少的数量
        public int reduction_type;  //属于哪种减少方式（1.新一轮转化为星星  2.系统兑换 3.赠送好友）
        public int exchange_id;     //兑换条目ID-如果是系统兑换（StarReward.id）
        public card_reduction Fill(int eventId, int eventFrom, int eventParam, int roundNum, int itemId, int starNum, bool isGold, int ownNum, 
            int reductionNum, int reductionType, int exchangeId)
        {
            event_id = eventId;
            event_from = eventFrom;
            event_param = eventParam;
            round_num = roundNum;
            item_id = itemId;
            star = starNum;
            is_gold = isGold;
            own_num = ownNum;
            reduction_num = reductionNum;
            reduction_type = reductionType;
            exchange_id = exchangeId;
            return this;
        }
    }
    
    public static void TrackCardReduction(int eventId, int eventFrom, int eventParam, int roundNum, int itemId, int starNum, bool isGold, int ownNum, 
        int reductionNum, int reductionType, int exchangeId)
    {
        _TrackData(_GetTrackData<card_reduction>().Fill(eventId, eventFrom, eventParam, roundNum, itemId, starNum, isGold, ownNum, 
            reductionNum, reductionType, exchangeId));
    }
    
    //消耗星星兑换奖励时
    [Serializable]
    private class star_exchange : MergeCommonData
    {
        public int event_id;    //活动ID（EventTime.id）
        public int event_from;  //活动ID 活动来源（0：EventTime，1：EventTrigger）
        public int event_param; //活动模板ID（EventCardAlbum.id）
        public int exchange_id; //兑换条目ID（StarReward.id）
        public int cost_fixed_num;   //消耗星星固定库存的数量
        public int total_cost_star_num; //本期活动中累计兑换消耗的星星数量（计算StarExchange.costStar的总和）
        public int left_star_num;       //本次兑换结余的星星数量
        public star_exchange Fill(int eventId, int eventFrom, int eventParam, int exchangeId, int costFixedNum, int totalCostStarNum, int leftStarNum)
        {
            event_id = eventId;
            event_from = eventFrom;
            event_param = eventParam;
            exchange_id = exchangeId;
            cost_fixed_num = costFixedNum;
            total_cost_star_num = totalCostStarNum;
            left_star_num = leftStarNum;
            return this;
        }
    }
    
    public static void TrackCardStarExchange(int eventId, int eventFrom, int eventParam, int exchangeId, int costFixedNum, int totalCostStarNum, int leftStarNum)
    {
        _TrackData(_GetTrackData<star_exchange>().Fill(eventId, eventFrom, eventParam, exchangeId, costFixedNum, totalCostStarNum, leftStarNum));
    }
    
    //打开卡册概览界面时
    [Serializable]
    private class open_card_overview : MergeCommonData
    {
        public int event_id;    //活动ID（EventTime.id）
        public int event_from;  //活动ID 活动来源（0：EventTime，1：EventTrigger）
        public int event_param; //活动模板ID（EventCardAlbum.id）
        public int round_num;   //目前处于活动期间的第几轮（从1开始）
        public open_card_overview Fill(int eventId, int eventFrom, int eventParam, int roundNum)
        {
            event_id = eventId;
            event_from = eventFrom;
            event_param = eventParam;
            round_num = roundNum;
            return this;
        }
    }
    
    public static void TrackOpenCardOverview(int eventId, int eventFrom, int eventParam, int roundNum)
    {
        _TrackData(_GetTrackData<open_card_overview>().Fill(eventId, eventFrom, eventParam, roundNum));
    }
    
    //跳转到换卡社群时
    [Serializable]
    private class jump_trading_group : MergeCommonData
    {
        public int event_id;        //活动ID（EventTime.id）
        public int event_from;      //活动ID 活动来源（0：EventTime，1：EventTrigger）
        public int event_param;     //活动模板ID（EventCardAlbum.id）
        public int round_num;       //目前处于活动期间的第几轮（从1开始）
        public int button_type;     //点击哪个按钮前往的换卡社群（1.卡片详情 2.概览界面）
        public jump_trading_group Fill(int eventId, int eventFrom, int eventParam, int roundNum, int buttonType)
        {
            event_id = eventId;
            event_from = eventFrom;
            event_param = eventParam;
            round_num = roundNum;
            button_type = buttonType;
            return this;
        }
    }
    
    public static void TrackJumpTradingGroup(int eventId, int eventFrom, int eventParam, int roundNum, int buttonType)
    {
        _TrackData(_GetTrackData<jump_trading_group>().Fill(eventId, eventFrom, eventParam, roundNum, buttonType));
    }
    
}