/*
 *@Author:chaoran.zhang
 *@Desc:卡册活动重复奖励选卡界面滚动列表组件Item
 *@Created Time:2024.09.02 星期一 15:24:05
 */

using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static FAT.CardMan;

namespace FAT
{
    public class UICardExchangeItemCell : FancyGridViewCell<UICardStarExchangeData, UICardExchangeContext>
    {
        private UIImageRes _cardIcon;
        private Transform _starNode;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _count;
        private Transform _btnNode;
        private UIImageState _subBtnState;
        private UIImageState _addBtnState;
        private GameObject _mask;
        private GameObject _normalMask;
        private GameObject _goldMask;

        private UICardStarExchangeData _mData;

        public override void Initialize()
        {
            InitComp();
            InitButton();
        }

        /// <summary>
        /// 注册组件
        /// </summary>
        private void InitComp()
        {
            transform.Access("Normal/Icon", out _cardIcon);
            transform.Access("Normal/Star", out _starNode);
            transform.Access("Normal/Count/Num_txt", out _count);
            transform.Access("Normal/Name", out _name);
            transform.Access("Node", out _btnNode);
            transform.Access("Node/Sub", out _subBtnState);
            transform.Access("Node/Add", out _addBtnState);
            _mask = transform.Find("Normal/Mask").gameObject;
            _normalMask = _mask.transform.Find("Normal").gameObject;
            _goldMask = _mask.transform.Find("Gold").gameObject;
        }

        /// <summary>
        /// 初始化按钮点击函数
        /// </summary>
        private void InitButton()
        {
            transform.AddButton("SelectBtn", () => Context.OnCellClicked?.Invoke(_mData.CardId));
            transform.AddButton("Node/Sub", OnClickSub);
            transform.AddButton("Node/Add", OnClickAdd);
        }

        /// <summary>
        /// 减少选中数量
        /// </summary>
        private void OnClickSub()
        {
            if (_mData.SelectNum <= 0)
                return;
            _mData.SelectNum--;
            RefreshCount();
            Context.OnSelectNumChange?.Invoke(-_mData.CardStar);
        }

        /// <summary>
        /// 增加选中数量
        /// </summary>
        private void OnClickAdd()
        {
            if (_mData.SelectNum >= _mData.RepeatOwnCount)
                return;
            _mData.SelectNum++;
            RefreshCount();
            Context.OnSelectNumChange?.Invoke(_mData.CardStar);
        }


        public override void UpdateContent(UICardStarExchangeData itemData)
        {
            _mData = itemData;
            RefreshShow();
            RefreshCount();
            RefreshSelected();
        }

        /// <summary>
        /// 刷新Icon，name，star等
        /// </summary>
        private void RefreshShow()
        {
            var conf = _mData.GetObjBasicConfig();
            var card = _mData.GetConfig();
            _cardIcon.SetImage(conf.Icon);
            _normalMask.SetActive(!card.IsGold);
            _goldMask.SetActive(card.IsGold);
            _name.text = I18N.Text(conf.Name);
            var mat = FontMaterialRes.Instance.GetFontMatResConf(card.IsGold ? 38 : 8);
            mat?.ApplyFontMatResConfig(_name);
            for (var i = 0; i < _starNode.childCount; i++) _starNode.GetChild(i).gameObject.SetActive(i < card.Star);
        }

        /// <summary>
        /// 刷新选中数量以及受数量影响的状态
        /// </summary>
        private void RefreshCount()
        {
            _count.text = string.Concat(_mData.SelectNum, "/", _mData.RepeatOwnCount);
            _mask.gameObject.SetActive(_mData.SelectNum <= 0);
            _subBtnState.Setup(_mData.SelectNum <= 0 ? 1 : 0);
            _addBtnState.Setup(_mData.SelectNum >= _mData.RepeatOwnCount ? 1 : 0);
        }

        /// <summary>
        /// 刷新选中状态
        /// </summary>
        private void RefreshSelected()
        {
            _btnNode.gameObject.SetActive(Context.SelectedID == _mData.CardId);
        }
    }
}