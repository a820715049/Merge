/**
 * @Author: ShentuAnge
 * @Date: 2025/07/01 19:04:11
 * Description: 积分活动棋子样式界面
 */

using UnityEngine;
using EL;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;
using Coffee.UIExtensions;

namespace FAT
{
    public class UIScore_piece : UIBase, INavBack
    {
        #region UI组件

        [Header("基础UI组件")]
        [SerializeField] private Button playBtn;           // 开始游戏按钮
        [SerializeField] private Transform helpRoot;       // 帮助界面根节点
        [SerializeField] private Button helpBtn;           // 帮助按钮
        [SerializeField] private Button block;             // 遮罩按钮
        [SerializeField] private TMP_Text cd;              // 倒计时文本

        [Header("里程碑相关")]

        [SerializeField] private UICommonItem item;        // 大奖显示组件
        [SerializeField] private MBRewardProgress rewardProgress;  // 进度条组件
        [SerializeField] private MBRewardIcon rewardIcon;   // 奖励图标
        [SerializeField] private Animation progressItemAnim; // 进度条Item动画
        [SerializeField] private List<UIScoreMilestoneItem_piece> cellList = new();         // Cell列表
        [SerializeField] private GameObject car;//做路径动画的小车
        [SerializeField] private Animator carAnim;
        [SerializeField] private TMP_Text milestoneText;
        [SerializeField] private UIParticle milestoneParticle;
        [SerializeField] private Animation centerRewardAnim;

        [Header("进度条播放动画")]
        [Tooltip("进度条充满动画时长")]
        [SerializeField] private float progressBarFillDuration = 0.8f;
        [Header("起点位置")]
        [SerializeField]
        private Transform startPoint;

        [Header("路径动画配置")]
        [Header("小车移动动画时长")]
        [SerializeField]
        private float carMoveDuration = 1.0f;
        [Header("小车移动缓动类型")]
        [SerializeField]
        private AnimationCurve carMoveEase = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("路径移动类型")]
        [SerializeField]
        private PathType pathType = PathType.CatmullRom;

        [Header("路径模式")]
        [SerializeField]
        private PathMode pathMode = PathMode.TopDown2D;

        [Header("提前领奖时间")]
        [SerializeField]
        private float earlyGetRewardTime = 0.5f;
        [Header("奖励飞行动画延迟")]
        [SerializeField]
        private float rewardFlyDelay = 0.5f;
        [Header("旗子动画延迟")]
        [SerializeField]
        private float flagAnimDelay = 0.5f;
        [Header("Debug配置")]
        [Tooltip("Debug移动按钮")]
        [SerializeField]
        private bool debugMoveNext = false;

        #endregion

        #region 私有字段

        private ActivityScore activity;                    // 积分活动实例
        private bool isPopup;                              // 是否为弹脸模式
        private Action WhenCD;                             // 倒计时回调

        // 动画状态管理
        private Coroutine _progressAnimationCoroutine;     // 进度条动画协程

        // Debug相关
        private int _debugMoveIndex = 0;                   // Debug移动索引

        #endregion

        #region 生命周期和事件处理

        protected override void OnCreate()
        {
            transform.AddButton("Content/close", Close);
            helpBtn.onClick.AddListener(OnClickHelp);
            playBtn.onClick.AddListener(OnClickPlay);
            block.onClick.AddListener(OnClickBlock);
        }

        protected override void OnParse(params object[] items)
        {
            activity = (ActivityScore)items[0];
            if (items.Length > 1 && items[1] is bool isPopup)
            {
                this.isPopup = isPopup;
            }
            else
            {
                this.isPopup = false;
            }
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            milestoneParticle.gameObject.SetActive(false);
            activity.FillMilestoneData();
            RefreshCD();

            int curMilestoneIndex = 0;
            if (activity.ShouldPopup())
            {
                playBtn.gameObject.SetActive(false);
                //领奖弹出，这时的状态和ActivityScore中的数据状态是不一致的，需要准备用表现参数准备预领奖状态
                var (milestoneIndex, showScore, milestoneScore)
                = activity.CalculateScoreDisplayData(activity.LastShowScore_UI);
                rewardProgress.Refresh(showScore, milestoneScore);
                curMilestoneIndex = milestoneIndex;
                //需要领奖的时候，锁定UI
                LockEvent();
            }
            else
            {
                //其余状态下，直接把UI状态刷新到和ActivityScore中的数据状态一致
                var (milestoneIndex, showScore, milestoneScore)
                = activity.CalculateScoreDisplayData(activity.TotalScore);
                rewardProgress.Refresh(showScore, milestoneScore);
                curMilestoneIndex = milestoneIndex;
                if (isPopup)
                {
                    OnClickHelp();
                    StartCoroutine(CoDelayHideHelp());
                    playBtn.gameObject.SetActive(false);
                }
                else
                {
                    block.gameObject.SetActive(false);
                    helpRoot.gameObject.SetActive(false);
                }
            }

            RefreshList(curMilestoneIndex);
            RefreshBigReward();

            // 刷新rewardIcon显示
            RefreshRewardIcon(curMilestoneIndex);

            // 初始化小车位置到当前里程碑点
            InitializeCarPosition(curMilestoneIndex);

            // 初始化Debug索引
            _debugMoveIndex = 0;

            // 初始化里程碑进度文本
            UpdateMilestoneText(curMilestoneIndex, false);
#if UNITY_EDITOR
            // Debug按钮监听
            StartCoroutine(CoDebugMoveListener());
#endif
        }

        protected override void OnPostOpen()
        {
            base.OnPostOpen();

            OnCarIdle();
            // 播放进度条动画
            if (activity.ShouldPopup())
            {
                // 停止之前的动画
                if (_progressAnimationCoroutine != null)
                {
                    StopCoroutine(_progressAnimationCoroutine);
                }

                // 开始播放跨里程碑动画
                _progressAnimationCoroutine = StartCoroutine(CoPlayProgressAnimation(
                    activity.LastShowScore_UI,
                    activity.TotalShowScore_UI
                ));
            }
        }

        protected override void OnPostClose()
        {
            // 记录当前进度分数到ActivityScore中，用于下次打开时的起始点
            activity.OnGetRewardUIPostClose();

            // 停止动画协程
            if (_progressAnimationCoroutine != null)
            {
                StopCoroutine(_progressAnimationCoroutine);
                _progressAnimationCoroutine = null;
            }
            if (activity.HasComplete())
            {
                UIManager.Instance.OpenWindow(UIConfig.UIScoreFinish_Piece);
            }

            // 刷新Meta界面的多轮迷你棋盘入口红点（海盗棋盘为MiniBoardMulti换皮）
            var miniBoardMultiAct = Game.Manager.miniBoardMultiMan?.CurActivity;
            if (miniBoardMultiAct != null)
            {
                MessageCenter.Get<MSG.CHECK_MINI_BOARD_MULTI_ENTRY_RED_POINT>().Dispatch(miniBoardMultiAct);
            }
        }

        protected override void OnAddListener()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
        }

        public void OnNavBack()
        {
            if (_progressAnimationCoroutine != null)
            {
                return;
            }

            // 动画未播放，关闭界面
            Close();
        }
        #endregion

        #region UI交互和显示控制

        private void OnClickPlay()
        {
            Close();
            GameProcedure.SceneToMerge();
        }

        private void OnClickBlock()
        {
            helpRoot.gameObject.SetActive(false);
            block.gameObject.SetActive(false);
            PlayBtnShow();
        }

        private void OnClickHelp()
        {
            helpRoot.gameObject.SetActive(true);
            block.gameObject.SetActive(true);
        }

        private IEnumerator CoDelayHideHelp()
        {
            yield return new WaitForSeconds(3f);
            OnClickBlock();
        }

        private void PlayBtnShow()
        {
            if (playBtn.gameObject.activeSelf)
                return;
            if (activity.HasComplete())
                return;
            var btnTrans = playBtn.transform;
            btnTrans.localScale = Vector3.zero;
            playBtn.gameObject.SetActive(true);
            btnTrans.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        private void RefreshCD()
        {
            var v = activity.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
                Close();
        }

        /// <summary>
        /// 更新里程碑进度文本
        /// </summary>
        /// <param name="milestoneIndex">指定里程碑索引，如果为-1则使用当前里程碑索引</param>
        private void UpdateMilestoneText(int milestoneIndex, bool withParticle)
        {
            if (milestoneText == null) return;

            // 获取里程碑索引
            int currentIndex = milestoneIndex >= 0 ? milestoneIndex : activity.GetMilestoneIndex();

            // 获取总里程碑数量
            int totalMilestones = activity.ListM.Count;

            // 更新文本显示 "x/y" 格式
            milestoneText.text = $"{currentIndex}/{totalMilestones}";

            if (withParticle)
            {
                // 播放文本缩放动画：从1-1.2-1，全长0.23秒
                milestoneText.transform.localScale = Vector3.one;
                milestoneText.transform.DOScale(Vector3.one * 1.2f, 0.115f)  // 0.23秒的一半时间放大到1.2
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        milestoneText.transform.DOScale(Vector3.one, 0.115f)  // 0.23秒的一半时间缩小回1
                            .SetEase(Ease.InQuad);
                    });

                if (milestoneParticle != null)
                {
                    milestoneParticle.gameObject.SetActive(true);
                    milestoneParticle.Play();
                }
            }
        }
        private IEnumerator CoUpdateMilestoneText(int milestoneIndex, bool withParticle, float delay)
        {
            yield return new WaitForSeconds(delay);
            UpdateMilestoneText(milestoneIndex, withParticle);
        }
        #endregion

        #region 数据刷新和显示

        /// <summary>
        /// 刷新里程碑列表显示
        /// </summary>
        /// <param name="currentMilestoneIndex">当前里程碑索引</param>
        private void RefreshList(int currentMilestoneIndex)
        {
            // 获取里程碑数据
            var milestoneList = activity.ListM;
            if (milestoneList == null || milestoneList.Count == 0) return;

            for (int i = 0; i < cellList.Count && i < 8; i++)
            {
                var cellComponent = cellList[i];
                if (cellComponent != null)
                {
                    // 检查里程碑索引是否有效
                    if (i < milestoneList.Count)
                    {
                        bool showFlag = i < currentMilestoneIndex - 1;
                        bool isClaimed = i < currentMilestoneIndex;
                        // 根据传入的里程碑索引判断显示状态
                        cellComponent.UpdateContent(milestoneList[i], isClaimed, showFlag);
                    }
                }
            }
        }

        // 刷新大奖显示
        private void RefreshBigReward()
        {
            // 检查是否有最终大奖配置
            if (activity.ConfDetail.FinalMilestoneReward.Count > 0)
            {
                // 获取最终大奖配置，这里不考虑多个配置的情况，如果后续有不同的需求，可以在这里修改
                var finalReward = activity.ConfDetail.FinalMilestoneReward[0].ConvertToRewardConfig();

                // 刷新大奖显示
                item.Refresh(finalReward, 17);
                item.gameObject.SetActive(true);
            }
            else
            {
                // 保底处理 没有最终大奖配置，隐藏大奖组件
                item.gameObject.SetActive(false);
            }
        }

        private void RefreshRewardIcon(int milestoneIndex)
        {
            // 根据当前里程碑状态刷新rewardIcon显示
            if (milestoneIndex >= 0 && milestoneIndex < activity.ListM.Count)
            {
                var node = activity.ListM[milestoneIndex];
                rewardIcon.Refresh(node.reward.Id, node.reward.Count);
                rewardIcon.gameObject.SetActive(true);
            }
            else
            {
                // 如果没有有效的里程碑，隐藏图标
                rewardIcon.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 初始化小车位置到指定里程碑点
        /// </summary>
        /// <param name="milestoneIndex">里程碑索引</param>
        private void InitializeCarPosition(int milestoneIndex)
        {
            // 检查小车和里程碑索引是否有效
            if (car == null || milestoneIndex < 0 || milestoneIndex >= cellList.Count || cellList[milestoneIndex] == null)
            {
                return;
            }
            Vector3 targetPosition;
            float targetRotation;
            if (milestoneIndex == 0)
            {
                targetPosition = startPoint.position;
                targetRotation = startPoint.eulerAngles.z;
            }
            else
            {
                targetPosition = cellList[milestoneIndex - 1].transform.position;
                targetRotation = cellList[milestoneIndex - 1].rotation;
            }
            // 将小车移动到当前里程碑点的位置
            car.transform.position = targetPosition;
            car.transform.eulerAngles = new Vector3(0, 0, targetRotation);
        }

        #endregion

        #region 动画播放

        /// <summary>
        /// 播放进度条动画和小车路径移动动画
        /// </summary>
        /// <param name="startScore">起始分数</param>
        /// <param name="endScore">结束分数</param>
        private IEnumerator CoPlayProgressAnimation(int startScore, int endScore)
        {
            // 第一步：预计算起始和结束分数对应的里程碑信息
            var (startMilestoneIndex, startShowScore, startMilestoneScore)
                = activity.CalculateScoreDisplayData(startScore);
            var (endMilestoneIndex, endShowScore, endMilestoneScore)
                = activity.CalculateScoreDisplayData(endScore);

            // 第二步：进度条逐个充满每个里程碑
            for (int milestoneIndex = startMilestoneIndex; milestoneIndex <= endMilestoneIndex; milestoneIndex++)
            {
                // 计算当前里程碑的进度条参数
                int currentShowScore;
                int currentMilestoneScore;

                if (milestoneIndex == startMilestoneIndex)
                {
                    // 第一个里程碑：从起始分数到里程碑结束
                    currentShowScore = startMilestoneScore; // 充满到里程碑结束
                    currentMilestoneScore = startMilestoneScore;
                }
                else if (milestoneIndex == endMilestoneIndex)
                {
                    // 最后一个里程碑：从开始到结束分数
                    currentShowScore = endShowScore;
                    currentMilestoneScore = endMilestoneScore;
                }
                else
                {
                    // 中间里程碑：从开始到结束
                    currentMilestoneScore = activity.GetMilestoneEndValue(milestoneIndex);
                    currentShowScore = currentMilestoneScore; // 充满到里程碑结束
                }

                // 进度条充满当前里程碑
                rewardProgress.RefreshWithTextAnimation(currentShowScore, currentMilestoneScore, progressBarFillDuration);

                // 等待进度条动画完成
                yield return new WaitForSeconds(progressBarFillDuration);

                // 如果不是最后一个里程碑，重置进度条从头开始
                if (milestoneIndex < endMilestoneIndex)
                {
                    if (progressItemAnim.isPlaying)
                    {
                        progressItemAnim.Stop();
                    }
                    progressItemAnim.Play("ProgressGroup_punch_piece");
                    yield return new WaitForSeconds(0.367f);

                    // 动画完成后刷新图标
                    RefreshRewardIcon(milestoneIndex + 1);
                    rewardProgress.Refresh(0, currentMilestoneScore);
                }
            }

            // 第三步：小车路径移动动画 - 逐个移动到里程碑点并播放领奖动画
            // 从起始里程碑开始，逐个移动到每个里程碑点
            for (int milestoneIndex = startMilestoneIndex; milestoneIndex <= endMilestoneIndex; milestoneIndex++)
            {
                // 检查是否需要播放领奖动画
                // 只有完整跨越的里程碑才会触发领奖动画
                if (milestoneIndex < endMilestoneIndex || activity.HasComplete())
                {
                    // 移动小车到当前里程碑点
                    yield return StartCoroutine(MoveCarToMilestone(milestoneIndex));

                    // 播放领奖动画
                    yield return PlayRewardAnimation(milestoneIndex);
                }
            }

            // 动画结束，清理协程引用
            _progressAnimationCoroutine = null;
            UnlockEvent();
            PlayBtnShow();
        }

        /// <summary>
        /// 移动小车到指定里程碑点
        /// </summary>
        /// <param name="milestoneIndex">里程碑索引</param>
        private IEnumerator MoveCarToMilestone(int milestoneIndex)
        {
            // 检查里程碑索引是否有效
            if (milestoneIndex < 0 || milestoneIndex >= cellList.Count || cellList[milestoneIndex] == null)
            {
                yield break;
            }
            Game.Manager.audioMan.TriggerSound("ScorePieceGo");
            if (milestoneIndex > 0)
            {
                cellList[milestoneIndex - 1].PlayFlagShow(flagAnimDelay);
            }
            // 获取目标里程碑点
            UIScoreMilestoneItem_piece targetCell = cellList[milestoneIndex];

            if (cellList.Count > 0) // 确保cellList不为空
            {
                // 使用路径移动
                yield return StartCoroutine(MoveCarAlongPath(targetCell, milestoneIndex));
            }
            else
            {
                Vector3 targetPosition = targetCell.transform.position;
                // 如果没有设置路径点，使用直线移动
                car.transform.DOMove(targetPosition, carMoveDuration)
                    .SetEase(carMoveEase)
                    .OnStart(OnCarMove)
                    .OnComplete(OnCarIdle);
                yield return new WaitForSeconds(carMoveDuration - earlyGetRewardTime);
            }
        }
        List<Vector3> pathPositions = new List<Vector3>();

        /// <summary>
        /// 沿路径移动小车到目标位置
        /// </summary>
        /// <param name="targetCell">目标cell组件</param>
        /// <param name="milestoneIndex">里程碑索引，用于更新进度文本</param>
        private IEnumerator MoveCarAlongPath(UIScoreMilestoneItem_piece targetCell, int milestoneIndex = -1)
        {
            // 构建路径点数组
            pathPositions.Clear();

            // 使用目标cell的路径点
            if (targetCell != null && targetCell.pathPoints.Count > 0)
            {
                // 添加目标cell的所有路径点
                foreach (var pathPoint in targetCell.pathPoints)
                {
                    if (pathPoint != null)
                    {
                        pathPositions.Add(pathPoint.position);
                    }
                }
            }

            // 如果没有路径点，添加目标位置作为终点
            if (pathPositions.Count == 0)
            {
                pathPositions.Add(targetCell.transform.position);
            }

            // 使用DOTween的DOPath进行路径移动
            var tween = car.transform.DOPath(pathPositions.ToArray(), carMoveDuration, pathType, pathMode)
                .SetEase(carMoveEase)
                .OnStart(OnCarMove)
                .OnComplete(OnCarIdle);

            // 直接使用目标cell的rotation配置
            float targetRotation = targetCell.rotation;

            float rotationDuration = carMoveDuration - targetCell.rotationDelay - targetCell.rotationEarly; // 总时长50%（提前25%结束）

            // 延迟执行旋转动画
            car.transform.DORotate(new Vector3(0, 0, targetRotation), rotationDuration)
                .SetEase(Ease.Linear)
                .SetDelay(targetCell.rotationDelay); // 延迟开始

            yield return new WaitForSeconds(carMoveDuration - earlyGetRewardTime);
        }

        /// <summary>
        /// 播放领奖动画
        /// </summary>
        /// <param name="milestoneIndex">里程碑索引</param>
        private IEnumerator PlayRewardAnimation(int milestoneIndex)
        {
            var milestoneList = activity.ListM;
            if (milestoneIndex >= 0 && milestoneIndex < milestoneList.Count)
            {
                var node = milestoneList[milestoneIndex];
                var reward = node.reward;
                var rewardData = activity.TryGetCommitReward(reward);
                if (rewardData != null)
                {
                    // 根据RefreshList的逻辑，cellList[i]显示milestoneList[i]
                    // 所以milestoneIndex就是对应的Cell索引
                    int cellIndex = milestoneIndex;

                    // 检查Cell索引是否有效（限制在0-7范围内）
                    if (cellIndex >= 0 && cellIndex < cellList.Count && cellIndex < 8 && cellList[cellIndex] != null)
                    {
                        UIScoreMilestoneItem_piece item = cellList[cellIndex];
                        if (item != null)
                        {
                            item.Hide();
                            yield return new WaitForSeconds(rewardFlyDelay);

                            UIFlyUtility.FlyReward(rewardData, item.commonItem.transform.position);
                            // 延迟0.75秒更新里程碑文本，不阻塞其他流程
                            StartCoroutine(CoUpdateMilestoneText(milestoneIndex + 1, true, 0.75f));
                            //特殊处理宝箱的流程
                            if (rewardData.rewardType == ObjConfigType.RandomBox)
                            {
                                yield return new WaitForSeconds(1f);
                                // 等待宝箱UI关闭（表示领取完成）
                                yield return new WaitUntil(() => !UIManager.Instance.IsShow(UIConfig.UIRandomBox) && !UIManager.Instance.IsShow(UIConfig.UISingleReward));
                            }
                            else
                            {
                                yield return new WaitForSeconds(1.5f);
                            }
                            // 更新里程碑进度文本
                            //如果领的是最终奖励，需要领额外大奖
                            if (milestoneIndex == milestoneList.Count - 1 && activity.ConfDetail.FinalMilestoneReward.Count > 0)
                            {
                                yield return StartCoroutine(PlayFinalRewardAnimation());
                                //这里如果表现需要等一会关，可以加一个延迟
                            }
                            if (milestoneIndex == milestoneList.Count - 1)
                            {
                                if (activity.HasComplete())
                                {
                                    Close();
                                    yield break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 播放最终大奖动画
        /// </summary>
        private IEnumerator PlayFinalRewardAnimation()
        {
            Game.Manager.audioMan.TriggerSound("ScorePieceHit");
            centerRewardAnim.Play("Score_piece_centerreward_disappear");

            // 获取最终大奖配置
            var finalReward = activity.ConfDetail.FinalMilestoneReward[0].ConvertToRewardConfig();
            var finalRewardData = activity.TryGetCommitReward(finalReward);

            yield return new WaitForSeconds(rewardFlyDelay);

            // 从大奖显示位置飞向目标
            UIFlyUtility.FlyReward(finalRewardData, item.transform.position, size: 256);

            // 等待最终大奖飞行动画完成
            yield return new WaitForSeconds(1f);
        }
        private void OnCarMove()
        {
            carAnim.Play("Score_piece_car_gear_move");
        }
        private void OnCarIdle()
        {
            carAnim.Play("Score_piece_car_gear_idle");
        }
        #endregion

        #region Debug方法
#if UNITY_EDITOR
        /// <summary>
        /// Debug移动监听协程
        /// </summary>
        private IEnumerator CoDebugMoveListener()
        {
            Debug.Log("Debug监听协程已启动");

            while (true)
            {
                yield return null;

                // 检查Debug按钮是否被点击
                if (debugMoveNext)
                {
                    Debug.Log("检测到Debug按钮点击");
                    debugMoveNext = false; // 重置按钮状态
                    DebugMoveToNext();
                }

                // 检查键盘输入
                CheckKeyboardInput();
            }
        }

        /// <summary>
        /// 检查键盘输入
        /// </summary>
        private void CheckKeyboardInput()
        {
            // 按G键移动到下一个格子
            if (Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("检测到G键按下，移动到下一个格子");
                DebugMoveToNext();
            }

            // 按0-8键重置到第n个格子
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                Debug.Log("检测到0键按下，重置到起点");
                DebugResetToPosition(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("检测到1键按下，重置到第1个格子");
                DebugResetToPosition(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("检测到2键按下，重置到第2个格子");
                DebugResetToPosition(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("检测到3键按下，重置到第3个格子");
                DebugResetToPosition(3);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Debug.Log("检测到4键按下，重置到第4个格子");
                DebugResetToPosition(4);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                Debug.Log("检测到5键按下，重置到第5个格子");
                DebugResetToPosition(5);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                Debug.Log("检测到6键按下，重置到第6个格子");
                DebugResetToPosition(6);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                Debug.Log("检测到7键按下，重置到第7个格子");
                DebugResetToPosition(7);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                Debug.Log("检测到8键按下，重置到第8个格子");
                DebugResetToPosition(8);
            }
        }

        /// <summary>
        /// Debug移动到下一个格子
        /// </summary>
        private void DebugMoveToNext()
        {
            if (cellList == null || cellList.Count == 0)
            {
                Debug.LogWarning("cellList为空，无法进行Debug移动");
                return;
            }

            // 从当前里程碑索引开始移动
            int currentIndex = activity.GetMilestoneIndex();
            int nextIndex = currentIndex + _debugMoveIndex;

            // 如果超出范围，循环到开始
            if (nextIndex >= cellList.Count)
            {
                nextIndex = nextIndex % cellList.Count;
            }

            // 获取目标cell
            var targetCell = cellList[nextIndex];
            if (targetCell == null)
            {
                Debug.LogWarning($"目标cell为空，索引: {nextIndex}");
                return;
            }

            // 执行Debug移动动画协程
            StartCoroutine(MoveCarAlongPath(targetCell, nextIndex));

            // 更新Debug索引
            _debugMoveIndex++;

            Debug.Log($"Debug移动: 从当前里程碑{currentIndex}开始，移动{_debugMoveIndex}步到{nextIndex}");
        }

        /// <summary>
        /// Debug重置到指定位置
        /// </summary>
        /// <param name="positionIndex">位置索引，0为起点，1-8为格子索引</param>
        private void DebugResetToPosition(int positionIndex)
        {
            if (car == null)
            {
                Debug.LogWarning("小车为空，无法重置位置");
                return;
            }

            Vector3 targetPosition;
            float targetRotation;

            if (positionIndex == 0)
            {
                // 重置到起点
                targetPosition = startPoint.position;
                targetRotation = startPoint.eulerAngles.z;
                Debug.Log("重置小车到起点位置");
                // 重置到起点时，Debug索引设为0（初始状态）
                _debugMoveIndex = 0;
            }
            else
            {
                // 重置到指定格子
                int cellIndex = positionIndex - 1; // 转换为cell索引
                if (cellIndex < 0 || cellIndex >= cellList.Count)
                {
                    Debug.LogWarning($"格子索引超出范围: {positionIndex}，最大格子数: {cellList.Count}");
                    return;
                }

                var targetCell = cellList[cellIndex];
                if (targetCell == null)
                {
                    Debug.LogWarning($"目标cell为空，索引: {cellIndex}");
                    return;
                }

                targetPosition = targetCell.transform.position;
                targetRotation = targetCell.rotation;
                Debug.Log($"重置小车到第{positionIndex}个格子位置");

                // _debugMoveIndex是从初始状态开始的步数
                // 如果重置到第n个格子，那么从初始状态需要移动n步
                _debugMoveIndex = positionIndex;
            }

            // 直接设置位置和角度，不使用动画
            car.transform.position = targetPosition;
            car.transform.eulerAngles = new Vector3(0, 0, targetRotation);

            Debug.Log($"小车已重置到位置{positionIndex}，坐标: {targetPosition}，角度: {targetRotation}，Debug索引调整为: {_debugMoveIndex}（从初始状态开始的步数）");
        }
#endif
        #endregion
    }
}
