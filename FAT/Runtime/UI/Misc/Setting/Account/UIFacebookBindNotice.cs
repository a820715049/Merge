/**
 * @Author: zhangpengjian
 * @Date: 2024/10/17 14:46:34
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/17 14:46:34
 * Description: facebook登陆提示弹窗
 */

using FAT.Platform;
using UnityEngine;

namespace FAT
{
    public class UIFacebookBindNotice : UIBase
    {
        protected override void OnCreate()
        {
            transform.Access("Content/Panel", out Transform root);
            root.Access("close", out MapButton close);
            root.Access("change", out MapButton change);
            close.WhenClick = Close;
            change.WhenClick = OnClickBind;
        }

        private void OnClickBind()
        {
            //当弹出这个界面时,说明没有绑定或者没有登录Facebook,直接走一遍绑定/登录流程
            PlatformSDK.Instance.binding.TryBind(AccountLoginType.Facebook, true);
        }

        protected override void OnPreOpen()
        {
        }
    }
}