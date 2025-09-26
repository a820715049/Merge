/*
 * @Author: tang.yan
 * @Description: 来源与产出界面
 * @Date: 2023-11-23 20:11:02
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;
using fat.rawdata;

namespace FAT
{
    public class UIItemInfo : UIBase
    {
        [SerializeField] private RectTransform root;
        //顶部信息
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text itemLevel;
        [SerializeField] private UIImageRes itemIcon;
        [SerializeField] private GameObject itemFrozenGo;
        [SerializeField] private GameObject levelMaxIcon;
        [SerializeField] private GameObject boostEnergyIcon;
        [SerializeField] private Image lineImage;
        //中部信息
        [SerializeField] private GameObject bigInfoPanel;
        [SerializeField] private GameObject middleInfoPanel;
        [SerializeField] private GameObject smallInfoPanel;
        [SerializeField] private UIImageRes mSingleItemIcon;
        [SerializeField] private UIImageRes sSingleItemIcon;
        [SerializeField] private GameObject normalBg1;
        [SerializeField] private GameObject normalBg2;
        [SerializeField] private GameObject boostBg1;
        [SerializeField] private GameObject boostBg2;
        //底部信息
        [SerializeField] private GameObject smallBottomPanel;
        [SerializeField] private GameObject middleBottomPanel;
        [SerializeField] private GameObject bigBottomPanel;
        [SerializeField] private GameObject mProduceGo;
        [SerializeField] private UIImageRes mProduceIcon;
        [SerializeField] private Button mProduceBtn;
        [SerializeField] private GameObject mProduceTipsGo;
        [SerializeField] private GameObject mUseBonusGo;
        [SerializeField] private TMP_Text mUseBonusNum;
        [SerializeField] private UIImageRes mUseBonusIcon;
        [SerializeField] private GameObject mSkillItemGo;
        [SerializeField] private TMP_Text mSkillItemDesc;
        [SerializeField] private TMP_Text bTopInfoText;
        [SerializeField] private TMP_Text bBottomInfoText;
        [SerializeField] private GameObject boostEnergyGo;
        [SerializeField] private LayoutElement bInfoTopSpace;
        [SerializeField] private LayoutElement bInfoBottomSpace;
        //阶梯活动相关
        [SerializeField] private GameObject stepActInfoGo;
        [SerializeField] private TMP_Text stepActInfoText;
        [SerializeField] private GameObject stepActTextGo;
        [SerializeField] private TMP_Text stepActText;
        //特殊样式 - 阶梯活动 / 冰冻棋子
        [SerializeField] private GameObject stepActBg;
        [SerializeField] private GameObject stepActTime;
        [SerializeField] private TMP_Text stepActTimeText;
        [SerializeField] private TMP_Text bottomText;
        //面板scroll高度样式
        private UIItemInfoGroupScroll _threeGroupScroll;    //3行 scroll
        private UIImageState _threeSlider1;
        private UIImageState _threeSlider2;
        private UIItemInfoGroupScroll _fourGroupScroll;     //4行 scroll
        private UIImageState _fourSlider1;
        private UIImageState _fourSlider2;
        private UIItemInfoGroupScroll _fiveGroupScroll;     //5行 scroll
        private UIImageState _fiveSlider1;
        private UIImageState _fiveSlider2;
        private UIItemInfoGroupScroll _sixGroupScroll;      //6行及以上 scroll
        private UIImageState _sixSlider1;
        private UIImageState _sixSlider2;

        private List<List<int>> _chainGroupList = new List<List<int>>();
        private long _curStepActEndTime = -1;   //界面中缓存目前阶梯活动的结束时间
        private long _frozenItemLifeTime = -1;   //界面中缓存目前冰冻棋子的消失时间 单位毫秒

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/Root/BtnClose", base.Close).FixPivot();
            mProduceBtn.onClick.AddListener(OnProduceBtnClick);
            var path = "Content/Root/Panel/Info/Big/ScrollArea";
            //3行 scroll
            var path3 = path + "3";
            _threeGroupScroll = transform.FindEx<UIItemInfoGroupScroll>(path3);
            _threeSlider1 = transform.FindEx<UIImageState>(path3 + "/Scrollbar Vertical");
            _threeSlider2 = transform.FindEx<UIImageState>(path3 + "/Scrollbar Vertical/Sliding Area/Handle");
            _threeGroupScroll.InitLayout();
            _threeGroupScroll.SetScrollableMax(3);
            //4行 scroll
            var path4 = path + "4";
            _fourGroupScroll = transform.FindEx<UIItemInfoGroupScroll>(path4);
            _fourSlider1 = transform.FindEx<UIImageState>(path4 + "/Scrollbar Vertical");
            _fourSlider2 = transform.FindEx<UIImageState>(path4 + "/Scrollbar Vertical/Sliding Area/Handle");
            _fourGroupScroll.InitLayout();
            _fourGroupScroll.SetScrollableMax(4);
            //5行 scroll
            var path5 = path + "5";
            _fiveGroupScroll = transform.FindEx<UIItemInfoGroupScroll>(path5);
            _fiveSlider1 = transform.FindEx<UIImageState>(path5 + "/Scrollbar Vertical");
            _fiveSlider2 = transform.FindEx<UIImageState>(path5 + "/Scrollbar Vertical/Sliding Area/Handle");
            _fiveGroupScroll.InitLayout();
            _fiveGroupScroll.SetScrollableMax(5);
            //6行及以上 scroll
            var path6 = path + "6";
            _sixGroupScroll = transform.FindEx<UIItemInfoGroupScroll>(path6);
            _sixSlider1 = transform.FindEx<UIImageState>(path6 + "/Scrollbar Vertical");
            _sixSlider2 = transform.FindEx<UIImageState>(path6 + "/Scrollbar Vertical/Sliding Area/Handle");
            _sixGroupScroll.InitLayout();
            _sixGroupScroll.SetScrollableMax(5);
        }

        protected override void OnPreOpen()
        {
            _RefreshItemBaseInfo();
            _RefreshInfoPanel();
            MessageCenter.Get<MSG.GAME_ITEM_INFO_SHOW>().Dispatch();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondUpdate);
        }

        protected override void OnRefresh()
        {
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondUpdate);
        }

        protected override void OnPreClose()
        {
            _curStepActEndTime = -1;
            _frozenItemLifeTime = -1;
            UIManager.Instance.CloseWindow(UIConfig.UIItemInfoTips);
            MessageCenter.Get<MSG.GAME_ITEM_INFO_END>().Dispatch();
        }

        protected override void OnPostClose()
        {
        }

        private void _RefreshItemBaseInfo()
        {
            var itemInfoData = Game.Manager.itemInfoMan.CurShowItemData;
            if (itemInfoData == null || itemInfoData.ItemId <= 0)
                return;
            var cfg = Game.Manager.objectMan.GetBasicConfig(itemInfoData.ItemId);
            if (cfg == null)
                return;
            itemName.text = I18N.Text(cfg.Name);
            itemIcon.SetImage(cfg.Icon.ConvertToAssetConfig());
            itemFrozenGo.SetActive(itemInfoData.Type == ItemInfoMan.ItemInfoType.FrozenItem);
            itemLevel.text = I18N.FormatText("#SysComDesc18", itemInfoData.ItemLevel);
            levelMaxIcon.SetActive(itemInfoData.IsLevelMax);
            bool canShowBoost = itemInfoData.CanShowBoost;
            //刷新能量加倍图标
            boostEnergyIcon.SetActive(canShowBoost);
            //刷新点点颜色
            var config = FontMaterialRes.Instance.GetFontMatResConf(canShowBoost ? 6 : 5);
            if (config != null)
            {
                lineImage.color = config.color;
            }
            //刷新背景图
            normalBg1.SetActive(!canShowBoost);
            normalBg2.SetActive(!canShowBoost);
            boostBg1.SetActive(canShowBoost);
            boostBg2.SetActive(canShowBoost);
            //刷新标题文本颜色
            var config1 = FontMaterialRes.Instance.GetFontMatResConf(canShowBoost ? 10 : 9);
            config1?.ApplyFontMatResConfig(itemName);
            //刷新slider
            _threeSlider1.Enabled(!canShowBoost);
            _threeSlider2.Enabled(!canShowBoost);
            _fourSlider1.Enabled(!canShowBoost);
            _fourSlider2.Enabled(!canShowBoost);
            _fiveSlider1.Enabled(!canShowBoost);
            _fiveSlider2.Enabled(!canShowBoost);
            _sixSlider1.Enabled(!canShowBoost);
            _sixSlider2.Enabled(!canShowBoost);
        }

        private void _RefreshInfoPanel()
        {
            var itemInfoMan = Game.Manager.itemInfoMan;
            var itemInfoData = itemInfoMan.CurShowItemData;
            if (itemInfoData == null || itemInfoData.ItemId <= 0)
                return;
            _chainGroupList.Clear();
            //选择显示哪个面板 生成棋显示大面板 普通棋子显示中面板 链上只有一个棋子显示小面板
            itemInfoMan.FillItemCellDataGroupList(_chainGroupList, out int panelSize);
            //刷新中部信息
            _RefreshMiddlePanel(panelSize);
            //刷新底部信息
            _RefreshBottomPanel(panelSize);
            //刷新特殊样式通用UI
            _RefreshSpecialCommonUI();
            //刷新阶梯活动相关信息 需要在底部信息刷新完后再调用
            _RefreshStepActPanel();
            //刷新冰冻棋子相关信息 和阶梯活动互斥
            _RefreshFrozenItemPanel();
        }

        private void _RefreshMiddlePanel(int panelSize)
        {
            bigInfoPanel.SetActive(panelSize == 3 || panelSize == 2);
            middleInfoPanel.SetActive(panelSize == 1);
            smallInfoPanel.SetActive(panelSize == 0);
            if (panelSize == 3 || panelSize == 2)
            {
                var count = _chainGroupList.Count;
                _threeGroupScroll.gameObject.SetActive(count <= 3);
                _fourGroupScroll.gameObject.SetActive(count == 4);
                _fiveGroupScroll.gameObject.SetActive(count == 5);
                _sixGroupScroll.gameObject.SetActive(count >= 6);
                switch (count)
                {
                    case <= 3:
                        _threeGroupScroll.UpdateData(_chainGroupList);
                        _threeGroupScroll.JumpTo(0);
                        break;
                    case 4:
                        _fourGroupScroll.UpdateData(_chainGroupList);
                        _fourGroupScroll.JumpTo(0);
                        break;
                    case 5:
                        _fiveGroupScroll.UpdateData(_chainGroupList);
                        _fiveGroupScroll.JumpTo(0);
                        break;
                    default:
                        _sixGroupScroll.UpdateData(_chainGroupList);
                        _sixGroupScroll.JumpTo(0);
                        break;
                }
            }
            else
            {
                if (_chainGroupList.Count == 1 && _chainGroupList[0].Count == 1)
                {
                    int itemId = _chainGroupList[0][0];
                    var cfg = Game.Manager.objectMan.GetBasicConfig(itemId);
                    if (cfg != null)
                    {
                        if (panelSize == 1)
                        {
                            mSingleItemIcon.SetImage(cfg.Icon.ConvertToAssetConfig());
                        }
                        else if (panelSize == 0)
                        {
                            sSingleItemIcon.SetImage(cfg.Icon.ConvertToAssetConfig());
                        }
                    }
                }
            }
        }

        private void _RefreshBottomPanel(int panelSize)
        {
            if (panelSize == 0)
            {
                bigBottomPanel.SetActive(true);
                middleBottomPanel.SetActive(false);
                smallBottomPanel.SetActive(false);
                bTopInfoText.text = "";
                bBottomInfoText.text = "";
                return;
            }
            else
            {
                bigBottomPanel.SetActive(false);
            }
            var itemInfoMan = Game.Manager.itemInfoMan;
            var curShowItemData = itemInfoMan.CurShowItemData;
            //刷新能量加倍底部信息
            if (curShowItemData.CanShowBoost)
            {
                smallBottomPanel.SetActive(false);
                middleBottomPanel.SetActive(true);
                mProduceGo.SetActive(false);
                mUseBonusGo.SetActive(false);
                mSkillItemGo.SetActive(false);
                boostEnergyGo.SetActive(true);
                return;
            }
            else
            {
                boostEnergyGo.SetActive(false);
            }
            if (curShowItemData.OriginChainId > 0 && curShowItemData.DirectChainId > 0)
            {
                //根据OriginChainId获得当前合成链中已解锁的最高等级的棋子id
                var showItemId = Game.Manager.mergeItemMan.GetMaxUnlockLevelItemIdInChain(curShowItemData.OriginChainId);
                var cfg = Game.Manager.objectMan.GetBasicConfig(showItemId);
                if (cfg != null)
                {
                    smallBottomPanel.SetActive(false);
                    middleBottomPanel.SetActive(true);
                    mProduceGo.SetActive(true);
                    mUseBonusGo.SetActive(false);
                    mSkillItemGo.SetActive(false);
                    //获得showItemId在链中的等级
                    Game.Manager.mergeItemMan.GetItemCategoryIdAndLevel(showItemId, out _, out var level);
                    level = level + 1;  //获取到的等级从0开始 所以要+1
                    //判断当前该合成链条中已获得的最高等级棋子是否≥配置的等级 若不大于 则进行半透处理
                    Color color = Color.white;
                    color.a = level >= curShowItemData.OriginChainLevel ? 1f : 0.55f;
                    mProduceIcon.image.color = color;
                    //两种情况下会显示tips按钮 点击事件会区分对应逻辑
                    var isShowTips = (curShowItemData.OriginChainId != curShowItemData.DirectChainId)
                                     || level < curShowItemData.OriginChainLevel;
                    mProduceTipsGo.gameObject.SetActive(isShowTips);
                    mProduceBtn.interactable = isShowTips;
                    mProduceIcon.SetImage(cfg.Icon.ConvertToAssetConfig());
                }
                else
                {
                    smallBottomPanel.SetActive(true);
                    middleBottomPanel.SetActive(false);
                }
            }
            else
            {
                var compConfig = Game.Manager.mergeItemMan.GetItemComConfig(curShowItemData.ItemId);
                var bonusConfig = compConfig?.bonusConfig;
                if (bonusConfig != null)
                {
                    smallBottomPanel.SetActive(false);
                    middleBottomPanel.SetActive(true);
                    mProduceGo.SetActive(false);
                    mUseBonusGo.SetActive(true);
                    mSkillItemGo.SetActive(false);
                    //产出信息
                    var cfg = Game.Manager.objectMan.GetBasicConfig(bonusConfig.BonusId);
                    if (cfg != null)
                    {
                        mUseBonusIcon.SetImage(cfg.Icon.ConvertToAssetConfig());
                    }
                    mUseBonusNum.text = I18N.FormatText("#SysComDesc37", bonusConfig.BonusCount);
                    return;
                }
                //分割器、万能卡底部描述
                var skillConfig = compConfig?.skillConfig;
                if (skillConfig != null)
                {
                    smallBottomPanel.SetActive(false);
                    middleBottomPanel.SetActive(true);
                    mProduceGo.SetActive(false);
                    mUseBonusGo.SetActive(false);
                    mSkillItemGo.SetActive(true);
                    var desc = "";
                    if (skillConfig.Type == SkillType.Upgrade)
                        desc = I18N.Text("#SysComDesc378");
                    if (skillConfig.Type == SkillType.Degrade)
                        desc = I18N.Text("#SysComDesc379");
                    if (skillConfig.Type == SkillType.Lightbulb)
                        desc = I18N.FormatText("#SysComDesc1097", skillConfig.Param2[0]);
                    mSkillItemDesc.text = desc;
                    return;
                }
                //三选一盒子底部描述
                var choiceBoxConfig = compConfig?.choiceBoxConfig;
                if (choiceBoxConfig != null)
                {
                    smallBottomPanel.SetActive(false);
                    middleBottomPanel.SetActive(true);
                    mProduceGo.SetActive(false);
                    mUseBonusGo.SetActive(false);
                    mSkillItemGo.SetActive(true);
                    mSkillItemDesc.text = I18N.Text("#SysComDesc460");
                    return;
                }
                smallBottomPanel.SetActive(true);
                middleBottomPanel.SetActive(false);
            }
        }

        private void _RefreshSpecialCommonUI()
        {
            stepActBg.SetActive(false);
            stepActTime.SetActive(false);
            stepActTimeText.text = "";
            bottomText.gameObject.SetActive(false);
            bottomText.text = "";
        }

        //刷新阶梯活动相关信息 需要在底部信息刷新完后再调用
        //限时合成订单也用这个方法，在时间显示上略有不同
        private void _RefreshStepActPanel()
        {
            var itemInfoData = Game.Manager.itemInfoMan.CurShowItemData;
            var gallerySpecialMap = Game.Manager.configMan.GetGallerySpecialMap();
            var conf = gallerySpecialMap?.GetDefault(itemInfoData?.ItemId ?? 0);
            ActivityLike act = null;
            if (conf != null)
            {
                var activity = Game.Manager.activity;
                foreach (var param in conf.EventParam)
                {
                    if (activity.LookupAny(conf.EventType, param, out act))
                    {
                        break;
                    }
                }
            }
            //检测目前是否有正在开启的阶梯活动
            if (act != null)
            {
                //活动相关
                _curStepActEndTime = act.endTS;
                _RefreshStepActTime();
                var isShowProduce = middleBottomPanel.activeSelf && mProduceGo.activeSelf;
                var isShowActInfo = !string.IsNullOrEmpty(conf.TipOneKey);
                stepActInfoGo.SetActive(isShowActInfo);
                stepActInfoText.text = I18N.Text(conf.TipOneKey);
                //底部高度
                if (isShowProduce)
                {
                    if (isShowActInfo)
                    {
                        smallBottomPanel.SetActive(false);
                        middleBottomPanel.SetActive(true);
                        bigBottomPanel.SetActive(false);
                        bInfoTopSpace.minHeight = 16;
                        bInfoBottomSpace.minHeight = 16;
                    }
                    else
                    {
                        bInfoTopSpace.minHeight = 0;
                        bInfoBottomSpace.minHeight = 16;
                    }
                }
                else
                {
                    //不显示来源 但配置上需要显示提示时 特殊处理 显示middleBottomPanel
                    if (isShowActInfo)
                    {
                        mProduceGo.SetActive(false);
                        mUseBonusGo.SetActive(false);
                        mSkillItemGo.SetActive(false);
                        boostEnergyGo.SetActive(false);
                        smallBottomPanel.SetActive(false);
                        middleBottomPanel.SetActive(true);
                        bigBottomPanel.SetActive(false);
                        bInfoTopSpace.minHeight = 50;
                        bInfoBottomSpace.minHeight = 70;
                    }
                    else
                    {
                        bInfoTopSpace.minHeight = 0;
                        bInfoBottomSpace.minHeight = 16;
                    }
                }
                //底部文字
                var isShowActText = !string.IsNullOrEmpty(conf.TipTwoKey);
                stepActTextGo.SetActive(isShowActText);
                stepActText.text = I18N.Text(conf.TipTwoKey);
                stepActBg.SetActive(true);
                stepActTime.SetActive(true);
            }
            else
            {
                _curStepActEndTime = -1;
                bInfoTopSpace.minHeight = 0;
                bInfoBottomSpace.minHeight = 16;
                stepActInfoGo.SetActive(false);
                stepActTextGo.SetActive(false);
            }
        }

        private void _RefreshStepActTime()
        {
            if (_curStepActEndTime > 0)
            {
                var countdown = _curStepActEndTime - Game.Instance.GetTimestampSeconds();
                UIUtility.CountDownFormat(stepActTimeText, countdown);
                if (countdown <= 0)
                    Close();
            }
        }

        //刷新冰冻棋子相关信息 和阶梯活动互斥
        private void _RefreshFrozenItemPanel()
        {
            var itemInfoData = Game.Manager.itemInfoMan.CurShowItemData;
            if (itemInfoData.Type != ItemInfoMan.ItemInfoType.FrozenItem)
            {
                _frozenItemLifeTime = -1;
                return;
            }
            var rectTrans = bottomText.transform as RectTransform;
            if (rectTrans != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                var posX = rectTrans.anchoredPosition.x;
                rectTrans.anchoredPosition = new Vector2(posX, -root.rect.height);
            }
            bottomText.gameObject.SetActive(true);
            bottomText.text = I18N.Text("#SysComDesc1558");
            _frozenItemLifeTime = itemInfoData.RemainTime;
            stepActBg.SetActive(true);
            stepActTime.SetActive(true);
            _RefreshFrozenItemTime();
        }

        private void _RefreshFrozenItemTime()
        {
            if (_frozenItemLifeTime != -1)
            {
                UIUtility.CountDownFormat(stepActTimeText, _frozenItemLifeTime / 1000);
                if (_frozenItemLifeTime <= 0)
                    Close();
                _frozenItemLifeTime -= 1000;
            }
        }

        private void OnProduceBtnClick()
        {
            var curShowItemData = Game.Manager.itemInfoMan.CurShowItemData;
            if (curShowItemData.OriginChainId == curShowItemData.DirectChainId)
            {
                //这种情况下目前默认为：直接生成的棋子，其对应来源生成器合成链中当前已获得的最高等级棋子<配置的等级(OriginChainLevel)
                UIManager.Instance.OpenWindow(UIConfig.UIItemInfoTips, mProduceIcon.transform.position, 113f, 0, curShowItemData.OriginChainLevel);
            }
            else
            {
                curShowItemData.GenerateTipsDataList();
                var count = curShowItemData.TipsDataList.Count;
                //传入图标位置和偏移值 偏移值=cell宽度的一半 170/2 + 28
                if (count <= 4)
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIItemInfoTips, mProduceIcon.transform.position, 113f, curShowItemData.ItemId);
                }
                //显示数量超过4个时用更宽的面板
                else
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIItemInfoWideTips, mProduceIcon.transform.position, 113f, curShowItemData.ItemId);
                }
            }
        }

        private void _OnSecondUpdate()
        {
            _RefreshStepActTime();
            _RefreshFrozenItemTime();
        }
    }
}
