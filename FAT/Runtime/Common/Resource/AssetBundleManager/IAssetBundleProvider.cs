/**
 * @Author: handong.liu
 * @Date: 2020-07-09 19:19:09
 */
using System.Collections;

namespace EL.Resource
{
    public interface IAssetBundleProvider
    {
        bool HasAssetBundle(string name);
        string GetIdentifer();
        IEnumerator ATLoadManifest();               //get manifest
        IEnumerator CoLoadAssetBundle(string name, ResourceAsyncTask task);     //if failed, don't resolve task, only resolve when success
    }
}