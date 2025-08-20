/*
 * @Author: ange.shentu
 * @Description: 概率公示跳转外链UI
 * @Date: 2025-07-03 15:06:17
 * @doc: 概率公示案子：https://centurygames.feishu.cn/wiki/SQVLw3tsSiHxEOkqdq3c85kXnMf?fromScene=spaceOverview
 */
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIProbabilityTips : UIBase, INavBack
    {
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnClose;
        [SerializeField] private GameObject titleObj;
        [SerializeField] private GameObject contentObj;

        private bool m_isCardPackView = false;
        //卡包标题多语言Key
        private const string TITLE_CARD_KEY = "#SysComDesc1381";
        //卡包内容多语言Key
        private const string CONTENT_CARD_KEY = "#SysComDesc1382";
        //宝箱标题多语言Key
        private const string TITLE_BOX_KEY = "#SysComDesc1378";
        //宝箱内容多语言Key
        private const string CONTENT_BOX_KEY = "#SysComDesc1379";
        protected override void OnCreate()
        {
            btnConfirm.onClick.AddListener(_OnBtnConfirm);
            btnClose.onClick.AddListener(Close);
        }
        protected override void OnParse(params object[] items)
        {
            //如果有更多模式，可以把bool改成enum，改的时候记得要把以前的引用都改了
            m_isCardPackView = items.Length > 0 && items[0] is bool b && b;
        }
        protected override void OnPreOpen()
        {
            if (titleObj != null)
            {
                MBI18NText.SetKey(titleObj, m_isCardPackView ? TITLE_CARD_KEY : TITLE_BOX_KEY);
            }

            if (contentObj != null)
            {
                MBI18NText.SetKey(contentObj, m_isCardPackView ? CONTENT_CARD_KEY : CONTENT_BOX_KEY);
            }
        }
        private void _OnBtnConfirm()
        {
            UIBridgeUtility.OpenURL(Game.Manager.configMan.globalConfig.DropProbability);
            Close();
        }

        void INavBack.OnNavBack()
        {
            Close();
        }
    }
}
