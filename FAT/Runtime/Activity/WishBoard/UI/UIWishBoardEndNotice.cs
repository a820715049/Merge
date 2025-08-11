/*
 * @Author: yanfuxing
 * @Date: 2025-06-13 14:32:09
 */
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIWishBoardEndNotice : UIBase
    {
        [SerializeField] private Button _closeBtn;
        [SerializeField] private Button _confirmBtn;
        [SerializeField] private TextProOnACircle title;
        [SerializeField] private TextMeshProUGUI desc;
        [SerializeField] private UIImageRes milestoneHeadImage;
        [SerializeField] private TextMeshProUGUI levelTex;
        [SerializeField] private Transform levelFx;
        [SerializeField] private Transform textureFx;
        private EventWishMilestone _milestoneData;
        private WishBoardActivity wishBoardActivity;

        protected override void OnCreate()
        {
            base.OnCreate();
            _closeBtn.onClick.AddListener(OnClickClose);
            _confirmBtn.onClick.AddListener(OnClickClose);
        }
        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            wishBoardActivity = (WishBoardActivity)items[0];
            _milestoneData = items[1] as EventWishMilestone;
        }
        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            wishBoardActivity.VisualEndNoticePopup.visual.Refresh(title, "mainTitle");
            wishBoardActivity.VisualEndNoticePopup.visual.Refresh(desc, "desc1");
            levelTex.text = I18N.FormatText("#SysComDesc18", _milestoneData.Id);
            levelFx.gameObject.SetActive(!string.IsNullOrEmpty(_milestoneData.Image));
            textureFx.gameObject.SetActive(!string.IsNullOrEmpty(_milestoneData.Image));
            milestoneHeadImage.SetImage(_milestoneData.Image);
        }

        private void OnClickClose()
        {
            Close();
        }
    }
}