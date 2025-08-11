/*
 *@Author:chaoran.zhang
 *@Desc:
 *@Created Time:2024.03.07 星期四 19:25:04
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FAT
{
    public class FlyableItemSlice
    {
        public List<RectTransform> Transforms = new();
        public int CurIdx;
        public int SplitNum;
        public int ID;
        public int Amount;
        public RewardCommitData Reward;

        public Action<FlyableItemSlice> OnCollectedPartially;
        public Action<FlyableItemSlice> OnCollectedWholly;

        public Vector3 WorldFrom { get; set; }
        public Vector3 WorldTo { get; set; }
        public float Size { get; set; }
        public FlyReason Reason { get; set; }
        public FlyStyle Style { get; set; }
        public FlyType FlyType { get; set; }

        public void Init(int id, int count, Vector3 from, Vector3 to, FlyStyle style, FlyType type, float size, int split = 0)
        {
            ID = id;
            Amount = count;
            CurIdx = 0;
            SplitNum = split == 0 ? UIFlyFactory.CalcClipNum(ID, Amount) : split;
            FlyType = type;
            WorldFrom = from;
            WorldTo = to;
            Style = style;
            _InitReason();
            //多个图标时统一走分散逻辑，大小不能自定义
            if (size != 0 && Reason == FlyReason.None)
                Size = size;
            else
                _InitSize();
        }

        public void Reset()
        {
            Reason = FlyReason.None;
            Style = FlyStyle.Common;

            WorldFrom = Vector3.zero;
            WorldTo = Vector3.zero;
            Size = 0f;

            Reward = null;
            OnCollectedPartially = null;
            OnCollectedWholly = null;
        }

        private void _InitReason()
        {
            if (FlyType == FlyType.TreasureBag)
                Reason = FlyReason.None;
            else if (Game.Manager.objectMan.IsType(ID, ObjConfigType.Coin) || ID == Constant.kMergeEnergyObjId ||
                     FlyType == FlyType.EventScore || FlyType == FlyType.DecorateToken || FlyType == FlyType.RaceToken
                     || ID == DailyEvent.TokenId || FlyType == FlyType.EndlessToken || FlyType == FlyType.EndlessThreeToken)
                Reason = FlyReason.CoinChange;
            else if (ID == Constant.kMergeExpObjId)
                Reason = FlyReason.ExpChange;
            else
                Reason = FlyReason.None;
        }

        private void _InitSize()
        {
            switch (Style)
            {
                case FlyStyle.Cost:
                    {
                        Size = Reason == FlyReason.CoinChange ? 76f : 136f;
                        break;
                    }
                case FlyStyle.Reward:
                    {
                        Size = Reason switch
                        {
                            FlyReason.CoinChange => 85f,
                            FlyReason.ExpChange => 100f,
                            _ => 136f
                        };
                        Size = FlyType switch
                        {
                            FlyType.EventScore or FlyType.OrderLikeToken => 70f,
                            FlyType.FightBoardMonster or FlyType.FightBoardTreasure => 85f,
                            FlyType.Inventory => 136f,
                            _ => Size,
                        };
                        break;
                    }
            }
        }
    }
}
