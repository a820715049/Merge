/*
 * @Author: pengjian.zhang
 * @Description: 积分活动帮助界面
 * @Date: 2024-2-28 15:17:26
 */

using System;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIScoreHelp : UIBase
    {
        public Button close;
        public GameObject group;
        public Transform helpRoot;
        public UIImageRes rewardIcon;
        public UIImageRes scoreIcon;
        public UIImageRes bg;
        public UIImageRes titleBg;
        public UIImageRes progressBg;
        public UIImageRes progressValueBg;
        // public UIImageRes progressFore;
        public TMP_Text tip;
        public TMP_Text tip2;
        public TMP_Text help1;
        public TMP_Text help2;
        public TMP_Text help3;
        public TMP_Text help4;
        public TMP_Text num;
        public TMP_Text rewardNum;
        public TMP_Text cd;
        public Button playBtn;
        public TextMeshProUGUI hint;
        public MBRewardProgress progress;
        public Image tipsIcon;
        public Button rewardIconBtn;
        public Button infoBtn;
        public TextProOnACircle title;
        public Animator progressAnim;

        private Action WhenCD;
        private ActivityScore activityScore;
        private int _tipOffset = 4;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            var root = transform.Find("Content");
            num = root.FindEx<TextMeshProUGUI>("progressScore/progress/text");
            rewardNum = root.FindEx<TextMeshProUGUI>("progressScore/rewardNum");
            rewardIcon = root.FindEx<UIImageRes>("progressScore/rewardIcon");
            bg = root.FindEx<UIImageRes>("bg");
            titleBg = root.FindEx<UIImageRes>("bg1");
            progressBg = root.FindEx<UIImageRes>("progressScore");
            progressValueBg = root.FindEx<UIImageRes>("progressScore/progress/back");
            // progressFore = root.FindEx<UIImageRes>("progressScore/progress/mask/fore");
            scoreIcon = root.FindEx<UIImageRes>("progressScore/scoreIcon");
            cd = root.FindEx<TextMeshProUGUI>("_cd/text");
            playBtn = root.FindEx<Button>("btnPlay");
            hint = root.FindEx<TextMeshProUGUI>("hint");
            progress = root.FindEx<MBRewardProgress>("progressScore/progress");
            tipsIcon = root.FindEx<Image>("progressScore/info");
            progressAnim = root.FindEx<Animator>("progressScore");
            rewardIconBtn = root.FindEx<Button>("progressScore/rewardIcon");
            infoBtn = root.FindEx<Button>("progressScore/info");
            title = root.FindEx<TextProOnACircle>("title");
            tip = root.FindEx<TMP_Text>("desc");
            tip2 = root.FindEx<TMP_Text>("helpBg/tip");
            help1 = root.FindEx<TMP_Text>("helpBg/help1");
            help2 = root.FindEx<TMP_Text>("helpBg/help2");
            help3 = root.FindEx<TMP_Text>("helpBg/help3");
            help4 = root.FindEx<TMP_Text>("helpBg/help4");
            helpRoot = root.Find("helpRoot");
        }
#endif

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/close", base.Close).FixPivot();
            transform.AddButton("Content/btnPlay", OnClickPlay).FixPivot();
            transform.AddButton("Content/helpRoot/Image", OnClickHelp).FixPivot();
            rewardIconBtn.onClick.AddListener(_OnTipsBtnClick);
            infoBtn.onClick.AddListener(_OnTipsBtnClick);
        }

        private void OnClickHelp()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIScoreGuide);
        }

        protected override void OnAddListener()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
        }

        protected override void OnPreOpen()
        {
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.Score, out var activity);
            if (activity == null)
            {
                return;
            }
            activityScore = (ActivityScore)activity;
            var valid = activityScore is { Valid: true } && activityScore.IsUnlock;
            if (!valid)
                return;
            RefreshTheme();
            progressAnim.SetTrigger("Punch");
            var isShowBtn = Game.Manager.mapSceneMan.scene.Active;
            playBtn.gameObject.SetActive(isShowBtn);
            hint.gameObject.SetActive(!isShowBtn);
            RefreshCD();
            num.text = string.Format("{0}/{1}", activityScore.CurShowScore, activityScore.CurMileStoneScore);
            scoreIcon.SetImage(Game.Manager.objectMan.GetBasicConfig(activityScore.ConfD.RequireCoinId).Icon);
            var reward = activityScore.GetCurMileStoneReward();
            var image = Game.Manager.rewardMan.GetRewardIcon(reward.Id, reward.Count);
            if (image != null)
            {
                rewardIcon.SetImage(image);
            }
            var cfg = Game.Manager.objectMan.GetBasicConfig(reward.Id);
            if (cfg != null)
            {
                bool showTips = UIItemUtility.ItemTipsInfoValid(cfg.Id);
                tipsIcon.gameObject.SetActive(showTips);
            }
            progress.Refresh(activityScore.CurShowScore, activityScore.CurMileStoneScore);
            rewardNum.SetText(reward.Count.ToString());
            helpRoot.gameObject.SetActive(activityScore.ConfD.IsHelp);
        }

        private void RefreshTheme()
        {
            //字体
            activityScore.Visual.Refresh(title, "mainTitle");
            activityScore.Visual.Refresh(tip, "tip1");
            //新增
            activityScore.Visual.Refresh(rewardNum, "rewardNum");
            activityScore.Visual.Refresh(num, "num");
            activityScore.Visual.Refresh(help1, "help");
            activityScore.Visual.Refresh(help2, "help");
            activityScore.Visual.Refresh(help3, "help");
            activityScore.Visual.Refresh(help4, "help");
            //图片
            activityScore.Visual.Refresh(bg, "bg");
            activityScore.Visual.Refresh(titleBg, "titleBg");
            activityScore.Visual.Refresh(progressBg, "progressBg");
            activityScore.Visual.Refresh(progressValueBg, "bar1");
            // activityScore.Visual.Refresh(progressFore, "bar2");

            activityScore.Visual.Theme.TextInfo.TryGetValue("tip1", out var t);
            activityScore.Visual.Theme.TextInfo.TryGetValue("tip3", out var t3);
            activityScore.Visual.Theme.AssetInfo.TryGetValue("tmpIcon2", out var icon);
            var sprite1 = "";
            if (icon != null)
                sprite1 = "<sprite name=\"" + icon.ConvertToAssetConfig().Asset + "\">";
            var scoreConfig = Game.Manager.objectMan.GetBasicConfig(activityScore.ConfD.RequireCoinId);
            var sprite2 = "<sprite name=\"" + scoreConfig.Icon.ConvertToAssetConfig().Asset + "\">";
            tip2.text = I18N.FormatText(t3, sprite2);
            tip.text = I18N.FormatText(t, sprite2, sprite1);

        }

        private void RefreshCD()
        {
            var v = activityScore.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
                Close();
        }

        private void OnClickPlay()
        {
            Close();
            GameProcedure.SceneToMerge();
        }

        private void _OnTipsBtnClick()
        {
            var reward = activityScore.GetCurMileStoneReward();
            if (!UIItemUtility.ItemTipsInfoValid(reward.Id))
            {
                return;
            }
            var icon = rewardIcon.image;
            var root = icon.rectTransform;
            UIItemUtility.ShowItemTipsInfo(reward.Id, root.position, _tipOffset + root.rect.size.y * 0.5f);
        }
    }
}
