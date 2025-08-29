/**
 * @Author: zhangpengjian
 * @Date: 2025/7/16 11:16:51
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/16 11:16:51
 * Description: 沙堡里程碑cell
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class MBActivityCastleCell : MonoBehaviour
    {
        [SerializeField] private Transform cur;
        [SerializeField] private Transform goal;
        [SerializeField] private Transform done;
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;
        [SerializeField] private Transform up;
        [SerializeField] private Transform down;
        [SerializeField] private UIImageRes rewardIcon;
        [SerializeField] private UICommonItem[] items;

        public void UpdateContent(int index, fat.rawdata.CastleMilestoneGroup conf, int scorePhase, bool isLast)
        {
            var isCur = index == scorePhase;
            var isGoal = index > scorePhase;
            var isDone = index < scorePhase;
            for (int i = 0; i < items.Length; i++)
            {
                items[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < conf.MilestoneReward.Count; i++)
            {
                items[i].Refresh(conf.MilestoneReward[i].ConvertToRewardConfig(), isCur ? 9 : 17);
                items[i].gameObject.SetActive(true);
                items[i].transform.Find("count").gameObject.SetActive(isCur || isGoal);
                items[i].transform.Find("Finish").gameObject.SetActive(isDone);
            }
            cur.gameObject.SetActive(isCur);
            goal.gameObject.SetActive(isGoal);
            done.gameObject.SetActive(isDone);
            bg1.gameObject.SetActive(!isCur);
            bg2.gameObject.SetActive(isCur);
            up.gameObject.SetActive(!isLast);
            down.gameObject.SetActive(isDone && !isLast);
            rewardIcon.SetImage(conf.MilestoneRewardIcon2);
        }

        public void PlayCurrent2Finish()
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i].transform.Find("Finish").gameObject.SetActive(false);
                items[i].transform.Find("Finish").gameObject.SetActive(true);
                items[i].transform.Find("Finish").GetComponent<Animation>().Play("CastleMilestoneCell_finish");
            }
        }

        public void PlayGoal2Current()
        {
            
        }
    }
}