/*
 * @Author: tang.yan
 * @Description: 主棋盘订单区域上的活动tips父节点管理脚本 
 * @Date: 2025-07-07 18:07:43
 */

using System.Collections.Generic;
using UnityEngine;
using EL;
using EventType = fat.rawdata.EventType;
using System.Collections;
using Config;

namespace FAT
{
    //活动数据类覆写 用于获取对应的tips资源
    public interface IBoardActivityTips
    {
        public string BoardActivityTipsAsset();
    }
    
    //Mono脚本与对应活动数据绑定
    public interface IBoardActivityTipsMono
    {
        public void BindActivity(ActivityLike activity);
    }
    
    public class MBBoardActivityTips : MonoBehaviour
    {
        public RectTransform root;
        private Dictionary<EventType, GameObject> _tipsGoDict = new();  //当前持有的所有加载好的go
        private HashSet<EventType> _loadingTypes = new();  //当前正在加载中的go
        private List<EventType> _tipsGoRemoveList = new();  //当前正在等待移除的go

        public void Setup() { }

        public void InitOnPreOpen()
        {
            MessageCenter.Get<MSG.ACTIVITY_ACTIVE>().AddListener(_OnActivityActive);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(_OnActivityEnd);
            _RemoveEndActTips();
            _TryCreateAllTips();
            _SetAllGoActive(true);
        }

        public void CleanupOnPostClose()
        {
            MessageCenter.Get<MSG.ACTIVITY_ACTIVE>().RemoveListener(_OnActivityActive);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(_OnActivityEnd);
            _SetAllGoActive(false);
            StopAllCoroutines();
            _loadingTypes.Clear();
            _tipsGoRemoveList.Clear();
        }

        private void _RemoveEndActTips()
        {
            var activity = Game.Manager.activity;
            //将已经结束的活动tips prefab移除掉
            _tipsGoRemoveList.Clear();
            foreach (var type in _tipsGoDict.Keys)
            {
                if (!activity.IsActive(type)) 
                    _tipsGoRemoveList.Add(type);
            }
            foreach (var type in _tipsGoRemoveList)
            {
                if (_tipsGoDict.TryGetValue(type, out var go))
                {
                    GameObject.Destroy(go);
                    _tipsGoDict.Remove(type);
                }
            }
            _tipsGoRemoveList.Clear();
        }

        private void _TryCreateAllTips()
        {
            //创建活动的tips prefab
            var map = Game.Manager.activity.map;
            foreach (var (_, a) in map)
            {
                _TryCreateTips(a);
            }
        }

        private void _TryCreateTips(ActivityLike act)
        {
            if (act is IBoardActivityTips activityTips)
            {
                var type = act.Type;
                //已经加载完了 跳过
                if (_tipsGoDict.ContainsKey(type))
                    return;
                //正在加载中 跳过
                if (_loadingTypes.Contains(type))
                    return;
                //资源名错误 跳过
                var asset = activityTips.BoardActivityTipsAsset()?.ConvertToAssetConfig();
                if (asset == null)
                {
                    DebugEx.Error($"MBBoardActivityTips : Try show tips, but prefabName is null act:{nameof(type)}");
                    return;
                }
                StartCoroutine(CoLoadTipsPrefab(asset, act));
            }
        }
        
        private IEnumerator CoLoadTipsPrefab(AssetConfig asset, ActivityLike act)
        {
            var actType = act.Type;
            _loadingTypes.Add(actType);
            var loader = EL.Resource.ResManager.LoadAsset<GameObject>(asset.Group, asset.Asset);
            yield return loader;
            _loadingTypes.Remove(actType);
            if (!loader.isSuccess || loader.asset == null)
            {
                DebugEx.Error($"MBBoardActivityTips::CoLoadTipsPrefab ----> loading res error, actType = {actType}, error = {loader.error}");
                yield break;
            }
            if (!act.Active)
                yield break;
            var assetGo = (loader.asset as GameObject);
            if (assetGo != null)
            {
                var go = Instantiate(assetGo, root);
                var comp = go.GetComponent<IBoardActivityTipsMono>();
                if (comp != null)
                {
                    comp.BindActivity(act);
                }
                go.SetActive(true);
                _tipsGoDict.Add(actType, go);
            }
        }

        private void _SetAllGoActive(bool isActive)
        {
            foreach (var go in _tipsGoDict.Values)
            {
                go.SetActive(isActive);
            }
        }

        private void _OnActivityActive(ActivityLike act, bool isNew)
        {
            _TryCreateTips(act);
        }
        
        private void _OnActivityEnd(ActivityLike act, bool expire)
        {
            if (act != null && _tipsGoDict.TryGetValue(act.Type, out var go))
            {
                var type = act.Type;
                GameObject.Destroy(go);
                _tipsGoDict.Remove(type);
                _loadingTypes.Remove(type);
            }
        }
    }
}