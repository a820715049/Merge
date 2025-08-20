/**
 * @Author: zhangpengjian
 * @Date: 2024/8/28 20:18:15
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/28 20:18:15
 * Description: 挖沙工具类
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EL;
using DG.Tweening;

namespace FAT
{
    public class UIDiggingUtility
    {
        public enum SoundEffect
        {
            DiggingDotGrowth,
            DiggingEntryFeedback,
            Digging, //挖格子
            DiggingShow,
            DiggingCollect,
            DiggingLevelReward,
            Digging_dig2,
        }

        public static bool IsLoading { get; private set; }
        public static bool willShowGiftShop { get; private set; }
        private static GameObject goBlock;
        private static ActivityDigging actInst;
        private static bool isEnterFromMerge;
        public static Vector3 boardOriginPos;

        public static void PlaySound(SoundEffect se)
        {
            Game.Manager.audioMan.TriggerSound(se.ToString());
        }

        public static bool IsEventActive()
        {
            return actInst != null && actInst.Active;
        }

        public static void TryEndActivity()
        {
            if (!IsLoading && !IsBlocking() && UIManager.Instance.IsIdleIn(actInst.Res.ActiveR))
            {
                SetBlock(true);
                actInst.TryExchangeExpireKey();
            }
        }

        #region key shop

        // 弹窗需求发起时的frameCount
        private static int giftShopRequestFrameCount;

        public static void RegisterToShowGiftShop()
        {
            willShowGiftShop = true;
            giftShopRequestFrameCount = Time.frameCount;
        }

        public static void TryResolveGiftShopRequest(int frameCount)
        {
            // 需要避免错误的resolve了不属于自己触发的request
            // 不能resolve未来的request
            if (frameCount < giftShopRequestFrameCount)
                return;
            if (willShowGiftShop)
            {
                if (TryGetEventInst(out var act))
                {
                    if (act.GetKeyNum() > 0)
                    {
                        willShowGiftShop = false;
                        return;
                    }

                    TryOpenGiftShop();
                }
            }
        }

        public static bool PackValid()
        {
            TryGetEventInst(out var act);
            if (act == null || !act.Active || !act.PackValid) return false;
            return true;
        }

        public static void OnKeyNotEnough(Vector3 worldPos)
        {
            if (PackValid())
            {
                TryOpenGiftShop();
            }
            else
            {
                var c = Game.Manager.objectMan.GetTokenConfig(actInst.diggingConfig.TokenId);
                Game.Manager.commonTipsMan.ShowPopTips(fat.rawdata.Toast.DiggingNoShovel, worldPos, c.SpriteName);
            }
        }

        public static void TryOpenGiftShop()
        {
            if (!IsLoading && UIManager.Instance.IsIdleIn(actInst.Res.ActiveR))
            {
                TryGetEventInst(out var act);
                if (act == null || !act.Active) return;
                if (!act.PackValid)
                {
                    actInst.HelpRes.ActiveR.Open();
                    return;
                }
                act.PopupGift.OpenPopup();
                willShowGiftShop = false;
            }
        }

        #endregion

        #region block

        public static void InstallBlock(GameObject go)
        {
            goBlock = go;
        }

        public static void SetBlock(bool b)
        {
            if (goBlock != null)
                goBlock.SetActive(b);
        }

        public static bool IsBlocking()
        {
            if (goBlock != null)
                return goBlock.activeSelf;
            return false;
        }

        #endregion

        public static bool TryGetEventInst(out ActivityDigging act)
        {
            act = actInst;
            if (act == null)
            {
                DebugEx.Error($"[digging] activity inst not found");
            }

            return act != null;
        }

        public static IEnumerator CoOnCellDigging(ActivityDigging.DiggingCellState state, Transform t,
            List<RewardCommitData> rewards, fat.rawdata.EventDiggingLevel level, Action callback = null)
        {
            if (state != ActivityDigging.DiggingCellState.GetRandom && state != ActivityDigging.DiggingCellState.Fail)
            {
                SetBlock(true);
                yield return new WaitForSeconds(0.2f);
            }

            // 开箱子动画时的frameCount
            int openFrameCount = Time.frameCount;
            if (state == ActivityDigging.DiggingCellState.Fail)
            {
                float _ = 0;
                DOTween.To(() => _, x => _ = x, 1, 0.5f)
                    .OnComplete(() => { TryResolveGiftShopRequest(openFrameCount); });
            }
            else if (state == ActivityDigging.DiggingCellState.GetRandom)
            {
                yield return new WaitForSeconds(0.5f);
                var completedCount = 0;
                var totalRewards = rewards.Count;
                
                foreach (var item in rewards)
                {
                    UIFlyUtility.FlyReward(item, t.position, () => 
                    {
                        if (item.rewardId == actInst.diggingConfig.TokenId)
                        {
                            callback?.Invoke();
                        }
                        
                        // 当所有奖励都飞完后再清空列表
                        completedCount++;
                        if (completedCount >= totalRewards)
                        {
                            rewards.Clear();
                        }
                    }, 142f);
                }
            }
            else if (state == ActivityDigging.DiggingCellState.Get || state == ActivityDigging.DiggingCellState.BombAndGet)
            {
               //处理飞一个海马
               
            }
            else if (state == ActivityDigging.DiggingCellState.GetAll || state == ActivityDigging.DiggingCellState.BombAndGetAll)
            {
                //海马飞
                yield return new WaitForSeconds(1.5f);
                //进度条动画
                MessageCenter.Get<MSG.DIGGING_PROGRESS_REFRESH>().Dispatch();
                yield return new WaitForSeconds(1f);
                //弹出关卡奖励
                UIManager.Instance.OpenWindow(UIConfig.UIDiggingLevelReward, t.position, rewards, level);
            }

            SetBlock(false);
        }

        private static void _RegisterActivityInstance()
        {
            actInst = Game.Manager.activity.LookupAny(fat.rawdata.EventType.Digging) as ActivityDigging;
        }

        private static void _UnregisterActivityInstance()
        {
            actInst = null;
        }

        public static void EnterActivity()
        {
            _RegisterActivityInstance();
            if (!TryGetEventInst(out var act))
                return;
            if (!IsEventActive())
                return;
            if (!act.HasNextRound())
                return;
            UIManager.Instance.ChangeIdleActionState(false);
            Game.Manager.screenPopup.Block(delay_: true);
            Game.Instance.StartCoroutineGlobal(_CoLoading(_MergeToActivity));
        }

        public static void LeaveActivity()
        {
            willShowGiftShop = false;

            Game.Instance.StartCoroutineGlobal(_CoLoading(_ActivityToMerge));
        }

        public static void MoveToNextLevel()
        {
            // 活动过期 / 直接结束
            if (!IsEventActive())
            {
                LeaveActivity();
                return;
            }

            Game.Instance.StartCoroutineGlobal(_CoLoading(_MovoToLevel));
        }

        //正常情况哪里进挖沙 回到哪里
        //现新增：没钥匙时 从meta进 而回棋盘
        public static void SetEnterFrom(bool isBoard)
        {
            isEnterFromMerge = isBoard;
        }

        private static void _MovoToLevel()
        {
            actInst.Res.ActiveR.Close();
            actInst.Res.ActiveR.Open();
        }

        private static void _MergeToActivity()
        {
            isEnterFromMerge = UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain);

            UIConfig.UIStatus.Close();

            if (isEnterFromMerge)
            {
                UIConfig.UIMergeBoardMain.Close();
            }
            else
            {
                Game.Manager.mapSceneMan.Exit();
            }
            actInst.Res.ActiveR.Open();
        }

        private static void _ActivityToMerge()
        {
            actInst.Res.ActiveR.Close();
            UIConfig.UIStatus.Open();

            if (isEnterFromMerge)
            {
                UIConfig.UIMergeBoardMain.Open();
            }
            else
            {
                Game.Manager.mapSceneMan.Enter(null);
            }

            if (!TryGetEventInst(out var act)) return;

            if (!act.HasNextRound())
                act.TryExchangeExpireKey();
            UIManager.Instance.ChangeIdleActionState(true);
            Game.Manager.screenPopup.Block(false, false);
            _UnregisterActivityInstance();
        }

        private static IEnumerator _CoLoading(Action afterFadeIn = null, Action afterFadeOut = null)
        {
            IsLoading = true;

            var waitFadeInEnd = new SimpleAsyncTask();
            var waitFadeOutEnd = new SimpleAsyncTask();
            var waitLoadingJobFinish = new SimpleAsyncTask();
            //复用寻宝loading音效
            Game.Manager.audioMan.TriggerSound("UnderseaTreasure");
            
            actInst.LoadingRes.ActiveR.Open(waitLoadingJobFinish, waitFadeInEnd, waitFadeOutEnd);

            yield return waitFadeInEnd;

            afterFadeIn?.Invoke();

            // TODO
            waitLoadingJobFinish.ResolveTaskSuccess();

            yield return waitFadeOutEnd;

            afterFadeOut?.Invoke();

            IsLoading = false;
        }
    }
}