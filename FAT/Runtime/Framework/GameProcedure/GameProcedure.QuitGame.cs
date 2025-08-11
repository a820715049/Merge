/*
 * @Author: qun.chao
 * @Date: 2023-12-06 19:20:00
 */
using System;
using System.Collections;
using UnityEngine;
using EL;

namespace FAT
{
    public static partial class GameProcedure
    {
        public static void QuitGame()
        {
            Game.Instance.StartCoroutineGlobal(_CoQuitGame());
        }

        private static IEnumerator _CoQuitGame()
        {
            yield return Platform.PlatformSDK.Instance.Adapter.Logout();
            yield return null;
            CommonUtility.QuitApp();
        }
    }
}