using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using EL.Resource;
using FAT.Merge;
using fat.rawdata;
using UnityEngine;

namespace FAT
{
    public class AutoGuide : IGameModule, ISecondUpdate
    {
        private class Runner : MonoBehaviour
        {
            public Action Handler;
            public bool NeedUpdate;
            public Action Interrupt;
            public bool NeedInterrupt;

            private void LateUpdate()
            {
                if (NeedUpdate)
                    Handler?.Invoke();
                if (NeedInterrupt)
                    Interrupt?.Invoke();
            }
        }

        private readonly List<(AutoFinger type, Func<Transform> handler)> _fingerPosResolverList = new();
        private Runner _triggerRunner;
        private GameObject _finger;
        private Transform _target;
        private bool _isShowing;
        private float _maxInterval;
        private float _curInterval;
        private bool _needGuide;
        private readonly List<AutoFingerInfo> _list = new();
        private bool _remindMerge;
        private bool _itemInfo;
        private bool _cardPackOpen;
        private bool _randomBox;
        private int _curFinger = -1;
        private int _uiInterval;
        private Sequence _sequence;
        private Tweener _tweener;
        private bool _firstShowSale = false;
        private bool _hasRefreshItem;
        private readonly List<(int, int)> _bonus = new();
        private readonly List<(int, int)> _chest = new();
        private readonly List<(int, int)> _auto = new();
        private readonly List<(int, int)> _tap = new();
        private readonly Dictionary<int, List<Item>> _cat = new();
        private readonly List<(int, int)> _weightList = new();
        private readonly List<int> _catNeed = new();
        private readonly Dictionary<int, int> _weight = new();
        private readonly Dictionary<int, int> _require = new();
        private readonly Dictionary<int, int> _order = new();

        #region 事件注册和注销

        public void RegisterMessage()
        {
            MessageCenter.Get<MSG.GAME_BOARD_TOUCH>().AddListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.ORDER_FINISH>().AddListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.GUIDE_OPEN>().AddListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.UI_ABOVE_STATUE_HAS_CHILD>().AddListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.UI_ABOVE_STATUE_NO_CHILD>().AddListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.UI_MERGE_BOARD_MAIN_OPEN>().AddListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.UI_MERGE_BOARD_MAIN_CLOSE>().AddListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.GAME_REMIND_MERGE_END>().AddListener(EndRemind);
            MessageCenter.Get<MSG.GAME_REMIND_MERGE_START>().AddListener(StartRemind);
            MessageCenter.Get<MSG.GAME_CLAIM_REWARD>().AddListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.GAME_ITEM_INFO_SHOW>().AddListener(StartInfo);
            MessageCenter.Get<MSG.GAME_ITEM_INFO_END>().AddListener(EndInfo);
            MessageCenter.Get<MSG.GAME_CARD_PACK_OPEN>().AddListener(StartCardPackOpen);
            MessageCenter.Get<MSG.GAME_CARD_PACK_OPEN_END>().AddListener(EndCardPackOpen);
        }

        public void UnRegisterMessage()
        {
            MessageCenter.Get<MSG.GAME_BOARD_TOUCH>().RemoveListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.ORDER_FINISH>().RemoveListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.GUIDE_OPEN>().RemoveListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.UI_ABOVE_STATUE_HAS_CHILD>().RemoveListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.UI_ABOVE_STATUE_NO_CHILD>().RemoveListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.UI_MERGE_BOARD_MAIN_OPEN>().RemoveListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.UI_MERGE_BOARD_MAIN_CLOSE>().RemoveListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.GAME_REMIND_MERGE_END>().RemoveListener(EndRemind);
            MessageCenter.Get<MSG.GAME_REMIND_MERGE_START>().RemoveListener(StartRemind);
            MessageCenter.Get<MSG.GAME_CLAIM_REWARD>().RemoveListener(_TryInterruptGuide);
            MessageCenter.Get<MSG.GAME_ITEM_INFO_SHOW>().RemoveListener(StartInfo);
            MessageCenter.Get<MSG.GAME_ITEM_INFO_END>().RemoveListener(EndInfo);
            MessageCenter.Get<MSG.GAME_CARD_PACK_OPEN>().RemoveListener(StartCardPackOpen);
            MessageCenter.Get<MSG.GAME_CARD_PACK_OPEN_END>().RemoveListener(EndCardPackOpen);
        }

        #endregion

        #region 引导位置注册和注销

        public void RegisterAutoFingerPos(AutoFinger autoType, Func<Transform> func)
        {
            var idx = _fingerPosResolverList.FindIndex(x => x.type == autoType && x.handler == func);
            if (idx < 0) _fingerPosResolverList.Add((autoType, func));
        }

        public void UnRegisterAutoFingerPos(AutoFinger autoType, Func<Transform> func)
        {
            var idx = _fingerPosResolverList.FindIndex(x => x.type == autoType && x.handler == func);
            if (idx >= 0) _fingerPosResolverList.RemoveAt(idx);
        }

        #endregion

        public void LoadConfig()
        {
            foreach (var kv in fat.conf.Data.GetAutoFingerInfoSlice()) _list.Add(kv);

            _maxInterval = Game.Manager.configMan.globalConfig.AutoFingerTriggerTime;
            _list.Sort((a, b) => a.Priority - b.Priority);
            _needGuide = Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureAutoFinger);
        }

        public void Reset()
        {
            UnRegisterMessage();
        }

        public void Startup()
        {
            if (!_needGuide)
                return;

            _fingerPosResolverList.Clear();
            if (_triggerRunner == null)
            {
                var go = new GameObject("AutoGuideTrigger", typeof(RectTransform));
                go.transform.localPosition = Vector3.zero;
                _triggerRunner = go.AddComponent<Runner>();
                _triggerRunner.Interrupt += _InterruptGuide;
            }

            //加载弱引导使用的资源
            if (!GameObjectPoolManager.Instance.HasPool($"{PoolItemType.Auto_Guide_Finger}"))
                Game.Instance.StartCoroutineGlobal(_LoadFinger());
            RegisterMessage();
        }

        #region 资源加载

        private IEnumerator _LoadFinger()
        {
            var req = ResManager.LoadAsset("fat_global", "AutoGuideFinger.prefab");
            yield return req;
            if (req.isSuccess && req.asset != null)
            {
                var go = UnityEngine.Object.Instantiate(req.asset,
                    UIManager.Instance.GetLayerRootByType(UILayer.Effect)) as GameObject;
                if (go != null)
                {
                    GameObjectPoolManager.Instance.PreparePool(PoolItemType.Auto_Guide_Finger, go);
                    GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.Auto_Guide_Finger, go);
                }
                else
                {
                    Debug.Log("AutoGuideFinger Instantiate Fail");
                }
            }
            else
            {
                Debug.Log("Load AutoGuideFinger Fail");
            }
        }

        #endregion

        public void SecondUpdate(float dt)
        {
            if (_needGuide)
                _CheckAutoGuide();
        }

        private void _CheckAutoGuide()
        {
            if (_itemInfo || _cardPackOpen || Game.Manager.specialRewardMan.IsBusy()) return;

            if (_curInterval >= _maxInterval / 1000)
            {
                if (_CheckUIState())
                    _TryStartGuide();
                else
                    _InterruptGuide();
            }
            else
            {
                if (!_isShowing)
                {
                    _curInterval++;
                    _uiInterval++;
                }
            }
        }

        private void StartRemind()
        {
            _remindMerge = true;
            if (_curFinger == -1)
                return;
            if (!fat.conf.Data.GetAutoFingerInfoByIndex(_curFinger).IsHideMerge)
                return;
            _TryInterruptGuide();
        }

        private void EndRemind()
        {
            _remindMerge = false;
        }

        private void StartInfo()
        {
            _itemInfo = true;
            _TryInterruptGuide();
        }

        private void EndInfo()
        {
            _itemInfo = false;
        }

        private void StartCardPackOpen()
        {
            _cardPackOpen = true;
        }

        private void EndCardPackOpen()
        {
            _cardPackOpen = false;
        }

        private bool _CheckUIState()
        {
            if (Game.Manager.mergeBoardMan.activeWorld == null ||
                Game.Manager.mergeBoardMan.activeWorld.activeBoard == null)
                return false;
            if (Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId != Constant.MainBoardId)
                return false;
            if (!UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain))
                return false;
            if (Game.Manager.screenPopup.list.Count != 0)
                return false;
            if (UIManager.Instance.GetLayerRootByType(UILayer.AboveStatus).childCount >= 1)
                return false;
            if (UIManager.Instance.IsOpen(UIConfig.UIGuide))
                return false;
            return true;
        }

        private void _TryStartGuide()
        {
            if (_isShowing)
                return;
            //检测开启哪一项引导
            _uiInterval++;
            var start = false;
            _hasRefreshItem = false;
            foreach (var kv in _list)
                if (CheckRequire(kv))
                {
                    start = true;
                    break;
                }

            if (start) _isShowing = true;
        }

        #region RequireCheck

        private bool CheckRequire(AutoFingerInfo info)
        {
            switch (info.AutoFinger)
            {
                case AutoFinger.Meta: return CheckMeta(info);
                case AutoFinger.Order: return CheckOrder(info);
                case AutoFinger.GiftBox: return CheckReward(info);
                case AutoFinger.MergeBonus: return CheckMergeBonus(info);
                case AutoFinger.MergeChest: return CheckMergeChest(info);
                case AutoFinger.AutoSource: return CheckAutoSource(info);
                case AutoFinger.TapSource: return CheckTapSource(info);
                case AutoFinger.Bag: return CheckAutoBag(info);
                case AutoFinger.SaleUi: return CheckSaleUI(info);
                case AutoFinger.Sale: return CheckSaleItem(info);
            }

            return false;
        }

        private bool CheckLevel(AutoFingerInfo info)
        {
            var level = Game.Manager.mergeLevelMan.level;
            return level >= info.ActiveLv && level < info.ShutdownLv;
        }

        private bool CheckMeta(AutoFingerInfo info)
        {
            if (!CheckLevel(info))
                return false;
            if (info.IsHideMerge && _remindMerge)
                return false;
            var next = Game.Manager.mapSceneMan.NextBuildingToFocus();
            var ready = next != null && next.UpgradeCostReady;
            if (ready)
            {
                ShowFinger(info.AutoFinger);
                DataTracker.auto_finger.Track(info.AutoFinger,
                    Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, 0);
            }

            return ready;
        }

        private bool CheckOrder(AutoFingerInfo info)
        {
            if (!CheckLevel(info))
                return false;
            if (info.IsHideMerge && _remindMerge)
                return false;
            var idx = _fingerPosResolverList.FindLastIndex(x => x.type == info.AutoFinger);
            if (idx < 0)
                return false;

            var list = _fingerPosResolverList.FindAll(x => x.type == info.AutoFinger);
            list.Sort((a, b) =>
                a.handler.Invoke().parent.GetSiblingIndex() -
                b.handler.Invoke().parent.GetSiblingIndex()); //找最左边对那个订单
            ShowFinger(list.First().handler.Invoke());
            DataTracker.auto_finger.Track(info.AutoFinger,
                Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, 0);
            return true;
        }

        private bool CheckReward(AutoFingerInfo info)
        {
            if (!CheckLevel(info))
                return false;
            if (info.IsHideMerge && _remindMerge)
                return false;
            var idx = _fingerPosResolverList.FindLastIndex(x => x.type == info.AutoFinger);
            if (idx < 0)
                return false;
            ShowFinger(info.AutoFinger);
            DataTracker.auto_finger.Track(info.AutoFinger,
                Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, 0);
            return true;
        }

        private bool CheckMergeBonus(AutoFingerInfo info)
        {
            if (!CheckLevel(info))
                return false;
            if (info.IsHideMerge && _remindMerge)
                return false;
            _bonus.Clear();
            foreach (var kv in BoardViewManager.Instance.ActiveBonusCache) _bonus.Add((kv.Key, kv.Value));
            _bonus.Sort((a, b) => a.Item2 - b.Item2);
            foreach (var kv in _bonus)
                if (Game.Manager.mergeItemMan.IsLastItemInChain(kv.Item2))
                {
                    if (kv.Item2 == 12000005 && Game.Manager.mergeEnergyMan.Energy > 20)
                        continue;
                    ShowFinger(BoardViewManager.Instance.GetItemView(kv.Item1).transform.position);
                    DataTracker.auto_finger.Track(info.AutoFinger,
                        Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, kv.Item2);
                    return true;
                }

            return false;
        }

        private bool CheckMergeChest(AutoFingerInfo info)
        {
            if (!CheckLevel(info))
                return false;
            if (info.IsHideMerge && _remindMerge)
                return false;
            _chest.Clear();
            foreach (var kv in BoardViewManager.Instance.ActiveChestCache) _chest.Add((kv.Key, kv.Value));
            _chest.Sort((a, b) => a.Item2 - b.Item2);
            foreach (var kv in _chest)
                if (Game.Manager.mergeItemMan.IsLastItemInChain(kv.Item2))
                {
                    ShowFinger(BoardViewManager.Instance.GetItemView(kv.Item1).transform.position);
                    DataTracker.auto_finger.Track(info.AutoFinger,
                        Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, kv.Item2);
                    return true;
                }

            return false;
        }

        private bool CheckAutoSource(AutoFingerInfo info)
        {
            if (!CheckLevel(info))
                return false;
            if (info.IsHideMerge && _remindMerge)
                return false;
            if (Game.Manager.mainMergeMan.world.activeBoard.emptyGridCount == 0)
                return false;
            _auto.Clear();
            foreach (var kv in BoardViewManager.Instance.ActiveAutoSourceCache) _auto.Add((kv.Key, kv.Value));
            _auto.Sort((a, b) => a.Item2 - b.Item2);
            foreach (var kv in _auto)
                if (Game.Manager.mergeItemMan.IsLastItemInChain(kv.Item2))
                {
                    ShowFinger(BoardViewManager.Instance.GetItemView(kv.Item1).transform.position);
                    DataTracker.auto_finger.Track(info.AutoFinger,
                        Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, kv.Item2);
                    return true;
                }

            return false;
        }

        private bool CheckTapSource(AutoFingerInfo info)
        {
            if (!CheckLevel(info))
                return false;
            if (info.IsHideMerge && _remindMerge)
                return false;
            if (Game.Manager.mainMergeMan.world.activeBoard.emptyGridCount == 0)
                return false;
            _tap.Clear();
            foreach (var kv in BoardViewManager.Instance.ActiveTapSourceCache) _tap.Add((kv.Key, kv.Value));
            switch (_tap.Count())
            {
                case 0: return false;
                case 1:
                {
                    ShowFinger(BoardViewManager.Instance.GetItemView(_tap[0].Item1).transform);
                    return true;
                }
                default:
                {
                    //统计链条并合并
                    _cat.Clear();
                    foreach (var kv in _tap)
                    {
                        var c = Game.Manager.mergeItemMan.GetItemCategoryId(kv.Item2);
                        if (c != 0)
                        {
                            if (!_cat.ContainsKey(c))
                            {
                                _cat.Add(c, new List<Item>());
                                _cat[c].Add(Game.Manager.mergeBoardMan.activeWorld.GetItem(kv.Item1));
                            }
                            else
                            {
                                _cat[c].Add(Game.Manager.mergeBoardMan.activeWorld.GetItem(kv.Item1));
                            }
                        }
                    }

                    //<id,count> 统计订单需求
                    _order.Clear();
                    using (ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var allOrderDataList))
                    {
                        BoardViewWrapper.FillBoardOrder(allOrderDataList);
                        foreach (var orderData in allOrderDataList)
                        foreach (var itemInfo in orderData.Requires)
                            if (_order.ContainsKey(itemInfo.Id))
                                _order[itemInfo.Id] += itemInfo.TargetCount;
                            else
                                _order.Add(itemInfo.Id, itemInfo.TargetCount);
                    }

                    //统计真正未拥有
                    _require.Clear();
                    var tracer = Game.Manager.mergeBoardMan.activeTracer;
                    var itemOnBoardDict = tracer.GetCurrentActiveBoardItemCount();
                    foreach (var kv in _order)
                        if (!itemOnBoardDict.ContainsKey(kv.Key))
                        {
                            _require.Add(kv.Key, kv.Value);
                        }
                        else
                        {
                            var count = itemOnBoardDict[kv.Key];
                            if (count >= kv.Value)
                                _require.Add(kv.Key, kv.Value - count);
                        }

                    //未拥有链条id
                    _catNeed.Clear();
                    foreach (var kv in _require)
                        for (var i = 0; i < kv.Value; i++)
                            _catNeed.Add(Game.Manager.mergeItemMan.GetItemCategoryId(kv.Key));

                    //计算权重
                    _weight.Clear();
                    foreach (var kv in _catNeed)
                    {
                        var conf = Game.Manager.mergeItemMan.GetCategoryConfig(kv);
                        if (conf.OriginFrom.Count == 0 || conf.DirectFrom.Count == 0)
                            continue;
                        if (conf.OriginFrom[0] == conf.DirectFrom[0])
                        {
                            if (_weight.ContainsKey(conf.OriginFrom[0]))
                                _weight[conf.OriginFrom[0]] += 1;
                            else
                                _weight.Add(conf.OriginFrom[0], 1);
                        }
                        else
                        {
                            if (_weight.ContainsKey(conf.OriginFrom[0]))
                                _weight[conf.OriginFrom[0]] += 1;
                            else
                                _weight.Add(conf.OriginFrom[0], 1);

                            if (_weight.ContainsKey(conf.DirectFrom[0]))
                                _weight[conf.DirectFrom[0]] += 1;
                            else
                                _weight.Add(conf.DirectFrom[0], 1);
                        }
                    }

                    //转为list方便排序
                    _weightList.Clear();
                    foreach (var kv in _weight) _weightList.Add((kv.Key, kv.Value));

                    _weightList.Sort((a, b) => b.Item2 - a.Item2);
                    foreach (var kv in _weightList)
                    {
                        if (!_cat.ContainsKey(kv.Item1))
                            continue;
                        ShowFinger(BoardUtility.GetWorldPosByCoord(_cat[kv.Item1].First().coord));
                        DataTracker.auto_finger.Track(info.AutoFinger,
                            Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, kv.Item2);
                        return true;
                    }

                    return false;
                }
            }
        }

        private bool CheckAutoBag(AutoFingerInfo info)
        {
            if (!CheckLevel(info))
                return false;
            if (Game.Manager.mergeBoardMan.activeWorld.activeBoard.emptyGridCount > 0)
                return false;
            if (BoardViewManager.Instance.checker.HasMatchPair())
                return false;
            if (!Game.Manager.bagMan.CheckBagUnlock(BagMan.BagType.Item))
                return false;
            if (!Game.Manager.bagMan.CanPutItemInBag)
                return false;
            Item item = null;
            BoardViewManager.Instance.board.WalkAllItem((it) =>
            {
                if (!it.isActive) return;
                if (it.tid >= 12000016 && it.tid <= 12000020) return;
                if (item != null)
                {
                    if (it.tid < item.tid)
                        item = it;
                }
                else
                {
                    item = it;
                }
            });
            if (item == null) return false;
            ShowFingerBag(BoardUtility.GetWorldPosByCoord(item.coord));
            DataTracker.auto_finger.Track(info.AutoFinger,
                Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, item.tid);
            return true;
        }

        private bool CheckSaleUI(AutoFingerInfo info)
        {
            if (!CheckLevel(info))
                return false;
            if (Game.Manager.mergeBoardMan.activeWorld.activeBoard.emptyGridCount > 0)
                return false;
            if (BoardViewManager.Instance.checker.HasMatchPair())
                return false;
            if (!Game.Manager.bagMan.CheckBagUnlock(BagMan.BagType.Item))
                return false;
            if (Game.Manager.bagMan.CanPutItemInBag)
                return false;
            if (_uiInterval < info.Time / 1000 && _firstShowSale)
                return false;

            ShowSaleUI();
            _firstShowSale = true;
            DataTracker.auto_finger.Track(info.AutoFinger,
                Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, 0);
            return true;
        }

        private bool CheckSaleItem(AutoFingerInfo info)
        {
            if (Game.Manager.mergeBoardMan.activeWorld.activeBoard.emptyGridCount > 0)
                return false;
            if (BoardViewManager.Instance.checker.HasMatchPair())
                return false;
            if (!Game.Manager.bagMan.CheckBagUnlock(BagMan.BagType.Item))
                return false;
            if (Game.Manager.bagMan.CanPutItemInBag)
                return false;
            var idx = _fingerPosResolverList.FindLastIndex(x => x.type == AutoFinger.Sale);
            if (idx < 0)
                return false;
            ShowFinger(AutoFinger.Sale);
            DataTracker.auto_finger.Track(info.AutoFinger,
                Game.Manager.mergeBoardMan.activeWorld.activeBoard.boardId, Game.Manager.mergeBoardMan.activeItem.tid);

            return true;
        }

        #endregion


        public void TryInterruptGuide()
        {
            if (!_needGuide)
                return;
            if (_triggerRunner == null)
                return;
            _triggerRunner.NeedInterrupt = true;
        }

        /// <summary>
        /// 更改runner状态，在late update中尝试打断
        /// </summary>
        private void _TryInterruptGuide()
        {
            if (!_needGuide)
                return;
            if (_triggerRunner == null)
                return;
            _triggerRunner.NeedInterrupt = true;
        }

        private void _TryInterruptGuide(bool state)
        {
            if (!_needGuide)
                return;
            if (_triggerRunner == null)
                return;
            _triggerRunner.NeedInterrupt = true;
        }

        private void _TryInterruptGuide(int id, Vector3 vet)
        {
            if (!_needGuide)
                return;
            if (_triggerRunner == null)
                return;
            _triggerRunner.NeedInterrupt = true;
        }

        /// <summary>
        /// 打断自动引导
        /// </summary>
        private void _InterruptGuide()
        {
            if (_triggerRunner == null)
                return;
            //重置计时器
            _curInterval = 0;
            _triggerRunner.NeedInterrupt = false;
            _curFinger = -1;
            if (!_isShowing)
                return;
            if (_finger != null)
            {
                _finger.transform.eulerAngles = Vector3.zero;
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.Auto_Guide_Finger, _finger);
            }

            if (_sequence != null)
            {
                _sequence.Kill();
                _sequence = null;
            }

            _target = null;
            _triggerRunner.NeedUpdate = false;
            _isShowing = false;
        }

        private void ShowFingerBag(Vector3 pos)
        {
            var start = pos;
            var idx = _fingerPosResolverList.FindLastIndex(x => x.type == AutoFinger.Bag);
            if (idx < 0)
                return;
            _isShowing = true;
            _target = _fingerPosResolverList[idx].handler?.Invoke();
            _finger = GameObjectPoolManager.Instance.CreateObject(PoolItemType.Auto_Guide_Finger,
                UIManager.Instance.GetLayerRootByType(UILayer.Effect));
            var temp = DOTween.Sequence();
            temp.Pause();
            temp.Append(_finger.transform.DOScale(0.85f * Vector3.one, 0.5f));
            temp.Append(_finger.transform.DOMove(_target.position, 2f).From(start));
            temp.InsertCallback(2.5f, () => { _target.GetComponent<Animator>()?.SetTrigger("Punch"); });
            temp.SetLoops(-1);
            temp.Play();
            _sequence = temp;
        }

        private void ShowSaleUI()
        {
            _isShowing = true;
            _uiInterval = 0;
            Game.Manager.commonTipsMan.ShowMessageTips(I18N.Text("#SysComDesc573"), I18N.Text("#SysComDesc6"), null,
                () => { _isShowing = false; }, true);
        }

        private void ShowFinger(AutoFinger type)
        {
            _finger = GameObjectPoolManager.Instance.CreateObject(PoolItemType.Auto_Guide_Finger,
                UIManager.Instance.GetLayerRootByType(UILayer.Effect));
            var idx = _fingerPosResolverList.FindLastIndex(x => x.type == type);
            if (idx < 0)
                return;
            _target = _fingerPosResolverList[idx].handler?.Invoke();
            //跟随功能
            _triggerRunner.Handler += _FingerFollow;
            _triggerRunner.NeedUpdate = true;
            _isShowing = true;
            _curInterval = 0;
            if (type == AutoFinger.Meta || type == AutoFinger.Sale)
                _finger.transform.eulerAngles = 180 * Vector3.forward;
            _curFinger = (int)type;
        }

        private void ShowFinger(Transform trans)
        {
            _finger = GameObjectPoolManager.Instance.CreateObject(PoolItemType.Auto_Guide_Finger,
                UIManager.Instance.GetLayerRootByType(UILayer.Effect));
            _target = trans;
            _triggerRunner.Handler += _FingerFollow;
            _triggerRunner.NeedUpdate = true;
            _isShowing = true;
            _curInterval = 0;
        }

        private void ShowFinger(Vector3 pos)
        {
            _finger = GameObjectPoolManager.Instance.CreateObject(PoolItemType.Auto_Guide_Finger,
                UIManager.Instance.GetLayerRootByType(UILayer.Effect));
            _finger.transform.position = pos;
        }

        private void _FingerFollow()
        {
            if (_finger != null && _target != null)
            {
                if (_target.gameObject.activeInHierarchy)
                    _finger.transform.position = _target.position;
                else
                    TryInterruptGuide();
            }
        }
    }
}