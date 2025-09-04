/*
 * @Author: yanfuxing
 * @Date: 2025-07-22 15:40:09
 */
using System.Collections.Generic;
using Config;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIMultiplyRankingCell : FancyScrollRectCell<MultiplierRankingPlayerData, RankingContext>
    {
        [SerializeField] private UIStateGroup Group;
        [SerializeField] private TextMeshProUGUI NameTxt; //名称
        [SerializeField] private TextMeshProUGUI RankTxt; //排名
        [SerializeField] private TextMeshProUGUI NumTxt; //右侧积分
        [SerializeField] private MBRewardLayout Reward; //奖励
        [SerializeField] private UIImageRes ScoreImg;
        [SerializeField] private UIStateGroup BgGroup;
        private List<RewardConfig> _rewardList = new();
        private MultiplierRankingPlayerData _itemData;
        public override void UpdateContent(MultiplierRankingPlayerData itemData)
        {
            UpdatePanelInfo(itemData);
        }

        public void UpdatePanelInfo(MultiplierRankingPlayerData itemData)
        {
            if (itemData == null)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
            _itemData = itemData;
            Group.Select(itemData.isBot ? 0 : 1);
            BgGroup.Select(itemData.ranking >= 4 ? 0 : itemData.ranking);
            NameTxt.text = itemData.IsPlayer ? I18N.Text("#SysComDesc459") : I18N.FormatText("#SysComDesc1446", itemData.id);
            RankTxt.text = itemData.ranking.ToString();
            NumTxt.text = itemData.score.ToString();
            UpdateReward(itemData.rewardID);
            Reward.RefreshFrame(itemData.IsPlayer ? 1 : 0);
            if (Context != null)
            {
                var activity = Context.ActivityRanking;
                if (activity != null)
                {
                    var cfgData = Game.Manager.objectMan.GetBasicConfig(activity.conf.Token);
                    if (cfgData != null)
                    {
                        ScoreImg.SetImage(cfgData.Icon);
                    }
                }
            }
        }

        protected override void UpdatePosition(float normalizedPosition, float localPosition)
        {
            base.UpdatePosition(normalizedPosition, localPosition);
            if (_itemData == null) { return; }
            if (_itemData.IsPlayer)
            {
                //结算界面禁止检测滚动
                if (Context.RankingOpenType == RankingOpenType.Main)
                {
                    Context.UpdateVisual(normalizedPosition);
                }
            }
        }

        private void UpdateReward(int rewardID)
        {
            var rewardData = fat.conf.MultiRankRewardVisitor.Get(rewardID);
            if (rewardData == null)
            {
                Reward.gameObject.SetActive(false);
                return;
            }
            Reward.gameObject.SetActive(rewardData.RankReward.Count > 0);
            _rewardList.Clear();
            foreach (var reward in rewardData.RankReward)
            {
                _rewardList.Add(reward.ConvertToRewardConfig());
            }
            Reward.Refresh(_rewardList);
        }
    }
}