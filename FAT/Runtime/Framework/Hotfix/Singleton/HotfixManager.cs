/**
 * @Author: handong.liu
 * @Date: 2021-06-11 12:26:03
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using EL.Resource;
using IFix;
using IFix.Core;

/*
2023.12.22
目前项目设置的 Api Compatibility Level* 为默认值 => .NET Standad 2.1
需要在link.xml里配置避免裁剪, 配置粒度还需优化

加载patch前，需要对目标dll进行inject操作
编辑器: 直接在菜单操作 [InjectFix/Inject]
打包流程: inject的目标是 "./Library/Bee/PlayerScriptAssemblies/asmdef_fat_runtime.dll"

patch文件加载逻辑见HotfixManager
按当前文件获取机制，在编辑器测试时，patch需要放到 "StreamingAssets/data/hotfix/asmdef_fat_runtime.patch.bytes"
*/
namespace Hotfix
{
    public class HotfixManager : EL.Singleton<HotfixManager>
    {
        public bool isHotfix => mIsHotfix;
        private const string kHotfixDataPath = "hotfix/asmdef_fat_runtime.patch.bytes";
        private bool mIsHotfix = false;
        public IEnumerator ATInitPatch()
        {
            var task = new SimpleAsyncTask();
            yield return task;

            // 函数内会拼接扩展名
            var file = FAT.ConfHelper.GetTableLoadPath(kHotfixDataPath, out _, false, string.Empty);
            var fullPath = FAT.ConfHelper.FixFilePath(file);
            DebugEx.Info($"HotFixManager::ATInitPatch origin {file} after {fullPath}");
            using (var req = UnityEngine.Networking.UnityWebRequest.Get(fullPath))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    DebugEx.Info("HotFixManager::ATInitPatch ----> patch loaded");
                    try
                    {
                        IFix.Core.PatchManager.Load(new System.IO.MemoryStream(req.downloadHandler.data));
                        mIsHotfix = true;
                        task.ResolveTaskSuccess();
                    }
                    catch (System.Exception ex)
                    {
                        DebugEx.Warning($"HotFixManager::ATInitPatch ----> patch apply fail {ex}");
                        task.ResolveTaskFail();
                    }
                }
                else
                {
                    DebugEx.Warning($"HotFixManager::ATInitPatch failed {req.result} {req.error}");
                    task.ResolveTaskFail();
                }
            }
        }
    }
}