/**
 * @Author: zhangpengjian
 * @Date: 2024/10/28 15:51:51
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/28 15:51:51
 * Description: tmp超链接
 */
 
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Text))]
public class TMP_LinkOpener : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        TMP_Text pTextMeshPro = GetComponent<TMP_Text>();
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(pTextMeshPro, eventData.position, null);
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = pTextMeshPro.textInfo.linkInfo[linkIndex];
            FAT.UIBridgeUtility.OpenURL(linkInfo.GetLinkID());
        }
    }
}