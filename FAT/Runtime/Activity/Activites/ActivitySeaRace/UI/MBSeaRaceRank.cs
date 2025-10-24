using System.Collections.Generic;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using System.Collections;
using DG.Tweening;

namespace FAT
{
    public class MBSeaRaceRank : MBSeaRaceLayer
    {
        [SerializeField] private MapButton m_BtnInfo;
        [SerializeField] private MapButton m_BtnClose;
        [SerializeField] private MapButton m_BtnGo;
        [SerializeField] private TextMeshProUGUI m_TxtTarget;
        [SerializeField] private TextMeshProUGUI m_TxtDesc;
        [SerializeField] private List<MBSeaRaceItemStageRank> m_Stages;
        [SerializeField] private List<MBSeaRaceItemStageRank> m_StageRanks;
        [SerializeField] private List<MBSeaRaceChest> m_Chests;
        [SerializeField] private List<MBSeaRaceItemPlayer> m_Players;

        public override bool ActivityIsCanClose => m_MoveCoroutine == null;
        public override string AnimatorTrigger => "Rank";

        private EventSeaRaceRound m_RoundConfig => GetRoundConfig();
        private int m_TargetNum => GetTargetNum();
        private int m_StageNum => m_Activity.confDetail.StageNum;

        private Coroutine m_MoveCoroutine;
        private List<Sequence> m_TweenSeqs = new();
        private bool m_InMoveingHasChange = false; // 移动过程中分数是否发生了变化
        private bool m_SelfHasStageChange = false; // 自己的阶段是否发生了变化


        protected override void AddListeners()
        {
            base.AddListeners();

            m_BtnGo.WithClickScale().FixPivot().WhenClick = OnBtnGoClick;
            m_BtnInfo.WithClickScale().FixPivot().WhenClick = OnBtnInfoClick;
            m_BtnClose.WithClickScale().FixPivot().WhenClick = OnBtnCloseClick;

            MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLOSE>().AddListener(ChangeToMilestoneState);
            MessageCenter.Get<MSG.SEA_RACE_ROBOT_ADD_ONLINE_SCORE>().AddListener(InfoChange);
            MessageCenter.Get<MSG.UI_SEA_RACE_SCORE_CHANGE>().AddListener(InfoChange);
        }

        protected override void RemoveListeners()
        {
            base.RemoveListeners();

            m_BtnGo.WhenClick = null;
            m_BtnInfo.WhenClick = null;
            m_BtnClose.WhenClick = null;

            MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLOSE>().RemoveListener(ChangeToMilestoneState);
            MessageCenter.Get<MSG.SEA_RACE_ROBOT_ADD_ONLINE_SCORE>().RemoveListener(InfoChange);
            MessageCenter.Get<MSG.UI_SEA_RACE_SCORE_CHANGE>().RemoveListener(InfoChange);
        }

        public override void Enter()
        {
            base.Enter();

            DebugData();
            PreDealData();
            UpdateView();
        }

        public override void Leave()
        {
            base.Leave();
            StopMoveAni();
        }

        public override void OnOpened()
        {
            base.OnOpened();
            UpdatePlayersToNew();
        }

        private void DebugData()
        {
            /* foreach (var item in m_Activity.Cache.Infos)
            {
                item.Score = 0;
            } */

            /* var i = 1;
            foreach (var item in m_Data.Infos)
            {
                item.Score = m_TargetNum - i * 10;
                i++;
            } */

            // m_Data.Infos[0].Score = m_TargetNum;
        }

        private void PreDealData()
        {
            // 排行和排序数据初始使用最新
            if (m_Data != null)
            {
                foreach (var info in m_Activity.Cache.Infos)
                {
                    var infoNew = m_Data.GetPlayerInfo(info.Uid);
                    info.SetRank(infoNew.GetRank());
                }
                m_Activity.Cache.Infos.Sort((a, b) => m_Data.Infos.IndexOf(a).CompareTo(m_Data.Infos.IndexOf(b)));
            }
        }

        private EventSeaRaceRound GetRoundConfig()
        {
            var roundConfig = m_Data != null ? m_Data.RoundConfig : m_Activity.GetConfRound();
            if (roundConfig == null)
            {
                roundConfig = m_Activity.GetConfRound(0);
            }
            return roundConfig;
        }

        private int GetTargetNum()
        {
            var roundConfig = GetRoundConfig();
            return m_Activity.GetCurRoundTarget(roundConfig);
        }

        private int GetStageByScore(int score)
        {
            if (score < 0 || score >= m_TargetNum)
            {
                return m_Stages.Count;
            }
            return Mathf.Max(Mathf.CeilToInt((score * (m_StageNum - 1) * 1f) / m_TargetNum) + 1, 1);
        }

        private void UpdateView()
        {
            UpdateTxt();
            InAniStartState();
            UpdateChests();
            UpdatePlayersByData(m_Activity.Cache.Infos);
        }

        private void UpdateTxt()
        {
            m_TxtTarget.SetText(m_TargetNum.ToString());
            m_TxtDesc.SetText(I18N.FormatText("#SysComDesc1679", m_TargetNum));
        }

        private void UpdateChests()
        {
            var rewardIds = m_RoundConfig.RoundReward;
            for (int i = 0; i < m_Chests.Count; i++)
            {
                if (i < rewardIds.Count)
                {
                    m_Chests[i].SetData(rewardIds[i], i == 0);
                }
                else
                {
                    m_Chests[i].SetData(0, false);
                }
            }
        }

        private void UpdatePlayersByData(List<SeaRacePlayerInfo> infos, bool useTween = false)
        {

            m_Stages.ForEach(stage => stage.ClearPlayers());
            m_StageRanks.ForEach(stage => stage.ClearPlayers());
            m_Players.ForEach(player => player.BindStage(null));

            for (int i = 0; i < m_Players.Count; i++)
            {
                var player_item = m_Players[i];
                var player_data = infos[i];
                var player_rank = player_data.Score < 0 ? Mathf.Abs(player_data.Score) : 0;
                var player_stage = GetStageByScore(player_data.Score);
                var stage_new = m_Stages[player_stage - 1];

                if (player_rank > 0)
                {
                    stage_new = m_StageRanks[player_rank - 1];
                    var chest = m_Chests[player_rank - 1];
                    chest.Visible(false);
                }

                player_item.SetData(player_data);
                player_item.UpdateAll();
                player_item.BindStage(stage_new);
                stage_new.AddPlayer(player_item, useTween);
            }
        }

        private void UpdatePlayersToNew()
        {
            if (m_Data == null)
            {
                InAniEndState();
                return;
            }
            StopMoveAni();
            InAniStartState();
            m_MoveCoroutine = StartCoroutine(MoveAni());
        }

        private void HasStageChange(out bool hasChange, out bool hasSelf)
        {
            hasChange = false;
            hasSelf = false;

            var cacheOld = m_Activity.Cache;
            var cacheNew = m_Data;

            if (cacheNew == null)
            {
                return;
            }

            foreach (var infoOld in cacheOld.Infos)
            {
                var infoNew = cacheNew.GetPlayerInfo(infoOld.Uid);
                var stageOld = GetStageByScore(infoOld.Score);
                var stageNew = GetStageByScore(infoNew.Score);
                if (stageNew > stageOld)
                {
                    hasChange = true;
                    if (infoOld.Uid == cacheOld.PlayerInfo.Uid)
                    {
                        hasSelf = true;
                    }
                }
            }
        }

        private void InAniStartState()
        {
            HasStageChange(out var hasChange, out var m_SelfHasStageChange);
            m_BtnGo.gameObject.SetActive(!(hasChange && m_SelfHasStageChange));
        }

        private void InAniEndState()
        {
            m_BtnGo.gameObject.SetActive(true);
            m_SelfHasStageChange = false;
        }

        private IEnumerator MoveAni()
        {
            var changeIndex = -1;
            for (int i = 0; i < m_Players.Count; i++)
            {
                var player = m_Players[i];
                var dataOld = player.GetData();
                var dataNew = m_Data.GetPlayerInfo(dataOld.Uid);
                var stageNum = GetStageByScore(dataNew.Score);
                var rank = dataNew.Score < 0 ? Mathf.Abs(dataNew.Score) : 0;
                var scoreOld = dataOld.Score >= 0 ? dataOld.Score : m_TargetNum;
                var scoreNew = dataNew.Score >= 0 ? dataNew.Score : m_TargetNum;

                MBSeaRaceItemStageRank endStage = null;//最终的阶段

                var scoreChangeTime = 0f;

                //阶段变化
                var stage_new = GetStageByScore(dataNew.Score);
                var stage_old = GetStageByScore(dataOld.Score);
                if (stage_new != stage_old || (dataOld.Score > 0 && dataNew.Score < 0))
                {
                    changeIndex++;
                    var addDelay = changeIndex * player.addMoveDelay;

                    var stageIndex_new = stage_new - 1;
                    var stageIndex_old = stage_old - 1;

                    var index = 0;
                    var sequence = DOTween.Sequence();
                    m_TweenSeqs.Add(sequence);
                    for (var j = stageIndex_old + 1; j <= stageIndex_new; j++)
                    {
                        var stage = m_Stages[j];
                        var isToNew = j == stageIndex_new && rank == 0;
                        index = j - stageIndex_old - 1;
                        sequence.Append(player.TweenToStageX(stage, index, isToNew, addDelay));
                        sequence.Join(player.TweenToStageY(stage, index, isToNew, addDelay));
                        endStage = stage;
                        addDelay = 0;

                        scoreChangeTime += addDelay + stage.duration + stage.delay;
                    }

                    if (rank > 0)
                    {
                        var stageRank = m_StageRanks[rank - 1];
                        index++;
                        sequence.Append(player.TweenToStageX(stageRank, index, true, addDelay, true));
                        sequence.Join(player.TweenToStageY(stageRank, index, true, addDelay));
                        endStage = stageRank;

                        scoreChangeTime += addDelay + stageRank.duration + stageRank.delay;
                    }

                    endStage?.AddPlayer(player);
                    sequence.OnStart(() =>
                    {
                        player.BindStage(null);
                        endStage?.AddPlayer(player);
                    });
                    sequence.AppendCallback(() =>
                    {
                        if (endStage != null)
                        {
                            player.BindStage(endStage);
                            endStage?.AddPlayer(player, true);
                        }
                        m_TweenSeqs.Remove(sequence);
                        if (rank > 0)
                        {
                            if (player.IsSelf())
                            {
                                ForceCompleteTween();
                            }
                            player.HideScore();
                            var chest = m_Chests[rank - 1];
                            // chest.Visible(false);
                            chest.Occupy();
                            m_Activity.TriggerSound("Reward");
                        }
                    });
                }

                //积分动画
                if (scoreOld != scoreNew)
                {
                    if (scoreChangeTime < 0.1f)
                    {
                        scoreChangeTime = 1f;
                    }
                    var sequenceScore = DOTween.Sequence();
                    m_TweenSeqs.Add(sequenceScore);
                    sequenceScore.Append(player.TweenToScore(scoreNew, scoreChangeTime));
                    sequenceScore.AppendCallback(() =>
                    {
                        m_TweenSeqs.Remove(sequenceScore);
                    });
                }

            }
            yield return new WaitUntil(() => m_TweenSeqs.Count == 0);
            m_MoveCoroutine = null;
            StartCoroutine(MoveDone(false));
        }

        private void ForceCompleteTween()
        {
            foreach (var seq in m_TweenSeqs)
            {
                seq.Kill(true);
            }
            m_TweenSeqs.Clear();
        }


        // 用于外部中断携程
        private void StopMoveAni()
        {
            if (m_MoveCoroutine != null)
            {
                StopCoroutine(m_MoveCoroutine);
                m_MoveCoroutine = null;
            }
            ForceCompleteTween();
        }

        private IEnumerator MoveDone(bool isJump)
        {
            InAniEndState();
            if (isJump)
            {
                m_InMoveingHasChange = false;
                SetData(new SeaRaceCache(m_Activity).Cache());
                UpdatePlayersByData(m_Data.Infos, !isJump);
            }
            else
            {
                UpdatePlayersByData(m_Data.Infos, !isJump);
                if (m_InMoveingHasChange)
                {
                    UpdateViewAgain();
                    yield break;
                }
            }

            if (m_Data.IsRoundFinish)
            {
                var score = m_Data.PlayerInfo.Score;

                if (score >= 0)//没有名次则失败
                {
                    m_Activity.OpenFail(ChangeToMilestoneState);
                }
                else //有名次则打开奖励
                {
                    if (!isJump)
                    {
                        m_Panel.SetUIBlock(true);
                        yield return new WaitForSeconds(m_RewardDelay);
                        m_Panel.SetUIBlock(false);
                    }
                    var rank = Mathf.Abs(score);
                    var chest = m_Chests[rank - 1];
                    var data = new List<RewardCommitData>();
                    var iconUrl = chest.GetRewardIcon();
                    var desc = I18N.FormatText("#SysComDesc1683", rank);
                    var closeTip = I18N.Text("#SysComDesc943");
                    m_Activity.FillRankRewards(data);
                    UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, chest.transform.position, data, iconUrl, desc, closeTip);
                }

                m_InMoveingHasChange = false;
            }
            else
            {
                m_Panel.CheckEnd();
            }
        }

        private void OnBtnInfoClick()
        {
            m_Activity.OpenHelp();
        }

        private void OnBtnCloseClick()
        {
            OnCloseCheck();
        }

        private void OnBtnGoClick()
        {
            OnCloseCheck();
        }

        private void JumpMoveAni()
        {
            StopMoveAni();
            UpdatePlayersByData(m_Data.Infos);
            StartCoroutine(MoveDone(true));
        }

        private void OnCloseCheck()
        {
            if (m_MoveCoroutine != null)
            {
                JumpMoveAni();
                if (!m_SelfHasStageChange && !m_Activity.IsRoundFinish)
                {
                    Close();
                }
            }
            else
            {
                Close();
            }
        }

        private void ChangeToMilestoneState()
        {
            m_Panel.ChangeToState(ActivitySeaRace.SeaRaceUIState.Milestone);
        }

        private void InfoChange()
        {
            if (m_MoveCoroutine != null || m_Panel.IsInOpen)
            {
                m_InMoveingHasChange = true;
                return;
            }
            UpdateViewAgain();
        }

        private void UpdateViewAgain()
        {
            if (m_Data != null)
            {
                m_Activity.Cache.Paste(m_Data);
            }
            m_InMoveingHasChange = false;
            SetData(new SeaRaceCache(m_Activity).Cache());
            UpdatePlayersToNew();
        }
    }
}