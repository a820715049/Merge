/*
 * @Author: qun.chao
 * @Date: 2021-05-28 10:30:24
 */
using UnityEngine;
using FAT.Merge;
using DG.Tweening;

namespace FAT
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/FatBoardRes", order = 1)]
    public class BoardRes : ScriptableObject
    {
        [System.Serializable]
        public class SpawnPopParam
        {
            [Tooltip("启动延迟")]
            public float startDelay;
            [Tooltip("飞行100单位需要花费的时间")]
            public float flyDurationPer100;
            [Tooltip("飞行时间的额外加成")]
            public float flyDurationExtra;
            [Tooltip("落地前多久开始播放下落动画")]
            public float flyEndDropTimeOffset;
            /*[Tooltip("是否启用动态飞行时间")]*/
            /*public bool isDynamicFlyDuration;*/
            /*[Tooltip("飞行时长")]*/
            /*public float flyDuration;*/
            [Tooltip("飞行中点y偏移")]
            public float flyMidOffsetY;
            [Tooltip("飞行落点偏移距离")]
            public float flyEndOffsetDist;
            [Tooltip("飞行节奏曲线")]
            public Ease flyEase = Ease.Linear;
            [Tooltip("落地漂移时长")]
            public float moveDuration;
            [Tooltip("落地漂移节奏曲线")]
            public Ease moveEase = Ease.Linear;
        }

        #region board param
        public float itemPopDuration = 0.4f;
        public bool isConstantPopDuration = false;
        public bool snapToFinger = true;
        #endregion

        public Sprite frozenCoverSprite;
        public Sprite chestOpenTimerSprite;
        public Sprite bubbleCoverSprite;
        public Sprite bubbleFrozenCoverSprite;
        public Sprite bottomSprite;
        public Sprite cloudSprite;
        public SpawnPopParam spawnPopParam;

        [NamedArrayAttribute(typeof(ItemEffectType))]
        public GameObject[] effectHolder;

        public string TapLockedSound;

        public void Install(int bid)
        {
            BoardUtility.itemPopDuration = itemPopDuration;
            BoardUtility.isConstantPopDuration = isConstantPopDuration;
            BoardUtility.snapToFinger = snapToFinger;

            // BoardUtility.SetBoxSprite(boxSprite);
            BoardUtility.SetFrozenCoverSprite(frozenCoverSprite);
            BoardUtility.SetBubbleCoverSprite(bubbleCoverSprite);
            BoardUtility.SetBubbleFrozenCoverSprite(bubbleFrozenCoverSprite);
            BoardUtility.SetBottomSprite(bottomSprite);
            BoardUtility.SetSpawnPopParam(spawnPopParam);

            // 如有必要 可以清除后再全新载入此配置对应的特效
            for (int i = 0; i < effectHolder.Length; ++i)
            {
                // 与棋盘图片强相关的特效 需要清除
                if (i == (int)ItemEffectType.UnlockNormal || i == (int)ItemEffectType.UnlockLevel || i == (int)ItemEffectType.UnFrozen || i == (int)ItemEffectType.TapLocked
                    || i == (int)ItemEffectType.TrigAutoSource)
                {
                    var key = BoardUtility.EffTypeToPoolType((ItemEffectType)i);
                    GameObjectPoolManager.Instance.ClearPool(key);

                    // 目前的需求是仅对开箱特效区分其在不同棋盘上的效果 故此处仅清空开箱特效
                    // var cfg = Game.Manager.mergeBoardMan.GetBoardConfig(bid);
                    // if (!string.IsNullOrEmpty(cfg.BoxUnlockEffect))
                    // {
                    //     // 替代该棋盘的开箱特效
                    //     BoardUtility.LoadAndPreparePoolItem(cfg.BoxUnlockEffect, key.ToString());
                    //     // 如果资源缺失 pool可能仍然不存在
                    //     if (GameObjectPoolManager.Instance.HasPool(key.ToString()))
                    //     {
                    //         continue;
                    //     }
                    // }
                }

                var pt = BoardUtility.EffTypeToPoolType((ItemEffectType)i);
                GameObjectPoolManager.Instance.PreparePool(pt, effectHolder[i]);
            }
        }
    }
}