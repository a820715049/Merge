using System;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;

namespace FAT
{
    #region 结构体声明
    public struct SignInReward
    {
        public int RewardId;
        public int RewardCount;
        public int RewardWeight;
    }
    #endregion
    public class SigninManager : IGameModule, IUserDataHolder
    {
        #region 运行时字段
        public bool IsCycle;
        public bool IsActive;
        public int StartUtc;
        public List<int> SignIn7DaysRewards = new List<int>(); //7天奖励
        public readonly List<int> SignIn30DaysRewards = new List<int>();
        public RewardCommitData SignInReward;   //单次签到奖励
        public List<RewardCommitData> SignInRewards = new List<RewardCommitData>();   //连续签到奖励
        public int TotalSignInDay => _totalSigninDay;
        public int ConsecutiveSignInDay => _consecutiveSigninDay;
        public SigninPop signinPop = new SigninPop();
        public List<int> ConsecutiveSignInRewards => _rewards;
        public bool needPopup => SignInReward != null;

        #endregion
        #region 存档字段
        private int _totalSigninDay;    //30天循环奖励
        private int _consecutiveSigninDay;   //7天连续签到奖励
        private string _lastSigninDate = string.Empty;   //最后签到日期
        private readonly List<int> _rewards = new List<int>();
        private int _milestone;
        #endregion

        #region UserDataHolder

        public void FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.SignInData ??= new fat.gamekitdata.SignInData();
            data.SignInData.TotalSignIn = _totalSigninDay;
            data.SignInData.ConsecutiveSignIn = _consecutiveSigninDay;
            data.SignInData.LastSignIn = _lastSigninDate;
            data.SignInData.MilestoneNum = _milestone;
            data.SignInData.Rewards.Clear();
            data.SignInData.Rewards.AddRange(_rewards);
        }

        public void SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.SignInData;
            if (data == null) return;
            _totalSigninDay = data.TotalSignIn;
            _consecutiveSigninDay = data.ConsecutiveSignIn;
            _lastSigninDate = data.LastSignIn;
            _milestone = data.MilestoneNum;
            _rewards.Clear();
            _rewards.AddRange(data.Rewards);
        }
        #endregion

        #region GameModule
        public void LoadConfig()
        {
            var man = Game.Manager.configMan;
            var login = man.GetLoginSignConfig();
            StartUtc = login.StartUtc;
            IsCycle = login.IsCycle;
            IsActive = login.IsActive;
            SignIn7DaysRewards.Clear();
            SignIn7DaysRewards.AddRange(login.Rewards);
            SignIn30DaysRewards.Clear();
            SignIn30DaysRewards.AddRange(login.TotalReward);
            signinPop.Setup(Game.Manager.configMan.GetPopupConfig(login.Popup), UIConfig.UISignInpanel, login.Popup);
        }

        public void Reset()
        {
        }


        public void Startup()
        {
            MessageCenter.Get<MSG.SCREEN_POPUP_QUERY>().AddListenerUnique(PopupQuery);
        }

        public void PopupQuery(ScreenPopup popup_, PopupType state_)
        {
            if (!needPopup) return;
            if (state_ != PopupType.Login) return;
            popup_.TryQueue(signinPop, state_);
        }
        #endregion

        #region 调用接口
        /// <summary>
        /// 尝试签到
        /// </summary>
        public void TrySignIn()
        {
            if (!Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureLoginSign)) return;
            if (!CheckSignInEnable() || !CheckCycleOrActive()) return;
            SignIn();
        }

        /// <summary>
        /// 检测传入的天数是否是当前签到天数，需要时弹出toast
        /// </summary>
        /// <param name="day">天数</param>
        /// <returns></returns>
        public bool CheckSignInToday(int day)
        {
            if (day > _consecutiveSigninDay)
                Game.Manager.commonTipsMan.ShowPopTips(Toast.LoginSign);
            return _consecutiveSigninDay == day;
        }

        public void ClearConsecutiveReward()
        {
            SignInReward = null;
        }

        public void ClearTotalReward()
        {
            SignInRewards.Clear();
        }
        #endregion
        #region 签到功能实现


        /// <summary>
        /// 检查签到系统是否处于配置关闭的状态
        /// </summary>
        /// <returns></returns>
        private bool CheckCycleOrActive()
        {
            if (!IsActive && _rewards.Count == 0) return false;
            if (!IsCycle && _totalSigninDay == 30) return false;
            return true;
        }

        /// <summary>
        /// 签到具体实现逻辑
        /// </summary>
        private void SignIn()
        {
            var last = _consecutiveSigninDay;
            SignInConsecutive();
            SignInTotal();
            _lastSigninDate = GetUtcOffsetTime().ToString("yyyy-MM-dd");
            DataTracker.loginsign_rwd.Track(_consecutiveSigninDay, last, isbreak, _totalSigninDay, 30, _milestone, _totalSigninDay == 30);
        }

        bool isbreak = false;
        /// <summary>
        /// 七日奖励领取逻辑
        /// </summary>
        private void SignInConsecutive()
        {
            if (!CheckSignInConsecutive())
            {
                _consecutiveSigninDay = 0;
                _rewards.Clear();
                isbreak = true;
            }
            else
                isbreak = false;
            _consecutiveSigninDay++;
            if (_consecutiveSigninDay > 7)
            {
                _consecutiveSigninDay = 1;
                _rewards.Clear();
            }
            var poolId = SignIn7DaysRewards[_consecutiveSigninDay - 1];
            var reward = ClaimSignInReward(poolId);
            SignInReward = Game.Manager.rewardMan.BeginReward(reward.id, reward.count, ReasonString.SignIn);
            _rewards.Add(SignInReward.rewardId);
        }

        /// <summary>
        /// 根据奖励池中的奖励权重随机实际奖励
        /// </summary>
        private RewardValue ClaimSignInReward(int id)
        {
            var result = new RewardValue();
            var rewardList = new List<SignInReward>();
            var pool = Game.Manager.configMan.GetLoginSignPoolConfig(id);
            foreach (var reward in pool.Pool)
            {
                var info = reward.Split(':');
                rewardList.Add(new SignInReward
                {
                    RewardId = int.Parse(info[0]),
                    RewardCount = int.Parse(info[1]),
                    RewardWeight = int.Parse(info[2])
                });
            }
            var rand = rewardList.RandomChooseByWeight(r => r.RewardWeight);
            var time = 0;
            while (_rewards.Count != 0 && _rewards[_rewards.Count - 1] == rand.RewardId && time < 10)
            {
                rand = rewardList.RandomChooseByWeight(r => r.RewardWeight);
                time++;
            }
            result.id = rand.RewardId;
            result.count = rand.RewardCount;
            return result;
        }

        /// <summary>
        /// 30日奖励领取逻辑
        /// </summary>
        private void SignInTotal()
        {
            if (_totalSigninDay < 30)
            {
                _totalSigninDay++;
            }
            else
            {
                _totalSigninDay = 1;
                _milestone++;
            }
            foreach (var reward in SignIn30DaysRewards)
            {
                if (_totalSigninDay == reward)
                {
                    var rewardinfo = Game.Manager.configMan.GetLoginSignTotalConfig(reward);
                    foreach (var item in rewardinfo.TotalPool)
                    {
                        var info = item.ConvertToRewardConfig();
                        SignInRewards.Add(Game.Manager.rewardMan.BeginReward(info.Id, info.Count, ReasonString.SignIn));
                    }
                }
            }
        }

        private void TryPop()
        {
            Game.Manager.screenPopup.TryQueue(signinPop, PopupType.Login);
        }

        #endregion

        #region UTC时间判断逻辑
        /// <summary>
        /// 获取当前UTC十点为新的一天的依据，判断当前日期
        /// </summary>
        /// <returns></returns>
        private DateTime GetUtcOffsetTime()
        {
            var utcTime = DateTime.UtcNow.AddSeconds(Game.Manager.networkMan.networkBias);
            var utcTime_offset = new DateTime(utcTime.Year, utcTime.Month, utcTime.Day, StartUtc, 0, 0, DateTimeKind.Utc);
            if (utcTime < utcTime_offset)
                utcTime_offset = utcTime_offset.AddDays(-1);
            return utcTime_offset;
        }
        /// <summary>
        /// 检查是否可以签到
        /// </summary>
        /// <returns></returns>
        private bool CheckSignInEnable()
        {
            if (string.IsNullOrEmpty(_lastSigninDate))
                return true;
            DateTime lastSignin;
            if (!DateTime.TryParseExact(_lastSigninDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out lastSignin))
                return true;
            return lastSignin.Date != GetUtcOffsetTime().Date;
        }


        /// <summary>
        /// 检查是否是连续签到
        /// </summary>
        /// <returns></returns>
        private bool CheckSignInConsecutive()
        {
            if (string.IsNullOrEmpty(_lastSigninDate))
                return true;
            DateTime lastSignin;
            if (!DateTime.TryParseExact(_lastSigninDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out lastSignin))
                return false;
            // 判断是否是前一天的下一天
            return lastSignin.Date.AddDays(1) == GetUtcOffsetTime().Date;
        }
        #endregion

        #region debug
        public void DebugReset()
        {
            _lastSigninDate = string.Empty;
            _rewards.Clear();
            _totalSigninDay = 0;
            _consecutiveSigninDay = 0;
        }

        public void DebugSetTotalSign(int day)
        {
            _totalSigninDay = day;
        }
        #endregion
    }
}
