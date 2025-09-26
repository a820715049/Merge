/**
 * @Author: zhangpengjian
 * @Date: 2025/3/26 11:25:09
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/3/26 11:25:09
 * Description: 钓鱼棋盘结束界面
 */

using System.Collections.Generic;
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
        [SerializeField] private GameObject desc1;
        [SerializeField] private GameObject desc2;
        [SerializeField] private GameObject fishObj;
        [SerializeField] private GameObject fishObjRoot;
        [SerializeField] private Button close;
        [SerializeField] private Button confirm;

        private string fishObj_key = "fish_obj";
        private List<GameObject> fishItems = new();

        protected override void OnCreate()
        {
            close.onClick.AddListener(Close);
            confirm.onClick.AddListener(Close);
            GameObjectPoolManager.Instance.PreparePool(fishObj_key, fishObj);
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

        protected override void OnPreClose()
        {
            foreach (var item in fishItems)
            {
                GameObjectPoolManager.Instance.ReleaseObject(fishObj_key, item);
            }
            fishItems.Clear();
        }

        private void RefreshFish()
        {
            if (_activity.FishInfoList.Count > 10)
            {
                desc2.SetActive(true);
                desc1.SetActive(false);
                fishItems.Clear();
                for (int i = 0; i < _activity.FishInfoList.Count; i++)
                {
                    var fish = _activity.FishInfoList[i];
                    var obj = GameObjectPoolManager.Instance.CreateObject(fishObj_key, fishObjRoot.transform);
                    var item = obj.GetComponent<MBFishItem>();
                    item.Setup(_activity, fish, false);
                    fishItems.Add(obj);
                }
            }
            else
            {
                desc2.SetActive(false);
                desc1.SetActive(true);
                for (int i = 0; i < _activity.FishInfoList.Count; i++)
                {
                    var fish = _activity.FishInfoList[i];
                    var item = fishRoot.transform.GetChild(i).GetComponent<MBFishItem>();
                    item.Setup(_activity, fish, false);
                }
            }
        }
    }
}