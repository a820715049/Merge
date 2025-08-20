/**
 * @Author: zhangpengjian
 * @Date: 2025/5/13 19:12:45
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/13 19:12:45
 * Description: 打怪棋盘主界面
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using fat.rawdata;
using FAT.Merge;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public enum HPState
    {
        Green,
        Yellow,
        Red
    }
    public class UIActivityFightMain : UIBase, INavBack
    {
        public MBBoardView _view;
        private TextMeshProUGUI _cd;
        private MBFightBoardReward _reward;
        private PackOnePlusOneFight _giftPack;
        private GameObject _giftPackGo;
        private UIImageRes _giftPackIcon;
        private TextMeshProUGUI _giftPackCd;
        private FightBoardActivity _activity;
        private Image _boardEntry;
        private bool _isTapBonus;
        private Sequence _commonResSeq;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private UICommonProgressBar progressBar;
        [SerializeField] private GameObject block;
        [SerializeField] private MBRewardProgress hpProgressGreen;
        [SerializeField] private MBRewardProgress hpProgressYellow;
        [SerializeField] private MBRewardProgress hpProgressRed;
        [SerializeField] private Button milestoneBtn;
        [SerializeField] private TextMeshProUGUI milestoneNum;
        [SerializeField] private Transform rewardNode;
        [SerializeField] private Button goBtn;
        [SerializeField] private MBFlyTarget mergeItem;
        [SerializeField] private Transform hpLineRoot;
        [SerializeField] private Transform talkAtkRoot;
        [SerializeField] private Transform milestoneRoot;
        [SerializeField] private GameObject hpLine;
        [SerializeField] private GameObject talkObj;
        [SerializeField] private GameObject milestoneReward;
        [SerializeField] private Transform talkCurse;
        [SerializeField] private Transform talkAtkA;
        [SerializeField] private Transform talkAtkB;
        [SerializeField] private Transform talkAtkC;
        [SerializeField] private GameObject cycleNode;
        [SerializeField] private GameObject cycleTalk;
        [SerializeField] private GameObject monsterRoot;
        [SerializeField] private Animator monsterAnimator;
        [SerializeField] private Animator cycleAnimator;
        [SerializeField] private GameObject talkPlay;
        [SerializeField] private TextMeshProUGUI talkPlayText;
        [SerializeField] private GameObject talkDie;
        [SerializeField] private TextMeshProUGUI talkDieText;
        [SerializeField] private float initialTextScale = 1.5f; // 初始字体大小缩放
        [SerializeField] private float finalTextScale = 1f;     // 最终字体大小缩放 
        [SerializeField] private float scaleTime = 0.2f;        // 缩放动画时间
        [SerializeField] private float moveTime = 0.3f;         // 位移动画时间
        [SerializeField]
        private AnimationCurve scaleEaseCurve = new AnimationCurve(  // 缩放动画曲线
            new Keyframe(0, 1),
            new Keyframe(0.5f, 1.2f),
            new Keyframe(1, 1)
        );
        [SerializeField] private float damageInterval = 0.5f;
        private string hp_line_key = "hp_line_key";
        private string talk_atk_key = "talk_atk_key";
        private string milestone_reward_key = "milestone_reward_key";
        private List<RewardCommitData> _levelRewards = new();
        private EventFightLevel _currentLevel;
        private HPState _currentHPState = HPState.Green; // 记录当前血条状态
        private int curLevelIdx;
        private GameObject monsterObj;
        private GameObject boxObj;
        private bool isChangeLevel = false;
        private bool isLastLevel = false;
        private List<GameObject> milestoneObj = new();
        private float loadingChangeDelay = 1f;
        private bool isReceiveAttack = false;
        private bool isTransitioning = false;
        private List<Item> items = new();

        protected override void OnCreate()
        {
            RegiestComp();
            Setup();
            AddButton();
        }

        private void RegiestComp()
        {
            transform.Access("Content/TopBg/text", out _cd);
            transform.Access("Content/BoardRewardNode", out _reward);
            transform.Access("Content/Bottom/FlyTarget/Entry", out _boardEntry);
            transform.FindEx("Content/Center/GiftNode", out _giftPackGo);
            transform.Access("Content/Center/GiftNode/Icon", out _giftPackIcon);
            transform.Access("Content/Center/GiftNode/cd", out _giftPackCd);
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
            transform.AddButton("Content/Center/GiftNode", ClickPack);
            milestoneBtn.onClick.AddListener(ClickMilestone);
            goBtn.onClick.AddListener(ClickGo);
        }

        private void ClickPack()
        {
            if (_giftPack != null)
                UIManager.Instance.OpenWindow(_giftPack.Res.ActiveR, _giftPack);
        }

        private void ClickGo()
        {
            ActivityTransit.Exit(_activity, ResConfig, () => {
                UIConfig.UIMessageBox.Close();
            }, true); // 退出时默认返回主棋盘
        }

        private void ClickMilestone()
        {
            _activity.MilestoneRes.res.ActiveR.Open(_activity);
        }

        private void ClickHelp()
        {
            UIManager.Instance.OpenWindow(_activity.HelpRes.res.ActiveR, _activity);
        }

        private void ClickClose()
        {
            ActivityTransit.Exit(_activity, ResConfig, () => {
                UIConfig.UIMessageBox.Close();
            });
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as FightBoardActivity;
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
            cycleTalk.SetActive(false);
            isReceiveAttack = false;
            isTransitioning = false;
            rewardNode.gameObject.SetActive(true);
            mergeItem.enabled = false;
            mergeItem.gameObject.SetActive(false);
            _reward.Refresh(_activity);
            RefreshGiftPack();
            RefreshHP(_activity.monster.Hp, _activity.monster.MaxHp);
            RefreshCD(false);
            RefreshMilestone();
            RefreshMonster();
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

            MessageCenter.Get<MSG.FIGHT_RECEIVE_ATTACK>().AddListener(OnReceiveAttack);
            MessageCenter.Get<MSG.FIGHT_LEVEL_REWARD>().AddListener(OnReceiveLevelReward);
            MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLOSE>().AddListener(OnMilestoneEnd);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(OnMilestoneEnd);
            MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLICK_CLAIM>().AddListener(WhenFlyReward);
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().AddListener(RefreshGiftPack);

        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCDSecond);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FlyFeedBack);
            MessageCenter.Get<MSG.FLY_ICON_START>().RemoveListener(CheckNewFly);

            MessageCenter.Get<MSG.FIGHT_RECEIVE_ATTACK>().RemoveListener(OnReceiveAttack);
            MessageCenter.Get<MSG.FIGHT_LEVEL_REWARD>().RemoveListener(OnReceiveLevelReward);
            MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLOSE>().RemoveListener(OnMilestoneEnd);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnMilestoneEnd);
            MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLICK_CLAIM>().RemoveListener(WhenFlyReward);
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().RemoveListener(RefreshGiftPack);
        }

        private void RefreshCDSecond()
        {
            RefreshCD();
        }

        private void OnReceiveLevelReward(List<RewardCommitData> rewards, EventFightLevel eventFightLevel)
        {
            _levelRewards.AddRange(rewards);
            _currentLevel = eventFightLevel;
        }

        private void OnReceiveAttack(AttackInfo info)
        {
            isReceiveAttack = true;
            talkPlay.SetActive(false);
            StartCoroutine(CoOnReceiveAttack(info));
        }

        private IEnumerator CoOnReceiveAttack(AttackInfo info)
        {
            // 延迟0.35秒
            yield return new WaitForSeconds(0.42f);

            var talkObjA = GameObjectPoolManager.Instance.CreateObject(talk_atk_key, talkAtkRoot);
            var talkObjB = GameObjectPoolManager.Instance.CreateObject(talk_atk_key, talkAtkRoot);
            var talkObjC = GameObjectPoolManager.Instance.CreateObject(talk_atk_key, talkAtkRoot);
            var talkObjD = GameObjectPoolManager.Instance.CreateObject(talk_atk_key, talkAtkRoot);
            talkObjA.transform.localPosition = new Vector3(0, 0, 0);
            talkObjB.transform.localPosition = new Vector3(0, 0, 0);
            talkObjC.transform.localPosition = new Vector3(0, 0, 0);
            talkObjD.transform.localPosition = new Vector3(0, 0, 0);
            talkObjA.transform.localScale = new Vector3(1, 1, 1);
            talkObjB.transform.localScale = new Vector3(1, 1, 1);
            talkObjC.transform.localScale = new Vector3(1, 1, 1);
            talkObjD.transform.localScale = new Vector3(1, 1, 1);
            talkObjA.transform.localRotation = Quaternion.identity;
            talkObjB.transform.localRotation = Quaternion.identity;
            talkObjC.transform.localRotation = Quaternion.identity;
            talkObjD.transform.localRotation = Quaternion.identity;
            talkObjA.SetActive(false);
            talkObjB.SetActive(false);
            talkObjC.SetActive(false);
            talkObjD.SetActive(false);
            var damageList = info.damage;

            // 添加最小显示逻辑
            int displayAfterHp = info.afterHp;
            if (info.afterHp > 0 && info.afterHp < _activity.monster.MaxHp * 0.05f)
            {
                displayAfterHp = Mathf.CeilToInt(_activity.monster.MaxHp * 0.05f);
            }

            var progress = _currentHPState == HPState.Green ? hpProgressGreen :
                           _currentHPState == HPState.Yellow ? hpProgressYellow :
                           hpProgressRed;

            progress.Refresh(displayAfterHp, _activity.monster.MaxHp, 0.5f, () =>
            {
                hpProgressGreen.Refresh(displayAfterHp, _activity.monster.MaxHp);
                hpProgressYellow.Refresh(displayAfterHp, _activity.monster.MaxHp);
                hpProgressRed.Refresh(displayAfterHp, _activity.monster.MaxHp);
                RefreshHPState(info.afterHp, _activity.monster.MaxHp);
            });
            if (monsterObj != null)
            {
                if (_currentLevel != null && _levelRewards.Count > 0)
                {
                    if (IsComplete() && _activity.CanShowCycleHint())
                    {
                        isLastLevel = true;
                    }
                }
                // 同步播放 monster 和 box 的攻击动画
                var monsterSpine = monsterObj.transform.GetChild(0).GetComponent<SkeletonGraphic>();
                var boxSpine = boxObj?.transform.GetChild(0).GetComponent<SkeletonGraphic>();
                if (damageList.Any(d => d.Item2))
                {
                    if (IsComplete() && !isLastLevel)
                    {
                        cycleAnimator.SetTrigger("Effect02");
                    }
                    else
                    {
                        monsterAnimator.SetTrigger("Effect02");
                    }
                }
                else
                {
                    if (IsComplete() && !isLastLevel)
                    {
                        cycleAnimator.SetTrigger("Effect01");
                    }
                    else
                    {
                        monsterAnimator.SetTrigger("Effect01");
                    }
                }
                monsterSpine.AnimationState.SetAnimation(0, "attack", false).Complete += delegate (TrackEntry entry)
                {
                    if (_currentLevel != null && _levelRewards.Count > 0)
                    {
                        HideHpProgress();
                        if (!IsComplete() || isLastLevel)
                        {
                            Game.Manager.audioMan.TriggerSound("FightBoardEscape");
                            StartCoroutine(CoDelayTriggerSound());
                            monsterSpine.AnimationState.SetAnimation(0, "hide", false);
                            boxSpine?.AnimationState.SetAnimation(0, "hide", false);
                            TalkDie();
                        }
                        else
                        {
                            monsterSpine.AnimationState.SetAnimation(0, "idle", true);
                            boxSpine?.AnimationState.SetAnimation(0, "idle", true);
                        }
                    }
                    else
                    {
                        monsterSpine.AnimationState.SetAnimation(0, "idle", true);
                        boxSpine?.AnimationState.SetAnimation(0, "idle", true);
                    }
                };

                boxSpine?.AnimationState.SetAnimation(0, "attack", false);
            }

            if (!IsComplete())
            {
                StartCoroutine(ShowAttackText(talkObjD, talkCurse, damageList[0], 0f, true, info));
            }
            if (damageList.Count > 0)
                StartCoroutine(ShowAttackText(talkObjA, talkAtkA, damageList[0], damageInterval));
            if (damageList.Count > 1)
                StartCoroutine(ShowAttackText(talkObjB, talkAtkB, damageList[1], damageInterval * 2));
            if (damageList.Count > 2)
                StartCoroutine(ShowAttackText(talkObjC, talkAtkC, damageList[2], damageInterval * 2 + damageInterval));
            if (_currentLevel != null && _levelRewards.Count > 0)
            {
                StartCoroutine(CoOnShowEnd());
            }
            else
            {
                yield return new WaitForSeconds(1f);
                isReceiveAttack = false;
            }
        }

        private IEnumerator CoDelayTriggerSound()
        {
            yield return new WaitForSeconds(0.23f);
            Game.Manager.audioMan.TriggerSound("BoardReward");
        }

        private void HideHpProgress()
        {
            hpProgressGreen.gameObject.SetActive(false);
            hpProgressYellow.gameObject.SetActive(false);
            hpProgressRed.gameObject.SetActive(false);
            for (int i = 0; i < hpLineRoot.childCount; i++)
            {
                hpLineRoot.GetChild(i).gameObject.SetActive(false);
            }
        }

        private IEnumerator CoOnShowEnd()
        {
            block.transform.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            boxObj?.gameObject.SetActive(false);
            monsterObj.gameObject.SetActive(false);
            Game.Manager.audioMan.TriggerSound("DuelReward");
            var from = (IsComplete() && !isLastLevel) ? UIFlyFactory.ResolveFlyTarget(FlyType.FightBoardTreasure) : cycleNode.transform.position;
            UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, from, _levelRewards, _currentLevel.LevelRewardIcon, I18N.Text("#SysComDesc726"), from);
            isChangeLevel = true;
        }

        private void WhenFlyReward()
        {
            if (mergeItem != null)
            {
                mergeItem.enabled = true;
                mergeItem.gameObject.SetActive(true);
            }
        }

        private void OnMilestoneEnd()
        {
            if (!isChangeLevel)
            {
                return;
            }
            _currentLevel = null;
            _levelRewards.Clear();
            if (boxObj != null)
            {
                Destroy(boxObj);
                boxObj = null;
            }
            if (monsterObj != null)
            {
                Destroy(monsterObj);
                monsterObj = null;
            }
            StartCoroutine(CoOnMilestoneEnd());
        }

        private IEnumerator CoOnMilestoneEnd()
        {
            yield return new WaitForSeconds(1f);
            if (!IsComplete() || !_activity.CanShowCycleHint())
            {
                Game.Instance.StartCoroutineGlobal(_CoLoading(_MovoToLevel));
            }
            else
            {
                ShowCycleTalkTip(() => { Game.Instance.StartCoroutineGlobal(_CoLoading(_MovoToLevel)); });
            }
        }

        private void _MovoToLevel()
        {
            _activity.BoardRes.res.ActiveR.Close();
            _activity.BoardRes.res.ActiveR.Open(_activity);
        }

        private IEnumerator _CoLoading(Action afterFadeIn = null, Action afterFadeOut = null)
        {

            var waitFadeInEnd = new SimpleAsyncTask();
            var waitFadeOutEnd = new SimpleAsyncTask();
            var waitLoadingJobFinish = new SimpleAsyncTask();
            //复用寻宝loading音效
            Game.Manager.audioMan.TriggerSound("UnderseaTreasure");

            _activity.LoadingRes.res.ActiveR.Open(waitLoadingJobFinish, waitFadeInEnd, waitFadeOutEnd);

            yield return waitFadeInEnd;

            afterFadeIn?.Invoke();

            waitLoadingJobFinish.ResolveTaskSuccess();

            yield return waitFadeOutEnd;

            afterFadeOut?.Invoke();

        }

        private IEnumerator ShowAttackText(GameObject talkObj, Transform parent, (int damage, bool isCritical) damageInfo, float delay, bool isTalk = false, AttackInfo info = null)
        {
            yield return new WaitForSeconds(delay);
            if (!isTalk)
            {
                Game.Manager.audioMan.TriggerSound("FightBoardAttack");
            }
            talkObj.transform.SetParent(parent);
            talkObj.transform.localPosition = Vector3.zero;
            talkObj.transform.localScale = Vector3.one;
            talkObj.transform.localRotation = Quaternion.identity;
            var textComp = talkObj.GetComponent<TextMeshProUGUI>();
            var canvasGroup = talkObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = talkObj.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1;

            // 设置伤害数值
            textComp.text = damageInfo.damage.ToString();
            textComp.color = Color.white;
            // 根据是否暴击设置不同样式
            if (damageInfo.isCritical)
            {
                _activity.Visual.Refresh(textComp, "critical");
            }
            else
            {
                _activity.Visual.Refresh(textComp, "normal");
            }
            if (isTalk)
            {
                textComp.text = info.curseStr;
                _activity.Visual.Refresh(textComp, "talk");
                _activity.Visual.StyleMap.TryGetValue("talkColor", out var colorStr);
                if (colorStr.StartsWith('#'))
                {
                    textComp.color = ColorUtility.TryParseHtmlString(colorStr, out var color) ? color : Color.white;
                }
            }

            talkObj.SetActive(true);

            var startPos = talkObj.transform.localPosition;
            var endPos = startPos + new Vector3(0, 100, 0);

            // 设置初始缩放
            talkObj.transform.localScale = Vector3.one * initialTextScale;

            var seq = DOTween.Sequence();

            // 先执行缩放动画
            seq.Append(talkObj.transform.DOScale(Vector3.one * finalTextScale, scaleTime).SetEase(scaleEaseCurve));

            // 缩放完成后执行位移和渐隐动画
            seq.Join(talkObj.transform.DOLocalMove(endPos, moveTime).SetEase(Ease.OutQuad).SetDelay(0.1f));
            seq.Join(canvasGroup.DOFade(0, moveTime));

            seq.OnComplete(() =>
            {
                GameObjectPoolManager.Instance.ReleaseObject(talk_atk_key, talkObj);
            });
        }

        private void PreparePool()
        {
            if (!GameObjectPoolManager.Instance.HasPool(hp_line_key))
            {
                GameObjectPoolManager.Instance.PreparePool(hp_line_key, hpLine);
            }
            if (!GameObjectPoolManager.Instance.HasPool(talk_atk_key))
            {
                GameObjectPoolManager.Instance.PreparePool(talk_atk_key, talkObj);
            }
            if (!GameObjectPoolManager.Instance.HasPool(milestone_reward_key))
            {
                GameObjectPoolManager.Instance.PreparePool(milestone_reward_key, milestoneReward);
            }
        }

        protected override void OnPreClose()
        {
            _currentLevel = null;
            _levelRewards.Clear();
            if (boxObj != null)
            {
                Destroy(boxObj);
                boxObj = null;
            }
            if (monsterObj != null)
            {
                Destroy(monsterObj);
                monsterObj = null;
            }
            for (int i = 0; i < hpLineRoot.childCount; i++)
            {
                GameObjectPoolManager.Instance.ReleaseObject(hp_line_key, hpLineRoot.GetChild(i).gameObject);
            }
            for (int i = 0; i < talkAtkRoot.childCount; i++)
            {
                GameObjectPoolManager.Instance.ReleaseObject(talk_atk_key, talkAtkRoot.GetChild(i).gameObject);
            }
            if (!isChangeLevel)
            {
                for (int i = 0; i < milestoneObj.Count; i++)
                {
                    GameObjectPoolManager.Instance.ReleaseObject(milestone_reward_key, milestoneObj[i]);
                }
            }
            UIManager.Instance.CloseWindow(UIConfig.UIPopFlyTips);
            UIManager.Instance.CloseWindow(UIConfig.UIPopFlyTips);
            UIManager.Instance.CloseWindow(UIConfig.UIEnergyBoostTips);
            if (_commonResSeq != null) _commonResSeq.Kill();
            if (BoardViewWrapper.GetCurrentWorld() == null) return;
            _view.OnBoardLeave();
            BoardViewWrapper.PopWorld();
            _giftPack = null;
            var world = _activity?.World;
            if (world != null)
            {
                world.onItemEvent -= OnItemEvent;
            }
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

        private void RefreshMonster()
        {
            var monster = _activity.monster;
            if (!string.IsNullOrEmpty(monster.boxAsset) && boxObj == null)
            {
                Game.Instance.StartCoroutineGlobal(CoLoadBox(monster.boxAsset));
            }
            if (!string.IsNullOrEmpty(monster.monsterAsset) && monsterObj == null)
            {
                Game.Instance.StartCoroutineGlobal(CoLoadMonster(monster.monsterAsset));
            }
            /*else
            {
                if (isChangeLevel)
                {
                    isChangeLevel = false;
                    StartCoroutine(CoDelayShowSpine());
                }
                else
                {
                    monsterObj.transform.GetChild(0).GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, "idle", true);
                    TryTrick();
                }
            }*/

        }

        private IEnumerator CoDelayShowSpine()
        {
            monsterObj.gameObject.SetActive(false);
            boxObj?.gameObject.SetActive(false);
            yield return new WaitForSeconds(loadingChangeDelay);
            var monsterSpine = monsterObj.transform.GetChild(0).GetComponent<SkeletonGraphic>();
            var boxSpine = boxObj?.transform.GetChild(0).GetComponent<SkeletonGraphic>();
            monsterObj.gameObject.SetActive(true);
            boxObj?.gameObject.SetActive(true);
            monsterSpine.AnimationState.SetAnimation(0, "enter", false).Complete += (entry) =>
            {
                if (!IsComplete())
                {
                    Talk(true);
                }
                monsterSpine.AnimationState.SetAnimation(0, "idle", true);
                boxSpine?.AnimationState.SetAnimation(0, "idle", true);
                TryTrick();
            };
            if (boxSpine != null)
            {
                Game.Manager.audioMan.TriggerSound("FightBoardAppear");
            }
            else
            {
                Game.Manager.audioMan.TriggerSound("BoardReward");
            }
            boxSpine?.AnimationState.SetAnimation(0, "enter", false);
        }

        private IEnumerator CoLoadBox(string asset)
        {
            var assetConfig = asset.ConvertToAssetConfig();
            var loader = EL.Resource.ResManager.LoadAsset<GameObject>(assetConfig.Group, assetConfig.Asset);
            yield return loader;
            boxObj = Instantiate(loader.asset as GameObject, monsterRoot.transform);
            boxObj.transform.localPosition = Vector3.zero;
            boxObj.transform.localScale = Vector3.one;
            boxObj.transform.localRotation = Quaternion.identity;
            boxObj.transform.SetParent(monsterRoot.transform);
            boxObj.SetActive(true);
            boxObj.transform.SetSiblingIndex(0);
            boxObj.transform.GetChild(0).GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, "idle", true);
        }

        private IEnumerator CoLoadMonster(string asset)
        {
            var assetConfig = asset.ConvertToAssetConfig();
            var loader = EL.Resource.ResManager.LoadAsset<GameObject>(assetConfig.Group, assetConfig.Asset);
            yield return loader;
            monsterObj = Instantiate(loader.asset as GameObject, monsterRoot.transform);
            monsterObj.transform.localPosition = Vector3.zero;
            monsterObj.transform.localScale = Vector3.one;
            monsterObj.transform.localRotation = Quaternion.identity;
            monsterObj.transform.SetParent(monsterRoot.transform);
            monsterRoot.SetActive(true);
            monsterObj.transform.SetSiblingIndex(1);
            if (isChangeLevel)
            {
                isChangeLevel = false;
                StartCoroutine(CoDelayShowSpine());
            }
            else
            {
                monsterObj.transform.GetChild(0).GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, "idle", true);
                TryTrick();
            }
        }

        private void TryTrick()
        {
            // 开始循环播放动画
            if (!IsComplete())
            {
                StartCoroutine(PlayMonsterAnimation());
            }
        }

        private bool IsComplete()
        {
            return _activity.GetCurrentMilestoneIndex() >= _activity.GetFightLevels().Count;
        }


        private IEnumerator PlayMonsterAnimation()
        {
            while (monsterObj != null)
            {
                yield return new WaitForSeconds(5f);

                if (monsterObj != null && _currentLevel == null && _levelRewards.Count <= 0 && !isReceiveAttack)
                {
                    var monsterSpine = monsterObj.transform.GetChild(0).GetComponent<SkeletonGraphic>();
                    var boxSpine = boxObj?.transform.GetChild(0).GetComponent<SkeletonGraphic>();

                    // 播放动画，完成后回到idle
                    monsterSpine.AnimationState.SetAnimation(0, "provoke", false).Complete += (entry) =>
                    {
                        if (monsterObj != null)
                        {
                            monsterSpine.AnimationState.SetAnimation(0, "idle", true);
                            boxSpine?.AnimationState.SetAnimation(0, "idle", true);
                        }
                    };

                    boxSpine?.AnimationState.SetAnimation(0, "provoke", false);

                    Talk();
                }
            }
        }

        private void TalkDie()
        {
            var canvasGroup = talkDie.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = talkDie.AddComponent<CanvasGroup>();
            }

            talkDie.SetActive(true);
            talkDieText.text = _activity.monster.diedStr();
            canvasGroup.alpha = 0;

            var seq = DOTween.Sequence();
            // 淡入
            seq.Append(canvasGroup.DOFade(1, 0.3f));
            // 停留
            seq.AppendInterval(1.5f);
            // 淡出
            seq.Append(canvasGroup.DOFade(0, 0.3f));
            seq.OnComplete(() =>
            {
                talkDie.SetActive(false);
            }).SetDelay(0.15f);
        }

        private void Talk(bool isAppear = false)
        {
            // 处理talkPlay的渐显渐隐
            var canvasGroup = talkPlay.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = talkPlay.AddComponent<CanvasGroup>();
            }

            talkPlay.SetActive(true);
            talkPlayText.text = isAppear ? _activity.monster.appearStr() : _activity.monster.trickStr();
            canvasGroup.alpha = 0;

            var seq = DOTween.Sequence();
            // 淡入
            seq.Append(canvasGroup.DOFade(1, 0.3f));
            // 停留
            seq.AppendInterval(1.5f);
            // 淡出
            seq.Append(canvasGroup.DOFade(0, 0.3f));
            seq.OnComplete(() =>
            {
                talkPlay.SetActive(false);
            }).SetDelay(0.15f);
        }

        private void RefreshMilestone()
        {
            milestoneNum.text = _activity.GetMilestoneText();
            var mL = _activity.GetFightLevels();
            var max = _activity.GetFightLevels().Count - 1;
            var cur = _activity.GetCurrentMilestoneIndex();

            if (isChangeLevel && (!IsComplete() || isLastLevel))
            {
                isLastLevel = false;
                progressBar.SetFormatter((cur, tar) => $"{cur / 100}/{tar / 100}");
                progressBar.ForceSetup(0, max * 100, (cur - 1) * 100);
                curLevelIdx = cur;
                StartCoroutine(OnProgressAnim(cur));
            }
            else
            {
                if (milestoneObj.Count > 0)
                {
                    for (int i = 0; i < milestoneObj.Count; i++)
                    {
                        GameObjectPoolManager.Instance.ReleaseObject(milestone_reward_key, milestoneObj[i]);
                    }
                    milestoneObj.Clear();
                }

                for (int i = 0; i < mL.Count; i++)
                {
                    var item = GameObjectPoolManager.Instance.CreateObject(milestone_reward_key, milestoneRoot);
                    var cell = item.GetComponent<MBFightBoardMilestoneCell>();
                    cell.UpdateContent(_activity, mL[i]);
                    milestoneObj.Add(item);
                }
                progressBar.SetFormatter((cur, tar) => $"{cur / 100}/{tar / 100}");
                progressBar.ForceSetup(0, max * 100, cur * 100);
                curLevelIdx = cur;

                StartCoroutine(DelayScroll());
            }
        }

        private IEnumerator OnProgressAnim(int cur)
        {
            yield return new WaitForSeconds(2f);

            if (cur > 0 && cur <= milestoneObj.Count)
            {
                var prevCell = milestoneObj[cur - 1].GetComponent<MBFightBoardMilestoneCell>();
                prevCell.PlayCompleteAnimation();
            }

            yield return new WaitForSeconds(0.25f);

            progressBar.SetProgress(cur * 100);

            yield return new WaitForSeconds(0.3f);

            if (cur < milestoneObj.Count)
            {
                var currentCell = milestoneObj[cur].GetComponent<MBFightBoardMilestoneCell>();
                currentCell.UpdateContent(_activity, _activity.GetFightLevels()[cur]);
                currentCell.PlayAnimation();
            }
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(DelayScroll());
        }

        private IEnumerator DelayScroll()
        {
            yield return null;
            float contentWidth = scroll.content.rect.width;
            float viewportWidth = scroll.viewport.rect.width;
            float normalizedPosition = Mathf.Clamp01((float)curLevelIdx / (_activity.GetFightLevels().Count - 1));

            if (contentWidth > viewportWidth)
            {
                scroll.horizontalNormalizedPosition = normalizedPosition;
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
                _activity.HelpRes.res.ActiveR.Close();
                ActivityTransit.Exit(_activity, ResConfig, () => {
                    UIConfig.UIMessageBox.Close();
                });
                return;
            }
            if (_giftPack != null)
            {
                var diff1 = (long)Mathf.Max(0, _giftPack.endTS - t);
                UIUtility.CountDownFormat(_giftPackCd, diff1);
            }
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

        private void RefreshGiftPack()
        {
            _giftPack = Game.Manager.activity.LookupAny(fat.rawdata.EventType.FightOnePlusOne) as PackOnePlusOneFight;
            if (_giftPack != null)
            {
                _giftPackGo.SetActive(true);
                _giftPackIcon.SetImage(_giftPack.EntryIcon);
            }
            else
            {
                _giftPackGo.SetActive(false);
            }
        }

        private bool NeedShowPlay()
        {
            if (_activity.World.rewardCount > 0)
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

        private void RefreshHPLines(int current, int max)
        {
            // 清理旧的竖线
            for (int i = hpLineRoot.childCount - 1; i >= 0; i--)
            {
                GameObjectPoolManager.Instance.ReleaseObject(hp_line_key, hpLineRoot.GetChild(i).gameObject);
            }

            if (max <= _activity.eventFight.GridHealth) return;

            // 获取血条总宽度
            float totalWidth = hpProgressGreen.GetComponent<RectTransform>().rect.width;

            // 计算总格子数（四舍五入）
            int gridCount = Mathf.RoundToInt((float)max / _activity.eventFight.GridHealth);
            // 计算每个格子的实际宽度（将总宽度平均分配）
            float gridWidth = totalWidth / gridCount;

            // 生成竖线
            for (int i = 1; i < gridCount; i++)
            {
                var line = GameObjectPoolManager.Instance.CreateObject(hp_line_key, hpLineRoot);
                var rectTransform = line.GetComponent<RectTransform>();

                float xPos = gridWidth * i;
                rectTransform.anchoredPosition = new Vector2(xPos, 0);
                rectTransform.localScale = Vector3.one;
                line.SetActive(true);
            }
        }

        private (HPState state, string image) GetHPStateAndImage(float percentage)
        {
            if (percentage > _activity.eventFight.ColorHealth[1].Split(":")[0].ConvertToInt() * 1.0f / 100)
            {
                return (HPState.Green, _activity.eventFight.ColorHealth[2].Split(":")[1]);
            }
            if (percentage > _activity.eventFight.ColorHealth[0].Split(":")[0].ConvertToInt() * 1.0f / 100)
            {
                return (HPState.Yellow, _activity.eventFight.ColorHealth[1].Split(":")[1]);
            }
            return (HPState.Red, _activity.eventFight.ColorHealth[0].Split(":")[1]);
        }

        public void RefreshHP(int current, int max)
        {
            // 添加最小显示逻辑
            int displayCurrent = current;
            if (current > 0 && current < max * 0.05f) // 如果血量大于0但小于5%，保持最小显示
            {
                displayCurrent = Mathf.CeilToInt(max * 0.05f); // 确保血条至少显示5%
            }

            hpProgressGreen.Refresh(displayCurrent, max);
            hpProgressYellow.Refresh(displayCurrent, max);
            hpProgressRed.Refresh(displayCurrent, max);
            RefreshHPLines(current, max);
            RefreshHPState(current, max);
        }

        private void RefreshHPState(int current, int max)
        {
            var (state, _) = GetHPStateAndImage(current * 1.0f / max);
            _currentHPState = state;
            if (state == HPState.Red)
            {
                hpProgressGreen.gameObject.SetActive(false);
                hpProgressYellow.gameObject.SetActive(false);
                hpProgressRed.gameObject.SetActive(true);
            }
            else if (state == HPState.Yellow)
            {
                hpProgressGreen.gameObject.SetActive(false);
                hpProgressYellow.gameObject.SetActive(true);
                hpProgressRed.gameObject.SetActive(false);
            }
            else if (state == HPState.Green)
            {
                hpProgressGreen.gameObject.SetActive(true);
                hpProgressYellow.gameObject.SetActive(false);
                hpProgressRed.gameObject.SetActive(false);
            }
        }

        private void ShowCycleTalkTip(Action callback = null)
        {
            _activity.SetHasCycleHint(true);
            UIManager.Instance.OpenWindow(UIConfig.UIActivityFightMilestoneTips, callback);
        }
    }
}
