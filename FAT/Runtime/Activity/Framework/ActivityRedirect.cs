using System;
using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;
using FAT.RemoteAnalysis;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FAT {
    public interface IActivityRedirect {
        EventType Type { get; }
        string SubType { get; }
        int PackId { get; }
        IList<int> Pool { get; }
        bool UseAPI { get; }
        string ModelVersion { get; }
        
        void RedirectApply(int id_);
    }
    
    public class ActivityRedirect {
        public static readonly string url = "https://dsc-ai.centurygame.com/api/v1/fat/gift-predict";

        public static bool Ready() => Game.Manager.iap.DataReady;
        
        public async UniTask TryRequest(IActivityRedirect r_, ActivityLike e_) {
            try {
                if (!Ready()) {
                    EL.DebugEx.Info($"api_pack {e_.Info3} delay until ready");
                    do await Task.Delay(100);
                    while (!Ready());
                    EL.DebugEx.Info($"api_pack {e_.Info3} ready");
                }
                DataTracker.APIPackSend(e_);
                var redirect = Game.Manager.activity.redirect;
                var gConf = Game.Manager.configMan.globalConfig;
                var (valid, id, err) = await redirect.RequestPack(r_).Timeout(TimeSpan.FromMilliseconds(gConf.PackApiTimeout));
                if (valid && r_.UseAPI) {
                    r_.RedirectApply(id);
                }
                if (!valid) EL.DebugEx.Warning($"api_pack {e_.Info3} request result invalid: {err}");
                DataTracker.APIPackReturn(e_, r_.PackId, r_.UseAPI, false);
            }
            catch (Exception e) {
                DataTracker.APIPackReturn(e_, r_.PackId, r_.UseAPI, e is TimeoutException);
                EL.DebugEx.Warning($"api_pack {e_.Info3} request failed:\n{e}");
            }
        }

        public async UniTask<(bool, int, string)> RequestPack(IActivityRedirect e_, int timeout_ = 10) {
            var req = CreateRequest(e_);
            var str = JsonConvert.SerializeObject(req);
            EL.DebugEx.Info($"api_pack request:{str}");
            using var web = UnityWebRequest.Post(url, str, "application/json");
            web.timeout = timeout_;
            await web.SendWebRequest();
            if (web.result is not UnityWebRequest.Result.Success) return (false, default, $"{web.result}");
            str = web.downloadHandler.text;
            EL.DebugEx.Info($"api_pack reponse:{str}");
            var pack = JsonConvert.DeserializeObject<PackResponse>(str);
            if (pack.code != 200 || pack.data.model_package_id.Length == 0) return (false, default, $"response fail");
            var id = pack.data.model_package_id[0];
            if (!(id == req.pack_info.pack_rec_next.pack_id || req.pack_info.pack_rec_pool.TryGetValue(id, out var p))
                || GetIAPPack(id) == null) return (false, default, $"validate fail");
            return (true, id, null);
        }

        public static PackRequest CreateRequest(IActivityRedirect e_)
            => new() {
                user_info = UserInfo.Current,
                require_info = CreateRequire(e_),
                purchase_info = CreatePurchase(e_),
                pack_info = new() {
                    pack_rec_next = CreatePack(e_.PackId),
                    pack_rec_pool = e_.Pool.Select(CreatePack).ToDictionary(e => e.pack_id),
                },
                model_info = new() {
                    model_version = e_.ModelVersion,
                },
            };

        public static RequireInfo CreateRequire(IActivityRedirect e_) {
            var lastOrder = Game.Manager.mainOrderMan.curOrderHelper.proxy.recentApiOrder;
            return new() {
                last_order_info = RemoteOrderWrapper.GetLastInfo(lastOrder),
                require_pack_type = $"{e_.Type}",
                require_pack_subtype = e_.SubType,
            };
        }

        public static PurchaseInfo CreatePurchase(IActivityRedirect e_) {
            var iap = Game.Manager.iap;
            var history = iap.History;
            var r = new PurchaseInfo() {
                total_pay_amount = iap.TotalIAPServer,
            };
            if (history != null && history.Count > 0) {
                var last = history[^1];
                r.last_pay_amount = (int)last.Detail.AmountCent;
                r.last_pay_timestamp = last.Detail.PaidTime;
                var lastT = history.Where(e => {
                    var (_, _, _, type, sub) = ActivityLite.InfoUnwrap(e.PayContext.ProductName);
                    return e_ == null || type == e_.Type && sub == e_.SubType;
                }).LastOrDefault();
                if (lastT != null) {
                    r.last_target_amount = (int)lastT.Detail.AmountCent;
                    r.last_target_timestamp = lastT.Detail.PaidTime;
                }
                var diff_min = -6;  // 统计最近7天
                var d7_amount = 0;
                foreach (var h in history)
                {
                    var diff = EvaluateEventTrigger.LT((long)h.Detail.PaidTime);
                    if (diff < diff_min || diff > 0)
                        continue;
                    d7_amount += (int)h.Detail.AmountCent;
                }
                r.recent_d7_pay_amount = d7_amount;
            }
            return r;
        }

        public static Pack CreatePack(int id_) {
            var pack = GetIAPPack(id_);
            var product = GetIAPProduct(pack.IapId);
            var list = new List<PackItem>();
            var rList = Game.Manager.activity.IsActive(EventType.CardAlbum) ? pack.RewardAlbum : pack.Reward;
            list = rList.Select(e => {
                var ee = e.AsSpan();
                var p = ee.IndexOf(':');
                var r = new PackItem();
                int.TryParse(ee[..p], out r.id);
                int.TryParse(ee[(p + 1)..], out r.count);
                return r;
            }).ToList();
            return new() {
                pack_id = id_,
                pack_price = (int)Math.Round(product.EditorPrice * 100),
                pack_item = list,
            };
        }
    }

    public struct PackRequest {
        public UserInfo user_info;
        public RequireInfo require_info;
        public PurchaseInfo purchase_info;
        public PackInfo pack_info;
        public ModelInfo model_info;
    }

    public struct RequireInfo {
        public LastInfo last_order_info;
        public string require_pack_type;
        public string require_pack_subtype;
    }

    public struct PurchaseInfo {
        public int total_pay_amount;
        public int recent_d7_pay_amount;
        public int last_pay_amount;
        public ulong last_pay_timestamp;
        public int last_target_amount;
        public ulong last_target_timestamp;
    }

    public struct PackInfo {
        public Pack pack_rec_next;
        public Dictionary<int, Pack> pack_rec_pool;
    }

    public struct Pack {
        public int pack_id;
        public int pack_price;
        public List<PackItem> pack_item;
    }

    public struct PackItem {
        public int id;
        public int count;
    }

    public struct PackResponse {
        public struct Data {
            public string data_source_type;
            public int[] model_package_id;
            public int[] model_package_price;
        }

        public int code;
        public string msg;
        public Data data;
    }
}