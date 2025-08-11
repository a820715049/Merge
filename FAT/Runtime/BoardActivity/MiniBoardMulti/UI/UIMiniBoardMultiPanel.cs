/*
 *@Author:chaoran.zhang
 *@Desc:迷你棋盘主界面
 *@Created Time:2025.01.10 星期五 17:56:17
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using FAT.Merge;
using FAT.MSG;
using fat.rawdata;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FAT
{
    public class UIMiniBoardMultiPanel : UIBase
    {
        public SkeletonGraphic screenSpine;
        public float height;
        public GameObject hand;
        public int interval;

        //组件
        private TextMeshProUGUI _cd;
        private TextProOnACircle _title;
        private TextMeshProUGUI _helpTips;
        private TextMeshProUGUI _endDesc1;
        private TextMeshProUGUI _endDesc2;
        private UITextState _btnText;
        private MBMiniBoardMultiProgress _progress;
        private MBBoardView _view;
        private MBMiniBoardMultiReward _reward;
        private UIImageRes _keyIcon;

        //GameObject
        private GameObject _helpNode;
        private GameObject _endNode;
        private GameObject _playBtn;
        private GameObject _clickMask;

        //transform
        private RectTransform _scale;
        private Transform _boardNode;
        private Transform _keyNode;
        private Transform _tempNode;
        private Transform _center;
        private Transform _normalNode;

        private MiniBoardMultiActivity _activity;
        private Item _gate;
        private bool _isEnd;
        private bool _hasInit;
        private int _interval;
        private bool _showEnd;

        #region 组件初始化

        protected override void OnCreate()
        {
            RegisterComp();
            RegisterObject();
            RegisterTransform();
            CheckCompSetup();
            AddButton();
        }

        /// <summary>
        /// 注册组件信息
        /// </summary>
        private void RegisterComp()
        {
            transform.Access("Content/ScaleNode/Title", out _title);
            transform.Access("Content/ScaleNode/cd", out _cd);
            transform.Access("Content/ScaleNode/HelpNode/Root/Content/HelpTips", out _helpTips);
            transform.Access("Content/ScaleNode/EndNode/Root/Desc1", out _endDesc1);
            transform.Access("Content/ScaleNode/EndNode/Root/Desc2", out _endDesc2);
            transform.Access("Content/ScaleNode/PlayBtn/Text", out _btnText);
            transform.Access("Content/ScaleNode/NormalNode/ProgressNode", out _progress);
            transform.Access("Content/ScaleNode/BoardNode/CompBoard", out _view);
            transform.Access("Content/ScaleNode/CompReward", out _reward);
            transform.Access("Content/ScaleNode/KeyMask/KeyNode/key", out _keyIcon);
        }

        /// <summary>
        /// 注册Object
        /// </summary>
        private void RegisterObject()
        {
            _helpNode = transform.Find("Content/ScaleNode/HelpNode/Root").gameObject;
            _endNode = transform.Find("Content/ScaleNode/EndNode/Root").gameObject;
            _playBtn = transform.Find("Content/ScaleNode/PlayBtn").gameObject;
            _clickMask = transform.Find("Content/ScaleNode/ClickMask").gameObject;
        }

        /// <summary>
        /// 注册Transform
        /// </summary>
        private void RegisterTransform()
        {
            _scale = transform.Find("Content/ScaleNode") as RectTransform;
            _boardNode = transform.Find("Content/ScaleNode/BoardNode");
            _keyNode = transform.Find("Content/ScaleNode/KeyMask/KeyNode");
            _tempNode = transform.Find("Content/ScaleNode/TempNode");
            _center = transform.Find("Content/Center");
            _normalNode = transform.Find("Content/ScaleNode/NormalNode");
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void CheckCompSetup()
        {
            _progress.SetUp();
            _view.Setup();
        }

        /// <summary>
        /// 注册按钮点击函数
        /// </summary>
        private void AddButton()
        {
            transform.AddButton("Content/ScaleNode/CloseBtn", ClickExit);
            transform.AddButton("Content/ScaleNode/HelpBtn", ClickHelp);
            transform.AddButton("Content/ScaleNode/PlayBtn", ClickPlay);
            transform.AddButton("Content/ScaleNode/CompReward/PlayBtn", ClickPlay);
        }

        /// <summary>
        /// 点击关闭按钮
        /// </summary>
        private void ClickExit()
        {
            if (_showEnd)
            {
                ClickPlay();
                return;
            }
            if (!_activity.UIOpenState && !_isEnd)
            {
                ClickPlay();
                return;
            }

            if (_hasInit)
                Game.Manager.miniBoardMultiMan.ExitMiniBoard(_activity);
            else
                Close();
        }

        /// <summary>
        /// 点击帮助按钮
        /// </summary>
        private void ClickHelp()
        {
            if (Game.Manager.activity.mapR.ContainsKey(_activity))
                UIManager.Instance.OpenWindow(_activity.HelpResAlt.ActiveR);
        }

        /// <summary>
        /// 点击Play按钮
        /// </summary>
        private void ClickPlay()
        {
            if (_showEnd)
                PlayStartAnim();
            else if (!_activity.UIOpenState && Game.Manager.activity.mapR.ContainsKey(_activity))
                PlayStartAnim();
            else
                ClickExit();
        }

        #endregion

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
                _activity = items[0] as MiniBoardMultiActivity;
            _activity?.BoardTheme.Refresh(_title, "mainTitle");
        }

        protected override void OnPreOpen()
        {
            _hasInit = false;
            _isEnd = false;
            Game.Manager.screenPopup.Block(true, false);
            InitUIState();
            UIUtility.CountDownFormat(_cd, _activity.Countdown > 0 ? _activity.Countdown : 0);
            var world = Game.Manager.miniBoardMultiMan.World;
            var tracer = Game.Manager.miniBoardMultiMan.WorldTracer;
            if (_isEnd) return;
            _hasInit = true;
            BoardViewWrapper.PushWorld(world);
            _view.OnBoardEnter(world, tracer);
            _gate = null;
            world.WalkAllItem(item =>
            {
                item.TryGetItemComponent<ItemFeatureComponent>(out var component);
                if (component != null) _gate = component.item;
            });
            BoardViewWrapper.GetCurrentWorld().onRewardListChange += _reward.RefreshReward;
            RefreshScale();
        }

        private void RefreshScale()
        {
            var board = UIManager.Instance.TryGetUI(UIConfig.UIMergeBoardMain)?.TryFind("Adapter/Root/CompBoard");
            if (board == null) return;
            var rect1 = _scale.sizeDelta;
            var rect2 = board.transform as RectTransform;
            if (rect2 == null) return;
            if (rect2.localScale.x < 1) return;
            _view.transform.localScale = board.transform.localScale;
            _scale.sizeDelta = new Vector2(rect1.x,
                height +
                rect2.sizeDelta.y * (rect2.localScale.y - 1));
        }

        /// <summary>
        /// 初始化UI显示状态
        /// </summary>
        private void InitUIState()
        {
            _helpNode.SetActive(false);
            if (_activity.UIOpenState) screenSpine.AnimationState.SetAnimation(0, "idle", true);
            _playBtn.SetActive(!_activity.UIOpenState || !Game.Manager.activity.mapR.ContainsKey(_activity));
            if (Game.Manager.miniBoardMultiMan.CheckHasNextBoard())
                _keyIcon.SetImage(Game.Manager.miniBoardMultiMan.GetCurInfoConfig().KeyIcon.ConvertToAssetConfig());
            SetUIState();
        }

        private void SetUIState()
        {
            if (!Game.Manager.activity.mapR.ContainsKey(_activity))
                SetEndState();
            else if (!_activity.UIOpenState)
                SetStartState();
            else
                SetIdleState();
        }

        /// <summary>
        /// 结束状态
        /// </summary>
        private void SetEndState()
        {
            _clickMask.SetActive(true);
            _isEnd = true;
            _reward.HideReward();
            _helpNode.SetActive(false);
            var isComplete = _activity.UnlockMaxLevel >=
                             Game.Manager.configMan.GetEventMiniBoardMultiGroupConfig(_activity.GroupId).Milestone - 1;
            _endDesc1.text = I18N.Text(isComplete ? "#SysComDesc490" : "#SysComDesc273");
            _endDesc2.text = I18N.Text("#SysComDesc486");
            screenSpine.AnimationState.SetAnimation(0, "close", false).Complete +=
                entry => { _endNode.SetActive(true); };
            Game.Manager.audioMan.TriggerSound("MiniboardMultiCurtainClose");
            UIUtility.CountDownFormat(_cd, 0);
            _btnText.Select(1);
            _progress.transform.SetParent(_tempNode, true);
            CheckCompInit();
        }

        /// <summary>
        /// 开始状态
        /// </summary>
        private void SetStartState()
        {
            _clickMask.SetActive(true);
            _endNode.SetActive(false);
            _btnText.Select(0);
            _reward.SetUp();
            _reward.HideReward();
            screenSpine.AnimationState.SetEmptyAnimation(0, 0);
            _progress.transform.SetParent(_normalNode, true);
            CheckCompInit();
        }

        /// <summary>
        /// 正常游玩状态
        /// </summary>
        private void SetIdleState()
        {
            _endNode.SetActive(false);
            _playBtn.SetActive(false);
            _clickMask.SetActive(false);
            _reward.SetUp();
            _reward.ShowReward();
            _progress.transform.SetParent(_normalNode, true);
            CheckCompInit();
        }

        private void CheckCompInit()
        {
            _progress.CheckInit(_activity);
            _progress.Refresh();
        }


        private bool _playingAni;

        /// <summary>
        /// 播放幕布拉开动画
        /// </summary>
        private void PlayStartAnim()
        {
            if (_playingAni)
                return;
            _activity.UIOpenState = true;
            _playingAni = true;
            _helpNode.GetComponent<Animator>()?.SetTrigger("Hide");
            Game.Manager.audioMan.TriggerSound("MiniboardMultiCurtainOpen");
            _endNode.SetActive(false);
            screenSpine.AnimationState.SetAnimation(0, "open", false).Complete += entry =>
            {
                screenSpine.AnimationState.SetAnimation(0, "idle", true);
                _helpNode.SetActive(false);
                _reward.ShowReward();
                _playBtn.SetActive(false);
                _playingAni = false;
                _clickMask.SetActive(false);
                Game.Manager.guideMan.TriggerGuide();
                if (_showEnd)
                {
                    _progress.StartTextChange();
                    _showEnd = false;
                }
            };
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().AddListener(ActivityEnd);
            MessageCenter.Get<UI_MINI_BOARD_MULTI_UNLOCK_ITEM>().AddListener(UnlockItem);
            MessageCenter.Get<UI_MINI_BOARD_MULTI_FINISH>().AddListener(AllFinish);
            MessageCenter.Get<UI_MINI_BOARD_MULTI_COLLECT>().AddListener(EnterNextRound);
            MessageCenter.Get<UI_MINI_BOARD_MULTI_INHERIT_ITEM>().AddListener(GetInheritItem);
            MessageCenter.Get<UI_CLICK_LOCK_REWARD>().AddListener(ShowLockRewardTip);
            MessageCenter.Get<UI_CLICK_LOCK_DOOR>().AddListener(ShowLockDoorTip);
            MessageCenter.Get<GAME_BOARD_TOUCH>().AddListener(HideHand);
            MessageCenter.Get<UI_MINI_BOARD_SHOW_END>().AddListener(ShowEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(ActivityEnd);
            MessageCenter.Get<UI_MINI_BOARD_MULTI_UNLOCK_ITEM>().RemoveListener(UnlockItem);
            MessageCenter.Get<UI_MINI_BOARD_MULTI_FINISH>().RemoveListener(AllFinish);
            MessageCenter.Get<UI_MINI_BOARD_MULTI_COLLECT>().RemoveListener(EnterNextRound);
            MessageCenter.Get<UI_MINI_BOARD_MULTI_INHERIT_ITEM>().RemoveListener(GetInheritItem);
            MessageCenter.Get<UI_CLICK_LOCK_REWARD>().RemoveListener(ShowLockRewardTip);
            MessageCenter.Get<UI_CLICK_LOCK_DOOR>().RemoveListener(ShowLockDoorTip);
            MessageCenter.Get<GAME_BOARD_TOUCH>().RemoveListener(HideHand);
            MessageCenter.Get<UI_MINI_BOARD_SHOW_END>().RemoveListener(ShowEnd);
        }

        private void ShowEnd()
        {
            _clickMask.SetActive(true);
            _showEnd = true;
            _reward.HideReward();
            _helpNode.SetActive(false);
            _endDesc1.text = I18N.Text("#SysComDesc490");
            _endDesc2.text = I18N.Text("#SysComDesc1117");
            IEnumerator delay()
            {
                yield return new WaitForSeconds(0.7f);
                _endNode.SetActive(true);
                _playBtn.SetActive(true);            }
            Game.StartCoroutine(delay());
            screenSpine.AnimationState.SetAnimation(0, "close", false).Complete +=
                entry =>
                {
                    _clickMask.SetActive(false);
                };
            Game.Manager.audioMan.TriggerSound("MiniboardMultiCurtainClose");
            _btnText.Select(2);
            _progress.transform.SetParent(_tempNode, true);
        }


        private void ActivityEnd(ActivityLike _act, bool isNew = false)
        {
            if (_act.Id != _activity.Id) return;
            if (BoardViewWrapper.GetCurrentWorld() != null && _hasInit)
            {
                _view.OnBoardLeave();
                BoardViewWrapper.GetCurrentWorld().onRewardListChange -= _reward.RefreshReward;
                BoardViewWrapper.PopWorld();
            }
        }

        private void RefreshCd()
        {
            if (_isEnd) return;
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
            if (Game.Manager.miniBoardMultiMan.CheckCanEnterNextRound() &&
                UIManager.Instance.GetLayerRootByType(UILayer.AboveStatus).childCount == 0 &&
                UIManager.Instance.GetLayerRootByType(UILayer.SubStatus).childCount == 0 &&
                !UIManager.Instance.IsOpen(UIConfig.UIGuide)) _interval++;
            if (_interval > interval && Game.Manager.miniBoardMultiMan.IsValid) ShowHand();
            if (!Game.Manager.activity.mapR.ContainsKey(_activity))
            {
                if (_playingAni) return;
                SetEndState();
            }
        }

        private void UnlockItem(Item item)
        {
            _progress.UnlockNew(item);
        }

        private void AllFinish()
        {
            _progress.MoveToEnd();
            UIManager.Instance.OpenWindow(_activity.GetKeyResAlt.ActiveR, _gate, _keyNode.position,
                new Action(() =>
                {
                    OpenGate();
                    _reward.ShowReward();
                }));
        }

        private void OpenGate()
        {
            if (_gate == null) return;
            var holder = _view.boardHolder.FindItemView(_gate.id).GetComponent<MBItemContent>().Holder;
            if (holder == null) return;
            var resHolder = holder.ResHolder as MBResHolderMiniBoard;
            if (resHolder == null) return;
            resHolder.SetOpenState(Game.Manager.miniBoardMultiMan.CheckHasNextBoard() &&
                                   Game.Manager.miniBoardMultiMan.CheckCanEnterNextRound());
        }

        private void EnterNextRound()
        {
            Game.Instance.StartCoroutineGlobal(PlayEnterNextRound());
        }

        private List<Item> _inherit = new();

        private void GetInheritItem(Dictionary<int, int> dictionary)
        {
            var world = Game.Manager.miniBoardMultiMan.World;
            if (world == null) return;
            _inherit.Clear();
            world.WalkAllItem((item) =>
            {
                if (dictionary.ContainsKey(item.tid) && _inherit.Count(i => i.tid == item.tid) < dictionary[item.tid])
                    _inherit.Add(item);
            });
        }

        private IEnumerator PlayEnterNextRound()
        {
            UIManager.Instance.Block(true);
            _keyNode.localScale = Vector3.zero;
            yield return new WaitForSeconds(0.3f);
            Game.Manager.miniBoardMultiMan.TryEnterNextRound(out var _1, out var _2, out var _3);
            _playingAni = true;
            yield return new WaitForSeconds(1.3f);
            RewardMoveAnim();
            yield return new WaitForSeconds(1.25f);
            screenSpine.AnimationState.SetAnimation(0, "close", false);
            Game.Manager.audioMan.TriggerSound("MiniboardMultiCurtainClose");
            yield return new WaitForSeconds(1f);
            LeaveOldBoard();
            EnterNewBoard();
            Game.Manager.miniBoardMultiMan.SendRewardToCurBoard(_1, _2, _3);
            CheckCompInit();
            _reward.transform.GetChild(0).localPosition = Vector3.zero;
            _reward.transform.GetChild(0).localScale = Vector3.one;
            BoardViewWrapper.GetCurrentWorld().onRewardListChange += _reward.RefreshReward;
            if (Game.Manager.miniBoardMultiMan.CheckHasNextBoard())
                _keyIcon.SetImage(Game.Manager.miniBoardMultiMan.GetCurInfoConfig().KeyIcon.ConvertToAssetConfig());
            yield return new WaitForSeconds(1f);
            if (Game.Manager.activity.mapR.ContainsKey(_activity))
            {
                screenSpine.AnimationState.SetAnimation(0, "open", false);
                Game.Manager.audioMan.TriggerSound("MiniboardMultiCurtainOpen");
                _reward.ShowReward();
                UIManager.Instance.Block(false);
                _keyNode.localScale = Vector3.one;
            }

            RefreshScale();
            _playingAni = false;
        }

        private void LeaveOldBoard()
        {
            BoardViewWrapper.GetCurrentWorld().onRewardListChange -= _reward.RefreshReward;
            _view.OnBoardLeave();
            BoardViewWrapper.PopWorld();
        }

        private void EnterNewBoard()
        {
            var world = Game.Manager.miniBoardMultiMan.World;
            var tracer = Game.Manager.miniBoardMultiMan.WorldTracer;
            if (world == null || tracer == null) return;
            BoardViewWrapper.PushWorld(world);
            _view.OnBoardEnter(world, tracer);
            _gate = null;
            world.WalkAllItem(item =>
            {
                item.TryGetItemComponent<ItemFeatureComponent>(out var component);
                if (component != null) _gate = component.item;
            });
        }

        private void RewardMoveAnim()
        {
            var seq = DOTween.Sequence();
            seq.Append(_reward.transform.GetChild(0).DOMove(BoardUtility.GetWorldPosByCoord(_gate.coord), 0.75f));
            seq.Join(_reward.transform.GetChild(0).DOScale(Vector3.zero, 0.75f).SetEase(Ease.InSine));
            seq.onComplete += () =>
            {
                var holder = _view.boardHolder.FindItemView(_gate.id).GetComponent<MBItemContent>().Holder;
                var resHolder = holder.ResHolder as MBResHolderMiniBoard;
                if (resHolder != null)
                {
                    resHolder.SetPunchState();
                }
                _reward.HideReward();
                _reward.transform.GetChild(0).localScale = Vector3.one;
                _reward.transform.GetChild(0).localPosition = Vector3.one;
            };

            foreach (var item in _inherit)
            {
                var itemSeq = DOTween.Sequence();
                itemSeq.Append(BoardViewManager.Instance.GetItemView(item.id).transform
                    .DOMove(BoardUtility.GetWorldPosByCoord(_gate.coord), 0.75f));
                itemSeq.Join(BoardViewManager.Instance.GetItemView(item.id).transform.DOScale(Vector3.zero, 0.75f)
                    .SetEase(Ease.InSine));
            }

            Game.Manager.audioMan.TriggerSound("MiniboardMultiInheritItem");
        }

        private void Update()
        {
            if (_isEnd) return;
            if (!IsShow()) return;
            if (!_playingAni) BoardViewManager.Instance.Update(Time.deltaTime);
            if (!Game.Manager.miniBoardMultiMan.CheckHasNextBoard() ||
                Game.Manager.miniBoardMultiMan.CheckCanEnterNextRound())
            {
                _keyNode.gameObject.SetActive(false);
                return;
            }

            _keyNode.gameObject.SetActive(true);
            _keyNode.transform.position = new Vector3(_progress.GetKeyPosX(), _keyNode.position.y, _keyNode.position.z);
        }

        protected override void OnPostClose()
        {
            Game.Manager.screenPopup.Block(false, false);
            MessageCenter.Get<CHECK_MINI_BOARD_MULTI_ENTRY_RED_POINT>().Dispatch(_activity);
        }

        protected override void OnPreClose()
        {
            UIManager.Instance.CloseWindow(UIConfig.UIPopFlyTips);
            UIManager.Instance.CloseWindow(UIConfig.UIEnergyBoostTips);
            HideHand();
            if (!_hasInit) return;
            if (BoardViewWrapper.GetCurrentWorld() == null) return;
            _view.OnBoardLeave();
            BoardViewWrapper.GetCurrentWorld().onRewardListChange -= _reward.RefreshReward;
            BoardViewWrapper.PopWorld();
        }

        protected override void OnPostOpen()
        {
            if (_isEnd)
                return;
            BoardViewManager.Instance.CalcOrigin();
            BoardViewManager.Instance.CalcScaleCoe();
            BoardViewManager.Instance.CalcSize();
            if (!_activity.UIOpenState)
            {
                _helpNode.SetActive(true);
                var text = _helpNode.transform.Find("Content/HelpTips").GetComponent<TextMeshProUGUI>();
                if (Game.Manager.miniBoardMultiMan.GetCurInfoConfig() != null)
                {
                    var cfg = Game.Manager.objectMan.GetBasicConfig(Game.Manager.miniBoardMultiMan.GetCurInfoConfig()
                        .LevelItem[0]);
                    var sprite = "<sprite name=\"" + cfg.Icon.ConvertToAssetConfig().Asset + "\">";
                    text.text = I18N.FormatText("#SysComDesc484", sprite);
                }
            }
        }

        private void ShowLockRewardTip()
        {
            Game.Manager.commonTipsMan.ShowPopTips(Toast.MiniBoardMultiGiftBoxTip, _center.position);
        }

        private void ShowLockDoorTip()
        {
            _progress.MoveToEnd();
            Game.Manager.commonTipsMan.ShowPopTips(Toast.MiniBoardMultiDoorTip, _center.position,
                I18N.Text("#SysComDesc796"));
        }

        private void ShowHand()
        {
            if (hand.activeSelf) return;
            if (_gate == null) return;
            var pos = BoardUtility.GetWorldPosByCoord(_gate.coord);
            hand.transform.position = pos;
            hand.SetActive(true);
        }

        private void HideHand()
        {
            hand.gameObject.SetActive(false);
            _interval = 0;
        }
    }
}
