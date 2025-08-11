/*
 *@Author:chaoran.zhang
 *@Desc:卡册活动重复奖励选卡界面
 *@Created Time:2024.09.02 星期一 15:21:46
 */

using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class UICardExchangeSelect : UIBase
    {
        private UICardExchangeScroll _scroll;
        private Transform _autoOn;
        private RectTransform _progress;
        private TextMeshProUGUI _starNum;
        private UIImageRes _box;
        private Transform _btnNode;
        private readonly List<CardMan.UICardStarExchangeData> _data = new();
        private CardStarExchangeData _exchangeData;
        private int _curScore; //当前选中了多少积分
        private bool _isAuto; //当前是否处于自动选择状态

        protected override void OnCreate()
        {
            InitComp();
            InitButton();
        }

        /// <summary>
        /// 注册组件
        /// </summary>
        private void InitComp()
        {
            transform.Access("Content/Panel/SelectArea", out _scroll);
            transform.Access("Content/Panel/AutoChoose/BtnAuto/on", out _autoOn);
            transform.Access("Content/Panel/Progress/ProgressBg/Mask", out _progress);
            transform.Access("Content/Panel/Progress/ProgressBg/Num", out _starNum);
            transform.Access("Content/Panel/Progress/Box", out _box);
            transform.Access("Content/Panel/BtnRoot", out _btnNode);
        }

        /// <summary>
        /// 初始化点击函数
        /// </summary>
        private void InitButton()
        {
            _scroll.OnCellClicked(index => SelectCell(index));
            _scroll.OnSelectNumChange(i => ChangeStarNum(i));
            transform.AddButton("Content/Panel/BtnRoot/Confirm", OnClickConfirm);
            transform.AddButton("Content/Panel/Close", Close);
            transform.AddButton("Content/Panel/AutoChoose/BtnAuto", OnClickAuto);
        }

        /// <summary>
        /// 选中卡牌时的响应
        /// </summary>
        /// <param name="id"></param>
        private void SelectCell(int id)
        {
            if (_scroll.DataCount == 0) return;

            _scroll.UpdateSelection(id);
        }

        /// <summary>
        /// 修改选中卡牌的数量时的响应
        /// </summary>
        /// <param name="num"></param>
        private void ChangeStarNum(int num)
        {
            _curScore += num;
            _starNum.text = string.Concat(_curScore, "/", _exchangeData.GetConfig().CostStar);
            _progress.anchorMax =
                new Vector2(
                    (float)_curScore / _exchangeData.GetConfig().CostStar >= 1
                        ? 1
                        : (float)_curScore / _exchangeData.GetConfig().CostStar, 1);
            RefreshButton();
            RefreshAutoState(false);
        }

        /// <summary>
        /// 点击确认按钮
        /// </summary>
        private void OnClickConfirm()
        {
            if (Game.Manager.cardMan.TryConsumeCardForReward(_data, _exchangeData.StarExchangeId))
                Close();
        }

        /// <summary>
        /// 点击自动选择按钮
        /// </summary>
        private void OnClickAuto()
        {
            if (_isAuto)
            {
                RefreshAutoState(false);
            }
            else
            {
                RefreshAutoState(true);
                Game.Manager.cardMan.ChooseBestSelectCard(_data, _exchangeData.StarExchangeId);
                RefreshProgress();
                RefreshScroll();
                RefreshButton();
            }
        }

        /// <summary>
        /// 刷新自动选择状态
        /// </summary>
        /// <param name="state">是否处于自动选择状态</param>
        private void RefreshAutoState(bool state)
        {
            _isAuto = state;
            _autoOn.gameObject.SetActive(_isAuto);
        }

        protected override void OnParse(params object[] items)
        {
            _exchangeData = Game.Manager.cardMan.GetCardRoundData().GetStarExchangeData((int)items[0]);
        }

        protected override void OnPreOpen()
        {
            Game.Manager.cardMan.FillBestSelectCardList(_data, _exchangeData.StarExchangeId);
            RefreshProgress();
            RefreshBox();
            RefreshButton();
            RefreshScroll();
            RefreshAutoState(true);
        }

        /// <summary>
        /// 刷新进度条
        /// </summary>
        private void RefreshProgress()
        {
            GetCurStarNum();
            _starNum.text = string.Concat(_curScore, "/", _exchangeData.GetConfig().CostStar);
            _progress.anchorMax = new Vector2((float)_curScore / _exchangeData.GetConfig().CostStar, 1);
        }

        /// <summary>
        /// 获取当前选取卡牌的星星总和
        /// </summary>
        private void GetCurStarNum()
        {
            _curScore = 0;
            foreach (var kv in _data) _curScore += kv.SelectNum * kv.CardStar;
            _curScore += Game.Manager.cardMan.GetTotalFixedStarNum();
        }

        /// <summary>
        /// 刷新宝箱显示
        /// </summary>
        private void RefreshBox()
        {
            _box.SetImage(_exchangeData.GetBasicConfig().Icon);
        }

        /// <summary>
        /// 刷新按钮显示状态
        /// </summary>
        private void RefreshButton()
        {
            _btnNode.GetChild(0).gameObject.SetActive(_curScore >= _exchangeData.GetConfig().CostStar);
            _btnNode.GetChild(1).gameObject.SetActive(_curScore < _exchangeData.GetConfig().CostStar);
        }

        /// <summary>
        /// 刷新列表数据
        /// </summary>
        private void RefreshScroll()
        {
            _scroll.UpdateContents(_data);
            _scroll.UpdateSelection(-1);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(OnActivityEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(OnActivityEnd);
        }

        /// <summary>
        /// 活动结束时自动关闭界面
        /// </summary>
        /// <param name="act"></param>
        /// <param name="expire"></param>
        private void OnActivityEnd(ActivityLike act, bool expire)
        {
            if (act.Lite.Type == EventType.CardAlbum)
                Close();
        }
    }
}