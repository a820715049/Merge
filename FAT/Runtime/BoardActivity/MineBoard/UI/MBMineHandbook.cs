using System.Collections;
using Cysharp.Text;
using EL;
using FAT.Merge;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBMineHandbook : MonoBehaviour
    {
        public float BannerAnimTime;
        private TextMeshProUGUI _text;
        private UIImageRes _icon;
        private GameObject _complete;
        private Animator _animator;
        private MineBoardActivity _activity;

        public void Setup()
        {
            _animator = transform.GetComponent<Animator>();
            transform.Access("Icon/text", out _text);
            transform.Access("Icon", out _icon);
            _complete = transform.Find("Complete").gameObject;
            transform.AddButton("ClickBtn", Click);
        }

        public void Refresh(MineBoardActivity activity, bool isUnlock = false)
        {
            if (activity != null)
                _activity = activity;
            var list = Game.Manager.mineBoardMan.GetAllItemIdList();
            if (list.Count == 0) return;
            var curLevel = Game.Manager.mineBoardMan.GetCurUnlockItemMaxLevel();
            if (curLevel == 0) curLevel = Game.Manager.mineBoardMan.IsItemUnlock(list[0]) ? 0 : -1;
            curLevel++;
            var curItem = Game.Manager.objectMan.GetBasicConfig(list[Game.Manager.mineBoardMan.GetCurUnlockItemMaxLevel()]);
            _icon.SetImage(curItem.Icon);
            _text.text = ZString.Format("{0}/{1}", curLevel, list.Count);
            _complete.SetActive(curLevel == list.Count);
            if (!isUnlock || curLevel < list.Count) return;

            UIManager.Instance.OpenWindow(_activity.HandBookResAlt.ActiveR, _activity);
            _activity.HandBookTheme.TextMap.TryGetValue("desc1", out var mainTitle);
            _activity.HandBookTheme.TextMap.TryGetValue("mainTitle", out var subTitle);
            IEnumerator bannerAnim()
            {
                yield return new WaitForSeconds(BannerAnimTime);
                UIManager.Instance.OpenWindow(_activity.BannerResAlt.ActiveR, _activity, ZString.Format(I18N.Text(mainTitle), I18N.Text(subTitle)), true);
            }
            StartCoroutine(bannerAnim());
        }
        private void Click()
        {
            UIManager.Instance.OpenWindow(_activity.HandBookResAlt.ActiveR, _activity);
        }

        public void Unlock(Item item)
        {

            IEnumerator unlock()
            {
                yield return new WaitForSeconds(0.1f);
                var from = BoardViewManager.Instance.CoordToWorldPos(item.coord);
                UIFlyUtility.FlyCustom(item.config.Id, 1, from,
                    _icon.transform.position,
                    FlyStyle.Common,
                    FlyType.None, () =>
                    {
                        _animator.SetTrigger("Punch");
                        Refresh(null, true);
                    }, size: 136f);
            }

            StartCoroutine(unlock());
        }
    }
}