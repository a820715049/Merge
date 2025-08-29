using System;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Ease = UnityEngine.UI.Extensions.EasingCore.Ease;

public class MBBPProgress : MonoBehaviour
{
    private class ProgressInfo
    {
        public int afterAnimCurProgress;
        public int beforeAnimMaxProgress;

        public ProgressInfo(int afterAnimCurProgress, int beforeAnimMaxProgress)
        {
            this.afterAnimCurProgress = afterAnimCurProgress;
            this.beforeAnimMaxProgress = beforeAnimMaxProgress;
        }
    }

    private TextMeshProUGUI _progressTxt;
    private TextMeshProUGUI _lvTxt;
    private RectTransform _mask;
    private UIImageRes lvIcon;
    private UIImageRes boxIcon;
    private UIImageRes expIcon;

    private readonly Queue<ProgressInfo> _progressQueue = new();
    private BPActivity _activity;

    private void Awake()
    {
        RegisterComp();
        AddButton();
    }

    private void RegisterComp()
    {
        transform.Access("progress/text", out _progressTxt);
        transform.Access("LvNode/Lv", out _lvTxt);
        transform.Access("progress/mask", out _mask);
        transform.Access("LvNode/LvIcon", out lvIcon);
        transform.Access("LvNode/boxIcon", out boxIcon);
        transform.Access("ExpNode/exp", out expIcon);
    }

    private void AddButton()
    {
        transform.AddButton("LvNode/boxIcon", OnClickBoxIcon);
    }

    private void OnEnable()
    {
        MessageCenter.Get<UI_BP_MILESTONE_CHANGE>().AddListener(EnqueueProgressInfo);
    }

    private void OnDisable()
    {
        MessageCenter.Get<UI_BP_MILESTONE_CHANGE>().RemoveListener(EnqueueProgressInfo);
    }

    public void Init(BPActivity activity)
    {
        this._activity = activity;

        var curLvIndex = _activity.GetCurMilestoneLevel();
        var curLvConfig = _activity.GetMilestoneInfo(curLvIndex);
        _lvTxt.SetText($"{curLvIndex + 2}");

        var token = fat.conf.Data.GetObjBasic(_activity.ConfD.ScoreId);
        expIcon.SetImage(token.Icon);

        lvIcon.gameObject.SetActive(true);
        boxIcon.gameObject.SetActive(false);
        _lvTxt.gameObject.SetActive(true);

        // 当前等级超过上限
        if (curLvConfig == null)
        {
            // 使用循环奖励的配置
            var cycleId = _activity.GetCycleMilestoneId();
            curLvConfig = Game.Manager.configMan.GetBpMilestoneConfig(cycleId);
        }

        var to = CalProgressRight(_activity.GetCurMilestoneNum(), curLvIndex);
        _mask.offsetMax = new Vector2(to, 0);

        _progressTxt.SetText($"{_activity.GetCurMilestoneNum()}/{curLvConfig.Score}");

        SetMilestoneCycle();
    }

    private void SetMilestoneCycle()
    {
        //检查当前是否处于循环奖励阶段
        if (!_activity.CheckMilestoneCycle())
        {
            return;
        }

        // 此时处于循环宝箱进度 所以登记为最后一级
        var curLvIndex = _activity.GetCurMilestoneLevel();
        var config = _activity.GetMilestoneInfo(curLvIndex);
        _lvTxt.gameObject.SetActive(false);
        lvIcon.gameObject.SetActive(false);


        // 已领取次数达到最大值
        if (config == null)
        {
            _progressTxt.SetText(I18N.Text("#SysComDesc723"));
            boxIcon.gameObject.SetActive(false);
        }
        else
        {
            // 宝箱icon
            boxIcon.gameObject.SetActive(true);
        }
    }

    private void OnClickBoxIcon()
    {
        //检查当前是否处于循环奖励阶段
        if (!_activity.CheckMilestoneCycle())
        {
            return;
        }

        var ui = UIManager.Instance.TryGetUI(UIConfig.UIBPMain);
        if (ui != null && ui is UIBPMain main)
        {
            // 定位到奖金银行
            main.FancyScroll.JumpToBottom();

            // 打开循环奖励气泡
            MessageCenter.Get<UI_BP_OPEN_CYCLE_TIP>().Dispatch();
        }
    }

    private void EnqueueProgressInfo(int afterAnimCurProgress, int beforeAnimMaxProgress)
    {
        _progressQueue.Enqueue(new ProgressInfo(afterAnimCurProgress, beforeAnimMaxProgress));
    }

    public void PlayProgressAnim()
    {
        if (_progressQueue.Count > 0)
        {
            var progressInfo = _progressQueue.Dequeue();
            var to = CalProgressRight(progressInfo, out var lvUp);
            ProgressAnim(to, () =>
            {
                // 如果升级 此时的CurLv为最新等级
                var curLvIndex = _activity.GetCurMilestoneLevel();
                var mileStoneConfig = _activity.GetMilestoneInfo(curLvIndex);

                // 当前等级超过上限
                if (mileStoneConfig != null)
                {
                    // 播放动画完成后
                    _progressTxt.text = $"{progressInfo.afterAnimCurProgress}/{mileStoneConfig.Score}";
                }

                // 滚动结束 如果是循环奖励 设置进度状态
                SetMilestoneCycle();

                if (lvUp)
                {
                    _lvTxt.SetText($"{curLvIndex + 2}");

                    // 经验值动画 
                    ClearProgress(); // 清空经验值
                    ProgressAnim(CalProgressRight(_activity.GetCurMilestoneNum(), curLvIndex), () => { });

                    // 通知滚动Scroll
                    var ui = UIManager.Instance.TryGetUI(UIConfig.UIBPMain);
                    if (ui != null && ui is UIBPMain main)
                    {
                        main.FancyScroll.BpLvUpScrollTo(curLvIndex + 1, 0.33f,
                            Ease.Linear, 0.5f, () => { });
                    }
                }
                else
                {
                    // 从队列中取下一个继续播放
                    PlayProgressAnim();
                }
            });
        }
    }

    private float CalProgressRight(int curExp, int lvIndex)
    {
        Canvas.ForceUpdateCanvases();

        // 获取mask相对于canvas的缩放
        var maskRect = _mask.parent.GetComponent<RectTransform>();

        var max = maskRect.rect.width;
        var milestoneInfo = _activity.GetMilestoneInfo(lvIndex);

        // 当前等级超过上限
        if (milestoneInfo == null)
        {
            return max;
        }

        return max * ((curExp * 1f / milestoneInfo.Score * 1f));
    }

    private float CalProgressRight(ProgressInfo info, out bool lvUp)
    {
        // 获取mask相对于canvas的缩放
        var maskRect = _mask.parent.GetComponent<RectTransform>();
        var max = maskRect.rect.width;

        if (info.beforeAnimMaxProgress != -1)
        {
            // 超过当前最大经验值
            lvUp = true;
            return max;
        }

        Canvas.ForceUpdateCanvases();
        lvUp = false;

        var curLvIndex = _activity.GetCurMilestoneLevel();
        var milestoneInfo = _activity.GetMilestoneInfo(curLvIndex);

        // 当前等级超过上限
        if (milestoneInfo == null)
        {
            return max;
        }
        else
        {
            // 正常经验值动画 不跨级
            return max * ((info.afterAnimCurProgress * 1f / milestoneInfo.Score));
        }
    }

    private void ClearProgress()
    {
        _mask.offsetMax = new Vector2(0, 0);
    }

    private void ProgressAnim(float to, Action onComplete = null)
    {
        // 进度条动画只能单向改变
        if (to <= _mask.offsetMax.x)
        {
            onComplete?.Invoke();
            return;
        }

        DOTween.To(() => _mask.offsetMax, x => _mask.offsetMax = x,
                new Vector2(to, 0), 1f)
            .OnComplete(() => { onComplete?.Invoke(); });
    }

    public void Release()
    {
        ClearProgress();
        _progressQueue.Clear();
    }
}