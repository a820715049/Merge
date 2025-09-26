/**
 * @Author: zhangpengjian
 * @Date: 2025/6/6 14:55:08
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/6/6 14:55:08
 * Description: 循环关卡提示
 */

using System.Collections;
using System.Linq;
using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIActivityFightMilestoneTips : UIBase
    {
        // Image字段
        private UIImageRes _Bg;
        private UIImageRes _Left;

        // Text字段
        private TextMeshProUGUI _Desc;

        // Object字段
        private System.Action _callback;
        private FightBoardActivity _activity;

        protected override void OnCreate()
        {
            base.OnCreate();
            // Image绑定
            transform.Access("Root/Bg_img", out _Bg);
            transform.Access("Root/Left_img", out _Left);
            // Text绑定
            transform.Access("Root/Desc_txt", out _Desc);
        }

        protected override void OnParse(params object[] items)
        {
            _callback = items[0] as System.Action;
            Game.Manager.audioMan.TriggerSound("GuessCardRight");
            transform.GetComponent<Animator>().SetTrigger("Show");
            transform.AddButton("Mask", OnClickMask);
            _activity = items[1] as FightBoardActivity;
            _activity.MilestoneTipsRes.visual.Refresh(_Desc, "desc");
        }

        private void OnClickMask()
        {
            transform.GetComponent<Animator>().SetTrigger("Hide");
            StartCoroutine(CoHide());
        }

        private IEnumerator CoHide()
        {
            yield return new WaitForSeconds(0.4f);
            _callback?.Invoke();
            Close();
        }
    }
}
