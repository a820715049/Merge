using System;
using EL;
using UnityEngine;
using System.Collections;

namespace FAT
{
    public class UIOrderChallengeFail : UIBase
    {
        private ActivityOrderChallenge _activity;
        public Animator _animator;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Go_btn", ClickClose);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
                _activity = items[0] as ActivityOrderChallenge;
            _animator.SetTrigger("Show");
        }

        protected override void OnPostClose()
        {
            UIManager.Instance.CloseWindow(_activity?.Res.ActiveR ?? UIConfig.UIOrderChallengeFail);
        }

        protected void ClickClose()
        {
            _animator.SetTrigger("Hide");
            IEnumerator par()
            {
                yield return new WaitForSeconds(0.15f);
                Close();
            }
            Game.Instance.StartCoroutineGlobal(par());
        }
    }
}
