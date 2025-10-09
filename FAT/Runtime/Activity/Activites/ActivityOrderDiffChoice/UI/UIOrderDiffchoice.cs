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
        [SerializeField] private float processBtnTime = 0.15f;
        
        [Header("基础UI组件")]
        [SerializeField] private Button downBtn;
        [SerializeField] private Button upBtn;
        [SerializeField] private CanvasGroup downCg;
        [SerializeField] private CanvasGroup upCg;
        [SerializeField] private CanvasGroup[] backs;
        [SerializeField] private CanvasGroup[] frames;
        [SerializeField] private Button[] btns;
        [SerializeField] private Image process;
        [SerializeField] private GameObject effect;
        [SerializeField] private GameObject hardEffect;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private Animator watchAnim;

        private ActivityOrderDiffChoice _activity;
        private Action _whenCd;                             // 倒计时回调
        private int _index = 0;

        protected override void OnCreate()
        {
            downBtn.WithClickScale().FixPivot().onClick.AddListener(OnClickDown);
            upBtn.WithClickScale().FixPivot().onClick.AddListener(OnClickUp);
            foreach (var btn in btns)
            {
                btn.WithClickScale().FixPivot().onClick.AddListener(OnComplete);
            }
            effect.SetActive(false);
            descText.text = I18N.FormatText("#SysComDesc1771", new object[] { "<sprite name=selfselect_plus>", "<sprite name=selfselect_reduce>" });
        }

        protected override void OnParse(params object[] items)
        {
            _activity = (ActivityOrderDiffChoice)items[0];
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            RefreshCd();
            
            _index = 0;
            ChangeIndex(0, false);
        }

        protected override void OnAddListener()
        {
            _whenCd ??= RefreshCd;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_whenCd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_whenCd);
        }

        private void RefreshCd()
        {
            var t = Game.Instance.GetTimestampSeconds();
            var diff = _activity.endShowTS - t;
            if (diff <= 0)
                Close();
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
            
            downBtn.interactable = _index != 0;
            upBtn.interactable = _index != 2;

            if (animate)
            {
                downCg.DOFade(_index != 0 ? 0 : 1, processBtnTime).SetLink(gameObject).SetId("UIOrderProcess");
                upCg.DOFade(_index != 2 ? 0 : 1, processBtnTime).SetLink(gameObject).SetId("UIOrderProcess");
            }
            else
            {
                downCg.alpha = _index != 0 ? 0 : 1;
                upCg.alpha = _index != 2 ? 0 : 1;
            }
        }

        private void OnComplete()
        {
            _activity.TryChoiceDiff((ActivityOrderDiffChoice.DiffChoiceType)(_index + 1));
            Close();
            Debug.Log($"UIOrderDiffchoice OnComplete {_index}");
        }

        private void OnClickDown()
        {
            ChangeIndex(-1);
            effect.SetActive(false);
            effect.SetActive(true);
        }

        private void OnClickUp()
        {
            ChangeIndex(1);
            effect.SetActive(false);
            effect.SetActive(true);
            hardEffect.SetActive(false);
            hardEffect.SetActive(true);
            watchAnim.Play("Punch");
        }
    }
}