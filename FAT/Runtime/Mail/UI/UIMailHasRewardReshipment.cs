/*
 * @Author: yanfuxing
 * @Date: 2025-05-31 11:20:05
 */
using System.Collections;
using System.Collections.Generic;
using Config;
using EL;
using fat.conf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMailHasRewardReshipment : UIBase
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _desc;
        [SerializeField] private Transform _cellParent;
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private TextMeshProUGUI _countDownText;
        [SerializeField] private Button btnClaim;
        [SerializeField] private Button btnClaimUnable;
        private IAPLateDelivery _delivery;
        private int _ReshipmentId;
        public Activity Activity => Game.Manager.activity;
        private List<GameObject> cellList = new(); //cell列表
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
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MAIL_RESHIPMENT_CELL, _itemPrefab);
        }

        protected override void OnParse(params object[] items)
        {
            DebugEx.Info("mailReshipment OnParse Enter");
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
                DebugEx.Info("mailReshipment Popup Track is Fail");
            }
        }

        private void StartCountdown()
        {
            DebugEx.Info("mailReshipment StartCountdown Enter");
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
            if (_delivery == null)
            {
                DebugEx.Info("mail: _delivery is null");
                return;
            }

            if (_delivery.from == IAPFrom.ShopMan)
            {
                var gemTabData = (ShopTabGemData)Game.Manager.shopMan.GetShopTabData(ShopTabType.Gem);
                if (gemTabData == null)
                {
                    DebugEx.Info("ShopReshipment: gemTabData is null");
                    return;
                }
                var shopGemData = gemTabData.GemDataList.FindEx(x => x.PackId == _delivery.pack.Id);
                if (shopGemData == null)
                {
                    DebugEx.Info("ShopReshipment: shopGemData is null" + _delivery.pack.Id);
                    return;
                }
                DebugEx.Info("ShopReshipment: _deliveryPackId" + _delivery.pack.Id);
                var iapPackConfig = Data.GetIAPPack(_delivery.pack.Id);
                if (iapPackConfig != null)
                {
                    var name = I18N.Text(iapPackConfig.PackName);
                    _desc.text = I18N.FormatText("#SysComDesc1166", name);
                }
                RefreshShopReward(shopGemData.Reward.reward);
            }
            else
            {
                var (valid, id, from, _, _) = ActivityLite.InfoUnwrap(_delivery.context.ProductName);
                var id2 = valid ? (id, from) : ActivityLite.IdUnwrap((int)_delivery.context.OrderId);
                var packId = _delivery.context.PayStoreId;
                DebugEx.Info("mailReshipment is Init PackId: +" + packId);
                var iap = Game.Manager.iap;
                if (ActivityLite.Exist(id2) && iap.FindIAPPack(packId, out var p))
                {
                    DebugEx.Info("mailReshipment: get pack " + packId);
                    var iapPackConfig = Data.GetIAPPack(packId);
                    if (iapPackConfig != null)
                    {
                        var name = I18N.Text(iapPackConfig.PackName);
                        _desc.text = I18N.FormatText("#SysComDesc1166", name);
                    }
                    var AlbumActive = Game.Manager.activity.IsActive(fat.rawdata.EventType.CardAlbum);
                    var rewardList = AlbumActive ? p.RewardAlbum : p.Reward;
                    RefreshReward(rewardList);
                }
            }
        }

        private void RefreshReward(IList<string> reward)
        {
            var goods = reward;
            DebugEx.Info("mailReshipment: goods " + goods.Count);
            foreach (var rewardItem in goods)
            {
                var cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.MAIL_RESHIPMENT_CELL, _cellParent);
                cell.transform.localPosition = Vector3.zero;
                cell.transform.localScale = Vector3.one;
                cell.SetActive(true);
                var item = cell.GetComponent<UICommonItem>();
                var r = rewardItem.ConvertToRewardConfig();
                item.Refresh(r.Id, r.Count);
                DebugEx.Info("mailReshipment: reward" + r.Id + " rewardCount:" + r.Count);
                cellList.Add(cell);
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
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.MAIL_RESHIPMENT_CELL, item);
            }
            cellList.Clear();
        }

        private void SetClaimState(bool isShow)
        {
            btnClaim.gameObject.SetActive(isShow);
            btnClaimUnable.gameObject.SetActive(!isShow);
        }

        private void RefreshShopReward(List<RewardConfig> reward)
        {
            var goods = reward;
            DebugEx.Info("mailReshipment: goods " + goods.Count);
            foreach (var rewardItem in goods)
            {
                var cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.MAIL_RESHIPMENT_CELL, _cellParent);
                cell.transform.localPosition = Vector3.zero;
                cell.transform.localScale = Vector3.one;
                cell.SetActive(true);
                var item = cell.GetComponent<UICommonItem>();
                item.Refresh(rewardItem.Id, rewardItem.Count);
                DebugEx.Info("mailReshipment: reward" + rewardItem.Id + " rewardCount:" + rewardItem.Count);
                cellList.Add(cell);
            }
        }
    }

    public enum MailReshipmentMoudleType
    {
        HasReward = 1,
        NotHasReward = 2,
    }
}

