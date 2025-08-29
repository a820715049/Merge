using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EL;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FAT
{
    public class MBOrderChallengeStand : MonoBehaviour
    {
        public AnimationCurve curveX;
        public AnimationCurve CurveY;
        public int indexStand;
        public float interval;
        public float delay;
        public float duration;
        public float sub;
        public List<float> paramX;
        public List<float> paramY;
        private Animator _animator;
        private RectTransform _standPos;
        public float width => _width;
        public float height => _height;
        private float _width;
        private float _height;
        private readonly List<Vector3> _posList = new();
        private Button _btn;

        public Vector3 PlayerPos => transform.localPosition + Vector3.down * _standPos.rect.height / 3;

        public void Init()
        {
            _animator = transform.GetComponent<Animator>();
            _standPos = transform.GetChild(0).transform as RectTransform;
            transform.Access("Stand_img/Btn", out _btn);
            _width = _standPos.rect.width;
            _height = _standPos.rect.height;
        }

        public Vector3 GetPosByIndex(int index)
        {
            return _posList[index];
        }

        public void RandomPos(int index, bool start)
        {
            _posList.Clear();
            var rect = _standPos.rect;
            var listX = new List<float>();
            var listY = new List<float>();
            if (index <= 14)
            {
                for (var i = 0; i < index; i++)
                {
                    var x = start
                        ? Random.Range(0, rect.width * 1 / 4)
                        : Random.Range(0, rect.width * 1 / 3);
                    listX.Add(x);
                    var y = Random.Range(-rect.height / 4, rect.height / 4);
                    listY.Add(y);
                }

                listX.Sort((a, b) => b.CompareTo(a));
                var abs = false;
                for (var i = 0; i < listX.Count; i++)
                {
                    listX[i] *= abs ? 1 : -1;
                    abs = !abs;
                    _posList.Add(transform.localPosition + new Vector3(listX[i], listY[i], 0));
                }

                _posList.Sort((a, b) => b.y.CompareTo(a.y));
            }
            else
            {
                var pos = transform.localPosition;
                for (var i = 0; i < 14; i++)
                    _posList.Add(pos + new Vector3(rect.width * paramX[i], rect.height * paramY[i]));
            }
        }

        public IEnumerator PlayHide(float delay, bool needSound = true)
        {
            yield return new WaitForSeconds(delay);
            _animator.SetTrigger("Hide");
            if (indexStand >= 0 && needSound)
                Game.Manager.audioMan.TriggerSound("OrderChallengeOut");
        }

        public void SetAnim(bool show)
        {
            _animator.SetBool("Show", show);
        }

        public void AddClick(Action<int, MBOrderChallengeStand> action)
        {
            if (transform.GetChild(0).childCount > 0)
                transform.AddButton("Stand_img/Btn", () => action(indexStand, this));
        }
    }
}