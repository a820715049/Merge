/*
 * @Author: qun.chao
 * @Date: 2022-10-09 17:27:45
 */

using UnityEngine;

namespace FAT
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;
    using Merge;

    public class MBItemResHolder : MonoBehaviour
    {
        private GameObject mContentPrefab;
        private MBResHolderBase mResHolder;
        private Action<GameObject> mOnLoadCallback;
        private int itemId;
        private Item mItem;
        private Transform itemRoot;
        private Spine.Unity.SkeletonGraphic mSpineGraphic;
        public MBResHolderBase ResHolder => mResHolder;
        
        public void LoadRes(int tid, Item item, Transform root)
        {
            itemId = tid;
            mItem = item;
            itemRoot = root;
            var mgCfg = Env.Instance.GetItemMergeConfig(tid);
            StartCoroutine(_CoLoadRes(mgCfg.DisplayRes, _GetDisplayPrefabKey(tid), tid));
        }

        public void ClearRes()
        {
            _TryReleaseDisplayPrefab();
            if (mResHolder != null)
                mResHolder.OnClear();
            mResHolder = null;
            itemId = -1;
            mItem = null;
        }

        public void SetOnLoadAction(Action<GameObject> callback)
        {
            if (mContentPrefab != null)
                callback?.Invoke(mContentPrefab);
            else
                mOnLoadCallback = callback;
        }

        public void SetAlpha(float a)
        {
            if (mSpineGraphic != null)
            {
                var color = mSpineGraphic.color;
                color.a = a;
                mSpineGraphic.color = color;
            }
        }

        private string _GetDisplayPrefabKey(int tid)
        {
            return $"board_item_prefab_{tid}";
        }

        private void _AddDisplayPrefab(int tid)
        {
            if (tid != itemId)
                return;
            if (mContentPrefab != null)
                return;
            if (mItem == null)
                return;
            var prefab = GameObjectPoolManager.Instance.CreateObject(_GetDisplayPrefabKey(tid));
            prefab.transform.SetParent(itemRoot);
            prefab.transform.SetAsFirstSibling();
            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localScale = Vector3.one;
            mSpineGraphic = prefab.GetComponentInChildren<Spine.Unity.SkeletonGraphic>();
            mContentPrefab = prefab;
            if (prefab.TryGetComponent(out mResHolder))
            {
                mResHolder.OnInit(mItem);
            }
            if (mOnLoadCallback != null)
                mOnLoadCallback.Invoke(prefab);
            else
                SetBoard(prefab);
            mOnLoadCallback = null;
        }

        public static void SetReward(GameObject go)
        {
            if (go.TryGetComponent<MBResHolderBase>(out var resHolder))
                resHolder.SetRewardState();
        }

        public static void SetBoard(GameObject go)
        {
            if (go.TryGetComponent<MBResHolderBase>(out var resHolder))
                resHolder.SetBoardState();
        }

        public static void SetBorn(GameObject go)
        {
            if (go.TryGetComponent<MBResHolderBase>(out var resHolder))
                resHolder.SetBornState();
        }

        private void _TryReleaseDisplayPrefab()
        {
            if (mContentPrefab != null && itemId > 0)
            {
                GameObjectPoolManager.Instance.ReleaseObject(_GetDisplayPrefabKey(itemId), mContentPrefab);
                mContentPrefab = null;
                mSpineGraphic = null;
            }

            mOnLoadCallback = null;
        }

        private IEnumerator _CoLoadRes(string resConfig, string poolKey, int tid)
        {
            if (GameObjectPoolManager.Instance.HasPool(poolKey))
            {
                _AddDisplayPrefab(tid);
                yield break;
            }

            var res = resConfig.ConvertToAssetConfig();
            var task = EL.Resource.ResManager.LoadAsset<GameObject>(res.Group, res.Asset);
            yield return task;
            if (task.isSuccess && task.asset != null)
            {
                GameObjectPoolManager.Instance.PreparePool(poolKey, task.asset as GameObject);
                _AddDisplayPrefab(tid);
            }
        }
    }
}