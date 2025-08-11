/*
 * @Author: tang.yan
 * @Description: 编辑器广告测试界面 用于替代广告播放 3秒钟后自动关闭 
 * @Date: 2023-12-06 10:12:48
 */
using System.Collections;
using UnityEngine;
using TMPro;
using EL;

namespace FAT
{
    public class AdsEditorTest : UIBase
    {
        private TMP_Text _text;
        private int _countDown = 3; //默认3秒钟后自动关闭
        private SimpleAsyncTask _waitTask;
        private Coroutine _coroutine;

        protected override void OnCreate()
        {
            _text = transform.FindEx<TMP_Text>("Content/Tips");
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
                _waitTask = items[0] as SimpleAsyncTask;
        }

        protected override void OnPreOpen()
        {
            _coroutine = StartCoroutine(_CoWaitTask());
        }

        private IEnumerator _CoWaitTask()
        {
            _countDown = 3;
            _text.text = _countDown.ToString();
            yield return new WaitForSeconds(1);
            _countDown--;
            _text.text = _countDown.ToString();
            yield return new WaitForSeconds(1);
            _countDown--;
            _text.text = _countDown.ToString();
            yield return new WaitForSeconds(1);
            _waitTask.ResolveTaskSuccess();
            Close();
        }

        protected override void OnPostClose()
        {
            _waitTask = null;
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }
    }
}