/*
 * @Author: pengjian.zhang
 * @Description: 卡册集齐领奖界面
 * @Date: 2024-01-16 16:08:02
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT
{
    public class UICardAlbumReward : UIBase
    {
        private class UICardAlbumRewardCell
        {
            public GameObject CellGo;
            public UIImageRes Icon;
            public TMP_Text NumText;
            public Image TipsIcon;
            public Button Btn;
        }
        
        [SerializeField] private TextProOnACircle title;
        [SerializeField] private UIImageRes cardAlbumIcon;
        [SerializeField] private Button getRewardBtn;
        [SerializeField] private HorizontalLayoutGroup layoutGroup;

        
        private List<UICardAlbumRewardCell> _cellList = new List<UICardAlbumRewardCell>();
        private List<RewardCommitData> _rewardConfigList = new List<RewardCommitData>();
        private int _curCardAlbumId;    //当前卡册id
        private int _layoutGroupSapcing = 80;  //默认1个或两个奖励时 layoutGroup的spacing
        private int _layoutGroupSapcing3 = 70;  //有3个奖励时 layoutGroup的spacing
        private int _layoutGroupSapcing4 = 30;  //有4个奖励时 layoutGroup的spacing
        public int _tipOffset = 4;


        protected override void OnCreate()
        {
            getRewardBtn.WithClickScale().onClick.AddListener(OnGetRewardBtnClick);

            string path = "Content/rewardGroup/UICardRewardCell";
            for (int i = 0; i < 5; i++)
            {
                string tempPath = path + i;
                var cell = new UICardAlbumRewardCell();
                transform.FindEx(tempPath, out cell.CellGo);
                cell.Icon = transform.FindEx<UIImageRes>(tempPath + "/Content/Icon");
                cell.NumText = transform.FindEx<TMP_Text>(tempPath + "/Content/Num");
                cell.TipsIcon = transform.FindEx<Image>(tempPath + "/Content/TipsIcon");
                cell.Btn = transform.FindEx<Button>(tempPath + "/Content/Icon");
                int tempGroupIndex = i;
                cell.Btn.WithClickScale().onClick.AddListener(() => _OnTipsBtnClick(tempGroupIndex));
                _cellList.Add(cell);
            }
        }
        
        protected override void OnPreOpen()
        {
            _RefreshRewardPanel();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 1)
            {
                _curCardAlbumId = (int)items[0];
                _rewardConfigList.Clear();
                if (items[1] is List<RewardCommitData> rewards)
                {
                    _rewardConfigList.AddRange(rewards);
                }
            }
        }
        
        protected override void OnAddListener()
        {
        }

        protected override void OnRefresh()
        {
        }

        protected override void OnRemoveListener()
        {
        }

        protected override void OnPreClose()
        {
        }

        private void _RefreshRewardPanel()
        {
            //奖励数据
            var cardAlbumConfig = Game.Manager.configMan.GetEventCardAlbumConfig(_curCardAlbumId);
            title.SetText(I18N.Text(cardAlbumConfig.Name));
            cardAlbumIcon.SetImage(cardAlbumConfig.Icon.ConvertToAssetConfig());
            //刷新奖励
            int index = 0;
            int length = _rewardConfigList.Count;
            //效果图要求：不同个数预览时 奖励之间的间隙不同
            layoutGroup.spacing = _layoutGroupSapcing;
            if (length == 3)
            {
                layoutGroup.spacing = _layoutGroupSapcing3;
            }
            else if (length == 4)
            {
                layoutGroup.spacing = _layoutGroupSapcing4;
            }
            foreach (var cell in _cellList)
            {
                if (index < length)
                {
                    var data = _rewardConfigList[index];
                    var cfg = Game.Manager.objectMan.GetBasicConfig(data.rewardId);
                    if (cfg != null)
                    {
                        cell.CellGo.SetActive(true);
                        bool showTips = UIItemUtility.ItemTipsInfoValid(cfg.Id);
                        cell.TipsIcon.gameObject.SetActive(showTips);
                        cell.Icon.image.raycastTarget = showTips;
                        cell.Icon.SetImage(cfg.Icon.ConvertToAssetConfig());
                        cell.NumText.text = data.rewardCount.ToString();
                    }
                    else
                    {
                        cell.CellGo.SetActive(false);
                    }
                }
                else
                {
                    cell.CellGo.SetActive(false);
                }
                index++;
            }
        }
        
        /// <summary>
        /// 点击领奖按钮
        /// </summary>
        private void OnGetRewardBtnClick()
        {
            base.Close();
        }
        
        protected override void OnPostClose()
        {
            //领奖延迟到界面关闭时
            UIFlyUtility.FlyRewardList(_rewardConfigList, getRewardBtn.transform.position);
            //领奖后，检查是否有随机宝箱等特殊奖励，如果有则执行特殊奖励的相关表现，如果没有就通过事件来执行 TryOpenPackDisplay
            Game.Manager.specialRewardMan.CheckSpecialRewardFinish();
        }
        
        private void _OnTipsBtnClick(int groupIndex)
        {
            var reward = _rewardConfigList[groupIndex];
            if (!UIItemUtility.ItemTipsInfoValid(reward.rewardId))
            {
                return;
            }
            var icon = _cellList[groupIndex].Icon.image;
            var root = icon.rectTransform;
            UIItemUtility.ShowItemTipsInfo(reward.rewardId, root.position, _tipOffset + root.rect.size.y * 0.5f);
        }
    }
}