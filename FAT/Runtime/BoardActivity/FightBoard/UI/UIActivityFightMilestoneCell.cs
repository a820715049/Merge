/**
 * @Author: zhangpengjian
 * @Date: 2025/5/13 19:12:17
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/13 19:12:17
 * Description: 打怪棋盘阶段进度item
 */

using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityFightMilestoneCell : MonoBehaviour
    {
        [SerializeField] private Transform cur;
        [SerializeField] private Transform goal;
        [SerializeField] private Transform done;
        [SerializeField] private Transform goalLock;
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;
        [SerializeField] private Transform up;
        [SerializeField] private Transform upV;
        [SerializeField] private Transform down;
        [SerializeField] private Transform downV;
        [SerializeField] private UITextState num;
        [SerializeField] private UICommonItem[] item;

        public void UpdateContent(UIActivityFightMilestone.Milestone node)
        {
            for (int i = 0; i < item.Length; i++)
            {
                item[i].transform.parent.gameObject.SetActive(i < node.reward.Length);
            }
            for (int i = 0; i < node.reward.Length; i++)
            {
                item[i].Refresh(node.reward[i], node.isCur ? 17 : item[i].TryFind("count").GetComponent<UITextState>().state[0].style);
                item[i].transform.Find("finish").gameObject.SetActive(node.isDone);
                item[i].transform.Find("count").gameObject.SetActive(!node.isDone);
            }
            num.text.text = node.showNum.ToString();
            num.Select(node.isCur ? 1 : node.isDone ? 2 : 0);
            cur.gameObject.SetActive(node.isCur);
            goal.gameObject.SetActive(node.isGoal);
            goalLock.gameObject.SetActive(node.isGoal);
            done.gameObject.SetActive(node.isDone);
            bg1.gameObject.SetActive(!node.isCur && (node.isDone || node.isGoal));
            bg2.gameObject.SetActive(node.isCur);
            up.gameObject.SetActive(true);
            upV.gameObject.SetActive(node.isDone && !node.isCur && !node.isGoal);
            down.gameObject.SetActive(node.showNum > 1);
            downV.gameObject.SetActive(node.isCur || node.isDone);
        }
    }
}