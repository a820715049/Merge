/*
 * @Author: qun.chao
 * @Date: 2023-10-25 12:13:36
 */
using UnityEngine;

namespace FAT
{
    public class MBBoardOrderRequireItem : MonoBehaviour
    {
        [SerializeField] private MBCommonItem item;
        [SerializeField] private GameObject bgDefault;
        [SerializeField] private GameObject bgFinished;
        [SerializeField] private GameObject bgOnGoing;
        [SerializeField] private GameObject tagShop;
        [SerializeField] private GameObject goCheck;

        public int itemId { get; private set; }
        private bool mConsumed;

        public void SetData(int itemId, bool itemFiiled, bool orderFinished)
        {
            this.itemId = itemId;
            mConsumed = false;
            item.ShowItemNormal(itemId, 1);
            bgDefault.SetActive(true);
            if (orderFinished)
            {
                SetChecker(true);
                bgFinished.SetActive(true);
                bgOnGoing.SetActive(false);
            }
            else
            {
                SetChecker(false);
                bgFinished.SetActive(false);
                bgOnGoing.SetActive(itemFiiled);
            }
        }

        public void RefreshTagShop()
        {
            var od = Game.Manager.shopMan.TryGetChessOrderDataById(itemId);
            tagShop.SetActive(od != null && od.CheckCanBuy());
        }

        public void SetOrderConsumeState()
        {
            // bgDefault.SetActive(false);
            // bgFinished.SetActive(false);
            // bgOnGoing.SetActive(false);
            // tagShop.SetActive(false);
            mConsumed = true;
        }

        public bool IsInConsumedState()
        {
            // return !bgDefault.gameObject.activeSelf;
            return mConsumed;
        }

        private void SetChecker(bool b)
        {
            b = UIUtility.ABTest_OrderItemChecker() && b;
            goCheck.SetActive(b);
        }
    }
}