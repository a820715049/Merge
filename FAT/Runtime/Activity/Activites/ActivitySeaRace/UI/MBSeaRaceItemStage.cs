
using UnityEngine;
using UnityEngine.UI;
using fat.rawdata;
using System.Linq;
using System.Collections.Generic;

namespace FAT
{
    public class MBSeaRaceItemStage : MonoBehaviour
    {
        [SerializeField] private GameObject m_StateAhead;
        [SerializeField] private GameObject m_StateBehind;
        [SerializeField] private RectTransform m_RtfPos;
        [SerializeField] private Button m_BtnRewardTips;
        [SerializeField] private List<UIImageRes> m_ImgResChests;
        [SerializeField] private Animator m_Animator;

        [SerializeField] private AnimationCurve m_CurveX;
        [SerializeField] private AnimationCurve m_CurveY;
        [SerializeField] private float m_Duration = 0.915f; // 持续时间
        [SerializeField] private float m_Delay = 0f;

        private EventSeaRaceRound m_ConfigRound;
        private EventSeaMilestoneReward m_ConfigReward; // 里程碑奖励
        private bool m_IsAchieved;

        public AnimationCurve curveX => m_CurveX;
        public AnimationCurve curveY => m_CurveY;
        public float duration => m_Duration;
        public float delay => m_Delay;


        private void OnEnable()
        {
            m_BtnRewardTips.WithClickScale().FixPivot().onClick.AddListener(OnBtnRewardTipsClick);
        }

        private void OnDisable()
        {
            m_BtnRewardTips.onClick.RemoveListener(OnBtnRewardTipsClick);
        }

        public void SetData(EventSeaRaceRound round, EventSeaMilestoneReward reward, bool isAchieved)
        {
            m_ConfigRound = round;
            m_ConfigReward = reward;
            m_IsAchieved = isAchieved;
            UpdateView();
        }

        public void Complete()
        {
            m_IsAchieved = true;
            m_Animator.SetTrigger("Behind_Appear");
        }

        public void UpdateView()
        {
            UpdateChest();
            UpdateState();
        }



        public Vector3 GetPos()
        {
            return m_RtfPos.position;
        }

        public bool IsChest()
        {
            return m_ConfigReward != null;
        }

        public string GetRewardIcon()
        {
            return m_ConfigReward?.RewardIcon ?? "";
        }

        public Vector3 GetChestPos()
        {
            return m_ImgResChests[0].transform.position;
        }

        private void UpdateState()
        {
            m_Animator.SetTrigger(m_IsAchieved ? "Behind_Idle" : "Ahead_Idle");
        }

        private void UpdateChest()
        {
            var hasChestObj = m_ImgResChests != null && m_ImgResChests.Count > 0;
            var hasChestData = m_ConfigReward != null && !string.IsNullOrEmpty(m_ConfigReward.RewardIcon);
            if (hasChestObj && hasChestData) m_ImgResChests.ForEach(s => s.SetImage(m_ConfigReward.RewardIcon));
            m_BtnRewardTips.enabled = m_ConfigReward != null;
        }

        private void OnBtnRewardTipsClick()
        {
            if (m_ConfigReward == null)
            {
                return;
            }
            var list = Enumerable.ToList(m_ConfigReward.Reward.Select(s => s.ConvertToRewardConfig()));
            UIManager.Instance.OpenWindow(UIConfig.UIActivityRewardTips, m_BtnRewardTips.transform.position, 35f, list);
        }
    }
}