using System.Collections;
using Cysharp.Text;
using EL;
using FAT.Merge;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBMineCartHandbook : MonoBehaviour
    {
        [SerializeField] private float bannerAnimTime = 0.5f;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private UIImageRes _icon;
        [SerializeField] private GameObject _complete;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _redDot;
        private MineCartActivity _activity;

        private IEnumerator ShowBannerCoroutine()
        {
            if (_activity == null) yield break;

            UIManager.Instance.OpenWindow(UIConfig.UIMineCartHandbook, _activity);
            yield return new WaitForSeconds(bannerAnimTime);
            UIManager.Instance.OpenWindow(UIConfig.UIMineCartBoardBannerTip, _activity, I18N.Text("UI_MINECART_HANDBOOK_COMPLETE_DESC"));
        }

        public void OnCreate()
        {
            transform.AddButton("ClickBtn", Click);
        }
        public void OnParse(MineCartActivity activity)
        {
            if (activity != null)
                _activity = activity;
        }

        public void Refresh(bool isUnlock = false)
        {
            var list = _activity.GetAllItemIdList();
            if (list.Count == 0) return;
            var curLevel = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var itemId = list[i];
                if (_activity.IsItemUnlock(itemId))
                {
                    curLevel = i + 1;
                }
            }
            int imgLevel = curLevel - 1;
            if (imgLevel < 0) imgLevel = 0;
            var curItemForImg = Game.Manager.objectMan.GetBasicConfig(list[imgLevel]);
            _icon.SetImage(curItemForImg.Icon);
            bool finish = curLevel == list.Count;
            if (finish)
            {
                _complete.SetActive(true);
                _text.gameObject.SetActive(false);
            }
            else
            {
                _complete.SetActive(false);
                _text.gameObject.SetActive(true);
                _text.text = ZString.Format("{0}/{1}", curLevel, list.Count);
            }
            if (isUnlock && curLevel == list.Count && !_activity.PlayedHandbookBanner)
            {
                _animator.SetTrigger("Complete");
                StartCoroutine(ShowBannerCoroutine());
                _activity.PlayedHandbookBanner = true;
                _activity.NeedShowHandbookRedDot = false;
            }
            SetRedDot(_activity.NeedShowHandbookRedDot);
        }
        public void SetRedDot(bool show)
        {
            _redDot.SetActive(show);
        }

        private void Click()
        {
            if (_activity == null) return;
            UIManager.Instance.OpenWindow(UIConfig.UIMineCartHandbook, _activity);
            _activity.NeedShowHandbookRedDot = false;
            SetRedDot(false);
        }

        public void Unlock(Item item)
        {
            if (item == null || _activity == null) return;

            StartCoroutine(UnlockCoroutine(item));
        }

        private IEnumerator UnlockCoroutine(Item item)
        {
            yield return new WaitForSeconds(0.1f);
            var from = BoardViewManager.Instance.CoordToWorldPos(item.coord);
            UIFlyUtility.FlyCustom(item.config.Id, 1, from,
                _icon.transform.position,
                FlyStyle.Common,
                FlyType.None, () =>
                {
                    _animator.SetTrigger("Punch");
                    Refresh(true);
                }, size: 136f);
        }
    }
}
