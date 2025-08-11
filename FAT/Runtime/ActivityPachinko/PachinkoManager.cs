/*
 *@Author:chaoran.zhang
 *@Desc:弹珠掉落活动管理类
 *@Created Time:2024.12.09 星期一 11:05:20
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Text;
using EL;
using fat.rawdata;
using Random = UnityEngine.Random;

namespace FAT
{
    public class PachinkoManager : IGameModule
    {
        public bool Valid => _activity != null;
        public int EnergyID => _activity?.Conf.EnergyId ?? 0;
        public int TokenID => _activity?.Conf.TokenId ?? 0;
        public int ScoreID => _activity?.Conf.RequireScoreId ?? 0;
        public UIResource MainUI;
        public UIResource LoadingUI;

        private bool _isPlaying;
        private int _totalEnergy;
        private int _multiple = 1;
        private int _dropIndex;
        private int _impactNum;
        private readonly List<EventPachinkoMilestone> _energyMilestone = new();
        private readonly List<int> _dropRange = new();
        private readonly Dictionary<int, (List<PachinkoDropInfo>, float)> _dropInfos = new();
        private readonly List<RewardCommitData> _waitCommit = new();
        private readonly List<RewardCommitData> _finalCommit = new();
        private RewardCommitData _scoreCommit;

        #region 数据获取接口

        /// <summary>
        /// 获得图文混排时，能量使用的key
        /// </summary>
        /// <returns></returns>
        public string GetEnergyIcon()
        {
            return _activity == null ? null : Game.Manager.objectMan.GetTokenConfig(_activity.Conf.EnergyId).SpriteName;
        }

        /// <summary>
        /// 获得图文混排时，代币使用的key
        /// </summary>
        /// <returns></returns>
        public string GetTokenIcon()
        {
            return _activity == null ? null : Game.Manager.objectMan.GetTokenConfig(_activity.Conf.TokenId).SpriteName;
        }

        public ActivityPachinko GetActivity()
        {
            return _activity;
        }

        public bool GetPlayState()
        {
            return _isPlaying;
        }

        /// <summary>
        /// 获取当前累计能量
        /// </summary>
        /// <returns></returns>
        public int GetEnergy()
        {
            return _activity?.GetEnergy() ?? 0;
        }

        /// <summary>
        /// 获取当前里程碑需要的最大能量
        /// </summary>
        /// <returns></returns>
        public int GetMaxEnergy()
        {
            return _activity?.GetMaxEnergy() ?? 0;
        }

        /// <summary>
        /// /获取当前里程碑完成进度
        /// </summary>
        /// <returns></returns>
        public int GetCurMilestonePhase()
        {
            return _activity == null || _finalCommit.Count > 0 ? _energyMilestone.Count : _activity.GetCurMilestone();
        }

        /// <summary>
        /// 获取当前里程碑的档位总数
        /// </summary>
        /// <returns></returns>
        public int GetCurMilestoneCount()
        {
            return _energyMilestone.Count;
        }

        /// <summary>
        /// 获取当前倍率
        /// </summary>
        /// <returns>倍率</returns>
        public int GetMultiple()
        {
            return _multiple;
        }

        /// <summary>
        /// 当前倍率是否为最大倍率
        /// </summary>
        /// <returns></returns>
        public bool IsMaxMultiple()
        {
            var coin = GetCoinCount();
            var list = Enumerable.ToList(Game.Manager.configMan.GetPachinkoMultipleList()
                .Where(info => info.Multiple * 4 <= coin));
            if (!list.Any()) return true;
            return _multiple >= list[^1].Multiple;
        }

        /// <summary>
        /// 获取当前代币数量
        /// </summary>
        /// <returns>代币数量</returns>
        public int GetCoinCount()
        {
            return _activity?.GetTokenNum() ?? 0;
        }

        /// <summary>
        /// 获取进度里程碑奖励
        /// </summary>
        /// <returns>奖励List</returns>
        public List<EventPachinkoMilestone> GetMilestone()
        {
            return _energyMilestone;
        }

        /// <summary>
        /// 获取所有保险杠信息
        /// </summary>
        /// <returns>保险杠信息List</returns>
        public List<PachinkoBumper> GetBumpers()
        {
            return _activity?.GetBumpers();
        }

        /// <summary>
        /// 根据序号获取保险杠信息
        /// </summary>
        /// <param name="index">想要获取的保险杠信息的序号</param>
        /// <returns>保险杠信息，若传入的index序号非法，则返回NULL</returns>
        public PachinkoBumper GetBumperByIndex(int index)
        {
            return _activity?.GetBumperByIndex(index);
        }

        /// <summary>
        /// 获取结果档位List,已经乘上档位
        /// </summary>
        /// <returns>int类型的List</returns>
        public List<int> GetDropRange()
        {
            return Enumerable.ToList(_dropRange.Select(reward => reward * _multiple));
        }

        /// <summary>
        /// 获取本轮当前获得的Energy总量
        /// </summary>
        /// <returns></returns>
        public int GetTotalEnergyGet()
        {
            return _totalEnergy;
        }

        /// <summary>
        /// 获取本次游戏获得的里程碑奖励（不包含最终奖励）
        /// </summary>
        /// <returns></returns>
        public List<RewardCommitData> GetMilestoneRewardCommitData()
        {
            return _waitCommit;
        }

        /// <summary>
        /// 获取本轮里程碑最终奖励
        /// </summary>
        /// <returns></returns>
        public List<RewardCommitData> GetFinalMilestoneRewardCommitData()
        {
            return _finalCommit;
        }

        /// <summary>
        /// 获取当前积分里程碑进度
        /// </summary>
        /// <returns></returns>
        public (int, int) GetScoreProgress()
        {
            return _activity?.GetScoreProgress() ?? (0, 0);
        }

        /// <summary>
        /// 获取当前积分奖励
        /// </summary>
        /// <returns></returns>
        public RewardConfig GetScoreReward()
        {
            return _activity.GetScoreRewardConfig();
        }

        #endregion

        #region 逻辑功能接口

        /// <summary>
        /// 尝试主动切换当前倍率并返回
        /// </summary>
        public void TryChangeCoinRate()
        {
            if (_activity == null) return;
            var coin = GetCoinCount();
            var list = Enumerable.ToList(Game.Manager.configMan.GetPachinkoMultipleList()
                .Where(info => info.Multiple * 4 <= coin));
            if (!list.Any()) return;
            var multiple = list.FirstOrDefault(info => info.Multiple > _multiple);
            if (multiple != null) _multiple = multiple.Multiple;
            else if (list.Any(info => info.Multiple == _multiple)) _multiple = 1;
            else _multiple = list.FindLast(info => info.Multiple < _multiple).Multiple;
        }

        /// <summary>
        /// 尝试开始游戏
        /// </summary>
        /// <param name="index">孔道序号，从0开始</param>
        /// <param name="offset">初始偏移</param>
        /// <param name="v">初始速度</param>
        /// <param name="angle">初始角度</param>
        /// <returns>是否可以开始游戏</returns>
        public bool TryStartGame(int index, out float offset, out float v, out float angle)
        {
            _totalEnergy = 0;
            offset = 0;
            v = 0;
            angle = 0;
            if (_isPlaying) return false;
            if (!_activity.TrySpendToken(_multiple))
            {
                Game.Manager.commonTipsMan.ShowClientTips(ZString.Format(I18N.Text("#SysComDesc725"),
                    ZString.Format("<sprite name=\"{0}\">", Game.Manager.pachinkoMan.GetTokenIcon())));
                UIManager.Instance.OpenWindow(UIConfig.UIPachinkoHelp);
                return false;
            }

            _waitCommit.Clear();
            var drop = RandomDrop(index);
            if (drop == null) return false;
            _dropIndex = index;
            offset = drop.Offset;
            v = drop.Velocity;
            angle = drop.Angle;
            _isPlaying = true;
            return true;
        }

        /// <summary>
        /// 撞击保险杠时调用，返回撞击后获得的奖励
        /// </summary>
        /// <param name="index">保险杠序号</param>
        /// <returns>撞击获得的能量,进度条满时获得的奖励(如果此次撞击没有填满能量，则返回NULL)</returns>
        public (int, RewardCommitData) ImpactBumper(int index)
        {
            var bumper = _activity?.GetBumperByIndex(index);
            var result = bumper == null
                ? (0, null)
                : _activity.TryImpactBumper(bumper);
            _totalEnergy += result.Item1;
            _impactNum++;
            return result;
        }

        /// <summary>
        /// 获取弹珠落入出口后的结果
        /// </summary>
        /// <param name="index">出口序号</param>
        /// <returns></returns>
        public void WhenDrop(int index)
        {
            var dropEnergy = 0;
            if (index < _dropRange.Count) dropEnergy += _dropRange[index] * _multiple;
            _totalEnergy += dropEnergy;
            DataTracker.event_pachinko_drop.Track(_activity, _multiple, _dropIndex + 1, index + 1, _impactNum,
                _totalEnergy);
            Game.Manager.rewardMan.BeginReward(_activity.Conf.EnergyId, _totalEnergy,
                ReasonString.pachinko_energy);
            _isPlaying = false;
            _impactNum = 0;
            _dropIndex = 0;
            CheckMultiple();
        }

        /// <summary>
        /// 获取活动代币时调用
        /// </summary>
        /// <param name="id">代币id</param>
        /// <param name="count">代币数量</param>
        public void TryAddToken(int id, int count, ReasonString reason)
        {
            var conf = _activity?.Conf;
            if (conf == null) return;

            if (id == conf.TokenId) _activity?.TryAddToken(count, reason);
            if (id == conf.RequireScoreId) _activity?.TryAddScore(count);
            if (id == conf.EnergyId) _activity?.TryAddEnergy(count);
        }

        /// <summary>
        /// 添加里程碑奖励
        /// </summary>
        /// <param name="data"></param>
        public void AddWaitCommit(RewardCommitData data)
        {
            _waitCommit.Add(data);
        }

        /// <summary>
        /// 添加最终奖励
        /// </summary>
        /// <param name="data"></param>
        public void AddFinalCommit(List<RewardCommitData> data)
        {
            _finalCommit.Clear();
            _finalCommit.AddRange(data);
        }


        /// <summary>
        /// 当前轮次是否完成
        /// </summary>
        /// <returns></returns>
        public bool CheckRoundFinish()
        {
            return _finalCommit.Count > 0;
        }

        /// <summary>
        /// 调用轮次完成获得大奖等后续表现
        /// </summary>
        public void FinishRound()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIPachinkoLevelReward);
        }

        /// <summary>
        /// 尝试刷新下一轮的数据
        /// </summary>
        public void GetNextRoundData()
        {
            if (_activity?.HasEnd ?? true) return;
            RefreshData();
        }

        /// <summary>
        /// 提交积分里程碑奖励
        /// </summary>
        /// <param name="data"></param>
        public void SetScoreCommit(RewardCommitData data)
        {
            _scoreCommit = data;
        }

        /// <summary>
        /// 获取当前积分里程碑奖励
        /// </summary>
        /// <returns></returns>
        public RewardCommitData GetScoreRewardCommit()
        {
            return _scoreCommit;
        }

        /// <summary>
        /// 进入游戏场景
        /// </summary>
        public void EnterMainScene()
        {
            if (!_activity?.Valid ?? true) return;
            UIManager.Instance.ChangeIdleActionState(false);
            Game.Manager.screenPopup.Block(true);
            Game.Instance.StartCoroutineGlobal(_CoLoading(_MergeToActivity, _AfterEnterFadeOut));
        }

        /// <summary>
        /// 退出游戏场景
        /// </summary>
        public void ExitMainScene()
        {
            Game.Instance.StartCoroutineGlobal(_CoLoading(_ActivityToMerge));
        }

        private void _AfterEnterFadeOut()
        {
            //进界面loading结束时发消息
            MessageCenter.Get<MSG.UI_PACHINKO_LOADING_END>().Dispatch();
        }

        #endregion

        #region 活动开始和结束相关

        public GroupPachinko Group = new();
        private ActivityPachinko _activity;

        public void AddActivity(ActivityPachinko activity)
        {
            if (activity == null) return;
            _activity = activity;
            RefreshData();
        }

        public void EndActivity(ActivityPachinko activity)
        {
            if (activity == null) return;
            if (activity.Id != _activity.Id) return;
            _activity = null;
        }

        #endregion

        #region 内部逻辑处理

        private void RefreshData()
        {
            RefreshDropInfo();
            RefreshDropRange();
            RefreshMilestone();
            _isPlaying = false;
            _totalEnergy = 0;
            _multiple = 1;
            if (_activity != null) MainUI = _activity.MainResAlt.ActiveR;
            if (_activity != null) LoadingUI = _activity.LoadingResAlt.ActiveR;
            _waitCommit.Clear();
            _finalCommit.Clear();
            _scoreCommit = null;
        }

        /// <summary>
        /// 刷新DropInfo信息
        /// </summary>
        private void RefreshDropInfo()
        {
            _dropInfos.Clear();
            if (_activity?.ConfD == null) return;
            var index = 0;
            foreach (var drop in _activity.ConfD.IncludeDropButton)
            {
                var info = Game.Manager.configMan.GetPachinkoDropInfoByID(drop);
                var list = Enumerable.ToList(info.ButtonOffset.Select(x => new PachinkoDropInfo(x)));
                var totalWeight = list.Sum(x => x.Weight);
                if (totalWeight > 0) _dropInfos.Add(index++, (list, totalWeight));
            }
        }

        public PachinkoDropInfo RandomDrop(int index)
        {
            _dropInfos.TryGetValue(index, out var dropInfo);
            var list = dropInfo.Item1;
            if (list == null) return null;
            var total = 0f;
            var weight = Random.Range(0, dropInfo.Item2);
            foreach (var info in list)
            {
                total += info.Weight;
                if (total >= weight) return info;
            }

            return null;
        }

        private void RefreshDropRange()
        {
            _dropRange.Clear();
            if (_activity?.ConfD == null) return;
            _dropRange.AddRange(_activity.ConfD.IncludeDropRange);
        }

        private void RefreshMilestone()
        {
            _energyMilestone.Clear();
            if (_activity?.ConfD == null) return;
            var milestones =
                _activity.ConfD.IncludeMilestone.Select(id => Game.Manager.configMan.GetPachinkoMilestoneByID(id));
            _energyMilestone.AddRange(milestones);
        }

        /// <summary>
        /// 根据当前代币数量检测是否应该自动切换
        /// </summary>
        private void CheckMultiple()
        {
            if (_activity == null) return;
            var coin = GetCoinCount();
            if (coin >= _multiple) return;
            var list = Enumerable.ToList(Game.Manager.configMan.GetPachinkoMultipleList()
                .Where(info => info.Multiple * 4 <= coin));
            if (!list.Any())
            {
                _multiple = 1;
                return;
            }

            var multiple = list.FirstOrDefault(info => info.Multiple >= _multiple);
            if (multiple != null) return;
            _multiple = list.FindLast(info => info.Multiple < _multiple).Multiple;
        }

        private bool _isEnterFromMerge;
        private bool _isLoading;

        private void _MergeToActivity(SimpleAsyncTask task)
        {
            _isEnterFromMerge = UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain);

            if (_isEnterFromMerge)
                UIConfig.UIMergeBoardMain.Close();
            else
                Game.Manager.mapSceneMan.Exit();

            UIManager.Instance.OpenWindow(MainUI, task);
        }

        private void _ActivityToMerge(SimpleAsyncTask task)
        {
            UIManager.Instance.CloseWindow(MainUI);

            if (_isEnterFromMerge)
                UIConfig.UIMergeBoardMain.Open();
            else
                Game.Manager.mapSceneMan.Enter(null);

            task.ResolveTaskSuccess();
            // if (!act.HasNextRound())
            //     act.TryExchangeExpireKey();
            UIManager.Instance.ChangeIdleActionState(true);
            Game.Manager.screenPopup.Block(false, false);
        }

        private IEnumerator _CoLoading(Action<SimpleAsyncTask> afterFadeIn = null, Action afterFadeOut = null)
        {
            _isLoading = true;

            var waitFadeInEnd = new SimpleAsyncTask();
            var waitFadeOutEnd = new SimpleAsyncTask();
            var waitLoadingJobFinish = new SimpleAsyncTask();
            //复用寻宝loading音效
            Game.Manager.audioMan.TriggerSound("UnderseaTreasure");

            UIManager.Instance.OpenWindow(LoadingUI, waitLoadingJobFinish, waitFadeInEnd,
                waitFadeOutEnd);

            yield return waitFadeInEnd;

            afterFadeIn?.Invoke(waitLoadingJobFinish);

            yield return waitFadeOutEnd;

            afterFadeOut?.Invoke();

            _isLoading = false;
        }

        #endregion


        public void Reset()
        {
            _isPlaying = false;
            _totalEnergy = 0;
            _activity = null;
            _multiple = 1;
            _energyMilestone.Clear();
            _waitCommit.Clear();
            _finalCommit.Clear();
            _scoreCommit = null;
        }

        public void LoadConfig()
        {
        }

        public void Startup()
        {
        }
    }

    public class PachinkoDropInfo
    {
        public float Offset;
        public float Velocity;
        public float Angle;
        public float Weight;

        public PachinkoDropInfo(string str)
        {
            var info = str.Split(":");
            if (info.Length < 4) return;
            float.TryParse(info[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out Offset);
            float.TryParse(info[1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out Velocity);
            float.TryParse(info[2], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out Angle);
            float.TryParse(info[3], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out Weight);
        }

        public override string ToString()
        {
            return string.Concat("Offset = ", Offset, " Velocity = ", Velocity, " Angle = ", Angle, " Weight = ",
                Weight);
        }
    }
}