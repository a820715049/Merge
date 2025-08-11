/*
 * @Author: tang.yan
 * @Description: 删除账号-认证方式Item
 * @Date: 2023-12-18 10:12:23
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EL;
using centurygame;
using TMPro;

namespace FAT
{
    public class UIAuthenticationSelectItem : UIGenericItemBase<CGAccountType>
    {
        protected override void InitComponents()
        {
            transform.AddButton(null, _OnBtnClick);
        }

        protected override void UpdateOnDataChange()
        {
            var text = transform.GetChild(0).GetComponent<TMP_Text>();
            text.text = AccountDelectionUtility.GetAuthenticationTypeDesc(mData);
        }

        private void _OnBtnClick()
        {
            StartCoroutine(_TryProcessAuthentication());
        }

        private IEnumerator _TryProcessAuthentication()
        {
            UIManager.Instance.Block(true);
            var task = AccountDelectionUtility.TryProcessAuthentication(mData);
            yield return task;
            UIManager.Instance.Block(false);
            if (task.isSuccess)
            {
#if !UNITY_EDITOR
                var info = (task as SimpleResultedAsyncTask<CGBaseRemovalAccountInfo>).result;
                if (string.Equals(info.GetFpid(), Game.Manager.networkMan.fpId))
                {
                    UIManager.Instance.CloseWindow(UIConfig.UIAuthenticationSelect);
                    // 认证成功 进入后续流程
                    UIManager.Instance.OpenWindow(UIConfig.UIAuthenticationEmail, info);
                }
                else
                {
                    // 身份不同
                    Game.Manager.commonTipsMan.ShowClientTips(I18N.Text("#SysComDesc173"));
                }
#else
                UIManager.Instance.CloseWindow(UIConfig.UIAuthenticationSelect);
                // 认证成功 进入后续流程
                UIManager.Instance.OpenWindow(UIConfig.UIAuthenticationEmail);
#endif
            }
            else
            {
                // 认证失败
                Game.Manager.commonTipsMan.ShowClientTips(I18N.Text("#SysComDesc173"));
            }
        }
    }
}