using UnityEngine;

namespace FAT
{
    public class MBSeaRaceLayer : MonoBehaviour
    {
        [SerializeField] protected float m_RewardDelay = 1.5f;
        protected ActivitySeaRace m_Activity { get; private set; }
        protected UISeaRacePanel m_Panel { get; private set; }
        protected ActivitySeaRace.SeaRaceUIState m_State { get; private set; }
        protected SeaRaceCache m_Data { get; private set; }

        public virtual bool ActivityIsCanClose => true;
        public virtual string AnimatorTrigger => "";

        public void Init(ActivitySeaRace activity, UISeaRacePanel panel, ActivitySeaRace.SeaRaceUIState state, SeaRaceCache data)
        {
            m_Activity = activity;
            m_Panel = panel;
            m_State = state;
            m_Data = data;
        }

        public void SetData(SeaRaceCache data)
        {
            m_Data = data;
        }

        public void Clear()
        {
            m_Activity = null!;
            m_Panel = null!;
            m_State = ActivitySeaRace.SeaRaceUIState.None;
        }

        public virtual void Enter()
        { }

        public virtual void Leave()
        { }

        public virtual void OnEnable()
        {
            AddListeners();
        }

        public virtual void OnDisable()
        {
            RemoveListeners();
        }

        public virtual void OnOpened()
        { }

        protected virtual void AddListeners()
        { }

        protected virtual void RemoveListeners()
        { }

        protected void Close()
        {
            var act = m_Activity;
            m_Panel?.Close();
            if (act.IsEnd)
            {
                act.OpenEnd(true);
            }
        }
    }
}