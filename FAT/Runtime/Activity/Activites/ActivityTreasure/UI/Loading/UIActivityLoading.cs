/*
 * @Author: qun.chao
 * @Date: 2024-04-22 11:42:21
 */
using System;
using System.Collections;
using UnityEngine;
using EL;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityLoading : UIBase
    {
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private MBLoadingSlice sliceComp;
        [SerializeField] private Animator animatorLogo;
        [SerializeField] private float logoDuration = 2f;
        public UIVisualGroup visualGroup;

        private SimpleAsyncTask waitFadeInEnd = null;
        private SimpleAsyncTask waitFadeOutEnd = null;
        private SimpleAsyncTask waitLoadingJobFinish = null;
        private ActivityVisual visual;

        protected override void OnParse(params object[] items)
        {
            waitLoadingJobFinish = items[0] as SimpleAsyncTask;
            waitFadeInEnd = items[1] as SimpleAsyncTask;
            waitFadeOutEnd = items[2] as SimpleAsyncTask;
            if (items.Length > 3) visual = (ActivityVisual)items[3];
        }

        protected override void OnPreOpen()
        {
            sliceComp.Refresh();
            RefreshTheme();
            StartCoroutine(_CoLoading());
        }

        public void RefreshTheme() {
            if (visual == null || visualGroup == null) return;
            visual.Refresh(visualGroup);
        }

        private void _FadeInProgress(float p)
        {
            sliceComp.loadingProgress = p;
        }

        private void _FadeOutProgress(float p)
        {
            sliceComp.loadingProgress = p;
        }

        private IEnumerator _CoLoading()
        {
            bool isTweening = false;
            var p = 0f;
            var fadeInEase = Ease.InCirc;
            var fadeOutEase = Ease.InCirc;

            // fade in
            sliceComp.SetInvert(false);
            p = 0f;
            isTweening = true;
            DOTween.To(() => p, x => p = x, 1f, fadeInDuration).OnUpdate(() => _FadeInProgress(p)).SetEase(fadeInEase).OnComplete(() => { isTweening = false; });
            animatorLogo.SetTrigger("Show");

            yield return new WaitUntil(() => !isTweening);

            waitFadeInEnd.ResolveTaskSuccess();

            var logoEnd = Time.timeSinceLevelLoad + logoDuration;

            yield return waitLoadingJobFinish;

            yield return new WaitUntil(() => Time.timeSinceLevelLoad > logoEnd);

            // fade out
            sliceComp.SetInvert(true);
            p = 1f;
            isTweening = true;
            DOTween.To(() => p, x => p = x, 0f, fadeOutDuration).OnUpdate(() => _FadeOutProgress(p)).SetEase(fadeOutEase).OnComplete(() => { isTweening = false; });
            animatorLogo.SetTrigger("Hide");

            yield return new WaitUntil(() => !isTweening);

            waitFadeOutEnd.ResolveTaskSuccess();

            // close
            base.Close();
        }

        public static void Open(UIResource ui_, Action afterFadeIn = null, Action afterFadeOut = null, ActivityVisual visual_ = null)
        {
            IEnumerator L() {
                var waitFadeInEnd = new SimpleAsyncTask();
                var waitFadeOutEnd = new SimpleAsyncTask();
                var waitLoadingJobFinish = new SimpleAsyncTask();
                //复用寻宝loading音效
                Game.Manager.audioMan.TriggerSound("UnderseaTreasure");
                
                ui_.Open(waitLoadingJobFinish, waitFadeInEnd, waitFadeOutEnd, visual_);

                yield return waitFadeInEnd;

                afterFadeIn?.Invoke();

                waitLoadingJobFinish.ResolveTaskSuccess();

                yield return waitFadeOutEnd;

                afterFadeOut?.Invoke();
            }
            Game.Instance.StartCoroutineGlobal(L());
        }
    }
}