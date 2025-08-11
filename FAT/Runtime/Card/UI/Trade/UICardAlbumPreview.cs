/**
 * @Author: zhangpengjian
 * @Date: 2024/10/18 16:11:55
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/18 16:11:55
 * Description: 卡片交换-全部卡片概览
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UICardAlbumPreview : UIBase
    {
        [SerializeField] private List<UICardAlbumPreviewCell> listGroup;
        [SerializeField] private Transform groupB;
        [SerializeField] private Transform groupA;

        protected override void OnCreate()
        {
            transform.Access("Content", out Transform root);
            root.Access("BtnClose", out Button close);
            root.Access("BtnFacebook", out Button facebook);
            close.onClick.AddListener(Close);
            facebook.onClick.AddListener(OnClickFacebook);
        }

        private void OnClickFacebook()
        {
            Game.Manager.cardMan.JumpTradingGroup(2);
        }

        protected override void OnPreOpen()
        {
            Game.Manager.cardMan.TrackOpenCardOverview();
            RefreshCardGroup();
            Game.Manager.audioMan.TriggerSound("OpenCardPreview");
        }

        private void RefreshCardGroup()
        {
            var albumData = Game.Manager.cardMan.GetCardAlbumData();
            if (albumData == null) return;
            var groupInfo = albumData.GetConfig().GroupInfo;
            groupB.gameObject.SetActive(groupInfo.Count > listGroup.Count);
            groupA.gameObject.SetActive(groupInfo.Count <= listGroup.Count);
            if (groupInfo.Count > listGroup.Count)
            {
                for (int i = 0; i < albumData.GetConfig().GroupInfo.Count; i++)
                {
                    groupB.GetChild(i).gameObject.GetComponent<UICardAlbumPreviewCellMulti>().Refresh(albumData.GetConfig().GroupInfo[i]);
                }
            }
            else
            {
                for (int i = 0; i < albumData.GetConfig().GroupInfo.Count; i++)
                {
                    listGroup[i].Refresh(albumData.GetConfig().GroupInfo[i]);
                }
            }

        }
    }
}