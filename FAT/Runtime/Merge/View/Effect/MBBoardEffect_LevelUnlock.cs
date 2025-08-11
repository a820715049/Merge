/*
 * @Author: qun.chao
 * @Date: 2023-11-04 11:05:15
 */

using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Text;

namespace FAT
{
    public class MBBoardEffect_LevelUnlock : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI txtLevel;
        [SerializeField] private Button btn;
        [SerializeField] private float delay;

        private bool hasPlaySound;
        private Action onEffectFinish;

        private void Awake()
        {
            btn.onClick.AddListener(_OnBtnClick);
        }

        private void OnEnable()
        {
            hasPlaySound = false;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _TryResolveOnEffectFinishCallback();
        }

        public void Setup(int level, Action cb)
        {
            onEffectFinish = cb;
            txtLevel.SetText(level);
            StartCoroutine(CoPlaySound());
        }

        private IEnumerator CoPlaySound()
        {
            yield return new WaitForSeconds(delay);
            TryPlaySandUnlockSound();
        }
        
        private void _OnBtnClick()
        {
            // // 尝试提前release
            // if (TryGetComponent(out MBAutoRelease rel))
            // {
            //     rel.lifeTime = -1f;
            // }
            StopAllCoroutines();
            TryPlaySandUnlockSound();
            if (transform.GetChild(0).TryGetComponent(out Animator animator))
            {
                animator.SetTrigger("Die");
            }
        }

        private void TryPlaySandUnlockSound()
        {
            if (hasPlaySound)
                return;
            Game.Manager.audioMan.TriggerSound("BoardSandUnlock");
            hasPlaySound = true;
            // 音效播放和特效结束的时机基本匹配
            _TryResolveOnEffectFinishCallback();
        }

        private void _TryResolveOnEffectFinishCallback()
        {
            onEffectFinish?.Invoke();
            onEffectFinish = null;
        }
    }
}