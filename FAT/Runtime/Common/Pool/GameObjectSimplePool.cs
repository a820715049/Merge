/**
 * @Author: handong.liu
 * @Date: 2021-03-18 20:18:33
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public class MBGameObjectSimplePoolStub : MonoBehaviour
{
    public GameObjectPool pool;
}

public static class GameObjectSimplePool
{
    private static Dictionary<GameObject, GameObjectPool> sPoolByGo = new Dictionary<GameObject, GameObjectPool>();
    private static GameObject sRoot = null;

    public static GameObject Alloc(GameObject prefab)
    {
        return _GetOrCreate(prefab).Alloc();
    }

    public static void Clear()
    {
        sPoolByGo.Clear();
        GameObject.Destroy(sRoot);
        sRoot = null;
    }

    public static void Free(GameObject go)
    {
        var mb = go.GetComponent<MBGameObjectSimplePoolStub>();
        if(mb == null)
        {
            DebugEx.FormatWarning("GameObjectSimplePool::Free ----> no stub for go {0}", go.name);
            return;
        }
        if(sRoot == null || mb.pool.Root == null)
        {
            GameObject.Destroy(go);
        }
        else
        {
            mb.pool.Recycle(go);
        }
    }

    private static GameObjectPool _GetOrCreate(GameObject prefab)
    {
        if(sRoot == null)
        {
            sRoot = new GameObject("GameObjectSimplePool");
            GameObject.DontDestroyOnLoad(sRoot);
        }
        if(!sPoolByGo.TryGetValue(prefab, out var ret))
        {
            var trans = new GameObject(prefab.name);
            trans.transform.SetParent(sRoot.transform);
            ret = new GameObjectPool(trans.transform, prefab, null);
            ret.SetCreateHandler((go)=>go.AddComponent<MBGameObjectSimplePoolStub>().pool = ret);
            sPoolByGo[prefab] = ret;
        }
        return ret;
    }
}