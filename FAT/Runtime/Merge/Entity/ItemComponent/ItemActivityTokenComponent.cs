/*
 * @Author: tang.yan
 * @Description: 用于处理活动token挂载到棋子上的组件 设计上希望通过一个组件就可以完成各个活动往棋子上挂载token的需求
 * @Doc: https://centurygames.feishu.cn/wiki/FCr6wUVEZiwH77kZn6pcjmTxn1g
 * @Date: 2025-09-15 14:09:28
 */

using System.Collections.Generic;
using fat.gamekitdata;
using static FAT.RecordStateHelper;

namespace FAT.Merge
{
    //设计上希望通过一个组件就可以完成各个活动往棋子上挂载token的需求
    public class ItemActivityTokenComponent : ItemComponentBase
    {
        //写存档
        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            var data = new ComActivityToken();
            var paramList = data.ParamList;
            //参数顺序不可变动
            var index = 0;
            //BL
            _SerializeBL(ref index, paramList);
            itemData.ComActivityToken = data;
        }

        //读存档
        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            var data = itemData.ComActivityToken;
            if (data == null) 
                return;
            var paramList = data.ParamList;
            //参数顺序不可变动
            var index = 0;
            //BL
            _DeserializeBL(ref index, paramList);
        }
        
        #region 活动token相关参数 - 棋子左下角

        //左下角是否可以显示icon
        public bool CanShow_BL => ActivityId_BL > 0 && TokenId_BL > 0;
        public int ActivityId_BL { get; private set; }
        public int TokenId_BL { get; private set; }
        public int TokenNum_BL { get; private set; }

        public void SetActivityInfo_BL(int activityId, int tokenId, int tokenNum)
        {
            ActivityId_BL = activityId;
            TokenId_BL = tokenId;
            TokenNum_BL = tokenNum;
        }

        //左下角活动积分用完后清理
        public void ClearActivityInfo_BL()
        {
            ActivityId_BL = 0;
            TokenId_BL = 0;
            TokenNum_BL = 0;
            //触发item的组件刷新事件 进而触发界面刷新
            item.OnComponentChanged(this);
        }
        
        private void _SerializeBL(ref int index, IList<AnyState> paramList)
        {
            paramList.Add(ToRecord(index++, ActivityId_BL));
            paramList.Add(ToRecord(index++, TokenId_BL));
            paramList.Add(ToRecord(index++, TokenNum_BL));
        }

        private void _DeserializeBL(ref int index, IList<AnyState> paramList)
        {
            ActivityId_BL = ReadInt(index++, paramList);
            TokenId_BL = ReadInt(index++, paramList);
            TokenNum_BL = ReadInt(index++, paramList);
        }

        #endregion
    }
}