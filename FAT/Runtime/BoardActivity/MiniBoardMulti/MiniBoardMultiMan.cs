/*
 * @Author: tang.yan
 * @Description: 多轮迷你棋盘 管理器
 * @Doc: https://centurygames.feishu.cn/wiki/T4X1wPwIniNnGwkHiEAc4Rp1n5c
 * @Date: 2025-01-02 15:01:48
 */

using System.Collections.Generic;
using Cysharp.Text;
using fat.rawdata;
using fat.gamekitdata;
using EL;
using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public class MiniBoardMultiMan : IGameModule, IUserDataHolder
    {
        //迷你棋盘是否解锁
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureMiniBoardMulti);
        //迷你棋盘相关数据是否有效
        public bool IsValid => CurActivity != null && World != null;
        
        //记录当前正在进行的活动 若不在活动时间内会置为null
        //外部调用时记得判空 默认同一时间内只会开一个迷你棋盘 
        public MiniBoardMultiActivity CurActivity { get; private set; }
        //活动处理器 用于创建活动 默认跟随迷你棋盘管理器创建
        public MiniBoardMultiActivityHandler ActivityHandler = new MiniBoardMultiActivityHandler();

        public MergeWorld World { get; private set; }   //世界实体
        public MergeWorldTracer WorldTracer { get; private set; }   //世界实体追踪器

        public void DebugResetMiniBoard()
        {
            ClearMiniBoardData();
        }
        
        //外部调用需判空
        public EventMiniBoardMultiGroup GetCurGroupConfig()
        {
            return CurActivity == null ? null : Game.Manager.configMan.GetEventMiniBoardMultiGroupConfig(CurActivity.GroupId);
        }
        
        //外部调用需判空
        public EventMiniBoardMultiInfo GetCurInfoConfig()
        {
            return GetTargetIndexInfoConfig(_curRoundIndex);
        }
        
        //获取指定序号的棋盘配置信息
        public EventMiniBoardMultiInfo GetTargetIndexInfoConfig(int targetIndex)
        {
            var infoIdList = GetCurGroupConfig()?.InfoId;
            if (infoIdList == null) return null;
            return infoIdList.TryGetByIndex(targetIndex, out var infoId) 
                ? Game.Manager.configMan.GetEventMiniBoardMultiInfoConfig(infoId) 
                : null;
        }
        
        //检查当前是否还有下一个棋盘
        public bool CheckHasNextBoard()
        {
            var infoIdList = GetCurGroupConfig()?.InfoId;
            if (infoIdList == null)
                return false;
            return _curRoundIndex + 1 < infoIdList.Count;
        }
        
        //检查当前是否是最后一个棋盘
        public bool CheckIsLastBoard()
        {
            var infoIdList = GetCurGroupConfig()?.InfoId;
            if (infoIdList == null)
                return false;
            return _curRoundIndex == infoIdList.Count - 1;
        }
        
        //图鉴棋子是否解锁
        public bool IsItemUnlock(int itemId)
        {
            return Game.Manager.handbookMan.IsItemUnlocked(itemId);
        }
        
        //获取入口处是否显示红点 以及红点上显示的数字
        public bool CheckIsShowRedPoint(out int rpNum)
        {
            rpNum = 0;
            if (!IsValid) return false;
            rpNum = World.rewardCount;
            return true;
        }

        public int GetCurRoundIndex()
        {
            return _curRoundIndex;
        }

        // 区分迷你棋盘是从哪里打开的 主棋盘或者meta场景
        private static bool _isEnterFromMain;
        //进入迷你棋盘
        public void EnterMiniBoard()
        {
            if (!IsValid) return;
            if (UIManager.Instance.IsOpen(CurActivity.BoardResAlt.ActiveR)) return;
            //如果在主棋盘 则关闭棋盘界面并打开背景界面
            _isEnterFromMain = UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain);
            if (_isEnterFromMain)
            {
                MessageCenter.Get<MSG.GAME_MAIN_BOARD_STATE_CHANGE>().Dispatch(false);
            }
            UIManager.Instance.OpenWindow(CurActivity.BoardResAlt.ActiveR, CurActivity);
        }

        //离开迷你棋盘
        public void ExitMiniBoard(MiniBoardMultiActivity act)
        {
            if(act == null)
                return;
            if(!UIManager.Instance.IsOpen(act.BoardResAlt.ActiveR))
                return;
            //关闭迷你棋盘界面
            UIManager.Instance.CloseWindow(act.BoardResAlt.ActiveR);
            //返回来源处
            if (_isEnterFromMain)
            {
                MessageCenter.Get<MSG.GAME_MAIN_BOARD_STATE_CHANGE>().Dispatch(true);
            }
        }

        //检查目前是否可以进入下一个棋盘
        public bool CheckCanEnterNextRound()
        {
            if (!IsValid) return false;
            var itemCount = GetCurInfoConfig()?.LevelItem?.Count ?? 0;
            //默认通关条件为当前轮次的棋盘对应合成链的最高级棋子已经解锁
            return itemCount > 0 && CurActivity.UnlockMaxLevel >= itemCount - 1;
        }
        
        //点击棋盘上的传送门 打开确认界面
        public void TryOpenUIEnterNextRoundTips()
        {
            if (!CheckCanEnterNextRound() || !CheckHasNextBoard())
            {
                MessageCenter.Get<MSG.UI_CLICK_LOCK_DOOR>().Dispatch();
                return;
            }
            var targetIndex = _curRoundIndex + 1;
            var conf = GetTargetIndexInfoConfig(targetIndex);
            if (conf == null) 
                return;
            //收集棋盘上存留的继承棋子id信息
            var inheritItemIdDict = new Dictionary<int, int>();
            _CollectInheritItem(inheritItemIdDict);
            //打开确认界面 告知继承的棋子id信息
            UIManager.Instance.OpenWindow(CurActivity.NextRoundResAlt.ActiveR, inheritItemIdDict);
            //发送事件 告知显示层目前要继承的棋子id信息
            MessageCenter.Get<MSG.UI_MINI_BOARD_MULTI_INHERIT_ITEM>().Dispatch(inheritItemIdDict);
        }

        //点击确认界面确定按钮后调用
        //第一步是直接断引用当前world 然后创建注册新的world
        //第二步是界面层pop原来的world push新的world
        //第三步是界面层调用 SendRewardToCurBoard方法 往新棋盘上发送继承的各种奖励
        //以上三步是连贯调用的 不可有异步
        public bool TryEnterNextRound(out Dictionary<int, int> bonusItemIdDict, out Dictionary<int, int> inheritItemIdDict, out Dictionary<int, int> giftBoxItemIdDict)
        {
            bonusItemIdDict = new Dictionary<int, int>();
            inheritItemIdDict = new Dictionary<int, int>();
            giftBoxItemIdDict = new Dictionary<int, int>();
            if (!CheckCanEnterNextRound() || !CheckHasNextBoard())
                return false;
            var targetIndex = _curRoundIndex + 1;
            var conf = GetTargetIndexInfoConfig(targetIndex);
            if (conf == null) 
                return false;
            //当前棋盘上的棋子回收逻辑
            //1、直接使用棋盘上存留的bonus tapbonus棋子
            _ClaimBonusItem(bonusItemIdDict);
            //2、收集棋盘上存留的继承棋子id 用于发到下个棋盘
            _CollectInheritItem(inheritItemIdDict);
            //3、收集棋盘奖励箱的所有棋子，直接发到下个棋盘
            _CollectGiftBoxItem(giftBoxItemIdDict);
            //清理当前棋盘
            ClearMiniBoardData(false);
            //初始化下个棋盘
            _InitMiniBoardDataByIndex(targetIndex);
            return true;
        }
        
        //发送继承奖励到当前新的棋盘，调用此方法前确保旧的棋盘已经pop 新的棋盘已经push
        public void SendRewardToCurBoard(Dictionary<int, int> bonusItemIdDict, Dictionary<int, int> inheritItemIdDict, Dictionary<int, int> giftBoxItemIdDict)
        {
            //向棋盘中发送继承的奖励 先发棋盘的 再发奖励箱的 
            _SendInheritRewardToBoard(inheritItemIdDict, giftBoxItemIdDict);
            //最后根据配置向棋盘发送初始奖励
            _SendStartRewardToBoard();
            //立即存档
            Game.Manager.archiveMan.SendImmediately(true);
            //进入下一个棋盘时打点
            DataTracker.event_miniboard_multi_newboard.Track(CurActivity, World.activeBoard?.boardId ?? 0, _curRoundIndex + 1,
                ConvertDictToString(bonusItemIdDict), 
                ConvertDictToString(inheritItemIdDict), 
                ConvertDictToString(giftBoxItemIdDict));
        }

        //获取当前已解锁到的迷你棋盘棋子最大等级 等级从0开始 这里的等级涵盖了整条合成链的所有棋子
        public int GetCurUnlockItemMaxLevel()
        {
            if (!IsValid)
                return 0;
            var maxLevel = 0;
            for (var i = 0; i < _allItemIdList.Count; i++)
            {
                var itemId = _allItemIdList[i];
                if (IsItemUnlock(itemId) && maxLevel < i)
                    maxLevel = i;
            }
            return maxLevel;
        }
        
        //用于活动入口界面显示的最大等级 只以当前阶段棋盘里合成链的最大等级为准
        public int GetCurUnlockItemMaxLevelEntry()
        {
            var infoConfig = GetCurInfoConfig();
            if (infoConfig == null)
                return 0;
            var maxLevel = -1;
            for (var i = 0; i < infoConfig.LevelItem.Count; i++)
            {
                var itemId = infoConfig.LevelItem[i];
                if (IsItemUnlock(itemId) && maxLevel < i)
                    maxLevel = i;
            }
            return maxLevel;
        }

        //检查是否是迷你棋盘专属棋子
        public bool CheckIsMiniBoardItem(int itemId)
        {
            if (!IsValid || itemId <= 0) return false;
            var infoConfig = GetCurInfoConfig();
            if (infoConfig == null)
                return false;
            return _allItemIdList.Contains(itemId);
        }

        //检查传入的棋子id是否属于所有迷你棋盘活动使用到的主合成链棋子
        public bool CheckIsBelongMiniBoard(int itemId)
        {
            if (itemId <= 0) return false;
            var mapConf = Game.Manager.configMan.GetEventMiniBoardMultiInfoMapConfig();
            if (mapConf == null) return false;
            foreach (var conf in mapConf)
            {
                foreach (var id in conf.LevelItem)
                {
                    if (id == itemId)
                        return true;
                }
            }
            return false;
        }
        
        //当迷你棋盘中有新棋子解锁时刷新相关数据(数据层)
        public void OnNewItemUnlock()
        {
            if (!IsValid) return;
            _RefreshSpawnHandlerInfo();
            CurActivity.UnlockMaxLevel = GetCurUnlockItemMaxLevel();
        }

        //当迷你棋盘中有新棋子解锁时执行相关表现(表现层)
        public void OnNewItemShow(Merge.Item itemData)
        {
            if (!IsValid) return;
            //只有在解锁的新棋子是迷你棋盘棋子时才发事件并打点
            if (CheckIsMiniBoardItem(itemData.config.Id))
            {
                MessageCenter.Get<MSG.UI_MINI_BOARD_MULTI_UNLOCK_ITEM>().Dispatch(itemData);
                var groupConfig = GetCurGroupConfig();
                var maxLevel = 0;
                var totalItemNum = _allItemIdList.Count;
                var diff = 0;
                if (IsValid && groupConfig != null)
                {
                    for (var i = 0; i < totalItemNum; i++)
                    {
                        var itemId = _allItemIdList[i];
                        if (IsItemUnlock(itemId) && maxLevel < i)
                            maxLevel = i;
                    }
                    diff = groupConfig.Diff;
                }
                var isFinal = maxLevel + 1 == totalItemNum;
                DataTracker.event_miniboard_multi_milestone.Track(CurActivity, GetCurUnlockItemMaxLevel() + 1, World.activeBoard?.boardId ?? 0, _curRoundIndex + 1, totalItemNum, diff, isFinal);
            }
        }

        //当前迷你棋盘界面是否打开(新手引导专用
        public bool CheckMiniBoardOpen()
        {
            if (!IsValid)
                return false;
            return UIManager.Instance.IsOpen(CurActivity.BoardResAlt.ActiveR) && CurActivity.UIOpenState;
        }

        public bool CheckMiniBoardUIOpen()
        {
            if (!IsValid)
                return false;
            return UIManager.Instance.IsOpen(CurActivity.BoardResAlt.ActiveR);
        }

        #region 内部数据构造相关逻辑
        
        private int _curRoundIndex;   //当前正在轮次序号,默认从0开始,需要使用此序号去EventMiniBoardMultiGroup.id中去索引
        private List<int> _allItemIdList = new List<int>();    //当前活动主链条的所有棋子idList 按等级由小到大排序

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.MiniBoardActivity?.MiniBoardMultiData;
            if (data == null) return;
            //存档有数据时初始化相关数据
            //棋盘数据
            var boardData = data.MiniBoard;
            if (boardData != null)
            {
                _InitCurRoundIndex(data.CurRoundIndex);
                _InitWorld(boardData.BoardId,false);
                World.Deserialize(boardData, null);
                WorldTracer.Invalidate();
            }
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.MiniBoardActivity ??= new fat.gamekitdata.MiniBoardActivity();
            if (IsValid)
            {
                var miniBoardMultiData = new MiniBoardMultiData();
                miniBoardMultiData.BindActivityId = CurActivity.Id;
                miniBoardMultiData.CurRoundIndex = _curRoundIndex;
                miniBoardMultiData.MiniBoard = new fat.gamekitdata.Merge();
                World.Serialize(miniBoardMultiData.MiniBoard);
                data.MiniBoardActivity.MiniBoardMultiData = miniBoardMultiData;
            }
        }
        
        public void SetCurActivity(MiniBoardMultiActivity activity)
        {
            CurActivity = activity;
            _RefreshSpawnHandlerInfo();
            _RefreshAllItemIdList();
        }

        public void InitMiniBoardData(bool isNew)
        {
            //不是第一次创建活动时 return
            if (!isNew)
                return;
            //第一次创建活动时 先清理一下之前可能存在的棋盘数据和图鉴数据
            ClearMiniBoardData();
            //默认活动开始时取第0个作为初始棋盘
            _InitMiniBoardDataByIndex(0);
            //根据配置向棋盘发送初始奖励
            _SendStartRewardToBoard();
            //活动开启且棋盘创建成功时打点
            if (IsValid)
                DataTracker.event_miniboard_multi_newboard.Track(CurActivity, World.activeBoard?.boardId ?? 0, _curRoundIndex + 1, "", "", "");
        }

        //此方法在活动第一次开启或者进入下一轮棋盘时调用
        private void _InitMiniBoardDataByIndex(int index)
        {
            var infoConfig = GetTargetIndexInfoConfig(index);
            if (infoConfig == null)
                return;
            _InitCurRoundIndex(index);
            _InitWorld(infoConfig.BoardId,true);
            _RefreshSpawnHandlerInfo();
            WorldTracer.Invalidate();
        }
        
        private void _InitCurRoundIndex(int index)
        {
            _curRoundIndex = index;
        }
        
        public void ClearMiniBoardData(bool isEnd = true)
        {
            if (isEnd)
            {
                //活动结束时将关联棋子的图鉴置为锁定状态
                Game.Manager.handbookMan.LockHandbookItem(_allItemIdList);
            }
            //取消注册并清理当前world
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            World = null;
            WorldTracer = null;
        }
        
        //活动结束时回收棋盘中可以领取但未领取的各种奖励 这个方法会直接beginReward 需要在界面中合适时机自行commit
        //(MergeBoardMan中有个ClearBoard的清理方法 但是和当前需求可能不太相符 因此单独起一个干净的逻辑)
        public bool CollectAllBoardReward(List<RewardCommitData> rewards)
        {
            if (!IsValid || rewards == null)
                return false;
            var itemIdMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();    //棋子id整合结果(用于打点)
            var rewardMap = ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();    //奖励整合结果
            World.WalkAllItem((item) =>
            {
                //遍历整个棋盘 找出所有可以回收的棋子 并整合
                _TryCollectReward(item, itemIdMap, rewardMap);
            }, MergeWorld.WalkItemMask.NoInventory);    //迷你棋盘没有背包
            //发奖 相当于帮玩家把棋盘上没有使用的棋子直接用了 所以from用use_item
            foreach (var reward in rewardMap)
            {
                rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Key, reward.Value, ReasonString.use_item));
            }
            //打点
            DataTracker.event_miniboard_multi_end_collect.Track(CurActivity, ConvertDictToString(itemIdMap));
            //数据回收
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(itemIdMap);
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
            return true;
        }
        
        private static void Collect(Dictionary<int, int> map, int id, int count, int maxCount = -1)
        {
            //maxCount为-1表示没有最大数量限制
            if (maxCount == -1)
            {
                if (map.ContainsKey(id)) map[id] += count;
                else map.Add(id, count);
            }
            else if (maxCount > 0)
            {
                if (map.TryGetValue(id, out var curCount))
                {
                    var checkCount = curCount + count;
                    map[id] = Mathf.Min(checkCount, maxCount);
                }
                else
                {
                    map.Add(id, Mathf.Min(count, maxCount));
                }
            }
        }

        private static string ConvertDictToString(Dictionary<int, int> dict)
        {
            var sb = ZString.CreateStringBuilder();
            foreach (var info in dict)
            {
                if (sb.Length > 0) sb.Append(",");  // 只有在不是第一个元素时才加逗号
                //id:数量:棋子等级  逗号隔开
                sb.Append(info.Key.ToString());
                sb.Append(":");
                sb.Append(info.Value.ToString());
                sb.Append(":");
                sb.Append(ItemUtility.GetItemLevel(info.Key).ToString());
            }
            return sb.ToString();
        }

        private void _TryCollectReward(Merge.Item item, Dictionary<int, int> itemIdMap, Dictionary<int, int> rewardMap)
        {
            if (item.TryGetItemComponent<ItemBonusCompoent>(out var bonusComp) && bonusComp.funcType == FuncType.Reward)
            {
                //收集棋子id
                Collect(itemIdMap, item.tid, 1);
                //收集奖励信息
                Collect(rewardMap, bonusComp.bonusId, bonusComp.bonusCount);
            }
            else if (item.TryGetItemComponent<ItemTapBonusComponent>(out var tapBonusComp) && tapBonusComp.funcType == FuncType.Collect)
            {
                //收集棋子id
                Collect(itemIdMap, item.tid, 1);
                //收集奖励信息
                Collect(rewardMap, tapBonusComp.bonusId, tapBonusComp.bonusCount);
            }
        }

        private void _InitWorld(int boardId, bool isFirstOpen)
        {
            World = new MergeWorld();
            WorldTracer = new MergeWorldTracer(null, null);
            Game.Manager.mergeBoardMan.RegisterMergeWorldEntry(new MergeWorldEntry()
            {
                world = World,
                type = MergeWorldEntry.EntryType.MiniBoardMulti,
            });
            WorldTracer.Bind(World);
            World.BindTracer(WorldTracer);
            //迷你棋盘不需要背包 也没有订单 和底部信息栏
            // World.BindOrderHelper(Game.Manager.mainOrderMan.curOrderHelper);
            Game.Manager.mergeBoardMan.InitializeBoard(World, boardId, isFirstOpen);
        }

        //当迷你棋盘中有新棋子解锁时刷新产出配置信息
        private void _RefreshSpawnHandlerInfo()
        {
            if (!IsValid) return;
            var confId = _GetCurDropConfId();
            CurActivity.SpawnHandler.RefreshOutputsInfo(confId);
        }

        private void _RefreshAllItemIdList()
        {
            _allItemIdList.Clear();
            var infoIdList = GetCurGroupConfig()?.InfoId;
            if (infoIdList == null) 
                return;
            foreach (var infoId in infoIdList)
            {
                var itemList = Game.Manager.configMan.GetEventMiniBoardMultiInfoConfig(infoId)?.LevelItem;
                if (itemList == null) continue;
                foreach (var id in itemList)
                {
                    _allItemIdList.AddIfAbsent(id);
                }
            }
        }
        
        //获取当前已解锁到的迷你棋盘掉落信息id (EventMiniBoardMultiDrop.id) 
        private int _GetCurDropConfId()
        {
            var infoConfig = GetCurInfoConfig();
            if (infoConfig == null)
                return 0;
            var maxLevel = 0;
            for (var i = 0; i < infoConfig.LevelItem.Count; i++)
            {
                var itemId = infoConfig.LevelItem[i];
                if (IsItemUnlock(itemId) && maxLevel < i)
                    maxLevel = i;
            }
            infoConfig.Levelid.TryGetByIndex(maxLevel, out var dropId);
            return dropId;
        }

        //往迷你棋盘上发初始奖励奖励
        private void _SendStartRewardToBoard()
        {
            if (!IsValid) return;
            var infoConfig = GetCurInfoConfig();
            if (infoConfig == null)
                return;
            var startReward = infoConfig.ItemNum.ConvertToRewardConfig();
            if (startReward == null) return;
            var rewardMan = Game.Manager.rewardMan;
            rewardMan.PushContext(new RewardContext() { targetWorld = World });
            rewardMan.CommitReward(rewardMan.BeginReward(startReward.Id, startReward.Count, ReasonString.miniboard_multi_start));
            rewardMan.PopContext();
        }

        #region 切换进入下一个棋盘的相关逻辑

        //直接使用棋盘上存留的bonus tapbonus棋子
        private void _ClaimBonusItem(Dictionary<int, int> bonusItemIdDict)
        {
            if (!IsValid || bonusItemIdDict == null)
                return;
            var bonusItemList = ObjectPool<List<Item>>.GlobalPool.Alloc();
            var tapBonusItemList = ObjectPool<List<Item>>.GlobalPool.Alloc();
            World.WalkAllItem((item) =>
            {
                if (item.TryGetItemComponent<ItemBonusCompoent>(out var bonusComp) && bonusComp.funcType == FuncType.Reward)
                {
                    bonusItemList.Add(item);
                    //收集棋子id
                    Collect(bonusItemIdDict, item.tid, 1);
                }
                else if (item.TryGetItemComponent<ItemTapBonusComponent>(out var tapBonusComp) && tapBonusComp.funcType == FuncType.Collect)
                {
                    tapBonusItemList.Add(item);
                    //收集棋子id
                    Collect(bonusItemIdDict, item.tid, 1);
                }
            }, MergeWorld.WalkItemMask.Board);    //只遍历棋盘上的棋子
            //使用bonus item
            if (bonusItemList.Count > 0)
            {
                World.onCollectBonus += _EnsureClaimRewardCommit;
                foreach (var item in bonusItemList)
                {
                    World.UseBonusItem(item);
                }
                World.onCollectBonus -= _EnsureClaimRewardCommit;
                bonusItemList.Clear();
            }
            //使用tap bonus item
            if (tapBonusItemList.Count > 0)
            {
                World.onCollectTapBonus += _EnsureClaimRewardCommit;
                foreach (var item in tapBonusItemList)
                {
                    World.UseTapBonusItem(item);
                }
                World.onCollectTapBonus -= _EnsureClaimRewardCommit;
                tapBonusItemList.Clear();
            }
            //数据回收
            ObjectPool<List<Item>>.GlobalPool.Free(bonusItemList);
            ObjectPool<List<Item>>.GlobalPool.Free(tapBonusItemList);
        }
        
        private void _EnsureClaimRewardCommit(Merge.MergeWorld.BonusClaimRewardData rewardData)
        {
            var reward = rewardData.GrabReward();
            if (reward != null)
            {
                Game.Manager.rewardMan.CommitReward(reward);
            }
        }

        private void _CollectInheritItem(Dictionary<int, int> inheritItemIdDict)
        {
            if (!IsValid || inheritItemIdDict == null)
                return;
            //可以继承到下一关卡的棋子id list
            var canInheritIdList = GetCurInfoConfig()?.NextBoardItem;
            if (canInheritIdList == null)
                return;
            World.WalkAllItem((item) =>
            {
                if (canInheritIdList.TryGetValue(item.tid, out var maxInheritCount))
                {
                    Collect(inheritItemIdDict, item.tid, 1, maxInheritCount);
                }
            }, MergeWorld.WalkItemMask.Board);    //只遍历棋盘上的棋子
        }

        private void _CollectGiftBoxItem(Dictionary<int, int> giftBoxItemIdDict)
        {
            if (!IsValid || giftBoxItemIdDict == null)
                return;
            World.WalkAllItem((item) =>
            {
                Collect(giftBoxItemIdDict, item.tid, 1);
            }, MergeWorld.WalkItemMask.RewardList);    //只遍历奖励箱中的棋子
        }

        //向棋盘中发送继承的奖励 先发奖励箱的 再发棋盘的
        private void _SendInheritRewardToBoard(Dictionary<int, int> inheritItemIdDict, Dictionary<int, int> giftBoxItemIdDict)
        {
            if (!IsValid) return;
            var rewardMan = Game.Manager.rewardMan;
            rewardMan.PushContext(new RewardContext() { targetWorld = World });
            foreach (var info in inheritItemIdDict)
            {
                rewardMan.CommitReward(rewardMan.BeginReward(info.Key, info.Value, ReasonString.miniboard_multi_inherititem));
            }
            foreach (var info in giftBoxItemIdDict)
            {
                rewardMan.CommitReward(rewardMan.BeginReward(info.Key, info.Value, ReasonString.miniboard_multi_gift_box_item));
            }
            rewardMan.PopContext();
        }

        #endregion

        public void Reset()
        {
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            World = null;
            WorldTracer = null;
            CurActivity = null;
        }
        public void LoadConfig() { }
        public void Startup() { }

        #endregion
    }
}