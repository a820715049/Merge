/**
 * @Author: handong.liu
 * @Date: 2021-01-22 12:59:28
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;


public class GameObjectPool
{
    public int Count => mElems.Count;
    public Transform Root => mRoot;
    private GameObject mPrefab;
    private System.Action<GameObject> mCreateHandler;
    private Stack<GameObject> mElems = new Stack<GameObject>();
    private Transform mRoot;

    public GameObjectPool(Transform root, GameObject prefab, System.Action<GameObject> createHandler = null)
    {
        mRoot = root;
        mPrefab = prefab;
        SetCreateHandler(createHandler);
    }

    public void SetCreateHandler(System.Action<GameObject> createHandler)
    {
        mCreateHandler = createHandler;
    }

    public void Recycle(GameObject obj)
    {
        mElems.Push(obj);
        obj.SetActive(false);
        obj.transform.SetParent(mRoot, false);
        DebugEx.FormatTrace("GameObjectPool ----> {0}@{1} recycle, pool size {2}", mRoot.name, mPrefab.name, mElems.Count);
    }

    public GameObject Alloc()
    {
        DebugEx.FormatTrace("GameObjectPool ----> {0}@{1} alloc, pool size {2}", mRoot.name, mPrefab.name, mElems.Count);
        GameObject ret = null;
        if(mElems.Count > 0)
        {
            ret = mElems.Pop();
        }
        else
        {
            ret = GameObject.Instantiate(mPrefab) as GameObject;
            ret.transform.SetParent(mRoot, false);
            mCreateHandler?.Invoke(ret);
        }
        ret.SetActive(true);
        return ret;
    }
}