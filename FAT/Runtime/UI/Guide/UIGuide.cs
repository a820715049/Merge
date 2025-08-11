/*
 * @Author: qun.chao
 * @Date: 2022-03-07 10:17:42
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using fat.rawdata;

namespace FAT
{
    public class UIGuide : UIBase
    {
        [SerializeField] private Button btnTimeoutSkip;
        [SerializeField] private float timeout = 1f;
        [SerializeField] private Text debugText;
        [SerializeField] private Text uidText;
        [Range(0f, 1)]
        public float maskFadeDuration = 0.5f;
        private UIGuideContext mContext { get; set; } = new UIGuideContext();
        private GuideMerge mGuide;
        private int mStartActIdx;
        private GuideActImpBase curAct;
        private float curActStartTime;
        private readonly string mLogTag = "[GUIDE]";

        protected override void OnCreate()
        {
            var _context = _GetContext();
            _context.RegisterModuleEntry(this);
            _context.RegisterModuleMask(transform, "Comp/Mask/CircleMask");
            _context.RegisterModuleMaskBoard(transform, "Comp/Mask/CircleMaskBoard");
            _context.RegisterModuleRectMask(transform, "Comp/Mask/RectMask");
            _context.RegisterModuleBlocker(transform, "Comp/Blocker");
            _context.RegisterModulePointer(transform, "Comp/Pointer");
            _context.RegisterModuleBoardTap(transform, "Comp/Board");
            _context.RegisterModuleBoardFinger(transform, "Comp/BoardFinger");
            _context.RegisterModuleDragBoard(transform, "Comp/DragBoard");
            _context.RegisterModuleTalk(transform, "Comp/Talk");
            // _context.RegisterModuleBoardDialog(transform, "Content/CompDialog");
            _context.Install();
        }

        protected override void OnPreOpen()
        {
            _GetContext().InitOnPreOpen();

            Game.Manager.guideMan.OnPushGuideContext(_GetContext());

            debugText.gameObject.SetActive(GameSwitchManager.Instance.isDebugMode);
            debugText.text = string.Empty;
            uidText.text = Game.Manager.networkMan.fpId;
        }

        protected override void OnParse(params object[] items)
        {
            if (items == null || items.Length < 1)
                return;
            mGuide = items[0] as GuideMerge;
            mStartActIdx = (int)items[1];

            _SetSkipBtnState(false);
            curActStartTime = -1f;

            if (mGuide.Skip != 0)
            {
                btnTimeoutSkip.transform.localScale = Vector3.one;
            }
            else
            {
                btnTimeoutSkip.transform.localScale = Vector3.zero;
            }
        }

        protected override void OnPostOpen()
        {
            MessageCenter.Get<MSG.GUIDE_OPEN>().Dispatch();
            StartCoroutine(_CoPlayAction(mGuide, mStartActIdx));
        }

        protected override void OnPostClose()
        {
            Game.Manager.guideMan.OnPopGuideContext();
            StopAllCoroutines();
            curAct?.Clear();
            curAct = null;
            _GetContext().CleanupOnPostClose();

            Game.Manager.guideMan.DropGuide();
            MessageCenter.Get<MSG.GUIDE_CLOSE>().Dispatch();
        }

        public void Setup()
        {
            btnTimeoutSkip.onClick.AddListener(_OnBtnTimeoutSkip);
        }

        private void Update()
        {
            if (curActStartTime > 0 && curActStartTime + timeout < Time.realtimeSinceStartup)
            {
                if (!btnTimeoutSkip.gameObject.activeSelf)
                {
                    _SetSkipBtnState(true);
                }
            }
        }

        private IEnumerator _CoPlayAction(GuideMerge guide, int startIdx)
        {
            if (startIdx > 0)
                _Print($"guide {guide.Id} skip to idx-{startIdx} act-{guide.Actions[startIdx]}");
            else
                _Print($"guide {guide.Id} start");

            for (int i = startIdx; i < guide.Actions.Count; ++i)
            {
                var cfg = Game.Manager.guideMan.GetGuideAction(guide.Actions[i]);
                var act = Game.Manager.guideMan.CreateGuideAction(cfg);
                curAct = act;
                curActStartTime = Time.realtimeSinceStartup;

                _Print($"check default act id {cfg.Id}, {cfg.Act} | {string.Concat(cfg.Param)}");

                // 检查基本条件
                if (!_CheckDefaultRequire(cfg))
                    yield return new WaitUntil(() => _CheckDefaultRequire(cfg));

                _Print($"check trigger act id {cfg.Id}, {cfg.Act}");

                // 检查触发条件
                while (true)
                {
                    if (!_CheckDefaultRequire(cfg))
                    {
                        // 中断
                        _Quit(cfg);
                        yield break;
                    }
                    if (!_CheckTriggerRequire(cfg))
                    {
                        // 未触发
                        yield return null;
                    }
                    // 可执行
                    break;
                }

                _Print($"play act id {cfg.Id}, {cfg.Act}");

                // 执行
                try
                {
                    act.Play(cfg.Param.ToList().ToArray());
                }
                catch (System.Exception e)
                {
                    string log = $"[GUIDE_EXCEPTION] \n guideId: {guide.Id} \n actId: {cfg.Id} \n msg: {e.Message} \n callstack: {e.StackTrace}";
                    _Print(log);
                    Debug.LogError(log);
                    throw new System.SystemException(log);
                }

                // 检查所有条件
                while (act.keepWaiting)
                {
                    if (UIManager.Instance.LoadingCount == 0 && (!_CheckDefaultRequire(cfg) || !_CheckTriggerRequire(cfg)))
                    {
                        _Quit(cfg);
                        yield break;
                    }
                    yield return null;
                }

                _Print($"finish act id {cfg.Id}, {cfg.Act}");

                act.Clear();
            }

            _Print($"finish guide id {guide.Id}");

            _Finish();
        }

        private bool _CheckDefaultRequire(GuideMergeAction act)
        {
            return Game.Manager.guideMan.IsMatchRequirement(act.Requires);
        }

        private bool _CheckTriggerRequire(GuideMergeAction act)
        {
            return Game.Manager.guideMan.IsMatchRequirement(act.Triggers);
        }

        private void _Finish()
        {
            Game.Manager.guideMan.FinishGuideAndMoveNext(mGuide.Id);
            base.Close();
            GuideUtility.TriggerGuide();
        }

        private void _Quit(GuideMergeAction act)
        {
            var guide = mGuide;
            DataTracker.TrackTutorialForDebug($"break {guide.Id}-{act.Id}");

            _Print($"break guide id {guide.Id}, act id {act.Id}-{act.Act}");

            Game.Manager.guideMan.DropGuide();
            EL.MessageCenter.Get<MSG.GUIDE_QUIT>().Dispatch();
            GuideUtility.TriggerGuide();
        }

        private void _Print(string log)
        {
            debugText.text = $"{mLogTag} {log}";
            Debug.LogWarning($"{mLogTag} {log}");
        }

        private void _SetSkipBtnState(bool s)
        {
            btnTimeoutSkip.gameObject.SetActive(s);
        }

        private void _OnBtnTimeoutSkip()
        {
            Game.Manager.guideMan.ActionSaveProgress();
            Game.Manager.guideMan.DropGuide();
        }

        private UIGuideContext _GetContext()
        {
            return mContext;
        }
    }
}
