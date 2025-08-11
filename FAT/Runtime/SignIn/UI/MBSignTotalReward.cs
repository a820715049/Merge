using System.Collections;
using System.ComponentModel.Design.Serialization;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBSignTotalReward : MonoBehaviour
    {
        public Animator claimAnimator;
        public TextMeshProUGUI dayText;
        private int _day;
        public void OValidate()
        {
            claimAnimator = GetComponent<Animator>();
            transform.Access("Root/Icon/DayTxt", out dayText);
        }

        void Awake()
        {
            transform.AddButton("Root/Reward", OnClick);
        }

        public void Setup(int day)
        {
            _day = day;
            InitState();
        }

        /// <summary>
        /// 初始化显示状态
        /// </summary>
        public void InitState()
        {
            dayText.text = _day.ToString();
            if (_day < Game.Manager.loginSignMan.TotalSignInDay) transform.Find("Root/Reward/Info").gameObject.SetActive(false);
            if (Game.Manager.loginSignMan.TotalSignInDay > _day)
            {
                claimAnimator.SetTrigger("Claim");
            }
        }

        public bool TryShowClaimed()
        {
            if (Game.Manager.loginSignMan.TotalSignInDay != _day) return false;
            claimAnimator.SetTrigger("Punch");
            UIManager.Instance.Block(true);
            IEnumerator coroutine()
            {
                yield return new WaitForSeconds(1.5f);
                UIManager.Instance.Block(false);
                UIManager.Instance.OpenWindow(UIConfig.UISignInReward);
            }
            Game.Instance.StartCoroutineGlobal(coroutine());
            return true;
        }

        private void OnClick()
        {
            if (_day < Game.Manager.loginSignMan.TotalSignInDay) return;
            var reward = Game.Manager.configMan.GetLoginSignTotalConfig(_day).TotalPool;
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, transform.Find("Root/Reward").position, 10f, reward, false);
        }
    }
}