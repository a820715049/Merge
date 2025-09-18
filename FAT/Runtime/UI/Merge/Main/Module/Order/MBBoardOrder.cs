/*
 * @Author: qun.chao
 * @Date: 2023-10-25 12:13:36
 */

using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using FAT.Merge;
using TMPro;
using System;

namespace FAT
{
    public class MBBoardOrder : UIGenericItemBase<IOrderData>
    {
        public interface ICommitButton
        {
            Button BtnCommit { get; }
            void OnDataChange(IOrderData data);
            void OnDataClear();
            void Refresh();
            void RefreshOffset(bool isExtraReward);
        }

        [System.Serializable]
        class Counting
        {
            public GameObject root;
            public RectTransform progress;
            public TextMeshProUGUI txtCounting;
        }

        [System.Serializable]
        class Score
        {
            public GameObject root;
            public UIImageRes icon;
            public TextMeshProUGUI txtScore;
        }


        [Tooltip("标准尺寸")][SerializeField] private float sizeNormal = 288;

        [Tooltip("加长尺寸 需求超过2项时使用")]
        [SerializeField]
        private float sizePlus = 322;

        [Tooltip("间隔 需要叠加到元素尺寸上 | 为了无缝入场/退场 布局上的元素间隔设置为0")]
        [SerializeField]
        private float space = 34;

        [Tooltip("礼盒图标变成奖励需要的时间")]
        [SerializeField]
        private float effOrderBoxConvertRewardDelay = 1.3f;

        [Tooltip("礼盒从棋盘消失需要的时间")]
        [SerializeField]
        private float effOrderBoxDieDelay = 1f;

        [SerializeField] private GameObject goBgLock;
        [SerializeField] private GameObject goBgSimple;
        [SerializeField] private GameObject goBgMultiReward;
        [SerializeField] private Transform requireRoot;
        [SerializeField] private TextMeshProUGUI txtUnlockLevel;
        [SerializeField] private UIImageRes roleImg;
        [SerializeField] private RectTransform bgRect;
        [SerializeField] private MBBoardOrderBox orderBox;

        [Tooltip("订单消失动画的延迟")]
        [SerializeField]
        private float orderDieDelay = 0f;
        // 限时订单倒计时
        [SerializeField] private TextMeshProUGUI txtFlashCountdown;
        // 额外奖励挂接位置
        [SerializeField] private Transform extraRewardRoot;
        // 计数订单
        [SerializeField] private Counting countingGroup;
        // 订单积分
        [SerializeField] private Score scoreGroup;
        // 订单右下角积分
        [SerializeField] private Score scoreGroupBR;
        // 好评订单
        [SerializeField] private Transform orderLikeRoot;

        public string poolKey { get; set; } // itemPoolType

        // https://centurygames.yuque.com/ywqzgn/ne0fhm/ux1astmzw3sars5l#mpJ6E
        // 按文档需求 以组的概念进行排序和换位
        public int sortGroupPrev { get; set; } // 上次的组
        public int sortGroup { get; set; } // 当前的组
        public int sortWeight { get; set; }
        private Tween mTween;

        // 额外奖励
        private MBBoardOrderAttachment att_extraRewardMini = new();
        // 好评订单
        private MBBoardOrderAttachment att_orderLike = new();
        // 进度礼盒
        private MBBoardOrderAttachment att_orderRate = new();
        // 订单助力
        private MBBoardOrderAttachment att_orderBonus = new();
        // 抓宝订单
        private MBBoardOrderAttachment att_clawOrder = new();

        private List<RewardCommitData> orderBoxRewardsToCommit;
        private List<RewardCommitData> normalRewardsToCommit;

        private Button btnCommit => commitButton.BtnCommit;

        // 表示正在完成订单
        // 用于在收集棋子的操作发生时 识别具体是哪个订单需要响应
        private bool isCommitting;      // 表示正在完成订单
        private int consumeIdx = 0;     // 当前正在收集的item序号
        private float commitFlyDelay;   // 收集棋盘item时的延迟fly时间
        private ICommitButton commitButton;

        #region 星想事成
        private Item magicHourRewardItem;
        private IOrderData magicHourTargetOrder;
        #endregion

        public string GetItemThemeKey()
        {
            return MBBoardOrderUtility.GetItemTypeKey(mData);
        }

        #region anim

        public void PlayAnim_Born(float bornDelayForApi)
        {
            _ClearAnim();
            var seq = DOTween.Sequence();
            seq.AppendInterval(bornDelayForApi);
            seq.AppendCallback(_OnBornDelayComplete);
            seq.Append(transform.DOScale(Vector3.one, .5f).From(Vector3.zero, true).OnUpdate(_OnAnimating).SetEase(Ease.OutCubic));
            seq.AppendCallback(_OnAnimComplete_Born);
            mTween = seq;
            seq.Play();
        }

        public void PlayAnim_Die()
        {
            if (_IsInCommitProcess())
                return;
            var dieDelay = mData.OrderType == (int)OrderType.Challenge ? orderDieDelay + 1.5f : orderDieDelay;
            _ClearAnim();
            mTween = transform.DOScale(Vector3.zero, .5f).SetDelay(dieDelay).SetEase(Ease.InOutSine)
                .OnUpdate(_OnAnimating).OnComplete(_OnAnimComplete_Die);
        }

        public void PlayAnim_AddOrderBox()
        {
            _ClearAnim();
            mTween = transform.DOPunchScale(Vector3.one * 0.2f, .5f, 1).OnUpdate(_OnAnimating).OnComplete(_OnAnimating);
        }

        public Transform FindFinishButton()
        {
            return btnCommit.transform;
        }

        private bool _HasMagicHourReward()
        {
            return magicHourRewardItem != null;
        }

        private bool _HasNormalRewardsToCommit()
        {
            return normalRewardsToCommit?.Count > 0;
        }

        private bool _IsInCommitProcess()
        {
            return _HasNormalRewardsToCommit() || _HasMagicHourReward();
        }

        private bool _HasOrderBoxRewardsToCommit()
        {
            return orderBoxRewardsToCommit?.Count > 0;
        }

        private void _OnBornDelayComplete()
        {
            if (mData != null)
            {
                if (mData.ApiStatus == OrderApiStatus.Requesting)
                {
                    if (mData.RemoteOrderResolver?.Invoke(mData) == true)
                    {
                        mData.RemoteOrderResolver = null;
                        _RefreshRequire();
                        _RefreshReward();
                    }
                }
                OrderUtility.TryTrackOrderShow(mData);
                if (mData.State == OrderState.Finished)
                {
                    // 订单完成音效
                    Game.Manager.audioMan.TriggerSound("OrderReward");
                    MessageCenter.Get<MSG.UI_NEWLY_FINISHED_ORDER_SHOW>().Dispatch(transform);
                }
                else
                {
                    // 订单出场音效
                    Game.Manager.audioMan.TriggerSound("OrderEnter");
                }
            }
        }

        private void _OnAnimating()
        {
            MessageCenter.Get<MSG.UI_BOARD_ORDER_ANIMATING>().Dispatch();
        }

        private void _OnAnimComplete_Born()
        {
            MessageCenter.Get<MSG.UI_BOARD_ORDER_ANIMATING>().Dispatch();
            _TryResolveScrollRequest();
        }

        private void _OnAnimComplete_Die()
        {
            MessageCenter.Get<MSG.UI_BOARD_ORDER_TRY_RELEASE>().Dispatch(this);
        }

        private void _ClearAnim()
        {
            mTween?.Kill();
            mTween = null;
        }

        #endregion

        protected override void InitComponents()
        {
            commitButton = GetComponent<ICommitButton>();
            btnCommit.WithClickScale().onClick.AddListener(() => _OnBtnFinish(true));
            orderBox.Init();
#if UNITY_EDITOR
            transform.Access<Button>("Debug", out var btnDebug);
            btnDebug.onClick.AddListener(_OnBtnDebug);
            btnDebug.gameObject.SetActive(true);
#endif
        }

        protected override void UpdateOnDataChange()
        {
            commitButton.OnDataChange(mData);
            orderBox.Setup();
            isCommitting = false;

            _UnRegister();
            _Register();
            _Refresh();
        }

        protected override void UpdateOnDataClear()
        {
            magicHourRewardItem = null;
            magicHourTargetOrder = null;
            commitButton.OnDataClear();
            orderBox.Cleanup();

            StopAllCoroutines();
            _UnRegister();
            _ClearAnim();
            _ForceCommitRewards();
            _ReleaseRewardsContainer();
            att_extraRewardMini.Clear();
            att_orderLike.Clear();
            att_orderRate.Clear();
            att_orderBonus.Clear();
            att_clawOrder.Clear();
        }

        private void _Register()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondPass);
            MessageCenter.Get<MSG.GAME_ORDER_CHANGE>().AddListener(_OnMessageOrderChange);
            MessageCenter.Get<MSG.GAME_ORDER_REFRESH>().AddListener(_OnMessageOrderRefresh);
            MessageCenter.Get<MSG.GAME_ORDER_TRY_FINISH_FROM_UI>().AddListener(_OnMessageTryFinishOrderFromUI);
            MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().AddListener(_OnMessageShopItemInfoChange);
            MessageCenter.Get<MSG.GAME_ORDER_ORDERBOX_BEGIN>().AddListener(_OnMessageOrderBoxBegin);
            MessageCenter.Get<MSG.GAME_ORDER_ORDERBOX_END>().AddListener(_OnMessageOrderBoxEnd);
            MessageCenter.Get<MSG.GAME_ORDER_MAGICHOUR_REWARD_BEGIN>().AddListener(_OnMessageMagicHourRewardBegin);
            MessageCenter.Get<MSG.UI_BOARD_DRAG_ITEM_END>().AddListener(_OnMessageDragItemEnd);
            MessageCenter.Get<MSG.UI_ON_ORDER_ITEM_CONSUMED>().AddListener(_OnMessageOrderCommitItem);
            MessageCenter.Get<MSG.RACE_ROUND_START>().AddListener(_OnMessageRaceRoundStart);
            MessageCenter.Get<MSG.ACTIVITY_ENTRY_LAYOUT_REFRESH>().AddListener(_RefreshScore);
            MessageCenter.Get<MSG.ROCKET_ANIM_COMPLETE>().AddListener(WhenBonusShowEnd);
            MessageCenter.Get<MSG.CLEAR_BONUS>().AddListener(ClearBonus);
            MessageCenter.Get<MSG.GAME_ORDER_TOKEN_MULTI_BEGIN>().AddListener(_OnMessageTokenMultiBegin);
            MessageCenter.Get<MSG.GAME_ORDER_TOKEN_MULTI_END>().AddListener(_OnMessageTokenMultiEnd);
        }

        private void _UnRegister()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondPass);
            MessageCenter.Get<MSG.GAME_ORDER_CHANGE>().RemoveListener(_OnMessageOrderChange);
            MessageCenter.Get<MSG.GAME_ORDER_REFRESH>().RemoveListener(_OnMessageOrderRefresh);
            MessageCenter.Get<MSG.GAME_ORDER_TRY_FINISH_FROM_UI>().RemoveListener(_OnMessageTryFinishOrderFromUI);
            MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().RemoveListener(_OnMessageShopItemInfoChange);
            MessageCenter.Get<MSG.GAME_ORDER_ORDERBOX_BEGIN>().RemoveListener(_OnMessageOrderBoxBegin);
            MessageCenter.Get<MSG.GAME_ORDER_ORDERBOX_END>().RemoveListener(_OnMessageOrderBoxEnd);
            MessageCenter.Get<MSG.GAME_ORDER_MAGICHOUR_REWARD_BEGIN>().RemoveListener(_OnMessageMagicHourRewardBegin);
            MessageCenter.Get<MSG.UI_BOARD_DRAG_ITEM_END>().RemoveListener(_OnMessageDragItemEnd);
            MessageCenter.Get<MSG.UI_ON_ORDER_ITEM_CONSUMED>().RemoveListener(_OnMessageOrderCommitItem);
            MessageCenter.Get<MSG.RACE_ROUND_START>().RemoveListener(_OnMessageRaceRoundStart);
            MessageCenter.Get<MSG.ACTIVITY_ENTRY_LAYOUT_REFRESH>().RemoveListener(_RefreshScore);
            MessageCenter.Get<MSG.ROCKET_ANIM_COMPLETE>().RemoveListener(WhenBonusShowEnd);
            MessageCenter.Get<MSG.CLEAR_BONUS>().RemoveListener(ClearBonus);
            MessageCenter.Get<MSG.GAME_ORDER_TOKEN_MULTI_BEGIN>().RemoveListener(_OnMessageTokenMultiBegin);
            MessageCenter.Get<MSG.GAME_ORDER_TOKEN_MULTI_END>().RemoveListener(_OnMessageTokenMultiEnd);
        }

        private void _Refresh()
        {
            _RefreshTheme();
            // 刷新theme后可能mData失效
            if (mData == null)
                return;
            _RefreshSize();
            if (mData.State == OrderState.PreShow)
            {
                _RefreshRole(true);
                _RefreshLocked();
            }
            else
            {
                _RefreshRole(false);
                _RefreshReward();
                _RefreshRequire();
                _RefreshClaim();
                _RefreshClaimOffset();
                _RefreshOrderBox();
                _RefreshDurationOrder();
                _RefreshCounting();
                _RefreshScore();
                _RefreshScoreBR();
                _RefreshOrderLike();
                _RefreshOrderRate();
                _RefreshOrderBonus();
                _RefreshClawOrder();
            }

            _TryResolveScrollRequest();
        }

        private void _TryResolveScrollRequest()
        {
            if (mData.HasScrollRequest && mData.Displayed)
            {
                mData.HasScrollRequest = false;
                MessageCenter.Get<MSG.UI_ORDER_REQUEST_SCROLL>().Dispatch(transform);
            }
        }

        private void _RefreshTheme()
        {
            var key = GetItemThemeKey();
            if (!string.Equals(key, poolKey))
            {
                // 需要按照当前样式重新加载
                MessageCenter.Get<MSG.UI_BOARD_ORDER_RELOAD>().Dispatch(mData, key);
            }
        }

        private void _RefreshSize()
        {
            var size = sizeNormal;
            if (mData.State != OrderState.PreShow)
            {
                int count = 0;
                foreach (var item in mData.Requires)
                {
                    count += item.TargetCount;
                }

                size = count > 2 ? sizePlus : sizeNormal;
            }

            // 元素自带间隔
            size += space;
            var trans = transform as RectTransform;
            trans.sizeDelta = new Vector2(size, trans.sizeDelta.y);
            bgRect.sizeDelta = new Vector2(-space, bgRect.rect.height);
        }

        private void _RefreshRole(bool locked)
        {
            var role = Game.Manager.npcMan.GetNpcConfig(mData.RoleId);
            if (role != null)
            {
                roleImg.SetImage(role.OrderImage);
            }

            if (locked)
                GameUIUtility.SetGrayShader(roleImg.image);
            else
                GameUIUtility.SetDefaultShader(roleImg.image);
        }

        private void _RefreshLocked()
        {
            goBgLock.SetActive(true);
            goBgSimple.SetActive(false);
            goBgMultiReward.SetActive(false);
            requireRoot.gameObject.SetActive(false);
            btnCommit.gameObject.SetActive(false);
            transform.Find("AutoGuide").gameObject.SetActive(false);
            orderBox.gameObject.SetActive(false);
            _SetCountingState(false);
            _SetCountdownState(false);
            _SetScoreState(false);
            _SetScoreBRState(false);
            txtUnlockLevel.text = $"{mData.UnlockLevel}";
        }

        private void _RefreshReward()
        {
            goBgLock.SetActive(false);
            goBgSimple.SetActive(false);
            goBgMultiReward.SetActive(false);
            var rewardBg = _GetRewardBg();
            rewardBg.SetActive(true);
            var rewardRoot = _GetRewardRoot(rewardBg.transform);

            UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.SmartPlus);
            _RefreshReward_NormalSlot(rewardRoot);
            _RefreshReward_ExtraSlot();
            UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.NoPrefix);
        }

        private int _FindExtraSlotRewardIndex()
        {
            var (id, num) = mData.ExtraRewardMini;
            // 不需要额外展示位
            if (id <= 0 || num <= 0)
                return -1;
            for (var i = 0; i < mData.Rewards.Count; i++)
            {
                if (mData.Rewards[i].Id == id && mData.Rewards[i].Count == num)
                    return i;
            }
            return -1;
        }

        // 常规位置奖励
        private void _RefreshReward_NormalSlot(Transform rewardRoot)
        {
            var extraSlotIdx = _FindExtraSlotRewardIndex();
            var rewards = mData.Rewards;
            for (int i = 0, slotIdx = 0; i < rewards.Count; i++)
            {
                if (i == extraSlotIdx || slotIdx >= rewardRoot.childCount)
                    continue;
                var rewardItem = rewardRoot.GetChild(slotIdx).GetComponent<MBBoardOrderRewardItem>();
                rewardItem.gameObject.SetActive(true);
                rewardItem.SetData(mData.Rewards[i]);
                ++slotIdx;
            }
        }

        // 额外位置奖励
        private void _RefreshReward_ExtraSlot()
        {
            var extraSlotIdx = _FindExtraSlotRewardIndex();
            if (extraSlotIdx < 0)
            {
                att_extraRewardMini.Clear();
                return;
            }
            var reward = mData.Rewards[extraSlotIdx];
            if (mData.TryGetExtraRewardMiniRes(out var res))
            {
                att_extraRewardMini.RefreshAttachment(res, extraRewardRoot, (obj) =>
                {
                    var rewardItem = obj.GetComponent<MBBoardOrderRewardItem>();
                    UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.SmartPlus);
                    rewardItem.SetData(reward);
                    UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.NoPrefix);
                    _RefreshClaimOffset();
                });
            }
            else
            {
                // 配置无效 不能显示
                att_extraRewardMini.Clear();
            }
        }

        private void _RefreshScore()
        {
            var score = mData.Score;
            if (score <= 0)
            {
                _SetScoreState(false);
            }
            else
            {
                var act = Game.Manager.activity.Lookup(mData.GetValue(OrderParamType.ScoreEventId));
                if (act is ActivityScore actScore && actScore.HasCycleMilestone())
                {
                    actScore.Visual.RefreshStyle(scoreGroup.txtScore, actScore.themeFontStyleId_Score);
                    var coinId = actScore.ConfD.RequireCoinId;
                    scoreGroup.icon.SetImage(Game.Manager.objectMan.GetBasicConfig(coinId).Image);
                    scoreGroup.txtScore.text = $"{score}";
                    _SetScoreState(true);
                }
                else if (act is ActivityRace && RaceManager.GetInstance().RaceStart)
                {
                    //act.Visual.RefreshStyle(scoreGroup.txtScore, act.themeFontStyleId_Score);
                    var coinID = RaceManager.GetInstance().Race.ConfD.RequireScoreId;
                    scoreGroup.icon.SetImage(Game.Manager.objectMan.GetBasicConfig(coinID).Image);
                    scoreGroup.txtScore.text = $"{score}";
                    _SetScoreState(true);
                }
                else if (act is ActivityDuel activityDuel && !activityDuel.HasComplete() && activityDuel.RoundActive)
                {
                    activityDuel.Visual.RefreshStyle(scoreGroup.txtScore, "score");
                    var coinID = activityDuel.Conf.TokenId;
                    scoreGroup.icon.SetImage(Game.Manager.objectMan.GetBasicConfig(coinID).Image);
                    scoreGroup.txtScore.text = $"{score}";
                    _SetScoreState(true);
                }
                else if (act is ActivityMultiplierRanking ranking)
                {
                    var coinID = ranking.conf.Token;
                    scoreGroup.icon.SetImage(Game.Manager.objectMan.GetBasicConfig(coinID).Image);
                    scoreGroup.txtScore.text = $"{score}";
                    _SetScoreState(true);
                }
                else if (act is ActivityScoreMic activityScoreMic && !activityScoreMic.IsComplete())
                {
                    _RefreshScoreMic(activityScoreMic, score);
                }
                else
                {
                    _SetScoreState(false);
                }
            }
        }

        //刷新订单右下角积分
        private void _RefreshScoreBR()
        {
            var score = mData.ScoreBR;
            if (score <= 0)
            {
                _SetScoreBRState(false);
            }
            else
            {
                var act = Game.Manager.activity.Lookup(mData.GetValue(OrderParamType.ScoreEventIdBR));
                if (act is MineBoardActivity mineBoardActivity && Game.Manager.mineBoardMan.IsValid)
                {
                    act.Visual.RefreshStyle(scoreGroupBR.txtScore, "mineOrderScore");
                    var coinID = mineBoardActivity.ConfD.TokenId;
                    scoreGroupBR.icon.SetImage(Game.Manager.objectMan.GetBasicConfig(coinID).Image);
                    scoreGroupBR.txtScore.text = $"{score}";
                    _SetScoreBRState(true);
                }
                else if (act is FarmBoardActivity farmBoardActivity && farmBoardActivity.Valid)
                {
                    act.Visual.RefreshStyle(scoreGroupBR.txtScore, "farmOrderScore");
                    var coinID = farmBoardActivity.ConfD.TokenId;
                    scoreGroupBR.icon.SetImage(Game.Manager.objectMan.GetBasicConfig(coinID).Image);
                    scoreGroupBR.txtScore.text = $"{score}";
                    _SetScoreBRState(true);
                }
                else if (act is WishBoardActivity wish && wish.Valid)
                {
                    act.Visual.RefreshStyle(scoreGroupBR.txtScore, "wishorderScore");
                    scoreGroupBR.icon.SetImage(Game.Manager.objectMan.GetBasicConfig(data.ScoreRewardBR).Icon);
                    scoreGroupBR.txtScore.text = $"{score}";
                    _SetScoreBRState(true);
                }
                else if (act is MineCartActivity mineCart && mineCart.Valid)
                {
                    act.Visual.RefreshStyle(scoreGroupBR.txtScore, "mineCartOrderScore");
                    scoreGroupBR.icon.SetImage(Game.Manager.objectMan.GetBasicConfig(data.ScoreRewardBR).Icon);
                    scoreGroupBR.txtScore.text = $"{score}";
                    _SetScoreBRState(true);
                }
                else
                {
                    _SetScoreBRState(false);
                }
            }
        }

        private void _RefreshCounting()
        {
            if (!mData.IsCounting)
            {
                _SetCountingState(false);
            }
            else
            {
                var max = mData.OrderCountRequire;
                var progress = Mathf.Min(mData.OrderCountTotal - mData.OrderCountFrom, max);
                countingGroup.txtCounting.text = $"{progress}/{max}";
                countingGroup.progress.anchorMax = new Vector2(progress * 1f / max, 1);
                _SetCountingState(true);
            }
        }

        private void _RefreshDurationOrder()
        {
            if (mData == null)
                return;
            if (!mData.IsFlash && !mData.IsStep && mData.BonusID == 0)
            {
                _SetCountdownState(false);
            }
            else
            {
                if (mData.Duration > 0)
                {
                    UIUtility.CountDownFormat(txtFlashCountdown, mData.Countdown);
                    _SetCountdownState(true);
                    if (mData.State != OrderState.Rewarded && mData.IsExpired)
                    {
                        // 订单未完成 但是过期
                        // 主动触发dirty 使订单刷新
                        var tracer = Game.Manager.mergeBoardMan.activeTracer;
                        tracer.Invalidate();
                    }
                }
                else if (mData.BonusEndTime - Game.Instance.GetTimestampSeconds() > 0)
                {
                    _SetCountdownState(!mData.needBonusAnim);
                    var diff = mData.BonusEndTime - Game.Instance.GetTimestampSeconds();
                    UIUtility.CountDownFormat(txtFlashCountdown, diff);
                }
                else
                {
                    _SetCountdownState(false);
                }
            }
        }

        private void _SetCountdownState(bool show)
        {
            txtFlashCountdown.transform.parent.gameObject.SetActive(show);
        }

        private void _SetCountingState(bool show)
        {
            countingGroup.root.SetActive(show);
        }

        private void _SetScoreState(bool show)
        {
            scoreGroup.root.SetActive(show);
        }

        private void _SetScoreBRState(bool show)
        {
            scoreGroupBR.root.SetActive(show);
        }

        private GameObject _GetRewardBg()
        {
            // 默认的奖励槽数量
            var defaultSlotCount = mData.Rewards.Count;
            if (mData.HasExtraRewardMini)
            {
                --defaultSlotCount;
            }
            return defaultSlotCount > 1 ? goBgMultiReward : goBgSimple;
        }

        private Transform _GetRewardRoot(Transform rewardBg)
        {
            return rewardBg.Find("Head");
        }

        /*
        将堆叠记录的订单需求平铺展示
        意思是相同id物品按顺序排列
        */
        private void _RefreshRequire()
        {
            requireRoot.gameObject.SetActive(mData.Requires.Count > 0);
            var idx = 0;
            var finished = mData.State == OrderState.Finished || mData.State == OrderState.Rewarded;
            foreach (var req in mData.Requires)
            {
                for (int i = 0; i < req.TargetCount; i++)
                {
                    var container = requireRoot.GetChild(idx);
                    if (container != null)
                    {
                        container.gameObject.SetActive(true);
                        var item = container.GetComponent<MBBoardOrderRequireItem>();
                        item.SetData(req.Id, req.CurCount > i, finished);
                        item.RefreshTagShop();
                        ++idx;
                    }
                }
            }

            for (int i = idx; i < requireRoot.childCount; i++)
            {
                requireRoot.GetChild(i).gameObject.SetActive(false);
            }
        }

        private bool _IsFinished()
        {
            return mData.State == OrderState.Finished;
        }

        private void _RefreshClaim()
        {
            commitButton.Refresh();
            transform.Find("AutoGuide").gameObject.SetActive(btnCommit.gameObject.activeInHierarchy);
        }

        private void _RefreshClaimOffset()
        {
            commitButton.RefreshOffset(att_extraRewardMini.HasLoaded || att_orderRate.HasLoaded || att_orderBonus.HasLoaded || att_clawOrder.HasLoaded);
        }

        private void _OnBtnFinish(bool needConfirm)
        {
            if (!_IsFinished())
                return;

            normalRewardsToCommit = ObjectPool<List<RewardCommitData>>.GlobalPool.Alloc();
            orderBoxRewardsToCommit = ObjectPool<List<RewardCommitData>>.GlobalPool.Alloc();
            consumeIdx = 0;
            isCommitting = true;
            if (BoardViewWrapper.TryFinishOrder(mData, normalRewardsToCommit, orderBoxRewardsToCommit, needConfirm))
            {
                // 好评订单
                _TryClaimOrderLike();
                // 积分
                _TryClaimScore();
                // 积分 - 右下角
                _TryClaimScoreBR();
                // 进度礼盒
                _TryClaimOrderRate();
                // 订单助力
                _TryClaimOrderBonus();
                // 抓宝订单
                _TryClaimClawOrder();
                StartCoroutine(_CoClaimRewards());
                MessageCenter.Get<MSG.ORDER_FINISH>().Dispatch();
                MessageCenter.Get<MSG.ORDER_FINISH_DATA>().Dispatch(mData);
            }
            else
            {
                _ReleaseRewardsContainer();
            }

            isCommitting = false;
        }

        private bool _IsAllRequiredItemConsumed(int requireCount)
        {
            for (int i = 0; i < requireCount; i++)
            {
                var item = requireRoot.GetChild(i).GetComponent<MBBoardOrderRequireItem>();
                if (!item.IsInConsumedState())
                    return false;
            }

            return true;
        }

        private bool _TryClaimScore()
        {
            if (_CheckShouldCommitScore())
            {
                MessageCenter.Get<MSG.ON_COMMIT_ORDER>().Dispatch(mData.Score);
                //订单上积分部分的表现
                var state = UIUtility.CheckScreenViewState(transform as RectTransform);
                var delay = UIUtility.GetScreenViewStateDelay(state);
                scoreGroup.icon.transform.DOScale(1.5f, 0.2f).SetDelay(delay).OnComplete(() =>
                {
                    //订单积分表现结束
                    MessageCenter.Get<MSG.ORDER_SCORE_ANIM_COMPLETE>().Dispatch(scoreGroup.icon.transform.position, true);
                    scoreGroup.icon.transform.localScale = Vector3.one;
                });

                return true;
            }

            return false;
        }

        private bool _CheckShouldCommitScore()
        {
            // 无分数
            if (mData.Score <= 0)
                return false;
            var act = Game.Manager.activity.Lookup(mData.GetValue(OrderParamType.ScoreEventId));
            if (act is IActivityComplete activityComplete && !activityComplete.IsActive)
            {
                return false;
            }
            if (act is ActivityRace)
            {
                // 分数来自竞赛活动 比赛进行中分数才有效
                return RaceManager.GetInstance().RaceStart;
            }
            if (act is ActivityScore activityScore)
            {
                return activityScore.HasCycleMilestone();
            }
            if (act is ActivityScoreMic activityScoreMic)
            {
                return !activityScoreMic.IsComplete();
            }
            // 其他情况分数都有效
            return true;
        }

        //尝试领取订单右下角积分奖励
        private bool _TryClaimScoreBR()
        {
            if (_CheckShouldCommitScoreBR())
            {
                //override发放奖励
                if (mData.ScoreRewardBR > 0)
                {
                    var act = Game.Manager.activity.Lookup(mData.GetValue(OrderParamType.ScoreEventIdBR));
                    if (act is WishBoardActivity wish && wish.Valid)
                    {
                        var reward = Game.Manager.rewardMan.BeginReward(mData.GetValue(OrderParamType.ScoreRewardBR), mData.GetValue(OrderParamType.ScoreBR), ReasonString.wish_order);
                        Game.Manager.rewardMan.CommitReward(reward);
                        UIFlyUtility.FlyCustom(reward.rewardId, reward.rewardCount, scoreGroupBR.icon.transform.position, UIFlyFactory.ResolveFlyTarget(FlyType.WishBoardToken),
                            FlyStyle.Reward, FlyType.WishBoardToken);
                        DataTracker.event_wish_getitem_order.Track(wish, wish.GetCurProgressPhase() + 1, wish.GetCurGroupConfig().BarRewardId.Count,
                            wish.GetCurGroupConfig().Diff, Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, 1, wish.CurDepthIndex, reward.rewardId,
                            ItemUtility.GetItemLevel(reward.rewardId), reward.rewardCount, mData.GetValue(OrderParamType.PayDifficulty));
                    }
                    else if (act is MineCartActivity mineCart && mineCart.Valid)
                    {
                        var reward = Game.Manager.rewardMan.BeginReward(mData.ScoreRewardBR, mData.ScoreBR, ReasonString.mine_cart_order);
                        //直接commit
                        Game.Manager.rewardMan.CommitReward(reward);
                        //FlyCustom 为纯表现 不会执行commit逻辑
                        UIFlyUtility.FlyCustom(reward.rewardId, reward.rewardCount, scoreGroupBR.icon.transform.position, UIFlyFactory.ResolveFlyTarget(FlyType.MineCartGetItem),
                            FlyStyle.Reward, FlyType.MineCartGetItem);
                        //打点
                        mineCart.TrackOrderGetItem(reward.rewardId, reward.rewardCount, mData.GetValue(OrderParamType.PayDifficulty));
                    }
                }
                else
                {
                    MessageCenter.Get<MSG.ON_COMMIT_ORDER_BR>().Dispatch(mData.ScoreBR);
                    //订单上积分部分的表现
                    var state = UIUtility.CheckScreenViewState(transform as RectTransform);
                    var delay = UIUtility.GetScreenViewStateDelay(state);
                    scoreGroupBR.root.transform.DOScale(1.5f, 0.2f).SetDelay(delay).OnComplete(() =>
                    {
                        //订单积分表现结束
                        MessageCenter.Get<MSG.ORDER_SCORE_ANIM_COMPLETE>().Dispatch(scoreGroupBR.icon.transform.position, false);
                        scoreGroupBR.root.transform.localScale = Vector3.one;
                    });
                    return true;
                }
            }
            return false;
        }

        //检查是否可以领取右下角分数
        private bool _CheckShouldCommitScoreBR()
        {
            // 无分数
            if (mData.ScoreBR <= 0)
                return false;
            var act = Game.Manager.activity.Lookup(mData.GetValue(OrderParamType.ScoreEventIdBR));
            if (act is MineBoardActivity)
            {
                //挖矿棋盘数据有效时才算分
                return Game.Manager.mineBoardMan.IsValid;
            }
            if (act is FarmBoardActivity farmBoardActivity)
            {
                //农场棋盘数据有效时才算分
                return farmBoardActivity.Valid;
            }
            if (act is WishBoardActivity wish)
            {
                return wish.Valid;
            }
            if (act is MineCartActivity mineCart)
            {
                return mineCart.Valid;
            }
            // 其他情况分数都有效
            return true;
        }

        private float _TryClaimMagicHourRewards()
        {
            var waitTime = 0f;
            if (_HasMagicHourReward())
            {
                if (commitButton is MBBoardOrderCommitButton_Magic magic)
                {
                    waitTime = magic.ShowMagicOutputEffect(magicHourRewardItem, magicHourTargetOrder);
                }
                magicHourRewardItem = null;
                magicHourTargetOrder = null;
            }
            return waitTime;
        }

        private void _TryClaimNormalRewards()
        {
            // 飞常规奖励
            if (_HasNormalRewardsToCommit())
            {
                var rewardBg = _GetRewardBg();
                var rewardRoot = _GetRewardRoot(rewardBg.transform);
                var extraSlotIdx = _FindExtraSlotRewardIndex();
                var rewardCount = normalRewardsToCommit.Count;
                for (int i = 0, slotIdx = 0; i < rewardCount; i++)
                {
                    if (i == extraSlotIdx)
                    {
                        // 从额外展示位飞奖励
                        if (att_extraRewardMini.HasLoaded)
                        {
                            UIFlyUtility.FlyReward(normalRewardsToCommit[i], att_extraRewardMini.AttachedObject.transform.GetChild(0).position);
                            att_extraRewardMini.AttachedObject.SetActive(false);
                        }
                    }
                    else
                    {
                        // 从常规位置飞奖励
                        if (slotIdx < rewardRoot.childCount)
                        {
                            var item = rewardRoot.GetChild(slotIdx);
                            UIFlyUtility.FlyReward(normalRewardsToCommit[i], item.GetChild(0).position);
                            item.gameObject.SetActive(false);
                        }
                        ++slotIdx;
                    }
                }
            }
        }

        private void _TryClaimOrderBoxRewards()
        {
            if (_HasOrderBoxRewardsToCommit())
            {
                foreach (var reward in orderBoxRewardsToCommit)
                {
                    UIFlyUtility.FlyReward(reward, orderBox.transform.position);
                }
            }
        }

        private IEnumerator _CoClaimRewards()
        {
            float delay = 0f;
            float preEffTime = Time.timeSinceLevelLoad;

            // 礼盒转变成奖励
            if (_HasOrderBoxRewardsToCommit())
            {
                delay = effOrderBoxConvertRewardDelay;
                orderBox.ShowReward(orderBoxRewardsToCommit[0]);
                orderBox.PlayOpenBoxEffect();
            }

            // 等待棋子都被扣除
            int count = _GetRequiredItemNum();
            yield return new WaitUntil(() => OrderUtility.isDebug || _IsAllRequiredItemConsumed(count));

            // 等待礼盒转换特效已结束
            yield return new WaitUntil(() => Time.timeSinceLevelLoad > preEffTime + delay);

            // 飞星想事成奖励
            var waitTime = _TryClaimMagicHourRewards();
            if (waitTime > 0)
                yield return new WaitForSeconds(waitTime);

            // 飞常规奖励
            _TryClaimNormalRewards();

            // 飞礼盒奖励
            _TryClaimOrderBoxRewards();

            _ReleaseRewardsContainer();

            PlayAnim_Die();
        }

        private bool _CanUseOrderBox()
        {
            return mData?.ProviderType == (int)OrderProviderType.Random;
        }

        private void _OnSecondPass()
        {
            _RefreshDurationOrder();
        }

        private void _OnMessageOrderChange(List<IOrderData> changedOrders, List<IOrderData> newlyAddedOrders)
        {
            foreach (var order in changedOrders)
            {
                if (order == mData)
                {
                    _Refresh();
                    break;
                }
            }
        }

        private void _OnMessageOrderRefresh(IOrderData order)
        {
            if (order == mData)
            {
                _Refresh();
            }
        }

        private void _OnMessageTryFinishOrderFromUI(IOrderData order, bool needConfirm)
        {
            if (order == mData)
            {
                _OnBtnFinish(needConfirm);
            }
        }

        private void _OnMessageShopItemInfoChange()
        {
            for (int i = 0; i < requireRoot.childCount; i++)
            {
                var container = requireRoot.GetChild(i);
                if (container.gameObject.activeSelf)
                {
                    var item = container.GetComponent<MBBoardOrderRequireItem>();
                    item.RefreshTagShop();
                }
            }
        }

        private void _OnMessageDragItemEnd(Vector2 screenPos, Merge.Item item)
        {
            var order = mData;
            if (order != null && order.State == OrderState.Finished && !item.HasComponent(ItemComponentType.Bubble))
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(transform as RectTransform, screenPos))
                {
                    var idx = order.Requires.FindIndex(x => x.Id == item.tid);
                    if (idx >= 0)
                    {
                        // 预先注册准备优先消耗的item
                        BoardViewManager.Instance.world.AddPriorityConsumeItem(item);
                        _OnBtnFinish(true);
                    }
                }
            }
        }

        private void _OnMessageOrderCommitItem(int tid, Vector3 worldPos)
        {
            if (isCommitting)
            {
                int idx = consumeIdx;
                // 尝试让订单移动到画面内
                if (idx == 0)
                {
                    var state = UIUtility.CheckScreenViewState(transform as RectTransform);
                    commitFlyDelay = UIUtility.GetScreenViewStateDelay(state);
                    if (state != UIUtility.ScreenViewState.Inside)
                    {
                        var contentRoot = transform.parent.parent as RectTransform;
                        var viewRoot = contentRoot.parent as RectTransform;
                        if (UIUtility.CalcScrollPosForTargetInMiddleOfView(transform as RectTransform, contentRoot, viewRoot, out var pos))
                        {
                            MessageCenter.Get<MSG.UI_ORDER_ADJUST_SCROLL>().Dispatch(pos, commitFlyDelay);
                        }
                    }
                }
                ++consumeIdx;

                _CoDelayCollectBoardItem(tid, worldPos, commitFlyDelay, idx);
            }
        }

        private void _CoDelayCollectBoardItem(int tid, Vector3 worldPos, float waitTime, int consumeIdx)
        {
            // item从棋盘飞到订单
            UIFlyUtility.FlyCompleteOrder(tid, 1, worldPos, waitTime, () => _GetFlyTargetByRequireIdx(consumeIdx),
                () => _OnOrderItemFlyEnd(mData, consumeIdx));
        }

        private int _GetRequiredItemNum()
        {
            int count = 0;
            for (int i = 0; i < requireRoot.childCount; i++)
            {
                var item = requireRoot.GetChild(i);
                if (item.gameObject.activeSelf)
                    ++count;
                else
                    break;
            }

            return count;
        }

        private Vector3 _GetFlyTargetByRequireIdx(int idx)
        {
            if (idx < requireRoot.childCount)
            {
                var item = requireRoot.GetChild(idx);
                return item.transform.position;
            }
            return Vector3.zero;
        }

        private void _OnOrderItemFlyEnd(IOrderData orderInst, int idx)
        {
            if (orderInst != mData)
                return;
            if (idx < requireRoot.childCount && _IsInCommitProcess())
            {
                var item = requireRoot.GetChild(idx).GetComponent<MBBoardOrderRequireItem>();
                item.SetOrderConsumeState();

                // 添加炸开效果
                var itemType = BoardUtility.EffTypeToPoolType(ItemEffectType.OrderItemConsumed);
                var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.Effect);
                var eff = GameObjectPoolManager.Instance.CreateObject(itemType, effRoot);
                eff.transform.position = item.transform.position;
                eff.SetActive(true);
                BoardUtility.AddAutoReleaseComponent(eff, 3f, itemType);
            }
        }

        private void _ForceCommitRewards()
        {
            if (normalRewardsToCommit != null)
            {
                foreach (var reward in normalRewardsToCommit)
                {
                    Game.Manager.rewardMan.CommitReward(reward);
                }
            }

            if (orderBoxRewardsToCommit != null)
            {
                foreach (var reward in orderBoxRewardsToCommit)
                {
                    Game.Manager.rewardMan.CommitReward(reward);
                }
            }
        }

        private void _ReleaseRewardsContainer()
        {
            if (normalRewardsToCommit != null)
            {
                ObjectPool<List<RewardCommitData>>.GlobalPool.Free(normalRewardsToCommit);
                normalRewardsToCommit = null;
            }

            if (orderBoxRewardsToCommit != null)
            {
                ObjectPool<List<RewardCommitData>>.GlobalPool.Free(orderBoxRewardsToCommit);
                orderBoxRewardsToCommit = null;
            }
        }

        #region extra reward
        #endregion

        #region orderlike

        private void _RefreshOrderLike()
        {
            if (mData.State == OrderState.Rewarded)
            {
                return;
            }
            if (mData.LikeNum <= 0)
            {
                att_orderLike.Clear();
                return;
            }
            if (mData.TryGetOrderLikeRes(out var res))
            {
                att_orderLike.RefreshAttachment(res, orderLikeRoot, obj =>
                {
                    var rewardItem = obj.GetComponent<MBBoardOrderRewardItem>();
                    UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.StartsWithPlus);
                    rewardItem.SetData(mData.LikeId, mData.LikeNum);
                    UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.NoPrefix);
                    _RefreshClaimOffset();
                });
            }
            else
            {
                att_orderLike.Clear();
            }
        }

        private bool _TryClaimOrderLike()
        {
            var id = mData.LikeId;
            var num = mData.LikeNum;
            if (id <= 0 || num <= 0)
                return false;

            Game.Manager.rewardMan.PushContext(new RewardContext() { paramProvider = new OrderLikeParamProvider() { orderId = mData.Id } });
            var commitData = Game.Manager.rewardMan.BeginReward(id, num, ReasonString.order_like);
            Game.Manager.rewardMan.PopContext();
            var targetTrans = orderLikeRoot.GetChild(0) as RectTransform;
            var targetPos = targetTrans.TransformPoint(UIUtility.GetLocalCenterInRect(targetTrans));
            UIFlyUtility.FlyReward(commitData, targetPos);
            att_orderLike.Clear();
            return true;
        }

        #endregion

        #region orderRate

        private void _RefreshOrderRate()
        {
            if (mData.State == OrderState.Rewarded)
            {
                return;
            }
            if (mData.RateId <= 0 || mData.IsClawOrder)
            {
                att_orderRate.Clear();
                return;
            }
            if (mData.TryGetOrderRateRes(out var res))
            {
                att_orderRate.RefreshAttachment(res, extraRewardRoot, obj =>
                {
                    var rewardItem = obj.GetComponent<MBOrderRateReward>();
                    UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.StartsWithPlus);
                    rewardItem.SetReward(mData.RateId, mData.RateNum, true, mData.Id);
                    UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.NoPrefix);
                    _RefreshClaimOffset();
                });
            }
            else
            {
                att_orderRate.Clear();
            }
        }

        private bool _TryClaimOrderRate()
        {
            var id = mData.RateId;
            var num = mData.RateNum;
            if (id <= 0 || num <= 0 || mData.IsClawOrder)
                return false;

            Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderRate, out var act);
            mData.TryGetOrderRateRes(out var res);
            if (act is ActivityOrderRate orderRate && act.Id == mData.GetValue(OrderParamType.ExtraSlot_TR_EventId))
            {
                orderRate.TryClaimReward(mData.Id, id, extraRewardRoot.position);
            }
            att_orderRate.RefreshAttachment(res, extraRewardRoot, obj =>
            {
                var rewardItem = obj.GetComponent<MBOrderRateReward>();
                rewardItem.ClearReward();
            });
            return true;
        }

        private void ClearBonus(int id)
        {
            if (mData.BonusEventID != id) return;
            mData.BonusID = 0;
            mData.BonusEndTime = 0;
            mData.BonusEventID = 0;
            mData.BonusPhase = 0;
            att_orderBonus.Clear();
            _RefreshTheme();
        }

        private void _RefreshOrderBonus()
        {
            if (mData.State == OrderState.Rewarded)
            {
                return;
            }
            if (mData.BonusID <= 0)
            {
                att_orderBonus.Clear();
                return;
            }
            if (mData.TryGetOrderBonusRes(out var res))
            {
                att_orderBonus.RefreshAttachment(res, extraRewardRoot, obj =>
                {
                    var rewardItem = obj.GetComponent<MBOrderRewardBonus>();
                    UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.StartsWithPlus);
                    UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.NoPrefix);
                    rewardItem.SetReward(mData.BonusPhase, mData.BonusID);
                    if (mData.needBonusAnim) rewardItem.PlayShowAnim();
                    var role = transform.GetComponent<MBOrderItemBonus>();
                    if (role != null)
                    {
                        role.SetReward(mData.BonusPhase);
                    }
                    _RefreshClaimOffset();
                });
            }
            else
            {
                att_orderBonus.Clear();
            }
        }

        private bool _TryClaimOrderBonus()
        {
            var id = mData.BonusID;
            if (id <= 0)
                return false;

            Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderBonus, out var act);
            mData.TryGetOrderBonusRes(out var res);
            if (act is ActivityOrderBonus orderBonus)
            {
                var reward = orderBonus.TryClaimReward(mData);
                if (reward != null)
                    UIFlyUtility.FlyReward(reward, extraRewardRoot.GetChild(0).GetChild(0).position);
            }
            att_orderBonus.RefreshAttachment(res, extraRewardRoot, obj =>
            {
                var rewardItem = obj.GetComponent<MBOrderRewardBonus>();
                rewardItem.PlayAnim();
            });
            return true;
        }

        private void WhenBonusShowEnd()
        {
            if (mData.needBonusAnim)
            {
                mData.needBonusAnim = false;
                _RefreshTheme();
            }
        }

        #endregion

        #region ClawOrder / 抓宝订单

        private void _RefreshClawOrder()
        {
            if (mData.State == OrderState.Rewarded)
                return;
            if (!mData.IsClawOrder)
                return;
            if (mData.TryGetClawOrderRes(out var res))
            {
                att_clawOrder.RefreshAttachment(res, extraRewardRoot, obj => _RefreshSlotReward(obj, OrderAttachmentUtility.slot_extra_tr));
            }
            else
            {
                att_clawOrder.Clear();
            }
        }

        private void _TryClaimClawOrder()
        {
            if (!mData.IsClawOrder)
                return;
            var slot = OrderAttachmentUtility.slot_extra_tr;
            var id = slot.GetEventId(mData);
            if (id <= 0)
                return;
            if (Game.Manager.activity.Lookup(id, out var act) && act is ActivityClawOrder claw)
            {
                var reward = slot.GetReward(mData);
                if (claw.TryClaimOrderToken(mData, reward.id, reward.num, out var commitData))
                {
                    var targetTrans = extraRewardRoot.GetChild(0) as RectTransform;
                    var targetPos = targetTrans.TransformPoint(UIUtility.GetLocalCenterInRect(targetTrans));
                    UIFlyUtility.FlyReward(commitData, targetPos);
                    att_clawOrder.Clear();
                }
            }
        }

        #endregion

        private void _RefreshSlotReward(GameObject obj, OrderAttachmentUtility.Slot slot)
        {
            var rewardItem = obj.GetComponent<MBBoardOrderRewardItem>();
            UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.SmartPlus);
            var (id, num) = slot.GetReward(mData);
            rewardItem.SetData(id, num);
            UIItemUtility.SetCountStringStyle(UIItemUtility.CountStringStyle.NoPrefix);
            _RefreshClaimOffset();
        }

        #region orderbox

        private void _RefreshOrderBox()
        {
            if (!_CanUseOrderBox())
            {
                orderBox.gameObject.SetActive(false);
            }
            else
            {
                if (orderBox.TryShowBox(mData.Id))
                {
                    orderBox.gameObject.SetActive(true);
                }
                else
                {
                    orderBox.gameObject.SetActive(false);
                }
            }
        }

        private void _TryAddOrderBox(Item item)
        {
            if (BoardViewWrapper.TryGetOrderBoxDetail(mData.Id, out _, out _, out _))
            {
                Vector3 from = Vector3.zero;
                Vector3 to = orderBox.transform.position;
                float delay = 0f;

                // 新增礼包显示
                if (item.parent == null)
                {
                    // 来自奖励盒子
                    var obj = BoardViewWrapper.GetParam(BoardViewWrapper.ParamType.CompRewardTrack);
                    if (obj != null && obj is MBMergeCompReward reward)
                    {
                        var tar = reward.FirstRewardTrans();
                        from = tar.position;
                    }
                }
                else
                {
                    // 来自棋盘
                    from = BoardUtility.GetWorldPosByCoord(item.coord);
                    // 棋子正在播放开盒动画
                    delay = effOrderBoxDieDelay;
                }

                // 添加拖尾效果
                var itemType = BoardUtility.EffTypeToPoolType(ItemEffectType.OrderBoxTrail);
                var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.Effect);
                var eff = GameObjectPoolManager.Instance.CreateObject(itemType, effRoot);
                eff.SetActive(false);
                eff.transform.position = from;
                eff.SetActive(true);

                var seq = DOTween.Sequence();
                seq.Append(eff.transform.DOMove(to, 0.5f).SetDelay(delay).SetEase(Ease.Linear));
                seq.AppendCallback(() =>
                {
                    _RefreshOrderBox();
                    PlayAnim_AddOrderBox();
                });
                seq.AppendInterval(0.5f);
                seq.OnComplete(() => { GameObjectPoolManager.Instance.ReleaseObject(itemType, eff); });
            }
        }

        private void _OnMessageOrderBoxBegin(Item item)
        {
            if (!_CanUseOrderBox())
                return;
            _TryAddOrderBox(item);
        }

        private void _OnMessageOrderBoxEnd()
        {
            _RefreshOrderBox();
        }

        #endregion

        #region 星想事成

        private void _OnMessageMagicHourRewardBegin((IOrderData order, IOrderData targetOrder, Item item) data)
        {
            if (mData == data.order)
            {
                magicHourTargetOrder = data.targetOrder;
                magicHourRewardItem = data.item;
            }
        }

        #endregion

        #region 活动token棋子翻倍

        private void _OnMessageTokenMultiBegin(Item item)
        {
            var score = mData.Score;
            if (score <= 0)
            {
                _SetScoreState(false);
                return;
            }
            var seq = DOTween.Sequence();
            // 棋子可能正在从礼物盒拿出 效果延迟到棋子落地再处理
            float delay = 0.5f; //拖尾特效飞行时间
            var itemView = BoardViewManager.Instance.GetItemView(item.id);
            if (!itemView.IsViewIdle())
            {
                delay += 0.7f;
            }
            seq.AppendInterval(delay);
            seq.AppendCallback(() =>
            {
                //先播特效
                var effType_Disp = BoardUtility.EffTypeToPoolType(ItemEffectType.TokenMultiTrigger).ToString();
                var disappear = GameObjectPoolManager.Instance.CreateObject(effType_Disp, BoardViewManager.Instance.boardView.topEffectRoot);
                disappear.transform.position = scoreGroup.txtScore.transform.position;
                BoardUtility.AddAutoReleaseComponent(disappear, 2f, effType_Disp);
            });
            //延迟一段时间 等特效播到一定程度后 再刷新UI
            delay = 0.15f;
            seq.AppendInterval(delay);
            seq.AppendCallback(() =>
            {
                var act = Game.Manager.activity.Lookup(mData.GetValue(OrderParamType.ScoreEventId));
                if (act is ActivityScoreMic activityScoreMic && !activityScoreMic.IsComplete())
                {
                    _RefreshScoreMic(activityScoreMic, score);
                }
                else
                {
                    _SetScoreState(false);
                }
            });
            seq.Play();
        }

        private void _OnMessageTokenMultiEnd()
        {
            _RefreshScore();
        }

        private void _RefreshScoreMic(ActivityScoreMic activityScoreMic, int score)
        {
            if (activityScoreMic == null)
                return;
            var tokenId = activityScoreMic.Conf.Token;
            //设置图片
            scoreGroup.icon.SetImage(Game.Manager.objectMan.GetBasicConfig(tokenId).Image);
            //根据是否有倍率决定不同颜色和样式
            var isMulti = activityScoreMic.CheckTokenMultiRate(tokenId, out var rate);
            var key = activityScoreMic.GetScoreTextStyleKey(isMulti);
            activityScoreMic.Visual.RefreshStyle(scoreGroup.txtScore, key);
            var scoreStr = isMulti ? score * rate : score;
            scoreGroup.txtScore.text = $"{scoreStr}";
            _SetScoreState(true);
        }

        #endregion

        private void _OnMessageRaceRoundStart(bool start)
        {
            _RefreshScore();
        }

        private void _OnBtnDebug()
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                UIConfig.UIOrderDebug.Open(mData);
            }
        }
    }
}
