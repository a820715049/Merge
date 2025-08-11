/*
 * @Author: qun.chao
 * @Date: 2023-11-22 15:31:17
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using fat.gamekitdata;
using fat.rawdata;
using EL;
using FAT.Merge;

namespace FAT
{
    public class GuideMan : IGameModule, IUserDataHolder
    {
        class Runner : MonoBehaviour
        {
            public bool IsDirty { get; set; }
            public Action handler { get; set; }

            private void LateUpdate()
            {
                if (IsDirty)
                {
                    IsDirty = false;
                    // Debug.Log($"[guide] frame resolve {Time.frameCount}");
                    handler?.Invoke();
                }
            }
        }

        public Action<Item, Item, Item> OnItemMerge { get; set; }
        public Action<Item, Item> OnItemConsume { get; set; }
        public Action<Item> OnItemPutIntoInventory { get; set; }
        public UIGuideContext ActiveGuideContext { get; private set; }

        private Dictionary<int, GuideMergeAction> mGuideActionDict = new();
        private List<GuideMerge> mGuideList = new();
        private List<GuideMerge> mNextGuideList = new();
        private GuideMerge mActiveGuide;
        private Bitmap64 mGuideRecord = new(1);
        private GuideRequireChecker mRequireChecker = new();
        private GuideActionFactory mActionFactory = new();
        private bool mIsDirty;
        private Runner triggerRunner;

        private void _Unregister()
        {
            MessageCenter.Get<MSG.MAP_BUILDING_UPDATE_ANY>().RemoveListener(_CheckGuide);
            MessageCenter.Get<MSG.MAP_FOCUS_POPUP>().RemoveListener(_CheckGuide);
            MessageCenter.Get<MSG.GAME_ORDER_DISPLAY_CHANGE>().RemoveListener(_CheckGuide);
            MessageCenter.Get<MSG.MAP_SETUP_FINISHED>().RemoveListener(_CheckGuide);
        }

        private void _Register()
        {
            MessageCenter.Get<MSG.MAP_BUILDING_UPDATE_ANY>().AddListener(_CheckGuide);
            MessageCenter.Get<MSG.MAP_FOCUS_POPUP>().AddListener(_CheckGuide);
            MessageCenter.Get<MSG.GAME_ORDER_DISPLAY_CHANGE>().AddListener(_CheckGuide);
            MessageCenter.Get<MSG.MAP_SETUP_FINISHED>().AddListener(_CheckGuide);
        }

        #region imp

        void IGameModule.Reset()
        {
            ActiveGuideContext = null;
            mActiveGuide = null;
            mRequireChecker.Reset();
            mNextGuideList.Clear();
            if (triggerRunner == null)
            {
                var _go = new GameObject("GuideTriggerSchedule", typeof(RectTransform));
                _go.transform.localPosition = Vector3.zero;
                triggerRunner = _go.AddComponent<Runner>();
                triggerRunner.handler += _RefreshGuide;
            }
            _Unregister();
        }

        void IGameModule.LoadConfig()
        {
            // 只读取id有效的条目
            var _allGuide = Game.Manager.configMan.GetGuideMergeConfigs();
            var _guideList = new List<GuideMerge>();
            foreach (var g in _allGuide)
            {
                if (g.Id > 0)
                {
                    _guideList.Add(g);
                }
            }
            _guideList.Sort(_SortGuide);
            mGuideList.Clear();
            mGuideList.AddRange(_guideList);

            var acts = Game.Manager.configMan.GetGuideMergeActionConfigs();
            mGuideActionDict.Clear();
            foreach (var a in acts)
            {
                mGuideActionDict.Add(a.Id, a);
            }
            if (GameSwitchManager.Instance.isUseGuideDebug)
                _DebugPrintGuide();
        }

        void IGameModule.Startup()
        {
            _Register();
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var data = new Tutorial();
            archive.ClientData.PlayerGeneralData.Tutorial = data;
            data.FinishMask.AddRange(mGuideRecord.data);
        }

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGeneralData.Tutorial;
            data ??= new();
            mGuideRecord.Reset(data.FinishMask);

            _PrepareNextGuide();
        }

        #endregion

        #region guide trigger

        private void _CheckGuide()
        {
            if (!triggerRunner.IsDirty)
            {
                triggerRunner.IsDirty = true;
                // Debug.Log($"[guide] frame trigger {Time.frameCount}");
            }
        }

        private void _RefreshGuide()
        {
            if (!GameProcedure.IsInGame)
                return;
            if (mActiveGuide != null || mNextGuideList.Count < 1)
                return;

            int startActIdx = 0;
            GuideMerge nextGuide = null;
            foreach (var guide in mNextGuideList)
            {
                if (_IsGuideMatchRequirement(guide, out startActIdx))
                {
                    nextGuide = guide;
                    break;
                }
            }

            if (nextGuide != null)
            {
                mActiveGuide = nextGuide;
                _OpenGuideUI(nextGuide, startActIdx);
            }
        }


        #endregion

        public void OnPushGuideContext(UIGuideContext context)
        {
            ActiveGuideContext = context;
        }

        public void OnPopGuideContext()
        {
            ActiveGuideContext = null;
        }

        public void FinishGuideAndMoveNext(int id)
        {
            mActiveGuide = null;
            // 清除旧记录
            // _ClearSpecialCheckFlag();
            _SetGuideFinish(id);
            _PrepareNextGuide();
        }

        public void FinishGuideById(int id)
        {
            _SetGuideFinish(id);
        }

        public void DropGuide()
        {
            mActiveGuide = null;
            UIManager.Instance.CloseWindow(UIConfig.UIGuide);
            _PrepareNextGuide();
        }

        public GuideMergeAction GetGuideAction(int id)
        {
            return mGuideActionDict[id];
        }

        public bool IsGuideFinished(int gid)
        {
            // -1表示guide未完成
            if (gid < 0)
                return false;
            return mGuideRecord.ContainsId(gid);
        }

        public void PlayGuideById(int gid)
        {
            if (mActiveGuide != null)
            {
                DebugEx.FormatError("[GUIDE] another guide {0} is playing, cant start {1}", mActiveGuide.Id, gid);
                return;
            }
            foreach (var g in mGuideList)
            {
                if (g.Id == gid)
                {
                    _OpenGuideUI(g, 0);
                    break;
                }
            }
        }

        #region DevPanel

        public bool IsGuideValid(int gid)
        {
            var guides = mGuideList;
            if (guides == null)
                return false;
            for (int i = 0; i < guides.Count; ++i)
            {
                if (guides[i].Id == gid)
                {
                    if (guides[i].PreSteps.Count == 1 && guides[i].PreSteps[0] == -1)
                        return false;
                    return true;
                }
            }
            return false;
        }

        public void SetAllGuideFinished()
        {
            _FinishAll();
            mActiveGuide = null;
            UIManager.Instance.CloseWindow(UIConfig.UIGuide);
            // 清除旧记录
            // _ClearSpecialCheckFlag();
            _PrepareNextGuide();
        }

        public void UnfinishGuideAndRefresh(int gid)
        {
            mActiveGuide = null;
            // 清除旧记录
            // _ClearSpecialCheckFlag();
            _SetGuideUnfinished(gid);
            _PrepareNextGuide();
        }

        private void _FinishAll()
        {
            ulong _complete = 0xFFFF_FFFF_FFFF_FFFFUL;
            mGuideRecord.Clear();

            // TODO 目前支持 64 * 3 个引导，如果数量不满足可以接着加
            mGuideRecord.Reset(new List<ulong>() { _complete, _complete, _complete });
        }

        #endregion

        public void TriggerGuide()
        {
            if (GameProcedure.IsInGame)
                _CheckGuide();
        }

        public bool IsMatchRequirement(IList<string> requires)
        {
            return mRequireChecker.IsMatchRequirement(requires);
        }

        public bool IsMatchUIState(int state, int extra)
        {
            return mRequireChecker.IsMatchUIState(state, extra);
        }

        private void _OpenGuideUI(GuideMerge guide, int startIdx)
        {
            UIManager.Instance.OpenWindow(UIConfig.UIGuide, guide, startIdx);
        }

        #region entry ctrl

        // public bool IsNewUser()
        // {
        //     return !IsGuideFinished(1);
        // }

        // public bool IsShowFinger()
        // {
        //     // 订单25y完成前需要提示手
        //     return !_IsRequireOrderComplete(25);
        // }

        // public bool IsBubbleUnlockGuideFinished()
        // {
        //     return IsGuideFinished(Constant.kMergeBubbleGuideId);
        // }

        #endregion

        // public void SetCollectItem(int itemId)
        // {
        //     mLastCollectedItemTid = itemId;
        // }

        // private void _ClearSpecialCheckFlag()
        // {
        //     mLastCollectedItemTid = 0;
        // }

        private void _OnGuideFinished(int id)
        {
            // track
            DataTracker.TrackTutorial(id, id.ToString());

            mGuideRecord.AddId(id);
            EL.MessageCenter.Get<MSG.GUIDE_FINISH>().Dispatch(id);
            DebugEx.Warning($"[GUIDE] finished guide {id}");

            // 通知外部
            Game.Manager.featureUnlockMan.OnGuideFinished();
        }

        private void _SetGuideFinish(int id)
        {
            if (!IsGuideFinished(id))
            {
                _OnGuideFinished(id);
            }
            if (id > 1024)
            {
                // 为合理存储数据 guide应该连续id增长 id过大可能配置有错
                DebugEx.Error($"[GUIDE] guide id too big -> {id}");
            }
        }

        private void _SetGuideUnfinished(int id)
        {
            mGuideRecord.RemoveId(id);
            DebugEx.Warning($"[GUIDE] unfinish guide {id}");
        }

        private bool _IsGuideMatchRequirement(GuideMerge guide, out int startActIdx)
        {
            startActIdx = 0;
            for (int i = 0; i < guide.Actions.Count; i++)
            {
                var act = mGuideActionDict[guide.Actions[i]];
                if (mRequireChecker.IsMatchRequirement(act.Requires))
                {
                    startActIdx = i;
                    return true;
                }
                else if (act.AllowSkip)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private void _PrepareNextGuide()
        {
            mNextGuideList.Clear();
            foreach (var guide in mGuideList)
            {
                if (!IsGuideFinished(guide.Id) && _IsPreviousGuideReady(guide))
                {
                    mNextGuideList.Add(guide);
                }
            }
        }

        private bool _IsPreviousGuideReady(GuideMerge guide)
        {
            for (int i = 0; i < guide.PreSteps.Count; ++i)
            {
                if (!IsGuideFinished(guide.PreSteps[i]))
                {
                    return false;
                }
            }
            return true;
        }

        #region creation

        public GuideActImpBase CreateGuideAction(GuideMergeAction action)
        {
            return mActionFactory.CreateGuideAction(action);
        }

        public void ActionSetBlock(bool show)
        {
            _GetUIGuideContext()?.SetBlocker(show);
        }

        public void ActionShowRectMask(Transform target, float size = 1f)
        {
            _GetUIGuideContext()?.ShowRectMask(target, size);
        }

        public void ActionHideRectMask()
        {
            _GetUIGuideContext()?.HideRectMask();
        }

        public void ActionShowMask(Transform target)
        {
            _GetUIGuideContext()?.ShowMask(target);
        }

        public void ActionHideMask()
        {
            _GetUIGuideContext()?.HideMask();
        }

        public void ActionShowHand(Transform target, Action cb)
        {
            _GetUIGuideContext()?.ShowPointerPro(target, true, true, cb);
        }

        public void ActionShowHandFree(Transform target, Action cb)
        {
            _GetUIGuideContext()?.ShowPointerPro(target, false, false, cb);
        }

        public void ActionShowHandPro(Transform target, bool block, bool mask, Action cb)
        {
            _GetUIGuideContext()?.ShowPointerPro(target, block, mask, cb);
        }

        public void ActionHideHand()
        {
            _GetUIGuideContext()?.HidePointer();
        }

        public void ActionShowBoardMask(int x, int y)
        {
            _GetUIGuideContext()?.ShowBoardMask(x, y);
        }

        public void ActionShowTalk(IEnumerable<int> talks, Action cb)
        {
            _GetUIGuideContext()?.ShowTalk(talks, cb);
        }

        public void ActionBoardShowDialog(string contentKey, string img)
        {
            MessageCenter.Get<MSG.UI_GUIDE_BOARD_SHOW_DIALOG>().Dispatch(contentKey, img);
        }

        public void ActionBoardHideDialog()
        {
            MessageCenter.Get<MSG.UI_GUIDE_BOARD_HIDE_DIALOG>().Dispatch();
        }

        public void ActionTopShowDialog(string contentKey, string img)
        {
            MessageCenter.Get<MSG.UI_GUIDE_TOP_SHOW_DIALOG>().Dispatch(contentKey, img);
        }

        public void ActionTopHideDialog()
        {
            MessageCenter.Get<MSG.UI_GUIDE_TOP_HIDE_DIALOG>().Dispatch();
        }

        public void ActionShowBoardSelectTarget(int x, int y, Action cb, bool setMask = false)
        {
            _GetUIGuideContext()?.ShowBoardSelectTarget(x, y, cb, setMask);
        }

        public void ActionShowBoardUseTarget(int x, int y, Action cb, bool setMask = false)
        {
            _GetUIGuideContext()?.ShowBoardUseTarget(x, y, cb, setMask);
        }

        public void ActionWaitClick(Action cb)
        {
            _GetUIGuideContext()?.WaitClick(cb);
        }

        public void ActionSaveProgress()
        {
            if (mActiveGuide != null)
            {
                _SetGuideFinish(mActiveGuide.Id);
            }
        }

        public Transform FindByPath(IList<string> param)
        {
            return FindByPathWithRoot(param, UIManager.Instance.SafeRoot) ?? FindByPathForSceneUI(param);
        }

        public Transform FindByPathForSceneUI(IList<string> param)
        {
            var root = Game.Manager.mapSceneMan.ui.canvas.transform;
            return FindByPathWithRoot(param, root);
        }

        public Transform FindByPathWithRoot(IList<string> param, Transform root)
        {
            if (param.Count < 1)
                return null;
            Transform tar = root;
            int idx = 0;
            for (int i = 0; i < param.Count; i++)
            {
                idx = _TryGetNumber(param[i]);
                if (idx >= 0)
                {
                    tar = tar.GetChild(idx);
                }
                else
                {
                    if (param[i].StartsWith("*"))
                    {
                        var searchName = param[i].Replace("*", "");
                        for (int j = 0; j < tar.childCount; j++)
                        {
                            if (tar.GetChild(j).name.StartsWith(searchName))
                            {
                                tar = tar.GetChild(j);
                                break;
                            }
                        }
                    }
                    else
                    {
                        tar = tar.Find(param[i]);
                    }
                }
            }
            return tar;
        }

        private int _TryGetNumber(string str)
        {
            if (int.TryParse(str, out var num))
            {
                return num;
            }
            return -1;
        }

        private UIGuideContext _GetUIGuideContext()
        {
            return ActiveGuideContext;
        }

        #endregion

        private int _SortGuide(GuideMerge a, GuideMerge b)
        {
            // 优先级大的在前 / id小的在前
            if (a.Priority != b.Priority)
                return b.Priority - a.Priority;
            return a.Id - b.Id;
        }

        private void _DebugPrintGuide()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < mGuideList.Count; i++)
            {
                var guide = mGuideList[i];
                sb.Append($"guide {guide.Id} begin\n");
                for (int j = 0; j < guide.Actions.Count; j++)
                {
                    var act = mGuideActionDict[guide.Actions[j]];
                    sb.Append($"{act.Id} {act.Act} | ");
                }
                if (guide.Actions.Count > 0)
                    sb.Append("\n");
                sb.Append($"guide {guide.Id} finished\n\n");
            }

            Debug.LogWarningFormat(sb.ToString());
        }
    }
}
