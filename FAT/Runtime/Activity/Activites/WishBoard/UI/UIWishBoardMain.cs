/**
 * @Author: zhangpengjian
 * @Date: 2025/6/16 15:55:52
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/6/16 15:55:52
 * Description: 许愿主棋盘
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using FAT.Merge;
using FAT.MSG;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class UIWishBoardMain : UIBase, IAutoGuide, INavBack
    {
        [SerializeField] private RectTransform scroll;
        [SerializeField] private RectTransform bgBoard;
        [SerializeField] private UITilling tilling;
        [SerializeField] private MBRewardProgress progress;
        [SerializeField] private Button milestoneBtn;
        [SerializeField] private Button rewardBtn;
        [SerializeField] private Button talkBtn;
        [SerializeField] private UIImageRes rewardIcon;
        [SerializeField] private Transform rewardNode;
        [SerializeField] private UIImageRes milestoneIcon;
        [SerializeField] private TextMeshProUGUI milestoneNum;
        [SerializeField] private Transform talkRoot;
        [SerializeField] private Transform bubbleRoot;
        [SerializeField] private UIImageRes bubbleIcon;
        [SerializeField] private TextMeshProUGUI talkText;
        [SerializeField] private Transform efx;
        [SerializeField] private SkeletonGraphic spine;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private float spineDelayTime;
        [SerializeField] private Animator levelAnimator;
        [SerializeField] private Button goBtn;
        [SerializeField] private Button tipBtn;
        [SerializeField] private Transform tipRoot;
        [SerializeField] private Transform tip1;
        [SerializeField] private Transform tip2;
        [SerializeField] private Transform tip3;
        [SerializeField] private Transform block;
        private RectMask2D boardMask;
        private MBWishBoardHandbook _handbook;
        public float moveTime;
        public AnimationCurve moveCurve;
        // 滚动
        private MBBoardView _view;
        // 奖励箱
        private MBWishBoardReward _reward;
        // 云
        private MBWishBoardCloudHolder _cloudHolder;
        private MBWishBoardLockView lockView;
        public MBWishBoardLockView LockView => lockView;
        public MBWishBoardCloudHolder CloudHolder => _cloudHolder;
        // 手指引导
        private MBAutoGuideController _autoGuideController;
        public MBAutoGuideController AutoGuideController => _autoGuideController;
        // 礼包
        private CanvasGroup packGroup;
        private UIImageRes packIcon;
        private TextMeshProUGUI packCd;
        private PackEndlessWishBoard _giftPack;
        // 主棋盘入口
        private Image _boardEntry;
        // cd
        private TextMeshProUGUI _cd;
        private readonly string TopBgPath = $"Content/Top/TopBg";
        private readonly string CompBoardPath = $"Content/Center/BoardNode/CompBoard";
        private readonly string ScrollPath = $"Content/Center/BoardNode/CompBoard/Root/Mask/Scroll";
        private readonly string BottomPath = $"Content/Bottom";
        private WishBoardActivity _activity;
        private bool _isPlayingMove;
        private bool _isPlayingMoveCloudUnlock;
        // 气泡动画相关
        private CanvasGroup talkRootGroup;
        private float _talkTimer = 0f;
        private bool _talkShowing = false;
        private Sequence _talkSeq;
        private bool alreadyOpen = false;
        private bool alreadyShowTip = false;

        private bool _isCheckCommonRes;
        private bool _isTapBonus;
        private Sequence _commonResSeq;
        private const int MILESTONE_LIMIT_LEVEL = 4;

        // 进度变化事件队列管理
        private Queue<(List<RewardCommitData> data, string icon, int max, int cur)> _progressChangeQueue = new Queue<(List<RewardCommitData>, string, int, int)>();
        private bool _isProcessingProgressChange = false;

        #region Mono
        private void Update()
        {
            if (_isPlayingMove) return;
            BoardViewManager.Instance.Update(Time.deltaTime);

            // 气泡定时出现逻辑
            _talkTimer += Time.deltaTime;
            if (!_talkShowing && _talkTimer >= 5f && _activity.UnlockMaxLevel > MILESTONE_LIMIT_LEVEL)
            {
                ShowTalkRoot(_activity.GetRandomKey());
            }
        }
        #endregion

        #region UI
        protected override void OnCreate()
        {
            RegisterComp();
            Setup();
            AddButton();
            // 初始化talkRootGroup
            talkRootGroup = talkRoot.GetComponent<CanvasGroup>();
            if (talkRootGroup == null)
                talkRootGroup = talkRoot.gameObject.AddComponent<CanvasGroup>();
            talkRoot.gameObject.SetActive(false);
            talkRootGroup.alpha = 0;
            _talkTimer = 0f;
            _talkShowing = false;
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;
            _activity = (WishBoardActivity)items[0];
            EnterBoard();
        }

        protected override void OnPreOpen()
        {
            // 通知数据层检查是否需要棋盘移动
            alreadyOpen = false;
            alreadyShowTip = false;
            _activity.World.currentTracer?.Invalidate();
            _reward.Refresh(_activity);
            _cloudHolder.InitOnPreOpen(_activity.UnlockMaxLevel);
            _CalcMaskSize();
            RefreshGiftPack();
            _handbook.Setup(_activity);
            var p = _activity.GetProgressInfo(_activity.GetCurProgressPhase());
            if (p != null)
            {
                progress.Refresh(_activity.GetCurProgressNum(), p.BarNum);
                rewardIcon.SetImage(p.RewardIcon2);
                phaseText.text = (_activity.GetCurProgressPhase() + 1).ToString();
            }
            else
            {
                rewardNode.gameObject.SetActive(false);
                progress.Refresh(1, 1);
                progress.text.text = I18N.Text("#SysComDesc890");
                phaseText.text = _activity.GetCurProgressPhase().ToString();
            }
            milestoneNum.text = I18N.FormatText("#SysComDesc18", _activity.UnlockMaxLevel == 0 ? 1 : _activity.UnlockMaxLevel);
            milestoneIcon.SetImage(_activity.GetCurMilestone().Image);
            milestoneIcon.gameObject.SetActive(_activity.UnlockMaxLevel > MILESTONE_LIMIT_LEVEL);
            milestoneIcon.transform.parent.gameObject.SetActive(_activity.UnlockMaxLevel > MILESTONE_LIMIT_LEVEL);

            if (_activity.UnlockMaxLevel > MILESTONE_LIMIT_LEVEL)
            {
                var cfg = Env.Instance.GetItemConfig(_activity.GetCurMilestone().ItemId);
                bubbleIcon.SetImage(cfg.Icon);
                bubbleRoot.gameObject.SetActive(true);
            }
            else
            {
                bubbleRoot.gameObject.SetActive(false);
            }
            OnMergeItem(null, null, null);
            var world = _activity?.World;
            var board = world?.activeBoard;
            if (world != null)
            {
                world.onItemEvent += OnItemEvent;
            }

            if (board != null)
            {
                board.onItemMerge += OnMergeItem;
            }

            // 修改UIStatus的位置
            UIConfig.UIStatus.Open();
            MessageCenter.Get<UI_TOP_BAR_PUSH_STATE>().Dispatch(UIStatus.LayerState.AboveStatus);
            MessageCenter.Get<GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<FLY_ICON_FEED_BACK>().AddListener(FlyFeedBack);
            MessageCenter.Get<GAME_BOARD_TOUCH>().AddListener(_autoGuideController.Interrupt);
            MessageCenter.Get<GUIDE_OPEN>().AddListener(_autoGuideController.Interrupt);
            MessageCenter.Get<FLY_ICON_START>().AddListener(CheckNewFly);
            MessageCenter.Get<ACTIVITY_UPDATE>().AddListener(RefreshGiftPack);
            MessageCenter.Get<UI_BOARD_DRAG_ITEM_END_CUSTOM>().AddListener(OnDragItemEndCustom);

            MessageCenter.Get<UI_WISH_BOARD_MOVE_UP_FINISH>().AddListener(MoveDown);
            MessageCenter.Get<UI_WISH_BOARD_UNLOCK_ITEM>().AddListener(OnUnlockNewItem);
            MessageCenter.Get<UI_WISH_EXTREME_CASE_BLOCK>().AddListener(OnBoardExtremeCase);
            MessageCenter.Get<UI_WISH_PROGRESS_CHANGE>().AddListener(OnProgressChange);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<FLY_ICON_FEED_BACK>().RemoveListener(FlyFeedBack);
            MessageCenter.Get<GAME_BOARD_TOUCH>().RemoveListener(_autoGuideController.Interrupt);
            MessageCenter.Get<GUIDE_OPEN>().RemoveListener(_autoGuideController.Interrupt);
            MessageCenter.Get<FLY_ICON_START>().RemoveListener(CheckNewFly);
            MessageCenter.Get<ACTIVITY_UPDATE>().RemoveListener(RefreshGiftPack);
            MessageCenter.Get<UI_BOARD_DRAG_ITEM_END_CUSTOM>().RemoveListener(OnDragItemEndCustom);

            MessageCenter.Get<UI_WISH_BOARD_MOVE_UP_FINISH>().RemoveListener(MoveDown);
            MessageCenter.Get<UI_WISH_BOARD_UNLOCK_ITEM>().RemoveListener(OnUnlockNewItem);
            MessageCenter.Get<UI_WISH_EXTREME_CASE_BLOCK>().RemoveListener(OnBoardExtremeCase);
            MessageCenter.Get<UI_WISH_PROGRESS_CHANGE>().RemoveListener(OnProgressChange);
        }

        private void OnProgressChange(List<RewardCommitData> data, string icon, int _)
        {
            if (data != null && data.Count > 0)
            {
                block.gameObject.SetActive(true);
            }
            var cur = _activity.GetCurProgressNum();
            var max = _activity.GetProgressInfo(_activity.GetCurProgressPhase())?.BarNum ?? 1;
            _progressChangeQueue.Enqueue((data, icon, max, cur));
        }

        private void ProcessNextProgressChange()
        {
            if (_progressChangeQueue.Count == 0)
            {
                _isProcessingProgressChange = false;
                return;
            }
            _isProcessingProgressChange = true;
            var (data, icon, max, cur) = _progressChangeQueue.Dequeue();
            if (data != null && data.Count > 0)
            {
                CoProgressChange(data, icon, max, cur);
            }
            else
            {
                progress.Refresh(cur, max, 0.5f, () =>
                {
                    ProcessNextProgressChange();
                });
            }
        }

        private void CoProgressChange(List<RewardCommitData> data, string icon, int max, int cur)
        {
            progress.Refresh(max, max, 0.5f, ()
            =>
            {
                UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, rewardIcon.transform.position, data, icon, I18N.Text("#SysComDesc726"));
                block.gameObject.SetActive(false);
                var p = _activity.GetProgressInfo(_activity.GetCurProgressPhase());
                if (p != null)
                {
                    phaseText.text = (_activity.GetCurProgressPhase() + 1).ToString();
                    progress.Refresh(_activity.GetCurProgressNum(), max, 0.5f, () =>
                    {
                        ProcessNextProgressChange();
                    });
                    rewardIcon.SetImage(p.RewardIcon2);
                }
                else
                {
                    progress.Refresh(1, 1);
                    progress.text.text = I18N.Text("#SysComDesc890");
                    rewardNode.gameObject.SetActive(false);
                    phaseText.text = _activity.GetCurProgressPhase().ToString();
                }
            });
        }

        protected override void OnPostOpen()
        {
            _autoGuideController.SetUp(this);
            SetCloudLockPos();
            // 判断是否可以滚动
            if (_activity.IsReadyToMove)
            {
                UIManager.Instance.Block(true);
                StartMove();
            }
        }

        protected override void OnPreClose()
        {
            _giftPack = null;
            if (BoardViewWrapper.GetCurrentWorld() == null) return;
            _view.OnBoardLeave();
            BoardViewWrapper.PopWorld();
            _commonResSeq?.Kill();
        }

        protected override void OnPostClose()
        {
            _cloudHolder.Cleanup();
            var world = _activity?.World;
            var board = world?.activeBoard;
            if (world != null)
            {
                world.onItemEvent -= OnItemEvent;
            }
            if (board != null)
            {
                board.onItemMerge -= OnMergeItem;
            }
            _autoGuideController.Release();
            _progressChangeQueue.Clear();
            _isProcessingProgressChange = false;
            MessageCenter.Get<UI_TOP_BAR_POP_STATE>().Dispatch();
            MessageCenter.Get<GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
            _StopCoroutine();
        }
        #endregion

        #region 流程
        private void RegisterComp()
        {
            transform.Access(CompBoardPath, out _view);
            transform.Access($"{CompBoardPath}/Root/Mask", out boardMask);
            transform.Access("Content/BoardRewardNode", out _reward);
            transform.Access($"{ScrollPath}/CloudBoard", out _cloudHolder);
            transform.Access($"{ScrollPath}/LockBoard/Anchor/LockHolder", out lockView);
            transform.Access($"Content/AutoguideControll", out _autoGuideController);
            transform.Access($"{CompBoardPath}/Root/Pack", out packGroup);
            transform.Access($"{CompBoardPath}/Root/Pack/Icon", out packIcon);
            transform.Access($"{CompBoardPath}/Root/Pack/cd", out packCd);
            transform.Access("Content/BottomFly/FlyTarget/Entry", out _boardEntry);
            transform.Access($"{TopBgPath}/cd/cd", out _cd);
            transform.Access("Content/Milestone/Level/info", out _handbook);
        }

        private void Setup()
        {
            _reward.Setup();
            _cloudHolder.SetUp();
            _view.Setup();
            lockView.SetUp();
        }

        // 计算遮罩的大小与棋盘一致
        private void _CalcMaskSize()
        {
            var size = BoardViewManager.Instance.board.size;
            var cellSize = BoardViewManager.Instance.boardView.cellSize;
            var rect = boardMask.GetComponent<RectTransform>();

            rect.sizeDelta = new Vector2(cellSize * size.x, cellSize * size.y);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private void AddButton()
        {
            transform.AddButton($"{TopBgPath}/CloseBtn", OnClickClose).WithClickScale().FixPivot();
            transform.AddButton($"{TopBgPath}/HelpBtn", OnClickHelp).WithClickScale().FixPivot();
            transform.AddButton($"{CompBoardPath}/Root/Pack/Icon", OnClickPack).WithClickScale().FixPivot();
            talkBtn.onClick.AddListener(OnClickTalk);
            milestoneBtn.onClick.AddListener(OnClickMilestone);
            rewardBtn.onClick.AddListener(OnClickReward);
            goBtn.onClick.AddListener(OnClickGo);
            tipBtn.onClick.AddListener(OnClickTip);
        }

        private void OnClickTip()
        {
            tipRoot.gameObject.SetActive(false);
            tipBtn.gameObject.SetActive(false);
        }

        private void OnClickReward()
        {
            var list = Enumerable.ToList(_activity.GetProgressInfo(_activity.GetCurProgressPhase()).BarReward.Select(s => s.ConvertToRewardConfig()));
            UIManager.Instance.OpenWindow(UIConfig.UIActivityRewardTips, rewardBtn.transform.position, 35f, list);
        }

        private void OnClickTalk()
        {
            ShowTalkRoot(_activity.GetRandomKey());
            var cfg = Env.Instance.GetItemConfig(_activity.GetCurMilestone().ItemId);
            bubbleIcon.SetImage(cfg.Icon);
            bubbleRoot.gameObject.SetActive(_activity.UnlockMaxLevel > MILESTONE_LIMIT_LEVEL);
        }
        private void OnClickMilestone()
        {
            _activity.VisualUIMilestone.res.ActiveR.Open(_activity);
        }

        /// <summary>
        /// 显示气泡，带动画，内容可变。点击和自动都用这个。
        /// </summary>
        /// <param name="text">要显示的内容</param>
        private void ShowTalkRoot(string text)
        {
            _talkShowing = true;
            _talkTimer = 0f;
            talkRoot.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(text))
                talkText.text = _activity.GetRandomKey();
            _talkSeq?.Kill();
            talkRootGroup.alpha = 0;
            _talkSeq = DOTween.Sequence();
            _talkSeq.Append(talkRootGroup.DOFade(1, 0.5f)) // 0.5秒淡入
                .AppendInterval(4f) // 停留4秒
                .Append(talkRootGroup.DOFade(0, 0.5f)) // 0.5秒淡出
                .OnComplete(() =>
                {
                    talkRoot.gameObject.SetActive(false);
                    bubbleRoot.gameObject.SetActive(HasMilestoneItem());
                    _talkShowing = false;
                    _talkTimer = 0f;
                });
        }

        private void RefreshCd()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            _cd.SetCountDown(diff);
            if (_activity.World == null) return;
            if (_giftPack != null)
            {
                var diff1 = (long)Mathf.Max(0, _giftPack.endTS - t);
                packCd.SetCountDown(diff1);
                if (_activity.World.rewardCount <= 0 && !alreadyOpen)
                {
                    alreadyOpen = true;
                    UIManager.Instance.OpenWindow(_giftPack.Res.ActiveR, _giftPack);
                }
                else
                {
                    if (!alreadyShowTip && _activity.World.rewardCount <= 0)
                    {
                        tipRoot.gameObject.SetActive(true);
                        tipBtn.gameObject.SetActive(true);
                        tip1.gameObject.SetActive(_activity.GetCurGroupConfig().OrderItemId.Count > 0);
                        tip2.gameObject.SetActive(_giftPack != null && _giftPack.Valid);
                        tip3.gameObject.SetActive(_activity.GetCurGroupConfig().DropId.Count > 0);
                        goBtn.gameObject.SetActive(true);
                        alreadyShowTip = true;
                    }
                    else
                    {
                        goBtn.gameObject.SetActive(_activity.World.rewardCount <= 0);
                    }
                }
            }
            else
            {
                if (!alreadyShowTip && _activity.World.rewardCount <= 0)
                {
                    tipRoot.gameObject.SetActive(true);
                    tip1.gameObject.SetActive(_activity.GetCurGroupConfig().OrderItemId.Count > 0);
                    tip2.gameObject.SetActive(_giftPack != null && _giftPack.Valid);
                    tip3.gameObject.SetActive(_activity.GetCurGroupConfig().DropId.Count > 0);
                    tipBtn.gameObject.SetActive(true);
                    goBtn.gameObject.SetActive(true);
                    alreadyShowTip = true;
                }
                else
                {
                    goBtn.gameObject.SetActive(_activity.World.rewardCount <= 0);
                }
            }
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
            var move = transform.Find("Content/Center/BoardNode/CompBoard");
            move.localScale = new Vector3(scale, scale, scale);
            (root.parent as RectTransform).sizeDelta = new Vector2(scale * root.sizeDelta.x, scale * root.sizeDelta.y);
            (move.parent as RectTransform).sizeDelta = new Vector2(scale * root.sizeDelta.x, scale * root.sizeDelta.y);
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is WishBoardActivity)
            {
                Exit();
            }
        }

        public void Exit(bool ignoreFrom = false)
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

        public void OnClickHelp()
        {
            _activity.VisualUIHelp.res.ActiveR.Open(_activity);
        }

        //场上有可被吃棋子时 显示气泡
        private void OnMergeItem(Item src, Item dst, Item result)
        {
            if (_activity == null)
            {
                return;
            }
            if (HasMilestoneItem())
            {
                var cfg = Env.Instance.GetItemConfig(_activity.GetCurMilestone().ItemId);
                bubbleIcon.SetImage(cfg.Icon);
                bubbleRoot.gameObject.SetActive(true);
            }
        }

        private bool HasMilestoneItem()
        {
            var id = _activity.GetCurMilestone().ItemId;
            var dict = _activity.World.currentTracer.GetCurrentActiveBoardItemCount();
            return dict.ContainsKey(id) && _activity.UnlockMaxLevel > MILESTONE_LIMIT_LEVEL;
        }

        private IEnumerator CoDelayShowSpine()
        {
            yield return new WaitForSeconds(spineDelayTime);
            spine.gameObject.SetActive(true);
            spine.AnimationState.SetAnimation(0, "show", false).Complete += (_) =>
            {
                spine.gameObject.SetActive(false);
            };
        }

        private void OnDragItemEndCustom(Vector2 pos, Item item)
        {
            if (_activity == null)
            {
                return;
            }
            if (_activity.CheckMilestoneItemCanUse(item.config.Id))
            {
                StartCoroutine(CoDelayShowSpine());
                _activity.BeginDragReward(item);
                bubbleRoot.gameObject.SetActive(HasMilestoneItem());
            }
            else
            {
                // 返回原棋子
                _view.boardHolder.MoveBack(item);
            }
        }
        #endregion

        #region 飞奖励

        private void OnItemEvent(Item item, ItemEventType eventType)
        {
            if (eventType == ItemEventType.ItemEventRewardListOut)
            {
                _reward.Refresh(_activity);
            }
            //特殊情况 棋子移到奖励箱
            else if (eventType == ItemEventType.ItemEventMoveToRewardBox)
            {
                _reward.RefreshWithPunch();
            }
        }

        // 飞奖励完成事件回调
        private void FlyFeedBack(FlyableItemSlice slice)
        {
            if (slice.FlyType == FlyType.MergeItemFlyTarget)
            {
                _reward.FlyFeedBack(slice);
                tipRoot.gameObject.SetActive(_activity.World.rewardCount <= 0);
                tipBtn.gameObject.SetActive(_activity.World.rewardCount <= 0);
            }
            if (slice.FlyType == FlyType.WishBoardMilestone)
            {
                bubbleRoot.gameObject.SetActive(HasMilestoneItem());
                spine.gameObject.SetActive(true);
                spine.AnimationState.SetAnimation(0, "show", false).Complete += (_) =>
                {
                    spine.gameObject.SetActive(false);
                };
            }
            if (slice.FlyType == FlyType.WishBoardScore)
            {
                if (slice.CurIdx == 1)
                {
                    if (!_isProcessingProgressChange && _progressChangeQueue.Count > 0)
                    {
                        ProcessNextProgressChange();
                    }
                    levelAnimator.SetTrigger("Punch");
                    Game.Manager.audioMan.TriggerSound("MineFlyMilestoneToken");
                }
            }
        }

        // 飞奖励前 判断显示状态栏或入口
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

        // 显示主棋盘入口
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

        // 显示资源栏
        private void CheckCommonRes()
        {
            if (_isCheckCommonRes) return;
            _isCheckCommonRes = true;
            _commonResSeq = DOTween.Sequence();
            MessageCenter.Get<GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
            _commonResSeq.AppendInterval(1.5f);
            _commonResSeq.AppendCallback(() =>
            {
                MessageCenter.Get<GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
                _isCheckCommonRes = false;
                _commonResSeq = null;
            });
            _commonResSeq.OnKill(() =>
            {
                MessageCenter.Get<GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
                _isCheckCommonRes = false;
                _commonResSeq = null;
            });
            _commonResSeq.Play();
        }
        #endregion

        #region 手指引导
        public void CheckShowRefresh()
        {
            if (_isPlayingMove)
            {
                _autoGuideController.Interrupt();
                return;
            }
        }
        #endregion

        #region 礼包
        // 打开6格无限
        private void OnClickPack()
        {
            if (_giftPack != null)
            {
                UIManager.Instance.OpenWindow(_giftPack.Res.ActiveR, _giftPack);
            }
        }

        private void RefreshGiftPack()
        {
            _giftPack = Game.Manager.activity.LookupAny(EventType.WishEndlessPack) as PackEndlessWishBoard;
            if (_giftPack != null)
            {
                packGroup.alpha = 1;
                packGroup.interactable = true;
                packIcon.SetImage(_giftPack.EntryIcon);
            }
            else
            {
                packGroup.alpha = 0;
                packGroup.interactable = false;
            }
        }
        #endregion

        #region 滚动 + 收集
        // 预处理 通知数据层可以滚动
        public void StartMove()
        {
            _isPlayingMove = true;
            BoardViewManager.Instance.OnUserActive();

            IEnumerator coroutine()
            {
                yield return new WaitForSeconds(0.5f);
                _activity.StartMoveUpBoard(); // 数据层滚动
            }

            Game.Instance.StartCoroutineGlobal(coroutine());
        }

        // 数据层滚动完成 开始表现层滚动
        private void MoveDown(int downRowCount)
        {
            _isPlayingMove = true;

            PrepareMove(downRowCount);

            // 播放音效 滚动
            switch (downRowCount)
            {
                case 1:
                    Game.Manager.audioMan.TriggerSound("FarmboardBoarddownOne");
                    break;
                case 2:
                    Game.Manager.audioMan.TriggerSound("FarmboardBoarddownTwo");
                    break;
                case 3:
                    Game.Manager.audioMan.TriggerSound("FarmboardBoarddownThree");
                    break;
                case 4:
                    Game.Manager.audioMan.TriggerSound("FarmboardBoarddownFour");
                    break;
                default:
                    Game.Manager.audioMan.TriggerSound("FarmboardBoarddownOne");
                    break;
            }
            var moveDis = downRowCount * _view.cellSize * Game.Manager.mainMergeMan.mainBoardScale;
            var seqY = DOTween.Sequence();
            seqY.Append(DOTween
                    .To(() => scroll.localPosition, x => scroll.localPosition = x,
                        scroll.localPosition - new Vector3(0, moveDis, 0), moveTime * downRowCount)
                    .SetOptions(AxisConstraint.Y))
                .SetEase(moveCurve);
            seqY.OnComplete(OnMove);
        }

        // 滚动前准备工作
        private void PrepareMove(int rowCount)
        {
            // 根据数据层更新棋子位置
            BoardViewManager.Instance.boardView.boardHolder.ReFillItem();
            // 更新云层位置
            _cloudHolder.ReFillCloud();

            // 遮罩
            boardMask.enabled = true;

            // 计算开始滚动前root的位置
            var moveDis = rowCount * _view.cellSize * Game.Manager.mainMergeMan.mainBoardScale;
            scroll.localPosition += new Vector3(0, moveDis, 0);
            var size = Game.Manager.mergeBoardMan.activeWorld.activeBoard.size;
            tilling.SetTilling(new Vector2(size.x * 0.5f, (size.y + rowCount) * 0.5f));
            bgBoard.offsetMin = new Vector2(0, -_view.cellSize * rowCount);
        }

        // 滚动结束后处理
        private void OnMove()
        {
            var size = Game.Manager.mergeBoardMan.activeWorld.activeBoard.size;
            tilling.SetTilling(new Vector2(size.x * 0.5f, (size.y) * 0.5f));
            bgBoard.offsetMax = Vector2.zero;
            bgBoard.offsetMin = Vector2.zero;

            scroll.localPosition = Vector3.zero;
            _view.boardHolder.ReFillItem();
            boardMask.enabled = false;
            _isPlayingMove = false;
            SetCloudLockPos();

            //滚动结束后 主动检测一下目前是否有合成可能性
            _activity.CheckBoardExtremeCase();

            UIManager.Instance.Block(false);
        }

        private Coroutine _blockCoroutine;

        //棋盘遇到极端情况时界面block
        private void OnBoardExtremeCase()
        {
            _StopCoroutine();
            _blockCoroutine = StartCoroutine(_BoardExtremeBlock());
        }

        private IEnumerator _BoardExtremeBlock()
        {
            if (!UIManager.Instance.IsBlocking())
            {
                UIManager.Instance.Block(true);
            }

            //等1s解开block
            yield return new WaitForSeconds(1);

            if (UIManager.Instance.IsBlocking() && !_isPlayingMove && !_isPlayingMoveCloudUnlock)
            {
                UIManager.Instance.Block(false);
            }
        }

        private void _StopCoroutine()
        {
            if (_blockCoroutine != null)
            {
                StopCoroutine(_blockCoroutine);
                _blockCoroutine = null;

                //避免提前停掉 导致协程没走完
                if (UIManager.Instance.IsBlocking() && !_isPlayingMove && !_isPlayingMoveCloudUnlock)
                    UIManager.Instance.Block(false);
            }
        }

        #endregion

        #region 云层
        private void OnUnlockNewItem(Item itemData)
        {
            if (_activity.UnlockMaxLevel == 0)
            {
                return;
            }
            milestoneNum.text = I18N.FormatText("#SysComDesc18", _activity.UnlockMaxLevel);
            // 判断进度条解锁(云层解锁之前的里程碑)
            var idList = _activity.GetAllItemIdList();
            // 判断云层解锁
            var nextCloud = _cloudHolder.GetNextCloud();
            var unlockCloud = nextCloud?.UnlockLevel == _activity.UnlockMaxLevel;
            if (nextCloud != null)
            {
                if (unlockCloud)
                {
                    // 云层解锁 block玩家
                    UIManager.Instance.Block(true);
                    _isPlayingMoveCloudUnlock = true;
                    UnLockCloud(nextCloud, itemData);
                }
            }

            // 不解锁云的进度条动画
            if (!unlockCloud && _activity.UnlockMaxLevel > MILESTONE_LIMIT_LEVEL)
            {
                _handbook.Unlock(itemData, () =>
                {
                    StartCoroutine(CoPlayEfx());
                });
            }

            // 打开任务完成界面
            if (itemData.config.Id == idList[^1])
            {
            }
        }

        private void UnLockCloud(Cloud cloud, Item item)
        {
            // from 位置
            var from = BoardViewManager.Instance.CoordToWorldPos(item.coord);
            StartCoroutine(CoUnlockCloud(cloud, item, from));
        }

        private IEnumerator CoPlayEfx()
        {
            if (_activity.UnlockMaxLevel <= MILESTONE_LIMIT_LEVEL)
            {
                yield break;
            }
            yield return new WaitForSeconds(0.08f);
            efx.gameObject.SetActive(false);
            efx.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.15f);
            milestoneIcon.SetImage(_activity.GetCurMilestone().Image);
            Game.Manager.audioMan.TriggerSound("WishMilestoneChange");
            milestoneIcon.gameObject.SetActive(_activity.UnlockMaxLevel > MILESTONE_LIMIT_LEVEL);
            milestoneIcon.transform.parent.gameObject.SetActive(_activity.UnlockMaxLevel > MILESTONE_LIMIT_LEVEL);
            milestoneNum.text = I18N.FormatText("#SysComDesc18", _activity.UnlockMaxLevel == 0 ? 1 : _activity.UnlockMaxLevel);
            _handbook.Refresh();
        }

        private IEnumerator CoUnlockCloud(Cloud cloud, Item item, Vector3 from)
        {
            var to = lockView.Bubble.position;
            lockView.PlayOpen(); // 解锁
            UIFlyUtility.FlyCustom(item.config.Id, 1, from, to, FlyStyle.Common,
                FlyType.None, () =>
                {
                    StartCoroutine(CoPlayEfx());
                }, size: 136f);

            // 飞棋子时间 + 锁打开的时间
            yield return new WaitForSeconds(1f);
            // 云层消失
            PlayCloudUnlockAnim(cloud);

            // 云层动画时间 同时也要等云层表现被释放
            yield return new WaitForSeconds(1.5f);
            // 刷新云层数据
            _cloudHolder.FillCurShowCloud();
            _isPlayingMoveCloudUnlock = false;
            // yield return new WaitForSeconds(1f);

            // 判断是否可以滚动
            if (_activity.IsReadyToMove)
            {
                StartMove();
            }
            else
            {
                // 不滚动 解除block
                UIManager.Instance.Block(false);
                // 刷新锁的位置
                SetCloudLockPos();
            }
        }

        // 设置锁的世界坐标
        public void SetCloudLockPos()
        {
            var idList = _activity.GetAllItemIdList();

            // 判断是否还有云层未解锁
            if (_activity.UnlockMaxLevel >= idList.Count)
            {
                lockView.gameObject.SetActive(false);
            }

            // 获得下一个解锁的云的坐标
            var cloud = _cloudHolder.GetNextCloud();
            if (cloud == null)
            {
                lockView.gameObject.SetActive(false);
                return;
            }

            List<(int, int)> coords = Enumerable.ToList(cloud.CloudArea);

            if (coords.Count == 0)
            {
                return;
            }

            foreach (var coord in coords)
            {
                if (_cloudHolder.GetCloudViewByCoord(coord.Item1, coord.Item2, out var cloudView))
                {
                    cloudView.RefreshMask();
                }
            }

            var maxRectangle = FindMaxCompleteRectangle(coords);
            var (min_x, max_x, min_y, max_y) = FindMaxMinCoords(maxRectangle);

            var posMax = Vector3.zero;
            if (_cloudHolder.GetCloudViewByCoord(max_x, max_y, out var maxView))
            {
                posMax = maxView.transform.position;
            }

            var posMin = Vector3.zero;
            if (_cloudHolder.GetCloudViewByCoord(min_x, min_y, out var minView))
            {
                posMin = minView.transform.position;
            }

            var lockPos = (posMax + posMin) * 0.5f;


            if (lockPos == Vector3.zero)
            {
                Debug.LogError("[UIWishBoardMain] lock view position error");
                return;
            }

            var id = idList[cloud.UnlockLevel - 1];
            lockView.Init(id);
            lockView.transform.position = lockPos;
            lockView.gameObject.SetActive(true);
            // 播放锁的出现动画
            lockView.PlayShow();

            foreach (var coord in coords)
            {
                _cloudHolder.GetCloudViewByCoord(coord.Item1, coord.Item2, out var view);
                view.PlayPunch();
            }

            // 播放音效 云出现
            Game.Manager.audioMan.TriggerSound("FarmboardCloudCover");
        }

        // 获得一组坐标中的最值
        private (int, int, int, int) FindMaxMinCoords(List<(int, int)> coords)
        {
            int minX = coords[0].Item1;
            int maxX = coords[0].Item1;
            int minY = coords[0].Item2;
            int maxY = coords[0].Item2;

            foreach (var coord in coords)
            {
                if (coord.Item1 < minX)
                {
                    minX = coord.Item1;
                }

                if (coord.Item1 > maxX)
                {
                    maxX = coord.Item1;
                }

                if (coord.Item2 < minY)
                {
                    minY = coord.Item2;
                }

                if (coord.Item2 > maxY)
                {
                    maxY = coord.Item2;
                }
            }

            return (minX, maxX, minY, maxY);
        }

        // 找到最大的完整矩形区域
        private List<(int x, int y)> FindMaxCompleteRectangle(List<(int x, int y)> coords)
        {
            if (coords.Count == 0) return new List<(int, int)>();

            // 预处理：每个 x 对应的 y 区间（minY, maxY）
            var xYRange = new Dictionary<int, (int minY, int maxY)>();
            foreach (var (x, y) in coords)
            {
                if (!xYRange.ContainsKey(x))
                    xYRange[x] = (y, y);
                else
                    xYRange[x] = (Math.Min(xYRange[x].minY, y), Math.Max(xYRange[x].maxY, y));
            }

            var sortedX = Enumerable.ToList((xYRange.Keys.OrderBy(x => x)).ToArray()); // 排序 x 坐标

            int maxArea = 0;
            List<(int x, int y)> result = new List<(int, int)>();

            // 遍历所有 x 区间 [left, right]
            for (int i = 0; i < sortedX.Count; i++)
            {
                int left = sortedX[i];
                int currentMinY = xYRange[left].minY; // 初始 minY 为当前 x 的 minY
                int currentMaxY = xYRange[left].maxY; // 初始 maxY 为当前 x 的 maxY

                for (int j = i; j < sortedX.Count; j++)
                {
                    int rightX = sortedX[j];
                    // 更新公共 y 区间：取所有 x 的 minY 的最大值 和 maxY 的最小值
                    currentMinY = Math.Max(currentMinY, xYRange[rightX].minY);
                    currentMaxY = Math.Min(currentMaxY, xYRange[rightX].maxY);

                    if (currentMinY > currentMaxY) continue; // 无公共 y 区间，跳过

                    int width = rightX - left + 1; // x 区间宽度
                    int height = currentMaxY - currentMinY + 1; // y 区间高度
                    int area = width * height;

                    if (area > maxArea)
                    {
                        maxArea = area;
                        // 生成矩形内所有坐标（利用内部连续性，无需检查每个点）
                        result = Enumerable.ToList(Enumerable.Range(left, width)
                            .SelectMany(x => Enumerable.Range(currentMinY, height)
                                .Select(y => (x, y))));
                    }
                }
            }

            return result;
        }

        // 云层解锁动画
        private void PlayCloudUnlockAnim(Cloud cloud)
        {
            foreach (var area in cloud.CloudArea)
            {
                if (_cloudHolder.GetCloudViewByCoord(area.col, area.row, out var view))
                {
                    StartCoroutine(view.CoPlayUnlock());
                }
            }

            // 播放音效 云消失
            Game.Manager.audioMan.TriggerSound("FarmboardCloudFade");
        }
        #endregion
        
        public void OnNavBack()
        {
            if (!block.transform.gameObject.activeSelf)
            {
                Exit();
            }
        }
    }
}
