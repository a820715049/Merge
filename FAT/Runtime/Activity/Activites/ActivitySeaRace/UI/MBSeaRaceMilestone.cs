using UnityEngine;
using UnityEngine.UI;
using FAT.MSG;
using EL;
using TMPro;
using System.Collections.Generic;
using fat.conf;
using System.Collections;
using DG.Tweening;
using Spine.Unity;
using Spine;
using Cysharp.Threading.Tasks;
using System;

namespace FAT
{
    public class MBSeaRaceMilestone : MBSeaRaceLayer
    {

        [SerializeField] private TextMeshProUGUI m_TxtCD;
        [SerializeField] private Button m_BtnStart;
        [SerializeField] private Image m_ImgFlag;
        [SerializeField] private MBSeaRaceItemPlayer m_Player;
        [SerializeField] private SkeletonGraphic m_SpineProgress;
        [SerializeField] private List<MBSeaRaceItemStage> m_Stages;

        public override bool ActivityIsCanClose => m_MoveCoroutine == null;
        public override string AnimatorTrigger => "Milestone";

        private Coroutine m_MoveCoroutine;

        protected override void AddListeners()
        {
            base.AddListeners();
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(OnSecond);
            m_BtnStart.WithClickScale().FixPivot().onClick.AddListener(OnBtnStartClick);
        }

        protected override void RemoveListeners()
        {
            base.RemoveListeners();
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(OnSecond);
            m_BtnStart.onClick.RemoveListener(OnBtnStartClick);
        }

        public override void Enter()
        {
            base.Enter();

            m_Player.ShowSelf();
            UpdateView();
        }

        public override void Leave()
        {
            base.Leave();
            StopMoveAni();
        }

        private void UpdateView()
        {
            if (m_Activity != null && m_Activity.Cache != null)
            {
                UpdateStage(m_Activity.Cache.GetCompletedRoundIndex());
                UpdatePlayerByRoundIndex(m_Activity.Cache.GetCompletedRoundIndex());
                UpdatePlayersToNew();
            }
        }


        private void UpdateStage(int completedRoundIndex)
        {
            var configDetail = m_Activity.confDetail;
            var roundIds = configDetail.IncludeRoundId;
            for (int i = 0; i < m_Stages.Count; i++)
            {
                var roundId = roundIds[i];
                var configRound = Data.GetEventSeaRaceRound(roundId);
                var milestoneIndex = configDetail.MilestoneScore.IndexOf(i + 1);
                var isMilestone = milestoneIndex != -1;
                var rewardId = isMilestone ? configDetail.MilestoneReward[milestoneIndex] : -1;
                var reward = isMilestone ? Data.GetEventSeaMilestoneReward(rewardId) : null;
                var isCompleted = i <= completedRoundIndex;
                var item = m_Stages[i];
                item.SetData(configRound, reward, isCompleted);
            }
            UpdateProgress(completedRoundIndex, false);
            // animGroup.talk.SetTrigger("Show");
        }

        private TrackEntry UpdateProgress(int completedRoundIndex, bool userAni)
        {
            var index = Mathf.Max(completedRoundIndex, 0);
            if (index == 0)
            {
                userAni = false;
            }
            var aniName = userAni ? $"{index - 1}to{index}" : index.ToString();
            var ani = m_SpineProgress.AnimationState.SetAnimation(0, aniName, false);
            return ani;
        }

        private void UpdatePlayerByRoundIndex(int completedRoundIndex)
        {
            var completeIndex = completedRoundIndex;
            var showIndex = Mathf.Min(completeIndex, m_Stages.Count - 1);
            var targetPos = Vector3.zero;
            if (showIndex >= 0)
            {
                targetPos = m_Stages[showIndex].GetPos();
            }
            else
            {
                targetPos = m_ImgFlag.transform.position;
                targetPos.y += m_ImgFlag.rectTransform.rect.height / 2;
            }
            m_Player.transform.position = targetPos;
            m_ImgFlag.gameObject.SetActive(showIndex >= 0);
        }

        private IEnumerator ClaimReward(int roundIndex)
        {
            m_Panel.SetUIBlock(true);
            yield return new WaitForSeconds(m_RewardDelay);
            m_Panel.SetUIBlock(false);
            var stage = m_Stages[roundIndex];
            var data = new List<RewardCommitData>();
            var iconUrl = stage.GetRewardIcon();
            var desc = I18N.Text("#SysComDesc1575");
            var chestPos = stage.GetChestPos();
            m_Activity.FillMilestoneRewards(data);
            UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, chestPos, data, iconUrl, desc);
            if (m_Activity.IsEnd)
            {
                Close();
            }
        }

        private void OnSecond()
        {
            UIUtility.CountDownFormat(m_TxtCD, m_Activity?.Countdown ?? 0);
        }

        private void OnBtnStartClick()
        {
            if (m_Activity.IsEnd)
            {
                Close();
            }
            else if (m_Activity.NeedJoinRound)
            {
                m_Panel.StartRound();
            }
            else
            {
                Close();
            }
        }

        private void UpdatePlayersToNew()
        {
            if (m_Data == null || m_Activity.Cache.GetCompletedRoundIndex() == m_Data.GetCompletedRoundIndex())
            {
                return;
            }
            // 如果已有协程在运行，先停止
            StopMoveAni();
            m_BtnStart.gameObject.SetActive(false);
            m_MoveCoroutine = StartCoroutine(MoveAni());
        }

        private IEnumerator MoveAni()
        {
            yield return new WaitForSeconds(1f);
            var completedRoundIndex = m_Data.GetCompletedRoundIndex();
            var ani = UpdateProgress(completedRoundIndex, true);
            var aniTime = ani.Animation.Duration;
            yield return new WaitForSeconds(aniTime);
            var sequence = DOTween.Sequence();
            var stage = m_Stages[completedRoundIndex];
            sequence.Append(m_Player.TweenToStageX(stage));
            sequence.Join(m_Player.TweenToStageY(stage));
            yield return sequence.WaitForCompletion();
            stage.Complete();
            yield return new WaitForSeconds(0.5f);
            m_MoveCoroutine = null;
            MoveDone();
        }

        // 用于外部中断携程
        private void StopMoveAni()
        {
            if (m_MoveCoroutine != null)
            {
                StopCoroutine(m_MoveCoroutine);
                m_MoveCoroutine = null;
            }
        }

        private void MoveDone()
        {
            m_BtnStart.gameObject.SetActive(true);
            var completedRoundIndex = m_Data.GetCompletedRoundIndex();
            UpdatePlayerByRoundIndex(completedRoundIndex);
            var stage = m_Stages[completedRoundIndex];
            if (stage.IsChest())
            {
                StartCoroutine(ClaimReward(completedRoundIndex));
            }
            else
            {
                m_Panel.CheckEnd();
            }
        }
    }
}