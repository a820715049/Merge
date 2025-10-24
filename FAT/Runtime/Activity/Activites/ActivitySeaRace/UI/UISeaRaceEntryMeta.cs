/*
 * ============================
 * File: UISeaRaceEntryMeta.cs
 * Author: jiangkun.dai
 * Date: 2025-09-03 16:17:00
 * Desc: 海上竞速 Meta入口
 * ============================
*/

using UnityEngine;
using TMPro;
using EL;
using FAT.MSG;
using System.Collections.Generic;

namespace FAT
{
    public class UISeaRaceEntryMeta : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_TxtRank;
        [SerializeField] private List<GameObject> m_WaitShowList = new();//处于等待中显示的对象列表
        [SerializeField] private List<GameObject> m_StartShowList = new();//处于开始中显示的对象列表

        private ActivitySeaRace m_Activity;

        public void OnEnable()
        {
            MessageCenter.Get<UI_SEA_RACE_ENTRY_UPDATE>().AddListener(UpdateView);
            MessageCenter.Get<UI_SEA_RACE_SCORE_CHANGE>().AddListener(UpdateView);
            MessageCenter.Get<SEA_RACE_ROBOT_ADD_ONLINE_SCORE>().AddListener(UpdateView);
            UpdateView();
        }

        public void OnDisable()
        {
            MessageCenter.Get<UI_SEA_RACE_ENTRY_UPDATE>().RemoveListener(UpdateView);
            MessageCenter.Get<UI_SEA_RACE_SCORE_CHANGE>().RemoveListener(UpdateView);
            MessageCenter.Get<SEA_RACE_ROBOT_ADD_ONLINE_SCORE>().RemoveListener(UpdateView);
        }

        public void SetData(ActivitySeaRace activity)
        {
            m_Activity = activity;
            UpdateView();
        }

        private void UpdateView()
        {
            RefreshState();
            RefreshRank();
        }

        private void RefreshState()
        {
            var inStart = m_Activity?.IsRoundStart == true;

            m_WaitShowList.ForEach(item => item.SetActive(!inStart));
            m_StartShowList.ForEach(item => item.SetActive(inStart));
        }

        private void RefreshRank()
        {
            if (m_Activity != null && m_TxtRank != null)
            {
                var hasRank = m_Activity.TryGetUserRank(out var rank);
                m_TxtRank.gameObject.SetActive(hasRank);
                if (hasRank) m_TxtRank.text = rank.ToString();
            }
        }

    }
}