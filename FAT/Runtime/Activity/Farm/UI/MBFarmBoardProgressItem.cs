// ================================================
// File: MBFarmBoardProgressItem.cs
// Author: yueran.li
// Date: 2025/04/24 20:31:56 星期四
// Desc: 农场棋盘进度条Item
// ================================================

using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBFarmBoardProgressItem : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _itemNode;
        [SerializeField] private UIImageRes _icon;
        [SerializeField] private UIImageRes _shadow;

        // private int _id;
        // private int _index;

        private readonly int _Punch = Animator.StringToHash("Punch");
        private readonly int _Hide = Animator.StringToHash("Hide");
        private readonly int _Unlock = Animator.StringToHash("Unlock");


        public void Init(int id, int index, FarmBoardActivity activity)
        {
            // _id = id;
            // _index = index;
            var obj = Game.Manager.objectMan.GetBasicConfig(id);
            _icon.SetImage(obj.Icon);
            _shadow.SetImage(obj.BlackIcon);

            gameObject.SetActive(true);
            Refresh(activity, index);
        }

        public void Refresh(FarmBoardActivity activity,  int index)
        {
            if (activity == null) return;
            transform.localScale = Vector3.one;

            transform.GetComponent<LayoutElement>().ignoreLayout = false;


            var mileStone = activity.GetAllItemIdList();
            if (mileStone.Count > index)
            {
                var unlock = IsItemUnlock(activity, index);
                _animator.SetBool(_Unlock, unlock);
                _animator.SetBool(_Hide, false);
                transform.GetComponent<LayoutElement>().ignoreLayout = false;
            }
            else if (mileStone.Count == index)
            {
                _animator.SetBool(_Unlock, false);
                _animator.SetBool(_Hide, true);
                transform.GetComponent<LayoutElement>().ignoreLayout = false;
            }
            else
            {
                transform.GetComponent<LayoutElement>().ignoreLayout = true;
                transform.localScale = Vector3.zero;
            }
        }

        private bool IsItemUnlock(FarmBoardActivity activity, int index)
        {
            if (activity == null)
            {
                return false;
            }

            var curLevel = activity.UnlockMaxLevel;
            if (curLevel == 0)
            {
                return false;
            }

            return curLevel - 1 >= index;
        }

        public void PlayUnlock()
        {
            _animator.SetTrigger(_Punch);
        }
    }
}