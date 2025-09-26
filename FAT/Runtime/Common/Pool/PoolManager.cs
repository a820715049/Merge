/*
 * @Author: qun.chao
 * @Date: 2020-08-24 18:51:48
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PoolItemType
{
    NONE,

    #region order

    MERGE_BOARD_ORDER,

    #endregion

    COMMON_EFFECT_FLY_ICON,
    COMMON_COLLECT_ITEM,

    BOARD_GRID_INFO_ITEM,

    #region gallery

    HANDBOOK_GROUP_CELL,
    HANDBOOK_ITEM_CELL,

    #endregion

    TOAST_ITEM,
    TOAST_FLY_ITEM,

    MAIL_DETAIL_REWARD_ITEM,
    AUTHENTICATION_TYPE_ITEM,
    SETTING_COMMUNITY_TYPE_ITEM,
    SETTING_COMMUNITY_SHOP_ITEM,
    COMMUNITY_PLAN_GIFT_REWARD_ITEM,

    CARD_CELL_LAYOUT,
    CARD_JOKER_GROUP_CELL,
    CARD_JOKER_ITEM_CELL,

    #region merge board

    MERGE_ITEM_VIEW,
    MERGE_ITEM_HOLDER,

    #endregion

    #region 挖沙活动

    DIGGING_BOARD_CELL_LOCK,
    DIGGING_BOARD_ITEM, //海马等各种可挖出来的物品
    DIGGING_BOARD_ITEM_BG, //海马等物品的背景板（某些主题为了表现效果）
    DIGGING_PROGRESS_REWARD,
    DIGGING_BOARD_CELL_BG,

    #endregion

    #region 沙堡里程碑活动
    CASTLE_MILESTONE_CELL,
    #endregion

    #region 拼图活动 里程碑样式
    PUZZLE_MILESTONE_CELL,
    #endregion

    #region 积分活动 里程碑样式

    SCORE_MILESTONE_CELL,

    #endregion

    #region 挖矿活动 里程碑样式

    MINE_BOARD_MILESTONE_CELL,

    #endregion

    #region 连续订单
    ORDER_STREAK_CELL,
    #endregion

    #region 积分活动 轨道样式

    SCORE_TRACK_CELL,

    #endregion


    #region 周任务活动

    WEEKLY_TASK_CELL,

    #endregion

    #region 钓鱼活动 里程碑样式

    FISH_BOARD_MILESTONE_ITEM,

    #endregion

    #region 农场活动
    MERGE_CLOUD_VIEW, // 云层样式
    #endregion

    #region 许愿棋盘活动
    WISH_BOARD_CLOUD_VIEW,
    #endregion

    SPECIAL_BOX_ITEM,

    EVENT_TREASURE_PACK_ITEM,

    BOARD_FLY_ITEM,

    MERGE_ITEM_BG,

    MERGE_BOARD_EFFECT_ENERGY,
    MERGE_BOARD_EFFECT_BOOST_ENERGY,
    MERGE_BOARD_EFFECT_SPAWNABLE,
    MERGE_BOARD_EFFECT_TOPLEVEL,
    MERGE_BOARD_EFFECT_ON_BOARD,
    MERGE_BOARD_EFFECT_ON_MERGE,
    MERGE_BOARD_EFFECT_ON_COLLECT,
    MERGE_BOARD_EFFECT_UNFROZEN,
    MERGE_BOARD_EFFECT_UNLOCK_NORMAL,
    MERGE_BOARD_EFFECT_UNLOCK_LEVEL,
    MERGE_BOARD_EFFECT_TAP_LOCKED,
    MERGE_BOARD_EFFECT_ORDERBOX_TRAIL,
    MERGE_BOARD_EFFECT_ORDERBOX_OPEN,
    MERGE_BOARD_EFFECT_ORDER_ITEM_CONSUMED,
    MERGE_BOARD_EFFECT_ORDER_CAN_FINISH,
    MERGE_BOARD_EFFECT_JUMPCD_DISAPPEAR,
    MERGE_BOARD_EFFECT_JUMPCD_TRAIL,
    MERGE_BOARD_EFFECT_JUMPCD_BG,

    MERGE_ITEM_EFFECT_HIGHLIGHT,
    MERGE_ITEM_EFFECT_TESLA_SOURCE,
    MERGE_ITEM_EFFECT_TESLA_BUFF,
    MERGE_ITEM_EFFECT_SCISSOR,
    MERGE_ITEM_EFFECT_FEED_IND,
    MERGE_ITEM_EFFECT_SPEEDUP_TIP,
    MERGE_ITEM_EFFECT_SELL_TIP,
    MERGE_ITEM_EFFECT_ENERGYBOOST_4X,

    MERGE_ITEM_EFFECT_MAGIC_HOUR_TRAIL,
    MERGE_ITEM_EFFECT_MAGIC_HOUR_HIT,
    MERGE_ITEM_EFFECT_TRIG_AUTO_SOURCE,
    MERGE_ITEM_EFFECT_LIGHTBULB,
    MERGE_ITEM_EFFECT_FROZEN_ITEM,

    Auto_Guide_Finger,
    PACK_MARKET_SLIDE_CELL,
    MIX_COST_ITEM,
    PACK_ERG_LIST_CELL,

    MAP_BUILDING_EFFECT,

    #region 小游戏相关

    MINIGAME_BEADS_ROOT_CELL,
    MINIGAME_BEADS_CELL,
    MINIGAME_BEADS_ROOT_EFFECT_CELL,

    #endregion

    #region 弹珠游戏相关

    PACHINKO_PROGRESS_REWARD,
    PACHINKO_FLY_ITEM,

    #endregion

    #region 挖矿棋盘活动

    MINE_BOARD_GALLERY_ITEM,

    #endregion

    #region 积分活动 里程碑样式
    Ranking_MILESTONE_CELL,
    Ranking_REWARD_CELL,

    #endregion

    #region 兑换商店里程碑
    RedeemShop_MILESTONE_CELL,
    RedeemShop_SMALLREWARD_CELL,
    RedeemShop_BIGEWARD_CELL,

    #endregion

    #region 邮件补单
    MAIL_RESHIPMENT_CELL,
    #endregion
    #region 许愿棋盘 里程碑样式

    WISH_BOARD_MILESTONE_CELL,
    WISH_BOARD_GALLERY_ITEM,

    #endregion

    #region 矿车棋盘活动

    MINE_CART_BOARD_GALLERY_ITEM,
    #endregion

    #region 倍率排行榜
    Ranking_REWARD_ITEM,
    
    #endregion
}

public class GameObjectPoolManager : MonoSingleton<GameObjectPoolManager>
{
    private Transform mPoolRoot = null;

    private Dictionary<string, GameObject> mInstDict = new();

    // private Dictionary<string, List<GameObject>> mPoolDict = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, Queue<GameObject>> mPoolDict = new();

    public void Initialize()
    {
        var root = new GameObject("PoolRoot");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        root.SetActive(false);
        mPoolRoot = root.transform;
    }

    public void CreateObject(string resKey, Transform parent, System.Action<GameObject> cb)
    {
        if (HasPool(resKey))
            cb?.Invoke(CreateObject(resKey, parent));
        else
            FAT.Game.Instance.StartCoroutineGlobal(ATLoadObject(resKey, parent, cb));
    }

    public IEnumerator ATLoadObject(string resKey, Transform parent, System.Action<GameObject> cb)
    {
        var res = resKey.ConvertToAssetConfig();
        var task = EL.Resource.ResManager.LoadAsset<GameObject>(res.Group, res.Asset);
        yield return task;
        if (task.isSuccess)
        {
            var obj = Instantiate(task.asset as GameObject);
            PreparePool(resKey, obj);
            ReleaseObject(resKey, obj);
            cb?.Invoke(CreateObject(resKey, parent));
        }
    }

    #region poolitemtype wrapper

    public void PreparePool(PoolItemType type, GameObject origin)
    {
        PreparePool(type.ToString(), origin);
    }

    public void ClearPool(PoolItemType type)
    {
        _ClearPool(type.ToString());
    }

    public GameObject CreateObject(PoolItemType type)
    {
        return CreateObject(type.ToString());
    }

    public GameObject CreateObject(PoolItemType type, Transform parent)
    {
        return CreateObject(type.ToString(), parent);
    }

    public void ReleaseObject(PoolItemType type, GameObject obj)
    {
        ReleaseObject(type.ToString(), obj);
    }

    #endregion

    public bool HasPool(string type)
    {
        return _HasPool(type);
    }

    public void PreparePool(string type, GameObject origin)
    {
        _PreparePool(type, origin);
    }

    public GameObject CreateObject(string type)
    {
        return _CreateObject(type);
    }

    public GameObject CreateObject(string type, Transform parent)
    {
        var go = _CreateObject(type);
        go.transform.SetParent(parent);
        go.transform.localScale = Vector3.one;
        return go;
    }

    public void ReleaseObject(string type, GameObject obj)
    {
        _ReleaseObject(type, obj);
    }

    public void ClearAllPool()
    {
        var keyList = new List<string>();
        foreach (var kv in mInstDict) keyList.Add(kv.Key);
        foreach (var key in keyList) _ClearPool(key);
        keyList.Clear();
        mInstDict.Clear();
    }

    private void _PreparePool(string objKey, GameObject origin)
    {
        if (_HasPool(objKey))
            return;

        var obj = Instantiate(origin);

        mInstDict.Add(objKey, obj);
        mPoolDict.Add(objKey, new Queue<GameObject>());

        _CreatePool(objKey, obj);
    }

    private void _ClearPool(string objKey)
    {
        if (!_HasPool(objKey))
            return;

        mInstDict.Remove(objKey);
        if (mPoolDict.ContainsKey(objKey))
        {
            var objs = mPoolDict[objKey];
            if (objs != null) objs.Clear();
            mPoolDict.Remove(objKey);
        }

        var pool = mPoolRoot.Find(objKey);
        if (pool != null) Destroy(pool.gameObject);
    }

    private void _CreatePool(string objKey, GameObject obj)
    {
        // pool root
        var pool = new GameObject(objKey);
        pool.transform.SetParent(mPoolRoot);

        // cache root
        var cache = new GameObject("_cache");
        cache.transform.SetParent(pool.transform);

        // prefab
        // obj.SetActive(false);
        obj.name = objKey;
        obj.transform.SetParent(pool.transform);
    }

    private bool _HasPool(string objKey)
    {
        return mInstDict.ContainsKey(objKey);
    }

    private GameObject _CreateObject(string objKey)
    {
        if (!_HasPool(objKey))
            return null;

        var container = mPoolDict[objKey];
        if (container.Count > 0)
        {
            var obj = container.Dequeue();
            obj.transform.SetParent(null);
            return obj;
        }
        else
        {
            var newObj = Instantiate(mInstDict[objKey]) as GameObject;
            return newObj;
        }
    }

    private void _ReleaseObject(string objKey, GameObject obj)
    {
        if (!_HasPool(objKey))
            return;

        mPoolDict[objKey].Enqueue(obj);
        obj.transform.SetParent(mPoolRoot.Find(objKey).GetChild(0));
    }
}
