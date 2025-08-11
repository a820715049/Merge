/**
 * @Author: zhangpengjian
 * @Date: 2025/4/24 18:18:42
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/4/24 18:18:42
 * Description: 周任务主界面
 */

using System.Collections.Generic;
using System.Linq;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
namespace FAT
{
    public class UIActivityWeeklyTaskMain : UIBase
    {

        [SerializeField]
        private Button btnStage1;
        [SerializeField]
        private Button btnStage2;
        [SerializeField]
        private Button btnStage3;
        [SerializeField]
        private Button btnStage4;
        [SerializeField]
        private Button btnBox1;
        [SerializeField]
        private Button btnBox2;
        [SerializeField]
        private Button btnBox3;
        [SerializeField]
        private Button btnBox4;
        [SerializeField]
        private GameObject[] boxes;
        [SerializeField]
        private Button btnBoxFinal;
        private TextProOnACircle title;
        [SerializeField]
        private Button btnHelp;
        [SerializeField]
        private Button btnClose;
        [SerializeField]
        private Button btnClaim;
        [SerializeField]
        private Button btnClaim1;
        [SerializeField]
        private Button btnClaim2;
        [SerializeField]
        private Button btnClaim3;
        [SerializeField]
        private Button btnClaim4;
        [SerializeField]
        private Transform progressRoot;
        [SerializeField]
        private ScrollRect scroll;
        [SerializeField]
        private GameObject cell;
        [SerializeField]
        private GameObject cellRoot;
        [SerializeField]
        private TextMeshProUGUI cd;
        [SerializeField]
        private GameObject[] phaseStates;
        [SerializeField]
        private GameObject[] btnStage;
        [SerializeField]
        private GameObject block;
        [SerializeField]
        private GameObject efx1;
        [SerializeField]
        private GameObject efx2;
        private ActivityWeeklyTask activity;
        private List<GameObject> cellList = new();
        [SerializeField]
        private float itemAnimationDelay = 0.05f; // 每个动画的延迟时间
        [SerializeField]
        private float itemAnimationDuration = 0.3f; // 动画持续时间
        [SerializeField]
        private Vector3 startScale = new Vector3(0.8f, 0.8f, 0.8f); // 初始缩放
        [SerializeField]
        private float startAlpha = 0f; // 初始透明度
        private Coroutine currentAnimationCoroutine; // 添加协程引用
        [SerializeField]
        private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 添加缩放曲线
        [SerializeField]
        private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 添加透明度曲线
        private List<int> completeList;

        protected override void OnParse(params object[] items)
        {
            activity = items[0] as ActivityWeeklyTask;
            if (items.Length > 1)
            {
                completeList = items[1] as List<int>;
            }
        }

        protected override void OnPreOpen()
        {
            block.SetActive(false);
            btnClaim.gameObject.SetActive(activity.IsComplete());
            RefreshTheme();
            
            // 找到第一个有未完成任务的页签
            int targetStage = 0;
            bool found = false;
            for (int i = 0; i < activity.phaseTasks.Count; i++)
            {
                for (int j = 0; j < activity.phaseTasks[i].Count; j++)
                {
                    if (!activity.phaseTasks[i][j].complete)
                    {
                        targetStage = i;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
            if (completeList != null && completeList.Count > 0)
            {
                targetStage = completeList[^1];
            }
            OnClickStage(targetStage, true);
            RefreshProgress();
            RefreshFinalReward();
            
            // 延迟一帧执行滚动，确保UI已经更新
            Game.Instance.StartCoroutineGlobal(ScrollToUnfinishedTask(targetStage));
        }

        private IEnumerator ScrollToUnfinishedTask(int stageIndex)
        {
            yield return null;
            
            // 找到第一个未完成的任务的索引
            int unfinishedIndex = -1;
            for (int i = 0; i < activity.phaseTasks[stageIndex].Count; i++)
            {
                if (!activity.phaseTasks[stageIndex][i].complete)
                {
                    unfinishedIndex = i;
                    break;
                }
            }
            
            if (unfinishedIndex != -1)
            {
                // 计算目标位置
                float itemHeight = 180f + 8f; // 单个任务项的高度加间距
                float contentHeight = scroll.content.sizeDelta.y;
                float viewportHeight = scroll.viewport.rect.height;
                float targetY = itemHeight * unfinishedIndex;
                
                // 计算归一化位置 (1 - targetY / (contentHeight - viewportHeight))
                float normalizedPosition = Mathf.Clamp01(1f - (targetY / (contentHeight - viewportHeight)));
                scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, normalizedPosition);
            }
        }

        private void RefreshFinalReward()
        {
            efx1.SetActive(false);
            efx2.SetActive(false);
            for (int i = 0; i < 4; i++)
            {
                if (!activity.IsPhaseComplete(i))
                {
                    btnClaim.gameObject.SetActive(false);
                    return;
                }
            }
            if (activity.finalGet == 0)
            {
                btnClaim.gameObject.SetActive(true);
                efx1.SetActive(true);
                efx2.SetActive(true);
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void RefreshCD()
        {
            if (activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            if (diff <= 0)
            {
                Close();
                return;
            }
            UIUtility.CountDownFormat(cd, diff);
        }

        private void RefreshTheme()
        {
            var idx = 1;
            btnStage1.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = I18N.FormatText("#SysComDesc1072", idx++);
            btnStage2.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = I18N.FormatText("#SysComDesc1072", idx++);
            btnStage3.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = I18N.FormatText("#SysComDesc1072", idx++);
            btnStage4.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = I18N.FormatText("#SysComDesc1072", idx++);
            for (int i = 0; i < 4; i++)
            {
                btnStage[i].transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = I18N.FormatText("#SysComDesc1072", i + 1);
            }
        }

        private void RefreshProgress()
        {
            for (int i = 0; i < phaseStates.Length; i++)
            {
                phaseStates[i].SetActive(activity.IsPhaseComplete(i));
            }

            for (int i = 0; i < 4; i++)
            {
                progressRoot.GetChild(i).GetChild(2).gameObject.SetActive(false);
                progressRoot.GetChild(i).GetChild(1).gameObject.SetActive(false);
                if (activity.IsPhaseGot(i))
                {
                    progressRoot.GetChild(i).GetChild(2).gameObject.SetActive(true);
                }
                else if (activity.IsPhaseCanGet(i))
                {
                    progressRoot.GetChild(i).GetChild(1).gameObject.SetActive(true);
                }
                else
                {
                    var phase = activity.GetPhaseProgress(i);
                    progressRoot.GetChild(i).GetChild(0).GetComponent<MBRewardProgress>().Refresh(phase.Item1, phase.Item2);
                }
            }
        }

        protected override void OnCreate()
        {
            btnBox1.onClick.AddListener(() => OnClickBox(0));
            btnBox2.onClick.AddListener(() => OnClickBox(1));
            btnBox3.onClick.AddListener(() => OnClickBox(2));
            btnBox4.onClick.AddListener(() => OnClickBox(3));
            btnBoxFinal.onClick.AddListener(() => OnClickBoxFinal());
            btnStage1.onClick.AddListener(() => OnClickStage(0));
            btnStage2.onClick.AddListener(() => OnClickStage(1));
            btnStage3.onClick.AddListener(() => OnClickStage(2));
            btnStage4.onClick.AddListener(() => OnClickStage(3));
            btnHelp.onClick.AddListener(OnClickHelp);
            btnClose.onClick.AddListener(OnClickClose);
            btnClaim.onClick.AddListener(OnClickClaimFinal);
            btnClaim1.onClick.AddListener(() => OnClickClaim(0));
            btnClaim2.onClick.AddListener(() => OnClickClaim(1));
            btnClaim3.onClick.AddListener(() => OnClickClaim(2));
            btnClaim4.onClick.AddListener(() => OnClickClaim(3));
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.WEEKLY_TASK_CELL, cell);
        }

        private void OnClickBox(int index)
        {
            var id = activity.DetailConf.TaskGroup[index];
            var conf = fat.conf.Data.GetEventWeeklyTaskGrp(id);
            var list = Enumerable.ToList(conf.GrpReward.Select(s => s.ConvertToRewardConfig()));
            UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyTaskRewardTips, boxes[index].transform.position, 35f, list, false);
        }

        private void OnClickBoxFinal()
        {
            efx1.SetActive(false);
            efx2.SetActive(false);
            var list = Enumerable.ToList(activity.DetailConf.FinalReward.Select(s => s.ConvertToRewardConfig()));
            UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyTaskRewardTips, btnBoxFinal.transform.position, 35f, list, true);
        }

        private void OnClickStage(int index, bool isInit = false)
        {
            if (!isInit)
            {
                Game.Manager.audioMan.TriggerSound("UIClick");
                Game.Manager.audioMan.TriggerSound("WeeklyTaskJump");
                boxes[index].transform.parent.GetComponent<Animator>().SetTrigger("Punch");
            }
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = null;
            }

            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.WEEKLY_TASK_CELL, item);
            }
            cellList.Clear();
            for (int i = 0; i < btnStage.Length; i++)
            {
                btnStage[i].transform.GetChild(1).gameObject.SetActive(false);
            }
            btnStage[index].transform.GetChild(1).gameObject.SetActive(true);
            
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, (180 + 8) * activity.phaseTasks[index].Count + 28);
            activity.phaseTasks[index].Sort(TaskSort);
            
            scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 1f);
            
            currentAnimationCoroutine = StartCoroutine(ShowItemsWithAnimation(index));
        }

        private IEnumerator ShowItemsWithAnimation(int index)
        {
            for (int i = 0; i < activity.phaseTasks[index].Count; i++)
            {
                var cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.WEEKLY_TASK_CELL, cellRoot.transform);
                cell.GetComponent<UIActivityWeeklyTaskItem>().UpdateContent(activity.phaseTasks[index][i]);
                cellList.Add(cell);
                
                var canvasGroup = cell.GetComponent<CanvasGroup>();
                if (!canvasGroup) canvasGroup = cell.AddComponent<CanvasGroup>();
                canvasGroup.alpha = startAlpha;
                cell.transform.localScale = startScale;
            }
            
            scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 1f);
            
            // 记录每个项目开始动画的时间
            float[] startTimes = new float[cellList.Count];
            for (int i = 0; i < cellList.Count; i++)
            {
                startTimes[i] = Time.time + (i * itemAnimationDelay);
            }
            
            // 所有动画同时进行，但错开开始时间
            while (true)
            {
                bool allComplete = true;
                float currentTime = Time.time;
                
                for (int i = 0; i < cellList.Count; i++)
                {
                    if (currentTime < startTimes[i]) 
                    {
                        allComplete = false;
                        continue;
                    }
                    
                    var cell = cellList[i];
                    var canvasGroup = cell.GetComponent<CanvasGroup>();
                    
                    float elapsedTime = currentTime - startTimes[i];
                    if (elapsedTime < itemAnimationDuration)
                    {
                        allComplete = false;
                        float t = elapsedTime / itemAnimationDuration;
                        
                        float currentScale = startScale.x + (1f - startScale.x) * scaleCurve.Evaluate(t);
                        cell.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
                        canvasGroup.alpha = startAlpha + (1f - startAlpha) * alphaCurve.Evaluate(t);
                    }
                    else
                    {
                        cell.transform.localScale = Vector3.one;
                        canvasGroup.alpha = 1f;
                    }
                }
                
                scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 1f);
                
                if (allComplete) break;
                yield return null;
            }
        }

        private int TaskSort(ActivityWeeklyTask.WeeklyTask a_, ActivityWeeklyTask.WeeklyTask b_)
        {
            return a_.conf.Sort - b_.conf.Sort;
        }

        private void OnClickClaim(int index)
        {
            block.SetActive(true);
            progressRoot.GetChild(index).GetChild(2).gameObject.SetActive(true);
            progressRoot.GetChild(index).GetChild(2).GetChild(0).GetComponent<Animator>().SetTrigger("Punch");
            StartCoroutine(ShowBlock(index));
        }

        private IEnumerator ShowBlock(int index)
        {
            yield return new WaitForSeconds(0.35f);
            activity.ClaimStageReward(index, boxes[index].transform.position);
            block.SetActive(false);
        }

        private void OnClickClaimFinal()
        {
            btnClaim.gameObject.SetActive(false);
            activity.ClaimFinalReward(btnBoxFinal.transform.position);
            MessageCenter.Get<MSG.ACTIVITY_WEEKLY_TASK_END>().Dispatch();
        }

        private void OnClickHelp()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIActivityWeeklyTaskHelp);
        }

        private void OnClickClose()
        {
            Close();
        }

        protected override void OnPostClose()
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = null;
            }

            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.WEEKLY_TASK_CELL, item);
            }
            cellList.Clear();
            completeList = null;
        }
    }
}