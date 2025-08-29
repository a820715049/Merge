using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FAT.Merge;
using Config;
using fat.rawdata;
using DG.Tweening;
using EL; // Added for DOTween
using System.Linq;
using static EL.PoolMapping;
using Coffee.UIExtensions;
using TMPro;

namespace FAT
{
    /// <summary>
    /// 矿车主控组件（动画 + 数字进度 + 下一个奖励UI）
    /// 职责概述：
    /// 1) 运动系统：加速/匀速/减速，保持小车居中，通过背景滚动体现移动；同步控制 cart/wheel/box 的 Spine 动画与粒子/音效。
    /// 2) 数字进度：只记录「开始值」与「目标值」，用矿车运动百分比线性插值，转米显示（配合 EventMineCartDetail.DistanceFactor 与 #SysComDesc892）。
    /// 3) 下一个奖励UI：根据快照数据动态计算“下一个奖励”（小/大），显示图标与剩余里程，并在变化时播放位移刷新动画（移出→刷新→移入）。
    /// 4) 数据快照：在初始化和每段运动结束时快照 Base 值与回合配置，避免大奖结算/回合切换造成显示抖动或错误。
    /// 5) 交互约束：次级奖励（小奖）不弹窗且按钮禁用；仅大奖允许查看奖励 Tips。
    /// </summary>
    public class MineCartItem : MonoBehaviour
    {
        #region Movement
        [SerializeField] private Spine.Unity.SkeletonGraphic cartSpineAnim;
        [SerializeField] private Spine.Unity.SkeletonGraphic wheelSpineAnim;
        [SerializeField] private Spine.Unity.SkeletonGraphic boxSpineAnim;
        [SerializeField] private GameObject cart;
        [Header("所有运动的背景,顺序无关紧要")]
        [SerializeField] private List<MineCartBackground> backgroundLayers = new List<MineCartBackground>();
        [Header("运动参数")]
        [SerializeField] private float moveDistance = 5f;       // 每次移动的距离
        [SerializeField] private float firstSpeed = 2f;         // 第一速度
        [SerializeField] private float secondSpeed = 4f;        // 第二速度
        [SerializeField] private float acceleration = 4f;       // 恒定加速度
        [SerializeField] private float deceleration = 4f;       // 恒定减速度
        [Header("Box 动画配置")]
        [SerializeField] private float boxStopDelay = 0.1f;     // 减速后延迟播放stop
        [SerializeField] private GameObject finalItem;
        [SerializeField] private Animation finalFireWorkAni;
        [SerializeField] private UIParticle wheelParticle1;
        [SerializeField] private UIParticle wheelParticle2;
        [SerializeField] private UIParticle wheelParticle3;
        [SerializeField] private UIParticle collectParticle;
        private const string ANIM_IDLE = "idle";
        private const string ANIM_MOVE = "move";
        private const string ANIM_FINISH = "finish";
        private const string BOX_ANIM_IDLE = "idle";
        private const string BOX_ANIM_MOVE = "move";
        private const string BOX_ANIM_STOP = "stop";
        private const float MIX_TIME_TO_IDLE = 0.9f;    // 切换到idle的混合时间
        private const float MIX_TIME_DEFAULT = 0.1f;    // 其他动画切换的混合时间
        private const float FINAL_BUBBLE_OFFSET = 130f;  // Final气泡的视觉偏移
        private const float NORMAL_BUBBLE_OFFSET = 130f; // 普通气泡的视觉偏移
        private const float FINAL_ITEM_OFFSET = 328f;   // FinalItem的视觉偏移
        [SerializeField] private float spineTimeScale_Move_Min = 0.5f;
        [SerializeField] private float spineTimeScale_Move_Max = 2f;
        [SerializeField] private float wheelSpineTimeScale_Move_Min = 0f;  // 轮子可以完全停止
        [SerializeField] private float wheelSpineTimeScale_Move_Max = 2f;
        [Header("入场动画配置")]
        [SerializeField] private float enterAnimationTime = 1.5f;
        [SerializeField] private float enterAnimationDelay = 1f;           // 入场动画时间
        [SerializeField] private AnimationCurve enterAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // 入场动画曲线
        [SerializeField] private string moveLoopSoundName = "MineCartBoardMove";
        [SerializeField] private string acceptSoundName = "MineCartBoardAccept";
        [SerializeField] private string boxBreakSoundName = "MineCartBoardBox";
        [SerializeField] private string finalRewardSoundName = "MineCartBoardWin";
        private UIBase ui;
        private MineCartActivity _activity;
        private CartState _state;
        private float currentMoveSpeed;
        private float totalDistance = 0f;           // 已移动的总距离
        private float targetDistance = 0f;          // 目标移动距离
        private bool isSecondSpeed = false;         // 是否使用第二速度
        private bool isWheelParticlesPlaying = false; // 轮子粒子效果播放状态
        private bool isMoveLoopSoundPlaying = false;  // 前进循环音效是否播放中

        private enum CartState
        {
            None,
            Enter,
            Idle,
            Accelerating,   // 加速阶段
            Moving,         // 匀速移动
            Decelerating,   // 减速阶段
        }

        public System.Action OnDistanceCompleted;   // 每完成一个Distance时触发
        public System.Action OnFinalRewardCollected; // Final奖励收集完成时触发
        public System.Action OnFinalRewardClaimStart; // Final奖励开始领取时触发（用于UI动画）

        // （已简化）显示更新不再单独 gating，依赖 _dispActive 与移动百分比
        private int _baseSnapshotValue = 0;
        private bool _boxExpectIdleAfterStop = false;
        private Coroutine _boxStopDelayCo;

        /// <summary>
        /// 解析并注入 UI 与活动实例（生命周期早于 OnPreOpen）。
        /// </summary>
        public void OnParse(UIBase ui, MineCartActivity activity)
        {
            this.ui = ui;
            _activity = activity;
        }

        /// <summary>
        /// 初始化显示与动画初态，并快照 Base 值与刷新数值/奖励UI。
        /// 应在窗口打开前调用（Activity 已存在）。
        /// </summary>
        public void OnPreOpen()
        {
            // 初始化基础状态
            totalDistance = 0f;
            targetDistance = 0f;
            currentMoveSpeed = 0f;
            finalItem.SetActive(false);
            dialogBubbleLeft.gameObject.SetActive(false);
            TargetItemDialog.Hide();
            currentBubbleState = DialogBubbleState.Idle;
            idleTimer = 0;
            showTimer = 0;
            // 初始化Spine动画
            if (cartSpineAnim != null)
            {
                cartSpineAnim.timeScale = 1f;
                var track = cartSpineAnim.AnimationState.SetAnimation(0, ANIM_IDLE, true);
                track.MixDuration = MIX_TIME_TO_IDLE;
            }
            if (boxSpineAnim != null)
            {
                var t = boxSpineAnim.AnimationState.SetAnimation(0, BOX_ANIM_IDLE, true);
                t.MixDuration = MIX_TIME_TO_IDLE;
                _boxExpectIdleAfterStop = false;
            }
            if (wheelSpineAnim != null)
            {
                wheelSpineAnim.timeScale = 0f;  // 轮子完全停止
                var track = wheelSpineAnim.AnimationState.SetAnimation(0, ANIM_MOVE, true);
                track.MixDuration = MIX_TIME_DEFAULT;
            }

            // 确保轮子粒子效果停止并隐藏
            StopWheelParticles();
            isWheelParticlesPlaying = false; // 重置状态

            // 确保粒子默认隐藏
            if (wheelParticle1 != null)
            {
                wheelParticle1.gameObject.SetActive(false);
            }
            if (wheelParticle2 != null)
            {
                wheelParticle2.gameObject.SetActive(false);
            }
            if (wheelParticle3 != null)
            {
                wheelParticle3.gameObject.SetActive(false);
            }
            // Init 时快照一次 Base（在初始化显示之前）
            if (_activity != null)
            {
                _baseSnapshotValue = _activity.BaseMilestoneNum;
            }
            // 初始化数字进度与下一个大奖UI（若已绑定）
            InitProgressAndNextRewardUI();
            // 显示推进随运动开始自然进行
        }
        /// <summary>
        /// 入场：未播放过入场动画则触发入场位移并在 70% 时切至 Idle 与 Box-Stop；否则直接 Idle。
        /// </summary>
        public void OnPostOpen()
        {
            // 检查是否需要播放入场动画
            if (_activity != null && !_activity.HasPlayedEnterAnimation)
            {
                ChangeState(CartState.Enter);  // 启动入场动画
            }
            else
            {
                // 确保小车在中心位置
                RectTransform cartRect = cart.GetComponent<RectTransform>();
                if (cartRect != null)
                {
                    float anchoredPosY = cartRect.anchoredPosition.y;
                    cartRect.anchoredPosition = new Vector2(0, anchoredPosY);
                }
                ChangeState(CartState.Idle);   // 直接进入Idle状态
            }
        }


        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.G))
            {
                MoveCart("", default);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                var listT = PoolMapping.PoolMappingAccess.Take<List<RewardCommitData>>(out var list);
                string debugStr = "31:100,1:50";
                var debugReward = debugStr.ConvertToRewardConfig();
                var reward = Game.Manager.rewardMan.BeginReward(debugReward.Id, debugReward.Count, ReasonString.mine_cart_round_reward);
                list.Add(reward);
                MoveCart("event_minecartboard_s001:i_s_minecart_reward6_s001.png", listT);
            }
#endif

            // 根据当前状态处理移动
            switch (_state)
            {
                case CartState.Accelerating:
                    HandleAccelerating();
                    break;
                case CartState.Moving:
                    HandleMoving();
                    break;
                case CartState.Decelerating:
                    HandleDecelerating();
                    break;
            }

            UpdateBackgrounds();
            UpdateDialogBubble();
#if UNITY_EDITOR
            UpdateDebugInfo();
#endif
        }

        /// <summary>
        /// 移动小车，每次调用移动一个Distance的距离
        /// </summary>
        public void MoveCart(string icon, Ref<List<RewardCommitData>> reward, float delay = 0f)
        {
            if (!string.IsNullOrEmpty(icon) && ui != null)
            {
                ui.LockEvent();
            }
            StartCoroutine(DelayedMoveCart(icon, reward, delay));
        }

        /// <summary>
        /// 获取当前一次或累计移动相对于目标距离的进度(0~1)。若无目标距离则视为完成返回1。
        /// </summary>
        private float GetMovementProgress01()
        {
            if (targetDistance <= 0.0001f)
            {
                // 移动尚未开始（Idle前置延迟）返回0；
                // 已结束时由 CompleteMovement 收尾并停止更新。
                return _state == CartState.Idle ? 0f : 1f;
            }
            return Mathf.Clamp01(totalDistance / targetDistance);
        }

        /// <summary>
        /// 开始移动，计算各阶段距离并进入加速阶段
        /// </summary>
        private void StartMove()
        {
            totalDistance = 0f;
            targetDistance = moveDistance;
            currentMoveSpeed = 0f;
            isSecondSpeed = false;
            // 播放矿车前进行效（配置为Loop）
            Game.Manager.audioMan.TriggerSound(moveLoopSoundName);
            isMoveLoopSoundPlaying = true;
            ChangeState(CartState.Accelerating);
        }

        /// <summary>
        /// 处理移动过程中的额外移动请求
        /// </summary>
        private void HandleAdditionalMove()
        {
            targetDistance += moveDistance;
            switch (_state)
            {
                case CartState.Accelerating:
                    isSecondSpeed = true;
                    break;
                case CartState.Moving:
                    if (!isSecondSpeed)
                    {
                        isSecondSpeed = true;
                        ChangeState(CartState.Accelerating);
                    }
                    break;
                case CartState.Decelerating:
                    if (Mathf.Abs(currentMoveSpeed) > firstSpeed)
                    {
                        isSecondSpeed = true;
                        ChangeState(CartState.Accelerating);
                    }
                    else
                    {
                        isSecondSpeed = false;
                        ChangeState(CartState.Accelerating);
                    }
                    break;
            }
        }

        /// <summary>
        /// 更新背景位置
        /// </summary>
        private void UpdateBackgrounds()
        {
            if (backgroundLayers.Count <= 0) return;

            float frameOffset = currentMoveSpeed * Time.deltaTime;
            foreach (var bg in backgroundLayers)
            {
                if (bg != null)
                {
                    bg.SetPosition(frameOffset);
                }
            }

            UpdateRewardPositions();
            UpdateFinalItemPosition(frameOffset);
        }

        private void UpdateFinalItemPosition(float frameOffset)
        {
            if (finalItem != null && finalItem.activeSelf)
            {
                var finalItemRect = finalItem.GetComponent<RectTransform>();

                // 更新位置
                Vector2 pos = finalItemRect.anchoredPosition;
                pos.x += frameOffset;
                finalItemRect.anchoredPosition = pos;

                // 转换到屏幕坐标来判断是否移出屏幕
                Canvas canvas = mainBackground.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Vector3[] corners = new Vector3[4];
                    finalItemRect.GetWorldCorners(corners);
                    // corners[2]是右下角
                    Vector2 rightEdgeScreenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);

                    if (rightEdgeScreenPos.x < 0)
                    {
                        finalItem.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// 处理加速阶段
        /// </summary>
        private void HandleAccelerating()
        {
            float currentSpeedAbs = Mathf.Abs(currentMoveSpeed);
            float newSpeedAbs = currentSpeedAbs + acceleration * Time.deltaTime;
            float frameDistance = (currentSpeedAbs + newSpeedAbs) * 0.5f * Time.deltaTime;

            currentMoveSpeed = -newSpeedAbs;
            totalDistance += frameDistance;

            // 更新动画速度
            UpdateAnimationSpeed(newSpeedAbs);

            CheckRewardCollision();
            TickDisplayProgressByMovement();

            // 先检查是否需要开始减速
            float remainingDistance = targetDistance - totalDistance;
            float decelerationDistance = (newSpeedAbs * newSpeedAbs) / (2f * deceleration);
            if (remainingDistance <= decelerationDistance)
            {
                ChangeState(CartState.Decelerating);
                return;
            }

            // 如果不需要减速，检查是否达到目标速度
            float targetSpeed = isSecondSpeed ? secondSpeed : firstSpeed;
            if (newSpeedAbs >= targetSpeed)
            {
                currentMoveSpeed = -targetSpeed;
                ChangeState(CartState.Moving);
            }
        }

        /// <summary>
        /// 处理匀速阶段
        /// </summary>
        private void HandleMoving()
        {
            float remainingDistance = targetDistance - totalDistance;
            float frameDistance = Mathf.Abs(currentMoveSpeed) * Time.deltaTime;
            totalDistance += frameDistance;

            CheckRewardCollision();
            TickDisplayProgressByMovement();

            float currentSpeedAbs = Mathf.Abs(currentMoveSpeed);
            float decelerationDistance = (currentSpeedAbs * currentSpeedAbs) / (2f * deceleration);

            // 更新动画速度
            UpdateAnimationSpeed(currentSpeedAbs);

            if (remainingDistance <= decelerationDistance)
            {
                ChangeState(CartState.Decelerating);
            }
        }

        /// <summary>
        /// 更新动画播放速度
        /// </summary>
        private void UpdateAnimationSpeed(float currentSpeed)
        {
            if (_state != CartState.Idle)
            {
                // 根据当前速度计算动画速度倍率
                float targetSpeed = isSecondSpeed ? secondSpeed : firstSpeed;

                // 防止除零错误和异常值
                float speedRatio = 0f;
                if (targetSpeed > 0.01f)
                {
                    speedRatio = currentSpeed / targetSpeed;
                    // 限制speedRatio在合理范围内
                    speedRatio = Mathf.Clamp01(speedRatio);
                }

                // 更新cart的动画速度
                if (cartSpineAnim != null)
                {
                    float cartTimeScale = Mathf.Lerp(spineTimeScale_Move_Min, spineTimeScale_Move_Max, speedRatio);
                    cartSpineAnim.timeScale = cartTimeScale;
                }

                // 更新轮子的动画速度
                if (wheelSpineAnim != null)
                {
                    float wheelTimeScale = Mathf.Lerp(wheelSpineTimeScale_Move_Min, wheelSpineTimeScale_Move_Max, speedRatio);
                    wheelSpineAnim.timeScale = wheelTimeScale;
                }
            }
        }

        /// <summary>
        /// 处理减速阶段
        /// </summary>
        private void HandleDecelerating()
        {
            float currentSpeedAbs = Mathf.Abs(currentMoveSpeed);
            float remainingDistance = targetDistance - totalDistance;

            if (remainingDistance <= 0.01f)
            {
                CompleteMovement();
                return;
            }

            float newSpeedAbs = currentSpeedAbs - deceleration * Time.deltaTime;
            if (newSpeedAbs < 0) newSpeedAbs = 0;

            float frameDistance = (currentSpeedAbs + newSpeedAbs) * 0.5f * Time.deltaTime;
            if (frameDistance > remainingDistance)
            {
                frameDistance = remainingDistance;
                newSpeedAbs = 0;
            }

            currentMoveSpeed = newSpeedAbs == 0 ? 0 : -newSpeedAbs;
            totalDistance += frameDistance;

            // 更新动画速度
            UpdateAnimationSpeed(newSpeedAbs);

            CheckRewardCollision();
            TickDisplayProgressByMovement();

            // 计算剩余时间
            float remainingTime = newSpeedAbs / deceleration;
            if (remainingTime <= 1f)
            {
                var currentTrack = cartSpineAnim.AnimationState.GetCurrent(0);
                if (currentTrack != null && currentTrack.Animation.Name == ANIM_MOVE)
                {
                    var track = cartSpineAnim.AnimationState.SetAnimation(0, ANIM_IDLE, true);
                    track.MixDuration = MIX_TIME_TO_IDLE;
                }
            }

            if (newSpeedAbs <= 0.01f || totalDistance >= targetDistance - 0.01f)
            {
                CompleteMovement();
            }
        }

        /// <summary>
        /// 完成当前一段移动：
        /// - 触发 OnDistanceCompleted 回调
        /// - 落定状态至 Idle、停止循环音效
        /// - 将显示数值推进至目标并停止动画
        /// - 统一走“下一个奖励面板”的移出→刷新→移入流程
        /// - 更新 Base 快照，确保后续里程/奖励计算稳定
        /// </summary>
        private void CompleteMovement()
        {
            if (totalDistance >= targetDistance)
            {
                OnDistanceCompleted?.Invoke();
            }
            ChangeState(CartState.Idle);
            // 真正停止后，下一次进入 Idle 时播放 box 的 stop
            // Idle 内部逻辑会根据延迟与当前状态决定是否真正播放 stop
            if (_dispActive)
            {
                _dispCurrent = _dispTargetScore;
                _dispActive = false;
                ApplyProgressText();
            }
            // 矿车本段运动结束后，统一检测并刷新"下一个奖励"面板（走移出→应用→移入流程）
            int nextIdxAfterMove = GetNextMilestoneIndex();
            RequestNextRewardRefresh(nextIdxAfterMove);
            // 显示推进结束
            // 运动全部结束时再快照一次 Base
            if (_activity != null)
            {
                _baseSnapshotValue = _activity.BaseMilestoneNum;
            }
        }

        /// <summary>
        /// 切换矿车状态
        /// </summary>
        private void ChangeState(CartState state)
        {
            if (_state == state) return;
            _state = state;

            if (cartSpineAnim != null)
            {
                switch (state)
                {
                    case CartState.Enter:
                        StartEnterAnimation();
                        PlayBoxMove();
                        break;
                    case CartState.Idle:
                        PlayIdleAnimation();
                        PlayBoxStopThenIdleIfNeeded();
                        totalDistance = 0f;
                        targetDistance = 0f;
                        currentMoveSpeed = 0f;
                        // 结束前进时停止循环音效
                        if (isMoveLoopSoundPlaying)
                        {
                            Game.Manager.audioMan.StopLoopSound();
                            isMoveLoopSoundPlaying = false;
                        }
                        break;
                    case CartState.Accelerating:
                        PlayAcceleratingAnimation();
                        PlayBoxMove();
                        break;
                    case CartState.Moving:
                        PlayMovingAnimation();
                        PlayBoxMove();
                        break;
                    case CartState.Decelerating:
                        PlayDeceleratingAnimation();
                        // 减速时延迟触发 box 的 stop，期间若再次移动会被 PlayBoxMove 取消
                        ScheduleBoxStopDelayed();
                        break;
                }
            }
        }

        // 动画相关方法
        private void StartEnterAnimation()
        {
            // 获取小车的RectTransform
            RectTransform cartRect = cart.GetComponent<RectTransform>();

            // 获取Canvas来计算屏幕尺寸
            Canvas canvas = cartRect.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found!");
                ChangeState(CartState.Idle);
                return;
            }

            float anchoredPosY = cartRect.anchoredPosition.y;
            // 计算屏幕中心位置（目标位置）
            Vector2 centerPosition = new Vector2(0, anchoredPosY);

            // 计算屏幕最左侧位置（起始位置）
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float screenWidth = canvasRect.rect.width;
            Vector2 leftPosition = new Vector2(-screenWidth * 0.5f, anchoredPosY);

            // 设置初始位置为屏幕最左侧
            cartRect.anchoredPosition = leftPosition;


            // 播放DoTween入场动画（用 OnUpdate 绑定 70% 事件，避免时间误差导致的提前执行）
            bool invoked70Percent = false;
            var enterTween = cartRect.DOAnchorPosX(centerPosition.x, enterAnimationTime)
                .OnStart(() =>
                {
                    cartSpineAnim.AnimationState.SetAnimation(0, ANIM_MOVE, true);
                    // 轮子播放Move动画
                    if (wheelSpineAnim != null)
                    {
                        wheelSpineAnim.timeScale = 1f;
                    }
                    // 入场开始时也播放前进循环音效
                    Game.Manager.audioMan.TriggerSound(moveLoopSoundName);
                    isMoveLoopSoundPlaying = true;
                })
                .SetEase(enterAnimationCurve)
                .SetDelay(enterAnimationDelay)
                .OnUpdate(() =>
                {
                    if (invoked70Percent) return;
                    // 基于位置计算归一化进度
                    float ratio = Mathf.InverseLerp(leftPosition.x, centerPosition.x, cartRect.anchoredPosition.x);
                    if (ratio >= 0.7f)
                    {
                        invoked70Percent = true;
                        if (cartSpineAnim != null && cartSpineAnim.AnimationState.GetCurrent(0).Animation.Name == ANIM_MOVE)
                        {
                            var track = cartSpineAnim.AnimationState.SetAnimation(0, ANIM_IDLE, true);
                            track.MixDuration = MIX_TIME_TO_IDLE;
                        }
                        // 先 Stop 再 Idle：触发 Stop，之后根据状态回落到 Idle（若被移动打断则不回 Idle）
                        PlayBoxStop();
                    }
                })
                .OnComplete(() =>
                {
                    // 标记Activity中的入场动画已播放
                    if (_activity != null)
                    {
                        _activity.HasPlayedEnterAnimation = true;
                    }
                    ChangeState(CartState.Idle);
                });
        }

        private void PlayIdleAnimation()
        {
            if (cartSpineAnim != null && !isPlayingFinalSpine)
            {
                if (cartSpineAnim.AnimationState.GetCurrent(0).Animation.Name != ANIM_IDLE)
                {
                    cartSpineAnim.timeScale = 1f;
                    var track = cartSpineAnim.AnimationState.SetAnimation(0, ANIM_IDLE, true);
                    track.MixDuration = MIX_TIME_TO_IDLE;
                }
            }
            // 轮子保持Move动画，通过TimeScale控制速度
            if (wheelSpineAnim != null && !isPlayingFinalSpine)
            {
                wheelSpineAnim.timeScale = 0f;  // 静止时轮子停止
            }

            // 停止轮子粒子效果
            StopWheelParticles();
        }
        private void PlayBoxStopThenIdleIfNeeded()
        {
            if (boxSpineAnim == null) return;
            // Idle 时不强制 stop，stop 延迟由减速阶段的协程触发
            if (!_boxExpectIdleAfterStop)
            {
                var t = boxSpineAnim.AnimationState.SetAnimation(0, BOX_ANIM_IDLE, true);
                t.MixDuration = MIX_TIME_TO_IDLE;
            }
        }

        private void PlayAcceleratingAnimation()
        {
            if (!isPlayingFinalSpine)
            {
                if (cartSpineAnim != null)
                {
                    var track = cartSpineAnim.AnimationState.SetAnimation(0, ANIM_MOVE, true);
                    track.MixDuration = MIX_TIME_DEFAULT;
                    cartSpineAnim.timeScale = spineTimeScale_Move_Min;
                }
                // 轮子保持Move动画，通过TimeScale控制速度
                if (wheelSpineAnim != null)
                {
                    wheelSpineAnim.timeScale = wheelSpineTimeScale_Move_Min;
                }
            }

            // 播放轮子粒子效果
            PlayWheelParticles();
        }

        private void PlayBoxMove()
        {
            if (boxSpineAnim == null) return;
            _boxExpectIdleAfterStop = false; // 进入移动，取消任何等待 idle 的预期
            if (_boxStopDelayCo != null) { StopCoroutine(_boxStopDelayCo); _boxStopDelayCo = null; }
            var t = boxSpineAnim.AnimationState.SetAnimation(0, BOX_ANIM_MOVE, true);
            t.MixDuration = MIX_TIME_DEFAULT;
        }

        private void PlayMovingAnimation()
        {
            if (!isPlayingFinalSpine)
            {
                if (cartSpineAnim != null)
                {
                    // 保持当前动画，只更新速度
                    cartSpineAnim.timeScale = 1f;
                }
                // 轮子保持Move动画，通过TimeScale控制速度
                if (wheelSpineAnim != null)
                {
                    wheelSpineAnim.timeScale = 1f;
                }
            }

            // 播放轮子粒子效果
            PlayWheelParticles();
        }

        private void PlayDeceleratingAnimation()
        {
            if (!isPlayingFinalSpine)
            {
                if (cartSpineAnim != null)
                {
                    // 保持move动画，速度会在Update中逐渐降低
                    cartSpineAnim.timeScale = spineTimeScale_Move_Min;
                }
                // 轮子保持Move动画，通过TimeScale控制速度
                if (wheelSpineAnim != null)
                {
                    wheelSpineAnim.timeScale = wheelSpineTimeScale_Move_Min;
                }
            }

            // 播放轮子粒子效果
            PlayWheelParticles();
        }

        private void PlayBoxStop()
        {
            if (boxSpineAnim == null) return;
            // 开始播放 stop。stop 完成后，如果此时仍处于 Idle（未被新的移动打断），再回到 idle
            _boxExpectIdleAfterStop = true;
            var t = boxSpineAnim.AnimationState.SetAnimation(0, BOX_ANIM_STOP, false);
            t.MixDuration = MIX_TIME_DEFAULT;
            t.Complete += entry =>
            {
                if (_boxExpectIdleAfterStop && _state == CartState.Idle)
                {
                    var ti = boxSpineAnim.AnimationState.SetAnimation(0, BOX_ANIM_IDLE, true);
                    ti.MixDuration = MIX_TIME_TO_IDLE;
                }
                _boxExpectIdleAfterStop = false;
            };
        }

        private void ScheduleBoxStopDelayed()
        {
            if (boxSpineAnim == null) return;
            if (_boxStopDelayCo != null) { StopCoroutine(_boxStopDelayCo); _boxStopDelayCo = null; }
            float delay = Mathf.Max(0f, boxStopDelay);
            if (delay <= 0f)
            {
                PlayBoxStop();
                return;
            }
            _boxStopDelayCo = StartCoroutine(CoDelayPlayBoxStop(delay));
        }

        private IEnumerator CoDelayPlayBoxStop(float delay)
        {
            yield return new WaitForSeconds(delay);
            // 仅当仍处于减速或已进入 Idle 时，才触发 stop；若已重新加速/移动则跳过
            if (_state == CartState.Decelerating || _state == CartState.Idle)
            {
                PlayBoxStop();
            }
            _boxStopDelayCo = null;
        }

        /// <summary>
        /// 播放轮子粒子效果
        /// </summary>
        private void PlayWheelParticles()
        {
            if (isWheelParticlesPlaying) return; // 如果已经在播放，直接返回

            if (wheelParticle1 != null)
            {
                wheelParticle1.gameObject.SetActive(true);
                SetParticleLoop(wheelParticle1, true);
                wheelParticle1.Play();
            }
            if (wheelParticle2 != null)
            {
                wheelParticle2.gameObject.SetActive(true);
                SetParticleLoop(wheelParticle2, true);
                wheelParticle2.Play();
            }
            if (wheelParticle3 != null)
            {
                wheelParticle3.gameObject.SetActive(true);
                SetParticleLoop(wheelParticle3, true);
                wheelParticle3.Play();
            }
            isWheelParticlesPlaying = true;
        }

        /// <summary>
        /// 设置粒子系统的Loop属性
        /// </summary>
        private void SetParticleLoop(UIParticle particle, bool loop)
        {
            if (particle == null) return;

            // 获取所有子对象的ParticleSystem组件
            ParticleSystem[] particleSystems = particle.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.loop = loop;
            }
        }

        /// <summary>
        /// 停止轮子粒子效果
        /// </summary>
        private void StopWheelParticles()
        {
            if (!isWheelParticlesPlaying) return; // 如果已经停止，直接返回

            if (wheelParticle1 != null)
            {
                //所有子集的粒子Loop改成false
                SetParticleLoop(wheelParticle1, false);
            }
            if (wheelParticle2 != null)
            {
                //所有子集的粒子Loop改成false
                SetParticleLoop(wheelParticle2, false);
            }
            if (wheelParticle3 != null)
            {
                //所有子集的粒子Loop改成false
                SetParticleLoop(wheelParticle3, false);
            }
            isWheelParticlesPlaying = false;
        }
        #endregion

        #region Rewards
        [SerializeField] private MineCartBackground mainBackground;
        [SerializeField] private MineCartRewardBubble bubblePrefab;
        [SerializeField] private Animator bubbleAnimator;

        private class RewardInfo
        {
            public bool isFinal => reward.Valid;
            public float targetDistance;    // 奖励的目标距离点
            public bool created;            // 是否已创建
            public bool collected;          // 是否已收集
            public string icon;
            public Ref<List<RewardCommitData>> reward;
            public RewardInfo(float distance, string icon, Ref<List<RewardCommitData>> reward)
            {
                this.icon = icon;
                targetDistance = distance;
                created = false;
                collected = false;
                this.reward = reward;
            }
        }

        private List<RewardInfo> pendingRewards = new List<RewardInfo>();
        public System.Action OnRewardCollected;

        /// <summary>
        /// 检查奖励碰撞和创建时机
        /// </summary>
        private void CheckRewardCollision()
        {
            for (int i = pendingRewards.Count - 1; i >= 0; i--)
            {
                var reward = pendingRewards[i];

                if (!reward.created && totalDistance >= reward.targetDistance - moveDistance)
                {
                    CreateRewardBubble(reward);
                    reward.created = true;
                }

                if (!reward.collected && totalDistance >= reward.targetDistance)
                {
                    CollectReward(reward, i);
                }
            }
        }

        /// <summary>
        /// 更新所有奖励的位置
        /// </summary>
        private void UpdateRewardPositions()
        {
            RectTransform cartRect = cart.GetComponent<RectTransform>();
            if (bubblePrefab != null && bubblePrefab.gameObject.activeSelf)
            {
                // 找到当前显示的奖励
                var currentReward = pendingRewards.Find(r => !r.collected);
                if (currentReward != null)
                {
                    // 更新逻辑位置
                    bubblePrefab.UpdatePosition(cartRect, totalDistance);

                    // 根据奖励类型添加视觉偏移
                    Vector2 pos = bubblePrefab.RectTransform.anchoredPosition;
                    if (currentReward.isFinal)
                    {
                        pos.x += FINAL_BUBBLE_OFFSET;

                        // 更新finalItem的位置
                        if (finalItem != null && finalItem.activeSelf)
                        {
                            Vector2 anchorPos = pos;
                            anchorPos.y = finalItem.GetComponent<RectTransform>().anchoredPosition.y;
                            anchorPos.x += (FINAL_ITEM_OFFSET - FINAL_BUBBLE_OFFSET); // 相对Final气泡再偏移
                            finalItem.GetComponent<RectTransform>().anchoredPosition = anchorPos;
                        }
                    }
                    else
                    {
                        pos.x += NORMAL_BUBBLE_OFFSET;
                    }
                    bubblePrefab.RectTransform.anchoredPosition = pos;
                }
            }
        }

        /// <summary>
        /// 创建奖励气泡
        /// </summary>
        private void CreateRewardBubble(RewardInfo reward)
        {
            // 显示气泡
            bubblePrefab.gameObject.SetActive(true);

            // 初始化气泡
            bubblePrefab.Init(reward.targetDistance, reward.icon, reward.isFinal);

            // 播放Idle动画
            if (bubbleAnimator != null)
            {
                bubbleAnimator.Play("UIMineCartRewardBubbleIdle");
            }

            // 设置初始位置
            RectTransform cartRect = cart.GetComponent<RectTransform>();
            bubblePrefab.UpdatePosition(cartRect, totalDistance);

            // 根据奖励类型添加视觉偏移
            Vector2 pos = bubblePrefab.RectTransform.anchoredPosition;
            if (reward.isFinal)
            {
                pos.x += FINAL_BUBBLE_OFFSET;

                // 如果是Final奖励，显示finalItem并设置位置
                if (finalItem != null)
                {
                    finalItem.SetActive(true);
                    Vector2 anchorPos = pos;
                    anchorPos.y = finalItem.GetComponent<RectTransform>().anchoredPosition.y;
                    anchorPos.x += (FINAL_ITEM_OFFSET - FINAL_BUBBLE_OFFSET); // 相对Final气泡再偏移
                    finalItem.GetComponent<RectTransform>().anchoredPosition = anchorPos;
                }
            }
            else
            {
                pos.x += NORMAL_BUBBLE_OFFSET;
            }
            bubblePrefab.RectTransform.anchoredPosition = pos;
        }

        /// <summary>
        /// 收集奖励并触发回调
        /// </summary>
        private void CollectReward(RewardInfo reward, int index)
        {
            reward.collected = true;
            if (bubblePrefab != null && bubblePrefab.gameObject.activeSelf)
            {
                // 播放Punch动画
                if (bubbleAnimator != null)
                {
                    bubbleAnimator.Play("UIMineCartRewardBubblePunch");
                }
                Game.Manager.audioMan.TriggerSound(boxBreakSoundName);
                // 延迟隐藏泡泡，等待punch动画播放完成
                StartCoroutine(DelayedHideBubble(reward, index));
            }
            else
            {
                // 无泡泡动效时，直接解锁并完成收尾，避免锁未释放导致卡死
                ui.UnlockEvent();
                if (reward.isFinal)
                {
                    OnFinalRewardCollected?.Invoke();
                }
                if (reward.reward.Valid)
                {
                    reward.reward.Free();
                }
                OnRewardCollected?.Invoke();
                pendingRewards.RemoveAt(index);
            }
        }

        bool isPlayingFinalSpine = false;
        private IEnumerator DelayedMoveCart(string icon, Ref<List<RewardCommitData>> reward, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            Game.Manager.audioMan.TriggerSound(acceptSoundName);
            // 执行移动逻辑
            if (_state == CartState.Idle)
            {
                StartMove();
            }
            else if (_state == CartState.Accelerating || _state == CartState.Moving || _state == CartState.Decelerating)
            {
                HandleAdditionalMove();
            }
            //图标有值，代表要领奖
            if (!string.IsNullOrEmpty(icon))
            {
                pendingRewards.Add(new RewardInfo(targetDistance, icon, reward));
            }
        }

        private IEnumerator DelayedHideBubble(RewardInfo reward, int index)
        {
            // 等待punch动画播放完成（假设动画时长为0.5秒）
            yield return new WaitForSeconds(0.5f);

            // 隐藏泡泡
            bubblePrefab.gameObject.SetActive(false);

            // 继续原有的收集逻辑
            StartCoroutine(CollectRewardCoroutine(reward, index));
        }

        private IEnumerator CollectRewardCoroutine(RewardInfo reward, int index)
        {
            if (reward.isFinal)
            {
                // 通知开始领取最终大奖（用于触发UI动画）
                AnimateNextRewardGo(true);
                OnFinalRewardClaimStart?.Invoke();
                finalFireWorkAni.Play("UIMinecartBoardFirework");
                isPlayingFinalSpine = true;
                Game.Manager.audioMan.TriggerSound(finalRewardSoundName);
                cartSpineAnim.timeScale = 1f;
                var t = cartSpineAnim.AnimationState.SetAnimation(0, ANIM_FINISH, false);
                t.MixDuration = MIX_TIME_DEFAULT;
                // 停止轮子粒子效果
                StopWheelParticles();

                // 等待finish动画播放完成
                yield return new WaitForSeconds(3.6f);

                UIManager.Instance.OpenWindow(_activity.VisualMilestoneReward.res.ActiveR, reward.reward, reward.icon);
                //首次打开UI的时候是异步方法，加一点延迟
                yield return new WaitForSeconds(0.5f);
                while (UIManager.Instance.IsOpen(_activity.VisualMilestoneReward.res.ActiveR))
                {
                    yield return null;
                }

                ui.UnlockEvent();
                var track = cartSpineAnim.AnimationState.SetAnimation(0, ANIM_IDLE, true);
                track.MixDuration = MIX_TIME_TO_IDLE;
                // 轮子恢复Move动画
                if (wheelSpineAnim != null)
                {
                    wheelSpineAnim.timeScale = 0f;  // 静止时轮子停止
                }
                isPlayingFinalSpine = false;

                // 通知Final奖励收集完成
                OnFinalRewardCollected?.Invoke();
                // 大奖流程刚刚结束：刷新回合配置快照，并再刷新面板
                UpdateRoundSnapshotFromActivity();
                RefreshNextRewardUI();
                AnimateNextRewardGo(false);
            }
            else
            {
                ui.UnlockEvent();
            }

            OnRewardCollected?.Invoke();
            pendingRewards.RemoveAt(index);
        }
        #endregion

        #region Debug
#if UNITY_EDITOR
        private Text TextTip_Speed;
        private Text TextTip_Distance;
        private Text TextTip_RewardDistance;
        private Text TextTip_State;

        /// <summary>
        /// 更新调试信息显示
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (TextTip_Speed == null)
                TextTip_Speed = transform.Find("DebugRoot/TextTip_Speed")?.GetComponent<Text>();
            if (TextTip_Distance == null)
                TextTip_Distance = transform.Find("DebugRoot/TextTip_Distance")?.GetComponent<Text>();
            if (TextTip_RewardDistance == null)
                TextTip_RewardDistance = transform.Find("DebugRoot/TextTip_RewardDistance")?.GetComponent<Text>();
            if (TextTip_State == null)
                TextTip_State = transform.Find("DebugRoot/TextTip_State")?.GetComponent<Text>();

            if (TextTip_Speed != null)
                TextTip_Speed.text = $"当前速度:{(Mathf.Approximately(currentMoveSpeed, 0f) ? "N/A" : $"{(int)Mathf.Abs(currentMoveSpeed)}")}";

            if (TextTip_Distance != null)
            {
                float remainingDistance = targetDistance - totalDistance;
                TextTip_Distance.text = $"终点距离:{(Mathf.Approximately(remainingDistance, 0f) ? "N/A" : $"{(int)remainingDistance}")}";
            }

            if (TextTip_State != null)
                TextTip_State.text = $"状态:{_state}";

            if (TextTip_RewardDistance != null)
            {
                float nearestRewardDistance = float.MaxValue;
                foreach (var reward in pendingRewards)
                {
                    if (!reward.collected)
                    {
                        float distance = reward.targetDistance - totalDistance;
                        if (distance < nearestRewardDistance)
                        {
                            nearestRewardDistance = distance;
                        }
                    }
                }

                TextTip_RewardDistance.text = $"奖励距离:{(nearestRewardDistance == float.MaxValue ? "N/A" : $"{(int)nearestRewardDistance}")}";
            }
        }
#endif
        #endregion
        #region DialogBubble
        [Header("对话气泡")]
        [SerializeField] private MineCartDialogBubble dialogBubbleLeft;
        //[SerializeField] private MineCartDialogBubble dialogBubbleRight;//右边的小熊猫暂时禁言
        [SerializeField] private MineCartDialogBubble TargetItemDialog;
        [SerializeField] private float waitInterval = 15;
        [SerializeField] private float showDuration = 5;

        // 对话气泡状态枚举
        private enum DialogBubbleState
        {
            Idle,           // 空闲状态，等待显示
            ShowingDialog,  // 正在显示普通对话
            ShowingTarget   // 正在显示目标物品
        }

        private DialogBubbleState currentBubbleState = DialogBubbleState.Idle;
        private float idleTimer = 0;
        private float showTimer = 0;

        private void UpdateDialogBubble()
        {
            bool hasTargetItemNow = _activity.HasSpecialItem();

            // 根据当前状态和目标棋子状态，决定下一步动作
            switch (currentBubbleState)
            {
                case DialogBubbleState.Idle:
                    HandleIdleState(hasTargetItemNow);
                    break;

                case DialogBubbleState.ShowingDialog:
                    HandleShowingDialogState(hasTargetItemNow);
                    break;

                case DialogBubbleState.ShowingTarget:
                    HandleShowingTargetState(hasTargetItemNow);
                    break;
            }
        }

        /// <summary>
        /// 处理空闲状态
        /// </summary>
        private void HandleIdleState(bool hasTargetItemNow)
        {
            if (hasTargetItemNow)
            {
                // 有目标棋子，切换到目标显示状态
                SwitchToTargetState();
            }
            else
            {
                // 没有目标棋子，开始普通对话计时
                idleTimer += Time.deltaTime;
                if (idleTimer >= waitInterval)
                {
                    SwitchToDialogState();
                }
            }
        }

        /// <summary>
        /// 处理显示普通对话状态
        /// </summary>
        private void HandleShowingDialogState(bool hasTargetItemNow)
        {
            if (hasTargetItemNow)
            {
                // 有目标棋子出现，切换到目标显示状态
                SwitchToTargetState();
            }
            else
            {
                // 继续显示普通对话
                showTimer += Time.deltaTime;
                if (showTimer >= showDuration)
                {
                    Debug.Log($"对话气泡时间到，切换到空闲状态。showTimer: {showTimer}, showDuration: {showDuration}");
                    SwitchToIdleState();
                }
            }
        }

        /// <summary>
        /// 处理显示目标状态
        /// </summary>
        private void HandleShowingTargetState(bool hasTargetItemNow)
        {
            if (!hasTargetItemNow)
            {
                // 目标棋子消失，切换到空闲状态
                SwitchToIdleState();
            }
            // 如果还有目标棋子，继续显示目标气泡
        }

        /// <summary>
        /// 切换到空闲状态
        /// </summary>
        private void SwitchToIdleState()
        {
            Debug.Log($"切换到空闲状态，隐藏所有气泡");
            // 隐藏所有气泡
            dialogBubbleLeft.Hide();
            TargetItemDialog.Hide();

            // 重置状态和计时器
            currentBubbleState = DialogBubbleState.Idle;
            idleTimer = 0;
            showTimer = 0;
        }

        /// <summary>
        /// 切换到普通对话状态
        /// </summary>
        private void SwitchToDialogState()
        {
            // 隐藏目标气泡（如果有的话）
            TargetItemDialog.Hide();

            // 显示普通对话
            ShowRandomDialog();

            // 设置状态和计时器
            currentBubbleState = DialogBubbleState.ShowingDialog;
            showTimer = 0;
        }

        /// <summary>
        /// 切换到目标显示状态
        /// </summary>
        private void SwitchToTargetState()
        {
            // 隐藏普通对话
            dialogBubbleLeft.Hide();

            // 显示目标气泡
            TargetItemDialog.Show("");

            // 设置状态和重置计时器
            currentBubbleState = DialogBubbleState.ShowingTarget;
            idleTimer = 0;
            showTimer = 0;
        }

        /// <summary>
        /// 显示随机对话
        /// </summary>
        private void ShowRandomDialog()
        {
            var config = _activity.ConfD;
            if (config != null && config.ChatContent.Count > 0)
            {
                int randomIndex = Random.Range(0, config.ChatContent.Count);
                string key = config.ChatContent[randomIndex];
                dialogBubbleLeft.Show(key);
            }
        }





        public void SetNeedShowFinalItem(bool show)
        {
            if (finalItem == null) return;

            finalItem.SetActive(show);
            if (show)
            {
                // 获取小车的位置
                RectTransform finalItemRect = finalItem.GetComponent<RectTransform>();

                // 设置finalItem的初始位置：相对小车向右偏移FINAL_ITEM_OFFSET
                Vector2 pos = Vector2.zero;
                pos.x = FINAL_ITEM_OFFSET;
                pos.y = finalItemRect.anchoredPosition.y;
                finalItemRect.anchoredPosition = pos;
            }
        }

        void OnDisable()
        {
            // 重置所有状态
            SwitchToIdleState();
            if (finalItem != null)
            {
                finalItem.SetActive(false);
            }
            // 组件禁用时确保停止任何循环音效
            if (isMoveLoopSoundPlaying)
            {
                Game.Manager.audioMan.StopLoopSound();
                isMoveLoopSoundPlaying = false;
            }
        }

        #endregion

        #region 数字进度与下一个大奖UI
        private TextMeshProUGUI progressText;
        private UIImageRes nextRewardIcon;
        private Button nextRewardBtn;
        private TextMeshProUGUI nextRewardText;
        private GameObject nextRewardGo;
        private Animation nextRewardAnim;
        private Tween _nextRewardGoTween;
        private GameObject infoObj;
        private Vector2 _nextRewardGoOriginPos;
        private const float NextRewardGoMoveOffsetX = 300f;
        private const float NextRewardGoTweenDuration = 0.25f;

        // 显示进度（只记录开始与目标，按矿车运动百分比驱动）
        private float _dispCurrent;
        private float _dispStartScore;
        private float _dispTargetScore;
        private bool _dispActive;
        private Coroutine _applyTargetCo;
        private int _cachedNextIndex = -1;
        private Coroutine _nextRewardPanelAnimCo;
        // 回合配置快照（用于避免最终大奖阶段切换回合导致显示错乱）
        private List<int> _roundSnapshotMilestoneScore = new List<int>();
        private List<int> _roundSnapshotMilestoneReward = new List<int>();
        private string _roundSnapshotBigRewardIcon = null;

        public void BindProgressUI(TextMeshProUGUI progress, UIImageRes rewardIcon, Button rewardBtn, TextMeshProUGUI rewardText, GameObject rewardGo)
        {
            progressText = progress;
            nextRewardIcon = rewardIcon;
            nextRewardBtn = rewardBtn;
            nextRewardText = rewardText;
            nextRewardGo = rewardGo;
            infoObj = rewardGo.transform.Find("InfoImg").gameObject;
            nextRewardAnim = rewardGo.GetComponent<Animation>();
            if (nextRewardBtn != null)
            {
                nextRewardBtn.onClick.RemoveAllListeners();
                nextRewardBtn.transform.AddButton("", OpenNextRewardTips);
            }

            if (nextRewardGo != null)
            {
                var rect = nextRewardGo.GetComponent<RectTransform>();
                _nextRewardGoOriginPos = rect != null ? rect.anchoredPosition : Vector2.zero;
            }

            InitProgressAndNextRewardUI();
        }

        private void InitProgressAndNextRewardUI()
        {
            if (_activity == null) return;
            _dispActive = false;
            _dispCurrent = _baseSnapshotValue + _activity.MilestoneNum;
            UpdateRoundSnapshotFromActivity();
            ApplyProgressText();
            RefreshNextRewardUI();
        }

        public void SetDisplayTargetScore(int targetScore, float applyDelaySec = 0f)
        {
            if (_applyTargetCo != null)
            {
                StopCoroutine(_applyTargetCo);
                _applyTargetCo = null;
            }
            if (applyDelaySec > 0f)
            {
                _applyTargetCo = StartCoroutine(CoApplyTargetAfterDelay(targetScore, applyDelaySec));
            }
            else
            {
                ApplyDisplayTarget(targetScore);
            }
        }

        /// <summary>
        /// 用矿车运动百分比驱动数字进度：_dispCurrent = Lerp(start, target, p)。
        /// 仅在 _dispActive 时更新。
        /// </summary>
        private void TickDisplayProgressByMovement()
        {
            if (!_dispActive || progressText == null) return;
            float p = GetMovementProgress01();
            _dispCurrent = Mathf.Lerp(_dispStartScore, _dispTargetScore, p);
            ApplyProgressText();
        }

        private void ApplyDisplayTarget(int targetScore)
        {
            if (!_dispActive)
            {
                _dispStartScore = _dispCurrent;
            }
            _dispTargetScore = targetScore;
            _dispActive = true;
        }

        private IEnumerator CoApplyTargetAfterDelay(int targetScore, float delay)
        {
            yield return new WaitForSeconds(delay);
            _applyTargetCo = null;
            ApplyDisplayTarget(targetScore);
        }

        /// <summary>
        /// 应用数字进度文案：显示为“米” + 单位（#SysComDesc892），并顺带刷新剩余里程文案。
        /// </summary>
        private void ApplyProgressText()
        {
            if (progressText != null)
            {
                int factor = GetDistanceFactor();
                int meters = Mathf.Max(0, Mathf.FloorToInt(_dispCurrent * factor));
                var unit = I18N.Text("#SysComDesc892");
                progressText.text = meters.ToString() + unit;
            }
            UpdateNextRewardRemainText();
        }

        private int GetDistanceFactor()
        {
            var detail = _activity != null ? _activity.GetCurDetailConfig() : null;
            if (detail == null || detail.DistanceFactor <= 0)
            {
                return 1;
            }
            return detail.DistanceFactor;
        }

        /// <summary>
        /// 刷新“下一个奖励”UI：初始化阶段不播放动画，直接按当前 next 索引应用；随后更新剩余里程。
        /// </summary>
        private void RefreshNextRewardUI()
        {
            if (_activity == null) return;
            if (nextRewardIcon != null)
            {
                // 初始刷新直接应用当前 next index（不走动画）
                int idx = GetNextMilestoneIndex();
                _cachedNextIndex = -1; // 强制刷新
                ApplyNextRewardByIndex(idx);
                _cachedNextIndex = idx;
            }
            UpdateNextRewardRemainText();
        }

        /// <summary>
        /// 从活动读取“当期回合配置”并快照：MilestoneScore / MilestoneReward / BigRewardIcon。
        /// 在大奖结算或回合切换后需重新快照，保证显示一致性。
        /// </summary>
        private void UpdateRoundSnapshotFromActivity()
        {
            var roundConfig = _activity.GetCurRoundConfig();
            _roundSnapshotMilestoneScore.Clear();
            _roundSnapshotMilestoneReward.Clear();
            if (roundConfig != null)
            {
                if (roundConfig.MilestoneScore != null)
                {
                    _roundSnapshotMilestoneScore.AddRange(roundConfig.MilestoneScore);
                }
                if (roundConfig.MilestoneReward != null)
                {
                    _roundSnapshotMilestoneReward.AddRange(roundConfig.MilestoneReward);
                }
                _roundSnapshotBigRewardIcon = roundConfig.RewardIcon;
            }
            else
            {
                _roundSnapshotBigRewardIcon = null;
            }
        }

        /// <summary>
        /// 取得“下一个奖励”的总目标值（含 Base）。若越过所有小奖，返回大奖阈值。
        /// </summary>
        private int GetNextMilestoneTotalScore()
        {
            if (_roundSnapshotMilestoneScore == null || _roundSnapshotMilestoneScore.Count == 0)
            {
                return _baseSnapshotValue; // 无配置则目标等于当前Base
            }
            int currentScore = Mathf.FloorToInt(_dispCurrent);
            for (int i = 0; i < _roundSnapshotMilestoneScore.Count; i++)
            {
                int threshold = _roundSnapshotMilestoneScore[i] + _baseSnapshotValue;
                if (threshold > currentScore)
                {
                    return threshold;
                }
            }
            // 已越过所有节点，返回最后一个（大奖）
            return _baseSnapshotValue + _roundSnapshotMilestoneScore[_roundSnapshotMilestoneScore.Count - 1];
        }

        /// <summary>
        /// 取得“下一个奖励”的索引（小奖范围内返回具体索引，越界表示大奖）。
        /// </summary>
        private int GetNextMilestoneIndex()
        {
            if (_roundSnapshotMilestoneScore == null || _roundSnapshotMilestoneScore.Count == 0)
            {
                return -1;
            }
            int currentScore = Mathf.FloorToInt(_dispCurrent);
            for (int i = 0; i < _roundSnapshotMilestoneScore.Count; i++)
            {
                int threshold = _roundSnapshotMilestoneScore[i] + _baseSnapshotValue;
                if (threshold > currentScore)
                {
                    return i;
                }
            }
            return _roundSnapshotMilestoneScore.Count - 1;
        }


        /// <summary>
        /// 按索引应用奖励：小奖设置具体图标并禁用按钮；大奖设置回合大奖图标并启用按钮。
        /// </summary>
        private void ApplyNextRewardByIndex(int idx)
        {
            // 小奖
            if (idx >= 0 && idx < _roundSnapshotMilestoneReward.Count)
            {
                var rewardId = _roundSnapshotMilestoneReward[idx];
                var conf = Game.Manager.configMan.GetEventMineCartRewardConfig(rewardId);
                string icon = conf != null ? conf.Icon : _roundSnapshotBigRewardIcon;
                nextRewardIcon.SetImage(icon);
                infoObj.SetActive(false);
                if (nextRewardBtn != null)
                {
                    nextRewardBtn.interactable = false; // 小奖屏蔽点击
                }
            }
            else
            {
                // 大奖
                nextRewardIcon.SetImage(_roundSnapshotBigRewardIcon);
                infoObj.SetActive(true);
                if (nextRewardBtn != null)
                {
                    nextRewardBtn.interactable = true; // 大奖允许点击
                }
            }
        }

        /// <summary>
        /// 请求刷新“下一个奖励”面板：若索引变化，播放位移动画（移出→刷新→移入）。
        /// </summary>
        private void RequestNextRewardRefresh(int idx)
        {
            if (idx == _cachedNextIndex) return;
            // 如果没有面板对象，直接应用
            if (nextRewardGo == null)
            {
                ApplyNextRewardByIndex(idx);
                _cachedNextIndex = idx;
                UpdateNextRewardRemainText();
                return;
            }
            // 走位移动画：先移出 → 应用刷新 → 再移入
            if (_nextRewardPanelAnimCo != null) { StopCoroutine(_nextRewardPanelAnimCo); }
            _nextRewardPanelAnimCo = StartCoroutine(CoPlayNextPanelMoveRefreshInAndApply(idx));
        }

        /// <summary>
        /// 面板位移 + 刷新 + 回位 的协程实现，确保内容在移出后刷新，避免提前闪切。
        /// </summary>
        private IEnumerator CoPlayNextPanelMoveRefreshInAndApply(int idx)
        {
            // 移出
            AnimateNextRewardGo(true);
            yield return new WaitForSeconds(NextRewardGoTweenDuration);
            // 应用图标与交互，再刷新文本
            ApplyNextRewardByIndex(idx);
            _cachedNextIndex = idx;
            UpdateNextRewardRemainText();
            // 移入
            AnimateNextRewardGo(false);
            yield return new WaitForSeconds(NextRewardGoTweenDuration);
            _nextRewardPanelAnimCo = null;
        }

        // 剩余相对距离 = 目标总值(含Base) - 当前显示值
        /// <summary>
        /// 更新剩余里程文案：目标(含Base) - 当前显示值 → 转米后向上取整，避免最后一步提前显示 0。
        /// </summary>
        private void UpdateNextRewardRemainText()
        {
            if (nextRewardText == null || _activity == null) return;
            int factor = GetDistanceFactor();
            // 动态选择"下一个奖励"（小/大），但仍基于 Base 快照
            int targetScoreTotal = Mathf.Max(0, GetNextMilestoneTotalScore());
            float targetMetersF = Mathf.Max(0f, (float)targetScoreTotal * (float)factor);
            float currentMetersF = Mathf.Max(0f, _dispCurrent * (float)factor);
            // 使用向上取整避免最后一步提前显示为0
            int remainMeters = Mathf.Max(0, Mathf.CeilToInt(targetMetersF - currentMetersF));
            var unit = I18N.Text("#SysComDesc892");
            nextRewardText.text = remainMeters.ToString() + unit;
        }

        /// <summary>
        /// 打开奖励说明：仅当“下一个奖励”为大奖时弹出 Tips；小奖不弹窗。
        /// </summary>
        private void OpenNextRewardTips()
        {
            if (_activity == null || nextRewardBtn == null) return;
            int idx = GetNextMilestoneIndex();
            // 只有大奖弹窗：当 idx 超出 MilestoneReward 范围时，为大奖
            if (idx < 0 || idx >= _roundSnapshotMilestoneReward.Count)
            {
                // 大奖：沿用回合大奖的展示
                var roundConfig = _activity.GetCurRoundConfig();
                if (roundConfig == null || roundConfig.RoundReward == null || roundConfig.RoundReward.Count == 0) return;
                var list = System.Linq.Enumerable.ToList(roundConfig.RoundReward.Select(s => s.ConvertToRewardConfig()));
                UIManager.Instance.OpenWindow(UIConfig.UIMineCartRewardTips, nextRewardBtn.transform.position, 35f, list);
            }
            // 小奖点击不弹窗
        }

        /// <summary>
        /// 播放“下一个奖励”面板位移动画：moveOut=true 向右移出，false 回到原位；
        /// 回位完成若存在 Animation 组件，触发一次扫光动画。
        /// </summary>
        private void AnimateNextRewardGo(bool moveOut)
        {
            if (nextRewardGo == null) return;
            var rect = nextRewardGo.GetComponent<RectTransform>();
            if (rect == null) return;
            if (_nextRewardGoTween != null && _nextRewardGoTween.IsActive())
            {
                _nextRewardGoTween.Kill();
                _nextRewardGoTween = null;
            }
            Vector2 target = _nextRewardGoOriginPos;
            if (moveOut)
            {
                target = new Vector2(_nextRewardGoOriginPos.x + NextRewardGoMoveOffsetX, _nextRewardGoOriginPos.y);
            }
            _nextRewardGoTween = rect
                .DOAnchorPos(target, NextRewardGoTweenDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (!moveOut && nextRewardAnim != null)
                    {
                        nextRewardAnim.Play("UIRewardTipTabSweepPunch");
                    }
                });
        }
        #endregion

        #region 计算运动时间的复杂逻辑
        /// <summary>
        /// 计算当前运动结束的时间点，如果当前没有运动则计算假设开始运动需要的时间
        /// </summary>
        public float CalcMoveEndTime(float delay = 0f)
        {
            if (_state == CartState.Idle)
            {
                // 如果当前静止，模拟delay后从0开始加速
                return delay + CalcMoveTimeFromIdle(moveDistance, 0f);
            }
            else
            {
                // 如果正在运动，先计算当前运动的剩余时间
                float currentRemainingTime = CalcCurrentMovementRemainingTime();

                // 如果delay时间比当前运动剩余时间要长
                if (delay >= currentRemainingTime)
                {
                    // 当前运动会先完成，然后等待剩余的延迟时间
                    float remainingDelay = delay - currentRemainingTime;
                    // 最后执行新的完整运动（从0开始到0结束）
                    return currentRemainingTime + CalcMoveTimeFromIdle(moveDistance, remainingDelay);
                }
                else
                {
                    // delay时间比当前运动剩余时间短，需要计算delay后的状态
                    float currentRemainingDistance = targetDistance - totalDistance;
                    float currentSpeedAbsValue = Mathf.Abs(currentMoveSpeed);
                    float targetSpeed = isSecondSpeed ? secondSpeed : firstSpeed;

                    // 计算delay期间会移动的距离，考虑运动状态变化
                    float delayDistance = CalcDistanceDuringDelay(delay, currentSpeedAbsValue, targetSpeed, currentRemainingDistance);

                    // 如果delay期间就能完成当前运动
                    if (delayDistance >= currentRemainingDistance)
                    {
                        // 计算完成当前运动需要的时间
                        float timeToCompleteCurrent = CalcCurrentMovementRemainingTime();
                        // 剩余时间用于新的moveDistance
                        float remainingDelay = delay - timeToCompleteCurrent;
                        // 重新计算总时间：完成当前运动时间 + 剩余等待时间 + 新移动时间
                        return timeToCompleteCurrent + CalcMoveTimeFromIdle(moveDistance, remainingDelay);
                    }
                    else
                    {
                        // delay后还有剩余距离，计算delay后的状态
                        float remainingDistanceAfterDelay = currentRemainingDistance - delayDistance;
                        float speedAfterDelay = CalcSpeedAfterDelay(delay, currentSpeedAbsValue, targetSpeed);
                        CartState stateAfterDelay = CalcStateAfterDelay(delay, currentSpeedAbsValue, targetSpeed);

                        // 要走的距离是delay时间过后的剩余距离
                        float distanceToMove = remainingDistanceAfterDelay + moveDistance;

                        // 如果delay时间过后是匀速或加速状态，目标速度为速度2，否则是速度1
                        float newTargetSpeed = (stateAfterDelay == CartState.Moving || stateAfterDelay == CartState.Accelerating) ? secondSpeed : firstSpeed;

                        // 开始计算模拟运动
                        return delay + CalcMoveTimeFromState(stateAfterDelay, speedAfterDelay, newTargetSpeed, distanceToMove);
                    }
                }
            }
        }

        /// <summary>
        /// 计算从静止状态开始的移动时间（用于新的moveDistance）
        /// </summary>
        private float CalcMoveTimeFromIdle(float distance, float additionalDelay = 0f)
        {
            float targetSpeed = isSecondSpeed ? secondSpeed : firstSpeed;
            float totalTime = additionalDelay;

            // 1. 计算加速阶段（从0速度开始）
            float accelerationTime = targetSpeed / acceleration;
            float accelerationDistance = (0 + targetSpeed) * 0.5f * accelerationTime;

            if (accelerationDistance >= distance)
            {
                // 加速过程中就能完成，但还需要减速到0
                float accelTime = Mathf.Sqrt(2 * distance / acceleration);
                float finalSpeed = acceleration * accelTime;
                float decelTime = finalSpeed / deceleration;
                return additionalDelay + accelTime + decelTime;
            }

            totalTime += accelerationTime;
            distance -= accelerationDistance;

            // 2. 计算需要减速的距离
            float decelerationDistance = (targetSpeed * targetSpeed) / (2f * deceleration);

            // 3. 计算匀速阶段
            if (distance > decelerationDistance)
            {
                float constantSpeedDistance = distance - decelerationDistance;
                float constantSpeedTime = constantSpeedDistance / targetSpeed;
                totalTime += constantSpeedTime;
            }

            // 4. 计算减速阶段
            float decelerationTime = targetSpeed / deceleration;
            totalTime += decelerationTime;

            return totalTime;
        }

        /// <summary>
        /// 计算delay期间移动的距离，考虑运动状态变化
        /// </summary>
        private float CalcDistanceDuringDelay(float delay, float currentSpeedAbs, float targetSpeed, float remainingDistance)
        {
            float totalDistance = 0f;
            float remainingTime = delay;
            float currentSpeed = currentSpeedAbs;

            // 1. 如果当前速度小于目标速度，先加速
            if (currentSpeed < targetSpeed)
            {
                float accelerationTime = (targetSpeed - currentSpeed) / acceleration;
                float actualAccelerationTime = Mathf.Min(accelerationTime, remainingTime);

                float finalSpeed = currentSpeed + acceleration * actualAccelerationTime;
                float accelerationDistance = (currentSpeed + finalSpeed) * 0.5f * actualAccelerationTime;
                totalDistance += accelerationDistance;
                remainingTime -= actualAccelerationTime;
                currentSpeed += acceleration * actualAccelerationTime;

                if (remainingTime <= 0f) return totalDistance;
            }
            // 2. 如果当前速度大于目标速度，先减速
            else if (currentSpeed > targetSpeed)
            {
                float decelerationTime = (currentSpeed - targetSpeed) / deceleration;
                float actualDecelerationTime = Mathf.Min(decelerationTime, remainingTime);

                float finalSpeed = currentSpeed - deceleration * actualDecelerationTime;
                float decelerationDistance = (currentSpeed + finalSpeed) * 0.5f * actualDecelerationTime;
                totalDistance += decelerationDistance;
                remainingTime -= actualDecelerationTime;
                currentSpeed -= deceleration * actualDecelerationTime;

                if (remainingTime <= 0f) return totalDistance;
            }

            // 3. 匀速运动
            if (remainingTime > 0f)
            {
                float constantSpeedDistance = currentSpeed * remainingTime;
                totalDistance += constantSpeedDistance;
            }

            return totalDistance;
        }

        /// <summary>
        /// 计算delay后的速度
        /// </summary>
        private float CalcSpeedAfterDelay(float delay, float currentSpeedAbs, float targetSpeed)
        {
            float currentSpeed = currentSpeedAbs;
            float remainingTime = delay;

            // 1. 如果当前速度小于目标速度，先加速
            if (currentSpeed < targetSpeed)
            {
                float accelerationTime = (targetSpeed - currentSpeed) / acceleration;
                float actualAccelerationTime = Mathf.Min(accelerationTime, remainingTime);
                currentSpeed += acceleration * actualAccelerationTime;
                remainingTime -= actualAccelerationTime;

                if (remainingTime <= 0f) return currentSpeed;
            }
            // 2. 如果当前速度大于目标速度，先减速
            else if (currentSpeed > targetSpeed)
            {
                float decelerationTime = (currentSpeed - targetSpeed) / deceleration;
                float actualDecelerationTime = Mathf.Min(decelerationTime, remainingTime);
                currentSpeed -= deceleration * actualDecelerationTime;
                remainingTime -= actualDecelerationTime;

                if (remainingTime <= 0f) return currentSpeed;
            }

            // 3. 匀速运动期间速度不变
            return currentSpeed;
        }

        /// <summary>
        /// 计算当前运动剩余的时间
        /// </summary>
        /// <returns>当前运动剩余的时间，如果当前没有运动则返回0</returns>
        private float CalcCurrentMovementRemainingTime()
        {
            // 如果当前静止，没有剩余时间
            if (_state == CartState.Idle)
            {
                return 0f;
            }

            // 计算当前剩余距离
            float remainingDistance = targetDistance - totalDistance;
            if (remainingDistance <= 0.01f)
            {
                return 0f;
            }

            float currentSpeedAbs = Mathf.Abs(currentMoveSpeed);
            float targetSpeed = isSecondSpeed ? secondSpeed : firstSpeed;

            // 计算完成剩余距离所需的总时间
            return CalcTimeToCompleteDistance(currentSpeedAbs, targetSpeed, remainingDistance);
        }

        /// <summary>
        /// 计算完成指定距离所需的总时间
        /// </summary>
        /// <param name="currentSpeedAbs">当前速度</param>
        /// <param name="targetSpeed">目标速度</param>
        /// <param name="remainingDistance">剩余距离</param>
        /// <returns>完成剩余距离所需的总时间</returns>
        private float CalcTimeToCompleteDistance(float currentSpeedAbs, float targetSpeed, float remainingDistance)
        {
            // 根据当前状态计算剩余时间
            switch (_state)
            {
                case CartState.Accelerating:
                    return CalcMoveTimeFromAcceleratingState(currentSpeedAbs, targetSpeed, remainingDistance);
                case CartState.Moving:
                    return CalcMoveTimeFromMovingState(currentSpeedAbs, targetSpeed, remainingDistance);
                case CartState.Decelerating:
                    return CalcMoveTimeFromDeceleratingState(currentSpeedAbs, targetSpeed, remainingDistance);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 计算delay后的运动状态
        /// </summary>
        private CartState CalcStateAfterDelay(float delay, float currentSpeedAbs, float targetSpeed)
        {
            float remainingTime = delay;
            float currentSpeed = currentSpeedAbs;

            // 1. 如果当前速度小于目标速度，先加速
            if (currentSpeed < targetSpeed)
            {
                float accelerationTime = (targetSpeed - currentSpeed) / acceleration;
                float actualAccelerationTime = Mathf.Min(accelerationTime, remainingTime);
                currentSpeed += acceleration * actualAccelerationTime;
                remainingTime -= actualAccelerationTime;

                if (remainingTime <= 0f)
                {
                    return currentSpeed < targetSpeed ? CartState.Accelerating : CartState.Moving;
                }
            }
            // 2. 如果当前速度大于目标速度，先减速
            else if (currentSpeed > targetSpeed)
            {
                float decelerationTime = (currentSpeed - targetSpeed) / deceleration;
                float actualDecelerationTime = Mathf.Min(decelerationTime, remainingTime);
                currentSpeed -= deceleration * actualDecelerationTime;
                remainingTime -= actualDecelerationTime;

                if (remainingTime <= 0f)
                {
                    return currentSpeed > targetSpeed ? CartState.Decelerating : CartState.Moving;
                }
            }

            // 3. 匀速运动期间状态不变
            return CartState.Moving;
        }

        /// <summary>
        /// 从指定状态开始计算运动时间
        /// </summary>
        private float CalcMoveTimeFromState(CartState startState, float startSpeed, float targetSpeed, float distance)
        {
            switch (startState)
            {
                case CartState.Accelerating:
                    return CalcMoveTimeFromAcceleratingState(startSpeed, targetSpeed, distance);
                case CartState.Moving:
                    return CalcMoveTimeFromMovingState(startSpeed, targetSpeed, distance);
                case CartState.Decelerating:
                    return CalcMoveTimeFromDeceleratingState(startSpeed, targetSpeed, distance);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 从加速状态开始计算运动时间
        /// </summary>
        private float CalcMoveTimeFromAcceleratingState(float startSpeed, float targetSpeed, float distance)
        {
            // 如果是加速阶段，计算：继续加速到2阶段要走多远，加速到2阶段后，是否还有足够的距离减速到0
            float accelerationTime = (targetSpeed - startSpeed) / acceleration;
            float accelerationDistance = (startSpeed + targetSpeed) * 0.5f * accelerationTime;

            // 如果加速过程中就能完成
            if (accelerationDistance >= distance)
            {
                float a = 0.5f * acceleration;
                float b = startSpeed;
                float c = -distance;
                float accelTime = (-b + Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
                float finalSpeed = startSpeed + acceleration * accelTime;
                float decelTime = finalSpeed / deceleration;
                return accelTime + decelTime;
            }

            // 加速到目标速度后，还需要匀速和减速
            float remainingDistanceAfterAcceleration = distance - accelerationDistance;
            float decelerationDistance = (targetSpeed * targetSpeed) / (2f * deceleration);

            // 计算匀速阶段
            float constantSpeedTime = 0f;
            if (remainingDistanceAfterAcceleration > decelerationDistance)
            {
                float constantSpeedDistance = remainingDistanceAfterAcceleration - decelerationDistance;
                constantSpeedTime = constantSpeedDistance / targetSpeed;
            }

            // 减速时间
            float decelerationTime = targetSpeed / deceleration;

            return accelerationTime + constantSpeedTime + decelerationTime;
        }

        /// <summary>
        /// 从匀速状态开始计算运动时间
        /// </summary>
        private float CalcMoveTimeFromMovingState(float startSpeed, float targetSpeed, float distance)
        {
            // 如果是匀速阶段，计算各种条件下的时长
            float decelerationDistance = (startSpeed * startSpeed) / (2f * deceleration);

            // 计算匀速阶段
            float constantSpeedTime = 0f;
            if (distance > decelerationDistance)
            {
                float constantSpeedDistance = distance - decelerationDistance;
                constantSpeedTime = constantSpeedDistance / startSpeed;
            }

            // 减速时间
            float decelerationTime = startSpeed / deceleration;

            return constantSpeedTime + decelerationTime;
        }

        /// <summary>
        /// 从减速状态开始计算运动时间
        /// </summary>
        private float CalcMoveTimeFromDeceleratingState(float startSpeed, float targetSpeed, float distance)
        {
            // 如果是减速阶段，把速度加到目标速度1，然后计算后续运动时长
            // 先加速到目标速度
            float accelerationTime = (targetSpeed - startSpeed) / acceleration;
            float accelerationDistance = (startSpeed + targetSpeed) * 0.5f * accelerationTime;

            // 如果加速过程中就能完成
            if (accelerationDistance >= distance)
            {
                float a = 0.5f * acceleration;
                float b = startSpeed;
                float c = -distance;
                float accelTime = (-b + Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
                float finalSpeed = startSpeed + acceleration * accelTime;
                float decelTime = finalSpeed / deceleration;
                return accelTime + decelTime;
            }

            // 加速到目标速度后，还需要匀速和减速
            float remainingDistanceAfterAcceleration = distance - accelerationDistance;
            float decelerationDistance = (targetSpeed * targetSpeed) / (2f * deceleration);

            // 计算匀速阶段
            float constantSpeedTime = 0f;
            if (remainingDistanceAfterAcceleration > decelerationDistance)
            {
                float constantSpeedDistance = remainingDistanceAfterAcceleration - decelerationDistance;
                constantSpeedTime = constantSpeedDistance / targetSpeed;
            }

            // 减速时间
            float decelerationTime = targetSpeed / deceleration;

            return accelerationTime + constantSpeedTime + decelerationTime;
        }

        internal void PlayCollectTargetItem(FlyableItemSlice slice)
        {
            if (slice.FlyType != FlyType.MineCartUseItem) return;
            collectParticle.Play();
        }
        #endregion
    }
}
