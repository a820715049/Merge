/*
 * @Author: qun.chao
 * @Date: 2022-03-07 18:41:50
 */
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class UIGuidePointer : MonoBehaviour
    {
        private Transform mPointerTarget = null;
        private Button mBtnClickArea = null;
        private Transform mAnchor = null;
        private Transform mCenter = null;
        private RectTransform mIndicator = null;
        private RectTransform mOffsetRoot = null;
        private bool mInitialized = false;

        private float mAngleOffset = 0f;
        private bool mIsSceneCanvas = false;
        private Vector2 mOffsetForUI = Vector2.zero;

        public void Setup()
        {
            mInitialized = true;

            mAnchor = transform.Find("Anchor");
            mCenter = transform.Find("Center");
            mIndicator = transform.Find("Anchor/Ind") as RectTransform;
            mOffsetRoot = mIndicator.Find("Offset") as RectTransform;
            mBtnClickArea = transform.AddButton("Anchor/ClickArea", _OnBtnUserClick);
        }

        public void InitOnPreOpen()
        {
            HidePointer();
        }

        public void CleanupOnPostClose()
        {
            HidePointer();
        }

        public void SetAngleOffset(float offset)
        {
            mAngleOffset = offset;
        }

        public void SetOffsetForUI(Vector2 offset)
        {
            mOffsetForUI = offset;
        }

        public void ShowPointer(Transform target)
        {
            _ClearPointer(true);
            mPointerTarget = target;
            mOffsetRoot.anchoredPosition = mOffsetForUI;

            var tarTrans = target as RectTransform;
            var areaTrans = mBtnClickArea.transform as RectTransform;

            if (GuideUtility.CheckIsAncestor(UIManager.Instance.CanvasRoot, target))
            {
                mIsSceneCanvas = false;
            }
            else
            {
                mIsSceneCanvas = true;
            }

            _RefreshClickArea(tarTrans);
            _RefreshIndicator(areaTrans);

            mBtnClickArea.gameObject.SetActive(true);
            gameObject.SetActive(true);
        }

        private void _ClearPointer(bool keepOffset = false)
        {
            mPointerTarget = null;
            mIsSceneCanvas = false;
            mAngleOffset = 0f;
            if (!keepOffset)
                mOffsetForUI = Vector2.zero;
        }

        public void HidePointer()
        {
            _ClearPointer();
            mBtnClickArea.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!mInitialized)
                return;
            if (mPointerTarget == null)
                return;
            _RefreshClickArea(mPointerTarget as RectTransform);
            _RefreshIndicator(mBtnClickArea.transform as RectTransform);
        }

        private void _RefreshClickArea(RectTransform tarTrans)
        {
            var areaTrans = mBtnClickArea.transform as RectTransform;
            if (mIsSceneCanvas)
            {
                var (w, h) = GuideUtility.CalcRectTransformScreenSize(Camera.main, tarTrans);
                var sp = GuideUtility.CalcRectTransformScreenPos(Camera.main, tarTrans);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(areaTrans.parent as RectTransform, sp, null, out var lp);

                var screenCoe = UIManager.Instance.CanvasRoot.localScale.x;
                areaTrans.sizeDelta = new Vector2(w / screenCoe, h / screenCoe);
                areaTrans.pivot = tarTrans.pivot;
                areaTrans.anchoredPosition = lp;
            }
            else
            {
                areaTrans.sizeDelta = new Vector2(tarTrans.rect.width, tarTrans.rect.height);
                areaTrans.pivot = tarTrans.pivot;
                areaTrans.position = tarTrans.position;
            }
        }

        // 以ClickArea为基准调整角度即可
        private void _RefreshIndicator(RectTransform clickArea)
        {
            var area = clickArea;
            Vector2 center = area.anchoredPosition + area.rect.center;
            Vector3 anchoredPos = mAnchor.TransformPoint(center);

            // // rot
            // Vector3 diff = mCenter.position - anchoredPos;
            // float angle = Vector3.Angle(Vector3.down, diff);
            // var cross = Vector3.Cross(Vector3.down, diff);
            // if (cross.z < 0)
            //     angle = 360f - angle;
            // angle += mAngleOffset;
            // mIndicator.localEulerAngles = new Vector3(0f, 0f, angle);

            mIndicator.localEulerAngles = new Vector3(0f, 0f, mAngleOffset);

            mIndicator.position = anchoredPos;
        }

        public void _OnBtnUserClick()
        {
            if (mPointerTarget != null)
            {
                var btn = mPointerTarget.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.Invoke();
                }
                else
                {
                    // hack for map button
                    if (mPointerTarget.TryGetComponent(out MapButton mapButton))
                    {
                        mapButton.Click();
                    }
                    var handler = mPointerTarget.GetComponent<IPointerClickHandler>();
                    if (handler != null)
                    {
                        var evt = new PointerEventData(EventSystem.current);
                        handler.OnPointerClick(evt);
                    }
                }
            }
            _GetContext().UserClickFocusArea();
            mAngleOffset = 0f;
        }

        private UIGuideContext _GetContext()
        {
            return Game.Manager.guideMan.ActiveGuideContext;
        }
    }
}