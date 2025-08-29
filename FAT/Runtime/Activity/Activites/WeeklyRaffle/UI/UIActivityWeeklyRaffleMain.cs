// ================================================
// File: UIActivityWeeklyRaffleMain.cs
// Author: yueran.li
// Date: 2025/06/09 17:47:07 星期一
// Desc: 签到抽奖 主界面
// ================================================

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIActivityWeeklyRaffleMain : UIBase
    {
        private static readonly int PunchHash = Animator.StringToHash("Punch");
        private string _normalDescKey = "#SysComDesc1169";
        private string _raffleDescKey = "#SysComDesc1170";

        public AnimationCurve flyToCenterCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float flyToCenterDuration = 1f;

        public AnimationCurve flyToTokenCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float flyToTokenDuration = 1f;

        [SerializeField] private UIVisualGroup visualGroup;
        [SerializeField] private MBWeeklyRaffleItem _item;
        [SerializeField] private Transform _jackpot;
        [SerializeField] private Transform signTokenImg; // 签到的动画表现
        [SerializeField] private Transform tokenPos; // 签到的动画表现
        [SerializeField] private GameObject curdayfx;

        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _tokenNum;
        private TextMeshProUGUI _raffleBtnText;
        private TextMeshProUGUI _descText;
        private Animator _tokenAnim;
        private NonDrawingGraphic _block;

        private ActivityWeeklyRaffle _activity; // 活动实例
        private Dictionary<int, MBWeeklyRaffleItem> _itemMap = new(); // key: day, value: item
        public Dictionary<int, MBWeeklyRaffleItem> ItemMap => _itemMap;

        private bool _playSignAnim = false; // 是否播放签到动画

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("Content/_cd/text", out _cd);
            transform.Access("Content/finalBox/num", out _tokenNum);
            transform.Access("Content/finalBox/TokenRoot", out _tokenAnim);
            transform.Access("Content/RaffleBtn/txt", out _raffleBtnText);
            transform.Access("Content/SubTitle", out _descText);
            transform.Access("Content/block", out _block);
        }

        private void AddButton()
        {
            transform.AddButton("Content/TitleBg/close", OnClickClose).WithClickScale().FixPivot();
            transform.AddButton("Content/TitleBg/help", OnClickHelp).WithClickScale().FixPivot();
            transform.AddButton("Content/RaffleBtn", OnClickRaffleBtn).WithClickScale().FixPivot();
            transform.AddButton("Content/finalBox", OnClickJackPot).WithClickScale().FixPivot();
        }

        private void OnClickClose()
        {
            Close();
        }

        private void OnClickHelp()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyRaffleHelp);
        }


        // 点击抽奖
        private void OnClickRaffleBtn()
        {
            if (_activity.CheckIsRaffleDay())
            {
                UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyRaffleDraw, _activity);
            }
            else
            {
                Close();
            }
        }

        private void OnClickJackPot()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyRaffleRewardTips, _jackpot.position, 70f, _activity);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2)
            {
                return;
            }

            _activity = items[0] as ActivityWeeklyRaffle;
            _playSignAnim = (bool)items[1];
        }

        protected override void OnPreOpen()
        {
            // block 弹窗
            Game.Manager.screenPopup.Block(delay_: true);

            InitRaffleItem();

            var showNum = _playSignAnim ? _activity.TokenNum - 1 : _activity.TokenNum;
            _tokenNum.SetText($"{showNum}/{_activity.GetConfigSignDayCount()}");

            var isRaffleDay = _activity.CheckIsRaffleDay();
            _raffleBtnText.SetText(isRaffleDay ? I18N.Text("#SysComDesc1177") : I18N.Text("#SysComDesc1176"));
            _descText.SetText(isRaffleDay ? I18N.Text(_raffleDescKey) : I18N.Text(_normalDescKey));

            curdayfx.SetActive(false);

            for (var i = 0; i < _activity.GetConfigSignDayCount(); i++)
            {
                // 检查是否需要播放签到动画
                int offsetDay = _activity.GetOffsetDay();
                if (i == offsetDay && _playSignAnim)
                {
                    // 先把需要播放动画的item切换到未签到状态
                    _itemMap[i].ResetState(0, false);
                    // 播放签到动画
                    StartCoroutine(CoOnSignIn(offsetDay, false));
                }
                else
                {
                    var dayState = _activity.GetDayState(i);
                    var sign = _activity.HasSign(i);
                    _itemMap[i].ResetState(dayState, sign);
                }
            }

            RefreshTheme();
            _RefreshCD();
        }

        private void InitRaffleItem()
        {
            // 清理旧的item
            foreach (var item in _itemMap.Values)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _itemMap.Clear();

            // 创建新的item
            var parent = _item.transform.parent;
            var configDayCount = _activity.GetConfigSignDayCount();

            for (var day = 0; day < configDayCount; day++)
            {
                var item = Instantiate(_item.gameObject, parent);
                item.SetActive(true);
                var itemComponent = item.GetComponent<MBWeeklyRaffleItem>();
                itemComponent.InitOnPreOpen(day, _activity);
                _itemMap[day] = itemComponent;
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(_RefreshCD);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);

            MessageCenter.Get<WEEKLYRAFFLE_REFILL>().AddListener(OnRefill);
            MessageCenter.Get<WEEKLYRAFFLE_TOKEN_CHANGE>().AddListener(OnTokenChange);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshCD);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);

            MessageCenter.Get<WEEKLYRAFFLE_REFILL>().RemoveListener(OnRefill);
            MessageCenter.Get<WEEKLYRAFFLE_TOKEN_CHANGE>().RemoveListener(OnTokenChange);
        }


        protected override void OnPostClose()
        {
            // 取消 block 弹窗
            Game.Manager.screenPopup.Block(false, false);
        }

        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(_cd, _activity?.Countdown ?? 0);
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != _activity)
            {
                return;
            }

            // 活动结束 避免因为页面关闭 协程被打断 签到动画导致的block
            if (IsBlock)
            {
                SetBlock(false);
            }

            Close();
        }

        // 播放签到动画
        public void PlaySignAnim(int day, bool refill)
        {
            StartCoroutine(CoOnSignIn(day, refill));
        }

        // 纯动画表现 补签也在调用
        private IEnumerator CoOnSignIn(int day, bool refill)
        {
            SetBlock(true);
            yield return new WaitForSeconds(0.5f);

            var token = _itemMap[day].TokenImg;
            var originPos = signTokenImg.position;
            signTokenImg.position = token.transform.position;
            signTokenImg.localScale = Vector3.zero;
            signTokenImg.gameObject.SetActive(true);


            for (var i = 0; i < day; i++)
            {
                var dayState = _activity.GetDayState(i);
                var sign = _activity.HasSign(i);
                _itemMap[i].ResetState(dayState, sign);
            }

            if (!refill) // 签到
            {
                var dayState = _activity.GetDayState(day);
                var sign = _activity.HasSign(day);
                _itemMap[day].ResetState(dayState, sign, false);
            }
            else // 补签
            {
                _itemMap[day].PlayRefill();
            }

            // 对勾动画
            _itemMap[day].PlayCheckPunch();
            // 按钮变对勾音效
            Game.Manager.audioMan.TriggerSound("WeeklyRaffleToken");

            // 签到特效
            if (!refill)
            {
                curdayfx.transform.position = _itemMap[day].transform.position;
                curdayfx.SetActive(true);
            }

            yield return new WaitForSeconds(1f);

            var seq = DOTween.Sequence();

            // 飞到屏幕中间
            seq.Append(signTokenImg.DOScale(Vector3.one, flyToCenterDuration).SetEase(flyToCenterCurve));
            seq.Join(signTokenImg.DOMove(originPos, flyToCenterDuration).SetEase(flyToCenterCurve));

            // 飞到tokenPos
            seq.Append(signTokenImg.DOScale(Vector3.zero, flyToTokenDuration).SetEase(flyToTokenCurve));
            seq.Join(signTokenImg.DOMove(tokenPos.position, flyToTokenDuration).SetEase(flyToTokenCurve));

            seq.OnComplete(() =>
            {
                _tokenAnim.SetTrigger(PunchHash);
                SetTokenNum();

                _raffleBtnText.SetText(_activity.CheckIsRaffleDay()
                    ? I18N.Text("#SysComDesc1177")
                    : I18N.Text("#SysComDesc1176"));

                signTokenImg.gameObject.SetActive(false);
                signTokenImg.position = originPos;
            });

            yield return new WaitForSeconds(flyToCenterDuration - 0.1f);

            // token数字变化音效
            Game.Manager.audioMan.TriggerSound("WeeklyRaffleAccept");

            yield return new WaitForSeconds(flyToTokenDuration + 0.6f);
            // 签到结束 弹出Tip
            OnClickJackPot();

            // 签到特效结束
            yield return new WaitForSeconds(1.5f);
            curdayfx.SetActive(false);
            SetBlock(false);
        }

        // 补签成功
        private void OnRefill(int day, bool buyAll)
        {
            if (buyAll)
            {
                SetTokenNum();
                for (var i = 0; i < _activity.GetConfigSignDayCount(); i++)
                {
                    var dayState = _activity.GetDayState(i);
                    var sign = _activity.HasSign(i);
                    _itemMap[i].ResetState(dayState, sign);
                }
            }
        }

        private void OnTokenChange(bool refresh)
        {
            if (refresh)
            {
                SetTokenNum();
            }
        }

        private void SetTokenNum()
        {
            _tokenNum.SetText($"{_activity.TokenNum}/{_activity.GetConfigSignDayCount()}");
        }

        public void RefreshTheme()
        {
            _activity.MainPopUp.Refresh(visualGroup);
            _activity.MainPopUp.visual.RefreshText(_descText, _activity.CheckIsRaffleDay() ? "subTitle2" : "subTitle1");
        }

        private void SetBlock(bool value)
        {
            _block.raycastTarget = value;
        }

        private bool IsBlock => _block.raycastTarget;
    }
}