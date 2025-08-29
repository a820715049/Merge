/*
 * @Author: qun.chao
 * @Date: 2024-11-12 10:11:17
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    public class UIPackGemThreeForOneItem : MonoBehaviour
    {
        [SerializeField] private Button btnBuyNormal;
        [SerializeField] private Button btnBuyIAP;
        [SerializeField] private UIImageRes bg;
        [SerializeField] private UIImageRes icon;
        [SerializeField] private TextMeshProUGUI txtCount;
        [SerializeField] private GameObject goSale;
        [SerializeField] private GameObject goDone;

        private int CurIdx => transform.GetSiblingIndex();
        private PackGemThreeForOne _pack;

        private void Awake()
        {
            btnBuyNormal.onClick.AddListener(OnBtnBuyNormal);
            btnBuyIAP.onClick.AddListener(OnBtnBuyIAP);
        }

        public void InitOnPreOpen(PackGemThreeForOne pack)
        {
            _pack = pack;
            goDone.SetActive(false);
        }

        public void RefreshTheme()
        {
            var visual = _pack.Visual;
            visual.Refresh(bg, "itemBg");
            visual.Refresh(txtCount, "itemCount");
        }

        public void RefreshPack()
        {
            gameObject.SetActive(CurIdx < _pack.PackCount);
            goSale.SetActive(CurIdx + 1 == _pack.PackCount);
            var pd = GetCurrencyPack(CurIdx);
            if (pd == null) return;
            if (pd.CoinType == CoinType.Iapcoin)
            {
                RefreshReward(GetIAPPack(pd.Price).Reward);
            }
            else
            {
                RefreshReward(pd.Reward);
            }
        }

        private void RefreshReward(IList<string> rewards)
        {
            var reward = rewards[0].ConvertToRewardConfig();
            var iconRes = Game.Manager.rewardMan.GetRewardIcon(reward.Id, reward.Count);
            icon.SetImage(iconRes);
            txtCount.text = $"{reward.Count}";
        }

        public void RefreshPrice()
        {
            var cp = GetCurrencyPack(CurIdx);
            if (cp == null) return;
            var btnI = btnBuyIAP;
            var btnN = btnBuyNormal;
            if (cp.CoinType == CoinType.Iapcoin)
            {
                btnN.gameObject.SetActive(false);
                btnI.gameObject.SetActive(true);
                SetIapButton(cp, btnI);
            }
            else
            {
                btnN.gameObject.SetActive(true);
                btnI.gameObject.SetActive(false);
                SetNormalButton(cp, btnN);
            }
        }

        private void SetNormalButton(CurrencyPack cp, Button btn)
        {
            btn.interactable = true;
            GameUIUtility.SetDefaultShader(btn.image);
            var conf = Game.Manager.coinMan.GetConfigByCoinType(cp.CoinType);
            if (conf != null)
                btn.Access<UIImageRes>("Icon").SetImage(conf.Image);
            btn.Access<TextMeshProUGUI>("Num").text = $"{cp.Price}";
        }

        private void SetIapButton(CurrencyPack cp, Button btn)
        {
            var valid = _pack.GetIAPPack(cp.Price, out var iapPack);
            btn.Access<UITextState>("Num").Enabled(valid, Game.Manager.iap.PriceInfo(iapPack?.IapId ?? 0));
            if (valid)
            {
                btn.interactable = true;
                GameUIUtility.SetDefaultShader(btn.image);
            }
            else
            {
                btn.interactable = false;
                GameUIUtility.SetGrayShader(btn.image);
            }
        }

        private CurrencyPack GetCurrencyPack(int idx)
        {
            return _pack.GetCurrencyPack(idx);
        }

        private void OnBtnBuyNormal()
        {
            _pack.TryPurchase(CurIdx, WhenPurchaseSuccess);
        }

        private void OnBtnBuyIAP()
        {
            _pack.TryPurchase(CurIdx, WhenPurchaseSuccess);
        }

        private void WhenPurchaseSuccess(IList<RewardCommitData> rewards)
        {
            UIFlyUtility.FlyRewardList(rewards as List<RewardCommitData>, icon.transform.position);
            goDone.SetActive(true);
            btnBuyIAP.gameObject.SetActive(false);
            btnBuyNormal.gameObject.SetActive(false);
        }
    }
}