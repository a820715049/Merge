using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBOrderChallengePlayer : MonoBehaviour
    {
        private TextMeshProUGUI _name;
        private Animator _animator;
        private UIImageState _avatar;
        public Vector3 _targetPos;
        private bool _isMe;
        private string _anim;

        public void Init(bool me = false)
        {
            _isMe = me;
            transform.Access("Player", out _animator);
            transform.Access("Player/Icon_img", out _avatar);
            if (!me) _avatar.Select(Random.Range(0, 5));
            transform.Access("Player/name_txt", out _name);
            var randomLetter = (char)Random.Range('A', 'Z' + 1);
            _name.text = me ? "YOU" : randomLetter.ToString();
            _name.fontSizeMax = me ? 54 : 60;
            transform.localScale = me ? Vector3.one * 1.3f : Vector3.one;
        }

        public void SetTarget(Vector3 pos)
        {
            _targetPos = pos;
        }

        public void SetAnim(int index)
        {
            _anim = "Index" + index;
        }

        public IEnumerator MoveNext(MBOrderChallengeStand stand, float delay)
        {
            yield return new WaitForSeconds(delay);
            var seq = DOTween.Sequence();
            seq.Append(transform.DOLocalMoveX(_targetPos.x, stand.duration).SetEase(stand.curveX));
            seq.Join(transform.DOLocalMoveY(_targetPos.y, stand.duration).SetEase(stand.CurveY));
            _animator.SetTrigger(_anim);
        }

        public IEnumerator FailAnim(float delay, Transform node)
        {
            yield return new WaitForSeconds(delay);
            _animator.SetTrigger("fail");
            if (_isMe) Game.Manager.audioMan.TriggerSound("OrderChallengeLoseOnly");
            yield return new WaitForSeconds(2.7f);
            transform.SetParent(node);
        }
    }
}