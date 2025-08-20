/*
 * @Author: tang.yan
 * @Description: 万能卡选卡界面 
 * @Date: 2024-03-27 20:03:54
 */
using EL;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace FAT
{
    public class UICardJokerSelect : UIBase
    {
        [SerializeField] private GameObject normalGo;
        [SerializeField] private GameObject goldGo;
        [SerializeField] private TMP_Text jokerName;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text tipsText;
        [SerializeField] private Button selectBtn;
        [SerializeField] private Button selectTextBtn;
        [SerializeField] private GameObject rightGo;
        [SerializeField] private Button confirmBtn;
        [SerializeField] private GameObject selectGo;
        [SerializeField] private GameObject unselectGo;
        [SerializeField] private UICardJokerScrollView infoScrollView;
        [SerializeField] private ScrollRect infoScrollRect;
        private Button _closeBtn;
        
        //目前正准备使用的万能卡数据
        private CardJokerData _curSelectJokerData;
        //目前是否显示所有的卡
        private bool _isShowAll = false;
        //目前选择的卡牌id
        private int _curSelectCardId = 0;
        //卡组cell数据 key:要显示的卡组id value:卡组中要显示的卡片id List
        private List<CardJokerGroupCellData> _groupCellDataDict = new List<CardJokerGroupCellData>();
        //是否显示下一张万能卡的入口
        private bool _isShowNext = false;
        //是否强制玩家选择(不显示关闭按钮)
        private bool _isForceChoose = false;
        //目前卡片是否值得被使用
        private bool _isUseful = false;
        //记录目前是否在滚动
        private bool _isScrolling = false;
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", _OnBtnClose);
            _closeBtn = transform.FindEx<Button>("Content/Root/BtnClose");
            _closeBtn.WithClickScale().FixPivot().onClick.AddListener(_OnBtnClose);
            transform.AddButton("Content/Root/Panel/Info/Top/TipsBtn", _OnBtnTipsClick);
            selectBtn.FixPivot().WithClickScale().onClick.AddListener(_OnSelectBtnClick);
            selectTextBtn.onClick.AddListener(_OnSelectBtnClick);
            confirmBtn.FixPivot().WithClickScale().onClick.AddListener(_OnConfirmBtnClick);
            infoScrollView.Setup();
        }

        protected override void OnParse(params object[] items) { }

        protected override void OnPreOpen()
        {
            var roundData = Game.Manager.cardMan.GetCardRoundData();
            if (roundData == null) return;
            var jokerData = roundData.GetCurIndexJokerData();
            if (jokerData == null) return;
            _curSelectJokerData = jokerData;
            _curSelectJokerData.SetLockExpire(true);
            _curSelectCardId = _curSelectJokerData.GetCurSelectCardId();
            _RefreshBaseInfo();
            _RefreshTime();
            _RefreshExpire();
            _RefreshSelectBtn();
            _RefreshConfirmBtn();
            _RefreshGroupScroll(true);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_RefreshTime);
            MessageCenter.Get<MSG.GAME_CARD_JOKER_SELECT>().AddListener(_OnCardSelect);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshTime);
            MessageCenter.Get<MSG.GAME_CARD_JOKER_SELECT>().RemoveListener(_OnCardSelect);
        }

        protected override void OnPostClose()
        {
            infoScrollView.Clear();
            _curSelectCardId = 0;
            _curSelectJokerData.SetLockExpire(false);
            _curSelectJokerData = null;
            _isShowAll = false;
            _isUseful = false;
            if (_isShowNext)
            {
                bool isLast = Game.Manager.cardMan.GetCardRoundData()?.ProcessNextJokerIndex() ?? true;
                if (!isLast)
                {
                    Game.Manager.cardMan.OpenJokerEntranceUI();
                }
            }
            _isShowNext = false;
            _isScrolling = false;
        }

        private void _RefreshBaseInfo()
        {
            if (_curSelectJokerData == null) 
                return;
            var isGold = _curSelectJokerData.IsGoldCard == 1;
            normalGo.SetActive(!isGold);
            goldGo.SetActive(isGold);
            jokerName.text = I18N.Text(_curSelectJokerData.GetObjBasicConfig()?.Name ?? "");
            tipsText.text = I18N.Text(!isGold ? "#SysComDesc330" : "#SysComDesc331");
            //在选卡界面打开时，如果当前万能卡是不值得被使用的卡，则默认显示所有卡片
            _isUseful = Game.Manager.cardMan.GetCardRoundData()?.CheckIsUsefulJokerData(_curSelectJokerData) ?? false;
            if (!_isUseful)
            {
                _isShowAll = true;
            }
        }
        
        private void _RefreshTime()
        {
            //当活动结束或者数据非法时 直接关界面
            if (!Game.Manager.cardMan.CheckValid() || _curSelectJokerData == null)
            {
                Close();
                return;
            }
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _curSelectJokerData.ExpireTs - t);
            UIUtility.CountDownFormat(timeText, diff);
            _isForceChoose = diff <= 0;
            //当检查到时间上过期时 还要检查一下当前卡片是否值得被使用，如果不值得被使用则允许玩家暂时关闭界面 反之则不允许
            if (_isForceChoose)
            {
                if (!_isUseful)
                    _isForceChoose = false;
            }
            _RefreshExpire();
        }
        
        private void _RefreshExpire()
        {
            _closeBtn.gameObject.SetActive(!_isForceChoose);
        }

        private void _OnCardSelect()
        {
            if (!Game.Manager.cardMan.CheckValid() || _curSelectJokerData == null)
            {
                return;
            }
            _curSelectCardId = _curSelectJokerData.GetCurSelectCardId();
            _RefreshConfirmBtn();
            infoScrollView.RefreshGroup();
        }

        private void _RefreshSelectBtn()
        {
            rightGo.SetActive(!_isShowAll);
        }
        
        private void _RefreshConfirmBtn()
        {
            confirmBtn.interactable = _curSelectCardId > 0;
            selectGo.SetActive(_curSelectCardId > 0);
            unselectGo.SetActive(_curSelectCardId <= 0);
        }
        
        private void _RefreshGroupScroll(bool isFirst = false)
        {
            _RefreshGroupData(out var firstNormalGroupId, out var firstGoldGroupId);
            infoScrollRect.StopMovement();
            infoScrollView.Clear();
            infoScrollView.BuildByGroupData(_groupCellDataDict);
            //切换显示时 检查一下当前有没有选择的卡牌id
            //如果有 检查一下_groupCellDataDict是否含有卡牌所在的组 如果有则跳转显示该组  如果没有则清空选择并滑到最顶处
            var curSelectCardData = Game.Manager.cardMan.GetCardAlbumData()?.TryGetCardData(_curSelectCardId);
            if (curSelectCardData != null)
            {
                bool isShow = false;
                foreach (var cellData in _groupCellDataDict)
                {
                    if (cellData.GroupId == curSelectCardData.BelongGroupId)
                    {
                        isShow = true;
                        break;
                    }
                }
                if (isShow)
                {
                    infoScrollView.JumpToMatch(x => x.data.GroupId == curSelectCardData.BelongGroupId, null);
                }
                else
                {
                    //如果没有则清空选择
                    _curSelectJokerData.SetCurSelectCardId(0);
                }
            }
            //如果没有 且 这张卡片值得被使用 则走自动定位逻辑
            //a. 白卡使用弹窗：自动定位到未获得的白卡处（按卡组顺序在前的）
            //b. 闪卡使用弹窗：
            //   i. 自动定位到未获得的金卡处（按卡组顺序在前的）
            //   ii. 如果没有未获得的金卡，定位到未获得的白卡处（按卡组顺序在前的）
            else if (_isUseful)
            {
                var isGold = _curSelectJokerData.IsGoldCard == 1;
                var belongGroupId = !isGold ? firstNormalGroupId : (firstGoldGroupId > 0 ? firstGoldGroupId : firstNormalGroupId);
                if (isFirst)
                {
                    _isScrolling = true;
                    infoScrollView.ScrollToMatch(x => x.data.GroupId == belongGroupId, () => { _isScrolling = false;});
                }
                else
                {
                    infoScrollView.JumpToMatch(x => x.data.GroupId == belongGroupId, null);
                }
            }
        }

        private void _OnBtnTipsClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UICardAlbumGuide, 4); 
        }

        private void _OnSelectBtnClick()
        {
            if (_isScrolling)
                return;
            _isShowAll = !_isShowAll;
            _RefreshSelectBtn();
            _RefreshGroupScroll();
        }

        private void _OnConfirmBtnClick()
        {
            _isShowNext = false;
            UIManager.Instance.OpenWindow(UIConfig.UICardJokerConfirm);
        }
        
        private void _OnBtnClose()
        {
            if (_isForceChoose) return;
            if (_isScrolling) return;
            _isShowNext = true;
            Close();
        }

        private void _RefreshGroupData(out int firstNormalGroupId, out int firstGoldGroupId)
        {
            firstNormalGroupId = 0;    //第一个未获得的白卡id
            firstGoldGroupId = 0;      //第一个未获得的金卡id
            _groupCellDataDict.Clear();
            var cardMan = Game.Manager.cardMan;
            var albumData = cardMan.GetCardAlbumData();
            if (albumData == null) return;
            foreach (var groupData in albumData.GetAllGroupDataMap().Values)
            {
                //根据是否显示全部 来构造不同的数据
                if (_isShowAll || (!_isShowAll && !groupData.IsCollectAll))
                {
                    var cardList = new List<int>();
                    foreach (var cardId in groupData.GetConfig().CardInfo)
                    {
                        var cardData = albumData.TryGetCardData(cardId);
                        if (cardData == null)
                            continue;
                        if (_isShowAll)
                        {
                            cardList.Add(cardId);
                        }
                        else
                        {
                            if (!cardData.IsOwn)
                                cardList.Add(cardId);
                        }
                        //尝试设置第一个未获得的白卡id
                        if (firstNormalGroupId <= 0)
                        {
                            if (!cardData.IsOwn && !cardData.GetConfig().IsGold)
                            {
                                firstNormalGroupId = cardData.BelongGroupId;
                            }
                        }
                        //尝试设置第一个未获得的金卡id
                        if (firstGoldGroupId <= 0)
                        {
                            if (!cardData.IsOwn && cardData.GetConfig().IsGold)
                            {
                                firstGoldGroupId = cardData.BelongGroupId;
                            }
                        }
                    }

                    var data = new CardJokerGroupCellData()
                    {
                        GroupId = groupData.CardGroupId,
                        CardIdList = cardList,
                    };
                    _groupCellDataDict.Add(data);
                }
            }
        }
    }
}