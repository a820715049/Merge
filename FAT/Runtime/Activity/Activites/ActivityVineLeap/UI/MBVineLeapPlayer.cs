// ===================================================
// Author: mengqc
// Date: 2025/09/09
// ===================================================

using System;
using System.Collections;
using BezierSolution;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FAT
{
    public class MBVineLeapPlayer : MonoBehaviour
    {
        private TextMeshProUGUI _nameText;
        private Animator _animator;
        private UIImageState _avatar;
        public BezierTweenCtrl tweenCtrl;
        public float moveDuration = 1f;
        public AnimationCurve moveCurve;
        public float fallDuration = 1f;
        public float fallJumpTop = 30f;
        public float fallJumpBottom = 40f;
        public float fallXRange = 50f;
        public float fallRoteRange = 20f;
        private bool _isMe;

        public void Init(bool isMe = false)
        {
            _isMe = isMe;

            // 获取组件引用
            transform.Access("Player", out _animator);
            transform.Access("Player/Icon_img", out _avatar);
            transform.Access("Player/name_txt", out _nameText);

            // 设置头像
            if (!isMe)
                _avatar.Select(Random.Range(0, 5));

            // 设置名字
            var randomLetter = (char)Random.Range('A', 'Z' + 1);
            _nameText.text = isMe ? "YOU" : randomLetter.ToString();
            _nameText.fontSizeMax = isMe ? 54 : 60;

            // 设置缩放
            transform.localScale = isMe ? Vector3.one * 1.3f : Vector3.one;
        }

        public IEnumerator MoveNext(BezierSpline spline, float delay, Action onComplete = null)
        {
            yield return new WaitForSeconds(delay);

            var bezierPoints = spline.GetComponentsInChildren<BezierPoint>();
            var startPt = bezierPoints[0];
            var endPt = bezierPoints[bezierPoints.Length - 1];
            var isJumpToRight = startPt.position.x < endPt.position.x;
            // 播放跳跃动画
            if (_animator)
            {
                var triggerName = isJumpToRight ? "Index1" : "Index2";
                _animator.SetTrigger(triggerName);
            }

            // 播放音效
            Game.Manager.audioMan.TriggerSound("VineLeapNextJump");

            tweenCtrl.spline = spline;
            tweenCtrl.value = 0;
            // 移动到目标位置
            var sequence = DOTween.Sequence();
            var moveValue = 0f;

            sequence.Append(DOTween.To(() => moveValue, (v) =>
            {
                moveValue = v;
                tweenCtrl.value = moveCurve.Evaluate(moveValue);
            }, 1, moveDuration));

            yield return sequence.WaitForCompletion();
            onComplete?.Invoke();
        }

        public IEnumerator SpawnAnime(float delay, Action onComplete = null)
        {
            gameObject.SetActive(false);
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(true);
            // 播放淘汰动画
            if (_animator)
                _animator.SetTrigger("Show");
            // 等待动画播放完成
            yield return new WaitForSeconds(2f);
            onComplete?.Invoke();
        }

        public IEnumerator EliminateAnim(float delay, Transform hideParent)
        {
            yield return new WaitForSeconds(delay);

            // 播放淘汰动画
            if (_animator)
                _animator.SetTrigger("Fall");
            var curPos = transform.localPosition;
            var sign = curPos.x < 0 ? -1 : 1;
            var seq = DOTween.Sequence();
            var upRate = 0.4f;
            seq.Insert(0, transform.DOLocalMoveY(curPos.y + fallJumpTop, upRate * fallDuration).SetEase(Ease.OutQuad));
            seq.Insert(upRate * fallDuration, transform.DOLocalMoveY(curPos.y - fallJumpBottom, (1 - upRate) * fallDuration).SetEase(Ease.InQuad));
            seq.Insert(0, transform.DOLocalMoveX(curPos.x + sign * fallXRange, fallDuration).SetEase(Ease.Linear));
            seq.Insert(0, transform.DOLocalRotate(new Vector3(0, 0, -sign * fallRoteRange), fallDuration).SetEase(Ease.Linear));
            yield return seq.WaitForCompletion();

            // 移动到隐藏节点
            transform.SetParent(hideParent);
            transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

        public void PlaySleep()
        {
            if (_animator)
                _animator.SetTrigger("Sleep");
        }

        public void PlayIdle()
        {
            if (_animator)
                _animator.SetTrigger("Idle");
        }

        public bool IsMe => _isMe;
    }
}