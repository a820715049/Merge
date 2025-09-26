/*
 * @Author: tang.yan
 * @Description: 万能卡获得界面 
 * @Date: 2024-03-27 14:03:37
 */
using EL;
using UnityEngine;
using TMPro;

namespace FAT
{
    public class UICardJokerGet : UIBase
    {
        [SerializeField] private RectTransform normalTrans;
        [SerializeField] private RectTransform goldTrans;
        [SerializeField] private TMP_Text jokerName;

        private int curShowJokerId = 0;
        
        protected override void OnCreate()
        {
            transform.AddButton("ClaimBtn", _OnBtnClaim);
        }

        protected override void OnParse(params object[] items)
        {
        }

        protected override void OnPreOpen()
        {
            curShowJokerId = Game.Manager.specialRewardMan.TryGetCanClaimId(ObjConfigType.CardJoker);
            var basicConfig = Game.Manager.objectMan.GetBasicConfig(curShowJokerId);
            var conf = Game.Manager.objectMan.GetCardJokerConfig(curShowJokerId);
            if (basicConfig == null || conf == null)
                return;
            normalTrans.gameObject.SetActive(conf.IsOnlyNormal);
            goldTrans.gameObject.SetActive(!conf.IsOnlyNormal);
            jokerName.text = I18N.Text(basicConfig.Name);
        }

        protected override void OnAddListener()
        {
        }

        protected override void OnRemoveListener()
        {
        }

        private void _OnBtnClaim()
        {
            Close();
        }
        
        protected override void OnPostClose()
        {
            Game.Manager.specialRewardMan.OnSpecialRewardUIClosed(ObjConfigType.CardJoker, curShowJokerId);
        }
    }
}
