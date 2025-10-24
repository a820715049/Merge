/*
 * @Author: qun.chao
 * @Date: 2023-10-23 15:49:34
 */
using EL;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class UISceneHud : UIBase
    {
        private GameObject _btnBagGo;   //背包入口
        //图鉴相关
        private GameObject _btnBookGo;  //图鉴入口
        private GameObject _bookDotGo;  //图鉴入口红点
        private GameObject _bookHandGo;  //图鉴入口引导手指
        //卡册相关
        private GameObject _btnCardGo;  //卡册入口
        private GameObject _cardDotGo;  //卡册入口红点
        private GameObject _cardLockGo;  //卡册入口锁
        private TMP_Text _cardTime;     //卡册入口活动倒计时
        private UIImageState _cardIcon;     //卡册icon
        private UIImageState _cardTimeBg;   //卡册时间倒计时背景
        private Animator _animator;
        //左下角Left节点
        private RectTransform _rootBLLeft;
        //小游戏相关
        private GameObject _btnMiniGameGo;  //小游戏入口
        private GameObject _miniGameDotGo;  //小游戏入口红点
        private GameObject _settingDotGo;  //设置入口红点

        private int _useHighBLLeftState = 0; //1为普通状态 高度值192; 2为拉高状态 高度值为324

        protected override void OnCreate()
        {
            _animator = transform.FindEx<Animator>("Content");
            transform.AddButton("Content/UL/BtnSetting", _OnClickBtnSetting).FixPivot();
            transform.AddButton("Content/UL/BtnMail", _OnClickBtnMail).FixPivot();
            transform.AddButton("Content/BR/BtnMerge", _OnClickBtnMerge).FixPivot();
            //左下角Bottom位置
            var rootBL = transform.Find("Content/BL/Bottom");
            rootBL.AddButton("BtnBag", _OnClickBtnBag).FixPivot();
            rootBL.AddButton("BtnBook", _OnClickBtnGallery).FixPivot();
            rootBL.AddButton("BtnCard", _OnClickBtnCard).FixPivot();
            rootBL.FindEx("BtnBag", out _btnBagGo);
            rootBL.FindEx("BtnBook", out _btnBookGo);
            rootBL.FindEx("BtnBook/dot", out _bookDotGo);
            rootBL.FindEx("BtnBook/Hand", out _bookHandGo);
            rootBL.FindEx("BtnCard", out _btnCardGo);
            rootBL.FindEx("BtnCard/dot", out _cardDotGo);
            rootBL.FindEx("BtnCard/Lock", out _cardLockGo);
            _cardTime = rootBL.FindEx<TMP_Text>("BtnCard/time/text");
            _cardIcon = rootBL.FindEx<UIImageState>("BtnCard");
            _cardTimeBg = rootBL.FindEx<UIImageState>("BtnCard/time");
            //左下角Left位置
            _rootBLLeft = transform.FindEx<RectTransform>("Content/BL/Left");
            _rootBLLeft.AddButton("BtnMiniGame", _OnClickBtnMiniGame).FixPivot();
            _rootBLLeft.FindEx("BtnMiniGame", out _btnMiniGameGo);
            _rootBLLeft.FindEx("BtnMiniGame/dot", out _miniGameDotGo);
            transform.FindEx("Content/UL/BtnSetting/dot", out _settingDotGo);
        }

        protected override void OnParse(params object[] items)
        {
            
        }

        protected override void OnPreOpen()
        {
            //界面打开时根据IsHideMainUI状态播不同动画
            _animator.SetTrigger(UIManager.Instance.IsHideMainUI ? UIManager.IdleHideAnimTrigger : UIManager.IdleShowAnimTrigger);
            _RefreshEntry();
            _RefreshDot();
            _RefreshCardTime();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_MAIN_UI_STATE_CHANGE>().AddListener(_OnShowStateChange);
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().AddListener(_RefreshEntry);
            MessageCenter.Get<MSG.GAME_HANDBOOK_UNLOCK_ITEM>().AddListener(_RefreshHandbookDot);
            MessageCenter.Get<MSG.GAME_HANDBOOK_REWARD>().AddListener(OnHandBookReward);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().AddListener(_OnMessageLevelChange);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnOneSecondDriver);
            MessageCenter.Get<MSG.COMMUNITY_LINK_REFRESH_RED_DOT>().AddListener(_RefreshSettingDot);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_MAIN_UI_STATE_CHANGE>().RemoveListener(_OnShowStateChange);
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().RemoveListener(_RefreshEntry);
            MessageCenter.Get<MSG.GAME_HANDBOOK_UNLOCK_ITEM>().RemoveListener(_RefreshHandbookDot);
            MessageCenter.Get<MSG.GAME_HANDBOOK_REWARD>().RemoveListener(OnHandBookReward);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().RemoveListener(_OnMessageLevelChange);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnOneSecondDriver);
            MessageCenter.Get<MSG.COMMUNITY_LINK_REFRESH_RED_DOT>().RemoveListener(_RefreshSettingDot);
        }

        protected override void OnPostClose()
        {
            _useHighBLLeftState = 0;
        }

        private void _OnShowStateChange(bool isShow)
        {
            //Reset Trigger
            _animator.ResetTrigger(UIManager.IdleShowAnimTrigger);
            _animator.ResetTrigger(UIManager.IdleHideAnimTrigger);
            _animator.ResetTrigger(UIManager.OpenAnimTrigger);
            _animator.ResetTrigger(UIManager.CloseAnimTrigger);
            if (isShow)
            {
                //播放显示动画
                _animator.SetTrigger(UIManager.OpenAnimTrigger);
            }
            else
            {
                //播放隐藏动画
                _animator.SetTrigger(UIManager.CloseAnimTrigger);
            }
        }
        
        private void _RefreshEntry()
        {
            _btnBagGo.SetActive(Game.Manager.bagMan.CheckBagUnlock(BagMan.BagType.Tool));
            _btnBookGo.SetActive(Game.Manager.handbookMan.IsHandbookOpen());
            _btnMiniGameGo.SetActive(Game.Manager.miniGameDataMan.IsMiniGameUnlock());
            _RefreshCardEntry();
        }

        private void _OnOneSecondDriver()
        {
            _RefreshCardEntry();
            _RefreshCardDot();
            _RefreshCardTime();
            _RefreshMiniGameDot();
        }
        
        private void _RefreshDot()
        {
            _RefreshHandbookDot();
            _RefreshCardDot();
            _RefreshMiniGameDot();
            _RefreshSettingDot();
        }

        private void _OnMessageLevelChange(int lvl)
        {
            //刷新图鉴红点和奖励动画
            _RefreshHandbookDot();
            //刷新小游戏入口红点
            _RefreshMiniGameDot();
            _RefreshSettingDot();
        }

        private void _RefreshHandbookDot()
        {
            bool rp = Game.Manager.handbookMan.HandbookHasRP;
            _bookDotGo.SetActive(rp);
            bool isLevel = Game.Manager.mergeLevelMan.level < Game.Manager.configMan.globalConfig.GalleryFingerLv;
            var isShow = rp && isLevel;
            _bookHandGo.SetActive(isShow);
            //只有当背包入口没有显示出来且图鉴入口显示了且图鉴入口上方有奖励动画时才提高root位置
            var isHigh = isShow 
                         && !Game.Manager.bagMan.CheckBagUnlock(BagMan.BagType.Tool) 
                         && Game.Manager.handbookMan.IsHandbookOpen();
            //刷新左下角root起始位置
            _RefreshBLLeft(isHigh);
        }

        private void _RefreshBLLeft(bool isHigh)
        {
            var state = isHigh ? 2 : 1;
            if (_useHighBLLeftState != state)
            {
                _useHighBLLeftState = state;
                var posY = state == 2 ? 324 : 192;
                var originPos = _rootBLLeft.localPosition;
                _rootBLLeft.localPosition = new Vector3(originPos.x, posY, originPos.z);
            }
        }

        private void OnHandBookReward(int itemId)
        {
            _RefreshHandbookDot();
        }

        private void _RefreshCardDot()
        {
            _cardDotGo.SetActive(Game.Manager.cardMan.CanShowEntryRP);
        }

        private void _RefreshCardEntry()
        {
            var cardMan = Game.Manager.cardMan;
            //刷新是否显示入口
            _btnCardGo.SetActive(cardMan.CheckCardEntryShow());
            //刷新是否显示锁头
            _cardLockGo.SetActive(!cardMan.CheckValid());
            //刷新是否显示时间
            _cardTimeBg.gameObject.SetActive(cardMan.CheckValid());
            //刷新入口样式
            _RefreshCardEntryStyle();
        }

        //刷新卡册入口样式
        private void _RefreshCardEntryStyle()
        {
            if (!Game.Manager.cardMan.CheckCardEntryShow())
                return;
            var curJokerData = Game.Manager.cardMan.GetCardRoundData()?.GetFirstJokerData();
            if (curJokerData == null)
            {
                _cardIcon.Select(0);
                _cardTimeBg.Select(0);
                var config = FontMaterialRes.Instance.GetFontMatResConf(3);
                if (config != null)
                {
                    _cardTime.color = config.color;
                }
            }
            else
            {
                _cardIcon.Select(curJokerData.IsGoldCard == 0 ? 1 : 2);
                _cardTimeBg.Select(1);
                var config = FontMaterialRes.Instance.GetFontMatResConf(27);
                if (config != null)
                {
                    _cardTime.color = config.color;
                }
            }
        }

        private void _RefreshCardTime()
        {
            var cardAct = Game.Manager.cardMan.GetCardActivity();
            if (cardAct == null) return;
            var curJokerData = Game.Manager.cardMan.GetCardRoundData()?.GetFirstJokerData();
            if (curJokerData == null)
            {
                UIUtility.CountDownFormat(_cardTime, cardAct.Countdown);
            }
            else
            {
                var t = Game.Instance.GetTimestampSeconds();
                var diff = (long)Mathf.Max(0, curJokerData.ExpireTs - t);
                UIUtility.CountDownFormat(_cardTime, diff);
            }
        }

        private void _OnClickBtnSetting()
        {
            UIManager.Instance.OpenWindow(UIConfig.UISetting);
        }

        private void _OnClickBtnMail()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIMailBox);
        }

        private void _OnClickBtnBag()
        {
            Game.Manager.bagMan.TryOpenUIBag();
        }

        private void _OnClickBtnGallery()
        {
            Game.Manager.handbookMan.OpenUIHandbook();
        }

        private void _OnClickBtnMerge()
        {
            GameProcedure.SceneToMerge();
            // 切换到棋盘时尝试发起推送开启提醒
            Game.Manager.notification.TryRemindSwitchScene();
        }

        private void _OnClickBtnCard()
        {
            var cardMan = Game.Manager.cardMan;
            //卡册功能开启且在活动内时 打开卡册界面
            if (cardMan.CheckValid())
            {
                cardMan.OpenCardAlbumUI();
            }
            //卡册功能未开启但可见且在活动内时 打开卡册活动tips界面
            else
            {
                TryOpenCardActivityTips();
            }
        }

        public bool TryOpenCardActivityTips()
        {
            //卡册功能未开启但可见且在活动内时 打开卡册活动tips界面
            var cardMan = Game.Manager.cardMan;
            if (cardMan.CheckCardEntryShow() && !cardMan.CheckValid())
            {
                var root = _cardIcon.image.rectTransform;
                UIManager.Instance.OpenWindow(UIConfig.UICardActivityTips, root.position, 4f + root.rect.size.y * 0.5f);
                return true;
            }
            return false;
        }
        
        private void _OnClickBtnMiniGame()
        {
            // UIManager.Instance.OpenWindow(UIConfig.UIBeadsSelect);
            // 入口替换为新的小游戏
            UIManager.Instance.OpenWindow(UIConfig.UISlideMergeSelect);
        }
        
        private void _RefreshMiniGameDot()
        {
            _miniGameDotGo.SetActive(Game.Manager.miniGameDataMan.CheckHasRP());
        }

        private void _RefreshSettingDot()
        {
            _settingDotGo.SetActive(Game.Manager.communityLinkMan.IsShowRedDot());
        }
    }
}