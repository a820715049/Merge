/*
 * @Author: qun.chao
 * @Date: 2021-02-23 12:01:31
 */
namespace FAT
{
    using UnityEngine;
    using UnityEngine.UI;
    using Merge;
    using fat.rawdata;
    using DG.Tweening;
    using TMPro;

    public class MBItemCharge : MonoBehaviour
    {
        [System.Serializable]
        class TimeWithLock
        {
            public GameObject goRoot;
            public GameObject goLock;
            public TextMeshProUGUI txtTime;
        }

        [SerializeField] private Transform tapCostRoot;
        [SerializeField] private GameObject progressBar;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject progressBarMini;
        [SerializeField] private Image progressFillMini;
        [SerializeField] private TimeWithLock fixedTimeLock;
        [SerializeField] private TimeWithLock countdownLock;
        [SerializeField] private Transform mixProgressRoot;

        public Transform tapCostComp => tapCostRoot;

        private MBItemView mView;
        private ItemClickSourceComponent clickSourceComp;
        private ItemMixSourceComponent mixSourceComp;
        private ItemAutoSourceComponent autoSourceComp;
        private ItemChestComponent chestComp;
        private ItemEatSourceComponent eaterComp;
        private ItemToolSourceComponent toolSourceComp;
        private ItemSkillComponent skillComp;
        private ItemJumpCDComponent jumpCDComp;
        private ItemBonusCompoent bonusComp;
        private ItemBubbleComponent bubbleFrozenComp;   //冰冻棋子
        private ItemTokenMultiComponent tokenMultiComp;

        private bool mIsCostEnergy;

        private bool mIsShowCd;

        private bool mIsShowOutput;
        private bool mIsShowEnergy;
        private bool mIsShowBoostEnergy;
        private bool mIsShowTapCost;
        private bool mIsShowLightbulb;

        private float mTapCostSwitchInterval = 2f;
        private float mTapCostSwitchTimer;
        private int id = -1;
        private int tid = -1;
        private Sequence mTapCostSwitchSeq;

        private int mMixProgress = -1;

        public void SetData(MBItemView view)
        {
            mView = view;
            clickSourceComp = mView.data.GetItemComponent<ItemClickSourceComponent>();
            mixSourceComp = mView.data.GetItemComponent<ItemMixSourceComponent>();
            autoSourceComp = mView.data.GetItemComponent<ItemAutoSourceComponent>();
            chestComp = mView.data.GetItemComponent<ItemChestComponent>();
            eaterComp = mView.data.GetItemComponent<ItemEatSourceComponent>();
            toolSourceComp = mView.data.GetItemComponent<ItemToolSourceComponent>();
            skillComp = mView.data.GetItemComponent<ItemSkillComponent>();
            jumpCDComp = mView.data.GetItemComponent<ItemJumpCDComponent>();
            bonusComp = mView.data.GetItemComponent<ItemBonusCompoent>();
            if (mView.data.TryGetItemComponent(out ItemBubbleComponent comp) && comp.IsFrozenItem())
                bubbleFrozenComp = comp;
            else
                bubbleFrozenComp = null;
            tokenMultiComp = mView.data.GetItemComponent<ItemTokenMultiComponent>();

            progressBar.SetActive(false);
            progressBarMini.SetActive(false);
            tapCostRoot.gameObject.SetActive(false);
            countdownLock.goRoot.SetActive(false);
            fixedTimeLock.goRoot.SetActive(false);
            mixProgressRoot.gameObject.SetActive(false);

            mIsCostEnergy = ItemUtility.GetItemEnergyPerUse(view.data) > 0;

            mIsShowCd = false;
            mIsShowOutput = false;
            mIsShowEnergy = false;
            mIsShowBoostEnergy = false;
            mIsShowTapCost = false;
            mIsShowLightbulb = false;
            mTapCostSwitchTimer = 0f;
            mMixProgress = -1;

            _RefreshCD(false);
            _RefreshBoxSource();
            _RefreshOrderBox();
            if (bonusComp != null)
                _TryRefreshBonus(true);
        }

        public void ClearData()
        {
            mView = null;
            mTapCostSwitchSeq?.Kill();
            mTapCostSwitchSeq = null;
            if (bonusComp != null)
                _TryRefreshBonus(false);
        }

        public void UpdateEx()
        {
            if (!mView.data.isActive)
                return;

            if (chestComp != null)
            {
                _RefreshChest();
            }
            else if (clickSourceComp != null)
            {
                _RefreshTapSource();
            }
            else if (mixSourceComp != null)
            {
                _RefreshMixSource();
            }
            else if (autoSourceComp != null)
            {
                _RefreshAutoSource();
            }
            else if (toolSourceComp != null)
            {
                _RefreshToolSource();
            }
            else if (eaterComp != null)
            {
                _RefreshEater();
            }
            else if (skillComp != null && skillComp.type == SkillType.Tesla)
            {
                _RefreshSkillCountdownMini();
            }
            else if (jumpCDComp != null)
            {
                _RefreshJumpCD();
            }
            else if (bubbleFrozenComp != null)
            {
                _RefreshFrozenItemTime();
            }
            else if (tokenMultiComp != null)
            {
                _RefreshTokenMulti();
            }
        }

        private float _CalcProgress(long totalMilli, long curMilli)
        {
            // 策划要求进度条一开始就有内容可以显示
            return Mathf.InverseLerp(totalMilli, 0f, curMilli) * 0.99f;
        }

        private void _RefreshChest()
        {
            bool showCd = false;
            float p = 0f;

            if (chestComp.isWaiting)
            {
                showCd = true;
                var cfg = chestComp.config;
                p = _CalcProgress(cfg.WaitTime * 1000, chestComp.openWaitMilli);
            }
            
            if (showCd)
            {
                progressFill.fillAmount = p;
            }

            if (showCd != mIsShowCd)
            {
                _RefreshCD(showCd);
            }

            // 不消耗能量的箱子 不显示产出效果 | 原版如此

            bool showOutput = false;
            bool showEnergy = false;
            if (mIsCostEnergy && !showCd)
            {
                // 显示 耗能&产出
                showOutput = true;
                showEnergy = true;
            }

            if (showOutput != mIsShowOutput)
            {
                _RefreshOutput(showOutput);
            }

            if (showEnergy != mIsShowEnergy)
            {
                _RefreshEnergy(showEnergy);
            }

            _TryRefreshChest(showCd);
        }

        private void _RefreshEater()
        {
            bool showOutput = false;
            bool showEnergy = false;
            bool showCd = false;

            if (eaterComp.state == ItemEatSourceComponent.Status.Eating)
            {
                showCd = true;
                progressFill.fillAmount = _CalcProgress(eaterComp.eatTotalMilli, eaterComp.eatMilli);
            }
            else if (eaterComp.state == ItemEatSourceComponent.Status.Output)
            {
                showOutput = true;
                if (eaterComp.config.EnergyCost > 0)
                {
                    showEnergy = true;
                }
            }

            if (showCd != mIsShowCd)
            {
                _RefreshCD(showCd);
            }
            if (showOutput != mIsShowOutput)
            {
                _RefreshOutput(showOutput);
            }
            if (showEnergy != mIsShowEnergy)
            {
                _RefreshEnergy(showEnergy);
            }
        }

        private void _RefreshTapSource()
        {
            int energyCost;
            bool showCd = false;
            bool showOutput = false;
            bool showEnergy = false;
            bool showBoostEnergy = false;
            bool showTapCost = false;
            bool showLightbulb = false;
            float p = 0f;

            energyCost = clickSourceComp.energyCost;
            bool isCostEnergy = energyCost > 0;

            if (clickSourceComp.itemCount > 0)
            {
                showLightbulb = clickSourceComp.isBoostItem;
                bool canBoost = Env.Instance.IsInEnergyBoost() && clickSourceComp.config.IsBoostable;
                showEnergy = !canBoost && isCostEnergy && !showLightbulb;
                showBoostEnergy = canBoost && isCostEnergy && !showLightbulb;
                _TryRefreshTapSource(false);
            }
            else
            {
                _TryRefreshTapSource(true);
                var cfg = clickSourceComp.config;
                if (clickSourceComp.isOutputing)
                {
                    showCd = true;
                    p = _CalcProgress(cfg.OutputTime * 1000, clickSourceComp.outputMilli);
                }
                else if (clickSourceComp.reviveTotalMilli > 0)
                {
                    showCd = true;
                    p = _CalcProgress(clickSourceComp.reviveTotalMilli, clickSourceComp.reviveMilli);
                }
            }

            if (!showCd && energyCost <= 0)
            {
                showTapCost = true;
                if (clickSourceComp.config.CostId.Count > 0)
                {
                    _TryShowNextTapCost();
                }
            }

            if (showTapCost)
            {
                showOutput = Env.Instance.FindPossibleCost(clickSourceComp.config.CostId) != null;
            }
            else if (showEnergy)
            {
                showOutput = Env.Instance.CanUseEnergy(energyCost);
            }
            else if (showBoostEnergy)
            {
                showOutput = Env.Instance.CanUseEnergy(energyCost);
            }
            else if (showLightbulb)
            {
                showOutput = Env.Instance.CanUseEnergy(energyCost);
            }

            if (showCd)
            {
                progressFill.fillAmount = p;
            }

            if (showCd != mIsShowCd)
            {
                _RefreshCD(showCd);
            }

            if (showTapCost != mIsShowTapCost)
            {
                _RefreshTapCost(showTapCost);
            }

            if (showEnergy != mIsShowEnergy)
            {
                _RefreshEnergy(showEnergy);
            }

            if (showBoostEnergy != mIsShowBoostEnergy)
            {
                _RefreshBoostEnergy(showBoostEnergy);
            }

            if (showLightbulb != mIsShowLightbulb)
            {
                _RefreshLightbulb(showLightbulb);
            }

            if (showOutput != mIsShowOutput)
            {
                _RefreshOutput(showOutput);
            }
        }

        private void _RefreshMixSource()
        {
            var showCd = false;
            var showOutput = false;
            var p = 0f;
            var com = mixSourceComp;
            var mixedCount = com.mixedCount;

            if (mView.currentStateType == ItemLifecycle.MixOutput)
            {
                // 刚产出完毕 暂时认为进度条满
                mixedCount = com.totalItemCount;
            }

            if (com.itemCount <= 0)
            {
                if (com.isOutputing)
                {
                    showCd = true;
                    p = _CalcProgress(com.config.OutputTime * 1000, com.outputMilli);
                }
                else if (com.reviveTotalMilli > 0)
                {
                    showCd = true;
                    p = _CalcProgress(com.reviveTotalMilli, com.reviveMilli);
                }
            }

            if (showCd)
            {
                progressFill.fillAmount = p;
            }
            if (showCd != mIsShowCd)
            {
                _RefreshCD(showCd);
            }
            if (showOutput != mIsShowOutput)
            {
                _RefreshOutput(showOutput);
            }
            if (mixedCount != mMixProgress)
            {
                // cd期间不显示进度
                _RefreshMixProgressShow(!showCd);
                _RefreshMixProgress(mixedCount, com.totalMixRequire);
            }
        }

        private void _RefreshAutoSource()
        {
            bool showCd = false;
            bool showOutput = false;
            float p = 0f;

            if (autoSourceComp.itemCount > 0)
            {
                showOutput = true;
            }
            else
            {
                showCd = true;
                p = _CalcProgress(autoSourceComp.outputWholeMilli, autoSourceComp.outputMilli);
            }

            if (showCd)
            {
                progressFill.fillAmount = p;
            }

            if (showCd != mIsShowCd)
            {
                _RefreshCD(showCd);
            }

            if (showOutput != mIsShowOutput)
            {
                _RefreshOutput(showOutput);
            }

            _TryRefreshAutoSource(showCd);
        }

        private void _RefreshToolSource()
        {
            bool showOutput = true;
            if (showOutput != mIsShowOutput)
            {
                _RefreshOutput(showOutput);
            }
        }

        private void _RefreshSkillCountdownMini()
        {
            if (!skillComp.teslaActive)
                return;
            progressFillMini.fillAmount = _CalcProgress(skillComp.teslaTotalLife, skillComp.teslaTotalLife - skillComp.teslaLeftMilli);
            progressBarMini.SetActive(true);
        }

        private void _RefreshCD(bool b)
        {
            mIsShowCd = b;
            progressBar.SetActive(b);
        }

        private void _RefreshOutput(bool b)
        {
            mIsShowOutput = b;
            if (b)
                mView.AddHintForReadyToUse();
            else
                mView.RemoveHintForReadyToUse();
        }

        private void _RefreshEnergy(bool b)
        {
            mIsShowEnergy = b;
            if (b)
                mView.AddHintForConsumeEnergy();
            else
                mView.RemoveHintForConsumeEnergy();
        }

        private void _RefreshLightbulb(bool b)
        {
            mIsShowLightbulb = b;
            if (b)
                mView.AddHintForLightbulb();
            else
                mView.RemoveHintForLightbulb();
        }
        private void _RefreshBoostEnergy(bool b)
        {
            mIsShowBoostEnergy = b;
            if (b)
                mView.AddHintForConsumeBoostEnergy();
            else
                mView.RemoveHintForConsumeBoostEnergy();
        }

        private void _RefreshBoxSource()
        {
            if (mView.data.HasComponent(ItemComponentType.Box))
            {
                // 这种物品没有CD逻辑 直接显示产出和能量
                if (mIsCostEnergy)
                {
                    _RefreshOutput(true);
                    _RefreshEnergy(true);
                }
            }
        }

        private void _RefreshOrderBox()
        {
            if (mView.data.TryGetItemComponent(out ItemOrderBoxComponent com))
            {
                UIUtility.CountDownFormat(fixedTimeLock.txtTime, com.config.Time / 1000, UIUtility.CdStyle.OmitZero);
                fixedTimeLock.goRoot.SetActive(true);
                countdownLock.goRoot.SetActive(false);
            }
        }

        private void _RefreshJumpCD()
        {
            var com = jumpCDComp;
            if (com.isCounting)
            {
                // 倒计时展示
                UIUtility.CountDownFormat(countdownLock.txtTime, com.countdown / 1000);
                countdownLock.goLock.SetActive(false);
                countdownLock.goRoot.SetActive(true);
                fixedTimeLock.goRoot.SetActive(false);
            }
            else
            {
                // 常规展示
                if (!fixedTimeLock.goRoot.activeSelf)
                {
                    UIUtility.CountDownFormat(fixedTimeLock.txtTime, com.config.Time / 1000, UIUtility.CdStyle.OmitZero);
                    fixedTimeLock.goRoot.SetActive(true);
                    countdownLock.goRoot.SetActive(false);
                }
            }
        }
        
        private void _RefreshTokenMulti()
        {
            var com = tokenMultiComp;
            if (com.isCounting)
            {
                // 倒计时展示
                UIUtility.CountDownFormat(countdownLock.txtTime, com.countdown / 1000);
                countdownLock.goLock.SetActive(false);
                countdownLock.goRoot.SetActive(true);
                fixedTimeLock.goRoot.SetActive(false);
            }
            else
            {
                // 常规展示
                if (!fixedTimeLock.goRoot.activeSelf)
                {
                    UIUtility.CountDownFormat(fixedTimeLock.txtTime, com.config.Time / 1000, UIUtility.CdStyle.OmitZero);
                    fixedTimeLock.goRoot.SetActive(true);
                    countdownLock.goRoot.SetActive(false);
                }
            }
        }

        private void _RefreshTapCost(bool b)
        {
            mIsShowTapCost = b;
            tapCostRoot.gameObject.SetActive(b);
            if (b)
            {
                // 顺序显示最多childCount个可能的cost
                var costs = clickSourceComp.config.CostId;
                for (int i = 0; i < tapCostRoot.childCount; i++)
                {
                    var holder = tapCostRoot.GetChild(i);
                    if (i < costs.Count)
                    {
                        var costCfg = Env.Instance.GetMergeTapCostConfig(costs[i]);
                        var cfg = Env.Instance.GetItemConfig(costCfg?.Cost ?? 0);
                        if (cfg != null)
                        {
                            holder.GetComponent<UIImageRes>().SetImage(cfg.Image);
                            holder.gameObject.SetActive(true);
                        }
                        else
                        {
                            holder.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        holder.gameObject.SetActive(false);
                    }
                }
            }
        }

        // 多个tap消耗需要渐变切换
        private void _TryShowNextTapCost()
        {
            if (clickSourceComp?.config.CostId.Count <= 1)
                return;
            mTapCostSwitchTimer -= Time.deltaTime;
            if (mTapCostSwitchTimer < 0f)
            {
                mTapCostSwitchTimer = mTapCostSwitchInterval;
                var total = clickSourceComp.config.CostId.Count;
                if (total > 1)
                {
                    var curIdx = 0;
                    for (int i = 0; i < tapCostRoot.childCount; i++)
                    {
                        // 以透明度为依据判断当前显示的是几号元素
                        if (tapCostRoot.GetChild(i).GetComponent<Image>().color.a > 0.5f)
                        {
                            curIdx = i;
                            break;
                        }
                    }
                    var nextIdx = (curIdx + 1) % total;
                    var transCur = tapCostRoot.GetChild(curIdx);
                    var transNext = tapCostRoot.GetChild(nextIdx);

                    var seq = DOTween.Sequence();
                    seq.Append(transCur.GetComponent<Image>().DOFade(0f, 0.5f).From(1f, true));
                    seq.Join(transNext.GetComponent<Image>().DOFade(1f, 0.5f).From(0f, true));
                    seq.Play();
                    mTapCostSwitchSeq = seq;
                }
            }
        }

        private void _TryRefreshBonus(bool show)
        {
            if (show)
            {
                id = bonusComp.item.id;
                tid = bonusComp.item.tid;
                BoardViewManager.Instance.RegisterBonusCache(id, tid);
            }
            else
            {
                id = -1;
                tid = -1;
                BoardViewManager.Instance.UnregisterBonusCache(id);
            }
  
        }

        private void _TryRefreshChest(bool cd)
        {
            if (cd)
                BoardViewManager.Instance.UnregisterBonusCache(chestComp.item.id);
            else
                BoardViewManager.Instance.RegisterChestCache(chestComp.item.id, chestComp.item.tid);
        }

        private void _TryRefreshAutoSource(bool cd)
        {
            if (cd)
                BoardViewManager.Instance.UnregisterAutoSourceCache(autoSourceComp.item.id);
            else
                BoardViewManager.Instance.RegisterAutoSourceCache(autoSourceComp.item.id, autoSourceComp.item.tid);
        }

        private void _TryRefreshTapSource(bool cd)
        {
            if (cd)
                BoardViewManager.Instance.UnregisterTapSourceCache(clickSourceComp.item.id);
            else
                BoardViewManager.Instance.RegisterTapSourceCache(clickSourceComp.item.id, clickSourceComp.item.tid);
        }

        private void _RefreshMixProgressShow(bool show)
        {
            var root = mixProgressRoot;
            if (root.gameObject.activeSelf != show)
            {
                root.gameObject.SetActive(show);
            }
        }

        private void _RefreshMixProgress(int p, int total)
        {
            var root = mixProgressRoot;
            for (var i = 0; i < root.childCount; i++)
            {
                var item = root.GetChild(i);
                if (i < total)
                {
                    item.gameObject.SetActive(true);
                    item.GetChild(0).gameObject.SetActive(i < p);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
        }
        
        private void _RefreshFrozenItemTime()
        {
            var com = bubbleFrozenComp;
            // 倒计时展示
            UIUtility.CountDownFormat(countdownLock.txtTime, com.LifeRemainTime / 1000);
            countdownLock.goLock.SetActive(false);
            countdownLock.goRoot.SetActive(true);
            fixedTimeLock.goRoot.SetActive(false);
        }
    }
}