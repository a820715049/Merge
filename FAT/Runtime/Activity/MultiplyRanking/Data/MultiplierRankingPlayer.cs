/*
 * @Author: chaoran.zhang
 * @Date: 2025-07-21 17:46:21
 * @LastEditors: chaoran.zhang
 * @LastEditTime: 2025-09-11 10:43:49
 */
using System.Linq;
using fat.rawdata;
using UnityEngine;

namespace FAT
{
    public class MultiplierRankingPlayerData
    {
        /// <summary>
        /// 名称，机器人显示用
        /// </summary>
        public int id;

        /// <summary>
        /// 是否为机器人，玩家自己为false
        /// </summary>
        public bool isBot;

        /// <summary>
        /// 当前积分
        /// </summary>
        public int score;

        /// <summary>
        /// 当前排名
        /// </summary>
        public int ranking;

        /// <summary>
        /// 是否为玩家自己
        /// </summary>
        public bool IsPlayer => !isBot;

        /// <summary>
        /// 奖励id
        /// </summary>
        public int rewardID;

        public MultiplierRankingPlayerData(MultiplierRankingPlayerData data)
        {
            id = data.id;
            isBot = data.isBot;
            score = data.score;
            ranking = data.ranking;
            rewardID = data.rewardID;
        }

        public MultiplierRankingPlayerData() { }
    }

    public class MultiplierRankingPlayer
    {
        public readonly MultiplierRankingPlayerData data = new();
        public MultiRankRobotDetail detail;
        public int configID;
        public bool isWaiting;
        public bool isChasing;
        public long nextUpdateTime;
        public int updateCount;
        public bool needPrepare = true;
        public MultiplierRankingPlayer(int id = 0, int score = 0, int name = 0, int updateCount = 0)
        {
            configID = id;
            detail = fat.conf.MultiRankRobotDetailVisitor.Get(configID);
            data.isBot = id != 0;
            data.score = score;
            this.updateCount = updateCount;
            if (name == 0 && data.isBot) { data.id = UnityEngine.Random.Range(10000000, 100000000); }
            else { data.id = name; }
        }

        /// <summary>
        /// 尝试更新,若更新成功返回true
        /// </summary>
        /// <returns></returns>
        public bool TryUpdateScore(ActivityMultiplierRanking rank)
        {
            if (detail == null) { return false; }
            if (nextUpdateTime == 0) { nextUpdateTime = Game.Instance.GetTimestampSeconds(); }
            if (needPrepare) { PrepareNextUpdate(rank); }
            if (nextUpdateTime <= Game.Instance.GetTimestampSeconds()) { return TryAddScore(rank); }
            return false;
        }

        /// <summary>
        /// 计算下一次更新的时间，同时表示当前是否处于追赶或者等分的状态
        /// </summary>
        /// <param name="rank"></param>
        /// <returns></returns>
        public bool PrepareNextUpdate(ActivityMultiplierRanking rank)
        {
            isWaiting = detail.IfChasing && rank.CheckNeedWaiting(this);
            isChasing = detail.IfChasing && rank.CheckNeedChasing(this);
            if (isWaiting) { nextUpdateTime += detail.PtGrowthTime.Last(); }
            else if (isChasing) { nextUpdateTime += detail.PtGrowthTime.First(); }
            else { nextUpdateTime += Random.Range(detail.PtGrowthTime.First(), detail.PtGrowthTime.Last() + 1); }
            needPrepare = false;
            return false;
        }

        /// <summary>
        /// 尝试进行加分逻辑
        /// </summary>
        /// <returns></returns>
        public bool TryAddScore(ActivityMultiplierRanking rank)
        {
            needPrepare = true;
            if (data.score >= detail.MaxPt && !isChasing) { return false; }
            AddScore(rank);
            return true;
        }

        /// <summary>
        /// 在线加积分逻辑
        /// </summary>
        /// <param name="rank"></param>
        public void AddScore(ActivityMultiplierRanking rank)
        {
            var multi = rank.conf.MultiplierSeq[updateCount >= rank.conf.MultiplierSeq.Count ? rank.conf.MultiplierSeq.Count - 1 : updateCount];
            var before = data.score;
            if (isChasing) { data.score += (int)(Random.Range(detail.PtGrowthRange.First(), detail.PtGrowthRange.Last() + 1) * multi * detail.AccelMultiplier); }
            else if (isWaiting) { data.score += (int)(Random.Range(detail.PtGrowthRange.First(), detail.PtGrowthRange.Last() + 1) * detail.DecelMultiplier * multi); }
            else { data.score += Random.Range(detail.PtGrowthRange.First(), detail.PtGrowthRange.Last() + 1) * multi; }
            updateCount++;
        }

        /// <summary>
        /// 尝试离线加积分逻辑
        /// </summary>
        public void TryAddScoreOffline(int offline, ActivityMultiplierRanking rank)
        {
            if (detail == null) { return; }
            isWaiting = detail.IfChasing && rank.CheckNeedWaiting(this);
            isChasing = detail.IfChasing && rank.CheckNeedChasing(this);
            if (data.score >= detail.MaxPt && !isChasing) { return; }
            var growth = detail.OfflinePtGrowth.ConvertToInt3();
            var baseScore = offline / growth.Item1 * growth.Item2;
            var addscore = 0f;
            if (isWaiting) { addscore = baseScore * detail.DecelOfflineMultiplier; }
            else if (isChasing) { addscore = baseScore * detail.AcclOfflineMultiplier; }
            else { addscore = baseScore; }
            CheckChasingOrWaiting(ref addscore, rank);
            AddScoreOffline(addscore);
        }

        /// <summary>
        /// 离线加积分具体逻辑
        /// </summary>
        /// <param name="score"></param>
        public void AddScoreOffline(float score) { data.score += (int)score; }

        /// <summary>
        /// 处理等待或者追赶时溢出的分数
        /// </summary>
        private void CheckChasingOrWaiting(ref float score, ActivityMultiplierRanking rank)
        {
            if (score + data.score <= detail.MaxPt) { return; }
            if (!isChasing) { score = detail.MaxPt - data.score; }
            else { if (score + data.score > rank.myself.data.score) { score = rank.myself.data.score + detail.OfflinePtGrowth.ConvertToInt3().Item2 - data.score; } }
        }
    }
}
