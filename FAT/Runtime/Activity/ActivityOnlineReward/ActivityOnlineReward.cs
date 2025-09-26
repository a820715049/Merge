/**FileHeader
 * @Author: zhangpengjian
 * @Date: 2025/8/20 14:05:13
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/8/20 14:08:30
 * @Description: 在线奖励
 * @Copyright: Copyright (©)}) 2025 zhangpengjian. All rights reserved.
 */

using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using EL;

namespace FAT
{
    public class ActivityOnlineReward : ActivityLike, IBoardEntry
    {
        public EventOnline conf;
        public EventOnlineDetail confD;
        public override bool Valid => conf != null;
        public VisualPopup VisualMain { get; } = new(UIConfig.UIActivityOnlineRewardMain);
        public override ActivityVisual Visual => VisualMain.visual;
        public List<RewardCommitData> rewardList = new();
        public List<int> claimedIndexes = new(); // 记录这次领取的所有档位索引

        public int onlineIndex => _onlineIndex;
        public int onlineTs => _onlineTs;
        public int preOnlineIndex = -1;
        private int _onlineTs;
        private int _onlineIndex;
        private int _canShowActivity;

        public ActivityOnlineReward(ActivityLite lite_)
        {
            Lite = lite_;
            conf = fat.conf.EventOnlineVisitor.GetOneByFilter((conf) => conf.Id == lite_.Param);
            VisualMain.Setup(conf.ThemeId, this, active_: false);
        }

        public override void SetupFresh()
        {
            confD = fat.conf.EventOnlineDetailVisitor.GetOneByFilter((c) => c.Id == conf.IncludeReward[_onlineIndex]);
            _onlineTs = (int)(Game.Instance.GetTimestampSeconds() + confD.Time);
            if (Game.Instance.GetTimestampSeconds() <= Lite.EndTS - conf.Deadline)
            {
                _canShowActivity = 1;
            }
            else
            {
                _canShowActivity = 0;
            }
            if (_canShowActivity <= 0)
            {
                return;
            }
            Game.Manager.screenPopup.TryQueue(VisualMain.popup, PopupType.Login);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
        }

        public string BoardEntryAsset()
        {
            Visual.Theme.AssetInfo.TryGetValue("boardEntry", out var s);
            return s;
        }
        
        public override bool EntryVisible => CanShowEntry();

        public bool BoardEntryVisible => CanShowEntry();

        private bool CanShowEntry()
        {
            var complete = _onlineIndex < conf.IncludeReward.Count;
            return complete && _canShowActivity > 0;
        }

        public override void WhenEnd()
        {
            if (_canShowActivity <= 0)
            {
                return;
            }
            if (HasReward() && _onlineIndex < conf.IncludeReward.Count)
            {
                // 先统计总共有多少个可领奖励
                int totalRewardCount = 1; // 当前这个奖励
                preOnlineIndex = _onlineIndex;
                int tempIndex = _onlineIndex + 1;
                while (tempIndex < conf.IncludeReward.Count)
                {
                    var nextConfD = fat.conf.EventOnlineDetailVisitor.GetOneByFilter((c) => c.Id == conf.IncludeReward[tempIndex]);
                    if (nextConfD.Time == 0)
                    {
                        totalRewardCount++;
                        tempIndex++;
                    }
                    else
                    {
                        break;
                    }
                }
                
                // 如果只有一个奖励，保持原来逻辑
                if (totalRewardCount == 1)
                {
                    claimedIndexes.Clear();
                    claimedIndexes.Add(_onlineIndex);
                    ClaimReward();
                }
                else
                {
                    // 多个奖励使用新逻辑：先领取当前奖励，然后批量处理后续的即时奖励
                    claimedIndexes.Clear(); // 清空之前的记录
                    
                    // 先记录当前档位并领取
                    claimedIndexes.Add(_onlineIndex);
                    ClaimReward();
                    
                    // 收集所有time==0的奖励
                    var instantRewards = new List<EventOnlineDetail>();
                    tempIndex = _onlineIndex;
                    while (tempIndex < conf.IncludeReward.Count)
                    {
                        var nextConfD = fat.conf.EventOnlineDetailVisitor.GetOneByFilter((c) => c.Id == conf.IncludeReward[tempIndex]);
                        if (nextConfD.Time == 0)
                        {
                            instantRewards.Add(nextConfD);
                            tempIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    // 将所有即时奖励添加到rewardList并领取
                    if (instantRewards.Count > 0)
                    {
                        var rewardMan = Game.Manager.rewardMan;
                        for (int i = 0; i < instantRewards.Count; i++)
                        {
                            // 记录即时奖励的档位索引
                            claimedIndexes.Add(_onlineIndex);
                            
                            var rewards = instantRewards[i].Rewards;
                            for (int j = 0; j < rewards.Count; j++)
                            {
                                var rewardConfig = rewards[j].ConvertToRewardConfig();
                                var rewardCommit = rewardMan.BeginReward(rewardConfig.Id, rewardConfig.Count, ReasonString.online_reward);
                                rewardList.Add(rewardCommit);
                            }
                            
                            _onlineIndex++;
                            DataTracker.event_online_rwd.Track(this, _onlineIndex, conf.IncludeReward.Count, _onlineIndex == conf.IncludeReward.Count, _onlineTs > 0 ? (int)(Game.Instance.GetTimestampSeconds() - _onlineTs) : 0);
                        }
                    }
                }
                Game.Manager.screenPopup.TryQueue(VisualMain.popup, PopupType.Login, true);
            }
        }

        public override void Open()
        {
            VisualMain.res.ActiveR.Open(this);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, _onlineTs));
            any.Add(ToRecord(1, _onlineIndex));
            any.Add(ToRecord(2, _canShowActivity));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            _onlineTs = ReadInt(0, any);
            _onlineIndex = ReadInt(1, any);
            _canShowActivity = ReadInt(2, any);

            if (_onlineIndex >= 0 && _onlineIndex < conf.IncludeReward.Count)
            {
                confD = fat.conf.EventOnlineDetailVisitor.GetOneByFilter((c) => c.Id == conf.IncludeReward[_onlineIndex]);
            }
        }

        public bool HasReward()
        {
            return Game.Instance.GetTimestampSeconds() >= _onlineTs;
        }

        public void ClaimReward()
        {
            if (!HasReward()) return;
            var reward = confD.Rewards;
            var rewardMan = Game.Manager.rewardMan;
            rewardList.Clear();
            for (int i = 0; i < reward.Count; i++)
            {
                var rewardConfig = reward[i].ConvertToRewardConfig();
                var rewardCommit = rewardMan.BeginReward(rewardConfig.Id, rewardConfig.Count, ReasonString.online_reward);
                rewardList.Add(rewardCommit);
            }
            _onlineIndex++;
            DataTracker.event_online_rwd.Track(this, _onlineIndex, conf.IncludeReward.Count, _onlineIndex == conf.IncludeReward.Count, _onlineTs > 0 ? (int)(Game.Instance.GetTimestampSeconds() - _onlineTs) : 0);
            if (_onlineIndex < conf.IncludeReward.Count)
            {
                confD = fat.conf.EventOnlineDetailVisitor.GetOneByFilter((c) => c.Id == conf.IncludeReward[_onlineIndex]);
                _onlineTs = (int)(Game.Instance.GetTimestampSeconds() + confD.Time);
            }
            else
            {
                Game.Manager.activity.EndImmediate(this, false);
            }
        }
    }

    public class OnlineRewardEntry : ListActivity.IEntrySetup
    {
        private readonly ListActivity.Entry e;
        private readonly ActivityOnlineReward p;
        public OnlineRewardEntry(ListActivity.Entry ent, ActivityOnlineReward act)
        {
            (e, p) = (ent, act);
            ent.dot.SetActive(act.HasReward());
        }

        public override void Clear(ListActivity.Entry e_)
        {
        }

        public override string TextCD(long diff_)
        {
            e.dot.SetActive(p.HasReward());
            return UIUtility.CountDownFormat(diff_);
        }
    }
}