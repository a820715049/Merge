/*
 * @Author: yanfuxing
 * @Date: 2025-07-01 10:20:05
 */
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using CDKeyReward = fat.gamekitdata.Reward;
using UnityEngine;
using System;
using System.Web;

namespace FAT
{
    public class CommunityLinkMan : IGameModule, IUserDataHolder
    {
        public int RecordClickLinkId = -1;
        private List<SettingsCommunity> _communityList = new();
        private List<ShopCommunity> _communityShopList = new();
        public List<RewardCommitData> RewardCommitDataList = new();
        public Dictionary<int, int> CommunityLinkDataDic = new();
        public const int CommunityPlanRewardPopupId = 134; //社区奖励弹窗ID
        private static bool _IsCheckAbsoluteURL = false;

        public void Startup()
        {
            // 无后台直接拉起处理
            if (!_IsCheckAbsoluteURL && !string.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
                _IsCheckAbsoluteURL = true;
            }
            Application.deepLinkActivated += OnDeepLinkActivated;
        }

        private void OnDeepLinkActivated(string url)
        {
            DebugEx.Info($"communityLinkMan deepLinkActivated: {url}");
            var payload = ParsePayloadFromUrl(url);
            if (string.IsNullOrEmpty(payload))
            {
                DebugEx.Info("communityLinkMan deepLinkActivated payload is null");
                return;
            }
            TryPopGiftReward(payload);
        }

        public void LoadConfig() { }
        public void Reset()
        {
            Clear();
        }

        public void SetData(LocalSaveData archive)
        {
            //读取社群外链状态数据
            var CommunityLinkData = archive.ClientData.PlayerGameData.CommunityLinkData;
            if (CommunityLinkData != null)
            {
                CommunityLinkDataDic.Clear();
                foreach (var item in CommunityLinkData.LinkDic)
                {
                    CommunityLinkDataDic[item.Key] = item.Value;
                }
            }
        }

        public void FillData(LocalSaveData archive)
        {
            //保存社群外链状态数据
            var CommunityLinkData = new CommunityLinkData();
            foreach (var item in CommunityLinkDataDic)
            {
                CommunityLinkData.LinkDic.Add(item.Key, item.Value);
            }
            archive.ClientData.PlayerGameData.CommunityLinkData = CommunityLinkData;
        }

        /// <summary>
        /// 获取社区列表
        /// </summary>
        /// <returns>社区列表</returns>
        public List<SettingsCommunity> GetCommunityList()
        {
            _communityList.Clear();
            var configs = Game.Manager.configMan.GetSettingsCommunity();
            foreach (var config in configs)
            {
                var unlock = Game.Manager.activityTrigger.Evaluate(config.UnlockReguire);
                if (unlock && config.IsOn)
                    _communityList.Add(config);
            }
            _communityList.Sort((a, b) => a.Sequence - b.Sequence);
            return _communityList;
        }

        /// <summary>
        /// 获取社区商店列表
        /// </summary>
        /// <returns></returns>
        public List<ShopCommunity> GetCommunityShopList()
        {
            _communityShopList.Clear();
            var configs = fat.conf.Data.GetShopCommunityMap().Values;
            foreach (var config in configs)
            {
                var linkData = GetCommunityLinkDataById(config.Link);
                //策划约定：社区商店链接必须有奖励才能展示
                if (linkData != null && !string.IsNullOrEmpty(linkData.Reward))
                {
                    if (config.IsOn)
                        _communityShopList.Add(config);
                }
            }
            _communityShopList.Sort((a, b) => a.Sequence - b.Sequence);
            return _communityShopList;
        }

        /// <summary>
        /// 获取社区链接数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public CommunityLink GetCommunityLinkDataById(int id)
        {
            return fat.conf.Data.GetCommunityLink(id);
        }

        /// <summary>
        /// 获取社区链接奖励
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        public RewardConfig GetCommunityLinkReward(int linkId)
        {
            var linkData = GetCommunityLinkDataById(linkId);
            if (linkData != null)
            {
                return linkData.Reward.ConvertToRewardConfig();
            }
            return null;
        }

        /// <summary>
        /// 设置社区链接奖励状态
        /// </summary>
        /// <param name="linkId"></param>
        public void SetLinkRewardState(int linkId, CommunityLinkRewardState state)
        {
            CommunityLinkDataDic[linkId] = (int)state;
        }

        /// <summary>
        /// 是否显示奖励标签(有无奖励)
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        public bool IsHasReward(int linkId)
        {
            if (linkId == -1)
            {
                return false;
            }
            return !CommunityLinkDataDic.ContainsKey(linkId);
        }

        /// <summary>
        /// 是否显示商店链接Item
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        public bool IsShowShopLinkItem(ShopCommunity data)
        {
            return data.IsOn && !CommunityLinkDataDic.ContainsKey(data.Link);
        }

        /// <summary>
        /// 是否显示商店链接节点
        /// </summary>
        /// <returns></returns>
        public bool IsShowShopLink()
        {
            var communityShopList = GetCommunityShopList();
            foreach (var communityShop in communityShopList)
            {
                if (IsShowShopLinkItem(communityShop))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 是否显示奖励弹窗
        /// </summary>
        /// <returns></returns>
        public bool IsShowRewardUI()
        {
            return RecordClickLinkId != -1 && IsGetReward(RecordClickLinkId);
        }

        /// <summary>
        /// 是否领取奖励(表现层)
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        public bool IsGetReward(int linkId)
        {
            if (linkId == -1)
            {
                return false;
            }
            //未领取做展示
            return CommunityLinkDataDic.ContainsKey(linkId) && CommunityLinkDataDic[linkId] == (int)CommunityLinkRewardState.NotReceivedReward;
        }

        /// <summary>
        /// 是否显示奖励
        /// </summary>
        /// <returns></returns>
        public bool IsShowReward(CommunityPopupType type)
        {
            return type == CommunityPopupType.LinkGetReward
            || type == CommunityPopupType.LinkHadGetReward
            || type == CommunityPopupType.CommunityLinkReward;
        }

        /// <summary>
        /// 是否显示链接按钮
        /// </summary>
        /// <returns></returns>
        public bool IsShowAllLinkBtn(CommunityPopupType type)
        {
            return type == CommunityPopupType.LinkNotValid || type == CommunityPopupType.LinkRewardCountIsTop;
        }

        /// <summary>
        /// 根据类型设置描述
        /// </summary>
        /// <param name="type"></param>
        public string SetDescByType(CommunityPopupType type)
        {
            switch (type)
            {
                case CommunityPopupType.LinkNotValid:
                    return I18N.Text("#SysComDesc1322");
                case CommunityPopupType.LinkRewardCountIsTop:
                    return I18N.Text("#SysComDesc1323");
                case CommunityPopupType.LinkHadGetReward:
                case CommunityPopupType.LinkAgainActivate:
                    return I18N.Text("#SysComDesc1321");
                case CommunityPopupType.LinkGetReward:
                    return I18N.Text("#SysComDesc1320");
                case CommunityPopupType.CommunityLinkReward:
                    return I18N.Text("#SysComDesc1318");
            }
            return null;
        }

        /// <summary>
        /// 根据类型设置标题
        /// </summary>
        /// <param name="type"></param>
        public string SetTitleByType(CommunityPopupType type)
        {
            switch (type)
            {
                case CommunityPopupType.LinkNotValid:
                case CommunityPopupType.LinkRewardCountIsTop:
                case CommunityPopupType.LinkHadGetReward:
                case CommunityPopupType.LinkGetReward:
                case CommunityPopupType.LinkAgainActivate:
                    return I18N.Text("#SysComDesc1319");
                case CommunityPopupType.CommunityLinkReward:
                    return I18N.Text("#SysComDesc1317");
            }
            return null;
        }

        /// <summary>
        /// 根据类型设置领取文本
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string SetClaimTextByType(CommunityPopupType type)
        {
            switch (type)
            {
                case CommunityPopupType.LinkGetReward:
                case CommunityPopupType.CommunityLinkReward:
                    return I18N.Text("#SysComDesc1175");
                case CommunityPopupType.LinkHadGetReward:
                case CommunityPopupType.LinkNotValid:
                case CommunityPopupType.LinkRewardCountIsTop:
                case CommunityPopupType.LinkAgainActivate:
                    return I18N.Text("#SysComDesc1176");
            }
            return null;
        }

        /// <summary>
        /// 数据Clear
        /// </summary>
        public void Clear()
        {
            RecordClickLinkId = -1;
            CommunityLinkDataDic.Clear();
            RewardCommitDataList.Clear();
            _communityList.Clear();
            _communityShopList.Clear();
            Application.deepLinkActivated -= OnDeepLinkActivated;
        }

        /// <summary>
        /// 尝试弹出礼包奖励
        /// </summary>
        /// <param name="giftCode"></param>
        public void TryPopGiftReward(string giftCode)
        {
            if (string.IsNullOrEmpty(giftCode))
            {
                DebugEx.Info("CommunityLinkMan TryPopGiftReward giftCode is null");
                return;
            }

            UniTask.Void(async () =>
            {
                RewardCommitDataList.Clear();
                var (suc, errCode, CDKeyRewardList) = await Game.Manager.cdKeyMan.ExchangeGiftCode(giftCode, RewardCommitDataList);
                if (errCode == (long)CommunityPopupType.CDKeyNotInvalid)
                {
                    DataTracker.TrackLogInfo($"CommunityLinkMan NotValid errCode: {errCode}");
                    return;
                }

                //添加错误码异常情况下的容错处理，防止出现异常弹窗
                if (!IsValidErrorCode(errCode))
                {
                    DataTracker.TrackLogInfo($"CommunityLinkMan NotValid errCode: {errCode}");
                    return;
                }

                var communityLinkRewardData = new CommunityLinkRewardData();
                communityLinkRewardData.CommunityPopupType = GetCommunityPopupTypeByErrorCode(errCode);
                communityLinkRewardData.commitRewardList = RewardCommitDataList;
                communityLinkRewardData.CDKeyRewardList = CDKeyRewardList;
                communityLinkRewardData.GiftCode = giftCode;
                communityLinkRewardData.LinkType = LinkType.CommunityGiftLink;
                DataTracker.TrackLogInfo($"CommunityLinkMan rewardCount: {RewardCommitDataList.Count},CDKeyRewardList: {CDKeyRewardList.Count},giftCode: {giftCode},CommunityPopupType: {communityLinkRewardData.CommunityPopupType},errCode: {errCode}");
                CommunityPlanRewardPop rewardPop = new CommunityPlanRewardPop(CommunityPlanRewardPopupId, new UIResAlt(UIConfig.UICommunityPlanGiftReward));
                Game.Manager.screenPopup.TryQueue(rewardPop, PopupType.Login, communityLinkRewardData);
            });
        }

        /// <summary>
        /// 根据错误码获取社区弹窗类型
        /// </summary>
        /// <param name="ErrorCode"></param>
        /// <returns></returns>
        public CommunityPopupType GetCommunityPopupTypeByErrorCode(long ErrorCode)
        {
            switch (ErrorCode)
            {
                case (long)CommunityPopupType.LinkNotValid:
                    return CommunityPopupType.LinkNotValid;
                case (long)CommunityPopupType.LinkRewardCountIsTop:
                    return CommunityPopupType.LinkRewardCountIsTop;
                case (long)CommunityPopupType.LinkHadGetReward:
                case (long)CommunityPopupType.LinkAgainActivate:
                    return CommunityPopupType.LinkHadGetReward;
            }
            return CommunityPopupType.None;
        }

        /// <summary>
        /// 是否需要领取奖励
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsNeedClaimReward(CommunityPopupType type)
        {
            return type == CommunityPopupType.LinkGetReward;
        }

        /// <summary>
        /// 是否显示红点
        /// </summary>
        /// <returns></returns>
        public bool IsShowRedDot()
        {
            bool isShow = false;
            var communityList = GetCommunityList();
            foreach (var community in communityList)
            {
                if (community.IsOn)
                {
                    var linkData = GetCommunityLinkDataById(community.Id);
                    if (linkData != null && !string.IsNullOrEmpty(linkData.Reward))
                    {
                        isShow = !CommunityLinkDataDic.ContainsKey(community.Id);
                        if (isShow)
                        {
                            break;
                        }
                    }

                }
            }
            return isShow;
        }

        /// <summary>
        /// 社区邮件活动是否显示
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsShowCommunityLinkById(int id)
        {
            return !CommunityLinkDataDic.ContainsKey(id);
        }

        /// <summary>
        /// 解析url获取payload
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string ParsePayloadFromUrl(string url)
        {
            Uri uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            return query.Get("payload");
        }

        /// <summary>
        /// Debug用，清空社区链接数据
        /// </summary>
        public void DebugClearCommunityLinkData()
        {
            CommunityLinkDataDic.Clear();
        }

        /// <summary>
        /// 是否是有效错误码(增加容错处理，防止异常弹窗)
        /// </summary>
        /// <param name="errCode"></param>
        /// <returns></returns>
        private bool IsValidErrorCode(long errCode)
        {
            return errCode == (long)CommunityPopupType.LinkNotValid ||
                   errCode == (long)CommunityPopupType.LinkRewardCountIsTop ||
                   errCode == (long)CommunityPopupType.LinkHadGetReward ||
                   errCode == (long)CommunityPopupType.LinkGetReward ||
                   errCode == (long)CommunityPopupType.LinkAgainActivate;
        }
    }
    public enum CommunityPopupType
    {
        None,
        LinkNotValid = 1010, //链接无效(不在礼包可兑换时间内)
        LinkRewardCountIsTop = 1008, //链接奖励次数达到上限
        LinkHadGetReward = 1014, //链接已获取奖励
        LinkGetReward = 0, //链接获取奖励(正常)
        CDKeyNotInvalid = 1001, //CDKey无效(禁止弹窗)
        CommunityLinkReward, //社区链接奖励
        LinkAgainActivate = 1007, //重复激活
    }

    public class CommunityLinkRewardData
    {
        public int LinkId;
        public string GiftCode;
        public LinkType LinkType;
        public CommunityPopupType CommunityPopupType;
        public List<RewardCommitData> commitRewardList;
        public List<CDKeyReward> CDKeyRewardList;
    }

    /// <summary>
    /// 社区链接奖励状态(仅表现层)
    /// </summary>
    public enum CommunityLinkRewardState
    {
        NotReceivedReward = 1, //未领取
        GetHadReceivedReward = 2, //已领取
    }

    public enum CommunityLinkClickType
    {
        Setting = 1, //设置页
        Shop = 2, //商店
        Community = 3, //社区弹窗
    }

    public enum LinkType
    {
        CommunityLink = 1, //社区链接
        CommunityGiftLink = 2, //礼包链接
        CommunityMailLink = 3, //社区邮件链接
    }
}