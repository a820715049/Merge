// ================================================
// File: UITrainOrderMain.cs
// Author: yueran.li
// Date: 2025/07/28 17:57:11 星期一
// Desc: 火车任务主界面
// ================================================

using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT.Merge;
using FAT.MSG;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UITrainMissionMain : UIBase, INavBack
    {
        public float scrollInTime = 2f;
        public float scrollOutTime = 2f;

        public AnimationCurve scrollOutCurve;

        public float recycleInTime = 2f;
        public float recycleOutTime = 2f;

        public float recycleFlyTime = 0.13f;

        // 标题
        private TextProOnACircle _title;

        // cd
        private TextMeshProUGUI _cd;

        // 进度条
        public UITrainMissionProgressModule ProgressModule;

        // 火车任务
        public UITrainMissionTrainModule TrainModule;
        private Vector2 _originalOffsetMax;

        // 棋盘
        private MBBoardView _view;
        public GameObject recycle;
        private Animator _recycleAnimator;

        // 背包
        public MBTrainMisssionBagEntry bag;
        public MBBoardMoveRoot boardDragRoot;

        public MBTrainMissionItemDetail detailCtrl;

        // 主棋盘入口
        private Image _boardEntry;

        // block
        private NonDrawingGraphic _block;

        // 活动实例 
        private TrainMissionActivity _activity;

        #region UI基础
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
            _view.Setup();
            bag.Setup();
            detailCtrl.Setup();
            boardDragRoot.Setup();
        }

        private void RegisterComp()
        {
            transform.Access("block", out _block);
            transform.Access("Content/Top/Title", out _title);
            transform.Access("Content/Top/_cd/cd", out _cd);
            transform.Access("Content/Root/CompBoard", out _view);
            transform.Access("Content/Root/CompBoard/Recycle", out _recycleAnimator);
            transform.Access($"Content/Bottom/FlyTarget/Entry", out _boardEntry);

            ProgressModule = AddModule(new UITrainMissionProgressModule(transform.Find("Content/Root/progress")));
            TrainModule = AddModule(new UITrainMissionTrainModule(transform.Find("Content/Root/Train/")));
        }

        private void AddButton()
        {
            transform.AddButton("Content/Top/HelpBtn", OnClickHelp).WithClickScale().FixPivot();
            transform.AddButton("Content/Top/CloseBtn", OnClickClose).WithClickScale().FixPivot();
            transform.AddButton("Content/Root/CompBoard/Recycle/mask/btn", OnClickRecycleBtn).WithClickScale()
                .FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (TrainMissionActivity)items[0];

            EnterBoard();
        }

        protected override void OnPreOpen()
        {
            // 刷新主题 换皮
            RefreshTheme();

            // 倒计时
            RefreshCd();

            ProgressModule.Show(_activity, this);
            TrainModule.Show(_activity, this);
            bag.InitOnPreOpen(_activity);
            detailCtrl.InitOnPreOpen();
            boardDragRoot.InitOnPreOpen();

            // 关闭进入动画
            _activity.ChangeAnimState(false);


            // 资源栏状态控制
            // MessageCenter.Get<UI_TOP_BAR_PUSH_STATE>().Dispatch(UIStatus.LayerState.AboveStatus); // 修改层级 // 注释的原因是为了不在above层级 让弹窗正确弹出
            MessageCenter.Get<GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(false); // 隐藏商店
            MessageCenter.Get<GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(false); // 隐藏等级
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<UI_BOARD_DRAG_ITEM_END_CUSTOM>().AddListener(OnDragItemEndCustom);
            MessageCenter.Get<UI_BOARD_DRAG_ITEM_CUSTOM>().AddListener(OnDragItemCustom);
            MessageCenter.Get<FLY_ICON_START>().AddListener(CheckNewFly);
            MessageCenter.Get<GAME_SHOP_ENTRY_STATE_CHANGE>().AddListener(OnShopStateChange);
            MessageCenter.Get<GAME_MERGE_PRE_BEGIN_REWARD>().AddListener(OnBeginReward);
            MessageCenter.Get<UI_BOARD_SELECT_ITEM>().AddListener(_OnMessageBoardSelectItem);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<UI_BOARD_DRAG_ITEM_END_CUSTOM>().RemoveListener(OnDragItemEndCustom);
            MessageCenter.Get<UI_BOARD_DRAG_ITEM_CUSTOM>().RemoveListener(OnDragItemCustom);
            MessageCenter.Get<FLY_ICON_START>().RemoveListener(CheckNewFly);
            MessageCenter.Get<GAME_SHOP_ENTRY_STATE_CHANGE>().RemoveListener(OnShopStateChange);
            MessageCenter.Get<GAME_MERGE_PRE_BEGIN_REWARD>().RemoveListener(OnBeginReward);
            MessageCenter.Get<UI_BOARD_SELECT_ITEM>().RemoveListener(_OnMessageBoardSelectItem);
        }

        protected override void OnPostOpen()
        {
            // 判断回收遮罩是否显示
            recycle.SetActive(_activity.waitRecycle);
            _originalOffsetMax = TrainModule.BottomTrain.viewport.offsetMax;
        }


        protected override void OnPreClose()
        {
            if (BoardViewWrapper.GetCurrentWorld() == null)
            {
                return;
            }

            // 需要在_view.OnBoardLeave()之前调用 
            // 不然world就被置空了
            detailCtrl.CleanupOnPostClose();

            _view.OnBoardLeave();
            BoardViewWrapper.PopWorld();
            _recycleSequence.Kill();
            _tapBonusSequence.Kill();
        }

        protected override void OnPostClose()
        {
            if (IsBlock)
            {
                SetBlock(false);
            }

            ProgressModule.Hide();
            TrainModule.Hide();

            bag.CleanupOnPostClose();
            boardDragRoot.CleanupOnPostClose();

            TrainModule.BottomTrain.viewport.offsetMax = _originalOffsetMax;

            // 资源栏状态控制
            MessageCenter.Get<UI_TOP_BAR_POP_STATE>().Dispatch();
            MessageCenter.Get<GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(true);
        }
        #endregion

        #region Mono
        private void Update()
        {
            BoardViewManager.Instance.Update(Time.deltaTime);
        }
        #endregion

        #region 事件
        private void RefreshCd()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            _cd.SetCountDown(diff);
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is TrainMissionActivity _ && !_activity.waitRecycle)
            {
                TrainMissionUtility.LeaveActivity();
            }
        }

        private void OnClickHelp()
        {
            UIManager.Instance.OpenWindow(_activity.VisualHelp.res.ActiveR, _activity);
        }

        private void OnClickClose()
        {
            if (TrainModule.BottomTrain.IsBlock() || TrainModule.TopTrain.IsBlock())
            {
                return;
            }

            if (_activity.waitRecycle || recycle.activeSelf) // 用UI辅助判断 处理多轮的情况
            {
                OnClickRecycleBtn();
                return;
            }

            TrainMissionUtility.LeaveActivity();
        }

        // 拖拽结束
        private void OnDragItemEndCustom(Vector2 pos, Item item)
        {
            if (_activity == null)
            {
                return;
            }

            if (_activity.PutItem(item))
            {
                //棋盘上也取消选中当前棋子
                BoardViewManager.Instance.CancelSelectCurItem();
                _view.boardEffect.ShowInventoryPutInEffect(pos);
                Game.Manager.audioMan.TriggerSound("InventoryPutIn");
                MessageCenter.Get<UI_INVENTORY_ENTRY_FEEDBACK>().Dispatch();
            }
            else
            {
                _view.boardHolder.MoveBack(item);
            }
        }

        // 拖拽中
        private void OnDragItemCustom(Vector2 pos, Item item)
        {
            if (_activity == null)
            {
                return;
            }

            _view.boardEffect.ShowInventoryInd(bag.transform.position);
            _view.boardEffect.HideHighlight();
        }


        private void OnShopStateChange(bool state)
        {
            if (state)
            {
                MessageCenter.Get<GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(false); // 隐藏商店
            }
        }

        private Item _selectedSourceItem = null;

        private void _OnMessageBoardSelectItem(Item item)
        {
            _selectedSourceItem = item;
        }

        private void OnBeginReward(RewardCommitData data)
        {
            var mgr = Game.Manager.objectMan;
            if (mgr == null)
            {
                return;
            }

            // 只处理token
            if (!mgr.IsType(data.rewardId, ObjConfigType.ActivityToken))
            {
                return;
            }

            var tokenConf = mgr.GetTokenConfig(data.rewardId);
            if (tokenConf != null && tokenConf.Feature == FeatureEntry.FeatureScore)
            {
                // 排除积分活动 不飞入口 飞积分活动滑动条
                return;
            }

            // 判断是否有对应图标的配置
            if (string.IsNullOrEmpty(Game.Manager.rewardMan.GetRewardIcon(data.rewardId, data.rewardCount).Asset))
            {
                return;
            }

            // 显示入口
            CheckTapBonus();

            // 从上次选择的棋子位置飞图标
            if (_selectedSourceItem != null)
            {
                var view = BoardViewManager.Instance.GetItemView(_selectedSourceItem.id);
                UIFlyUtility.FlyReward(data, view.transform.position);
            }
        }

        // 飞奖励前 判断显示状态栏或入口
        private void CheckNewFly(FlyableItemSlice slice)
        {
            if (_activity == null) return;

            // 能量 或者 飞完成任务的图标表现
            if (slice.ID == Constant.kMergeEnergyObjId || slice.FlyType == FlyType.MergeItemFlyTarget)
                return;

            // 判断是否有对应图标的配置
            if (string.IsNullOrEmpty(Game.Manager.rewardMan.GetRewardIcon(slice.ID, slice.Amount).Asset))
            {
                return;
            }

            var mgr = Game.Manager.objectMan;
            if (mgr == null)
            {
                return;
            }

            var tokenConf = mgr.GetTokenConfig(slice.ID);
            if (tokenConf != null && tokenConf.Feature == FeatureEntry.FeatureScore)
            {
                // 排除积分活动 不飞入口 飞积分活动滑动条
                return;
            }

            // 如果是活动ID 也显示入口

            if (mgr.IsType(slice.ID, ObjConfigType.Coin))
                return;

            if (mgr.IsType(slice.ID, ObjConfigType.MergeItem) || mgr.IsType(slice.ID, ObjConfigType.ActivityToken) ||
                slice.FlyType == FlyType.TapBonus ||
                slice.FlyType == FlyType.FlyToMainBoard)
            {
                CheckTapBonus();
            }
        }

        // private bool _isTapBonus;
        private Sequence _tapBonusSequence;

        // 显示主棋盘入口
        private void CheckTapBonus()
        {
            _tapBonusSequence?.Kill();
            _tapBonusSequence = DOTween.Sequence();
            _tapBonusSequence.Append(_boardEntry.DOFade(1, 0.5f));
            _tapBonusSequence.AppendInterval(0.5f);
            _tapBonusSequence.Append(_boardEntry.DOFade(0, 0.5f));
            _tapBonusSequence.OnComplete(() =>
            {
                var color = Color.white;
                color.a = 0;
                _boardEntry.color = color;
            });
            _tapBonusSequence.Play();
        }
        #endregion

        #region 棋盘
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
            var move = transform.Find("Content/Root/CompBoard");
            move.localScale = new Vector3(scale, scale, scale);
            (root.parent as RectTransform).sizeDelta = new Vector2(scale * root.sizeDelta.x, scale * root.sizeDelta.y);
            (move.parent as RectTransform).sizeDelta = new Vector2(scale * root.sizeDelta.x, scale * root.sizeDelta.y);
        }
        #endregion


        #region 回收
        private List<Item> _recycleItems;

        public void StartRecycle()
        {
            // 获得棋盘上的棋子
            _recycleItems = _activity.FinishRound();

            // 棋盘上没有棋子 直接退出
            if (_recycleItems.Count == 0)
            {
                TrainMissionUtility.LeaveActivity();
                return;
            }

            // 棋盘上取消选中当前棋子
            BoardViewManager.Instance.CancelSelectCurItem();
            MessageCenter.Get<UI_BOARD_SELECT_ITEM>().Dispatch(null);

            // 打开回收界面
            recycle.SetActive(true);
            _recycleAnimator.SetTrigger("Punch");

            // block
            SetBlock(true);

            // 回收火车驶入
            TrainModule.BottomTrain.ReleaseTrain();
            TrainModule.BottomTrain.CreateRecycleTrain(MBTrainMissionTrain.TrainPos.From);

            // 修改遮罩高度
            TrainModule.BottomTrain.viewport.offsetMax = new Vector2(_originalOffsetMax.x, _originalOffsetMax.y * 2);

            TrainModule.BottomTrain.ScrollIn(() => { SetBlock(false); });
        }

        // 点击回收按钮
        private void OnClickRecycleBtn()
        {
            recycle.SetActive(false);

            // block
            SetBlock(true);

            // 开箱动画
            TrainModule.BottomTrain.recycleTrain.OpenCover(_recycleItems.Count * recycleFlyTime, () =>
            {
                // 使用DOTween实现飞棋子动画表现
                _FlyItemsWithDOTween(_recycleItems);
            });
        }

        private Sequence _recycleSequence;

        // 使用DOTween实现飞棋子动画表现
        private void _FlyItemsWithDOTween(List<Item> items)
        {
            if (_view == null)
            {
                DebugEx.Error("train mission: boardView is null");
                SetBlock(false);
                return;
            }

            var to = TrainModule.BottomTrain.recycleTrain.boxPos.position;
            var interval = recycleFlyTime; // 一个奖励的时间是0.13s

            // 创建主序列
            _recycleSequence?.Kill();
            _recycleSequence = DOTween.Sequence();

            // 为每个棋子创建飞行动画 
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var v = _view.boardHolder.FindItemView(item.id);
                if (v != null)
                {
                    // 延迟飞棋子，创造连续飞行的效果
                    var delay = i * interval;

                    _recycleSequence.InsertCallback(delay,
                        () =>
                        {
                            Game.Manager.audioMan.TriggerSound("TrainItemFly"); // 火车-棋子回收-飞棋子
                            var seq = DOTween.Sequence();
                            seq.Append(v.transform.DOMove(to, interval)
                                .OnComplete(() => v.gameObject.SetActive(false)));
                            seq.Join(v.transform.DOScale(Vector3.one * 0.5f, interval / 2f).SetDelay(interval / 2f));
                        });
                }
                else
                {
                    DebugEx.Error("train mission: itemView is null");
                }
            }

            _recycleSequence.AppendCallback(() => { TrainModule.BottomTrain.recycleTrain.BlueComplete(); });

            _recycleSequence.AppendInterval(1.6f);

            // 所有棋子飞完后解锁界面
            _recycleSequence.OnComplete(() =>
            {
                SetBlock(false);

                var reward = _activity.GetRecycleReward();
                UIManager.Instance.OpenWindow(UIConfig.UITrainMissionRecycleReward, _activity, reward, this);
            });

            // 播放序列
            _recycleSequence.Play();
        }
        #endregion


        public bool IsBlock => _block.raycastTarget;

        public void SetBlock(bool value)
        {
            _block.raycastTarget = value;
        }

        public void OnNavBack()
        {
            if (IsBlock)
            {
                return;
            }

            OnClickClose();
        }

        private void RefreshTheme()
        {
            if (_activity == null)
            {
                return;
            }

            _title.SetText(I18N.Text("#SysComDesc1538"));
        }
    }
}