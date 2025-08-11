/*
 * @Author: tang.yan
 * @Description: 迷你棋盘 管理器
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/srvlglwhgesf04qp
 * @Date: 2024-08-06 15:08:26
 */

using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using EL;
using FAT.Merge;

namespace FAT
{
    public class MiniBoardMan : IGameModule, IUserDataHolder
    {
        //迷你棋盘是否解锁
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureMiniBoard);
        //迷你棋盘相关数据是否有效
        public bool IsValid => CurActivity != null && World != null;
        
        //记录当前正在进行的活动 若不在活动时间内会置为null
        //外部调用时记得判空 默认同一时间内只会开一个迷你棋盘 
        public MiniBoardActivity CurActivity { get; private set; }
        //活动处理器 用于创建活动 默认跟随迷你棋盘管理器创建
        public MiniBoardActivityHandler ActivityHandler = new MiniBoardActivityHandler();

        public MergeWorld World { get; private set; }   //世界实体
        public MergeWorldTracer WorldTracer { get; private set; }   //世界实体追踪器

        public void DebugResetMiniBoard()
        {
            ClearMiniBoardData();
        }
        
        public EventMiniBoardDetail GetCurDetailConfig()
        {
            return !IsValid ? null : Game.Manager.configMan.GetEventMiniBoardDetailConfig(CurActivity.DetailId);
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
        public void ExitMiniBoard(MiniBoardActivity act)
        {
            if(act == null)
                return;
            //关闭迷你棋盘界面
            UIManager.Instance.CloseWindow(act.BoardResAlt.ActiveR);
            //返回来源处
            if (_isEnterFromMain)
            {
                MessageCenter.Get<MSG.GAME_MAIN_BOARD_STATE_CHANGE>().Dispatch(true);
            }
        }
        
        //获取当前已解锁到的迷你棋盘棋子最大等级 等级从0开始
        public int GetCurUnlockItemMaxLevel()
        {
            var detailConfig = GetCurDetailConfig();
            if (detailConfig == null)
                return 0;
            var maxLevel = 0;
            for (var i = 0; i < detailConfig.LevelItem.Count; i++)
            {
                var itemId = detailConfig.LevelItem[i];
                if (IsItemUnlock(itemId) && maxLevel < i)
                    maxLevel = i;
            }
            return maxLevel;
        }
        
        public int GetCurUnlockItemMaxLevelEntry()
        {
            var detailConfig = GetCurDetailConfig();
            if (detailConfig == null)
                return 0;
            var maxLevel = -1;
            for (var i = 0; i < detailConfig.LevelItem.Count; i++)
            {
                var itemId = detailConfig.LevelItem[i];
                if (IsItemUnlock(itemId) && maxLevel < i)
                    maxLevel = i;
            }
            return maxLevel;
        }

        //检查是否是迷你棋盘专属棋子
        public bool CheckIsMiniBoardItem(int itemId)
        {
            if (itemId <= 0)
                return false;
            var detailConfig = GetCurDetailConfig();
            if (detailConfig == null)
                return false;
            return detailConfig.LevelItem.Contains(itemId);
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
                MessageCenter.Get<MSG.UI_MINI_BOARD_UNLOCK_ITEM>().Dispatch(itemData);
                var detailConfig = GetCurDetailConfig();
                var maxLevel = 0;
                var totalItemNum = 0;
                var diff = 0;
                if (detailConfig != null)
                {
                    totalItemNum = detailConfig.LevelItem.Count;
                    for (var i = 0; i < totalItemNum; i++)
                    {
                        var itemId = detailConfig.LevelItem[i];
                        if (IsItemUnlock(itemId) && maxLevel < i)
                            maxLevel = i;
                    }
                    diff = detailConfig.Diff;
                }
                var isFinal = maxLevel + 1 == totalItemNum;
                DataTracker.event_miniboard_milestone.Track(CurActivity, maxLevel + 1, totalItemNum, diff, isFinal);
            }
        }

        //当前迷你棋盘界面是否打开(新手引导专用
        public bool CheckMiniBoardOpen()
        {
            if (!IsValid)
                return false;
            return UIManager.Instance.IsOpen(CurActivity.BoardResAlt.ActiveR) && Game.Manager.miniBoardMan.CurActivity.UIOpenState;
        }

        public bool CheckMiniBoardUIOpen()
        {
            if (!IsValid)
                return false;
            return UIManager.Instance.IsOpen(CurActivity.BoardResAlt.ActiveR);
        }

        #region 内部数据构造相关逻辑
        
        void IUserDataHolder.SetData(LocalSaveData archive)
        {
             var data = archive.ClientData.PlayerGameData.MiniBoardActivity?.MiniBoardDataList;
             if (data == null) return;
            //设计上支持记录多期棋盘数据 使用时目前默认只取第一个数据 开新一期时会覆盖现有的
            if (!data.TryGetByIndex(0, out var firstData)) return;
            //存档有数据时初始化相关数据
            //棋盘数据
            var boardData = firstData.MiniBoard;
            if (boardData != null)
            {
                _InitWorld(boardData.BoardId,false);
                World.Deserialize(boardData, null);
                WorldTracer.Invalidate();
            }
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.MiniBoardActivity ??= new fat.gamekitdata.MiniBoardActivity();
            var dataList = data.MiniBoardActivity.MiniBoardDataList;
            dataList.Clear();
            if (IsValid)
            {
                var miniBoardData = new MiniBoardData();
                miniBoardData.BindActivityId = CurActivity.Id;
                miniBoardData.MiniBoard = new fat.gamekitdata.Merge();
                World.Serialize(miniBoardData.MiniBoard);
                dataList.Add(miniBoardData);
            }
        }
        
        public void SetCurActivity(MiniBoardActivity activity)
        {
            CurActivity = activity;
            if (CurActivity != null)
                _RefreshSpawnHandlerInfo();
        }
        
        public void InitMiniBoardData(bool isNew)
        {
            //不是第一次创建活动时 return
            if (!isNew)
                return;
            //第一次创建活动时 先清理一下之前可能存在的棋盘数据和图鉴数据
            ClearMiniBoardData();
            var detailConfig = Game.Manager.configMan.GetEventMiniBoardDetailConfig(CurActivity?.DetailId ?? 0);
            if (detailConfig == null)
                return;
            _InitWorld(detailConfig.BoardId,true);
            _RefreshSpawnHandlerInfo();
            _SendStartRewardToBoard();
            WorldTracer.Invalidate();
        }
        
        public void ClearMiniBoardData()
        {
            //活动结束时将关联棋子的图鉴置为锁定状态
            Game.Manager.handbookMan.LockHandbookItem(GetCurDetailConfig()?.LevelItem);
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
            foreach (var item in itemIdMap)
            {
                DataTracker.event_miniboard_end_collect.Track(CurActivity, item.Key, item.Value);
            }
            //数据回收
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(itemIdMap);
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(rewardMap);
            return true;
        }

        private void _TryCollectReward(Merge.Item item, Dictionary<int, int> itemIdMap, Dictionary<int, int> rewardMap)
        {
            static void Collect(Dictionary<int, int> map, int id, int count)
            {
                if (map.ContainsKey(id)) map[id] += count;
                else map.Add(id, count);
            }
            
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
                type = MergeWorldEntry.EntryType.MiniBoard,
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
        
        //获取当前已解锁到的迷你棋盘掉落信息id (EventMiniBoardDrop.id) 
        private int _GetCurDropConfId()
        {
            var detailConfig = GetCurDetailConfig();
            if (detailConfig == null)
                return 0;
            var maxLevel = 0;
            for (var i = 0; i < detailConfig.LevelItem.Count; i++)
            {
                var itemId = detailConfig.LevelItem[i];
                if (IsItemUnlock(itemId) && maxLevel < i)
                    maxLevel = i;
            }
            detailConfig.Levelid.TryGetByIndex(maxLevel, out var dropId);
            return dropId;
        }

        //往迷你棋盘上发初始奖励奖励
        private void _SendStartRewardToBoard()
        {
            if (!IsValid) return;
            var detailConfig = GetCurDetailConfig();
            if (detailConfig == null)
                return;
            var startReward = detailConfig.ItemNum.ConvertToRewardConfig();
            if (startReward == null) return;
            var rewardMan = Game.Manager.rewardMan;
            rewardMan.PushContext(new RewardContext() { targetWorld = World });
            rewardMan.CommitReward(rewardMan.BeginReward(startReward.Id, startReward.Count, ReasonString.miniboard_start));
            rewardMan.PopContext();
        }

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