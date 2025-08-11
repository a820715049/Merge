/*
 * @Author: qun.chao
 * @Date: 2022-03-07 18:32:23
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace FAT
{
    public class UIGuideContext
    {
        public UIGuide entry { get; set; }
        public UIGuideTalk talkCtrl { get; set; }
        public UIGuideBlocker blockerCtrl { get; set; }
        public UIGuidePointer pointerCtrl { get; set; }
        public UIGuideBoardTap boardTapCtrl { get; set; }
        public UIGuideMask maskCtrl { get; set; }
        public UIGuideRectMask rectMaskCtrl { get; set; }
        public UIGuideMaskForBoardItem boardMaskCtrl { get; set; }
        public UIGuideDrag dragCtrl { get; set; }
        public MBBoardFinger boardFingerCtrl { get; set; }
        public MBBoardDialog boardDialogCtrl { get; set; }
        private Action mClickCallback = null;
        private Action mPointerCallback = null;
        private Action mBoardTapCallback = null;
        private bool mIsWeakPointer = false;

        public void RegisterModuleEntry(UIGuide guide)
        {
            entry = guide;
        }

        public void RegisterModuleTalk(Transform transform, string path)
        {
            talkCtrl = transform.Find(path).GetComponent<UIGuideTalk>();
        }

        public void RegisterModuleBlocker(Transform transform, string path)
        {
            blockerCtrl = transform.Find(path).GetComponent<UIGuideBlocker>();
        }

        public void RegisterModulePointer(Transform transform, string path)
        {
            pointerCtrl = transform.Find(path).GetComponent<UIGuidePointer>();
        }

        public void RegisterModuleBoardTap(Transform transform, string path)
        {
            boardTapCtrl = transform.Find(path).GetComponent<UIGuideBoardTap>();
        }

        public void RegisterModuleMask(Transform transform, string path)
        {
            maskCtrl = transform.Find(path).GetComponent<UIGuideMask>();
        }

        public void RegisterModuleMaskBoard(Transform transform, string path)
        {
            boardMaskCtrl = transform.Find(path).GetComponent<UIGuideMaskForBoardItem>();
        }

        public void RegisterModuleRectMask(Transform transform, string path)
        {
            rectMaskCtrl = transform.Find(path).GetComponent<UIGuideRectMask>();
        }

        public void RegisterModuleDragBoard(Transform transform, string path)
        {
            dragCtrl = transform.Find(path).GetComponent<UIGuideDrag>();
        }

        public void RegisterModuleBoardFinger(Transform transform, string path)
        {
            boardFingerCtrl = transform.Find(path).GetComponent<MBBoardFinger>();
        }

        public void RegisterModuleBoardDialog(Transform transform, string path)
        {
            boardDialogCtrl = transform.Find(path).GetComponent<MBBoardDialog>();
        }

        public void Install()
        {
            entry.Setup();
            talkCtrl.Setup();
            blockerCtrl.Setup();
            pointerCtrl.Setup();
            maskCtrl.Setup();
            rectMaskCtrl.Setup();
            boardMaskCtrl.Setup();
            boardTapCtrl.Setup();
            dragCtrl.Setup();
            boardFingerCtrl.Setup();
            // boardDialogCtrl.Setup();
        }

        public void InitOnPreOpen()
        {
            talkCtrl.InitOnPreOpen();
            blockerCtrl.InitOnPreOpen();
            pointerCtrl.InitOnPreOpen();
            maskCtrl.InitOnPreOpen();
            rectMaskCtrl.InitOnPreOpen();
            boardMaskCtrl.InitOnPreOpen();
            boardTapCtrl.InitOnPreOpen();
            dragCtrl.InitOnPreOpen();
            boardFingerCtrl.InitOnPreOpen();
            // boardDialogCtrl.InitOnPreOpen(true);
        }

        public void CleanupOnPostClose()
        {
            talkCtrl.CleanupOnPostClose();
            blockerCtrl.CleanupOnPostClose();
            pointerCtrl.CleanupOnPostClose();
            maskCtrl.CleanupOnPostClose();
            rectMaskCtrl.CleanupOnPostClose();
            boardMaskCtrl.CleanupOnPostClose();
            boardTapCtrl.CleanupOnPostClose();
            dragCtrl.CleanupOnPostClose();
            boardFingerCtrl.CleanupOnPostClose();
            // boardDialogCtrl.CleanupOnPostClose();
        }

        public void UserClickFocusArea()
        {
            mPointerCallback?.Invoke();
            mPointerCallback = null;

            pointerCtrl.HidePointer();
            mIsWeakPointer = false;

            // auto unmask
            HideMask();
            // auto hide block
            SetBlocker(false);
        }

        public void UserClickBlocker()
        {
            mClickCallback?.Invoke();
            mClickCallback = null;

            // 如果是weak_pointer 点背景也会导致结束
            if (mIsWeakPointer)
                UserClickFocusArea();
        }

        public void UserTapBoardTarget()
        {
            mBoardTapCallback?.Invoke();
            mBoardTapCallback = null;

            // auto hide block
            SetBlocker(false);
        }

        #region action imp

        public void WaitClick(Action cb)
        {
            mClickCallback = cb;
        }

        public void SetBlocker(bool show)
        {
            if (show)
                blockerCtrl.ShowBlocker();
            else
                blockerCtrl.HideBlocker();
        }

        public void SetAngleOffset(float angle)
        {
            pointerCtrl.SetAngleOffset(angle);
        }

        public void ShowPointerOffsetForUI(Vector2 offset)
        {
            pointerCtrl.SetOffsetForUI(offset);
        }

        public void ShowPointerPro(Transform target, bool block, bool mask, Action cb, float offset = 0f)
        {
            mPointerCallback = cb;
            pointerCtrl.ShowPointer(target);
            // 不改变旧机制 仅处理true的情况 需要显式false时可使用独立的hide操作进行控制
            if (mask)
                ShowMask(target, offset);
            if (block)
                SetBlocker(block);
        }

        // public void ShowPointer(Transform target, Action cb)
        // {
        //     mPointerCallback = cb;
        //     pointerCtrl.ShowPointer(target);
        // }

        public void ShowForcePointer(Transform target, Action cb)
        {
            mPointerCallback = cb;
            pointerCtrl.ShowPointer(target);

            // // auto mask
            // ShowMask(target);
            // // auto block
            // SetBlocker(true);
        }

        // public void ShowPointerVirtual(Vector2 screenPos, Action cb)
        // {
        //     mPointerCallback = cb;
        //     pointerCtrl.ShowPointerVirtual(screenPos);
        // }

        public void HidePointer()
        {
            pointerCtrl.HidePointer();
        }

        public void ShowBoardMask(int x, int y)
        {
            boardTapCtrl.ShowMask(x, y);
            _TweenMaskAlpha(maskCtrl);
        }

        public void ShowBoardSelectTarget(int x, int y, Action cb, bool setMask = true)
        {
            mBoardTapCallback = cb;
            boardTapCtrl.ShowSelectTarget(x, y, setMask);
        }

        public void ShowBoardUseTarget(int x, int y, Action cb, bool setMask = true)
        {
            mBoardTapCallback = cb;
            boardTapCtrl.ShowUseTarget(x, y, setMask);
        }

        public void ShowMask(Transform target, float offset = 0f)
        {
            maskCtrl.Show(target, offset);
            _TweenMaskAlpha(maskCtrl);
        }

        public void HideMask()
        {
            maskCtrl.Hide();
        }

        public void ShowRectMask(Transform target, float size = 1f)
        {
            rectMaskCtrl.Show(target, size);
        }

        private void _MaskMatSetParam(Material mat, int pid, float val)
        {
            mat.SetFloat(pid, val);
        }

        private void _TweenMaskAlpha(MonoBehaviour mask)
        {
            var img = mask.GetComponent<UnityEngine.UI.RawImage>();
            if (img != null)
            {
                var mat = img.material;
                if (mat != null)
                {
                    var pid = Shader.PropertyToID("_AlphaAnim");
                    float duration = entry.maskFadeDuration;
                    float curAlpha = 0;
                    float tarAlpha = 1;
                    _MaskMatSetParam(mat, pid, curAlpha);
                    DOTween.To(() => curAlpha, x => curAlpha = x, tarAlpha, duration)
                        .OnUpdate(() => _MaskMatSetParam(mat, pid, curAlpha));
                }
            }
        }

        public void HideRectMask()
        {
            rectMaskCtrl.Hide();
        }

        public void ShowTalk(IEnumerable<int> talks, Action cb)
        {
            // talkCtrl.ShowTalk(talks, cb);
        }

        public void ShowTalkByCoord(string c, Vector2 coord, bool next = false)
        {
            talkCtrl.ShowByCoord(c, coord, next);
        }

        public void ShowTalkByCoordWithBtn(string c, Vector2 coord, string b, Action cb, bool next)
        {
            talkCtrl.ShowByCoordWithButton(c, coord, b, cb, next);
        }

        public void SetHeadImage(string head) => talkCtrl.SetHeadImage(head);
        public void SetBgImage(string bg) => talkCtrl.SetBgImage(bg);

        public void SetTextColor(Color color)
        {
            talkCtrl.SetTextColor(color);
        }

        public void HideTalk()
        {
            talkCtrl.Hide();
        }

        public void SetRedirector(IEventSystemHandler handler)
        {
            dragCtrl.SetRedirector(handler);
        }

        public void HideBoardCommonMask()
        {
            boardMaskCtrl.Hide();
        }

        public void ShowBoardCommonMask(List<int> items, List<Transform> transList, float radiusOffsetCoe)
        {
            boardMaskCtrl.ShowMask(items, transList, radiusOffsetCoe);
            _TweenMaskAlpha(maskCtrl);
        }

        #endregion
    }
}