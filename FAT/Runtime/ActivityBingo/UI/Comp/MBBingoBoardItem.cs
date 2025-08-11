/*
 * @Author: qun.chao
 * @Date: 2025-03-04 17:48:52
 */
using System;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBBingoBoardItem : MonoBehaviour
    {
        [SerializeField] private UIImageRes iconRes;
        [SerializeField] private GameObject goBingo;
        [SerializeField] private GameObject goCommitted;
        [SerializeField] private GameObject readyToCommitBg;
        [SerializeField] private GameObject readyToCommitFlag;
        [SerializeField] private Animator animator;

        private BingoItem data;
        private Action<BingoItem> clickHandler;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        public void Bind(Action<BingoItem> clickHandler_)
        {
            clickHandler = clickHandler_;
        }

        public void RefreshItem(BingoItem item)
        {
            data = item;
            var cfg = Merge.Env.Instance.GetItemConfig(item.ItemId);
            iconRes.SetImage(cfg.Icon);
            var isBingo = item.HasBingo;
            var isClaimed = !isBingo && item.IsClaimed;
            var canCommit = !item.IsClaimed && BingoUtility.HasActiveItemInMainBoardAndInventory(item.ItemId);
            goBingo.SetActive(isBingo);
            goCommitted.SetActive(isClaimed);
            readyToCommitBg.SetActive(canCommit);
            readyToCommitFlag.SetActive(canCommit);
        }

        public bool IsViewBingo()
        {
            return goBingo.activeSelf;
        }

        public bool IsViewCommitted()
        {
            return goCommitted.activeSelf;
        }

        public void PlayConvert(BingoItem item)
        {
            if (item.HasBingo)
            {
                goBingo.SetActive(true);
                animator.SetTrigger("Bingo");
            }
            else if (item.IsClaimed)
            {
                goCommitted.SetActive(true);
                animator.SetTrigger("Commit");
            }
        }

        public void PlayHide()
        {
            goBingo.SetActive(true);
            animator.SetTrigger("Hide");
        }

        private void OnClick()
        {
            if (data == null)
                return;
            if (data.IsClaimed)
                return;
            clickHandler?.Invoke(data);
        }
    }
}