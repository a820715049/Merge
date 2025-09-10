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
    public class UIScore_mic : UIBase, INavBack
    {
        public const int itemHeight = 172;
        public const int spacing = 8;
        public const int topAndBottome = 40;
        public const float ToNextTime = 0.99f * 0.5f;
        public const float ToThisTime = 0.8f * 0.5f;
        
        public const int MinNum = 6;
        
        #region UI组件

        [Header("基础UI组件")]
        [SerializeField] private Button playBtn;           // 开始游戏按钮
        [SerializeField] private Transform helpRoot;       // 帮助界面根节点
        [SerializeField] private Button helpBtn;           // 帮助按钮
        [SerializeField] private Button block;             // 遮罩按钮
        [SerializeField] private TextProOnACircle title;   // 标题文本
        [SerializeField] private TMP_Text cd;              // 倒计时文本
        [SerializeField] private TMP_Text tip;             // 提示文本#SysComDesc1711
        [SerializeField] private UIImageRes bg;            // 背景图片
        [SerializeField] private UIImageRes cdBg;          // 倒计时背景
        [SerializeField] private UIImageRes titleBg;       // 标题背景

        [Header("里程碑相关")]
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private GameObject cell;
        [SerializeField] private GameObject cellRoot;
        
        [SerializeField] private UICommonItem item;        // 大奖显示组件
        [SerializeField] private MBRewardProgress rewardProgress;  // 进度条组件
        [SerializeField] private MBRewardIcon rewardIcon;   // 奖励图标
        [SerializeField] private Animation progressItemAnim; // 进度条Item动画

        [Header("动画配置 - 进度条播放动画")]
        [Tooltip("进度条充满动画时长")]
        [SerializeField] private float progressBarFillDuration = 0.8f;

        #endregion

        #region 私有字段

        private ActivityScore activity;                    // 积分活动实例
        private bool isPopup;                              // 是否为弹脸模式
        private Action WhenCD;                             // 倒计时回调

        // Cell管理
        private List<ActivityScore.Node> nodes = new();
        private List<int> listPrime = new();
        private List<GameObject> cellList = new();

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
            
            // MBI18NText.SetPlainText(tip.gameObject, "#SysComDesc1711", activity.Conf.Token);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.SCORE_MIC_CELL, cell);
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
                var (milestoneIndex, showScore, milestoneScore) = activity.CalculateScoreDisplayData(activity.LastShowScore_UI);
                rewardProgress.Refresh(showScore, milestoneScore);
                curMilestoneIndex = milestoneIndex;
                //需要领奖的时候，锁定UI
                LockEvent();
            }
            else
            {
                //其余状态下，直接把UI状态刷新到和ActivityScore中的数据状态一致
                var (milestoneIndex, showScore, milestoneScore) = activity.CalculateScoreDisplayData(activity.TotalScore);
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
                UIManager.Instance.OpenWindow(UIConfig.UIScoreFinish_Mic);
            }

            // // 清理创建的Cell
            foreach (var cellObj in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.SCORE_MIC_CELL, cellObj);
            }
            cellList.Clear();
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
        private void RefreshList(int currentMilestoneIndex)
        {
            activity.FillMilestoneData();
            nodes.Clear();
            for (int i = activity.ListM.Count - 2; i >= 0; i--)
            {
                if (activity.ListM[i].showNum - 1 < currentMilestoneIndex - 1)
                    continue;
                nodes.Add(activity.ListM[i]);
            }
            listPrime.Clear();
            foreach (var rewardStep in activity.ConfDetail.RewardStepNum)
            {
                if (rewardStep > activity.GetMilestoneIndex() + MinNum || (rewardStep > activity.GetMilestoneIndex() && nodes.Count < MinNum))
                {
                    listPrime.Add(rewardStep);
                }
            }
            if (nodes.Count < MinNum)
            {
                var needNum = MinNum - nodes.Count;
                for (int i = 0; i < needNum; i++)
                {
                    nodes.Add(activity.ListM[activity.GetMilestoneIndex() - 1 - i]);
                }
            }

            float height = (itemHeight + spacing) * nodes.Count + topAndBottome;
            if (activity.ListM[0].showNum - 1 != currentMilestoneIndex)
            {
                height -= (itemHeight + spacing) * 2 + spacing;
            }
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, height);
            scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 0f);
            for (int i = 0; i < nodes.Count; i++)
            {
                var cellSand = GameObjectPoolManager.Instance.CreateObject(PoolItemType.SCORE_MIC_CELL, cellRoot.transform);
                cellSand.GetComponent<UIMicItem>().UpdateContent(nodes[i]);
                cellList.Add(cellSand);
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
            
            int offset = activity.ListM[0].showNum - 1 != startMilestoneIndex ? 1 : 0;
            for (int milestoneIndex = startMilestoneIndex; milestoneIndex <= endMilestoneIndex; milestoneIndex++)
            {
                var uiItem = cellList[^(1 + offset + milestoneIndex - startMilestoneIndex)].GetComponent<UIMicItem>();
                uiItem.UpdateContentByValue(milestoneIndex == startMilestoneIndex, milestoneIndex < startMilestoneIndex, milestoneIndex > startMilestoneIndex);
            }

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
                    progressItemAnim.Play("ProgressGroup_punch");
                    yield return new WaitForSeconds(0.367f);
                
                    // 播放领奖动画
                    // rewardIcon.gameObject.SetActive(false);
                    yield return StartCoroutine(PlayRewardAnimation(milestoneIndex));

                    // 动画完成后刷新图标
                    // rewardIcon.gameObject.SetActive(true);
                    RefreshRewardIcon(milestoneIndex + 1);
                    rewardProgress.Refresh(0, currentMilestoneScore);
                    if (activity.HasComplete()) yield break;
                }
            }

            // 第三步：轨道动画 - 逐个播放里程碑动画和领奖动画
            // 从起始里程碑开始，逐个播放每个里程碑的动画
            int num = endMilestoneIndex - startMilestoneIndex;
            scroll.content.DOSizeDelta(new Vector2(scroll.content.sizeDelta.x, scroll.content.sizeDelta.y - num * (itemHeight + spacing) * 2), (ToNextTime + ToThisTime) * num).SetEase(Ease.Linear).SetLink(gameObject);
            for (int milestoneIndex = startMilestoneIndex; milestoneIndex <= endMilestoneIndex; milestoneIndex++)
            {
                yield return StartCoroutine(PlayCellAnimation(milestoneIndex - startMilestoneIndex + offset, milestoneIndex == startMilestoneIndex, milestoneIndex == endMilestoneIndex));
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
        /// </summary>
        /// <param name="milestoneIndex">里程碑索引</param>
        private IEnumerator PlayRewardAnimation(int milestoneIndex)
        {
            var milestoneList = activity.ListM;
            var node = milestoneList[milestoneIndex];
            var reward = node.reward;
            var isPrimeReward = node.isPrime;
            var rewardData = activity.TryGetCommitReward(reward);
            if (rewardData != null)
            {
                UIFlyUtility.FlyReward(rewardData, rewardIcon.transform.position);
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
                    }
                }
            }
        }

        /// <summary>
        /// 播放Cell动画流程
        /// </summary>
        private IEnumerator PlayCellAnimation(int index, bool isStart, bool isEnd)
        {
            var uiItem = cellList[^(1 + index)].GetComponent<UIMicItem>();
            if (!isStart)
            {
                yield return uiItem.ProcessToThis();
            }

            if (!isEnd)
            {
                yield return uiItem.ProcessToNext();
            }
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