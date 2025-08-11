/*
 * @Author: yanfuxing
 * @Date: 2025-06-20 11:40:05
 */
using System;
using System.Collections;
using Cysharp.Text;
using EL;
using FAT.Merge;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBWishBoardHandbook : MonoBehaviour
    {
        public float BannerAnimTime;
        public UIImageRes Icon;
        private WishBoardActivity _activity;

        void Awake()
        {
            transform.GetComponent<Button>().onClick.AddListener(Click);
        }

        public void Setup(WishBoardActivity activity)
        {
            _activity = activity;
        }

        public void Refresh()
        {
            var list = _activity.GetAllItemIdList();
            if (list.Count == 0) return;
            var curLevel = _activity._GetCurUnlockItemMaxLevel();
            if (curLevel == 0) curLevel = _activity.IsItemUnlock(list[0]) ? 0 : -1;
            if (curLevel < list.Count) return;
            UIManager.Instance.OpenWindow(_activity.VisualUIHandbook.res.ActiveR, _activity);
            _activity.VisualUIHandbook.visual.TextMap.TryGetValue("desc1", out var tipsDes);
            IEnumerator bannerAnim()
            {   //庆祝横幅
                yield return new WaitForSeconds(BannerAnimTime);
                UIManager.Instance.OpenWindow(_activity.VisualUIHandbookTips.res.ActiveR, _activity, I18N.Text(tipsDes), true);
            }
            StartCoroutine(bannerAnim());
        }
        private void Click()
        {
            UIManager.Instance.OpenWindow(_activity.VisualUIHandbook.res.ActiveR, _activity);
        }

        public void Unlock(Item item, Action callback = null)
        {
            IEnumerator unlock()
            {
                yield return new WaitForSeconds(0.1f);
                var from = BoardViewManager.Instance.CoordToWorldPos(item.coord);
                UIFlyUtility.FlyCustom(item.config.Id, 1, from,
                    Icon.transform.position,
                    FlyStyle.Common,
                    FlyType.None, () =>
                    {
                        Refresh();
                        callback?.Invoke();
                    }, size: 136f);
            }
            StartCoroutine(unlock());
        }
    }
}