/*
 * @Author: qun.chao
 * @Date: 2020-09-14 12:01:38
 */

using UnityEngine;

public class UIGenericItemBase<T> : UIClearableItem
{
    protected T mData = default(T);
    protected bool mInitialized = false;
    protected bool mIsDirty = false;
    protected bool mIsDataReady = false;
    public T data { get { return mData; } }

    private void Awake()
    {
        _Init();
    }

    private void OnDestroy()
    {
        if (!mInitialized)
            return;

        DestroyComponents();
    }

    public void SetData(T data)
    {
        _Init();

        mData = data;
        mIsDataReady = true;

        if (gameObject.activeSelf)
        {
            UpdateOnDataChange();
            mIsDirty = false;
        }
        else
            mIsDirty = true;
    }

    //不修改数据 只触发刷新
    public void ForceRefresh()
    {
        if (gameObject.activeSelf)
        {
            UpdateOnForce();
        }
    }

    public override void Clear()
    {
        _Init();

        UpdateOnDataClear();
        mIsDirty = false;

        mData = default(T);
        mIsDataReady = false;

        // if (gameObject.activeSelf)
        // {
        //     UpdateOnDataClear();
        //     mIsDirty = false;
        // }
        // else
        //     mIsDirty = true;
    }

    protected virtual void OnEnable()
    {
        if (mIsDirty)
        {
            if (mIsDataReady)
                UpdateOnDataChange();
            else
                UpdateOnDataClear();

            mIsDirty = false;
        }
    }

    protected virtual void OnDisable()
    { }

    protected virtual void UpdateOnDataChange()
    { }

    protected virtual void UpdateOnDataClear()
    { }
    
    protected virtual void UpdateOnForce()
    { }

    protected virtual void InitComponents()
    { }

    protected virtual void DestroyComponents()
    { }

    private void _Init()
    {
        if (mInitialized)
            return;
        mInitialized = true;

        InitComponents();
    }
}