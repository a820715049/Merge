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
        
        public const int MinNum = 7;

        #region 数据
        
        private readonly List<Node> ListM = new();
        private bool shouldPopup;
        private PoolMapping.Ref<List<RewardCommitData>> rewardCommitData;
        public struct Node
        {
            public Config.RewardConfig reward;
            public int value; // scoremax
            public int showNum; // index
            public bool isPrime; //是否阶段性大奖
            public bool isCur;
            public bool isDone;
            public bool isGoal;
        }

        private void SetupMilestone()
        {
            var ids = activity.GetCurDetailConfig().MilestoneGroup;
            
            ListM.Clear();
            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                var conf = Game.Manager.configMan.GetMicMilestoneGroupConfig(id);
                ListM.Add(new()
                {
                    reward = conf.MilestoneReward.ConvertToRewardConfig(),
                    value = conf.MilestoneScore,
                    showNum = i,
                    isPrime = conf.IfGrandReward,
                });
            }
        }

        private void FillMilestoneData()
        {
            var curMileStoneIndex = activity.CurMilestoneLevel;
            for (int i = 0; i < ListM.Count; i++)
            {
                var m = ListM[i];
                var idx = m.showNum;
                m.isCur = curMileStoneIndex == idx;
                m.isGoal = idx > curMileStoneIndex;
                m.isDone = idx < curMileStoneIndex;
                ListM[i] = m;
            }
        }

        public RewardCommitData TryGetCommitReward(Config.RewardConfig reward)
        {
            RewardCommitData rewardCommitData = null;
            foreach (var commitData in this.rewardCommitData.obj)
            {
                if (commitData.rewardId == reward.Id && commitData.rewardCount == reward.Count)
                {
                    rewardCommitData = commitData;
                    break;
                }
            }

            return rewardCommitData;
        }

        #endregion
        
        #region UI组件

        [Header("基础UI组件")]
        [SerializeField] private Button playBtn;           // 开始游戏按钮
        [SerializeField] private Transform helpRoot;       // 帮助界面根节点
        [SerializeField] private Button helpBtn;           // 帮助按钮
        [SerializeField] private Button block;             // 遮罩按钮
        [SerializeField] private TMP_Text cd;              // 倒计时文本
        [SerializeField] private TMP_Text tip;             // 提示文本
        [SerializeField] private UIImageRes bg;            // 背景图片
        [SerializeField] private UIImageRes bg2;            // 背景图片
        [SerializeField] private UIImageRes titleBg;       // 标题背景

        [Header("里程碑相关")]
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private GameObject cell;
        [SerializeField] private GameObject cellRoot;
        [SerializeField] private RectTransform cellArea;
        
        [SerializeField] private TMP_Text helpTitle;
        [SerializeField] private MBRewardProgress rewardProgress;  // 进度条组件
        [SerializeField] private MBRewardIcon rewardIcon;   // 奖励图标
        [SerializeField] private Animation progressItemAnim; // 进度条Item动画

        [Header("动画配置 - 进度条播放动画")]
        [Tooltip("进度条充满动画时长")]
        [SerializeField] private float progressBarFillDuration = 0.8f;

        #endregion

        #region 私有字段

        private ActivityScoreMic activity;                    // 积分活动实例
        private bool isPopup;                              // 是否为弹脸模式
        private Action WhenCD;                             // 倒计时回调

        // Cell管理
        private List<Node> nodes = new();
        private List<GameObject> cellList = new();
        private int nodeOffset = 0;

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
            
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.SCORE_MIC_CELL, cell);
        }

        protected override void OnParse(params object[] items)
        {
            activity = (ActivityScoreMic)items[0];
            RefreshUI();
            SetupMilestone();
            
            shouldPopup = activity.LastMilestoneLevel < activity.CurMilestoneLevel;
            if (shouldPopup)
            {
                rewardCommitData = activity.PopCommitDataList();
            }
        }
        
        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            FillMilestoneData();
            RefreshCd();

            int curMilestoneIndex = 0;
            if (shouldPopup)
            {
                playBtn.gameObject.SetActive(false);
                //领奖弹出，这时的状态和ActivityScore中的数据状态是不一致的，需要准备用表现参数准备预领奖状态
                curMilestoneIndex = activity.LastMilestoneLevel;
                rewardProgress.Refresh(activity.LastMilestoneNum, ListM[activity.LastMilestoneLevel].value);
                //需要领奖的时候，锁定UI
                LockEvent();
            }
            else
            {
                //其余状态下，直接把UI状态刷新到和ActivityScore中的数据状态一致
                curMilestoneIndex = activity.CurMilestoneLevel;
                rewardProgress.Refresh(activity.CurMilestoneNum, ListM[activity.CurMilestoneLevel].value);
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
            
            if (shouldPopup)
            {
                PreCoPlayProgressAnimation();
            }
        }

        protected override void OnPostOpen()
        {
            base.OnPostOpen();

            // 播放进度条动画
            if (shouldPopup)
            {
                // 停止之前的动画
                if (_progressAnimationCoroutine != null)
                {
                    StopCoroutine(_progressAnimationCoroutine);
                }

                // 开始播放跨里程碑动画
                _progressAnimationCoroutine = StartCoroutine(CoPlayProgressAnimation());
            }
        }

        protected override void OnPostClose()
        {
            // 记录当前进度分数到ActivityScore中，用于下次打开时的起始点
            activity.OnMainUIClose();
            if (shouldPopup)
            {
                rewardCommitData.Free();
            }

            // 停止动画协程
            if (_progressAnimationCoroutine != null)
            {
                StopCoroutine(_progressAnimationCoroutine);
                _progressAnimationCoroutine = null;
            }

            // 清理创建的Cell
            foreach (var cellObj in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.SCORE_MIC_CELL, cellObj);
            }
            cellList.Clear();
            activity.CheckCanEnd();
        }

        protected override void OnAddListener()
        {
            WhenCD ??= RefreshCd;
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
            if (activity.IsComplete()) 
                return;
            var btnTrans = playBtn.transform;
            btnTrans.localScale = Vector3.zero;
            playBtn.gameObject.SetActive(true);
            btnTrans.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        private void RefreshCd()
        {
            var v = activity.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
                Close();
        }

        #endregion

        #region 数据刷新和显示

        private void RefreshUI()
        {
            MBI18NText.SetFormatKey(tip.gameObject, "#SysComDesc1711", UIUtility.FormatTMPString(activity.Conf.Token));
            MBI18NText.SetFormatKey(helpTitle.gameObject, "#SysComDesc1712", UIUtility.FormatTMPString(activity.Conf.Token));
        }

        /// <summary>
        /// 刷新里程碑列表
        /// 根据当前里程碑索引正确显示里程碑状态
        /// </summary>
        private void RefreshList(int currentMilestoneIndex)
        {
            nodes.Clear();
            for (int i = ListM.Count - 1; i >= 0; i--)
            {
                if (ListM[i].showNum < currentMilestoneIndex - 1)
                    continue;
                nodes.Add(ListM[i]);
            }

            nodeOffset = 0;
            if (nodes.Count < MinNum)
            {
                var needNum = MinNum - nodes.Count;
                for (int i = 0; i < needNum; i++)
                {
                    nodes.Add(ListM[currentMilestoneIndex - 2 - i]);
                    nodeOffset++;
                }
            }

            float height = (itemHeight + spacing) * nodes.Count + topAndBottome;
            if (currentMilestoneIndex != 0)
            {
                height -= (itemHeight + spacing) + spacing;
            }
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, height);
            scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 0f);
            cellArea.anchoredPosition = new Vector2(cellArea.anchoredPosition.x, currentMilestoneIndex != 0 ? -(itemHeight + spacing + spacing) / 2f : 0);
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
            if (milestoneIndex >= 0 && milestoneIndex < ListM.Count)
            {
                var node = ListM[milestoneIndex];
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

        private void PreCoPlayProgressAnimation()
        {
            var startMilestoneIndex = activity.LastMilestoneLevel;
            var endMilestoneIndex = activity.CurMilestoneLevel;
            int offset = nodeOffset + (startMilestoneIndex != 0 ? 1 : 0);
            
            for (int milestoneIndex = startMilestoneIndex; milestoneIndex <= endMilestoneIndex; milestoneIndex++)
            {
                var uiItem = cellList[^(1 + offset + milestoneIndex - startMilestoneIndex)].GetComponent<UIMicItem>();
                uiItem.UpdateContentByValue(milestoneIndex == startMilestoneIndex, milestoneIndex < startMilestoneIndex, milestoneIndex > startMilestoneIndex);
            }
        }

        /// <summary>
        /// 播放进度条动画
        /// 动画流程：
        /// 1. 预计算起始和结束分数对应的里程碑信息
        /// 2. 进度条：逐个充满每个里程碑，每次充满后重置从头开始
        /// 3. 轨道动画：进度条完成后，轨道逐个播放里程碑动画和领奖动画
        /// </summary>
        private IEnumerator CoPlayProgressAnimation()
        {
            // 设置动画状态参数
            _currentAnimationMilestoneIndex = -1;

            // 第一步：预计算起始和结束分数对应的里程碑信息
            var startMilestoneIndex = activity.LastMilestoneLevel;
            var startShowScore = activity.LastMilestoneNum;
            var startMilestoneScore = activity.GetCurMilestoneNumMax(activity.LastMilestoneLevel);
            var endMilestoneIndex = activity.CurMilestoneLevel;
            var endShowScore = activity.CurMilestoneNum;
            var endMilestoneScore = activity.GetCurMilestoneNumMax(activity.CurMilestoneLevel);
            int offset = nodeOffset + (startMilestoneIndex != 0 ? 1 : 0);

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
                    currentMilestoneScore = ListM[milestoneIndex].value;
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
                    if (IsComplete(milestoneIndex)) 
                        yield break;

                    // 动画完成后刷新图标
                    // rewardIcon.gameObject.SetActive(true);
                    RefreshRewardIcon(milestoneIndex + 1);
                    rewardProgress.Refresh(0, currentMilestoneScore);
                }
            }

            // 第三步：轨道动画 - 逐个播放里程碑动画和领奖动画
            // 从起始里程碑开始，逐个播放每个里程碑的动画
            int num = endMilestoneIndex - startMilestoneIndex;
            float height = Mathf.Max(scroll.content.sizeDelta.y - num * (itemHeight + spacing), (itemHeight + spacing) * (MinNum - 1) + topAndBottome);
            float moveNum = (scroll.content.sizeDelta.y - height) / (itemHeight + spacing);
            float areaPos = cellArea.anchoredPosition.y - (itemHeight + spacing) / 2f * moveNum;
            scroll.content.DOSizeDelta(new Vector2(scroll.content.sizeDelta.x, height), (ToNextTime + ToThisTime) * num).SetEase(Ease.Linear).SetLink(gameObject);
            cellArea.DOAnchorPos(new Vector2(cellArea.anchoredPosition.x, areaPos), (ToNextTime + ToThisTime) * num).SetEase(Ease.Linear).SetLink(gameObject);
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
            var milestoneList = ListM;
            var node = milestoneList[milestoneIndex];
            var reward = node.reward;
            var isPrimeReward = node.isPrime;
            var rewardData = TryGetCommitReward(reward);
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
                if (IsComplete(milestoneIndex))
                {
                    Close();
                }
            }
        }

        private bool IsComplete(int milestoneIndex)
        {
            if (milestoneIndex == ListM.Count - 1)
            {
                if (activity.IsComplete())
                {
                    return true;
                }
            }

            return false;
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