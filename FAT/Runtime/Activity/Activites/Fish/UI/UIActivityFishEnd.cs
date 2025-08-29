/**
 * @Author: zhangpengjian
 * @Date: 2025/3/26 11:25:09
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/3/26 11:25:09
 * Description: 钓鱼棋盘结束界面
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityFishEnd : UIBase
    {
        [SerializeField] private ActivityFishing _activity;
        [SerializeField] private TextProOnACircle title;
        [SerializeField] private TextMeshProUGUI desc;
        [SerializeField] private GameObject fishRoot;
        [SerializeField] private Button close;
        [SerializeField] private Button confirm;

        protected override void OnCreate()
        {
            close.onClick.AddListener(Close);
            confirm.onClick.AddListener(Close);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;
            _activity = (ActivityFishing)items[0];
        }

        protected override void OnPreOpen()
        {
            if (_activity == null)
            {
                return;
            }
            RefreshFish();
            RefreshTheme();
        }

        private void RefreshTheme()
        {
            _activity.VisualEnd.visual.Refresh(title, "mainTitle");
            _activity.VisualEnd.visual.Refresh(desc, "desc");
        }

        private void RefreshFish()
        {
            for (int i = 0; i < _activity.FishInfoList.Count; i++)
            {
                var fish = _activity.FishInfoList[i];
                var item = fishRoot.transform.GetChild(i).GetComponent<MBFishItem>();
                item.Setup(_activity, fish, false);
            }
        }
    }
}