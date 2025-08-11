/*
 * @Author: tang.yan
 * @Description: 集卡系统管理器-Debug相关方法
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/wz88ugv6graz2uq0#BQkOq
 * @Date: 2024-10-18 10:10:53
 */

using System;
using System.Collections.Generic;
using EL;

namespace FAT
{
    //集卡Debug相关方法
    public partial class CardMan
    {
        //Debug面板重置集卡活动数据
        public void DebugClearCardData()
        {
            _allCardRoundDataDict.Clear();
            CurCardActId = 0;
        }

        //打开抽卡模拟器
        public void OpenDrawCardDebugUI()
        {
            UIManager.Instance.CloseWindow(UIConfig.UIDebugPanelProMax);
            UIManager.Instance.OpenWindow(UIConfig.UIDebugDrawCard);
        }

        //debug面板添加指定数量的卡片
        public void DebugAddCard(int cardId, int count)
        {
            if (!CheckValid() || count == 0) return;
            var allCardData = GetCardRoundData()?.TryGetCardAlbumData()?.GetAllCardDataMap();
            if (allCardData == null || allCardData.Count < 1)
                return;
            if (allCardData.TryGetValue(cardId, out var cardData))
            {
                var isAdd = count > 0;
                var realCount = Math.Abs(count);
                for (var i = 0; i < realCount; i++)
                {
                    cardData.ChangeCardCount(isAdd);
                }
                Game.Manager.commonTipsMan.ShowClientTips($"Success! [{cardId}] current Num = {cardData.OwnCount}!");
            }
            else
            {
                Game.Manager.commonTipsMan.ShowClientTips($"[{cardId}] not belong this Card Album!");
            }
        }
        
        //设置固定星星库存
        public void DebugChangeFixedStarNum(int fixedStarNum)
        {
            var roundData = GetCardRoundData();
            if (roundData == null) return;
            if (fixedStarNum == 0) return;
            var isAdd = fixedStarNum > 0;
            roundData.DebugChangeFixedStarNum(Math.Abs(fixedStarNum), isAdd);
        }

        //重置卡片星星兑换cd
        public void DebugResetExchangeCd()
        {
            GetCardRoundData()?.DebugResetExchangeCd();
        }
        
        //重置每日赠卡次数
        public void DebugResetGiveCardNum()
        {
            CurGiveCardNum = 0;
            NextRefreshGiveCardTs = 0;
        }

        //打开面板支持自定义当前账号的facebook信息
        public void DebugSetFacebookInfo(string info)
        {
            var r = info.Split(",");
            var facebookId = r.GetElementEx(0, ArrayExt.OverflowBehaviour.Default);
            var name = r.GetElementEx(1, ArrayExt.OverflowBehaviour.Default);
            var avatarUrl = r.GetElementEx(2, ArrayExt.OverflowBehaviour.Default);
            //必须要传入facebookId和name, avatarUrl可以不传 会默认给一个随机的
            if (!string.IsNullOrEmpty(facebookId) && !string.IsNullOrEmpty(name))
            {
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    avatarUrl = _CheckAndGetAvatarUrl();
                }
                Platform.PlatformSDK.Instance.Adapter.profile = new(Platform.AccountLoginType.Facebook, facebookId, name, avatarUrl); 
                Game.Instance.AbortRestart("Change success! \nConfirm to Restart!", 0);
            }
        }

        //卡片交换流程中忽略检查facebook账号绑定
        public bool DebugIsIgnoreFbBind;
        public void DebugSetIgnoreFacebookBind()
        {
            DebugIsIgnoreFbBind = !DebugIsIgnoreFbBind;
        }

        //本次登录设置facebook好友id信息
        private static List<string> _friendIdList;
        public void DebugSetFacebookFriendIdInfo(string friendIdInfo)
        {
            var r = friendIdInfo.Split(",");
            if (r.Length < 1) return;
            _friendIdList ??= new List<string>();
            _friendIdList.Clear();
            for (int i = 0; i < r.Length; i++)
            {
                var id = r[i];
                _friendIdList.Add(id);
            }
            Game.Manager.commonTipsMan.ShowClientTips("Set friend id success! count = " + _friendIdList.Count);
            //顺便重置一下拉取cd
            _pullFriendInfoCd = -1;  
        }
        
        //debug时使用的随机url
        private static List<string> _avatarUrlList;
        private string _CheckAndGetAvatarUrl()
        {
            if (_avatarUrlList == null)
            {
                _avatarUrlList = new List<string>()
                {
                    "https://placekitten.com/100/100",
                    "https://placebear.com/100/100",
                    "https://loremflickr.com/100/100/animal",
                    "https://images.dog.ceo/breeds/hound-afghan/n02088094_1003.jpg",
                    "https://random-d.uk/api/100.jpg",
                    "https://loremflickr.com/100/100/bird",
                    "https://place-animal.com/100x100/animals",
                    "https://loremflickr.com/100/100/fox",
                    "https://loremflickr.com/100/100/fish",
                    "https://placekitten.com/100/101",
                };
            }
            return _avatarUrlList.RandomChooseByWeight();
        }
    }
}


