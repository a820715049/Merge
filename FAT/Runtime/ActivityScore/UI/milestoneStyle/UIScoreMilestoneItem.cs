/**
 * @Author: zhangpengjian
 * @Date: 2024/9/23 10:55:11
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/9/23 10:55:11
 * Description: milestone item
 */

using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMilestoneItem : MonoBehaviour
    {

        [SerializeField] private Transform cur;
        [SerializeField] private Transform goal;
        [SerializeField] private Transform done;
        [SerializeField] private Transform goalLock;
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;
        [SerializeField] private Transform bg3;
        [SerializeField] private Transform bg4;
        [SerializeField] private Transform up;
        [SerializeField] private Transform upV;
        [SerializeField] private Transform down;
        [SerializeField] private Transform downV;
        [SerializeField] private UITextState num;
        [SerializeField] private UICommonItem item;
        [SerializeField] private Transform complete;
        [SerializeField] private Transform sweep;

        public void UpdateContent(ActivityScore.Node node)
        {
            item.Refresh(node.reward, node.isCur ? 17 : 41);
            num.text.text = node.showNum.ToString();
            num.Select(node.isCur ? 1 : 0);
            cur.gameObject.SetActive(node.isCur);
            goal.gameObject.SetActive(node.isGoal);
            goalLock.gameObject.SetActive(node.isGoal);
            done.gameObject.SetActive(node.isDone);
            bg1.gameObject.SetActive(!node.isCur && !node.isPrime && (node.isDone || node.isGoal));
            bg2.gameObject.SetActive(node.isCur && !node.isPrime);
            bg3.gameObject.SetActive(!node.isCur && node.isPrime);
            bg4.gameObject.SetActive(node.isCur && node.isPrime);
            sweep.gameObject.SetActive((node.isCur && node.isPrime) || (!node.isCur && node.isPrime));
            up.gameObject.SetActive(true);
            upV.gameObject.SetActive(node.isDone && !node.isCur && !node.isGoal);
            down.gameObject.SetActive(node.showNum > 1);
            downV.gameObject.SetActive(node.isCur || node.isDone);
            complete.gameObject.SetActive(node.isComplete && !node.isCur && !node.isGoal);
            item.gameObject.SetActive(!(node.isComplete && !node.isCur && !node.isGoal));
        }
    }
}