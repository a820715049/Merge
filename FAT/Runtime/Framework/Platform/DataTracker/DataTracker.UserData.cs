/**
 * @Author: handong.liu
 * @Date: 2021-11-19 17:59:11
 */
using System;
using FAT;
using System.Collections.Generic;
using fat.rawdata;
using FAT.Platform;

public static partial class DataTracker {
    public static UserData data;

    public static UserData TraceUser() {
        data ??= new();
        data.Clear();
        return data;
    }

    public class UserData {
        private readonly EL.SimpleJSON.JSONObject data = new();
        private readonly EL.SimpleJSON.JSONObject add = new();

        public void Clear() {
            data.Clear();
            add.Clear();
        }

        //等级
        public UserData CurrentLevel() {
            data["current_level"] = Game.Manager.mergeLevelMan.level;
            return this;
        }

        public UserData MetaUpdate() {
            data["task_num"] = Game.Manager.mapSceneMan.CostCount;
            data["build_base"] = Game.Manager.mapSceneMan.BuildingIdMax;
            return this;
        }

        //货币变化
        public UserData Coin(CoinType type) {
            long val = Game.Manager.coinMan.GetCoin(type);
            var key = type switch {
                CoinType.Gem => "current_diamond",
                CoinType.MergeCoin => "current_coin",
                CoinType.ToolWood => "current_wood",
                CoinType.ToolStone => "current_stone",
                CoinType.ToolCeramics => "current_ceramic",
                CoinType.ToolTile => "current_tile",
                _ => null,
            };
            if (key != null) data[key] = val;
            return this;
        }

        public UserData Energy() {
            data["current_energy"] = Game.Manager.mergeEnergyMan.EnergyAfterFly;
            return this;
        }

        public UserData TotalOrder(IOrderData order, int v) {
            var key = order.ProviderType switch {
                0 => "total_order_common",
                1 => "total_order_detect",
                2 => "total_order_random",
                _ => null,
            };
            if (key != null) add[key] = v;
            return this;
        }

        public UserData TotalRevenue() {
            if (Game.Manager.iap.Initialized) {
                data["total_revenue"] = Game.Manager.iap.TotalIAPServer;
            }
            return this;
        }

        //广告总观看次数
        public UserData TotalADViewTimes(int count) {
            add["total_ad_viewtimes"] = count;
            return this;
        }
        
        public UserData auto_group()
        {
            var playerGroupMan = Game.Manager.playerGroupMan;
            var groups = Game.Manager.configMan.GetPlayerGroupConfigs();
            foreach (var group in groups)
            {
                var id = group.Id;
                var keyName = playerGroupMan.GetDivideTrackKey(id, out var isFirst);
                var value = playerGroupMan.GetPlayerGroup(id);
                if (string.IsNullOrEmpty(keyName)) continue;
                if (isFirst)
                {
                    //原来的存储形式 在TGA上转换为对象类型 该类型最多存储100个元素
                    data[keyName].Add(id.ToString(), value); 
                }
                else
                {
                    //新的存储形式 会在TGA上转换为对象组类型 该类型最多存储500个对象 方便后续AB分组扩展
                    var abInfo = new EL.SimpleJSON.JSONObject
                    {
                        ["ab_group"] = id,
                        ["ab_value"] = value
                    };
                    data[keyName].Add(abInfo);
                }
            }
            return this;
        }

        public UserData Notification(bool v_) {
            data["state_notification"] = v_;
            return this;
        }

        public UserData Sound(bool m_, bool s_) {
            data["state_music"] = m_;
            data["state_sound"] = s_;
            return this;
        }
        
        public UserData Boost(int v_) {
            data["state_bet"] = v_;
            return this;
        }

        public UserData Dialog(bool v_) {
            data["state_dialog"] = v_;
            return this;
        }

        public UserData Binding(AccountLoginType type_, string id_) {
            var key = type_ switch {
                AccountLoginType.Facebook => "fb_id",
                AccountLoginType.Google => "google_id",
                AccountLoginType.Apple => "ios_id",
                _ => null,
            };
            if (key != null) data[key] = id_;
            return this;
        }

        public void Apply() {
            if (!data.IsEmpty()) {
                PlatformSDK.Instance.TraceUser(data);
            }
            if (!add.IsEmpty()) {
                PlatformSDK.Instance.TraceUserAdd(add);
            }
        }
    }
}