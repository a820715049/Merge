/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏-结束界面 (成功/失败)
 * @Date: 2024-10-06 18:44:14
 */

using UnityEngine;
using FAT;
using EL;
using fat.rawdata;
using UnityEngine.UI.Extensions;
using TMPro;

namespace MiniGame
{
    public class UIBeadsResult : UIBase
    {
        [SerializeField] private GameObject successGo;
        [SerializeField] private GameObject failGo;
        [SerializeField] private Animator beadsAnimator;

        private bool _isSuccess;
        private bool _isPlayAnim;
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", _TryClickClose);
        }

        protected override void OnParse(params object[] items)
        {
            _isSuccess = (bool)items[0];
        }

        protected override void OnPreOpen()
        {
            successGo.SetActive(_isSuccess);
            failGo.SetActive(!_isSuccess);
            _isPlayAnim = true;
            //关卡成功时播动画和音效 同时可以手动关界面
            if (_isSuccess)
            {
                beadsAnimator.SetTrigger("Success");
                Game.Manager.audioMan.TriggerSound("BeadsSuccess");
            }
            //关卡失败时执播动画 动画播完后自动关界面
            else
            {
                beadsAnimator.SetTrigger("Fail");
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<FAT.MSG.UI_SIMPLE_ANIM_FINISH>().AddListener(_OnAnimPlayEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<FAT.MSG.UI_SIMPLE_ANIM_FINISH>().RemoveListener(_OnAnimPlayEnd);
        }

        protected override void OnPreClose()
        {
            _isPlayAnim = false;
            UIManager.Instance.CloseWindow(UIConfig.UIBeads);
        }

        private void _TryClickClose()
        {
            if (!_isSuccess || _isPlayAnim) return;    //成功时可以手动关界面 失败时会播动画 动画播完后自动关界面
            Close();
        }
        
        private void _OnAnimPlayEnd(AnimatorStateInfo stateInfo)
        {
            if (stateInfo.IsName("UIBeadsResult_ani_success"))
            {
                _isPlayAnim = false;
            }
            else if (stateInfo.IsName("UIBeadsResult_ani_fall"))
            {
                _isPlayAnim = false;
                Close();
            }
        }
    }
}
