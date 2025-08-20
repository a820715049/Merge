/*
 * @Author: qun.chao
 * @Date: 2023-10-25 12:13:36
 */
using UnityEngine;
using EL;
using System;
using fat.conf;

namespace FAT
{
    public class UIMergeBoardMain : UIBase
    {
        protected UIMergeBoardMainContext mContext { get; set; } = new UIMergeBoardMainContext();
        private MBBoardView mBoardView;
        private long mLastCloseTime = -1;
        private GameObject _boardRoot;  //主棋盘根节点
        private Animator _animator;
        private bool _showState = true;

        protected override void OnCreate()
        {
            var _context = _GetContext();
            _context.RegisterModuleEntry(this);
            _context.RegisterModuleItemDetail(transform, "Adapter/Root/Bottom/CompDetail");
            _context.RegisterModuleOrder(transform, "Adapter/Root/CompOrder");
            _context.RegisterModuleReward(transform, "Adapter/Root/CompOrder/SV/Viewport/Content/Misc/DEAndRewardRoot/RewardRoot/Reward");
            _context.RegisterModuleInventoryEntry(transform, "Adapter/Root/Bottom/BtnInventory");
            _context.RegisterModuleBoardFly(transform, "Adapter/Root/BoardFly/ScoreBoardFly");
            _context.RegisterModuleMisc(transform, "Adapter/Root/CompOrder/SV/Viewport/Content/Misc");
            _context.RegisterModuleDragRoot(transform, "Adapter/Root/CompMoveOverride");
            _context.RegisterOrderBagTips(transform, "Adapter/Root/Bottom/OrderNeedBagBoard");
            _context.RegisterModuleActivityTips(transform, "Adapter/Root/CompOrder/ActivityTipsRoot");
            _context.Install();
            transform.FindEx("Adapter/Root", out _boardRoot);
            _animator = transform.FindEx<Animator>("Adapter/Root");
        }

        protected override void OnPreOpen()
        {
            //进入主棋盘
            _EnterMainBoard();
            DataTracker.TrackShowBoard();
        }

        private void _EnterMainBoard()
        {
            MessageCenter.Get<MSG.UI_MERGE_BOARD_MAIN_OPEN>().Dispatch();
            _boardRoot.SetActive(true);
            _showState = true;
            var world = Game.Manager.mainMergeMan.world;
            var tracer = Game.Manager.mainMergeMan.worldTracer;
            BoardViewWrapper.PushWorld(world);
            mBoardView.OnBoardEnter(world, tracer);
            _GetContext().InitOnPreOpen();
            _TickCloseTime();
            //界面打开时根据IsHideMainUI状态播不同动画
            _animator.SetTrigger(UIManager.Instance.IsHideMainUI ? UIManager.IdleHideAnimTrigger : UIManager.IdleShowAnimTrigger);
        }

        private void _ExitMainBoard()
        {
            mLastCloseTime = Game.Instance.GetTimestampSeconds();
            _GetContext().CleanupOnPostClose();
            mBoardView.OnBoardLeave();
            BoardViewWrapper.PopWorld();
            //棋盘界面关闭时 同步也关闭一些子界面
            UIManager.Instance.CloseWindow(UIConfig.UIPopFlyTips);
            UIManager.Instance.CloseWindow(UIConfig.UIEnergyBoostTips);
            _boardRoot.SetActive(false);
            _showState = false;
            MessageCenter.Get<MSG.UI_MERGE_BOARD_MAIN_CLOSE>().Dispatch();

          
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_MAIN_UI_STATE_CHANGE>().AddListener(_OnShowStateChange);
            MessageCenter.Get<MSG.GAME_MAIN_BOARD_STATE_CHANGE>().AddListener(_OnMainBoardStateChange);
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_MAIN_UI_STATE_CHANGE>().RemoveListener(_OnShowStateChange);
            MessageCenter.Get<MSG.GAME_MAIN_BOARD_STATE_CHANGE>().RemoveListener(_OnMainBoardStateChange);
        }

        protected override void OnPostClose()
        {
            _ExitMainBoard();
        }

        public void Setup()
        {
            transform.FindEx("Adapter/Root/CompBoard", out mBoardView);
            mBoardView.Setup();
        }

        private void Update()
        {
            if (!Game.Instance.isRunning)
                return;
            if (!_showState)
                return;
            BoardViewManager.Instance.Update(Time.deltaTime);
        }

        private void _TickCloseTime()
        {
            if (mLastCloseTime > 0)
            {
                BoardViewManager.Instance.SyncBoard(Game.Instance.GetTimestampSeconds() - mLastCloseTime);
            }
        }

        private UIMergeBoardMainContext _GetContext()
        {
            return mContext as UIMergeBoardMainContext;
        }

        private void _OnShowStateChange(bool isShow)
        {
            _animator.ResetTrigger(UIManager.IdleShowAnimTrigger);
            _animator.ResetTrigger(UIManager.IdleHideAnimTrigger);
            _animator.ResetTrigger(UIManager.OpenAnimTrigger);
            _animator.ResetTrigger(UIManager.CloseAnimTrigger);
            if (isShow)
            {
                //播放显示动画
                _animator.SetTrigger(UIManager.OpenAnimTrigger);
            }
            else
            {
                //播放隐藏动画
                _animator.SetTrigger(UIManager.CloseAnimTrigger);
            }
        }

        private void _OnMainBoardStateChange(bool isShow)
        {
            if (isShow)
                _EnterMainBoard();
            else
                _ExitMainBoard();
        }
    }
}