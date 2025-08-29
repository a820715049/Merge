// ================================================
// File: UIFarmBoardMain.cs
// Author: yueran.li
// Date: 2025/04/24 19:51:58 星期四
// Desc: 农场主棋盘
// ================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using FAT.Merge;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class UIFarmBoardMain : UIBase, IAutoGuide
    {
        // 美术效果控制
        public float moveTime;
        public AnimationCurve moveCurve;

        // 滚动
        private MBBoardView _view;
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform scroll;
        [SerializeField] private RectTransform bgBoard;
        [SerializeField] private UITilling tilling;
        private RectMask2D boardMask;

        // 奖励箱
        private MBFarmBoardReward _reward;

        // 云
        private MBBoardCloudHolder _cloudHolder;
        private MBLockView lockView;
        public MBLockView LockView => lockView;
        public MBBoardCloudHolder CloudHolder => _cloudHolder;

        // 进度条
        private MBFarmBoardProgress progress;

        // 收集
        [SerializeField] private RectTransform TempNode;
        [SerializeField] private RectTransform HideNodeContainer;
        [SerializeField] private GameObject TempIconHolder;

        // 手指引导
        private MBAutoGuideController _autoGuideController;
        public MBAutoGuideController AutoGuideController => _autoGuideController;
        private MBGuideTarget guideAnimal;
        private MBGuideTarget guideFarm;

        // 动物
        private MBFarmBoardAnimal mbAnimal;

        // 农场
        public MBFarmBoardFarm mbFarm;

        // token
        public MBFarmBoardToken mbtoken;

        // 礼包
        private CanvasGroup packGroup;
        private UIImageRes packIcon;
        private TextMeshProUGUI packCd;
        private PackEndlessFarm _giftPack;

        // 主棋盘入口
        private Image _boardEntry;

        // cd
        private TextMeshProUGUI _cd;

        private readonly string TopBgPath = $"Content/Top/TopBg";
        private readonly string CompBoardPath = $"Content/Center/BoardNode/CompBoard";
        private readonly string ScrollPath = $"Content/Center/BoardNode/CompBoard/Root/Mask/Scroll";
        private readonly string BottomPath = $"Content/Bottom";

        // 活动实例
        private FarmBoardActivity _activity;
        private List<MBFarmTempIcon> _tempIcons = new();
        private bool _isPlayingMove;
        private bool _isPlayingMoveCloudUnlock;

        #region Mono
        private void Update()
        {
            if (_isPlayingMove) return;
            BoardViewManager.Instance.Update(Time.deltaTime);
        }
        #endregion

        #region UI
        protected override void OnCreate()
        {
            RegisterComp();
            Setup();
            AddButton();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;
            _activity = (FarmBoardActivity)items[0];
            EnterBoard();
        }

        protected override void OnPreOpen()
        {
            // 通知数据层检查是否需要棋盘移动
            _activity.WorldTracer?.Invalidate();

            RefreshTheme();
            SetRewardBoxPos();
            _reward.Refresh(_activity);
            _cloudHolder.InitOnPreOpen(_activity);
            progress.InitOnPreOpen(_activity);
            _CalcMaskSize();
            RefreshGiftPack();
            mbAnimal.InitOnPreOpen(_activity);
            mbFarm.InitOnPreOpen(_activity);
            mbtoken.InitOnPreOpen(_activity);

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

            MessageCenter.Get<UI_FARM_BOARD_MOVE_UP_COLLECT>().AddListener(PrepareTempIcon);
            MessageCenter.Get<UI_FARM_BOARD_MOVE_UP_FINISH>().AddListener(MoveDown);
            MessageCenter.Get<UI_FARM_BOARD_UNLOCK_ITEM>().AddListener(OnUnlockNewItem);
            MessageCenter.Get<FARM_BOARD_SEED_CLOSE>().AddListener(mbFarm.OnSeedClose);
            MessageCenter.Get<FARM_BOARD_TOKEN_CHANGE>().AddListener(mbtoken.OnTokenChange);
            MessageCenter.Get<UI_FARM_EXTREME_CASE_BLOCK>().AddListener(OnBoardExtremeCase);
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

            MessageCenter.Get<UI_FARM_BOARD_MOVE_UP_COLLECT>().RemoveListener(PrepareTempIcon);
            MessageCenter.Get<UI_FARM_BOARD_MOVE_UP_FINISH>().RemoveListener(MoveDown);
            MessageCenter.Get<UI_FARM_BOARD_UNLOCK_ITEM>().RemoveListener(OnUnlockNewItem);
            MessageCenter.Get<FARM_BOARD_SEED_CLOSE>().RemoveListener(mbFarm.OnSeedClose);
            MessageCenter.Get<FARM_BOARD_TOKEN_CHANGE>().RemoveListener(mbtoken.OnTokenChange);
            MessageCenter.Get<UI_FARM_EXTREME_CASE_BLOCK>().RemoveListener(OnBoardExtremeCase);
        }

        protected override void OnPostOpen()
        {
            _autoGuideController.SetUp(this);
            progress.RefreshProgress();
            progress.ScrollToItem(_activity.UnlockMaxLevel);
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
            for (var i = _tempIcons.Count - 1; i >= 0; i--)
            {
                if (_tempIcons[i])
                {
                    Destroy(_tempIcons[i].gameObject);
                }
            }
            _tempIcons.Clear();

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
            transform.Access($"{CompBoardPath}/Root/BoardRewardNode", out _reward);
            transform.Access($"{ScrollPath}/CloudBoard", out _cloudHolder);
            transform.Access($"{ScrollPath}/LockBoard/Anchor/LockHolder", out lockView);
            transform.Access($"{CompBoardPath}/Root/ProgressNode", out progress);
            transform.Access($"Content/AutoguideControll", out _autoGuideController);
            transform.Access($"{BottomPath}/Animal/animalBtn", out guideAnimal);
            transform.Access($"{BottomPath}/Animal", out mbAnimal);
            transform.Access($"{BottomPath}/Farm", out guideFarm);
            transform.Access($"{CompBoardPath}/Root/Pack", out packGroup);
            transform.Access($"{CompBoardPath}/Root/Pack/Icon", out packIcon);
            transform.Access($"{CompBoardPath}/Root/Pack/cd", out packCd);
            transform.Access($"{BottomPath}/FlyTarget/Entry", out _boardEntry);
            transform.Access($"{TopBgPath}/cd/cd", out _cd);
        }

        private void Setup()
        {
            _reward.Setup();
            _cloudHolder.SetUp();
            _view.Setup();
            progress.SetUpOnCreate();
            mbAnimal.SetUp();
            mbFarm.SetUp();
            mbtoken.SetUp();
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
        }

        private void RefreshTheme()
        {
        }

        private void RefreshCd()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            _cd.SetCountDown(diff);

            if (_giftPack != null)
            {
                var diff1 = (long)Mathf.Max(0, _giftPack.endTS - t);
                packCd.SetCountDown(diff1);
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
            if (act is FarmBoardActivity)
            {
                Exit();
            }
        }

        public void Exit(bool ignoreFrom = false)
        {
            _activity.VisualAnimalTip.res.ActiveR.Close();
            _activity.VisualTokenTip.res.ActiveR.Close();
            ActivityTransit.Exit(_activity, ResConfig, null, ignoreFrom); // 退出活动时默认返回主棋盘
        }

        private void OnClickClose()
        {
           Exit();
        }
        
        public void OnClickHelp()
        {
            _activity.VisualHelp.res.ActiveR.Open(_activity);
        }

        // 拖拽结束后 判断是否有可喂食给动物的棋子 显示气泡Tip
        private void OnMergeItem(Item src, Item dst, Item result)
        {
            if (_activity == null)
            {
                return;
            }

            if (_activity.CheckHasConsumeItem() && mbAnimal.CurAnimalState == MBFarmBoardAnimal.AnimalState.Idle)
            {
                _activity.VisualAnimalTip.res.ActiveR.Open(mbAnimal.bubbleTrans.position, 0f,
                    _activity.GetConsumeItemId(), _activity);
            }
        }

        private void OnDragItemEndCustom(Vector2 pos, Item item)
        {
            if (_activity == null)
            {
                return;
            }

            if (item.config.Id != _activity.GetConsumeItemId() || _activity.CheckIsInOutput())
            {
                // 返回原棋子
                _view.boardHolder.MoveBack(item);
                return;
            }

            mbAnimal.OnDragItemEndCustom(item);
        }
        #endregion

        #region 飞奖励
        private void SetRewardBoxPos()
        {
            _activity?.SetRewardBoxPos(_reward.transform.position);
        }

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
            }

            if (slice.FlyType == FlyType.FarmToken)
            {
                mbtoken._tokenNum.SetText($"x{_activity.TokenNum.ToString()}");
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

        private bool _isCheckCommonRes;
        private bool _isTapBonus;
        private Sequence _commonResSeq;

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

            var id = _activity.GetConsumeItemId();
            Item item = BoardViewManager.Instance.FindItem(id, false);
            if (mbAnimal.CurAnimalState == MBFarmBoardAnimal.AnimalState.Idle && item != null)
            {
                var from = BoardUtility.GetWorldPosByCoord(item.coord);
                _autoGuideController.ShowFingerMove(guideAnimal.key, from);
            }
            else if (mbAnimal.CurAnimalState == MBFarmBoardAnimal.AnimalState.Reward)
            {
                _autoGuideController.ShowFinger(guideAnimal.key);
            }
            else if (_activity.TokenNum > 0)
            {
                _autoGuideController.ShowFinger(guideFarm.key);
            }
        }
        #endregion

        #region 礼包
        // 打开6格无限
        private void OnClickPack()
        {
            if (_giftPack != null)
            {
                // 关闭tips
                MessageCenter.Get<MSG.UI_CLOSE_LAYER>().Dispatch(UIConfig.UIFarmBoardAnimalTips.layer);
                UIManager.Instance.OpenWindow(_giftPack.Res.ActiveR, _giftPack);
            }
        }

        private void RefreshGiftPack()
        {
            _giftPack = Game.Manager.activity.LookupAny(EventType.FarmEndlessPack) as PackEndlessFarm;
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

        // 生成临时的ItemView 替换原view
        private void PrepareTempIcon(List<Item> collectItemList, int rowCount)
        {
            foreach (var item in collectItemList)
            {
                var go = Instantiate(TempIconHolder, HideNodeContainer);
                go.SetActive(true);
                var tempIconHolder = go.GetComponent<MBFarmTempIcon>();
                tempIconHolder.Init(item, HideNodeContainer, TempNode);
                _tempIcons.Add(tempIconHolder);
            }
        }

        // 数据层滚动完成 开始表现层滚动
        private void MoveDown(int downRowCount)
        {
            _isPlayingMove = true;

            PrepareMove(downRowCount, null);

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

            var factor = downRowCount == 1 ? 1f : (downRowCount / (downRowCount - 1f));
            var speed = _view.cellSize * Game.Manager.mainMergeMan.mainBoardScale * factor / moveTime;
            _tempIcons.ForEach(temp => temp.SetSpeed(speed));
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
        private void PrepareMove(int rowCount, List<Item> collectItemList)
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
            // 释放被收集的ItemView
        }
        
        private Coroutine _blockCoroutine;
        
        //农场棋盘遇到极端情况时界面block
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
            if (!unlockCloud)
            {
                var itemIndex = idList.IndexOf(itemData.config.Id);
                if (itemIndex != -1)
                {
                    StartCoroutine(progress.CoUnlockItem(itemData));
                }
            }

            // 打开任务完成界面
            if (itemData.config.Id == idList[^1])
            {
                StartCoroutine(CoOpenComplete());
            }
        }

        private IEnumerator CoOpenComplete()
        {
            // 进度条动画1s + 飞棋子动画1s + 等待0.5s
            yield return new WaitForSeconds(2.5f);
            _activity.VisualAnimalTip.res.ActiveR.Close();
            _activity.VisualTokenTip.res.ActiveR.Close();
            _activity.VisualComplete.res.ActiveR.Open(_activity);
        }

        private void UnLockCloud(Cloud cloud, Item item)
        {
            // from 位置
            var from = BoardViewManager.Instance.CoordToWorldPos(item.coord);
            StartCoroutine(CoUnlockCloud(cloud, item, from));
        }

        private IEnumerator CoUnlockCloud(Cloud cloud, Item item, Vector3 from)
        {
            var to = lockView.transform.position;
            lockView.PlayOpen(); // 解锁
            UIFlyUtility.FlyCustom(item.config.Id, 1, from, to, FlyStyle.Common,
                FlyType.None, () => { }, size: 136f);

            // 飞棋子时间 + 锁打开的时间
            yield return new WaitForSeconds(1f);
            // 云层消失
            PlayCloudUnlockAnim(cloud);

            // 云层看不见的时候 检查是否有种子包
            yield return new WaitForSeconds(0.6f);
            bool haveSeed = _activity.CheckHasFarmland(cloud.UnlockLevel);
            if (haveSeed)
            {
                // 有种子包 解除block
                UIManager.Instance.Block(false);
                UIManager.Instance.OpenWindow(UIConfig.UIFarmBoardGetSeed, _activity, lockView.transform.position);

                // 关闭tips
                MessageCenter.Get<MSG.UI_CLOSE_LAYER>().Dispatch(UIConfig.UIFarmBoardAnimalTips.layer);
            }

            // 云层动画时间 同时也要等云层表现被释放
            yield return new WaitForSeconds(1f);
            // 刷新云层数据
            _cloudHolder.FillCurShowCloud();
            StartCoroutine(progress.CoUnlockItemAfterCloud(item));
            _isPlayingMoveCloudUnlock = false;
            // yield return new WaitForSeconds(1f);

            // 判断是否有种子包 
            if (!haveSeed)
            {
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
                Debug.LogError("锁的计算位置错误");
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
    }
}