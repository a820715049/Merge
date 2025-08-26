/**
 * @Author: zhangpengjian
 * @Date: 2024/11/25 11:41:16
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/11/25 11:41:16
 * Description: 闪卡必得礼包 https://centurygames.yuque.com/ywqzgn/ne0fhm/ul9odvg3tkgmb9yc#HgeeT
 */

using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    public class PackShinnyGuar : GiftPack
    {
        public ShinnyGuarPack confD;
        public override int ThemeId => confD.EventTheme;
        public override int StockTotal => confD.Paytimes;
        public override int LabelId => confD.Label;
        public override UIResAlt Res { get; } = new(UIConfig.UIPackShinnyGuar);
        public override int PackId { get; set; }
        private List<int> cardInfos = new();

        public static (bool, string) ReadyToCreate(int id_)
        {
            var confD = GetShinnyGuarPack(id_);
            var r = confD != null && Activity.LevelValid(confD.ActiveLevel, confD.ShutdownLevel);
            return (r, r ? "not ready by config" : null);
        }
        
        public PackShinnyGuar() { }

        
        public PackShinnyGuar(ActivityLite lite_)
        {
            Lite = lite_;
            confD = GetShinnyGuarPack(lite_.Param);
            RefreshTheme();
        }

        public override void SetupFresh()
        {
            PackId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.PackGrpId);
            RefreshContent();
            Track(true);
        }

        public void Track(bool isShow = false)
        {
            cardInfos.Clear();
            Game.Manager.cardMan.FillShowCardIdList(confD.CardDisplayNum, cardInfos);
            if (cardInfos.Count > 0)
            {
                if (cardInfos.Count == 2)
                {
                    var card1 = cardInfos[0];
                    var card2 = cardInfos[1];
                    var group1 = Game.Manager.cardMan.GetCardData(card1).BelongGroupId;
                    var group2 = Game.Manager.cardMan.GetCardData(card2).BelongGroupId;
                    var isNew1 = Game.Manager.cardMan.GetCardData(card1).IsOwn ? 0 : 1;
                    var isNew2 = Game.Manager.cardMan.GetCardData(card2).IsOwn ? 0 : 1;
                    var group = $"{group1},{group2},0";
                    var card = $"{card1},{card2},0";
                    var isNew = $"{isNew1},{isNew2},0";
                    if (isShow)
                        DataTracker.shinnyguarpack_show.Track(this, group, card, isNew);
                    else
                        DataTracker.shinnyguarpack_reward.Track(this, group, card, isNew);
                }
                else if (cardInfos.Count == 3)
                {
                    var card1 = cardInfos[0];
                    var card2 = cardInfos[1];
                    var card3 = cardInfos[2];
                    var group1 = Game.Manager.cardMan.GetCardData(card1).BelongGroupId;
                    var group2 = Game.Manager.cardMan.GetCardData(card2).BelongGroupId;
                    var group3 = Game.Manager.cardMan.GetCardData(card3).BelongGroupId;
                    var isNew1 = Game.Manager.cardMan.GetCardData(card1).IsOwn ? 0 : 1;
                    var isNew2 = Game.Manager.cardMan.GetCardData(card2).IsOwn ? 0 : 1;
                    var isNew3 = Game.Manager.cardMan.GetCardData(card3).IsOwn ? 0 : 1;
                    var group = $"{group1},{group2},{group3}";
                    var card = $"{card1},{card2},{card3}";
                    var isNew = $"{isNew1},{isNew2},{isNew3}";
                    if (isShow)
                        DataTracker.shinnyguarpack_show.Track(this, group, card, isNew);
                    else
                        DataTracker.shinnyguarpack_reward.Track(this, group, card, isNew);
                }
                else if (cardInfos.Count == 1)
                {
                    var card1 = cardInfos[0];
                    var group1 = Game.Manager.cardMan.GetCardData(card1).BelongGroupId;
                    var isNew1 = Game.Manager.cardMan.GetCardData(card1).IsOwn ? 0 : 1;
                    var group = $"{group1}";
                    var card = $"{card1}";
                    var isNew = $"{isNew1}";
                    if (isShow)
                        DataTracker.shinnyguarpack_show.Track(this, group, card, isNew);
                    else
                        DataTracker.shinnyguarpack_reward.Track(this, group, card, isNew);
                }
            }
        }
    }
}