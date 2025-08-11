/*
 * @Author: chaoran.zhang
 * @Description: 评价引导UI的基本功能逻辑
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/hhb6uxr57rxwgsz1
 * @Date: 2024-1-04 15：55：30
 */
using EL;
using FAT.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIRate : UIBase
    {
        private GameObject mStarLayout;
        private int mScore = 0;

        private TextMeshProUGUI RateText;

        protected override void OnCreate()
        {
            RateText = transform.Find("Content/Panel/Info/RateText").GetComponent<TextMeshProUGUI>();
            mStarLayout = transform.Find("Content/Panel/Info/StarLayout").gameObject;
            transform.AddButton("Content/Panel/Top/BtnClose", OnClickBtnClose);
            transform.AddButton("Content/Panel/Bottom/BtnRate", OnClickBtnSubmit);
            for (int i = 0; i < 5; i++)
            {
                int index = new();
                index = i;
                mStarLayout.transform.GetChild(i).GetComponent<Button>().onClick.AddListener(() => { OnClickStar(index + 1); });
            }
            RateText.text = I18N.Text("#SysComDesc200");
        }

        protected override void OnPreOpen()
        {
            RefreshStar(0);
            RateText.text = I18N.Text("#SysComDesc200");
            //评价开始打点
            DataTracker.rate_start.Track();
        }

        protected void OnClickStar(int score)
        {
            mScore = score;
            RefreshStar(mScore);
            switch (mScore)
            {
                case 1:
                    {
                        RateText.text = I18N.Text("#SysComDesc195");
                        break;
                    }
                case 2:
                    {
                        RateText.text = I18N.Text("#SysComDesc196");
                        break;
                    }
                case 3:
                    {
                        RateText.text = I18N.Text("#SysComDesc197");
                        break;
                    }
                case 4:
                    {
                        RateText.text = I18N.Text("#SysComDesc198");
                        break;
                    }
                case 5:
                    {
                        RateText.text = I18N.Text("#SysComDesc199");
                        break;
                    }
            }
        }

        public void OnClickBtnSubmit()
        {
            if (mScore == 0)
            {
                Game.Manager.commonTipsMan.ShowPopTips(fat.rawdata.Toast.RateFirst);
            }
            else if (mScore < Game.Manager.configMan.globalConfig.RateRedirectLv)
            {
                //评价结束打点
                DataTracker.rate_end.Track(mScore);

                Game.Manager.accountMan.SetClientStorage(Constant.kHaveRated, 1.ToString());

                PlatformSDK.Instance.ShowCustomService();
                base.Close();
            }
            else if (mScore >= Game.Manager.configMan.globalConfig.RateRedirectLv)
            {
                //评价结束打点
                DataTracker.rate_end.Track(mScore);

                Game.Manager.accountMan.SetClientStorage(Constant.kHaveRated, 1.ToString());

// #if UNITY_ANDROID
//                 Application.OpenURL("market://details?id=com.fatmerge.global");
// #endif

// #if UNITY_IOS
//                 Application.OpenURL("itms-apps://itunes.apple.com/app/id6471045672");
// #endif
                UIBridgeUtility.OpenAppStore();
                base.Close();
            }
        }

        protected void OnClickBtnClose()
        {
            //评价结束打点
            DataTracker.rate_end.Track(0);
            base.Close();
        }

        protected void RefreshStar(int score)
        {
            for (int i = 0; i < 5; i++)
            {
                if (i < score)
                {
                    mStarLayout.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
                }
                else
                {
                    mStarLayout.transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
                }
            }
        }
    }
}
