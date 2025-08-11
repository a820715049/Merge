/*
 * @Author: qun.chao
 * @Date: 2023-11-01 12:23:43
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EL;
using TMPro;
using Text = TMPro.TMP_Text;
using InputField = TMPro.TMP_InputField;

namespace FAT
{
    public class UIDebugDrawCard : UIBase
    {
        [SerializeField] private GameObject goItem;
        [SerializeField] private GameObject goCompButton;
        [SerializeField] private GameObject goCompInput;
        [SerializeField] private Transform rootA;
        [SerializeField] private Transform rootB;
        [SerializeField] private Transform rootC;
        [SerializeField] private Transform rootD;

        private Transform mCurItemRoot;
        private Dictionary<Text, Action<Text>> mInfoUpdator = new Dictionary<Text, Action<Text>>();

        protected override void OnCreate()
        {
            transform.AddButton("Content/TR/BtnClose", base.Close);
            _Build();
        }

        protected override void OnPreOpen()
        {
            MessageCenter.Get<MSG.GAME_CARD_DRAW_FINISH>().AddListener(_OnDrawCardFinish);
            _OnDrawCardFinish();
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.GAME_CARD_DRAW_FINISH>().RemoveListener(_OnDrawCardFinish);
        }
        
        #region build

        private void _Build()
        {
            mCurItemRoot = rootA;
            var cardMan = Game.Manager.cardMan;
            if (cardMan.CurCardActId <= 0)
            {
                _RegisterButton($"Card Activity Not Open, IsUnlock : {cardMan.IsUnlock}");
                return;
            }

            var albumData = cardMan.GetCardAlbumData();

            _RegisterButton($"Activity Id : {cardMan.CurCardActId}");
            _RegisterButton($"Album Id : {cardMan.GetCardAlbumConfig()?.Id}");
            _RegisterButton($"Album TempId : {albumData?.CardLimitTempId}");
            _RegisterButton($"Cur Album Index : {cardMan.GetCardRoundData().GetCurOpenAlbumIndex()}");

            if (albumData == null)
                return;
            
            mCurItemRoot = rootB;
            _RegisterButtonWithInput("Set Total Cost Energy:", albumData.TotalCostEnergy.ToString(), (str) =>
            {
                bool isNumber = ulong.TryParse(str, out var energy);
                if (isNumber)
                {
                    albumData.TotalCostEnergy = energy;
                    _OnDrawCardFinish();
                }
            });
            
            int totalCostIap = Game.Manager.iap.TotalIAPServer - albumData.StartTotalIAP;
            _RegisterButtonWithInput("Set Total IAP(cent):", totalCostIap.ToString(), (str) =>
            {
                bool isNumber = int.TryParse(str, out var iap);
                if (isNumber)
                {
                    albumData.StartTotalIAP = Game.Manager.iap.TotalIAPServer - iap;
                    _OnDrawCardFinish();
                }
            });
            int openPackId = 0;
            _RegisterButtonWithInput("Click Sure", "27000001", (str) =>
            {
                bool isNumber = int.TryParse(str, out var id);
                if (isNumber)
                {
                    openPackId = id;
                }
            });
            
            _RegisterInfo("Album Progress: ", (text) =>
            {
                albumData.GetAllCollectProgress(out var ownCount, out var allCount);
                text.text = ownCount + "/" + allCount;
            });
            
            _RegisterButtonWithInput("Change Card Count", "22000001:1", (str) =>
            {
                var r = str.ConvertToRewardConfig();
                if (r != null)
                {
                    Game.Manager.cardMan.DebugAddCard(r.Id, r.Count);
                    _OnDrawCardFinish();
                }
            });
            
            _RegisterButton("ResetExchangeCd", () =>
            {
                Game.Manager.cardMan.DebugResetExchangeCd();
            });
            
            _RegisterButtonWithInput("Change Fixed Star Num", "1000", (str) =>
            {
                var isNumber = int.TryParse(str, out var num);
                if (isNumber)
                {
                    Game.Manager.cardMan.DebugChangeFixedStarNum(num);
                }
            });
            //重置每日送卡次数和刷新时间
            _RegisterButton("ResetGiveCardNum", () =>
            {
                Game.Manager.cardMan.DebugResetGiveCardNum();
            });
            //忽略检查fb绑定
            _RegisterDisplayButton("IgnoreFacebookBind : " + Game.Manager.cardMan.DebugIsIgnoreFbBind, _OnBtnSetIgnoreFacebookBind);
            //向服务器塞个人信息假数据
            _RegisterButtonWithInput("SetFacebookInfo", Game.Manager.networkMan.fpId + "," + Game.Manager.networkMan.fpId, (str) =>
            {
                Game.Manager.cardMan.DebugSetFacebookInfo(str);
            });
            //设置fb好友id信息
            _RegisterButtonWithInput("SetFacebookFriendIdInfo", "", (str) =>
            {
                Game.Manager.cardMan.DebugSetFacebookFriendIdInfo(str);
            });

            mCurItemRoot = rootC;
            
            _RegisterDisplayButton("Click Draw Card", () =>
            {
                var result = cardMan.OpenCardPack(openPackId);
                string resultStr = "";
                foreach (var cardId in result)
                {
                    var cardData = cardMan.GetCardData(cardId);
                    string idStr = $"{cardId - Constant.kCardIdBase}";
                    string star = cardData.GetConfig().Star + "X";
                    string starStr = cardData.GetConfig().IsGold ? $"<color=#FFB31F>{star}</color>" : $"<color=#A6A6A6>{star}</color>";
                    string newStr = cardData.OwnCount == 1 ? "New" : "Old";
                    string tempStr = $"{idStr}({starStr})({newStr})(Group {cardData.BelongGroupId})\n";
                    resultStr += tempStr;
                }
                return $"Draw Card Finish. Result :\n{resultStr}Click To Continue";
            });
            
            mCurItemRoot = rootD;

            foreach (var groupData in albumData.GetAllGroupDataMap().Values)
            {
                var groupId = groupData.CardGroupId;
                _RegisterInfo("", (text) =>
                {
                    albumData.GetCollectProgress(groupId, out var ownCount, out var allCount);
                    string progress = ownCount + "/" + allCount;
                    string collectStr = groupData.IsCollectAll ? $"<color=#5CC1EA>{progress}</color>" : $"<color=#797474>{progress}</color>";
                    var desc = $"Group {groupId}({collectStr})\n";
                    var cardInfoStr = "";
                    var groupConfig = groupData.GetConfig();
                    if (groupConfig != null && groupConfig.CardInfo.Count > 0)
                    {
                        var cardPackConfig = Game.Manager.objectMan.GetCardPackConfig(openPackId);
                        int goldPayPass = (cardPackConfig != null && !cardPackConfig.IsShinnyGuar) ? cardPackConfig.GoldPayPass[albumData.CardLimitTempId] : 0;
                        int totalIap = Game.Manager.iap.TotalIAPServer - albumData.StartTotalIAP;    //计算累充金额
                        int payRate = albumData.GetConfig().PayRate;//累计充值对ergPass的加成系数（百分数）
                        ulong totalCostEnergy = albumData.TotalCostEnergy;
                        foreach (var cardId in groupConfig.CardInfo)
                        {
                            if (albumData.GetAllCardDataMap().TryGetValue(cardId, out var cardData))
                            {
                                var config = cardData.GetConfig();
                                string idStr = $"{cardId - Constant.kCardIdBase}";
                                string starStr = config.IsGold ? $"<color=#FFB31F>{config.Star + "X"}</color>" : $"<color=#A6A6A6>{config.Star + "X"}</color>";
                                string countStr = cardData.OwnCount > 0 ? $"<color=#4E96E0>{cardData.OwnCount}</color>" : $"<color=#A6A6A6>{cardData.OwnCount}</color>";
                                bool canGoldPass = config.IsGoldable && cardData.IsLimitGoldAble;    //搭配使用才能决断出是否可以被加成
                                bool isPassEnergy = cardData.CheckIsPassEnergy(config.IsGold, canGoldPass, totalCostEnergy, totalIap, goldPayPass, payRate);
                                bool isPassPay = cardData.CheckIsPassPay(config.IsGold, canGoldPass, totalIap, goldPayPass);
                                bool isOwn = cardData.IsOwn;
                                string energyLimitStr = isPassEnergy && !isOwn ? $"<color=#73C155>{cardData.EnergyPassNum}</color>" : $"<color=#A6A6A6>{cardData.EnergyPassNum}</color>";
                                string passLimitStr = isPassPay && !isOwn ? $"<color=#73C155>{cardData.PayPassNum}</color>" : $"<color=#A6A6A6>{cardData.PayPassNum}</color>";
                                string tempIdStr = $"{cardData.LimitId},{energyLimitStr},{passLimitStr}";
                                string tempStr = $"{idStr}({starStr})({countStr})({tempIdStr})\n";
                                cardInfoStr += tempStr;
                            }
                        }
                    }
                    text.text = desc + cardInfoStr;
                });
            }
        }

        private Transform _AddItem(Transform root)
        {
            var go = Instantiate(goItem, root);
            go.SetActive(true);
            return go.transform;
        }

        private Transform _AddButton(Transform root)
        {
            var go = Instantiate(goCompButton, root);
            go.SetActive(true);
            return go.transform;
        }

        private Transform _AddInput(Transform root)
        {
            var go = Instantiate(goCompInput, root);
            go.SetActive(true);
            return go.transform;
        }

        private void _RegisterButton(string desc, Action act = null)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            btn.FindEx<Text>("Text").text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                act?.Invoke();
            };
            btn.AddButton(null, cb);
        }

        private void _RegisterDisplayButton(string desc, Func<string> f)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var text = btn.FindEx<Text>("Text");
            text.text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                text.text = f?.Invoke();
            };
            btn.AddButton(null, cb);
        }

        private void _RegisterButtonWithInput(string desc, string inputDesc, Action<string> act = null)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var input = _AddInput(item).GetComponent<InputField>();
            input.text = inputDesc;
            btn.FindEx<Text>("Text").text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                act?.Invoke(input.text);
            };
            btn.AddButton(null, cb);
        }
        
        private void _RegisterInfo(string desc, Action<Text> act)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var area = btn.FindEx<Text>("Text");
            Action<Text> imp = text =>
            {
                act.Invoke(text);
                text.text = $"{desc}{text.text}";
            };
            area.enableAutoSizing = true;
            area.enableWordWrapping = false;
            area.alignment = TextAlignmentOptions.MidlineLeft;
            imp.Invoke(area);
            mInfoUpdator.Add(area, imp);
        }
        
        private void _OnDrawCardFinish()
        {
            foreach (var entry in mInfoUpdator)
            {
                entry.Value(entry.Key);
            }
        }

        private string _OnBtnSetIgnoreFacebookBind()
        {
            Game.Manager.cardMan.DebugSetIgnoreFacebookBind();
            return "IgnoreFacebookBind : " + Game.Manager.cardMan.DebugIsIgnoreFbBind;
        }

        #endregion
    }
}