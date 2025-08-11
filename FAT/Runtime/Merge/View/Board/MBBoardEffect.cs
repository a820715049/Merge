/*
 * @Author: qun.chao
 * @Date: 2021-02-23 18:51:48
 */

using System.Collections.Generic;
using TMPro;

namespace FAT
{
    using System.Collections;
    using UnityEngine;
    using FAT.Merge;
    using DG.Tweening;

    public class MBBoardEffect : MonoBehaviour, IMergeBoard
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private RectTransform highlightEff;
        [SerializeField] private RectTransform inventoryInd;
        [SerializeField] private RectTransform inventoryFeedback;
        [SerializeField] private RectTransform highlightEff_AB;
        private int width;
        private int height;
        private BoardEffectHolder holder = new BoardEffectHolder();
        private RectTransform highlightEffect
        {
            get
            {
                return highlightEff_AB;
            }
        }

        void IMergeBoard.Init()
        { }

        void IMergeBoard.Setup(int width, int height)
        {
            this.width = width;
            this.height = height;

            HideHighlight();
            HideInventoryInd();
            inventoryFeedback.gameObject.SetActive(false);

            holder.Setup(effectRoot);
        }

        void IMergeBoard.Cleanup()
        {
            holder.Cleanup();
        }

        public void ShowHighlight(Vector2 pos)
        {
            highlightEffect.gameObject.SetActive(true);
            highlightEffect.anchoredPosition = pos;
        }

        public void HideHighlight()
        {
            highlightEffect.gameObject.SetActive(false);
        }
        
        public void ShowScoreAnim(Vector2Int pos, string prefab, ScoreEntity.ScoreFlyRewardData r)
        {
            //3s自动回池
            var go = holder.GetInstantEffectByLoadAndPrepareItem(pos, prefab,3f);
            go.GetComponent<MbFlyScoreItem>().FlyScore(r);
        }

        public void ShowInventoryInd(Vector2 screenPos)
        {
            var eff = inventoryInd;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(eff.parent as RectTransform, screenPos, null, out var lp);
            eff.anchoredPosition = lp;
            eff.gameObject.SetActive(true);
        }

        public void ShowInventoryPutInEffect(Vector2 screenPos)
        {
            var eff = inventoryFeedback;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(eff.parent as RectTransform, screenPos, null, out var lp);
            eff.anchoredPosition = lp;
            eff.gameObject.SetActive(false);
            eff.gameObject.SetActive(true);
        }

        public void HideInventoryInd()
        {
            inventoryInd.gameObject.SetActive(false);
        }

        public void AddStateEffect(Vector2Int coord, string effConfig)
        {
            holder.AddStateEffect(coord, effConfig);
        }

        public void RemoveStateEffect(Vector2Int coord, string effConfig)
        {
            holder.RemoveStateEffect(coord, effConfig);
        }

        public void ShowTapLockedEffect(Vector2Int coord)
        {
            _ShowEffect(coord, ItemEffectType.TapLocked, 3f);
        }

        public void ShowUnlockNormalEffect(Vector2Int coord)
        {
            _ShowEffect(coord, ItemEffectType.UnlockNormal, 3f);
        }

        public void ShowUnlockLevelEffect(Vector2Int coord, int level)
        {
            // 锁定item所在位置 不参与合成提示
            BoardViewManager.Instance.SetCheckerMask(coord);

            var go = holder.AddInstantEffect(coord, BoardUtility.EffTypeToPoolType(ItemEffectType.UnlockLevel).ToString(), 6f);
            var eff = go.GetComponent<MBBoardEffect_LevelUnlock>();

            // 特效完成后解锁
            eff.Setup(level, () => BoardViewManager.Instance.UnsetCheckerMask(coord));
        }

        public void ShowOrderBoxDieEffect(Vector2Int coord, int tid)
        {
            var go = holder.AddInstantEffect(coord, BoardUtility.EffTypeToPoolType(ItemEffectType.OrderBoxOpen).ToString(), 4f);
            var eff = go.GetComponent<MBBoardEffect_OrderBoxDie>();
            eff.Setup(tid);
        }

        public void ShowMergeEffect(Vector2Int coord)
        {
            _ShowEffect(coord, ItemEffectType.OnMerge, 3f);
        }

        public void ShowCollectFeedback(Vector2Int coord)
        {
            _ShowEffect(coord, ItemEffectType.OnCollect, 3f);
        }

        public void ShowMagicHourHitEffect(Vector2Int coord)
        {
            _ShowEffect(coord, ItemEffectType.MagicHourHit, 3f);
        }

        public void ShowJumpCDEffect(Vector2Int from, Vector2Int to, Item target, float delay)
        {
            var effRoot = BoardViewManager.Instance.boardView.topEffectRoot;
            var effType = BoardUtility.EffTypeToPoolType(ItemEffectType.JumpCDTrail).ToString();
            var goTrail = GameObjectPoolManager.Instance.CreateObject(effType, effRoot);
            BoardUtility.AddAutoReleaseComponent(goTrail, 2f, effType);

            goTrail.SetActive(false);
            goTrail.transform.position = BoardUtility.GetWorldPosByCoord(from);
            // goTrail.SetActive(true);
            var seq = DOTween.Sequence();
            seq.AppendInterval(delay);
            seq.AppendCallback(() => { goTrail.SetActive(true); });
            seq.Append(goTrail.transform.DOMove(BoardUtility.GetWorldPosByCoord(to), 0.5f).SetEase(Ease.Linear));
            seq.AppendCallback(() =>
            {
                BoardViewManager.Instance.GetItemView(target.id).RefreshJumpCdState();
                var effType_Disp = BoardUtility.EffTypeToPoolType(ItemEffectType.JumpCDDisappear).ToString();
                var disappear = GameObjectPoolManager.Instance.CreateObject(effType_Disp, effRoot);
                disappear.transform.position = BoardUtility.GetWorldPosByCoord(to);
                BoardUtility.AddAutoReleaseComponent(disappear, 3f, effType_Disp);
            });
            seq.Play();
        }

        // public void ShowSellTip(Vector2Int coord)
        // {
        //     _ShowEffect(coord, ItemEffectType.Sell_Tip, 1f);
        // }

        // public void ShowSpeedUpTip(Vector2Int coord)
        // {
        //     _ShowEffect(coord, ItemEffectType.SpeedUp_Tip, 1f);
        // }

        public GameObject ShowInstantEffect(Vector2Int coord, string key, float lifeTime)
        {
            return holder.AddInstantEffect(coord, key, lifeTime);
        }

        private void _ShowEffect(Vector2Int coord, ItemEffectType et, float lifeTime)
        {
            holder.AddInstantEffect(coord, BoardUtility.EffTypeToPoolType(et).ToString(), lifeTime);
        }

        #region time skip

        public void UseTimeSkipper(Vector2Int coord)
        {
            // pause board
            BoardViewManager.Instance.SetPause(true);

            // // play effect
            // var cc = StartCoroutine(_CoPlayTimeSkip(coord));
            // UIUtility.RunModalTask(cc, _OnUseTimeSkipperCallback, true);
        }

        private IEnumerator _CoPlayTimeSkip(Vector2Int coord)
        {
            var maxRange = Mathf.Max(width, height);
            for (int r = 1; r < maxRange; ++r)
            {
                _ApplyTimeSkipToAffectedItem(r, coord.x, coord.y);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void _ApplyTimeSkipToAffectedItem(int r, int c_x, int c_y)
        {
            var container = BoardViewManager.Instance.GetBoardItemHolder();
            var board = BoardViewManager.Instance.board;

            for (int x = c_x - r, y = c_y - r; x < c_x + r; ++x)
            {
                _SetItemTimeSkip(board.GetItemByCoord(x, y), container);
            }

            for (int x = c_x + r, y = c_y - r; y < c_y + r; ++y)
            {
                _SetItemTimeSkip(board.GetItemByCoord(x, y), container);
            }

            for (int x = c_x + r, y = c_y + r; x > c_x - r; --x)
            {
                _SetItemTimeSkip(board.GetItemByCoord(x, y), container);
            }

            for (int x = c_x - r, y = c_y + r; y > c_y - r; --y)
            {
                _SetItemTimeSkip(board.GetItemByCoord(x, y), container);
            }
        }

        private void _SetItemTimeSkip(Item item, MBBoardItemHolder holder)
        {
            if (item != null)
            {
                _ShowEffect(item.coord, ItemEffectType.TimeSkip, 0.2f);
                holder.TimeSkipItem(item.id);
            }
        }

        private void _PlaceItemToCoord(GameObject go, Vector2Int coord)
        {
            (go.transform as RectTransform).anchoredPosition = new Vector2((coord.x + 0.5f) * BoardUtility.cellSize, -(coord.y + 0.5f) * BoardUtility.cellSize);
        }

        private void _OnUseTimeSkipperCallback()
        {
            // resume board
            BoardViewManager.Instance.SetPause(false);
        }

        #endregion
    }
}
