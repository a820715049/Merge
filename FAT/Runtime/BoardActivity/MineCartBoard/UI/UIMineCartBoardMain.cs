
using System.Collections.Generic;
using DG.Tweening;
using EL;
using System.Collections;
using FAT.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Coffee.UIExtensions;

namespace FAT
{
    public class UIMineCartBoardMain : UIBase
    {
        #region Fields
        [SerializeField] private MineCartItem _mineCartItem;
        [SerializeField] private MBBoardView _view;
        [SerializeField] private MBBoardViewMoveComp _moveComp;
        [SerializeField] private TextMeshProUGUI _cd;

        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private UIImageRes _nextRewardIcon;
        [SerializeField] private Button _nextRewardBtn;
        [SerializeField] private TextMeshProUGUI _nextRewardText;
        [SerializeField] private GameObject _nextRewardGo;
        [SerializeField] private MBMineCartHandbook _handbook;
        [SerializeField] private MBMineCartBoardReward _reward;
        private MineCartActivity _activity;
        [SerializeField] private Image _boardEntry;
        //右侧挖矿1+1礼包
        [SerializeField] private PackOnePlusOneMineCart _giftPack;
        [SerializeField] private GameObject _giftPackGo;
        [SerializeField] private UIImageRes _giftPackIcon;
        [SerializeField] private TextMeshProUGUI _giftPackCd;
        [SerializeField] private MBBoardAutoGuide _autoGuide;
        [SerializeField] private Transform goBtn;
        [SerializeField] private float _targetFlyTime = 1f;
        [SerializeField] private float _milestoneRewardDelayTime = 0f;
        [SerializeField] private UIParticle _moveEffect;
        private bool needShowFinalItem = false;

        // goBtn 动画状态
        private Tween _goBtnTween;
        private bool _goBtnVisible;
        [SerializeField] private float _goBtnAppearDelay = 1.5f; // goBtn 出现延迟（可配置）
        private Coroutine _goBtnDelayCo;
        private bool _goBtnShouldShow;
        private const float GoBtnFadeInDuration = 0.2f;
        private const float GoBtnFadeOutDuration = 0.15f;
        private const float GoBtnScaleHidden = 0.8f;

        #endregion
        protected override void OnCreate()
        {
            Setup();
            AddButton();
            _handbook.OnCreate();

        }

        private void Setup()
        {
            _view.Setup();
            _reward.Setup();
            // 配置移动组件
            if (_moveComp != null)
            {
                _moveComp.uiBase = this;
                _moveComp.OnLastRowFlyComplete = OnLastRowFlyTriggered;
                _moveComp.OnMoveEnd = OnBoardViewMoveEnd;
                _moveComp.OnRowMoveStart = OnRowMoveStart;
                _moveComp.OnMoveStart = OnMoveStart;
                _view.moveRoot.localScale = Game.Manager.mainMergeMan.mainBoardScale * Vector3.one;
            }

            // 监听Final奖励收集完成
            _mineCartItem.OnFinalRewardCollected = OnFinalRewardCollected;
        }

        private void AddButton()
        {
            transform.AddButton("Content/Top/HelpBtn", ClickHelp);
            transform.AddButton("Content/Top/CloseBtn", OnClickClose);
            transform.AddButton("Content/Center/GiftNode", ClickPack);
            transform.AddButton("Content/Center/BoardNode/CompBoard/BtnClaim", OnClickGo);
        }

        private void ClickHelp()
        {
            UIManager.Instance.OpenWindow(_activity.VisualHelp.res.ActiveR, _activity);
        }


        private void ClickPack()
        {
            if (_giftPack != null)
                UIManager.Instance.OpenWindow(_giftPack.Res.ActiveR, _giftPack);
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as MineCartActivity;
            if (_activity == null) return;
            Game.Manager.screenPopup.Block(true, false);
            _handbook.OnParse(_activity);
            _mineCartItem.OnParse(this, _activity);

            _reward.OnParse(_activity);
            EnterBoard();
        }


        private void EnterBoard()
        {
            var world = _activity.World;
            BoardViewWrapper.PushWorld(world);
            RefreshScale(Game.Manager.mainMergeMan.mainBoardScale);
            _view.OnBoardEnter(world, world.currentTracer);
        }

        private void RefreshScale(float scale)
        {
            var root = _view.transform as RectTransform;
            root.localScale = new Vector3(scale, scale, scale);
            (root.parent as RectTransform).sizeDelta = new Vector2(scale * root.sizeDelta.x, scale * root.sizeDelta.y);
        }

        protected override void OnPreOpen()
        {
            UIConfig.UIStatus.Open();  // 直接打开UIStatus
            _activity.WorldTracer?.Invalidate();    // 通知数据层检查目前是否需要棋盘移动
            // 由 MineCartItem 负责进度与大奖UI
            _handbook.Refresh();
            _reward.Refresh();
            _moveComp.ResetToInitialState();
            _mineCartItem.SetNeedShowFinalItem(needShowFinalItem);
            if (_moveEffect != null)
                _moveEffect.gameObject.SetActive(false);

            // 检查并播放入场动画（此时Activity已经设置）
            _mineCartItem.OnPreOpen();

            // 绑定进度与大奖UI给 MineCartItem
            _mineCartItem.BindProgressUI(_progressText, _nextRewardIcon, _nextRewardBtn, _nextRewardText, _nextRewardGo);
            _activity.World.onItemEvent += OnItemEvent;
            RefreshGiftPack();
            RefreshCD();
            MessageCenter.Get<MSG.UI_TOP_BAR_PUSH_STATE>().Dispatch(UIStatus.LayerState.AboveStatus);
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
        }
        protected override void OnPostOpen()
        {
            base.OnPostOpen();
            _mineCartItem.OnPostOpen();
        }
        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.UI_MINECART_BOARD_UNLOCK_ITEM>().AddListener(_handbook.Unlock);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FlyFeedBack);

            MessageCenter.Get<MSG.GAME_MINECART_BOARD_PROG_CHANGE>().AddListener(OnProgressChange);
            MessageCenter.Get<MSG.UI_ACTIVITY_BOARD_MOVE_COLLECT>().AddListener(OnGetMoveCommand);
            MessageCenter.Get<MSG.UI_ACTIVITY_BOARD_MOVE_START>().AddListener(StartMoveUp);
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().AddListener(RefreshGiftPack);
            MessageCenter.Get<MSG.FLY_ICON_START>().AddListener(CheckNewFly);
            MessageCenter.Get<MSG.GAME_BOARD_TOUCH>().AddListener(_autoGuide.Interrupt);
            MessageCenter.Get<MSG.GUIDE_OPEN>().AddListener(_autoGuide.Interrupt);
            MessageCenter.Get<MSG.UI_ACTIVITY_BOARD_EXTREME>().AddListener(OnBoardExtremeCase);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.UI_MINECART_BOARD_UNLOCK_ITEM>().RemoveListener(_handbook.Unlock);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FlyFeedBack);

            MessageCenter.Get<MSG.GAME_MINECART_BOARD_PROG_CHANGE>().RemoveListener(OnProgressChange);
            MessageCenter.Get<MSG.UI_ACTIVITY_BOARD_MOVE_COLLECT>().RemoveListener(OnGetMoveCommand);
            MessageCenter.Get<MSG.UI_ACTIVITY_BOARD_MOVE_START>().RemoveListener(StartMoveUp);
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().RemoveListener(RefreshGiftPack);
            MessageCenter.Get<MSG.FLY_ICON_START>().RemoveListener(CheckNewFly);
            MessageCenter.Get<MSG.GAME_BOARD_TOUCH>().RemoveListener(_autoGuide.Interrupt);
            MessageCenter.Get<MSG.GUIDE_OPEN>().RemoveListener(_autoGuide.Interrupt);
            MessageCenter.Get<MSG.UI_ACTIVITY_BOARD_EXTREME>().RemoveListener(OnBoardExtremeCase);
        }


        private void OnProgressChange(int targetScore, int milestonePhase, PoolMapping.Ref<List<RewardCommitData>> reward)
        {
            string icon = "";
            if (milestonePhase >= 0)
            {
                //获取奖励图标配置
                int rewardId = _activity.GetCurRoundConfig().MilestoneReward[milestonePhase];
                icon = Game.Manager.configMan.GetEventMineCartRewardConfig(rewardId).Icon;
            }
            else if (reward.Valid)
            {
                //获取当前轮次的大奖图标
                icon = _activity.GetCurRoundConfig().RewardIcon;
            }

            // 先计算时间，再调用MoveCart
            float moveTime = _mineCartItem.CalcMoveEndTime(_targetFlyTime);
            _mineCartItem.MoveCart(icon, reward, _targetFlyTime);

            // 进度目标基于总里程碑值（TotalMilestoneNum）
            int totalTarget = _activity.BaseMilestoneNum + _activity.MilestoneNum;
            // 目标值延迟应用与矿车延迟一致，Base快照在 MineCartItem 内部处理
            _mineCartItem.SetDisplayTargetScore(totalTarget, _targetFlyTime);
            //延迟增加0.23s，给泡泡爆炸留时间.
            _activity.TryClaimMilestoneReward(milestonePhase, moveTime + _milestoneRewardDelayTime);
        }

        protected override void OnPreClose()
        {
            UIManager.Instance.CloseWindow(UIConfig.UIPopFlyTips);
            UIManager.Instance.CloseWindow(UIConfig.UIEnergyBoostTips);
            if (_commonResSeq != null) _commonResSeq.Kill();
            if (BoardViewWrapper.GetCurrentWorld() == null) return;
            _view.OnBoardLeave();
            BoardViewWrapper.PopWorld();
            _giftPack = null;
        }

        protected override void OnPostClose()
        {
            _activity.World.onItemEvent -= OnItemEvent;
            MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().Dispatch();
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
            _StopCoroutine();
        }

        private void Update()
        {
            // 使用MBBoardViewMoveComp的状态检查
            if (_moveComp != null && _moveComp.IsPlayingMove) return;
            BoardViewManager.Instance.Update(Time.deltaTime);

        }

        private void RefreshCD()
        {
            if (_activity == null) return;
            // 使用MBBoardViewMoveComp的状态检查
            if (_moveComp == null || !_moveComp.IsPlayingMove)
                _autoGuide.SecondUpdate();
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            UIUtility.CountDownFormat(_cd, diff);
            if (_giftPack != null)
            {
                var diff1 = (long)Mathf.Max(0, _giftPack.endTS - t);
                UIUtility.CountDownFormat(_giftPackCd, diff1);
            }
            if (diff <= 1) OnClickClose();
            SetGoBtnVisible(NeedShowPlay());
        }

        private void RefreshGiftPack()
        {
            _giftPack = Game.Manager.activity.LookupAny(fat.rawdata.EventType.CartOnePlusOne) as PackOnePlusOneMineCart;
            if (_giftPack != null)
            {
                _giftPackGo.SetActive(true);
                _giftPackIcon.SetImage(_giftPack.EntryIcon);
            }
            else
            {
                _giftPackGo.SetActive(false);
            }
        }
        private void OnItemEvent(Item item, ItemEventType eventType)
        {
            if (eventType == ItemEventType.ItemEventRewardListOut)
            {
                _reward.Refresh();
            }
            //特殊情况 棋子移到奖励箱
            else if (eventType == ItemEventType.ItemEventMoveToRewardBox)
            {
                _reward.RefreshWithPunch();
            }
        }
        private void FlyFeedBack(FlyableItemSlice slice)
        {
            if (slice.FlyType == FlyType.MergeItemFlyTarget)
            {
                _reward.FlyFeedBack(slice);
            }
            if (slice.FlyType == FlyType.MineCartUseItem)
            {
                _mineCartItem.PlayCollectTargetItem(slice);
            }
        }


        private void CheckNewFly(FlyableItemSlice slice)
        {
            if (_activity == null) return;
            if (slice.FlyType == FlyType.TapBonus || slice.FlyType == FlyType.FlyToMainBoard)
            {
                CheckTapBonus();
            }
            if (slice.FlyType == FlyType.Coin || slice.FlyType == FlyType.Gem || slice.FlyType == FlyType.Energy)
            {
                CheckCommonRes();
            }
        }

        private bool _isTapBonus;
        private void CheckTapBonus()
        {
            if (_isTapBonus) return;
            _isTapBonus = true;
            var seq = DOTween.Sequence();
            seq.Append(_boardEntry.DOFade(1, 0.5f));
            seq.AppendInterval(0.5f);
            seq.Append(_boardEntry.DOFade(0, 0.5f));
            seq.AppendCallback(() => _isTapBonus = false);
            seq.OnKill(() =>
            {
                var color = Color.white;
                color.a = 0;
                _boardEntry.color = color;
                _isTapBonus = false;
            });
            seq.Play();
        }

        private bool _isCheckCommonRes;
        private Sequence _commonResSeq;
        private void CheckCommonRes()
        {
            if (_isCheckCommonRes) return;
            _isCheckCommonRes = true;
            _commonResSeq = DOTween.Sequence();
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
            _commonResSeq.AppendInterval(1.5f);
            _commonResSeq.AppendCallback(() =>
            {
                MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
                _isCheckCommonRes = false;
                _commonResSeq = null;
            });
            _commonResSeq.OnKill(() =>
            {
                MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
                _isCheckCommonRes = false;
                _commonResSeq = null;
            });
            _commonResSeq.Play();
        }
        private void OnGetMoveCommand(List<Item> items)
        {
            // 使用MBBoardViewMoveComp处理移动逻辑
            if (_moveComp != null)
            {
                _moveComp.AccumulateCollectIcons(items);
            }
        }


        private void StartMoveUp()
        {
            // 使用MBBoardViewMoveComp执行移动
            if (_moveComp != null)
            {
                _moveComp.Execute();
            }
        }
        private void ClickGo()
        {
            ActivityTransit.Exit(_activity, ResConfig, () =>
            {
                UIConfig.UIMessageBox.Close();
            });
        }
        private void Exit(bool ignoreFrom = false)
        {
            ActivityTransit.Exit(_activity, ResConfig, null, ignoreFrom);
        }

        private void OnClickGo()
        {
            Exit(true);
        }

        private void OnClickClose()
        {
            Exit();
        }
        private bool NeedShowPlay()
        {
            if (_activity.World.rewardCount > 0)
            {
                return false;
            }
            return true;
        }
        #region MBBoardViewMoveComp回调

        private void OnLastRowFlyTriggered() { }

        /// <summary>
        /// 每行移动开始回调
        /// </summary>
        private void OnRowMoveStart()
        {

        }

        /// <summary>
        /// 所有动画完成回调
        /// </summary>
        private void OnBoardViewMoveEnd()
        {
            //在当前所有动画表现完成后检查一下棋盘是否卡死
            _activity?.CheckBoardExtremeCase();
            // 播放移动粒子效果
            if (_moveEffect != null)
            {
                SetParticleLoop(_moveEffect, false);
            }
        }

        /// <summary>
        /// Final奖励收集完成回调
        /// </summary>
        private void OnFinalRewardCollected() { }

        private void OnMoveStart()
        {
            if (_moveEffect != null)
            {
                SetParticleLoop(_moveEffect, true);
                _moveEffect.gameObject.SetActive(true);
                _moveEffect.Play();
            }
        }

        #endregion

        #region 棋盘遇到卡死情况时界面block

        private Coroutine _blockCoroutine;

        private void OnBoardExtremeCase()
        {
            _StopCoroutine();
            _blockCoroutine = StartCoroutine(_BoardExtremeBlock());
        }

        private IEnumerator _BoardExtremeBlock()
        {
            LockEvent();
            //等1s解开block
            yield return new WaitForSeconds(1);
            UnlockEvent();
        }

        private void _StopCoroutine()
        {
            if (_blockCoroutine != null)
            {
                UnlockEvent();
                StopCoroutine(_blockCoroutine);
                _blockCoroutine = null;
            }
        }

        #endregion

        /// <summary>
        /// 设置 UIParticle 下所有粒子系统的循环开关
        /// </summary>
        private void SetParticleLoop(UIParticle particle, bool loop)
        {
            if (particle == null) return;
            var systems = particle.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                var main = ps.main;
                main.loop = loop;
            }
        }

        /// <summary>
        /// 带 DoTween 动画地设置 goBtn 显示/隐藏
        /// </summary>
        private void SetGoBtnVisible(bool show)
        {
            if (goBtn == null) return;
            var go = goBtn.gameObject;
            _goBtnShouldShow = show;

            bool currentlyVisible = go.activeSelf;

            // 隐藏：立即执行，且取消等待中的显示协程
            if (!show)
            {
                if (_goBtnDelayCo != null)
                {
                    StopCoroutine(_goBtnDelayCo);
                    _goBtnDelayCo = null;
                }

                if (!currentlyVisible && !_goBtnVisible)
                {
                    return;
                }

                if (_goBtnTween != null && _goBtnTween.IsActive())
                {
                    _goBtnTween.Kill();
                    _goBtnTween = null;
                }

                var rectHide = go.GetComponent<RectTransform>();
                var cgHide = go.GetComponent<CanvasGroup>();
                if (cgHide == null) cgHide = go.AddComponent<CanvasGroup>();

                _goBtnTween = DOTween.Sequence()
                    .Join(cgHide.DOFade(0f, GoBtnFadeOutDuration))
                    .Join(rectHide.DOScale(GoBtnScaleHidden, GoBtnFadeOutDuration).SetEase(Ease.InBack))
                    .OnComplete(() => { go.SetActive(false); _goBtnTween = null; _goBtnVisible = false; });
                return;
            }

            // 显示：若已可见则跳过；否则按配置延迟显示
            if (_goBtnVisible || currentlyVisible)
            {
                return;
            }

            if (_goBtnDelayCo == null)
            {
                _goBtnDelayCo = StartCoroutine(CoShowGoBtnDelayed());
            }
        }

        // 进度与大奖UI交由 MineCartItem 管理

        private IEnumerator CoShowGoBtnDelayed()
        {
            float delay = Mathf.Max(0f, _goBtnAppearDelay);
            yield return new WaitForSeconds(delay);
            _goBtnDelayCo = null;
            if (!_goBtnShouldShow || goBtn == null) yield break;

            var go = goBtn.gameObject;

            if (_goBtnTween != null && _goBtnTween.IsActive())
            {
                _goBtnTween.Kill();
                _goBtnTween = null;
            }

            var rect = go.GetComponent<RectTransform>();
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            if (!go.activeSelf) go.SetActive(true);
            cg.alpha = 0f;
            rect.localScale = Vector3.one * GoBtnScaleHidden;
            _goBtnTween = DOTween.Sequence()
                .Join(cg.DOFade(1f, GoBtnFadeInDuration))
                .Join(rect.DOScale(1f, GoBtnFadeInDuration).SetEase(Ease.OutBack))
                .OnComplete(() => { _goBtnTween = null; _goBtnVisible = true; });
        }
    }
}
