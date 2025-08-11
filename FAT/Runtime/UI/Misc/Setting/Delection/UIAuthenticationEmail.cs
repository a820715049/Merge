/*
 * @Author: tang.yan
 * @Description: 删除账号-输入邮箱界面 
 * @Date: 2023-12-18 10:12:42
 */
using UnityEngine;
using UnityEngine.UI;
using EL;
using System.Text.RegularExpressions;
using TMPro;
using centurygame;
using System.Collections;

namespace FAT
{
    public class UIAuthenticationEmail : UIBase
    {
        [SerializeField] private TMP_InputField inputEmail;
        [SerializeField] private TMP_InputField inputEmailConfirm;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnConfirm;

        private CGBaseRemovalAccountInfo _curSdkInfo = null;    //sdk返回来的用于验证的info 不可自己new

        protected override void OnCreate()
        {
            btnClose.WithClickScale().FixPivot().onClick.AddListener(base.Close);
            btnConfirm.WithClickScale().FixPivot().onClick.AddListener(_OnBtnConfirm);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                _curSdkInfo = items[0] as CGBaseRemovalAccountInfo;
                DebugEx.FormatInfo("UIAuthenticationEmail = {0}", _curSdkInfo?.ToJsonString());
            }
        }

        protected override void OnPreOpen()
        {
            inputEmail.SetTextWithoutNotify(string.Empty);
            inputEmailConfirm.SetTextWithoutNotify(string.Empty);

            // 允许更多字符
            inputEmail.characterLimit = 40;
            inputEmailConfirm.characterLimit = 40;
        }

        private bool _IsValid(string email)
        {
            try
            {
                // 改用正则
                string pattern = @"[A-Z0-9a-z._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}";
                return Regex.IsMatch(email, pattern);
            }
            catch
            {
                return false;
            }
        }

        private void _OnBtnConfirm()
        {
            string emailStr = inputEmail.text;
            if (!_IsValid(emailStr))
            {
                Game.Manager.commonTipsMan.ShowClientTips(I18N.Text("#SysComDesc169"));
                return;
            }

            if (!string.Equals(emailStr, inputEmailConfirm.text))
            {
                Game.Manager.commonTipsMan.ShowClientTips(I18N.Text("#SysComDesc170"));
                return;
            }
#if !UNITY_EDITOR
            if (_curSdkInfo == null)
            {
                Game.Manager.commonTipsMan.ShowClientTips(I18N.Text("#SysComDesc172"));
                return;
            }
             //设置邮箱
            AccountDelectionUtility.SetReceiveEmail(_curSdkInfo, emailStr);
            //提交账号删除申请
            StartCoroutine(_TryProcessRemoveUser());
#else
            Game.Manager.commonTipsMan.ShowMessageTips(I18N.Text("#SysComDesc171"), null, GameProcedure.QuitGame, true);
#endif
        }
        
        private IEnumerator _TryProcessRemoveUser()
        {
            UIManager.Instance.Block(true);
            var task = AccountDelectionUtility.TryProcessRemoveUser(_curSdkInfo);
            yield return task;
            UIManager.Instance.Block(false);
            if (task.isSuccess)
            {
                //执行删账号准备的流程
                yield return AccountDelectionUtility.TryProcessSureRemoveUser();
                Close();
                //点确定退出游戏
                Game.Manager.commonTipsMan.ShowMessageTips(I18N.Text("#SysComDesc171"), null, GameProcedure.QuitGame, true);
            }
            else
            {
                // 认证失败
                Game.Manager.commonTipsMan.ShowClientTips(I18N.Text("#SysComDesc172"));
            }
        }
    }
}