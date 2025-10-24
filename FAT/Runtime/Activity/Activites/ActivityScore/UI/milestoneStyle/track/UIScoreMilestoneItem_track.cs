/**
 * @Author: ShentuAnge
 * @Date: 2025/06/24 14:24:11
 * Description: milestone item
 */

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace FAT
{
    public class UIScoreMilestoneItem_track : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private Transform m_base;
        [SerializeField] private Transform m_rootLeft;
        [SerializeField] private Transform m_rootRight;
        [SerializeField] private UIImageState m_under;
        [SerializeField] public UICommonItem commonItem;
        [SerializeField] private float m_localPosOffset;
        [SerializeField] private Animation m_ani;
        [SerializeField] private GameObject effectNormal;
        [SerializeField] private GameObject effectPrime;
        [SerializeField] private string m_idleAnimName = "ScoreItem_track_idle";
        [SerializeField] private string m_hideAnimName = "ScoreItem_track_disappear";

        [SerializeField] private bool mirrorUnder = false;
        [SerializeField] private Transform mirrorRoot;
        [SerializeField] private int fontNum = 17;
        //是否在左侧.
        public bool OnLeft { get; private set; }

        public void UpdateContent(ActivityScore.Node node, bool isLast)
        {
            commonItem.Refresh(node.reward, fontNum);
            //偶数在右侧,奇数在左侧.
            OnLeft = node.showNum % 2 == 0;
            m_rootLeft.gameObject.SetActive(!OnLeft && !isLast);
            m_rootRight.gameObject.SetActive(OnLeft && !isLast);
            m_base.localPosition = new Vector3(OnLeft ? -m_localPosOffset : m_localPosOffset, 0, 0);
            m_under.Select(node.isPrime ? 1 : 0);
            effectNormal.SetActive(!node.isPrime);
            effectPrime.SetActive(node.isPrime);
            m_ani.Play(m_idleAnimName);
            if (mirrorUnder && mirrorRoot != null)
            {
                mirrorRoot.localScale = OnLeft ? new Vector3(-1, 1, 1) : Vector3.one;
            }
        }

        public void PlayHide()
        {
            m_ani.Play(m_hideAnimName);
        }
    }
}
