/*
 * @Author: tang.yan
 * @Description: 用于棋子上面显示活动token 
 * @Date: 2025-09-16 10:09:16
 */

namespace FAT
{
    using UnityEngine;
    using Merge;
    
    public class MBItemActivityToken : MonoBehaviour
    {
        [SerializeField] private RectTransform scoreMicRect;
        [SerializeField] private UIImageRes scoreMicIcon;
        
        private MBItemView mView;
        private int itemId;

        public void SetData(MBItemView view)
        {
            mView = view;
            itemId = view.data.tid;
            _RefreshRes(view.data);
        }

        public void ClearData()
        {
            mView = null;
            itemId = -1;
            _ResetUI();
        }

        public void RefreshActivityTokenState()
        {
            if (mView == null)
                return;
            _RefreshRes(mView.data);
        }

        public RectTransform GetScoreMicRect()
        {
            return scoreMicRect;
        }

        private void _RefreshRes(Item item)
        {
            _ResetUI();
            if (item == null)
                return;
            if (!item.TryGetItemComponent<ItemActivityTokenComponent>(out var comp))
                return;
            var curWorld = BoardViewManager.Instance.world;
            if (curWorld == null)
                return;
            var hasActiveTokenMulti = curWorld.tokenMulti.hasActiveTokenMulti;
            ItemTokenMultiComponent tokenMultiComp = null;
            if (hasActiveTokenMulti)
            {
                var activeItem = curWorld.activeBoard.FindItemById(curWorld.tokenMulti.activeTokenMultiId);
                tokenMultiComp = activeItem?.GetItemComponent<ItemTokenMultiComponent>();
            }
            //左下角
            _RefreshBL(comp, tokenMultiComp);
        }

        private void _RefreshBL(ItemActivityTokenComponent comp, ItemTokenMultiComponent tokenMultiComp)
        {
            if (comp.CanShow_BL)
            {
                if (Game.Manager.activity.Lookup(comp.ActivityId_BL, out var act) && act.Active)
                {
                    scoreMicIcon.gameObject.SetActive(true);
                    var cfg = Env.Instance.GetItemConfig(comp.TokenId_BL);
                    if (cfg != null)
                    {
                        var isMulti = ItemUtility.CheckTokenCanMulti(tokenMultiComp, comp.TokenId_BL);
                        //非翻倍时读小图Image，翻倍时借用BlackIcon字段
                        var image = !isMulti ? cfg.Image : cfg.BlackIcon;
                        scoreMicIcon.SetImage(image);
                    }
                }
            }
        }

        private void _ResetUI()
        {
            scoreMicIcon.gameObject.SetActive(false);
        }
    }
}