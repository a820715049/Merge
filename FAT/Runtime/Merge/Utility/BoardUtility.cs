/*
 * @Author: qun.chao
 * @Date: 2021-02-19 18:30:11
 */

namespace FAT
{
    using UnityEngine;
    using System.Collections.Generic;
    using Config;
    using Merge;
    using UnityEngine.UI.Extensions;
    using EL;

    public static class BoardUtility
    {
        public static bool debugShow { get; set; }
        public static bool botPlaying { get; set; }
        public static float cellSize { get; private set; }
        public static float screenCellSize { get; private set; }
        public static float canvasToScreenCoe { get; private set; }
        public static float screenToCanvasCoe { get; private set; }
        public static Vector2 originPosInScreenSpace { get; private set; }
        public static bool isBoardCheckerPaused { get; private set; }
        public static readonly Color iconColFrozen = new Color32(0xC8, 0xC8, 0xC8, 255);
        #region sprite
        public static Sprite bubbleCoverSprite { get; private set; }
        public static Sprite frozenCoverSprite { get; private set; }
        public static Sprite BottomSprite { get; private set; }
        // private static Sprite[] boxSprite;
        private static List<AssetConfig> boxAssets = new List<AssetConfig>();
        #endregion

        public static BoardRes.SpawnPopParam spawnPopParam { get; private set; }

        public static float itemPopDuration;
        public static bool isConstantPopDuration;
        public static bool snapToFinger;

        #region item spawn request

        // 棋子生成请求 => id/位置/动画延迟(用于表现一系列物品错落生成的效果)
        private static List<(int id, Vector3 pos, float delay)> itemSpawnRequestList = new();

        public static void RegisterSpawnRequest(int itemId, Vector3 worldPos, float delay = -1f)
        {
            itemSpawnRequestList.Add((itemId, worldPos, delay));
        }

        public static bool ResolveSpawnRequest(int itemId, out Vector3 pos, out float delay)
        {
            var idx = itemSpawnRequestList.FindIndex(item => item.id == itemId);
            if (idx >= 0)
            {
                (_, pos, delay) = itemSpawnRequestList[idx];
                itemSpawnRequestList.RemoveAt(idx);
                return true;
            }
            else
            {
                pos = Vector3.zero;
                delay = -1f;
                return false;
            }
        }

        public static void ClearSpawnRequest()
        {
            itemSpawnRequestList.Clear();
        }

        public static void PopSpawnRequest()
        {
            if (itemSpawnRequestList.Count > 0)
                itemSpawnRequestList.RemoveAt(itemSpawnRequestList.Count - 1);
        }

        #endregion

        public static void SetCellSize(float size)
        {
            cellSize = size;
            screenCellSize = size * canvasToScreenCoe;
        }

        public static void SetOriginPos(Vector2 pos)
        {
            originPosInScreenSpace = pos;
        }

        public static void SetCanvasToScreenCoe(float coe)
        {
            canvasToScreenCoe = coe;
            screenToCanvasCoe = 1f / coe;
        }

        public static void BoardCheckerPause(bool p)
        {
            isBoardCheckerPaused = p;
        }

        // public static void ShowBoardItemSelector(Item item)
        // {
        //     UIManager.Instance.OpenWindow(UIConfig.UIBoardItemSelector, item);
        // }

        public static Vector3 GetWorldPosByCoord(Vector2Int coord)
        {
            // 整数坐标格 => 屏幕坐标
            var sp = GetScreenPosByCoord(coord.x, coord.y);
            // 屏幕坐标 => 世界坐标
            RectTransformUtility.ScreenPointToWorldPointInRectangle(UIManager.Instance.CanvasRoot, sp, null, out var wp);
            return wp;
        }

        public static Vector2 GetRealCoordByScreenPos(Vector2 pos)
        {
            return new Vector2((pos.x - originPosInScreenSpace.x) / screenCellSize, -(pos.y - originPosInScreenSpace.y) / screenCellSize);
        }

        public static Vector2 GetRealCoordByBoardPos(Vector2 pos)
        {
            var coord = new Vector2();
            coord.x = pos.x / BoardUtility.cellSize - 0.5f;
            coord.y = -pos.y / BoardUtility.cellSize - 0.5f;
            return coord;
        }

        public static Vector2Int GetCoordByScreenPos(Vector2 pos)
        {
            return new Vector2Int(Mathf.FloorToInt((pos.x - originPosInScreenSpace.x) / screenCellSize),
                -Mathf.CeilToInt((pos.y - originPosInScreenSpace.y) / screenCellSize));
        }

        public static Vector2 GetPosByCoord(float x, float y)
        {
            return new Vector2(cellSize * x + cellSize * 0.5f, -cellSize * y - cellSize * 0.5f);
        }

        public static Vector2 GetScreenPosByCoord(float x, float y)
        {
            return originPosInScreenSpace + new Vector2(screenCellSize * x + screenCellSize * 0.5f, -screenCellSize * y - screenCellSize * 0.5f);
        }

        public static Vector2 GetScreenPosByBoardPos(Vector2 boardPos)
        {
            return boardPos / screenToCanvasCoe + originPosInScreenSpace;
        }

        public static Vector2 GetBoardPosByScreenPos(Vector2 pos)
        {
            return screenToCanvasCoe * (pos - originPosInScreenSpace);
        }

        public static Vector2 CalcItemLocalPosInMoveRootByCoord(Vector2 coord)
        {
            var sp = GetScreenPosByCoord(coord.x, coord.y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(BoardViewManager.Instance.moveRoot, sp, null, out var lp);
            return lp;
        }

        public static Vector2 CalcItemLocalPosInMoveRoot(Vector2 boardPos)
        {
            var sp = GetScreenPosByBoardPos(boardPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(BoardViewManager.Instance.moveRoot, sp, null, out var lp);
            return lp;
        }

        public static void PlaceItemToBoardCoord(GameObject go, Vector2Int coord)
        {
            (go.transform as RectTransform).anchoredPosition = new Vector2((coord.x + 0.5f) * BoardUtility.cellSize, -(coord.y + 0.5f) * BoardUtility.cellSize);
        }

        // P = (1−t)^2 * P0 + 2(1−t) * t * P1 + t^2 * P2
        public static Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector2 p = uu * p0;
            p += 2 * u * t * p1;
            p += tt * p2;

            return p;
        }

        public static int FiilMatchItemList(Vector2Int coord, List<int> container = null)
        {
            var tid = BoardViewManager.Instance.board.GetGridTid(coord.x, coord.y);
            return Game.Manager.mergeItemMan.FillMatchItemByGridTemplate(tid, container);
        }

        public static (bool unlock, bool showTip) TryUnlockGallery(Item item)
        {
            if (item.isActive)
            {
                var conf = Env.Instance.GetCategoryByItem(item.tid);
                if (conf != null)
                {
                    if (!Game.Manager.handbookMan.IsItemUnlocked(item.tid) && !Game.Manager.handbookMan.IsItemUnlockedInList(item.tid))
                    {
                        ItemUtility.SetItemShowInCategory(item.tid);
                        //使用配置字段来决定棋子第一次获得时是否要飘字 默认0为飘字
                        if (conf.NewItemToast == 0 && Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(fat.rawdata.FeatureEntry.FeatureNewItemToast))
                        {
                            return (true, true);
                        }
                        return (true, false);
                    }
                }
            }
            return (false, false);
        }


        public static void LoadAndPreparePoolItem(string resConfig, string poolKey)
        {
            if (GameObjectPoolManager.Instance.HasPool(poolKey))
                return;
            var res = resConfig.ConvertToAssetConfig();
            var task = EL.Resource.ResManager.TryLoadAssetSync<GameObject>(res.Group, res.Asset);
            if (task != null && task.isSuccess && task.asset != null)
            {
                GameObjectPoolManager.Instance.PreparePool(poolKey, task.asset as GameObject);
            }
        }

        public static PoolItemType EffTypeToPoolType(ItemEffectType type)
        {
            switch (type)
            {
                case ItemEffectType.Energy:
                    return PoolItemType.MERGE_BOARD_EFFECT_ENERGY;
                case ItemEffectType.BoostEnergy:
                    return PoolItemType.MERGE_BOARD_EFFECT_BOOST_ENERGY;
                case ItemEffectType.Spawnable:
                    return PoolItemType.MERGE_BOARD_EFFECT_SPAWNABLE;
                case ItemEffectType.TopLevel:
                    return PoolItemType.MERGE_BOARD_EFFECT_TOPLEVEL;
                case ItemEffectType.OnBoard:
                    return PoolItemType.MERGE_BOARD_EFFECT_ON_BOARD;
                case ItemEffectType.OnMerge:
                    return PoolItemType.MERGE_BOARD_EFFECT_ON_MERGE;
                case ItemEffectType.OnCollect:
                    return PoolItemType.MERGE_BOARD_EFFECT_ON_COLLECT;

                case ItemEffectType.UnFrozen:
                    return PoolItemType.MERGE_BOARD_EFFECT_UNFROZEN;
                case ItemEffectType.UnlockNormal:
                    return PoolItemType.MERGE_BOARD_EFFECT_UNLOCK_NORMAL;
                case ItemEffectType.UnlockLevel:
                    return PoolItemType.MERGE_BOARD_EFFECT_UNLOCK_LEVEL;
                case ItemEffectType.TapLocked:
                    return PoolItemType.MERGE_BOARD_EFFECT_TAP_LOCKED;

                case ItemEffectType.OrderBoxTrail:
                    return PoolItemType.MERGE_BOARD_EFFECT_ORDERBOX_TRAIL;
                case ItemEffectType.OrderBoxOpen:
                    return PoolItemType.MERGE_BOARD_EFFECT_ORDERBOX_OPEN;
                case ItemEffectType.OrderItemConsumed:
                    return PoolItemType.MERGE_BOARD_EFFECT_ORDER_ITEM_CONSUMED;
                case ItemEffectType.OrderCanFinish:
                    return PoolItemType.MERGE_BOARD_EFFECT_ORDER_CAN_FINISH;

                case ItemEffectType.JumpCDDisappear:
                    return PoolItemType.MERGE_BOARD_EFFECT_JUMPCD_DISAPPEAR;
                case ItemEffectType.JumpCDTrail:
                    return PoolItemType.MERGE_BOARD_EFFECT_JUMPCD_TRAIL;
                case ItemEffectType.JumpCDBg:
                    return PoolItemType.MERGE_BOARD_EFFECT_JUMPCD_BG;

                case ItemEffectType.TimeSkip:
                    return PoolItemType.MERGE_ITEM_EFFECT_HIGHLIGHT;
                case ItemEffectType.TeslaSource:
                    return PoolItemType.MERGE_ITEM_EFFECT_TESLA_SOURCE;
                case ItemEffectType.TeslaBuff:
                    return PoolItemType.MERGE_ITEM_EFFECT_TESLA_BUFF;
                case ItemEffectType.Filter_Scissor:
                    return PoolItemType.MERGE_ITEM_EFFECT_SCISSOR;
                case ItemEffectType.Filter_Feed:
                    return PoolItemType.MERGE_ITEM_EFFECT_FEED_IND;

                case ItemEffectType.SpeedUp_Tip:
                    return PoolItemType.MERGE_ITEM_EFFECT_SPEEDUP_TIP;
                case ItemEffectType.EnergyBoostBg4X:
                    return PoolItemType.MERGE_ITEM_EFFECT_ENERGYBOOST_4X;
                // case ItemEffectType.Sell_Tip:
                //     return PoolItemType.MERGE_ITEM_EFFECT_SELL_TIP;

                case ItemEffectType.MagicHourTrail:
                    return PoolItemType.MERGE_ITEM_EFFECT_MAGIC_HOUR_TRAIL;
                case ItemEffectType.MagicHourHit:
                    return PoolItemType.MERGE_ITEM_EFFECT_MAGIC_HOUR_HIT;
                case ItemEffectType.TrigAutoSource:
                    return PoolItemType.MERGE_ITEM_EFFECT_TRIG_AUTO_SOURCE;
                case ItemEffectType.Lightbulb:
                    return PoolItemType.MERGE_ITEM_EFFECT_LIGHTBULB;
            }
            return PoolItemType.NONE;
        }

        public static void AddAutoReleaseComponent(GameObject go, float lifeTime, string type)
        {
            var script = go.GetOrAddComponent<MBAutoRelease>();
            script.Setup(type, lifeTime);
        }

        public static void AddAutoReleaseComponent(GameObject go, float lifeTime, PoolItemType type)
        {
            AddAutoReleaseComponent(go, lifeTime, type.ToString());
        }

        public static void ReleaseAutoPoolItemFromChildren(Transform root)
        {
            MBAutoRelease script = null;
            for (int i = root.childCount - 1; i >= 0; --i)
            {
                script = root.GetChild(i).GetComponent<MBAutoRelease>();
                if (script != null)
                {
                    script.Release();
                }
            }
        }

        #region fast sprite

        public static void SetBoxAssets(int boardId)
        {
            var cfg = Game.Manager.mergeBoardMan.GetBoardConfig(boardId);
            boxAssets.Clear();
            foreach (var box in cfg.ImgBox)
            {
                boxAssets.Add(box.ConvertToAssetConfig());
            }
        }

        public static AssetConfig GetLevelLockBg()
        {
            return GetBoxAsset(0);
        }

        public static AssetConfig GetBoxAsset(int idx)
        {
            return boxAssets[idx % boxAssets.Count];
        }

        public static void SetFrozenCoverSprite(Sprite sp)
        {
            frozenCoverSprite = sp;
        }

        public static void SetBubbleCoverSprite(Sprite sp)
        {
            bubbleCoverSprite = sp;
        }

        public static void SetBottomSprite(Sprite sp)
        {
            BottomSprite = sp;
        }

        public static void SetSpawnPopParam(BoardRes.SpawnPopParam param)
        {
            spawnPopParam = param;
        }

        #endregion

        #region spawn pos

        /// <summary>
        /// 计算棋盘左上角为起点的坐标体系中的飞行路径
        /// 棋子有三种坐标体系
        /// 1.规则棋盘坐标
        /// 2.move坐标
        /// 3.屏幕坐标
        /// 坐标1和2之间通过屏幕坐标进行换算
        /// 这里计算的是坐标1
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="midOffsetY"></param>
        /// <param name="endOffsetDist"></param>
        /// <returns></returns>
        public static (Vector2, Vector2, Vector2, Vector3) CalcBezierControlPosForSpawnByCoord(Vector2 from, Vector2 to, float midOffsetY, float endOffsetDist)
        {
            Vector2 p0, p1, p2, pEnd;
            p0 = GetPosByCoord(from.x, from.y);
            pEnd = GetPosByCoord(to.x, to.y);

            // 落点需要在终点附近偏移
            var dir = (pEnd - p0).normalized;
            p2 = pEnd - dir * endOffsetDist;

            // 中点需要在纵向偏移
            p1 = (p0 + p2) * 0.5f;
            p1.y += midOffsetY;

            return (p0, p1, p2, pEnd);
        }

        /// <summary>
        /// 根据起点终点构建掉落路径中间点
        /// 基于棋盘格坐标系 左上角(0,0)
        /// </summary>
        public static (Vector2 a, Vector2 b, Vector2 c) CalcBezierControlPos(Vector2 from, Vector2 to, bool ignoreOffset = false)
        {
            var cellSize = BoardUtility.cellSize;
            var halfSize = cellSize * 0.5f;

            Vector2 p0, p1, p2;

            // 从from位置产生 落到to格子的边界
            p0 = GetPosByCoord(from.x, from.y);
            p2 = GetPosByCoord(to.x, to.y);

            if (!ignoreOffset)
            {
                if (from.x > to.x) p2.x += halfSize;
                else if (from.x < to.x) p2.x -= halfSize;

                if (from.y > to.y) p2.y -= halfSize;
                else if (from.y < to.y) p2.y += halfSize;
            }

            p1 = (p0 + p2) * 0.5f;
            p1.y += cellSize * 2;

            return (p0, p1, p2);
        }

        public static (Vector2, float) GetRequestedSpawnPos(MBItemView view)
        {
            if (BoardUtility.ResolveSpawnRequest(view.data.tid, out var worldPos, out var delay))
            {
                var screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
                // 换算到棋盘坐标体系 使用左上角为锚点的节点
                var origMoveRoot = BoardViewManager.Instance.boardView.moveRoot;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(origMoveRoot, screenPos, null, out var localPos);
                return (localPos, delay);
            }
            else
            {
                // 没找到 直接在目标位置出现 | 顺便清理数据
                BoardUtility.ClearSpawnRequest();
                var cellSize = BoardUtility.cellSize;
                var halfSize = cellSize * 0.5f;
                var to = view.data.coord;
                return (GetPosByCoord(to.x, to.y), 0f);
            }
        }
        #endregion

        #region interact

        public static void OnLackOfEnergy()
        {
            if(Game.Manager.guideMan.IsGuideFinished(16)) 
                Game.Manager.screenPopup.WhenOutOfEnergy();
            // FAT_TODO
            // Game.Instance.conditionMan.TriggerEvent(ConditionEventType.UseEnergyWhenNoEnergy);
            // // 为了识别此时处于空闲状态
            // if (!UIManager.Instance.IsResolvingRes() &&
            //     UIManager.Instance.IsLayerEmpty(UILayer.AboveStatus) &&
            //     UIManager.Instance.IsLayerEmpty(UILayer.Modal))
            // {
            //     if (BoardViewWrapper.IsPvpEventBoard(out _))
            //     {
            //         UIManager.Instance.OpenWindow(UIConfig.UIEnergyShopForActivity);
            //     }
            //     else
            //     {
            //         var lackOfEnergy = true;
            //         UIManager.Instance.OpenWindow(UIConfig.UIEnergyShop, lackOfEnergy);
            //     }
            // }
        }

        public static void OnLackOfEnergyAfterEnergyShopClosed()
        {
            // FAT_TODO
            // if (!UIManager.Instance.IsResolvingRes() &&
            //     UIManager.Instance.IsLayerEmpty(UILayer.AboveStatus) &&
            //     UIManager.Instance.IsLayerEmpty(UILayer.Modal))
            // {
            //     UIManager.Instance.OpenWindow(UIConfig.UIStarterPackShop);
            // }
        }

        public static bool CanWatchBubbleAds()
        {
            return Game.Manager.adsMan.CheckCanWatchAds(Game.Manager.configMan.globalConfig.BubbleAdId);
        }

        public static bool UseItemOnBoard(Item item, UserMergeOperation oper)
        {
            var result = ItemUtility.UseItem(item, oper);
            if (result == ItemUseState.Success)
            {
                return true;
            }
            ItemUtility.ProcessItemUseState(item, result);
            return false;
        }
        #endregion
    }
}