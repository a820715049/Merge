using UnityEngine;
using fat.rawdata;
using fat.conf;
using UnityEngine.UI;
using System.Linq;

namespace FAT
{
    public class MBSeaRaceChest : MonoBehaviour
    {
        [SerializeField] private UIImageRes m_ImgResChest;
        [SerializeField] private Button m_BtnChestTips;
        [SerializeField] private RectTransform m_RtfPos;
        [SerializeField] private Animator m_Animator;
        [SerializeField] private bool m_UpdateChest = true;

        private EventSeaRaceReward m_ConfigReward;
        private bool m_On1st;

        private void OnEnable()
        {
            m_BtnChestTips.WithClickScale().FixPivot().onClick.AddListener(OnBtnChestTipsClick);
        }

        private void OnDisable()
        {
            m_BtnChestTips.onClick.RemoveListener(OnBtnChestTipsClick);
        }

        public void SetData(int rewardId, bool on1st)
        {
            m_ConfigReward = Data.GetEventSeaRaceReward(rewardId);
            m_On1st = on1st;
            Visible(true);
            UpdateView();
        }

        public void Visible(bool visible)
        {
            m_ImgResChest.gameObject.SetActive(visible);
        }

        public void Interactable(bool interactable)
        {
            m_BtnChestTips.interactable = interactable;
        }

        public void Occupy()
        {
            m_Animator.SetTrigger("Disappear");
        }

        private void UpdateView()
        {
            if (m_ConfigReward != null && !string.IsNullOrEmpty(m_ConfigReward.Icon) && m_UpdateChest)
            {
                m_ImgResChest.SetImage(m_ConfigReward.Icon);
            }
        }

        private void OnBtnChestTipsClick()
        {
            if (m_ConfigReward == null)
            {
                return;
            }
            var pos = m_RtfPos.position;
            if (m_On1st)
            {
                var list = Enumerable.ToList(m_ConfigReward.Reward.Select(s => s.ConvertToRewardConfig()));
                UIManager.Instance.OpenWindow(UIConfig.UIActivityRewardTips, pos, 0f, list);
            }
            else
            {
                UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, pos, 0f, m_ConfigReward.Reward);
            }
        }

        public string GetRewardIcon()
        {
            return m_ConfigReward?.Icon ?? "";
        }

    }
}