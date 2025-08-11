/*
 * @Author: qun.chao
 * @Date: 2024-06-12 11:17:16
 */
using System.Collections.Generic;

namespace FAT.RemoteAnalysis
{
    public class ModelInfo
    {
        public string model_version;
    }

    public class OrderResponse
    {
        public int code; 
        public string msg;
        public OrderResponseData data;
    }

    public class OrderResponseData
    {
        public string account_id;
        public string data_source_rtype;
        public long event_time;
        public string rec_order;
    }

    public class OrderRequest
    {
        public UserInfo user_info;
        public ModelInfo model_info;
        public LastInfo last_info;
        public RecentOrderDiff last_5_order_diff;
        public NextInfo next_info;
        public PurchaseInfo purchase_info;
    }

    public partial class UserInfo
    {
        public string account_id;
        public long event_time;
        public string app_version;
        public int current_level;
        public int energy_num;
        public int coin_num;
        public int diamond_num;
        public int stone_num;
        public int wood_num;
        public int tile_num;
        public int ceramic_num;
        public int board_space;
        public string token;
        public long login_days;
        public int order_id;    // 当前请求的slotId
        public int liveops_diff;
        public bool is_erg_boost;
        public int last_wish_producer;
        public int today_session_count;
        public int today_order_api_count;
    }

    public class LastInfo
    {
        public int last_order_type;
        public int last_order_id;
        public bool last_is_api_order;
        public OrderInfo last_order_info;
    }

    public class NextInfo
    {
        public OrderInfo order_rec_next;
        public List<OrderInfo> order_cur_set;
        public List<OrderInfo> order_rec_set;
    }

    public class RecentOrderDiff
    {
        public List<int> order_pay_diff_list;
        public List<int> order_act_diff_list;
    }

    public class OrderInfo
    {
        public List<RequireItemInfo> require_info;
        public int total_pay_diff;
        public int total_act_diff;
    }

    public class RequireItemInfo
    {
        public int item_id;
        public int category_id;
        public bool is_auto;
    }
}