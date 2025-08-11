/*
 * @Author: tang.yan
 * @Description: UI提示条
 * @Date: 2023-10-23 18:10:01
 */
using System.Collections;
using EL;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class UIPopTips : UIBase
    {
        public float toastLifetime = 2f;
        private string _curContent;
        private GameObject mCurItem = null;
        private TMP_Text mCurText = null;

        protected override void OnCreate()
        {
            mCurItem = transform.Find("Content/ToastItem").gameObject;
            mCurText = transform.FindEx<TMP_Text>("Content/ToastItem/Text");
        }

        protected override void OnParse(params object[] items)
        {
            _curContent = (string)items[0];
        }

        protected override void OnPreOpen()
        {
            PlayOpenAnim();
            _ShowTips();
        }

        protected override void OnRefresh()
        { 
            PlayOpenAnim();
            _ShowTips();
        }

        protected override void OnPostClose()
        {
            _Clear();
        }

        private void _ShowTips()
        {
            _Clear();
            StartCoroutine(_CoShowToast(_curContent));
        }

        private void _Clear()
        {
            StopAllCoroutines();
        }

        private IEnumerator _CoShowToast(string content)
        {
            yield return null;
            mCurText.text = content;
            yield return new WaitForSeconds(toastLifetime / 4 * 3);
            PlayCloseAnim();
            yield return new WaitForSeconds(toastLifetime / 4);
            Close();
        }
    }
}