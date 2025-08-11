/*
 * @Author: qun.chao
 * @Date: 2021-03-18 12:22:06
 */
using UnityEngine;

namespace FAT
{
    public class UIMergeDot : MonoBehaviour
    {
        private void Start()
        {
            TweenUtility.CreateNoticeTween(gameObject, transform.localPosition, (transform as RectTransform).rect.height);
        }
    }
}