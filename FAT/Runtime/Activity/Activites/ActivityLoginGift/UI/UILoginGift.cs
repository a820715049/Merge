/*
 *@Author:chaoran.zhang
 *@Desc:登入赠品界面逻辑
 *@Created Time:2024.03.21 星期四 18:41:57
 */

using System.Collections.Generic;
using Config;
using EL;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UILoginGift : UIBase
    {
        private TextProOnACircle _title;
        private MBRewardLayout _rewardLayout;
        private List<RewardConfig> _rewardConfigList = new List<RewardConfig>();
        private LoginGiftActivity _activity;
        private List<RewardCommitData> _reward = new List<RewardCommitData>();
        [SerializeField] private SkeletonGraphic _spineL; //左侧气球
        [SerializeField] private SkeletonGraphic _spineR; //右侧气球

        protected override void OnCreate()
        {
            _title = transform.Find("Content/Panel/Title").GetComponent<TextProOnACircle>();
            _rewardLayout = transform.Find("Content/Panel/_group").GetComponent<MBRewardLayout>();
            transform.AddButton("Content/Panel/BtnConfirm", _ClickConfirm);
        }

        protected override void OnPreOpen()
        {
            _spineR.AnimationState.SetAnimation(0, "appear", false).Complete += delegate(TrackEntry entry)
            {
                _spineR.AnimationState.SetAnimation(0, "idle", true);
            };
            _spineL.AnimationState.SetAnimation(0, "appear", false).Complete += delegate(TrackEntry entry)
            {
                _spineL.AnimationState.SetAnimation(0, "idle", true);
            };
            _rewardLayout.gameObject.SetActive(true);
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as LoginGiftActivity;
            _rewardConfigList.Clear();
            foreach (var str in _activity.confD.Reward)
            {
                _rewardConfigList.Add((str.ConvertToRewardConfig()));
            }
            _rewardLayout.Refresh(_rewardConfigList);
            _activity.Visual.Refresh(_title, "mainTitle");
            //_rewardLayout.gameObject.SetActive(false);
        }

        protected override void OnPreClose()
        {
            _spineL.AnimationState.SetAnimation(0, "disappear", false);
            _spineR.AnimationState.SetAnimation(0, "disappear", false);
        }

        private void _ClickConfirm()
        {
            base.Close();
            _reward.Clear();
            foreach (var r in _rewardConfigList)
            {
                _reward.Add(Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.login_gift));
            }
            
            for (var i = 0; i < _reward.Count; i++)
            {
                UIFlyUtility.FlyReward(_reward[i], _rewardLayout.list[i].icon.transform.position);
            }

            Game.Manager.activity.EndImmediate(_activity, false);
        }
    }
}