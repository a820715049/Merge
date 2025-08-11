/*
 * @Author: yanfuxing
 * @Date: 2025-05-08 11:20:05
 */
using System.Collections;
using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static fat.conf.Data;
using System.Linq;
using System;

namespace FAT
{
    /// <summary>
    /// 兑换商店主界面
    /// </summary>
    public class UIRedeemShopMain : UIBase
    {
        private Transform _groupTrans;
        private UIVisualGroup _visualGroup;
        private TextProOnACircle _titleText;
        private TextMeshProUGUI titleDescText;
        private TextMeshProUGUI _roundCountText;
        private TextMeshProUGUI _roundShowText;
        private TextMeshProUGUI _cdText;
        private TextMeshProUGUI _redeemCoinNumText;
        private Button _helpBtn;
        private MBRewardIcon _mbRewardIcon;
        private RectTransform _milestoneScrowViewTrans;
        private GameObject _milestoneProCellItem;
        private GameObject _milestoneCellRoot;
        private UICommonProgressBar _mbProgressBar;
        private UIMilestoneProItemCell _milestoneProItemCell;
        private HorizontalLayoutGroup _milestoneLayoutGroup;
        private Button _btnClose;
        private ActivityRedeemShopLike _activityRedeemShopLike;
        private List<GameObject> cellList = new();
        private List<GameObject> cellList1 = new();
        private List<GameObject> cellList2 = new();
        private (int, int) _curProgress; //当前进度条的值

        private Transform _shopRewardPoolParentTrans;
        private Transform _shopRewardPool1CellRoot;
        private Transform _shopRewardPool2CellRoot;
        private Transform _shopRewardPool3CellRoot;
        private GameObject _shopRewardSamllItemCell;
        private GameObject _shopRewardBigItemCell;
        private bool _isRefresh;
        private bool isClickUpdate = false;
        private Action WhenTick;
        private Action<ActivityLike, bool> WhenEnd;




        protected override void OnCreate()
        {
            base.OnCreate();
            _groupTrans = transform.Find("Content");
            _mbRewardIcon = _groupTrans.Access<MBRewardIcon>("Panel/Progress/Reward");
            _visualGroup = transform.Access<UIVisualGroup>();
            _visualGroup.Prepare(_groupTrans.Access<TextProOnACircle>("Panel/Top/Title"), "mainTitle");
            _visualGroup.Prepare(_groupTrans.Access<TextMeshProUGUI>("Panel/Top/TitleDesc"), "titleDesc");
            _visualGroup.Prepare(_groupTrans.Access<TextMeshProUGUI>("Panel/Top/MilestoneRoundInfo/RoundBg/RoundShowText"), "roundShowText");

            _roundCountText = _groupTrans.Access<TextMeshProUGUI>("Panel/Top/MilestoneRoundInfo/RoundCountText");
            _roundShowText = _groupTrans.Access<TextMeshProUGUI>("Panel/Top/MilestoneRoundInfo/RoundBg/RoundShowText");
            _cdText = _groupTrans.Access<TextMeshProUGUI>("Panel/Top/CdTrans/text");
            _redeemCoinNumText = _groupTrans.Access<TextMeshProUGUI>("Panel/RedeemCoinTrans/NumBg/NumText");
            _milestoneCellRoot = _groupTrans.Find("Panel/Progress/MilestoneItemScrollView/Viewport/Content").gameObject;
            _milestoneScrowViewTrans = _groupTrans.Access<RectTransform>("Panel/Progress/MilestoneItemScrollView");
            _milestoneLayoutGroup = _milestoneCellRoot.GetComponent<HorizontalLayoutGroup>();
            _milestoneProCellItem = _groupTrans.Find("Panel/Progress/MilestoneProItem").gameObject;
            _mbProgressBar = _groupTrans.Access<UICommonProgressBar>("Panel/Progress");

            _shopRewardPoolParentTrans = _groupTrans.Access<Transform>("Panel/RewardPoolTrans");
            _shopRewardPool1CellRoot = _shopRewardPoolParentTrans.Access<Transform>("1/Scroll View/Viewport/Content");
            _shopRewardPool2CellRoot = _shopRewardPoolParentTrans.Access<Transform>("2/Scroll View/Viewport/Content");
            _shopRewardPool3CellRoot = _shopRewardPoolParentTrans.Access<Transform>("3/Scroll View/Viewport/Content");

            _shopRewardSamllItemCell = _groupTrans.Find("RedeemRewardSmallItem").gameObject;
            _shopRewardBigItemCell = _groupTrans.Find("RedeemRewardBigItem").gameObject;

            _btnClose = _groupTrans.Access<Button>("Panel/CloseBtn");
            _btnClose.onClick.AddListener(() =>
            {
                Close();
            });

            _helpBtn = _groupTrans.Access<Button>("Panel/Info");
            _helpBtn.onClick.AddListener(() =>
            {
                _activityRedeemShopLike.OpenHelp();
            });

            GameObjectPoolManager.Instance.PreparePool(PoolItemType.RedeemShop_MILESTONE_CELL, _milestoneProCellItem);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.RedeemShop_SMALLREWARD_CELL, _shopRewardSamllItemCell);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.RedeemShop_BIGEWARD_CELL, _shopRewardBigItemCell);

        }
        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            if (items.Length < 1) return;
            _activityRedeemShopLike = (ActivityRedeemShopLike)items[0];
            _curProgress = _activityRedeemShopLike.GetCurMelistonStageScoreProgress();
        }
        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            if (_activityRedeemShopLike == null) return;
            isClickUpdate = false;
            RefreshTheme();
            RefreshCD();
            RefreshMilestonePanel();
            InitRedeemShopPanel(false);
            _mbProgressBar.ForceSetup(0, _curProgress.Item2, _curProgress.Item1);
            //_mbProgressBar.Refresh(_curProgress.Item1, _curProgress.Item2);
            _isRefresh = false;
            WhenTick ??= RefreshCD;
            WhenEnd ??= RefreshEnd;
            if(_activityRedeemShopLike.IsHasCanRedeemReward())
            {
                _activityRedeemShopLike.UpdateLookRedPointFlag(true);
            }
          
            MessageCenter.Get<REDEEMSHOP_BUY_REFRESH>().AddListener(RefreshItemPanel);
            MessageCenter.Get<REDEEMSHOP_PANEL_REFRESH>().AddListener(InitRedeemShopPanel);
            MessageCenter.Get<REDEEMSHOP_DATA_CHANGE>().AddListener(RefreRedeemCoin);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnPostClose()
        {
            base.OnPostClose();

            ClearShopCell();
            ClearMilestoneCell();
        }

        #region 方法实现

        #region 主题刷新
        private void RefreshTheme()
        {
            var visual = _activityRedeemShopLike.VisualUIRedeemShopMain.visual;
            visual.Refresh(_visualGroup);
            _roundCountText.text = _activityRedeemShopLike.CurMileStoneStateNum.ToString();
            var roundCountText = _activityRedeemShopLike.CurMileStoneStateNum + "/" + _activityRedeemShopLike.CurMileStoneStateAllNum;
            _roundShowText.text = I18N.FormatText("#SysComDesc1152", roundCountText);
            _redeemCoinNumText.text = _activityRedeemShopLike.CurRedeemCoinNum.ToString();
        }

        #endregion

        #region 刷新倒计时
        public void RefreshCD()
        {
            var time = _activityRedeemShopLike.Countdown;
            UIUtility.CountDownFormat(_cdText, time);
        }

        protected void RefreshEnd(ActivityLike acti_, bool expire_)
        {
            if (acti_ != _activityRedeemShopLike) return;  //|| !expire_
            Close();
        }

        #endregion

        #region 里程碑刷新
        private void RefreshMilestonePanel()
        {
            if (_activityRedeemShopLike == null || _activityRedeemShopLike.MilestoneNodeList.Count <= 0)
            {
                return;
            }
            float barWidth = _milestoneScrowViewTrans.rect.width;
            var milestoneList = _activityRedeemShopLike.MilestoneNodeList;

            // 获取配置中的分数列表
            List<int> scores = System.Linq.Enumerable.ToList(milestoneList.Select(x => x.milestoneScore));

            // 计算每个分数对应的位置
            for (int i = 0; i < milestoneList.Count; i++)
            {
                // 直接使用分数比例
                float normalized = (float)scores[i] / scores[scores.Count - 1];
                float xPos = barWidth * normalized;

                var cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.RedeemShop_MILESTONE_CELL, _milestoneCellRoot.transform);
                var milestoneData = milestoneList[i];
                RectTransform rt = cell.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(xPos, 0);
                cell.transform.localScale = Vector3.one;

                cell.SetActive(true);
                var item = cell.GetComponent<UIMilestoneProItemCell>();
                var iConf = GetObjBasic(milestoneData.milestoneRewardId);
                item.SetData(iConf.Icon, milestoneData.milestoneRewardCount);
                item.SetFinish(milestoneData.IsDonePro);
                cellList.Add(cell);
            }
            var id = Constant.kMergeEnergyObjId;
            _mbRewardIcon.Refresh(id);
        }

        #endregion

        #region 兑换商店初始化
        private void InitRedeemShopPanel(bool isRefresh)
        {
            _isRefresh = isRefresh;
            if (isRefresh)
            {
                ClearShopCell();
            }
            if (_activityRedeemShopLike == null) return;
            var redeemShopStageDic = _activityRedeemShopLike.RedeemShopStageDic;
            foreach (var item in redeemShopStageDic)
            {
                var rewardPoolId = item.Key;
                var rewardPoolList = item.Value;
                if (rewardPoolList.Count <= 0)
                {
                    DebugEx.Info($"奖池数据为空，rewardPoolId :{rewardPoolId}");
                    continue;
                }
                Transform parentTrans = GetParentTransByRewardPoolType((ShopRewardPoolType)rewardPoolId);
                foreach (var nodeItem in rewardPoolList)
                {
                    var cell = GetCellByShopItemBoardType(nodeItem.BoardType, parentTrans);
                    cell.transform.localPosition = Vector3.zero;
                    cell.transform.localScale = Vector3.one;
                    cell.SetActive(true);
                    if (nodeItem.BoardType == ShopBoardType.Big)
                    {
                        var itemCell = cell.GetComponent<UIRedeemRewardBigItemCell>();
                        var bigItemAnimator = cell.GetComponent<Animator>();
                        var bigItemLockAnimtor = cell.transform.Find("Lock/ani").GetComponent<Animator>();
                        var bigItemAnimtorEvent = cell.transform.Find("Lock/ani").GetComponent<AnimationEvent>();
                        var leftCountAimator = cell.transform.Find("LeftCountImage").GetComponent<Animator>();

                        bigItemAnimator.Rebind(); // 重置动画状态
                        bigItemAnimator.Update(0f); // 立即更新动画状态

                        bigItemAnimtorEvent.SetCallBack(AnimationEvent.AnimationTrigger, () =>
                        {
                            cell.GetComponent<Animator>().SetTrigger("unlock");
                            itemCell.SetBtnState(true);
                            itemCell.SetBtnBgState(true);
                            itemCell.SetBtnFreeState(true);
                            itemCell.SetBtnImageClick(true);
                            ActivityRedeemShopLike.PlaySound(AudioEffect.RedeemUnlock);
                        });
                        cellList2.Add(cell);

                        if (isRefresh)
                        {
                            //如果是刷新状态 并且是CanRedeem状态
                            itemCell.SetBtnState(false);
                        }

                        StartCoroutine(SetAnimByItemState(nodeItem, bigItemLockAnimtor, bigItemAnimator, leftCountAimator, itemCell.gameObject));

                        if (nodeItem.RedeemRewardList.Count >= 1)
                        {
                            var reward1 = GetObjBasic(nodeItem.RedeemRewardList[0].Id);
                            var reward2 = GetObjBasic(nodeItem.RedeemRewardList[1].Id);
                            if (reward1 != null)
                            {
                                itemCell.SetData(reward1.Icon, nodeItem.RedeemRewardList[0].Count, reward2.Icon, nodeItem.RedeemRewardList[1].Count, nodeItem);

                                if (nodeItem.ItemState == RedeemShopItemState.Lock)
                                {
                                    itemCell.SetBtnState(false);
                                    itemCell.SetBtnBgState(false);
                                    itemCell.SetBtnFreeState(false);
                                    itemCell.SetBtnImageClick(true);
                                }
                                if (nodeItem.needRedeemScore <= 0 && nodeItem.ItemState == RedeemShopItemState.CanRedeem)
                                {
                                    itemCell.SetBtnFreeState(true);
                                }
                            }
                        }
                    }
                    else
                    {
                        var itemCell = cell.GetComponent<RedeemRewardItemCell>();
                        var smallItemAnimtor = cell.GetComponent<Animator>();
                        var smallItemLockAnimtor = cell.transform.Find("Lock/ani").GetComponent<Animator>();
                        var smallItemLockAnimtorEvent = cell.transform.Find("Lock/ani").GetComponent<AnimationEvent>();
                        var leftCountAimator = cell.transform.Find("LeftCountImage").GetComponent<Animator>();

                        smallItemAnimtor.Rebind(); // 重置动画状态
                        smallItemAnimtor.Update(0f); // 立即更新动画状态

                        smallItemLockAnimtorEvent.SetCallBack(AnimationEvent.AnimationTrigger, () =>
                        {
                            cell.GetComponent<Animator>().SetTrigger("unlock");
                            itemCell.SetBtnState(true);
                            itemCell.SetBtnBgState(true);
                            itemCell.SetBtnFreeState(true);
                            itemCell.SetBtnImageClick(true);
                            ActivityRedeemShopLike.PlaySound(AudioEffect.RedeemUnlock);
                        });
                        cellList1.Add(cell);

                        if (isRefresh)
                        {
                            //如果是刷新状态 并且是CanRedeem状态
                            itemCell.SetBtnState(false);
                        }
                        StartCoroutine(SetAnimByItemState(nodeItem, smallItemLockAnimtor, smallItemAnimtor, leftCountAimator, itemCell.gameObject));
                        foreach (var reward in nodeItem.RedeemRewardList)
                        {
                            var iConf = GetObjBasic(reward.Id);
                            if (iConf != null)
                            {
                                itemCell.SetData(iConf.Icon, reward.Count, nodeItem);
                            }
                        }
                        if (nodeItem.ItemState == RedeemShopItemState.Lock)
                        {
                            itemCell.SetBtnState(false);
                            itemCell.SetBtnBgState(false);
                            itemCell.SetBtnFreeState(false);
                            itemCell.SetBtnImageClick(true);
                        }
                        if (nodeItem.needRedeemScore <= 0 && nodeItem.ItemState == RedeemShopItemState.CanRedeem)
                        {
                            itemCell.SetBtnFreeState(true);
                        }
                    }
                }
            }
        }

        private GameObject GetCellByShopItemBoardType(ShopBoardType type, Transform parent)
        {
            GameObject cell = null;
            switch (type)
            {
                case ShopBoardType.Small:
                    cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.RedeemShop_SMALLREWARD_CELL, parent);
                    break;
                case ShopBoardType.Big:
                    cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.RedeemShop_BIGEWARD_CELL, parent);
                    break;
            }
            return cell;
        }


        private Transform GetParentTransByRewardPoolType(ShopRewardPoolType type)
        {
            Transform parentTrans = null;
            switch (type)
            {
                case ShopRewardPoolType.Pool1:
                    parentTrans = _shopRewardPool1CellRoot;
                    break;
                case ShopRewardPoolType.Pool2:
                    parentTrans = _shopRewardPool2CellRoot;
                    break;
                case ShopRewardPoolType.Pool3:
                    parentTrans = _shopRewardPool3CellRoot;
                    break;
            }
            return parentTrans;
        }

        #endregion

        #region 清除Clear

        private void ClearShopCell()
        {
            foreach (var item in cellList1)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.RedeemShop_SMALLREWARD_CELL, item);
            }
            cellList1.Clear();

            foreach (var item in cellList2)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.RedeemShop_BIGEWARD_CELL, item);
            }
            cellList2.Clear();
        }


        private void ClearMilestoneCell()
        {
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.RedeemShop_MILESTONE_CELL, item);
            }
            cellList.Clear();
        }


        #endregion

        #region Item动画处理
        private IEnumerator SetAnimByItemState(RedeemShopNodeItem nodeItem, Animator itemLockAnimator, Animator itemAnimator, Animator leftCountAnimator, GameObject cellObj)
        {
            switch (nodeItem.ItemState)
            {
                case RedeemShopItemState.Free:
                    break;
                case RedeemShopItemState.Lock:
                    if (!_isRefresh)
                    {
                        itemAnimator.SetTrigger("lock");
                        itemLockAnimator.Rebind(); // 重置动画状态
                        itemLockAnimator.Update(0f); // 立即更新动画状态
                        yield return null;
                        itemLockAnimator.SetTrigger("close");
                    }
                    else
                    {
                        cellObj.transform.Find("Lock").transform.localScale = Vector3.zero;
                        // 先触发锁定动画
                        itemAnimator.SetTrigger("lock");

                        // 等待一帧确保重置完成
                        yield return null;

                        cellObj.transform.Find("Lock").transform.localScale = Vector3.one;

                        // 再触发关闭动画
                        itemLockAnimator.SetTrigger("close");
                    }
                 
                    break;
                case RedeemShopItemState.CanRedeem:
                    //锁到能领取状态 2.锁打开
                    if (IsPlayUnLockToCanRedeemAnim(nodeItem))
                    {
                        if (isClickUpdate)
                        {
                            SetItemClickState(nodeItem.BoardType, cellObj);
                        }

                        cellObj.transform.Find("Lock").transform.localScale = Vector3.zero;
                        itemAnimator.SetTrigger("lock");
                        itemLockAnimator.SetTrigger("close");
                        // 等待一帧确保重置完成
                        yield return null;
                        cellObj.transform.Find("Lock").transform.localScale = Vector3.one;
                        itemLockAnimator.SetTrigger("open");
                       
                        nodeItem.IsPlayAnim = true;
                        _activityRedeemShopLike.SetRewardPoolPlayStateId(nodeItem.RewardPoolType, 1);
                    }
                    else
                    {
                        if (GetPlayItemAnimStateId(nodeItem.RewardPoolType) == 0)
                        {
                            itemLockAnimator.SetTrigger("open");
                        }
                        else
                        {
                            //仅仅点击状态
                            itemAnimator.SetTrigger("onlyClick");
                            SetItemBtnState(nodeItem.BoardType, cellObj);
                        }
                    }

                    break;
                case RedeemShopItemState.Done:
                    //能领取到完成状态 
                    if (isClickUpdate)
                    {
                        itemAnimator.SetTrigger("finish");
                        ActivityRedeemShopLike.PlaySound(AudioEffect.RedeemSwitch);
                    }
                    else
                    {
                        itemAnimator.SetTrigger("onlyFinish");
                    }

                    if (nodeItem.IsCur)
                    {
                        leftCountAnimator.SetTrigger("Punch");
                    }
                    else
                    {
                        leftCountAnimator.SetTrigger("Hide");
                    }

                    break;
            }
        }

        #endregion

        #region 判断是播放过可兑换动画
        private bool IsPlayUnLockToCanRedeemAnim(RedeemShopNodeItem nodeItem)
        {
            if (nodeItem.IsCur && !nodeItem.IsPlayAnim && GetPlayItemAnimStateId(nodeItem.RewardPoolType) == 0)
            {
                return true;
            }
            return false;
        }

        private int GetPlayItemAnimStateId(ShopRewardPoolType poolType)
        {
            int id = 0;
            switch (poolType)
            {
                case ShopRewardPoolType.Pool1:
                    id = _activityRedeemShopLike._pool1PlayAnimStateId;
                    break;
                case ShopRewardPoolType.Pool2:
                    id = _activityRedeemShopLike._pool2PlayAnimStateId;
                    break;
                case ShopRewardPoolType.Pool3:
                    id = _activityRedeemShopLike._pool3PlayAnimStateId;
                    break;
            }
            return id;
        }

        private void SetItemBtnState(ShopBoardType boardType, GameObject cell)
        {
            if (boardType == ShopBoardType.Big)
            {
                var itemCell = cell.GetComponent<UIRedeemRewardBigItemCell>();
                itemCell.SetBtnState(true);
            }
            else
            {
                var itemCell = cell.GetComponent<RedeemRewardItemCell>();
                itemCell.SetBtnState(true);
            }
        }


        private void SetItemClickState(ShopBoardType boardType, GameObject cell)
        {
            if (boardType == ShopBoardType.Big)
            {
                var itemCell = cell.GetComponent<UIRedeemRewardBigItemCell>();
                itemCell.SetBtnImageClick(false);
            }
            else
            {
                var itemCell = cell.GetComponent<RedeemRewardItemCell>();
                itemCell.SetBtnImageClick(false);
            }
        }

        #endregion

        #region 刷新兑换Panel刷新
        private void RefreshItemPanel(int poolType, int index, int leftNum)
        {
            if (_activityRedeemShopLike.RedeemShopStageDic.ContainsKey(poolType))
            {
                isClickUpdate = true;
                _activityRedeemShopLike.RefreshItemByIndex(index, poolType, leftNum);
                RefreshPanleByPoolType(index, (ShopRewardPoolType)poolType);
                _redeemCoinNumText.text = _activityRedeemShopLike.CurRedeemCoinNum.ToString();
            }
        }

        private void RefreshPanleByPoolType(int index, ShopRewardPoolType poolType)
        {
            var poolList = _activityRedeemShopLike.RedeemShopStageDic[(int)poolType];
            if (poolList != null && poolList.Count > 0)
            {
                var itemData = poolList[index];
                GameObject cell = null;
                if (poolType == ShopRewardPoolType.Pool1)
                {
                    cell = _shopRewardPool1CellRoot.GetChild(index).gameObject;
                }
                else if (poolType == ShopRewardPoolType.Pool2)
                {
                    cell = _shopRewardPool2CellRoot.GetChild(index).gameObject;
                }
                else if (poolType == ShopRewardPoolType.Pool3)
                {
                    cell = _shopRewardPool3CellRoot.GetChild(index).gameObject;
                }

                if (cell != null)
                {
                    if (itemData.BoardType == ShopBoardType.Small)
                    {
                        var itemCell = cell.GetComponent<RedeemRewardItemCell>();
                        var smallItemAnimtor = cell.GetComponent<Animator>();
                        var smallItemLockAnimtor = cell.transform.Find("Lock/ani").GetComponent<Animator>();
                        var leftCountAimator = cell.transform.Find("LeftCountImage").GetComponent<Animator>();

                        if (itemCell != null)
                        {
                            var iConf = GetObjBasic(itemData.RedeemRewardList[0].Id);
                            itemCell.SetData(iConf.Icon, itemData.RedeemRewardList[0].Count, itemData);
                            StartCoroutine(SetAnimByItemState(itemData, smallItemLockAnimtor, smallItemAnimtor, leftCountAimator, itemCell.gameObject));
                        }
                    }
                    else
                    {
                        var itemCell = cell.GetComponent<UIRedeemRewardBigItemCell>();
                        var bigItemAnimator = cell.GetComponent<Animator>();
                        var bigItemLockAnimtor = cell.transform.Find("Lock/ani").GetComponent<Animator>();
                        var leftCountAimator = cell.transform.Find("LeftCountImage").GetComponent<Animator>();

                        if (itemCell != null)
                        {
                            var reward1 = GetObjBasic(itemData.RedeemRewardList[0].Id);
                            var reward2 = GetObjBasic(itemData.RedeemRewardList[1].Id);
                            if (reward1 != null)
                            {
                                itemCell.SetData(reward1.Icon, itemData.RedeemRewardList[0].Count, reward2.Icon, itemData.RedeemRewardList[1].Count, itemData);
                                StartCoroutine(SetAnimByItemState(itemData, bigItemLockAnimtor, bigItemAnimator, leftCountAimator, itemCell.gameObject));
                            }
                        }
                    }
                }

            }
        }

        #endregion

        #region 刷新兑换币数量
        private void RefreRedeemCoin()
        {
            _redeemCoinNumText.text = _activityRedeemShopLike.CurRedeemCoinNum.ToString();
        }
        #endregion

     

        #endregion
        void OnDisable()
        {
            isClickUpdate = false;
            MessageCenter.Get<REDEEMSHOP_BUY_REFRESH>().RemoveListener(RefreshItemPanel);
            MessageCenter.Get<REDEEMSHOP_PANEL_REFRESH>().RemoveListener(InitRedeemShopPanel);
            MessageCenter.Get<REDEEMSHOP_DATA_CHANGE>().RemoveListener(RefreRedeemCoin);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }
    }
    
    public class MilestoneRewardItemCell
    {
        public GameObject cell;
        public UICommonItem item;
        public RewardCommitData data;

        public void Init(GameObject cell_)
        {
            cell = cell_;
            item = cell.GetComponent<UICommonItem>();
        }

        public void SetData(RewardCommitData data_)
        {
            data = data_;
        }

    }
}

