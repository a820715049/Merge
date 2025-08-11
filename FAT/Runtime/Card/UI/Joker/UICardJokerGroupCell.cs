/*
 * @Author: tang.yan
 * @Description: 万能卡选卡界面卡组cell  
 * @Date: 2024-03-29 12:03:52
 */
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using EL;

namespace FAT
{
    public class CardJokerGroupCellData
    {
        public int GroupId;             //卡组id
        public List<int> CardIdList;    //卡组中所有卡牌id list
    }
    
    public class UICardJokerGroupCell : UIGenericItemBase<(CardJokerGroupCellData groupCellData, UICardJokerContext context)>
    {
        [SerializeField] private Transform itemRoot;
        [SerializeField] private UIImageRes bgFrame;
        [SerializeField] private UIImageRes groupIcon;
        [SerializeField] private GameObject groupNormalGo;
        [SerializeField] private GameObject groupFinishGo;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text groupName;
        [SerializeField] private List<UICommonItem> rewardList;
        
        private PoolItemType _itemCellType = PoolItemType.CARD_JOKER_ITEM_CELL;

        protected override void InitComponents()
        {
            foreach (var item in rewardList)
            {
                item.Setup();
            }
        }
        
        protected override void UpdateOnDataChange()
        {
            _Refresh();
        }
        
        protected override void UpdateOnForce()
        {
            _RefreshCard();
        }

        protected override void UpdateOnDataClear()
        {
            _Clear();
        }

        private void _Refresh()
        {
            _Clear();
            var albumData = Game.Manager.cardMan.GetCardAlbumData();
            if (albumData == null)
                return;
            var groupData = albumData.TryGetCardGroupData(mData.groupCellData.GroupId);
            if (groupData == null)
                return;
            var config = groupData.GetConfig();
            if (config == null)
                return;
            bgFrame.SetImage(config.TitleImage);
            groupIcon.SetImage(config.MiniIcon);
            groupName.text = I18N.Text(config.Name);
            albumData.GetCollectProgress(groupData.CardGroupId, out var ownCount, out var allCount);
            progressText.text = string.Concat(ownCount, "/", allCount);
            bool isCollectAll = ownCount == allCount;
            groupNormalGo.SetActive(!isCollectAll);
            groupFinishGo.SetActive(isCollectAll);
            if (!isCollectAll)
            {
                int rewardCount = config.Reward.Count;
                for (int i = 0; i < rewardList.Count; i++)
                {
                    var reward = rewardList[i];
                    if (i < rewardCount)
                    {
                        reward.gameObject.SetActive(true);
                        reward.Refresh(config.Reward[i]?.ConvertToRewardConfig());
                    }
                    else
                    {
                        reward.gameObject.SetActive(false);
                    }
                }
            }
            _ShowCard();
        }
        
        private void _ShowCard()
        {
            foreach (var cardId in mData.groupCellData.CardIdList)
            {
                var go = GameObjectPoolManager.Instance.CreateObject(_itemCellType, itemRoot);
                go.GetComponent<UIGenericItemBase<int>>().SetData(cardId);
                go.SetActive(true);
            }
        }
        
        private void _RefreshCard()
        {
            for (int i = itemRoot.childCount - 1; i >= 0; --i)
            {
                var item = itemRoot.GetChild(i);
                item.GetComponent<UIGenericItemBase<int>>().ForceRefresh();
            }
        }

        private void _Clear()
        {
            UIUtility.ReleaseClearableItem(itemRoot, _itemCellType);
        }
        
    }
}
