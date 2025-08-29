using EL;
using fat.msg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class MBRankingCell : FancyScrollRectCell<PlayerRankingInfo, MBRankingContext>
    {
        internal ulong Order;
        internal UIStateGroup Group;
        internal TextMeshProUGUI NameTxt; //名称
        internal TextMeshProUGUI RankTxt; //排名
        internal TextMeshProUGUI NumTxt; //积分
        internal MBRewardLayout Reward;
        internal UIImageRes ScoreImg;
        internal UIStateGroup BgGroup;

        private MBRewardLayout.RewardList _rewardList;

        /// <summary>
        /// 初始化组件
        /// </summary>
        public override void Initialize()
        {
            transform.Access("PlayerNode", out Group);
            transform.Access("PlayerNode/Name", out NameTxt);
            transform.Access("PlayerNode/RankNode/RankTxt", out RankTxt);
            transform.Access("PlayerNode/ScoreNode/NumBg/NumTxt", out NumTxt);
            transform.Access("RewardNode/_group", out Reward);
            transform.Access("PlayerNode/ScoreNode/ScoreBg/ScoreImg", out ScoreImg);
            transform.Access("PlayerNode/RankNode", out BgGroup);
        }

        /// <summary>
        /// 更新内容显示
        /// </summary>
        /// <param name="itemData"></param>
        public override void UpdateContent(PlayerRankingInfo itemData)
        {
            if (itemData == null) return;
            if (Context.MatchPlayer(Order)) Context.UpdateVisual(itemData.RankingOrder > Order ? 0 : 1);
            transform.localScale = itemData.Player == null ? Vector3.zero : Vector3.one;
            if (itemData.Player == null) return;
            Order = itemData.RankingOrder;
            Context.UpdateTargetTrans(itemData.RankingOrder, transform);
            NameTxt.text = Context.MatchPlayer(Order)
                ? I18N.Text("#SysComDesc459")
                : I18N.FormatText("#SysComDesc629", itemData.Player.Fpid);
            RankTxt.text = itemData.RankingOrder.ToString();
            NumTxt.text = itemData.Score.ToString();
            UpdateReward((int)itemData.RankingOrder);
            Group.Select(Context.MatchPlayer(Order) ? 1 : 0);
            BgGroup.Select(Order >= 4 ? 0 : (int)Order);
        }

        public void UpdateContent(MBRankingContext context, PlayerRankingInfo itemData)
        {
            if (itemData == null) return;
            Order = itemData.RankingOrder;
            NameTxt.text = I18N.Text("#SysComDesc459");
            RankTxt.text = itemData.RankingOrder.ToString();
            NumTxt.text = itemData.Score.ToString();
            UpdateReward((int)itemData.RankingOrder, context);
            Group.Select(1);
            BgGroup.Select(Order >= 4 ? 0 : (int)Order);
        }

        /// <summary>
        /// 更新奖励内容显示
        /// </summary>
        /// <param name="index"></param>
        private void UpdateReward(int index, MBRankingContext context)
        {
            if (index <= context.ActivityRanking.reward.Count && index > 0)
            {
                Reward.gameObject.SetActive(true);
                Reward.Refresh(context.ActivityRanking?.reward[index - 1]);
                Reward.RefreshFrame(context.MatchPlayer(Order) ? 0 : 1);
            }
            else
            {
                Reward.gameObject.SetActive(false);
            }
        }

        private void UpdateReward(int index)
        {
            if (index <= Context.ActivityRanking.reward.Count && index > 0)
            {
                Reward.gameObject.SetActive(true);
                Reward.Refresh(Context.ActivityRanking?.reward[index - 1]);
                Reward.RefreshFrame(Context.MatchPlayer(Order) ? 0 : 1);
            }
            else
            {
                Reward.gameObject.SetActive(false);
            }
        }

        protected override void UpdatePosition(float normalizedPosition, float localPosition)
        {
            base.UpdatePosition(normalizedPosition, localPosition);
            if (Context.MatchPlayer(Order)) Context.UpdateVisual(normalizedPosition);
        }
    }
}