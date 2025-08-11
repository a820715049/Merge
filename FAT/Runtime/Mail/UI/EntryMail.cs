/*
 * @Author: qun.chao
 * @Date: 2021-09-29 14:31:53
 */
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class EntryMail : MonoBehaviour
    {
        [SerializeField] private GameObject goDot;

        private void Awake()
        {
            transform.AddButton(null, _OnBtnClick);
            MessageCenter.Get<MSG.GAME_MAIL_LIST_CHANGE>().AddListener(_OnMessageMailListChange);
            MessageCenter.Get<MSG.GAME_MAIL_STATE_CHANGE>().AddListener(_OnMessageMailStateChange);
            _RefreshShow();
            _RefreshDot();
        }

        private void OnDestroy()
        {
            MessageCenter.Get<MSG.GAME_MAIL_LIST_CHANGE>().RemoveListener(_OnMessageMailListChange);
            MessageCenter.Get<MSG.GAME_MAIL_STATE_CHANGE>().RemoveListener(_OnMessageMailStateChange);
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondPass);
            _RefreshDot();
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondPass);
        }

        private void _RefreshShow()
        {
            gameObject.SetActive(Game.Manager.mailMan.MailCount > 0);
        }

        private void _RefreshDot()
        {
            goDot.SetActive(Game.Manager.mailMan.HasReward() || Game.Manager.mailMan.HasNewMail());
        }

        private void _OnBtnClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIMailBox);
        }

        private void _OnMessageMailListChange()
        {
            _RefreshShow();
            _RefreshDot();
        }

        private void _OnMessageMailStateChange()
        {
            _RefreshShow();
            _RefreshDot();
        }

        private void _OnSecondPass()
        {
            // 每秒都尝试删除过期邮件
            Game.Manager.mailMan.RemoveExpiredMail();
        }
    }
}