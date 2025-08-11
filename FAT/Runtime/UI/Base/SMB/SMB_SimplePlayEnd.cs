/*
 * @Author: tang.yan
 * @Description: UI界面中监听动画播放完成 
 * @Date: 2024-10-10 10:10:06
 */
using UnityEngine;

namespace FAT
{
    public class SMB_SimplePlayEnd : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            EL.MessageCenter.Get<MSG.UI_SIMPLE_ANIM_FINISH>().Dispatch(stateInfo);
        }
    }

}