/*
 * @Author: qun.chao
 * @Date: 2022-05-07 19:03:05
 */
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class MBClickEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float scale = 0.9f;
        private bool mAvailable = false;
        private Vector3 mPreScale;
        private Button mBtn;

        private void Start()
        {
            if (transform.GetComponent<IPointerDownHandler>() != null && transform.GetComponent<IPointerUpHandler>() != null)
            {
                mAvailable = true;
            }
            transform.TryGetComponent(out mBtn);
        }

        private bool _Interactable()
        {
            if (mBtn != null)
                return mBtn.interactable;
            return true;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (mAvailable && _Interactable())
            {
                mPreScale = transform.localScale;
                transform.localScale = mPreScale * scale;
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (mAvailable && _Interactable())
            {
                transform.localScale = mPreScale;
            }
        }
    }
}