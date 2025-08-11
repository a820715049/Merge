/*
 * @Author: qun.chao
 * @Date: 2020-07-27 14:34:32
 */
using UnityEngine;
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _Instance = null;
    public static T Instance
    {
        get
        {
            if (_Instance != null)
            {
                return _Instance;
            }

            _Instance = FindObjectOfType<T>();
            if (_Instance != null)
            {
                return _Instance;
            }

            GameObject go = new GameObject(typeof(T).ToString());
            DontDestroyOnLoad(go);
            _Instance = go.AddComponent<T>();

            return _Instance;
        }
    }
} 