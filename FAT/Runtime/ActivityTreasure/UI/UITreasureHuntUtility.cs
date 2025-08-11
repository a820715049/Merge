/*
 * @Author: qun.chao
 * @Date: 2024-04-18 12:14:59
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EL;
using Config;
using DG.Tweening;

namespace FAT
{
    public class UITreasureHuntUtility
    {
        public enum State
        {
            None,
            Born,
            Idle,
            Open,
            Dead,
        }

        public enum SoundEffect
        {
            TreasureProgressGrowth,
            TreasureGetKey, // 开宝箱音效和获得钥匙音效用同一个
            TreasureDotGrowth,
            TreasureSpawn,
            TreasureEmpty,
            TreasurePrize,
            TreasureGrandPrize,
            TreasureBag,
            TreasureEntryFeedback,
            PandaBoxDown,
            PandaOpenBox,
        }

        public static bool IsLoading { get; private set; }
        public static Vector3 levelRewardTarget { get; private set; }
        public static Vector3 progressRewardTarget { get; private set; }
        public static List<Config.RewardConfig> tempLevelRewards = new();
        public static List<Config.RewardConfig> tempProgressRewards = new();
        public static bool willShowGiftShop { get; private set; }

        private static Dictionary<string, string> resTable = new();
        private static List<GameObject> efxResList = new();
        private static string efxPoolKey = "treasure_efx_empty";
        private static GameObject goBlock;
        private static GameObject effectRoot;
        private static float openDelay;

        // 活动全程不应改变实例
        private static ActivityTreasure actInst;

        public static void RegisterFlyTarget_LevelReward(Vector3 pos)
        {
            levelRewardTarget = pos;
        }

        public static void RegisterFlyTarget_ProgressReward(Vector3 pos)
        {
            progressRewardTarget = pos;
        }

        public static void PlaySound(SoundEffect se)
        {
            Game.Manager.audioMan.TriggerSound(se.ToString());
        }

        public static bool IsEventActive()
        {
            return actInst != null && actInst.Active;
        }

        public static void TryForceLeaveActivity()
        {
            if (!IsLoading && !IsBlocking() && UIManager.Instance.IsIdleIn(actInst.Res.ActiveR))
            {
                SetBlock(true);
                var content = I18N.Text("#SysComDesc273");
                Game.Manager.commonTipsMan.ShowMessageTips(content, LeaveActivity, LeaveActivity, true);
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
                Game.Manager.commonTipsMan.ShowPopTips(fat.rawdata.Toast.TreasureNoKey, worldPos);
            }
        }

        public static void TryOpenGiftShop()
        {
            if (!IsLoading && UIManager.Instance.IsIdleIn(actInst.Res.ActiveR))
            {
                TryGetEventInst(out var act);
                if (act == null || !act.Active || !act.PackValid) return;
                UIManager.Instance.OpenWindow(act.PopupGift.PopupRes, act);
                willShowGiftShop = false;
            }
        }

        #endregion

        #region msg

        public static void NotifyTreasureFlyFeedback(FlyType ft)
        {
            MessageCenter.Get<MSG.UI_TREASURE_REWARD_FLY_FEEDBACK>().Dispatch(ft);
        }

        public static void NotifyTreasureProgressFeedback()
        {
            MessageCenter.Get<MSG.UI_TREASURE_FLY_FEEDBACK>().Dispatch();
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

        public static void FLyTempReward(Vector3 from, RewardConfig reward)
        {
            if (!TryGetEventInst(out var act)) return;

            FlyType ft;
            if (reward.Id == act.ConfD.RequireCoinId)
            {
                // 钥匙
                ft = FlyType.TreasureKey;
            }
            else
            {
                // 其他
                ft = FlyType.TreasureBag;
            }

            UIFlyFactory.GetFlyTarget(ft, out var to);
            UIFlyUtility.FlyCustom(reward.Id, 1, from, to, FlyStyle.Reward, ft,
                () => UITreasureHuntUtility.NotifyTreasureFlyFeedback(ft));
        }

        public static bool TryGetEventInst(out ActivityTreasure act)
        {
            act = actInst;
            if (act == null)
            {
                DebugEx.Error($"[treasurehunt] activity inst not found");
            }

            return act != null;
        }

        public static void InstallRes(UITreasureHuntMain.TreasureRes[] prefabs)
        {
            resTable.Clear();

            for (var i = 0; i < prefabs.Length; i++)
            {
                var res = prefabs[i];
                if (res != null && !string.IsNullOrEmpty(res.name) && res.prefab != null)
                {
                    var poolKey = $"th_treasure_{i}_{actInst?.ConfD.TreasureTheme}";
                    GameObjectPoolManager.Instance.PreparePool(poolKey, res.prefab);
                    resTable.Add(res.name, poolKey);
                }
            }
        }

        public static void InstallEffectRes(GameObject prefab, GameObject root)
        {
            if (prefab != null)
            {
                effectRoot = root;
                GameObjectPoolManager.Instance.PreparePool(efxPoolKey, prefab);
            }
        }

        public static void InstallOpenDelay(float d)
        {
            openDelay = d;
        }

        public static GameObject CreateTreasure(string treasureType)
        {
            if (resTable.TryGetValue(treasureType, out var poolKey))
            {
                return GameObjectPoolManager.Instance.CreateObject(poolKey);
            }

            return null;
        }

        public static void ReleaseTreasure(string treasureType, GameObject obj)
        {
            if (resTable.TryGetValue(treasureType, out var poolKey))
            {
                GameObjectPoolManager.Instance.ReleaseObject(poolKey, obj);
            }
        }

        public static void ReleaseEfxObj()
        {
            foreach (var item in efxResList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(efxPoolKey, item);
            }
            efxResList.Clear();
        }

        public static IEnumerator CoOnTreasureOpened(ActivityTreasure.TreasureBoxState state, Vector3 worldPos,
            List<RewardConfig> rewards)
        {
            UITreasureHuntUtility.SetBlock(true);

            yield return new WaitForSeconds(0.2f);
            if (openDelay != 0)
            {
                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.PandaOpenBox);
            }
            else
            {
                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureGetKey);
            }

            yield return new WaitForSeconds(1.3f);
            if (openDelay != 0)
            {
                yield return new WaitForSeconds(openDelay);
            }
            // 开箱子动画时的frameCount
            int openFrameCount = Time.frameCount;
            bool hasBonusToken = false;
            if (state == ActivityTreasure.TreasureBoxState.Empty)
            {
                if (effectRoot != null)
                {
                    var obj = GameObjectPoolManager.Instance.CreateObject(efxPoolKey);
                    obj.transform.SetParent(effectRoot.transform);
                    obj.transform.position = worldPos;
                    obj.gameObject.SetActive(false);
                    obj.gameObject.SetActive(true);
                    efxResList.Add(obj);
                }
                // 飘字
                Game.Manager.commonTipsMan.ShowPopTips(fat.rawdata.Toast.TreasureEmpty, worldPos);
                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureEmpty);
                float _ = 0;
                DOTween.To(() => _, x => _ = x, 1, 0.5f)
                    .OnComplete(() => { TryResolveGiftShopRequest(openFrameCount); });
            }
            else if (state == ActivityTreasure.TreasureBoxState.NormalReward)
            {
                var center = UIUtility.GetScreenCenterWorldPosForUICanvas();
                // 常规奖励 单独物品奖励
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (!TryGetEventInst(out var act))
                        continue;

                    var reward = rewards[i];
                    FlyType ft;
                    if (reward.Id == act.ConfD.RequireCoinId)
                    {
                        // 钥匙
                        ft = FlyType.TreasureKey;
                    }
                    else if (reward.Id == act.ConfD.BonusToken)
                    {
                        // 空箱token
                        ft = FlyType.TreasureBonusToken;
                        hasBonusToken = true;
                    }
                    else
                    {
                        // 其他
                        ft = FlyType.TreasureBag;
                    }

                    UIFlyFactory.GetFlyTarget(ft, out var to);
                    UIFlyUtility.FlyCustom(reward.Id, reward.Count, worldPos, to, FlyStyle.Show, ft, () =>
                    {
                        UITreasureHuntUtility.NotifyTreasureFlyFeedback(ft);
                        if (ft != FlyType.TreasureBonusToken)
                        {
                            // 飞完奖励尝试触发钥匙购买
                            UITreasureHuntUtility.TryResolveGiftShopRequest(openFrameCount);
                        }

                    }, null, 200f);
                    if (i > 0)
                    {
                        yield return new WaitForSeconds(0.2f * i);
                    }
                }

                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasurePrize);
            }
            else if (state == ActivityTreasure.TreasureBoxState.Treasure)
            {
                // 关卡奖励
                actInst.LevelRewardRes.ActiveR.Open(worldPos, rewards);
                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureGrandPrize);
            }
            if (actInst.ConfD.BonusToken == 0 || !hasBonusToken)
            {
                UITreasureHuntUtility.SetBlock(false);
            }
        }

        private static void _RegisterActivityInstance()
        {
            actInst = Game.Manager.activity.LookupAny(fat.rawdata.EventType.Treasure) as ActivityTreasure;
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
            if (!act.HasNextLevelGroup())
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

        private static void _MovoToLevel()
        {
            actInst.Res.ActiveR.Close();
            actInst.Res.ActiveR.Open();
        }

        // 区分活动入口来自 合成 or meta
        private static bool isEnterFromMerge;

        // TODO: 活动是否一定在主棋盘
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
            if (!act.IsLeaveHuntGuided())
            {
                UIManager.Instance.RegisterIdleAction("ui_idle_treasure_help", 100, () =>
                    {
                        act.HelpRes.ActiveR.Open();
                        act.SetLeaveGuideHasPopup();
                    }
                );
            }

            if (!act.HasNextLevelGroup() || act.Countdown <= 0)
                act.TryExchangeExpireTreasureKey();
            act.TryClaimTempBagReward();
            UIManager.Instance.ChangeIdleActionState(true);
            Game.Manager.screenPopup.Block(delay_: false);

            _UnregisterActivityInstance();
        }

        private static IEnumerator _CoLoading(Action afterFadeIn = null, Action afterFadeOut = null)
        {
            IsLoading = true;

            var waitFadeInEnd = new SimpleAsyncTask();
            var waitFadeOutEnd = new SimpleAsyncTask();
            var waitLoadingJobFinish = new SimpleAsyncTask();

            // 音效是配合了loading屏的开和关的
            Game.Manager.audioMan.TriggerSound("UnderseaTreasure");

            actInst.LoadingRes.ActiveR.Open(waitLoadingJobFinish, waitFadeInEnd,waitFadeOutEnd);

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