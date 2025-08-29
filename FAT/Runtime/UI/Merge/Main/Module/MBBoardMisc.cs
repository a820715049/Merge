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

                activityLikes.Sort((ActivityLike a, ActivityLike b) => a.Type - b.Type);
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

                if (_HasEntry(key, act)) return;
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
                        GameObjectPoolManager.Instance.ReleaseObject(_GetBoardEntryKey(entryName[0], act), entryPrefabs[i]);
                        entryPrefabs.RemoveAt(i);
                    }
            }
        }

        private void _TryClearEntryPrefab()
        {
            if (entryPrefabs.Count <= 0) return;
            foreach (var go in entryPrefabs)
            {
                GameObjectPoolManager.Instance.ReleaseObject(go.name, go);
            }

            entryPrefabs.Clear();
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
            //积分活动为扁长条 需要为ActivityEntry节点下第一个 位于最左边 其他则按EventType排序 符合动态加载前的排列规则
            if (activity.Type == EventType.Score)
            {
                entry.transform.SetSiblingIndex(0);
            }
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
            _RefreshLayout();
            
            // 红点位置适配
            entry.GetComponentInChildren<MBDotPosFit>()?.FitPos(entry);
        }
    }
}