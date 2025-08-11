/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡册中单个卡组的相关信息 
 * @Date: 2024-10-18 10:10:29
 */

using fat.rawdata;

namespace FAT
{
    //卡册中单个卡组的相关信息
    public class CardGroupData
    {
        public int CardGroupId;     //卡组id
        public int BelongAlbumId;   //卡组所属卡册id
        public bool IsCollectAll = false;   //是否已收集完卡组中的所有卡片
        public bool IsRecReward = false;    //是否已领取本卡组的集齐奖励
        
        //获取本卡组对应的配置表数据
        public CardGroup GetConfig()
        {
            return Game.Manager.configMan.GetCardGroupConfig(CardGroupId);
        }
    }
}

