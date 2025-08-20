/*
 * @Author: qun.chao
 * @Date: 2025-03-03 18:25:07
 */
using System;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using Config;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class MBBingoBoard : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private Transform effectRoot;
        [SerializeField] private GameObject effPrefab;
        [SerializeField] private GameObject goNextLevel;
        [SerializeField] private GameObject goCongrat;
        [SerializeField] private GameObject goBlock;
        [SerializeField] private float time_wait_congrat = 0.5f;
        [SerializeField] private float time_wait_item = 0.5f;
        [SerializeField] private float time_wait_refresh = 0.5f;

        private UIBingoMain uiMain;
        private ActivityBingo actInst => uiMain.ActInst;
        private string pool_key_item => $"bingo_board_item_{actInst.Id}";
        private string pool_key_item_eff => $"bingo_item_eff_{actInst.Id}";
        private int width;
        private int height;
        private (int, int)[] spread_dir_round = new (int, int)[] { (-1, 1), (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0) };
        private CancellationTokenSource cts;

        public void InitOnPreOpen(UIBingoMain main)
        {
            uiMain = main;
            EnsurePool();
            goNextLevel.SetActive(false);
            goCongrat.SetActive(false);
            goBlock.SetActive(false);
        }

        public void CleanupOnPostClose()
        {
            if (cts != null && goBlock.activeSelf)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
            goBlock.SetActive(false);
            goCongrat.SetActive(false);

            BoardUtility.ReleaseAutoPoolItemFromChildren(effectRoot);
            ClearItem();
            width = 0;
            height = 0;
        }

        public void Refresh()
        {
            EnsureBoard();
            RefreshBoard();
        }

        private void EnsurePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(pool_key_item))
                return;
            GameObjectPoolManager.Instance.PreparePool(pool_key_item, itemPrefab);
            GameObjectPoolManager.Instance.PreparePool(pool_key_item_eff, effPrefab);
        }

        private void ClearItem()
        {
            for (var i = root.childCount - 1; i >= 0; --i)
            {
                var item = root.GetChild(i);
                GameObjectPoolManager.Instance.ReleaseObject(pool_key_item, item.gameObject);
            }
        }

        private void EnsureBoard()
        {
            var cfg = fat.conf.Data.GetItemBingoBoard(actInst.ConfBoardID);
            if (width == cfg.BoardColNum && height == cfg.BoardRowNum)
                return;
            ClearItem();
            width = cfg.BoardColNum;
            height = cfg.BoardRowNum;
            for (var i = 1; i <= width; ++i)
            {
                for (var j = 1; j <= height; ++j)
                {
                    var item = GameObjectPoolManager.Instance.CreateObject(pool_key_item, root);
                    item.transform.localScale = Vector3.one;
                    item.name = $"item_{i}_{j}";
                    item.GetComponent<MBBingoBoardItem>().Bind(OnClickItem);
                }
            }
        }

        private void OnClickItem(BingoItem item)
        {
            actInst.ItemRes.ActiveR.Open(actInst, item, (Action)(() => TryCommitItem(item)));
        }

        private void TryCommitItem(BingoItem item)
        {
            if (item.IsClaimed)
                return;
            cts ??= new CancellationTokenSource();
            AsyncCommitItem(item, cts.Token).Forget();
        }

        private async UniTask AsyncCommitItem(BingoItem item, CancellationToken token)
        {
            goBlock.SetActive(true);

            var result = actInst.CompleteBingo(item, out var rewardCommitDatas, out var enterNextBoard, out var enterNextRound);

            await PlayResult(result, item, token).AttachExternalCancellation(token);

            var itemTrans = GetItem(item.CoordX, item.CoordY);
            UIFlyUtility.FlyRewardList(rewardCommitDatas, itemTrans.transform.position);

            if (enterNextRound)
            {
                await UniTask.WaitForSeconds(1f).AttachExternalCancellation(token);
                uiMain.MoveToNextRound();
                goBlock.SetActive(false);
                return;
            }

            if (enterNextBoard)
            {
                Game.Manager.audioMan.TriggerSound("BingoLevelRefresh");
                await Effect_NextLevel(token).AttachExternalCancellation(token);
                await uiMain.RefreshForNextLevel().AttachExternalCancellation(token);
            }

            RefreshBoard();
            goBlock.SetActive(false);
        }

        private async UniTask PlayResult(ItemBingoState state, BingoItem item, CancellationToken token)
        {
            Effect_Congrat(state, item, token).Forget();

            // 自身播放效果
            var mbItem = GetItem(item.CoordX, item.CoordY);
            mbItem.PlayConvert(item);
            Effect_Star(mbItem.transform.position);

            // 播放扩散效果
            await UniTask.WaitForSeconds(time_wait_item);

            if (state.HasFlag(ItemBingoState.FullHouse))
            {
                Game.Manager.audioMan.TriggerSound("BingoLevelComplete");
                Effect_FullHouse(state, item, token).Forget();
                // 等待一个延迟时间
                await UniTask.WaitForSeconds(time_wait_refresh).AttachExternalCancellation(token);
            }
            else if (item.HasBingo)
            {
                // 提交触发bingo
                Effect_Spread(state, item, token).Forget();
                // 根据提交位置等待一个差不多的时间
                var dist_x = Mathf.Max(Mathf.Abs(item.CoordX - width), Mathf.Abs(item.CoordX - 1));
                var dist_y = Mathf.Max(Mathf.Abs(item.CoordY - height), Mathf.Abs(item.CoordY - 1));
                var dist = Mathf.Max(dist_x, dist_y);
                await UniTask.WaitForSeconds(dist * time_wait_item).AttachExternalCancellation(token);
            }
        }

        private async UniTask Effect_NextLevel(CancellationToken token)
        {
            goNextLevel.SetActive(true);
            var min = 1 + 1;
            var max = width + height;
            for (var x_plus_y = min; x_plus_y <= max; x_plus_y++)
            {
                for (var x = 1; x <= x_plus_y - 1; x++)
                {
                    var y = x_plus_y - x;
                    if (x < 1 || x > width || y < 1 || y > height)
                        continue;
                    var item = GetItem(x, y);
                    var data = GetItemData(x, y);
                    item.RefreshItem(data);
                    item.PlayHide();
                }
                await UniTask.WaitForSeconds(time_wait_item).AttachExternalCancellation(token);
            }
        }

        private async UniTask Effect_Congrat(ItemBingoState state, BingoItem item, CancellationToken token)
        {
            // 如果有bingo 则播放庆祝效果
            if (item.HasBingo)
            {
                Game.Manager.audioMan.TriggerSound("BingoCelebrate");
                goCongrat.SetActive(false);
                await UniTask.WaitForSeconds(time_wait_congrat).AttachExternalCancellation(token);
                goCongrat.SetActive(true);
            }
        }

        private async UniTask Effect_FullHouse(ItemBingoState state, BingoItem itemData, CancellationToken token)
        {
            var orig_x = itemData.CoordX;
            var orig_y = itemData.CoordY;
            for (var dist = 1; dist < width; dist++)
            {
                int x, y;
                for (x = orig_x - dist, y = orig_y + dist; x < orig_x + dist; x++) TryEffect_Star(x, y);
                for (x = orig_x + dist, y = orig_y + dist; y > orig_y - dist; y--) TryEffect_Star(x, y);
                for (x = orig_x + dist, y = orig_y - dist; x > orig_x - dist; x--) TryEffect_Star(x, y);
                for (x = orig_x - dist, y = orig_y - dist; y < orig_y + dist; y++) TryEffect_Star(x, y);
                await UniTask.WaitForSeconds(time_wait_item).AttachExternalCancellation(token);
            }
        }

        private async UniTask Effect_Spread(ItemBingoState state, BingoItem itemData, CancellationToken token)
        {
            var maxOffset = Mathf.Max(width, height);
            for (var offset = 1; offset <= maxOffset; offset++)
            {
                var should_play_sound = false;
                foreach (var dir in spread_dir_round)
                {
                    var x = itemData.CoordX + dir.Item1 * offset;
                    var y = itemData.CoordY + dir.Item2 * offset;
                    if (x < 1 || x > width || y < 1 || y > height)
                        continue;
                    should_play_sound = true;
                    // 以数据层为准
                    var data = GetItemData(x, y);
                    if (data.HasBingo)
                    {
                        var item = GetItem(x, y);
                        var pos = item.transform.position;
                        if (!item.IsViewBingo())
                            item.PlayConvert(itemData);
                        if (dir.Item2 == 0) // 横
                        {
                            if (state.HasFlag(ItemBingoState.RowCompleted))
                                Effect_Star(pos);
                        }
                        else if (dir.Item1 == 0) // 纵
                        {
                            if (state.HasFlag(ItemBingoState.ColumnCompleted))
                                Effect_Star(pos);
                        }
                        else
                        {
                            if ((state.HasFlag(ItemBingoState.MainDiagonalCompleted) && x == y) ||
                                (state.HasFlag(ItemBingoState.AntiDiagonalCompleted) && x + y == width + 1)) // 斜
                                Effect_Star(pos);
                        }
                    }
                }
                if (should_play_sound)
                    Game.Manager.audioMan.TriggerSound("BingoLight");
                await UniTask.WaitForSeconds(time_wait_item).AttachExternalCancellation(token);
            }
        }

        private bool TryEffect_Star(int x, int y)
        {
            var item = GetItem(x, y);
            if (item == null)
                return false;
            Effect_Star(item.transform.position);
            return true;
        }

        private void Effect_Star(Vector2 pos)
        {
            var eff = GameObjectPoolManager.Instance.CreateObject(pool_key_item_eff, effectRoot);
            eff.transform.localScale = Vector3.one;
            eff.transform.position = pos;
            eff.SetActive(true);
            eff.GetOrAddComponent<MBAutoRelease>().Setup(pool_key_item_eff, 2f);
        }

        private void RefreshBoard()
        {
            var items = actInst.GetBingoItemList();
            foreach (var item in items)
            {
                RefreshItem(item);
            }
            if (!Game.Manager.activity.mapR.ContainsKey(actInst))
            {
                uiMain.Close();
            }
        }

        private void RefreshItem(BingoItem item)
        {
            GetItem(item.CoordX, item.CoordY).RefreshItem(item);
        }

        private BingoItem GetItemData(int x, int y)
        {
            return actInst.GetBingoItemList().Find(item => item.CoordX == x && item.CoordY == y);
        }

        private MBBingoBoardItem GetItem(int x, int y)
        {
            var itemObj = GetItemTransform(x, y);
            if (itemObj == null)
                return null;
            return itemObj.GetComponent<MBBingoBoardItem>();
        }

        private Transform GetItemTransform(int x, int y)
        {
            var item_name = $"item_{x}_{y}";
            return root.Find(item_name);
        }
    }
}