/*
 * @Author: qun.chao
 * @Date: 2024-11-20 18:03:53
 */
using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    // 根据左下角的icon显示状况 决策左上列表最大高度
    // 用于决定左上列表是否切换为scroll状态
    public class UISceneHudCheckBL : MonoBehaviour
    {
        [SerializeField] private GameObject goLeft;
        [SerializeField] private ListActivity actList_BL;
        // 三档高度
        private List<float> rectHeight = new() { 1388, 1200f, 1022f };
        private bool _initialized;
        private bool _isDirty;

        private void Ensure()
        {
            if (_initialized)
                return;
            _initialized = true;

            goLeft.AddComponent<MBTransChange>().OnDimensionsChange = SetDirty;
            actList_BL.group.AddComponent<MBTransChange>().OnDimensionsChange = SetDirty;
        }

        private void Start()
        {
            Ensure();
        }

        private void OnEnable()
        {
            Ensure();
            Refresh();
        }

        private void SetDirty()
        {
            _isDirty = true;
        }

        private void Update()
        {
            if (_isDirty)
            {
                _isDirty = false;
                Refresh();
            }
        }

        private void Refresh()
        {
            var iconNum = 0;
            // 迷你游戏入口
            var miniGame = goLeft.transform.GetChild(1);
            if (miniGame != null && miniGame.gameObject.activeSelf)
            {
                ++iconNum;
            }
            // 好友功能入口
            var childCount = actList_BL.group.transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                if (actList_BL.group.transform.GetChild(i).gameObject.activeSelf)
                    ++iconNum;
            }
            var height = iconNum switch
            {
                0 => rectHeight[0],
                1 => rectHeight[1],
                _ => rectHeight[2],
            };
            var trans = transform as RectTransform;
            trans.sizeDelta = new Vector2(trans.sizeDelta.x, height);
        }
    }
}