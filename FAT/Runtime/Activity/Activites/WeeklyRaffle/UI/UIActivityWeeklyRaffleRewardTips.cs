// ==================================================
// File: UIActivityWeeklyRaffleRewardTips.cs
// Author: liyueran
// Date: 2025-06-03 19:06:14
// Desc: $签到抽奖 大奖Tip
// ==================================================

using EL;
using FAT.MSG;
using UnityEngine;

namespace FAT
{
    public class UIActivityWeeklyRaffleRewardTips : UITipsBase
    {
        [SerializeField] private UICommonItem _item;
        [SerializeField] private Transform itemRoot;
        private ActivityWeeklyRaffle _activity;

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 3) return;

            // items[0] Vector3 位置
            // items[1] float 偏移参数
            _SetTipsPosInfo(items);


            _activity = (ActivityWeeklyRaffle)(items[2]);
        }

        protected override void OnPreOpen()
        {
            InitRewardItems();
            // 刷新tips位置
            _RefreshTipsPos(18);
        }

        private void InitRewardItems()
        {
            _activity.HandleRewardPool(out _, out var jackpot);
            if (jackpot == null)
            {
                return;
            }

            for (int i = 0; i < jackpot.Reward.Count; i++)
            {
                var reward = jackpot.Reward[i];
                var (cfgID, cfgCount, _) = reward.ConvertToInt3();

                var item = itemRoot.childCount > i + 1
                    ? itemRoot.GetChild(i + 1).GetComponent<UICommonItem>()
                    : Instantiate(_item, itemRoot);
                item.gameObject.SetActive(true);
                item.GetComponent<UICommonItem>().Setup();
                item.Refresh(cfgID, cfgCount);
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }


        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        protected override void OnPostClose()
        {
            ReleaseItems();
        }

        private void ReleaseItems()
        {
            for (int i = 0; i < itemRoot.childCount; i++)
            {
                itemRoot.GetChild(i).gameObject.SetActive(false);
            }
        }


        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != _activity)
            {
                return;
            }

            Close();
        }
    }
}