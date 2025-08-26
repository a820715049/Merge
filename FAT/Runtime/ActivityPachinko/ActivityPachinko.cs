/*
 *@Author:chaoran.zhang
 *@Desc:弹珠活动实例脚本
 *@Created Time:2024.12.04 星期三 20:18:36
 */

using System.Collections.Generic;
using System.Linq;
using Config;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using Random = UnityEngine.Random;

namespace FAT
{
    using static PoolMapping;

    #region 保险杠数据结构

    public class PachinkoBumper
    {
        public PachinkoBumper(int id, int energy)
        {
            Id = id;
            Energy = energy;
            InitOther();
        }

        private void InitOther()
        {
            var conf = Game.Manager.configMan.GetPachinkoBumperInfoByID(Id);
            MaxEnergy = conf.BumperEnergy;
            ExtraEnergy = conf.ExtraEnergy;
            ImpactEnergy = conf.ImpactEnergy;
            RewardConfig = conf.BumperReward.First().ConvertToRewardConfig();
        }

        public int Id { get; private set; } //对应BumperInfo表中的ID
        public int Energy { get; private set; } //当前能量值
        public int MaxEnergy { get; private set; } //最大能量值
        public int ExtraEnergy { get; private set; } //额外能量
        public int ImpactEnergy { get; private set; } //碰撞能量
        public RewardConfig RewardConfig { get; private set; } //奖励

        /// <summary>
        /// 撞击调用
        /// </summary>
        public int ImpactBumper()
        {
            var multiple = Game.Manager.pachinkoMan.GetMultiple();
            Energy += multiple * ImpactEnergy;
            return Energy >= MaxEnergy ? multiple * ImpactEnergy + ExtraEnergy : multiple * ImpactEnergy;
        }

        /// <summary>
        /// 当前保险杠进度是否已满
        /// </summary>
        /// <returns></returns>
        public bool CheckBumperComplete()
        {
            return Energy >= MaxEnergy;
        }

        public void ResetBumper(int id)
        {
            Id = id;
            Energy = 0;
            InitOther();
        }
    }

    #endregion

    public class ActivityPachinko : ActivityLike, IBoardEntry
    {
        public EventPachinkoRound ConfRound;
        public EventPachinko Conf;
        public EventPachinkoDetail ConfD;
        public bool HasEnd;
        public override bool Valid => Conf != null && ConfD != null;
        public override ActivityVisual Visual => MainVisual;
        private PachinkoSpawnBonusHandler _bonus;

        #region 存档数据

        private int _detailID; //EventPachinkoDetail的ID，因为分层会变化，所以存档
        private int _tokenEnergy; //获取代币的能量条进度
        private int _tokenNum; //代币数量
        private int _tokenPhase; //获取代币的阶段
        private int _energy; //里程碑能量
        private int _energyPhase; //里程碑奖励领取进度
        private List<PachinkoBumper> _bumpers = new();
        //本次活动是否弹过最后一档大奖tips
        private bool _isPopRewardTips;

        #endregion

        #region UI相关：弹板、换皮等

        //开启弹板相关
        public ActivityVisual StartVisual = new();
        public PopupActivity StartPopup = new();
        public UIResAlt StartResAlt = new(UIConfig.UIPachinkoStartNotice);

        //主界面相关
        public ActivityVisual MainVisual = new();
        public UIResAlt MainResAlt = new(UIConfig.UIPachinkoMain);

        //新一轮弹板相关
        public ActivityVisual RestartVisual = new();
        public PopupActivity RestartPopup = new();
        public UIResAlt RestartResAlt = new(UIConfig.UIPachinkoRestartNotice);

        //回收弹板相关
        public ActivityVisual ConvertVisual = new();
        public PopupActivity ConvertPopup = new();
        public UIResAlt ConvertResAlt = new(UIConfig.UIPachinkoConvert);

        //玩法界面
        public ActivityVisual HelpVisual = new();
        public UIResAlt HelpResAlt = new(UIConfig.UIPachinkoHelp);

        //Loading界面
        public ActivityVisual LoadingVisual = new();
        public UIResAlt LoadingResAlt = new(UIConfig.UIPachinkoLoading);

        private bool _hasPop; //用于防止SetupFresh和TryPop重复弹窗

        #endregion

        #region 通用逻辑

        public ActivityPachinko() { }

        public ActivityPachinko(ActivityLite lite)
        {
            Lite = lite;
            ConfRound = Game.Manager.configMan.GetPachinkoRoundByID(lite.Id);
            _bonus = new PachinkoSpawnBonusHandler(this);
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(_bonus);
        }

        public override void SetupFresh()
        {
            StartNewRound();
            TryAddToken(Conf.FreeTokenNum, ReasonString.free);
            Game.Manager.screenPopup.Queue(StartPopup);
            _hasPop = true;
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var i = 0;
            var any = data_.AnyState;
            any.Add(ToRecord(i++, _detailID));
            any.Add(ToRecord(i++, _tokenEnergy));
            any.Add(ToRecord(i++, _tokenNum));
            any.Add(ToRecord(i++, _tokenPhase));
            any.Add(ToRecord(i++, _energy));
            any.Add(ToRecord(i++, _energyPhase));
            foreach (var bumper in _bumpers)
            {
                any.Add(ToRecord(i++, bumper.Id));
                any.Add(ToRecord(i++, bumper.Energy));
            }
            any.Add(ToRecord(i, _isPopRewardTips));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var i = 0;
            _detailID = ReadInt(i++, data_.AnyState);
            _tokenEnergy = ReadInt(i++, data_.AnyState);
            _tokenNum = ReadInt(i++, data_.AnyState);
            _tokenPhase = ReadInt(i++, data_.AnyState);
            _energy = ReadInt(i++, data_.AnyState);
            _energyPhase = ReadInt(i++, data_.AnyState);
            //先加载配置
            LoadConf();
            //再依照配置决定 设置_bumpers时存档要读到哪个序号结束
            var str = ConfD.IncludeBumper;
            var infos = str.Split(":");
            var startIndex = i;
            var endIndex = i + infos.Length * 2;
            for (int j = startIndex; j < endIndex; j += 2)
            {
                var id = ReadInt(i++, data_.AnyState);
                var energy = ReadInt(i++, data_.AnyState);
                _bumpers.Add(new PachinkoBumper(id, energy));
            }
            //检查一下是否有新配置 有的话读取
            if (i < data_.AnyState.Count)
            {
                _isPopRewardTips = ReadBool(i, data_.AnyState);
            }
            RefreshVisual();
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (Countdown <= 0 || phase > 0 || _hasPop) return;
            popup_.TryQueue(StartPopup, state_);
        }

        public override void WhenEnd()
        {
            DataTracker.event_pachinko_end.Track(this, phase, _tokenNum);
            using var _ = PoolMappingAccess.Borrow(out Dictionary<int, int> map);
            map[Conf.TokenId] = _tokenNum;
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            ActivityExpire.ConvertToReward(Conf.ExpirePopup, list, ReasonString.pachinko_end, map);
            CheckNeedCloseWindow();
            Game.Manager.screenPopup.Queue(ConvertPopup, listT);
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(_bonus);
        }

        public override void WhenReset()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(_bonus);
        }

        public override void Open()
        {
            Game.Manager.pachinkoMan.EnterMainScene();
        }

        public string BoardEntryAsset()
        {
            MainVisual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        #endregion

        #region 外部接口

        public (int, int) GetScoreProgress()
        {
            var cycle = _tokenPhase >= ConfD.LevelScore.Count;
            var max = cycle ? ConfD.CycleLevelScore : ConfD.LevelScore[_tokenPhase];
            return (_tokenEnergy, max);
        }

        public RewardConfig GetScoreRewardConfig()
        {
            var cycle = _tokenPhase >= ConfD.LevelScore.Count;
            var str = cycle ? ConfD.CycleLevelToken : ConfD.LevelToken[_tokenPhase];
            return str.ConvertToRewardConfig();
        }

        /// <summary>
        /// 获取当前里程碑能量进度
        /// </summary>
        /// <returns></returns>
        public int GetEnergy()
        {
            return _energy;
        }

        /// <summary>
        /// 获取当前轮次里程碑最终奖励要求的能量进度
        /// </summary>
        /// <returns></returns>
        public int GetMaxEnergy()
        {
            if (ConfD == null || !ConfD.IncludeMilestone.Any()) return 0;
            return Game.Manager.configMan.GetPachinkoMilestoneByID(ConfD.IncludeMilestone[^1]).MilestoneEnergy;
        }

        /// <summary>
        /// 获取当前里程碑进度
        /// </summary>
        /// <returns></returns>
        public int GetCurMilestone()
        {
            return _energyPhase;
        }

        /// <summary>
        /// 获取代币数量
        /// </summary>
        /// <returns></returns>
        public int GetTokenNum()
        {
            return _tokenNum;
        }

        /// <summary>
        /// 获取所有保险杠信息
        /// </summary>
        /// <returns></returns>
        public List<PachinkoBumper> GetBumpers()
        {
            return _bumpers;
        }

        public PachinkoBumper GetBumperByIndex(int index)
        {
            return index >= _bumpers.Count ? null : _bumpers[index];
        }

        /// <summary>
        /// 加积分，判断是否能获得代币
        /// </summary>
        /// <param name="count"></param>
        public void TryAddScore(int count)
        {
            if (ConfD == null || !Valid || count < 0) return;
            _tokenEnergy += count;
            MessageCenter.Get<MSG.PACHINKO_SCORE_UPDATE>().Dispatch(count);
            CheckTokenPhaseChange();
        }

        /// <summary>
        /// 加代币
        /// </summary>
        /// <param name="count"></param>
        public void TryAddToken(int count, ReasonString reason)
        {
            if (!Valid || count <= 0) return;
            _tokenNum += count;
            DataTracker.token_change.Track(Conf.TokenId, count, _tokenNum, reason);
        }

        /// <summary>
        /// 增加里程碑能量
        /// </summary>
        /// <param name="count"></param>
        public void TryAddEnergy(int count)
        {
            if (!Valid || count < 0) return;
            _energy += count;
            CheckEnergyOver();
            CheckMilestoneReward();
            CheckFinalMilestoneReward();
            TryEnterNextRound();
        }

        /// <summary>
        /// 尝试消耗代币
        /// </summary>
        /// <param name="cost"></param>
        /// <returns></returns>
        public bool TrySpendToken(int cost)
        {
            if (!Valid) return false;
            if (_tokenNum < cost) return false;
            _tokenNum -= cost;
            DataTracker.token_change.Track(Conf.TokenId, -cost, _tokenNum, ReasonString.pachinko_use);
            return true;
        }

        public (int, RewardCommitData) TryImpactBumper(PachinkoBumper bumper)
        {
            return (bumper.ImpactBumper(),
                bumper.CheckBumperComplete() ? CompleteBumper(bumper) : null);
        }

        /// <summary>
        /// 刷新meta界面入口
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public ListActivity.IEntrySetup SetupEntry(ListActivity.Entry e)
        {
            e.dot.SetActive(_tokenNum > 0);
            e.dotCount.gameObject.SetActive(_tokenNum > 0);
            e.dotCount.SetRedPoint(_tokenNum);
            return null;
        }

        //配置上允许且还没有弹时 可以弹
        public bool GetCanPopRewardTips()
        {
            if (Conf == null)
                return false;
            return !_isPopRewardTips && Conf.TipPopAuto;
        }
        
        public void SetIsPopRewardTips()
        {
            _isPopRewardTips = true;
        }

        #endregion

        #region 内部处理逻辑

        /// <summary>
        /// 开启新一轮的数据处理
        /// </summary>
        private void StartNewRound()
        {
            ResetData();
            LoadConf();
            InitBumper();
            RefreshVisual();
            if (phase > 0) Game.Manager.screenPopup.Queue(RestartPopup); //第二轮开始主动弹出新一轮UI
        }

        /// <summary>
        /// 重置进度相关数据
        /// </summary>
        private void ResetData()
        {
            _detailID = 0;
            _tokenPhase = 0;
            _energy = 0;
            _energyPhase = 0;
            _bumpers.Clear();
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConf()
        {
            var man = Game.Manager.configMan;
            ConfRound ??= man.GetPachinkoRoundByID(Lite.Param);
            Conf = man.GetEventPachinkoByID(ConfRound.IncludePachinkoId[phase]);
            ConfD = man.GetEventPachinkoDetailByID(_detailID == 0
                ? Game.Manager.userGradeMan.GetTargetConfigDataId(Conf.GradeId)
                : _detailID);
            _detailID = ConfD.Id;
        }

        /// <summary>
        /// 目前没有保险杠数据时，初始化保险杠数据
        /// </summary>
        private void InitBumper()
        {
            var str = ConfD.IncludeBumper;
            var infos = str.Split(":");
            foreach (var info in infos)
            {
                var id = RandomBumper(info.Split(","));
                _bumpers.Add(new PachinkoBumper(id, 0));
            }
        }

        /// <summary>
        /// 随机一个保险杠配置，并返回id
        /// </summary>
        /// <param name="data">配置字符串</param>
        /// <returns>id</returns>
        private int RandomBumper(string[] data)
        {
            var list = GetAllBumper(data);
            var totalWeight = list.Sum(info => info.Weight);
            var random = Random.Range(1, totalWeight + 1);
            var weight = 0;
            foreach (var bumper in list)
            {
                weight += bumper.Weight;
                if (weight >= random) return bumper.Id;
            }

            return 0;
        }

        /// <summary>
        /// 获取到当前字符串所表示的保险杠随机池
        /// </summary>
        /// <param name="data">data</param>
        /// <returns>可以随机到到保险杠List</returns>
        private List<BumperInfo> GetAllBumper(string[] data)
        {
            var list = new List<BumperInfo>();
            foreach (var info in data)
            {
                if (!int.TryParse(info, out var id)) continue;
                var bumper = Game.Manager.configMan.GetPachinkoBumperInfoByID(id);
                if (bumper == null) continue;
                list.Add(bumper);
            }

            return list;
        }

        /// <summary>
        /// 获取保险杠奖励
        /// </summary>
        /// <param name="bumperInfo"></param>
        private RewardCommitData CompleteBumper(PachinkoBumper bumperInfo)
        {
            var commit = Game.Manager.rewardMan.BeginReward(bumperInfo.RewardConfig.Id, bumperInfo.RewardConfig.Count,
                ReasonString.pachinko_bumper);
            DataTracker.event_pachinko_bumper_reward.Track(this, bumperInfo.Id, Game.Manager.pachinkoMan.GetMultiple());
            var index = _bumpers.IndexOf(bumperInfo);
            var id = RandomBumper(ConfD.IncludeBumper.Split(":")[index].Split(","));
            bumperInfo.ResetBumper(id);
            return commit;
        }

        /// <summary>
        /// 刷新换皮
        /// </summary>
        private void RefreshVisual()
        {
            if (StartVisual.Setup(Conf.StartTheme, StartResAlt))
                StartPopup.Setup(this, StartVisual, StartResAlt);
            if (RestartVisual.Setup(Conf.RestartTheme, RestartResAlt))
                RestartPopup.Setup(this, RestartVisual, RestartResAlt);
            if (ConvertVisual.Setup(Conf.RecontinueTheme, ConvertResAlt))
                ConvertPopup.Setup(this, ConvertVisual, ConvertResAlt, false, false);
            MainVisual.Setup(Conf.PachinkoTheme, MainResAlt);
            HelpVisual.Setup(Conf.HelpPlayTheme, HelpResAlt);
            LoadingVisual.Setup(Conf.LoadingTheme, LoadingResAlt);
        }

        /// <summary>
        /// 获取转换奖励map
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, int> GetConvertMap()
        {
            var map = new Dictionary<int, int>();
            foreach (var (f, t) in Conf.ExpirePopup)
            {
                ConvertExchange(t, out var exchange, out var target);
                if (exchange <= 0 || f <= 0 || target <= 0) continue;

                map.TryGetValue(target, out var v);
                map[target] = v + _tokenNum * exchange / 10;
            }

            return map;
        }

        /// <summary>
        /// 读取过期物品转换配置
        /// </summary>
        /// <param name="str">配置str</param>
        /// <param name="exchange">转换比例</param>
        /// <param name="cost">过期物品id</param>
        /// <param name="target">转换目标id</param>
        private void ConvertExchange(string str, out int exchange, out int target)
        {
            var split1 = str.Split("=");
            int.TryParse(split1[1], out var _exchange);
            int.TryParse(split1[0], out var _target);
            exchange = _exchange;
            target = _target;
        }

        private void CheckEnergyOver()
        {
            var last = Game.Manager.configMan.GetPachinkoMilestoneByID(ConfD.IncludeMilestone.LastOrDefault());
            if (last == null) return;
            if (_energy >= last.MilestoneEnergy) _energy = last.MilestoneEnergy;
        }

        /// <summary>
        /// 每次获得代币能量之后调用，检测是否完成当前阶段
        /// </summary>
        private void CheckTokenPhaseChange()
        {
            var cycle = _tokenPhase >= ConfD.LevelScore.Count;
            var max = cycle ? ConfD.CycleLevelScore : ConfD.LevelScore[_tokenPhase];
            if (_tokenEnergy < max) return;
            var reward = cycle
                ? ConfD.CycleLevelToken.ConvertToRewardConfig()
                : ConfD.LevelToken[_tokenPhase].ConvertToRewardConfig();
            var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.pachinko_energy);
            Game.Manager.pachinkoMan.SetScoreCommit(commit);
            _tokenPhase++;
            _tokenEnergy -= max;
        }

        /// <summary>
        /// 每次获得里程碑积分之后检测是否有可以领取的里程碑奖励
        /// </summary>
        private void CheckMilestoneReward()
        {
            var curReach = CurReachMilestoneList();
            var needBegin = curReach.Skip(_energyPhase);
            var cur = _energyPhase;
            foreach (var str in needBegin)
            {
                if (str.MilestoneReward.Count > 1) continue;
                var config = str.MilestoneReward.First().ConvertToRewardConfig();
                if (config == null) continue;
                var commit =
                    Game.Manager.rewardMan.BeginReward(config.Id, config.Count, ReasonString.pachinko_milestone);
                DataTracker.event_pachinko_milestone.Track(this, ++cur, phase + 1, ConfD.IncludeMilestone.Count, ConfD.Diff, phase + 1, false);
                Game.Manager.pachinkoMan.AddWaitCommit(commit);
            }

            _energyPhase = curReach.Count;
        }

        /// <summary>
        /// 获取当前可以到达的里程碑等级
        /// </summary>
        /// <returns></returns>
        private List<EventPachinkoMilestone> CurReachMilestoneList()
        {
            return Enumerable.ToList(ConfD.IncludeMilestone
                .Select(id => Game.Manager.configMan.GetPachinkoMilestoneByID(id))
                .TakeWhile(info => info.MilestoneEnergy <= _energy));
        }

        /// <summary>
        /// 尝试领取最终奖励，每次里程碑进度增加时都调用
        /// </summary>
        private void CheckFinalMilestoneReward()
        {
            var last = Game.Manager.configMan.GetPachinkoMilestoneByID(ConfD.IncludeMilestone.LastOrDefault());
            if (last == null || _energy < last.MilestoneEnergy) return;
            DataTracker.event_pachinko_milestone.Track(this, _energyPhase, phase + 1, ConfD.IncludeMilestone.Count, ConfD.Diff, phase + 1, true);
            var rewardList = last.MilestoneReward.Select(str => str.ConvertToRewardConfig());
            var commit = Enumerable.ToList(rewardList.Select(reward =>
                Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.pachinko_milestone)));
            Game.Manager.pachinkoMan.AddFinalCommit(commit);
        }

        /// <summary>
        /// 尝试进入下一轮
        /// </summary>
        private void TryEnterNextRound()
        {
            var last = Game.Manager.configMan.GetPachinkoMilestoneByID(ConfD.IncludeMilestone.LastOrDefault());
            if (last == null || _energy < last.MilestoneEnergy) return;
            phase++;
            if (phase < ConfRound.IncludePachinkoId.Count)
            {
                StartNewRound();
                DataTracker.event_pachinko_restart.Track(this, phase);
            }
            else
            {
                HasEnd = true;
                Game.Manager.activity.EndImmediate(this, false);
            }
        }

        /// <summary>
        /// 判断是否需要活动实例来关闭各个UI
        /// </summary>
        private void CheckNeedCloseWindow()
        {
            if (Game.Manager.pachinkoMan.CheckRoundFinish()) return;
            UIManager.Instance.CloseWindow(StartResAlt.ActiveR);
            UIManager.Instance.CloseWindow(RestartResAlt.ActiveR);
            UIManager.Instance.CloseWindow(HelpResAlt.ActiveR);
            if (UIManager.Instance.IsOpen(MainResAlt.ActiveR)) Game.Manager.pachinkoMan.ExitMainScene();
        }

        #endregion
    }

    public class PachinkoEntry : ListActivity.IEntrySetup
    {
        public PachinkoEntry(ListActivity.Entry e)
        {
            e.dot.SetActive(Game.Manager.pachinkoMan.GetCoinCount() > 0);
            e.dotCount.gameObject.SetActive(Game.Manager.pachinkoMan.GetCoinCount() > 0);
            e.dotCount.SetRedPoint(Game.Manager.pachinkoMan.GetCoinCount());
        }

        public override void Clear(ListActivity.Entry e_)
        {
        }

        public override string TextCD(long diff_)
        {
            return UIUtility.CountDownFormat(diff_);
        }
    }
}