/*
 * @Author: qun.chao
 * @Date: 2022-04-13 20:18:04
 */
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class MBCanvasAutoMatchWH : MonoBehaviour
    {
        private void OnEnable()
        {
            var scaler = transform.GetComponent<CanvasScaler>();
            var area = Screen.safeArea;
            if (area.width / area.height > scaler.referenceResolution.x / scaler.referenceResolution.y + 0.00001f)
            {
                scaler.matchWidthOrHeight = 1f;
            }
            else
            {
                scaler.matchWidthOrHeight = 0f;
            }
        }
    }
}