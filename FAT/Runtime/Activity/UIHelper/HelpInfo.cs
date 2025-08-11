using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using static fat.conf.Data;
using fat.rawdata;
using FAT.MSG;
using DG.Tweening;

namespace FAT {
    using static MessageCenter;

    public class HelpInfo : MonoBehaviour {
        public float infoPopupDelay = 1.2f;
        public float infoPopupDuration = 0.25f;
        public float infoFadeDuration = 0.1f;
        public float autoHideDelay = 0f;
        public bool autoTipsClear;

        public MapButton info;
        public MapButton mask;
        public GameObject help;
        private WaitForSeconds wait;
        private Coroutine hide;

        public void OnValidate() {
            if (Application.isPlaying) return;
            var root = transform;
            info = root.FindEx<MapButton>("button");
            mask = root.FindEx<MapButton>("mask");
            help = root.Find("group")?.gameObject;
        }

        public void Start() {
            if (help == null) return;
            info.WithClickScale().FixPivot().WhenClick = InfoClick;
            mask.WhenClick = Hide;
            Visible(false);
        }

        public void RefreshActive() {
            if (help == null) return;
            Visible(false);
            if (infoPopupDelay >= 0) {
                wait ??= new WaitForSeconds(infoPopupDelay);
                IEnumerator InfoPopup() {
                    yield return wait;
                    Active();
                }
                StartCoroutine(InfoPopup());
            }
            else {
                Active();
            }
        }

        public void InfoClick() {
            Active();
        }

        public void Visible(bool v_) {
            mask.gameObject.SetActive(v_);
            help.SetActive(v_);
            info.image?.Enabled(!v_);
        }

        public void Hide() {
            var r = help.transform;
            r.localScale = Vector3.one;
            r.DOScale(Vector3.one * 0.1f, infoFadeDuration).SetEase(Ease.OutFlash)
            .OnComplete(() => {
                Visible(false);
            });
        }

        public void Active() {
            Visible(true);
            var r = help.transform;
            r.localScale = Vector3.one * 0.1f;
            r.DOScale(Vector3.one, infoPopupDuration).SetEase(Ease.OutBack);
            if (hide != null) {
                StopCoroutine(hide);
                hide = null;
            }
            if (autoHideDelay > 0) {
                IEnumerator AutoHide() {
                    yield return new WaitForSeconds(autoHideDelay);
                    Hide();
                    if (autoTipsClear) UIItemUtility.HideTips();
                }
                hide = StartCoroutine(AutoHide());
            }
        }
    }
}