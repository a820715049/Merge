/*
 *@Author:chaoran.zhang
 *@Desc:迷你棋盘进度条图标
 *@Created Time:2024.08.13 星期二 13:57:03
 */

using FAT.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MbMiniBoardProgressItem : MonoBehaviour
    {
        [SerializeField] private UIImageRes _icon;
        [SerializeField] private UIImageRes _shadow;
        [SerializeField] private GameObject _arrow;
        [SerializeField] private Animator _animator;
        [SerializeField] private TextMeshProUGUI _num;
        private int _id;
        private int _index;

        public void SetUp(int id, int index, bool actEnd = false)
        {
            _id = id;
            _index = index;
            var obj = Game.Manager.objectMan.GetBasicConfig(_id);
            _icon.SetImage(obj.Icon);
            _shadow.SetImage(obj.BlackIcon);
            _num.text = (_index + 1).ToString();
            if (actEnd)
                Refresh();
            gameObject.SetActive(true);
        }


        public void Refresh()
        {
            if (Game.Manager.miniBoardMan.IsValid)
            {
                _animator.SetBool("Unlock", Game.Manager.miniBoardMan.IsItemUnlock(_id));
                _arrow.SetActive(_index == Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel());
            }
        }

        public void RefreshEnd(int maxLevel)
        {
            _animator.SetBool("Unlock", _index <= maxLevel);
            _arrow.SetActive(_index == maxLevel);
            _num.text = (_index + 1).ToString();
        }

        public void Unlock()
        {
            _animator.SetTrigger("Punch");
        }
    }
}