/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡组中单个卡片的相关信息 
 * @Date: 2024-10-18 10:10:15
 */

using System.Collections.Generic;
using fat.rawdata;
using EL;
using fat.gamekitdata;

namespace FAT
{
    //卡组中单个卡片的相关信息
    public class CardData
    {
        public int CardId;          //卡片Id
        public int BelongAlbumId;   //卡组所属卡册id
        public int BelongGroupId;   //卡片所属卡组id
        public int LimitId;         //本次活动此卡片的限制条件信息(CardLimit.id) 只在活动开始时随机一次并存档
        public bool IsLimitGoldAble;//卡片在当前LimitId的情况下是否可以被ObjCardPack.goldPayPass加成,需要与GetConfig().IsGoldable一起做且逻辑搭配使用
        //EnergyPassNum和PayPassNum 实际计算时 二者满足其中一个的限制值 即可获得对应卡片
        public int EnergyPassNum;   //根据LimitId确定的本卡片在本次活动中要想获得的话，需要满足的能量限制值，-1代表无论能量是多少，都无法满足条件
        public int PayPassNum;      //根据LimitId确定的本卡片在本次活动中要想获得的话，需要满足的充值金额限制值 单位美分，-1代表无论累充金额是多少，都无法满足条件
        public int MinEryPassNum;   //根据LimitId确定的本卡片在本次活动中要想获得的话，需要满足的最低能量限制值，-1代表无论能量是多少，都无法满足条件
        public int DayPassNum;      //根据LimitId确定的本卡片在本次活动中要想获得的话，需要满足的当前活动已经开了多少天的限制值 单位天，-1代表无论活动开了多少天，都无法满足条件

        public bool IsOwn = false;  //此卡片是否已拥有
        public bool IsSee = false;  //此卡片是否在获得后被查看过,用于红点显示 需要与IsOwn结合使用 默认false,查看完此卡后此值设为true
        public int OwnCount;        //此卡片已拥有的张数 默认为0

        //获取本卡片对应的配置表数据
        public ObjCard GetConfig()
        {
            return Game.Manager.objectMan.GetCardConfig(CardId);
        }

        public ObjBasic GetObjBasicConfig()
        {
            return Game.Manager.objectMan.GetBasicConfig(CardId);
        }

        //根据存档初始化数据
        public void SetData(CardInfo info)
        {
            LimitId = info.LimitId;
            IsOwn = info.IsOwn;
            IsSee = info.IsNew;
            OwnCount = info.OwnCount;
        }

        public CardInfo FillData()
        {
            var info = new CardInfo();
            info.LimitId = LimitId;
            info.IsOwn = IsOwn;
            info.IsNew = IsSee;
            info.OwnCount = OwnCount;
            return info;
        }

        //修改目前已拥有的卡片张数，同时设置卡片拥有状态
        //isAdd传false时代表和其他玩家换卡了(后续功能)
        public void ChangeCardCount(bool isAdd = true)
        {
            if (isAdd)
            {
                IsOwn = true;
                OwnCount++;
                MessageCenter.Get<MSG.GAME_CARD_ADD>().Dispatch();
            }
            else if (OwnCount > 1)  //只有自己拥有大于1张卡时才允许卡片减少(换卡)
            {
                OwnCount--;
            }
        }

        //批量减少卡片数量  减少时打点
        public void ReduceCardCount(int reduceNum, int reduceType, int reduceParam)
        {
            for (var i = 0; i < reduceNum; i++)
            {
                ChangeCardCount(false);
            }
            Game.Manager.cardMan.TrackCardReduction(CardId, reduceNum, reduceType, reduceParam);
        }

        //获取当前卡片对应的星星总数量 计算方式为重复卡数量*卡片星级 
        //调用此方法获得的值认为是星星的虚拟库存
        public int GetVirtualStarNum()
        {
            if (!IsOwn || OwnCount <= 1) return 0;
            var star = GetConfig()?.Star ?? 1;
            return (OwnCount - 1) * star;
        }

        //检查指定卡片的红点状态
        public bool CheckIsNew()
        {
            return IsOwn && !IsSee;
        }

        //查看完此卡后 将IsSee设为true
        public void SetCardSee()
        {
            IsSee = true;
        }

        public void RandomLimitId(List<int> usedIdList, List<int> randomList)
        {
            //将本张卡配置的LimitInfoId中没有被使用过的Id加入randomList
            var limitInfo = GetConfig().LimitInfo;
            foreach (var id in limitInfo)
            {
                if (!usedIdList.Contains(id))
                {
                    randomList.Add(id);
                }
            }
            var resultId = randomList.RandomChooseByWeight();
            if (resultId <= 0)
            {
                DebugEx.FormatError("[CardMan.CardData.RandomLimitId : ] random LimitInfoId Fail! CardId = {0}, resultId = {1}, LimitInfo = {2}",
                    CardId, resultId, limitInfo);
            }
            else
            {
                LimitId = resultId;
                usedIdList.Add(resultId);
            }
            randomList.Clear();
        }

        //根据LimitId和tempId确定本卡片的限制信息
        public void RefreshPassInfo(int tempId)
        {
            //找不到相关配置时 默认设置为-1
            EnergyPassNum = -1;
            PayPassNum = -1;
            MinEryPassNum = -1;
            DayPassNum = -1;
            var cardLimitConfig = Game.Manager.configMan.GetCardLimitConfig(LimitId);
            if (cardLimitConfig == null) return;
            if (cardLimitConfig.ErgPassInfo.TryGetValue(tempId, out var energyNum))
                EnergyPassNum = energyNum;
            if (cardLimitConfig.PayPassInfo.TryGetValue(tempId, out var payNum))
                PayPassNum = payNum;
            if (cardLimitConfig.MinPassInfo.TryGetValue(tempId, out var minPassNum))
                MinEryPassNum = minPassNum;
            if (cardLimitConfig.DayPassInfo.TryGetValue(tempId, out var dayPassNum))
                DayPassNum = dayPassNum;
            IsLimitGoldAble = cardLimitConfig.IsGoldable;
        }

        //抽卡流程中 根据卡片自身情况 决定被分到哪个组
        public int CheckBelongGroup(ulong totalEnergy, int totalIap, int goldPayPass, int payRate, long curTS, long actStartTs)
        {
            var config = GetConfig();
            if (config == null)
                return 0;
            bool isGold = config.IsGold;
            if (IsOwn)
            {
                return isGold ? 2 : 1;
            }
            if (EnergyPassNum == -1 && PayPassNum == -1 && (MinEryPassNum == -1 || DayPassNum == -1))
            {
                DebugEx.FormatError("[CardData.CheckBelongGroup]: card pass num error! cardId = {0}", CardId);
                return 0;
            }
            bool canGoldPass = config.IsGoldable && IsLimitGoldAble;    //搭配使用才能决断出是否可以被加成
            bool isPassEnergy = CheckIsPassEnergy(isGold, canGoldPass, totalEnergy, totalIap, goldPayPass, payRate);
            bool isPassPay = CheckIsPassPay(isGold, canGoldPass, totalIap, goldPayPass);
            //每张卡片现在有3中解锁方式
            //1、能量达标  2、充值达标
            if (isPassEnergy || isPassPay)
            {
                return isGold ? 4 : 3;
            }
            //3、若前2个都没达标则再检查最小体力和活动天数是否都达标
            else
            {
                var isPassMinEnergy = CheckIsPassMinEnergy(totalEnergy);
                var isPassDay = CheckIsPassDay(curTS, actStartTs);
                if (isPassMinEnergy && isPassDay)   //二者必须同时达标
                {
                    return isGold ? 4 : 3;
                }
            }
            return 0;
        }

        public bool CheckIsPassEnergy(bool isGold, bool canGoldPass, ulong totalEnergy, int totalIap, int goldPayPass, int payRate)
        {
            //如果是金卡且canGoldPass为true 则说明此卡可以被goldPayPass加成 否则只看totalEnergy + (totalIap * payRate)
            bool isPassEnergy = (isGold && canGoldPass)
                ? (EnergyPassNum != -1 && (ulong)EnergyPassNum <= totalEnergy + (ulong)((totalIap + goldPayPass) * payRate / 100))
                : (EnergyPassNum != -1 && (ulong)EnergyPassNum <= totalEnergy + (ulong)(totalIap * payRate / 100));
            return isPassEnergy;
        }

        public bool CheckIsPassPay(bool isGold, bool canGoldPass, int totalIap, int goldPayPass)
        {
            //如果是金卡且canGoldPass为true 则说明此卡可以被goldPayPass加成 否则只看totalIap
            bool isPassPay = (isGold && canGoldPass)
                ? (PayPassNum != -1 && PayPassNum <= (totalIap + goldPayPass))
                : (PayPassNum != -1 && PayPassNum <= totalIap);
            return isPassPay;
        }

        public bool CheckIsPassMinEnergy(ulong totalEnergy)
        {
            return MinEryPassNum != -1 && (ulong)MinEryPassNum <= totalEnergy;
        }

        public bool CheckIsPassDay(long curTS, long actStartTs)
        {
            if (DayPassNum == -1)
                return false;
            //计算当前活动已经开了多少天 从1开始
            var curDay = curTS >= actStartTs ? (int)((curTS - actStartTs) / Constant.kSecondsPerDay) + 1 : 0;
            return DayPassNum <= curDay;
        }
    }
}

