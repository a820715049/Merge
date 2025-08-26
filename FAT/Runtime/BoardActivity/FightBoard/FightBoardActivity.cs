using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using static FAT.RecordStateHelper;
using Cysharp.Text;
using UnityEngine;
using EL;
using DG.Tweening;
using System.Linq;
using UnityEngine.UI.Extensions;
using System.Collections;

namespace FAT
{
    public class FightBoardActivity : ActivityLike, IBoardEntry, IBoardArchive, IExternalOutput, ISpawnEffectWithTrail
    {
        #region 运行时字段
        public FightMonster monster = new();
        public MergeWorld World => _world;
        private MergeWorld _world;
        private MergeWorldTracer _tracer;
        private FightBoardItemSpawnBonusHandler _spawnBonusHandler;
        private readonly List<(int itemId, int weight)> _itemOutputs = new();
        private readonly List<RewardCommitData> _attackRewards = new();
        private readonly List<RewardCommitData> _levelRewards = new();
        public override ActivityVisual Visual => BoardRes.visual;
        #endregion

        #region 配置
        public EventFight eventFight;
        public EventFightDetail eventFightDetail;
        public EventFightLevel eventFightLevel;
        #endregion

        #region UI Resource
        public VisualPopup StartPopup { get; } = new(UIConfig.UIActivityFightBegin);
        public VisualPopup EndPopup { get; } = new(UIConfig.UIActivityFightEnd);
        public VisualPopup ConvertPopup { get; } = new(UIConfig.UIActivityFightConvert);
        public VisualRes BoardRes { get; } = new(UIConfig.UIActivityFightMain);
        public VisualRes LoadingRes { get; } = new(UIConfig.UIActivityFightLoading);
        public VisualRes HelpRes { get; } = new(UIConfig.UIActivityFightHelp);
        public VisualRes MilestoneRes { get; } = new(UIConfig.UIActivityFightMilestone);
        #endregion

        #region 存档字段
        private int _detailID;
        private int _monsterHp;
        private int _hasCycleHint;
        private int _attackCount;
        #endregion

        #region IBoardArchive
        public FeatureEntry Feature => FeatureEntry.FeatureFight;
        public void FillBoardData(fat.gamekitdata.Merge data)
        {
            World?.Serialize(data);
        }
        public void SetBoardData(fat.gamekitdata.Merge data)
        {
            if (World != null) return;
            InitWorld(data);
        }
        #endregion

        #region IBoardEntry
        public string BoardEntryAsset()
        {
            BoardRes.visual.AssetMap.TryGetValue("boardEntry", out var res);
            return res;
        }

        #endregion
        #region ISpawnEffectWithTrail

        public string order_trail_key = "fat_guide:fx_common_trail.prefab";
        void ISpawnEffectWithTrail.AddTrail(MBItemView view, Tween tween)
        {
            var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.MiddleStatus);
            GameObjectPoolManager.Instance.CreateObject(order_trail_key, effRoot, trail =>
            {
                trail.SetActive(false);
                trail.transform.position = view.transform.position;
                var script = trail.GetOrAddComponent<MBAutoRelease>();
                script.Setup(order_trail_key, 4f);
                trail.transform.Find("particle/eff/glow03").gameObject.SetActive(true);
                if (tween.IsPlaying())
                {
                    var act = tween.onUpdate;
                    tween.OnUpdate(() =>
                    {
                        act?.Invoke();
                        if (!trail.activeSelf) { trail.SetActive(true); }
                        trail.transform.position = view.transform.position;
                    });
                    var act_complete = tween.onComplete;
                    tween.OnComplete(() =>
                    {
                        act_complete?.Invoke();
                        if (trail != null)
                        {
                            // 隐藏head粒子
                            trail.transform.Find("particle/eff/glow03").gameObject.SetActive(false);
                        }
                    });
                }
            });
        }
        #endregion

        #region ActivityLike
        public FightBoardActivity() { }

        public FightBoardActivity(ActivityLite lite)
        {
            Lite = lite;
            eventFight = Game.Manager.configMan.GetEventFightById(Param);
            RefreshTheme();
        }
        public override void Open()
        {
            EnterBoard();
        }
        public override void LoadSetup(ActivityInstance data_)
        {
            var i = 0;
            var AnyState = data_.AnyState;
            _detailID = ReadInt(i++, AnyState);
            _monsterHp = ReadInt(i++, AnyState);
            _hasCycleHint = ReadInt(i++, AnyState);
            _attackCount = ReadInt(i++, AnyState);
            LoadData();
        }
        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            any.Add(ToRecord(i++, _detailID));
            any.Add(ToRecord(i++, _monsterHp));
            any.Add(ToRecord(i++, _hasCycleHint));
            any.Add(ToRecord(i++, +_attackCount));
        }
        public override void SetupFresh()
        {
            InitData();
            EnterNextLevel();
            InitWorld(null);
            Game.Manager.screenPopup.TryQueue(StartPopup.popup, PopupType.Login);
        }
        public override void WhenEnd()
        {
            Game.Manager.screenPopup.TryQueue(EndPopup.popup, PopupType.Login);
            var reward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> rewardList);
            if (CollectAllBoardReward(rewardList) && rewardList.Count > 0)
            {
                Game.Manager.screenPopup.TryQueue(ConvertPopup.popup, PopupType.Login, reward);
            }
            else
            {
                reward.Free();
            }
            Cleanup();
        }
        private bool CollectAllBoardReward(List<RewardCommitData> rewards)
        {
            if (World == null || rewards == null)
                return false;
            using var _1 = PoolMapping.PoolMappingAccess.Borrow<Dictionary<int, int>>(out var itemIdMap);
            using var _2 = PoolMapping.PoolMappingAccess.Borrow<Dictionary<int, int>>(out var rewardMap);
            World.WalkAllItem((item) =>
            {
                ItemUtility.CollectRewardItem(item, itemIdMap, rewardMap);
            }, MergeWorld.WalkItemMask.NoInventory);
            foreach (var reward in rewardMap)
            {
                rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Key, reward.Value, ReasonString.use_item));
            }
            DataTracker.event_fight_end_collect.Track(this, ItemUtility.ConvertItemDictToString_Id_Num_Level(itemIdMap));
            return true;
        }
        public override void WhenReset()
        {
            Cleanup();
        }
        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            popup_.TryQueue(StartPopup.popup, state_);
        }
        #endregion

        #region IExternalOutput
        public bool CanUseItem(Item source)
        {
            return eventFight.Weapon == source.tid && monster.Hp > 0;
        }
        public bool TrySpawnItem(Item source, out int outputId, out ItemSpawnContext context)
        {
            outputId = -1;
            context = null;
            var com = source.GetItemComponent<ItemActiveSourceComponent>();
            if (com.WillDead)
            {
                var isNormalLevel = GetCurrentMilestoneIndex() <= eventFightDetail.Levels.Count - 1;
                var to = isNormalLevel ? UIFlyFactory.ResolveFlyTarget(FlyType.FightBoardMonster) : UIFlyFactory.ResolveFlyTarget(FlyType.FightBoardTreasure);
                var t = isNormalLevel ? FlyType.FightBoardMonster : FlyType.FightBoardTreasure;
                UIFlyUtility.FlyCustom(eventFight.AttackId, 3, BoardUtility.GetWorldPosByCoord(source.coord), to, FlyStyle.Reward, t, split: 3);
                monster.ReceiveAttack(eventFight.AttackNum, eventFight.AttackCritical, eventFight.AttackDamage[0], eventFight.AttackDamage[1]);
                DataTracker.event_fight_attack.Track(this, eventFight.Weapon, monster.AttackInfo.beforeHp - monster.AttackInfo.afterHp, monster.AttackInfo.damage.Count(x => x.Item2 == true),
                    phase, eventFightDetail.Levels.Count);
                _attackCount++;
                _monsterHp = monster.Hp;
                _BeginReward(com.DropCount);
                MessageCenter.Get<MSG.FIGHT_RECEIVE_ATTACK>().Dispatch(monster.AttackInfo);
                if (_monsterHp <= 0) EnterNextLevel();
                return true;
            }
            return false;
        }
        #endregion

        #region Activity
        /// <summary>
        /// 通过loading界面进入棋盘
        /// </summary>
        private void EnterBoard()
        {
            ActivityTransit.Enter(this, LoadingRes, BoardRes.res);
        }
        /// <summary>
        /// 通过loading界面离开活动棋盘
        /// </summary>
        private void LeaveBoard()
        {
            ActivityTransit.Exit(this, LoadingRes.res.ActiveR);
        }
        /// <summary>
        /// 第一次创建时初始化活动数据
        /// </summary>
        private void InitData()
        {
            _detailID = Game.Manager.userGradeMan.GetTargetConfigDataId(eventFight?.GradeId ?? 0);
            eventFightDetail = Game.Manager.configMan.GetEventFightDetailById(_detailID);
        }
        /// <summary>
        /// 从存档中加载数据
        /// </summary>
        private void LoadData()
        {
            eventFightDetail = Game.Manager.configMan.GetEventFightDetailById(_detailID);
            RefreshLevel();
            monster.SetMonsterData(_monsterHp, eventFightLevel, eventFight);
        }
        /// <summary>
        /// 刷新弹版和UI资源
        /// </summary>
        private void RefreshTheme()
        {
            StartPopup.Setup(eventFight.StartTheme, this, active_: false);
            EndPopup.Setup(eventFight.EndTheme, this, active_: false);
            ConvertPopup.Setup(eventFight.ExpirePopup, this, active_: false);
            BoardRes.Setup(eventFight.BoardTheme);
            LoadingRes.Setup(eventFight.LoadingTheme);
        }
        /// <summary>
        /// 里程碑进入下一个阶段
        /// </summary>
        private void EnterNextLevel()
        {
            phase++;
            RefreshLevel();
            RefreshMonsterData();
        }
        /// <summary>
        /// 刷新关卡信息
        /// </summary>
        private void RefreshLevel()
        {
            if (phase <= eventFightDetail.Levels.Count)
            {
                var level = eventFightDetail.Levels[phase - 1];
                eventFightLevel = Game.Manager.configMan.GetEventFightLevelById(level);
            }
            else
            {
                var cycle = phase - eventFightDetail.Levels.Count - 1;
                eventFightLevel = Game.Manager.configMan.GetEventFightLevelById(eventFightDetail.CircleLevels[cycle % eventFightDetail.CircleLevels.Count]);
            }
            _attackCount = 0;
            _itemOutputs.Clear();
            foreach (var item in eventFightLevel.AttackOutputs)
            {
                var (id, weight, _) = item.ConvertToInt3();
                _itemOutputs.Add((id, weight));
            }
        }
        /// <summary>
        /// 刷新monster信息
        /// </summary>
        private void RefreshMonsterData()
        {
            if (eventFightLevel == null) return;
            _monsterHp = eventFightLevel.Health;
            monster.SetMonsterData(_monsterHp, eventFightLevel, eventFight);
        }
        /// <summary>
        /// 初始化棋盘
        /// </summary>
        /// <param name="data"></param>
        private void InitWorld(fat.gamekitdata.Merge data)
        {
            var world_ = new MergeWorld();
            var tracer_ = new MergeWorldTracer(null, null);
            tracer_.Bind(world_);
            world_.BindTracer(tracer_);
            Game.Manager.mergeBoardMan.RegisterMergeWorldEntry(new MergeWorldEntry()
            {
                world = world_,
                type = MergeWorldEntry.EntryType.FightBoard,
            });
            var isFirstInit = data == null;
            Game.Manager.mergeBoardMan.InitializeBoard(world_, eventFightDetail.BoardId, isFirstInit);
            if (!isFirstInit)
            {
                world_.Deserialize(data, null);
            }
            else
            {
                var rewardMan = Game.Manager.rewardMan;
                foreach (var item in eventFightDetail.FreeItem)
                {
                    rewardMan.PushContext(new RewardContext() { targetWorld = World });
                    rewardMan.CommitReward(rewardMan.BeginReward(item, 1, ReasonString.fight_getitem));
                    rewardMan.PopContext();
                }
            }
            var handler = new FightBoardItemSpawnBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(handler);
            world_.RegisterActivityHandler(this);
            _world = world_;
            _tracer = tracer_;
            _spawnBonusHandler = handler;
        }
        /// <summary>
        /// 清理事件和棋盘
        /// </summary>
        private void Cleanup()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(_spawnBonusHandler);
            World?.UnregisterActivityHandler(this);
            Game.Manager.mergeBoardMan.UnregisterMergeWorldEntry(World);
            _world = null;
            _tracer = null;
        }
        /// <summary>
        /// 发奖逻辑，包含攻击奖励和阶段奖励
        /// </summary>
        private void _BeginReward(int output)
        {
            _attackRewards.Clear();
            _levelRewards.Clear();
            _BeginAttackReward(output);
            _BeginLevelReward();
        }
        /// <summary>
        /// 攻击奖励
        /// </summary>
        private void _BeginAttackReward(int output)
        {
            var context = ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Fight);
            context.spawnEffect = this;
            var origin = GetCurrentMilestoneIndex() < eventFightDetail.Levels.Count ? UIFlyFactory.ResolveFlyTarget(FlyType.FightBoardMonster) : UIFlyFactory.ResolveFlyTarget(FlyType.FightBoardTreasure);
            for (var index = 0; index < output; index++)
            {
                var result = _itemOutputs.RandomChooseByWeight(e => e.weight);
                BoardUtility.RegisterSpawnRequest(result.itemId, origin, 0.42f + 0.06f * index);
                var item = _world.activeBoard.TrySpawnItem(result.itemId, ItemSpawnReason.ActiveSource, context);
                if (item == null)
                {
                    BoardUtility.PopSpawnRequest();
                    var re = Game.Manager.rewardMan.BeginReward(result.itemId, 1, ReasonString.fight_attack);
                    _attackRewards.Add(re);
                }
                else
                {
                    var delay = 0.42f + 0.06f * index;
                    Game.Instance.StartCoroutineGlobal(CoPlaySound(delay));
                }
            }
            if (_attackRewards.Count > 0)
            {
                Game.Instance.StartCoroutineGlobal(CoDelayReward(origin));
            }
        }

        private IEnumerator CoPlaySound(float delay)
        {
            yield return new WaitForSeconds(delay);
            Game.Manager.audioMan.TriggerSound("BoardReward");
        }

        private IEnumerator CoDelayReward(Vector3 origin)
        {
            yield return new WaitForSeconds(0.42f);
            UIFlyUtility.FlyRewardList(_attackRewards, origin);
        }

        /// <summary>
        /// 阶段奖励
        /// </summary>
        private void _BeginLevelReward()
        {
            if (_monsterHp > 0) return;
            Game.Manager.rewardMan.PushContext(new RewardContext() { targetWorld = Game.Manager.mainMergeMan.world });
            foreach (var item in eventFightLevel.LevelReward)
            {
                var (id, count, _) = item.ConvertToInt3();
                var re = Game.Manager.rewardMan.BeginReward(id, count, ReasonString.fight_milestone);
                _levelRewards.Add(re);
            }
            MessageCenter.Get<MSG.FIGHT_LEVEL_REWARD>().Dispatch(_levelRewards, eventFightLevel);
            Game.Manager.rewardMan.PopContext();
            DataTracker.event_fight_milestone.Track(this, phase, eventFightDetail.Levels.Count, eventFightDetail.Diff, 1, phase == eventFightDetail.Levels.Count,
                _attackCount, phase - eventFightDetail.Levels.Count > 0 ? phase - eventFightDetail.Levels.Count : 1, eventFightLevel.Id);
        }
        #endregion

        #region 对外接口
        public void Exit()
        {
            LeaveBoard();
        }
        /// <summary>
        /// 获取当前里程碑进度文本
        /// </summary>
        /// <returns>当前关卡/总关卡</returns>
        public string GetMilestoneText()
        {
            return $"{I18N.Text("#SysComDesc193")} {ZString.Format("{0}/{1}", phase - 1 > eventFightDetail.Levels.Count ? eventFightDetail.Levels.Count : phase - 1, eventFightDetail.Levels.Count)}";
        }
        /// <summary>
        /// 获取所有关卡信息
        /// </summary>
        /// <returns></returns>
        public List<EventFightLevel> GetFightLevels()
        {
            var list = new List<EventFightLevel>();
            foreach (var info in eventFightDetail.Levels)
            {
                list.Add(Game.Manager.configMan.GetEventFightLevelById(info));
            }
            return list;
        }
        /// <summary>
        /// 获取当前关卡进度，从0开始
        /// </summary>
        /// <returns></returns>
        public int GetCurrentMilestoneIndex()
        {
            return phase - 1;
        }

        public bool CheckIsShowRedPoint(out int rpNum)
        {
            rpNum = 0;
            if (!Active || World == null) return false;
            rpNum = World.rewardCount;
            return true;
        }

        public void SetHasCycleHint(bool isShow)
        {
            _hasCycleHint = isShow ? 1 : 0;
        }

        public bool CanShowCycleHint()
        {
            return _hasCycleHint == 0;
        }
        #endregion
    }

    public class FightMonster
    {
        #region monster属性
        private int _maxHp;
        private int _hp;
        private int _id;
        private Monster _monsterConf;
        private MonsterTalk _talk;
        private int _gridHealth;
        private readonly List<string> _colorHealth = new();
        private readonly AttackInfo _attackInfo = new();
        #endregion

        #region 对外属性和接口
        /// <summary>
        /// monster资源字段
        /// </summary>
        public string monsterAsset => _monsterConf.AssetMonster;
        public string boxAsset => _monsterConf.AssetBox;
        /// <summary>
        /// 随机待机文本
        /// </summary>
        public string trickStr() => RandomTalk(_talk.Trick);
        /// <summary>
        /// 随机死亡文本
        /// </summary>
        public string diedStr() => RandomTalk(_talk.Died);
        /// <summary>
        /// 随机刷新文本
        /// </summary>
        public string appearStr() => RandomTalk(_talk.Appear);
        /// <summary>
        /// 随机脏话文本
        /// </summary>
        public string curseStr() => RandomTalk(_talk.Curse);
        /// <summary>
        /// 血量
        /// </summary>
        public int Hp => _hp;
        /// <summary>
        /// 最大血量
        /// </summary>
        public int MaxHp => _maxHp;
        /// <summary>
        /// 伤害信息
        /// </summary>
        public AttackInfo AttackInfo => _attackInfo;
        /// <summary>
        /// 刷新monster属性
        /// </summary>
        /// <param name="hp">当前血量</param>
        /// <param name="level">conf</param>
        public void SetMonsterData(int hp, EventFightLevel level, EventFight eventFight)
        {
            SetData(level);
            SetHealth(hp, level.Health, eventFight);
        }
        /// <summary>
        /// 获取最大血条格子数
        /// </summary>
        /// <returns>最大格子数量float</returns>
        public float GetMaxGrid()
        {
            return (float)_maxHp / _gridHealth;
        }
        /// <summary>
        /// 获取当前血量格子数量
        /// </summary>
        /// <returns></returns>
        public float GetCurrentGrid()
        {
            return (float)_hp / _gridHealth;
        }
        /// <summary>
        /// 获取当前血量的显示资源
        /// </summary>
        /// <returns></returns>
        public string GetCurrentColor()
        {
            foreach (var info in _colorHealth)
            {
                var infos = info.Split(':');
                {
                    float.TryParse(infos[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result);
                    if ((float)_hp / _maxHp <= result)
                        return infos[1];
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// 收到攻击调用函数
        /// </summary>
        /// <param name="attackNum">攻击次数</param>
        /// <param name="critical">暴击概率</param>
        /// <param name="minDamage">最小伤害</param>
        /// <param name="maxDamage">最大伤害</param>
        /// <returns></returns>
        public void ReceiveAttack(int attackNum, int critical, int minDamage, int maxDamage)
        {
            _attackInfo.ClearInfo();
            _ReceiveAttack(attackNum, critical, minDamage, maxDamage);
        }
        #endregion

        #region 内部逻辑
        private void SetData(EventFightLevel level)
        {
            _id = level.Monster;
            _monsterConf = Game.Manager.configMan.GetMonsterById(_id);
            _talk = Game.Manager.configMan.GetMonsterTalkById(_monsterConf.MonsterTalk);
        }

        private void SetHealth(int hp, int max, EventFight eventFight)
        {
            _hp = hp;
            _maxHp = max;
            _gridHealth = eventFight.GridHealth;
            _colorHealth.Clear();
            _colorHealth.AddRange(eventFight.ColorHealth);
        }

        private void _ReceiveAttack(int attackNum, int critical, int minDamage, int maxDamage)
        {
            _attackInfo.beforeHp = _hp;
            for (var i = 0; i < attackNum; i++)
            {
                var baseDamage = Random.Range(minDamage, maxDamage + 1);
                if (UIDebugPanelProMax.FightDebug) baseDamage = 9999;
                var isCritical = Random.Range(0f, 1f) <= (float)critical / 100;
                var realDamage = isCritical ? baseDamage * 2 : baseDamage;
                _attackInfo.damage.Add((realDamage, isCritical));
                _hp = _hp - realDamage >= 0 ? _hp - realDamage : 0;
            }
            _attackInfo.afterHp = _hp;
            _attackInfo.curseStr = curseStr();
            if (_hp <= 0) _attackInfo.diedStr = diedStr();
        }

        private string RandomTalk(IEnumerable<string> strings)
        {
            var random = Random.Range(0, strings.Count());
            return I18N.Text(strings.ElementAt(random));
        }
        #endregion
    }

    public class FightBoardEntry : ListActivity.IEntrySetup
    {
        public FightBoardEntry(ListActivity.Entry ent, FightBoardActivity act)
        {
            var showRedPoint = act.CheckIsShowRedPoint(out var redNum);
            ent.dot.SetActive(showRedPoint && redNum > 0);
            ent.dotCount.SetText(redNum.ToString());
            ent.dotCount.gameObject.SetActive(showRedPoint && redNum > 0);
        }

        public override void Clear(ListActivity.Entry e_)
        {
        }
    }

    public class AttackInfo
    {
        /// <summary>
        /// (int伤害数值，bool是否暴击)
        /// </summary>
        public readonly List<(int, bool)> damage = new();
        public int beforeHp;
        public int afterHp;
        /// <summary>
        /// 受击脏话文本
        /// </summary>
        public string curseStr = string.Empty;
        /// <summary>
        /// 受击死亡文本
        /// </summary>
        public string diedStr = string.Empty;
        /// <summary>
        /// 清理数据
        /// </summary>
        public void ClearInfo()
        {
            damage.Clear();
            beforeHp = 0;
            afterHp = 0;
            curseStr = string.Empty;
            diedStr = string.Empty;
        }
    }
}
