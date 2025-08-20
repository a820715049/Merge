/**
 * @Author: zhangpengjian
 * @Date: 2025/3/26 11:26:14
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/3/26 11:26:14
 * Description: 钓鱼棋盘主界面
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using FAT.Merge;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static FAT.ActivityFishing;

namespace FAT
{
    public class UIActivityFishMain : UIBase,INavBack
    {
        public MBBoardView _view;
        private TextMeshProUGUI _cd;
        private MBFishBoardReward _reward;
        private ActivityFishing _activity;
        private Image _boardEntry;
        private bool _isTapBonus;
        private Sequence _commonResSeq;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private GameObject block;
        [SerializeField] private GameObject efx;
        [SerializeField] private GameObject efxRoot;
        [SerializeField] private SkeletonGraphic spine;
        [SerializeField] private UIImageRes fishingIcon;
        [SerializeField] private UnityEngine.Animation fishAnim;
        [SerializeField] private MBRewardProgress progress;
        [SerializeField] private Animator progressAnim;
        [SerializeField] private Button milestoneBtn;
        [SerializeField] private Button rewardBtn;
        [SerializeField] private UIImageRes rewardIcon;
        [SerializeField] private TextMeshProUGUI milestoneNum;
        [SerializeField] private Transform rewardNode;
        [SerializeField] private Button goBtn;
        [SerializeField] private MBFlyTarget mergeItem;
        [SerializeField] private float waitForFishSipne = 1.5f;
        [SerializeField] private float flyDuration = 1f;
        [SerializeField] private float waitForFly = 0.8f;
        private string efx_key = "fish_milestone_star";
        private List<Item> items = new();
        private bool isTransitioning = false;

        protected override void OnCreate()
        {
            RegiestComp();
            Setup();
            AddButton();
        }

        private void RegiestComp()
        {
            transform.Access("Content/Center/ProgressBg/_cd/text", out _cd);
            transform.Access("Content/Center/BoardRewardNode", out _reward);
            transform.Access("Content/Bottom/FlyTarget/Entry", out _boardEntry);
        }

        private void Setup()
        {
            _reward.Setup();
            _view.Setup();
        }

        private void AddButton()
        {
            transform.AddButton("Content/Top/HelpBtn", ClickHelp);
            transform.AddButton("Content/Top/CloseBtn", ClickClose);
            milestoneBtn.onClick.AddListener(ClickMilestone);
            rewardBtn.onClick.AddListener(ClickReward);
            goBtn.onClick.AddListener(ClickGo);
        }

        private void ClickGo()
        {
            ActivityTransit.Exit(_activity, ResConfig, () => {
                UIConfig.UIMessageBox.Close();
            }, true); // 退出时默认返回主棋盘
        }

        private void ClickMilestone()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIActivityFishMilestone, _activity);
        }

        private void ClickReward()
        {
            var list = Enumerable.ToList(_activity.ConfMilestoneCur.Reward.Select(s => s.ConvertToRewardConfig()));
            UIManager.Instance.OpenWindow(UIConfig.UIFishRewardTips, rewardBtn.transform.position, 35f, list);
        }

        private void ClickHelp()
        {
            UIManager.Instance.OpenWindow(_activity.VisualHelp.res.ActiveR, _activity);
        }

        private void ClickClose()
        {
            ActivityTransit.Exit(_activity, ResConfig, () => {
                UIConfig.UIMessageBox.Close();
            });
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as ActivityFishing;
            if (_activity == null) return;
            Game.Manager.screenPopup.Block(true, false);
            EnterBoard();
        }

        private void EnterBoard()
        {
            var world = _activity.World;
            BoardViewWrapper.PushWorld(world);
            RefreshScale(Game.Manager.mainMergeMan.mainBoardScale);
            _view.OnBoardEnter(world, world.currentTracer);
            if (world != null)
            {
                world.onItemEvent += OnItemEvent;
            }
        }

        private void RefreshScale(float scale)
        {
            var root = _view.transform as RectTransform;
            root.localScale = new Vector3(scale, scale, scale);
            (root.parent as RectTransform).sizeDelta = new Vector2(scale * root.sizeDelta.x, scale * root.sizeDelta.y);
        }

        protected override void OnPreOpen()
        {
            PreparePool();
            goBtn.gameObject.SetActive(NeedShowPlay());
            block.transform.gameObject.SetActive(false);
            rewardNode.gameObject.SetActive(true);
            isTransitioning = false;
            mergeItem.enabled = false;
            mergeItem.gameObject.SetActive(false);
            spine.AnimationState.SetAnimation(0, "idle", true);
            _reward.Refresh(_activity);
            progress.Refresh(_activity.CurToken, _activity.MaxToken);
            if (_activity.CurToken == _activity.MaxToken)
            {
                progress.text.text = I18N.Text("#SysComDesc890");
                rewardNode.gameObject.SetActive(false);
            }
            rewardIcon.SetImage(_activity.ConfMilestoneCur.RewardIcon);
            milestoneNum.text = _activity.ConfMilestoneCur.ShowNum.ToString();
            RefreshCD(false);
            RefreshFish();
            MessageCenter.Get<MSG.UI_TOP_BAR_PUSH_STATE>().Dispatch(UIStatus.LayerState.AboveStatus);
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCDSecond);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FlyFeedBack);
            MessageCenter.Get<MSG.FLY_ICON_START>().AddListener(CheckNewFly);
            MessageCenter.Get<MSG.FISHING_FISH_CAUGHT>().AddListener(OnFishCaught);
            MessageCenter.Get<MSG.FISHING_FISH_CAUGHT_CLOSE>().AddListener(OnFishCaughtClose);
            MessageCenter.Get<MSG.FISHING_FISH_BOARD_SPAWN_ITEM>().AddListener(OnFishBoardSpawnItem);
            MessageCenter.Get<MSG.FISHING_FISH_CAUGHT_REPEAT>().AddListener(OnFishCaughtRepeat);
            MessageCenter.Get<MSG.FISHING_FISH_COLLECT_CLOSE>().AddListener(OnFishCollectClose);
            MessageCenter.Get<MSG.FISHING_MILESTONE_REWARD_CLOSE>().AddListener(OnFishMilestoneRewardClose);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCDSecond);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FlyFeedBack);
            MessageCenter.Get<MSG.FLY_ICON_START>().RemoveListener(CheckNewFly);
            MessageCenter.Get<MSG.FISHING_FISH_CAUGHT>().RemoveListener(OnFishCaught);
            MessageCenter.Get<MSG.FISHING_FISH_CAUGHT_CLOSE>().RemoveListener(OnFishCaughtClose);
            MessageCenter.Get<MSG.FISHING_FISH_BOARD_SPAWN_ITEM>().RemoveListener(OnFishBoardSpawnItem);
            MessageCenter.Get<MSG.FISHING_FISH_CAUGHT_REPEAT>().RemoveListener(OnFishCaughtRepeat);
            MessageCenter.Get<MSG.FISHING_FISH_COLLECT_CLOSE>().RemoveListener(OnFishCollectClose);
            MessageCenter.Get<MSG.FISHING_MILESTONE_REWARD_CLOSE>().RemoveListener(OnFishMilestoneRewardClose);
        }

        private void RefreshCDSecond()
        {
            RefreshCD();
        }

        private void OnFishMilestoneRewardClose()
        {
            mergeItem.enabled = true;
            mergeItem.gameObject.SetActive(true);
        }

        private void PreparePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(efx_key))
            {
                return;
            }
            GameObjectPoolManager.Instance.PreparePool(efx_key, efx);
        }

        private void OnFishCollectClose(FishCaughtInfo info)
        {
            var item = FindFishItem(info.fishId);
            if (item == null) return;
            UIManager.Instance.Block(true);
            TryFlyToMilestoneStar(item, info);
        }

        private void OnFishCaughtRepeat()
        {
            block.transform.gameObject.SetActive(false);
        }

        private void OnFishBoardSpawnItem()
        {
            spine.AnimationState.SetAnimation(0, "click", false).Complete += delegate (TrackEntry entry)
            {
                spine.AnimationState.SetAnimation(0, "idle", true);
            };
            Game.Manager.audioMan.TriggerSound("BoardReward");
        }

        private void OnFishCaughtClose(FishCaughtInfo info)
        {
            StartCoroutine(CoOnFishCaughtClose(info));
        }

        private IEnumerator CoOnFishCaughtClose(FishCaughtInfo info)
        {
            var item = FindFishItem(info.fishId);
            if (item == null) yield break;
            item.GetComponent<MBFishItem>().PlayAnim(info);
            var hasCollect = info.nowStar >= item.GetComponent<MBFishItem>().fish.Star.Count;
            var hasNewStar = info.nowStar - info.preStar > 0;
            var wait = 0f;
            if (hasCollect || hasNewStar)
            {
                wait = 0.58f + 0.5f + waitForFly;
            }
            else
            {
                wait = 0.58f + 0.5f;
            }
            yield return new WaitForSeconds(wait);
            if (hasCollect)
            {
                _activity.VisualCollect.res.ActiveR.Open(_activity, info);
                yield return new WaitForSeconds(0.5f);
                UIManager.Instance.Block(false);
                yield break;
            }
            if (hasNewStar)
            {
                TryFlyToMilestoneStar(item, info);
            }
            else
            {
                block.transform.gameObject.SetActive(false);
                UIManager.Instance.Block(false);
            }
        }

        private void TryFlyToMilestoneStar(RectTransform item, FishCaughtInfo info)
        {
            GameObjectPoolManager.Instance.CreateObject(efx_key, efxRoot.transform, trail =>
            {
                trail.SetActive(false);
                trail.transform.localPosition = Vector3.zero;
                trail.transform.position = item.GetComponent<MBFishItem>().stars[info.nowStar - 1].transform.position;
                trail.SetActive(true);
                var v = UIFlyFactory.ResolveFlyTarget(FlyType.FishMilestoneStar);
                trail.GetOrAddComponent<MBAutoRelease>().Setup(efx_key, 1.5f);
                Game.Manager.audioMan.TriggerSound("FishBoardStarFly");
                trail.transform.DOMove(v, flyDuration).SetEase(Ease.InCubic).OnComplete(() =>
                {
                    progressAnim.SetTrigger("Punch");
                    if (_activity.CurToken == fat.conf.Data.GetEventFishMilestone(_activity.MilestoneIdx).Star || _activity.CurToken == _activity.MaxToken)
                    {
                        progress.Refresh(_activity.CurToken, _activity.CurToken, 0.5f);
                        StartCoroutine(CoProgressAnim(_activity.CurToken == _activity.MaxToken));
                    }
                    else
                    {
                        progress.Refresh(_activity.CurToken, _activity.MaxToken, 0.5f);
                        UIManager.Instance.Block(false);
                    }
                    block.transform.gameObject.SetActive(false);
                });
            });
        }

        private IEnumerator CoProgressAnim(bool isMax)
        {
            yield return new WaitForSeconds(0.5f);
            UIManager.Instance.OpenWindow(UIConfig.UIActivityFishReward, _activity);
            yield return new WaitForSeconds(0.5f);
            UIManager.Instance.Block(false);
            progress.Refresh(_activity.CurToken, _activity.MaxToken);
            if (isMax)
            {
                progress.text.text = I18N.Text("#SysComDesc890");
                rewardNode.gameObject.SetActive(false);
            }
            rewardIcon.SetImage(_activity.ConfMilestoneCur.RewardIcon);
            milestoneNum.text = _activity.ConfMilestoneCur.ShowNum.ToString();
        }

        private void OnFishCaught(FishCaughtInfo info)
        {
            UIManager.Instance.Block(true); // 在获得鱼界面 关闭了block
            StartCoroutine(CoOnFishCaught(info));
        }

        private IEnumerator CoOnFishCaught(FishCaughtInfo info)
        {
            block.transform.gameObject.SetActive(true);
            fishingIcon.SetImage(fat.conf.Data.GetFishInfo(info.fishId).IconAni);
            spine.AnimationState.SetAnimation(0, "fish", false).Complete += delegate (TrackEntry entry)
            {
                spine.AnimationState.SetAnimation(0, "idle", true);
            };
            Game.Manager.audioMan.TriggerSound("FishBoardCatch");
            fishAnim.Play();
            yield return new WaitForSeconds(waitForFishSipne);
            _activity.VisualGet.res.ActiveR.Open(_activity, info);
            yield return new WaitForSeconds(0.5f);
            ScrollToFishItem(info.fishId);
        }

        protected override void OnPreClose()
        {
            UIManager.Instance.CloseWindow(UIConfig.UIPopFlyTips);
            UIManager.Instance.CloseWindow(UIConfig.UIEnergyBoostTips);
            if (_commonResSeq != null) _commonResSeq.Kill();
            if (BoardViewWrapper.GetCurrentWorld() == null) return;
            _view.OnBoardLeave();
            BoardViewWrapper.PopWorld();
            var world = _activity?.World;
            if (world != null)
            {
                world.onItemEvent -= OnItemEvent;
            }
        }

        private void OnItemEvent(Item item, ItemEventType eventType)
        {
            if (eventType == ItemEventType.ItemEventMoveToRewardBox)
            {
                StartCoroutine(CoRefreshWithPunch());
            }
        }

        private IEnumerator CoRefreshWithPunch()
        {
            yield return new WaitForSeconds(0.5f);
            _reward.RefreshWithPunch();
            yield return new WaitForSeconds(0.5f);
            block.SetActive(false);
        }

        protected override void OnPostClose()
        {
            MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().Dispatch();
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
        }

        private void Update()
        {
            BoardViewManager.Instance.Update(Time.deltaTime);
        }

        private void RefreshFish()
        {
            scroll.content.sizeDelta = new Vector2((154 + 32) * _activity.FishInfoList.Count, 0);
            for (int i = 0; i < _activity.FishInfoList.Count; i++)
            {
                var fish = _activity.FishInfoList[i];
                var item = scroll.content.GetChild(0).GetChild(i).GetComponent<MBFishItem>();
                item.Setup(_activity, fish);
            }
        }

        // 根据ID 获得对应鱼图鉴的transform
        public RectTransform FindFishItem(int fishId)
        {
            var obj = scroll.content.GetChild(0);
            for (int i = 0; i < obj.childCount; i++)
            {
                var item = obj.GetChild(i).GetComponent<MBFishItem>();
                if (fishId == item.fish.Id)
                {
                    return item.GetComponent<RectTransform>();
                }
            }
            return null;
        }

        private void ScrollToFishItem(int fishId)
        {
            var targetItem = FindFishItem(fishId);
            if (targetItem == null) return;

            // 获取目标item在content中的索引
            int itemIndex = targetItem.transform.GetSiblingIndex();
            int totalItems = _activity.FishInfoList.Count;

            float targetHorizontalPosition;
            if (itemIndex == 0)
            {
                // 第一个item，滚动到最左
                targetHorizontalPosition = 0;
            }
            else if (itemIndex == totalItems - 1)
            {
                // 最后一个item，滚动到最右
                targetHorizontalPosition = 1;
            }
            else
            {
                // 其他情况，居中显示
                float itemWidth = 154 + 32; // item宽度加间距
                float contentWidth = scroll.content.rect.width;
                float viewportWidth = scroll.viewport.rect.width;

                // 计算目标位置，使item居中
                float itemCenter = (itemIndex * itemWidth) + (itemWidth / 2);
                targetHorizontalPosition = (itemCenter - (viewportWidth / 2)) / (contentWidth - viewportWidth);

                // 确保位置在0-1之间
                targetHorizontalPosition = Mathf.Clamp01(targetHorizontalPosition);
            }

            // 使用DOTween平滑滚动到目标位置
            DOTween.To(() => scroll.horizontalNormalizedPosition,
                x => scroll.horizontalNormalizedPosition = x,
                targetHorizontalPosition, 0.3f)
                .SetEase(Ease.OutCubic);
        }

        // 确保点击的item完全显示在视图中
        public void EnsureItemFullyVisible(RectTransform targetItem, int fishId)
        {
            if (targetItem == null) return;

            // 将目标item的位置转换为视口坐标
            Vector3[] itemCorners = new Vector3[4];
            Vector3[] viewportCorners = new Vector3[4];
            targetItem.GetWorldCorners(itemCorners);
            scroll.viewport.GetWorldCorners(viewportCorners);

            // 计算item和视口的边界
            float itemLeft = itemCorners[0].x;
            float itemRight = itemCorners[2].x;
            float viewportLeft = viewportCorners[0].x;
            float viewportRight = viewportCorners[2].x;

            // 计算需要滚动的距离
            float targetPosition = scroll.horizontalNormalizedPosition;
            float contentWidth = scroll.content.rect.width;
            float viewportWidth = scroll.viewport.rect.width;
            float scrollableWidth = contentWidth - viewportWidth;

            if (itemLeft < viewportLeft)
            {
                // 如果item左侧被遮挡，计算需要向左滚动的距离
                float worldDelta = viewportLeft - itemLeft;
                float normalizedDelta = worldDelta / scrollableWidth;
                targetPosition = Mathf.Max(0, scroll.horizontalNormalizedPosition - normalizedDelta);
            }
            else if (itemRight > viewportRight)
            {
                // 如果item右侧被遮挡，计算需要向右滚动的距离
                float worldDelta = itemRight - viewportRight;
                float normalizedDelta = worldDelta / scrollableWidth;
                targetPosition = Mathf.Min(1, scroll.horizontalNormalizedPosition + normalizedDelta);
            }

            // 执行滚动
            if (!Mathf.Approximately(targetPosition, scroll.horizontalNormalizedPosition))
            {
                block.transform.gameObject.SetActive(true);
                DOTween.To(() => scroll.horizontalNormalizedPosition,
                    x => scroll.horizontalNormalizedPosition = x,
                    targetPosition, 0.3f)
                    .SetEase(Ease.OutCubic).OnComplete(() =>
                    {
                        block.transform.gameObject.SetActive(false);
                        _activity.VisualTip.res.ActiveR.Open(targetItem.position, 0f, _activity, fishId);
                    });
            }
            else
            {
                _activity.VisualTip.res.ActiveR.Open(targetItem.position, 0f, _activity, fishId);
            }
        }

        private void RefreshCD(bool isTransition = true)
        {
            if (_activity == null) return;
            if (isTransition)
            {
                TryTransitionItem();
            }
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            if (diff <= 0)
            {
                _activity.VisualHelp.res.ActiveR.Close();
                UIConfig.UIFishRewardTips.Close();
                ActivityTransit.Exit(_activity, ResConfig, () => {
                    UIConfig.UIMessageBox.Close();
                });
                return;
            }
            goBtn.gameObject.SetActive(NeedShowPlay());
            UIUtility.CountDownFormat(_cd, diff);
        }

        private void TryTransitionItem()
        {
            var world = Game.Manager.mergeBoardMan.activeWorld;
            var board = world?.activeBoard;
            if (!isTransitioning && board.emptyGridCount == 0 && !BoardViewManager.Instance.checker.HasMatchPair())
            {
                isTransitioning = true;
                var content = I18N.Text("#SysComDesc1244");
                Game.Manager.commonTipsMan.ShowMessageTips(content, TransitionItem, TransitionItem, true);
            }
        }

        private void TransitionItem()
        {
            isTransitioning = false;
            var dict = _activity.World.currentTracer.GetCurrentActiveBoardItemCount();
            var world = Game.Manager.mergeBoardMan.activeWorld;
            var board = world?.activeBoard;
            if (board != null)
            {
                items.Clear();
                board.WalkAllItem((item) => items.Add(item));
                items.Sort((a, b) => a.config.Id.CompareTo(b.config.Id));
                block.SetActive(true);
                foreach (var item in items)
                {
                    if (!item.isActive) continue;
                    board.MoveItemToRewardBox(item, true);
                }
            }
        }

        private bool NeedShowPlay()
        {
            var dict = _activity.World.currentTracer.GetCurrentActiveBoardItemCount();
            if (_activity.World.rewardCount > 0 || _activity.Conf.Check.Any(key => dict.ContainsKey(key)) || BoardViewManager.Instance.checker.HasMatchPair())
            {
                return false;
            }
            return true;
        }

        private void FlyFeedBack(FlyableItemSlice slice)
        {
            if (slice.FlyType == FlyType.MergeItemFlyTarget) _reward.FlyFeedBack(slice);
        }

        private void CheckNewFly(FlyableItemSlice slice)
        {
            if (_activity == null) return;
            if (slice.FlyType == FlyType.TapBonus || slice.FlyType == FlyType.FlyToMainBoard)
            {
                CheckTapBonus();
            }
        }

        private void CheckTapBonus()
        {
            if (_isTapBonus) return;
            _isTapBonus = true;
            var seq = DOTween.Sequence();
            seq.Append(_boardEntry.DOFade(1, 0.5f));
            seq.AppendInterval(0.5f);
            seq.Append(_boardEntry.DOFade(0, 0.5f));
            seq.AppendCallback(() => _isTapBonus = false);
            seq.OnKill(() =>
            {
                var color = Color.white;
                color.a = 0;
                _boardEntry.color = color;
                _isTapBonus = false;
                mergeItem.enabled = false;
                mergeItem.gameObject.SetActive(false);
            });
            seq.Play().OnComplete(() =>
            {
                mergeItem.enabled = false;
                mergeItem.gameObject.SetActive(false);
            });
        }

        public void OnNavBack()
        {
            if (!block.transform.gameObject.activeSelf)
            {
                ClickClose();
            }
        }
    }
}
