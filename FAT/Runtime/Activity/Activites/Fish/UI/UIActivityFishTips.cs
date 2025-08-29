// ================================================
// File: UIActivityFishTips.cs
// Author: yueran.li
// Date: 2025/04/10 17:25:28 星期四
// Desc: 钓鱼棋盘鱼图鉴收集Tip
// ================================================


using EL;
using fat.conf;
using FAT.MSG;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityFishTips : UITipsBase
    {
        private FishInfo _fishInfo;

        // 三星Tip
        private CanvasGroup catchAll;
        private TextMeshProUGUI all_desc;
        private TextMeshProUGUI all_fishName;
        private Image all_rare;
        private TextMeshProUGUI all_rareTxt;

        // 未满三星Tip
        private CanvasGroup catchMore;
        private TextMeshProUGUI more_desc;
        private TextMeshProUGUI more_fishName;
        private Image more_rare;
        private TextMeshProUGUI more_rareTxt;

        private ActivityFishing activityFish;
        private int fishId;
        private bool allCatch; // 判断是否获得全部星星

        protected override void OnCreate()
        {
            transform.Access("Panel/CatchAll", out catchAll);
            transform.Access("Panel/CatchMore", out catchMore);

            transform.Access("Panel/CatchAll/fishName", out all_fishName);
            transform.Access("Panel/CatchAll/desc", out all_desc);
            all_rare = transform.Find("Panel/CatchAll/rare").GetComponent<Image>();
            transform.Access("Panel/CatchAll/rare/rareTxt", out all_rareTxt);

            transform.Access("Panel/CatchMore/fishName", out more_fishName);
            transform.Access("Panel/CatchMore/desc", out more_desc);
            more_rare = transform.Find("Panel/CatchMore/rare").GetComponent<Image>();
            transform.Access("Panel/CatchMore/rare/rareTxt", out more_rareTxt);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            // items[0] Vector3 位置
            // items[1] float 偏移参数
            _SetTipsPosInfo(items);

            if (items.Length > 2)
            {
                activityFish = (ActivityFishing)items[2];
                fishId = (int)items[3];
            }
        }

        protected override void OnPreOpen()
        {
            // 刷新tips位置
            _RefreshTipsPos(18);

            // config
            _fishInfo = activityFish.FishInfoList.FindEx(f => f.Id == fishId);
            var rarity = Data.GetFishRarity(_fishInfo.Rarity);

            // 计算是否获得全部星星
            var caught = activityFish.GetFishCaughtCount(fishId);
            var star = activityFish.CalcFishStarByCount(fishId, caught);
            var maxStar = activityFish.GetFishMaxStar(fishId);
            allCatch = star >= maxStar;

            // 计算还需要几颗星星
            var moreCnt = activityFish.CalcFishStarRequireCount(fishId, star + 1) - caught;

            ColorUtility.TryParseHtmlString(rarity.IconBg, out var color);

            // 根据是否收集全部显示对应的描述
            if (allCatch)
            {
                catchAll.alpha = 1;
                catchMore.alpha = 0;
                all_rareTxt.SetText(I18N.Text(rarity.Name));
                all_rare.color = color;
                all_fishName.SetText(I18N.Text(_fishInfo.Name));
            }
            else
            {
                catchAll.alpha = 0;
                catchMore.alpha = 1;
                more_rareTxt.SetText(I18N.Text(rarity.Name));
                more_rare.color = color;
                more_fishName.SetText(I18N.Text(_fishInfo.Name));
            }

            RefreshTheme(moreCnt);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != activityFish || !expire) return;
            Close();
        }

        // 换皮
        private void RefreshTheme(int moreCnt)
        {
            var textInfo = activityFish.VisualTip.visual.Theme.TextInfo;

            activityFish.VisualTip.visual.Refresh(all_desc, "desc2");
            more_desc.SetText(I18N.FormatText(textInfo["desc1"], moreCnt));
        }
    }
}