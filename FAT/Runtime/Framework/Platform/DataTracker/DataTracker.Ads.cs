/**
 * @Author: handong.liu
 * @Date: 2021-11-19 17:59:11
 */
using System;
using FAT;

public static partial class DataTracker
{
    [Serializable]
    private class ad_loadingsuccess : DataTrackBase
    {
        public string ad_unitid;
        public string ad_format;
        public ad_loadingsuccess Fill(string _unitid, string _format)
        {
            ad_unitid = _unitid;
            ad_format = _format;
            return this;
        }
    }
    public static void TrackAdLoadingSuccess(string unitid, string from)
    {
        _TrackData(_GetTrackData<ad_loadingsuccess>().Fill(unitid, from));
    }

    [Serializable]
    private class ad_loadingfail : DataTrackBase
    {
        public string ad_unitid;
        public string ad_format;
        public string errormsg;
        public ad_loadingfail Fill(string _unitid, string _format, string _error)
        {
            ad_unitid = _unitid;
            ad_format = _format;
            errormsg = _error;
            return this;
        }
    }
    public static void TrackAdLoadingFail(string unitid, string from, string error)
    {
        _TrackData(_GetTrackData<ad_loadingfail>().Fill(unitid, from, error));
    }

    [Serializable]
    private class ad_showfail : DataTrackBase
    {
        public string ad_unitid;
        public string ad_format;
        public string from;
        public string errormsg;
        public ad_showfail Fill(string _unitid, string _format, string _from, string _error)
        {
            ad_unitid = _unitid;
            ad_format = _format;
            from = _from;
            errormsg = _error;
            return this;
        }
    }
    public static void TrackAdShowFail(string unitid, string format, string from, string error)
    {
        _TrackData(_GetTrackData<ad_showfail>().Fill(unitid, format, from, error));
    }

    [Serializable]
    private class ad_iconshow : DataTrackBase
    {
        public string ad_format;
        public string from;
        public ad_iconshow Fill(string _format, string _from)
        {
            ad_format = _format;
            from = _from;
            return this;
        }
    }
    public static void TrackAdIconShow(int adId)
    {
        string format = centurygame.CGAdvertising.CGAdvertisingDuty.RewardAd.ToString();
        string from = Game.Manager.adsMan.GetAdsName(adId);
        _TrackData(_GetTrackData<ad_iconshow>().Fill(format, from));
    }

    [Serializable]
    private class ad_iconclick : DataTrackBase
    {
        public string ad_format;
        public string from;
        public ad_iconclick Fill(string _format, string _from)
        {
            ad_format = _format;
            from = _from;
            return this;
        }
    }
    public static void TrackAdIconClick(int adId)
    {
        string format = centurygame.CGAdvertising.CGAdvertisingDuty.RewardAd.ToString();
        string from = Game.Manager.adsMan.GetAdsName(adId);
        _TrackData(_GetTrackData<ad_iconclick>().Fill(format, from));
    }

    [Serializable]
    private class ad_openads : DataTrackBase
    {
        public string ad_format;
        public string from;
        public ad_openads Fill(string _format, string _from)
        {
            ad_format = _format;
            from = _from;
            return this;
        }
    }
    public static void TrackAdOpenAds(string format, string from)
    {
        _TrackData(_GetTrackData<ad_openads>().Fill(format, from));
    }

    [Serializable]
    private class ad_viewsuccess : DataTrackBase
    {
        public string ad_format;
        public string from;
        public ad_viewsuccess Fill(string _format, string _from)
        {
            ad_format = _format;
            from = _from;
            return this;
        }
    }
    
    public static void TrackAdViewSuccess(string format, string from)
    {
        _TrackData(_GetTrackData<ad_viewsuccess>().Fill(format, from));
    }

    [Serializable]
    private class ad_reward : DataTrackBase
    {
        public string ad_format;
        public string from;
        public int num;     //获得的物品/资源数量
        public int id;      //物品/资源对应的id
        public ad_reward Fill(string _format, string _from, int _num, int _id)
        {
            ad_format = _format;
            from = _from;
            num = _num;
            id = _id;
            return this;
        }
    }
    
    public static void TrackAdReward(int adId, int num, int id)
    {
        string format = centurygame.CGAdvertising.CGAdvertisingDuty.RewardAd.ToString();
        string from = Game.Manager.adsMan.GetAdsName(adId);
        _TrackData(_GetTrackData<ad_reward>().Fill(format, from, num, id));
    }
}