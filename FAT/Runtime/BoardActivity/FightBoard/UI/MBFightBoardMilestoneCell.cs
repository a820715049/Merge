/**
 * @Author: zhangpengjian
 * @Date: 2025/5/13 19:12:17
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/13 19:12:17
 * Description: 打怪棋盘里程碑进度item
 */

using System.Linq;
using EL;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBFightBoardMilestoneCell : MonoBehaviour
    {
        [SerializeField] private Transform cur;
        [SerializeField] private Transform goal;
        [SerializeField] private Transform done;
        [SerializeField] private Transform check;
        [SerializeField] private UIImageRes icon;
        [SerializeField] private UIImageRes iconDark;
        [SerializeField] private Button rewardBtn;
        [SerializeField] private Animator animator;
        [SerializeField] private Animator animatorComplete;
        public EventFightLevel node;
        private FightBoardActivity _activity;

        private void OnEnable()
        {
            rewardBtn.onClick.AddListener(ClickReward);
        }

        private void OnDisable()
        {
            rewardBtn.onClick.RemoveListener(ClickReward);
        }

        public void UpdateContent(FightBoardActivity activity, EventFightLevel node)
        {
            _activity = activity;
            this.node = node;
            icon.SetImage(node.LevelRewardIcon);
            var isCur = activity.GetCurrentMilestoneIndex() + 1 == node.ShowNum;
            var isGoal = activity.GetCurrentMilestoneIndex() + 1 < node.ShowNum;
            var isDone = activity.GetCurrentMilestoneIndex() + 1 > node.ShowNum;
            cur.gameObject.SetActive(isCur);
            goal.gameObject.SetActive(isGoal);
            done.gameObject.SetActive(isDone);
            check.gameObject.SetActive(isDone);
            icon.gameObject.SetActive(!isGoal);
            icon.SetImage(node.LevelRewardIcon);
            iconDark.SetImage(node.LevelRewardIconDark);
        }

        private void ClickReward()
        {
            _activity.MilestoneRes.res.ActiveR.Open(_activity);
        }

        public void PlayAnimation()
        {
            animator.SetTrigger("Punch");
        }

        public void PlayCompleteAnimation()
        {
            check.gameObject.SetActive(true);
            cur.gameObject.SetActive(false);
            goal.gameObject.SetActive(false);
            done.gameObject.SetActive(true);
            animatorComplete.SetTrigger("Punch");
        }
    }
}