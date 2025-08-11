/*
 * @Author: qun.chao
 * @Date: 2023-10-11 18:21:17
 */
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using EL;
using EL.Resource;
using Cysharp.Threading.Tasks;

namespace FAT
{
    public class UIManager : MonoSingleton<UIManager>, IGameModule
    {
        // 常规ui的canvas
        public RectTransform CanvasRoot { get; private set; }
        // 常规ui所在的根结点 | 位于canvas的子层级 size已限制在美术设计的极限尺寸内
        public RectTransform SafeRoot { get; private set; }
        public int LoadingCount => _curLoadingResCount;
        //动画trigger名字
        public static readonly int IdleShowAnimTrigger = Animator.StringToHash("IdleShow");
        public static readonly int IdleHideAnimTrigger = Animator.StringToHash("IdleHide");
        public static readonly int OpenAnimTrigger = Animator.StringToHash("Show");
        public static readonly int CloseAnimTrigger = Animator.StringToHash("Hide");
        public static readonly int PauseAnimTrigger = Animator.StringToHash("HideStack");
        public static readonly int ResumeAnimTrigger = Animator.StringToHash("ShowStack");
        public bool IsHideMainUI = false; //记录主界面显隐状态
        public bool IsHideStatusUI = false; //记录状态栏显隐状态
        private bool isBlocked => _block.raycastTarget || _blockLayerObj.activeSelf;
        public bool IsBlocked => isBlocked;
        private CanvasScaler _canvasScaler; 

        #region safe area
        //UI安全区相关
        private Rect _curSafeArea;
        private Rect _lastSafeArea;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        #endregion

        private Vector2 _safeAreaAnchorMin = Vector2.zero;
        private Vector2 _safeAreaAnchorMax = Vector2.one;
        //layer
        private List<RectTransform> _uiLayerList;
        private GameObject _blockLayerObj;  //阻挡点击层
        private Transform _cacheLayerTrans; //缓存UI层
        private Graphic _block;
        //记录当前外部界面逻辑屏蔽点击的情况
        private int _miscBlockCount;
        
        //当前正在加载的界面
        private Dictionary<UIResource, int> _loadRequestDict;
        //所有已经成功加载完成的界面
        private Dictionary<UIResource, UIBase> _allCacheUIDict;
        //当前正在打开的UI队列
        private List<(UIResource ui, Action imp)> _openingUIQueue;
        //记录当前正在加载中的资源数量
        private int _curLoadingResCount;
        //记录当前内部逻辑屏蔽点击的情况
        private int _curBlockCount;
        
        //当界面环境处于idle状态时要依次执行的回调List
        private List<(string key, int priority, Action callback)> _idleActionList;
        //上次注册请求回调时的游戏帧数
        private int _cbReqFrameCount = -1;
        //外部逻辑控制是否开关idle action逻辑运行
        private bool _isCloseIdleAction = false;
        
        void IGameModule.Reset()
        {
            _Init();
            _Reset();
        }
        void IGameModule.LoadConfig() { }
        void IGameModule.Startup() { }

        //打开指定界面并在成功打开后执行回调(时机在PreOpen和PostOpen之间)
        public void OpenWindowAndCallback(UIResource uiRes, Action openSuccessCb, params object[] args)
        {
            _OpenWindowImp(uiRes, openSuccessCb, args);
        }

        //打开指定界面
        public void OpenWindow(UIResource uiRes, params object[] args)
        {
            _OpenWindowImp(uiRes, null, args);
        }
        
        //关闭指定界面
        public void CloseWindow(UIResource uiRes)
        {
            _CloseWindowImp(uiRes);
        }
        
        public UIBase TryGetUI(UIResource res)
        {
            return _allCacheUIDict.TryGetValue(res, out var ui) ? ui : null;
        }
        
        public RectTransform GetLayerRootByType(UILayer type)
        {
            return _uiLayerList[(int)type];
        }

        public bool TryGetCache(UIResource res_, out UIBase root_) {
            return _allCacheUIDict.TryGetValue(res_, out root_);
        }
        
        public void DropCache(UIResource uiRes) {
            _allCacheUIDict.Remove(uiRes);
            _loadRequestDict.Remove(uiRes);
        }

        public bool IsBlocking()
        {
            return _block.raycastTarget;
        }

        public void Block(bool v_) {
            if (v_)
            {
                ++_miscBlockCount;
            }
            else
            {
                --_miscBlockCount;
            }

            if (_miscBlockCount > 0)
            {
                if (!_block.raycastTarget)
                    _block.raycastTarget = true;
            }
            else if (_miscBlockCount == 0)
            {
                if (_block.raycastTarget)
                    _block.raycastTarget = false;
            }
            else
            {
                Debug.LogErrorFormat("[UIManager.Block] : block unbalance {0}", _miscBlockCount);
            }
        }

        public void Visible(bool v_, int from_ = 0, int to_ = int.MaxValue) {
            to_ = Mathf.Min(to_, _uiLayerList.Count - 1) + 1;
            for (var k = from_; k < to_; ++k) {
                _uiLayerList[k].gameObject.SetActive(v_);
            }
        }

        public void Visible(UIResource ui_, bool v_) {
            var e = TryGetCache(ui_, out var n);
            if (!e) {
                if (v_) OpenWindow(ui_);
                return;
            }
            n.Visible(v_);
        }

        public bool TryChangeUILayer(UIResource res, UILayer targetLayer)
        {
            if (_allCacheUIDict.TryGetValue(res, out UIBase ui))
            {
                //层级与现在相同时 不做处理
                if (ui.BelongLayer == targetLayer)
                    return false;
                //设置目标层级
                _SetupLayer(ui, targetLayer);
                //将UI提到最前面
                var layer = _uiLayerList[(int)targetLayer];
                if (ui.transform.GetSiblingIndex() != layer.childCount - 1)
                {
                    ui.transform.SetAsLastSibling();
                }
            }
            return false;
        }
        
        //传入任意UI节点 返回其所属的UI界面名称
        public string GetBelongUIPrefabName(Transform node)
        {
            var current = node;
            while (current.parent != null)
            {
                if (current.parent.TryGetComponent<UIBase>(out var uiBase))
                {
                    return uiBase.ResConfig.prefabPath;
                }
                else
                {
                    current = current.parent;
                }
            }
            return "";
        }

        //对外提供检测目前UI处理idle状态的接口
        public bool CheckUIIsIdleState()
        {
            //游戏未在运行状态时返回
            if (!Game.Instance.isRunning)
                return false;
            if (!GameProcedure.IsInGame)
                return false;
            // 处于加载状态时返回
            if (LoadingCount != 0)
                return false;
            if(isBlocked)
                return false;
            //弹脸系统有弹脸界面时返回
            if (Game.Manager.screenPopup.HasPopup())
                return false;
            //新手引导开着时返回
            if (IsOpen(UIConfig.UIGuide))
                return false;
            //迷你棋盘界面开着时返回(迷你棋盘界面略有特殊 固直接在此判断)
            if (Game.Manager.miniBoardMan.CheckMiniBoardUIOpen() || Game.Manager.miniBoardMultiMan.CheckMiniBoardUIOpen())
                return false;
            //特殊奖励系统显示繁忙时返回
            if (Game.Manager.specialRewardMan.IsBusyShow())
                return false;
            //Above和Sub层级不为空时返回
            if (!_IsLayerEmpty(UILayer.AboveStatus) || !_IsLayerEmpty(UILayer.SubStatus))
                return false;
            return true;
        }

        public bool CheckUIIsIdleStateForPopup(bool checkSpecialReward = false)
        {
            //游戏未在运行状态时返回
            if (!Game.Instance.isRunning)
                return false;
            if (!GameProcedure.IsInGame)
                return false;
            // 处于加载状态时返回
            if (LoadingCount != 0)
                return false;
            if (isBlocked)
                return false;
            //新手引导开着时返回
            if (IsOpen(UIConfig.UIGuide))
                return false;
            //迷你棋盘界面开着时返回(迷你棋盘界面略有特殊 固直接在此判断)
            if (Game.Manager.miniBoardMan.CheckMiniBoardUIOpen() || Game.Manager.miniBoardMultiMan.CheckMiniBoardUIOpen())
                return false;
            //奖励显示时返回
            if (IsOpen(UIConfig.UIRandomBox) || IsOpen(UIConfig.UICardPackOpen))
                return false;
            if (checkSpecialReward)
            {
                //特殊奖励系统显示繁忙时返回
                if (Game.Manager.specialRewardMan.IsBusyShow())
                    return false;
            }
            //Above和Sub层级不为空时返回
            if (!_IsLayerEmpty(UILayer.AboveStatus) || !_IsLayerEmpty(UILayer.SubStatus))
                return false;
            return true;
        }

        #region UI状态判断方法

        public bool IsOpen(UIResource res)
        {
            if (_allCacheUIDict.TryGetValue(res, out UIBase ui))
            {
                if (ui.IsOpen())
                    return true;
            }
            return false;
        }
        
        public bool IsShow(UIResource res)
        {
            if (_allCacheUIDict.TryGetValue(res, out UIBase ui))
            {
                if (ui.IsShow())
                    return true;
            }
            return false;
        }

        public bool IsPause(UIResource res)
        {
            if (_allCacheUIDict.TryGetValue(res, out UIBase ui))
            {
                if (ui.IsPause())
                    return true;
            }
            return false;
        }

        public bool IsClosed(UIResource res)
        {
            if (_allCacheUIDict.TryGetValue(res, out UIBase ui))
            {
                if (ui.IsClosed())
                    return true;
            }
            return false;
        }
        
        public bool IsIdleIn(UIResource res)
        {
            return _openingUIQueue.Count == 0 && IsShow(res)
                && _IsLayerEmpty(UILayer.AboveStatus) && _IsLayerEmpty(UILayer.SubStatus);
        }

        public bool IsValid(UIResource res)
        {
            if (_allCacheUIDict.ContainsKey(res)) return true;
            foreach(var (r, _) in _openingUIQueue) {
                if (r == res) return true;
            }
            return false;
        }
        
        public bool IsVisible(UIResource res)
        {
            if (_allCacheUIDict.TryGetValue(res, out UIBase ui))
            {
                if (ui.IsVisible())
                    return true;
            }
            return false;
        }

        #endregion

        #region 坐标转换 适配相关

        //世界坐标转UI本地坐标
        public Vector3 TransWorldPosToLocal(Vector3 worldPos)
        {
            //世界坐标转屏幕坐标
            var screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
            //屏幕坐标转UI本地坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasRoot, screenPos, null, out var localPos);
            return localPos;
        }

        //传入tips界面的初始显示位置 以及其宽度 获取tips界面在宽度上适配后的位置
        //默认面板锚点在底部中间
        public Vector3 FitUITipsPosByWidth(Vector3 startPos, float tipsWidth)
        {
            float halfWidth = tipsWidth / 2;
            float halfCanvasWidth = SafeRoot.rect.width / 2;
            //只考虑宽度 超框了就减去超出的距离
            if (startPos.x > 0)
            {
                if (startPos.x + halfWidth > halfCanvasWidth)
                {
                    startPos.x -= startPos.x + halfWidth - halfCanvasWidth;
                }
            }
            else
            {
                if (startPos.x - halfWidth < -halfCanvasWidth)
                {
                    var delta = -(startPos.x - halfWidth) - halfCanvasWidth;
                    startPos.x += delta;
                }
            }
            return startPos;
        }

        //传入tips界面的初始显示位置 以及其高度 获取tips界面在高度上适配后的位置
        //默认面板锚点在底部中间
        public Vector3 FitUITipsPosByHeight(Vector3 startPos, float tipsHeight, float topOffset, out bool isOverTop)
        {
            //安全区上下部分的长度可能会不一样 所以分开计算
            var canvasHeight = SafeRoot.rect.height;
            var canvasHalfHeight = canvasHeight / 2;
            var topDeltaY = canvasHeight - _curSafeArea.height - _curSafeArea.y;
            var topHalfHeight = canvasHalfHeight - topDeltaY - 100; //顶部空间额外减去100 避免盖住状态栏
            var bottomDeltaY = _curSafeArea.y;
            var bottomHalfHeight = canvasHalfHeight - bottomDeltaY;
            isOverTop = false;

            // 由于锚点在底部中心，只需计算顶部位置是否超出Canvas顶部
            float topPosition = startPos.y + tipsHeight;
            // 检查顶部是否超出Canvas顶部边界
            if (topPosition > topHalfHeight)
            {
                //如果认为超出顶部边界 则直接在下方位置显示
                startPos.y = startPos.y - tipsHeight - topOffset * 2;
                isOverTop = true;
            }
            // 检查底部是否低于Canvas底部边界（startPos.y本身就是底部的位置）
            else if (startPos.y < -bottomHalfHeight)
            {
                float underflow = -bottomHalfHeight - startPos.y;
                startPos.y += underflow; // 向上调整位置以适应Canvas
            }
            return startPos;
        }

        #endregion
        
        /// <summary>
        /// 注意：同一业务逻辑内不应该滥用此接口 最好是相应Manager中自己包好回调List再注册进来
        /// 注意：只应在可预见的时间段内需要idle action时才调用注册，不要一开始就注册进来 寄希望于UIManager帮忙检测！！！
        /// 外部业务逻辑注册回调 会在界面环境处于idle状态时按优先级执行 idle状态命中的时机相对靠后具体可看内部逻辑
        /// </summary>
        /// <param name="reqKey">业务逻辑自主命名key  命名格式可以为 "ui_idle_xxx"</param>
        /// <param name="priority">多个回调执行的优先级 按从大到小的顺序执行</param>
        /// <param name="callback">回调方法</param>
        public void RegisterIdleAction(string reqKey, int priority, Action callback)
        {
            DebugEx.FormatInfo("[UIManager.RegisterIdleAction] : reqKey = {0}, priority = {1}", reqKey, priority);
            _idleActionList.RemoveAll(x => x.key == reqKey);
            _idleActionList.Add((reqKey, priority, callback));
            if (_idleActionList.Count > 1)
            {
                _idleActionList.Sort((a, b) => b.priority - a.priority);
            }
            _cbReqFrameCount = Time.frameCount;
        }

        //外部逻辑在合适时机调用开启/关闭IdleAction逻辑运行状态
        //注意本接口应该成对调用！
        public void ChangeIdleActionState(bool isOpen)
        {
            _isCloseIdleAction = !isOpen;
        }

        #region 初始化相关

        private static bool _hasInit = false;
        private void _Init()
        {
            //使用标记位确保只初始化一次
            if (_hasInit)
                return;
            _hasInit = true;
            _InitData();
            _InitCanvas();
            var changed = _RefreshCanvasScaler();
            _RefreshSafeRoot();
            _RefreshSafeArea();
            _InitLayer();
            _InitAnimListener();

            if (changed)
            {
                _DelayRefreshCanvasAndSafeArea().Forget();
            }
        }
        
        //数据初始化
        private void _InitData()
        {
            _loadRequestDict = new Dictionary<UIResource, int>();
            _openingUIQueue = new List<(UIResource ui, Action imp)>();
            _allCacheUIDict = new Dictionary<UIResource, UIBase>();
            _uiLayerList = new List<RectTransform>();
            _idleActionList = new List<(string key, int priority, Action callback)>();
        }

        /*
        基础    => 9:16     (1080/1920)
        最高    => 9:19.5   (1080/2340)
        最宽    => 3:4      (1440/1920)
        超出部分用黑边补足
        */
        private void _RefreshSafeRoot()
        {
            var canvas_root = CanvasRoot;
            var safe_root = SafeRoot;
            if (_canvasScaler.matchWidthOrHeight < 1f)
            {
                var maxH = 2340;
                // 宽适配 => 设计宽度为基准 高度自由
                if (canvas_root.rect.height > maxH)
                {
                    var width = (canvas_root.rect.height - maxH) * 0.5f;
                    safe_root.offsetMax = new Vector2(0f, -width);
                    safe_root.offsetMin = new Vector2(0f, width);
                    _SetEdge(width, 0f);
                }
                else
                {
                    safe_root.offsetMax = Vector2.zero;
                    safe_root.offsetMin = Vector2.zero;
                    _SetEdge(0f, 0f);
                }
            }
            else
            {
                var maxW = 1440;
                // 高适配 => 设计高度为基准 宽度自由
                if (canvas_root.rect.width > maxW)
                {
                    var width = (canvas_root.rect.width - maxW) * 0.5f;
                    safe_root.offsetMax = new Vector2(-width, 0f);
                    safe_root.offsetMin = new Vector2(width, 0f);
                    _SetEdge(0f, width);
                }
                else
                {
                    safe_root.offsetMax = Vector2.zero;
                    safe_root.offsetMin = Vector2.zero;
                    _SetEdge(0f, 0f);
                }
            }
        }

        private void _SetEdge(float width_v, float width_h)
        {
            var edgeRoot = CanvasRoot.Access<RectTransform>("Edge");
            var top = edgeRoot.Access<RectTransform>("Top");
            var bottom = edgeRoot.Access<RectTransform>("Bottom");
            var left = edgeRoot.Access<RectTransform>("Left");
            var right = edgeRoot.Access<RectTransform>("Right");
            top.sizeDelta = new Vector2(0f, width_v);
            bottom.sizeDelta = new Vector2(0f, width_v);
            left.sizeDelta = new Vector2(width_h, 0f);
            right.sizeDelta = new Vector2(width_h, 0f);
        }

        // 初始化UI安全区
        private void _RefreshSafeArea()
        {
            var canvas_root = CanvasRoot;
            var safe_root = SafeRoot;
            // 设备安全区
            var safeMin = Screen.safeArea.min / canvas_root.localScale.x;
            var safeMax = Screen.safeArea.max / canvas_root.localScale.x;
            // 美术设计区域 换算到canvas的左下角为初始位置的坐标 | 外部黑色填充
            var designMin = safe_root.rect.min - canvas_root.rect.min;
            var designMax = safe_root.rect.max - canvas_root.rect.min;
            // 以SafeRoot为基准的显示区域
            var max = Vector2.Min(safeMax, designMax) - designMin;
            var min = Vector2.Max(safeMin, designMin) - designMin;
            DebugEx.Info($"[SafeArea] min = {min}, max = {max}");

            var width = safe_root.rect.width;
            var height = safe_root.rect.height;
            var anchorMin = new Vector2(min.x / width, min.y / height);
            var anchorMax = new Vector2(max.x / width, max.y / height);

            if (anchorMin.x >= 0 && anchorMin.y >= 0 && anchorMax.x >= 0 && anchorMax.y >= 0)
            {
                _safeAreaAnchorMin = anchorMin;
                _safeAreaAnchorMax = anchorMax;
                _curSafeArea = new Rect(min, max - min);
            }
            else
            {
                _safeAreaAnchorMin = Vector2.zero;
                _safeAreaAnchorMax = Vector2.one;
                _curSafeArea = safe_root.rect;
            }

            _lastSafeArea = Screen.safeArea;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            DebugEx.Info($"[SafeArea] rect is {_curSafeArea}, anchorMin = {anchorMin}, anchorMax = {anchorMax}");
        }
        
        //初始化Canvas
        private void _InitCanvas()
        {
            var uiRootObj = GameObject.Find("Root_UI");
            DontDestroyOnLoad(uiRootObj);
            var canvasRoot = uiRootObj.transform.Find("Canvas_Common").transform;
            if (canvasRoot == null)
            {
                DebugEx.Error("[UIManager._InitCanvas]: canvasRoot is null!");
                return;
            }
            CanvasRoot = canvasRoot as RectTransform;
            _canvasScaler = canvasRoot.GetComponent<CanvasScaler>();
            SafeRoot = canvasRoot.Find("SafeRoot") as RectTransform;
        }
        
        private bool _RefreshCanvasScaler()
        {
            var preMatch = _canvasScaler.matchWidthOrHeight;
            var sa = Screen.safeArea;
            if (sa.width / sa.height > 1080f / 1920f + 0.00001f)
            {
                _canvasScaler.matchWidthOrHeight = 1f;
            }
            else
            {
                _canvasScaler.matchWidthOrHeight = 0f;
            }
            var changed = !Mathf.Equals(preMatch, _canvasScaler.matchWidthOrHeight);
            return changed;
        }

        //返回值<1表示宽度适配  >=1表示高度适配  默认为宽度适配
        public float GetCanvasMatchValue()
        {
            return _canvasScaler == null ? 0 : _canvasScaler.matchWidthOrHeight;
        }

        public Vector2 GetCanvasRefRes()
        {
            return _canvasScaler == null ? Vector2.zero : _canvasScaler.referenceResolution;
        }

        private void _InitLayer()
        {
            if (SafeRoot == null)
                return;
            for (int i = 0; i < (int)UILayer.Max; i++)
            {
                var layer = new GameObject(((UILayer)i).ToString(), typeof(RectTransform)).transform as RectTransform;
                layer.transform.SetParent(SafeRoot);
                layer.gameObject.layer = LayerMask.NameToLayer("UI");
                layer.sizeDelta = Vector2.zero;
                layer.anchorMin = Vector2.zero;
                layer.anchorMax = Vector2.one;
                layer.anchoredPosition3D = Vector3.zero;
                layer.localScale = Vector3.one;
                _uiLayerList.Add(layer);
            }
            //初始化阻挡点击层级
            _blockLayerObj = GetLayerRootByType(UILayer.Block).gameObject;
            _blockLayerObj.AddComponent<CanvasRenderer>();
            _blockLayerObj.AddComponent<UnityEngine.UI.Extensions.NonDrawingGraphic>();
            _blockLayerObj.SetActive(false);
            var obj = GetLayerRootByType(UILayer.BlockUser).gameObject;
            _block = obj.AddComponent<UnityEngine.UI.Extensions.NonDrawingGraphic>();
            _block.raycastTarget = false;
            //初始化prefab缓存层级
            _cacheLayerTrans = GetLayerRootByType(UILayer.Cache);
            _cacheLayerTrans.gameObject.SetActive(false);
        }

        //初始化时监听动画相关事件
        private void _InitAnimListener()
        {
            MessageCenter.Get<MSG.UI_OPEN_ANIM_FINISH>().AddListenerUnique(_OnOpenAnimFinish);
            MessageCenter.Get<MSG.UI_CLOSE_ANIM_FINISH>().AddListenerUnique(_OnCloseAnimFinish);
            MessageCenter.Get<MSG.UI_PAUSE_ANIM_FINISH>().AddListenerUnique(_OnPauseAnimFinish);
            MessageCenter.Get<MSG.UI_RESUME_ANIM_FINISH>().AddListenerUnique(_OnResumeAnimFinish);
        }

        #endregion

        #region 界面打开/关闭流程相关
        
        private void _OpenWindowImp(UIResource uiRes, Action openSuccessCb, params object[] args)
        {
            if (_openingUIQueue.Count > 0)
            {
                // 需要排队
                if (_openingUIQueue.FindIndex(item => item.ui == uiRes) >= 0)
                {
                    // 重复加载
                    DebugEx.FormatWarning("[UIManager] try duplicate open ui {0}", uiRes.prefabPath);
                    return;
                }
                // 排队
                _AddOpenRequest(uiRes, openSuccessCb, args);
                // 尝试准备资源
                _TryPrepareRes(uiRes);
            }
            else
            {
                if (_TryPrepareRes(uiRes))
                {
                    var ui = _allCacheUIDict[uiRes];
                    //界面为关闭状态时 直接开启
                    if (ui.IsClosed())
                    {
                        _Open(ui, args);
                        openSuccessCb?.Invoke();
                    }
                    //界面为打开/正在打开状态时 走Refresh方法 并传入参数 有需要的界面可以在OnRefresh中更新数据并刷新界面
                    else if (ui.CheckIsState(UIWindowState.Open) || ui.CheckIsState(UIWindowState.Opening))
                    {
                        ui.Refresh(args);
                    }
                    //界面为暂停状态时 直接提到最前面 并走Resume和Refresh方法
                    else if (ui.CheckIsState(UIWindowState.Pause))
                    {
                        var layer = _uiLayerList[(int)ui.ResConfig.layer];
                        if (ui.transform.GetSiblingIndex() != layer.childCount - 1)
                        {
                            //提到最前
                            ui.transform.SetAsLastSibling();
                            //走Resume
                            if (ui.IsPause())
                            {
                                ui.Resume();
                                ui.PlayResumeAnim();
                            }
                            //走刷新
                            ui.Refresh(args);
                            //下层的走Pause
                            if (ui.ResConfig.layer == UILayer.AboveStatus)
                                _SetupPause(layer);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    // 排队
                    _AddOpenRequest(uiRes, openSuccessCb, args);
                }
            }
        }
        
        private void _AddOpenRequest(UIResource uiRes, Action openSuccessCb, params object[] args)
        {
            Action imp = () =>
            {
                _Open(_allCacheUIDict[uiRes], args);
                openSuccessCb?.Invoke();
            };
            _openingUIQueue.Add((uiRes, imp));
            DebugEx.FormatInfo("[UIManager._AddOpenRequest] : uiRes = {0}, waitCount = {1}", uiRes.prefabPath, _openingUIQueue.Count);
        }
        
        private bool _TryPrepareRes(UIResource uiRes)
        {
            if (!_allCacheUIDict.ContainsKey(uiRes))
            {
                if (!_loadRequestDict.ContainsKey(uiRes))
                {
                    DebugEx.FormatInfo("[UIManager] ui load request {0}", uiRes.prefabPath);
                    StartCoroutine(_CoLoadRequest(uiRes, _allCacheUIDict, _TryResolveOpenRequest));
                }
                else
                {
                    DebugEx.FormatError("[UIManager] try duplicate load request {0}", uiRes.prefabPath);
                }
                return false;
            }
            return true;
        }
        
        private IEnumerator _CoLoadRequest(UIResource uiRes, Dictionary<UIResource, UIBase> container, Action<UIBase> callback)
        {
            _loadRequestDict.Add(uiRes, 0);
            _SetupBlock(true);
            ++_curLoadingResCount;

            var loadRequest = ResManager.LoadAsset(uiRes.prefabGroup, uiRes.prefabPath);
            yield return loadRequest;

            _loadRequestDict.Remove(uiRes);
            _SetupBlock(false);
            --_curLoadingResCount;

            if (loadRequest.isSuccess && loadRequest.asset != null)
            {
                if (container.ContainsKey(uiRes))
                {
                    DebugEx.FormatError("[UIManager] duplicate ui load request {0}", uiRes.prefabPath);
                }
                else
                {
                    var uiObj = (Instantiate(loadRequest.asset) as GameObject);
                    var ui = uiObj.GetComponent<UIBase>();
                    if (ui == null)
                    {
                        DebugEx.FormatError("UIManager::_CoLoadRequest ----> wrong ui {0}", uiRes.prefabPath);
                    }
                    container.Add(uiRes, ui);
                    _SetupConfig(ui, uiRes);
                    _SetupSafeArea(ui);
                    _SetupCache(ui);
                    ui.OnLoaded();
                    callback?.Invoke(ui);
                }
            }
            else
            {
                DebugEx.FormatError("[UIManager] ui res missing {0}@{1}", uiRes.prefabPath, uiRes.prefabGroup);
                _TryCancelOpenRequest(uiRes);
            }
        }
        
        private void _TryResolveOpenRequest(UIBase _)
        {
            while (_openingUIQueue.Count > 0)
            {
                var (ui, imp) = _openingUIQueue[0];
                if (!_allCacheUIDict.ContainsKey(ui))
                    break;
                DebugEx.FormatInfo("[UIManager._TryResolveOpenRequest] : ui = {0}", ui.prefabPath);
                _openingUIQueue.RemoveAt(0);
                imp?.Invoke();
            }
        }
        
        private void _Open(UIBase ui, params object[] args)
        {
            _SetupBlock(true);
            _SetupLayer(ui, ui.ResConfig.layer);

            var layer = _uiLayerList[(int)ui.ResConfig.layer];
            if (ui.transform.GetSiblingIndex() != layer.childCount - 1)
            {
                ui.transform.SetAsLastSibling();
            }
            //界面加载完准备打开前 尝试隐藏界面
            _TryPauseMainUI(ui.ResConfig);
            //检查above层级界面数量
            _CheckAboveStateChildCount(ui.ResConfig);
            
            try {
                ui.PreOpen(args);
            }
            catch (Exception e) {
                DebugEx.Error($"{e.Message}\n{e.StackTrace}");
            }

            Game.Manager.audioMan.TriggerSound(ui.ResConfig.openSound.eventName);
            
            void ResolveOpen() 
            { 
                _SetupBlock(false); 
                ui.PostOpen();
                if (LoadingCount == 0)
                    GuideUtility.TriggerGuide();
            }
            
            if (!ui.CheckAnimValid())
            {
                ResolveOpen();
            }
            else
            {
                var hasPausedUI = false;
                // 仅对above层支持pause/resume
                if (ui.ResConfig.layer == UILayer.AboveStatus)
                    hasPausedUI = _SetupPause(layer);
                if (hasPausedUI)
                    ui.PlayResumeAnim(ResolveOpen);
                else
                    ui.PlayOpenAnim(ResolveOpen);
            }
        }
        
        private void _SetupBlock(bool b)
        {
            if (b)
            {
                ++_curBlockCount;
            }
            else
            {
                --_curBlockCount;
            }

            if (_curBlockCount > 0)
            {
                if (!_blockLayerObj.activeSelf)
                    _blockLayerObj.SetActive(true);
            }
            else if (_curBlockCount == 0)
            {
                if (_blockLayerObj.activeSelf)
                    _blockLayerObj.SetActive(false);
            }
            else
            {
                Debug.LogErrorFormat("[UIManager._SetupBlock] : block unbalance {0}", _curBlockCount);
            }
        }
        
        private void _SetupConfig(UIBase ui, UIResource res)
        {
            ui.ResConfig = res;
        }

        private void _SetupCache(UIBase ui)
        {
            ui.gameObject.SetActive(false);
            ui.transform.SetParent(_cacheLayerTrans);
        }

        private void _SetupSafeArea(UIBase ui)
        {
            var content = ui.transform.Find("Content") as RectTransform;
            if (content != null)
            {
                content.anchorMin = _safeAreaAnchorMin;
                content.anchorMax = _safeAreaAnchorMax;
            }
        }
        
        private void _SetupLayer(UIBase ui, UILayer _layer)
        {
            ui.BelongLayer = _layer;
            ui.transform.SetParent(_uiLayerList[(int)_layer], false);

            var rectTrans = ui.transform as RectTransform;
            rectTrans.sizeDelta = Vector2.zero;
            rectTrans.anchorMin = Vector2.zero;
            rectTrans.anchorMax = Vector2.one;
            rectTrans.anchorMax = Vector2.one;
            rectTrans.anchoredPosition3D = Vector3.zero;
            rectTrans.localScale = Vector3.one;

            ui.gameObject.SetActive(true);
        }
        
        private void _CloseWindowImp(UIResource uiRes)
        {
            if (uiRes == null)
                return;

            _TryCancelOpenRequest(uiRes);

            if (!_allCacheUIDict.ContainsKey(uiRes))
            {
                return;
            }

            var ui = _allCacheUIDict[uiRes];
            if (!ui.IsOpen())
                return;

            _ClosingEffectImplement(ui);
        }
        
        private void _TryCancelOpenRequest(UIResource uiRes)
        {
            var idx = _openingUIQueue.FindIndex(item => item.ui == uiRes);
            if (idx >= 0)
            {
                _openingUIQueue.RemoveAt(idx);
            }
        }
        
        private void _ClosingEffectImplement(UIBase ui)
        {
            _SetupBlock(true);
            ui.PreClose();
            Game.Manager.audioMan.TriggerSound(ui.ResConfig.closeSound.eventName);
            
            void ResolveClose()
            {
                _SetupBlock(false);
                int siblingIdx = ui.transform.GetSiblingIndex();
                int siblingCount = ui.transform.parent.childCount;
                _SetupCache(ui);
                if (siblingIdx == siblingCount - 1)
                {
                    _SetupResume(_uiLayerList[(int)ui.BelongLayer]);
                }
                //PostClose放到最后 避免PostClose中开新界面失败
                ui.PostClose();
                Game.Manager.screenPopup.WhenClose(ui.ResConfig);
                if (!ui.ResConfig.IsUITips)
                    MessageCenter.Get<MSG.UI_CLOSE_LAYER>().Dispatch(ui.ResConfig.layer);
                //界面关闭后尝试恢复界面
                _TryResumeMainUI(ui.ResConfig);
                //检查above层级界面数量
                _CheckAboveStateChildCount(ui.ResConfig);

                if (LoadingCount == 0)
                    GuideUtility.TriggerGuide();
            }

            var prev = _GetPreviousUI(_uiLayerList[(int)ui.BelongLayer]);
            if (!ui.CheckAnimValid() || (prev != null && prev.IsPause()))
            {
                //有正在pause的ui存在 则不播放关闭动画。避免和pause ui的resume动画冲突造成画面闪烁
                ResolveClose();
            }
            else
            {
                ui.PlayCloseAnim(ResolveClose);
            }
        }

        private bool _SetupPause(RectTransform layerRoot)
        {
            bool ret = false;
            for (int i = layerRoot.childCount - 1, count = 0; i >= 0 && count < 2; --i)
            {
                var ui = layerRoot.GetChild(i).GetComponent<UIBase>();
                if (ui != null)
                {
                    ++count;
                    // 倒数第二个ui进入pause
                    if (count == 2)
                    {
                        if (!ui.IsPause())
                        {
                            ret = true;
                            ui.Pause();
                        }
                        break;
                    }
                }
            }
            return ret;
        }

        private void _SetupResume(RectTransform layerRoot)
        {
            for (int i = layerRoot.childCount - 1; i >= 0; --i)
            {
                var ui = layerRoot.GetChild(i).GetComponent<UIBase>();
                if (ui != null)
                {
                    if (ui.IsPause())
                    {
                        ui.Resume();
                        ui.PlayResumeAnim();
                    }
                    break;
                }
            }
        }

        private void _TryPauseMainUI(UIResource res)
        {
            if (res == null)
                return;
            if (res.layer != UILayer.AboveStatus && res.layer != UILayer.SubStatus)
                return;
            //允许隐藏资源栏UI 且 当前资源栏正在显示中
            if (res.IsAllowHideStatusUI && !IsHideStatusUI)
            {
                IsHideStatusUI = true;
                MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
            }
            //允许隐藏主界面UI 且 当前主界面正在显示中
            if (res.IsAllowHideMainUI && !IsHideMainUI)
            {
                IsHideMainUI = true;
                MessageCenter.Get<MSG.GAME_MAIN_UI_STATE_CHANGE>().Dispatch(false);
            }
        }
        
        private void _TryResumeMainUI(UIResource res)
        {
            if (res == null)
                return;
            if (res.layer != UILayer.AboveStatus && res.layer != UILayer.SubStatus)
                return;
            //从上往下开始检查部分层级中的界面是否允许在打开时隐藏主界面UI 如果有 则阻止恢复界面
            var allowHideMainUICount = 0;
            var allowHideStatusUICount = 0;
            foreach (var uiRes in _loadRequestDict.Keys)
            {
                if (uiRes.IsAllowHideMainUI) allowHideMainUICount++;
                if (uiRes.IsAllowHideStatusUI) allowHideStatusUICount++;
            }
            for (var i = (int)UILayer.SubStatus; i >= (int)UILayer.AboveStatus; i--)
            {
                var layerRoot =  _uiLayerList[i];
                if (layerRoot.childCount < 1)
                    continue;
                for (var j = layerRoot.childCount - 1; j >= 0; j--)
                {
                    var cur = layerRoot.GetChild(j).GetComponent<UIBase>();
                    if (cur == null || cur.ResConfig == null)
                        continue;
                    if (cur.ResConfig.IsAllowHideMainUI) allowHideMainUICount++;
                    if (cur.ResConfig.IsAllowHideStatusUI) allowHideStatusUICount++;
                }
            }
            //允许隐藏主界面的界面数量小于1 且当前主界面正在隐藏中 则显示主界面
            if (allowHideMainUICount < 1 && IsHideMainUI)
            {
                IsHideMainUI = false;
                MessageCenter.Get<MSG.GAME_MAIN_UI_STATE_CHANGE>().Dispatch(true);
            }
            //允许隐藏资源栏的界面数量小于1 且当前资源栏正在隐藏中 则显示资源栏
            if (allowHideStatusUICount < 1 && IsHideStatusUI)
            {
                IsHideStatusUI = false;
                MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
            }
        }

        //强行改变资源栏显隐状态
        public void ForceChangeStatusUI(bool isShow)
        {
            IsHideStatusUI = !isShow;
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(isShow);
        }

        private void _CheckAboveStateChildCount(UIResource res)
        {
            if (res == null || res.layer != UILayer.AboveStatus) return;
            var aboveLayerRoot = GetLayerRootByType(UILayer.AboveStatus);
            if (aboveLayerRoot.childCount == 1)
            {
                MessageCenter.Get<MSG.UI_ABOVE_STATUE_HAS_CHILD>().Dispatch();
            }
            else if (aboveLayerRoot.childCount == 0)
            {
                MessageCenter.Get<MSG.UI_ABOVE_STATUE_NO_CHILD>().Dispatch();
            }
        }

        private UIBase _GetPreviousUI(RectTransform layerRoot)
        {
            for (int i = layerRoot.childCount - 1, count = 0; i >= 0 && count < 2; --i)
            {
                var ui = layerRoot.GetChild(i).GetComponent<UIBase>();
                if (ui != null)
                {
                    ++count;
                    if (count == 2)
                        return ui;
                }
            }
            return null;
        }

        private void _OnOpenAnimFinish(UIResource uiRes)
        {
            if (_allCacheUIDict.TryGetValue(uiRes, out UIBase ui))
            {
                ui.OnPlayOpenAnimFinish();
            }
        }

        private void _OnCloseAnimFinish(UIResource uiRes)
        {
            if (_allCacheUIDict.TryGetValue(uiRes, out UIBase ui))
            {
                ui.OnPlayCloseAnimFinish();
            }
        }

        private void _OnPauseAnimFinish(UIResource uiRes)
        {
            if (_allCacheUIDict.TryGetValue(uiRes, out UIBase ui))
            {
                ui.OnPlayPauseAnimFinish();
            }
        }

        private void _OnResumeAnimFinish(UIResource uiRes)
        {
            if (_allCacheUIDict.TryGetValue(uiRes, out UIBase ui))
            {
                ui.OnPlayResumeAnimFinish();
            }
        }

        #endregion

        #region 内部重置/刷新

        private void _Reset()
        {
            StopAllCoroutines();
            //Reset逻辑中先执行CloseAll,确保当前所有UI都会走到自身的Close逻辑(有的UI会在close逻辑中起新的tween)
            CloseAll();
            //然后再执行KillAll方法停掉目前所有tween(会走tween的oncomplete方法)
            DOTween.KillAll(true);
            //之后再真正销毁UI的gameObject，这样可以避免tween的oncomplete方法中引用prefab中的节点造成空引用异常
            foreach (var kv in _allCacheUIDict)
            {
                GameObject.Destroy(kv.Value.gameObject);
            }
            IsHideMainUI = false;
            IsHideStatusUI = false;
            _allCacheUIDict.Clear();
            // count
            _curLoadingResCount = 0;
            _curBlockCount = 0;
            _blockLayerObj.SetActive(false);
            _miscBlockCount = 0;
            _block.raycastTarget = false;
            _idleActionList.Clear();
            _cbReqFrameCount = -1;
            _isCloseIdleAction = false;
            _InitAnimListener();
        }
        
        private void Update()
        {
            _UpdateGlobalNav();
#if UNITY_EDITOR
            _UpdateCanvasScale();
#endif
            _UpdateIdleActionList();
        }

        private void _UpdateGlobalNav()
        {
            //界面处于锁定状态时返回
            if (isBlocked)
                return;
            //游戏未在运行状态时返回
            if (!Game.Instance.isRunning)
                return;
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;
            DebugEx.Info("[uimanager] global nav try");
            for (int i = (int)UILayer.Top; i > (int)UILayer.Hud; --i)
            {
                var isSuccess = _TryNavBack((UILayer)i, out var isStop);
                //若NavBack处理成功或检查停止 则返回
                if (isSuccess || isStop)
                {
                    return;
                }
            }
            //处理完相关UI层级后发现都没有命中，且场景UI正在开启，则通知处理NavBack
            var mapMan = Game.Manager.mapSceneMan;
            if (mapMan.Active)
            {
                //需求变更 暂时不需要场景中响应全局返回
            }
        }

        private bool _TryNavBack(UILayer layer, out bool isStop)
        {
            //是否停止NavBack检查
            isStop = false;
            // 忽略status
            if (layer == UILayer.Status)
                return false;

            var layerRoot = _uiLayerList[(int)layer];

            // 此layer没有ui
            if (layerRoot.childCount < 1)
                return false;

            for (var i = layerRoot.childCount - 1; i >= 0; --i)
            {
                var cur = layerRoot.GetChild(i).GetComponent<UIBase>();
                //若此界面忽略NavBack 则继续查找后续界面
                if (cur.ResConfig.IsIgnoreNavBack)
                    continue;
                //此界面当前处于隐藏状态时，继续查找后续界面
                if (cur.gameObject.activeInHierarchy==false)
                    continue;
                //若此界面支持NavBack 则尝试处理
                if (cur.ResConfig.IsSupportNavBack)
                {
                    if (cur.IsShow())
                    {
                        DebugEx.Info($"[uimanager] global nav back {cur.ResConfig.prefabPath}@{cur.ResConfig.prefabGroup}");
                        // if (layer == UILayer.Hud)
                        // {
                        //     // 弹窗确认是否退出游戏
                        //     Game.Manager.commonTipsMan.ShowMessageTips(I18N.Text("#SysComDesc177"), null, Application.Quit, false);
                        // }
                        if (cur is INavBack icur)
                        {
                            icur.OnNavBack();
                        }
                        else
                        {
                            cur.Close();
                        }
                    }
                    return true;
                }
                //若此界面不支持NavBack 则处理到此为止
                else
                {
                    isStop = true;
                    return false;
                }
            }
            //此层级上没有界面支持NavBack
            return false;
        }

        private void _UpdateCanvasScale()
        {
            if (Time.frameCount % 3 == 0)
            {
                if (Screen.width != _lastScreenWidth ||
                    Screen.height != _lastScreenHeight ||
                    !_lastSafeArea.Equals(Screen.safeArea))
                {
                    _RefreshCanvasAndSafeArea();
                }
            }
        }

        private void _RefreshCanvasAndSafeArea()
        {
            _RefreshCanvasScaler();
            _RefreshSafeRoot();
            _RefreshSafeArea();
            foreach (var item in _allCacheUIDict.Values)
            {
                _SetupSafeArea(item);
            }
        }

        // 适配模式发生改变 需要等canvas尺寸更新后再次刷新安全区
        private async UniTaskVoid _DelayRefreshCanvasAndSafeArea()
        {
            var pre_w = CanvasRoot.rect.width;
            var pre_h = CanvasRoot.rect.height;
            await UniTask.WaitWhile(() => CanvasRoot.rect.width == pre_w && CanvasRoot.rect.height == pre_h);
            _RefreshCanvasAndSafeArea();
        }

        //当界面处于idle状态时执行回调 此逻辑执行的优先级较低
        private void _UpdateIdleActionList()
        {
            //外部主动关闭检测IdleAction时返回
            if (_isCloseIdleAction)
                return;
            // 延迟弹出 避免和guide popup等同步弹窗系统冲突
            if (Time.frameCount == _cbReqFrameCount)
                return;
            // 没有弹窗需求时返回
            if (!_HasIdleAction())
                return;
            // ui不处于idle状态时返回
            if (!CheckUIIsIdleState())
                return;
            //每次都取第一个回调执行 并从list中移除
            var first = _idleActionList[0];
            _idleActionList.RemoveAt(0);
            DebugEx.FormatInfo("[UIManager._UpdateIdleActionList] : reqKey = {0}, priority = {1}", first.key, first.priority);
            first.callback?.Invoke();
        }
        
        private bool _HasIdleAction()
        {
            return _idleActionList.Count > 0;
        }
        
        private bool _IsLayerEmpty(UILayer layer)
        {
            return _uiLayerList[(int)layer].childCount < 1;
        }
        
        private void CloseAll()
        {
            _openingUIQueue.Clear();
            UIBase ui;
            foreach (var kv in _allCacheUIDict)
            {
                ui = kv.Value;
                if (ui.ResConfig.ignoreCloseAll)
                    continue;
                if (ui.IsOpen())
                {
                    ui.PreClose();
                    ui.PostClose();
                    _SetupCache(ui);
                }
            }
        }

        #endregion
        
    }
}