/*
 * @Author: qun.chao
 * @Date: 2023-11-21 14:36:16
 */
namespace FAT
{
    using UnityEngine;
    using FAT.Merge;
    using EL;
    using fat.rawdata;

    public class MBBoardFg : MonoBehaviour, IMergeBoard
    {
        [SerializeField] private UIImageRes imgRes;

        void IMergeBoard.Init()
        {
        }

        void IMergeBoard.Setup(int w, int h)
        {
            var board = BoardViewManager.Instance.board;
            var cfg = Game.Manager.mergeBoardMan.GetBoardConfig(board.boardId);
            // var wp_default = Game.Manager.objectMan.GetWallpaperConfig(boardCfg.WallPaperId);
            // ObjWallpaper custom = null;
            // if (board.world == Game.Manager.mainMergeMan.world)
            // {
            //     var customActive = Game.Manager.wallpaperMan.Active;
            //     custom = Game.Manager.objectMan.GetWallpaperConfig(customActive);
            // }
            // var cfg = custom ?? wp_default;
            // 棋盘区前景
            if (string.IsNullOrEmpty(cfg.ImgTop))
            {
                imgRes.gameObject.SetActive(false);
            }
            else
            {
                imgRes.gameObject.SetActive(true);
                imgRes.SetImage(cfg.ImgTop);
            }
        }

        void IMergeBoard.Cleanup()
        {
        }
    }
}