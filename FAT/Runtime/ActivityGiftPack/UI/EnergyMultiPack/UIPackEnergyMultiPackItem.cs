/*
 * @Author: qun.chao
 * @Date: 2024-12-03 12:28:56
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using fat.rawdata;
using Config;

namespace FAT
{
    public class UIPackEnergyMultiPackItem : MonoBehaviour
    {
        [SerializeField] private Button btnBuyIAP;
        [SerializeField] private UIImageRes bg;
        [SerializeField] private GameObject goSale;
        [SerializeField] private GameObject goDone;
        [SerializeField] private Transform itemRoot;

        private int CurIdx => transform.GetSiblingIndex();
        private PackEnergyMultiPack _pack;

        private void Awake()
        {
            btnBuyIAP.onClick.AddListener(OnBtnBuyIAP);
        }

        public void InitOnPreOpen(PackEnergyMultiPack pack)
        {
            _pack = pack;
            if (goDone != null)
                goDone.SetActive(false);
        }

        public void RefreshTheme()
        {
            var visual = _pack.Visual;
            visual.Refresh(bg, "itemBg");
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var item = itemRoot.GetChild(i);
                visual.Refresh(item.Access<TextMeshProUGUI>("Count"), "itemNumText");
            }
        }

        public void RefreshPack()
        {
            gameObject.SetActive(CurIdx < _pack.PackCount);
            goSale.SetActive(CurIdx + 1 == _pack.PackCount);

            _pack.GetIAPPackByIdx(CurIdx, out var iapPack);
            var bonus =  _pack.MatchPack(iapPack.Id);
            RefreshReward(bonus.reward);
        }

        private void RefreshReward(IList<RewardConfig> rewards)
        {
            var root = itemRoot;
            for (var i = 0; i < root.childCount; ++i)
            {
                var item = root.GetChild(i);
                if (i >= rewards.Count)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }
                item.gameObject.SetActive(true);
                item.Access<MBCommonItem>().ShowRewardConfig(rewards[i]);
            }
        }

        public void RefreshPrice()
        {
            var btn = btnBuyIAP;
            var valid = _pack.GetIAPPackByIdx(CurIdx, out var iapPack);
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

        private void OnBtnBuyIAP()
        {
            _pack.TryPurchase(CurIdx, WhenPurchaseSuccess);
        }

        private void WhenPurchaseSuccess(IList<RewardCommitData> rewards)
        {
            for (var i = 0; i < rewards.Count; i++)
            {
                if (i >= itemRoot.childCount)
                {
                    UIFlyUtility.FlyReward(rewards[i], btnBuyIAP.transform.position);
                }
                else
                {
                    UIFlyUtility.FlyReward(rewards[i], itemRoot.GetChild(i).position);
                }
            }
            if (goDone != null)
                goDone.SetActive(true);
        }
    }
}