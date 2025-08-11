/*
 * @Author: tang.yan
 * @Description: 卡片收取界面
 * @Date: 2024-10-24 19:10:57
 */
using EL;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FAT.Platform;
using fat.rawdata;

namespace FAT
{
    public class UICardPending : UIBase
    {
        [SerializeField] public Button sendBtn;
        [SerializeField] public UIImageState sendBtnImage;
        [SerializeField] public UITextState sendBtnText;
        [SerializeField] private UICardPendingScrollRect infoScrollRect;
        [SerializeField] private GameObject infoTextGo;
        [SerializeField] private GameObject waitingGo;
        [SerializeField] private GameObject facebookGo;
        [SerializeField] private Button facebookBtn;
        private Coroutine _coroutine;
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", Close);
            transform.AddButton("Content/Root/BtnClose", Close).FixPivot();
            sendBtn.FixPivot().onClick.AddListener(_OnClickBtnSend);
            facebookBtn.FixPivot().onClick.AddListener(_OnClickBtnFacebook);
            infoScrollRect.InitLayout();
        }

        protected override void OnParse(params object[] items) { }

        protected override void OnPreOpen()
        {
            _RefreshSendBtn();
            _RefreshInfo();
        }

        protected override void OnAddListener()
        {
            
        }

        protected override void OnRemoveListener()
        {
            
        }

        protected override void OnPostClose()
        {
            _StopCoroutine();
        }

        private void _RefreshSendBtn()
        {
            var cardMan = Game.Manager.cardMan;
            var isEnable = false;
            var hasSendNum = cardMan.CurGiveCardNum < Game.Manager.configMan.globalConfig.GiveCardNum;
            if (hasSendNum)
            {
                isEnable = cardMan.GetCardRoundData()?.CheckHasRepeatNormalCard() ?? false;
            }
            sendBtnImage.Enabled(isEnable);
            sendBtnText.Enabled(isEnable);
        }
        
        private void _OnClickBtnSend()
        {
            var cardMan = Game.Manager.cardMan;
            var hasSendNum = cardMan.CurGiveCardNum < Game.Manager.configMan.globalConfig.GiveCardNum;
            if (!hasSendNum)
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.GiveCardLimit);
                return;
            }
            var hasRepeat = cardMan.GetCardRoundData()?.CheckHasRepeatNormalCard() ?? false;
            if (!hasRepeat)
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.NoDuplicateCard);
                return;
            }
            //将CardAlbum界面跳转回主视图
            MessageCenter.Get<MSG.UI_JUMP_TO_ALBUM_MAIN_VIEW>().Dispatch();
            //弹提示
            Game.Manager.commonTipsMan.ShowPopTips(Toast.GiveCardChooseFriend);
            //关闭本界面
            Close();
        }

        private void _RefreshInfo()
        {
            if (!Game.Manager.cardMan.DebugIsIgnoreFbBind)
            {
                //检查是否绑定了fb
                var isBindFb = PlatformSDK.Instance.binding.CheckBind(AccountLoginType.Facebook, false);
                if (!isBindFb)
                {
                    facebookGo.SetActive(true);
                    infoScrollRect.gameObject.SetActive(false);
                    return;
                }
            }
            facebookGo.SetActive(false);
            //刷新收卡列表
            _StopCoroutine();
            _coroutine = StartCoroutine(_TryRefresh());
        }
        
        private IEnumerator _TryRefresh()
        {
            waitingGo.SetActive(true);
            infoScrollRect.gameObject.SetActive(false);
            infoTextGo.SetActive(false);
            var cardMan = Game.Manager.cardMan;
            yield return cardMan.CoPullPendingCardInfo();
            //刷新界面
            waitingGo.SetActive(false);
            var info = cardMan.GetPendingCardInfoList();
            if (info.Count < 1)
            {
                infoTextGo.SetActive(true);
            }
            else
            {
                infoScrollRect.gameObject.SetActive(true);
                infoScrollRect.UpdateData(info);
            }
        }

        private void _OnClickBtnFacebook()
        {
            PlatformSDK.Instance.binding.CheckBind(AccountLoginType.Facebook);
            Close();
        }
        
        private void _StopCoroutine()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }
    }
}