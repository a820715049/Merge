/*
 * @Author: tang.yan
 * @Description: 弹珠游戏主界面 
 * @Date: 2024-12-04 19:12:38
 */

using System.Collections;
using System.Collections.Generic;
using Config;
using Cysharp.Text;
using DG.Tweening;
using UnityEngine;
using EL;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class UIPachinkoMain : UIBase
    {
        [SerializeField][Tooltip("小球发射前的延迟时间(秒)")] private float startDelayTime;
        private RectTransform _worldRoot;
        private PachinkoPhysicsWorld _physicsWorld;
        private List<(Button, UIImageState)> _startBtnList = new List<(Button, UIImageState)>();
        private Button _multipleLowBtn;     //低倍率按钮
        private TMP_Text _multipleLowText;  //低倍率按钮文本
        private Button _multipleHighBtn;    //高倍率按钮
        private TMP_Text _multipleHighText; //高倍率按钮
        private GameObject _multipleEffect; //倍率按钮特效
        private TMP_Text _ballNumText;      //小球数量文本
        //小球得分口奖励分数
        private List<PachinkoEndReward> _endRewardList = new List<PachinkoEndReward>();
        //底部进度条奖励相关
        private RectTransform _progressContent;
        private RectTransform _progressRewardRoot;
        private RectTransform _progressReward;
        private UICommonProgressBar _progressBar;
        private List<PachinkoProgressReward> _progressRewardList = new List<PachinkoProgressReward>();
        private List<(float, float)> _targetPosList = new List<(float, float)>(); //记录位置 Item1 奖励在滑动条上的位置 Item2 Content定位时要定位到的位置
        //进度条上方文本
        private TMP_Text _progressText;
        //进度条左侧奖励
        private Animator _rewardLeftAnim;
        private TMP_Text _rewardLeftNum;
        //进度条右侧奖励
        private Button _rewardRightBtn;
        private RectTransform _rewardRightRect;
        //右下角飞奖励时会短暂显示的主棋盘入口
        private Image _flyTarget;
        private Sequence _tweenSeqFly;

        private bool _isPlaying = false;    //是否正在游戏中 会禁止关界面禁止点击
        private bool _isPlayingBigReward = false;
        private bool _isPlayingProgress = false;
        private static int _bigRewardIndex = 3; //默认中间第3个始终是大奖 已和策划确认好
        private int _progressMaxEnergy = -1;    //记录进度条最大能量值
        private int _curStartPosIndex = -1;     //目前选择的发射口
        private float _startProgressWidth = 0;  //记录底部进度条初始宽度

        private Coroutine _loadResCo;   //加载物理关卡prefab
        private Coroutine _playBallCo;  //发射小球协程
        private Coroutine _progressCo;  //进度条动画协程
        
        protected override void OnCreate()
        {
            _worldRoot = transform.FindEx<RectTransform>("Content/Root/PhysicsRoot");
            transform.AddButton("Content/BtnClose/Btn", _OnClickBtnClose);
            transform.AddButton("Content/BtnHelp/Btn", _OnClickBtnHelp);
            //顶部发射按钮5个
            var btnPath = "Content/Root/Top/StartPos/Pos";
            for (var i = 0; i < 5; i++)
            {
                var path = ZString.Concat(btnPath, i, "/Btn");
                var index = i;
                var btn = transform.FindEx<Button>(path);
                btn.onClick.AddListener(() => _OnClickBtnStart(index));
                var state = transform.FindEx<UIImageState>(path);
                _startBtnList.Add((btn, state));
            }
            //顶部切换倍率按钮
            var multipleLowPath = "Content/Root/Top/MultipleBtn/BtnYellow";
            _multipleLowBtn = transform.FindEx<Button>(ZString.Concat(multipleLowPath));
            _multipleLowBtn.onClick.AddListener(_OnClickBtnMultiple);
            _multipleLowText = transform.FindEx<TMP_Text>(ZString.Concat(multipleLowPath, "/Text"));
            var multipleHighPath = "Content/Root/Top/MultipleBtn/BtnViolet";
            _multipleHighBtn = transform.FindEx<Button>(ZString.Concat(multipleHighPath));
            _multipleHighBtn.onClick.AddListener(_OnClickBtnMultiple);
            _multipleHighText = transform.FindEx<TMP_Text>(ZString.Concat(multipleHighPath, "/Text"));
            transform.FindEx("Content/Root/Top/MultipleBtn/fx_pachinko_click", out _multipleEffect);
            //小球数量文本
            _ballNumText = transform.FindEx<TMP_Text>("Content/Root/Top/BallNum/Text");
            //底部得分口奖励UI 7个 默认中间的是大奖
            var endRewardPath = "Content/Root/EndReward/PachinkoEndReward";
            for (var i = 0; i < 7; i++)
            {
                var path = ZString.Concat(endRewardPath, i);
                _endRewardList.Add(transform.FindEx<PachinkoEndReward>(path));
            }
            //底部进度条相关
            var progressPath = "Content/Root/Bottom/Reward/Viewport/Content";
            _progressContent = transform.FindEx<RectTransform>(progressPath);
            _progressRewardRoot = transform.FindEx<RectTransform>(ZString.Concat(progressPath, "/RewardList"));
            _startProgressWidth = _progressRewardRoot.rect.width;
            _progressReward = transform.FindEx<RectTransform>(ZString.Concat(progressPath, "/RewardList/PachinkoProgressReward"));
            _progressBar = transform.FindEx<UICommonProgressBar>(ZString.Concat(progressPath, "/ProgressBar"));
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.PACHINKO_PROGRESS_REWARD, _progressReward.gameObject);
            //进度条上方文本
            _progressText = transform.FindEx<TMP_Text>("Content/Root/Bottom/Text2");
            //进度条左侧奖励
            var rewardLeftPath = "Content/Root/Bottom/RewardLeft";
            _rewardLeftAnim = transform.FindEx<Animator>(rewardLeftPath);
            _rewardLeftNum = transform.FindEx<TMP_Text>(ZString.Concat(rewardLeftPath, "/Count"));
            //进度条右侧奖励
            _rewardRightBtn = transform.FindEx<Button>("Content/Root/Bottom/RewardRight/Icon");
            _rewardRightRect = transform.FindEx<RectTransform>("Content/Root/Bottom/RewardRight");
            _rewardRightBtn.onClick.AddListener(_OnClickRightReward);
            //右下角飞奖励时会短暂显示的主棋盘入口
            _flyTarget = transform.FindEx<Image>("Content/FlyTarget");
        }

        protected override void OnParse(params object[] items)
        {
            var loadingWaitTask = items.Length > 0 ? items[0] as SimpleAsyncTask : null;
            var detailConf = Game.Manager.pachinkoMan.GetActivity()?.ConfD;
            if (detailConf != null && _physicsWorld == null)
            {
                _ClearLoadResCo();
                var res = detailConf.MachineInfo.ConvertToAssetConfig();
                _loadResCo = StartCoroutine(_CoLoadPhysicsPrefab(res, loadingWaitTask));
            }
            else
            {
                loadingWaitTask?.ResolveTaskSuccess();
            }
        }

        private IEnumerator _CoLoadPhysicsPrefab(AssetConfig res, SimpleAsyncTask waitTask)
        {
            var task = EL.Resource.ResManager.LoadAsset<GameObject>(res.Group, res.Asset);
            yield return task;
            if (task != null && task.isSuccess && task.asset != null)
            {
                var obj = Instantiate(task.asset as GameObject);
                if (obj != null)
                {
                    _physicsWorld = obj.GetComponent<PachinkoPhysicsWorld>();
                    var rectTrans = obj.GetComponent<RectTransform>();
                    rectTrans.SetParent(_worldRoot);
                    rectTrans.anchoredPosition = Vector2.zero;
                    rectTrans.localScale = Vector3.one;
                    _physicsWorld.OnInit();
                    _physicsWorld.RegisterPlayEndCb(_OnBallPlayEnd);
                    _physicsWorld.RegisterBumperColliderCb(_OnBumperCollider);
                    _physicsWorld.OnRefresh();
                }
            }
            waitTask.ResolveTaskSuccess();
        }
        
        private void _ClearLoadResCo()
        {
            if (_loadResCo != null)
            {
                StopCoroutine(_loadResCo);
                _loadResCo = null;
            }
        }

        protected override void OnPreOpen()
        {
            _CheckAndInitProgressReward();
            _ChangeTopBarState(true);
            if (_physicsWorld != null)
                _physicsWorld.OnRefresh();
            _RefreshBallNum();
            _RefreshBtnMultipleInfo();
            _RefreshEndRewardList();
            _RefreshProgressReward();
            _RefreshProgressText();
            _JumpProgressReward();
            _RefreshRewardLeftNum(true);
            //打开场景bgm  主棋盘bgm关闭
            Game.Manager.audioMan.PlayBgm("PachinkoBgm");
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_DEBUG_PACHINKO_INFO>().AddListener(_DebugPachinkoInfo);
            MessageCenter.Get<MSG.UI_SIMPLE_ANIM_FINISH>().AddListener(_OnAnimPlayEnd);
            MessageCenter.Get<MSG.FLY_ICON_START>().AddListener(_TryShowFlyTarget);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(_OnRewardFeedBack);
            MessageCenter.Get<MSG.UI_PACHINKO_LOADING_END>().AddListener(_TryOpenRightRewardTips);
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_DEBUG_PACHINKO_INFO>().RemoveListener(_DebugPachinkoInfo);
            MessageCenter.Get<MSG.UI_SIMPLE_ANIM_FINISH>().RemoveListener(_OnAnimPlayEnd);
            MessageCenter.Get<MSG.FLY_ICON_START>().RemoveListener(_TryShowFlyTarget);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(_OnRewardFeedBack);
            MessageCenter.Get<MSG.UI_PACHINKO_LOADING_END>().RemoveListener(_TryOpenRightRewardTips);
        }

        protected override void OnPostClose()
        {
            _ChangeTopBarState(false);
            if (_physicsWorld != null)
                _physicsWorld.OnClear();
            _multipleEffect.gameObject.SetActive(false);
            _ClearProgressReward();
            _ClearFlyTargetTween();
            _ClearProgressCo();
            _ClearPlayBallCo();
            _ClearLoadResCo();
            //若界面关闭时还在播放过程中 则放开block
            if (_isPlaying || _isPlayingBigReward || _isPlayingProgress)
            {
                UIManager.Instance.Block(false);
                _ChangeAllBtnEnableState(true);
            }
            _isPlaying = false;
            _isPlayingBigReward = false;
            _isPlayingProgress = false;
            _curStartPosIndex = -1;
            _showWaitTime = 1;
            UIManager.Instance.CloseWindow(UIConfig.UIPachinkoMultipleTips);
            UIManager.Instance.CloseWindow(UIConfig.UIPachinkoPopFly);
            //关闭场景bgm  主棋盘bgm打开
            Game.Manager.audioMan.PlayDefaultBgm();
        }

        private void _DebugPachinkoInfo(int paramType, string param)
        {
            if (_physicsWorld != null)
                _physicsWorld.DebugPachinkoInfo(paramType, param);
        }
        
        private void _ChangeTopBarState(bool isTop)
        {
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(!isTop);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(!isTop);
            MessageCenter.Get<MSG.UI_STATUS_ADD_BTN_CHANGE>().Dispatch(!isTop);
            if (isTop)
                MessageCenter.Get<MSG.UI_TOP_BAR_PUSH_STATE>().Dispatch(UIStatus.LayerState.AboveStatus);
            else
                MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().Dispatch();
        }
        
        private void _OnAnimPlayEnd(AnimatorStateInfo stateInfo)
        {
            if (_physicsWorld == null) return;
            _physicsWorld.OnAnimPlayEnd(stateInfo);
            if (stateInfo.IsName("RewardLeft_High"))
            {
                //累计分数清0
                _RefreshRewardLeftNum(true);
                //播放底部进度条奖励所有动效
                _PlayAllProgressAnim();
            }
            //如果获得了大奖 则界面放开block延迟到大奖动画播完为止
            if (stateInfo.IsName("UIBasic_High"))
            {
                _isPlayingBigReward = false;
                _TrySwitchBlock(false);
            }
        }

        private void _OnClickBtnClose()
        {
            var pachinkoMan = Game.Manager.pachinkoMan;
            var isRoundFinish = pachinkoMan.CheckRoundFinish();
            //如果目前是回合完成状态 不能走正常关闭 得等表现完成后走
            if (isRoundFinish) return;
            pachinkoMan.ExitMainScene();
        }

        private void _OnClickBtnHelp()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIPachinkoHelp);
        }

        private void _TryOpenRightRewardTips()
        {
            var _activity = Game.Manager.pachinkoMan.GetActivity();
            if (_activity == null) return;
            var canShow = _activity.GetCanPopRewardTips();
            //打开后设置为已pop
            if (canShow)
            {
                _activity.SetIsPopRewardTips();
                _OpenRightRewardTips();
            }
        }

        //点击最右侧大奖按钮
        private void _OnClickRightReward()
        {
            _OpenRightRewardTips();
        }
        
        private void _OpenRightRewardTips()
        {
            var pachinkoMan = Game.Manager.pachinkoMan;
            var rewardList = pachinkoMan.GetMilestone();
            if (rewardList.TryGetByIndex(rewardList.Count - 1, out var lastReward))
            {
                UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, 
                    _rewardRightBtn.transform.position, 
                    _rewardRightRect.rect.size.y * 0.5f, 
                    lastReward.MilestoneReward);
            }
        }

        private void _OnClickBtnStart(int index)
        {
            Game.Manager.audioMan.TriggerSound("UIClick");
            _ClearPlayBallCo();
            _playBallCo = StartCoroutine(_CoStartPlayBall(index));
        }

        private IEnumerator _CoStartPlayBall(int index)
        {
            if (_physicsWorld == null)
                yield break;
            var canStart = Game.Manager.pachinkoMan.TryStartGame(index, out var posOffset, out var startVelocity, out var angleOffset);
            if (!canStart) 
                yield break;
            _isPlaying = true;
            _curStartPosIndex = index;
            _TrySwitchBlock(true);
            _RefreshBallNum();
            //小球发射前播放发射口特效  延迟startDelayTime秒后小球发射
            _physicsWorld.PlayStartPosEffect(index);
            yield return new WaitForSeconds(startDelayTime);
            _physicsWorld.SetBallStartInfo(index, posOffset, startVelocity, angleOffset);
        }
        
        private void _ClearPlayBallCo()
        {
            if (_playBallCo != null)
            {
                StopCoroutine(_playBallCo);
                _playBallCo = null;
            }
        }

        private void _TrySwitchBlock(bool isBlock)
        {
            if (isBlock)
            {
                UIManager.Instance.Block(true);
                _ChangeAllBtnEnableState(false);
            }
            else
            {
                //尝试放开block时 检查目前是否所有阻挡字段都不为true
                if (_isPlaying || _isPlayingBigReward || _isPlayingProgress)
                    return;
                UIManager.Instance.Block(false);
                _ChangeAllBtnEnableState(true);
                _curStartPosIndex = -1;
            }
        }
        
        private void _OnBallPlayEnd(int endIndex)
        {
            _isPlaying = false;
            var pachinkoMan = Game.Manager.pachinkoMan;
            pachinkoMan.WhenDrop(endIndex);
            var isBigReward = endIndex == _bigRewardIndex;
            //播放大奖动画时 block延迟到大奖动画播完为止
            if (isBigReward)
            {
                _isPlayingBigReward = true;
                //播获得大奖音效
                Game.Manager.audioMan.TriggerSound("PachinkoDropMiddle");
                _physicsWorld.PlayBigRewardAnim();
            }
            else
            {
                //播落到得分口音效
                Game.Manager.audioMan.TriggerSound("PachinkoDrop");
                _physicsWorld.PlayEndPosEffect(endIndex);
            }
            //播放落入口积分奖励动画
            _endRewardList[endIndex].PlayRewardAnim();
            //左侧奖励
            _RefreshRewardLeftNum();
            _PlayRewardLeftAnim();
            _isPlayingProgress = true;
            //要播动画前进度条先强制跳转定位到最靠后的已领取的奖励位置
            var jumpIndex = -1;
            for (var i = 0; i < _progressRewardList.Count; i++)
            {
                var reward = _progressRewardList[i];
                if (reward.GetIsReceive() && jumpIndex < i)
                {
                    jumpIndex = i;
                }
            }
            _JumpProgressReward(true, jumpIndex);
        }

        //当小球与保险杠碰撞时回调
        private void _OnBumperCollider(int bumperIndex)
        {
            _RefreshRewardLeftNum();
        }

        private void _OnClickBtnMultiple()
        {
            Game.Manager.audioMan.TriggerSound("UIClick");
            Game.Manager.pachinkoMan.TryChangeCoinRate();
            _multipleEffect.gameObject.SetActive(false);
            _multipleEffect.gameObject.SetActive(true);
            _RefreshBtnMultipleInfo(true);
            _RefreshEndRewardList();
        }

        private void _TryShowFlyTarget(FlyableItemSlice item)
        {
            if (item == null || (_tweenSeqFly != null && _tweenSeqFly.IsPlaying()))
                return;
            if (_CheckShowFlyTarget(item.Reward.rewardId))
                _OnFlyTargetShow();
        }

        private float _showWaitTime = 1f;
        private void _OnFlyTargetShow()
        {
            _ClearFlyTargetTween();
            _tweenSeqFly = DOTween.Sequence();
            _tweenSeqFly.Append(_flyTarget.DOFade(1, 0.5f));
            _tweenSeqFly.AppendInterval(_showWaitTime);
            _tweenSeqFly.Append(_flyTarget.DOFade(0, 0.5f));
            _tweenSeqFly.OnKill(() =>
            {
                var color = Color.white;
                color.a = 0;
                _flyTarget.color = color;
            });
            _tweenSeqFly.Play();
            _showWaitTime = 1;
        }
        
        private bool _CheckShowFlyTarget(int id)
        {
            if (id == Constant.kMergeEnergyObjId)
                return false;
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.Coin))
                return false;
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.ActivityToken))
                return false;
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.RandomBox))
                return false;
            return true;
        }
        
        private void _ClearFlyTargetTween()
        {
            _tweenSeqFly?.Kill();
            _tweenSeqFly = null;
        }

        private void _OnRewardFeedBack(FlyType flyType)
        {
            if (flyType != FlyType.Pachinko) return;
            _RefreshBallNum();
        }

        private void _RefreshBallNum()
        {
            _ballNumText.text = Game.Manager.pachinkoMan.GetCoinCount().ToString();
        }

        private void _RefreshEndRewardList()
        {
            var dropRange = Game.Manager.pachinkoMan.GetDropRange();
            var length = dropRange.Count;
            for (int i = 0; i < _endRewardList.Count; i++)
            {
                if (i < length)
                {
                    _endRewardList[i].gameObject.SetActive(true);
                    _endRewardList[i].Refresh(dropRange[i], i == _bigRewardIndex);
                }
                else
                {
                    _endRewardList[i].gameObject.SetActive(false);
                }
            }
        }

        private void _RefreshBtnMultipleInfo(bool isShowTips = false)
        {
            var pachinkoMan = Game.Manager.pachinkoMan;
            var isHigh = pachinkoMan.IsMaxMultiple();
            var multiple = pachinkoMan.GetMultiple();
            _multipleLowBtn.gameObject.SetActive(!isHigh);
            _multipleHighBtn.gameObject.SetActive(isHigh);
            var str = ZString.Concat("x", multiple);
            if (!isHigh)
                _multipleLowText.text = str;
            else
                _multipleHighText.text = str;
            if (isShowTips)
                UIManager.Instance.OpenWindow(UIConfig.UIPachinkoMultipleTips, _multipleLowBtn.transform.position, 70f, isHigh, multiple);
        }

        //修改所有按钮的可点击状态
        private void _ChangeAllBtnEnableState(bool isEnable)
        {
            for (var i = 0; i < _startBtnList.Count; i++)
            {
                var info = _startBtnList[i];
                info.Item1.interactable = isEnable;
                var isCur = i == _curStartPosIndex;
                info.Item2.Setup(isEnable ? 0 : (isCur ? 2 : 1));
            }
            _multipleLowBtn.interactable = isEnable;
            _multipleHighBtn.interactable = isEnable;
        }
        
        //初始时设置好底部进度条奖励
        private void _CheckAndInitProgressReward()
        {
            var pachinkoMan = Game.Manager.pachinkoMan;
            var maxEnergy = pachinkoMan.GetMaxEnergy();
            //如果记录的最大能量值和当前的一样 则不刷新后面的布局 
            if (_progressMaxEnergy == maxEnergy)
                return;
            _progressMaxEnergy = maxEnergy;
            _targetPosList.Clear();
            var rewardList = pachinkoMan.GetMilestone();
            //默认以第4个奖励对应的能量值为基准，以当前进度条宽度的4/5为基础宽度，计算整个进度条的长度以及每个奖励的分布位置
            if (!rewardList.TryGetByIndex(3, out var conf)) return;
            var energy = conf.MilestoneEnergy;
            var contentWidth = _progressMaxEnergy * (_startProgressWidth * 1) / energy;
            _progressContent.sizeDelta = new Vector2(contentWidth, _progressContent.sizeDelta.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_progressContent);
            _progressRewardRoot.sizeDelta = new Vector2(contentWidth, _progressRewardRoot.sizeDelta.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_progressRewardRoot);
            var rewardWidth = _progressReward.rect.width;
            var halfWidth = rewardWidth * 0.5f;
            var offsetWidth = rewardWidth * 0.1f;   //再往左加一点宽度 挡住item右上角的tips按钮
            foreach (var reward in rewardList)
            {
                if (reward.MilestoneEnergy >= _progressMaxEnergy) break; //最后一个奖励不加载
                var basePos = contentWidth * reward.MilestoneEnergy / _progressMaxEnergy;
                var offsetX = basePos - halfWidth;    //锚点在最左边 所以要减去一半宽度
                _targetPosList.Add((offsetX, -(basePos + offsetWidth)));  //记录content定位时的位置 避免重复计算 取负数是因为Content向左滑
            }
        }

        private void _RefreshProgressReward()
        {
            _progressRewardList.Clear();
            var pachinkoMan = Game.Manager.pachinkoMan;
            //刷新进度条初始参数
            _progressBar.Setup(0, _progressMaxEnergy, pachinkoMan.GetEnergy());
            _progressBar.RegisterNotify(_OnProgressAnimPlayEnd);
            //刷新进度条奖励
            var rewardList = pachinkoMan.GetMilestone();
            var curFinishIndex = pachinkoMan.GetCurMilestonePhase();    //当前已完成的最后一档奖励 默认0为啥也没完成 1为完成了第一档
            var index = 0;
            foreach (var reward in rewardList)
            {
                if (reward.MilestoneEnergy >= _progressMaxEnergy) break; //最后一个奖励不加载
                var obj = GameObjectPoolManager.Instance.CreateObject(PoolItemType.PACHINKO_PROGRESS_REWARD, _progressRewardRoot);
                var offsetX = _targetPosList.TryGetByIndex(index, out var pos) ? pos.Item1 : 0;
                obj.transform.localPosition = new Vector3(offsetX, 0, 0);
                var item = obj.GetComponent<PachinkoProgressReward>();
                item.Setup();
                item.Refresh(reward, index <= curFinishIndex - 1);
                _progressRewardList.Add(item);
                index++;
            }
        }

        private void _RefreshProgressText()
        {
            var pachinkoMan = Game.Manager.pachinkoMan;
            _progressText.text = ZString.Concat(pachinkoMan.GetCurMilestonePhase(), "/", pachinkoMan.GetCurMilestoneCount());
        }
        
        //isForce为false时跳转到目前已完成奖励对应的index 否则为指定跳转
        private void _JumpProgressReward(bool isForce = false, int forceIndex = -1)
        {
            //获取当前已完成的奖励Index 从0开始  -1是为了和记录的位置Index对齐
            var jumpIndex = !isForce ? Game.Manager.pachinkoMan.GetCurMilestonePhase() - 1 : forceIndex;
            if (_targetPosList.TryGetByIndex(jumpIndex, out var pos))
            {
                _progressContent.anchoredPosition = new Vector2(pos.Item2, 0);
            }
            else
            {
                _progressContent.anchoredPosition = Vector2.zero;
            }
        }

        private void _PlayAllProgressAnim()
        {
            var pachinkoMan = Game.Manager.pachinkoMan;
            var isRoundFinish = pachinkoMan.CheckRoundFinish();
            var curEnergy = isRoundFinish ? _progressMaxEnergy : pachinkoMan.GetEnergy();
            //播放进度条进度动画
            _progressBar.SetProgress(curEnergy);
        }

        //当进度条动画播完时
        private void _OnProgressAnimPlayEnd()
        {
            var pachinkoMan = Game.Manager.pachinkoMan;
            var isRoundFinish = pachinkoMan.CheckRoundFinish();
            var curEnergy = isRoundFinish ? _progressMaxEnergy : pachinkoMan.GetEnergy();
            _progressBar.Setup(0, _progressMaxEnergy, curEnergy);
            //多倍按钮
            _RefreshBtnMultipleInfo();
            //落入口上的奖励
            _RefreshEndRewardList();
            //目前奖励领取进度
            _RefreshProgressText();
            //获取本次发射球总共获得的奖励list
            var waitRewardList = pachinkoMan.GetMilestoneRewardCommitData();
            //发奖前计算flyTarget显示后要等待的时间
            var beginIndex = -1;
            var endIndex = -1;
            for (int i = 0; i < waitRewardList.Count; i++)
            {
                var r = waitRewardList[i];
                if (_CheckShowFlyTarget(r.rewardId))
                {
                    if (beginIndex == -1) beginIndex = i;
                    if (endIndex == -1 || endIndex < i) endIndex = i;
                }
            }
            var diff = (endIndex == beginIndex) ? 1 : endIndex - beginIndex;
            _showWaitTime = diff * 2 * _progressBaseTime;
            //播放进度条奖励动画
            _ClearProgressCo();
            _progressCo = StartCoroutine(_CoPlayProgressReward(waitRewardList, isRoundFinish));
        }

        private static float _progressBaseTime = 0.5f;
        private IEnumerator _CoPlayProgressReward(List<RewardCommitData> waitRewardList, bool isRoundFinish)
        {
            if (waitRewardList.Count > 0)
            {
                var count = 0;
                var index = 0;
                foreach (var reward in _progressRewardList)
                {
                    if (reward.GetIsReceive())
                    {
                        index++;
                        continue;
                    }
                    if (count >= waitRewardList.Count)
                        break;
                    if (_targetPosList.TryGetByIndex(index, out var pos))
                    {
                        reward.PlayReceiveAnim();
                        if (waitRewardList.TryGetByIndex(count, out var commitData))
                            UIFlyUtility.FlyReward(commitData, reward.transform.position);
                        yield return new WaitForSeconds(_progressBaseTime);
                        var targetPos = new Vector2(pos.Item2, 0);
                        _progressContent.DOAnchorPos(targetPos, _progressBaseTime).SetEase(Ease.InCubic)
                            .OnComplete(() =>
                            {
                                _progressContent.anchoredPosition = targetPos;
                            });
                        yield return new WaitForSeconds(_progressBaseTime);
                    }
                    count++;
                    index++;
                }
            }
            //如果本回合结束了 
            if (isRoundFinish)
            {
                Game.Manager.pachinkoMan.FinishRound();
            }
            //所有进度条奖励动画播放结束
            _isPlayingProgress = false;
            _TrySwitchBlock(false);
            yield return null;
        }
        
        private void _ClearProgressCo()
        {
            if (_progressCo != null)
            {
                StopCoroutine(_progressCo);
                _progressCo = null;
            }
        }

        private void _ClearProgressReward()
        {
            foreach (var item in _progressRewardList)
            {
                item.Clear();
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.PACHINKO_PROGRESS_REWARD, item.gameObject);
            }
            _progressRewardList.Clear();
            _progressBar.UnregisterNotify(_OnProgressAnimPlayEnd);
        }

        private void _RefreshRewardLeftNum(bool isClear = false)
        {
            var curNum = Game.Manager.pachinkoMan.GetTotalEnergyGet();
            _rewardLeftNum.text = curNum <= 0 || isClear ? "" : ZString.Concat("+", curNum);
        }

        private void _PlayRewardLeftAnim()
        {
            _rewardLeftAnim.ResetTrigger("Punch");
            _rewardLeftAnim.SetTrigger("Punch");
        }
    }
}
