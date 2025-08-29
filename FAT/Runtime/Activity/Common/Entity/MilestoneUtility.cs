// /**
//  * @Author: zhangpengjian
//  * @Date: 2024/10/10 17:03:02
//  * @LastEditors: zhangpengjian
//  * @LastEditTime: 2024/10/10 17:03:02
//  * Description: 里程碑通用工具
//  */

// using System.Collections.Generic;
// using Config;
// using EL;
// using Google.Protobuf.Collections;

// namespace FAT
// {
//     public class MilestoneUtility
//     {
//         public struct Node
//         {
//             public RewardConfig reward;
//             public int value;
//         }
        
//         private int score;
//         private int cycleScoreCount;
//         private RepeatedField<int> LevelScore;
//         private RepeatedField<string> LevelReward;
//         private int CycleLevelScore;
//         private string CycleLevelReward;

//         private int scoreValueMax;
//         public readonly List<Node> ListM = new();
//         private List<RewardConfig> rewardConfigList = new();
//         private int curShowScore;
//         private int curTargetScore;
//         private int curMileStoneIndex;
//         private RewardConfig NormalScoreReward;

//         public MilestoneUtility(int scoreCur, int cycleCount, RepeatedField<int> levelScore, int cycleLevelScore, RepeatedField<string> levelReward, string cycleLevelReward)
//         {
//             score = scoreCur;
//             cycleScoreCount = cycleCount;
//             LevelScore = levelScore;
//             CycleLevelScore = cycleLevelScore;
//             LevelReward = levelReward;
//             CycleLevelReward = cycleLevelReward;
//             SetupScoreReward();
//         }

//         private void SetupScoreReward()
//         {
//             var confR = LevelReward;
//             var confS = LevelScore;
//             ListM.Clear();
//             var s = 0;
//             for (var n = 0; n < confR.Count; ++n)
//             {
//                 s += confS[n];
//                 ListM.Add(new()
//                 {
//                     reward = confR[n].ConvertToRewardConfig(),
//                     value = s,
//                 });
//             }

//             scoreValueMax = s;

//             foreach (var reward in LevelReward)
//             {
//                 rewardConfigList.Add(reward.ConvertToRewardConfig());
//             }
//         }

//         public (int, int) GetScoreShowNum()
//         {
//             //初始化 积分进度和积分奖励
//             var goalScore = CycleLevelScore;
//             var mileStone = LevelScore;
//             //累计积分已经达到普通里程的最大值
//             if (score >= scoreValueMax)
//             {
//                 if (score - scoreValueMax >= goalScore)
//                 {
//                     //超过普通里程碑最大值且完成了一次循环里程碑
//                     curShowScore = (score - scoreValueMax) % goalScore;
//                 }
//                 else
//                 {
//                     //刚超过里程碑最大值 但还没有达到循环目标分值
//                     curShowScore = score - scoreValueMax;
//                 }

//                 curTargetScore = goalScore;
//             }
//             else
//             {
//                 //两种边界情况
//                 //1.当前分数小于第一里程碑要求分数
//                 if (score < mileStone[0])
//                 {
//                     curShowScore = score;
//                     curTargetScore = mileStone[0];
//                     curMileStoneIndex = 0;
//                     NormalScoreReward = rewardConfigList[0];
//                 }
//                 else
//                 {
//                     var mileStoneIndex = 0;
//                     for (var i = 0; i < ListM.Count; i++)
//                     {
//                         if (score >= ListM[i].value && score < ListM[i + 1].value)
//                         {
//                             curTargetScore = mileStone[i + 1];
//                             curShowScore = score - ListM[i].value;
//                             mileStoneIndex = i + 1;
//                             break;
//                         }
//                     }

//                     curMileStoneIndex = mileStoneIndex;
//                     NormalScoreReward = rewardConfigList[mileStoneIndex];
//                 }
//             }

//             return (curShowScore, curTargetScore);
//         }

//         public void CheckScore()
//         {
//             //通过总积分 算出显示数据 当前里程碑目标积分 当前展示积分
//             var goalScore = CycleLevelScore;
//             var mileStone = LevelScore;
//             //累计积分已经达到普通里程的最大值
//             if (score >= scoreValueMax)
//             {
//                 if (score - scoreValueMax >= goalScore + cycleScoreCount * goalScore)
//                 {
//                     cycleScoreCount += 1;
//                     var finalReward = CycleLevelReward.ConvertToRewardConfig();
//                     //发奖
//                     var reward = Game.Manager.rewardMan.BeginReward(finalReward.Id, finalReward.Count,
//                         ReasonString.treasure);
//                     if (reward.rewardId == ConfD.RequireCoinId)
//                         DataTracker.token_change.Track(reward.rewardId, reward.rewardCount, keyNum, ReasonString.treasure_reward);
//                     scoreCommitRewardList.Add(reward);
//                     DataTracker.event_treasure_score.Track(ConfD.Id, Param, mileStone.Count + cycleScoreCount, From);
//                     //超过普通里程碑最大值且完成了一次循环里程碑
//                     curShowScore = (score - scoreValueMax) % goalScore;
//                 }
//                 else
//                 {
//                     if (normalScoreRewardDone == 0)
//                     {
//                         //只发一次
//                         var reward = Game.Manager.rewardMan.BeginReward(rewardConfigList[rewardConfigList.Count - 1].Id,
//                             rewardConfigList[rewardConfigList.Count - 1].Count, ReasonString.treasure);
//                         if (reward.rewardId == ConfD.RequireCoinId)
//                             DataTracker.token_change.Track(reward.rewardId, reward.rewardCount, keyNum, ReasonString.treasure_reward);
//                         scoreCommitRewardList.Add(reward);
//                     }
//                     //刚超过里程碑最大值 但还没有达到循环目标分值 发放最后一个里程碑奖励
//                     curShowScore = score - scoreValueMax;
//                 }

//                 normalScoreRewardDone = 1;
//                 curTargetScore = goalScore;
//             }
//             else
//             {
//                 //两种边界情况
//                 //1.当前分数小于第一里程碑要求分数
//                 if (score < mileStone[0])
//                 {
//                     curShowScore = score;
//                     curTargetScore = mileStone[0];
//                     curMileStoneIndex = 0;
//                     NormalScoreReward = rewardConfigList[0];
//                 }
//                 else
//                 {
//                     var mileStoneIndex = 0;
//                     for (var i = 0; i < ListM.Count; i++)
//                     {
//                         if (score >= ListM[i].value && score < ListM[i + 1].value)
//                         {
//                             curTargetScore = mileStone[i + 1];
//                             curShowScore = score - ListM[i].value;
//                             mileStoneIndex = i + 1;
//                             break;
//                         }
//                     }

//                     if (mileStoneIndex != curMileStoneIndex)
//                     {
//                         //如果一次性获得大额积分 需要发n次奖
//                         for (var i = 0; i < mileStoneIndex - curMileStoneIndex; i++)
//                         {
//                             var reward = Game.Manager.rewardMan.BeginReward(
//                                 rewardConfigList[curMileStoneIndex + i].Id,
//                                 rewardConfigList[curMileStoneIndex + i].Count, ReasonString.treasure);
//                             scoreCommitRewardList.Add(reward);
//                             if (reward.rewardId == ConfD.RequireCoinId)
//                                 DataTracker.token_change.Track(reward.rewardId, reward.rewardCount, keyNum, ReasonString.treasure_reward);
//                             DataTracker.event_treasure_score.Track(ConfD.Id, Param, mileStoneIndex + 1, From);
//                         }
//                         MessageCenter.Get<MSG.BOARD_ORDER_SCROLL_RESET>().Dispatch();
//                     }
//                     curMileStoneIndex = mileStoneIndex;
//                     NormalScoreReward = rewardConfigList[mileStoneIndex];
//                 }
//             }
//         }

//         public RewardConfig GetScoreShowReward()
//         {
//             if (score >= scoreValueMax)
//                 return CycleLevelReward.ConvertToRewardConfig();
//             else
//                 return NormalScoreReward;
//         }
//     }
// }
