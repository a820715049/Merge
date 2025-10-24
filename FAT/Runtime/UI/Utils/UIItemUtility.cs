/*
 * @Author: qun.chao
 * @Date: 2022-10-24 15:31:46
 */
using UnityEngine;
using UnityEngine.UI;
using Config;
using fat.rawdata;
using EL;

namespace FAT
{
    public class UIItemUtility
    {
        //显示物品大面板形式的信息(如来源与产出界面)
        public static void ShowItemPanelInfo(int itemId)
        {
            DebugEx.FormatInfo("ShowItemPanelInfo :: ItemId = ----> {0}", itemId);
            var objectMan = Game.Manager.objectMan;
            if (objectMan.IsType(itemId, ObjConfigType.MergeItem))
            {
                Game.Manager.itemInfoMan.TryOpenItemInfo(itemId);
            }
        }

        public static bool ItemTipsInfoValid(int id_, int exclude_ = 0)
        {
            var objMan = Game.Manager.objectMan;
            var mask = ~exclude_ & (int)(ObjConfigType.CardPack
                        | ObjConfigType.RandomBox | ObjConfigType.CardJoker);
            var inWhiteList = objMan.IsOneOfType(id_, mask);
            // 随机宝箱新增开关：当不显示结果时，不弹Tips
            // 这里是因为LandMark活动用随机宝箱配了个Token，不要展示Info按钮
            if (inWhiteList && objMan.IsType(id_, ObjConfigType.RandomBox))
            {
                var chestConf = objMan.GetRandomBoxConfig(id_);
                if (chestConf != null && !chestConf.IsShowResult)
                {
                    return false;
                }
            }
            return inWhiteList || MergeItemTipsInfoValid(id_);
        }

        public static bool MergeItemTipsInfoValid(int id_)
        {
            if (!Game.Manager.objectMan.IsType(id_, ObjConfigType.MergeItem)) return false;
            var confC = Merge.Env.Instance.GetItemComConfig(id_);
            return confC.jumpCDConfig != null || confC.specialBoxConfig != null || confC.choiceBoxConfig != null 
                   || confC.tokenMultiConfig != null;
        }

        //显示物品气泡tips形式的信息 需要指定气泡显示的起始位置和偏移  气泡的箭头需要指向道具Icon中心
        //也可显示大面板形式的信息面板
        public static void ShowItemTipsInfo(int itemId, Vector3 startWorldPos = new Vector3(), float offset = 0)
        {
            DebugEx.FormatInfo("ShowItemTipsInfo :: ItemId = ----> {0}", itemId);
            var objectMan = Game.Manager.objectMan;
            if (objectMan.IsType(itemId, ObjConfigType.MergeItem))
            {
                if (Merge.Env.Instance.GetItemComConfig(itemId).jumpCDConfig != null)
                {
                    UIManager.Instance.OpenWindow(UIConfig.UITimeBoosterDetails, startWorldPos, offset, Merge.Env.Instance.GetItemComConfig(itemId).jumpCDConfig.Time);
                }
                else if (Merge.Env.Instance.GetItemComConfig(itemId).specialBoxConfig != null)
                {
                    UIManager.Instance.OpenWindow(UIConfig.UISpecialBoxInfo, itemId);
                }
                else if (Merge.Env.Instance.GetItemComConfig(itemId).tokenMultiConfig != null)
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIMicInfo, startWorldPos, offset, itemId);
                }
                else
                {
                    Game.Manager.itemInfoMan.TryOpenItemInfo(itemId);
                }
                return;
            }
            if (objectMan.IsType(itemId, ObjConfigType.RandomBox))
            {
                Game.Manager.randomBoxMan.TryOpenRandomBoxTips(itemId, startWorldPos, offset);
                return;
            }
            if (objectMan.IsType(itemId, ObjConfigType.CardPack))
            {
                var isShinnyPack = Game.Manager.objectMan.GetCardPackConfig(itemId)?.IsShinnyGuar ?? false;
                var uiRes = !isShinnyPack ? UIConfig.UICardPackPreview : UIConfig.UIShinnyGuarPreview;
                UIManager.Instance.OpenWindow(uiRes, startWorldPos, offset, itemId);
                return;
            }

            if (objectMan.IsType(itemId, ObjConfigType.CardJoker))
            {
                UIManager.Instance.OpenWindow(UIConfig.UIJokerCardTips, startWorldPos, offset, itemId);
                return;
            }
            DebugEx.Warning($"ShowItemTipsInfo no matching tip for {itemId}");
        }

        public static void HideTips()
        {
            var ui = UIManager.Instance;
            ui.CloseWindow(UIConfig.UITimeBoosterDetails);
            ui.CloseWindow(UIConfig.UIRandomBoxTips);
            ui.CloseWindow(UIConfig.UICardPackPreview);
            ui.CloseWindow(UIConfig.UIJokerCardTips);
        }

        public static bool ItemTipsInfoAuto(int id_, int type_)
        {
            var g = Game.Manager.configMan.globalConfig;
            if (g.TapSourceTips.ContainsKey(id_)) return true;
            return type_ > 0 && Game.Manager.objectMan.IsType(id_, type_);
        }

        public static void ShowItemTipsInfoAuto(int itemId, Vector3 startWorldPos = default, float offset = 0)
        {
            var g = Game.Manager.configMan.globalConfig;
            if (g.TapSourceTips.ContainsKey(itemId))
            {
                var conf = fat.conf.Data.GetObjBasic(itemId);
                UIManager.Instance.OpenWindow(UIConfig.UIEnergyBoxTips, conf, startWorldPos, offset);
                return;
            }
            ShowItemTipsInfo(itemId, startWorldPos, offset);
        }

        #region count string style

        public enum CountStringStyle
        {
            NoPrefix,
            Custom,
            StartsWithPlus,     // +
            StartsWithX,        // x
            SmartPlus,          // 大于1时显示+
        }

        private static CountStringStyle countStrStyle = CountStringStyle.NoPrefix;
        private static string count_str_custom;
        private static string str_plus = "+";
        private static string str_x = "x";

        public static void SetCountStringStyle(CountStringStyle style, string custom = null)
        {
            countStrStyle = style;
            count_str_custom = custom;
        }

        private static string _GetCountStringByStyle(int count)
        {
            switch (countStrStyle)
            {
                case CountStringStyle.NoPrefix:
                    return $"{count}";
                case CountStringStyle.Custom:
                    return $"{count_str_custom ?? string.Empty}{count}";
                case CountStringStyle.StartsWithPlus:
                    return $"{str_plus}{count}";
                case CountStringStyle.StartsWithX:
                    return $"{str_x}{count}";
                case CountStringStyle.SmartPlus:
                    return count > 1 ? $"{str_plus}{count}" : string.Empty;
            }
            return string.Empty;
        }

        private static string _GetCountStringPrefix(int count)
        {
            switch (countStrStyle)
            {
                case CountStringStyle.NoPrefix:
                    return string.Empty;
                case CountStringStyle.Custom:
                    return count_str_custom ?? string.Empty;
                case CountStringStyle.StartsWithPlus:
                    return str_plus;
                case CountStringStyle.StartsWithX:
                    return str_x;
                case CountStringStyle.SmartPlus:
                    return count > 1 ? str_plus : string.Empty;
            }
            return string.Empty;
        }

        #endregion

        public static void NormalRes(UIImageRes img, AssetConfig res)
        {
            img.color = Color.white;
            if (img.image != null)
                GameUIUtility.SetDefaultShader(img.image);
            img?.SetImage(res.Group, res.Asset);
        }

        public static void NormalIcon(UIImageRes img, int id, int count)
        {
            img.color = Color.white;
            if (img.image != null)
                GameUIUtility.SetDefaultShader(img.image);
            _ShowIcon(img, id, count);
        }

        public static void LockedIcon(UIImageRes img, int id, int count, Color lockedCol)
        {
            img.color = lockedCol;
            GameUIUtility.SetMonoShader(img.image);
            _ShowIcon(img, id, count);
        }

        private static void _ShowIcon(UIImageRes img, int id, int count)
        {
            var res = Game.Manager.rewardMan.GetRewardIcon(id, count);
            img?.SetImage(res.Group, res.Asset);
        }

        public static void CountString(Text text, int id, int count)
        {
            if (id > 0)
            {
                if (Game.Manager.rewardMan.IsRewardTimed(id))
                {
                    text.text = TimeUtility.FormatCountDownOmitZeroTail(count);
                }
                else if (Game.Manager.rewardMan.IsRewardCountable(id))
                {
                    // 和rewardMan的区别是没有x符号
                    // 在美术设计统一前保持一致
                    // 沙漏道具
                    var cfg = Game.Manager.mergeItemMan.GetItemComConfig(id);
                    if (cfg.skillConfig != null && cfg.skillConfig.Type == SkillType.SandGlass)
                    {
                        var sec = cfg.skillConfig.Params[0] * count;
                        text.text = TimeUtility.FormatCountDownOmitZeroTail(sec);
                    }
                    else
                    {
                        text.text = _GetCountStringByStyle(count);
                    }
                }
            }
            else
            {
                text.text = $"{count}";
            }
        }

        public static void CountString(TMPro.TMP_Text text, int id, int count)
        {
            if (id > 0)
            {
                if (Game.Manager.rewardMan.IsRewardTimed(id))
                {
                    text.text = TimeUtility.FormatCountDownOmitZeroTail(count);
                }
                else if (Game.Manager.rewardMan.IsRewardCountable(id))
                {
                    // 和rewardMan的区别是没有x符号
                    // 在美术设计统一前保持一致
                    // 沙漏道具
                    var cfg = Game.Manager.mergeItemMan.GetItemComConfig(id);
                    if (cfg.skillConfig != null && cfg.skillConfig.Type == SkillType.SandGlass)
                    {
                        var sec = cfg.skillConfig.Params[0] * count;
                        text.text = TimeUtility.FormatCountDownOmitZeroTail(sec);
                    }
                    else
                    {
                        text.text = _GetCountStringByStyle(count);
                    }
                }
            }
            else
            {
                text.text = $"{count}";
            }
        }

        public static void Info(Button btn, int id)
        {
            if (UIUtility.CanShowInfo(id))
            {
                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();
                btn.WithClickScale().onClick.AddListener(() => _OnBtnInfo(id));
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }

        private static void _OnBtnInfo(int id)
        {
            ShowItemPanelInfo(id);
        }

        public static void InfoForReward(Button btn, int id)
        {
            if (ItemTipsInfoAuto(id, 0) || ItemTipsInfoValid(id))
            {
                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();
                btn.WithClickScale().onClick.AddListener(() => _OnBtnInfo(btn.transform as RectTransform, id));
            }
            // v41限时合成订单新增，如果这里展示有图鉴的东西，点击弹出图鉴界面
            else if (UIUtility.CanShowInfo(id))
            {
                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();
                btn.WithClickScale().onClick.AddListener(() => _OnBtnInfo(id));
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }

        private static void _OnBtnInfo(RectTransform root, int id)
        {
            UIItemUtility.ShowItemTipsInfoAuto(id, root.position, 10 + root.rect.size.y * 0.5f);
        }
    }
}
