/**
 * @Author: zhangpengjian
 * @Date: 2024/12/3 11:04:34
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/12/3 11:04:34
 * Description: 闪卡必得礼包卡片cell
 */

using System.Collections.Generic;
using UnityEngine;
using EL;
using TMPro;
using UnityEngine.UI;
using fat.rawdata;

namespace FAT
{
    public class UIShinnyGuarCardCell : MonoBehaviour
    {
        [SerializeField] private UIImageRes cardIcon;
        [SerializeField] private UIImageRes groupIcon;
        [SerializeField] private Button groupBtn;
        [SerializeField] private TMP_Text cardName;
        [SerializeField] private TMP_Text groupName;
        [SerializeField] private List<GameObject> starList;
        [SerializeField] private Transform isNewRoot;

        private CardGroup cardGroupConf;

        public void Init()
        {
            groupBtn.onClick.AddListener(OnClickBtn);
        }

        private void OnClickBtn()
        {
            if (cardGroupConf != null)
                UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, groupIcon.transform.position, 0f, cardGroupConf.Reward, true);
        }

        public void Refresh(int cardId)
        {
            var cardData = Game.Manager.cardMan.GetCardData(cardId);
            if (cardData != null)
            {
                var groupData = Game.Manager.cardMan.GetCardAlbumData()?.TryGetCardGroupData(cardData.BelongGroupId);
                if (groupData != null)
                {
                    var c = groupData.GetConfig();
                    cardGroupConf = c;
                    groupIcon.SetImage(c.Icon);
                    groupName.text = I18N.Text(c.Name);
                }
                var card = Game.Manager.objectMan.GetCardConfig(cardId);
                var basicConfig = Game.Manager.objectMan.GetBasicConfig(cardId);
                if (basicConfig != null)
                {
                    cardIcon.SetImage(basicConfig.Icon);
                    cardName.text = I18N.Text(basicConfig.Name);
                }
                for (int i = 0; i < starList.Count; i++)
                {
                    starList[i].gameObject.SetActive(false);
                }
                for (int i = 0; i < card.Star; i++)
                {
                    starList[i].gameObject.SetActive(true);
                }
                isNewRoot.gameObject.SetActive(!cardData.IsOwn);
            }
        }
    }
}