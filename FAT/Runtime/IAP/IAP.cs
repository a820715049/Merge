/**
 * @Author: yingbo.li
 * @Date: 2023-12-20
 */
using System.Collections;
using System.Collections.Generic;
using System;
using FAT.Platform;
using fat.msg;
using fat.rawdata;
using fat.gamekitdata;
using EL;
using static fat.conf.Data;
using Debug = UnityEngine.Debug;
using EventType = fat.rawdata.EventType;
using fat.dbstate;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace FAT {
    public static class IAPFrom {
        public static readonly int GiftPack = 100;
        public static readonly int GiftPackLike = 105;
        public static readonly int ShopMan = 200;
    }

    public class IAPLateDelivery {
        public PayContext context;
        public IAPPack pack;
        public IAPProduct product;
        public int from;
        public string ClaimReason;
        public bool Claimed => !string.IsNullOrEmpty(ClaimReason);
    }

    public class IAP : IGameModule, IUserDataHolder {
        public bool Initialized => PlatformSDK.Instance.Adapter.IAPInit && Game.Manager.networkMan.isLogin;
        //记录目前累充金额/补单数据是否已经同步过
        public bool DataReady = false;
        //编辑器模式下累充金额使用客户端本地记录的数值 单位美分
#if UNITY_EDITOR
        public int TotalIAPServer
        {
            get => (int)_totalIAPLocal;
            private set {}
        }
#else
        public int TotalIAPServer { get; private set; }
#endif
        private ulong _totalIAPLocal = 0;
        public long FirstPayTS { get; private set; }
        public long LastPayTS { get; private set; }
        public int IAPCount { get; private set; }
        public IList<fat.msg.PayRecord> History { get; private set; }
        public List<Payment> IAPDelivery { get; } = new();
        public const int ErrorUninitialized = 12000;
        public const int ErrorProductNotFound = 12001;
        public const int ErrorMakeFailed = 12002;
        public const int ErrorDeliverFailed = 12003;
        public List<IAPLateDelivery> PendingDeliveries = new();

        void IUserDataHolder.FillData(LocalSaveData archive) {
            var game = archive.ClientData;
            var data = game.IAP ??= new();
            data.TotalIAPCent = _totalIAPLocal;
        }

        void IUserDataHolder.SetData(LocalSaveData archive) {
            var data = archive.ClientData.IAP;
            if (data == null) return;
            _totalIAPLocal = data.TotalIAPCent;
        }

        public void Reset() {
            MessageCenter.Get<MSG.ACTIVITY_STATE>().AddListenerUnique(CheckActivity);
        }

        public void Startup() {
            TryInit();
        }

        public void TryInit() {
            var sdk = PlatformSDK.Instance.Adapter;
            IEnumerator Init() {
                var retry = new RetryToken() { max = 10, delay = 10, delayN = 20 };
                var list = new List<string>();
                foreach (var (_, p) in GetIAPProductMap()) {
                    list.Add(p.ProductId);
                }
                init:
                var wait = sdk.InitIAP(list.ToArray(), null);
                yield return wait;
                if (!Initialized) {
                    var waitR = retry.Retry();
                    DebugEx.Warning($"iap init retry n={retry.n}");
                    yield return waitR;
                    goto init;
                }
                MessageCenter.Get<MSG.IAP_INIT>().Dispatch();
                yield return Late();
            }
            IEnumerator Late()
            {
                DebugEx.Info($"iap delivery check");
                var retry = new RetryToken() { max = 10, delay = 10, delayN = 20 };
            request:
                var task = Game.Manager.networkMan.IAPDeliveryCheck();
                yield return task;
                if (!task.isSuccess || task.result is not DeliverCargoResp resp)
                {
                    var waitR = retry.Retry();
                    var msg = $"failed to check unfinished iap delivery ({task.errorCode}){task.error} {task.result} retry:{retry.n}";
                    Debug.LogWarning(msg);
                    yield return waitR;
                    goto request;
                }
                PendingDeliveries.Clear();
                RefreshData(resp);
                foreach (var v in resp.Deliveries)
                {
                    TryDeliver(v);
                }
                DeliverComplete();
                DebugEx.Info("mailReshipment: PendingDeliveries count " + PendingDeliveries.Count);
                if (PendingDeliveries.Count > 0)
                {
                    IAPReshipmentPopup();
                }
            }
            if (sdk.IAPInit) return;
            Game.Instance.StartCoroutineGlobal(Init());
        }

        public void LoadConfig() { }

        

        public IAPProductInfo ProductInfo(string id_) {
            PlatformSDK.Instance.Adapter.IAPProduct.TryGetValue(id_, out var v);
            return v;
        }

        public bool FindIAPPack(int id_, out IAPPack conf) {
            conf = GetIAPPack(id_);
            if (conf == null) {
                Debug.LogError($"iap pack {id_} not found");
            }
            return conf != null;
        }

        public bool FindIAPProduct(int id_, out IAPProduct conf) {
            conf = GetIAPProduct(id_);
            if (conf == null) {
                Debug.LogError($"iap product {id_} not found");
            }
            return conf != null;
        }
        
        public bool FindCurrencyPack(int id_, out CurrencyPack conf) {
            conf = GetCurrencyPack(id_);
            if (conf == null) {
                Debug.LogError($"currency pack {id_} not found");
            }
            return conf != null;
        }

        public string PriceInfo(int iapId_) {
            if (FindIAPProduct(iapId_, out var conf)
                && PlatformSDK.Instance.Adapter.IAPProduct.TryGetValue(conf.ProductId, out var v)) {
                #if UNITY_EDITOR
                return conf.EditorPrice.ToString();
                #endif
                #pragma warning disable 0162
                return v.FormattedPrice;
                #pragma warning restore 0162
            }
            return I18N.Text("#SysComBtn1");
        }

        public void Purchase(int packId_, int from_, Action<bool, IAPPack> WhenComplete_) {
            if (!FindIAPPack(packId_, out var conf)) {
                Debug.LogError($"iap purchase pack {packId_} not found");
                WhenComplete_?.Invoke(false, null);
                return;
            }
            Purchase(conf, from_, WhenComplete_);
        }

        public void Purchase(IAPPack pack_, int from_, Action<bool, IAPPack> WhenComplete_, string info_ = null, ulong order_ = 0)
        {
            // 购买按钮复用uiopen音效
            Game.Manager.audioMan.TriggerSound("OpenWindow");

#if FAT_PIONEER
            ErrorToast(3101, null);
            return;
#endif

            IAPProduct conf = null;
            var block = new BlockToken();
            if (!Initialized) {
                Exit(false, ErrorUninitialized, $"iap purchase env not ready");
                return;
            }
            var packId = pack_.Id;
            var id = pack_.IapId;
            if (!FindIAPProduct(id, out conf)) {
                Exit(false, ErrorProductNotFound, $"iap purchase product {id} not found");
                return;
            }
            DataTracker.iap_purchase_start.Track(pack_, conf);
            AdjustTracker.TrackEvent(AdjustEventType.Purchase);
            Game.Instance.StartCoroutineGlobal(Routine());
            void Exit(bool r_, int error_ = 0, string msg_ = null) {
                if (r_) WhenPurchaseSuccess(conf, pack_);
                else ErrorToast(error_, msg_);
                block.Exit();
                DataTracker.TrackIAPDelivery(r_, pack_, conf, code_:error_, msg_:msg_, late_:false);
                try {
                    WhenComplete_?.Invoke(r_, pack_);
                }
                catch (Exception e) {
                    Debug.LogError($"iap purchase callback failure: {e.Message}\n{e.StackTrace}");
                }
                finally {
                    WhenComplete_ = null;
                }
            }
            IEnumerator Routine() {
                block.Enter();
                Debug.Log($"iap purchase id={packId}");
                var context = new PayContext() {
                    IapChannel = PlatformSDK.Instance.Adapter.GetIAPChannel(),
                    PayStoreId = packId,
                    PayStoreType = (PaymentType)from_,
                    ProductId = conf.ProductId,
                    ProductName = info_ ?? string.Empty,
                    OrderId = order_,
                };
                var task = Game.Manager.networkMan.IAPPurchase(packId, context);
                yield return task;
                if (!task.isSuccess || task.result is not MakeThroughCargoResp resp) {
                    Exit(false, ErrorMakeFailed, $"iap purchase with server failed ({task.errorCode}){task.error} {task.result}");
                    yield break;
                }
                var transaction = new Transaction(conf.ProductId, resp.ServerId, resp.ThroughCargo);
                var wait = true;
                var result = false;
                PlatformSDK.Instance.Purchase(transaction, (productId_, r_, e_) => {
                    wait = false;
                    result = r_;
                    var code = e_ == null ? 0 : (int)e_.ErrorCode;
                    if (!r_) {
                        Debug.LogWarning($"IAP purchase fail ----> ({code}){e_.Message}");
                        Exit(false, error_: code);
                        return;
                    }
                });
                while (wait) yield return null;
                if (!result) yield break;
                #if UNITY_EDITOR
                Exit(true);
                yield break;
                #endif
                #pragma warning disable 0162
                var taskD = Game.Manager.networkMan.IAPDeliveryCheck();
                yield return taskD;
                if (!taskD.isSuccess || taskD.result is not DeliverCargoResp respD) {
                    Exit(false, ErrorDeliverFailed, $"iap purchase failed to deliver ({taskD.errorCode}){taskD.error} {taskD.result}");
                    yield break;
                }
                PendingDeliveries.Clear();
                RefreshData(respD);
                if (respD.Deliveries.Count == 0) {
                    Exit(false, ErrorDeliverFailed, $"iap purchse fail: nothing in delivery");
                    yield break;
                }
                foreach (var v in respD.Deliveries) {
                    var c = v.PayContext;
                    if (WhenComplete_ != null && c.PayStoreId == packId && (int)c.PayStoreType == from_) {
                        Exit(true);
                        continue;
                    }
                    TryDeliver(v);
                }
                DeliverComplete();
                if (PendingDeliveries.Count > 0)
                {
                    IAPReshipmentPopup();
                }
#pragma warning restore 0162
            }
        }

        private void WhenPurchaseSuccess(IAPProduct conf_, IAPPack pack_) {
            var ts = Game.TimestampNow();
            if (FirstPayTS == 0) FirstPayTS = ts;
            LastPayTS = ts;
            ++IAPCount;
            _totalIAPLocal += (ulong)(conf_.EditorPrice * 100);
            //购买成功后进行Adjust打点 暂时默认一个账号一个礼包Id只打一次Adjust
            AdjustTracker.TrackIAPPackOnceEvent(pack_.Id);
            //对每个iapProduct进行打点 / token是根据价格进行划分的
#if UNITY_ANDROID
            AdjustTracker.TrackIAP(conf_.AndToken);
#elif UNITY_IOS
            AdjustTracker.TrackIAP(conf_.IosToken);
#endif
        }

        private void RefreshData(DeliverCargoResp resp_)
        {
            History = resp_.PayHistory;
            var info = resp_.PayStatInfo;
            TotalIAPServer = (int)info.TotalAmountCent;
            FirstPayTS = info.FirstPayTime;
            LastPayTS = info.LatestPayTime;
            IAPCount = info.PayCount;
            foreach(var d in resp_.Deliveries) {
                IAPDelivery.Add(d.Detail);
            }
            if (IAPDelivery.Count > 0) DebugEx.Info($"iap delivery {IAPDelivery.Count}");
            DataTracker.TraceUser().TotalRevenue().Apply();
            PlatformSDK.Instance.UpdateGameUserInfo();
        }

        private void DeliverComplete() {
            Game.Manager.archiveMan.SendImmediately(true);
            DataReady = true;
            MessageCenter.Get<MSG.IAP_DATA_READY>().Dispatch();
        }

        public void ConfirmDeliver() {
            if (IAPDelivery.Count > 0) DebugEx.Info($"iap delivery confirm");
            IAPDelivery.Clear();
        }

        private void TryDeliver(Delivery v) {
            var context = v.PayContext;
            var packId = context.PayStoreId;
            if (!FindIAPPack(packId, out var pConf)) return;
            if (!FindIAPProduct(pConf.IapId, out var dConf)) return;
            var from = (int)context.PayStoreType;
            var delivery = new IAPLateDelivery() { context = context, pack = pConf, product = dConf, from = from };
            // 保存未处理的交付信息
            PendingDeliveries.Add(delivery);
            DebugEx.Info($"mailReshipment: ProductName=>{delivery.context.ProductName}=>OrderId=>{delivery.context.OrderId}=>packId=>{packId}=>from=>{from}");
            try
            {
                MessageCenter.Get<MSG.IAP_LATE_DELIVERY>().Dispatch(delivery);
            }
            catch (Exception e)
            {
                Debug.LogError($"unfinished iap deliver for {packId}({from}) failure: {e.Message}\n{e.StackTrace}");
            }
            DataTracker.TrackIAPDelivery(delivery.Claimed, pConf, dConf, msg_: delivery.ClaimReason, late_: true);
            if (!delivery.Claimed) {
                Debug.LogError($"unfinished iap for {packId}({from}) is not claimed");
            }
            else {
                Debug.Log($"unfinished iap for {packId}({from}) claimed by {delivery.ClaimReason}");
                WhenPurchaseSuccess(dConf, pConf);
            }
        }

        public void ErrorToast(int code_, string msg_) {
            if (msg_ != null) Debug.LogError(msg_);
            var toast = code_ switch {
                ErrorUninitialized or ErrorProductNotFound or ErrorMakeFailed or ErrorDeliverFailed
                    => Toast.IapRequestFail,
                3102 or 3106 or 3120 or 3007 or 3008 or 3000 or 3001 or 3002 or 3003 or 3004 or
                3005 or 3006 or 3010 or 3011 or 3012 or 3014 or 3203 or 3220 or 3100 or 3112 or
                3201 or 3205 or 3206 or 3214 or 3215
                    => Toast.IapAccountError,
                3101 or 3103 or 3105 or 3107 or 3113 or 3114 or 3115 or 3116 or 3117 or 3118 or
                3119 or 3200 or 3202 or 3204 or 3208 or 3209 or 3210 or 3216 or 3217 or 3218 or 3219
                    => Toast.IapPurchaseError,
                3108 or 3221 or 3212
                    => Toast.IapNetworkError,
                3109 or 3110 or 3111 or 3211 or 3213
                    => Toast.IapDeviceError,
                3121 or 3009 or 3013
                    => Toast.IapPurchasing,
                3104 or 3207
                    => Toast.IapCancel,
                _ => Toast.IapUnknownError,
            };
            Game.Manager.commonTipsMan.ShowPopTips(toast);
        }

        public bool IAPLabel(int id_, int packId_, out Label confL_, out string text_) {
            text_ = null;
            confL_ = GetLabel(id_);
            if (confL_ == null) {
                if (id_ > 0) DebugEx.Warning($"failed to find label config {id_}");
                return false;
            }
            var confP = GetIAPPack(packId_);
            if (confP == null) return false;
            var type = confL_.LabelType;
            static string LDiscount(int e_) {
                var v = (int)Math.Ceiling(100f * (e_ - 100f) / e_);
                return v <= 0 ? null : I18N.FormatText("#SysComDesc463", v);
            }
            static string LBonus(int e_) {
                var v = e_;
                return v <= 100 ? null : I18N.FormatText("#SysComDesc464", v);
            }
            static string LMore(int e_) {
                var v = e_ - 100;
                return v <= 0 ? null : I18N.FormatText("#SysComDesc466", v);
            }
            text_ = type switch {
                LabelType.Discount => LDiscount(confP.Effect),
                LabelType.Bonus => LBonus(confP.Effect),
                LabelType.Limit => I18N.Text(confL_.Key),
                LabelType.More => LMore(confP.Effect),
                _ => null,
            };
            return text_ != null;
        }

        public void CheckActivity(ActivityLike acti_) {
            if (acti_.Type == EventType.CardAlbum) {
                MessageCenter.Get<MSG.IAP_REWARD_CHECK>().Dispatch();
            }
        }
        
        public void DebugSetIAPCent(ulong iapcent)
        {
            _totalIAPLocal = iapcent;
            var ts = Game.TimestampNow();
            if (FirstPayTS == 0) FirstPayTS = ts;
            LastPayTS = ts;
            Game.Manager.archiveMan.SendImmediately(true);
        }

        public void TestHistory() {
            var now = Game.TimestampNow();
            History = new List<fat.msg.PayRecord>() {
                new() {
                    PayContext = new() {
                        ProductName = ActivityLite.InfoCompact(900100, 0, EventType.Energy, "daily"),
                    },
                    Detail = new() {
                        AmountCent = 299,
                        PaidTime = (ulong)now - 100,
                    }
                },
                new() {
                    PayContext = new() {
                        ProductName = ActivityLite.InfoCompact(900200, 0, EventType.DailyPop, "daily"),
                    },
                    Detail = new() {
                        AmountCent = 499,
                        PaidTime = (ulong)now - 50,
                    }
                }
            };
            DeliverComplete();
        }

        public void ReportHistory() {
            var b = new StringBuilder();
            var k = 0;
            batch:
            var i = 0;
            for (; i < 10 && k < History.Count; ++k) {
                var str = JsonConvert.SerializeObject(History[k]);
                b.AppendLine(str);
            }
            b.Insert(0, $"iap history {k-i}-{k}:\n");
            DebugEx.Warning(b.ToString());
            b.Clear();
            if (k < History.Count) goto batch;
        }

        /// <summary>
        /// 补单弹窗
        /// </summary>
        /// <param name="delivery"></param>
        private void IAPReshipmentPopup()
        {
            foreach (var delivery in PendingDeliveries)
            {
                IAPMailReshipment.LateDelivery(delivery);
            }
        }
    }
}