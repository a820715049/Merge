/*
 *@Author:chaoran.zhang
 *@Desc:
 *@Created Time:2024.03.07 星期四 19:25:13
 */

using System;
using System.Collections.Generic;
using DG.Tweening;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public static class UIFlyFactory
    {
        private static readonly List<(FlyType ft, Func<Vector3> handler)> FlyPosResolverList = new();

        private static readonly List<(FlyType ft, GameObject obj)> FlyObjectList = new();

        #region Num and Type

        public static int CalcClipNum(int id, int num)
        {
            var ft = ResolveFlyType(id);
            return _CalcClipNum(ft, num);
        }

        public static FlyType ResolveFlyType(int rewardId)
        {
            var mgr = Game.Manager.objectMan;
            var ft = FlyType.None;
            if (rewardId == Constant.kMergeExpObjId)
            {
                ft = FlyType.Exp;
            }
            else if (rewardId == Constant.kMergeEnergyObjId)
            {
                ft = FlyType.Energy;
            }
            else if (mgr.IsType(rewardId, ObjConfigType.ActivityToken))
            {
                var tokenConf = Game.Manager.objectMan.GetTokenConfig(rewardId);
                if (tokenConf.Feature == FeatureEntry.FeatureDem)
                    ft = FlyType.EventCoin;
                else if (tokenConf.Feature == FeatureEntry.FeatureScore)
                    ft = FlyType.EventScore;
                else if (tokenConf.Feature == FeatureEntry.FeatureTreasure)
                    ft = FlyType.TreasureKey;
                else if (tokenConf.Feature == FeatureEntry.FeatureDecorate)
                    ft = FlyType.DecorateToken;
                else if (tokenConf.Feature == FeatureEntry.FeatureRace)
                    ft = FlyType.RaceToken;
                else if (tokenConf.Feature == FeatureEntry.FeatureDigging)
                    ft = FlyType.DiggingShovel;
                else if (tokenConf.Feature == FeatureEntry.FeatureRedeem)
                    ft = FlyType.RedeemCoindEntry;
                else if (tokenConf.Feature == FeatureEntry.FeaturePachinko)
                    ft = FlyType.Pachinko;
                else if (tokenConf.Feature == FeatureEntry.FeatureEndlessPack)
                    ft = FlyType.EndlessToken;
                else if (tokenConf.Feature == FeatureEntry.FeatureEndlessThreePack)
                    ft = FlyType.EndlessThreeToken;
                else if (tokenConf.Feature == FeatureEntry.FeatureDiscountPack)
                    ft = FlyType.PackDiscountToken;
                else if (tokenConf.Feature == FeatureEntry.FeatureOrderLike)
                    ft = FlyType.OrderLikeToken;
                else if (tokenConf.Feature == FeatureEntry.FeatureClawOrder)
                    ft = FlyType.ClawOrderToken;
                else if (tokenConf.Feature == FeatureEntry.FeatureWeeklyTask)
                    ft = FlyType.WeeklyTaskEntry;
                else if (tokenConf.Feature == FeatureEntry.FeatureFarmBoard)
                    ft = FlyType.FarmToken;
                else if (tokenConf.Feature == FeatureEntry.FeatureCastleMilestone)
                    ft = FlyType.CastleToken;
                else if (tokenConf.Feature == FeatureEntry.FeatureGuess)
                {
                    var acti = (ActivityGuess)Game.Manager.activity.LookupAny(fat.rawdata.EventType.Guess);
                    ft = acti switch
                    {
                        null => FlyType.GuessToken,//fail-safe
                        _ when acti.confD.MilestoneScoreId == tokenConf.Id => FlyType.GuessMilestone,
                        _ => FlyType.GuessToken,
                    };
                }
                else if (tokenConf.Feature == FeatureEntry.FeatureMine)
                {
                    var acti = (MineBoardActivity)Game.Manager.activity.LookupAny(fat.rawdata.EventType.Mine);
                    ft = acti switch
                    {
                        null => FlyType.MineToken,//fail-safe
                        _ when acti.IsMileStoneToken(rewardId) => FlyType.MineScore,
                        _ => FlyType.MineToken,
                    };
                }
                else if (tokenConf.Feature == FeatureEntry.FeatureScoreDuel)
                {
                    var acti = (ActivityDuel)Game.Manager.activity.LookupAny(fat.rawdata.EventType.ScoreDuel);
                    ft = acti switch
                    {
                        null => FlyType.DuelToken,//fail-safe
                        _ when acti.MilestoneTokenId == tokenConf.Id => FlyType.DuelMilestone,
                        _ => FlyType.DuelToken,
                    };
                }
                else if (tokenConf.Feature == FeatureEntry.FeatureBp)
                {
                    ft = FlyType.BPExp;
                }
            }
            else if (mgr.IsType(rewardId, ObjConfigType.Coin))
            {
                if (Game.Manager.coinMan.GetCoinTypeById(rewardId) == CoinType.MergeCoin)
                    ft = FlyType.Coin;
                else if (Game.Manager.coinMan.GetCoinTypeById(rewardId) == CoinType.Gem)
                    ft = FlyType.Gem;
                else
                    ft = FlyType.Inventory;
            }
            else if (mgr.IsType(rewardId, ObjConfigType.CardPack))
            {
                //卡包必须飞往主棋盘中的奖励箱，如果在活动棋盘获得卡包，则应该飞向主棋盘入口
                var curWorld = Game.Manager.mergeBoardMan.activeWorld;
                if (curWorld != null && (curWorld.activeBoard?.boardId ?? 0) == Constant.MainBoardId)
                {
                    ft = FlyType.MergeItemFlyTarget;
                }
                //当前没有活跃的棋盘时 认为在meta场景
                else if (curWorld == null)
                {
                    ft = FlyType.MergeItemFlyTarget;
                }
                else
                {
                    ft = FlyType.FlyToMainBoard;
                }
            }
            else if (mgr.IsType(rewardId, ObjConfigType.MergeItem))
            {
                var conf = mgr.GetMergeItemConfig(rewardId);
                if (conf != null)
                {
                    var curWorld = Game.Manager.mergeBoardMan.activeWorld;
                    //如果棋子配置了必须飞往主棋盘，则依照目前是否真的在主棋盘中来判断最终flyType
                    if (conf.BoardId == Constant.MainBoardId)
                    {
                        if (curWorld != null && (curWorld.activeBoard?.boardId ?? 0) == Constant.MainBoardId)
                        {
                            ft = FlyType.MergeItemFlyTarget;
                        }
                        //当前没有活跃的棋盘时 认为在meta场景
                        else if (curWorld == null)
                        {
                            ft = FlyType.MergeItemFlyTarget;
                        }
                        else
                        {
                            ft = FlyType.FlyToMainBoard;
                        }
                    }
                    //如果棋子不要求必须飞往主棋盘，则再检查是否必须飞往某个活动棋盘
                    else
                    {
                        //检查棋子是否是迷你棋盘主链条棋子且目前存在活跃棋盘，符合条件的话就往迷你棋盘的活动入口飞
                        if (curWorld != null && Game.Manager.miniBoardMultiMan.CheckIsMiniBoardItem(rewardId))
                        {
                            ft = FlyType.MiniBoardMulti;
                        }
                        else
                        {
                            ft = FlyType.MergeItemFlyTarget;
                        }
                    }
                }
                else
                {
                    ft = FlyType.MergeItemFlyTarget;
                }
            }
            else if (mgr.IsType(rewardId, ObjConfigType.Wallpaper))
            {
                ft = FlyType.Wallpaper;
            }
            else if (mgr.IsType(rewardId, ObjConfigType.ProfileDeco))
            {
                ft = FlyType.ProfileDeco;
            }
            else if (mgr.IsType(rewardId, ObjConfigType.CardJoker))
            {
                ft = FlyType.CardJoker;
            }

            if (rewardId == (Game.Manager.dailyEvent?.milestone?.RequireCoinId ?? -1)) return FlyType.MilestoneToken;

            return ft;
        }

        private static int _CalcClipNum(FlyType ft, int total)
        {
            var rankList = _matchList(ft);
            var rank = _matchRank(total, rankList);
            return UIFlyConfig.ClipNumList[rank];
        }

        private static List<int> _matchList(FlyType ft)
        {
            var rankList = ft switch
            {
                FlyType.Exp => UIFlyConfig.ExpRank,
                FlyType.Energy => UIFlyConfig.EnergyRank,
                FlyType.Coin => UIFlyConfig.CoinRank,
                FlyType.Gem => UIFlyConfig.GemRank,
                _ => UIFlyConfig.DefaultRank
            };
            return rankList;
        }

        private static int _matchRank(int total, List<int> list)
        {
            var rank = list.Count - 1;
            for (var i = 0; i < list.Count; ++i)
            {
                if (total > list[i]) continue;

                rank = i;
                break;
            }

            if (rank >= UIFlyConfig.ClipNumList.Count)
                rank = UIFlyConfig.ClipNumList.Count - 1;
            return rank;
        }

        #endregion

        #region api

        public static Vector3 ResolveFlyTarget(FlyType ft)
        {
            var idx = FlyPosResolverList.FindLastIndex(x => x.ft == ft);
            return idx >= 0 ? FlyPosResolverList[idx].handler() : Vector3.zero;
        }

        public static void RegisterFlyTarget(FlyType flyType, Func<Vector3> func)
        {
            var idx = FlyPosResolverList.FindIndex(x => x.ft == flyType && x.handler == func);
            if (idx < 0) FlyPosResolverList.Add((flyType, func));
        }

        public static void UnregisterFlyTarget(FlyType flyType, Func<Vector3> func)
        {
            var idx = FlyPosResolverList.FindIndex(x => x.ft == flyType && x.handler == func);
            if (idx >= 0) FlyPosResolverList.RemoveAt(idx);
        }

        public static void RegisterFlyObj(FlyType ft, GameObject obj)
        {
            var idx = FlyObjectList.FindIndex(x => x.ft == ft && x.obj == obj);
            FlyObjectList.Add((ft, obj));
        }

        public static void UnRegisterFlyObj(FlyType ft, GameObject obj)
        {
            var idx = FlyObjectList.FindIndex(x => x.ft == ft && x.obj == obj);
            FlyObjectList.RemoveAt(idx);
        }

        public static GameObject TryGetObj(FlyType ft)
        {
            var idx = FlyObjectList.FindLastIndex(x => x.ft == ft);
            return idx >= 0 ? FlyObjectList[idx].obj : null;
        }

        #endregion

        #region 拒绝采样

        /// <summary>
        /// 在矩形区域内随机一个点
        /// </summary>
        public static Vector2 InRect(Rect rect)
        {
            var pos = new Vector2();
            pos.x = UnityEngine.Random.Range(0, rect.width) + rect.x;
            pos.y = UnityEngine.Random.Range(0, rect.height) + rect.y;
            return pos;
        }

        /// <summary>
        /// 在矩形区域内随机一个点，并判断是否符合条件，不符合再次随机
        /// </summary>
        /// <param name="rect">范围</param>
        /// <param name="judgeFunc">判断条件</param>
        /// <param name="defaultValue">失败返回坐标</param>
        /// <param name="maxRandomTime">最大尝试次数,当maxRandomTime小于0时,将无限尝试直到要求被满足</param>
        /// <returns></returns>
        public static Vector2 RejectSampling(Rect rect, Func<Vector2, bool> judgeFunc, Vector2 defaultValue,
            int maxRandomTime)
        {
            for (; maxRandomTime != 0; maxRandomTime--)
            {
                var pos = InRect(rect);
                if (judgeFunc(pos)) return pos;
            }

            return defaultValue;
        }

        /// <summary>
        /// 椭圆随机结构体
        /// </summary>
        public struct Ellipse
        {
            public Vector2 Center;
            public float Width;
            public float Height;
            public float MinWidth;
            public float MinHeight;

            public Ellipse(Vector2 center, float width, float height, float minWidth = 0, float minHeight = 0)
            {
                Center = center;
                Width = width;
                Height = height;
                MinWidth = minWidth;
                MinHeight = minHeight;
            }

            public Rect OutsideRect()
            {
                var size = new Vector2(Width, Height);
                return new Rect(Center - size, size * 2);
            }

            public bool Inside(Vector2 pos)
            {
                pos -= Center;
                return pos.x * pos.x / (Width * Width) + pos.y * pos.y / (Height * Height) < 1 &&
                       pos.x * pos.x / (MinWidth * MinWidth) + pos.y * pos.y / (MinHeight * MinHeight) >= 1;
            }
        }

        #endregion


        public static bool GetFlyTarget(FlyType ft, out Vector3 pos)
        {
            pos = Vector3.zero;
            var idx = FlyPosResolverList.FindLastIndex(x => x.ft == ft);

            if (idx < 0)
                return false;

            pos = FlyPosResolverList[idx].handler();
            return true;
        }

        public static bool CheckNeedFlyIcon(int id)
        {
            return !Game.Manager.objectMan.IsType(id, ObjConfigType.RandomBox)
                   && !Game.Manager.objectMan.IsType(id, ObjConfigType.CardJoker);
        }

        public static void CreateStraightTween(Sequence seq, Transform trans, Vector3 to, float duration = 0)
        {
            if (duration == 0)
                duration = UIFlyConfig.Instance.durationFly;
            seq.Append(DOTween.To(() => trans.position, x => trans.position = x, to, duration)
                .SetEase(Ease.InCubic));
        }

        public static void CreateStopTween(Sequence seq, Transform trans, float duration)
        {
            seq.Append(trans.DOScale(Vector3.one, duration));
        }

        public static void CreateScatterTween(Sequence seq, Transform trans, float time = 0)
        {
            var ellipse = new Ellipse(new Vector2(trans.position.x, trans.position.y),
                UIFlyConfig.Instance.scatterWidth,
                UIFlyConfig.Instance.scatterHeight);
            Func<Vector2, bool> judgeFunc = ellipse.Inside;
            var ellipseOutsideRect = ellipse.OutsideRect();
            var pos = RejectSampling(ellipseOutsideRect, judgeFunc, Vector2.zero, 100);
            var offset = new Vector3(pos.x, pos.y, trans.position.z);
            if (time == 0) time = UIFlyConfig.Instance.durationScatter;
            seq.Append(DOTween.To(() => trans.position, x => trans.position = x, offset,
                time));
            seq.Join(DOTween
                .To(() => trans.localScale, x => trans.localScale = x,
                    Vector3.one * UIFlyConfig.Instance.scaleScatterEnd,
                    time));
        }

        public static void CreateScatterTween(Sequence seq, Transform trans, float width, float height,
            float minWidth = 0, float minHeight = 0)
        {
            var ellipse = new Ellipse(new Vector2(trans.position.x, trans.position.y),
                width,
                height, minWidth, minHeight);
            Func<Vector2, bool> judgeFunc = ellipse.Inside;
            var ellipseOutsideRect = ellipse.OutsideRect();
            var pos = RejectSampling(ellipseOutsideRect, judgeFunc, Vector2.zero, 100);
            var offset = new Vector3(pos.x, pos.y, trans.position.z);

            seq.Append(DOTween.To(() => trans.position, x => trans.position = x, offset,
                UIFlyConfig.Instance.durationScatter));
            seq.Join(DOTween
                .To(() => trans.localScale, x => trans.localScale = x,
                    Vector3.one * UIFlyConfig.Instance.scaleScatterEnd,
                    UIFlyConfig.Instance.durationScatter));
        }

        public static void CreateCurveTween(Sequence seq, Transform trans, Vector3 to)
        {
            seq.Append(DOTween.To(() => trans.position, x => trans.position = x, to, UIFlyConfig.Instance.durationFly)
                .SetEase(UIFlyConfig.Instance.curveHorizontal).SetOptions(AxisConstraint.X));
            seq.Join(DOTween.To(() => trans.position, x => trans.position = x, to, UIFlyConfig.Instance.durationFly)
                .SetEase(UIFlyConfig.Instance.curveVertical).SetOptions(AxisConstraint.Y));
        }

        public static void CreateElasticTween(Sequence seq, Transform trans, bool append, float end, float duration,
            AnimationCurve curve)
        {
            if (append)
                seq.Append(trans.DOScale(end, duration).SetEase(curve));
            else
                seq.Join(trans.DOScale(end, duration).SetEase(curve));
        }

        public static void CreateShowRewardTween(Sequence seq, Transform trans, float end, float duration)
        {
            var center = UIUtility.GetScreenCenterWorldPosForUICanvas();
            seq.Append(trans.DOMove(center, duration).SetEase(Ease.Linear));
            seq.Join(trans.DOScale(end, duration).SetEase(Ease.Linear));
        }

        public static void CreateEndShowRewardTween(Sequence seq, Transform trans, Vector3 pos, float end,
            float duration)
        {
            seq.Append(trans.DOMove(pos, duration).SetEase(Ease.InCubic));
            seq.Join(trans.DOScale(end, duration).SetEase(Ease.Linear));
        }
    }
}
