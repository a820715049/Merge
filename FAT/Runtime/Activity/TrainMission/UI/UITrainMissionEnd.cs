/**
 * @Author: lizhenpeng
 * @Date: 2025/8/6 11:25:09
 * @LastEditors: lizhenpeng
 * @LastEditTime: 2025/8/6 11:25:09
 * Description: 火车棋盘结束界面情况1+2
 */

using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UITrainMissionEnd : UIBase
    {
        private TrainMissionActivity _activity;

        [SerializeField] private TextMeshProUGUI Title;

        // 文本
        [SerializeField] private GameObject EndGroup1; // 对应只有标题的组
        [SerializeField] private GameObject EndGroup2; // 对应奖励 + 描述组

        [SerializeField] private TextMeshProUGUI Title1; // EndGroup1 下的标题
        [SerializeField] private TextMeshProUGUI Title2; // EndGroup2 的主标题
        [SerializeField] private TextMeshProUGUI Desc3;
        [SerializeField] private TextMeshProUGUI TextConfirm;


        // 奖励容器中的物品展示组件 默认只有一种奖励
        [SerializeField] private UICommonItem reward;

        // 按钮
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnConfirm;

        private RewardCommitData _result;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(OnClickClose);
            btnConfirm.onClick.AddListener(OnClickConfirm);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2) return;

            _activity = items[0] as TrainMissionActivity;


            _result = _activity.recycleReward;
        }

        private void RefreshReward()
        {
            if (_result == null)
            {
                return;
            }

            reward.Setup();
            reward.Refresh(_result.rewardId, _result.rewardCount);
        }

        protected override void OnPreOpen()
        {
            if (_activity == null)
                return;

            RefreshUI();
            RefreshReward();
        }

        private void RefreshUI()
        {
            bool isWithReward = !_activity.Active && _result != null;

            Title.SetText(I18N.Text("#SysComDesc1538"));

            EndGroup1.SetActive(!isWithReward); // 情况一：只显示大标题
            EndGroup2.SetActive(isWithReward); // 情况二：显示详细内容

            if (isWithReward)
            {
                Title2.text = I18N.Text("#SysComDesc273"); // EVENT ENDED
                Desc3.text = I18N.Text("#SysComDesc1553"); // 描述文本
                TextConfirm.text = I18N.Text("#SysComDesc525"); // claim
            }
            else
            {
                Title1.text = I18N.Text("#SysComDesc273"); // EVENT ENDED
                TextConfirm.text = I18N.Text("#SysComBtn3"); // OK
            }
        }

        private void HandleConfirmLogic()
        {
            bool isWithReward = !_activity.Active && _result != null;

            if (isWithReward)
            {
                // 界面2
                UIFlyUtility.FlyReward(_result, reward.transform.position);
                Close();
            }
            else
            {
                // 界面1
                Close();
            }
        }


        private void OnClickConfirm()
        {
            HandleConfirmLogic();
        }

        private void OnClickClose()
        {
            HandleConfirmLogic();
        }

        protected override void OnPreClose()
        {
            _result = null;
        }
    }
}