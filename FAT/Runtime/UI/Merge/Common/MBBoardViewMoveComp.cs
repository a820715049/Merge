using System.Collections;
using System.Collections.Generic;
using Cysharp.Text;
using DG.Tweening;
using EL;
using FAT.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{


    /// <summary>
    /// 棋盘移动组件
    /// 支持上下左右四个方向的棋盘移动动画，包括临时图标的创建、移动和飞行动画
    /// </summary>
    [RequireComponent(typeof(MBBoardView))]
    public class MBBoardViewMoveComp : MonoBehaviour
    {
        /// <summary>
        /// 移动方向枚举
        /// </summary>
        public enum MoveDirection
        {
            Up,     // 向上移动
            Down,   // 向下移动
            Left,   // 向左移动
            Right   // 向右移动
        }

        [Header("移动配置")]
        [SerializeField] private AnimationCurve moveUp = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private float moveTime = 0.8f;
        [SerializeField] private float itemFlyDelayRatio = 0.2f; // 棋子飞行动画延迟比例
        [SerializeField] private MoveDirection moveDirection = MoveDirection.Up; // 移动方向
        [Header("振动效果配置")]
        [SerializeField] private bool enableShakeEffect = true;            // 是否启用振动效果
        [SerializeField] private float shakeAmplitude = 5f;               // 振动幅度
        [SerializeField] private float shakeFrequency = 10f;              // 振动频率
        [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // 振动曲线

        [SerializeField] private RectMask2D mask;
        [SerializeField] private UITilling tilling;
        [SerializeField] private RectTransform scroll;
        [SerializeField] private Transform tempRoot;
        [SerializeField] private RectTransform shakeMask;  // 震动效果的Mask层
        [SerializeField] private string moveSoundOne = "MineCartBoardupOne";
        [SerializeField] private string moveSoundTwo = "MineCartBoardupTwo";
        [SerializeField] private string moveSoundThree = "MineCartBoardupThree";
        [SerializeField] private string moveSoundFour = "MineCartBoardupFour";

        private GameObject tempIconPrefab;

        private MBBoardView _boardView;
        private List<List<MBBoardViewMoveTempIcon>> _tempIconsByRow = new List<List<MBBoardViewMoveTempIcon>>(); // 按行组织的临时图标
        private bool _isPlayingMove = false;

        public UIBase uiBase { get; set; }
        private int _accumulatedMoveCount = 0;
        private List<List<Item>> _accumulatedItemRows = new List<List<Item>>();
        private Coroutine _moveCoroutine;
        private bool _isShaking = false;               // 是否正在振动
        private Vector3 _originalScrollPosition;       // scroll原始位置
        private float _shakeStartTime;                 // 振动开始时间

        public System.Action OnLastRowFlyComplete;
        public System.Action OnMoveEnd;
        public System.Action OnMoveStart;
        public System.Action OnRowMoveStart;  // 每行开始移动时的回调

        public bool IsPlayingMove => _isPlayingMove;

        void Update()
        {
#if UNITY_EDITOR
            // Debug: 按S键播放震动动画
            if (Input.GetKeyDown(KeyCode.S))
            {
                PlayDebugShakeEffect(2f);
            }
#endif
        }

        /// <summary>
        /// 初始化组件
        /// 获取MBBoardView组件并创建临时图标预制体
        /// </summary>
        private void Awake()
        {
            _boardView = GetComponent<MBBoardView>();

            CreateTempIconPrefab();
        }

        /// <summary>
        /// 累积收集的棋子列表
        /// 将一行棋子添加到累积列表中，为后续的移动动画做准备
        /// </summary>
        /// <param name="collectItemList">要收集的棋子列表</param>
        public void AccumulateCollectIcons(List<Item> collectItemList)
        {
            if (collectItemList == null) return;

            _accumulatedItemRows.Add(new List<Item>(collectItemList));
            _accumulatedMoveCount = _accumulatedItemRows.Count;

            Debug.Log($"Accumulated row {_accumulatedMoveCount} with {collectItemList.Count} items, total rows: {_accumulatedMoveCount}");
        }

        /// <summary>
        /// 执行移动动画
        /// 开始播放累积的棋子移动动画
        /// </summary>
        public void Execute()
        {
            if (_accumulatedMoveCount <= 0)
            {
                Debug.LogWarning("No accumulated items to move");
                return;
            }

            ExecuteMoveUp(_accumulatedMoveCount);
            // 移除这里的清空操作，改为在EndMoveUpProcess中清空
        }

        /// <summary>
        /// 手动结束移动过程
        /// 强制停止当前移动动画并清理状态
        /// </summary>
        public void ManualEndMoveUpProcess()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            EndMoveUpProcess();
        }

        /// <summary>
        /// 重置到初始状态
        /// 清理所有临时图标、重置位置、恢复背景设置
        /// </summary>
        public void ResetToInitialState()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _isPlayingMove = false;
            _accumulatedMoveCount = 0;
            _accumulatedItemRows.Clear();

            // 清理所有临时图标
            foreach (var row in _tempIconsByRow)
            {
                foreach (var tempIcon in row)
                {
                    if (tempIcon != null && tempIcon.gameObject != null)
                    {
                        Destroy(tempIcon.gameObject);
                    }
                }
            }
            _tempIconsByRow.Clear();

            // 重置位置
            if (scroll != null)
            {
                scroll.localPosition = Vector3.zero;
            }

            if (tempRoot != null)
            {
                tempRoot.localPosition = Vector3.zero;
                tempRoot.localScale = Game.Manager.mainMergeMan.mainBoardScale * Vector3.one;
            }

            // 重置背景设置
            if (_boardView != null && _boardView.boardBg != null)
            {
                var bgRect = _boardView.boardBg.transform as RectTransform;
                if (bgRect != null)
                {
                    bgRect.offsetMin = Vector2.zero;
                    bgRect.offsetMax = Vector2.zero;
                }

                if (tilling != null)
                {
                    var boardSize = Game.Manager.mergeBoardMan.activeWorld.activeBoard.size;
                    tilling.SetTilling(new Vector2(
                        boardSize.x * 0.5f,
                        boardSize.y * 0.5f
                    ));
                }
            }

            if (mask != null)
            {
                mask.enabled = false;
            }

            if (_boardView != null && _boardView.boardHolder != null)
            {
                _boardView.boardHolder.ReFillItem();
            }

            uiBase?.UnlockEvent();
        }

        private void StartMoveUpProcess()
        {
            if (_isPlayingMove) return;

            _isPlayingMove = true;
            uiBase?.LockEvent();
            BoardViewManager.Instance.OnUserActive();

            OnMoveStart?.Invoke();
        }

        private void EndMoveUpProcess()
        {
            // 停止振动
            _isShaking = false;

            // 根据方向重置位置
            scroll.localPosition = Vector3.zero;

            // tempRoot也重置到初始位置
            if (tempRoot != null)
            {
                tempRoot.localPosition = Vector3.zero;
            }

            if (_boardView.boardBg != null)
            {
                var bgRect = _boardView.boardBg.transform as RectTransform;
                if (bgRect != null)
                {
                    bgRect.offsetMin = Vector2.zero;
                    bgRect.offsetMax = Vector2.zero;
                }

                if (tilling != null)
                {
                    var boardSize = Game.Manager.mergeBoardMan.activeWorld.activeBoard.size;
                    tilling.SetTilling(new Vector2(
                        boardSize.x * 0.5f,
                        boardSize.y * 0.5f
                    ));
                }
            }

            if (mask != null)
                mask.enabled = false;

            // 清理临时图标
            foreach (var row in _tempIconsByRow)
            {
                foreach (var tempIcon in row)
                {
                    if (tempIcon != null && tempIcon.gameObject != null)
                    {
                        Destroy(tempIcon.gameObject);
                    }
                }
            }
            _tempIconsByRow.Clear();

            // 清理数据
            _accumulatedMoveCount = 0;
            _accumulatedItemRows.Clear();

            _boardView.boardHolder.ReFillItem();
            _isPlayingMove = false;
            _moveCoroutine = null;

            uiBase?.UnlockEvent();
            OnMoveEnd?.Invoke();
        }

        private void CreateTempIconPrefab()
        {
            if (tempIconPrefab == null)
            {
                var tempIconObj = new GameObject("TempIcon");
                var rectTransform = tempIconObj.AddComponent<RectTransform>();
                tempIconObj.AddComponent<Image>();
                tempIconObj.AddComponent<UIImageRes>();
                tempIconObj.AddComponent<MBBoardViewMoveTempIcon>();

                rectTransform.sizeDelta = _boardView.cellSize * Vector2.one;

                tempIconPrefab = tempIconObj;
                tempIconPrefab.SetActive(false);
            }
        }

        private void InitializeMoveState(int moveCount)
        {
            if (mask != null)
                mask.enabled = true;

            _tempIconsByRow.Clear();
            var moveDis = moveCount * _boardView.cellSize;

            // 根据方向设置tempRoot的初始位置
            if (tempRoot != null)
            {
                Vector3 offset = GetInitialOffset(moveDis);
                tempRoot.localPosition += offset;
            }
            for (int i = 0; i < _accumulatedItemRows.Count; i++)
            {
                var rowTempIcons = new List<MBBoardViewMoveTempIcon>();
                for (int j = 0; j < _accumulatedItemRows[i].Count; j++)
                {
                    var tempIcon = Instantiate(tempIconPrefab, tempRoot).GetComponent<MBBoardViewMoveTempIcon>();
                    tempIcon.SetImage(_accumulatedItemRows[i][j]);
                    tempIcon.gameObject.SetActive(true);

                    // 使用正确的本地坐标计算
                    var rectTransform = tempIcon.transform as RectTransform;
                    if (rectTransform != null)
                    {
                        // 计算在moveRoot中的位置
                        Vector2 moveRootPos = BoardUtility.CalcItemLocalPosInMoveRootByCoord(_accumulatedItemRows[i][j].coord);

                        // 如果tempRoot不是moveRoot，需要转换坐标
                        if (tempRoot != BoardViewManager.Instance.moveRoot)
                        {
                            // 将moveRoot的坐标转换为tempRoot的坐标
                            Vector3 worldPos = BoardViewManager.Instance.moveRoot.TransformPoint(moveRootPos);
                            Vector2 tempRootPos = tempRoot.InverseTransformPoint(worldPos);
                            rectTransform.anchoredPosition = tempRootPos;
                        }
                        else
                        {
                            rectTransform.anchoredPosition = moveRootPos;
                        }
                    }

                    // 创建临时图标后立即回收原始的MBItemView
                    BoardViewManager.Instance.boardView.boardHolder.ReleaseItem(_accumulatedItemRows[i][j].id);

                    // 设置收集触发距离：根据行数设置不同的触发距离
                    float singleRowDistance = _boardView.cellSize;
                    // 第一行触发距离较短，后续行递增，避免同时触发
                    float triggerDistance = (i + itemFlyDelayRatio) * singleRowDistance;
                    Vector3 triggerDirection = GetTriggerDistanceVector(1f).normalized; // 获取方向向量

                    tempIcon.SetCollectTriggerDistance(triggerDistance, triggerDirection);
                    rowTempIcons.Add(tempIcon);
                }
                _tempIconsByRow.Add(rowTempIcons);
            }


            _boardView.boardHolder.ReFillItem();

            // 根据方向设置scroll的初始位置
            scroll.localPosition += GetInitialOffset(moveDis);



            // 仿照农场棋盘：设置背景和tilling
            var boardSize = Game.Manager.mergeBoardMan.activeWorld.activeBoard.size;
            if (tilling != null)
            {
                tilling.SetTilling(new Vector2(
                    boardSize.x * 0.5f,
                    (boardSize.y + moveCount) * 0.5f
                ));
            }

            if (_boardView.boardBg != null)
            {
                var bgRect = _boardView.boardBg.transform as RectTransform;
                if (bgRect != null)
                {
                    bgRect.offsetMax = new Vector2(0, _boardView.cellSize * moveCount);
                }
            }
        }

        public void ExecuteMoveUp(int upRowCount)
        {
            StartMoveUpProcess();
            switch (upRowCount)
            {
                case 1:
                    Game.Manager.audioMan.TriggerSound(moveSoundOne);
                    break;
                case 2:
                    Game.Manager.audioMan.TriggerSound(moveSoundTwo);
                    break;
                case 3:
                    Game.Manager.audioMan.TriggerSound(moveSoundThree);
                    break;
                default:
                    Game.Manager.audioMan.TriggerSound(moveSoundFour);
                    break;
            }
            InitializeMoveState(upRowCount);
            _moveCoroutine = StartCoroutine(CoStartMove());
        }

        private IEnumerator CoStartMove()
        {
            if (mask != null)
                mask.enabled = true;

            // 启动棋盘移动协程
            StartCoroutine(CoBoardMove());

            // 等待所有动画完成
            float totalTime = moveTime * _accumulatedItemRows.Count;
            yield return new WaitForSeconds(totalTime);

            EndMoveUpProcess();
        }

        /// <summary>
        /// 棋盘移动协程
        /// </summary>
        private IEnumerator CoBoardMove()
        {
            var singleRowDis = _boardView.cellSize;
            var totalMoveDis = _accumulatedItemRows.Count * singleRowDis;
            var totalTime = moveTime * _accumulatedItemRows.Count;
            var moveVector = GetMoveVector(totalMoveDis);

            // 记录原始位置用于振动
            _originalScrollPosition = scroll.localPosition;

            // 根据方向使用不同的DOTween方法
            Tween scrollTween;
            switch (moveDirection)
            {
                case MoveDirection.Up:
                case MoveDirection.Down:
                    scrollTween = scroll.DOLocalMoveY(scroll.localPosition.y + moveVector.y, totalTime)
                        .SetEase(moveUp);
                    break;
                case MoveDirection.Left:
                case MoveDirection.Right:
                    scrollTween = scroll.DOLocalMoveX(scroll.localPosition.x + moveVector.x, totalTime)
                        .SetEase(moveUp);
                    break;
                default:
                    scrollTween = scroll.DOLocalMoveY(scroll.localPosition.y + moveVector.y, totalTime)
                        .SetEase(moveUp);
                    break;
            }

            // tempRoot也一起移动
            if (tempRoot != null)
            {
                Tween tempRootTween;
                switch (moveDirection)
                {
                    case MoveDirection.Up:
                    case MoveDirection.Down:
                        tempRootTween = tempRoot.DOLocalMoveY(tempRoot.localPosition.y + moveVector.y, totalTime)
                            .SetEase(moveUp);
                        break;
                    case MoveDirection.Left:
                    case MoveDirection.Right:
                        tempRootTween = tempRoot.DOLocalMoveX(tempRoot.localPosition.x + moveVector.x, totalTime)
                            .SetEase(moveUp);
                        break;
                    default:
                        tempRootTween = tempRoot.DOLocalMoveY(tempRoot.localPosition.y + moveVector.y, totalTime)
                            .SetEase(moveUp);
                        break;
                }
            }

            // 启动每行移动效果协程
            StartCoroutine(CoRowMoveEffects(totalTime));

            yield return scrollTween.WaitForCompletion();

            // 停止振动
            _isShaking = false;
        }

        /// <summary>
        /// 根据移动方向获取初始偏移
        /// </summary>
        private Vector3 GetInitialOffset(float moveDis)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                    return new Vector3(0, -moveDis, 0); // 向下偏移，准备向上移动
                case MoveDirection.Down:
                    return new Vector3(0, moveDis, 0);  // 向上偏移，准备向下移动
                case MoveDirection.Left:
                    return new Vector3(moveDis, 0, 0);  // 向右偏移，准备向左移动
                case MoveDirection.Right:
                    return new Vector3(-moveDis, 0, 0); // 向左偏移，准备向右移动
                default:
                    return new Vector3(0, -moveDis, 0);
            }
        }

        /// <summary>
        /// 根据移动方向获取移动向量
        /// </summary>
        private Vector3 GetMoveVector(float moveDis)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                    return new Vector3(0, moveDis, 0);   // 向上移动
                case MoveDirection.Down:
                    return new Vector3(0, -moveDis, 0);  // 向下移动
                case MoveDirection.Left:
                    return new Vector3(-moveDis, 0, 0);  // 向左移动
                case MoveDirection.Right:
                    return new Vector3(moveDis, 0, 0);   // 向右移动
                default:
                    return new Vector3(0, moveDis, 0);
            }
        }

        /// <summary>
        /// 根据移动方向获取触发距离向量
        /// </summary>
        private Vector3 GetTriggerDistanceVector(float distance)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                    return new Vector3(0, distance, 0);   // Y轴正方向
                case MoveDirection.Down:
                    return new Vector3(0, -distance, 0);  // Y轴负方向
                case MoveDirection.Left:
                    return new Vector3(-distance, 0, 0);  // X轴负方向
                case MoveDirection.Right:
                    return new Vector3(distance, 0, 0);   // X轴正方向
                default:
                    return new Vector3(0, distance, 0);
            }
        }

        /// <summary>
        /// 单行振动效果
        /// </summary>
        /// <param name="duration">振动持续时间（一行的时间）</param>
        private void PlaySingleRowShake()
        {
            if (shakeMask == null) return;

            // 使用DoTween实现振动，只影响Mask层
            Vector3 shakeVector = Vector3.zero;
            switch (moveDirection)
            {
                case MoveDirection.Up:
                case MoveDirection.Down:
                    // 垂直移动时，应用水平振动
                    shakeVector = new Vector3(shakeAmplitude, 0, 0);
                    break;
                case MoveDirection.Left:
                case MoveDirection.Right:
                    // 水平移动时，应用垂直振动
                    shakeVector = new Vector3(0, shakeAmplitude, 0);
                    break;
            }

            // 直接播放震动动画，不需要协程
            shakeMask.DOShakePosition(moveTime, shakeVector, (int)(shakeFrequency * moveTime), 90, false, true)
                .SetEase(shakeCurve);
        }

        /// <summary>
        /// 每行移动效果协程
        /// </summary>
        /// <param name="totalDuration">总移动时间</param>
        private IEnumerator CoRowMoveEffects(float totalDuration)
        {
            if (_accumulatedItemRows.Count <= 0) yield break;

            float singleRowTime = totalDuration / _accumulatedItemRows.Count;

            for (int i = 0; i < _accumulatedItemRows.Count; i++)
            {
                // 触发每行移动开始回调
                OnRowMoveStart?.Invoke();

                // 触发每行震动
                if (enableShakeEffect)
                {
                    PlaySingleRowShake();
                }

                // 等待一行的时间
                yield return new WaitForSeconds(singleRowTime);
            }
        }

        /// <summary>
        /// Debug震动效果
        /// </summary>
        /// <param name="duration">震动持续时间</param>
        private void PlayDebugShakeEffect(float duration)
        {
            if (!enableShakeEffect || shakeMask == null) return;

            Debug.Log($"开始Debug震动动画，持续时间: {duration}秒");

            // 使用DoTween实现振动
            Vector3 shakeVector = Vector3.zero;
            switch (moveDirection)
            {
                case MoveDirection.Up:
                case MoveDirection.Down:
                    // 垂直移动时，应用水平振动
                    shakeVector = new Vector3(shakeAmplitude, 0, 0);
                    break;
                case MoveDirection.Left:
                case MoveDirection.Right:
                    // 水平移动时，应用垂直振动
                    shakeVector = new Vector3(0, shakeAmplitude, 0);
                    break;
            }

            // 直接播放震动动画
            shakeMask.DOShakePosition(duration, shakeVector, (int)(shakeFrequency * duration), 90, false, true)
                .SetEase(shakeCurve)
                .OnComplete(() => Debug.Log("Debug震动动画完成"));
        }


    }
}
