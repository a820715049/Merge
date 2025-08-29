// ================================================
// File: MBWeeklyRaffleDrawSlot.cs
// Author: yueran.li
// Date: 2025/06/09 17:44:15 星期一
// Desc: 签到抽奖 抽奖界面Item
// ================================================

using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using FAT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class MBWeeklyRaffleDrawSlot : MonoBehaviour
{
    private static readonly int Special = Animator.StringToHash("Special");
    private static readonly int Common = Animator.StringToHash("Common");
    private static readonly int Close = Animator.StringToHash("Close");
    private static readonly int Open = Animator.StringToHash("Open");

    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject close;
    [SerializeField] private GameObject open;
    [SerializeField] private Transform rewardPos;
    [SerializeField] private NonDrawingGraphic block;

    [SerializeField] private GameObject specialFx;
    [SerializeField] private GameObject commonFx;

    private UIImageRes _icon;
    private Button btn;


    public int RewardId { get; set; } = -1;
    public int BoxId { get; set; } = -1;
    private ActivityWeeklyRaffle _activity;

    private void OnEnable()
    {
        if (!block)
        {
            var ui = UIManager.Instance.TryGetUI(UIConfig.UIActivityWeeklyRaffleDraw);
            if (ui && ui is UIActivityWeeklyRaffleDrawn draw)
            {
                block = draw.Block;
            }
        }
    }

    public void InitOnPreOpen(ActivityWeeklyRaffle act, int boxID)
    {
        transform.AddButton("box/btn", OnClickSlot).WithClickScale().FixPivot();

        this._activity = act;
        this.BoxId = boxID;
        InitState();
    }

    private void InitState()
    {
        var hasOpen = _activity.HasOpen(BoxId);
        if (hasOpen)
        {
            _animator.Play("UIMBRaffleDrawSlot_Open");
        }
    }

    // 点击抽奖
    private void OnClickSlot()
    {
        _activity.TryRaffle(BoxId, out var ret);

        if (ret == ActivityWeeklyRaffle.CheckRet.TokenNotEnough)
        {
            UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyRaffleBuyToken, _activity, -1, true);
        }
    }

    public void PlayBoxOpen(PoolMapping.Ref<List<RewardCommitData>> container, int rewardId)
    {
        RewardId = rewardId;
        var jackpot = _activity.GetConfigJackPot().Id == RewardId;

        void cb()
        {
            foreach (var reward in container.obj)
            {
                UIFlyUtility.FlyReward(reward, rewardPos.position);
            }

            container.Free();
        }

        StartCoroutine(CoOpen(jackpot, cb));
    }

    // 盒子打开动画
    private IEnumerator CoOpen(bool jackpot, Action cb = null)
    {
        // 让盒子位于最前方 确保特效不被遮挡
        int lastIndex = transform.parent.parent.childCount - 1;
        transform.parent.SetSiblingIndex(lastIndex);

        SetBlock(true);


        if (jackpot)
        {
            PlayJackpotOpen();

            yield return new WaitForSeconds(0.3f);
            // 大奖音效
            Game.Manager.audioMan.TriggerSound("WeeklyRaffleSpecial");
            yield return new WaitForSeconds(0.7f);
        }
        else
        {
            PlayCommonOpen();
            yield return new WaitForSeconds(0.5f);
            // 小奖音效
            Game.Manager.audioMan.TriggerSound("WeeklyRaffleOpen");
        }

        yield return new WaitForSeconds(0.5f);

        if (cb != null)
        {
            cb.Invoke();
        }

        yield return new WaitForSeconds(1f);

        SetBlock(false);


        // 抽奖成功后 如果抽奖全部抽完 结束活动
        _activity.TryEndActivity();
    }

    // 盒子关闭状态 跳动
    public void PlayCloseAnim()
    {
        _animator.SetBool(Close, true);
    }


    // 盒子打开状态 静止
    public void PlayOpened()
    {
        _animator.SetTrigger(Open);
    }

    // 普通奖励打开动画
    public void PlayCommonOpen()
    {
        _animator.SetTrigger(Common);
    }

    // 特殊奖励打开动画 
    public void PlayJackpotOpen()
    {
        _animator.SetTrigger(Special);
    }

    public void SetBlock(bool value)
    {
        block.raycastTarget = value;
    }

    private bool IsBlock => block.raycastTarget;
}