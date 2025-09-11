/*
 * @Author: yanfuxing
 * @Date: 2025-07-22 15:40:09
 */
using System;
using DG.Tweening;
using EL;
using UnityEngine;

namespace FAT
{
    public static class RankingUIUtility
    {
        //升级动画
        private const string ANIM_PROMOTE = "TurntableTrans_Escalate";
        //升级动画空闲
        private const string ANIM_PROMOTE_IDLE = "TurntableTrans_EscalateIdle";
        //降级动画
        private const string ANIM_DEMOTE = "TurntableTrans_Demoted";
        //降级Idle
        private const string ANIM_DEMOTE_IDLE = "TurntableTrans_DemotedIdle";

        /// <summary>
        /// 刷新转盘
        /// </summary>
        /// <param name="num"></param>
        /// <param name="parentTrans"></param>
        public static void RefreshTurntableByNum(int num, Transform parentTrans, bool isChasing = false)
        {
            if (parentTrans == null) { return; }
            for (int i = 0; i < parentTrans.childCount; i++)
            {
                var go = parentTrans.GetChild(i).gameObject;
                if (go != null)
                {
                    var anim = go.GetComponent<Animator>();
                    if (anim != null)
                    {
                        var animName = GetAnimNameBySlotNum(num, i, isChasing);
                        anim.Play(animName);
                    }
                }
            }
        }

        /// <summary>
        /// 获取指定槽位动画名称
        /// </summary>
        /// <param name="slotNum"></param>
        /// <param name="index"></param>
        /// <param name="isChasing"></param>
        /// <returns></returns>
        private static string GetAnimNameBySlotNum(int slotNum, int index, bool isChasing)
        {
            if (index == slotNum - 1)
            {
                if (isChasing)
                {
                    //主动激活
                    return ANIM_PROMOTE;
                }
                else
                {
                    //主动激活Idle
                    return ANIM_PROMOTE_IDLE;
                }

            }
            else if (index < slotNum - 1)
            {
                //激活Idle
                return ANIM_PROMOTE_IDLE;
            }
            else
            {
                if (isChasing)
                {
                    if (index >= slotNum)
                    {
                        //降级Idle
                        return ANIM_DEMOTE_IDLE;
                    }
                    else
                    {
                        //降级
                        return ANIM_DEMOTE;
                    }
                }
                else
                {
                    //降级Idle
                    return ANIM_DEMOTE_IDLE;
                }
            }
        }

        /// <summary>
        /// 指针指向指定槽位
        /// </summary>
        /// <param name="slotIndex">槽位</param>
        /// <param name="parentTrans">转盘父节点</param>
        /// <param name="arrowTrans">指针</param>
        /// <param name="duration">动画时长</param>
        public static void PointerToSlot(int slotNum, Transform parentTrans, Transform arrowTrans, float duration = 0.6f, Action action = null)
        {
            Transform targetPoint = GetTargetSlotTrans(slotNum, parentTrans);
            if (targetPoint == null)
            {
                Debug.LogError("找不到对应的目标点：" + slotNum);
                return;
            }
            float targetZ = targetPoint.localEulerAngles.z;
            float finalAngle = targetZ;
            if (slotNum == 1) { action?.Invoke(); }
            if (slotNum > 1) arrowTrans.DOLocalRotate(new Vector3(0, 0, finalAngle), duration).SetEase(UIFlyConfig.Instance.curveRankingUp).OnComplete(() => action?.Invoke());
            else arrowTrans.DOLocalRotate(new Vector3(0, 0, finalAngle), 0.6f).SetEase(UIFlyConfig.Instance.curveRankingDown);
        }

        /// <summary>
        /// 获取指定槽位点
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <param name="parentTrans"></param>
        /// <returns></returns>
        private static Transform GetTargetSlotTrans(int slotIndex, Transform parentTrans)
        {
            slotIndex = GetMaxSlotIndexBySlotNum(slotIndex);
            var slotTrans = parentTrans.Find(slotIndex.ToString());
            if (slotTrans == null) return null;
            return slotTrans.Find("Point");
        }

        /// <summary>
        /// 播放指针摆动动画
        /// </summary>
        /// <param name="arrowTrans"></param>
        /// <param name="isPlay"></param>
        public static void PlayArrowAnim(Transform arrowTrans, Transform parentTrans, bool isPlay = false)
        {
            if (arrowTrans == null) return;
            var anim = arrowTrans.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = isPlay;
            }
        }

        /// <summary>
        /// 获取指定槽位动画名称
        /// </summary>
        /// <param name="slotNum"></param>
        /// <returns></returns>
        public static string GetAnimNameBySlotNum(int slotNum)
        {
            var act = (ActivityMultiplierRanking)Game.Manager.activity.LookupAny(fat.rawdata.EventType.MultiplierRanking);
            if (act == null)
            {
                DebugEx.Info("ActivityMultiplierRanking is null");
                return null;
            }
            //转盘动画
            var animList = act.conf.MultiplierAnime;
            int index = slotNum >= 5 ? animList.Count - 1 : slotNum - 1;
            if (animList.Count > 0 && index < animList.Count)
            {
                return animList[index];
            }
            return null;
        }

        /// <summary>
        /// 设置拖尾特效
        /// </summary>
        /// <param name="slotNum"></param>
        /// <param name="parentTrans"></param>
        public static void SetTrailFxBySlotNum(int slotNum, Transform parentTrans)
        {
            if (parentTrans == null) { return; }
            var act = (ActivityMultiplierRanking)Game.Manager.activity.LookupAny(fat.rawdata.EventType.MultiplierRanking);
            if (act == null)
            {
                DebugEx.Info("ActivityMultiplierRanking is null");
                return;
            }
            //拖尾特效
            string fxAnim = null;
            var fxAnimList = act.conf.MultiplierSfx;
            int index = slotNum - 1 >= fxAnimList.Count ? fxAnimList.Count - 1 : slotNum - 1;
            if (fxAnimList.Count > 0 && index < fxAnimList.Count)
            {
                fxAnim = fxAnimList[index];
                if (string.IsNullOrEmpty(fxAnim))
                {
                    return;
                }
            }
            for (int i = 0; i < parentTrans.childCount; i++)
            {
                var go = parentTrans.GetChild(i).gameObject;
                if (go != null)
                {
                    go.SetActive(go.name == fxAnim);
                }
            }
        }

        /// <summary>
        /// 获取最大槽位索引:5为最大槽位
        /// </summary>
        /// <param name="slotNum"></param>
        /// <returns></returns>
        public static int GetMaxSlotIndexBySlotNum(int slotNum)
        {
            return slotNum >= 5 ? 5 : slotNum;
        }
    }

    public enum RankingOpenType
    {
        Main,
        End,
    }
}