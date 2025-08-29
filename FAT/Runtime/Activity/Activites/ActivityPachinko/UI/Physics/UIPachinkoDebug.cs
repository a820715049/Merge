/*
 * @Author: tang.yan
 * @Description: 弹珠游戏Debug工具
 * @Date: 2024-12-05 19:12:00
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EL;
using TMPro;
using Text = TMPro.TMP_Text;
using InputField = TMPro.TMP_InputField;

namespace FAT
{
    public class UIPachinkoDebug : UIBase
    {
        [SerializeField] private GameObject goItem;
        [SerializeField] private GameObject goCompButton;
        [SerializeField] private GameObject goCompInput;
        [SerializeField] private Transform rootB;

        private Transform mCurItemRoot;
        private Dictionary<Text, Action<Text>> mInfoUpdator = new Dictionary<Text, Action<Text>>();

        protected override void OnCreate()
        {
            transform.AddButton("Content/TR/BtnClose", base.Close);
            _Build();
        }

        protected override void OnPreOpen()
        {
        }

        protected override void OnPreClose()
        {
        }
        
        #region build

        private void _Build()
        {
            mCurItemRoot = rootB;
            _RegisterButtonWithInput("SetGravity(0-100)", "9", (str) => DebugMsg(1, str));
            _RegisterButtonWithInput("SetBounciness(0-1)", "0.65", (str) => DebugMsg(2, str));
            _RegisterButtonWithInput("SetBallMass(0-100)", "1", (str) => DebugMsg(3, str));
            _RegisterButtonWithInput("SetBallStartInfo", "1,0,11,-60", (str) => DebugMsg(4, str));
            _RegisterButton("Play", () => DebugMsg(5, ""));
            _RegisterButton("Pause", () => DebugMsg(6, ""));
            _RegisterButtonWithInput("Run Auto Test", "0,11,-60", (str) => DebugMsg(7, str));
            _RegisterButtonWithInput("Run Weight Test", "500", (str) => DebugMsg(8, str));
        }

        private void DebugMsg(int paramType, string param)
        {
            MessageCenter.Get<MSG.GAME_DEBUG_PACHINKO_INFO>().Dispatch(paramType, param);
        }
        
        private Transform _AddItem(Transform root)
        {
            var go = Instantiate(goItem, root);
            go.SetActive(true);
            return go.transform;
        }

        private Transform _AddButton(Transform root)
        {
            var go = Instantiate(goCompButton, root);
            go.SetActive(true);
            return go.transform;
        }

        private Transform _AddInput(Transform root)
        {
            var go = Instantiate(goCompInput, root);
            go.SetActive(true);
            return go.transform;
        }

        private void _RegisterButton(string desc, Action act = null)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            btn.FindEx<Text>("Text").text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                act?.Invoke();
            };
            btn.AddButton(null, cb);
        }

        private void _RegisterDisplayButton(string desc, Func<string> f)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var text = btn.FindEx<Text>("Text");
            text.text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                text.text = f?.Invoke();
            };
            btn.AddButton(null, cb);
        }

        private void _RegisterButtonWithInput(string desc, string inputDesc, Action<string> act = null)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var input = _AddInput(item).GetComponent<InputField>();
            input.text = inputDesc;
            btn.FindEx<Text>("Text").text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                act?.Invoke(input.text);
            };
            btn.AddButton(null, cb);
        }
        
        private void _RegisterInfo(string desc, Action<Text> act)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var area = btn.FindEx<Text>("Text");
            Action<Text> imp = text =>
            {
                act.Invoke(text);
                text.text = $"{desc}{text.text}";
            };
            area.enableAutoSizing = true;
            area.enableWordWrapping = false;
            area.alignment = TextAlignmentOptions.MidlineLeft;
            imp.Invoke(area);
            mInfoUpdator.Add(area, imp);
        }

        #endregion
    }
}