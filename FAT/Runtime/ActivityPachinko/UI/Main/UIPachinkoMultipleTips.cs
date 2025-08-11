/*
 * @Author: tang.yan
 * @Description: 弹珠界面倍率tips 
 * @Date: 2024-12-20 14:12:34
 */

using System.Collections;
using Cysharp.Text;
using UnityEngine;
using TMPro;

namespace FAT
{
    public class UIPachinkoMultipleTips : UITipsBase
    {
        [SerializeField] private GameObject normalGo; 
        [SerializeField] private GameObject maxGo; 
        [SerializeField] private TMP_Text normalText;
        private bool _isMax = false;
        private int _multipleNum = 0;
        private Coroutine _coroutine;

        protected override void OnCreate()
        {
            
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 4)
            {
                _SetTipsPosInfo(items);
                _isMax = (bool)items[2];
                _multipleNum = (int)items[3];
            }
        }

        protected override void OnPreOpen()
        {
            //刷新tips位置
            _RefreshTipsPos();
            _Refresh();
        }

        protected override void OnRefresh()
        {
            _Refresh();
        }

        protected override void OnPostClose()
        {
            _ClearCoroutine();
        }

        private void _Refresh()
        {
            normalGo.SetActive(!_isMax);
            maxGo.SetActive(_isMax);
            normalText.text = ZString.Concat("x", _multipleNum);
            //每次刷新都重置自动关闭时间
            _ClearCoroutine();
            _coroutine = StartCoroutine(_CoClose());
        }

        private IEnumerator _CoClose()
        {
            yield return new WaitForSeconds(1f);
            Close();
        }
        
        private void _ClearCoroutine()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }
    }
}
