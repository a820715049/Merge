/*
 * @Author: qun.chao
 * @Date: 2024-04-17 12:05:11
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Config;
using EL;
using fat.rawdata;
using Spine.Unity;
using Spine;
using DG.Tweening;
using static FAT.UITreasureHuntUtility;

namespace FAT
{
    public class UITreasureHuntMain : UIBase
    {
        [Serializable]
        public class TreasureRes
        {
            public string name;
            public GameObject prefab;
        }

        [Serializable]
        class SceneGroup
        {
            /*
              bg默认使用和当前map布局相同的缩放
              当bg无法完整显示时 对bg再次适配(额外缩放)
            */
            public RectTransform sceneBg;
            public RectTransform mapRoot;
            public TreasureRes[] prefabs;
        }

        [Serializable]
        class TopGroup
        {
            public UICommonProgressBar progressBar;
            public Button btnRewardInfo;
            public Button btnEventInfo;
            public Animator treasureIconAni;
            public Transform rewardTipAnchor;
        }


        [Serializable]
        class BottomGroup
        {
            public TextMeshProUGUI tmpKeyNum;
            public Transform prizeRoot;
            public GameObject goBagEmptyTip;
        }

        [SerializeField] private SceneGroup sceneGroup;
        [SerializeField] private TopGroup topGroup;
        [SerializeField] private BottomGroup bottomGroup;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnPack;
        [SerializeField] private Button btnKey;
        [SerializeField] private GameObject keyGift;
        [SerializeField] private float treasureBornDelay = 0.2f;
        [SerializeField] private GameObject goBlock;
        [SerializeField] private Transform flyTarget_LevelReward;
        [SerializeField] private Transform flyTarget_ProgressReward;

        [SerializeField] private SkeletonGraphic pandaHeadSpine;
        [SerializeField] private SkeletonGraphic pandaBodySpine;
        [SerializeField] private AnimationCurve easeUp;
        [SerializeField] private AnimationCurve easeDown;
        [SerializeField] private float upTime;
        [SerializeField] private float downTime;
        [SerializeField] private bool isUseSpine;
        [SerializeField] private TMP_Text bottomText;
        [SerializeField] private List<float> pandaScale;
        [SerializeField] private List<float> downOffset;
        [SerializeField] private GameObject effectEmpty;
        [SerializeField] private GameObject effectRoot;
        [SerializeField] private float openDelay;
        [SerializeField] private MBTreasureHuntMilestone milestone;


        // UI开启期间禁止更换实例
        private ActivityTreasure eventInst;
        private Transform curMapRoot;
        private int curLevelIdx;
        private int totalLevelCount;
        private bool ignoreRefresh = false;
        private int tipShowFrameCount = 0;
        private int upHeight = 3000;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(_OnBtnQuit);
            btnPack.onClick.AddListener(_OnBtnPack);
            btnKey.onClick.AddListener(_OnBtnKey);

            topGroup.btnRewardInfo.onClick.AddListener(_OnBtnProgressRewardPreview);
            topGroup.btnEventInfo.onClick.AddListener(_OnBtnEventInfo);

            UIUtility.CommonItemSetup(bottomGroup.prizeRoot);
        }

        protected override void OnPreOpen()
        {
            if (!UITreasureHuntUtility.TryGetEventInst(out eventInst)) return;
            UITreasureHuntUtility.InstallBlock(goBlock);
            UITreasureHuntUtility.InstallRes(sceneGroup.prefabs);
            UITreasureHuntUtility.InstallEffectRes(effectEmpty, effectRoot);
            UITreasureHuntUtility.InstallOpenDelay(openDelay);
            StartCoroutine(_CoResetShow());
            ignoreRefresh = false;
            milestone.Visible(eventInst.ConfD.BonusToken > 0);
            if (eventInst.ConfD.BonusToken > 0)
            {
                milestone.SetData(eventInst.GetBonusTokenShowReward(), eventInst.GetBonusTokenShowNum(), eventInst.ConfD.BonusToken);
            }
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondPass);
            MessageCenter.Get<MSG.TREASURE_KEY_UPDATE>().AddListener(_OnMessageKeyChange);
            MessageCenter.Get<MSG.UI_TREASURE_REWARD_FLY_FEEDBACK>().AddListener(_OnMessageFlyFeedback);
            MessageCenter.Get<MSG.UI_TREASURE_FLY_FEEDBACK>().AddListener(_OnMessageTreasureFlyFeedback);
            MessageCenter.Get<MSG.UI_TREASURE_LEVEL_GROUP_CLEAR>().AddListener(_OnMessageLevelGroupClear);
            MessageCenter.Get<MSG.UI_TREASURE_LEVEL_CLEAR>().AddListener(_OnMessageLevelClear);
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().AddListener(_OnMessagePostCommitReward);
            MessageCenter.Get<MSG.TREASURE_OPENBOX>().AddListener(_OnMessageOpenBox);
        }

        protected override void OnPostClose()
        {
            _ClearStage();
            if (effectRoot != null)
            {
                UITreasureHuntUtility.ReleaseEfxObj();
            }
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondPass);
            MessageCenter.Get<MSG.TREASURE_KEY_UPDATE>().RemoveListener(_OnMessageKeyChange);
            MessageCenter.Get<MSG.UI_TREASURE_REWARD_FLY_FEEDBACK>().RemoveListener(_OnMessageFlyFeedback);
            MessageCenter.Get<MSG.UI_TREASURE_FLY_FEEDBACK>().RemoveListener(_OnMessageTreasureFlyFeedback);
            MessageCenter.Get<MSG.UI_TREASURE_LEVEL_GROUP_CLEAR>().RemoveListener(_OnMessageLevelGroupClear);
            MessageCenter.Get<MSG.UI_TREASURE_LEVEL_CLEAR>().RemoveListener(_OnMessageLevelClear);
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().RemoveListener(_OnMessagePostCommitReward);
            MessageCenter.Get<MSG.TREASURE_OPENBOX>().RemoveListener(_OnMessageOpenBox);
        }

        private void Update()
        {
            if (Input.touchSupported)
            {
                if (Input.touchCount > 0 && Time.frameCount > tipShowFrameCount + 2)
                {
                    _TryHideBagEmptyTip();
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _TryHideBagEmptyTip();
                }
            }
        }

        #region stage

        private void _PrepareData()
        {
            var cfg = eventInst.GetCurrentTreasureLevel();
            var totalTreasureNum = cfg.RewardInfo.Count;
            var mapName = $"Map_{totalTreasureNum}";
            curMapRoot = sceneGroup.mapRoot.Find(mapName);
            if (curMapRoot == null)
            {
                DebugEx.Error($"[treasurehunt] invalid treasure count {totalTreasureNum}");
            }
            else
            {
                DebugEx.Info($"[treasurehunt] treasure count {totalTreasureNum}");
            }
        }

        private void _ClearStage()
        {
            if (curMapRoot != null)
            {
                var root = curMapRoot;
                for (var i = 0; i < root.childCount; i++)
                {
                    if (!int.TryParse(root.GetChild(i).gameObject.name, out int index))
                    {
                        continue;
                    }
                    var slot = root.GetChild(i).GetComponent<MBTreasureSlot>();
                    slot.Cleanup();
                }
                root.gameObject.SetActive(false);
            }
        }

        private void _ResetBg()
        {
            if (pandaBodySpine != null && pandaHeadSpine != null)
            {
                var body = pandaBodySpine.gameObject.transform;
                var head = pandaHeadSpine.gameObject.transform;
                body.parent.transform.SetParent(sceneGroup.mapRoot);
                head.parent.transform.SetParent(sceneGroup.mapRoot);
            }

            var container = sceneGroup.sceneBg.parent as RectTransform;
            var screenW = container.rect.width;
            var screenH = container.rect.height;
            var bgScale = curMapRoot?.localScale.x ?? 1f;
            var bgW = sceneGroup.sceneBg.rect.width * bgScale;
            var bgH = sceneGroup.sceneBg.rect.height * bgScale;

            var adjustScaleX = screenW / bgW;
            var adjustScaleY = screenH / bgH;
            if (adjustScaleX > 1 || adjustScaleY > 1)
            {
                // bg和宝箱等比缩放后未能铺满全屏
                // 需要对bg额外缩放
                bgScale *= Mathf.Max(adjustScaleX, adjustScaleY);
            }
            if (!isUseSpine)
                sceneGroup.sceneBg.localScale = Vector3.one * bgScale;
        }

        private void _ResetStage()
        {
            var root = curMapRoot;
            if (root != null)
            {
                using (ObjectPool<Dictionary<int, MBTreasureSlot>>.GlobalPool.AllocStub(out var cache))
                {
                    var count = root.childCount;
                    // 加载资源 / 初始化index
                    for (var i = 0; i < count; i++)
                    {
                        if (!int.TryParse(root.GetChild(i).gameObject.name, out int index))
                        {
                            continue;
                        }
                        var slot = root.GetChild(i).GetComponent<MBTreasureSlot>();
                        slot.Setup(isUseSpine, easeUp, upTime);
                        cache.Add(slot.origSiblingIndex, slot);
                    }
                    if (!isUseSpine)
                    {
                        // 按照固有index重新排序 | 宝箱开启时层级会改变所以关卡重置时需要对slot修正层级
                        for (int i = 0; i < count; i++)
                        {
                            cache[i].transform.SetAsLastSibling();
                        }
                    }
                }
                root.gameObject.SetActive(true);
            }
        }

        private void _ResetProgress()
        {
            string formatter(long cur, long tar)
            {
                return $"{cur / 100}/{tar / 100}";
            }
            // 进度条x100 用于表现动画
            var (cur, max) = eventInst.GetLevelInfo();
            topGroup.progressBar.SetFormatter(formatter);
            topGroup.progressBar.ForceSetup(0, max * 100, cur * 100);

            curLevelIdx = cur;
            totalLevelCount = max;
        }

        private void _TryOpenGiftShopWhenLevelStart()
        {
            if (eventInst.GetKeyNum() == 0 && eventInst.PackValid && eventInst.pack.BuyCount < eventInst.pack.StockTotal)
            {
                var popup = Game.Manager.screenPopup;
                eventInst.TryPopupGift(popup, PopupType.TreasureEnterNoKey);
            }
        }

        private void _RefreshKeyNum()
        {
            bottomGroup.tmpKeyNum.text = $"{eventInst.GetKeyNum()}";
            if (!isUseSpine)
                keyGift.SetActive(eventInst.PackValid);
        }

        private void _ResetLevelReward(bool show)
        {
            var root = bottomGroup.prizeRoot;
            if (show)
            {
                root.gameObject.SetActive(true);
                using (ObjectPool<List<RewardConfig>>.GlobalPool.AllocStub(out var list))
                {
                    eventInst.GetCurrentLevelTreasureRewardConfig(list);
                    UIUtility.CommonItemRefresh(root, list);
                }
            }
            else
            {
                root.gameObject.SetActive(false);
            }
        }

        private void _BindFlyTarget()
        {
            UITreasureHuntUtility.RegisterFlyTarget_LevelReward(flyTarget_LevelReward.position);
            UITreasureHuntUtility.RegisterFlyTarget_ProgressReward(flyTarget_ProgressReward.position);
        }

        private IEnumerator _CoResetShow()
        {
            UITreasureHuntUtility.SetBlock(true);

            _PrepareData();

            _ResetLevelReward(false);
            _ResetBg();
            _ResetStage();
            _ResetProgress();
            _RefreshKeyNum();
            _TryOpenGiftShopWhenLevelStart();

            _RefreshText();
            // 等待loading结束
            yield return new WaitWhile(() => UITreasureHuntUtility.IsLoading);

            _BindFlyTarget();

            // 如果没有展示过教学UI 则显示教学UI
            // 等待教学关闭
            if (!eventInst.IsEnterHuntGuided())
            {
                _ShowTutorial();
                eventInst.SetEnterGuideHasPopup();
                // 等待关闭UI后才能继续
                yield return new WaitUntil(() => UIManager.Instance.LoadingCount == 0 && !UIManager.Instance.IsOpen(eventInst.HelpTabRes.ActiveR));
            }

            var root = curMapRoot;
            if (root == null)
                yield break;

            bool hasAnyOpen = false;
            // 显示已开的箱子
            for (var i = 0; i < root.childCount; i++)
            {
                if (!int.TryParse(root.GetChild(i).gameObject.name, out int index))
                {
                    continue;
                }
                var slot = root.GetChild(i).GetComponent<MBTreasureSlot>();
                if (eventInst.HasOpen(index))
                {
                    hasAnyOpen = true;
                    slot.Show();
                    slot.SetState(State.Dead);
                }
            }

            if (isUseSpine)
            {
                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.PandaBoxDown);
            }
            else
            {
                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureSpawn);
            }

            // 显示剩余箱子
            for (var i = 0; i < root.childCount; i++)
            {
                if (!int.TryParse(root.GetChild(i).gameObject.name, out int index))
                {
                    continue;
                }
                var slot = root.GetChild(i).GetComponent<MBTreasureSlot>();
                if (!eventInst.HasOpen(index))
                {
                    slot.Show();

                    // 每次进入关卡都会播放宝箱落地效果
                    slot.SetState(State.Born);
                    slot.SetIdleDelay(2.1f + 0.3f * index);
                    yield return new WaitForSeconds(treasureBornDelay);

                    // // 仅在没有开启过宝箱时播放落地效果
                    // if (!hasAnyOpen)
                    // {
                    //     slot.SetState(State.Born);
                    //     yield return new WaitForSeconds(treasureBornDelay);
                    // }
                    // else
                    // {
                    //     slot.SetState(State.Idle);
                    // }
                }
            }

            _ResetLevelReward(true);

            UITreasureHuntUtility.SetBlock(false);
        }

        private void _RefreshText()
        {
            eventInst.Visual.Theme.AssetInfo.TryGetValue("spriteName", out string s);
            bottomText.SetText(I18N.FormatText("#SysComDesc770", s));
        }

        private IEnumerator _CoLevelClear()
        {
            UITreasureHuntUtility.SetBlock(true);

            // 隐藏关卡奖励
            _ResetLevelReward(false);

            // 等待飞奖励 / 等待进度条变化
            yield return new WaitForSeconds(1f);

            UITreasureHuntUtility.SetBlock(false);

            var tar = curLevelIdx + 1;
            if (tar >= totalLevelCount)
            {
                // 关卡组完成 展示进度条奖励
                eventInst.ProgressRewardRes.ActiveR.Open(UITreasureHuntUtility.progressRewardTarget, UITreasureHuntUtility.tempProgressRewards);
            }
            else
            {
                UITreasureHuntUtility.MoveToNextLevel();
            }
        }

        private IEnumerator _CoProgressClear()
        {
            UITreasureHuntUtility.SetBlock(true);
            yield return new WaitForSeconds(1f);
            UITreasureHuntUtility.SetBlock(false);
            if (eventInst.HasNextLevelGroup())
            {
                UITreasureHuntUtility.MoveToNextLevel();
            }
            else
            {
                UITreasureHuntUtility.LeaveActivity();
            }
        }

        #endregion

        private void _ShowTutorial()
        {
            eventInst.HelpTabRes.ActiveR.Open();
        }

        private void _TryHideBagEmptyTip()
        {
            if (bottomGroup.goBagEmptyTip.activeSelf)
            {
                bottomGroup.goBagEmptyTip.SetActive(false);
            }
        }

        private void _OnBtnEventInfo()
        {
            _ShowTutorial();
        }

        private void _OnBtnProgressRewardPreview()
        {
            UIManager.Instance.OpenWindow(UIConfig.UITreasureHuntRewardTips, topGroup.rewardTipAnchor.position, 0f);
        }

        private void _OnBtnQuit()
        {
            UITreasureHuntUtility.LeaveActivity();
        }

        private void _OnBtnPack()
        {
            if (eventInst?.BagItemNum > 0)
            {
                UIManager.Instance.OpenWindow(UIConfig.UITreasureHuntBag);
            }
            else
            {
                tipShowFrameCount = Time.frameCount;
                bottomGroup.goBagEmptyTip.SetActive(true);
            }
        }

        private void _OnBtnKey()
        {
            UITreasureHuntUtility.TryOpenGiftShop();
        }

        private void _OnMessageLevelClear()
        {
            StartCoroutine(_CoLevelClear());
        }

        private void _OnMessageLevelGroupClear()
        {
            StartCoroutine(_CoProgressClear());
        }

        private void _OnMessageTreasureFlyFeedback()
        {
            // 进度条加100
            var tar = curLevelIdx + 1;
            topGroup.progressBar.SetProgress(tar * 100);
            UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureProgressGrowth);

            // anim
            topGroup.treasureIconAni?.SetTrigger("Punch");
        }

        private void _OnMessageKeyChange(int changeNum)
        {
            // 扣除时立即刷新
            if (changeNum < 0)
            {
                _RefreshKeyNum();
            }
            else if (changeNum > 0)
            {
                // 数量增加时需要看情况刷新
                // 在PostCommitReward时处理
                // 仅处理购买获得 / debug获得
            }
        }

        private void _OnMessagePostCommitReward(RewardCommitData data)
        {
            if (eventInst == null)
                return;
            // 此处忽略掉活动玩法中开出的钥匙
            if (data.rewardId == eventInst.ConfD.RequireCoinId && data.reason != ReasonString.treasure_key_by_open_box)
            {
                _RefreshKeyNum();
            }
        }

        private void _OnMessageOpenBox(Vector3 vector, int idx)
        {
            if (!isUseSpine)
            {
                return;
            }

            var body = pandaBodySpine.gameObject.transform;
            var head = pandaHeadSpine.gameObject.transform;
            var index = curMapRoot.transform.GetSiblingIndex();
            var bgScale = pandaScale[index];
            head.parent.position.Set(vector.x, head.position.y, head.position.z);
            body.parent.position.Set(vector.x, body.position.y, body.position.z);
            head.parent.SetParent(curMapRoot.Find(idx.ToString()));
            head.parent.SetAsLastSibling();
            body.parent.SetParent(curMapRoot.Find(idx.ToString()));
            body.parent.SetAsFirstSibling();
            head.parent.transform.localScale = Vector3.one * bgScale;
            body.parent.transform.localScale = Vector3.one * bgScale;
            var v3 = new Vector3(vector.x, vector.y + downOffset[index], vector.z);
            Sequence sequence = DOTween.Sequence();
            sequence.Append(body.parent.DOMove(new Vector3(vector.x, body.parent.position.y, body.parent.position.z), 0));
            sequence.Join(head.parent.DOMove(new Vector3(vector.x, body.parent.position.y, body.parent.position.z), 0));
            sequence.Append(body.parent.DOMove(v3, downTime).SetEase(easeDown));
            sequence.Join(head.parent.DOMove(v3, downTime).SetEase(easeDown));
            sequence.Play();
            pandaBodySpine.AnimationState.SetAnimation(0, "open_a", false).Complete += delegate (TrackEntry entry)
            {
                pandaBodySpine.AnimationState.SetAnimation(0, "open_b", false).Complete += delegate (TrackEntry entry)
                {
                    body.parent.DOMoveY(body.parent.position.y + upHeight, upTime).SetEase(easeUp);
                    pandaBodySpine.AnimationState.SetAnimation(0, "open_c", false);
                };
            };
            pandaHeadSpine.AnimationState.SetAnimation(0, "open_a", false).Complete += delegate (TrackEntry entry)
            {
                pandaHeadSpine.AnimationState.SetAnimation(0, "open_b", false).Complete += delegate (TrackEntry entry)
                {
                    head.parent.DOMoveY(head.parent.position.y + upHeight, upTime).SetEase(easeUp);
                    pandaHeadSpine.AnimationState.SetAnimation(0, "open_c", false);
                };
            };
        }

        private void _OnMessageFlyFeedback(FlyType ft)
        {
            if (ft == FlyType.TreasureBag)
            {
                btnPack.GetComponent<Animator>().SetTrigger("Punch");
                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureBag);
            }
            else if (ft == FlyType.TreasureKey)
            {
                btnKey.GetComponent<Animator>().SetTrigger("Punch");
                UITreasureHuntUtility.PlaySound(UITreasureHuntUtility.SoundEffect.TreasureGetKey);
                // 作为收集项飞到目标后刷新钥匙数量
                _RefreshKeyNum();
            }
            else if (ft == FlyType.TreasureBonusToken)
            {
                milestone.Anim();
            }
        }

        private void _OnSecondPass()
        {
            if (!UITreasureHuntUtility.IsEventActive())
            {
                UITreasureHuntUtility.TryForceLeaveActivity();
            }
        }
    }
}