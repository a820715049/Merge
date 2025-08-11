/**
 * @Author: handong.liu
 * @Date: 2020-08-04 20:41:05
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
namespace EL
{
    public class NetTexture2DPool : MonoSingleton<NetTexture2DPool>
    {
        private class CachedTexture
        {
            public Texture2D texture;
            public int refCount = 0;
            public bool noDelete = false;   //not deleted if memory low
        }
        public event System.Action<ICollection<string>> onTextureRefresh;
        private Dictionary<string, UnityEngine.Networking.UnityWebRequest> mLoadingTexture = new Dictionary<string, UnityEngine.Networking.UnityWebRequest>(); 
        private Dictionary<string, CachedTexture> mTextureCache = new Dictionary<string, CachedTexture>();
        private List<string> mAutoFreeTexture = new List<string>();
        private long mTotalMemory = 0;

        public void ClearAll()
        {
            foreach(var task in mLoadingTexture.Values)
            {
                task.Dispose();
            }
            mLoadingTexture.Clear();
            foreach(var t in mTextureCache.Values)
            {
                Object.DestroyImmediate(t.texture, true);
            }
            mTextureCache.Clear();
            mAutoFreeTexture.Clear();
            mTotalMemory = 0;
        }

        public void SetTexture(string url, Texture2D img)
        {
            if(img == null)
            {
                return;
            }
            CachedTexture cache = null;
            if(!mTextureCache.TryGetValue(url, out cache))
            {
                cache = mTextureCache[url] = new CachedTexture();
                cache.refCount = 1;
                mAutoFreeTexture.Add(url);
            }
            if(cache.texture != null)
            {
                mTotalMemory -= _CalculateTextureSize(cache.texture);
            }
            mTotalMemory += _CalculateTextureSize(img);
            cache.texture = img;
            if(mTotalMemory > 25000000)             //more than 25M
            {
                FreeUnusedTexture();
            }
        }

        public void SetTextureNoDelete(string url, Texture2D img)
        {
            if(img == null)
            {
                return;
            }
            CachedTexture cache = null;
            if(!mTextureCache.TryGetValue(url, out cache))
            {
                cache = mTextureCache[url] = new CachedTexture();
            }
            if(cache.texture != null)
            {
                mTotalMemory -= _CalculateTextureSize(cache.texture);
            }
            mTotalMemory += _CalculateTextureSize(img);
            cache.noDelete = true;
            cache.texture = img;
        }

        public Texture2D GetTexture(string url)
        {
            CachedTexture ret = null;
            mTextureCache.TryGetValue(url, out ret);
            if(ret != null && ret.texture == null)
            {
                DebugEx.FormatInfo("NetTexture2DPool.GetTexture ----> remove a destory texture {0}", url);
                mTextureCache.Remove(url);
                ret = null;
            }
            if(ret == null && !mLoadingTexture.ContainsKey(url))
            {
                var request = mLoadingTexture[url] = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
                request.SendWebRequest();
            }
            return ret?.texture;
        }

        public void RetainTexture(string url)
        {
            CachedTexture ret = null;
            if(mTextureCache.TryGetValue(url, out ret))
            {
                ret.refCount++;
            }
            else
            {
                DebugEx.FormatWarning("NetTexture2DPool.RetainTexture ----> no texture {0}", url);
            }
        }

        public void ReleaseTexture(string url)
        {
            CachedTexture ret = null;
            if(mTextureCache.TryGetValue(url, out ret))
            {
                ret.refCount--;
                if(ret.refCount < 0)
                {
                    DebugEx.FormatWarning("NetTexture2DPool.ReleaseTexture ----> not paired release for url {0}", url);
                }
            }
            else
            {
                DebugEx.FormatWarning("NetTexture2DPool.ReleaseTexture ----> no texture {0}", url);
            }
        }

        private List<string> mCachedUrlList = new List<string>();
        private HashSet<int> mAliveInstanceId = new HashSet<int>();
        public void FreeUnusedTexture()
        {
            mCachedUrlList.Clear();
            mAliveInstanceId.Clear();
            foreach(var cache in mTextureCache)
            {
                if(cache.Value.refCount <= 0 && !cache.Value.noDelete)
                {
                    mCachedUrlList.Add(cache.Key);
                }
                else if(cache.Value.texture != null)
                {
                    mAliveInstanceId.Add(cache.Value.texture.GetInstanceID());
                }
            }

            if(mCachedUrlList.Count > 0)
            {
                var before = mTotalMemory;
                for(int i = mCachedUrlList.Count - 1; i >= 0; i--)
                {
                    var k = mCachedUrlList[i];
                    var cache = mTextureCache[k];
                    if(cache.texture != null && mAliveInstanceId.Contains(cache.texture.GetInstanceID()))
                    {
                        //the same res is referenced in a alive cache
                        mCachedUrlList.RemoveAt(i);
                        continue;
                    }
                    if(cache.texture != null)
                    {
                        mTotalMemory -= _CalculateTextureSize(cache.texture);
                        UnityEngine.Object.Destroy(cache.texture);
                    }
                    mTextureCache.Remove(k);
                }
                DebugEx.FormatInfo("NetTexture2DPool.FreeUnusedTexture ----> remove texture {0}, memory {1} -> {2}", mCachedUrlList, before, mTotalMemory);
            }
        }

        private long _CalculateTextureSize(Texture2D tex)
        {
            return 3 * tex.width * tex.height;
        }

        private void Update()
        {
            if(mAutoFreeTexture.Count > 0)
            {
                foreach(var t in mAutoFreeTexture)
                {
                    if(mTextureCache.TryGetValue(t, out var cache))
                    {
                        cache.refCount --;
                    }
                }
                mAutoFreeTexture.Clear();
            }
            if(mLoadingTexture.Count > 0)
            {
                HashSet<string> loadedTexture = null;
                HashSet<string> errorTexture = null;
                foreach(var entry in mLoadingTexture)
                {
                    if(entry.Value.isDone)
                    {
                        if(entry.Value.isNetworkError || entry.Value.isHttpError)
                        {
                            DebugEx.FormatWarning("NetTexture2DPool.Update ----> error {0}:{1}", entry.Key, entry.Value.error);
                            if(errorTexture == null)
                            {
                                errorTexture = new HashSet<string>();
                            }
                            errorTexture.Add(entry.Key);
                        }
                        else
                        {
                            var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(entry.Value);
                            if(texture != null)
                            {
                                SetTexture(entry.Key, texture);
                                if(loadedTexture == null)
                                {
                                    loadedTexture = new HashSet<string>();
                                }
                                loadedTexture.Add(entry.Key);
                            }
                            else
                            {
                                DebugEx.FormatWarning("NetTexture2DPool.Update ----> error decode {0}", entry.Key);
                                if(errorTexture == null)
                                {
                                    errorTexture = new HashSet<string>();
                                }
                                errorTexture.Add(entry.Key);
                            }
                        }
                    }
                }
                if(errorTexture != null)
                {
                    foreach(var url in errorTexture)
                    {
                        mLoadingTexture.Remove(url);
                    }
                }
                if(loadedTexture != null)
                {
                    foreach(var url in loadedTexture)
                    {
                        mLoadingTexture.Remove(url);
                    }
                    onTextureRefresh?.Invoke(loadedTexture);
                }
            }
        }
    }
}