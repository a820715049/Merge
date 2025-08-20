/*
 * @Author: qun.chao
 * @Date: 2025-07-25 18:34:34
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class UIClawOrderEnd : UIBase
    {
        [SerializeField] private TextMeshProUGUI txtCD;
        [SerializeField] private TextMeshProUGUI txtDesc;
        [SerializeField] private UICommonItem rewardItem;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private GameObject goConvert;

        private ActivityClawOrder _actInst;
        private RewardCommitData _reward;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(Close);
            btnConfirm.onClick.AddListener(Close);
        }

        protected override void OnParse(params object[] items)
        {
            _reward = null;
            _actInst = items[0] as ActivityClawOrder;
            if (items.Length >= 2)
            {
                _reward = items[1] as RewardCommitData;
            }
        }

        protected override void OnPreOpen()
        {
            txtDesc.SetText(EL.I18N.FormatText("#SysComDesc1429", _actInst.GetEndConvertDescParam()));
            RefreshConvert();
        }

        protected override void OnPostClose()
        {
            if (_reward != null)
            {
                UIFlyUtility.FlyReward(_reward, rewardItem.transform.position);
            }
        }

        private void RefreshConvert()
        {
            if (_reward == null)
            {
                goConvert.SetActive(false);
                return;
            }

            goConvert.SetActive(true);
            rewardItem.Refresh(_reward.rewardId, _reward.rewardCount);
        }
    }
}