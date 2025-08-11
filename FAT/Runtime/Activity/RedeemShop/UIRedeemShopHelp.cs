/*
 * @Author: yanfuxing
 * @Date: 2025-05-08 11:25:05
 */
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    /// <summary>
    /// 兑换商店帮助
    /// </summary>
    public class UIRedeemShopHelp : UIBase
    {
        [SerializeField] private Button mask;

        [SerializeField]
        private UIVisualGroup _visualGroup;
        private ActivityRedeemShopLike _activityRedeemShopLike;


#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out _visualGroup);
            //_visualGroup = transform.GetComponent<UIVisualGroup>();
            transform.Access("Content", out Transform root);
            _visualGroup.Prepare(root.Access<TextProOnACircle>("page1/title1"), "mainTitle");
            _visualGroup.Prepare(root.Access<TMP_Text>("page1/text"), "desc1");
            _visualGroup.Prepare(root.Access<TMP_Text>("page1/text2"), "desc2");
            _visualGroup.Prepare(root.Access<TMP_Text>("page1/text3"), "desc3");
            _visualGroup.CollectTrim();
        }
#endif
        protected override void OnCreate()
        {
            mask.onClick.AddListener(OnClose);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                _activityRedeemShopLike = (ActivityRedeemShopLike)items[0];
            }
        }

        protected override void OnPreOpen()
        {
            transform.GetComponent<Animator>().SetTrigger("Show");
            if (_activityRedeemShopLike != null)
            {
                var requireScoreId = _activityRedeemShopLike.EventRedeemConfig.TokenId;
                var stmpStr = UIUtility.FormatTMPString(requireScoreId);
                var visual = _activityRedeemShopLike.VisualUIRedeemShopHelp.visual;
                visual.Refresh(_visualGroup);
                visual.RefreshText(_visualGroup, "mainTitle", stmpStr);
                visual.RefreshText(_visualGroup, "desc1", stmpStr);
                visual.RefreshText(_visualGroup, "desc2", stmpStr);
                visual.RefreshText(_visualGroup, "desc2", stmpStr);
            }
        }

        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }
    }
}

