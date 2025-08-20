// ==================================================
// // File: MBBingoTaskItem.cs
// // Author: liyueran
// // Date: 2025-07-16 11:07:46
// // Desc: bingoTask 进度条宝箱Item
// // ==================================================

using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBBingoTaskBoxItem : MonoBehaviour
    {
        [SerializeField] private UICommonItem _uiCommonItem;
        [SerializeField] private UIImageRes indexBg;
        [SerializeField] private TextMeshProUGUI indexTxt;
        [SerializeField] private GameObject check;
        [SerializeField] private Animator animator;

        private ActivityBingoTask _activity;

        private int _id;
        private int _count;
        [HideInInspector] public int _index;


        public void Init(ActivityBingoTask activity, int id, int count, int index)
        {
            this._activity = activity;

            _uiCommonItem.Setup();
            _uiCommonItem.Refresh(id, count);

            this._id = id;
            this._count = count;
            this._index = index;

            indexTxt.text = _index.ToString();

            check.SetActive(index <= _activity.score);

            RefreshTheme();
        }

        public void ShowCheck()
        {
            if (check.activeSelf)
            {
                return;
            }

            check.SetActive(true);
            animator.SetTrigger("Punch");
        }

        private void RefreshTheme()
        {
            _activity.Visual.Refresh(indexBg, "indexBg");
        }
    }
}