/*
 * @Author: tang.yan
 * @Description: 弹珠游戏得分口处的分数奖励
 * @Date: 2024-12-13 15:12:47
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class PachinkoEndReward : MonoBehaviour
    {
        [SerializeField] private TMP_Text itemCount;
        [SerializeField] private Animator animator;

        private bool _isBigReward = false;  //是否是大奖
        
        public void Refresh(int count, bool isBigReward)
        {
            itemCount.text = count.ToString();
            _isBigReward = isBigReward;
        }

        public void PlayRewardAnim()
        {
            if (_isBigReward)
            {
                animator.ResetTrigger("PunchMore");
                animator.SetTrigger("PunchMore");
            }
            else
            {
                animator.ResetTrigger("PunchOnce");
                animator.SetTrigger("PunchOnce");
            }
        }
    }
}