
namespace FAT.RemoteAnalysis
{
    public partial class UserInfo
    {
        public static UserInfo I { get; private set; } = new();
        public static readonly string Token = "NEOR1knNg";

        public static UserInfo Current {
            get {
                var info = I;
                info.account_id = Game.Manager.networkMan.fpId;
                info.event_time = Game.Instance.GetTimestampSeconds();
                info.app_version = Game.Instance.appSettings.version;
                info.current_level = Game.Manager.mergeLevelMan.level;
                info.energy_num = Game.Manager.mergeEnergyMan.Energy;
                info.coin_num = Game.Manager.coinMan.GetCoin(fat.rawdata.CoinType.MergeCoin);
                info.diamond_num = Game.Manager.coinMan.GetCoin(fat.rawdata.CoinType.Gem);
                info.stone_num = Game.Manager.coinMan.GetCoin(fat.rawdata.CoinType.ToolStone);
                info.wood_num = Game.Manager.coinMan.GetCoin(fat.rawdata.CoinType.ToolWood);
                info.tile_num = Game.Manager.coinMan.GetCoin(fat.rawdata.CoinType.ToolTile);
                info.ceramic_num = Game.Manager.coinMan.GetCoin(fat.rawdata.CoinType.ToolCeramics);
                info.board_space = Game.Manager.mainMergeMan.world.activeBoard.emptyGridCount;
                info.token = Token;
                info.login_days = EvaluateEventTrigger.LT(Game.Manager.accountMan.createAt);
                info.liveops_diff = Game.Manager.userGradeMan.GetUserGradeValue(Game.Manager.configMan.globalConfig.OrderApiLiveopsGrade);
                info.is_erg_boost = Merge.Env.Instance.IsInEnergyBoost();
                info.last_wish_producer = Game.Manager.remoteApiMan.last_wish_producer;
                info.today_session_count = Game.Manager.remoteApiMan.today_session_count;
                info.today_order_api_count = Game.Manager.remoteApiMan.today_order_api_count;
                return info;
            }
        }
    }
}