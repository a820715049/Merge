/*
 * @Author: qun.chao
 * @Date: 2021-06-28 10:36:59
 */

using UnityEngine;
using UnityEngine.UI;
using FAT.Merge;
using System.Collections.Generic;
using EL;

namespace FAT
{

    public class MBBoardIndicator : MonoBehaviour, IMergeBoard
    {
        [SerializeField] private RectTransform ind;
        [SerializeField] private Text countdown;
        [SerializeField] private GameObject tip;
        private Item mItem;

        #region activity indicator | 活动角标
        private readonly List<IMergeItemIndicatorHandler> activityIndicators = new();
        private readonly List<IMergeItemIndicatorHandler> activityIndicators_recent = new();
        private bool indicator_dirty_event = false;
        private bool indicator_dirty_item = false;
        #endregion

        void IMergeBoard.Init()
        { }

        void IMergeBoard.Setup(int w, int h)
        {
            Hide();
            RegisterActivityIndicator();
            MessageCenter.Get<MSG.ACTIVITY_STATE>().AddListener(OnMessageActivityStateChange);
        }

        void IMergeBoard.Cleanup()
        {
            Hide();
            ClearFlag();
            UnRegisterActivityIndicator();
            MessageCenter.Get<MSG.ACTIVITY_STATE>().RemoveListener(OnMessageActivityStateChange);
        }

        private void Update()
        {
            if (indicator_dirty_event)
            {
                ClearFlag();
                BackupActivityIndicator();
                UnRegisterActivityIndicator();
                RegisterActivityIndicator();
                if (CheckAnyHandlerChange())
                {
                    RefreshActivityIndicator();
                }
            }
            else if (indicator_dirty_item)
            {
                ClearFlag();
                RefreshActivityIndicator();
            }
        }

        public void Show(int x, int y)
        {
            var item = BoardViewManager.Instance.board.GetItemByCoord(x, y);
            if (item == null)
            {
                Hide();
                return;
            }
            _CacheItem(item);
            var cellSize = BoardUtility.cellSize;
            ind.anchoredPosition = new Vector2(cellSize * x + cellSize * 0.5f, -cellSize * y - cellSize * 0.5f);
            _ReShow();
        }

        public void Hide()
        {
            mItem = null;
            ind.gameObject.SetActive(false);
            tip.SetActive(false);
            _SetCountdownShow(false);
        }

        private void _CacheItem(Item item)
        {
            mItem = item;
        }

        private void _ReShow()
        {
            ind.gameObject.SetActive(false);
            ind.gameObject.SetActive(true);
        }

        private void _SetCountdownShow(bool b)
        {
            countdown.gameObject.SetActive(b);
        }

        #region activity indicator

        public bool TryGetActivityIndicatorInfo(Item item, out ItemIndType indType, out string asset)
        {
            indType = ItemIndType.None;
            asset = null;
            foreach (var indicator in activityIndicators)
            {
                var ind = indicator.CheckIndicator(item.tid, out asset);
                if (ind != ItemIndType.None)
                {
                    indType = ind;
                    return true;
                }
            }
            return false;
        }

        public void RefreshActivityIndicatorForItem(Item item)
        {
            var view = BoardViewManager.Instance.GetItemView(item.id);
            if (view != null)
            {
                view.RefreshActivityIndicator();
            }
        }

        private void ClearFlag()
        {
            indicator_dirty_event = false;
            indicator_dirty_item = false;
        }

        private void BackupActivityIndicator()
        {
            activityIndicators_recent.Clear();
            activityIndicators_recent.AddRange(activityIndicators);
        }

        private bool CheckAnyHandlerChange()
        {
            if (activityIndicators.Count != activityIndicators_recent.Count)
                return true;
            for (var i = 0; i < activityIndicators.Count; i++)
            {
                if (activityIndicators[i] != activityIndicators_recent[i])
                    return true;
            }
            return false;
        }

        private void RegisterActivityIndicator()
        {
            var all = Game.Manager.activity.map;
            foreach (var kv in all)
            {
                if (kv.Value is IMergeItemIndicatorHandler ind && kv.Value.Active)
                {
                    ind.Invalidate += _OnIndicatorDirty;
                    activityIndicators.Add(ind);
                }
            }
            activityIndicators.Sort(Sort);
        }

        private void UnRegisterActivityIndicator()
        {
            foreach (var indicator in activityIndicators)
            {
                indicator.Invalidate -= _OnIndicatorDirty;
            }
            activityIndicators.Clear();
        }

        private int Sort(IMergeItemIndicatorHandler a, IMergeItemIndicatorHandler b)
        {
            if (a is ActivityLike aAct && b is ActivityLike bAct)
            {
                if (aAct.Type == bAct.Type)
                    return aAct.Id.CompareTo(bAct.Id);
                return aAct.Type.CompareTo(bAct.Type);
            }
            return 0;
        }

        private void RefreshActivityIndicator()
        {
            var board = BoardViewManager.Instance.board;
            var width = board.size.x;
            var height = board.size.y;
            Item item;
            for (var i = 0; i < width; ++i)
            {
                for (var j = 0; j < height; j++)
                {
                    item = board.GetItemByCoord(i, j);
                    if (item != null)
                    {
                        RefreshActivityIndicatorForItem(item);
                    }
                }
            }
        }

        private void _OnIndicatorDirty()
        {
            indicator_dirty_item = true;
        }

        private void OnMessageActivityStateChange(ActivityLike activityLike)
        {
            indicator_dirty_event = true;
        }

        #endregion
    }
}