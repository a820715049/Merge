/*
 * @Author: qun.chao
 * @Date: 2024-01-05 20:05:15
 */
using UnityEngine;
using TMPro;
using EL;

namespace FAT.Merge
{
    public class MBBoardOrderBox : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private UIImageRes boxIcon;
        [SerializeField] private UIImageRes rewardIcon;
        [SerializeField] private TextMeshProUGUI txtBoxDuration;
        [SerializeField] private TextMeshProUGUI txtRewardNum;

        private int mOrderId;

        public void Init()
        {
            boxIcon.transform.AddButton(null, _OnBtnPreview);
        }

        public void Setup()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondPass);
        }

        public void Cleanup()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondPass);
        }

        public bool TryShowBox(int orderId)
        {
            if (BoardViewWrapper.TryGetOrderBoxDetail(orderId, out var totalMilli, out var countMilli, out var detail))
            {
                mOrderId = orderId;
                UIUtility.CountDownFormat(txtBoxDuration, (totalMilli - countMilli) / 1000);
                boxIcon.SetImage(detail.Icon);
                return true;
            }
            return false;
        }

        public void ShowReward(RewardCommitData reward)
        {
            var res = Game.Manager.rewardMan.GetRewardIcon(reward.rewardId, reward.rewardCount);
            rewardIcon.SetImage(res);
            txtRewardNum.text = $"{reward.rewardCount}";
        }

        public void PlayOpenBoxEffect()
        {
            animator.SetTrigger("Die");
        }

        private void _RefreshCountdown()
        {
            if (BoardViewWrapper.TryGetOrderBoxDetail(mOrderId, out var totalMilli, out var countMilli, out _))
            {
                UIUtility.CountDownFormat(txtBoxDuration, (totalMilli - countMilli) / 1000);
            }
            else
            {
                txtBoxDuration.text = string.Empty;
            }
        }

        private void _OnSecondPass()
        {
            if (!gameObject.activeSelf)
                return;
            _RefreshCountdown();
        }

        private void _OnBtnPreview()
        {
            //显示订单礼盒tips 气泡的箭头需要指向道具Icon中心
            UIManager.Instance.OpenWindow(UIConfig.UIOrderBoxTips, boxIcon.transform.position, mOrderId);
        }
    }
}