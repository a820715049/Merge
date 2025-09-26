/*
 * @Author: pengjian.zhang
 * @Description: 卡组集齐领奖界面
 * @Date: 2024-01-16 14:17:02
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT
{
    public class UICardGroupReward : UIBase
    {
        private class UICardGroupRewardCell
        {
            public GameObject CellGo;
            public UIImageRes Icon;
            public TMP_Text NumText;
            public Image TipsIcon;
            public Button Btn;
        }
        
        [SerializeField] private UIImageRes cardGroupIcon;
        [SerializeField] private TMP_Text cardGroupName;
        [SerializeField] private Button getRewardBtn;
        [SerializeField] private HorizontalLayoutGroup layoutGroup;

        private List<UICardGroupRewardCell> _cellList = new List<UICardGroupRewardCell>();
        private List<RewardCommitData> _rewardConfigList = new List<RewardCommitData>();
        private int _curCardGroupId;    //当前卡组id
        private int _layoutGroupSapcing = 80;  //默认1个或两个奖励时 layoutGroup的spacing
        private int _layoutGroupSapcing3 = 70;  //有3个奖励时 layoutGroup的spacing
        private int _layoutGroupSapcing4 = 30;  //有4个奖励时 layoutGroup的spacing
        private int _tipOffset = 4;
        private int _curCardPackId = 0;    //当前奖励的来源卡包id

        protected override void OnCreate()
        {
            getRewardBtn.WithClickScale().onClick.AddListener(OnGetRewardBtnClick);

            string path = "Content/rewardGroup/UICardRewardCell";
            for (int i = 0; i < 5; i++)
            {
                string tempPath = path + i;
                var cell = new UICardGroupRewardCell();
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
                _curCardGroupId = (int)items[0];
                _rewardConfigList.Clear();
                if (items[1] is List<RewardCommitData> rewards)
                {
                    _rewardConfigList.AddRange(rewards);
                }
                _curCardPackId = (int)items[2];
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
            var cardGroupConfig = Game.Manager.configMan.GetCardGroupConfig(_curCardGroupId);
            cardGroupIcon.SetImage(cardGroupConfig.Icon.ConvertToAssetConfig());
            cardGroupName.text = I18N.Text(cardGroupConfig.Name);
            
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
            
            //刷新奖励
            int index = 0;
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
            //根据卡包是否自动开启决定执行不同的方法
            var isAutoOpen = Game.Manager.cardMan.CheckIsAutoOpen(_curCardPackId);
            if (isAutoOpen)
            {
                //尝试执行之后的抽卡流程表现
                Game.Manager.cardMan.TryOpenPackDisplay();
            }
            else
            {
                //领奖后，检查是否有随机宝箱等特殊奖励，如果有则执行特殊奖励的相关表现，如果没有就通过事件来执行 TryOpenPackDisplay
                Game.Manager.specialRewardMan.CheckSpecialRewardFinish();
            }
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