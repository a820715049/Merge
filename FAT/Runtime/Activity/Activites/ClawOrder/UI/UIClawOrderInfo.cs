/*
 * @Author: qun.chao
 * @Date: 2025-07-25 16:58:41
 */
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIClawOrderInfo : MonoBehaviour
    {
        [SerializeField] private Button btnInfo;

        private void Awake()
        {
            btnInfo.onClick.AddListener(OnBtnInfoClick);
        }

        private void OnBtnInfoClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIClawOrderTips, btnInfo.transform.position, 35f);
        }
    }
}