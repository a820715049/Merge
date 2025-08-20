/**
 * @Author: zhangpengjian
 * @Date: 2025/7/4 11:41:57
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/4 11:41:57
 * Description: 连续订单item
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace FAT
{
    public class UIActivityOrderStreakCell : MonoBehaviour
    {
        [SerializeField] private Transform cur;
        [SerializeField] private Transform goal;
        [SerializeField] private Transform done;
        [SerializeField] private Transform down;
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;
        [SerializeField] private UITextState num;
        [SerializeField] private GameObject[] item;
        [SerializeField] private GameObject[] finish;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject efx;

        private List<(int, int)> _node = new();

        public void UpdateContent(int index, List<(int, int)> node, int orderIdx, IDictionary<int, int> dic, bool isLast)
        {
            down.gameObject.SetActive(!isLast);
            efx.SetActive(false);
            _node.Clear();
            _node.AddRange(node);
            for (int i = 0; i < node.Count; i++)
            {
                item[i].gameObject.SetActive(false);
                finish[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < node.Count; i++)
            {
                if (node[i].Item1 != 0)
                {
                    item[i].gameObject.SetActive(true);
                    var hasItem = dic.ContainsKey(node[i].Item1);
                    var icon = Game.Manager.objectMan.GetBasicConfig(node[i].Item1);
                    item[i].transform.Find("icon").GetComponent<UIImageRes>().SetImage(icon.Icon);
                    item[i].transform.Find("bg3").gameObject.SetActive(hasItem);
                    finish[i].gameObject.SetActive(index < orderIdx);
                }
            }
            var isCur = index == orderIdx;
            var isDone = index < orderIdx;
            var isGoal = index > orderIdx;
            num.text.text = (index + 1).ToString();
            num.Select(isCur ? 1 : isDone ? 2 : 0);

            if (isCur)
            {
                var anim = transform.GetComponent<Animator>();
                // 重置所有trigger
                anim.ResetTrigger("Current");
                anim.ResetTrigger("Future");
                anim.ResetTrigger("Finish");
                anim.ResetTrigger("Current_Finish");
                anim.ResetTrigger("Future_Current");
                // 设置当前trigger
                anim.SetTrigger("Current");
            }
            else if (isGoal)
            {
                var anim = transform.GetComponent<Animator>();
                // 重置所有trigger
                anim.ResetTrigger("Current");
                anim.ResetTrigger("Future");
                anim.ResetTrigger("Finish");
                anim.ResetTrigger("Current_Finish");
                anim.ResetTrigger("Future_Current");
                // 设置当前trigger
                anim.SetTrigger("Future");
            }
            else if (isDone)
            {
                var anim = transform.GetComponent<Animator>();
                // 重置所有trigger
                anim.ResetTrigger("Current");
                anim.ResetTrigger("Future");
                anim.ResetTrigger("Finish");
                anim.ResetTrigger("Current_Finish");
                anim.ResetTrigger("Future_Current");
                // 设置当前trigger
                anim.SetTrigger("Finish");
            }
        }

        public void PlayCurrent2Finish()
        {
            var anim = transform.GetComponent<Animator>();
            // 重置所有trigger
            anim.ResetTrigger("Current");
            anim.ResetTrigger("Future");
            anim.ResetTrigger("Finish");
            anim.ResetTrigger("Current_Finish");
            anim.ResetTrigger("Future_Current");
            for (int i = 0; i < _node.Count; i++)
            {
                if (_node[i].Item1 != 0)
                {
                    finish[i].gameObject.SetActive(true);
                    item[i].transform.Find("bg3").gameObject.SetActive(false);
                }
            }
            // 设置当前trigger
            anim.SetTrigger("Current_Finish");
            Game.Manager.audioMan.TriggerSound("OrderStreakComplete");
            num.Select(2);
        }

        public void PlayFuture2Current()
        {
            var anim = transform.GetComponent<Animator>();
            // 重置所有trigger
            anim.ResetTrigger("Current");
            anim.ResetTrigger("Future");
            anim.ResetTrigger("Finish");
            anim.ResetTrigger("Current_Finish");
            anim.ResetTrigger("Future_Current");
            // 设置当前trigger
            anim.SetTrigger("Future_Current");
            num.Select(1);
            Game.Manager.audioMan.TriggerSound("OrderStreakBlast");
        }

        public void PlayEfx()
        {
            // 获取特效的当前位置
            var currentPos = efx.transform.localPosition;
            
            // 设置特效到上一个cell的高度（y坐标减去260，即cell高度+spacing）
            var fromPos = new Vector3(currentPos.x, currentPos.y + 260f, currentPos.z);
            efx.transform.localPosition = fromPos;
            efx.SetActive(true);
            
            // 使用DOTween移动到当前cell的高度
            efx.transform.DOLocalMove(currentPos, 0.36f).SetEase(Ease.OutQuad);
        }
    }
}