/**
 * @Author: zhangpengjian
 * @Date: 2024/10/23 17:53:47
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/23 17:53:47
 * Description: 卡册概览cell 多组
 */

using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UICardAlbumPreviewCellMulti : MonoBehaviour
    {
        [SerializeField] private List<GameObject> lockList;
        [SerializeField] private List<GameObject> emptyList;
        [SerializeField] private UIImageRes image;

        public void Refresh(int groupId)
        {
            var cardMan = Game.Manager.cardMan;
            var albumData = cardMan.GetCardAlbumData();
            if (albumData == null) return;
            var groupData = albumData.TryGetCardGroupData(groupId);
            if (groupData == null) return;
            var groupConfig = groupData.GetConfig();
            if (groupConfig == null) return;
            image.SetImage(groupConfig.OverView);
            for (int i = 0; i < groupConfig.CardInfo.Count; i++)
            {
                var cardData = albumData.TryGetCardData(groupConfig.CardInfo[i]);
                var cardConfig = cardData.GetConfig();
                if (cardData != null && cardConfig != null)
                {
                    lockList[i].SetActive(cardData.IsOwn && (cardData.OwnCount <= 1 || cardConfig.IsGold));
                    emptyList[i].SetActive(!cardData.IsOwn);
                }
            }
        }
    }
}