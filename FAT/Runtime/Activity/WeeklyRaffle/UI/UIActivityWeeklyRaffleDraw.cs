// ================================================
// File: UIActivityWeeklyRaffleDraw.cs
// Author: yueran.li
// Date: 2025/06/09 17:46:29 星期一
// Desc: 签到抽奖 抽奖界面
// ================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Random = UnityEngine.Random;

namespace FAT
{
    public class UIActivityWeeklyRaffleDrawn : UIBase
    {
        public AnimationCurve flyToCenterCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float flyToCenterDuration = 1;

        public AnimationCurve flyToTokenCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float flyToTokenDuration = 1;

        [SerializeField] private UIVisualGroup visualGroup;
        [SerializeField] private List<MBWeeklyRaffleDrawSlot> _slots;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private UICommonItem rewardItem;
        [SerializeField] private Transform tokenImg;
        [SerializeField] private Transform tokenAnimImg;

        private TextMeshProUGUI _leftTime;
        private TextMeshProUGUI _tokenNum;
        private NonDrawingGraphic _block;
        public NonDrawingGraphic Block => _block;

        private ActivityWeeklyRaffle _activity;

        private int _lastPunTime = 0;

        protected override void OnCreate()
        {
            transform.Access("Content/topBg/_cd/text", out _leftTime);
            transform.Access("Content/bg/token/num", out _tokenNum);
            transform.AddButton("Content/topBg/close", OnClickClose).WithClickScale().FixPivot();
            transform.Access("Content/block", out _block);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = items[0] as ActivityWeeklyRaffle;
        }

        protected override void OnPreOpen()
        {
            InitRewardItems();
            InitSlots();
            OnTokenChange();
            RefreshTheme();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(SecondUpdate);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);

            MessageCenter.Get<WEEKLYRAFFLE_RAFFLE_END>().AddListener(OnRaffleEnd);
        }


        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(SecondUpdate);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);

            MessageCenter.Get<WEEKLYRAFFLE_RAFFLE_END>().RemoveListener(OnRaffleEnd);
        }

        protected override void OnPostOpen()
        {
            // 如果抽奖全部抽完 活动结束
            _activity.TryEndActivity();
        }

        private void InitRewardItems()
        {
            _activity.HandleRewardPool(out _, out var jackpot);
            if (jackpot == null)
            {
                return;
            }

            for (int i = 0; i < jackpot.Reward.Count; i++)
            {
                var reward = jackpot.Reward[i];
                var (cfgID, cfgCount, _) = reward.ConvertToInt3();

                var item = itemRoot.childCount > i + 1
                    ? itemRoot.GetChild(i + 1).GetComponent<UICommonItem>()
                    : Instantiate(rewardItem, itemRoot);
                item.gameObject.SetActive(true);
                item.GetComponent<UICommonItem>().Setup();
                item.Refresh(cfgID, cfgCount);
            }
        }

        private void InitSlots()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                _slots[i].InitOnPreOpen(_activity, i);
            }
        }

        private void SecondUpdate()
        {
            if (_activity == null)
            {
                return;
            }

            _RefreshCD();
            _lastPunTime += 1;

            if (_lastPunTime <= 1)
            {
                return;
            }

            var closBoxList = EnumerableExt.ToList(_slots.Where(x => !_activity.HasOpen(x.BoxId)));

            if (closBoxList.Count == 0)
            {
                return;
            }

            int punBox = Random.Range(0, closBoxList.Count);


            closBoxList[punBox].PlayCloseAnim();
            _lastPunTime = 0;
        }


        protected override void OnPostClose()
        {
            _lastPunTime = 0;

            if (IsBlock)
            {
                SetBlock(false);
            }

            ReleaseRewardItems();
        }

        private void ReleaseRewardItems()
        {
            for (int i = 0; i < itemRoot.childCount; i++)
            {
                itemRoot.GetChild(i).gameObject.SetActive(false);
            }
        }

        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(_leftTime, _activity?.Countdown ?? 0);
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != _activity)
            {
                return;
            }

            Close();
        }

        private void OnClickClose()
        {
            Close();
        }

        // 抽奖结束
        private void OnRaffleEnd(PoolMapping.Ref<List<RewardCommitData>> container, int boxId, int rewardId)
        {
            _lastPunTime = 0;
            _slots[boxId].PlayBoxOpen(container, rewardId);
            OnTokenChange();
        }

        private void OnTokenChange()
        {
            _tokenNum.SetText($"{_activity.TokenNum}");
        }

        public void PlayRefillAnim()
        {
            CoOnRefillAll();
        }

        // 全部补签动画
        private void CoOnRefillAll()
        {
            SetBlock(true);

            var originPos = tokenAnimImg.position;
            tokenAnimImg.localScale = Vector3.zero;
            tokenAnimImg.gameObject.SetActive(true);

            var seq = DOTween.Sequence();
            seq.AppendInterval(0.5f);
            seq.Append(tokenAnimImg.DOScale(Vector3.one, flyToCenterDuration).SetEase(flyToCenterCurve));

            seq.Append(tokenAnimImg.DOMove(tokenImg.position, flyToTokenDuration).SetEase(flyToTokenCurve));
            seq.Join(tokenAnimImg.DOScale(Vector3.one * 0.5f, flyToTokenDuration).SetEase(flyToTokenCurve));
            seq.OnComplete(() =>
            {
                tokenAnimImg.gameObject.SetActive(false);
                tokenAnimImg.position = originPos;
                OnTokenChange();
                SetBlock(false);
            });
        }

        public void RefreshTheme()
        {
            _activity.VisualRaffle.Refresh(visualGroup);
        }

        private void SetBlock(bool value)
        {
            _block.raycastTarget = value;
        }

        private bool IsBlock => _block.raycastTarget;
    }
}