// ================================================
// File: FishGet.cs
// Author: yueran.li
// Date: 2025/04/10 17:25:28 星期四
// Desc: Desc
// ================================================


using EL;
using TMPro;
using UnityEngine;
using static fat.conf.Data;

namespace FAT
{
    public class UIActivityFishGetItem : MonoBehaviour
    {
        private TextMeshProUGUI fishName;
        private GameObject newFish;
        private GameObject newRecord;

        // 重量
        private TextMeshProUGUI num1;
        private TextMeshProUGUI num2;
        private TextMeshProUGUI num3;
        private TextMeshProUGUI rareTxt;

        // 稀有度
        private UIImageRes RareImg;
        private UIActivityFishGet.FishGetItemData data;

        public void InitOnPreOpen(UIActivityFishGet.FishGetItemData fishInfoData)
        {
            transform.Access("FishName", out fishName);
            newFish = transform.Find("New").gameObject;
            newRecord = transform.Find("Weight/NewRecord").gameObject;
            transform.Access("Rare", out RareImg);
            transform.Access("Rare/RareTxt", out rareTxt);
            transform.Access("Weight/WeightNum/WeightGroup/num1/num", out num1);
            transform.Access("Weight/WeightNum/WeightGroup/num2/num", out num2);
            transform.Access("Weight/WeightNum/WeightGroup/num3/num", out num3);

            data = fishInfoData;
            RefreshData();
        }

        private void RefreshData()
        {
            // 通过 FishInfo的Rarity 获得 FishRarity.iconBg
            var rarity = GetFishRarity(data.fishInfo.Rarity);

            // 图片，稀有度名称多语言keu
            fishName.SetText(I18N.Text(data.fishInfo.Name));

            // 修改字体的material preset
            if (int.TryParse(rarity.ColorDesc, out var key)) 
            {
                var mat_ = FontMaterialRes.Instance.GetFontMatResConf(key);
                mat_.ApplyFontMatResConfig(rareTxt);
            }
            
            RareImg.SetImage(rarity.Color);
            rareTxt.SetText(I18N.Text(rarity.Name));

            // 判断是否是第一次获得 newFish 显隐
            newFish.SetActive(data.newFish);

            // 重量的显示 是否是历史最大重量
            newRecord.SetActive(data.maxWeight);

            // num1 = _weight的十位 num2 = _weight的个位 num3 = _weight的小数位
            num1.SetText($"{data.weight / 100}");
            num2.SetText($"{(data.weight % 100) / 10}");
            num3.SetText($"{data.weight % 10}");
        }


        public void OnPreClose()
        {
        }
    }
}