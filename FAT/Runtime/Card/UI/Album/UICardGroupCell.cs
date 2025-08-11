/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡册界面 卡组cell
 * @Date: 2024-01-25 15:01:11
 */

using EL;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using TMPro;

namespace FAT
{
    public class UICardGroupCell : FancyGridViewCell<int, UICommonScrollGridDefaultContext>
    {
        [SerializeField] public UIImageRes groupIcon;
        [SerializeField] public Button groupButton;
        [SerializeField] public TMP_Text groupName;
        [SerializeField] public MBRewardProgress progress;
        [SerializeField] public GameObject finishGo;
        [SerializeField] public GameObject redPointGo;
        [SerializeField] public TMP_Text redPointNum;
        private int _curGroupId;
        private static Action<int> _clockCb = null;

        public override void Initialize()
        {
            groupButton.WithClickScale().onClick.AddListener(_OnClickBtnGroup);
        }
        
        public void SetupCellClickCb(Action<int> cb)
        {
            _clockCb = cb;
        }

        public override void UpdateContent(int groupId)
        {
            var cardMan = Game.Manager.cardMan;
            var albumData = cardMan.GetCardAlbumData(cardMan.IsNeedFakeAlbumData);
            if (albumData == null) return;
            var groupData = albumData.TryGetCardGroupData(groupId);
            if (groupData == null) return;
            var groupConfig = groupData.GetConfig();
            if (groupConfig == null) return;
            _curGroupId = groupId;
            groupIcon.SetImage(groupConfig.Icon);
            groupName.text = I18N.Text(groupConfig.Name);
            if (groupData.IsCollectAll)
            {
                finishGo.SetActive(true);
                progress.gameObject.SetActive(false);
            }
            else
            {
                finishGo.SetActive(false);
                progress.gameObject.SetActive(true);
                albumData.GetCollectProgress(groupId, out var ownCount, out var allCount);
                progress.Refresh(ownCount, allCount);
            }
            int newCount = albumData.CheckNewCardCount(groupId);
            redPointGo.SetActive(newCount > 0);
            redPointNum.text = newCount.ToString();
        }

        private void _OnClickBtnGroup()
        {
            if (_curGroupId <= 0)
                return;
            _clockCb?.Invoke(_curGroupId);
        }
    }
}