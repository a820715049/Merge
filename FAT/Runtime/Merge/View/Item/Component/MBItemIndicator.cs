/*
 * @Author: qun.chao
 * @Date: 2021-02-19 15:58:26
 */
namespace FAT
{
    using UnityEngine;
    using UnityEngine.UI;
    using Merge;

    public class MBItemIndicator : MonoBehaviour
    {
        // 右上角角标
        [SerializeField] private UIImageRes flagRT;

        public bool HasFlag => flagRT.gameObject.activeSelf;

        private MBItemView mView;

        public void SetData(MBItemView view)
        {
            // 隐藏全部标记
            _Reset();
            mView = view;
            if (view.data.parent == null)
            {
                // 礼物队列上不显示
                return;
            }
            if (view.data.HasComponent(ItemComponentType.Bubble))
            {
                // bubble类物品 不显示
                return;
            }
            RefreshActivityIndicator();
        }

        public void ClearData()
        {
            _Reset();
        }

        public void UpdateEx()
        {
        }

        public void OnSelect()
        { }

        public void OnDeselect()
        { }

        public void TryRefreshChestTip()
        {
        }

        public void TryRefreshFeedProgress()
        { }

        public void RefreshActivityIndicator()
        {
            var has = BoardViewManager.Instance.boardView.boardInd.TryGetActivityIndicatorInfo(mView.data, out ItemIndType indType, out string asset);
            if (has != flagRT.gameObject.activeSelf)
            {
                // flag改变 需要触发bg改变
                BoardViewManager.Instance.OnItemFlagChange();
            }
            if (has)
            {
                flagRT.gameObject.SetActive(true);
                flagRT.SetImage(asset);
            }
            else
            {
                flagRT.gameObject.SetActive(false);
            }
        }

        private void _Reset()
        {
            mView = null;
            flagRT.gameObject.SetActive(false);
        }
    }
}