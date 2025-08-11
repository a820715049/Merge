/**
 * @Author: handong.liu
 * @Date: 2020-07-29 14:28:09
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using EL;
using EL.Resource;
using System;
using Config;

public class UIImageRes : MonoBehaviour
{
    public enum FitMode
    {
        None,
        WidthFullFit,
    }

    public abstract class GraphicTarget
    {
        public abstract Component Target { get; }
        public abstract bool Valid { get; }
        public abstract Color Color { get; set; }
        public abstract bool Enabled { set; }

        public void SetupState(UIImageRes setting_)
        {
            if (setting_.HideWhenEmpty) {
                Enabled = Valid;
            }
        }
    }

    private class ImageTarget : GraphicTarget
    {
        public Image target;
        public override Component Target => target;
        public override bool Valid => target.sprite != null;
        public override Color Color { get => target.color; set => target.color = value; }
        public override bool Enabled { set => target.enabled = value; }
    }
    private class RawImageTarget : GraphicTarget {
        public RawImage target;
        public override Component Target => target;
        public override bool Valid => target.texture != null;
        public override Color Color { get => target.color; set => target.color = value; }
        public override bool Enabled { set => target.enabled = value; }
    }
    private class SpriteTarget : GraphicTarget {
        public SpriteRenderer target;
        public override Component Target => target;
        public override bool Valid => target.sprite != null;
        public override Color Color { get => target.color; set => target.color = value; }
        public override bool Enabled { set => target.enabled = value; }
    }
    private class EmptyTarget : GraphicTarget {
        public override Component Target => null;
        public override bool Valid => true;
        public override Color Color { get => Color.white; set { } }
        public override bool Enabled { set { } }
    }

    public MaskableGraphic image => (CacheImage as ImageTarget)?.target;
    public Color color { get => CacheImage.Color; set => CacheImage.Color = value; }
    public Action<UIImageRes> OnImageLoadFail { get; set; }
    [SerializeField]
    private bool mUseNativeSize = false;
    [SerializeField]
    private FitMode mFitMode = FitMode.None;
    [SerializeField]
    private bool mHideWhenEmpty = false;
    private bool HideWhenEmpty => true || mHideWhenEmpty;
    public bool stopAnimation;
    public bool keepGraphic = false;
    public GraphicTarget CacheImage => cacheImage ??= WrapTarget();
    private GraphicTarget cacheImage;
    private GameObject cachedPrefab;
    private ResourceAsyncTask mImageLoadTask = null;
    private bool isPrefab;
    private string mGroup;
    private string mAsset;
    private string mUrl;
    private string mCurrentRawImageUrl;
    private bool mCreateTask = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.EditorApplication.isPlaying) return;
        var selectable = GetComponent<Selectable>();
        keepGraphic = selectable != null;
    }
#endif

    public void OnEnable()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            return;
        }
#endif
        NetTexture2DPool.Instance.onTextureRefresh += _OnTextureRefresh;
        MessageCenter.Get<FAT.MSG.GAME_RES_UPDATE_FINISH>().AddListener(_OnResUpdateFinish);
        _Refresh();
        _CheckReloadResource();
    }
    public virtual void OnDisable()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            return;
        }
#endif
        MessageCenter.Get<FAT.MSG.GAME_RES_UPDATE_FINISH>().RemoveListener(_OnResUpdateFinish);
        NetTexture2DPool.Instance.onTextureRefresh -= _OnTextureRefresh;
        _ReleaseTexture();
    }

    public void SetImage(string group, string asset)
    {
        _ClearImage();
        mCreateTask = true;
        mGroup = group;
        mAsset = asset;
        _CreateTask();
        _Refresh();
    }

    public void SetImage((string, string) info) => SetImage(info.Item1 ?? "", info.Item2 ?? "");
    public void SetImage(AssetConfig info) => SetImage(info?.Group ?? "", info?.Asset ?? "");

    public void SetImage(string assetConfig)
    {
        if (string.IsNullOrEmpty(assetConfig)) {
            DebugEx.Warning("empty image config");
            return;
        }
        var conf = assetConfig.ConvertToAssetConfig();
        SetImage(conf.Group, conf.Asset);
    }

    public void SetImage(Sprite sprite_) {
        _ClearImage();
        if (CacheImage is ImageTarget image) {
            image.target.sprite = sprite_;
            cacheImage.SetupState(this);
        }
    }

    public void RefreshTarget() {
        cacheImage = WrapTarget();
    }

    public GraphicTarget WrapTarget() {
        if (TryGetComponent<Image>(out var image)) return new ImageTarget() { target = image };
        if (TryGetComponent<RawImage>(out var rawImage)) return new RawImageTarget() { target = rawImage };
        if (TryGetComponent<SpriteRenderer>(out var renderer)) return new SpriteTarget() { target = renderer };
        return new EmptyTarget();
    }

    private void _OnResUpdateFinish()
    {
        mImageLoadTask = null;
        _CheckReloadResource();
    }

    private void _CheckReloadResource()
    {
        if(!string.IsNullOrEmpty(mAsset) && string.IsNullOrEmpty(mUrl))
        {
            if(mImageLoadTask == null || mImageLoadTask.asset == null)
            {
                mImageLoadTask = null;
                _CreateTask();
                _CheckEmptyState();
            }
        }
    }

    public void SetUrl(string url)
    {
        _ClearImage();
        mUrl = url;
        _Refresh();
    }

    public void SetUseNativeSize(bool b)
    {
        mUseNativeSize = b;
    }

    private void _ClearImage()
    {
        if(mImageLoadTask != null)
        {
            mImageLoadTask = null;
        }
        mGroup = null;
        mAsset = null;
        mUrl = null;
        // DebugEx.FormatInfo("UIImageRes._ClearImage", GetHashCode());
        if (cacheImage is ImageTarget image)
        {
            image.target.sprite = null;
        }
        else if (cacheImage is RawImageTarget)
        {
            _ReleaseTexture();
        }
        else if (cacheImage is SpriteTarget renderer)
        {
            renderer.target.sprite = null;
        }
        if (cachedPrefab != null)
        {
            GameObject.Destroy(cachedPrefab);
            cachedPrefab = null;
        }
        PrefabMode(false);
        _CheckEmptyState();
    }

    public virtual void Clear()
    {
        _ClearImage();
    }

    public void Hide() {
        Clear();
        image.enabled = false;
    }

    private void _OnTextureRefresh(ICollection<string> urls)
    {
        if(mUrl != null && urls.Contains(mUrl))
        {
            _Refresh();
        }
    }

    private void _CreateTask()
    {
        if (mImageLoadTask == null) {
            if (PrefabMode(mAsset?.EndsWith(".prefab") ?? false))
            {
                mImageLoadTask = ResManager.LoadAsset<GameObject>(mGroup, mAsset);
            }
            else
            {
                //TODO: 如果贴图首次以Texture形式读出，则sprite == null, 即便是后来再以Sprite形式读入，因此我们总是以Sprite形式读出，以后需要优化ResManager里的读取，按类型保存task
                mImageLoadTask = ResManager.LoadAsset<Sprite>(mGroup, mAsset);
            }
        }
    }

    private bool PrefabMode(bool v_)
    {
        isPrefab = v_;
        bool visible;
        if (v_)
        {
            visible = keepGraphic;
        }
        else
        {
            visible = true;
        }
        if (cacheImage != null) cacheImage.Enabled = visible;
        return v_;
    }

    private void _CheckEmptyState()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)          //not check empty color in editor
        {
            return;
        }
#endif
        cacheImage?.SetupState(this);
    }

    private void _Refresh()
    {
        RefreshTarget();
        if(!string.IsNullOrEmpty(mUrl))
        {
            //it is a url
            var image = NetTexture2DPool.Instance.GetTexture(mUrl);
            if(image != null)
            {
                _ApplyTexture(image);
            }
        }
        else if(mImageLoadTask != null && mImageLoadTask.isSuccess)
        {
            if (isPrefab)
            {
                LoadPrefabDone();
            }
            else
            {
                LoadTextureDone();
            }
        }
        _CheckEmptyState();
    }

    private void LoadPrefabDone()
    {
        var obj = mImageLoadTask.asset as GameObject;
        if (obj == null)
        {
            DebugEx.Warning($"loaded {mGroup}:{mAsset} as prefab, but won't cast to GameObject");
            return;
        }
        if (cachedPrefab != null)
        {
            if (cachedPrefab.name == obj.name) return;
            GameObject.Destroy(cachedPrefab);
        }
        cachedPrefab = GameObject.Instantiate(obj);
        cachedPrefab.name = obj.name;
        var c = cachedPrefab.GetComponent<UIPreviewControl>();
        c?.Enabled(false);
        var rectParent = (RectTransform)transform;
        var rectFrame = (RectTransform)cachedPrefab.transform;
        var sizeParent = rectParent.rect.size;
        var sizeFrame = rectFrame.rect.size;
        var scaleX = sizeParent.x / sizeFrame.x;
        var scaleY = sizeParent.y / sizeFrame.y;
        // DebugEx.Warning($"{cachedPrefab.name} {sizeParent} {sizeFrame} {scaleX} {scaleY}");
        rectFrame.SetParent(transform);
        rectFrame.localPosition = Vector3.zero;
        rectFrame.localScale = Vector3.one * Mathf.Min(scaleX, scaleY);
        if (c != null && !stopAnimation) c.Enabled(true);
    }

    private void LoadTextureDone()
    {
        //it is a asset
        var sprite = mImageLoadTask.asset as Sprite;
        var tex = mImageLoadTask.asset as Texture2D;
        if (sprite != null)
        {
            if (cacheImage is ImageTarget image)
            {
                var target = image.target;
                target.sprite = sprite;
                // DebugEx.FormatInfo("UIImageRes._Refresh ----> get image2 {0}", GetHashCode());
                if (mUseNativeSize)
                {
                    target.SetNativeSize();
                }
                if (mFitMode != FitMode.None)
                {
                    _SetFullWidthFit(target);
                }
            }
            else if (cacheImage is SpriteTarget renderer)
            {
                renderer.target.sprite = sprite;
                // DebugEx.FormatInfo("UIImageRes._Refresh ----> get image3 {0}", GetHashCode());
            }
            else
            {
                _ApplyTexture(sprite.texture);
            }
        }
        else if (tex != null)
        {
            _ApplyTexture(tex);
        }
    }

    private void _ApplyTexture(Texture tex)
    {
        if (cacheImage is not RawImageTarget image) return;
        var target = image.target;
        target.texture = tex;
        // DebugEx.FormatInfo("UIImageRes._ApplyTexture ----> get image4 {0}", GetHashCode());
        if(mUseNativeSize)
        {
            target.SetNativeSize();
        }
        if (mFitMode != FitMode.None)
        {
            _SetFullWidthFit(target);
        }
        if(mUrl != mCurrentRawImageUrl)
        {
            mCurrentRawImageUrl = mUrl;
            NetTexture2DPool.Instance.RetainTexture(mCurrentRawImageUrl);
        }
    }

    private void _ReleaseTexture()
    {
        if(cacheImage != null && cacheImage is RawImageTarget image)
        {
            image.target.texture = null;
        }
        if(!string.IsNullOrEmpty(mCurrentRawImageUrl))
        {
            NetTexture2DPool.Instance.ReleaseTexture(mCurrentRawImageUrl);
            mCurrentRawImageUrl = null;
        }
    }

    private void _SetFullWidthFit(MaskableGraphic graphic)
    {
        var fullWidth = (graphic.transform.parent as RectTransform).rect.width;
        float height = 0f;

        if (graphic is Image sp)
        {
            height = fullWidth / sp.sprite.rect.width * sp.sprite.rect.height;
        }
        else if (graphic is RawImage raw)
        {
            height = fullWidth / raw.mainTexture.width * raw.mainTexture.height;
        }
        (transform as RectTransform).sizeDelta = new Vector2(fullWidth, height);

        if (TryGetComponent<LayoutElement>(out var layout))
        {
            layout.preferredWidth = fullWidth;
            layout.preferredHeight = height;
        }
    }

    private void Awake()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            return;
        }
#endif
        RefreshTarget();
        if(mCreateTask)
        {
            mCreateTask = false;
            _CreateTask();
        }
        _Refresh();
    }

    private void Update()
    {
        if(mImageLoadTask != null && !mImageLoadTask.keepWaiting)
        {
            if(mImageLoadTask.isSuccess && mImageLoadTask.asset != null)
            {
                // DebugEx.FormatTrace("MBRoleImage.Update ----> image load success ----> {0}@{1}", mAsset, mGroup);  
                _Refresh();
            }
            else
            {
                DebugEx.FormatWarning("MBImageRes.Update ----> image load fail ----> {0}@{1}:{2}", mAsset, mGroup, mImageLoadTask.error);       //TODO: optimize, don't do it every frame
                OnImageLoadFail?.Invoke(this);
            }
            mImageLoadTask = null;
        }
    }
}