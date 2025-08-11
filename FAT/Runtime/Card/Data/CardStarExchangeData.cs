/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡片兑换功能中每个兑换条目的相关数据类 
 * @Date: 2024-10-18 10:10:34
 */

using fat.rawdata;

namespace FAT
{
    //卡片兑换功能中每个兑换条目的相关数据类
    public class CardStarExchangeData
    {
        public int StarExchangeId;
        public long NextCanExchangeTime;    //下次可兑换时间

        //获取本兑换条目对应的配置表数据
        public StarExchange GetConfig()
        {
            return Game.Manager.configMan.GetStarExchangeConfig(StarExchangeId);
        }

        public ObjBasic GetBasicConfig()
        {
            return Game.Manager.objectMan.GetBasicConfig(GetConfig()?.Reward ?? 0);
        }

        public void RefreshCd()
        {
            var curTime = Game.Instance.GetTimestampSeconds();
            var cd = GetConfig()?.WaitTime ?? 0;
            NextCanExchangeTime = curTime + cd;
        }

        //判断目前是否在cd中
        public bool IsInCd()
        {
            var time = Game.Instance.GetTimestampSeconds();
            return time < NextCanExchangeTime;
        }
    }
}

