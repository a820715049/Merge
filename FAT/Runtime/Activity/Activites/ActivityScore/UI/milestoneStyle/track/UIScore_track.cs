/**
 * @Author: ShentuAnge
 * @Date: 2025/06/26 14:53:00
 * Description: 积分活动轨道样式界面
 * 功能：
 * 1. 显示积分活动的基本信息（倒计时、主题等）
 * 2. 播放进度条动画（支持连升多级）
 * 3. 播放轨道动画（Cell移动和领奖效果）
 * 4. 管理里程碑Cell的显示和状态
 */

using UnityEngine;
using EL;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

namespace FAT
{
    public class UIScore_track : UIBase, INavBack
    {
        #region UI组件

        [Header("基础UI组件")]
        [SerializeField] private Button playBtn;           // 开始游戏按钮
        [SerializeField] private Transform helpRoot;       // 帮助界面根节点
        [SerializeField] private Button helpBtn;           // 帮助按钮
        [SerializeField] private Button block;             // 遮罩按钮
        [SerializeField] private TMP_Text cd;              // 倒计时文本
        [Header("里程碑相关")]
        [SerializeField] private GameObject cell;          // Cell预制体
        [SerializeField] private GameObject cellRoot;      // Cell根节点
        [SerializeField] private UICommonItem item;        // 大奖显示组件
        [SerializeField] private MBRewardProgress rewardProgress;  // 进度条组件
        [SerializeField] private MBRewardIcon rewardIcon;   // 奖励图标
        [SerializeField] private Animation progressItemAnim; // 进度条Item动画

        [Header("动画配置 - Cell移动动画")]
        [Tooltip("Cell移动动画时长")]
        [SerializeField] private float cellMoveDuration = 0.5f;

        [Tooltip("Cell移动动画贝塞尔曲线")]
        [SerializeField] private AnimationCurve cellMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("动画配置 - 进度条播放动画")]
        [Tooltip("进度条充满动画时长")]
        [SerializeField] private float progressBarFillDuration = 0.8f;

        [SerializeField] private string m_progressBarFillAnimName = "ProgressGroup_punch";
        #endregion

        #region 私有字段

        private ActivityScore activity;                    // 积分活动实例
        private bool isPopup;                              // 是否为弹脸模式
        private Action WhenCD;                             // 倒计时回调

        // Cell管理
        private List<GameObject> cellList = new();         // Cell列表
        private Vector3[] m_movePos = new Vector3[8];      // Cell移动位置数组
        private Vector3[] m_moveScale = new Vector3[8];    // Cell缩放数组

        // 动画状态管理
        private Coroutine _progressAnimationCoroutine;     // 进度条动画协程
        private int _currentBigRewardIndex = -1;           // 当前展示的大奖索引
        private int _currentAnimationMilestoneIndex = -1;  // 当前正在播放动画的里程碑索引

        #endregion

        #region 生命周期和事件处理

        protected override void OnCreate()
        {
            transform.AddButton("Content/close", Close);
            helpBtn.onClick.AddListener(OnClickHelp);
            playBtn.onClick.AddListener(OnClickPlay);
            block.onClick.AddListener(OnClickBlock);

            // 创建7个UIScoreMilestoneItem_track类型的Cell
            for (int i = 0; i < 7; i++)
            {
                var cellObj = Instantiate(cell, cellRoot.transform);
                var cellComponent = cellObj.GetComponent<UIScoreMilestoneItem_track>();
                if (cellComponent != null)
                {
                    cellList.Add(cellObj);
                }
            }

            GameObjectPoolManager.Instance.PreparePool(PoolItemType.SCORE_TRACK_CELL, cell);
            for (int i = 0; i < 8; i++)
            {
                m_movePos[i] = cellRoot.transform.GetChild(i).localPosition;
                m_moveScale[i] = cellRoot.transform.GetChild(i).localScale;
            }
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
            RefreshBigReward(curMilestoneIndex);

            // 刷新rewardIcon显示
            RefreshRewardIcon(curMilestoneIndex);
        }

        protected override void OnPostOpen()
        {
            base.OnPostOpen();

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
                UIManager.Instance.OpenWindow(activity.EndRes.ActiveR);
            }

            // // 清理创建的Cell
            // foreach (var cellObj in cellList)
            // {
            //     if (cellObj != null)
            //     {
            //         Destroy(cellObj);
            //     }
            // }
            // cellList.Clear();
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

        #endregion

        #region 数据刷新和显示

        /// <summary>
        /// 刷新里程碑列表
        /// 根据当前里程碑索引正确显示里程碑状态
        /// </summary>
        /// <param name="currentMilestoneIndex">当前里程碑索引</param>
        private void RefreshList(int currentMilestoneIndex)
        {
            for (int i = 0; i < cellList.Count; i++)
            {
                cellList[i].SetActive(false);
                cellList[i].transform.localPosition = m_movePos[i];
                cellList[i].transform.localScale = m_moveScale[i];
                cellList[i].transform.SetAsFirstSibling();
            }

            // 获取里程碑数据
            var milestoneList = activity.ListM;
            if (milestoneList == null || milestoneList.Count == 0) return;
            // 显示里程碑Cell
            for (int i = 0; i < cellList.Count && i < milestoneList.Count; i++)
            {
                var cellObj = cellList[i];
                var cellComponent = cellObj.GetComponent<UIScoreMilestoneItem_track>();
                if (cellComponent != null)
                {

                    // 检查索引是否超界
                    int milestoneIndex = i + currentMilestoneIndex;
                    if (milestoneIndex < milestoneList.Count)
                    {
                        // 直接使用activity.ListM中的数据
                        cellComponent.UpdateContent(milestoneList[milestoneIndex], milestoneIndex == milestoneList.Count - 1);
                        cellObj.SetActive(true);
                    }
                }
            }
        }

        // 灵活刷新大奖，传入当前里程碑索引，从该索引之后查找下一个Prime奖励
        private void RefreshBigReward(int curMilestoneIndex, float delay = 0)
        {
            int showPrimeRewardNum = 0;
            for (int i = curMilestoneIndex; i < activity.ListM.Count; i++)
            {
                var node = activity.ListM[i];
                if (node.isPrime)
                {
                    showPrimeRewardNum = node.showNum;
                    break;
                }
            }

            // 如果前后两次大奖是同一个，就不刷新
            if (showPrimeRewardNum == _currentBigRewardIndex)
            {
                return;
            }

            if (showPrimeRewardNum > 0)
            {
                var node = activity.ListM[showPrimeRewardNum - 1];
                _currentBigRewardIndex = showPrimeRewardNum; // 更新当前展示的大奖索引
                if (delay > 0)
                {
                    StartCoroutine(CoRefreshItem(node, delay));
                }
                else
                {
                    item.Refresh(node.reward, 17);
                }
            }
            else
            {
                // 没有下一个大奖，可以隐藏或清空item
                _currentBigRewardIndex = -1; // 重置当前展示的大奖索引
                item.gameObject.SetActive(false);
            }
        }

        private IEnumerator CoRefreshItem(ActivityScore.Node node, float delay)
        {
            yield return new WaitForSeconds(delay);
            item.Refresh(node.reward, 17);
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

        #endregion

        #region 动画播放

        /// <summary>
        /// 播放进度条动画
        /// 动画流程：
        /// 1. 预计算起始和结束分数对应的里程碑信息
        /// 2. 进度条：逐个充满每个里程碑，每次充满后重置从头开始
        /// 3. 轨道动画：进度条完成后，轨道逐个播放里程碑动画和领奖动画
        /// </summary>
        /// <param name="startScore">起始分数</param>
        /// <param name="endScore">结束分数</param>
        private IEnumerator CoPlayProgressAnimation(int startScore, int endScore)
        {
            // 设置动画状态参数
            _currentAnimationMilestoneIndex = -1;

            // 第一步：预计算起始和结束分数对应的里程碑信息
            var (startMilestoneIndex, startShowScore, startMilestoneScore)
                = activity.CalculateScoreDisplayData(startScore);
            var (endMilestoneIndex, endShowScore, endMilestoneScore)
                = activity.CalculateScoreDisplayData(endScore);

            // 第二步：进度条逐个充满每个里程碑
            for (int milestoneIndex = startMilestoneIndex; milestoneIndex <= endMilestoneIndex; milestoneIndex++)
            {
                // 更新当前正在播放动画的里程碑索引
                _currentAnimationMilestoneIndex = milestoneIndex;

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
                    progressItemAnim.Play(m_progressBarFillAnimName);
                    yield return new WaitForSeconds(0.367f);

                    // 动画完成后刷新图标
                    RefreshRewardIcon(milestoneIndex + 1);
                    rewardProgress.Refresh(0, currentMilestoneScore);
                }
            }

            // 第三步：轨道动画 - 逐个播放里程碑动画和领奖动画
            // 从起始里程碑开始，逐个播放每个里程碑的动画
            for (int milestoneIndex = startMilestoneIndex; milestoneIndex <= endMilestoneIndex; milestoneIndex++)
            {
                // 更新当前正在播放动画的里程碑索引
                _currentAnimationMilestoneIndex = milestoneIndex;

                // 检查是否需要播放领奖动画
                // 只有完整跨越的里程碑才会触发领奖动画
                if (milestoneIndex < endMilestoneIndex || activity.HasComplete())
                {
                    // 播放领奖动画
                    yield return StartCoroutine(PlayRewardAnimation(milestoneIndex));
                }
            }

            // 动画结束，清理协程引用
            _progressAnimationCoroutine = null;
            UnlockEvent();
            PlayBtnShow();
        }

        /// <summary>
        /// 播放领奖动画
        /// 动画流程：
        /// 1. 获取里程碑的奖励配置和待commit的奖励数据
        /// 2. 播放奖励图标的缩放动画效果
        /// 3. 等待奖励动画完成
        /// 4. 播放奖励飞行动画（从奖励图标位置飞到目标位置）
        /// 5. 播放Cell动画流程（重新排列Cell位置）
        /// 6. 如果领取的奖励是大奖，在奖励隐藏后刷新到下一个大奖
        /// </summary>
        /// <param name="milestoneIndex">里程碑索引</param>
        private IEnumerator PlayRewardAnimation(int milestoneIndex)
        {
            var milestoneList = activity.ListM;
            if (milestoneIndex >= 0 && milestoneIndex < milestoneList.Count)
            {
                var node = milestoneList[milestoneIndex];
                var reward = node.reward;
                var isPrimeReward = node.isPrime;
                var rewardData = activity.TryGetCommitReward(reward);
                if (rewardData != null)
                {
                    if (cellList.Count > 0 && cellList[0] != null)
                    {
                        UIScoreMilestoneItem_track item = cellList[0].GetComponent<UIScoreMilestoneItem_track>();
                        if (item != null)
                        {
                            item.PlayHide();
                            yield return new WaitForSeconds(0.5f);

                            UIFlyUtility.FlyReward(rewardData, item.commonItem.transform.position);
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
                    if (isPrimeReward)
                    {
                        RefreshBigReward(milestoneIndex + 1);
                    }

                    yield return StartCoroutine(PlayCellAnimation());
                }
            }
        }

        /// <summary>
        /// 播放Cell动画流程
        /// 动画流程：
        /// 1. 隐藏第一个位置的Cell（已领奖的Cell），并将其移动到数组最后
        /// 2. 等待最后一个Cell消失，确保视觉连续性
        /// 3. 所有Cell同步位移：前6个Cell向前移动一位，最后一个Cell从最后位置移动到第一个位置
        /// 4. 等待所有Cell移动完成
        /// </summary>
        private IEnumerator PlayCellAnimation()
        {
            // 第一步：隐藏第一个位置的Cell，并将其移动到数组最后
            // 第一个Cell是刚刚领奖的Cell，需要隐藏并移动到数组末尾
            if (cellList.Count > 0)
            {
                var firstCell = cellList[0];
                // 将第一个Cell移动到数组最后
                // 这样在后续的移动中，它会从最后位置移动到第一个位置
                cellList.RemoveAt(0);
                cellList.Add(firstCell);
                firstCell.transform.localPosition = m_movePos[^1]; // 设置到最后一个位置
                firstCell.transform.localScale = m_moveScale[^1];  // 设置对应的缩放
                firstCell.transform.SetAsFirstSibling();
                //这里要刷新被挪到最后的那个cell的显示状态
                var lastCellComponent = firstCell.GetComponent<UIScoreMilestoneItem_track>();
                if (lastCellComponent != null)
                {
                    // 使用维护的动画状态参数
                    // 被移动到最后的Cell应该显示当前里程碑后面的第7个奖励
                    int targetMilestoneIndex = _currentAnimationMilestoneIndex + 7;

                    // 确保索引在有效范围内
                    if (targetMilestoneIndex < activity.ListM.Count)
                    {
                        var node = activity.ListM[targetMilestoneIndex];
                        lastCellComponent.UpdateContent(node, targetMilestoneIndex == activity.ListM.Count - 1);
                        lastCellComponent.gameObject.SetActive(true);
                    }
                    else
                    {
                        // 如果后面已经没有奖励了（超界），就隐藏这个Cell
                        firstCell.SetActive(false);
                    }
                }
            }

            // 第三步：所有Cell同步位移（包括最后一个Cell从最后位置移动到第一个位置）
            // 使用DoTween直接执行动画，无需协程管理
            for (int i = 0; i < cellList.Count; i++)
            {
                var cellObj = cellList[i];
                if (cellObj != null)
                {
                    // 移动到对应的位置和缩放
                    // i=0对应第一个位置，i=1对应第二个位置，以此类推
                    var targetPos = m_movePos[i];
                    var targetScale = m_moveScale[i];

                    // 使用DoTween直接执行动画
                    MoveCellToPosition(cellObj, targetPos, targetScale, cellMoveDuration);
                }
            }

            // 等待所有Cell移动完成
            // DoTween动画会自动完成，这里等待动画时长
            yield return new WaitForSeconds(cellMoveDuration);
        }

        /// <summary>
        /// 移动Cell到指定位置和缩放
        /// </summary>
        private void MoveCellToPosition(GameObject cellObj, Vector3 targetPos, Vector3 targetScale, float duration)
        {
            // 使用DoTween同时移动位置和缩放
            cellObj.transform.DOLocalMove(targetPos, duration)
                .SetEase(cellMoveCurve);
            cellObj.transform.DOScale(targetScale, duration)
                .SetEase(cellMoveCurve);
        }

        public void OnNavBack()
        {
            // 如果动画正在播放，则不允许返回
            if (_progressAnimationCoroutine != null)
            {
                return;
            }

            // 动画未播放，关闭界面
            Close();
        }

        #endregion
    }
}
