using System.Collections.Generic;
using UnityEngine;
using EL;
using fat.rawdata;
using System.Linq;
using Config;

namespace FAT {
    using static fat.conf.Data;

    public class GuessAnswer {
        public int bet;
        public int from;
        public int value;
        public bool match;
        public int record;
        public int recordB;

        public void Copy(GuessCheck o_) {
            bet = o_.bet;
            from = o_.from;
            value = o_.bet;
            match = o_.match;
            recordB = o_.record;
            recordB = o_.recordB;
        }
    }

    public class GuessHand {
        public int value;
        public int place;

        public void Copy(GuessCheck o_) {
            value = o_.bet;
            place = o_.place;
        }
    }

    public struct GuessCheck {
        public int bet;
        public int from;
        public bool match;
        public int record;
        public int recordB;
        public int place;
        public bool change;
        public bool changeR;

        public GuessCheck(GuessAnswer a_, GuessHand h_, bool c_, bool r_) {
            bet = a_.bet;
            from = a_.from - 1;
            match = a_.match;
            record = a_.record;
            recordB = a_.recordB;
            place = h_.place;
            change = c_;
            changeR = r_;
        }
    }

    public readonly struct GuessRecord {
        public readonly int value;

        public GuessRecord(int value_) {
            value = value_;
        }
    }

    public readonly struct GuessMilestone {
        public readonly EventGuessMilestone conf;
        public readonly List<RewardConfig> reward;
        public int Value => conf.Score;
        public readonly AssetConfig icon;

        public GuessMilestone(int id_) {
            conf = GetEventGuessMilestone(id_);
            reward = Enumerable.ToList(conf.Reward.Select(r => r.ConvertToRewardConfig()));
            var v = conf.MilestoneRewardIcon2;
            if (string.IsNullOrEmpty(v)) {
                icon = null;
            }
            else {
                icon = v.ConvertToAssetConfig();
            }
        }
    }

    public class GuessLevel {
        public EventGuessLevel conf;
        public readonly List<RewardConfig> reward = new();

        public void Setup(int id_) {
            conf = GetEventGuessLevel(id_);
            reward.Clear();
            reward.AddRange(conf.Reward.Select(r => r.ConvertToRewardConfig()));
        }
    }

    public interface IValueSelect<T> {
        IEnumerable<T> List();
    }

    public readonly struct SelectKey : IValueSelect<int> {
        public readonly List<GuessAnswer> list;
        public readonly IEnumerable<int> List() => null;
    }
}