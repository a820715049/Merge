/*
 * @Author: yanfuxing
 * @Date: 2025-04-22 11:31:29
 */
using UnityEngine;
using UnityEngine.UI;
using static FAT.ActivityRanking;

namespace FAT
{
    public class RankingMilestoneItem : MonoBehaviour
    {
        [SerializeField] private Transform curProRankBg;    //当前进度圆底背景（进行中的）
        [SerializeField] private Transform targetProRankBg; //目标进度圆底背景(未完成的)
        [SerializeField] private Transform finishProRankBg; //完成进度圆底背景（已完成的）
        [SerializeField] private Transform goalLock;    //目标锁
        [SerializeField] private Transform rightFinishBg; //当前已完成的里程碑Bg
        [SerializeField] private Transform rightCurProBg; //当前正在进行的里程碑Bg
        [SerializeField] private Transform upProgress;   //上进度条
        [SerializeField] private Transform upProgressFore;   //上进度条Fore
        [SerializeField] private Transform downProgress;    //下进度条
        [SerializeField] private Transform downProgressFore;    //下进度条Fore
        [SerializeField] private UITextState progressNum;   //进度值
        [SerializeField] private UICommonItem rewardItem;   //奖励值
        [SerializeField] private Transform completeBg;   //完成对钩

        public void OnUpdateMilestoneItem(NodeItem itemData)
        {
            rewardItem.Refresh(itemData.Reward, itemData.IsCurPro ? 17 : 9);
            progressNum.text.text = itemData.showNum.ToString();
            progressNum.Select(GetTextStateIndex(itemData));
            curProRankBg.gameObject.SetActive(itemData.IsCurPro);
            targetProRankBg.gameObject.SetActive(itemData.IsGoalPro);
            goalLock.gameObject.SetActive(itemData.IsGoalPro);
            finishProRankBg.gameObject.SetActive(itemData.IsDonePro);
            rightFinishBg.gameObject.SetActive(!itemData.IsCurPro && (itemData.IsDonePro || itemData.IsGoalPro));
            rightCurProBg.gameObject.SetActive(itemData.IsCurPro);
            upProgress.gameObject.SetActive(true);
            upProgressFore.gameObject.SetActive(itemData.IsDonePro && !itemData.IsCurPro && !itemData.IsGoalPro);
            downProgress.gameObject.SetActive(itemData.showNum > 1);
            downProgressFore.gameObject.SetActive(itemData.IsCurPro || itemData.IsDonePro);
            completeBg.gameObject.SetActive(!itemData.IsCurPro && !itemData.IsGoalPro);
            //rewardItem.gameObject.SetActive(!(itemData.isComplete && !itemData.IsCurPro && !itemData.isGoal));
        }

        private int GetTextStateIndex(NodeItem itemData)
        {
            var isCurPro = itemData.IsCurPro;
            var isGoalPro = itemData.IsGoalPro;
            var isDonePro = itemData.IsDonePro;
            if (isCurPro)
            {
                return 1;
            }
            else if (isGoalPro)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }
    }
}
