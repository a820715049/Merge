/**
 * @Author: handong.liu
 * @Date: 2021-02-03 14:36:46
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CenturyGame.AssetBundleManager.Runtime;
using EL;

public class ABManifest
{
    private Dictionary<string, string[]> mAllDependencyByName = new Dictionary<string, string[]>();
    private static readonly string[] kEmptyDependency = new string[0];

    public void ResetWithManifest(ResManifest mani)
    {
        mAllDependencyByName.Clear();
        if(mani != null && mani.Tables != null)
        {
            foreach(var item in mani.Tables)
            {
                mAllDependencyByName[item.Name] = item.Dependencies;
            }
        }
    }

    public IEnumerable<string> GetAllAssetBundles()
    {
        return mAllDependencyByName.Keys;
    }

    public bool HasItem(string abName)
    {
        return mAllDependencyByName.ContainsKey(abName);
    }

    public string[] GetDependency(string abName)
    {
        return mAllDependencyByName.GetDefault(abName, kEmptyDependency);
    }
}