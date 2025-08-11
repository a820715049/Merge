/*
 * @Author: qun.chao
 * @Date: 2023-10-11 18:37:17
 */
using System.Collections.Generic;
using UnityEngine;
using EL;

namespace FAT
{
    public class MBSplashScreen : MonoBehaviour
    {
        private void Start()
        {
            MessageCenter.Get<MSG.UI_SPLASH_SCREEN_STATE>().AddListener(_OnMessageShowSplashScreen);
        }

        private void _OnMessageShowSplashScreen(bool b)
        {
            gameObject.SetActive(b);
        }
    }
}