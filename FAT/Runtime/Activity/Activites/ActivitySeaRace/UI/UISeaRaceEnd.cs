using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UISeaRaceEnd : UISeaRaceBase
    {
        [SerializeField] private MapButton m_BtnClose;
        [SerializeField] private List<MBSeaRaceChest> m_Chests;
        [SerializeField] private List<RectTransform> m_Rebuilds;

        protected override void OnCreate()
        {
            m_BtnClose.WithClickScale().FixPivot().WhenClick = OnBtnCloseClick;
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            SetBlock(true);
            UpdateView();
        }

        protected override void OnPostClose()
        {
            SetBlock(false);
            base.OnPostClose();
        }

        protected override void OnEnd()
        { }

        private void UpdateView()
        {
            if (m_Activity == null)
            {
                return;
            }

            var rewardIds = m_Activity.HistoryRewardIds;
            for (int i = 0; i < m_Chests.Count; i++)
            {
                var chest = m_Chests[i];
                var show = i < rewardIds.Count;
                chest.gameObject.SetActive(show);
                if (show)
                {
                    chest.Interactable(false);
                    chest.SetData(rewardIds[i], false);
                }
            }

            for (int i = 0; i < m_Rebuilds.Count; i++)
            {
                var rebuild = m_Rebuilds[i];
                LayoutRebuilder.ForceRebuildLayoutImmediate(rebuild);
            }
        }


        private void OnBtnCloseClick()
        {
            Close();
        }

    }
}