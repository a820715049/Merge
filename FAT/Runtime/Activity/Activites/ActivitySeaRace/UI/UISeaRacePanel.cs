using System.Collections.Generic;
using EL;
using UnityEngine;
using UnityEngine.UI.Extensions;
using System.Collections;
using System.Linq;

namespace FAT
{
    public class UISeaRacePanel : UISeaRaceBase
    {
        [SerializeField] private NonDrawingGraphic m_Block;
        [SerializeField] private MBSeaRaceRank m_LayerRank;
        [SerializeField] private MBSeaRaceMilestone m_LayerMilestone;
        [SerializeField] private Animator m_Animator;
        [SerializeField] private List<GameObject> m_LayerContentRank;
        [SerializeField] private List<GameObject> m_LayerContentMilestone;

        private SeaRaceCache m_Data;
        private ActivitySeaRace.SeaRaceUIState m_State = ActivitySeaRace.SeaRaceUIState.Milestone;
        private Dictionary<ActivitySeaRace.SeaRaceUIState, List<GameObject>> m_LayerContents = new();
        private Dictionary<ActivitySeaRace.SeaRaceUIState, MBSeaRaceLayer> m_Layers = new();

        private bool m_InWaitChange = false;

        private MBSeaRaceLayer m_CurrLayer => m_Layers.TryGetValue(m_State, out var layer) ? layer : null!;

        public bool IsRankOpen => m_State == ActivitySeaRace.SeaRaceUIState.Rank;
        public bool IsMilestoneOpen => m_State == ActivitySeaRace.SeaRaceUIState.Milestone;


        #region  ui base

        protected override void OnCreate()
        {
            m_LayerContents.Add(ActivitySeaRace.SeaRaceUIState.Rank, m_LayerContentRank);
            m_LayerContents.Add(ActivitySeaRace.SeaRaceUIState.Milestone, m_LayerContentMilestone);

            m_Layers.Add(ActivitySeaRace.SeaRaceUIState.Rank, m_LayerRank);
            m_Layers.Add(ActivitySeaRace.SeaRaceUIState.Milestone, m_LayerMilestone);
        }

        protected override void OnParse(params object[] items)
        {
            var list = items[1] as List<object>;
            var param = list.ToArray<object>();
            base.OnParse(param);
            m_State = param.Length > 1 ? (ActivitySeaRace.SeaRaceUIState)param[1] : ActivitySeaRace.SeaRaceUIState.Milestone;
            m_Data = param.Length > 2 ? (SeaRaceCache)param[2] : null;

            UpdateShowObj();
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            m_Activity._finishRoundCoroutine = null;
            MessageCenter.Get<MSG.UI_SIMPLE_ANIM_FINISH>().AddListener(OnAnimPlayEnd);
            SetBlock(true);
            SetUIBlock(false);
            m_CurrLayer?.Enter();
            ChangeAnimator(m_CurrLayer.AnimatorTrigger, false);
        }

        protected override void OnPostOpen()
        {
            base.OnPostOpen();
            m_CurrLayer?.OnOpened();
            CheckEnd();
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.UI_SIMPLE_ANIM_FINISH>().RemoveListener(OnAnimPlayEnd);
            base.OnPreClose();
            m_Activity.RefreshCache();
        }

        protected override void OnPostClose()
        {
            var activity = m_Activity;
            base.OnPostClose();
            SetBlock(false);
            m_CurrLayer?.Leave();
            m_Data = null;
        }

        protected override void OnEnd()
        {
            if (IsInOpen)
            {
                return;
            }
            if (m_CurrLayer == null || m_CurrLayer.ActivityIsCanClose)
            {
                base.OnEnd();
            }
        }

        #endregion

        #region  ui event

        public void StartRound()
        {
            var result = m_Activity.StartRound();
            if (!result)
            {
                Close();
                return;
            }
            m_Activity.RefreshCache();
            m_Data = null;
            foreach (var layer in m_Layers)
            {
                layer.Value.SetData(m_Data);
            }
            ChangeToState(ActivitySeaRace.SeaRaceUIState.Rank);
        }

        #endregion

        public void ChangeToState(ActivitySeaRace.SeaRaceUIState state)
        {
            StartCoroutine(ChangeToStateCoroutine(state));
        }

        public IEnumerator ChangeToStateCoroutine(ActivitySeaRace.SeaRaceUIState state)
        {
            var oldLayer = m_CurrLayer;
            var newLayer = m_Layers[state];

            m_InWaitChange = true;
            SetUIBlock(true);
            ChangeAnimator(newLayer.AnimatorTrigger, true);
            yield return new WaitUntil(() => !m_InWaitChange);
            SetUIBlock(false);

            oldLayer?.Leave();

            m_State = state;
            UpdateShowObj();

            newLayer.Enter();
        }

        private void UpdateShowObj()
        {
            foreach (var layer in m_Layers)
            {
                layer.Value.Init(m_Activity, this, layer.Key, m_Data);

                foreach (var content in m_LayerContents[layer.Key])
                {
                    content.SetActive(layer.Key == m_State);
                }
            }
        }

        public void ChangeAnimator(string trigger, bool useAppear)
        {
            var name = useAppear ? $"{trigger}_Appear" : $"{trigger}_Idle";
            m_Animator.SetTrigger(name);
        }

        public void SetUIBlock(bool value)
        {
            m_Block.raycastTarget = value;
        }

        private void OnAnimPlayEnd(AnimatorStateInfo stateInfo)
        {
            foreach (var layer in m_Layers)
            {
                if (stateInfo.IsName($"UISeaRacePanel_{layer.Key}_Ready"))
                {
                    m_InWaitChange = false;
                    return;
                }
            }
        }
    }
}