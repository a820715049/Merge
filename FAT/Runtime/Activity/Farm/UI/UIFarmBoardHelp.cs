// ================================================
// File: UIFarmBoardHelp.cs
// Author: yueran.li
// Date: 2025/04/29 14:45:59 星期二
// Desc: 农场活动帮助界面
// ================================================


using EL;
using FAT.MSG;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIFarmBoardHelp : UIBase
    {
        [SerializeField] private Button mask;
        [SerializeField] private RectTransform entry1;
        [SerializeField] private RectTransform entry2;
        [SerializeField] private RectTransform txt1;
        [SerializeField] private RectTransform txt2;

        private FarmBoardActivity _activity;

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

            _activity = (FarmBoardActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            // 根据配置 判断显示哪个 
            switch (_activity.OutputType)
            {
                case FarmBoardActivity.TokenOutputType.Order:
                    ShowEntry1();
                    break;
                case FarmBoardActivity.TokenOutputType.Energy:
                    ShowEntry2();
                    break;
                case FarmBoardActivity.TokenOutputType.All:
                    ShowBoth();
                    break;
                default:
                    ShowBoth();
                    break;
            }

            transform.GetComponent<Animator>().SetTrigger("Show");
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
            if (act is FarmBoardActivity)
            {
                Close();
            }
        }
    }
}