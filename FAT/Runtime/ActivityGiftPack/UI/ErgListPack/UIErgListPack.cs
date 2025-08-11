/*
 * @Author: tang.yan
 * @Description: 体力列表礼包界面 
 * @Date: 2025-04-14 10:04:28
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT
{
    public class UIErgListPack : UIBase
    {
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text subTitle;
        //未购买
        [SerializeField] private GameObject notBuyGo;
        [SerializeField] private Button buyBtn;
        [SerializeField] private UITextState iapPriceState;
        [SerializeField] private GameObject labelGo;
        //已购买
        [SerializeField] private GameObject hasBuyGo;
        [SerializeField] private TMP_Text hasBuyInfo;
        //scroll
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private GameObject cellPrefab;
        
        private Action WhenInit;
        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;
        private PackErgList pack;
        //性价比标签
        private UIIAPLabel _iapLabel = new();

        private List<UIErgListPackCell> _cellList = new List<UIErgListPackCell>(); 
        private Coroutine _coroutine;
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/Panel/BtnClose/Btn", base.Close);
            buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnClickBtnBuy);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.PACK_ERG_LIST_CELL, cellPrefab);
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackErgList)items[0];
        }

        protected override void OnPreOpen()
        {
            _RefreshCD();
            _RefreshPrice();
            _RefreshBaseInfo();
            _RefreshBuyInfo();
            _InitTaskScroll();
            pack?.OnOpenPackUI();
        }

        protected override void OnAddListener()
        {
            WhenInit ??= _RefreshPrice;
            WhenEnd ??= _RefreshEnd;
            WhenTick ??= _RefreshCD;
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            MessageCenter.Get<MSG.GAME_ERG_LIST_PACK_BUY_SUCC>().AddListener(_OnBuySuccess);
            MessageCenter.Get<MSG.GAME_ERG_LIST_PACK_CLAIM_SUCC>().AddListener(_OnClaimSuccess);
        }

        protected override void OnPreClose() { }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.GAME_ERG_LIST_PACK_BUY_SUCC>().RemoveListener(_OnBuySuccess);
            MessageCenter.Get<MSG.GAME_ERG_LIST_PACK_CLAIM_SUCC>().RemoveListener(_OnClaimSuccess);
        }

        protected override void OnPostClose()
        {
            _iapLabel.Clear();
            _ReleaseCellList();
            _StopCoroutine();
            //避免提前关节面导致协程没走完
            if (UIManager.Instance.IsBlocking())
                UIManager.Instance.Block(false);
            _isCellDirty = false;
            scrollRect.enabled = true;
        }
        
        private void _RefreshPrice()
        {
            if (pack == null)
                return;
            var iap = Game.Manager.iap;
            var valid = iap.Initialized;
            iapPriceState.Enabled(valid, iap.PriceInfo(pack.Content.IapId));
            if (valid)
            {
                buyBtn.interactable = true;
                GameUIUtility.SetDefaultShader(buyBtn.image);
            }
            else
            {
                buyBtn.interactable = false;
                GameUIUtility.SetGrayShader(buyBtn.image);
            }
        }

        private void _RefreshCD() {
            if (pack == null)
                return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, pack.endTS - t);
            UIUtility.CountDownFormat(timeText, diff);
        }

        private void _RefreshEnd(ActivityLike pack_, bool expire_) {
            if (pack_ != pack) return;
            Close();
        }

        private void _RefreshBaseInfo()
        {
            if (pack == null)
                return;
            subTitle.text = I18N.FormatText("#SysComDesc1027", pack.TotalEnergyNum);
        }

        private void _RefreshBuyInfo()
        {
            if (pack == null)
                return;
            var hasBuy = pack.HasBuy;
            notBuyGo.SetActive(!hasBuy);
            hasBuyGo.SetActive(hasBuy);
            if (hasBuy)
            {
                hasBuyInfo.text = pack.CheckIsAllTaskFinish() ? I18N.Text("#SysComDesc1031") : I18N.Text("#SysComDesc1030");
            }
            else
            {
                var labelId = pack.GetCurDetailConfig()?.Label ?? 0;
                _iapLabel.Setup(labelGo.transform, labelId, pack.PackId);
            }
        }

        private void _InitTaskScroll()
        {
            if (pack == null)
                return;
            _InitCellList();
            scrollRect.verticalNormalizedPosition = 1; //每次刷新默认置顶
        }

        private void _OnClickBtnBuy()
        {
            if (pack == null)
                return;
            pack.TryPurchasePack();
        }

        private void _OnBuySuccess()
        {
            _RefreshBuyInfo();
            //购买成功后播解锁特效
            Game.Manager.audioMan.TriggerSound("CommonUnlock");
        }
        
        private void _InitCellList()
        {
            _cellList.Clear();
            var taskList = pack.GetTaskDataList();
            foreach (var taskData in taskList)
            {
                var cell = _CreateCell(taskData);
                _cellList.Add(cell);
            }
        }

        private UIErgListPackCell _CreateCell(PackErgList.ErgTaskData taskData)
        {
            var obj = GameObjectPoolManager.Instance.CreateObject(PoolItemType.PACK_ERG_LIST_CELL, scrollContent);
            obj.SetActive(true);
            var cell = obj.GetComponent<UIErgListPackCell>();
            cell.SetData(taskData);
            return cell;
        }

        private void _OnClaimSuccess(PackErgList.ErgTaskData taskData)
        {
            _StopCoroutine();
            _coroutine = StartCoroutine(_TryRefresh(taskData));
        }

        private IEnumerator _TryRefresh(PackErgList.ErgTaskData taskData)
        {
            UIErgListPackCell findCell = null;
            foreach (var cell in _cellList)
            {
                if (cell.data != null && cell.data.Id == taskData.Id)
                {
                    cell.SetData(taskData);
                    //通知cell做表现
                    cell.OnClaimSuccess(taskData.Id);
                    findCell = cell;
                    break;
                }
            }
            if (findCell == null)
                yield break;
            //如果找到了目标cell 则做一些前置工作
            UIManager.Instance.Block(true); //锁用户操作
            _isCellDirty = true;    //每帧末强制刷新布局
            scrollRect.velocity = Vector2.zero;
            scrollRect.enabled = false; //失效scroll防止抖动
            //等一段时间 让cell自己做缩小表现
            yield return new WaitForSeconds(0.9f);
            //直接用移出之后再添加的方式达到将这个cell移到后面的目的
            _cellList.Remove(findCell);
            GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.PACK_ERG_LIST_CELL, findCell.gameObject);
            //等一帧
            yield return null;
            //刷新整个列表
            _RefreshCellList();
            UIManager.Instance.Block(false);
            _isCellDirty = false;
            scrollRect.enabled = true;
        }

        private bool _isCellDirty = false;
        private void LateUpdate()
        {
            if (_isCellDirty)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
            }
        }

        private void _RefreshCellList()
        {
            var taskList = pack.GetTaskDataList();
            for (int i = 0; i < taskList.Count; i++)
            {
                if (i < _cellList.Count)
                    _cellList[i].SetData(taskList[i]);
                else
                {
                    var cell = _CreateCell(taskList[i]);
                    _cellList.Add(cell);
                }
            }
        }

        private void _ReleaseCellList()
        {
            foreach (var item in _cellList)
            {
                item.Clear();
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.PACK_ERG_LIST_CELL, item.gameObject);
            }
            _cellList.Clear();
        }
        
        private void _StopCoroutine()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }
    }
}