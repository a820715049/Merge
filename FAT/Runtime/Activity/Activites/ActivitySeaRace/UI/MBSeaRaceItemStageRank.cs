using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Coffee.UIExtensions;

namespace FAT
{
    public class MBSeaRaceItemStageRank : MonoBehaviour
    {
        [SerializeField] private UIParticle m_Particle;
        [SerializeField] private AnimationCurve m_CurveX;
        [SerializeField] private AnimationCurve m_CurveY;

        [SerializeField] private float m_Duration = 0.915f; // 持续时间
        [SerializeField] private float m_Delay = 0f; // 延迟

        [SerializeField] private List<RectTransform> m_RtfPoss;

        private List<MBSeaRaceItemPlayer> m_Players = new();

        public AnimationCurve curveX => m_CurveX;
        public AnimationCurve curveY => m_CurveY;
        public float duration => m_Duration;
        public float delay => m_Delay;

        private void OnDisable()
        {
            m_Players.Clear();
        }

        private void OnEnable()
        {
            m_Particle.gameObject.SetActive(false);
        }

        public Vector3 GetPos(int index)
        {
            return m_RtfPoss[index].position;
        }

        public Vector3 GetNextPos()
        {
            return GetPos(m_Players.Count);
        }

        public void AddPlayer(MBSeaRaceItemPlayer player, bool userTween)
        {
            if (m_Players.Contains(player))
            {
                return;
            }
            m_Players.Add(player);
            UpdatePlayerPos(userTween);
        }

        public void AddPlayer(MBSeaRaceItemPlayer player)
        {
            m_Players.Add(player);
        }

        public void OutStage(MBSeaRaceItemPlayer player)
        {
            if (m_Players.Contains(player))
            {
                m_Players.Remove(player);
                UpdatePlayerPos();
            }
        }

        public void ClearPlayers()
        {
            m_Players.Clear();
        }

        public void PlayParticle()
        {
            m_Particle.gameObject.SetActive(true);
            m_Particle.Play();
        }

        private void UpdatePlayerPos(bool userTween = false)
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                var pos = GetPos(i);
                p.transform.DOKill(true);
                if (userTween)
                {
                    p.transform.DOMove(pos, 0.1f);
                }
                else
                {
                    p.transform.position = GetPos(i);
                }
            }
        }


    }
}