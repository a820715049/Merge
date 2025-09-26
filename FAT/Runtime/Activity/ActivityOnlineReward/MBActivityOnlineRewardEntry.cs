/**FileHeader
 * @Author: zhangpengjian
 * @Date: 2025/8/20 14:07:52
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/8/20 14:07:54
 * @Description: 在线奖励入口
 * @Copyright: Copyright (©)}) 2025 zhangpengjian. All rights reserved.
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;

namespace FAT
{
    public class MBActivityOnlineRewardEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject group;
        [SerializeField] private TMP_Text cd;
        [SerializeField] private GameObject redGo;

        private Action WhenCD;
        private ActivityOnlineReward _activity;

        public void Start()
        {
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        public void OnEnable()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
        }

        public void RefreshEntry(ActivityLike activity)
        {
            if (activity == null)
            {
                Visible(false);
                return;
            }
            if (activity is not ActivityOnlineReward)
            {
                Visible(false);
                return;
            }
            _activity = (ActivityOnlineReward)activity;
            var valid = _activity is { Valid: true};
            Visible(valid);
            if (!valid) return;
            RefreshCD();
            RefreshRedDot();
        }

        private void RefreshCD()
        {
            if (!group.activeSelf) return;
            var v = _activity.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
            {
                Visible(false);
            }
            redGo.SetActive(_activity.HasReward());
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }

        private void EntryClick()
        {
            _activity.Open();
        }

        private void RefreshRedDot()
        {
            redGo.SetActive(_activity.HasReward());
        }
    }
}