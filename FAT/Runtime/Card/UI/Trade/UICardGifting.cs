/*
 * @Author: tang.yan
 * @Description: 选择好友赠卡界面 
 * @Date: 2024-10-23 11:10:42
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using fat.gamekitdata;
using TMPro;
using fat.rawdata;

namespace FAT
{
    public class UICardGifting : UIBase
    {
        [SerializeField] private RectTransform infoRect;
        [SerializeField] private InputField inputField;
        [SerializeField] private UITextExt placeHolder;
        [SerializeField] private UICardGiftingScrollRect infoScrollRect;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private GameObject waitingGo;
        [SerializeField] public Button confirmBtn;
        [SerializeField] public UIImageState btnImageState;
        [SerializeField] public UITextState btnTextState;
        [SerializeField] private GameObject bottomRefreshGo;
        [SerializeField] public Button refreshBtn;
        private UICardCell _chooseCardInfo; //将要赠送的卡片
        private Coroutine _coroutine;
        private List<(int, PlayerOpenInfo)> _friendInfoList = new List<(int, PlayerOpenInfo)>();
        private int _giftCardId;    //要赠送的卡片id
        private int _realFriendCount = 0;   //好友数量

        protected override void OnCreate()
        {
            transform.AddButton("Mask", Close);
            transform.AddButton("Content/Root/BtnClose", Close).FixPivot();
            confirmBtn.FixPivot().onClick.AddListener(_OnClickBtnConfirm);
            refreshBtn.FixPivot().onClick.AddListener(_OnClickBtnRefresh);
            _chooseCardInfo = AddModule(new UICardCell(transform.Find("Content/Root/Panel/BtnConfirm/UICardCell")));
            infoScrollRect.InitLayout();
            inputField.onValueChanged.AddListener(OnInputValueChange);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
                _giftCardId = (int)items[0];
        }

        protected override void OnPreOpen()
        {
            Game.Manager.cardMan.CurSelectFriendIndex = -1;
            _realFriendCount = 0;
            var needShow = Game.Manager.cardMan.CheckNeedShowRefreshBtn();
            bottomRefreshGo.gameObject.SetActive(needShow);
            if (needShow)
                LayoutRebuilder.ForceRebuildLayoutImmediate(infoRect);
            _StopCoroutine();
            _coroutine = StartCoroutine(_TryRefresh());
            if (_giftCardId > 0)
                _chooseCardInfo.Show(_giftCardId, true);
            _OnSelectFriend();
            placeHolder.text = I18N.Text("#SysComDesc655");
            inputField.text = "";
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.UI_CARD_GIFTING_SELECT>().AddListener(_OnSelectFriend);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.UI_CARD_GIFTING_SELECT>().RemoveListener(_OnSelectFriend);
        }

        protected override void OnPostClose()
        {
            _StopCoroutine();
            Game.Manager.cardMan.CurSelectFriendIndex = -1;
            _friendInfoList.Clear();
            _realFriendCount = 0;
        }

        private void OnInputValueChange(string value)
        {
            if (_realFriendCount < 1)
            {
                return;
            }
            _FillFriendInfo(value);
            infoText.text = _friendInfoList.Count < 1 ? I18N.Text("#SysComDesc657") : "";
            infoScrollRect.UpdateData(_friendInfoList);
        }

        private IEnumerator _TryRefresh()
        {
            waitingGo.SetActive(true);
            infoScrollRect.gameObject.SetActive(false);
            infoText.text = "";
            var cardMan = Game.Manager.cardMan;
            //调用manager起task执行数据刷新任务
            cardMan.TryRefreshUICardGifting();
            var task = cardMan.RefreshUICardGiftingTask;
            yield return new WaitUntil(() => !task.keepWaiting);
            //任务完成后清空任务
            cardMan.ClearRefreshUICardGiftingTask();
            //刷新界面
            waitingGo.SetActive(false);
            _realFriendCount = cardMan.GetFriendInfoList().Count;
            _FillFriendInfo();
            if (_realFriendCount < 1)
            {
                infoText.text = I18N.Text("#SysComDesc656");
            }
            else
            {
                infoScrollRect.gameObject.SetActive(true);
                infoScrollRect.UpdateData(_friendInfoList);
            }
        }
        
        private void _StopCoroutine()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        private void _OnSelectFriend()
        {
            if (_friendInfoList != null)
            {
                infoScrollRect.UpdateData(_friendInfoList);
            }
            var hasSelect = Game.Manager.cardMan.CurSelectFriendIndex >= 0;
            btnImageState.Enabled(hasSelect);
            btnTextState.Enabled(hasSelect);
        }
        
        private void _OnClickBtnConfirm()
        {
            var index = Game.Manager.cardMan.CurSelectFriendIndex;
            var selectFriend = Game.Manager.cardMan.TryGetFriendInfo(index);
            if (selectFriend != null && _giftCardId > 0)
            {
                StartCoroutine(_TryGiftCardToFriend(selectFriend));
            }
            else
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.NeedChooseFriend);
            }
        }

        private IEnumerator _TryGiftCardToFriend(PlayerOpenInfo selectFriend)
        {
            UIManager.Instance.Block(true);
            yield return Game.Manager.cardMan.TryGiftCardToFriend(selectFriend, _giftCardId);
            UIManager.Instance.Block(false);
            Close();
        }

        //填充指定玩家名字的好友信息 checkName默认传空代表显示所有
        private void _FillFriendInfo(string checkName = "")
        {
            var cardMan = Game.Manager.cardMan;
            var list = cardMan.GetFriendInfoList();
            _friendInfoList.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var info = list[i];
                var playerName = info.FacebookInfo?.Name ?? "";
                if (playerName.Contains(checkName))
                {
                    _friendInfoList.Add((i, info));
                }
            }
        }

        private void _OnClickBtnRefresh()
        {
            //如果正在等待过程中 则忽略这次点击
            if (waitingGo.activeSelf)
                return;
            Game.Manager.cardMan.ReLoginToRefreshFriend();
        }
    }
}