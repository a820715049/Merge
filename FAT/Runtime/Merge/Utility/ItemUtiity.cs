/**
 * @Author: handong.liu
 * @Date: 2021-02-20 18:20:55
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.conf;
using Cysharp.Text;

namespace FAT.Merge
{
    public enum MergeState
    {
        CanMerge = 0,
        HasBubble = 1,
        TeslaInUse = 1,
        GridNotAllowed = 3,
        Unknown = 4
    }
    public enum MoveState
    {
        CanMove,
        TargetNotMove,  //目标位置不允许移动
        GridNotAllowed, //目标格子不允许移动
        ItemNotAllowed, //棋子自身不允许移动
        Unknown
    }

    public enum GridState
    {
        Normal,
        CantMove,
    }


    public static class ItemUtility
    {
        public static bool StackToTarget(Item src, Item dst)
        {
            if (!CanStack(src, dst)) return false;
            var com = src.GetItemComponent<ItemSkillComponent>();
            return com != null && com.StackToTarget(dst);
        }

        public static bool CanStack(Item src, Item dst)
        {
            if (src == dst)
            {
                return false;
            }
            if (src.tid != dst.tid ||
                src.isDead || dst.isDead ||
                !src.isActive || !dst.isActive ||
                dst.isLocked || dst.isUnderCloud)
            {
                return false;
            }
            if (src.HasComponent(ItemComponentType.Bubble) || dst.HasComponent(ItemComponentType.Bubble))
            {
                return false;
            }
            if (dst.parent.GetGridTid(dst.coord.x, dst.coord.y) > 0)
            {
                return false;
            }
            src.TryGetItemComponent(out ItemSkillComponent skill_src);
            dst.TryGetItemComponent(out ItemSkillComponent skill_dst);
            if (skill_src == null || skill_dst == null)
            {
                return false;
            }
            if (skill_src.type != skill_dst.type)
            {
                return false;
            }
            return skill_src.CanStack();
        }

        public static bool CanMerge(Item src, Item dst)
        {
            return GetMergeState(src, dst) == MergeState.CanMerge;
        }

        public static int GetGridMatchId(int gridTid, int itemTid)
        {
            if (gridTid == 0)
            {
                return 0;
            }
            if (itemTid == 0)
            {
                return -1;
            }
            var itemConfig = Env.Instance.GetItemMergeConfig(itemTid);
            if (itemConfig.MergeGrid.Count == 0)
            {
                return -1;
            }
            return itemConfig.MergeGrid.Contains(gridTid) ? gridTid : -1;
        }

        public static bool CanItemInGridByTid(int gridTid, int itemTid)
        {
            return GetGridMatchId(gridTid, itemTid) == gridTid;
        }

        public static bool CanSourceOutputInGrid(int gridId, ItemComponentBase comp)
        {
            if (gridId == 0)
            {
                return true;
            }
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var outputs))
            {
                if (comp is ItemSourceComponentBase baseSource)
                {
                    baseSource.FillPossibleOutput(outputs);
                }

                foreach (int tid in outputs)
                {
                    if (!CanItemInGridByTid(gridId, tid))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool IsItemInNormalState(Item item)
        {
            return !item.isDead && item.isActive && !item.HasComponent(ItemComponentType.Bubble);
        }

        public static MergeState GetMergeState(Item src, Item dst)
        {
            if (src == dst)
            {
                return MergeState.Unknown;
            }
            //细化针对ItemBubbleComponent组件的合成判断
            if (HasBubbleComponent(src) || HasBubbleComponent(dst))
            {
                src.TryGetItemComponent(out ItemBubbleComponent srcBubble);
                dst.TryGetItemComponent(out ItemBubbleComponent dstBubble);
                var srcIsBubble = srcBubble?.IsBubbleItem() ?? false;
                var dstIsBubble = dstBubble?.IsBubbleItem() ?? false;
                var srcIsFrozen = srcBubble?.IsFrozenItem() ?? false;
                var dstIsFrozen = dstBubble?.IsFrozenItem() ?? false;
                // 1) 任意一方是“泡泡”类型 -> 阻止合成
                if (srcIsBubble || dstIsBubble)
                    return MergeState.HasBubble;
                // 2) 双方都是“冰冻”类型 -> 阻止合成
                if (srcIsFrozen && dstIsFrozen)
                    return MergeState.HasBubble;
                //有组件但为单侧冰冻 & 另一侧是普通可合成对象 允许继续往下走
            }
            if (dst.parent.GetGridTid(dst.coord.x, dst.coord.y) > 0)
            {
                return MergeState.GridNotAllowed;
            }
            if ((src.TryGetItemComponent(out ItemSkillComponent skill_a) && skill_a.teslaActive) ||
                (dst.TryGetItemComponent(out ItemSkillComponent skill_b) && skill_b.teslaActive))
            {
                return MergeState.TeslaInUse;
            }
            else
            {
                var comSrc = src.GetItemComponent<ItemMergeComponent>();
                var comDst = dst.GetItemComponent<ItemMergeComponent>();

                if (comSrc == null || comDst == null
                        || src.isDead || dst.isDead ||
                        !src.isActive || dst.isLocked || dst.isUnderCloud ||
                        comDst.PeekMergeResult(comSrc) <= 0)
                {
                    return MergeState.Unknown;
                }
            }
            return MergeState.CanMerge;
        }

        public static bool CanUnlock(Item item)
        {
            return !item.isDead && item.isLocked && !item.isUnderCloud && item.isReachBoardLevel;
        }

        public static void SetItemShowInCategory(int tid)
        {
            Env.Instance.OnItemShowInView(tid);
        }

        public static bool CheckSourceCanJumpCD(Item item)
        {
            if (item.TryGetItemComponent(out ItemClickSourceComponent click) && click.config.IsJumpable)
            {
                return true;
            }
            return false;
        }

        public static bool CanConsumeAny(Item itemA, Item itemB, out Item consume, out Item dst)
        {
            consume = null;
            dst = null;
            if (itemA == itemB)
                return false;
            if (!IsItemInNormalState(itemA) || !IsItemInNormalState(itemB))
            {
                return false;
            }
            if (itemA.TryGetItemComponent(out ItemClickSourceComponent click))
            {
                if (click.CanConsumeItem(itemB))
                {
                    consume = itemB;
                    dst = itemA;
                    return true;
                }
            }
            if (itemB.TryGetItemComponent(out click))
            {
                if (click.CanConsumeItem(itemA))
                {
                    consume = itemA;
                    dst = itemB;
                    return true;
                }
            }
            return false;
        }

        public static bool CanConsume(Item consume, Item dst)
        {
            if (consume == dst)
                return false;
            if (!IsItemInNormalState(consume) || !IsItemInNormalState(dst))
            {
                return false;
            }
            if (dst.TryGetItemComponent(out ItemClickSourceComponent click))
            {
                return click.CanConsumeItem(consume);
            }
            return false;
        }

        public static bool CanMix(Item consume, Item dst)
        {
            if (consume == dst)
                return false;
            if (!IsItemInNormalState(consume) || !IsItemInNormalState(dst))
            {
                return false;
            }
            if (dst.TryGetItemComponent(out ItemMixSourceComponent com))
            {
                return com.IsNextItemReady() && com.CanMixItem(consume);
            }
            return false;
        }

        public static bool CanFeed(Item food, Item dst)
        {
            if (food == dst)
            {
                return false;
            }
            if (!IsItemInNormalState(food) || !IsItemInNormalState(dst))
            {
                return false;
            }
            if (dst.TryGetItemComponent<ItemEatComponent>(out var comEat))
            {
                return comEat.CanEatItemId(food.tid);
            }
            return false;
        }

        public static ItemUseState FeedItem(Item item, Item food)
        {
            ItemUseState ret = ItemUseState.UnknownError;
            if (_FeedItemImp(item, food, ref ret))
            {
                ret = ItemUseState.Success;
            }
            return ret;
        }

        private static bool _FeedItemImp(Item item, Item food, ref ItemUseState state)
        {
            if (item.parent == null)
            {
                DebugEx.FormatWarning("ItemUtility._FeedItemImp ----> can't feed item not on board:{0}@{1}", item.id, item.tid);
                return false;
            }
            if (!IsItemInNormalState(food))
            {
                DebugEx.FormatWarning("ItemUtility._FeedItemImp ----> can't feed item not active:{0}@{1}", item.id, item.tid);
                return false;
            }
            if (item.HasComponent(ItemComponentType.EatSource))
            {
                return item.parent.EatSourceEatItem(item, food);
            }
            else if (item.HasComponent(ItemComponentType.Eat))
            {
                return item.parent.EatItem(item, food);
            }
            return false;
        }

        public static bool IsUseForTarget(Item item)
        {
            return item.HasComponent(ItemComponentType.Skill);
        }

        //返回true代表技能能用，state表示技能使用的详细情况
        public static bool CanUseForTarget(Item item, Item target, out ItemSkillState state)
        {
            state = default;
            var com = item.GetItemComponent<ItemSkillComponent>();
            return com != null ? com.CanUseForTarget(target, out state) : false;
        }

        public static bool UseForTarget(Item item, Item target)
        {
            var com = item.GetItemComponent<ItemSkillComponent>();
            return com != null ? com.UseForTarget(target) : false;
        }

        public static bool CanUseInTracer(Item item)
        {
            return !item.isDead &&
                    item.isActive &&
                    !item.HasComponent(ItemComponentType.Bubble);
        }

        public static bool CanUseInOrder(Item item)
        {
            return item.parent != null &&
                    !item.isDead &&
                    item.isActive &&
                    !item.HasComponent(ItemComponentType.Bubble);
        }

        public static bool CanUseInOrderAllowInventory(Item item)
        {
            return !item.isDead &&
                    item.isActive &&
                    !item.HasComponent(ItemComponentType.Bubble);
        }

        public static bool IsNeededByTopBarOrder(int tid)
        {
            return Env.Instance.IsOrderItem(tid);
        }

        public static int GetItemLevel(int itemId)
        {
            var cateConfig = Env.Instance.GetCategoryByItem(itemId);
            if (cateConfig == null)
            {
                return 0;
            }
            else
            {
                return cateConfig.Progress.IndexOf(itemId) + 1;
            }
        }

        public static bool IsExpItem(int tid)
        {
            var cate = Env.Instance.GetCategoryByItem(tid);
            return cate != null && cate.Progress[0] == Constant.kMergeExpItemObjId;
        }

        public static bool IsCoinItem(int tid)
        {
            var cate = Env.Instance.GetCategoryByItem(tid);
            return cate != null && cate.Progress[0] == Constant.kMergeCoinItemObjId;
        }

        // 仅返回coin类卖出所得 | 兼容ClearBoard逻辑
        public static int GetSellCoin(int tid)
        {
            var cfg = Env.Instance.GetItemMergeConfig(tid);
            if (Env.Instance.GetMergeLevel() < cfg.SellPlayerLv)
            {
                return 0;
            }
            if (cfg.IsSellForCoin)
            {
                return cfg.SellNum;
            }
            else
            {
                return 0;
            }
        }

        public static (int id, int num) GetSellReward(int tid)
        {
            var cfg = Env.Instance.GetItemMergeConfig(tid);
            if (Env.Instance.GetMergeLevel() < cfg.SellPlayerLv)
            {
                return (0, 0);
            }
            if (cfg.IsSellForCoin)
            {
                // 金币
                var id = Game.Manager.coinMan.GetIdByCoinType(CoinType.MergeCoin);
                return (id, cfg.SellNum);
            }
            else
            {
                // 体力
                return (Constant.kMergeEnergyObjId, cfg.SellNum);
            }
        }

        public static bool IsItemMaxLevel(int tid)
        {
            var cat = Env.Instance.GetCategoryByItem(tid);
            if (cat != null)
            {
                if (cat.Progress.Count > 0)
                {
                    return cat.Progress[cat.Progress.Count - 1] == tid;
                }
            }
            return false;
        }

        public static int GetNextItem(int itemId)
        {
            //check category
            var cateConfig = Env.Instance.GetCategoryByItem(itemId);
            if (cateConfig != null && cateConfig.MergeStyle == 0)            //merge style = 0就是可合成
            {
                var idx = cateConfig.Progress.IndexOf(itemId);
                if (idx + 1 < cateConfig.Progress.Count)
                {
                    return cateConfig.Progress[idx + 1];
                }
            }
            //check rule
            var rule = Env.Instance.GetMergeRuleByItem(itemId);
            if (rule != null)
            {
                return rule.TargetId;
            }

            //no one
            return 0;
        }

        public static int GetMergeItem(int itemId)
        {
            return GetNextItem(itemId);
        }

        public static void GetBoxOutputs(int id, List<int> container)
        {
            var comConfig = Env.Instance.GetItemComConfig(id);
            if (comConfig != null && comConfig.boxConfig != null)
            {
                for (int i = 0; i < comConfig.boxConfig.Outputs.Count; i++)
                {
                    container.Add(comConfig.boxConfig.Outputs[i]);
                }
            }
        }
        public static void GetEatSourceOutputs(int id, Dictionary<int, int> container)
        {
            var comConfig = Env.Instance.GetItemComConfig(id);
            if (comConfig != null && comConfig.eatSourceConfig != null)
            {
                for (int i = 0; i < comConfig.eatSourceConfig.Outputs.Count; i++)
                {
                    container.Add(comConfig.eatSourceConfig.Outputs[i], comConfig.eatSourceConfig.OutputsWeight.GetElementEx(i));
                }
            }
        }
        public static void GetClickSourceOutputs(int id, Dictionary<int, int> container)
        {
            var comConfig = Env.Instance.GetItemComConfig(id);
            if (comConfig != null && comConfig.clickSourceConfig != null)
            {
                var costCfg = Env.Instance.GetMergeTapCostConfig(comConfig.clickSourceConfig.CostId[0]);
                foreach (var kv in costCfg.Outputs)
                {
                    container.Add(kv.Key, kv.Value);
                }
            }
        }
        public static void GetAutoSourceOutputs(int id, Dictionary<int, int> container)
        {
            var comConfig = Env.Instance.GetItemComConfig(id);
            if (comConfig != null && comConfig.autoSourceConfig != null)
            {
                for (int i = 0; i < comConfig.autoSourceConfig.Outputs.Count; i++)
                {
                    container.Add(comConfig.autoSourceConfig.Outputs[i], comConfig.autoSourceConfig.OutputsWeight.GetElementEx(i));
                }
            }
        }

        public static bool IsCardPack(int tid)
        {
            return Game.Manager.objectMan.IsType(tid, ObjConfigType.CardPack);
        }

        public static bool IsMergeItem(int tid)
        {
            return Game.Manager.objectMan.IsType(tid, ObjConfigType.MergeItem);
        }

        public static bool HasBubbleComponent(Item item)
        {
            return item.HasComponent(ItemComponentType.Bubble);
        }

        public static bool IsBubbleItem(Item item)
        {
            return item.TryGetItemComponent(out ItemBubbleComponent bubble) && bubble.IsBubbleItem();
        }
        
        public static bool IsFrozenItem(Item item)
        {
            return item.TryGetItemComponent(out ItemBubbleComponent bubble) && bubble.IsFrozenItem();
        }

        public static bool IsChest(Item item)
        {
            return item.HasComponent(ItemComponentType.Chest);
        }

        public static bool IsClickSourceInCD(ItemClickSourceComponent comp)
        {
            if (comp.isNoCD)
                return false;
            if (comp.itemCount < 1 && (comp.isOutputing || comp.isReviving))
                return true;
            return false;
        }

        public static bool IsClickSourceReviving(ItemClickSourceComponent comp)
        {
            if (comp.isNoCD)
                return false;
            if (comp.itemCount < 1 && !comp.isOutputing && comp.isReviving)
                return true;
            return false;
        }

        public static int GetUnfrozenPrice(Item item)
        {
            var com = item.GetItemComponent<ItemFrozenOverrideComponent>();
            if (com != null)
            {
                return com.unfrozenPrice;
            }
            else
            {
                return _GetUnfrozenPrice(item.tid);
            }
        }

        public static int GetBubbleDeadItemId(ItemBubbleType type)
        {
            var globalConf = Env.Instance.GetGlobalConfig();
            var pool = type == ItemBubbleType.Bubble ? globalConf.BubbleDeadWeight : globalConf.FrozenItemDeadWeight;
            int totalWeight = 0;
            using (ObjectPool<List<(int id, int weight)>>.GlobalPool.AllocStub(out var list))
            {
                foreach (var reward in pool)
                {
                    var (id, w, _) = reward.ConvertToInt3();
                    totalWeight += w;
                    list.Add((id, w));
                }
                var roll = UnityEngine.Random.Range(1, totalWeight);
                var weightSum = 0;
                foreach (var cand in list)
                {
                    weightSum += cand.weight;
                    if (weightSum >= roll)
                    {
                        return cand.id;
                    }
                }
            }
            return Env.Instance.GetGlobalConfig().BubbleDeadItem;
        }

        public static bool CanItemInInventory(Item item)
        {
            var isLightbulb = false;
            var world = item.world;
            if (item.TryGetItemComponent<ItemSkillComponent>(out var skillComp))
            {
                if (skillComp.type == SkillType.Lightbulb)
                {
                    isLightbulb = true;
                }
            }
            var ret = (item.id != world.currentWaitChest) &&       //waiting chest cann't in inventory
                    (!item.HasComponent(ItemComponentType.Bubble)) &&         //bubble item cann't in inventory
                    (!item.HasComponent(ItemComponentType.OrderBox)) &&       //OrderBox item cann't in inventory
                    (!item.HasComponent(ItemComponentType.JumpCD)) &&       //jump cd item cann't in inventory
                    item.isActive && !isLightbulb;                                    //inactive item cann't in inventory
            if (ret)
            {
                //prevent event entry/exit items
                // var com = item.GetItemComponent<ItemFeatureComponent>();
                // ret = com == null ||
                //     (com.feature != Config.FeatureEntry.FeatureSeasonEvent
                //     && com.feature != Config.FeatureEntry.FeaturePvpEvent);
            }
            return ret;
        }

        public static string GetItemShortName(int tid)
        {
            var config = Env.Instance.GetItemConfig(tid);
            return config == null ? tid.ToString() : I18N.Text(config.Name);
        }

        //null means use default in config
        //"" means no sound at all
        public static SfxValue GetSourceSpawnSound(int tid)
        {
            SfxValue ret = new SfxValue();
            var config = Env.Instance.GetItemComConfig(tid);
            //trigAuto生产棋子时不播音效
            if (config.trigAutoSourceConfig != null)
                return ret;
            ret.eventName = "Spawn";
            string audioAssetStr = null;
            if (config.clickSourceConfig != null)
            {
                audioAssetStr = config.clickSourceConfig.SpawnSfx;
            }
            else if (config.autoSourceConfig != null)
            {
                audioAssetStr = config.autoSourceConfig.SpawnSfx;
            }
            else if (config.eatSourceConfig != null)
            {
                audioAssetStr = config.eatSourceConfig.SpawnSfx;
            }
            if (!string.IsNullOrEmpty(audioAssetStr))
            {
                ret.audioName = audioAssetStr.ConvertToAssetConfig().Asset;
            }
            return ret;
        }

        public static string GetItemLongName(int tid)
        {
            var level = GetItemLevel(tid);
            if (level <= 0)
            {
                return GetItemShortName(tid);
            }
            else
            {
                return GetItemShortName(tid) + " " + GetItemLevelStr(tid);
            }
        }

        public static string GetItemRuntimeShortName(Item item)
        {
            return GetItemShortName(item.tid);
        }

        public static string GetItemLevelStr(int tid)
        {
            var level = GetItemLevel(tid);
            if (level <= 0)
                level = 1;
            return I18N.FormatText("#CommonLevelNum", level);
        }

        private static string _StringJoin(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return b;
            }
            else if (string.IsNullOrEmpty((b)))
            {
                return a;
            }
            return $"{a}{I18N.Text("#SysComDesc89")}{b}";
        }

        public static string GetBoardItemInfo(Item item)
        {
            var ret = string.Empty;
            if (item.isLocked)
            {
                if (!item.isReachBoardLevel)
                {
                    ret = I18N.FormatText("#SysComDesc150", item.unLockLevel);
                }
                else
                {
                    ret = I18N.Text("#SysComDesc151");
                }
                return ret;
            }

            if (item.TryGetItemComponent(out ItemBubbleComponent bubble))
            {
                if (bubble.IsBubbleItem())
                {
                    ret = _StringJoin(ret, I18N.Text("#SysComDesc81"));
                }
                else if (bubble.IsFrozenItem())
                {
                    ret = _StringJoin(ret, I18N.Text("#SysComDesc1559"));
                }
            }
            else if (item.HasComponent(ItemComponentType.OrderBox))
            {
                // <订单礼盒>仅显示这句话
                return I18N.Text("#SysComDesc226");
            }
            else if (item.HasComponent(ItemComponentType.JumpCD))
            {
                // <跳过冷却>仅显示这句话
                return I18N.Text("#SysComDesc278");
            }
            else if (item.TryGetItemComponent(out ItemSkillComponent compSkill))
            {
                if (compSkill.type == SkillType.Upgrade)
                    return I18N.Text("#SysComDesc378");
                if (compSkill.type == SkillType.Degrade)
                    return I18N.Text("#SysComDesc379");
                if (compSkill.type == SkillType.Lightbulb)
                    return I18N.Text("#SysComDesc1095");
            }
            else
            {
                // 属性区
                if (item.isActive)
                {
                    if (item.TryGetItemComponent(out ItemBonusCompoent comBonus))
                    {
                        var b = $"{comBonus.bonusCount}{TextSprite.FromId(comBonus.bonusId)}";
                        ret = _StringJoin(ret, I18N.FormatText("#SysComDesc77", b));
                    }
                    else if (item.TryGetItemComponent(out ItemChestComponent comChest))
                    {
                        var world = Game.Manager.mergeBoardMan.activeWorld;
                        if (comChest.canUse)
                        {
                            // 已开
                        }
                        else if (world.currentWaitChest <= 0)
                        {
                            // 箱子未开 且 没有正在开的箱子
                            ret = _StringJoin(ret, I18N.FormatText("#SysComDesc79", UIUtility.CountDownFormat(comChest.config.WaitTime, UIUtility.CdStyle.OmitZero)));
                        }
                        else if (item.id != world.currentWaitChest)
                        {
                            // 其他箱子正在开
                            ret = _StringJoin(ret, I18N.Text("#SysComDesc80"));
                        }
                        else
                        {
                            // 自己正在开
                            ret = _StringJoin(ret, I18N.FormatText("#SysComDesc557", UIUtility.CountDownFormat(comChest.openWaitLeftMilli / 1000)));
                        }
                    }
                }

                if (item.config == null)
                {
                    return ret;
                }

                // 等级区
                var tid = item.config.ReplaceId > 0 ? item.config.ReplaceId : item.tid;
                if (IsItemMaxLevel(tid))
                {
                    ret = _StringJoin(ret, I18N.Text("#SysComDesc76"));
                }
                else
                {
                    ret = _StringJoin(ret, I18N.Text("#SysComDesc75"));
                }
            }
            return ret;
        }

        private static void _TryAddToolCount(Dictionary<int, int> dict, int key, int val)
        {
            // 仅添加存在的类别
            if (dict.ContainsKey(key))
            {
                dict[key] += val;
            }
        }

        private static int _SortCostInfo(CostInfo a, CostInfo b)
        {
            if (a.require != b.require)
            {
                return -(a.require - b.require);
            }
            return a.id - b.id;
        }

        /// <summary>
        /// 尽量提供用户最需要的工具类item
        /// </summary>
        /// <param name="source">item</param>
        /// <param name="toolId">决定要提供的链条</param>
        /// <param name="lackScore">缺少的分数</param>
        /// <returns>成功</returns>
        public static bool TrySpawnTool(Item source, out int toolId, out int lackScore)
        {
            toolId = 0;
            lackScore = 0;
            using (ObjectPool<Dictionary<int, int>>.GlobalPool.AllocStub(out var scoreDict))
            {
                var mgr = Game.Manager.mergeItemMan;
                var toolMap = mgr.GetToolConfigMap();

                // 仅记录激活的的工具类别
                foreach (var kv in toolMap)
                {
                    if (kv.Value.IsActive)
                    {
                        scoreDict[kv.Key] = 0;
                    }
                }
                var itemFromMergeWorld = source.world.currentTracer.GetCurrentActiveItemCount();
                if (itemFromMergeWorld != null)
                {
                    foreach (var kv in itemFromMergeWorld)
                    {
                        var toolItem = mgr.GetToolMergeConfig(kv.Key);
                        if (toolItem != null)
                        {
                            _TryAddToolCount(scoreDict, toolItem.ToolId, toolItem.ToolScore * kv.Value);
                        }
                    }
                }
                // // 背包内的item
                // var itemInBagList = Game.Manager.bagMan.GetBagGirdDataList((int)BagMan.BagType.Item);
                // foreach (var item in itemInBagList)
                // {
                //     if (item.ItemTId > 0)
                //     {
                //         var toolItem = mgr.GetToolMergeConfig(item.ItemTId);
                //         if (toolItem != null)
                //         {
                //             _TryAddToolCount(scoreDict, toolItem.ToolId, toolItem.ToolScore);
                //         }
                //     }
                // }
                // 货币分数
                foreach (var kv in toolMap)
                {
                    var ct = Game.Manager.coinMan.GetCoinTypeById(kv.Value.Id);
                    var val = Game.Manager.coinMan.GetCoin(ct) * kv.Value.ToolScore;
                    _TryAddToolCount(scoreDict, kv.Value.Id, val);
                }

                List<CostInfo> costInfoList = null;
                using (ObjectPool<Dictionary<int, int>>.GlobalPool.AllocStub(out var scoreDictCopy))
                {
                    foreach (var kv in scoreDict)
                    {
                        scoreDictCopy.Add(kv.Key, kv.Value);
                    }
                    costInfoList = Game.Manager.mapSceneMan.CollectCostRecord(scoreDictCopy, 1);
                    costInfoList.Sort(_SortCostInfo);
                }

                var rollRange = Mathf.Min(costInfoList.Count, Game.Manager.configMan.globalConfig.MaxToolCount);
                if (rollRange < 1)
                {
                    // 没有可用的tool 随机给一种
                    using (ObjectPool<List<ObjTool>>.GlobalPool.AllocStub(out var tools))
                    {
                        foreach (var kv in toolMap)
                        {
                            if (kv.Value.IsActive)
                                tools.Add(kv.Value);
                        }
                        var _idx = Random.Range(0, tools.Count);
                        toolId = tools[_idx].Id;
                        lackScore = 0;
                        DebugEx.Warning($"[toolsource] Utility::SpawnTool cost no. total {tools.Count}, roll {_idx}, tool {toolId}, lack 0");
                    }
                }
                else
                {
                    var rollIdx = Random.Range(0, rollRange);
                    toolId = costInfoList[rollIdx].id;
                    // 库存积分
                    scoreDict.TryGetValue(toolId, out var toolAccScore);
                    // 标准积分单位
                    var unitScore = toolMap[toolId].ToolScore;
                    // 需求的积分
                    var requireScore = costInfoList[rollIdx].require * unitScore;
                    var lackScoreOrig = requireScore - toolAccScore % unitScore;
                    lackScore = lackScoreOrig % unitScore;
#if UNITY_EDITOR
                    using (ObjectPool<System.Text.StringBuilder>.GlobalPool.AllocStub(out var sb))
                    {
                        sb.Append("[toolsource] ItemUtility::SpawnTool costInfoList");
                        sb.AppendLine();
                        foreach (var info in costInfoList)
                        {
                            sb.Append($"id:{info.id}\trequire:{info.require}\ttarget:{info.target}");
                            sb.AppendLine();
                        }
                        DebugEx.Info(sb.ToString());
                    }
#endif
                    DebugEx.Info(@$"[toolsource] ItemUtility::SpawnTool cost normal.  total {costInfoList.Count}, roll {rollIdx}, tool {toolId}, lack {lackScore}={lackScoreOrig}%{unitScore} | {lackScoreOrig}={requireScore}-{toolAccScore}%{unitScore}");
                }
            }
            return true;
        }

        public static void ProcessItemUseState(Item item, ItemUseState state)
        {
            if (state == ItemUseState.CoolingDown)
            {
                // 进行棋盘提示 / 信息区提示
                EL.MessageCenter.Get<MSG.UI_BOARD_ITEM_SPEEDUP_TIP>().Dispatch(item);

                Game.Manager.commonTipsMan.ShowPopTips(fat.rawdata.Toast.Charging, BoardUtility.GetWorldPosByCoord(item.coord));

                Game.Manager.audioMan.TriggerSound("Cooldown");
            }
            else if (state == ItemUseState.NotEnoughCost)
            {
                Game.Manager.commonTipsMan.ShowPopTips(fat.rawdata.Toast.LackItem, BoardUtility.GetWorldPosByCoord(item.coord));
            }
            else if (state == ItemUseState.UnknownError)
            {
            }
            else if (state == ItemUseState.NotEnoughSpace)
            {
                Game.Manager.commonTipsMan.ShowPopTips(fat.rawdata.Toast.BoardFull, BoardUtility.GetWorldPosByCoord(item.coord));
                Game.Manager.audioMan.TriggerSound("BoardFull");
            }
            else if (state == ItemUseState.NotEnoughSpaceForDying)
            {
                Game.Manager.commonTipsMan.ShowPopTips(fat.rawdata.Toast.BoardFull, BoardUtility.GetWorldPosByCoord(item.coord));
                Game.Manager.audioMan.TriggerSound("BoardFull");
            }
        }

        private static UserMergeOperation sLastUserItemOper;
        public static UserMergeOperation lastUserItemOper => sLastUserItemOper;
        public static ItemUseState UseItem(Item item, UserMergeOperation oper)
        {
            ItemUseState ret = ItemUseState.UnknownError;
            if (_UseItemImp(item, oper, ref ret))
            {
                ret = ItemUseState.Success;
            }
            return ret;
        }
        // [System.Obsolete("please use new function UseItemNew")]
        // public static bool UseItem(Item item, UserMergeOperation oper)
        // {
        //     ItemUseState state = ItemUseState.Success;
        //     return _UseItemImp(item, oper, ref state);
        // }
        private static bool _UseItemImp(Item item, UserMergeOperation oper, ref ItemUseState state)
        {
            sLastUserItemOper = oper;
            if (item.parent == null)
            {
                DebugEx.FormatWarning("ItemUtility.UseItem ----> can't use item not on board:{0}@{1}", item.id, item.tid);
                return false;
            }
            if (item.isDead)
            {
                DebugEx.FormatWarning("ItemUtility.UseItem ----> item is dead: {0}@{1}", item.id, item.tid);
                return false;
            }
            if (!item.isActive)
            {
                DebugEx.FormatWarning("ItemUtility.UseItem ----> item not active: {0}@{1}", item.id, item.tid);
                return false;
            }
            if (item.HasComponent(ItemComponentType.Bonus))
            {
                return item.parent.UseBonusItem(item);
            }
            else if (item.HasComponent(ItemComponentType.TapBonus))
            {
                return item.parent.UseTapBonusItem(item);
            }
            else if (item.TryGetItemComponent(out ItemChestComponent chest))
            {
                if (!chest.canUse && Game.Manager.mergeBoardMan.activeWorld.currentWaitChest <= 0)
                {
                    // 直接open
                    state = ItemUseState.Success;
                    return chest.StartWait();
                }
                return item.parent.UseChest(item, out state) != null;
            }
            else if (item.HasComponent(ItemComponentType.ClickSouce))
            {
                return item.parent.UseClickItemSource(item, out state) != null;
            }
            else if (item.HasComponent(ItemComponentType.MixSource))
            {
                // 尝试在棋盘上打开UI
                var wp = BoardUtility.GetWorldPosByCoord(item.coord);
                UIConfig.UIMixSourceTips.Open(wp, 72f, item);
                return true;
            }
            else if (item.HasComponent(ItemComponentType.AutoSouce))
            {
                return item.parent.UseAutoItemSource(item, out state, -1) != null;
            }
            else if (item.HasComponent(ItemComponentType.ToolSouce))
            {
                return item.parent.UseToolSource(item, out state) != null;
            }
            else if (item.HasComponent(ItemComponentType.SpecialBox))
            {
                return item.parent.UseSpecialBox(item, out state) != null;
            }
            else if (item.HasComponent(ItemComponentType.ChoiceBox))
            {
                return item.parent.UseChoiceBox(item, out state);
            }
            // else if(item.TryGetItemComponent<ItemDyingComponent>(out var dyingComponent) && dyingComponent.isSelfTrigger)
            // {
            //     return item.parent.TriggerItemDie(item, null, out state);
            // }
            else if (item.HasComponent(ItemComponentType.Box))
            {
                return item.parent.UseBox(item) != null;
            }
            else if (item.HasComponent(ItemComponentType.FeatureEntry))
            {
                item.parent.UseFeatureEntry(item);
                return true;
            }
            else if (item.HasComponent(ItemComponentType.EatSource))
            {
                return item.parent.UseEatSource(item, out state) != null;
            }
            else if (item.TryGetItemComponent<ItemSkillComponent>(out var comSkill) && !comSkill.IsNeedTarget())
            {
                return comSkill.Use();
            }
            else if (item.HasComponent(ItemComponentType.TrigAutoSource))
            {
                return item.parent.UseTrigAutoSource(item, out state);
            }
            else if (item.HasComponent(ItemComponentType.ActiveSource))
            {
                return item.parent.UseActiveSource(item, out state);
            }
            else
            {
                DebugEx.FormatWarning("ItemUtility.UseItem ----> item not usable: {0}@{1}", item.id, item.tid);
                return false;
            }
        }

        #region  for ui display

        public static bool IsUseChestDetailView(int tid)
        {
            var comConfig = Env.Instance.GetItemComConfig(tid);
            return comConfig != null && (comConfig.chestConfig != null || comConfig.boxConfig != null);
        }

        public static bool IsFromAuto(int id)
        {
            // FAT_TODO
            // var map = Game.Instance.configMan.GetComMergeAutoSourceMap();
            // foreach(var p in map) {
            //     var conf = p.Value;
            //     foreach(var o in conf.Outputs) {
            //         var chain = Env.Instance.GetCategoryByItem(o);
            //         if (chain.Progress.Contains(id)) return true;
            //     }
            // }
            return false;
        }

        #endregion

        public static int GetItemEnergyPerUse(Item item)
        {
            if (item.parent == null)
            {
                return 0;
            }
            if (item.isDead)
            {
                return 0;
            }
            if (!item.isActive)
            {
                return 0;
            }
            if (item.TryGetItemComponent<ItemChestComponent>(out var chest))
            {
                return chest.energyCost;
            }
            else if (item.TryGetItemComponent<ItemClickSourceComponent>(out var clickSource))
            {
                return clickSource.energyCost;
            }
            else if (item.TryGetItemComponent<ItemEatSourceComponent>(out var eatSourceComponent))
            {
                return eatSourceComponent.energyCost;
            }
            else if (item.TryGetItemComponent<ItemBoxComponent>(out var box))
            {
                return box.energyCost;
            }
            else
            {
                return 0;
            }
        }

        public static bool IsItemReadyToUse(Item item)
        {
            if (item.parent == null)
            {
                return false;
            }
            if (item.isDead)
            {
                return false;
            }
            if (!item.isActive)
            {
                return false;
            }
            return GetItemEmptyWaitMilli(item) <= 0;
        }

        public static bool IsSupportSpeedup(int tid)
        {
            var comConfig = Env.Instance.GetItemComConfig(tid);
            return comConfig?.chestConfig != null && comConfig.chestConfig.WaitTime > 0;
        }


        //注意往往要结合上面的IsSupportSpeedup一起使用
        public static bool CanUseGlobalFreeSpeedup(int tid)
        {
            return true;
        }

        public static bool CanUseGlobalFreeRecharge(int tid)
        {
            var category = Env.Instance.GetCategoryByItem(tid);
            return category != null && category.Id == 1;
        }

        // ref: https://centurygames.yuque.com/ywqzgn/ne0fhm/umlvtm1dg1c7pgi7
        public static int CalcCostBySeconds(int sec)
        {
            var cfg = Game.Manager.configMan.globalConfig;
            var divide = cfg.SpdUpDivide;
            var par = cfg.SpdUpParam;
            if (sec < divide[0])
            {
                return 0;
            }
            int cost;
            if (sec < divide[1])
            {
                cost = (sec - divide[0]) / par[0] + 1;
            }
            else if (sec < divide[2])
            {
                cost = (divide[1] - divide[0]) / par[0] + (sec - divide[1]) / par[1] + 1;
            }
            else
            {
                cost = (divide[1] - divide[0]) / par[0] + (divide[2] - divide[1]) / par[1] + (sec - divide[2]) / par[2] + 1;
            }
            return cost;
        }

        public static int CalcSpeedUpCost(ItemComponentBase com)
        {
            if (com is ItemBubbleComponent _bubble)
            {
                return _bubble.BreakCost;
            }
            else
            {
                long countdownSec = 0;
                if (com is ItemClickSourceComponent _click)
                {
                    if (_click.isOutputing)
                    {
                        countdownSec = _click.config.OutputTime - _click.outputMilli / 1000;
                    }
                    else if (_click.isReviving)
                    {
                        countdownSec = (_click.reviveTotalMilli - _click.reviveMilli) / 1000;
                    }
                }
                else if (com is ItemMixSourceComponent _mix)
                {
                    if (_mix.isOutputing)
                    {
                        countdownSec = _mix.config.OutputTime - _mix.outputMilli / 1000;
                    }
                    else if (_mix.isReviving)
                    {
                        countdownSec = (_mix.reviveTotalMilli - _mix.reviveMilli) / 1000;
                    }
                }
                else if (com is ItemAutoSourceComponent _auto)
                {
                    countdownSec = (_auto.outputWholeMilli - _auto.outputMilli) / 1000;
                }
                else if (com is ItemChestComponent _chest && _chest.isWaiting)
                {
                    countdownSec = _chest.config.WaitTime - _chest.openWaitMilli / 1000;
                }
                return CalcCostBySeconds((int)countdownSec);
            }
        }

        public enum ItemSpeedUpType
        {
            NeedCoin,
            FreeSpeedUp,
            FreeRecharge,
            FreeBubble
        }

        public static bool TryGetItemSpeedUpInfo(Item item, out ItemComponentBase component, out ItemSpeedUpType operation, out int coinCost)
        {
            component = null;
            operation = ItemSpeedUpType.NeedCoin;
            coinCost = 0;

            var globalData = Env.Instance.GetGlobalData();
            var globalConfig = Env.Instance.GetGlobalConfig();

            //IsBubbleItem为true时才能加速
            if (item.TryGetItemComponent(out ItemBubbleComponent bubble) && bubble.IsBubbleItem())
            {
                var boardId = Game.Manager.mergeBoardMan.activeWorld?.activeBoard?.boardId ?? 0;
                if (globalConfig.FreeBubbleCount.TryGetValue(boardId, out var count) && globalData.FreeBubbleUsed < count)
                {
                    operation = ItemSpeedUpType.FreeBubble;
                }
                else
                {
                    coinCost = CalcSpeedUpCost(bubble);
                }
                component = bubble;
                return true;
            }

            if (!IsItemInNormalState(item) || item.parent == null)
            {
                return false;
            }

            if (item.TryGetItemComponent(out ItemClickSourceComponent _click) && !_click.isGridNotMatch)
            {
                if (_click.itemCount <= 0 && (_click.isOutputing || _click.isReviving))
                {
                    if (globalData.FreeRechargeUsed < globalConfig.FreeRechargeTimes && CanUseGlobalFreeRecharge(item.tid))
                    {
                        operation = ItemSpeedUpType.FreeRecharge;
                    }
                    else
                    {
                        coinCost = CalcSpeedUpCost(_click);
                    }
                    component = _click;
                    return true;
                }
            }

            if (item.TryGetItemComponent(out ItemMixSourceComponent _mix) && !_mix.isGridNotMatch)
            {
                if (_mix.itemCount <= 0 && (_mix.isOutputing || _mix.isReviving))
                {
                    if (globalData.FreeRechargeUsed < globalConfig.FreeRechargeTimes && CanUseGlobalFreeRecharge(item.tid))
                    {
                        operation = ItemSpeedUpType.FreeRecharge;
                    }
                    else
                    {
                        coinCost = CalcSpeedUpCost(_mix);
                    }
                    component = _mix;
                    return true;
                }
            }

            if (item.TryGetItemComponent(out ItemAutoSourceComponent _auto) && !_auto.isGridNotMatch)
            {
                if (_auto.isOutputing && _auto.itemCount <= 0)
                {
                    if (globalData.FreeRechargeUsed < globalConfig.FreeRechargeTimes && CanUseGlobalFreeRecharge(item.tid))
                    {
                        operation = ItemSpeedUpType.FreeRecharge;
                    }
                    else
                    {
                        coinCost = CalcSpeedUpCost(_auto);
                    }
                    component = _auto;
                    return true;
                }
            }

            if (item.TryGetItemComponent(out ItemEatSourceComponent _eat) && !_eat.isGridNotMatch)
            {
                if (_eat.state == ItemEatSourceComponent.Status.Eating)
                {
                    if (globalData.FreeRechargeUsed < globalConfig.FreeRechargeTimes && CanUseGlobalFreeRecharge(item.tid))
                    {
                        operation = ItemSpeedUpType.FreeRecharge;
                    }
                    else
                    {
                        coinCost = _eat.CalculateSpeedEatCost();
                    }
                    component = _eat;
                    return true;
                }
            }
            if (item.TryGetItemComponent(out ItemChestComponent _chest))
            {
                if (_chest.isWaiting)
                {
                    if (globalData.FreeSpeedUpUsed < globalConfig.FreeSpeedUpTimes && CanUseGlobalFreeSpeedup(item.tid))
                    {
                        operation = ItemSpeedUpType.FreeSpeedUp;
                    }
                    else
                    {
                        coinCost = CalcSpeedUpCost(_chest);
                    }
                    component = _chest;
                    return true;
                }
            }
            return false;
        }

        public static void TrySpeedUpEmptyItem(Item item, System.Action whenSuccess)
        {
            if (TryGetItemSpeedUpInfo(item, out var component, out var operation, out var price))
            {
                var componentType = ItemComponentTable.GetEnumByType(component.GetType());
                var globalData = Env.Instance.GetGlobalData();
                bool isFree = false;
                ReasonString reason;
                switch (operation)
                {
                    case ItemSpeedUpType.FreeBubble:
                        isFree = true;
                        reason = ReasonString.bubble;
                        globalData.FreeBubbleUsed++;
                        DebugEx.FormatInfo("Merge::ItemUtility::SpeedUpEmptyItem ----> free speed up used {0}:{1}.{2}", globalData.FreeBubbleUsed, item, componentType);
                        break;
                    case ItemSpeedUpType.FreeSpeedUp:
                        isFree = true;
                        reason = ReasonString.skip;
                        globalData.FreeSpeedUpUsed++;
                        DebugEx.FormatInfo("Merge::ItemUtility::SpeedUpEmptyItem ----> free speed up used {0}:{1}.{2}", globalData.FreeSpeedUpUsed, item, componentType);
                        break;
                    case ItemSpeedUpType.FreeRecharge:
                        isFree = true;
                        reason = ReasonString.skip;
                        globalData.FreeRechargeUsed++;
                        DebugEx.FormatInfo("Merge::ItemUtility::SpeedUpEmptyItem ----> free recharge used {0}:{1}.{2}", globalData.FreeRechargeUsed, item, componentType);
                        break;
                    default:
                        if (!Env.Instance.CanUseGem(price))
                        {
                            DebugEx.FormatWarning("Merge::ItemUtility::SpeedUpEmptyItem ----> recharge with money fail {0}:{1}.{2}({3})", globalData.FreeRechargeUsed, item, componentType, price);
                            return;
                        }
                        else
                        {
                            DebugEx.FormatInfo("Merge::ItemUtility::SpeedUpEmptyItem ----> recharge with money {0}:{1}.{2}({3})", globalData.FreeRechargeUsed, item, componentType, price);
                            if (componentType == ItemComponentType.Bubble)
                            {
                                reason = ReasonString.bubble;
                            }
                            else
                            {
                                reason = ReasonString.skip;
                            }
                        }
                        break;
                }
                if (isFree)
                {
                    ProcessPaySuccess();
                }
                else
                {
                    Env.Instance.UseGem(price, reason, ProcessPaySuccess, componentType != ItemComponentType.Bubble);
                }
                // 消费成功之后的事情
                void ProcessPaySuccess()
                {
                    var com = item.GetItemComponent(componentType);
                    switch (componentType)
                    {
                        case ItemComponentType.Bubble:
                            {
                                if (com is ItemBubbleComponent bubble && bubble.IsBubbleItem())
                                {
                                    item.parent?.UnleashBubbleItem(item);
                                    // track
                                    //免费的情况不会弹窗，不用担心打点问题
                                    DataTracker.TrackMergeActionBubble(item, ItemUtility.GetItemLevel(item.tid), false, isFree);
                                }
                            }
                            break;
                        case ItemComponentType.ClickSouce:
                            {
                                if (com is ItemClickSourceComponent _click)
                                {
                                    if (_click.isOutputing)
                                    {
                                        _click.SpeedOutput();
                                    }
                                    else if (_click.isReviving)
                                    {
                                        _click.SpeedRevive();
                                    }
                                }
                            }
                            break;
                        case ItemComponentType.MixSource:
                            {
                                if (com is ItemMixSourceComponent _mix)
                                {
                                    if (_mix.isOutputing)
                                    {
                                        _mix.SpeedOutput();
                                    }
                                    else if (_mix.isReviving)
                                    {
                                        _mix.SpeedRevive();
                                    }
                                }
                            }
                            break;
                        case ItemComponentType.AutoSouce:
                            {
                                if (com is ItemAutoSourceComponent _auto)
                                {
                                    if (_auto.isOutputing)
                                        _auto.SpeedOutput();
                                }
                            }
                            break;
                        case ItemComponentType.EatSource:
                            {
                                if (com is ItemEatSourceComponent _eat)
                                {
                                    if (_eat.state == ItemEatSourceComponent.Status.Eating)
                                        _eat.SpeedEat();
                                }
                            }
                            break;
                        case ItemComponentType.Chest:
                            {
                                if (com is ItemChestComponent _chest)
                                {
                                    if (_chest.isWaiting)
                                        _chest.SpeedOpen();
                                }
                            }
                            break;
                            // case ItemComponentType.Dying:
                            // {
                            //     if(com is ItemDyingComponent _dying && _dying.isSelfTrigger)
                            //     {
                            //         if(!_dying.canTriggerDie)
                            //             _dying.SpeedDie();
                            //     }
                            // }
                            // break;
                    }
                    whenSuccess?.Invoke();
                }
            }
        }

        public static long GetItemEmptyWaitMilli(Item item)
        {
            if (item.parent == null)
            {
                return 0;
            }
            if (item.isDead)
            {
                return 0;
            }
            if (!item.isActive)
            {
                return 0;
            }
            if (item.TryGetItemComponent<ItemChestComponent>(out var chest) && chest.isWaiting)
            {
                return chest.openWaitLeftMilli;
            }
            if (item.TryGetItemComponent<ItemEatSourceComponent>(out var eatSource) && eatSource.state == ItemEatSourceComponent.Status.Eating)
            {
                return eatSource.eatLeftMilli;
            }
            if (item.TryGetItemComponent<ItemClickSourceComponent>(out var clickSource) && clickSource.totalItemCount <= 0)
            {
                return clickSource.isReviving ? (clickSource.config.ReviveTime * 1000 - clickSource.reviveMilli) : 0;
            }
            if (item.TryGetItemComponent<ItemAutoSourceComponent>(out var autoSource) && autoSource.itemCount <= 0)
            {
                return autoSource.isOutputing ? autoSource.outputWholeMilli - autoSource.outputMilli : 0;
            }
            return 0;
        }

        public static int GetItemUsableCount(Item item)
        {
            if (item.parent == null)
            {
                return 0;
            }
            if (item.isDead)
            {
                return 0;
            }
            if (!item.isActive)
            {
                return 0;
            }
            if (item.TryGetItemComponent<ItemChestComponent>(out var chest))
            {
                return chest.canUse ? chest.countLeft : 0;
            }
            else if (item.TryGetItemComponent<ItemClickSourceComponent>(out var clickSource))
            {
                return clickSource.itemCount;
            }
            else if (item.TryGetItemComponent<ItemAutoSourceComponent>(out var autoSource))
            {
                return autoSource.itemCount;
            }
            else if (item.TryGetItemComponent<ItemBoxComponent>(out var box))
            {
                return box.countLeft;
            }
            else if (item.TryGetItemComponent<ItemEatSourceComponent>(out var eatSourceComponent))
            {
                return eatSourceComponent.countLeft;
            }
            else
            {
                return 0;
            }
        }

        private static string _GetItemBasicDesc(int tid)
        {
            var config = Env.Instance.GetItemMergeConfig(tid);
            if (!string.IsNullOrEmpty(config.Desc))
            {
                return FormatMergeItemDesc(tid, config.Desc);
            }
            else
            {
                return FormatMergeItemDesc(tid, Env.Instance.GetItemConfig(tid).Desc);
            }
        }

        public static string FormatMergeItemDesc(int tid, string desc, int count = 1)
        {
            if (string.IsNullOrEmpty(desc))
            {
                return "";
            }
            var comConfig = Env.Instance.GetItemComConfig(tid);
            string ret = I18N.Text(desc);
            if (comConfig != null)
            {
                if (comConfig.bonusConfig != null)
                {
                    var bonusConfig = Env.Instance.GetItemConfig(comConfig.bonusConfig.BonusId);
                    ret = ret.Replace("{C}", count.ToString());
                    ret = ret.Replace("{BN}", bonusConfig == null ? comConfig.bonusConfig.BonusId.ToString() : I18N.Text(bonusConfig.Name));
                    ret = ret.Replace("{BC}", comConfig.bonusConfig.BonusCount.ToString());
                    ret = ret.Replace("{BTC}", (comConfig.bonusConfig.BonusCount * count).ToString());
                }
                if (comConfig.timeSkipConfig != null)
                {
                    ret = ret.Replace("{ST}", (comConfig.timeSkipConfig.Seconds / 3600).ToString());
                }
                if (comConfig.skillConfig != null)
                {
                    ret = ItemSkillComponent.ProcessDesc(ret, comConfig.skillConfig.Type, comConfig.skillConfig.Params);
                }
            }
            var nextItem = GetMergeItem(tid);
            if (nextItem > 0)
            {
                ret = ret.Replace("{N}", GetItemShortName(nextItem));
            }
            return ret;
        }

        private static int _GetUnfrozenPrice(int tid)
        {
            var conf = Env.Instance.GetItemMergeConfig(tid);
            if (conf != null)
            {
                return conf.UnlockPrice;
            }
            else
            {
                return 0;
            }
        }

        public static bool IsDropLimitItem(int tid, out int convertId)
        {
            convertId = 0;
            if (Env.Instance.TryGetDropLimitItemConfig(tid, out var cfg))
            {
                var scoreCfg = Data.GetDropLimitScore(cfg.ScoreId);
                if (scoreCfg != null)
                {
                    var totalScore = 0;
                    var itemCountDict = Game.Manager.mergeBoardMan.activeWorld.currentTracer.GetCurrentActiveItemCount();
                    // 关联物品分数
                    foreach (var kv in scoreCfg.ItemScore)
                    {
                        if (itemCountDict.TryGetValue(kv.Key, out var count))
                        {
                            totalScore += kv.Value * count;
                        }
                    }
                    // 关联订单分数 | 目前只考虑主订单
                    var orderMan = Game.Manager.mainOrderMan;
                    foreach (var kv in scoreCfg.OrderScore)
                    {
                        if (orderMan.IsOrderCompleted(kv.Key))
                        {
                            totalScore += kv.Value;
                        }
                    }
                    if (totalScore >= cfg.LimitScore)
                    {
                        convertId = cfg.ReplaceInto;
                        return true;
                    }
                }
            }
            return false;
        }

        public static void CollectRewardItem(Merge.Item item, Dictionary<int, int> itemIdMap, Dictionary<int, int> rewardMap)
        {
            static void Collect(Dictionary<int, int> map, int id, int count)
            {
                if (map.ContainsKey(id)) map[id] += count;
                else map.Add(id, count);
            }

            if (item.TryGetItemComponent<ItemBonusCompoent>(out var bonusComp) && bonusComp.funcType == FuncType.Reward)
            {
                //收集棋子id
                Collect(itemIdMap, item.tid, 1);
                //收集奖励信息
                Collect(rewardMap, bonusComp.bonusId, bonusComp.bonusCount);
            }
            else if (item.TryGetItemComponent<ItemTapBonusComponent>(out var tapBonusComp) && tapBonusComp.funcType == FuncType.Collect)
            {
                //收集棋子id
                Collect(itemIdMap, item.tid, 1);
                //收集奖励信息
                Collect(rewardMap, tapBonusComp.bonusId, tapBonusComp.bonusCount);
            }
        }

        // id:数量:棋子等级  逗号隔开
        public static string ConvertItemDictToString_Id_Num_Level(Dictionary<int, int> dict)
        {
            var sb = ZString.CreateStringBuilder();
            foreach (var info in dict)
            {
                if (sb.Length > 0) sb.Append(",");  // 只有在不是第一个元素时才加逗号
                sb.Append(info.Key.ToString());
                sb.Append(":");
                sb.Append(info.Value.ToString());
                sb.Append(":");
                sb.Append(ItemUtility.GetItemLevel(info.Key).ToString());
            }
            return sb.ToString();
        }

    }
}
