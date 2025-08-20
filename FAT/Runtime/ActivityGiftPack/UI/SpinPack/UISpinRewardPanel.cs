using System;
using System.Collections.Generic;
using EL;
using UnityEngine;

namespace FAT
{
    public class UISpinRewardPanel : UIBase
    {
        public UICommonItem reward1;
        public UICommonItem reward2;
        public GameObject extraNode;
        public UICommonItem reward3;
        private List<RewardCommitData> _commonReward = new();
        private List<RewardCommitData> _extraReward = new();
        private Action _callback;
        protected override void OnCreate()
        {
            transform.AddButton("Content/btnClick", _ClickClose);
        }

        protected override void OnParse(params object[] items)
        {
            _callback = items[0] as Action;
            _commonReward.AddRange(items[1] as List<RewardCommitData>);
            _extraReward.AddRange(items[2] as List<RewardCommitData>);
        }

        protected override void OnPreOpen()
        {
            reward1.gameObject.SetActive(_commonReward.Count >= 1);
            reward2.gameObject.SetActive(_commonReward.Count >= 2);
            extraNode.gameObject.SetActive(_extraReward.Count >= 1);
            if (_commonReward.TryGetByIndex(0, out var ret1)) { reward1.Refresh(ret1.rewardId, ret1.rewardCount); }
            if (_commonReward.TryGetByIndex(1, out var ret2)) { reward2.Refresh(ret2.rewardId, ret2.rewardCount); }
            if (_extraReward.TryGetByIndex(0, out var extra)) { reward3.Refresh(extra.rewardId, extra.rewardCount); }
        }

        protected override void OnPreClose()
        {
            _commonReward.Clear();
            _extraReward.Clear();
        }

        protected override void OnPostClose()
        {
            _callback?.Invoke();
        }

        private void _ClickClose()
        {
            if (reward1.gameObject.activeSelf) { UIFlyUtility.FlyReward(_commonReward[0], reward1.transform.position); }
            if (reward2.gameObject.activeSelf) { UIFlyUtility.FlyReward(_commonReward[1], reward2.transform.position); }
            if (extraNode.gameObject.activeSelf) { UIFlyUtility.FlyReward(_extraReward[0], reward3.transform.position); }
            Close();
        }
    }
}