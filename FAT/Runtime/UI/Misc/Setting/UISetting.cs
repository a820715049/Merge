/*
 * @Author: tang.yan
 * @Description: 设置界面
 * @Date: 2023-12-01 17:12:03
 */
using FAT.Platform;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;
using System.Collections.Generic;

namespace FAT
{
    public class UISetting : UIBase
    {
        //角色信息
        [SerializeField] private TMP_Text useIdText;
        [SerializeField] private Button btnCopyId;
        //登录类型
        [SerializeField] private TMP_Text loginTypeText;
        //音乐音效
        [SerializeField] private Button btnMusic;
        [SerializeField] private UIImageState stateMusic;
        [SerializeField] private Button btnSound;
        [SerializeField] private UIImageState stateSound;
        //震动
        [SerializeField] private Button btnVibration;
        [SerializeField] private UIImageState stateVibration;
        //定时提示
        [SerializeField] private Button btnNotification;
        [SerializeField] private UIImageState stateNotification;
        //剧情开关
        [SerializeField] private Button btnPlot;
        [SerializeField] private UIImageState statePlot;
        //各个按钮
        [SerializeField] private Button btnContact;
        [SerializeField] private Button btnDelete;
        [SerializeField] private Button btnCommunity;
        [SerializeField] private Button btnAccount;
        [SerializeField] private Button btnChestInfo;
        [SerializeField] private GameObject LinkBtnItem;
        [SerializeField] private Transform LinkBtnRoot;
        //隐私协议
        [SerializeField] private Button btnService;
        [SerializeField] private Button btnPrivacy;
        //版本号
        [SerializeField] private TMP_Text versionText;
        private TextEditor _textEditor;
        private PoolItemType _mItemType = PoolItemType.SETTING_COMMUNITY_TYPE_ITEM;
        private List<GameObject> _cellList = new();

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/Root/BtnClose", base.Close).FixPivot();
            btnCopyId.WithClickScale().FixPivot().onClick.AddListener(_OnBtnCopyIdClick);

            btnMusic.FixPivot().onClick.AddListener(_OnBtnMusicClick);
            btnSound.FixPivot().onClick.AddListener(_OnBtnSoundClick);
            btnVibration.FixPivot().onClick.AddListener(_OnBtnVibrationClick);
            btnNotification.FixPivot().onClick.AddListener(_OnBtnNotificationClick);
            btnPlot.FixPivot().onClick.AddListener(_OnBtnPlotClick);

            btnContact.WithClickScale().FixPivot().onClick.AddListener(_OnBtnContactClick);
            btnDelete.WithClickScale().FixPivot().onClick.AddListener(_OnBtnDeleteClick);
            btnService.onClick.AddListener(_OnBtnServiceClick);
            btnPrivacy.onClick.AddListener(_OnBtnPrivacyClick);
            btnCommunity.WithClickScale().FixPivot().onClick.AddListener(_OnBtnCommunityClick);
            btnAccount.WithClickScale().FixPivot().onClick.AddListener(() => UIManager.Instance.OpenWindow(UIConfig.UIAccountBind));
            btnChestInfo.WithClickScale().FixPivot().onClick.AddListener(_OnBtnChestInfoClick);
            GameObjectPoolManager.Instance.PreparePool(_mItemType, LinkBtnItem);
        }

        protected override void OnPreOpen()
        {
            _Refresh();
            _InitAllLinkBtn();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.NOTIFICATION_STATE>().AddListener(NotificationStateChange);
            MessageCenter.Get<MSG.APP_ENTER_FOREGROUND_EVENT>().AddListener(RefreshCommunityLinkReward);
        }

        protected override void OnPostOpen()
        {

        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.NOTIFICATION_STATE>().RemoveListener(NotificationStateChange);
            MessageCenter.Get<MSG.APP_ENTER_FOREGROUND_EVENT>().RemoveListener(RefreshCommunityLinkReward);
        }

        protected override void OnPostClose()
        {
            _CloseLinkItem();
        }

        private void _Refresh()
        {
            _RefreshBaseInfo();
            _RefreshMusicState();
            _RefreshSoundState();
            _RefreshLoginType();
            _RefreshVisibility();
            _RefreshVibrationState();
            _RefreshNotificationState();
            _RefreshPlotState();
            if (btnChestInfo != null)
            {
                btnChestInfo.gameObject.SetActive(Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureDropProbability));
            }
        }

        private void _RefreshBaseInfo()
        {
            useIdText.text = I18N.FormatText("#SysComDesc129", PlatformSDK.Instance.GetUserId());
            versionText.text = I18N.FormatText("#SysComDesc140", Game.Instance.appSettings.version);
            var feature = Game.Manager.featureUnlockMan;
            //btnCommunity.gameObject.SetActive(feature.IsFeatureEntryUnlocked(FeatureEntry.FeatureSettingsCommunity));
            var bindAvailable = feature.IsFeatureEntryUnlocked(FeatureEntry.FeatureAccountBind)
                && AccountBindList.AnyAvailable();
            btnAccount.gameObject.SetActive(bindAvailable);
        }

        private void _RefreshMusicState()
        {
            stateMusic.Enabled(SettingManager.Instance.MusicIsOn);
        }

        private void _RefreshSoundState()
        {
            stateSound.Enabled(SettingManager.Instance.SoundIsOn);
        }

        private void _RefreshVibrationState()
        {
            stateVibration.Enabled(SettingManager.Instance.VibrationIsOn);
        }

        private void _RefreshNotificationState()
        {
            stateNotification.Enabled(Game.Manager.notification.Enabled);
        }

        private void NotificationStateChange(bool _)
        {
            _RefreshNotificationState();
            //手机给了权限后才会弹提示 否则会弹请求权限的界面
            if (Game.Manager.notification.PermissionGranted)
            {
                var toastText = Game.Manager.notification.Enabled ? I18N.Text("#SysComDesc582") : I18N.Text("#SysComDesc583");
                Game.Manager.commonTipsMan.ShowClientTips(toastText);
            }
        }

        private void _RefreshPlotState()
        {
            statePlot.Enabled(SettingManager.Instance.PlotIsOn);
        }

        //刷新当前账户类型
        private void _RefreshLoginType()
        {
            string type = "";
            if (_IsBind(AccountLoginType.Guest))
            {
                type = I18N.Text("#SysComDesc139");
            }
            else
            {
                type = I18N.Text("#SysComDesc139");
            }
            loginTypeText.text = I18N.FormatText("#SysComDesc138", type);
        }

        private bool _IsBind(AccountLoginType bt)
        {
            return PlatformSDK.Instance.Adapter.LoginType == bt;
        }

        private void _RefreshVisibility()
        {
            btnContact.gameObject.SetActive(true);
            btnDelete.gameObject.SetActive(true);
        }

        private void _OnBtnCopyIdClick()
        {
            _textEditor = _textEditor ?? new TextEditor();
            _textEditor.text = PlatformSDK.Instance.GetUserId();
            _textEditor.SelectAll();
            _textEditor.Copy();
            Game.Manager.commonTipsMan.ShowPopTips(Toast.CopySuccess);
        }

        private void _OnBtnMusicClick()
        {
            SettingManager.Instance.OnSwitchMusicState();
            _RefreshMusicState();
            var toastText = SettingManager.Instance.MusicIsOn ? I18N.Text("#SysComDesc576") : I18N.Text("#SysComDesc577");
            Game.Manager.commonTipsMan.ShowClientTips(toastText);
        }

        private void _OnBtnSoundClick()
        {
            SettingManager.Instance.OnSwitchSoundState();
            _RefreshSoundState();
            var toastText = SettingManager.Instance.SoundIsOn ? I18N.Text("#SysComDesc578") : I18N.Text("#SysComDesc579");
            Game.Manager.commonTipsMan.ShowClientTips(toastText);
        }

        private void _OnBtnVibrationClick()
        {
            SettingManager.Instance.OnSwitchVibrationState();
            _RefreshVibrationState();
            var toastText = SettingManager.Instance.VibrationIsOn ? I18N.Text("#SysComDesc580") : I18N.Text("#SysComDesc581");
            Game.Manager.commonTipsMan.ShowClientTips(toastText);
        }

        private void _OnBtnNotificationClick()
        {
            Game.Manager.notification.ToggleEnabled();
        }

        private void _OnBtnPlotClick()
        {
            SettingManager.Instance.OnSwitchPlotState();
            _RefreshPlotState();
            var toastText = SettingManager.Instance.PlotIsOn ? I18N.Text("#SysComDesc584") : I18N.Text("#SysComDesc585");
            Game.Manager.commonTipsMan.ShowClientTips(toastText);
        }

        private void _OnBtnContactClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UISupport);
        }

        private void _OnBtnDeleteClick()
        {
            Close();
            UIManager.Instance.OpenWindow(UIConfig.UIAuthenticationPolicy);
        }

        private void _OnBtnCommunityClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UISettingCommunity);
        }

        private void _OnBtnChestInfoClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIProbabilityTips, false);
        }

        private void _OnBtnServiceClick()
        {
            var gt = GameType.Asia;
            var cfg = _GetGameDiffConfig(gt) ?? _GetGameDiffConfig(GameType.Mainland);
            UIBridgeUtility.OpenURL(cfg.TermOfService);
        }

        private void _OnBtnPrivacyClick()
        {
            var gt = GameType.Asia;
            var cfg = _GetGameDiffConfig(gt) ?? _GetGameDiffConfig(GameType.Mainland);
            UIBridgeUtility.OpenURL(cfg.Privacy);
        }

        private GameDiff _GetGameDiffConfig(GameType gt)
        {
            return Game.Manager.configMan.GetGameDiffConfigs().FindEx(x => x.Type == gt);
        }

        private void _InitAllLinkBtn()
        {
            var settingList = Game.Manager.communityLinkMan.GetCommunityList();
            foreach (var settingData in settingList)
            {
                var cellItem = GameObjectPoolManager.Instance.CreateObject(_mItemType, LinkBtnRoot.transform);
                cellItem.SetActive(true);
                cellItem.gameObject.SetActive(settingData.IsOn);
                var btnItem = cellItem.GetComponent<UIBtnLinkItem>();
                btnItem.UpdateContent(settingData, LinkType.CommunityLink);
                _cellList.Add(cellItem);
            }
        }

        private void RefreshCommunityLinkReward()
        {
            if (!Game.Manager.communityLinkMan.IsShowRewardUI())
            {
                return;
            }
            CommunityLinkRewardData data = new CommunityLinkRewardData()
            {
                CommunityPopupType = CommunityPopupType.CommunityLinkReward,
                LinkId = Game.Manager.communityLinkMan.RecordClickLinkId
            };
            UIManager.Instance.OpenWindow(UIConfig.UICommunityPlanReward, data);
            Game.Manager.communityLinkMan.RecordClickLinkId = -1;
            _CloseLinkItem();
            _InitAllLinkBtn();
        }

        private void _CloseLinkItem()
        {
            foreach (var item in _cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(_mItemType, item);
            }
            _cellList.Clear();
        }
    }
}