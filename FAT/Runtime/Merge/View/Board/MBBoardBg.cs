/*
 * @Author: qun.chao
 * @Date: 2021-02-19 10:39:02
 */
namespace FAT
{
    using UnityEngine;
    using UnityEngine.UI;
    using FAT.Merge;
    using EL;
    using fat.rawdata;

    public class MBBoardBg : MonoBehaviour, IMergeBoard
    {
        [SerializeField] private UIImageRes bg;
        [SerializeField] private UIImageRes pattern;
        [SerializeField] private UITilling tilling;
        [SerializeField] private GameObject bgItem;
        [SerializeField] private Transform itemRoot;
        // [SerializeField] private Transform areaRoot;
        [SerializeField] private Transform effectRoot;
        [SerializeField] private MBBoardBgIndicator bgIndicator;

        private readonly Color colFinished = new Color32(0xd2, 0xfb, 0x85, 255);
        private readonly Color colOnGoing = new Color32(0x9b, 0xbb, 0x55, 255);
        private int width;
        private int height;
        private BoardEffectHolder holder = new BoardEffectHolder();

        void IMergeBoard.Init()
        {
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MERGE_ITEM_BG, bgItem);
        }

        void IMergeBoard.Setup(int w, int h)
        {
            width = w;
            height = h;

            holder.Setup(effectRoot);
            bgIndicator.Setup(w, h);

            _SetupBoardBgAndPattern(w, h);
            MessageCenter.Get<MSG.GAME_WALLPAPER_SWITCH>().AddListener(SwitchWallpaper);
        }

        void IMergeBoard.Cleanup()
        {
            holder.Cleanup();
            bgIndicator.Cleanup();

            MessageCenter.Get<MSG.GAME_WALLPAPER_SWITCH>().RemoveListener(SwitchWallpaper);
        }

        private void SwitchWallpaper(int id_)
        {
            _SetupBoardBgAndPattern(width, height);
        }

        private void _SetupBoardBgAndPattern(int w, int h)
        {
            var board = BoardViewManager.Instance.board;
            var cfg = Game.Manager.mergeBoardMan.GetBoardConfig(board.boardId);

            // 棋盘区背景
            if (string.IsNullOrEmpty(cfg.ImgBG))
            {
                bg.gameObject.SetActive(false);
            }
            else
            {
                bg.gameObject.SetActive(true);
                bg.SetImage(cfg.ImgBG);
            }

            // pattern
            if (string.IsNullOrEmpty(cfg.ImgTile))
            {
                pattern.gameObject.SetActive(false);
            }
            else
            {
                pattern.gameObject.SetActive(true);
                pattern.SetImage(cfg.ImgTile);
            }

            // _SetWallpaper(custom, wp_default);
            tilling.SetTilling(new Vector2(w * 0.5f, h * 0.5f));
        }

        #region order item bg

        public void Refresh()
        {
            bgIndicator.Refresh();
        }

        #endregion
    }
}
