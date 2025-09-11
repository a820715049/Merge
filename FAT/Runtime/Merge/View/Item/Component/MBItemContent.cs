/*
 * @Author: qun.chao
 * @Date: 2021-02-23 12:42:49
 */

namespace FAT
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;
    using Merge;
    using Config;
    using EventType = fat.rawdata.EventType;

    public class MBItemContent : MonoBehaviour
    {
        [SerializeField] private UIImageRes iconRes;
        [SerializeField] private UIImageRes coverRes;
        [SerializeField] private Image coverImg;
        [SerializeField] private MBItemResHolder holder;
        [SerializeField] private TMPro.TextMeshProUGUI textUnlockLevel;
        [SerializeField] private Image bottom;
        public MBItemResHolder Holder => holder;
        private static readonly float alpha = 0.5f;
        private MBItemView mView;
        public bool isInBox { get; private set; }
        public bool hasNewTip { get; private set; }
        private int itemId;
        private ResType resType = ResType.None;

        private enum ResType
        {
            None,
            Icon,
            Prefab
        }

        public void SetData(MBItemView view)
        {
            mView = view;
            itemId = view.data.tid;
            isInBox = false;
            hasNewTip = false;
            _RefreshRes(view.data);
        }

        public void ClearData()
        {
            mView = null;
            iconRes.Clear();
            holder.ClearRes();
            itemId = -1;
        }

        public void ResolveNewItemTip()
        {
            if (hasNewTip)
            {
                hasNewTip = false;
                Game.Manager.commonTipsMan.ShowPopTips(fat.rawdata.Toast.NewItem,
                    BoardUtility.GetWorldPosByCoord(mView.data.coord));
                //借用显示newTips时的逻辑来处理活动棋盘第一次解锁棋子时的表现需求
                Game.Manager.miniBoardMan.OnNewItemShow(mView.data);
                Game.Manager.miniBoardMultiMan.OnNewItemShow(mView.data);
                Game.Manager.mineBoardMan.OnNewItemShow(mView.data);
                var allActivity = Game.Manager.activity.map;
                foreach (var (_, activity) in allActivity)
                {
                    if (activity is IBoardActivityHandbook boardActivity)
                        boardActivity.OnNewItemShow(mView.data);
                }
            }
        }

        private void _RefreshRes(Item item)
        {
            var mgCfg = Env.Instance.GetItemMergeConfig(item.tid);
            var cfg = Env.Instance.GetItemConfig(item.tid);
            iconRes.color = Color.white;
            coverImg.color = Color.white;
            coverImg.transform.localScale = Vector3.one;

            if (item.parent != null && item.parent.boardId == Constant.MainBoardId)
            {
                UIUtility.ABTest_BoardItemSize(iconRes.transform as RectTransform, coverImg.transform as RectTransform);
            }


#if UNITY_EDITOR
            if (mgCfg == null)
            {
                if (!ItemUtility.IsCardPack(item.tid)) Debug.LogError($"item mgCfg is null : {item.tid}");
            }
            else if (cfg == null)
            {
                Debug.LogError($"item cfg is null : {item.tid}");
            }
#endif
            // mat
            if (item.isFrozen)
                GameUIUtility.SetFrozenItem(iconRes.image);
            else
                GameUIUtility.SetDefaultShader(iconRes.image);

            if (item.isLocked)
            {
                // 锁定中
                coverRes.enabled = true;
                AssetConfig res;
                if (!item.isReachBoardLevel)
                {
                    res = BoardUtility.GetLevelLockBg();
                    textUnlockLevel.gameObject.SetActive(true);
                    textUnlockLevel.text = $"{item.unLockLevel}";
                }
                else
                {
                    res = BoardUtility.GetBoxAsset(item.stateConfParam);
                    textUnlockLevel.gameObject.SetActive(false);
                }

                coverRes.SetImage(res.Group, res.Asset);
                coverImg.gameObject.SetActive(true);
                bottom.gameObject.SetActive(false);
                isInBox = true;
            }
            else if (item.isFrozen)
            {
                // 尘封
                coverRes.enabled = false;
                coverImg.enabled = true;
                _SetMainContent(cfg.Icon, mgCfg?.DisplayRes);
                coverImg.sprite = BoardUtility.frozenCoverSprite;
                coverImg.gameObject.SetActive(coverImg.sprite != null);
                bottom.sprite = BoardUtility.BottomSprite;
                bottom.gameObject.SetActive(bottom.sprite != null);
                textUnlockLevel.gameObject.SetActive(false);
            }
            else if (item.HasComponent(ItemComponentType.Bubble))
            {
                coverRes.enabled = false;
                coverImg.enabled = true;
                // bubble
                _SetMainContent(cfg.Icon, mgCfg?.DisplayRes);
                var bubble = item.GetItemComponent<ItemBubbleComponent>();
                //根据不同类型决定不同的覆盖图片
                coverImg.sprite = bubble.IsBubbleItem() ? BoardUtility.bubbleCoverSprite : BoardUtility.bubbleFrozenCoverSprite;
                coverImg.gameObject.SetActive(true);
                textUnlockLevel.gameObject.SetActive(false);
                bottom.gameObject.SetActive(false);
                // 气泡在子层级被缩小了 单独放大
                coverImg.transform.localScale = Vector3.one * 1.2f;
            }
            else if (item.HasComponent(ItemComponentType.TapBonus))
            {
                coverRes.enabled = false;
                coverImg.enabled = true;
                _SetMainContent(cfg.Icon, mgCfg?.DisplayRes);
                coverImg.sprite = BoardUtility.bubbleCoverSprite;
                coverImg.gameObject.SetActive(true);
                textUnlockLevel.gameObject.SetActive(false);
                bottom.gameObject.SetActive(false);
                coverImg.transform.localScale = Vector3.one;
            }
            else
            {
                // 图鉴解锁
                (_, hasNewTip) = BoardUtility.TryUnlockGallery(item);
                _SetMainContent(cfg.Icon, mgCfg?.DisplayRes);
                coverImg.gameObject.SetActive(false);
                bottom.gameObject.SetActive(false);
                textUnlockLevel.gameObject.SetActive(false);
            }
        }

        private void _SetMainContent(string iconStr, string prefabStr)
        {
            if (string.IsNullOrEmpty(prefabStr))
            {
                if (string.IsNullOrEmpty(iconStr))
                {
                    resType = ResType.None;
                }
                else
                {
                    resType = ResType.Icon;
                    var _icon = iconStr.ConvertToAssetConfig();
                    iconRes.SetImage(_icon.Group, _icon.Asset);
                    UIImageResHelper.BindFallback(iconRes, itemId, UIImageResHelper.Tag.BoardItem);
                }
            }
            else
            {
                resType = ResType.Prefab;
                holder.LoadRes(itemId, mView.data, iconRes.transform);
            }
        }

        public void SetBornFromRewardList()
        {
            holder.SetOnLoadAction(MBItemResHolder.SetBorn);
        }

        public void SetResAction(Action<GameObject> cb)
        {
            holder.SetOnLoadAction(cb);
        }

        public void ApplyFilter(bool selected)
        {
            _SetAlpha(selected ? 1f : alpha);
        }

        public void RemoveFilter()
        {
            _SetAlpha(1f);
        }

        //供外部从当前alpha值开始做tween动画 避免表现上透明度突变
        public float CurAlpha { get; private set; } = 1f;

        //用于外部控制做tween显隐动画
        public void TweenSetAlpha(float a)
        {
            if (mView == null || itemId <= 0)
                return;
            _SetAlpha(a);
        }

        private void _SetAlpha(float a)
        {
            CurAlpha = a;
            if (resType == ResType.Prefab)
            {
                holder.SetAlpha(a);
            }
            else if (resType == ResType.Icon)
            {
                var col = iconRes.color;
                col.a = a;
                iconRes.color = col;
                col = coverRes.color;
                col.a = a;
                coverRes.color = col;
            }
        }
    }
}