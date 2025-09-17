/**
 * @Author: zhangpengjian
 * @Date: 2024-03-19 19:05:10
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/8 17:17:47
 * Description: Misc节点下的布局管理
 */

using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;
using fat.conf;

namespace FAT
{
    public class MBBoardMisc : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup rewardAndDERoot;
        [SerializeField] private LayoutElement rewardAndDE;
        [SerializeField] private HorizontalLayoutGroup activityEntry;
        [SerializeField] private Transform specialEntry;

        private float activitySpacing = 15; //美术要求的活动入口之间间隙
        private int orderSpacing = 20; //活动区域和订单区域的间隔
        private int onlyScoreActPaddingTop = -35;
        private int hasActEntryPaddingTop = -78;
        private string boardEntryPrefix = "boardEntry_";
        private List<GameObject> entryPrefabs = new(); //棋盘活动入口obj列表
        private Dictionary<string, ActivityLike> entryToActivityMap = new(); //入口名称到活动对象的映射

        public void Setup()
        {
        }

        public void InitOnPreOpen()
        {
            MessageCenter.Get<MSG.ACTIVITY_ACTIVE>().AddListener(_OnActivityActive);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(_OnActivityEnd);
            MessageCenter.Get<MSG.ACTIVITY_STATE>().AddListener(_RefreshLayout);
            MessageCenter.Get<MSG.ACTIVITY_ENTRY_LAYOUT_REFRESH>().AddListener(OnRefreshLayout);
            BoardViewWrapper.GetCurrentWorld().onRewardListChange += OnMessageRewardListChange;
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().AddListener(_RefreshRewardAndDERoot);
            _FirstTimeShow();
        }

        public void CleanupOnPostClose()
        {
            MessageCenter.Get<MSG.ACTIVITY_ACTIVE>().RemoveListener(_OnActivityActive);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(_OnActivityEnd);
            MessageCenter.Get<MSG.ACTIVITY_STATE>().RemoveListener(_RefreshLayout);
            MessageCenter.Get<MSG.ACTIVITY_ENTRY_LAYOUT_REFRESH>().RemoveListener(OnRefreshLayout);
            BoardViewWrapper.GetCurrentWorld().onRewardListChange -= OnMessageRewardListChange;
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().RemoveListener(_RefreshRewardAndDERoot);
            _Cleanup();
        }

        private void _FirstTimeShow()
        {
            _RefreshActivityEntry();
            _RefreshLayout();
            _RefreshRewardAndDERoot();
        }

        private void _RefreshActivityEntry()
        {
            using (ObjectPool<List<ActivityLike>>.GlobalPool.AllocStub(out var activityLikes))
            {
                foreach (var activityList in Game.Manager.activity.index)
                    foreach (var act in activityList.Value)
                        activityLikes.Add(act);

                // 按 boardentWeight 排序，权重一致则按 EventType 排序
                activityLikes.Sort((ActivityLike a, ActivityLike b) => 
                {
                    var weightA = GetActivityBoardWeight(a);
                    var weightB = GetActivityBoardWeight(b);
                    
                    // 首先按权重排序（权重大的靠左）
                    if (weightA != weightB)
                        return weightB.CompareTo(weightA);
                    
                    // 权重一致则按 EventType 排序
                    if (a.Type != b.Type)
                        return a.Type.CompareTo(b.Type);
                    
                    // EventType 一致则按 activity.Id 排序
                    return a.Id.CompareTo(b.Id);
                });
                
                foreach (var act in activityLikes)
                    if (act is IBoardEntry e && e.BoardEntryVisible && act.Valid)
                    {
                        var prefabName = e.BoardEntryAsset();
                        if (string.IsNullOrEmpty(prefabName))
                        {
                            Debug.LogWarning(
                                $"MBBoardMisc:Try show entry,but prefabName is null act:{nameof(act.Type)}");
                            continue;
                        }

                        CreateBoardEntry(prefabName, act);
                    }
            }
        }

        /// <summary>
        /// 获取活动的棋盘入口权重
        /// </summary>
        /// <param name="activity">活动对象</param>
        /// <returns>权重值，权重越大越靠左</returns>
        private int GetActivityBoardWeight(ActivityLike activity)
        {
            if (activity == null) return 0;
            // 积分活动特殊处理，给予最高权重确保在最左边
            if (activity.Type == EventType.Score)
                return int.MaxValue;
                
            // 从配置中获取权重
            var eventTypeInfo = EventTypeInfoVisitor.GetOneByFilter(info => info.EventType == activity.Type);
            return eventTypeInfo?.BoardEntWeight ?? 0;
        }

        private void _Cleanup()
        {
            _TryClearEntryPrefab();
        }

        private string _GetBoardEntryKey(string entryName, ActivityLike activity)
        {
            return boardEntryPrefix + $"{entryName[0]}_{activity.Id}";
        }

        private void OnRefreshLayout()
        {
            _RefreshLayout();
        }

        private void _RefreshLayout(ActivityLike act = null)
        {
            Game.Manager.activity.LookupAny(EventType.Score, out var scoreAct);
            var score = (ActivityScore)scoreAct;
            var hasScoreEntry = false;
            if (score is { Valid: true } && score.HasCycleMilestone() && score is IBoardEntry e)
            {
                var asset = e.BoardEntryAsset();
                var name = asset.Split(".");
                var path = name[0].Split("#");
                var scoreName = _GetBoardEntryKey(path[1], score);
                if (activityEntry.transform.Find(scoreName) != null) hasScoreEntry = true;
            }

            if (hasScoreEntry)
            {
                activityEntry.padding.top = activityEntry.transform.childCount > 1
                    ? hasActEntryPaddingTop
                    : onlyScoreActPaddingTop;
                if (transform.GetComponent<HorizontalLayoutGroup>() != null)
                    DestroyImmediate(transform.GetComponent<HorizontalLayoutGroup>());
                var originComp = transform.GetComponent<VerticalLayoutGroup>();
                if (originComp == null)
                {
                    var comp = transform.gameObject.AddComponent<VerticalLayoutGroup>();
                    if (comp != null)
                    {
                        comp.spacing = activitySpacing;
                        comp.childForceExpandWidth = false;
                        comp.reverseArrangement = true;
                        comp.padding.right = orderSpacing;
                    }
                }
                else
                {
                    originComp.spacing = activitySpacing;
                }

                rewardAndDERoot.constraintCount = 2;
            }
            else
            {
                if (transform.GetComponent<VerticalLayoutGroup>() != null)
                    DestroyImmediate(transform.GetComponent<VerticalLayoutGroup>());
                var originComp = transform.GetComponent<HorizontalLayoutGroup>();
                if (originComp == null)
                {
                    var comp = transform.gameObject.AddComponent<HorizontalLayoutGroup>();
                    if (comp != null)
                    {
                        comp.childForceExpandWidth = false;
                        comp.spacing = activitySpacing;
                        comp.reverseArrangement = true;
                        comp.padding.right = orderSpacing;
                    }
                }
                else
                {
                    originComp.spacing = activitySpacing;
                }

                rewardAndDERoot.constraintCount = 1;
            }

            activityEntry.gameObject.SetActive(activityEntry.transform.childCount > 0);
            _RefreshRewardAndDERoot();
            //比较特殊的零度挑战类似订单样式的活动入口
            Game.Manager.activity.LookupAny(EventType.ZeroQuest, out var orderChaAct);
            var orderCha = (ActivityOrderChallenge)orderChaAct;
            specialEntry.GetComponent<HorizontalLayoutGroup>().padding.right =
                orderCha is { Valid: true } && !orderCha.IsOver() ? 10 : 0;
                
            // 重新排序活动入口
            ReorderActivityEntries();
        }

        private void _RefreshRewardAndDERoot(int _ = 0)
        {
            var world = BoardViewWrapper.GetCurrentWorld();
            var rewardCount = world.rewardCount;
            var de = Game.Manager.dailyEvent;
            var valid = de.Valid && de.Unlocked;
            if (valid || rewardCount > 0)
                rewardAndDE.ignoreLayout = false;
            else
                rewardAndDE.ignoreLayout = true;
        }

        private void OnMessageRewardListChange(bool _)
        {
            _RefreshRewardAndDERoot();
        }

        private void _OnActivityActive(ActivityLike act, bool isNew)
        {
            if (act is IBoardEntry e)
            {
                if (!e.BoardEntryVisible || !act.Valid) return;
                var key = e.BoardEntryAsset();
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogWarning($"MBBoardMisc:Try show entry,but prefabName is null act:{nameof(act.Type)}");
                    return;
                }

                if (_HasEntry(key, act)) 
                {
                    // 活动入口已存在，但仍然需要重新排序（可能权重配置发生了变化）
                    ReorderActivityEntries();
                    return;
                }
                CreateBoardEntry(key, act);
            }
        }

        private bool _HasEntry(string entry, ActivityLike act)
        {
            if (entryPrefabs.Count <= 0) return false;
            var asset = entry.ConvertToAssetConfig();
            var entryName = asset.Asset.Split(".");
            foreach (var prefab in entryPrefabs)
                if (prefab.name == _GetBoardEntryKey(entryName[0], act))
                    return true;

            return false;
        }

        private void _OnActivityEnd(ActivityLike act, bool expire)
        {
            if (entryPrefabs.Count <= 0) return;
            if (act is IBoardEntry e)
            {
                var name = e.BoardEntryAsset();
                if (string.IsNullOrEmpty(name)) return;
                var asset = name.ConvertToAssetConfig();
                var entryName = asset.Asset.Split(".");
                for (var i = 0; i < entryPrefabs.Count; i++)
                    if (entryPrefabs[i].name == _GetBoardEntryKey(entryName[0], act))
                    {
                        var key = _GetBoardEntryKey(entryName[0], act);
                        GameObjectPoolManager.Instance.ReleaseObject(key, entryPrefabs[i]);
                        entryPrefabs.RemoveAt(i);
                        // 清理映射关系
                        entryToActivityMap.Remove(key);
                    }
            }
            
            // 活动结束后重新排序
            ReorderActivityEntries();
        }

        private void _TryClearEntryPrefab()
        {
            if (entryPrefabs.Count <= 0) return;
            foreach (var go in entryPrefabs)
            {
                GameObjectPoolManager.Instance.ReleaseObject(go.name, go);
            }

            entryPrefabs.Clear();
            entryToActivityMap.Clear();
        }

        private void CreateBoardEntry(string prefabName, ActivityLike activity)
        {
            var asset = prefabName.ConvertToAssetConfig();
            var entryName = asset.Asset.Split(".");
            if (GameObjectPoolManager.Instance.HasPool(_GetBoardEntryKey(entryName[0], activity)))
                GenerateEntry(entryName[0], activity);
            else
                StartCoroutine(CoLoadEntryPrefab(prefabName, activity));
        }

        private IEnumerator CoLoadEntryPrefab(string prefabName, ActivityLike activity)
        {
            var asset = prefabName.ConvertToAssetConfig();
            var loader = EL.Resource.ResManager.LoadAsset<GameObject>(asset.Group, asset.Asset);
            yield return loader;
            if (!loader.isSuccess)
                DebugEx.Error($"MBBoardMisc::CoLoadEntryPrefab ----> loading res error {loader.error}");

            var assetName = loader.asset.name;
            var key = _GetBoardEntryKey(assetName, activity);
            GameObjectPoolManager.Instance.PreparePool(key, loader.asset as GameObject);
            GenerateEntry(assetName, activity);
        }

        private void GenerateEntry(string assetName, ActivityLike activity)
        {
            var prefabName = _GetBoardEntryKey(assetName, activity);
            var entry = GameObjectPoolManager.Instance.CreateObject(prefabName);
            if (activity.Type == EventType.ZeroQuest)
            {
                entry.transform.SetParent(specialEntry.transform);
            }
            else
            {
                entry.transform.SetParent(activityEntry.transform);
            }
            entry.transform.localPosition = Vector3.zero;
            entry.transform.localScale = Vector3.one;
            entry.name = prefabName;
            entry.gameObject.SetActive(true);
            
            // 维护映射关系
            entryToActivityMap[prefabName] = activity;
            
            // 获取并刷新Entry组件
            var entryComponent = entry.GetComponent<IActivityBoardEntry>();
            if (entryComponent != null)
            {
                entryComponent.RefreshEntry(activity);
            }
            else
            {
                DebugEx.Warning($"Entry组件未实现IActivityBoardEntry接口: {activity.Type}");
            }
            entryPrefabs.Add(entry);
            
            // 重新排序所有活动入口
            ReorderActivityEntries();
            _RefreshLayout();
            
            // 红点位置适配
            entry.GetComponentInChildren<MBDotPosFit>(true)?.FitPos(entry);
        }

        /// <summary>
        /// 重新排序所有活动入口
        /// 按 boardentWeight 排序，权重一致则按 EventType 排序
        /// </summary>
        private void ReorderActivityEntries()
        {
            // 获取所有活动入口并排序
            var activityEntries = new List<(Transform entry, ActivityLike activity)>();
            
            // 收集 activityEntry 下的所有入口
            for (int i = 0; i < activityEntry.transform.childCount; i++)
            {
                var child = activityEntry.transform.GetChild(i);
                var activity = GetActivityFromEntry(child);
                if (activity != null)
                {
                    activityEntries.Add((child, activity));
                }
            }
            
            // 按权重排序
            activityEntries.Sort((a, b) => 
            {
                var weightA = GetActivityBoardWeight(a.activity);
                var weightB = GetActivityBoardWeight(b.activity);
                
                // 首先按权重排序（权重大的靠左）
                if (weightA != weightB)
                    return weightB.CompareTo(weightA);
                
                // 权重一致则按 EventType 排序
                if (a.activity.Type != b.activity.Type)
                    return a.activity.Type.CompareTo(b.activity.Type);
                
                // EventType 一致则按 activity.Id 排序
                return a.activity.Id.CompareTo(b.activity.Id);
            });
            
            // 重新设置顺序
            for (int i = 0; i < activityEntries.Count; i++)
            {
                activityEntries[i].entry.SetSiblingIndex(i);
            }
            
            // 对 specialEntry 也进行相同的排序
            var specialEntries = new List<(Transform entry, ActivityLike activity)>();
            for (int i = 0; i < specialEntry.transform.childCount; i++)
            {
                var child = specialEntry.transform.GetChild(i);
                var activity = GetActivityFromEntry(child);
                if (activity != null)
                {
                    specialEntries.Add((child, activity));
                }
            }
            
            specialEntries.Sort((a, b) => 
            {
                var weightA = GetActivityBoardWeight(a.activity);
                var weightB = GetActivityBoardWeight(b.activity);
                
                if (weightA != weightB)
                    return weightB.CompareTo(weightA);
                
                if (a.activity.Type != b.activity.Type)
                    return a.activity.Type.CompareTo(b.activity.Type);
                
                // EventType 一致则按 activity.Id 排序
                return a.activity.Id.CompareTo(b.activity.Id);
            });
            
            for (int i = 0; i < specialEntries.Count; i++)
            {
                specialEntries[i].entry.SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// 从入口对象中获取对应的活动对象
        /// </summary>
        /// <param name="entry">入口Transform</param>
        /// <returns>对应的活动对象，如果找不到则返回null</returns>
        private ActivityLike GetActivityFromEntry(Transform entry)
        {
            var entryName = entry.name;
            if (string.IsNullOrEmpty(entryName) || !entryName.StartsWith(boardEntryPrefix))
                return null;
                
            // 直接从映射中获取活动对象
            return entryToActivityMap.TryGetValue(entryName, out var activity) ? activity : null;
        }
    }
}