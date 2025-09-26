/*
 * @Author: qun.chao
 * @Date: 2022-02-24 17:42:53
 */
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Text;
using UnityEngine;
using EL;
using FAT.Merge;
using fat.rawdata;
using static fat.conf.Data;
using Coffee.UIExtensions;

namespace FAT
{
    public static partial class UIUtility
    {
        public static bool CanPopupMergeItemDetail(Item item)
        {
            if (item.config == null) return false;
            var mergeItemTid = item.config.ReplaceId > 0 ? item.config.ReplaceId : item.tid;
            return Game.Manager.mergeItemMan.GetItemCategoryId(mergeItemTid) > 0;
        }

        public static void CommonItemRefresh(Transform root, List<Config.RewardConfig> list)
        {
            var count = root.childCount;
            for (int i = 0; i < count; i++)
            {
                if (root.GetChild(i).TryGetComponent<UICommonItem>(out var comp))
                {
                    if (i >= list.Count)
                    {
                        comp.gameObject.SetActive(false);
                    }
                    else
                    {
                        comp.gameObject.SetActive(true);
                        comp.Refresh(list[i]);
                    }
                }
            }
        }

        public static void CommonItemSetup(Transform root)
        {
            var count = root.childCount;
            for (int i = 0; i < count; i++)
            {
                if (root.GetChild(i).TryGetComponent<UICommonItem>(out var comp))
                {
                    comp.Setup();
                }
            }
        }
        
        //特殊道具的数量改成显示时间
        public static bool SpecialCountText(int id_, int count_, out string text_) {
            //note: count is ignored
            var confOB = GetComMergeOrderBox(id_);
            if (confOB != null) {
                text_ = UIUtility.CountDownFormat(confOB.Time / 1000, CdStyle.OmitZero);
                return true;
            }
            var confCD = GetComMergeJumpCD(id_);
            if (confCD != null) {
                text_ = UIUtility.CountDownFormat(confCD.Time / 1000, CdStyle.OmitZero);
                return true;
            }
            text_ = string.Empty;
            return false;
        }

        public static void CreateGenericPooItem<T>(Transform root, PoolItemType type, IList<T> list)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                var go = GameObjectPoolManager.Instance.CreateObject(type);
                go.transform.SetParent(root);
                go.transform.localScale = Vector3.one;
                go.GetComponent<UIGenericItemBase<T>>().SetData(list[i]);
                go.SetActive(true);
            }
        }

        public static void CreateGenericPooItem<T>(Transform root, string typeKey, IList<T> list)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                var go = GameObjectPoolManager.Instance.CreateObject(typeKey);
                go.transform.SetParent(root);
                go.transform.localScale = Vector3.one;
                go.GetComponent<UIGenericItemBase<T>>().SetData(list[i]);
                go.SetActive(true);
            }
        }

        public static void ReleaseClearableItem(Transform root, PoolItemType type)
        {
            for (int i = root.childCount - 1; i >= 0; --i)
            {
                var item = root.GetChild(i);
                item.GetComponent<UIClearableItem>().Clear();
                GameObjectPoolManager.Instance.ReleaseObject(type, item.gameObject);
            }
        }

        public static void ReleaseChildren(Transform root, PoolItemType type)
        {
            for (var i = root.childCount - 1; i >= 0; --i)
            {
                var item = root.GetChild(i);
                GameObjectPoolManager.Instance.ReleaseObject(type, item.gameObject);
            }
        }

        public static void ReleaseClearableItem(Transform root, string typeKey)
        {
            for (int i = root.childCount - 1; i >= 0; --i)
            {
                var item = root.GetChild(i);
                item.GetComponent<UIClearableItem>().Clear();
                GameObjectPoolManager.Instance.ReleaseObject(typeKey, item.gameObject);
            }
        }

        public static bool ShowMoneyNotEnoughTip(CoinType ct, int cost)
        {
            //目前只接受gem类型
            if (ct != CoinType.Gem)
                return false;
            if (cost <= Game.Manager.coinMan.GetCoin(ct))
            {
                return false;
            }
            ShowGemNotEnough();
            return true;
        }

        public static void ShowGemNotEnough() {
            //钻石不足时 打开IAP商店 并弹提示
            Game.Manager.commonTipsMan.ShowPopTips(Toast.OutOfDiamond);
            Game.Manager.screenPopup.WhenOutOfDiamond();
        }

        public static bool CanShowInfo(int itemId)
        {
            if (Game.Manager.objectMan.IsType(itemId, ObjConfigType.MergeItem))
            {
                return true;
            }
            else if (Game.Manager.objectMan.IsType(itemId, ObjConfigType.DecoReward))
            {
                return true;
            }
            else if (Game.Manager.objectMan.IsType(itemId, ObjConfigType.Wallpaper))
            {
                return true;
            }
            return false;
        }

        public static void ShowGameplayHelp()
        {
            UIConfig.UIGameplayHelp.Open();
        }

        public static void FadeIn(UIBase ui_, Animator uiAnim_) {
            uiAnim_.Play("Common_Panel_MiddleShow");
        }

        public static void FadeOut(UIBase ui_, Animator uiAnim_) {
            uiAnim_.Play("Common_Panel_MiddleHide");
            static IEnumerator WaitClose(UIBase ui_, Animator anim_) {
                yield return null;
                var n = anim_.GetCurrentAnimatorStateInfo(0);
                yield return new WaitForSeconds(n.length);
                ui_.Close();
            }
            ui_.StartCoroutine(WaitClose(ui_, uiAnim_));
        }

        public static Vector3 GetScreenCenterWorldPosForUICanvas()
        {
            return GetScreenWorldPosForUICanvas(Vector2.one * 0.5f);
        }

        public static Vector3 GetScreenWorldPosForUICanvas(Vector2 targetPivot)
        {
            var root = UIManager.Instance.CanvasRoot;
            var localPos = Vector2.Scale(targetPivot - root.pivot, root.rect.size);
            var worldPos = root.TransformPoint(localPos);
            return worldPos;
        }
        
        //itemA 使用的棋子id 如万能卡 分割器
        //itemB 目标棋子id 
        public static void ShowItemUseConfirm(string desc, Action cb, int itemA, int itemB)
        {
            UIManager.Instance.OpenWindow(UIConfig.UIItemUseConfirm, desc, cb, itemA, itemB);
        }

        public static void CommonResFeedBackSoundEffect(FlyType ft)
        {
            switch (ft)
            {
                case FlyType.Coin:
                    Game.Manager.audioMan.TriggerSound("AddCoinHit");
                    break;
                case FlyType.Gem:
                    Game.Manager.audioMan.TriggerSound("AddGemHit");
                    break;
                case FlyType.Energy:
                    Game.Manager.audioMan.TriggerSound("AddEnergyHit");
                    break;
                case FlyType.Exp:
                    Game.Manager.audioMan.TriggerSound("XpHit");
                    break;
                case FlyType.Handbook:
                    Game.Manager.audioMan.TriggerSound("AddGemHit");
                    break;
            }
        }
        
        public static void CommonResFeedBackShakeEffect(int objBasicId)
        {
            //根据配置决定是否在资源飞向资源栏时触发震动效果
            var shakeConf = Game.Manager.configMan.GetShakeConfig(objBasicId);
            var canShake = shakeConf?.IsShake ?? false;
            //默认可以震动时 播中等震动效果
            if (canShake)
                VibrationManager.VibrateMedium();
        }

        public static string FormatTMPString(int id)
        {
            var spriteName = "";
            var coinConf = Game.Manager.objectMan.GetCoinConfig(id);
            if (coinConf != null)
            {
                spriteName = coinConf.SpriteName;
            }
            else
            {
                var tokenConf = Game.Manager.objectMan.GetTokenConfig(id);
                if (tokenConf != null)
                {
                    spriteName = tokenConf.SpriteName;
                }
            }
            return ZString.Format("<sprite name=\"{0}\">", spriteName);
        }

        public static void SetRectFullScreenSize(RectTransform trans)
        {
            var canvasRect = UIManager.Instance.CanvasRoot;
            trans.anchorMin = trans.anchorMax = Vector2.one * 0.5f;
            trans.sizeDelta = canvasRect.sizeDelta;
        }

        public enum ScreenViewState
        {
            Inside,
            Part,
            Outside,
        }

        // 根据屏幕状态返回动画预计的延迟时间
        public static float GetScreenViewStateDelay(ScreenViewState state)
        {
            return state switch
            {
                ScreenViewState.Part => 0.25f,
                ScreenViewState.Outside => 0.5f,
                _ => -1f,
            };
        }

        // 检查trans是否在屏幕内 | 仅处理水平方向
        public static ScreenViewState CheckScreenViewState(RectTransform trans)
        {
            var orderRectMin = Vector2.Scale(Vector2.zero - trans.pivot, trans.rect.size);
            var orderRectMax = Vector2.Scale(Vector2.one - trans.pivot, trans.rect.size);
            var screenMin = RectTransformUtility.WorldToScreenPoint(null, UIUtility.GetScreenWorldPosForUICanvas(Vector2.zero));
            var screenMax = RectTransformUtility.WorldToScreenPoint(null, UIUtility.GetScreenWorldPosForUICanvas(Vector2.one));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trans, screenMin, null, out var localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trans, screenMax, null, out var localMax);
            var oss = ScreenViewState.Inside;
            if (orderRectMin.x < localMin.x || orderRectMax.x > localMax.x)
            {
                if (orderRectMax.x < localMin.x || orderRectMin.x > localMin.x)
                {
                    // 完全超出屏幕
                    oss = ScreenViewState.Outside;
                }
                else
                {
                    // 部分超出屏幕
                    oss = ScreenViewState.Part;
                }
            }
            else
            {
                // 无需调整
                oss = ScreenViewState.Inside;
            }
            return oss;
        }

        // 计算目标scroll位置 使targetTrans居中 | 仅处理水平方向
        public static bool CalcScrollPosForTargetInMiddleOfView(RectTransform targetTrans, RectTransform contentRoot, RectTransform viewRoot, out float pos)
        {
            pos = 0f;
            var contentWidth = contentRoot.rect.width;
            var viewWidth = viewRoot.rect.width;
            if (contentWidth < viewWidth)
                return false;
            var localCenter = Vector2.Scale(Vector2.one * 0.5f - targetTrans.pivot, targetTrans.rect.size);
            var localCenterInScroll = contentRoot.InverseTransformPoint(targetTrans.TransformPoint(localCenter));
            var halfView = viewWidth * 0.5f;
            if (localCenterInScroll.x > halfView)
            {
                if (contentWidth - localCenterInScroll.x > halfView)
                {
                    // safe
                    pos = -(localCenterInScroll.x - halfView);
                }
                else
                {
                    // 以右边为准
                    pos = -(contentWidth - viewWidth);
                }
            }
            else
            {
                // 以左边为准
                pos = 0;
            }
            return true;
        }

        public static Vector2 GetLocalCenterInRect(RectTransform rect)
        {
            return Vector2.Scale(Vector2.one * 0.5f - rect.pivot, rect.rect.size);
        }

        public static void ManuallyEmitParticle(UIParticle eff)
        {
            foreach (var p in eff.particles)
            {
                if (p.emission.burstCount > 0)
                {
                    var bst = p.emission.GetBurst(0);
                    var count = bst.count.mode == ParticleSystemCurveMode.Constant ?
                        (short)bst.count.constant : (bst.minCount + bst.maxCount) / 2;
                    if (count > 0)
                    {
                        p.Emit(count);
                    }
                }
            }
        }

        /// <summary>
        /// 截取指定长度字符串
        /// </summary>
        /// <param name="text">文本</param>
        /// <param name="maxLength">长度</param>
        /// <returns></returns>
        public static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length > maxLength ? text.Substring(0, maxLength) : text;
        }
    }
}
