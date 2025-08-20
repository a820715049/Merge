/*
 * @Author: tang.yan
 * @Description: UI Tips基类方法 主要处理了区域外点击关闭以及tips根据宽度适配位置的逻辑
 * @Date: 2023-12-26 19:12:20
 */

using DG.Tweening;
using EL;
using UnityEngine;

namespace FAT
{
    //UI Tips基类方法 主要处理了区域外点击关闭以及tips根据宽度适配位置的逻辑
    public class UITipsBase : UIBase, INavBack
    {
        [SerializeField] private RectTransform contentTrans;
        [SerializeField] private RectTransform arrowTrans; //底部箭头 箭头有朝下显示的情况时 不能为空
        [SerializeField] private RectTransform topArrowTrans; //顶部箭头 箭头有朝上显示的情况时 不能为空
        [SerializeField] private UIOutsideCheckDown outsideCheckDown;

        private Vector3 _startWorldPos = new Vector3();
        private float _curOffset;
        private float _curTipsWidth; //当前tips面板的最终宽度

        private float _curTipsHeight; //当前tips面板的最终高度

        //当前tips面板的附加宽度 默认情况下会贴着屏幕边缘进行宽度适配，若美术效果要求和边缘有一定空隙，则可以设置这个值 传个假的附加宽度
        private float _curExtraWidth;

        private Vector2 _bottomPivot = new Vector2(0.5f, 0);
        private Vector3 _targetScale = new Vector3(0.1f, 0.1f, 0.1f);
        private float _tweenDuration = 0.15f;
        private bool _isPlayTween = false;

        private void Awake()
        {
            if (outsideCheckDown != null)
            {
                outsideCheckDown.OnHideHandler = _OnOutsideClose; //注册关闭事件回调
            }
        }

        //设置tips期望的初始位置以及偏移参数
        protected void _SetTipsPosInfo(params object[] items)
        {
            if (items[0] is Vector3 pos)
            {
                _startWorldPos.Set(pos.x, pos.y, pos.z);
            }

            _curOffset = (float)items[1];
        }

        //设置当前tips面板宽度，如果tips面板宽度是不确定的，则需要在数据确定后计算好宽度并设置
        //如果不主动调用设置宽度，则会以contentTrans的宽度为准
        protected void _SetCurTipsWidth(float width)
        {
            _curTipsWidth = width;
        }

        //设置当前tips面板高度，如果tips面板高度是不确定的，则需要在数据确定后计算好高度并设置
        //如果不主动调用设置高度，则会以contentTrans的高度为准
        protected void _SetCurTipsHeight(float height)
        {
            _curTipsHeight = height;
        }

        //设置当前tips面板的附加宽度
        //默认情况下会贴着屏幕边缘进行宽度适配 若美术效果要求和边缘有一定空隙，则可以设置这个值 传个假的附加宽度
        //注意实际prefab宽度还是使用_SetCurTipsWidth设置，此方法只用于传虚拟额外宽度值
        protected void _SetCurExtraWidth(float width)
        {
            _curExtraWidth = width;
        }

        /// <summary>
        /// 刷新tips位置
        /// </summary>
        /// <param name="baseOffset">对应界面设置的固定的y轴上的偏移值 默认为0 一般为界面上箭头的高度</param>
        /// <param name="needFitWidth">是否需要x轴宽度上的适配 默认需要 会以传入的_startWorldPos为基点进行宽度适配
        ///                            若传false则保持界面原始位置不变(如居中展示) 但会有y轴上的偏移</param>
        protected void _RefreshTipsPos(float baseOffset = 0f, bool needFitWidth = true)
        {
            contentTrans.pivot = _bottomPivot; //适配时默认锚点在底部
            contentTrans.localScale = Vector3.one; //适配时默认Scale为1
            _curOffset += baseOffset; //固定偏移
            //如果外部没有主动设置，则以contentTrans当时的宽高为准进行适配
            if (_curTipsWidth <= 0) _curTipsWidth = contentTrans.rect.width;
            if (_curTipsHeight <= 0) _curTipsHeight = contentTrans.rect.height;
            //传入的起始点为世界坐标，先将世界坐标转屏幕坐标,然后屏幕坐标转UI坐标
            var localPos = UIManager.Instance.TransWorldPosToLocal(_startWorldPos);
            var pos = new Vector3();
            //记录是否发生了顶部位置适配
            bool isOverTop = false;
            if (needFitWidth)
            {
                //同时进行宽度高度适配
                //将UI坐标加上偏移值
                pos.Set(localPos.x, localPos.y + _curOffset, 0);
                //计算用于适配的数值宽度
                var fitWidth = _curTipsWidth + _curExtraWidth;
                //结合起始点以及面板宽度进行宽度适配
                pos = UIManager.Instance.FitUITipsPosByWidth(pos, fitWidth);
                //结合起始点以及面板高度和偏移进行高度适配
                pos = UIManager.Instance.FitUITipsPosByHeight(pos, _curTipsHeight, _curOffset, out isOverTop);
            }
            else
            {
                //不进行宽度适配 直接将x轴设为0 保持UI在宽度上居中,如果有y轴偏移值则会加上
                var contentPos = contentTrans.localPosition;
                pos.Set(0, localPos.y + _curOffset, contentPos.z);
                //结合起始点以及面板高度和偏移进行高度适配
                pos = UIManager.Instance.FitUITipsPosByHeight(pos, _curTipsHeight, _curOffset, out isOverTop);
            }

            //适配后的值回传给localPosition
            contentTrans.localPosition = pos;
            float deltaPivot = 0; //计算x轴锚点变化
            //设置完面板位置后设置箭头指向  高度上如果发生适配(isOverTop为true)需要将位置挪到起始点下方并且显示朝上的箭头
            if (isOverTop)
            {
                var arrowPos = topArrowTrans.position;
                var newArrowPos = new Vector3(_startWorldPos.x, arrowPos.y, arrowPos.z);
                topArrowTrans.position = newArrowPos;
                topArrowTrans.gameObject.SetActive(true);
                if (arrowTrans != null) arrowTrans.gameObject.SetActive(false);
                //根据底部箭头相对中间的偏移值比例 计算出锚点在x轴上的偏移值
                deltaPivot = ((0 - topArrowTrans.anchoredPosition.x) / _curTipsWidth);
            }
            else
            {
                var arrowPos = arrowTrans.position;
                var newArrowPos = new Vector3(_startWorldPos.x, arrowPos.y, arrowPos.z);
                arrowTrans.position = newArrowPos;
                arrowTrans.gameObject.SetActive(true);
                if (topArrowTrans != null) topArrowTrans.gameObject.SetActive(false);
                //根据底部箭头相对中间的偏移值比例 计算出锚点在x轴上的偏移值
                deltaPivot = ((0 - arrowTrans.anchoredPosition.x) / _curTipsWidth);
            }

            //计算最终x轴上的锚点位置 以及因为锚点改变而产生的位置偏移
            var pivotX = 0.5f - deltaPivot;
            var deltaPosX = deltaPivot * _curTipsWidth;
            _PlayShowTween(isOverTop, pivotX, deltaPosX);
        }

        private void _PlayShowTween(bool isOverTop, float pivotX, float deltaPosX)
        {
            if (_isPlayTween)
                return;
            _isPlayTween = true;
            //锚点随是否发生顶部适配而变化
            if (isOverTop)
            {
                contentTrans.pivot = new Vector2(pivotX, 1f);
                //默认是以锚点在底部进行适配的，所以如果锚点变成了在顶部 并且x轴上也发生了偏移 则位置也需要跟随变一下
                var pos = contentTrans.anchoredPosition;
                contentTrans.anchoredPosition = new Vector2(pos.x - deltaPosX, pos.y + _curTipsHeight);
            }
            else
            {
                contentTrans.pivot = new Vector2(pivotX, 0f);
                //默认是以锚点在底部进行适配的，所以y轴锚点保持为0 若x轴上也发生了偏移 则位置也需要跟随变一下
                var pos = contentTrans.anchoredPosition;
                contentTrans.anchoredPosition = new Vector2(pos.x - deltaPosX, pos.y);
            }

            contentTrans.localScale = _targetScale;
            contentTrans.DOScale(Vector3.one, _tweenDuration).SetEase(Ease.OutBack)
                .OnComplete(() => { _isPlayTween = false; });
        }

        private void _OnOutsideClose()
        {
            _PlayHideTween();
        }

        private void _PlayHideTween()
        {
            if (_isPlayTween)
                return;
            _isPlayTween = true;
            contentTrans.localScale = Vector3.one;
            contentTrans.DOScale(_targetScale, _tweenDuration).SetEase(Ease.OutFlash)
                .OnComplete(() =>
                {
                    _isPlayTween = false;
                    Close();
                });
        }

        public void OnNavBack()
        {
            _PlayHideTween();
        }

        protected override void OnPreOpen()
        {
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.UI_CLOSE_LAYER>().AddListener(CheckNeedClose);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.UI_CLOSE_LAYER>().RemoveListener(CheckNeedClose);
        }

        private void CheckNeedClose(UILayer layer)
        {
            if (layer == UILayer.AboveStatus || layer == UILayer.SubStatus)
                _PlayHideTween();
        }
    }
}
