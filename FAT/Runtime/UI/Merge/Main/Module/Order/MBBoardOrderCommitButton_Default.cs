/*
 * @Author: qun.chao
 * @Date: 2025-01-17 14:34:42
 */
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBBoardOrderCommitButton_Default : MonoBehaviour, MBBoardOrder.ICommitButton
    {
        [Tooltip("完成按钮位置 默认y")] [SerializeField]
        private float finishButtonY_Default = 34;
        [Tooltip("完成按钮位置 额外奖励y")] [SerializeField]
        private float finishButtonY_ExtraReward = 84;
        [SerializeField] private Button btnFinish;
        [SerializeField] private Button btnFinishOld;

        Button MBBoardOrder.ICommitButton.BtnCommit => UIUtility.ABTest_OrderItemChecker() ? btnFinish : btnFinishOld;
        private Button btnCommit => (this as MBBoardOrder.ICommitButton).BtnCommit;
        private IOrderData order;

        void MBBoardOrder.ICommitButton.OnDataChange(IOrderData data)
        {
            order = data;
        }

        void MBBoardOrder.ICommitButton.OnDataClear()
        {
            order = null;
        }

        void MBBoardOrder.ICommitButton.Refresh()
        {
            btnCommit.gameObject.SetActive(order.State == OrderState.Finished);
        }

        void MBBoardOrder.ICommitButton.RefreshOffset(bool isExtraReward)
        {
            var pos = (btnCommit.transform as RectTransform).anchoredPosition;
            if (isExtraReward)
            {
                pos.Set(pos.x, finishButtonY_ExtraReward);
            }
            else
            {
                pos.Set(pos.x, finishButtonY_Default);
            }
            (btnCommit.transform as RectTransform).anchoredPosition = pos;
        }

    }
}
