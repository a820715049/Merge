/*
 * @Author: tang.yan
 * @Description: 通用二次确认框管理器
 * @Date: 2023-10-20 18:10:58
 */

using System;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using UnityEngine;

namespace FAT
{
    //通用提示框类型(随需求增加不同类型)
    public enum TipsType 
    {
        Normal, //普通二次确认框
        
        //etc..
    }

    //通用提示框显示优先级
    public enum TipsPriority
    {
        Low,    //低优先级
        Normal, //一般优先级
        High,   //高优先级
        System, //系统级
    }
    
    public class CommonTipsMan : IGameModule
    {
        //消息提示数据
        public class TipsData
        {
            public TipsType Type;
            public TipsPriority Priority;
            public int SortId;      //排序id
            public string Title;    //标题
            public string Content;  //内容
            public bool IsShowLeftBtn;  //是否显示该按钮
            public bool IsShowRightBtn;
            public string LeftBtnName;  //该按钮UI上的名字
            public string RightBtnName;
            public Action LeftBtnCb;    //点击该按钮时的回调 默认点击后关闭界面
            public Action RightBtnCb;
            public bool IsShowInLoading;    //是否在Loading过程中显示
            public bool IsShowHelpBtn;    //是否显示帮助按钮 默认显示帮助按钮时不会显示左侧的取消按钮
            public bool IsFullScreen;       //是否全屏展示
        }

        // 使用List管理  界面从Man中拿取当前数据显示
        public TipsData CurTips;
        private static int _sortId; //排序自增id
        private List<TipsData> _tipsDataList;

        public void Reset()
        {
            _sortId = 0;
            CurTips = null;
            if (_tipsDataList != null)
                _tipsDataList.Clear();
            else
                _tipsDataList = new List<TipsData>();
        }

        public void LoadConfig()
        {
            //进游戏时提前打开缓存一下界面 避免多个地方同时请求加载时 只处理其中一个的情况
            UIManager.Instance.OpenWindow(UIConfig.UIPopFlyTips);
        }

        public void Startup() { }

        public void Update(float dt) { }

        /// <summary>
        /// 根据toast类型显示配置中对应的提示
        /// </summary>
        /// <param name="toastType">每个提示都会对应一个Toast Enum类型</param>
        /// <param name="showPos">Toast 出现的位置</param>
        /// <param name="param">提示信息文本中用到的参数</param>
        public void ShowPopTips(Toast toastType, Vector3 showPos, params object[] param)
        {
            _ShowPopTipsByType(toastType, "", showPos, param);
        }
        
        //不带出现位置参数时 默认为Vector3.zero 
        public void ShowPopTips(Toast toastType, params object[] param)
        {
            _ShowPopTipsByType(toastType, "", Vector3.zero, param);
        }
        
        //客户端处理特殊情况用到的提示  content为自定义显示内容
        public void ShowClientTips(string content)
        {
            _ShowPopTipsByType(Toast.Empty, content, Vector3.zero);
        }
        
        private void _ShowPopTipsByType(Toast toastType, string content, Vector3 showPos, params object[] param)
        {
            var toastConfig = Game.Manager.configMan.GetToastConfig(toastType);
            if (toastConfig != null)
            {
                string info = content == "" ? I18N.FormatText(toastConfig.Text, param) : content;
                if (toastConfig.Type == 1)  //横幅类型
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIPopTips, info);
                }
                else if (toastConfig.Type == 2 || toastConfig.Type == 3) //飘字类型 文本样式弯曲/文本样式水平
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIPopFlyTips, info, toastConfig.Type, toastConfig.Style, showPos);
                }
            }
        }
        
        //暂时只提供个通用方法 后续有需求可以再加
        public void ShowMessageTips(string content, Action leftBtnCb = null, Action rightBtnCb = null, bool isSingle = false)
        {
            _ShowTips(TipsType.Normal, TipsPriority.Normal, I18N.Text("#SysComDesc4"), content, 
                !isSingle, I18N.Text("#SysComBtn5"), leftBtnCb,
                true, I18N.Text("#SysComBtn4"), rightBtnCb);
        }
        
        public void ShowMessageTips(string content, string title, Action leftBtnCb = null, Action rightBtnCb = null, bool isSingle = false)
        {
            title = string.IsNullOrEmpty(title) ? I18N.Text("#SysComDesc4") : title;
            _ShowTips(TipsType.Normal, TipsPriority.Normal, title, content, 
                !isSingle, I18N.Text("#SysComBtn5"), leftBtnCb,
                true, I18N.Text("#SysComBtn4"), rightBtnCb);
        }
        
        public void ShowMessageTips(TipsPriority priority, string content, Action leftBtnCb = null, Action rightBtnCb = null)
        {
            _ShowTips(TipsType.Normal, priority, I18N.Text("#SysComDesc4"), content, 
                true, I18N.Text("#SysComBtn5"), leftBtnCb,
                true, I18N.Text("#SysComBtn4"), rightBtnCb);
        }

        public void ShowMessageTipsFullScreen(string content, Action leftBtnCb = null, Action rightBtnCb = null, bool isSingle = false)
        {
            _ShowTips(TipsType.Normal, TipsPriority.Normal, I18N.Text("#SysComDesc4"), content, 
                !isSingle, I18N.Text("#SysComBtn5"), leftBtnCb,
                true, I18N.Text("#SysComBtn4"), rightBtnCb,
                false, false, true);
        }

        //loading过程中显示Tips
        public void ShowLoadingTips(string content, Action leftBtnCb = null, Action rightBtnCb = null, bool isSingle = false)
        {
            _ShowTips(TipsType.Normal, TipsPriority.Normal, I18N.Text("#SysComDesc4"), content, 
                !isSingle, I18N.Text("#SysComBtn5"), leftBtnCb,
                true, I18N.Text("#SysComBtn4"), rightBtnCb, true);
        }
        
        public void ShowLoadingTips(string content, string title, Action leftBtnCb = null, Action rightBtnCb = null, bool isSingle = false)
        {
            title = string.IsNullOrEmpty(title) ? I18N.Text("#SysComDesc4") : title;
            _ShowTips(TipsType.Normal, TipsPriority.Normal, title, content, 
                !isSingle, I18N.Text("#SysComBtn5"), leftBtnCb,
                true, I18N.Text("#SysComBtn4"), rightBtnCb, true);
        }
        
        //loading过程中显示提示框 并且带联系客服按钮
        public void ShowLoadingTipsWithHelp(string content, string title, Action rightBtnCb = null, string rightBtnName = "")
        {
            title = string.IsNullOrEmpty(title) ? I18N.Text("#SysComDesc4") : title;
            rightBtnName = string.IsNullOrEmpty(rightBtnName) ? I18N.Text("#SysComBtn4") : rightBtnName;
            _ShowTips(TipsType.Normal, TipsPriority.Normal, title, content, 
                false, "", null,
                true, rightBtnName, rightBtnCb, true, true);
        }
        
        public void ShowMessageTipsCustom(string content, string title, string rightBtnStr, Action rightBtnCb)
        {
            _ShowTips(TipsType.Normal, TipsPriority.Normal, title, content, 
                false, "", null,
                true, rightBtnStr, rightBtnCb);
        }

        private void _ShowTips(TipsType type = TipsType.Normal, TipsPriority priority = TipsPriority.Normal, string title = "", string content = "",
            bool isShowLeftBtn = true, string leftBtnName = "", Action leftBtnCb = null, 
            bool isShowRightBtn = true, string rightBtnName = "", Action rightBtnCb = null,
            bool isShowInLoading = false, bool isShowHelpBtn = false, bool IsFullScreen = false)
        {
            TipsData tipsData = new TipsData
            {
                Type = type, Priority = priority, SortId = _sortId++,
                Title = title, Content = content,
                IsShowLeftBtn = isShowLeftBtn, LeftBtnName = leftBtnName, LeftBtnCb = leftBtnCb,
                IsShowRightBtn = isShowRightBtn, RightBtnName = rightBtnName, RightBtnCb = rightBtnCb,
                IsShowInLoading = isShowInLoading, IsShowHelpBtn = isShowHelpBtn, IsFullScreen = IsFullScreen,
            };
            if (CurTips == null)
            {
                CurTips = tipsData;
                _OpenTipsWindowByType(tipsData);
            }
            else
            {
                //如果新加的tips优先级大于当前显示的 则显示新加的 当前正在显示的重新进入队列
                if (tipsData.Priority > CurTips.Priority)
                {
                    _tipsDataList.Add(CurTips);
                    _tipsDataList.Add(tipsData);
                    _SortTipsDataList();
                    //界面中监听 收到时关闭界面 并在PostClose中调用TryShowNextTips
                    MessageCenter.Get<MSG.GAME_COMMON_CLOSE_CUR_TIPS>().Dispatch();
                }
                else
                {
                    _tipsDataList.Add(tipsData);
                    _SortTipsDataList();
                }
            }
        }

        //关闭当前tips界面的同时 会调用此方法  如果检测到list中还有排队的tips 则会显示下一个
        public void TryShowNextTips()
        {
            CurTips = null;
            if (_tipsDataList.Count > 0)
            {
                CurTips = _tipsDataList[0];
                _tipsDataList.RemoveAt(0);
                _OpenTipsWindowByType(CurTips);
            }
        }

        private void _SortTipsDataList()
        {
            //降序排序
            _tipsDataList.Sort((a, b) =>
            {
                if (a.Priority > b.Priority)
                    return -1;
                else if (a.Priority < b.Priority)
                    return 1;
                else
                    return a.SortId - b.SortId;
            });
        }

        private void _OpenTipsWindowByType(TipsData tipData)
        {
            var type = tipData.Type;
            if (type == TipsType.Normal)
            {
                if (tipData.IsShowInLoading)
                {
                    UIBridgeUtility.OpenWindowInLoading(UIConfig.UIMessageBox);
                }
                else
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIMessageBox);
                }
            }
            else
            {
                DebugEx.FormatError("CommonTipsMan : Try Show unknown TipsType , type = {0} ", type);
            }
        }
    }
}