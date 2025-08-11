// ================================================
// File: MBWeeklyRaffleItem.cs
// Author: yueran.li
// Date: 2025/06/09 17:44:56 星期一
// Desc: 签到抽奖 主界面Item
// ================================================

using DG.Tweening;
using EL;
using FAT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MBWeeklyRaffleItem : MonoBehaviour
{
    private static readonly int MissHash = Animator.StringToHash("Miss");
    private static readonly int MrHash = Animator.StringToHash("M_R");
    private static readonly int RightHash = Animator.StringToHash("Right");
    private static readonly int PunchHash = Animator.StringToHash("Punch");
    private static readonly int HideHash = Animator.StringToHash("Hide");
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    private string day1 = "#SysComDesc1017";
    private string day2 = "#SysComDesc1018";
    private string day3 = "#SysComDesc1019";
    private string day4 = "#SysComDesc1020";
    private string day5 = "#SysComDesc1021";
    private string day6 = "#SysComDesc1022";
    private string day7 = "#SysComDesc1023";

    private UIImageState _bg;
    private TextMeshProUGUI _dayTxt;
    private TextMeshProUGUI _luckydayTxt;
    private RectMask2D _luckydayMask;
    private Animator _checkAni;
    private Animator _ani;
    private CanvasGroup buy;

    public float luckyTime = 1f;
    public AnimationCurve luckyCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float luckyInterval = 1f;
    [SerializeField] private GameObject luckyDay;
    [SerializeField] private GameObject luckyfx;
    [SerializeField] private GameObject tokenImg;
    public GameObject TokenImg => tokenImg;
    public int Day { get; private set; }
    private ActivityWeeklyRaffle _activity;
    private Sequence _luckySeq;


    public void InitOnPreOpen(int day, ActivityWeeklyRaffle activity)
    {
        RegisterComp();
        AddButton();

        this._activity = activity;
        this.Day = day;
    }

    private void RegisterComp()
    {
        _ani = transform.GetComponent<Animator>();
        transform.Access("check", out _checkAni);
        transform.Access("bg", out _bg);
        transform.Access("day", out _dayTxt);
        transform.Access("buy", out buy);
        transform.Access("luckyDay/show/mask", out _luckydayMask);
        transform.Access("luckyDay/show/mask/maskTxt", out _luckydayTxt);
    }

    private void AddButton()
    {
        transform.AddButton("buy", OnClickBuy).WithClickScale().FixPivot();
    }

    // 幸运日 文字扫光
    private void LuckyTween()
    {
        _luckySeq?.Kill();
        _luckySeq = DOTween.Sequence();

        // 设置初始位置
        _luckydayMask.rectTransform.localPosition = new Vector3(-100f, 0f, 0f);
        _luckydayTxt.rectTransform.localPosition = new Vector3(100f, 0f, 0f);

        // 创建遮罩和文字的动画
        var maskTween = _luckydayMask.rectTransform
            .DOLocalMoveX(150f, luckyTime) // 从-50移动到+50
            .SetEase(luckyCurve);

        var textTween = _luckydayTxt.rectTransform
            .DOLocalMoveX(-150f, luckyTime) // 从+50移动到-50
            .SetEase(luckyCurve);

        // 将动画添加到序列中
        _luckySeq.Append(maskTween)
            .Join(textTween).AppendInterval(luckyInterval); // 同时执行两个动画

        // 设置循环
        _luckySeq.SetLoops(-1, LoopType.Restart);
    }


    /// <summary>
    /// 设置状态 
    /// </summary>¬
    /// <param name="dayState"> 0:未来 1:今天 2:过去</param>
    /// <param name="signed"></param>
    /// <param name="controlCheck"></param>
    /// 根据传入的数据做表现 而不是直接获取逻辑层数据
    public void ResetState(int dayState, bool signed, bool controlCheck = true)
    {
        _dayTxt.SetText(I18N.FormatText("#SysComDesc1172", Day + 1));
        FontMaterialRes.Instance.GetFontMatResConf(dayState == 1 ? 17 : 10).ApplyFontMatResConfig(_dayTxt);

        _bg.Setup(dayState != 2 ? dayState : 0);

        var past = dayState == 2;
        var miss = past && !signed; // 漏签

        if (miss) // 漏签
        {
            PlayMiss();
            PlayCheckHide();
        }
        else if (signed) // 已经签到 & 过去
        {
            if (controlCheck)
            {
                PlayCheckIdle();
            }

            if (past)
            {
                PlaySigned();
            }
        }

        if (_activity.CheckIsRaffleDay(Day))
        {
            var pos = _dayTxt.GetComponent<RectTransform>().anchoredPosition;
            _dayTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x, 33);
            luckyDay.SetActive(true);
            luckyfx.SetActive(true);
            LuckyTween();
        }
    }

    // ui 表现切换到未签到
    public void ChangeToUnSign()
    {
        _bg.Setup(0);
        buy.alpha = 0f;
        PlayCheckHide();
    }

    // 点击补签
    private void OnClickBuy()
    {
        if (_activity.GetSignCount() >= _activity.GetConfigSignDayCount() || _activity.HasSign(Day) || Day > _activity.GetOffsetDay())
        {
            return;
        }

        UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyRaffleBuyToken, _activity, Day, false);
    }

    // 补签动画
    public void PlayRefill()
    {
        _ani.SetTrigger(MrHash);
    }

    public void PlayMiss()
    {
        _ani.SetTrigger(MissHash);
    }

    public void PlaySigned()
    {
        _ani.SetTrigger(RightHash);
    }

    public void PlayCheckPunch()
    {
        _checkAni.SetTrigger(PunchHash);
    }

    public void PlayCheckIdle()
    {
        _checkAni.SetTrigger(IdleHash);
    }

    public void PlayCheckHide()
    {
        _checkAni.SetTrigger(HideHash);
    }
}