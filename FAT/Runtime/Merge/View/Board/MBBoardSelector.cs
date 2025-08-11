/*
 * @Author: qun.chao
 * @Date: 2021-02-22 18:43:58
 */
namespace FAT
{
    using UnityEngine;
    using UnityEngine.UI;
    using FAT.Merge;

    public class MBBoardSelector : MonoBehaviour, IMergeBoard
    {
        [SerializeField] private RectTransform selector;
        [SerializeField] private RectTransform selectorMax;

        private int width;
        private int height;

        void IMergeBoard.Init()
        { }

        void IMergeBoard.Setup(int w, int h)
        {
            width = w;
            height = h;
            var cellSize = BoardUtility.cellSize;
            selector.sizeDelta = new Vector2(cellSize, cellSize);
            selectorMax.sizeDelta = new Vector2(cellSize, cellSize);
            Hide();
        }

        void IMergeBoard.Cleanup()
        {
            Hide();
        }

        public void ForceShowSelector(int x, int y)
        {
            Hide();
            var cellSize = BoardUtility.cellSize;
            var _selector = selector;
            _selector.anchoredPosition = new Vector2(cellSize * x + cellSize * 0.5f, -cellSize * y - cellSize * 0.5f);
            _selector.gameObject.SetActive(true);
        }

        public void Show(int x, int y)
        {
            var item = BoardViewManager.Instance.board.GetItemByCoord(x, y);
            if (item == null)
            {
                Hide();
                return;
            }

            RectTransform _selector;
            if (ItemUtility.GetNextItem(item.tid) > 0)
            {
                _selector = selector;
                selector.gameObject.SetActive(true);
                selectorMax.gameObject.SetActive(false);
            }
            else
            {
                _selector = selectorMax;
                selector.gameObject.SetActive(false);
                selectorMax.gameObject.SetActive(true);
            }
            var cellSize = BoardUtility.cellSize;
            _selector.anchoredPosition = new Vector2(cellSize * x + cellSize * 0.5f, -cellSize * y - cellSize * 0.5f);
            _selector.gameObject.SetActive(false);
            _selector.gameObject.SetActive(true);
        }

        public void Hide()
        {
            selector.gameObject.SetActive(false);
            selectorMax.gameObject.SetActive(false);
        }
    }
}