/**
 * @Author: handong.liu
 * @Date: 2021-05-25 11:28:51
 */
#if UNITY_EDITOR
#define ENABLE_FAKE_SERVER
//#define TEST_ENDLESS_RANK
//#define TEST_ENERGY_ACTIVITY
//#define TEST_BATTLE_PASS
//#define TEST_GANENJIE_ACT
//#define TEST_ARCHIVE
//#define TEST_MONTHLY_CARD
// #define TEST_DELIVERY
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;

public class NetworkManTest
{
    public static GameNet.IProtobufCodec codec => sCodec;
    private static Dictionary<System.Type, System.Func<IMessage, IMessage>> sFakeHandler = new Dictionary<System.Type, System.Func<IMessage, IMessage>>();
    private static GameNet.IProtobufCodec sCodec = new GameNet.ProtobufCodec();

#if ENABLE_FAKE_SERVER
    #if TEST_MONTHLY_CARD
    static NetworkManTest()
    {
        ulong uid = 0;
        SetFakeResponseHandler<GameNet.GetActivitiesReq>((req) => {
            var instanceId = 3003;
            var ret = new GameNet.GetActivitiesResp();
            ret.Activities.Add(new GameNet.Activity() {
                InstanceId = instanceId,
                ActId = 3,
                ShowOffset = 60,
                StartTime = Game.Instance.GetTimestampSeconds() - 30,
                EndTime = Game.Instance.GetTimestampSeconds() + 86400,
                DisappearOffset = 180,
                Name = "测试月卡活动",
                TypeId = Config.ActivityType.MonthlyCard,
                SeasonConf = null,
                Desc = "测试月卡活动，本地发送非服务器"
            });
            return ret;
        });
    }
    #endif
    #if TEST_ARCHIVE
    static NetworkManTest()
    {
        ulong uid = 0;
        SetFakeResponseHandler<GameNet.StoreReq>((req) => {
            var bytesstring = codec.MarshalToByteString(req.User);
            PlayerPrefs.SetString(string.Format("User{0}", uid), bytesstring.ToBase64());
            return new GameNet.StoreResp();
        });

        SetFakeResponseHandler<GameNet.LoginReq>((req) => {
            var str = PlayerPrefs.GetString(string.Format("User{0}", req.User.Uid), "");
            GameNet.User user = null;
            if(!string.IsNullOrEmpty(str))
            {
                var bytestring = ByteString.FromBase64(str);
                user = codec.UnmarshalFromByteString<GameNet.User>(bytestring);
            }
            if(user == null)
            {
                user = new GameNet.User(){Uid = req.User.Uid};
            }
            uid = req.User.Uid;
            return new GameNet.LoginResp() {
                User = user,
                UserType = Gamekitdata.UserType.Test,
                NewUser = !Game.Instance.archiveMan.isArchiveLoaded,
                ServerSec = Game.Instance.GetTimestampSeconds()
            };
        });
    }
    #endif
    #if TEST_ENERGY_ACTIVITY
    static NetworkManTest()
    {
        bool presented = false;
        long fetchTime = 0;
        int score = 0;
        long coolDownTs = 0;
        SetFakeResponseHandler<GameNet.GetActivitiesReq>((req) => {
            var ret = new GameNet.GetActivitiesResp() {

            };
            if(fetchTime > 0 && fetchTime < Game.Instance.GetTimestampSeconds())
            {
                ret.Activities.Add(new GameNet.Activity() {
                InstanceId = 1,
                ShowTime = Game.Instance.GetTimestampSeconds(),
                RewardTime = Game.Instance.GetTimestampSeconds() + 90,
                    TypeId = RandomGroupActivity.kTypeId,
                    
                });
            }
            else if(fetchTime == 0)
            {
                fetchTime = Game.Instance.GetTimestampSeconds() + 10;
                Game.Instance.StartCoroutineGlobal(_CoWaitAndRun(11, ()=>Game.Instance.activityMan.TryRefetchServerActivity()));
            }
            return ret;
        });
        SetFakeResponseHandler<GameNet.RandomGroupActivityRewardReq>((req) => {
            if(coolDownTs > 0)
            {
                return new netutils.ErrorResponse(){Code = 1, Message = "error duplicate request"};
            }
            coolDownTs = Game.Instance.GetTimestampSeconds() + 180;
            var ret = new GameNet.RandomGroupActivityRewardResp();
            ret.CooldownEndTs = coolDownTs;
            var act = (Game.Instance.activityMan.GetDefaultActivity() as RandomGroupActivity);
            var id = act.GetRewardByRank(act.myRank);
            var config = Game.Instance.objectMan.GetPackConfig(id);
            if(config != null)
            {
                for(int i = 0; i < config.Items.Count; i++)
                {
                    ret.Rewards.Add(new GameNet.Reward(){Id = config.Items[i], Count = config.Counts[i]});
                }
            }
            return ret;
        });
        SetFakeResponseHandler<GameNet.RandomGroupActivityShowReq>((req) => {
            presented = true;
            return new GameNet.RandomGroupActivityShowResp();
        });
        SetFakeResponseHandler<GameNet.RandomGroupActivitySaveCostReq>((req) => {
            score += req.Energy * 3;
            presented = true;
            return new GameNet.RandomGroupActivityShowResp();
        });
        SetFakeResponseHandler<GameNet.RandomGroupActivityReq>((req) => {
            var ret = new GameNet.RandomGroupActivityResp();
            ret.TemplateId = 1;
            ret.Show = presented;
            ret.CooldownEndTs = coolDownTs;
            ret.Rewards.Add(13000006);
            ret.Rewards.Add(13000007);
            ret.Rewards.Add(13000008);
            ret.Members.Add(new GameNet.RandomGroupActivityMember() {
                Uid = Game.Instance.socialMan.myId,
                IsRobot = false,
                Name = Game.Instance.socialMan.GetName(),
                Score = score,
                EnterGroupTs = Game.Instance.GetTimestampSeconds()
            });
            for(int i = 0; i < 5; i++)
            {
                ret.Members.Add(new GameNet.RandomGroupActivityMember() {
                    Uid = (ulong)(i+1),
                    IsRobot = true,
                    Name = "client_test_" + i,
                    Score = i * 20 + 10,
                    EnterGroupTs = Game.Instance.GetTimestampSeconds()
                });
            }
            return ret;
        });
    }
    #elif TEST_BATTLE_PASS
    private class BPData
    {
        public int start = 100;
        public int reward = 120;
        public int hide = 500;
        public int score = 0;
    }
    static NetworkManTest()
    {
        bool presented = false;
        long fetchTime = 0;
        Dictionary<ulong, BPData> datas =  new Dictionary<ulong, BPData>() {
            [213] = new BPData() { score = 0, start = 10, reward = 30, hide = 100},
            [223] = new BPData() { score = 0, start = 60, reward = 100, hide = 1600},
        };
        long coolDownTs = 0;
        SetFakeResponseHandler<GameNet.GetActivitiesReq>((req) => {
            var ret = new GameNet.GetActivitiesResp() {

            };
            foreach(var data in datas)
            {
                ret.Activities.Add(new GameNet.Activity() {
                InstanceId = data.Key,
                ShowTime = Game.Instance.GetTimestampSeconds() + data.Value.start,
                EndTime = Game.Instance.GetTimestampSeconds() + data.Value.hide,
                RewardTime = Game.Instance.GetTimestampSeconds() + data.Value.reward,
                    TypeId = BattlePassActivity.kTypeId,
                    
                });
            }
            return ret;
        });
        SetFakeResponseHandler<GameNet.BattlePassActivityConfigReq>((req) => {
            var ret = new GameNet.BattlePassActivityConfigResp() {
                InstanceId = req.InstanceId,
                ActId = 1,
                VipPrice = 100,
                VipCoinType = Config.CoinType.Gem
            };
            ret.VipGoods.Add(new GameNet.Reward() {
                Id = 15000001,
                Count = 1
            });
            ret.VipGoods.Add(new GameNet.Reward() {
                Id = 15000003,
                Count = 1
            });
            ret.VipGoods.Add(new GameNet.Reward() {
                Id = 1,
                Count = 500
            });
            return ret;
        });
    }
    #elif TEST_GANENJIE_ACT
    static NetworkManTest()
    {
        bool presented = false;
        long fetchTime = 0;
        int score = 0;
        long coolDownTs = 0;
        SetFakeResponseHandler<GameNet.GetActivitiesReq>((req) => {
            var ret = new GameNet.GetActivitiesResp() {

            };
            if(fetchTime > 0 && fetchTime < Game.Instance.GetTimestampSeconds())
            {
                var config = new Config.OrderPointActInst() {
                    ActId = 1,
                    OrderCount = 2,
                    BoardId = 5,
                    StoreId = 1,
                    ActName = "感恩节活动",
                    ActIcon = "ui_common:tx_empty_role.png"
                };
                config.OrderIds.Add(1);
                config.OrderIds.Add(2);
                config.OrderIds.Add(3);
                ret.Activities.Add(new GameNet.Activity() {
                    InstanceId = 1,
                    ShowTime = Game.Instance.GetTimestampSeconds(),
                    EndTime = Game.Instance.GetTimestampSeconds() + 1190,
                    RewardTime = Game.Instance.GetTimestampSeconds() + 1190,
                    TypeId = OrderPointActivity.kTypeId,
                    OrderPointActivityConfig = config,
                });
            }
            else if(fetchTime == 0)
            {
                fetchTime = Game.Instance.GetTimestampSeconds() + 10;
                Game.Instance.StartCoroutineGlobal(_CoWaitAndRun(11, ()=>Game.Instance.activityMan.TryRefetchServerActivity()));
            }
            return ret;
        });
    }
    #elif TEST_DELIVERY
    static NetworkManTest()
    {
        Dictionary<ulong, ActivityOrder> orders = new Dictionary<ulong, ActivityOrder>();
        SetFakeResponseHandler<GameNet.SnapUpReq>((req) => {
            var order = new ActivityOrder(){
                OrderId = (ulong)(orders.Count + 1),
                ActivityId = req.ActivityId,
                Status = OrderStatus.OrderPending
            };
            var ret = new GameNet.SnapUpResp() {
                Order = order
            };
            orders[order.OrderId] = order;
            return ret;
        });
        SetFakeResponseHandler<GameNet.QueryOrderReq>((req) => {
            var ret = new GameNet.QueryOrderResp();
            foreach(var order in orders.Values)
            {
                ret.Orders.Add(order);
            }
            return ret;
        });
        SetFakeResponseHandler<GameNet.QueryStockReq>((req) => {
            var ret = new GameNet.QueryStockResp() { Num = 117};
            return ret;
        });
        SetFakeResponseHandler<GameNet.SetAddressReq>((req) => {
            if(orders.TryGetValue(req.OrderId, out var order))
            {
                order.Address = req.Address;
            }
            return new GameNet.SetAddressResp(){};
        });
    }
    #elif TEST_ENDLESS_RANK
    private class EndlessRankData
    {
        public ulong currentRank;
        public Config.EndlessRank config = new Config.EndlessRank();
        public int order;
        public int enterOrder;
        public GameNet.EndlessRankingInfo data;
        public int startOrder;

        public EndlessRankData()
        {
            config.MailTitle = "test";
            config.No1Reward[5] = 3; config.No1Reward[1] = 30;
            config.No1Reward[5] = 2; config.No1Reward[1] = 20;
            config.No1Reward[5] = 1; config.No1Reward[1] = 10;
            config.StartTime = Game.Instance.GetTimestampSeconds() - 10;
            config.EndTime = Game.Instance.GetTimestampSeconds() + 300;
            config.ActivityType = 4;
            config.Id = 1232;
        }

        public void Update()
        {
            if(data != null)
            {
                var member = data.Members.FindEx((e)=>e.Uid == Game.Instance.socialMan.myId);
                member.OrderNum = Game.Instance.schoolMan.FillTotalCompletedOrder() - startOrder;
                var list = new List<GameNet.EndlessMember>();
                list.AddRange(data.Members);
                list.Sort((a,b)=>(b.OrderNum - a.OrderNum) * 100000000 + (int)(a.UpdateTs - b.UpdateTs) % 10000000);
                for(int i = 0; i < list.Count; i++)
                {
                    list[i].Rank = i+1;
                }
            }
        }

        public void StartNextRank()
        {
            data = new GameNet.EndlessRankingInfo();
            currentRank++;
            data.Id = currentRank;
            data.TargetOrderNum = 5;
            data.Members.Add(new GameNet.EndlessMember() {
                Name = System.Guid.NewGuid().ToString().Substring(0, 6),
                Appearance = new Gamekitdata.Appearance(),
                OrderNum = 0,
                UpdateTs = Game.Instance.GetTimestampSeconds(),
                CreateTs = Game.Instance.GetTimestampSeconds()
            });
            data.Members.Add(new GameNet.EndlessMember() {
                Name = System.Guid.NewGuid().ToString().Substring(0, 6),
                Appearance = new Gamekitdata.Appearance(),
                OrderNum = 0,
                UpdateTs = Game.Instance.GetTimestampSeconds(),
                CreateTs = Game.Instance.GetTimestampSeconds()
            });
            data.Members.Add(new GameNet.EndlessMember() {
                Name = System.Guid.NewGuid().ToString().Substring(0, 6),
                Appearance = new Gamekitdata.Appearance(),
                OrderNum = 0,
                UpdateTs = Game.Instance.GetTimestampSeconds(),
                CreateTs = Game.Instance.GetTimestampSeconds()
            });
            var app = new Gamekitdata.Appearance();
            Game.Instance.roleMan.GetHostPlayer().MarshalToServerData(app);
            data.Members.Add(new GameNet.EndlessMember() {
                Name = Game.Instance.socialMan.GetName(),
                Uid = Game.Instance.socialMan.myId,
                Appearance = app,
                OrderNum = 0,
                UpdateTs = Game.Instance.GetTimestampSeconds(),
                CreateTs = Game.Instance.GetTimestampSeconds()
            });
            startOrder = Game.Instance.schoolMan.FillTotalCompletedOrder();
        }
    }
    static NetworkManTest()
    {
        var data = new EndlessRankData();
        SetFakeResponseHandler<GameNet.GetActivitiesReq>((req) => {
            var ret = new GameNet.GetActivitiesResp() {

            };
            var config = data.config;
            ret.Activities.Add(new GameNet.Activity() {
                InstanceId = 1,
                ShowTime = data.config.StartTime,
                EndTime = data.config.EndTime,
                RewardTime = data.config.EndTime,
                TypeId = EndlessRankActivity.kTypeId,
                EndlessRankConfig = config,
            });
            return ret;
        });

        SetFakeResponseHandler<GameNet.EndlessRankingJoinReq>((req) => {
            if(data.data == null)
            {
                data.StartNextRank();
                data.Update();
                var ret = data.data;
                return ret;
            }
            else
            {
                return new netutils.ErrorResponse() {
                    Code = 1111,
                };
            }
        });

        SetFakeResponseHandler<GameNet.EndlessRankingGetReq>((req) => {
            if(data.data != null)
            {
                data.Update();
                return data.data;
            }
            else
            {
                return new GameNet.EndlessRankingInfo() {
                    Id = 0,
                };
            }
        });
    }
    #endif
#endif

    private static IEnumerator _CoWaitAndRun(float seconds, System.Action cb)
    {
        yield return new WaitForSeconds(seconds);
        cb?.Invoke();
    }

    public static GameNet.NetResponse GetFakeResponse(IMessage req)
    {
#if ENABLE_FAKE_SERVER
        // FAT_TODO
        // if(sFakeHandler.TryGetValue(req.GetType(), out var handler))
        // {
        //     var msg = handler(req);
        //     if(msg != null)
        //     {
        //         var rawAny = new netutils.RawAny() {
        //             Raw = sCodec.MarshalToByteString(msg),
        //             Uri = msg.GetType().FullName
        //         };
        //         var resp = new GameNet.NetResponse();
        //         if(msg is netutils.ErrorResponse errorObj)
        //         {
        //             resp.SetError(errorObj);
        //         }
        //         else
        //         {
        //             resp.SetMessage(rawAny, NetworkManTest.codec);
        //         }
        //         return resp;
        //     }
        // }
#endif
        return null;
    }

    public static void SetFakeResponseHandler<TReq>(System.Func<TReq, IMessage> handler) where TReq : IMessage
    {
#if ENABLE_FAKE_SERVER
        sFakeHandler[typeof(TReq)] = (msg) => handler((TReq)msg);
#endif
    }
}