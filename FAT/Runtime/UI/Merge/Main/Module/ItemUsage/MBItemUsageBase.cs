/*
 * @Author: qun.chao
 * @Date: 2023-10-26 14:20:05
 */
using UnityEngine;
using EL;

namespace FAT.Merge
{
    public class MBItemUsageBase : MonoBehaviour
    {
        protected Item mItem;
        protected virtual void OnBtnClick()
        { }

        #region interface

        public virtual void Initialize()
        {
            transform.AddButton(null, OnBtnClick);
        }

        public virtual void SetData(Item item)
        {
            mItem = item;
        }

        public virtual void ClearData()
        {
            mItem = null;
        }

        public virtual void Refresh() { }

        public virtual void UpdateContent() { }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}