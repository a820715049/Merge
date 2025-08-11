/*
 * @Author: tang.yan
 * @Description: 图鉴界面 
 * @Date: 2023-11-16 11:11:23
 */

using System.Collections;
using System.Collections.Generic;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT
{
    public class UIHandbook : UIBase, INavBack
    {
        [SerializeField] private TMP_Text gemNum;
        [SerializeField] private UIHandbookTabScroll tabScrollView;
        [SerializeField] private UIHandbookScrollView infoScrollView;
        [SerializeField] private ScrollRect infoScrollRect;
        private int _curSelectTabId = 1;
        //底部页签cell数据
        private List<HandbookTabCellData> _tabCellDataList = new List<HandbookTabCellData>();
        //图鉴组cell数据 key:链条组所属页签
        private Dictionary<int, List<HandbookGroupCellData>> _groupCellDataDict = new Dictionary<int, List<HandbookGroupCellData>>();
        //记录目前是否在滚动
        private bool _isScrolling = false;
        //等待一段时间后自动滚动
        private Coroutine _coPlayScroll;

        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnBtnCloseClick);
            transform.AddButton("Content/Root/Bg/BtnClose", OnBtnCloseClick).FixPivot();
            tabScrollView.InitLayout();
            infoScrollView.Setup();
            _InitUIData();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                int index = (int)items[0];
                _curSelectTabId = index > 0 ? index : 1;
            }
        }

        protected override void OnPreOpen()
        {
            _RefreshTabScroll();
            tabScrollView.JumpTo(_curSelectTabId);  //每次界面打开都定位到当前选中的页签
            _RefreshGroupScroll();
            _RefreshGem();
        }

        protected override void OnPostOpen()
        {
            _TryScrollToReward(true);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_HANDBOOK_REWARD>().AddListener(OnHandbookRewarded);
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().AddListener(_OnMessageCoinChange);
        }

        protected override void OnRefresh()
        {
            _RefreshTabScroll();
            _RefreshGroupScroll();
            _RefreshGem();
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_HANDBOOK_REWARD>().RemoveListener(OnHandbookRewarded);
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().RemoveListener(_OnMessageCoinChange);
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().Dispatch();
        }

        protected override void OnPostClose()
        {
            infoScrollView.Clear();
            StopScroll();
        }

        private void _InitUIData()
        {
            _tabCellDataList.Clear();
            _groupCellDataDict.Clear();
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var tabIdList))
            {
                var mgr = Game.Manager.mergeItemMan;
                var handbookMgr = Game.Manager.handbookMan;
                //填充页签数据
                mgr.FillCollectionCategoryOrdered(tabIdList);
                foreach (var tabId in tabIdList)
                {
                    HandbookTabCellData data = new HandbookTabCellData()
                    {
                        Index = tabId,
                        IsSelect = tabId == _curSelectTabId,
                        OnClickCb = _OnTabBtnClick,
                        HasDot = false,
                        ImageConfig = mgr.GetGalleryCategoryConfigById(tabId)?.Icon.ConvertToAssetConfig()
                    };
                    _tabCellDataList.Add(data);
                    //填充页签中的所有链条数据
                    using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var seriesIdList))
                    {
                        mgr.FillSeriesInCategoryOrdered(tabId, seriesIdList, false);
                        int index = 1;
                        List<HandbookGroupCellData> groupDataList = new List<HandbookGroupCellData>();
                        foreach (var id in seriesIdList)
                        {
                            int temp = index;
                            var config = mgr.GetCategoryConfig(id);
                            if (config != null)
                            {
                                HandbookGroupCellData groupData = new HandbookGroupCellData()
                                {
                                    Index = temp,
                                    SeriesId = id,
                                    ItemList = new List<int>(),
                                };
                                int unlockNum = 0;
                                foreach (var itemId in config.Progress)
                                {
                                    if (handbookMgr.IsItemUnlocked(itemId))
                                    {
                                        unlockNum++;
                                    }
                                    groupData.ItemList.Add(itemId);
                                }
                                groupData.UnlockNum = unlockNum;
                                groupDataList.Add(groupData);
                            }
                            index++;
                        }
                        _groupCellDataDict.Add(tabId, groupDataList);
                    }
                }
            }
        }
        
        private void _RefreshTabScroll()
        {
            var handbookMgr = Game.Manager.handbookMan;
            //更新页签的选中状态
            foreach (var data in _tabCellDataList)
            {
                data.IsSelect = data.Index == _curSelectTabId;
                data.HasDot = handbookMgr.GetNextRewardableSeriesId(data.Index) > 0;
            }
            tabScrollView.UpdateData(_tabCellDataList);
        }

        private void _RefreshGroupScroll(bool isClear = true, bool isScroll = true)
        {
            _RefreshGroupData();
            infoScrollRect.StopMovement();
            if (isClear)
            {
                infoScrollView.Clear();
                if (_groupCellDataDict.TryGetValue(_curSelectTabId, out var groupDataList))
                {
                    infoScrollView.BuildByGroupData(groupDataList);
                }
            }
            else
            {
                //领取图鉴奖励时 只刷新视图 并移动scroll至下一个可领奖的group
                infoScrollView.RefreshGroup();
                if (isScroll)
                {
                    _TryScrollToReward();
                }
            }
        }

        //将列表滑动到最近的可领奖的位置
        private void _TryScrollToReward(bool isFirst = false)
        {
            var sid = Game.Manager.handbookMan.GetNextRewardableSeriesId(_curSelectTabId);
            if (sid > 0)
            {
                _isScrolling = true;
                infoScrollView.ScrollToMatch(x => x.data.SeriesId == sid, () => { _isScrolling = false;});
            }
            else if (isFirst)
            {
                infoScrollView.JumpToMatch(x => x.data.SeriesId == sid, null);
            }
        }

        private void _RefreshGroupData()
        {
            var handbookMgr = Game.Manager.handbookMan;
            foreach (var groupDataList in _groupCellDataDict.Values)
            {
                foreach (var groupData in groupDataList)
                {
                    int unlockNum = 0;
                    foreach (var itemId in groupData.ItemList)
                    {
                        if (handbookMgr.IsItemUnlocked(itemId))
                        {
                            unlockNum++;
                        }
                    }
                    groupData.UnlockNum = unlockNum;
                }
            }
        }

        private void _RefreshGem()
        {
            var coin = Game.Manager.coinMan.GetDisplayCoin(CoinType.Gem);
            gemNum.text = coin.ToString();
        }
        
        private void OnHandbookRewarded(int itemId)
        {
            //检查领奖的棋子对应链条中还有没有其他可领奖的棋子 如果有的话就不scroll
            bool hasReward = Game.Manager.handbookMan.CheckHasRewardInChain(itemId);
            if (!hasReward)
            {
                StopScroll();
                _coPlayScroll = StartCoroutine(_CoPlayScroll());
            }
        }

        private IEnumerator _CoPlayScroll()
        {
            //等待一段时间后才滚动
            yield return new WaitForSeconds(0.3f);
            _RefreshTabScroll();
            _RefreshGroupScroll(false, true);
        }

        private void _OnMessageCoinChange(CoinType ct)
        {
            if (ct == CoinType.Gem)
            {
                _RefreshGem();
            }
        }

        private void _OnTabBtnClick(int index)
        {
            if (_curSelectTabId != index)
            {
                StopScroll();
                _curSelectTabId = index;
                _RefreshTabScroll();
                _RefreshGroupScroll();
                _TryScrollToReward(true);
            }
        }

        private void OnBtnCloseClick()
        {
            if (_isScrolling)
                return;
            Close();
        }

        public void OnNavBack()
        {
            OnBtnCloseClick();
        }

        private void StopScroll()
        {
            if (_coPlayScroll != null)
            {
                StopCoroutine(_coPlayScroll);
                _coPlayScroll = null;
            }
        }
    }
}