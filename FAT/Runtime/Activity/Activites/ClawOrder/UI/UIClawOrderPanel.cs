/*
 * @Author: qun.chao
 * @Date: 2025-07-21 13:53:58
 */
using UnityEngine;
using UnityEngine.UI;
using EL;
using DG.Tweening;
using TMPro;
using Cysharp.Text;
using System;
using Spine.Unity;
using Cysharp.Threading.Tasks;

namespace FAT
{
    public class UIClawOrderPanel : UIBase, INavBack
    {
        [Serializable]
        public class EffectGroup
        {
            [Tooltip("奖励飞图标延迟")]
            public float rewardFlyDelay = 1.5f;
            [Tooltip("奖励图标原地停留延迟")]
            public float rewardStayDelay = 1.0f;
            [Tooltip("按钮图标切换延迟")]
            public float btnIconChangeDelay = 0.5f;
            [Tooltip("全局特效动画 | 配合抓取/切换")]
            public Animator aniEffect;
            [Tooltip("钩爪控制动画 | 配合切换")]
            public Animator aniClaw;
            [Tooltip("按钮")]
            public Animator aniClawBtn;
            [Tooltip("球")]
            public SkeletonGraphic spBall;
            [Tooltip("奖励节点")]
            public Transform rewardRoot;
            [Tooltip("当前钩爪节点")]
            public Transform curRoot;
            [Tooltip("下一个钩爪节点")]
            public Transform nextRoot;
            [Tooltip("奖励图标")]
            public UIImageRes rewardIcon;

            // 动态加载 当前钩爪
            public SkeletonGraphic spCurClaw { get; set; }
            // 当前钩爪Icon资源id
            public int clawIconResId { get; set; }
            // 当前钩爪Prefab资源id
            public int clawPrefabResId { get; set; }
        }

        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnInfo;
        [SerializeField] private Button btnDraw;
        [SerializeField] private UICommonProgressBar progressBar;
        [SerializeField] private TextMeshProUGUI txtCD;
        [SerializeField] private TextMeshProUGUI txtDrawCount;
        // 默认钩爪
        [SerializeField] private GameObject goDefaultClaw;
        // 常规钩爪
        [SerializeField] private GameObject goNormalClaw;
        // 进度节点
        [SerializeField] private RectTransform sliceRoot;
        // 奖励节点 (钩爪排列)
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private EffectGroup effectGroup;

        private const string drawCountPattern = "x  {0}";
        private ActivityClawOrder _actInst;

        private int blockCount = 0;

        protected override void OnCreate()
        {
            transform.AddButton("Mask", Close);
            btnClose.onClick.AddListener(Close);
            btnInfo.onClick.AddListener(OnBtnInfo);
            btnDraw.onClick.AddListener(OnBtnDraw);
        }

        protected override void OnParse(params object[] items)
        {
            _actInst = items[0] as ActivityClawOrder;
        }

        protected override void OnPreOpen()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);

            ResetClaw().Forget();
            RefreshCD();
            ShowTokenSlice();
            ShowReward();
            RefreshDrawBtn();
            SyncProgress();
        }

        protected override void OnPostClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);

            blockCount = 0;
            UnlockEvent();
        }

        private void Update()
        {
            if (!IsOpen())
                return;
            if (IsLocked())
                return;
            if (!_actInst.Active)
            {
                Close();
            }
        }

        /// <summary>
        /// 同步进度
        /// </summary>
        private void SyncProgress()
        {
            var synced = _actInst.SyncedToken;
            var max = _actInst.MaxToken;
            var cur = _actInst.CurToken;

            RefreshTokenProgress(max, synced);
            RefreshRewardState(synced);

            if (synced >= cur)
                return;

            Block(true);

            var from = synced * 100;
            var to = cur * 100;
            var duration = (cur - synced) * 0.5f;

            Game.Manager.audioMan.TriggerSound("ClawOrderBarIncrease");

            DOTween.To(() => from, x => from = x, to, duration).SetDelay(0.5f).OnUpdate(() =>
            {
                RefreshTokenProgress(max * 100, from);
                RefreshRewardState(from / 100, true);
            }).OnComplete(async () =>
            {
                RefreshTokenProgress(max, cur);
                RefreshRewardState(cur, true);

                var preDrawCount = _actInst.CalcTotalDrawChanceCountByToken(synced);
                _actInst.SyncToken();
                var curDrawCount = _actInst.CalcTotalDrawChanceCountByToken(cur);
                if (preDrawCount < curDrawCount)
                {
                    // 按钮变化
                    effectGroup.aniClawBtn.SetTrigger("Punch");
                    await UniTask.WaitForSeconds(effectGroup.btnIconChangeDelay);
                }
                RefreshDrawBtn();

                Block(false);
            });
        }

        /// <summary>
        /// 显示进度格
        /// </summary>
        private void ShowTokenSlice()
        {
            var maxToken = _actInst.MaxToken;
            var maxSliceNum = maxToken - 1;
            for (var i = 0; i < sliceRoot.childCount; i++)
            {
                var slice = sliceRoot.GetChild(i);
                slice.gameObject.SetActive(i < maxSliceNum);
            }
            var layout = sliceRoot.GetComponent<HorizontalLayoutGroup>();
            var padding = sliceRoot.rect.width / maxToken * 0.5f;
            layout.padding.left = (int)padding;
            layout.padding.right = (int)padding;
        }

        /// <summary>
        /// 显示奖励
        /// </summary>
        private void ShowReward()
        {
            var cfg = _actInst.ConfDetail;
            var maxToken = _actInst.MaxToken;
            var cellSize = sliceRoot.rect.width / maxToken;
            var rewardList = cfg.DrawMilestone;
            for (var i = 0; i < rewardRoot.childCount; i++)
            {
                var reward = rewardRoot.GetChild(i);
                if (i < rewardList.Count)
                {
                    reward.gameObject.SetActive(true);
                    var (token, _, resId) = _actInst.CalcTokenByRewardIdx(i);
                    var pos = cellSize * token - cellSize * 0.5f;
                    reward.localPosition = new Vector3(pos, 0, 0);
                    var resCfg = _actInst.GetConfigDrawRes(resId);
                    reward.Access<UIImageRes>("Icon").SetImage(resCfg.Image);
                    reward.Find("Check").gameObject.SetActive(false);
                }
                else
                {
                    reward.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 显示钩爪次数 / icon
        /// </summary>
        private bool RefreshDrawBtn()
        {
            var changed = false;
            var synced = _actInst.SyncedToken;
            var drawed = _actInst.DrawAttemptCount;
            var resId = _actInst.CalcClawResByToken(synced);
            if (resId > 0)
            {
                var resCfg = _actInst.GetConfigDrawRes(resId);
                goDefaultClaw.SetActive(false);
                goNormalClaw.SetActive(true);
                goNormalClaw.GetComponent<UIImageRes>().SetImage(resCfg.Image);
                var drawCount = _actInst.CalcTotalDrawChanceCountByToken(synced);
                txtDrawCount.SetTextFormat(drawCountPattern, drawCount - drawed);
            }
            else
            {
                goDefaultClaw.SetActive(true);
                goNormalClaw.SetActive(false);
                txtDrawCount.SetTextFormat(drawCountPattern, "?");
            }
            if (resId != effectGroup.clawIconResId)
            {
                changed = true;
            }
            effectGroup.clawIconResId = resId;
            return changed;
        }

        /// <summary>
        /// 检查钩爪按钮是否需要刷新
        /// </summary>
        private bool CheckShouldClawIconChange()
        {
            var changed = false;
            var resId = _actInst.CalcClawResByToken(_actInst.SyncedToken);
            if (resId != effectGroup.clawIconResId)
            {
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// 重置钩爪
        /// </summary>
        private async UniTask ResetClaw()
        {
            Block(true);
            var group = effectGroup;
            group.spBall.gameObject.SetActive(false);
            group.rewardIcon.gameObject.SetActive(false);

            var synced = _actInst.SyncedToken;
            var resId = _actInst.CalcClawResByToken(synced);
            if (resId <= 0)
            {
                // 应该加载第一个钩爪资源
                var first = _actInst.ConfDetail.DrawMilestone[0];
                var cfg = _actInst.GetConfigDrawInfo(first);
                resId = cfg.RewardIconId;
            }
            var curClaw = group.spCurClaw;
            if (resId != group.clawPrefabResId || curClaw == null)
            {
                if (curClaw != null)
                {
                    // 从prefab根节点销毁
                    Destroy(curClaw.transform.parent.gameObject);
                }
                var go = await InstallClaw(group.curRoot, resId);
                curClaw = go.transform.GetChild(0).GetComponent<SkeletonGraphic>();
                group.clawPrefabResId = resId;
                group.spCurClaw = curClaw;
            }
            group.spBall.gameObject.SetActive(true);
            group.spBall.AnimationState.SetAnimation(0, "show", false).Complete += OnBallAnimationComplete;
            curClaw.AnimationState.SetAnimation(0, "show", false).Complete += OnClawAnimationComplete;
            // 钩爪节点重置
            group.aniClaw.SetTrigger("Idle");

            Block(false);
        }

        private async UniTask<GameObject> InstallClaw(Transform parent, int resId)
        {
            var asset = _actInst.GetConfigDrawRes(resId).RewardPrefab.ConvertToAssetConfig();
            var task = EL.Resource.ResManager.LoadAsset<GameObject>(asset.Group, asset.Asset);
            if (task.keepWaiting)
                await UniTask.WaitWhile(() => task.keepWaiting && !task.isCanceling);
            if (task.isSuccess)
            {
                var prefab = task.asset as GameObject;
                if (prefab != null)
                {
                    var go = Instantiate(prefab, parent);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = Vector3.one;
                    return go;
                }
            }
            return null;
        }

        /// <summary>
        /// 刷新进度条
        /// </summary>
        private void RefreshTokenProgress(int max, int cur)
        {
            progressBar.ForceSetup(0, max, cur);
        }

        /// <summary>
        /// 刷新奖励状态
        /// </summary>
        private void RefreshRewardState(int token, bool playChangeAnim = false)
        {
            var rewardList = _actInst.ConfDetail.DrawMilestone;
            for (var i = 0; i < rewardList.Count; i++)
            {
                var (tokenNeed, _, _) = _actInst.CalcTokenByRewardIdx(i);
                if (token >= tokenNeed)
                {
                    var check = rewardRoot.GetChild(i).Find("Check");
                    if (check.gameObject.activeSelf)
                    {
                        continue;
                    }
                    check.gameObject.SetActive(true);
                    if (playChangeAnim)
                    {
                        check.GetComponent<Animator>().SetTrigger("Punch");
                        Game.Manager.audioMan.TriggerSound("ClawOrderReward");
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private void Block(bool b)
        {
            blockCount += b ? 1 : -1;
            if (blockCount > 0 && !IsLocked())
            {
                LockEvent();
            }
            else if (blockCount <= 0 && IsLocked())
            {
                UnlockEvent();
            }
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(txtCD, _actInst.Countdown);
        }

        private void OnBtnInfo()
        {
            UIConfig.UIClawOrderHelp.Open();
        }

        private void OnBtnDraw()
        {
            var drawCount = _actInst.CalcTotalDrawChanceCountByToken(_actInst.CurToken);
            var drawAttemptCount = _actInst.DrawAttemptCount;
            if (drawCount <= drawAttemptCount)
            {
                // 抽奖次数不足 点击后跳转棋盘
                Close();
                if (!UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain))
                {
                    GameProcedure.SceneToMerge();
                }
                return;
            }
            // 可以发起抽奖
            if (_actInst.TryDraw(out var reward))
            {
                // 飞奖励
                UniTask.Void(async () =>
                {
                    Block(true);
                    await UniTask.WaitForSeconds(effectGroup.rewardFlyDelay);
                    var res = Game.Manager.rewardMan.GetRewardIcon(reward.rewardId, reward.rewardCount);
                    var icon = effectGroup.rewardIcon;
                    icon.SetImage(res);
                    icon.gameObject.SetActive(true);
                    await UniTask.WaitForSeconds(effectGroup.rewardStayDelay);
                    icon.gameObject.SetActive(false);
                    Block(false);
                    UIFlyUtility.FlyReward(reward, effectGroup.rewardRoot.position, null, (icon.transform as RectTransform).rect.width);
                });
                // 抽奖后处理
                AfterDraw().Forget();
            }
        }

        private async UniTaskVoid AfterDraw()
        {
            Block(true);

            Game.Manager.audioMan.TriggerSound("ClawOrderDraw");

            var group = effectGroup;
            // 播放钩爪动画
            group.spCurClaw.AnimationState.SetAnimation(0, "catch", false).Complete += OnClawAnimationComplete;
            // 播放球动画
            group.spBall.AnimationState.SetAnimation(0, "catch", false).Complete += OnBallAnimationComplete;
            // 激活特效
            group.aniEffect.SetTrigger("Boom");
            // 刷新次数
            var drawCount = _actInst.CalcTotalDrawChanceCountByToken(_actInst.CurToken);
            txtDrawCount.SetTextFormat(drawCountPattern, drawCount - _actInst.DrawAttemptCount);
            // 等待钩爪动画结束
            await UniTask.WaitForSeconds(3f);

            // 刷新抽取按钮
            var change = CheckShouldClawIconChange();
            if (change)
            {
                // 钩爪按钮应该发生变化
                group.aniClawBtn.SetTrigger("Punch");
                // 延迟配合按钮动画刷新按钮icon
                await UniTask.WaitForSeconds(effectGroup.btnIconChangeDelay);
                RefreshDrawBtn();

                // 加载新钩爪 / 卸载旧钩爪
                var go = await InstallClaw(group.nextRoot, effectGroup.clawIconResId);
                var nextClaw = go.transform.GetChild(0).GetComponent<SkeletonGraphic>();
                // 获取当前钩爪动画的时间点
                var currentTrackEntry = group.spCurClaw.AnimationState.GetCurrent(0);
                if (currentTrackEntry != null)
                {
                    var currentTime = currentTrackEntry.TrackTime;
                    // 让新钩爪从相同的时间点开始播放动画
                    var newTrackEntry = nextClaw.AnimationState.SetAnimation(0, "idle", true);
                    newTrackEntry.TrackTime = currentTime;
                }

                Game.Manager.audioMan.TriggerSound("ClawOrderChange");
                // 钩爪节点动画配合
                group.aniClaw.SetTrigger("Change");
                // 特效配合
                group.aniEffect.SetTrigger("Change");
                // 等待特效结束
                await UniTask.WaitForSeconds(0.5f);
                // 钩爪切换
                group.clawPrefabResId = effectGroup.clawIconResId;
                Destroy(group.spCurClaw.transform.parent.gameObject);
                nextClaw.transform.parent.SetParent(group.curRoot);
                group.spCurClaw = nextClaw;
                // 钩爪节点动画回到idle
                group.aniClaw.SetTrigger("Idle");
            }

            Block(false);
        }

        void INavBack.OnNavBack()
        {
            if (!IsLocked())
            {
                Close();
            }
        }

        /// <summary>
        /// 钩爪动画完成回调
        /// </summary>
        private void OnClawAnimationComplete(Spine.TrackEntry trackEntry)
        {
            if (effectGroup.spCurClaw != null && effectGroup.spCurClaw.AnimationState != null)
            {
                effectGroup.spCurClaw.AnimationState.SetAnimation(0, "idle", true);
                effectGroup.spCurClaw.AnimationState.Complete -= OnClawAnimationComplete;
            }
        }

        /// <summary>
        /// 球动画完成回调
        /// </summary>
        private void OnBallAnimationComplete(Spine.TrackEntry trackEntry)
        {
            if (effectGroup.spBall != null && effectGroup.spBall.AnimationState != null)
            {
                effectGroup.spBall.AnimationState.SetAnimation(0, "idle", true);
                effectGroup.spBall.AnimationState.Complete -= OnBallAnimationComplete;
            }
        }
    }
}
