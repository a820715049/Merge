using UnityEngine;
using EL;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

namespace FAT
{
    public class UIOrderDiffchoice : UIBase
    {
        [Header("数据")]
        [SerializeField] private float processBackTime = 0.15f;
        [SerializeField] private float processTime = 0.15f;
        
        [Header("基础UI组件")]
        [SerializeField] private Button downBtn;
        [SerializeField] private Button upBtn;
        [SerializeField] private Image downImg;
        [SerializeField] private Image upImg;
        [SerializeField] private CanvasGroup[] backs;
        [SerializeField] private CanvasGroup[] frames;
        [SerializeField] private Button[] btns;
        [SerializeField] private Image process;
        [SerializeField] private Sprite[] btnSprites;

        private int _index = 0;

        protected override void OnCreate()
        {
            downBtn.WithClickScale().FixPivot().onClick.AddListener(OnClickDown);
            upBtn.WithClickScale().FixPivot().onClick.AddListener(OnClickUp);
            foreach (var btn in btns)
            {
                btn.WithClickScale().FixPivot().onClick.AddListener(OnComplete);
            }
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            
            _index = 0;
            ChangeIndex(0, false);
        }

        private void ChangeIndex(int value, bool animate = true)
        {
            _index += value;
            backs[_index].transform.SetSiblingIndex(3);
            frames[_index].transform.SetSiblingIndex(3);
            
            DOTween.Kill("UIOrderProcess");
            for (int i = 0; i < 3; i++)
            {
                if (animate)
                {
                    DOTween.Sequence()
                        .AppendInterval(_index == i ? 0 : processBackTime)
                        .Append(backs[i].DOFade(_index == i ? 1f : 0f, processBackTime)).SetLink(gameObject).SetId("UIOrderProcess");
                    DOTween.Sequence()
                        .AppendInterval(_index == i ? 0 : processBackTime)
                        .Append(frames[i].DOFade(_index == i ? 1f : 0f, processBackTime)).SetLink(gameObject).SetId("UIOrderProcess");
                    backs[i].alpha = _index == i ? 0f : 1f;
                    frames[i].alpha = _index == i ? 0f : 1f;
                }
                else
                {
                    backs[i].alpha = _index == i ? 1f : 0f;
                    frames[i].alpha = _index == i ? 1f : 0f;
                }
            }

            if (animate)
            {
                process.DOFillAmount((_index + 1) / 3f, processTime).SetLink(gameObject).SetId("UIOrderProcess");
            }
            else
            {
                process.fillAmount = (_index + 1) / 3f;
            }
            RefreshBtnActive();
        }

        private void RefreshBtnActive()
        {
            downBtn.interactable = _index != 0;
            upBtn.interactable = _index != 2;
            downImg.sprite = btnSprites[(_index != 0 ? 0 : 1) + 2];
            upImg.sprite = btnSprites[_index != 2 ? 0 : 1];
        }

        private void OnComplete()
        {
            Close();
            Debug.Log($"UIOrderDiffchoice OnComplete {_index}");
        }

        private void OnClickDown()
        {
            ChangeIndex(-1);
        }

        private void OnClickUp()
        {
            ChangeIndex(1);
        }
    }
}