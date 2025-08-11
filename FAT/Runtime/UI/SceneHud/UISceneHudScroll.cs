/*
 * @Author: qun.chao
 * @Date: 2024-11-20 10:58:20
 */
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace FAT
{
    public class UISceneHudScroll : MonoBehaviour
    {
        [SerializeField] private float scaleForScroll = 0.9f;
        private RectTransform _content;
        private RectTransform _viewport;
        private GameObject _up;
        private GameObject _down;
        private ScrollRect _scrollRect;
        private float _itemSize;
        private Tween _tween;

        private bool _initialized;
        private bool _isDirty;

        private void Ensure()
        {
            if (_initialized)
                return;
            _initialized = true;

            var sr = transform.Access<ScrollRect>();
            _content = sr.content;
            _viewport = sr.viewport;
            _itemSize = _content.childCount > 0 ? (_content.GetChild(0) as RectTransform).rect.height : 212f;

            _scrollRect = sr;
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            var detector = _content.gameObject.AddComponent<MBTransChange>();
            detector.OnDimensionsChange = SetDirty;
            detector.OnVisiableChange = b => SetDirty();
            _up = transform.Find("up").gameObject;
            _up.transform.Access<Button>("Arrow").onClick.AddListener(OnBtnUp);
            _down = transform.Find("down").gameObject;
            _down.transform.Access<Button>("Arrow").onClick.AddListener(OnBtnDown);
        }

        private void Start()
        {
            Ensure();
        }

        private void OnEnable()
        {
            Ensure();
            _content.anchoredPosition = Vector2.zero;
            Refresh();
        }

        private void Update()
        {
            if (_isDirty)
            {
                _isDirty = false;
                Refresh();
            }
        }

        private void SetDirty()
        {
            _isDirty = true;
        }

        private void Refresh()
        {
            Ensure();
            var height_v = _viewport.rect.height;
            var height_c = _content.rect.height * scaleForScroll;
            var pos_y = _content.anchoredPosition.y;
            if (!_content.gameObject.activeSelf || height_c < height_v)
            {
                HideScroll();
            }
            else
            {
                ShowScroll();
                if (pos_y <= 0f)
                {
                    RefreshIndicator(false, true);
                }
                else if (pos_y > height_c - height_v || Mathf.Approximately(pos_y, height_c - height_v))
                {
                    RefreshIndicator(true, false);
                }
                else
                {
                    RefreshIndicator(true, true);
                }
            }
        }

        private void ShowScroll()
        {
            if (_scrollRect.enabled)
                return;
            _scrollRect.enabled = true;
            _viewport.Access<Image>().enabled = true;
            _viewport.Access<RectMask2D>().enabled = true;
            _content.localScale = Vector3.one * scaleForScroll;
            _content.anchoredPosition = Vector3.zero;

            var layout =  _content.GetComponent<VerticalLayoutGroup>();
            layout.padding.top = 30;
            layout.padding.bottom = 50;
            LayoutRebuilder.MarkLayoutForRebuild(_content);
        }

        private void HideScroll()
        {
            if (!_scrollRect.enabled)
                return;
            _scrollRect.enabled = false;
            _viewport.Access<Image>().enabled = false;
            _viewport.Access<RectMask2D>().enabled = false;
            _content.localScale = Vector3.one;
            _content.anchoredPosition = Vector3.zero;

            _up.SetActive(false);
            _down.SetActive(false);

            var layout =  _content.GetComponent<VerticalLayoutGroup>();
            layout.padding.top = 0;
            layout.padding.bottom = 0;
            LayoutRebuilder.MarkLayoutForRebuild(_content);
        }

        private void RefreshIndicator(bool up, bool down)
        {
            if (_up.activeSelf != up)
                _up.SetActive(up);
            if (_down.activeSelf != down)
                _down.SetActive(down);
        }

        private void OnScrollValueChanged(Vector2 delta)
        {
            SetDirty();
        }

        private void OnBtnUp()
        {
            var trans = _content;
            var pos_y = trans.anchoredPosition.y;
            if (pos_y <= 0f)
                return;
            var duration = pos_y / 500f;
            if (duration > 0.5f)
                duration = 0.5f;
            _tween?.Kill();
            _tween = trans.DOLocalMoveY(0, duration).SetEase(Ease.InOutCubic).OnComplete(() => { trans.anchoredPosition = Vector2.zero; });
        }

        private void OnBtnDown()
        {
            var trans = _content;
            var pos_y = trans.anchoredPosition.y;
            var height_v = _viewport.rect.height;
            var height_c = _content.rect.height * scaleForScroll;
            var max = height_c - height_v;
            if (pos_y >= max)
                return;

            var layout = _content.GetComponent<VerticalLayoutGroup>();
            var item_space = layout.spacing;
            var top_space = layout.padding.top;
            var bottom_space = layout.padding.bottom;
            var display_offset = 80;  // 必须向上偏移 否则会被箭头挡住

            var item_size = _itemSize;
            // view区 底部有效显示边界在content里的位置
            var cur_bottom_offset = pos_y + height_v;
            // 下一个完整显示item所在位置的offset
            float item_offset = top_space;
            // 显示按钮的情况下, 认为真实活动数量等于ListActivity.list.Count, 此时没有占位元素
            for (var i = 0; i < _content.childCount; i++)
            {
                item_offset += item_size;
                var real_offset = (item_offset + display_offset) * scaleForScroll;
                if (real_offset > cur_bottom_offset)
                {
                    // 避免临界位置难以切换
                    if (!Mathf.Approximately(real_offset, cur_bottom_offset))
                    {
                        item_offset += display_offset;
                        break;
                    }
                }

                if (i == _content.childCount - 1)
                    item_offset += bottom_space;
                else
                    item_offset += item_space;
            }
            // 因为content层级有缩放 这里需要换算
            var item_offset_with_scale = item_offset * scaleForScroll;
            var duration = 0.5f * (item_offset_with_scale - cur_bottom_offset) / 500f;
            var target_y = item_offset_with_scale - height_v;
            if (target_y > max) target_y = max;
            _tween?.Kill();
            _tween = trans.DOLocalMoveY(target_y, duration).SetEase(Ease.InOutCubic).OnComplete(() => { trans.anchoredPosition = new Vector2(0f, target_y); });
        }
    }
}