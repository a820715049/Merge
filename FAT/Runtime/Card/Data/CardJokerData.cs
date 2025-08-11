/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡册中拥有的万能卡片数据类 
 * @Date: 2024-10-18 10:10:08
 */

using fat.rawdata;
using EL;
using fat.gamekitdata;

namespace FAT
{
    //卡册中拥有的万能卡片数据类
    public class CardJokerData
    {
        public int CardJokerId;     //万能卡配置id
        public int BelongRoundId;   //万能卡所属卡册轮次数据id
        public long ExpireTs;       //过期时间戳单位秒
        public int IsGoldCard;    //0代表白卡万能卡 1代表金卡万能卡
        private bool _isLockExpire = false;   //是否锁定过期 (用于玩家正在界面中操作时)
        private int _curSelectCardId = 0;    //界面操作中当前万能卡对应选择的卡片id

        public int GetCurSelectCardId()
        {
            return _curSelectCardId;
        }

        public void SetCurSelectCardId(int cardId)
        {
            if (cardId < 0)
                return;
            if (_curSelectCardId != cardId)
            {
                _curSelectCardId = cardId;
                MessageCenter.Get<MSG.GAME_CARD_JOKER_SELECT>().Dispatch();
            }
        }
        
        //获取本万能卡对应的配置表数据
        public ObjCardJoker GetConfig()
        {
            return Game.Manager.objectMan.GetCardJokerConfig(CardJokerId);
        }
        
        public ObjBasic GetObjBasicConfig()
        {
            return Game.Manager.objectMan.GetBasicConfig(CardJokerId);
        }

        //设置是否锁定过期 此值只在卡牌活动期间内才有效 非活动期间万能卡会直接走结算回收逻辑
        public void SetLockExpire(bool isLock)
        {
            _isLockExpire = isLock;
        }

        //当没有锁定过期且当前时间大于过期时间时 认为万能卡过期
        public bool CheckIsExpire()
        {
            return !_isLockExpire && Game.Instance.GetTimestampSeconds() > ExpireTs;
        }
        
        //根据存档初始化数据
        public void SetData(CardJokerInfo info)
        {
            ExpireTs = info.JokerExpireTs;
        }
        
        public CardJokerInfo FillData()
        {
            var info = new CardJokerInfo();
            info.JokerId = CardJokerId;
            info.JokerExpireTs = ExpireTs;
            return info;
        }
    }
}

