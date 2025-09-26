/*
 * @Author: qun.chao
 * @Date: 2021-03-17 10:36:35
 */
using EL;
using UnityEngine;

public class MBAutoRelease : MonoBehaviour
{
    [SerializeField] private string poolType;
    [SerializeField] private float lifeTime;
    private bool isActive;

    public void Setup(string _poolType, float _lifeTime)
    {
        poolType = _poolType;
        lifeTime = _lifeTime;
        isActive = true;
    }

    private void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime < 0f)
        {
            _ReleaseSelf();
        }
    }

    public void Release()
    {
        _ReleaseSelf();
    }

    private void _ReleaseSelf()
    {
        if (!isActive)
            return;
        isActive = false;

        // // TODO: 早期因为一些effect既用于状态 也用于瞬时 这里删除比较干净
        // var go = gameObject;
        // Component.Destroy(this);
        if (GameObjectPoolManager.Instance.HasPool(poolType))
            GameObjectPoolManager.Instance.ReleaseObject(poolType, gameObject);
        else
        {
            Destroy(this);
            DebugEx.FormatError("{0} can't find GameObjectpool type : {1}", name, poolType);
        }
    }
}