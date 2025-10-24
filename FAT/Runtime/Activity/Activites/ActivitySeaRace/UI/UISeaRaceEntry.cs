/*
 * ============================
 * File: UISeaRaceEntry.cs
 * Author: jiangkun.dai
 * Date: 2025-09-03 16:17:00
 * Desc: 海上竞速 入口
 * ============================
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using FAT.MSG;
using DG.Tweening;

namespace FAT
{
    public class UISeaRaceEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private Button m_BtnSelf;
        [SerializeField] private Button m_BtnStart;
        [SerializeField] private UICommonProgressBar m_ProgressBar;
        [SerializeField] private TextMeshProUGUI m_TxtCD;
        [SerializeField] private TextMeshProUGUI m_TxtRank;
        [SerializeField] private TextMeshProUGUI m_TxtAddNum;
        [SerializeField] private Image m_ImgRankBg;
        [SerializeField] private Animator m_Animator;
        [SerializeField] private Animator m_AnimatorAddNum;

        private ActivitySeaRace m_Activity;

        public void Start()
        {
            m_BtnSelf.WithClickScale().FixPivot().onClick.AddListener(OnClickEntry);
            m_BtnStart.WithClickScale().FixPivot().onClick.AddListener(OnClickStart);
        }

        public void OnEnable()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<UI_SEA_RACE_ENTRY_UPDATE>().AddListener(UpdateView);
            // MessageCenter.Get<UI_SEA_RACE_SCORE_CHANGE>().AddListener(OnScoreChange);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(OnScoreChange);
            MessageCenter.Get<SEA_RACE_ROBOT_ADD_ONLINE_SCORE>().AddListener(RefreshRank);
            UpdateView();
        }

        public void OnDisable()
        {
            m_AnimatorAddNum.gameObject.SetActive(false);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<UI_SEA_RACE_ENTRY_UPDATE>().RemoveListener(UpdateView);
            // MessageCenter.Get<UI_SEA_RACE_SCORE_CHANGE>().RemoveListener(OnScoreChange);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(OnScoreChange);
            MessageCenter.Get<SEA_RACE_ROBOT_ADD_ONLINE_SCORE>().RemoveListener(RefreshRank);
        }

        private void UpdateView()
        {
            RefreshState();
            RefreshCD();
            RefreshProgress(false);
            RefreshRank();
        }

        private void RefreshState()
        {
            m_BtnStart.gameObject.SetActive(m_Activity?.NeedJoinRound ?? false);
            m_ProgressBar.gameObject.SetActive(m_Activity?.IsRoundStart ?? false);
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(m_TxtCD, m_Activity?.Countdown ?? 0);
        }

        private void RefreshProgress(bool needAni)
        {
            if (m_Activity != null)
            {
                var numTotal = m_Activity.GetCurRoundTarget();
                var numCurr = m_Activity.Score;
                if (needAni)
                {
                    m_ProgressBar.SetProgress(numCurr);
                    m_Animator.SetTrigger("Punch");
                }
                else
                {
                    m_ProgressBar.ForceSetup(0, numTotal, numCurr);
                }
            }
        }

        private void OnScoreChange(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.Coin)
            {
                return;
            }
            var cur = m_ProgressBar.Cur;
            var target = m_Activity.Score;
            var num = target - cur;
            if (num >= 1f)
            {
                m_TxtAddNum.DOKill();
                m_TxtAddNum.DOFade(1f, 0.01f).SetDelay(0.15f).OnComplete(() =>
                {
                    m_TxtAddNum.text = "+" + Mathf.FloorToInt(num).ToString();
                    m_AnimatorAddNum.gameObject.SetActive(true);
                    m_AnimatorAddNum.SetTrigger("Punch");
                    RefreshProgress(true);
                    m_Activity.TriggerSound("Increase");
                });
            }
        }

        private void RefreshRank()
        {
            if (m_Activity != null)
            {
                var hasRank = m_Activity.TryGetUserRank(out var rank);
                m_TxtRank.gameObject.SetActive(hasRank);
                m_ImgRankBg.gameObject.SetActive(hasRank);
                if (hasRank) m_TxtRank.text = rank.ToString();
            }
        }

        public void RefreshEntry(ActivityLike activity)
        {
            m_Activity = activity as ActivitySeaRace;
            UpdateView();
        }

        private void OnClickEntry()
        {
            m_Activity?.Open();
        }

        private void OnClickStart()
        {
            OnClickEntry();
        }

    }
}