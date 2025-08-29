using System;
using DG.Tweening;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIOrderChallengeMatch : UIBase
    {
        public float delay;
        private TextMeshProUGUI _totalReward;
        private UIImageRes _rewardIcon;
        private TextMeshProUGUI _playerNum;
        private Animator _animator;
        private ActivityOrderChallenge _activity;
        private Action _invoke;

        protected override void OnCreate()
        {
            transform.Access("Content/Tips_root/Num_txt", out _totalReward);
            transform.Access("Content/Tips_root/Energy_img", out _rewardIcon);
            transform.Access("Content/NumNode/Bg/Num_Root/Num", out _playerNum);
            transform.Access("Content/NumNode/Bg/Num_Root", out _animator);
            transform.AddButton("Tap", Close);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0) _activity = items[0] as ActivityOrderChallenge;
            _invoke = items[1] as Action;
            _totalReward.text = _activity?.TotalReward.Count.ToString();
            _rewardIcon.SetImage(Game.Manager.objectMan.GetBasicConfig(_activity?.TotalReward.Id ?? 0)?.Icon);
        }

        protected override void OnPreOpen()
        {
            UIManager.Instance.Block(true);
            var num = 0;
            DOTween.To(() => num, x =>
            {
                num = x;
                _playerNum.text = num + "/" + _activity.TotalNum;
            }, _activity.TotalNum, delay).OnComplete(() =>
            {
                _animator.SetTrigger("Punch");
                UIManager.Instance.Block(false);
            });
            Game.Manager.audioMan.TriggerSound("OrderChallengeMatchNum");
            Game.Manager.audioMan.TriggerSound("OrderChallengeMatchAvatar");
        }

        protected override void OnPostClose()
        {
            _invoke?.Invoke();
        }
    }
}