/*
 * @Author: qun.chao
 * @Date: 2022-03-07 18:47:26
 */
using UnityEngine;
using EL;

namespace FAT
{
    public class UIGuideBlocker : MonoBehaviour
    {
        public void Setup()
        {
            transform.AddButton(null, _OnBtnClick);
        }

        public void InitOnPreOpen()
        {
            gameObject.SetActive(false);
        }

        public void CleanupOnPostClose()
        {
            gameObject.SetActive(false);
        }

        public void ShowBlocker()
        {
            gameObject.SetActive(true);
        }

        public void HideBlocker()
        {
            gameObject.SetActive(false);
        }

        private void _OnBtnClick()
        {
            _GetContext().UserClickBlocker();
        }

        private UIGuideContext _GetContext()
        {
            return Game.Manager.guideMan.ActiveGuideContext;
        }
    }
}