/*
 * @Author: tang.yan
 * @Description: UI动画状态机检测界面开启动画播完 
 * @Date: 2024-02-05 11:02:25
 */
using UnityEngine;

namespace FAT
{
    public class SMB_OnPostOpen : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent<UIBase>(out var uiBase))
            {
                EL.MessageCenter.Get<MSG.UI_OPEN_ANIM_FINISH>().Dispatch(uiBase.ResConfig);
            }
        }
    }

}