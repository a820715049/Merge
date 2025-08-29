// ==================================================
// // File: UIBPMileStoneCell.cs
// // Author: liyueran
// // Date: 2025-06-18 16:06:40
// // Desc: $bp里程碑cell
// // ==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIBPMileStoneCell : FancyScrollRectCell<BPMileStoneCellViewData, UICommonScrollRectDefaultContext>
    {
        // Icon
        public GameObject icon;
        private TextMeshProUGUI _freeText;
        private TextMeshProUGUI _luxuryText;
        private UIImageState _luxuryIcon;

        // Cycle
        public GameObject cycle;
        public GameObject redPoint;
        private Image _cycleLock;
        private UIImageRes _cycleBoxIcon;
        private TextMeshProUGUI _cycleRedText;
        private TextMeshProUGUI _boxCount;
        private Transform _cycleTipPos;
        private Button _cycleClaimButton;

        // Milestone
        public GameObject mileStone;
        public GameObject freeRewardTwo;
        public GameObject luxuryRewardTwo;
        public GameObject freeLvUpEffect;
        public GameObject luxuryLvUpEffect;
        public GameObject luxuryLvIdleEffect;
        public Animator freeClaimAnimator;
        public Animator luxuryClaimAnimator;
        private Button _freeClaimButton;
        private Button _luxuryClaimButton;
        private Image _luxuryLock;
        private RectTransform _freeTipPos;
        private RectTransform _luxuryTipPos;
        private UICommonItem _freeItem;
        private UICommonItem _luxuryItem;
        private UICommonItem _freeItem_1;
        private UICommonItem _freeItem_2;
        private UICommonItem _luxuryItem_1;
        private UICommonItem _luxuryItem_2;


        // Progress
        public Animator progressIcoAnimator;
        private UITextState _lvTextState;
        private RectMask2D _progressBar;
        private Image _backUp;
        private Image _backDown;
        private UIImageState _lvIconState; // 0:未达到 1:达到

        // Mask
        public GameObject claimMask;
        public GameObject freeClaimMask;
        public GameObject luxuryClaimMask;
        public Animator freeMaskAnimator;
        public Animator luxuryMaskAnimator;


        private BPActivity _activity;
        private BPMileStoneCellViewData _viewData;
        private Vector3 relativeScale;
        private Sequence _lvUpProgressAnimSeq = null;


        #region Mono
        private void Awake()
        {
            RegisterComp();
            AddButton();
            GetRelativeScale();
        }

        private void RegisterComp()
        {
            transform.Access("BPIcon/Free/txt", out _freeText);
            transform.Access("BPIcon/Luxury/txt", out _luxuryText);
            transform.Access("BPIcon/Luxury", out _luxuryIcon);

            transform.Access("Cycle/Bg/TipPos", out _cycleTipPos);
            transform.Access("Cycle/Bg/ClaimBtn", out _cycleClaimButton);
            transform.Access("Cycle/Bg/ClaimBtn/dotCount/Count", out _cycleRedText);
            transform.Access("Cycle/Bg/lock", out _cycleLock);
            transform.Access("Cycle/Bg/box", out _cycleBoxIcon);
            transform.Access("Cycle/Bg/box/count", out _boxCount);

            transform.Access("MileStoneItem/Progress/backUp", out _backUp);
            transform.Access("MileStoneItem/Progress/backDown", out _backDown);
            transform.Access("MileStoneItem/Progress/mask", out _progressBar);
            transform.Access("MileStoneItem/Progress/IconRoot/icon/lv", out _lvTextState);
            transform.Access("MileStoneItem/Progress/IconRoot/icon", out _lvIconState);

            transform.Access("MileStoneItem/Free/Bg/tipPos", out _freeTipPos);
            transform.Access("MileStoneItem/Luxury/Bg/tipPos", out _luxuryTipPos);
            transform.Access("MileStoneItem/Luxury/Bg/lock", out _luxuryLock);

            transform.Access("MileStoneItem/Free/Bg/claimRoot/ClaimBtn", out _freeClaimButton);
            transform.Access("MileStoneItem/Luxury/Bg/claimRoot/ClaimBtn", out _luxuryClaimButton);

            transform.Access("MileStoneItem/Free/Bg/UICommonItem", out _freeItem);
            transform.Access("MileStoneItem/Luxury/Bg/UICommonItem", out _luxuryItem);

            transform.Access("MileStoneItem/Free/Bg/rewardTwo/UICommonItem_1", out _freeItem_1);
            transform.Access("MileStoneItem/Free/Bg/rewardTwo/UICommonItem_2", out _freeItem_2);
            transform.Access("MileStoneItem/Luxury/Bg/rewardTwo/UICommonItem_1", out _luxuryItem_1);
            transform.Access("MileStoneItem/Luxury/Bg/rewardTwo/UICommonItem_2", out _luxuryItem_2);
        }

        private void AddButton()
        {
            transform.AddButton("BPIcon/Luxury", OnClickLuxuryIcon).WithClickScale().FixPivot();

            transform.AddButton("Cycle/Bg/ClaimBtn", OnClickCycleClaim).WithClickScale().FixPivot();
            transform.AddButton("Cycle/Bg", OnClickCycle).WithClickScale().FixPivot();

            transform.AddButton("MileStoneItem/Free/Bg", OnClickFreeCell);
            transform.AddButton("MileStoneItem/Luxury/Bg", OnClickLuxuryCell);

            transform.AddButton("MileStoneItem/Free/Bg/claimRoot/ClaimBtn", OnClickFreeClaim).WithClickScale()
                .FixPivot();
            transform.AddButton("MileStoneItem/Luxury/Bg/claimRoot/ClaimBtn", OnClickLuxuryClaim).WithClickScale()
                .FixPivot();
        }

        private void GetRelativeScale()
        {
            var maskRect = _progressBar.GetComponent<RectTransform>();
            var canvas = _progressBar.GetComponentInParent<Canvas>();
            var maskScale = maskRect.lossyScale;
            var canvasScale = canvas.transform.lossyScale;
            relativeScale = new Vector3(
                maskScale.x / canvasScale.x,
                maskScale.y / canvasScale.y,
                maskScale.z / canvasScale.z
            );
        }


        // 为了让等级高的cell一直在最上面
        private void OnEnable()
        {
            freeLvUpEffect.SetActive(false);
            luxuryLvUpEffect.SetActive(false);

            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().AddListener(OnBpBuySuccess);
            MessageCenter.Get<UI_BP_OPEN_CYCLE_TIP>().AddListener(OnOpenCycleTip);
            MessageCenter.Get<UI_BP_MILESTONECELL_PLAY_UP>().AddListener(PlayAchieveLvEffect);
            MessageCenter.Get<UI_BP_MILESTONECELL_PLAY_PROGRESS>().AddListener(PlayLvUpProgressAnim);
        }

        private void OnDisable()
        {
            luxuryClaimAnimator.ResetTrigger("Show");
            freeClaimAnimator.ResetTrigger("Show");

            _lvUpProgressAnimSeq?.Kill();
            if (_claimCoroutine != null)
            {
                StopCoroutine(_claimCoroutine);
                _claimCoroutine = null;
            }

            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().RemoveListener(OnBpBuySuccess);
            MessageCenter.Get<UI_BP_OPEN_CYCLE_TIP>().RemoveListener(OnOpenCycleTip);
            MessageCenter.Get<UI_BP_MILESTONECELL_PLAY_UP>().RemoveListener(PlayAchieveLvEffect);
            MessageCenter.Get<UI_BP_MILESTONECELL_PLAY_PROGRESS>().RemoveListener(PlayLvUpProgressAnim);
        }
        #endregion

        #region 事件
        private void OnBpBuySuccess(BPActivity.BPPurchaseType type, PoolMapping.Ref<List<RewardCommitData>> container,
            bool late_)
        {
            if (_viewData.IsEmpty)
            {
                return;
            }

            if (_viewData.IsCycle)
            {
                UpdateCycle(_viewData);
            }
            else if (_viewData.IsIcon)
            {
                RefreshIconTheme();
            }
            else
            {
                UpdateMileStone(_viewData);
            }
        }

        private void OnOpenCycleTip()
        {
            if (_viewData.IsCycle)
            {
                OnClickCycle();
            }
        }

        // 点击豪华icon
        private void OnClickLuxuryIcon()
        {
            var curState = _activity.PurchaseState;

            // 没有付费
            if (curState == BPActivity.BPPurchaseState.Free)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPBuyBoth, _activity); // 购买界面
            }
            // 购买了付费一
            else if (curState == BPActivity.BPPurchaseState.Normal)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPBuyUpgrade, _activity); // 升级界面
            }
        }

        // 点击循环宝箱
        private void OnClickCycle()
        {
            var pool = _activity.GetCycleRandomPool();
            var rewardList = new List<RewardConfig>();
            foreach (var reward in pool)
            {
                rewardList.Add(new RewardConfig
                {
                    Id = reward.Item1,
                    Count = reward.Item2
                });
            }

            UIManager.Instance.OpenWindow(UIConfig.UIBPRewardTip, _cycleTipPos.position, 0f, _activity, rewardList,
                true);
        }

        // 点击循环宝箱领取按钮
        private void OnClickCycleClaim()
        {
            if (!_activity.CheckMilestoneCycle())
            {
                return;
            }

            var container = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> rewardList);
            if (_activity.TryClaimAllCycleReward(container) && rewardList.Count > 0)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPReward, _activity, container);
            }

            UpdateCycle(_viewData);
        }

        // 普通奖励领取
        private void OnClickFreeClaim()
        {
            OnClickClaim(true);
        }

        // 付费奖励领取
        private void OnClickLuxuryClaim()
        {
            if (_activity.PurchaseState == BPActivity.BPPurchaseState.Free)
            {
                return;
            }

            OnClickClaim(false);
        }

        private Coroutine _claimCoroutine = null;

        private void OnClickClaim(bool isFree)
        {
            if (!_activity.CheckCanClaimReward(_viewData.Config.Id, isFree))
            {
                return;
            }

            if (_claimCoroutine != null)
            {
                StopCoroutine(_claimCoroutine);
                _claimCoroutine = null;
            }

            _claimCoroutine = StartCoroutine(CoWaitClaim(0.8f, isFree));
        }

        // 点击普通奖励单元格
        private void OnClickFreeCell()
        {
            // 如果可以领取 直接领取 不显示tip
            if (_activity.CheckCanClaimReward(_viewData.Config.Id, true))
            {
                OnClickClaim(true);
                return;
            }

            UIManager.Instance.OpenWindow(UIConfig.UIBPMileStoneTip, _freeTipPos.position, 0f, true,
                _viewData.Config.Id);
        }

        // 点击付费奖励单元格
        private void OnClickLuxuryCell()
        {
            // 如果可以领取 直接领取 不显示tip
            if (_activity.CheckCanClaimReward(_viewData.Config.Id, false) &&
                _activity.PurchaseState != BPActivity.BPPurchaseState.Free)
            {
                OnClickClaim(false);
                return;
            }

            UIManager.Instance.OpenWindow(UIConfig.UIBPMileStoneTip, _luxuryTipPos.position, 0f, false,
                _viewData.Config.Id);
        }
        #endregion

        #region 进度条动画
        private IEnumerator CoWaitClaim(float seconds, bool isFree)
        {
            // 数据称层领取
            var container = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> rewardList);
            var claim = _activity.TryClaimMilestoneReward(_viewData.Config.Id, isFree, container);

            var ui = UIManager.Instance.TryGetUI(UIConfig.UIBPMain);
            var main = ui as UIBPMain;
            if (main != null)
            {
                main.SetBlock(true);
            }

            // 点击音效
            Game.Manager.audioMan.TriggerSound("UIClick");

            if (isFree)
            {
                freeClaimMask.SetActive(true);
                freeClaimAnimator.SetTrigger("Hide");
                freeMaskAnimator.SetTrigger("Punch");
            }
            else
            {
                luxuryClaimMask.SetActive(true);
                luxuryClaimAnimator.SetTrigger("Hide");
                luxuryMaskAnimator.SetTrigger("Punch");
            }

            if (isFree)
            {
                _viewData.FreeCellViewState = BPMileStoneCellViewData.UIBpCellViewState.Claimed;
            }
            else
            {
                luxuryLvIdleEffect.gameObject.SetActive(false);
                _viewData.LuxuryCellViewState = BPMileStoneCellViewData.UIBpCellViewState.Claimed;
            }

            yield return new WaitForSeconds(seconds);

            SetUIState(_viewData.FreeCellViewState, _viewData.LuxuryCellViewState);
            if (main != null)
            {
                main.SetBlock(false);
            }

            if (claim && rewardList.Count > 0)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPReward, _activity, container);
            }
        }


        // 播放达到等级动画
        private void PlayAchieveLvProgressAnim(float duration, Action onComplete = null)
        {
            var maskRect = _progressBar.GetComponent<RectTransform>();
            var max = maskRect.rect.height;

            // 清空进度条
            _progressBar.padding = new Vector4(0, max * relativeScale.y, 0, 0);

            _lvUpProgressAnimSeq?.Kill();
            _lvUpProgressAnimSeq = DOTween.Sequence();
            _lvUpProgressAnimSeq.Append(
                DOTween.To(() => _progressBar.padding, x => _progressBar.padding = x,
                        new Vector4(0, 0, 0, 0), duration).SetEase(Ease.Linear)
                    .OnComplete(() => { onComplete?.Invoke(); }));
            _lvUpProgressAnimSeq.Play();
        }

        private void PlayAchieveLvProgressUpAnim(float duration, Action onComplete = null)
        {
            var maskRect = _progressBar.GetComponent<RectTransform>();
            var max = maskRect.rect.height;

            // 清空进度条
            _progressBar.padding = new Vector4(0, max * relativeScale.y, 0, 0);

            _lvUpProgressAnimSeq?.Kill();
            _lvUpProgressAnimSeq = DOTween.Sequence();
            _lvUpProgressAnimSeq.Append(
                DOTween.To(() => _progressBar.padding, x => _progressBar.padding = x,
                        new Vector4(0, max * relativeScale.y / 2, 0, 0), duration).SetEase(Ease.Linear)
                    .OnComplete(() => { onComplete?.Invoke(); }));
            _lvUpProgressAnimSeq.Play();
        }

        private void PlayAchieveLvProgressDownAnim(float duration, Action onComplete = null)
        {
            var maskRect = _progressBar.GetComponent<RectTransform>();
            var max = maskRect.rect.height;

            // 清空下方进度条
            _progressBar.padding = new Vector4(0, max * relativeScale.y / 2, 0, _progressBar.padding.w);

            if (_viewData.Config != null && _viewData.Config.ShowNum == 1)
            {
                // 清空上方进度条
                _progressBar.padding = new Vector4(0, _progressBar.padding.y, 0, max * relativeScale.y / 2);
            }

            _lvUpProgressAnimSeq?.Kill();
            _lvUpProgressAnimSeq = DOTween.Sequence();
            _lvUpProgressAnimSeq.Append(
                DOTween.To(() => _progressBar.padding, x => _progressBar.padding = x,
                        new Vector4(0, 0, 0, _progressBar.padding.w), duration).SetEase(Ease.Linear)
                    .OnComplete(() => { onComplete?.Invoke(); }));
            _lvUpProgressAnimSeq.Play();
        }

        // 设置首尾部进度条
        private void SetTailProgressState()
        {
            var allMilestoneInfo = _viewData.Activity.GetCurDetailConfig()?.MileStones;
            if (allMilestoneInfo == null)
            {
                return;
            }

            var conf = Game.Manager.configMan.GetBpMilestoneConfig(allMilestoneInfo[^2]);
            var maxShowNum = conf.ShowNum;

            _backUp.gameObject.SetActive(_viewData.ShowNum != 1);
            _backDown.gameObject.SetActive(_viewData.ShowNum != maxShowNum);


            // 第一个不显示上方进度条
            if (_viewData.ShowNum == 1)
            {
                var maskRect = _progressBar.GetComponent<RectTransform>();
                var max = maskRect.rect.height;

                // 清空上方进度条
                _progressBar.padding = new Vector4(0, _progressBar.padding.y, 0, max * relativeScale.y / 2);
            }
            // 最后一个不显示下方进度条 todo
            else if (_viewData.ShowNum == maxShowNum)
            {
                _backDown.gameObject.SetActive(false);
                var maskRect = _progressBar.GetComponent<RectTransform>();
                var max = maskRect.rect.height;

                // 清空下方进度条
                _progressBar.padding = new Vector4(0, max * relativeScale.y / 2, 0, _progressBar.padding.w);
            }
        }


        private void PlayLvUpProgressAnim(int start, float duration, int last, int to)
        {
            void OnComplete()
            {
                if (_viewData.FreeCellViewState == BPMileStoneCellViewData.UIBpCellViewState.UnAchieve)
                {
                    _viewData.FreeCellViewState = BPMileStoneCellViewData.UIBpCellViewState.Achieved;
                }

                if (_viewData.LuxuryCellViewState == BPMileStoneCellViewData.UIBpCellViewState.UnAchieve)
                {
                    _viewData.LuxuryCellViewState = BPMileStoneCellViewData.UIBpCellViewState.Achieved;
                }
            }

            if (start == 0)
            {
                start = 1;
            }

            if (Index == start && start != -1)
            {
                start = -1;
                PlayAchieveLvProgressDownAnim(duration, () =>
                {
                    MessageCenter.Get<UI_BP_MILESTONECELL_PLAY_PROGRESS>().Dispatch(start, duration, Index, to);
                    OnComplete();
                });
            }
            else if (Index == last + 1 && Index < to && start == -1)
            {
                PlayAchieveLvProgressAnim(duration, () =>
                {
                    MessageCenter.Get<UI_BP_MILESTONECELL_PLAY_PROGRESS>().Dispatch(start, duration, Index, to);
                    OnComplete();
                });
            }
            else if (Index == last + 1 && Index == to && start == -1)
            {
                PlayAchieveLvProgressUpAnim(duration, () =>
                {
                    OnComplete();

                    // 播放特效
                    MessageCenter.Get<UI_BP_MILESTONECELL_PLAY_UP>().Dispatch();
                });
            }
        }

        private void PlayAchieveLvEffect()
        {
            if (_viewData.IsCycle || _viewData.IsEmpty || _viewData.IsIcon)
            {
                return;
            }

            var curLvIndex = _activity.GetCurMilestoneLevel();
            if (_viewData.ShowNum > curLvIndex + 1)
            {
                return;
            }

            if (_viewData.ShowNum <= curLvIndex + 1)
            {
                if (_viewData.FreeCellViewState == BPMileStoneCellViewData.UIBpCellViewState.UnAchieve)
                {
                    _viewData.FreeCellViewState = BPMileStoneCellViewData.UIBpCellViewState.Achieved;
                }

                if (_viewData.LuxuryCellViewState == BPMileStoneCellViewData.UIBpCellViewState.UnAchieve)
                {
                    _viewData.LuxuryCellViewState = BPMileStoneCellViewData.UIBpCellViewState.Achieved;
                }
            }


            AchieveLvEffectTween();
        }

        private void AchieveLvEffectTween()
        {
            progressIcoAnimator.SetTrigger("Punch");

            // 里程碑达成音效
            Game.Manager.audioMan.TriggerSound("BattlePassMilestoneLight");

            var seq = DOTween.Sequence();
            seq.AppendInterval(0.25f);
            seq.OnComplete(() =>
            {
                RefreshViewLv();
                UpdateContent(_viewData);

                // 区分 free 和 luxury
                if (_viewData.FreeCellViewState != BPMileStoneCellViewData.UIBpCellViewState.Claimed)
                {
                    freeLvUpEffect.SetActive(true);
                    // 未领取=>可领取 特效
                }

                if (_viewData.LuxuryCellViewState != BPMileStoneCellViewData.UIBpCellViewState.Claimed)
                {
                    // 未领取=>可领取 特效
                    luxuryLvUpEffect.SetActive(true);
                }

                // 判断是否需要打开购买强弹界面
                if (_activity.CheckCanPopBuy())
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIBPBuyBothPop, _activity);
                }
            });
        }
        #endregion

        #region 刷新
        private void RefreshIconTheme()
        {
            if (_activity == null)
            {
                return;
            }

            // free
            // 多语言
            if (_activity.VisualMain.visual.TextMap.TryGetValue("pay3", out var freeIconTxt))
            {
                _freeText.SetText(I18N.Text(freeIconTxt));
            }

            // 样式
            if (_activity.VisualMain.visual.StyleMap.TryGetValue("pay3", out _))
            {
                _activity.VisualMain.visual.RefreshStyle(_freeText, "pay3");
            }


            // luxury
            if (_activity.PurchaseState == BPActivity.BPPurchaseState.Free)
            {
                // 多语言
                if (_activity.VisualMain.visual.TextMap.TryGetValue("pay1", out var normalIconTxt))
                {
                    _luxuryText.SetText(I18N.Text(normalIconTxt));
                }

                // 样式
                if (_activity.VisualMain.visual.StyleMap.TryGetValue("pay1", out _))
                {
                    _activity.VisualMain.visual.RefreshStyle(_luxuryText, "pay1");
                }
            }
            else
            {
                // 多语言
                if (_activity.VisualMain.visual.TextMap.TryGetValue("pay2", out var luxuryIconTxt))
                {
                    _luxuryText.SetText(I18N.Text(luxuryIconTxt));
                }

                // 样式
                if (_activity.VisualMain.visual.StyleMap.TryGetValue("pay2", out _))
                {
                    _activity.VisualMain.visual.RefreshStyle(_luxuryText, "pay2");
                }
            }


            // ICON 背景
            _luxuryIcon.Select(_activity.PurchaseState == BPActivity.BPPurchaseState.Free ? 0 : 1);
        }

        // 通用刷新 小于指定数量，数字隐藏
        private void CommonItemRefresh(UICommonItem item, RewardConfig config, int hideCount)
        {
            if (config == null)
            {
                return;
            }

            item.Refresh(config);
            item.Access<TMP_Text>("Count", out var countText);
            countText.gameObject.SetActive(config.Count > hideCount);
        }

        private void RefreshViewLv()
        {
            var curLvIndex = _activity.GetCurMilestoneLevel();
            _viewData.ProgressViewLv = curLvIndex + 1;
        }

        // 根据状态 设置进度条,icon和领取遮罩
        private void SetUIState(BPMileStoneCellViewData.UIBpCellViewState freeViewState,
            BPMileStoneCellViewData.UIBpCellViewState luxuryViewState)
        {
            if (_viewData.IsEmpty || _viewData.IsIcon)
            {
                return;
            }

            var maskRect = _progressBar.GetComponent<RectTransform>();
            var max = maskRect.rect.height;
            // 进度条 显示的条件是 达成下一等级 
            if (_viewData.ProgressViewLv > _viewData.ShowNum)
            {
                _progressBar.padding = new Vector4(0, 0, 0, 0);
            }
            else if (_viewData.ProgressViewLv == _viewData.ShowNum)
            {
                // 清空下方进度条
                _progressBar.padding = new Vector4(0, max * relativeScale.y / 2, 0, 0);
            }
            else
            {
                _progressBar.padding = new Vector4(0, max * relativeScale.y / 2, 0, max * relativeScale.y / 2);
            }

            SetTailProgressState();

            // 等级icon
            _lvIconState.Select(_viewData.ShowNum <= _viewData.ProgressViewLv ? 1 : 0);

            // 等级字体
            _lvTextState.Select(_viewData.ShowNum <= _viewData.ProgressViewLv ? 1 : 0);

            // 里程碑奖励的显示 
            // 约定配置的奖励只有1个 或 2个
            if (_viewData.Config.RewardFree.Count < 2)
            {
                freeRewardTwo.SetActive(false);
                CommonItemRefresh(_freeItem, _viewData.Config.RewardFree[0].ConvertToRewardConfig(), 1);
            }
            else
            {
                freeRewardTwo.SetActive(true);
                _freeItem.gameObject.SetActive(false);
                CommonItemRefresh(_freeItem_1, _viewData.Config.RewardFree[0].ConvertToRewardConfig(), 1);
                CommonItemRefresh(_freeItem_2, _viewData.Config.RewardFree[1].ConvertToRewardConfig(), 1);
            }

            if (_viewData.Config.RewardPay.Count < 2)
            {
                luxuryRewardTwo.SetActive(false);
                CommonItemRefresh(_luxuryItem, _viewData.Config.RewardPay[0].ConvertToRewardConfig(), 1);
            }
            else
            {
                luxuryRewardTwo.SetActive(true);
                _luxuryItem.gameObject.SetActive(false);
                CommonItemRefresh(_luxuryItem_1, _viewData.Config.RewardPay[0].ConvertToRewardConfig(), 1);
                CommonItemRefresh(_luxuryItem_2, _viewData.Config.RewardPay[1].ConvertToRewardConfig(), 1);
            }

            // 里程碑锁的显示
            _luxuryLock.gameObject.SetActive(_activity.PurchaseState == BPActivity.BPPurchaseState.Free);

            // 领取奖励后 遮罩的显示
            freeClaimMask.SetActive(freeViewState == BPMileStoneCellViewData.UIBpCellViewState.Claimed);
            luxuryClaimMask.SetActive(luxuryViewState == BPMileStoneCellViewData.UIBpCellViewState.Claimed);

            // 付费常驻特效的显示
            luxuryLvIdleEffect.gameObject.SetActive(
                luxuryViewState != BPMileStoneCellViewData.UIBpCellViewState.Claimed);

            // 领取按钮的显示
            if (freeViewState == BPMileStoneCellViewData.UIBpCellViewState.Achieved)
            {
                _freeClaimButton.gameObject.SetActive(true);
                freeClaimAnimator.SetTrigger("Show");
            }
            else
            {
                _freeClaimButton.gameObject.SetActive(false);
            }

            // 领取按钮的显示
            if (luxuryViewState == BPMileStoneCellViewData.UIBpCellViewState.Achieved &&
                _activity.PurchaseState != BPActivity.BPPurchaseState.Free)
            {
                _luxuryClaimButton.gameObject.SetActive(true);
                luxuryClaimAnimator.SetTrigger("Show");
            }
            else
            {
                _luxuryClaimButton.gameObject.SetActive(false);
            }


            // 循环奖励
            _cycleClaimButton.gameObject.SetActive(_activity.CycleAvailableCount > 0 &&
                                                   _activity.PurchaseState != BPActivity.BPPurchaseState.Free);
            _cycleRedText.text = _activity.CycleAvailableCount.ToString();
        }


        private void UpdateMileStone(BPMileStoneCellViewData cellViewData)
        {
            _activity = cellViewData.Activity;

            icon.SetActive(false);
            cycle.SetActive(false);
            mileStone.SetActive(true);
            claimMask.SetActive(true);

            _lvTextState.text.text = cellViewData.ShowNum.ToString();

            var milestonesConfig = _activity.GetCurDetailConfig()?.MileStones;
            if (milestonesConfig == null)
            {
                return;
            }


            SetUIState(cellViewData.FreeCellViewState, cellViewData.LuxuryCellViewState);
        }

        private void UpdateCycle(BPMileStoneCellViewData cellViewData)
        {
            // 循环奖励 显示在最上面
            transform.SetAsLastSibling();

            icon.SetActive(false);
            cycle.SetActive(true);
            mileStone.SetActive(false);
            claimMask.SetActive(false);

            // 宝箱Icon
            var token = fat.conf.Data.GetObjBasic(_activity.ConfD.CircleChestId);
            _cycleBoxIcon.SetImage(token.Icon);

            // 领取按钮
            _cycleClaimButton.gameObject.SetActive(_activity.CycleAvailableCount > 0 &&
                                                   _activity.PurchaseState != BPActivity.BPPurchaseState.Free);

            // 锁
            _cycleLock.gameObject.SetActive(_activity.PurchaseState == BPActivity.BPPurchaseState.Free);

            // 进度文本 只要到达25级就显示进度
            var curLvIndex = _activity.GetCurMilestoneLevel();
            var mileStones = _activity.GetCurDetailConfig().MileStones;
            var maxLv = curLvIndex >= mileStones.Count - 2;
            _boxCount.gameObject.SetActive(maxLv && _activity.PurchaseState != BPActivity.BPPurchaseState.Free);
            // 此时为循环宝箱显示 所以等级为最后一级
            var cycleId = _activity.GetCycleMilestoneId();
            var config = Game.Manager.configMan.GetBpMilestoneConfig(cycleId);
            if (config != null)
            {
                _boxCount.SetText($"{_activity.CycleReceivedCount}/{config.CircleLimit}");
            }

            // 红点
            redPoint.SetActive(_activity.CycleAvailableCount > 0);
            if (_activity.CycleAvailableCount > 0)
            {
                _cycleRedText.SetText($"{_activity.CycleAvailableCount}");
            }
        }

        public override void UpdateContent(BPMileStoneCellViewData cellViewData)
        {
            _activity = cellViewData.Activity;
            _viewData = cellViewData;

            if (cellViewData.IsEmpty)
            {
                icon.SetActive(false);
                cycle.SetActive(false);
                mileStone.SetActive(false);
                claimMask.SetActive(false);
            }
            else if (cellViewData.IsIcon)
            {
                icon.SetActive(true);
                cycle.SetActive(false);
                mileStone.SetActive(false);
                claimMask.SetActive(false);
                RefreshIconTheme();
            }
            else if (cellViewData.IsCycle)
            {
                UpdateCycle(cellViewData);
            }
            else
            {
                UpdateMileStone(cellViewData);
            }
        }
        #endregion
    }
}