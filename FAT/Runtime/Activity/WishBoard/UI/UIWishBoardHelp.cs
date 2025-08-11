/*
 * @Author: yanfuxing
 * @Date: 2025-06-13 15:32:09
 */
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIWishBoardHelp : UIBase
    {
        [SerializeField] private Button mask;
        [SerializeField] private RectTransform entry1;
        [SerializeField] private RectTransform entry2;
        [SerializeField] private RectTransform txt1;
        [SerializeField] private RectTransform txt2;
        [SerializeField] private TextMeshProUGUI middleDescText;
        [SerializeField] private TextMeshProUGUI bottomDescText;
        private WishBoardActivity _activity;

        protected override void OnCreate()
        {
            mask.onClick.AddListener(OnClose);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (WishBoardActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            // 根据配置 判断显示哪个 
            switch (_activity.OutputType)
            {
                case WishBoardActivity.TokenOutputType.Order:
                    ShowEntry1();
                    break;
                case WishBoardActivity.TokenOutputType.Energy:
                    ShowEntry2();
                    break;
                case WishBoardActivity.TokenOutputType.All:
                    ShowBoth();
                    break;
                default:
                    ShowBoth();
                    break;
            }
            transform.GetComponent<Animator>().SetTrigger("Show");
            _activity.VisualUIHelp.visual.Refresh(middleDescText, "desc1");
            _activity.VisualUIHelp.visual.Refresh(bottomDescText, "desc2");
        }

        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }

        private void ShowEntry1()
        {
            entry1.gameObject.SetActive(true);
            txt1.gameObject.SetActive(true);
            entry1.anchoredPosition = new Vector3(0, -252, 0);
            txt1.anchoredPosition = new Vector3(0, -215, 0);

            entry2.gameObject.SetActive(false);
            txt2.gameObject.SetActive(false);
        }

        private void ShowEntry2()
        {
            entry1.gameObject.SetActive(false);
            txt1.gameObject.SetActive(false);

            entry2.gameObject.SetActive(true);
            txt2.gameObject.SetActive(true);
            entry2.anchoredPosition = new Vector3(0, -252, 0);
            txt2.anchoredPosition = new Vector3(0, -215, 0);
        }

        private void ShowBoth()
        {
            entry1.gameObject.SetActive(true);
            entry1.anchoredPosition = new Vector3(-228, -252, 0);

            entry2.gameObject.SetActive(true);
            entry2.anchoredPosition = new Vector3(228, -252, 0);

            txt1.gameObject.SetActive(true);
            txt1.anchoredPosition = new Vector3(-226, -215, 0);

            txt2.gameObject.SetActive(true);
            txt2.anchoredPosition = new Vector3(228, -215, 0);
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is WishBoardActivity)
            {
                Close();
            }
        }
    }
}