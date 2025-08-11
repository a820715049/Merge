/*
 *@Author:chaoran.zhang
 *@Desc:迷你棋盘UI脚本
 *@Created Time:2024.08.13 星期二 13:56:20
 */

using EL;
using FAT.Merge;
using FAT.MSG;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace FAT
{
    public class UIMergeBoardMini : UIBase
    {
        [SerializeField] private MBBoardView _mbBoardView;
        [SerializeField] private SkeletonGraphic _screen;
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _helpNode;
        [SerializeField] private GameObject _endNode;

        [FormerlySerializedAs("_order")] [SerializeField]
        private MBMiniBoardProgress progress;

        [SerializeField] private MBMiniBoardReward _reward;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _help;
        [SerializeField] private TextMeshProUGUI _end;
        [SerializeField] private GameObject _helpBtn;
        [SerializeField] private GameObject _endbtn;
        [SerializeField] private MiniBoardActivity _curAct;
        private bool _isEnd; //当前活动是否已经结束
        private bool _isComplete; //当前活动是否全部完成
        private bool _init; //是否经历过初始化
        private bool _playingAni;

        private enum State
        {
            Start,
            Idle,
            End
        }

        private State _curState;

        protected override void OnCreate()
        {
            //活动有效时才刷新棋盘
            _mbBoardView.Setup();
            transform.AddButton("Content/CloseBtn", ClickPlay);
            transform.AddButton("Content/Panel/Bottom/CompReward/PlayBtn", ClickPlay);
            transform.AddButton("Content/Panel/PlayBtn", ClickPlay);
            transform.AddButton("Content/Panel/EndBtn", ClickPlay);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
                _curAct = items[0] as MiniBoardActivity;
        }

        //关闭界面调用
        private void Exit()
        {
            if (_init)
            {
                Game.Manager.miniBoardMan.ExitMiniBoard(_curAct);
                _curAct = null;
            }
            else
            {
                Close();
            }

            _playingAni = false;
        }

        //点击play按钮
        private void ClickPlay()
        {
            switch (_curState)
            {
                case State.Start:
                {
                    PlayStartAnim();
                    Game.Manager.miniBoardMan.CurActivity.UIOpenState = true;
                    Game.Manager.guideMan.TriggerGuide();
                    break;
                }
                case State.Idle:
                {
                    Exit();
                    break;
                }
                case State.End:
                {
                    Exit();
                    break;
                }
            }
        }

        private void PlayStartAnim()
        {
            if (_playingAni)
                return;
            _playingAni = true;
            _helpNode.GetComponent<Animator>()?.SetTrigger("Hide");
            _screen.AnimationState.SetAnimation(0, "open", false).Complete += entry =>
            {
                _screen.AnimationState.SetAnimation(0, "idle", true);
                _curState = State.Idle;
                SetUIState();
            };
        }

        protected override void OnPreOpen()
        {
            Game.Manager.screenPopup.Block(true, false);
            if (!_curAct.Active)
            {
                _init = false;
                _curState = State.End;
                _mbBoardView.gameObject.SetActive(false);
                UIUtility.CountDownFormat(_cd, 0);
                _isEnd = true;
                _isComplete = _curAct.UnlockMaxLevel == Game.Manager.configMan
                    .GetEventMiniBoardDetailConfig(_curAct.DetailId).LevelItem.Count - 1;
                SetEndState();
                progress.End(_curAct.UnlockMaxLevel, _curAct);
                return;
            }

            _init = true;
            //各个组件的初始化，棋盘，奖励箱，进度条
            SetComp();
            //换皮
            SetTheme();
            //UI状态，幕布、文本、帮助界面、结束界面
            SetUIState();
            //progress.Refresh();
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<UI_MINI_BOARD_UNLOCK_ITEM>().AddListener(UnlockItem);
            BoardViewWrapper.GetCurrentWorld().onRewardListChange += _reward.RefreshReward;
        }

        private void SetComp()
        {
            _mbBoardView.gameObject.SetActive(true);
            progress.SetUp();
            _reward.SetUp();
            var world = Game.Manager.miniBoardMan.World;
            var tracer = Game.Manager.miniBoardMan.WorldTracer;
            BoardViewWrapper.PushWorld(world);
            _mbBoardView.OnBoardEnter(world, tracer);
        }

        private void SetTheme()
        {
            Game.Manager.miniBoardMan.CurActivity.BoardTheme.Refresh(_title, "mainTitle");
            if (Game.Manager.miniBoardMan.GetCurDetailConfig() != null)
            {
                var cfg = Game.Manager.objectMan.GetBasicConfig(Game.Manager.miniBoardMan.GetCurDetailConfig()
                    .LevelItem[0]);
                var sprite = "<sprite name=\"" + cfg.Icon.ConvertToAssetConfig().Asset + "\">";
                _help.text = I18N.FormatText("#SysComDesc484", sprite);
            }

            var cd = Mathf.Max(0, Game.Manager.miniBoardMan.CurActivity.Countdown);
            UIUtility.CountDownFormat(_cd, (long)cd);
        }

        private void SetUIState()
        {
            if (Game.Manager.miniBoardMan.IsValid)
            {
                _isEnd = Game.Manager.miniBoardMan.CurActivity.Countdown <= 0;
                _isComplete = Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() >=
                              Game.Manager.miniBoardMan.GetCurDetailConfig().LevelItem.Count - 1;
            }
            else
            {
                _isEnd = true;
                _isComplete = _curAct.UnlockMaxLevel >=
                              Game.Manager.configMan.GetEventMiniBoardDetailConfig(_curAct.DetailId).LevelItem.Count -
                              1;
            }

            if (_isEnd)
                SetEndState();
            else if (Game.Manager.miniBoardMan.CurActivity.UIOpenState)
                SetIdleState();
            else
                SetStartState();
        }

        private void SetEndState()
        {
            _curState = State.End;
            _endbtn.SetActive(true);
            _reward.transform.GetChild(0).gameObject.SetActive(false);
            _reward.transform.GetChild(1).gameObject.SetActive(false);
            _screen.AnimationState.SetAnimation(0, "close", false);
            _endNode.gameObject.SetActive(true);
            _helpNode.gameObject.SetActive(false);
            _helpBtn.SetActive(false);
            _end.text = I18N.Text(_isComplete ? "#SysComDesc490" : "#SysComDesc273");
            progress.transform.SetParent(_endNode.transform, true);
        }

        private void SetIdleState()
        {
            _curState = State.Idle;
            _endbtn.SetActive(false);
            _screen.AnimationState.SetAnimation(0, "idle", true);
            _reward.ShowReward();
            _endNode.gameObject.SetActive(false);
            _helpNode.gameObject.SetActive(false);
            _helpBtn.SetActive(false);
            progress.transform.SetParent(_root.transform, true);
        }

        private void SetStartState()
        {
            _curState = State.Start;
            _endbtn.SetActive(false);
            _reward.transform.GetChild(0).gameObject.SetActive(false);
            _reward.transform.GetChild(1).gameObject.SetActive(false);
            _screen.AnimationState.SetEmptyAnimation(0, 0);
            _endNode.gameObject.SetActive(false);
            _helpNode.gameObject.SetActive(false);
            _helpBtn.SetActive(true);
            progress.transform.SetParent(_root.transform, true);
        }

        protected override void OnPostOpen()
        {
            if (_isEnd)
                return;
            BoardViewManager.Instance.CalcOrigin();
            BoardViewManager.Instance.CalcScaleCoe();
            BoardViewManager.Instance.CalcSize();
            progress.Refresh();
            if (_curState == State.Start)
            {
                _helpNode.gameObject.SetActive(true);
                _helpNode.GetComponent<Animator>()?.SetTrigger("Show");
            }
        }

        protected override void OnPreClose()
        {
            if (!_init)
            {
                Close();
                return;
            }

            _mbBoardView.OnBoardLeave();
            BoardViewWrapper.GetCurrentWorld().onRewardListChange -= _reward.RefreshReward;
            BoardViewWrapper.PopWorld();
            UIManager.Instance.CloseWindow(UIConfig.UIPopFlyTips);
            UIManager.Instance.CloseWindow(UIConfig.UIEnergyBoostTips);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<UI_MINI_BOARD_UNLOCK_ITEM>().RemoveListener(UnlockItem);
        }

        protected override void OnPostClose()
        {
            Game.Manager.screenPopup.Block(false, false);
        }

        //Unity Update
        private void Update()
        {
            if (!Game.Instance.isRunning)
                return;
            if (!IsShow())
                return;
            BoardViewManager.Instance.Update(Time.deltaTime);
        }

        private void RefreshCD()
        {
            if (_isEnd)
                return;
            if (!_curAct.Active)
            {
                ActivityEnd();
                return;
            }

            var cd = _curAct.Countdown <= 0
                ? 0
                : _curAct.Countdown;
            if (cd == 0)
                ActivityEnd();
            UIUtility.CountDownFormat(_cd, cd);
        }

        private void ActivityEnd()
        {
            if (_isEnd)
                return;
            _isEnd = true;
            _screen.AnimationState.SetAnimation(0, "close", false);
            _endNode.gameObject.SetActive(true);
            _reward.ActivityEnd();

            SetEndState();
        }

        private void UnlockItem(Item item)
        {
            if (_curState == State.Start)
                return;
            progress.UnlockNew(item);
        }
    }
}