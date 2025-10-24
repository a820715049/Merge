using System.Collections.Generic;
using System.Linq;
using fat.conf;
using fat.rawdata;
using UnityEngine;

namespace FAT
{
    public class RaceExtendPlayerBase
    {
        /// <summary>
        /// 头像资源路径
        /// </summary>
        public string avatar;
        /// <summary>
        /// 排名，未完成时排名为-1
        /// </summary>
        public int ranking;
        /// <summary>
        /// UI做动画表现使用的上次分数
        /// </summary>
        public int showScore;
        /// <summary>
        /// 实时更新的当前真实分数
        /// </summary>
        public int curScore;
        /// <summary>
        /// 最大分数
        /// </summary>
        public int maxScore;
        /// <summary>
        /// 是否已经完成
        /// </summary>
        public bool hasComplete => curScore >= maxScore;

        public void RefreshLastShowScore()
        {
            showScore = curScore;
        }
    }

    public class RaceExtendPlayer : RaceExtendPlayerBase
    {
        public void Reset()
        {
            showScore = 0;
            curScore = 0;
            ranking = 0;
            maxScore = 0;
        }
    }
    /// <summary>
    /// 热气球拓展活动玩家类，机器人和玩家都是这个类型
    /// </summary>
    public class RaceExtendRobot : RaceExtendPlayerBase
    {
        public int avatarID { get; private set; }
        public int RobotID { get; private set; }
        public long nextUpdateTime { get; private set; }
        public float addScoreLeft { get; private set; }
        public fat.rawdata.RaceExtendRobot robotConfig { get; private set; }
        public bool playerEnable => robotConfig != null;

        private const int TIME_UNIT_CONVERSION = 10; // 配置中时间单位为100ms，换算为秒
        private const float SCORE_NORMALIZATION_FACTOR = 0.01f; // 配置中分数单位为100，换算为小数

        public RaceExtendRobot()
        {
        }

        public RaceExtendRobot(int id = 0)
        {
            RobotID = id;
            robotConfig = RaceExtendRobotVisitor.Get(RobotID);
        }

        public void CheckUpdateSecond()
        {
            if (!playerEnable) { return; }
            if (hasComplete) { return; }
            nextUpdateTime = nextUpdateTime == 0 ? Game.Instance.GetTimestampLocal() + Random.Range(robotConfig.Online[0], robotConfig.Online[1] + 1) * TIME_UNIT_CONVERSION : nextUpdateTime;
            while (nextUpdateTime <= Game.Instance.GetTimestampLocal())
            {
                addScoreLeft += Random.Range(robotConfig.AddScore[0], robotConfig.AddScore[1] + 1) * SCORE_NORMALIZATION_FACTOR;
                curScore += (int)addScoreLeft;
                if (curScore >= maxScore) { break; }
                addScoreLeft -= (int)addScoreLeft;
                nextUpdateTime += Random.Range(robotConfig.Online[0], robotConfig.Online[1] + 1) * TIME_UNIT_CONVERSION;
            }
        }

        /// <summary>
        /// 添加离线分数
        /// </summary>
        /// <param name="offlineTime">离线时间</param>
        public void AddOfflineScore(long offlineTime)
        {
            if (!playerEnable) { return; }
            if (hasComplete) { return; }
            var interval = Random.Range(robotConfig.Offline[0], robotConfig.Offline[1] + 1);
            while (offlineTime >= interval)
            {
                addScoreLeft += Random.Range(robotConfig.OfflineAddScore[0], robotConfig.OfflineAddScore[1] + 1) * SCORE_NORMALIZATION_FACTOR;
                interval += Random.Range(robotConfig.Offline[0], robotConfig.Offline[1] + 1);
            }
            curScore += (int)addScoreLeft;
            addScoreLeft = 0;
        }

        public void SetAvatar(int id)
        {
            if (!playerEnable) { return; }
            avatarID = id;
            var robotIcon = RaceExtendRobotIconVisitor.Get(id);
            if (robotIcon == null) return;
            avatar = robotIcon.Icon;
        }
    }

    public class RaceExtendRoundManager
    {
        public readonly RaceExtendPlayer myself = new();
        public readonly List<RaceExtendRobot> robots = new();
        public RaceExtendRound round { get; private set; }

        #region interfaces

        /// <summary>
        /// 初始化比赛
        /// </summary>
        /// <param name="config">当前回合配置</param>
        /// <returns>当机器人列表中有一个以上是有效的，就判断为初始化成功，目的是防止因为个别配置错误导致活动进行不了</returns>
        public bool TryStartRaceRound(RaceExtendRound config)
        {
            FreshData(config);
            if (!_TryInitRobotPlayers()) { return false; }
            return true;
        }

        /// <summary>
        /// 加载玩家分数
        /// </summary>
        /// <param name="score">玩家分数</param>
        public void LoadMyself(int score)
        {
            myself.curScore = score;
        }

        /// <summary>
        /// 加载机器人
        /// </summary>
        /// <param name="id">机器人ID</param>
        /// <param name="score">机器人分数</param>
        /// <param name="avatarID">机器人头像ID</param>
        public void LoadRobot(int id, int score, int avatarID)
        {
            var robot = new RaceExtendRobot(id) { curScore = score };
            robot.SetAvatar(avatarID);
            robots.Add(robot);
        }

        /// <summary>
        /// UI动画播放完之后调用，刷新UI显示积分
        /// </summary>
        public void RefreshLastShowScore()
        {
            myself.RefreshLastShowScore();
            robots.Where(data => data.playerEnable).ToList().ForEach(robot => robot.RefreshLastShowScore());
        }

        /// <summary>
        /// 每秒更新机器人分数
        /// </summary>
        public void UpdateRobotScoreSecond()
        {
            foreach (var robot in robots) { robot.CheckUpdateSecond(); }
            _UpdateRankings();
        }

        /// <summary>
        /// 更新玩家自己的分数
        /// </summary>
        /// <param name="score"></param>
        public void UpdateMyself(int score)
        {
            myself.curScore += score;
            _UpdateRankings();
        }

        /// <summary>
        /// 添加离线分数
        /// </summary>
        /// <param name="offlineTime">离线时间</param>
        public void AddOfflineScore(long offlineTime)
        {
            foreach (var robot in robots) { robot.AddOfflineScore(offlineTime); }
            _UpdateRankings();
        }

        #endregion

        #region logic

        /// <summary>
        /// 刷新数据
        /// </summary>
        /// <param name="config">当前回合配置</param>
        private void FreshData(RaceExtendRound config)
        {
            round = config;
            myself.Reset();
            robots.Clear();
        }

        /// <summary>
        /// 初始化机器人玩家
        /// </summary>
        /// <returns>是否初始化成功</returns>
        private bool _TryInitRobotPlayers()
        {
            foreach (var id in round.RobotId) { LoadRobot(id, 0, 0); }
            _SetRobotAvatar();
            var randomRobots = robots.OrderBy(x => Random.Range(0, int.MaxValue)).ToList();
            robots.Clear();
            robots.AddRange(randomRobots);
            return robots.Any(data => data.playerEnable);
        }

        /// <summary>
        /// 随机设置机器人头像
        /// </summary>
        private void _SetRobotAvatar()
        {
            var allAvatarIds = RaceExtendRobotIconVisitor.All().Keys.ToList();
            if (allAvatarIds.Count == 0) { return; }

            var enabledRobots = robots.Where(robot => robot.playerEnable).ToList();
            if (enabledRobots.Count == 0) { return; }

            if (allAvatarIds.Count < enabledRobots.Count)
            {
                _AssignRandomAvatarsWithReuse(allAvatarIds, enabledRobots);
            }
            else
            {
                _AssignUniqueRandomAvatars(allAvatarIds, enabledRobots);
            }
        }

        /// <summary>
        /// 为机器人分配随机头像（允许重复使用）
        /// </summary>
        private void _AssignRandomAvatarsWithReuse(List<int> allAvatarIds, List<RaceExtendRobot> enabledRobots)
        {
            foreach (var robot in enabledRobots)
            {
                var randomIndex = Random.Range(0, allAvatarIds.Count);
                robot.SetAvatar(allAvatarIds[randomIndex]);
            }
        }

        /// <summary>
        /// 为机器人分配唯一随机头像
        /// </summary>
        private void _AssignUniqueRandomAvatars(List<int> allAvatarIds, List<RaceExtendRobot> enabledRobots)
        {
            var shuffledAvatarIds = _ShuffleAvatarIds(allAvatarIds);
            _AssignAvatarsToRobots(shuffledAvatarIds, enabledRobots);
        }

        /// <summary>
        /// 洗牌头像ID列表
        /// </summary>
        private List<int> _ShuffleAvatarIds(List<int> avatarIds)
        {
            var shuffled = new List<int>(avatarIds);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            return shuffled;
        }

        /// <summary>
        /// 将头像分配给机器人
        /// </summary>
        private void _AssignAvatarsToRobots(List<int> avatarIds, List<RaceExtendRobot> enabledRobots)
        {
            for (int i = 0; i < enabledRobots.Count; i++)
            {
                enabledRobots[i].SetAvatar(avatarIds[i]);
            }
        }

        /// <summary>
        /// 更新所有玩家的排名
        /// </summary>
        private void _UpdateRankings()
        {
            var enabledRobots = robots.Where(data => data.playerEnable);
            var allPlayers = new List<RaceExtendPlayerBase> { myself };
            allPlayers.AddRange(enabledRobots);
            allPlayers.Sort(_SortPlayer);

            for (var i = 0; i < allPlayers.Count; i++)
            {
                allPlayers[i].ranking = i;
            }
        }

        /// <summary>
        /// 排序玩家
        /// </summary>
        /// <param name="a">玩家a</param>
        /// <param name="b">玩家b</param>
        /// <returns>排序结果</returns>
        private int _SortPlayer(RaceExtendPlayerBase a, RaceExtendPlayerBase b)
        {
            // 1. 已完成的排在未完成的之前
            if (a.hasComplete != b.hasComplete) { return a.hasComplete ? -1 : 1; }
            // 2. 都为已完成的话，则ranking小的排在前
            if (a.hasComplete && a.ranking != b.ranking) { return a.ranking.CompareTo(b.ranking); }
            // 3. 分数高的排在前
            return b.curScore.CompareTo(a.curScore);
        }

        #endregion
    }
}