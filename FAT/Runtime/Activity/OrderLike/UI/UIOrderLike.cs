/*
 * @Author: qun.chao
 * @Date: 2025-03-26 18:16:48
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using Cysharp.Threading.Tasks;
using TMPro;

namespace FAT
{
    public class UIOrderLike : UIBase
    {
        [SerializeField] private UICommonProgressBar progressBar;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnClaim;
        [SerializeField] private Button btnNotReady;
        [SerializeField] private Button btnTip;
        [SerializeField] private Button btnHelp;
        [SerializeField] private GameObject goTips;
        [SerializeField] private TextProOnACircle txtTitle;
        [SerializeField] private TextMeshProUGUI txtDesc;

        private ActivityOrderLike _actInst;

        protected override void OnCreate()
        {
            transform.Access<Button>("Mask").onClick.AddListener(Close);
            btnClose.onClick.AddListener(Close);
            btnClaim.onClick.AddListener(OnBtnClaim);
            btnNotReady.onClick.AddListener(Close);
            btnTip.onClick.AddListener(() => goTips.SetActive(!goTips.activeSelf));
            btnHelp.onClick.AddListener(OnBtnHelp);
        }

        protected override void OnParse(params object[] items)
        {
            _actInst = items[0] as ActivityOrderLike;
        }

        protected override void OnPreOpen()
        {
            _actInst.MarkPopupDone();
            Refresh();
        }

        private void Refresh()
        {
            goTips.SetActive(false);

            txtTitle.SetText(I18N.Text("#SysComDesc940"));
            txtDesc.SetText(I18N.FormatText("#SysComDesc941", TextSprite.FromToken(_actInst.TokenId)));

            progressBar.ForceSetup(0, _actInst.MaxToken, _actInst.CurToken);
            btnClaim.gameObject.SetActive(_actInst.ReadyToClaim);
            btnNotReady.gameObject.SetActive(!_actInst.ReadyToClaim);
        }

        private void OnBtnClaim()
        {
            if (!_actInst.ReadyToClaim)
                return;
            TryClaimReward().Forget();
        }

        private void OnBtnHelp()
        {
            _actInst.OpenHelp();
        }

        private async UniTaskVoid TryClaimReward()
        {
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<(RewardCommitData, float)>>(out var rewards);
            if (!_actInst.TryClaimReward(rewards))
                return;
            base.Close();

            // block
            UIManager.Instance.Block(true);

            UIFlyFactory.GetFlyTarget(FlyType.OrderLikeToken, out var from);

            // 按延迟逐个发奖
            foreach (var (reward, delay) in rewards)
            {
                UniTask.Void(async () =>
                {
                    await UniTask.WaitForSeconds(delay);
                    // 播放音效
                    Game.Manager.audioMan.TriggerSound("OrderLikeRwd");
                    UIFlyUtility.FlyReward(reward, from);
                });
            }

            await UniTask.WaitForSeconds(2f);

            _actInst.OpenRoundStart();

            // unblock
            UIManager.Instance.Block(false);
        }
    }
}