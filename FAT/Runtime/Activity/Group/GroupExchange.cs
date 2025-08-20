using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using Config;

namespace FAT
{
    public class GroupExchange : ActivityGroup
    {
        private ToolExchange template;
        private (int id, int c, int m) coinConf;

        public GroupExchange()
        {
            SetupListener();
        }

        public void SetupListener()
        {
            MessageCenter.Get<MSG.GAME_COIN_ADD>().AddListenerUnique(CheckRefresh);
        }

        public (bool, string) CheckTemplate((int id, int) id_)
        {
            if (template != null) return (true, null);
            template = GetToolExchange(id_.id);
            if (template == null)
            {
                return Activity.Invalid(id_, $"no config for {nameof(ToolExchange)} {id_}");
            }
            coinConf = template.ActiveCoin.ConvertToInt3();
            return (true, null);
        }

        public void CheckRefresh(CoinChange _)
        {
            var activity = Game.Manager.activity;
            var idP = 1;
            var id = (idP, 0);
            if (activity.IsInvalid(id, out var _)) return;
            var (r, _) = CheckTemplate(id);
            if (!r) return;
            // if (!activity.NUPassed) return;
            var level = Game.Manager.mergeLevelMan.displayLevel;
            if (level < template.ActiveLevel || level >= template.ShutdownLevel) return;
            if (activity.IsActive(id)) return;
            var ts = activity.RecordOf(idP) + template.Interval;
            var now = Game.TimestampNow();
            if (now <= ts) return;
            var coin = Game.Manager.coinMan.GetCoin(CoinType.MergeCoin);
            var coinTarget = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(coinConf.c, coinConf.m);
            if (coin < coinTarget) return;
            TryAdd(activity, id, EventType.ToolExchange);
        }

        public override (bool, string) TryAdd(Activity activity_, (int, int) id_, EventType type_, ActivityInstance data_ = null, in Option option_ = default)
        {
            if (activity_.IsInvalid(id_, out var rsi)) return (false, rsi);
            var (r, rs) = CheckTemplate(id_);
            if (!r) return (r, rs);
            var acti = new ExchangeTool(template);
            acti.LoadData(data_);
            option_.Apply(acti);
            activity_.AddActive(id_, acti, new_: data_ == null);
            if (data_ == null) Game.Manager.screenPopup.Queue(acti.Popup);
            return (true, null);
        }

        public void Purchase(ExchangeTool pack_, Action<IList<RewardCommitData>> WhenComplete_ = null)
        {
            var coinMan = Game.Manager.coinMan;
            if (coinMan.CanUseCoin(CoinType.MergeCoin, pack_.price))
            {
                var reason = ReasonString.tool_exchange;
                coinMan.UseCoin(CoinType.MergeCoin, pack_.price, reason)
                .OnSuccess(() =>
                {
                    var rewardMan = Game.Manager.rewardMan;
                    using var _ = ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var list);
                    foreach (var r in pack_.goods)
                    {
                        var data = rewardMan.BeginReward(r.Id, r.Count, reason);
                        list.Add(data);
                    }
                    ++pack_.buyCount;
                    if (pack_.WillEnd) Activity.EndImmediate(pack_, expire_: false);
                    WhenComplete_?.Invoke(list);
                })
                .Execute();
            }
        }
    }
}
