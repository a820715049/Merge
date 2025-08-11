/*
 * @Author: chaoran.zhang
 * @Description: 对Vibration插件初始化以及定义了三个通用的震动接口
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/stfqgdv2xa5a2dgo
 * @Date: 2023-12-29 18：55：30
 */

using UnityEngine;

namespace FAT
{
    /// <summary>
    /// 完成振动插件的初始化，提供振动反馈接口
    /// 无论哪种接口，安卓的振动幅度都一致
    /// </summary>
    public static class VibrationManager
    {
        public static void Init()
        {
            Vibration.Init();
        }

        public static void VibrateLight()
        {
            if (!SettingManager.Instance.VibrationIsOn)
                return;

#if UNITY_IOS
            Vibration.VibrateIOS(ImpactFeedbackStyle.Light);
#elif UNITY_ANDROID
            AndroidVibrate();
#endif
        }

        public static void VibrateMedium()
        {
            if (!SettingManager.Instance.VibrationIsOn)
                return;

#if UNITY_IOS
            Vibration.VibrateIOS(ImpactFeedbackStyle.Medium);
#elif UNITY_ANDROID
            AndroidVibrate();
#endif
        }

        public static void VibrateHeavy()
        {
            if (!SettingManager.Instance.VibrationIsOn)
                return;

#if UNITY_IOS
            Vibration.VibrateIOS(ImpactFeedbackStyle.Heavy);
#elif UNITY_ANDROID
            AndroidVibrate();
#endif
        }

#if UNITY_ANDROID
        private static void AndroidVibrate()
        {
            if (Application.isMobilePlatform)
            {
                if (Vibration.AndroidVersion >= 29)
                {
                    AndroidJavaObject createPredefined = Vibration.vibrationEffect.CallStatic<AndroidJavaObject>("createPredefined", (int)2);
                    Vibration.vibrator.Call("vibrate", createPredefined);
                }
                else if (Vibration.AndroidVersion >= 26)
                {
                    AndroidJavaObject createOneShot = Vibration.vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", (long)50, -1);
                    Vibration.vibrator.Call("vibrate", createOneShot);
                }
                else
                {
                    Vibration.vibrator.Call("vibrate", new long[] {0, 5,5, 5, 5, 5, 5, -1});
                }
            }
        }
#endif
    }
}