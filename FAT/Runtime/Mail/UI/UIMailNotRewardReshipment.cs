/*
 * @Author: yanfuxing
 * @Date: 2025-05-31 11:30:01
 */
using System.Collections;
using EL;
using fat.conf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMailNotRewardReshipment : UIBase
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _desc;
        [SerializeField] private TextMeshProUGUI _countDownText;
        [SerializeField] private Button btnClaim;
        [SerializeField] private Button btnClaimUnable;
        private IAPLateDelivery _delivery;
        private int _ReshipmentId;
        public Activity Activity => Game.Manager.activity;
        private Coroutine _countdownCoroutine;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Panel/Top/BtnClose", base.Close);
            btnClaim.onClick.AddListener(() =>
            {
                base.Close();
                if (_delivery != null)
                {
                    DataTracker.iap_reshipment_claim.Track(_delivery.pack.Id, _delivery.product.ProductId, _delivery.pack.TgaName);
                }
                else
                {
                    DebugEx.Info("mailReshipment claim Track is Fail");
                }
            });
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                if (items[0] is ReshipmentData data)
                {
                    _delivery = data.Delivery;
                    _ReshipmentId = data.ReshipmentId;
                    DebugEx.Info($"mailReshipment ProductName:{_delivery.context.ProductName} _ReshipmentId: {_ReshipmentId}");
                }
            }
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            InitPanel();
            StartCountdown();
            if (_delivery != null)
            {
                DataTracker.iap_reshipment_popup.Track(_delivery.pack.Id, _delivery.product.ProductId, _delivery.pack.TgaName);
            }
            else
            {
                DebugEx.Info("mailReshipment Track is Fail");
            }
        }

        private void StartCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
            }
            _countdownCoroutine = StartCoroutine(CountdownCoroutine());
        }

        private IEnumerator CountdownCoroutine()
        {
            for (int i = 3; i > 0; i--)
            {
                _countDownText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }
            _countDownText.text = "0";
            OnCountdownComplete();
        }

        private void OnCountdownComplete()
        {
            DebugEx.Info("Countdown completed!");
            SetClaimState(true);
        }

        private void InitPanel()
        {
            SetClaimState(false);
            var reshipmentConfig = Data.GetReshipment(_ReshipmentId);

            if (reshipmentConfig == null)
            {
                DebugEx.Info("mail: reshipmentConfig is null");
                return;
            }
            var packId = _delivery.context.PayStoreId;
            var iapPackConfig = Data.GetIAPPack(packId);
            if (iapPackConfig != null)
            {
                var name = I18N.Text(iapPackConfig.PackName);
                _desc.text = I18N.FormatText("#SysComDesc1167", name);
            }
        }

        protected override void OnPostClose()
        {
            base.OnPostClose();
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
        }

        private void SetClaimState(bool isShow)
        {
            btnClaim.gameObject.SetActive(isShow);
            btnClaimUnable.gameObject.SetActive(!isShow);
        }
    }
}
   
