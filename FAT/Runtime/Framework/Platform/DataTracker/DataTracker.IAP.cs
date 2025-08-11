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

public static partial class DataTracker {
    [Serializable] internal class iap_purchase_start : MergeCommonData {
        public int pkg_id;
        public string product_id;
        public string iap_name;

        public static void Track(IAPPack pack_, IAPProduct product_) {
            var data = _GetTrackData<iap_purchase_start>();
            data.pkg_id = pack_.Id;
            data.product_id = product_.ProductId;
            data.iap_name = pack_.TgaName;
            _TrackData(data);
        }
    }

    [Serializable] internal class iap_delivery_success : MergeCommonData {
        public int pkg_id;
        public string product_id;
        public string iap_name;
        public string msg;
        public bool is_late;

        public static void Track(IAPPack pack_, IAPProduct product_, string msg_ = null, bool late_ = false) {
            var data = _GetTrackData<iap_delivery_success>();
            data.pkg_id = pack_.Id;
            data.product_id = product_.ProductId;
            data.iap_name = pack_.TgaName;
            data.msg = msg_;
            data.is_late = late_;
            _TrackData(data);
        }
    }

    [Serializable] internal class iap_delivery_fail : MergeCommonData {
        public int pkg_id;
        public string product_id;
        public string iap_name;
        public int errorcode;
        public string msg;
        public bool is_late;

        public static void Track(IAPPack pack_, IAPProduct product_, int code_, string msg_ = null, bool late_ = false) {
            var data = _GetTrackData<iap_delivery_fail>();
            data.pkg_id = pack_.Id;
            data.product_id = product_.ProductId;
            data.iap_name = pack_.TgaName;
            data.errorcode = code_;
            data.msg = msg_;
            data.is_late = late_;
            _TrackData(data);
        }
    }

    public static void TrackIAPDelivery(bool result_, IAPPack pack_, IAPProduct product_, int code_ = 0, string msg_ = null, bool late_ = false) {
        if (result_) iap_delivery_success.Track(pack_, product_, msg_, late_);
        else iap_delivery_fail.Track(pack_, product_, code_, msg_, late_);
    }
}