/*
 * @Author: qun.chao
 * @Date: 2025-05-30 16:07:34
 */
using System;
using System.IO;
using Unity.Notifications;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
using System.Collections.Generic;
#endif
using UnityEngine;
using EL;
using EL.Resource;
using Cysharp.Threading.Tasks;

namespace FAT
{
    public class DeviceNotificationHelper
    {
        public static bool debug { get; set; } = false;
        public static readonly string icon_folder = "noticeicon";
        public static readonly string icon_bundle = "event_noticeicon";
        private static string icon_follder_full_path = Path.Combine(Application.persistentDataPath, icon_folder);
        private static int attachment_idx = 0;

        private static bool IconExists(string iconName)
        {
            return File.Exists(Path.Combine(icon_follder_full_path, iconName));
        }

        private static void EnsureNotificationIcon(string iconName)
        {
            if (IconExists(iconName))
                return;
            if (!Directory.Exists(icon_follder_full_path))
            {
                Directory.CreateDirectory(icon_follder_full_path);
            }
            InstallNotificationImage(icon_bundle, iconName).Forget();
        }

        private static string GetAttachmentPathForIOS(string iconName)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var fileExtension = Path.GetExtension(iconName);
            var baseFileName = Path.GetFileNameWithoutExtension(iconName);
            var attachmentFileName = $"{baseFileName}_{attachment_idx}_{timestamp}{fileExtension}";
            attachment_idx++; // 递增附件索引
            var attachmentPath = Path.Combine(icon_follder_full_path, attachmentFileName);
            File.Copy(Path.Combine(icon_follder_full_path, iconName), attachmentPath, true);
            return attachmentPath;
        }

        public static void ScheduleNotification(ref Notification notice_, TimeSpan interval_, string iconName = null)
        {
            if (debug)
            {
                DebugEx.Info($"notificationhelper debug: original interval {interval_} -> 5s");
                interval_ = new(0, 0, 5);
            }

            if (string.IsNullOrEmpty(iconName))
            {
                // 不需要icon 直接默认调度
                ScheduleInterval(notice_, interval_);
                return;
            }
            if (!IconExists(iconName))
            {
                // icon未准备好
                EnsureNotificationIcon(iconName);
                ScheduleInterval(notice_, interval_);
                return;
            }
#if UNITY_ANDROID

            // 安卓无需备份附件
            var notification = new AndroidNotification()
            {
                Title = notice_.Title,
                Text = notice_.Text,
                FireTime = DateTime.Now.Add(interval_),
                ShowInForeground = true,
                ShouldAutoCancel = true,

                // 设置数据
                IntentData = notice_.Data,
                // 设置图标
                LargeIcon = Path.Combine(icon_follder_full_path, iconName),
            };
            AndroidNotificationCenter.SendNotification(notification, "default");
            DebugEx.Info($"notificationhelper schedule Android notification {notice_.Title} with attachment @{interval_} later");

#elif UNITY_IOS

            // icon完备, 创建附件副本
            var attachmentPath = GetAttachmentPathForIOS(iconName);
            var notification = new iOSNotification()
            {
                Title = notice_.Title,
                Body = notice_.Text,
                Data = notice_.Data,
                ShowInForeground = true,
                ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
                Attachments = new List<iOSNotificationAttachment>()
                {
                    new iOSNotificationAttachment
                    {
                        Url = "file://" + attachmentPath,
                    }
                },
                Trigger = new iOSNotificationTimeIntervalTrigger()
                {
                    TimeInterval = interval_,
                    Repeats = false
                }
            };
            
            iOSNotificationCenter.ScheduleNotification(notification);
            DebugEx.Info($"notificationhelper schedule iOS notification {notice_.Title} with attachment @{interval_} later");

#endif
        }

        public static void CheckRespondedNotification()
        {
#if UNITY_ANDROID
            var last = AndroidNotificationCenter.GetLastNotificationIntent();
            if (last != null)
            {
                DebugEx.Info($"notificationhelper check launch notification {last.Notification.Title} {last.Notification.IntentData}");
                TryTrackNotificationResponse(last.Notification.IntentData);
            }
#elif UNITY_IOS
            var last = iOSNotificationCenter.GetLastRespondedNotification();
            if (last != null)
            {
                DebugEx.Info($"notificationhelper check launch notification {last.Title} {last.Data}");
                TryTrackNotificationResponse(last.Data);
            }
#endif
        }

        private static void TryTrackNotificationResponse(string data)
        {
            try
            {
                var customData = JsonUtility.FromJson<DeviceNotification.NotificationCustomData>(data);
                DataTracker.user_notifi.Track(customData.eventId,
                                                customData.eventFrom,
                                                customData.eventParam,
                                                customData.eventType,
                                                customData.noticeDetailId);
            }
            catch (Exception e)
            {
                DebugEx.Error($"notificationhelper try track notification response error {data} | {e.Message}");
            }
        }

        public static void ScheduleInterval(Notification notice_, TimeSpan interval_)
        {
            var schedule = new NotificationIntervalSchedule(interval_);
            NotificationCenter.ScheduleNotification(notice_, schedule);
            DebugEx.Info($"notificationhelper schedule {notice_.Title} @{interval_} later");
        }

        public static async UniTaskVoid InstallNotificationImage(string group, string asset)
        {
            var tex = await LoadIcon(group, asset);
            if (tex == null) return;
            var path = Path.Combine(icon_follder_full_path, asset);
            var readableTex = CreateReadableTexture(tex);
            var bytes = readableTex.EncodeToPNG();
            if (bytes == null)
            {
                DebugEx.Error($"notificationhelper install notification image error {group}/{asset}");
                return;
            }
            File.WriteAllBytes(path, bytes);
            UnityEngine.Object.Destroy(readableTex);
        }

        public static async UniTask<Texture2D> LoadIcon(string group, string asset)
        {
            var task = ResManager.LoadAsset<Texture2D>(group, asset);
            if (task.keepWaiting)
                await UniTask.WaitWhile(() => task.keepWaiting && !task.isCanceling);
            if (task.isSuccess)
                return task.asset as Texture2D;
            return null;
        }

        // 将压缩纹理转换为可读写的非压缩纹理
        private static Texture2D CreateReadableTexture(Texture2D source)
        {
            // 创建渲染纹理
            var renderTexture = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32
            );

            // 将原始纹理内容复制到渲染纹理
            Graphics.Blit(source, renderTexture);

            // 保存当前的活动渲染纹理
            var previousRenderTexture = RenderTexture.active;

            // 设置渲染纹理为活动状态
            RenderTexture.active = renderTexture;

            // 创建新的可读写纹理
            var readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);

            // 将渲染纹理的内容读取到新纹理
            readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTexture.Apply();

            // 恢复之前的渲染纹理
            RenderTexture.active = previousRenderTexture;

            // 释放临时渲染纹理
            RenderTexture.ReleaseTemporary(renderTexture);

            return readableTexture;
        }
    }
}