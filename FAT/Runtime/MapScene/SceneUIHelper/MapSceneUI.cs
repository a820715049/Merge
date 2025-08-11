using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;
using DG.Tweening;

namespace FAT {
    public abstract class MapSceneUI : MonoBehaviour {
        internal float fadeInDuration = 0.25f;
        internal float fadeOutDuration = 0.1f;
        private static readonly float fadeSmall = 0.1f;
        public bool Active => gameObject.activeSelf;
        public Action<MapSceneUI> WhenClose { get; set; }
        private Vector3 scale = Vector3.one;
        internal bool concluded;

        public virtual void Init() {}

        public void Scale(float vCur, float vRef, float limit_ = 0.2f) {
            var f = Mathf.Max((vCur == 0 || vRef == 0) ? 1 : (vCur / vRef), limit_);
            transform.localScale = scale = Vector3.one * f;
        }

        public void Open(float delay_ = 0) {
            concluded = false;
            WillOpen();
            Game.Manager.audioMan.TriggerSound("OpenWindow");
            gameObject.SetActive(true);
            DOTween.Kill(this);
            transform.localScale = scale * fadeSmall;
            transform.DOScale(scale, fadeInDuration)
                .SetDelay(delay_ + 0.1f)
                .SetEase(Ease.OutBack)
                .OnComplete(WhenOpen)
                .SetId(this);
        }
        internal virtual void WillOpen() {}
        internal virtual void WhenOpen() {}

        public virtual void Clear() => Close();
        public void Close() => Close(0);
        public void Close(float delay_) {
            if (!Active) return;
            DOTween.Kill(this);
            WillClose();
            WillCloseInternal();
            transform.DOScale(scale * fadeSmall, fadeOutDuration)
                .SetDelay(delay_)
                .SetEase(Ease.OutFlash)
                .OnComplete(CloseImmediate)
                .SetId(this);
        }
        public void CloseImmediate() {
            WillCloseInternal();
            gameObject.SetActive(false);
        }
        private void WillCloseInternal() {
            WhenClose?.Invoke(this);
            WhenClose = null;
        }
        internal virtual void WillClose() {}

        internal void Block(bool v_ = true) {
            var uiMgr = UIManager.Instance;
            uiMgr.Block(v_);
        }

        public void Visible(bool v_) {
            gameObject.SetActive(v_);
        }

        internal bool TryConclude() {
            if (concluded) return false;
            return concluded = true;
        }
    }
}