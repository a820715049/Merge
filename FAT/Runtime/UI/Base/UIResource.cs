/*
 * @Author: qun.chao
 * @Date: 2023-10-12 10:49:04
 */
using EL;
using Config;

namespace FAT
{
    public class UIResource
    {
        public string name;
        public string prefabPath;
        public string prefabGroup;
        public UILayer layer;
        public SfxValue openSound;
        public SfxValue closeSound;
        public bool ignoreCloseAll { get; private set; }
        public bool IsSupportNavBack { get; private set; }  //是否支持返回关闭界面
        public bool IsIgnoreNavBack { get; private set; }  //是否忽略返回关闭界面的检查
        public bool IsAllowHideMainUI { get; private set; } //界面在打开时是否允许隐藏主界面UI(四周功能入口按钮)
        public bool IsAllowHideStatusUI { get; private set; } //界面在打开时是否允许隐藏顶部资源栏
        public bool IsUITips { get; private set; } //是否为tips界面，用于tips自动关闭逻辑
        
        public bool ActivePopup { get; set; }

        public UIResource(string path, UILayer _layer, string group = "ui_common")
        {
            name = path;
            prefabPath = path;
            layer = _layer;
            prefabGroup = group;
            if (_layer > UILayer.Status && _layer < UILayer.Top)
            {
                openSound = new SfxValue() { eventName = "OpenWindow" };
                closeSound = new SfxValue() { eventName = "CloseWindow" };
            }
        }

        public UIResource(string path, string group, UIResource ref_)
        {
            name = path;
            prefabPath = path;
            layer = ref_.layer;
            prefabGroup = group;
            openSound = ref_.openSound;
            closeSound = ref_.closeSound;
            ignoreCloseAll = ref_.ignoreCloseAll;
            IsSupportNavBack = ref_.IsSupportNavBack;
            IsIgnoreNavBack = ref_.IsIgnoreNavBack;
            IsAllowHideMainUI = ref_.IsAllowHideMainUI;
            IsAllowHideStatusUI = ref_.IsAllowHideStatusUI;
        }

        public bool Match(AssetConfig v_) {
            return v_.Asset == prefabPath && v_.Group == prefabGroup;
        }

        public UIResource SetMute()
        {
            CustomSoundEvent(null, null);
            return this;
        }

        public UIResource CustomSoundEvent(string openSfx, string closeSfx)
        {
            openSound.eventName = openSfx;
            closeSound.eventName = closeSfx;
            return this;
        }

        public UIResource IgnoreCloseAll()
        {
            ignoreCloseAll = true;
            return this;
        }

        public UIResource SupportNavBack()
        {
            IsSupportNavBack = true;
            return this;
        }
        
        public UIResource IgnoreNavBack()
        {
            IsIgnoreNavBack = true;
            return this;
        }

        //界面打开时是否支持隐藏主界面UI(默认允许)和资源栏UI(默认不允许)
        public UIResource AllowHideUI(bool hideMainUI = true, bool hideStatusUI = false)
        {
            IsAllowHideMainUI = hideMainUI;
            IsAllowHideStatusUI = hideStatusUI;
            return this;
        }

        public UIResource IsTips()
        {
            IsUITips = true;
            return this;
        }

        public void Replace(AssetConfig conf_, bool try_ = false) => Replace(conf_?.Group, conf_?.Asset, try_);
        public void Replace(string group_, string asset_, bool try_ = false) {
            if (string.IsNullOrEmpty(group_) && string.IsNullOrEmpty(asset_)) {
                if (!try_) DebugEx.Warning($"trying to override {prefabGroup}:{prefabPath} with empty info");
                return;
            }
            if (prefabGroup == group_ && prefabPath == asset_) return;
            prefabGroup = group_;
            prefabPath = asset_;
            #if UNITY_EDITOR
            DebugEx.Info($"ui res {name} redirect to {prefabGroup}:{prefabPath}");
            #endif
            UIManager.Instance.DropCache(this);
        }

        public void Open(params object[] args)
        {
            UIManager.Instance.OpenWindow(this, args);
        }

        public void Close()
        {
            UIManager.Instance.CloseWindow(this);
        }
    }

    public class UIResourceIdSuffix : UIResource
    {
        public int suffix;
        private string suffixSeg;

        public UIResourceIdSuffix(string path, UILayer _layer, string group = "ui_common") : base(path, _layer, group)
        { }

        public void ClearSuffix()
        {
            if (suffix == 0) return;
            suffix = 0;
            UIManager.Instance.DropCache(this);
            var lastSeg = suffixSeg;
            suffixSeg = $".prefab";
            prefabPath = prefabPath.Replace(lastSeg, suffixSeg);
        }

        public void Suffix(int suffix_)
        {
            if (suffix == suffix_) return;
            suffix = suffix_;
            UIManager.Instance.DropCache(this);
            if (suffixSeg != null)
            {
                var lastSeg = suffixSeg;
                suffixSeg = $"{suffix_}.prefab";
                prefabPath = prefabPath.Replace(lastSeg, suffixSeg);
            }
            else
            {
                suffixSeg = $"{suffix_}.prefab";
                prefabPath = prefabPath.Replace(".prefab", suffixSeg);
            }
        }
    }
}