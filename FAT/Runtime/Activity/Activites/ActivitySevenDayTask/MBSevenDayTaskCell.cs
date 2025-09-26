using System.Collections;
using System.Collections.Generic;
using Cysharp.Text;
using EL;
using fat.conf;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FAT
{
    public enum DayTaskCellState
    {
        Locked = 0,      // 未解锁
        Unfinished = 1,  // 已解锁未完成
        WaitToClaim = 2, // 可领取或待领取
        Completed = 3    // 已领取完成
    }
    public class MBSevenDayTaskCell : MonoBehaviour
    {
        public TextMeshProUGUI desc;
        public Animator animator;
        public Animator compeleAnim;
        public UIImageRes taskIcon;
        public UICommonItem tokenReward;
        public UICommonItem normalReward;
        public GameObject LockMask;
        public GameObject CompleteBg;
        public GameObject RewardNode;
        public GameObject progress;
        public Transform Complete;
        public SevenDayTaskInfo info;
        public bool waitComplete;
        public bool hasComplete;
        public bool unlock;
        public int groupId;
        public Animation btnAnim;
        public float btnDelay;
        public float descDelay;
        public UITextState descState;
        public UITextState plusState;
        [SerializeField] private RectTransform lockAnchor; // 锁图标的锚点

        [SerializeField] private RectTransform tokenInfoTips;   // RewardNode/第一个奖励位/Icon 下的 Tips
        [SerializeField] private RectTransform normalInfoTips;  // RewardNode/第二个奖励位/Icon 下的 Tips

        private void Awake()
        {
            // 整行点击区域,可点击任意处
            var btn = GetComponent<Button>();
            if (btn == null) btn = gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClickCell);
        }

        public void SetGroupId(int gid) => groupId = gid; // 供Panel设置

        private void OnClickCell()
        {
            var state = GetState();

            // 已完成或待领取时不弹气泡
            if (state == DayTaskCellState.Completed || state == DayTaskCellState.WaitToClaim) return;

            if (IsClickOnRewardInfo()) return;

            Transform anchor;
            string text;
            float offset;

            if (state == DayTaskCellState.Locked && LockMask != null)
            {
                // 在锁 icon 位置显示解锁条件
                if (hasComplete || waitComplete)
                {
                    var anchorTf = (Transform)(lockAnchor != null ? lockAnchor : LockMask.transform);
                    var anchorRt = lockAnchor != null ? lockAnchor : (LockMask.transform as RectTransform);

                    anchor = anchorTf;
                    int unlockDay = GetUnlockDay();
                    text = I18N.FormatText("#SysComDesc1595", unlockDay);
                }
                else
                {
                    anchor = taskIcon.transform;
                    text = I18N.FormatText(info.IconDesc, I18N.Text(info.ActivityTitle));
                }
            }
            else
            {
                // 已解锁未完成,在物品 icon 位置显示任务描述
                anchor = taskIcon.transform;
                text = I18N.FormatText(info.IconDesc, I18N.Text(info.ActivityTitle));
            }

            offset = GetHalfHeight(anchor as RectTransform);
            UIManager.Instance.OpenWindow(UIConfig.UISevenDayTaskTips, anchor.position, offset, text);
        }

        private bool IsClickOnRewardInfo()
        {
            if (!unlock) return false;
            return TryDispatchClickToTips(tokenInfoTips) || TryDispatchClickToTips(normalInfoTips);
        }

        private bool TryDispatchClickToTips(RectTransform tipsRt)
        {
            if (tipsRt == null || !tipsRt.gameObject.activeInHierarchy) return false;

            var canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            var sp = (Vector2)Input.mousePosition;
            if (!RectTransformUtility.RectangleContainsScreenPoint(tipsRt, sp, cam))
                return false;

            var go = tipsRt.gameObject;
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.Invoke();
                return true;
            }

            var es = EventSystem.current;
            if (es != null)
            {
                var ped = new PointerEventData(es) { position = sp };
                ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerClickHandler);
                return true;
            }

            return false;
        }

        private float GetHalfHeight(RectTransform rt) => rt != null ? rt.rect.height * 0.5f : 0f;

        private DayTaskCellState GetState()
        {
            if (!unlock) return DayTaskCellState.Locked;
            if (waitComplete && !hasComplete) return DayTaskCellState.WaitToClaim;
            if (!waitComplete && !hasComplete) return DayTaskCellState.Unfinished;
            return DayTaskCellState.Completed;
        }

        private int GetUnlockDay()
        {
            var grp = SevenDayTaskGroupVisitor.Get(groupId);
            return grp != null ? grp.UnlockDay : 0;
        }

        public void RefreshData(int id)
        {
            info = SevenDayTaskInfoVisitor.Get(id);
            taskIcon.SetImage(info.IconShow);
            tokenReward.Refresh(info.TaskReward[0].ConvertToRewardConfig());
            normalReward.Refresh(info.TaskReward[1].ConvertToRewardConfig());
        }

        public void RefreshState(bool wait, bool complete)
        {
            waitComplete = wait;
            hasComplete = complete;
            if (hasComplete) unlock = true;
            descState.Select(waitComplete || hasComplete ? 1 : 0);
            plusState.Select(waitComplete || hasComplete ? 1 : 0);
            desc.gameObject.SetActive(!wait);
            CompleteBg.SetActive(waitComplete || hasComplete);
            compeleAnim.enabled = false;
            Complete.transform.localScale = new Vector3(complete ? 1 : 0, 1, 1);
            progress.SetActive(!waitComplete && !hasComplete);
            transform.Find("CompleteBtn").localScale = (waitComplete && !hasComplete) ? Vector3.one : Vector3.zero;
            LockMask.GetComponent<Image>().transform.localScale = complete || !unlock ? Vector3.one : Vector3.zero;
            RewardNode.SetActive(!complete);
        }

        public void RefreshProgress(int progress, int target)
        {
            desc.text = I18N.FormatText(info.Desc, target);
            (this.progress.transform.Find("Mask") as RectTransform).sizeDelta = new Vector2(400 * (float)progress / target, 42);
            this.progress.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = ZString.Format("{0}/{1}", progress, target);
        }

        public void SetClaimState()
        {

        }

        public void SetLock()
        {
            unlock = false;
            animator.SetTrigger("Lock");
        }

        public void SetUnlock()
        {
            unlock = true;
            animator.SetTrigger("Unlock");
            StartCoroutine(BtnAfterUnlock());
            StartCoroutine(DescAfterUnlock());
        }

        IEnumerator BtnAfterUnlock()
        {
            yield return new WaitForSeconds(btnDelay);
            if (waitComplete) btnAnim.Play();
            LockMask.transform.localScale = Vector3.zero;
        }

        IEnumerator DescAfterUnlock()
        {
            yield return new WaitForSeconds(descDelay);
            if (waitComplete) desc.gameObject.SetActive(false);
        }

        public void SetIdle()
        {
            unlock = true;
            animator.SetTrigger("Idle");
        }

        public void Claim(List<RewardCommitData> rewardCommitDatas)
        {
            compeleAnim.enabled = true;
            waitComplete = false;
            hasComplete = true;
            animator.SetTrigger("Hide");
            compeleAnim.SetTrigger("Punch");
            transform.Find("CompleteBtn").localScale = Vector3.zero;
            LockMask.transform.localScale = Vector3.one;
            desc.gameObject.SetActive(true);
            if (rewardCommitDatas.Count > 0) { UIFlyUtility.FlyReward(rewardCommitDatas[0], RewardNode.transform.GetChild(0).GetChild(0).transform.position); }
            if (rewardCommitDatas.Count > 1) { UIFlyUtility.FlyReward(rewardCommitDatas[1], RewardNode.transform.GetChild(2).GetChild(0).transform.position); }
        }
    }
}