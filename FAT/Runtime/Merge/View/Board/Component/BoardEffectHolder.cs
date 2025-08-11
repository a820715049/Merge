/*
 * @Author: qun.chao
 * @Date: 2022-09-01 15:38:57
 */
namespace FAT
{
    using System.Collections.Generic;
    using UnityEngine;
    using FAT.Merge;

    public class BoardEffectHolder
    {
        private Transform effectRoot;
        private Dictionary<Vector2Int, Dictionary<string, GameObject>> stateEffectDict = new Dictionary<Vector2Int, Dictionary<string, GameObject>>();
        private string effect_prefix;

        public void Setup(Transform root)
        {
            effectRoot = root;
            effect_prefix = $"board_{BoardViewManager.Instance.board.boardId}_";
        }

        public void Cleanup()
        {
            _ClearStateEffect();
            BoardUtility.ReleaseAutoPoolItemFromChildren(effectRoot);
        }

        public GameObject AddInstantEffect(Vector2Int coord, string poolKey, float lifeTime)
        {
            var go = GameObjectPoolManager.Instance.CreateObject(poolKey, effectRoot);
            BoardUtility.PlaceItemToBoardCoord(go, coord);
            BoardUtility.AddAutoReleaseComponent(go, lifeTime, poolKey);
            go.SetActive(true);
            return go;
        }

        public GameObject GetInstantEffectByLoadAndPrepareItem(Vector2Int coord, string effConfig, float lifeTime)
        {
            BoardUtility.LoadAndPreparePoolItem(effConfig, effConfig);
            return AddInstantEffect(coord, effConfig, lifeTime);
        }

        public void AddStateEffect(Vector2Int coord, string effConfig)
        {
            var key = _GetEffectKey(effConfig);
            BoardUtility.LoadAndPreparePoolItem(effConfig, key);
            // 先提供容器
            if (!stateEffectDict.ContainsKey(coord))
            {
                stateEffectDict.Add(coord, new Dictionary<string, GameObject>());
            }
            var holder = stateEffectDict[coord];
            if (holder.ContainsKey(key))
            {
                return;
            }
            // 创建特效
            var eff = GameObjectPoolManager.Instance.CreateObject(key, effectRoot);
            BoardUtility.PlaceItemToBoardCoord(eff, coord);
            holder.Add(key, eff);
        }

        public void RemoveStateEffect(Vector2Int coord, string effConfig)
        {
            if (!stateEffectDict.ContainsKey(coord))
                return;
            var key = _GetEffectKey(effConfig);
            var holder = stateEffectDict[coord];
            if (!holder.ContainsKey(key))
                return;
            GameObjectPoolManager.Instance.ReleaseObject(key, holder[key]);
            holder.Remove(key);
        }

        private string _GetEffectKey(string effConfig)
        {
            return effect_prefix + effConfig.ConvertToAssetConfig().Asset;
        }

        private void _ClearStateEffect()
        {
            foreach (var effects in stateEffectDict.Values)
            {
                foreach (var kv in effects)
                {
                    GameObjectPoolManager.Instance.ReleaseObject(kv.Key, kv.Value);
                }
                effects.Clear();
            }
            stateEffectDict.Clear();
        }
    }
}
