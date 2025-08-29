// ================================================
// File: UIActivityFishCollect.cs
// Author: yueran.li
// Date: 2025/04/10 17:25:28 星期四
// Desc: 钓鱼棋盘鱼图鉴集齐界面
// ================================================


using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIActivityFishCollect : UIBase, INavBack
    {
        private UIImageRes fishImg;
        private TextMeshProUGUI fishName;
        private TextMeshProUGUI title;
        private TextMeshProUGUI desc;

        // 活动实例
        private ActivityFishing activityFish;
        private ActivityFishing.FishCaughtInfo info;

        #region UI
        protected override void OnCreate()
        {
            transform.Access("Content/FishName", out fishName);
            transform.Access("Content/FishImgRoot/FishImg", out fishImg);
            transform.Access("Content/TitleRoot/Title", out title);
            transform.Access("Content/desc", out desc);

            transform.AddButton("Mask", OnClick);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;
            activityFish = (ActivityFishing)items[0];
            info = (ActivityFishing.FishCaughtInfo)items[1];
        }

        protected override void OnPreOpen()
        {
            // 根据配置 显示UI
            var fishInfo = activityFish.FishInfoList.FindEx(f => f.Id == info.fishId);

            // 鱼的信息
            fishImg.SetImage(fishInfo.Icon);
            fishName.SetText(I18N.Text(fishInfo.Name));

            RefreshTheme();
            Game.Manager.audioMan.TriggerSound("BingoLevelComplete");
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }
        #endregion

        private void OnClick()
        {
            MessageCenter.Get<FISHING_FISH_COLLECT_CLOSE>().Dispatch(info);
            Close();
        }

        // 活动结束
        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != activityFish || !expire) return;
            Close();
        }

        private void RefreshTheme()
        {
            activityFish.VisualCollect.visual.Refresh(title, "mainTitle");
            activityFish.VisualCollect.visual.Refresh(desc, "desc");
        }

        public void OnNavBack()
        {
            OnClick();
        }
    }
}