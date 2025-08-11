/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡册界面
 * @Date: 2024-01-25 15:01:11
 */

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT
{
    public class UICardAlbum : UIBase, INavBack
    {
        [SerializeField] private GameObject overviewGo;
        [SerializeField] private GameObject groupInfoGo;
        //卡册信息总览
        [SerializeField] private UICardGroupScrollRect groupScrollRect;
        [SerializeField] private TMP_Text albumTitle;
        [SerializeField] private UITextState albumTitleState;
        [SerializeField] private UIImageRes albumBg;
        [SerializeField] private Button albumTipsBtn;
        [SerializeField] private TMP_Text actTimeText;
        [SerializeField] private GameObject albumNormalGo;
        [SerializeField] private GameObject albumFinishGo;
        [SerializeField] private List<MBRewardIcon> albumRewardList;
        [SerializeField] private MBRewardProgress albumProgress;
        //具体卡组信息
        [SerializeField] private TMP_Text groupTitle;
        [SerializeField] private UITextState groupTitleState;
        [SerializeField] private Button groupTipsBtn;
        [SerializeField] private GameObject groupNormalGo;
        [SerializeField] private GameObject groupFinishGo;
        [SerializeField] private UIImageRes groupIcon;
        [SerializeField] private TMP_Text groupName;
        [SerializeField] private List<MBRewardIcon> groupRewardList;
        [SerializeField] private MBRewardProgress groupProgress;
        [SerializeField] private TMP_Text groupIndexText;
        [SerializeField] private Button groupCloseBtn;
        [SerializeField] private UICardCellDrag cardCellDrag;   //卡组拖拽组件
        [SerializeField] private GameObject block;   //点击阻挡
        //普通版/典藏版UI切换相关
        [SerializeField] private UIImageState bgSwitch;
        [SerializeField] private UIImageState albumBgSwitch;
        [SerializeField] private UIImageState groupBgSwitch;
        [SerializeField] private UIImageState bottomMaskSwitch;
        [SerializeField] private UIImageState bottomLBarSwitch;
        [SerializeField] private UIImageState bottomRBarSwitch;
        [SerializeField] private Button switchBtn;
        [SerializeField] private UIImageState switchImageState;
        [SerializeField] private Transform noTradeTitleNode;
        [SerializeField] private Transform tradeTitleNode;

        //兑换
        [SerializeField] private GameObject exchangeEntry;
        [SerializeField] private GameObject exchangePoint;
        //交换
        [SerializeField] private GameObject inboxEntry;
        [SerializeField] private GameObject inboxRedPoint;
        [SerializeField] private GameObject exchangeEffect;
        [SerializeField] private GameObject cardPreView;
        
        //卡片组layout
        private List<UICardCellLayout> _uiCardLayoutList = new List<UICardCellLayout>();
        //默认最多可创建的layout个数
        private static readonly int MAX_CREATE_CARD_LAYOUT_NUM = 9;    
        //当前显示的卡组id
        private int _curShowGroupId = 0;
        //万能卡兑换卡片时用于显示的单独大卡片
        private UICardCell _bigCardCell;
        private Coroutine _coroutine;
        private Sequence _tweenSeq;

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/Root/Overview/BtnClose", base.Close).FixPivot();
            albumTipsBtn.WithClickScale().FixPivot().onClick.AddListener(_OnTipsBtnClick);
            groupTipsBtn.WithClickScale().FixPivot().onClick.AddListener(_OnTipsBtnClick);
            groupCloseBtn.WithClickScale().FixPivot().onClick.AddListener(_OnCloseGroup);
            switchBtn.WithClickScale().FixPivot().onClick.AddListener(_OnSwitchBtnClick);
            groupScrollRect.SetupCellClickCb(_OnGroupCellClick);
            groupScrollRect.InitLayout();
            //读配置获取当前卡册的卡组数量 用于初始化卡组layout list
            var cardGroupCount = Game.Manager.cardMan.GetCardAlbumData()?.GetConfig()?.GroupInfo?.Count ?? MAX_CREATE_CARD_LAYOUT_NUM;
            var totalNum = cardGroupCount + 2;
            cardCellDrag.Prepare(totalNum);
            for (int i = 1; i <= totalNum; i++)
            {
                bool isTemp = i == 1 || i == totalNum;
                var trans = cardCellDrag.CreateCardCellLayout(isTemp);
                if (trans != null)
                {
                    _uiCardLayoutList.Add(AddModule(new UICardCellLayout(trans, isTemp)));
                }
            }
            _bigCardCell = AddModule(new UICardCell(transform.Find("Content/Root/GroupInfo/CardInfo/UICardCell")));
            transform.AddButton("Content/Root/Overview/BtnExchange", _OnBtnExchangeClick);
            inboxEntry.transform.AddButton(null, _OnBtnInboxClick);
            cardPreView.transform.AddButton(null, _OnBtnPreviewClick);
        }

        protected override void OnParse(params object[] items)
        {
            
        }

        protected override void OnPreOpen()
        {
            Game.Manager.cardMan.ResetShowGoldAlbumState();
            cardCellDrag.InitDrag(_OnDragSuccess, _OnClickSuccess);
            _RefreshInfo();
            _RefreshCardActTime();
            _RefreshBgSwitch();
            RefreshExchangeState();
            RefreshCardTrade();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnOneSecondDriver);
            MessageCenter.Get<MSG.GAME_CARD_REDPOINT_UPDATE>().AddListener(_OnCardRedPointUpdate);
            MessageCenter.Get<MSG.UI_JUMP_TO_SELECT_CARD>().AddListener(_OnJumpToSelectCard);
            MessageCenter.Get<MSG.CARD_STAR_EXCHANGE>().AddListener(RefreshExchangeState);
            MessageCenter.Get<MSG.UI_JUMP_TO_ALBUM_MAIN_VIEW>().AddListener(_OnJumpToMainView);
            MessageCenter.Get<MSG.UI_PULL_PENDING_CARD_INFO_SUCCESS>().AddListener(RefreshCardTrade);
            MessageCenter.Get<MSG.UI_GIVE_CARD_SUCCESS>().AddListener(_RefreshCardCellLayout);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnOneSecondDriver);
            MessageCenter.Get<MSG.GAME_CARD_REDPOINT_UPDATE>().RemoveListener(_OnCardRedPointUpdate);
            MessageCenter.Get<MSG.UI_JUMP_TO_SELECT_CARD>().RemoveListener(_OnJumpToSelectCard);
            MessageCenter.Get<MSG.CARD_STAR_EXCHANGE>().RemoveListener(RefreshExchangeState);
            MessageCenter.Get<MSG.UI_JUMP_TO_ALBUM_MAIN_VIEW>().RemoveListener(_OnJumpToMainView);
            MessageCenter.Get<MSG.UI_PULL_PENDING_CARD_INFO_SUCCESS>().RemoveListener(RefreshCardTrade);
            MessageCenter.Get<MSG.UI_GIVE_CARD_SUCCESS>().RemoveListener(_RefreshCardCellLayout);
        }

        protected override void OnPostClose()
        {
            Game.Manager.cardMan.ResetShowGoldAlbumState();
            _curShowGroupId = 0;
            cardCellDrag.ClearDrag();
            _ClearCoroutine();
            _ClearSeq();
        }

        private void _RefreshInfo()
        {
            overviewGo.gameObject.SetActive(_curShowGroupId <= 0);
            groupInfoGo.gameObject.SetActive(_curShowGroupId > 0);
            if (_curShowGroupId <= 0)
            {
                _RefreshAlbumInfo();
            }
            else
            {
                _RefreshGroupInfo();
            }
        }

        private void _RefreshBgSwitch()
        {
            var cardMan = Game.Manager.cardMan;
            var isPremium = false;
            if (!cardMan.IsNeedFakeAlbumData)
            {
                //默认目前若为重玩的卡册 则肯定显示典藏UI
                isPremium = cardMan.CheckIsRestartAlbum();
            }

            bgSwitch.Select(CheckTradeState(isPremium));
            albumBgSwitch.Enabled(!isPremium);
            groupBgSwitch.Enabled(!isPremium);
            bottomMaskSwitch.Enabled(!isPremium);
            bottomLBarSwitch.Enabled(!isPremium);
            bottomRBarSwitch.Enabled(!isPremium);
            var fontResIndex = isPremium ? 27 : 3;
            var config = FontMaterialRes.Instance.GetFontMatResConf(fontResIndex);
            if (config != null)
            {
                config.ApplyFontMatResConfig(groupIndexText);
            }
            //当前如果是典藏版卡册 则切换按钮常显
            switchBtn.gameObject.SetActive(cardMan.CheckIsRestartAlbum());
            switchImageState.Enabled(isPremium);
            //切换标题颜色显示
            albumTitleState.Select(CheckTradeState(!isPremium));
            groupTitleState.Select(CheckTradeState(!isPremium));
            //切换标题位置
            albumTitleState.transform.position =
                cardMan.IsCardTradeUnlock ? tradeTitleNode.position : noTradeTitleNode.position;
            groupTitleState.transform.position =
                cardMan.IsCardTradeUnlock ? tradeTitleNode.position : noTradeTitleNode.position;
        }

        private int CheckTradeState(bool isPremium)
        {
            if (Game.Manager.cardMan.IsCardTradeUnlock) return isPremium ? 1 : 0;
            return isPremium ? 3 : 2;
        }

        private void _RefreshAlbumInfo()
        {
            if (_curShowGroupId > 0) return;
            var cardMan = Game.Manager.cardMan;
            var albumData = cardMan.GetCardAlbumData(cardMan.IsNeedFakeAlbumData);
            if (albumData == null) return;
            var albumConfig = albumData.GetConfig();
            albumTitle.text = I18N.Text(albumConfig.Name);
            albumBg.SetImage(albumConfig.Image);
            albumData.GetAllCollectProgress(out var ownCount, out var allCount);
            bool isCollectAll = ownCount == allCount;
            albumNormalGo.SetActive(!isCollectAll);
            albumFinishGo.SetActive(isCollectAll);
            if (!isCollectAll)
            {
                int rewardCount = albumConfig.Reward.Count;
                for (int i = 0; i < albumRewardList.Count; i++)
                {
                    var reward = albumRewardList[i];
                    if (i < rewardCount)
                    {
                        reward.gameObject.SetActive(true);
                        reward.Refresh(albumConfig.Reward[i]?.ConvertToRewardConfig());
                    }
                    else
                    {
                        reward.gameObject.SetActive(false);
                    }
                }
                albumProgress.Refresh(ownCount, allCount);
            }
            _RefreshCardGroup();
        }

        private void _RefreshCardGroup()
        {
            var albumData = Game.Manager.cardMan.GetCardAlbumData();
            if (albumData == null) return;
            groupScrollRect.UpdateData(albumData.GetConfig().GroupInfo);
        }
        
        private void _RefreshCardActTime()
        {
            var cardAct = Game.Manager.cardMan.GetCardActivity();
            if (cardAct == null)
            {
                Close();
                return;
            }
            UIUtility.CountDownFormat(actTimeText, cardAct.Countdown);
        }
        
        private void _RefreshGroupInfo()
        {
            if (_curShowGroupId <= 0) return;
            var cardMan = Game.Manager.cardMan;
            var albumData = cardMan.GetCardAlbumData(cardMan.IsNeedFakeAlbumData);
            if (albumData == null) return;
            var groupData = albumData.TryGetCardGroupData(_curShowGroupId);
            if (groupData == null) return;
            var groupConfig = groupData.GetConfig();
            if (groupConfig == null) return;
            groupTitle.text = I18N.Text(albumData.GetConfig()?.Name);
            groupIcon.SetImage(groupConfig.Icon);
            groupName.text = I18N.Text(groupConfig.Name);
            groupIndexText.text = ((albumData.GetConfig()?.GroupInfo?.IndexOf(_curShowGroupId) ?? 0) + 1).ToString();
            albumData.GetCollectProgress(_curShowGroupId, out var ownCount, out var allCount);
            bool isCollectAll = ownCount == allCount;
            if (isCollectAll)
            {
                groupNormalGo.SetActive(false);
                groupFinishGo.SetActive(true);
            }
            else
            {
                groupNormalGo.SetActive(true);
                groupFinishGo.SetActive(false);
                int rewardCount = groupConfig.Reward.Count;
                for (int i = 0; i < groupRewardList.Count; i++)
                {
                    var reward = groupRewardList[i];
                    if (i < rewardCount)
                    {
                        reward.gameObject.SetActive(true);
                        reward.Refresh(groupConfig.Reward[i]?.ConvertToRewardConfig());
                    }
                    else
                    {
                        reward.gameObject.SetActive(false);
                    }
                }
                groupProgress.Refresh(ownCount, allCount);
            }
            //刷新卡组中的卡片信息
            _RefreshCardCellLayout();
        }

        private void _RefreshCardCellLayout()
        {
            if (_curShowGroupId <= 0) return;
            var groupInfo = Game.Manager.cardMan.GetCardAlbumData()?.GetConfig()?.GroupInfo;
            if (groupInfo == null || groupInfo.Count < 0) return;
            int curGroupIndex = groupInfo.IndexOf(_curShowGroupId) + 1;
            int leftGroupId = curGroupIndex <= 1 ? -1 : groupInfo[curGroupIndex - 2];
            int rightGroupId = curGroupIndex >= groupInfo.Count ? -1 : groupInfo[curGroupIndex];
            cardCellDrag.GetCurShowIndex(out int curIndex, out int leftIndex, out int rightIndex);
            //刷新对应group
            _uiCardLayoutList[curIndex].Show(_curShowGroupId);
            _uiCardLayoutList[leftIndex].Show(leftGroupId);
            _uiCardLayoutList[rightIndex].Show(rightGroupId);
            //刷新是否可以左右拖拽的状态
            cardCellDrag.SetCanDragLeft(rightGroupId != -1);    //右侧group没有时 不能向左划
            cardCellDrag.SetCanDragRight(leftGroupId != -1);    //左侧group没有时 不能向右划
        }

        private void _OnOneSecondDriver()
        {
            _RefreshCardActTime();
        }

        private void _OnTipsBtnClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UICardAlbumGuide);
        }

        private void _OnCloseGroup()
        {
            Game.Manager.audioMan.TriggerSound("CardBack");
            //将本卡组中所有卡片标记为已查看
            _SetGroupCardSee();
            _curShowGroupId = 0;
            _RefreshInfo();
        }

        private void _OnJumpToMainView()
        {
            _curShowGroupId = 0;
            _RefreshInfo();
        }

        private void _OnSwitchBtnClick()
        {
            Game.Manager.cardMan.SwitchShowGoldAlbumState();  //切换显示
            _SwitchAlbumUI();
        }

        private void _SwitchAlbumUI()
        {
            PlayCloseAnim(() =>
            {
                _RefreshInfo();
                _RefreshBgSwitch();
                PlayOpenAnim();
            });
        }

        private void _OnGroupCellClick(int groupId)
        {
            if (groupId <= 0) return;
            Game.Manager.audioMan.TriggerSound("CardActivityClick");
            _curShowGroupId = groupId;
            var groupInfo = Game.Manager.cardMan.GetCardAlbumData()?.GetConfig()?.GroupInfo;
            if (groupInfo != null && groupInfo.Count > 0)
            {
                int curGroupIndex = groupInfo.IndexOf(_curShowGroupId) + 1;
                cardCellDrag.SetDragStartProgress(curGroupIndex);
            }
            _RefreshInfo();
        }

        //将本卡组中所有卡片标记为已查看
        private void _SetGroupCardSee()
        {
            if (_curShowGroupId <= 0) return;
            var cardMan = Game.Manager.cardMan;
            if (!cardMan.IsNeedFakeAlbumData)
            {
                cardMan.SetGroupCardSee(_curShowGroupId);
            }
        }

        private void _OnCardRedPointUpdate(int cardId)
        {
            _RefreshCardGroup();
        }

        private void _OnDragSuccess(bool isLeft)
        {
            //将本卡组中所有卡片标记为已查看
            _SetGroupCardSee();
            //计算新的group id
            var groupInfo = Game.Manager.cardMan.GetCardAlbumData()?.GetConfig()?.GroupInfo;
            if (groupInfo == null || groupInfo.Count < 0) return;
            int curGroupIndex = groupInfo.IndexOf(_curShowGroupId) + 1;
            if (isLeft)
            {
                _curShowGroupId = curGroupIndex >= groupInfo.Count ? -1 : groupInfo[curGroupIndex];
            }
            else
            {
                _curShowGroupId = curGroupIndex <= 1 ? -1 : groupInfo[curGroupIndex - 2];
            }
            //刷新
            _RefreshInfo();
        }

        private void _OnClickSuccess(int pageIndex, int cellIndex)
        {
            _uiCardLayoutList[pageIndex].OnClickCardCell(cellIndex);
        }

        public Transform FindGroupByIndex(int index)
        {
            var group = index / 3;
            return groupScrollRect.transform.GetChild(0).GetChild(group + 1).GetChild(index - group * 3);
        }

        public bool IsOverViewPanel()
        {
            return overviewGo.activeSelf;
        }

        public bool IsGroupInfoPanel()
        {
            return groupInfoGo.activeSelf;
        }
        
        #region 万能卡表现相关
        
        private void _OnJumpToSelectCard(int targetCardId, Action cb)
        {
            var cardData = Game.Manager.cardMan.GetCardData(targetCardId);
            if (cardData == null) return;
            var groupData = Game.Manager.cardMan.GetCardAlbumData()?.TryGetCardGroupData(cardData.BelongGroupId);
            if (groupData == null) return;
            var cellIndex = groupData.GetConfig()?.CardInfo?.IndexOf(targetCardId) ?? 0;
            _OnGroupCellClick(groupData.CardGroupId);
            UIManager.Instance.Block(true);
            _ClearCoroutine();
            _ClearSeq();
            _coroutine = StartCoroutine(_BigCardCellDoTween(cellIndex, targetCardId, cb));
        }

        private IEnumerator _BigCardCellDoTween(int cellIndex, int targetCardId, Action cb)
        {
            //每次都重置一下位置和大小
            _bigCardCell.ModuleRoot.localScale = new Vector3(1.8f, 1.8f,1.8f);
            _bigCardCell.ModuleRoot.localPosition = new Vector3(0, 242f,0);
            //等一帧 等待cell位置刷新
            yield return null;
            cardCellDrag.GetCurShowIndex(out int curIndex, out _, out _);
            //刷新对应group
            var targetPos = _uiCardLayoutList[curIndex].GetCardCellPos(cellIndex);
            _bigCardCell.Show(targetCardId);
            _tweenSeq = DOTween.Sequence();
            _tweenSeq.Append(_bigCardCell.ModuleRoot.DOMove(targetPos, 0.3f).SetEase(Ease.OutCubic));
            _tweenSeq.Join(_bigCardCell.ModuleRoot.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutCubic));
            _tweenSeq.OnComplete(() =>
            {
                UIManager.Instance.Block(false);
                cb?.Invoke();
                _bigCardCell.Hide();
            });
            _tweenSeq.Play();
        }

        private void _ClearCoroutine()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        private void _ClearSeq()
        {
            _tweenSeq?.Kill();
            _tweenSeq = null;
        }
        
        #endregion

        public void OnNavBack()
        {
            //OnNavBack时若处于卡组界面 则返回卡册界面，若处于卡册界面，则关闭整个界面
            if (_curShowGroupId > 0)
            {
                _OnCloseGroup();
            }
            else
            {
                Close();
            }
        }

        #region 兑换

        private void _OnBtnExchangeClick()
        {
            Game.Manager.cardMan.OpenUICardExchangeReward();
            RefreshExchangeState();
        }
        
        private void RefreshExchangeState()
        {
            exchangeEntry.gameObject.SetActive(Game.Manager.cardMan.IsOpenStarExchange());
            exchangePoint.gameObject.SetActive(Game.Manager.cardMan.CanShowStarExchangeRP());
            exchangeEffect.gameObject.SetActive(exchangePoint.activeSelf);
        }

        #endregion

        #region 交换

        private void RefreshCardTrade()
        {
            inboxEntry.SetActive(Game.Manager.cardMan.IsCardTradeUnlock);
            cardPreView.SetActive(Game.Manager.cardMan.IsCardTradeUnlock);
            inboxRedPoint.SetActive(Game.Manager.cardMan.CheckHasPendingCards());
        }
        
        private void _OnBtnInboxClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UICardPending);
        }
        
        private void _OnBtnPreviewClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UICardAlbumPreview);
        }

        #endregion
    }
}
