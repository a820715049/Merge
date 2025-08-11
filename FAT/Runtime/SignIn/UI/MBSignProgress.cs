using System.Collections;
using System.Collections.Generic;
using System.Threading;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBSignProgress : MonoBehaviour
    {
        public List<MBSignTotalReward> signRewards = new List<MBSignTotalReward>();
        public TextMeshProUGUI dayText;
        public RectMask2D mask;

        void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_SIGN_IN_CLICK>().AddListener(RefreshShow);
        }

        void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_SIGN_IN_CLICK>().RemoveListener(RefreshShow);
        }

        public void Setup()
        {
            var i = 0;
            foreach (var item in Game.Manager.loginSignMan.SignIn30DaysRewards)
            {
                if (signRewards.Count > i)
                    signRewards[i].Setup(item);
                i++;
            }
            dayText.text = (Game.Manager.loginSignMan.TotalSignInDay - 1).ToString();
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            var real = Game.Manager.loginSignMan.TotalSignInDay;
            mask.padding = new Vector4(0, 0, (30 - real) * 20.4f, 0);
        }

        public void RefreshShow()
        {
            dayText.text = Game.Manager.loginSignMan.TotalSignInDay.ToString();
            var hasAnim = false;
            foreach (var item in signRewards)
            {
                if (item.TryShowClaimed())
                {
                    hasAnim = true;
                }
            }
            if (!hasAnim)
            {
                UIManager.Instance.Block(true);
                IEnumerator coroutine()
                {
                    yield return new WaitForSeconds(2.0f);
                    UIManager.Instance.Block(false);
                    UIManager.Instance.CloseWindow(UIConfig.UISignInpanel);
                }
                Game.Instance.StartCoroutineGlobal(coroutine());
            }
        }
    }
}