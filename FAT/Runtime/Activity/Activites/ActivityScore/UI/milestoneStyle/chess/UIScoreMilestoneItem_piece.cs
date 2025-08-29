/**
 * @Author: ShentuAnge
 * @Date: 2025/07/01 19:04:11
 * Description: milestone item chess
 */

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

namespace FAT
{
    public class UIScoreMilestoneItem_piece : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private Transform m_base;
        [SerializeField] public UICommonItem commonItem;
        [SerializeField] private Animation m_itemAni;
        [SerializeField] private GameObject flagObj;
        [SerializeField] private Animation flagAni;

        #region 公共属性
        [SerializeField] public List<Transform> pathPoints = new List<Transform>();
        [SerializeField] public float rotation;
        [SerializeField] public float rotationDelay;
        [SerializeField] public float rotationEarly;
        #endregion

        //是否在左侧.

        public void UpdateContent(ActivityScore.Node node, bool isClaimed, bool showFlag)
        {
            flagObj.SetActive(showFlag);

            if (isClaimed)
            {
                commonItem.gameObject.SetActive(false);
            }
            else
            {
                commonItem.gameObject.SetActive(true);
                m_itemAni.Play("Score_piece_reward_idle");
                commonItem.Refresh(node.reward, 17);
            }
        }

        public void PlayFlagShow(float delay)
        {
            StartCoroutine(PlayFlagShowCoroutine(delay));
        }
        //协程延迟播放动画
        private IEnumerator PlayFlagShowCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            flagObj.gameObject.SetActive(true);
            flagAni.Play();
        }
        public void Hide()
        {
            m_itemAni.Play("Score_piece_reward_disappear");
            flagObj.SetActive(false);
        }
    }
}
