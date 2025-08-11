using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using EL;
using System.Text;
using System.Linq;
using Config;
using FAT.Merge;
using Spine;
using static EL.PoolMapping;
using System.Threading.Tasks;

namespace FAT {
    using static UILayer;
    using static PoolMapping;
    using static MessageCenter;

    public partial class UIConfig {
        public static UIResource UIActivityGuess = new("UIActivityGuess.prefab", AboveStatus, "event_guesscolor_default");
        public static UIResource UIActivityGuessStart = new("UIActivityGuessStart.prefab", AboveStatus, "event_guesscolor_default");
        public static UIResource UIActivityGuessEnd = new("UIActivityGuessEnd.prefab", AboveStatus, "event_guesscolor_default");
        public static UIResource UIActivityGuessReward = new("UIActivityGuessReward.prefab", SubStatus, "event_guesscolor_default");
        public static UIResource UIActivityGuessRestart = new("UIActivityGuessRestart.prefab", AboveStatus, "event_guesscolor_default");
        public static UIResource UIActivityGuessLoading = new("UIActivityGuessLoading.prefab", Loading, "event_guesscolor_default");
        public static UIResource UIActivityGuessHelp = new("UIActivityGuessHelp.prefab", AboveStatus, "event_guesscolor_default");
    }

    public class ActivityGuess : ActivityLike, IBoardEntry {
        public static readonly ReasonString guess = new(nameof(guess));
        public static readonly ReasonString guess_milestone = new(nameof(guess_milestone));
        public EventGuessRound confR;
        public EventGuess confD;
        public EventGuessDetail confD1;
        public EventGuessMilestone confM;
        public override bool Valid => confR != null;
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureGuess);
        public override ActivityVisual Visual => VisualMain.visual;
        public VisualRes VisualMain { get; } = new(UIConfig.UIActivityGuess);
        public VisualPopup VisualStart { get; } = new(UIConfig.UIActivityGuessStart);
        public VisualPopup VisualEnd { get; } = new(UIConfig.UIActivityGuessEnd);
        public VisualPopup VisualReward { get; } = new(UIConfig.UIActivityGuessReward);
        public VisualPopup VisualRestart { get; } = new(UIConfig.UIActivityGuessRestart);
        public VisualRes VisualLoading { get; } = new(UIConfig.UIActivityGuessLoading);
        public VisualRes VisualHelp { get; } = new(UIConfig.UIActivityGuessHelp);
        public GuessLevel level = new();
        public GuessMilestone Prize => prize[^1];
        public readonly List<GuessMilestone> prize = new();
        public readonly Dictionary<int, Ref<List<RewardCommitData>>> prizeCache = new();
        public readonly List<GuessAnswer> answer = new();
        public readonly List<GuessCheck> check = new();
        public readonly List<GuessHand> hand = new();
        public readonly List<GuessRecord> record = new();
        public int handCount;
        public int AnswerNext { get; private set; }
        public bool TokenReady => Token > 0;
        public bool AnswerReady => AnswerNext >= answer.Count;
        public int Token { get ; private set; }
        public int Score { get; private set; }
        public int ScorePhase { get; private set; }
        public RewardCommitData scoreCommit { get; private set; }
        public int Milestone { get; private set; }
        public int MilestoneMax { get; private set; }
        public bool MilestoneComplete => Milestone >= MilestoneMax;
        public int tokenCost;
        public int CorrectCount { get; set; }
        public bool PutRepeatedItem { get; set; }
        public UnityEngine.Transform PutRepeatedItemTarget { get; set; }
        public bool FirstCheck { get; set; }
        public string TokenSprite { get; set; }
        public string MilestoneSprite { get; set; }
        public int roundIndex;
        public int levelIndex;
        public int milestoneIndex;
        public bool firstInType;
        public int levelIndexR;
        private GuessSpawnBonusHandler spawnBonusHandler;

        public static void DebugTest() {
            var a = Game.Manager.activity;
            // ActivityLiteFlex.CreateInfo(1, ActivityLite.FromInternal, default, out var info);
            // ActivityLiteFlex.CreateInstance(info, EventType.Default, out var lite);
            // var now = Game.TimestampNow();
            // var acti = new ActivityGuess(lite);
            // acti.SetupFresh();
            // acti.RefreshTS(now, now + 600);
            // acti.Generate(new List<int>() { 1, 2, 1, 1, 1, 1 });
            // a.AddActive(acti.Id2, acti, new_:true);
            // acti.Open();
            a.DebugInsert("-1 guess 1");
            a.DebugActivate("-1,600");
            async Task F() {
                await Task.Delay(500);
                var acti = (ActivityGuess)a.LookupAny(EventType.Guess);
                acti.Token = 1000;
                var m = acti.prize.Count - 3;
                acti.milestoneIndex = m;
                acti.Milestone = acti.prize[m].Value - 100;
            }
            _ = F();
        }

        public static void DebugLastM() {
            var a = Game.Manager.activity;
            var acti = (ActivityGuess)a.LookupAny(EventType.Guess);
            var n = acti.prize.Count - 1;
            acti.milestoneIndex = n;
            acti.Milestone = acti.prize[n].Value - 50;
        }

        public static void DebugReady() {
            var a = Game.Manager.activity;
            var acti = (ActivityGuess)a.LookupAny(EventType.Guess);
            var aK = acti.AnswerKey();
            acti.ApplyGenerate(acti.answer.Count, aK, aK, 0, 0, 0, 0);
        }
        
        public ActivityGuess() {}
        public ActivityGuess(ActivityLite lite_) {
            Lite = lite_;
            confR = GetEventGuessRound(lite_.Param);
            scoreCommit = null;
            SetupBonusHandler();
        }

        public void SetupTheme() {
            VisualMain.Setup(confD.GuessMainTheme);
            VisualStart.Setup(confD.StartTheme, this);
            VisualEnd.Setup(confD.RecontinueTheme, this, active_:false);
            VisualReward.Setup(confD.RewardTheme, this);
            VisualRestart.Setup(confD.RestartTheme, this);
            VisualLoading.Setup(confD.LoadingTheme);
            VisualHelp.Setup(confD.HelpPlayTheme);
            TokenSprite = TextSprite.FromToken(confD.TokenId);
            MilestoneSprite = TextSprite.FromToken(confD.MilestoneScoreId);
            var map = VisualMain.visual.AssetMap;
            map.TryReplace("boardEntry", "event_guesscolor_default#UIGuessEntry.prefab");
            map = VisualMain.visual.TextMap;
            map.TryReplace("prize", "#SysComDesc345");
            map = VisualReward.visual.AssetMap;
            map.TryReplace("bg", Prize.conf.MilestoneRewardIcon2);
            map = VisualStart.visual.TextMap;
            map.TryReplace("prize", "#SysComDesc345");
            map.TryReplace("prize1", "#SysComDesc823");
            map = VisualRestart.visual.TextMap;
            map.TryReplace("prize", "#SysComDesc345");
            map.TryReplace("prize1", "#SysComDesc823");
            map = VisualLoading.visual.TextMap;
            map.TryReplace("mainTItle", confD.Name);
            map = VisualEnd.visual.TextMap;
            if (map.TryGetValue("desc1", out var t)) map.Replace("subTitle3", I18N.FormatText(t, TokenSprite));
            else map.TryReplace("subTitle3", I18N.FormatText("#SysComDesc829", TokenSprite));
            map.TryCopy("desc2", "subTitle1");
        }

        public void SetupDetail(int n_, int id_ = 0, int level_ = 0, int milestone_ = 0) {
            roundIndex = n_;
            var n = n_;
            var id = confR.IncludeGuessId[n];
            confD = GetEventGuess(id);
            id = id_ > 0 ? id_ : Game.Manager.userGradeMan.GetTargetConfigDataId(confD.GradeId);
            DebugEx.Info($"{nameof(ActivityGuess)} round index:{roundIndex} detail id:{id}");
            confD1 = GetEventGuessDetail(id);
            prize.Clear();
            foreach(var c in confD1.Milestones) {
                var e = new GuessMilestone(c);
                prize.Add(e);
            }
            Milestone = 0;
            MilestoneMax = Prize.conf.Score;
            SetupLevel(level_);
            levelIndexR = 0;
            milestoneIndex = milestone_;
            SetupTheme();
        }

        public void SetupLevel(int level_) {
            levelIndex = level_;
            tokenCost = 0;
            var list = confD1.IncludeLevel;
            if (level_ >= list.Count) {
                level_ -= list.Count;
                list = confD1.IncludeCycleLevel;
            }
            level_ %= list.Count;
            var id = list[level_];
            level.Setup(id);
            DebugEx.Info($"{nameof(ActivityGuess)} level id:{id} index:{levelIndex}");
        }

        public void NextLevel() {
            DataTracker.GuessLevelComplete(this);
            ++levelIndexR;
            SetupLevel(++levelIndex);
            Generate();
        }

        public bool SetupNextRound(bool loop_ = false) {
            var n = roundIndex + 1;
            var rCount = confR.IncludeGuessId.Count;
            if (loop_) n %= rCount;
            if (n >= rCount) {
                return false;
            }
            SetupDetail(n);
            Generate();
            DataTracker.GuessRestart(this);
            return true;
        }

        public override void SaveSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            any.Add(ToRecord(1, ScorePhase));
            any.Add(ToRecord(2, Score));
            any.Add(ToRecord(3, Token));
            any.Add(ToRecord(4, Milestone));
            any.Add(ToRecord(6, firstInType));
            any.Add(ToRecord(11, roundIndex));
            any.Add(ToRecord(12, confD1.Id));
            any.Add(ToRecord(13, levelIndex));
            any.Add(ToRecord(14, milestoneIndex));
            any.Add(ToRecord(15, levelIndexR));
            any.Add(ToRecord(16, tokenCost));
            any.Add(ToRecord(20, answer.Count));
            any.Add(ToRecord(21, AnswerKey()));
            any.Add(ToRecord(22, BetKey()));
            any.Add(ToRecord(23, MatchKey()));
            any.Add(ToRecord(24, FromKey()));
            any.Add(ToRecord(30, handCount));
            any.Add(ToRecord(31, HandKey()));
            any.Add(ToRecord(32, PlaceKey()));
            for (var k = 0; k < answer.Count; ++k) {
                any.Add(ToRecord(51 + k, answer[k].record));
            }
            any.Add(ToRecord(100, record.Count));
            for(var k = 0; k < record.Count; ++k) {
                any.Add(ToRecord(101 + k, record[k].value));
            }
        }

        public override void LoadSetup(ActivityInstance data_) {
            if (!Valid) return;
            var any = data_.AnyState;
            ScorePhase = ReadInt(1, any);
            Score = ReadInt(2, any);
            Token = ReadInt(3, any);
            firstInType = ReadBool(6, any);
            roundIndex = ReadInt(11, any);
            var id = ReadInt(12, any);
            levelIndex = ReadInt(13, any);
            milestoneIndex = ReadInt(14, any);
            SetupDetail(roundIndex, id, levelIndex, milestoneIndex);
            Milestone = ReadInt(4, any);
            levelIndexR = ReadInt(15, any);
            tokenCost = ReadInt(16, any);
            var count = ReadInt(20, any);
            var answerK = ReadInt(21, any);
            var betK = ReadInt(22, any);
            var matchK = ReadInt(23, any);
            var fromK = ReadInt(24, any);
            handCount = ReadInt(30, any);
            var handK = ReadInt(31, any);
            var placeK = ReadInt(32, any);
            ApplyGenerate(count, answerK, betK, matchK, fromK, handK, placeK);
            var (valid, rs) = ValidateLevel();
            if (!valid) {
                DebugEx.Error($"{nameof(ActivityGuess)} reason:{rs}");
                Generate();
                return;
            }
            for (var k = 0; k < answer.Count; ++k) {
                var rc = ReadInt(51 + k, any);
                answer[k].record = rc;
                foreach(var v in UnpackSet(rc)) answer[k].recordB |= 1 << (v - 1);
            }
            var r = ReadInt(100, any);
            for (var k = 0; k < r; ++k) {
                record.Add(new(ReadInt(101 + k, any)));
            }
        }

        public override void SetupFresh() {
            SetupDetail(0);
            Generate();
            Token = 0;
            UpdateToken(confD.FreeTokenNum);
            firstInType = Game.Manager.activity.IsFirst(Type);
            if (firstInType) DebugEx.Info($"{nameof(ActivityGuess)} first in type");
        }

        public (bool, string) ValidateLevel() {
            if (answer.Count == 0) return (false, "answer empty");
            try {
                var map = new Dictionary<int, int>();
                void V(int k_, int c_) {
                    map.TryGetValue(k_, out var v);
                    map[k_] = v + c_;
                }
                for (var k = 0; k < answer.Count; ++k) {
                    var a = answer[k];
                    var v = a.value;
                    if (v == 0) {
                        return (false, $"invalid answer at {k}");
                    }
                    V(v, 1);
                    V(a.bet, -1);
                }
                for (var k = 0; k < hand.Count; ++k) {
                    V(hand[k].value, -1);
                }
                foreach (var (k, v) in map) {
                    if (k != 0 && v != 0) {
                        return (false, $"answer not fullfillable card {k} diff:{v}");
                    }
                }
            }
            catch (Exception e) {
                return (false, $"answer validate fail:{e.Message}\n{e.StackTrace}");
            }
            return (true, null);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            if (MilestoneComplete) return;
            VisualStart.Popup(popup_, state_, limit_:1);
        }

        public override void Open() {
            if (Game.Manager.networkMan.isWeakNetwork) {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.IapNetworkError);
                return;
            }
            ActivityTransit.Enter(this, VisualLoading, VisualMain.res);
        }

        public bool TryNextRound() {
            if (!MilestoneComplete) return false;
            Exit(VisualMain.res.ActiveR);
            if (SetupNextRound()) {
                VisualRestart.Popup();
                return true;
            }
            Game.Manager.activity.EndImmediate(this, false);
            return false;
        }

        public void Exit(UIResource ui_) => ActivityTransit.Exit(this, ui_);

        public override void WhenActive(bool new_) {
            if (!new_) {
                if (MilestoneComplete) TryNextRound();
                return;
            }
            VisualStart.Popup();
        }

        public override void WhenReset()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(spawnBonusHandler);
        }

        public override void WhenEnd()
        {
            DataTracker.GuessEnd(this);
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(spawnBonusHandler);
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            using var _ = PoolMappingAccess.Borrow(out Dictionary<int, int> map);
            map[confD.TokenId] = Token;
            ActivityExpire.ConvertToReward(confD.ExpirePopup, list, guess, token_:map);
            VisualStart.res.ActiveR.Close();
            VisualRestart.res.ActiveR.Close();
            VisualEnd.Popup(custom_:listT);
        }

        public ListActivity.IEntrySetup SetupEntry(ListActivity.Entry e_)
        {
            e_.dot.SetActive(Token > 0);
            e_.dotCount.gameObject.SetActive(Token > 0);
            e_.dotCount.SetText($"{Token}");
            return null;
        }

        private void SetupBonusHandler()
        {
            if (spawnBonusHandler == null)
            {
                spawnBonusHandler = new GuessSpawnBonusHandler(this);
                Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(spawnBonusHandler);
            }
        }

        public void Generate() => Generate(level.conf.ItemInfo);
        public int Generate(IList<int> weight_, int offset = -1) {
            ClearGenerate();
            var max = 7;
            var random = new Random();
            if (offset < 0) offset = random.Next(7);
            int T(int v_) => (v_ + offset) % max + 1;
            using var _0 = PoolMappingAccess.Borrow(out List<int> list);
            using var _1 = PoolMappingAccess.Borrow(out List<(int, int)> listH);
            for(var i = 0; i < weight_.Count; ++i) {
                var w = weight_[i];
                for (var k = 0; k < w; ++k) {
                    list.Add(i);
                }
                listH.Add((i, w));
            }
            while(list.Count > 0) {
                var n = random.Next(list.Count);
                answer.Add(new() {
                    value = T(list[n]),
                });
                list.RemoveAt(n);
            }
            while (listH.Count > 0) {
                var n = random.Next(listH.Count);
                var (k, h) = listH[n];
                for (var o = 0; o < h; ++o) {
                    hand.Add(new() { value = T(k), place = hand.Count });
                }
                listH.RemoveAt(n);
            }
            handCount = hand.Count;
            var c = 0;
            for (; c < answer.Count; ++c) {
                if (answer[c].value != T(c)) break;
            }
            if (c == answer.Count) {
                var n0 = answer[0];
                var n1 = answer[^1];
                (n1.value, n0.value) = (n0.value, n1.value);
            }
            DebugEx.Info($"{nameof(ActivityGuess)} generate {AnswerKey()} {HandKey()} {offset}");
            PrintAnswer();
            return offset;
        }

        public void ClearGenerate() {
            answer.Clear();
            hand.Clear();
            record.Clear();
            AnswerNext = 0;
        }

        public void ApplyGenerate(int count_, int answer_, int bet_, int match_, int from_, int hand_, int place_) {
            ClearGenerate();
            foreach(var (a, b, m, f, h, p) in UnpackKey(count_, answer_, bet_, match_, from_, hand_, place_)) {
                answer.Add(new() { value = a, bet = b, from = f, match = m > 0 });
                hand.Add(new() { value = h, place = p });
            }
            CountAnswer();
            DebugEx.Info($"{nameof(ActivityGuess)} generate apply answer:{answer_} bet:{bet_} match:{match_} from:{from_} hand:{hand_} place:{place_} next:{AnswerNext}");
            PrintAnswer();
        }

        public void CountAnswer() {
            AnswerNext = answer.Count;
            for (var k = 0; k < answer.Count; ++k) {
                if (answer[k].bet == 0) {
                    AnswerNext = k;
                    break;
                }
            }
        }

        public void CountHand() {
            handCount = 0;
            for (var k = 0; k < hand.Count; ++k) {
                var h = hand[k];
                if (h.value == 0) continue;
                h.place = handCount++;
            }
        }

        public void PrintAnswer() {
            int F(int v_) {
                for (var k = 0; k < hand.Count; ++k) {
                    if (hand[k].value == v_) return k + 1;
                }
                return 0;
            }
            var order = answer.Select(a => F(a.value));
            DebugEx.Info($"{nameof(ActivityGuess)} generate order:{PackKey(order)}");
        }

        public void UpdateScoreOrToken(int rewardId, int addNum)
        {
            switch (rewardId) {
                case var _ when rewardId == confD.TokenId: {
                    UpdateToken(addNum);
                } break;
                case var _ when rewardId == confD.RequireScoreId: {
                    var prev = Score;
                    Score += addNum;
                    Get<MSG.ACTIVITY_GUESS_SCORE>().Dispatch(prev, Score);
                    CheckScore();
                    DataTracker.token_change.Track(rewardId, addNum, Score, guess);
                } break;
                case var _ when rewardId == confD.MilestoneScoreId: {
                    UpdateMilestone(addNum);
                } break;
            }
        }

        public void UpdateToken(int v_) {
            var vo = Token;
            Token += v_;
            Get<MSG.ACTIVITY_GUESS_TOKEN>().Dispatch(vo, Token);
            DataTracker.token_change.Track(confD.TokenId, v_, Token, guess);
        }

        public void UpdateMilestone(int v_) {
            var vo = Milestone;
            if (vo >= MilestoneMax) return;
            Milestone += v_;
            Get<MSG.ACTIVITY_GUESS_MILESTONE>().Dispatch(vo, Milestone);
            DataTracker.token_change.Track(confD.MilestoneScoreId, v_, Milestone, guess);
            CheckMilestone();
        }

        public void CheckMilestone() {
            if (milestoneIndex >= prize.Count) return;
            var rMan = Game.Manager.rewardMan;
            GuessMilestone node;
            while (milestoneIndex < prize.Count && Milestone >= (node = prize[milestoneIndex]).Value) {
                ++milestoneIndex;
                var listT = PoolMappingAccess.Take<List<RewardCommitData>>(out var list);
                foreach(var r in node.reward) {
                    var d = rMan.BeginReward(r.Id, r.Count, guess_milestone);
                    list.Add(d);
                }
                prizeCache.Add(milestoneIndex, listT);
                DataTracker.GuessMilestone(this, node.conf, milestoneIndex, prize.Count);
                DebugEx.Info($"{nameof(ActivityGuess)} milestone {milestoneIndex} reward {listT.obj.Count}");
            }
        }

        public bool ToHand(int i_, out int ii_, out int ni_, bool auto_ = false) {
            var a = answer[i_];
            ii_ = a.from - 1;
            ni_ = i_;
            if (ii_ < 0 || a.match) return false;
            if (!auto_) {
                UpdateToken(1);
                --tokenCost;
            }
            var h = hand[ii_];
            h.value = a.bet;
            a.bet = 0;
            a.from = 0;
            if (AnswerNext > i_) {
                ni_ = AnswerNext;
                AnswerNext = i_;
            }
            DebugEx.Info($"{nameof(ActivityGuess)} answer {i_} to hand {ii_} card:{h.value} next:{AnswerNext}");
            return true;
        }

        public bool ToAnswer(int i_, out int ii_, out int ni_) {
            var h = hand[i_];
            ii_ = AnswerNext;
            ni_ = AnswerNext;
            if (!TokenReady || h.value < 1) return false;
            UpdateToken(-1);
            ++tokenCost;
            var a = answer[ii_];
            a.from = i_ + 1;
            a.bet = h.value;
            h.value = 0;
            if (!PutRepeatedItem && (a.recordB & 1 << (a.bet - 1)) != 0) {
                PutRepeatedItem = true;
                if (UIManager.Instance.TryGetCache(VisualMain.res.ActiveR, out var uiV) && uiV is UIActivityGuess ui) {
                    PutRepeatedItemTarget = ui.answer[ii_].card.transform;
                }
                DebugEx.Info($"guess repeat {ii_} {PutRepeatedItemTarget.parent.name}");
                Game.Manager.guideMan.TriggerGuide();
            }
            while(++AnswerNext < answer.Count) {
                if (answer[AnswerNext].bet <= 0) break;
            }
            ni_ = AnswerNext;
            DebugEx.Info($"{nameof(ActivityGuess)} hand {i_} to answer {ii_} card:{a.bet} next:{AnswerNext}");
            return true;
        }

        public int PackKey(IEnumerable<int> v_) {
            var key = 0;
            var k = 0;
            foreach(var v in v_.Reverse()) {
                key += v * (int)Math.Pow(10, k);
                ++k;
            }
            return key;
        }

        public IEnumerable<(int, int, int, int, int, int)> UnpackKey(int count_, int v1_, int v2_, int v3_, int v4_, int v5_, int v6_) {
            static int P(ref int v_, int f_) {
                var v = v_ / f_;
                v_ %= f_;
                return v;
            }
            for (var k = count_ - 1; k >= 0; --k) {
                var f = (int)Math.Pow(10, k);
                yield return (
                    P(ref v1_, f),
                    P(ref v2_, f),
                    P(ref v3_, f),
                    P(ref v4_, f),
                    P(ref v5_, f),
                    P(ref v6_, f)
                );
            }
        }

        public IEnumerable<int> UnpackSet(int v_) {
            static int P(ref int v_, int f_) {
                var v = v_ % f_;
                v_ /= f_;
                return v;
            }
            while(v_ > 0) {
                yield return P(ref v_, 10);
            }
        }

        public int AnswerKey() {
            var _ = PoolMappingAccess.Borrow(out List<int> list);
            foreach(var a in answer) list.Add(a.value);
            return PackKey(list);
        }

        public int BetKey() {
            var _ = PoolMappingAccess.Borrow(out List<int> list);
            foreach(var a in answer) list.Add(a.bet);
            return PackKey(list);
        }

        public int MatchKey() {
            var _ = PoolMappingAccess.Borrow(out List<int> list);
            foreach(var a in answer) list.Add(a.match ? 1 : 0);
            return PackKey(list);
        }

        public int FromKey() {
            var _ = PoolMappingAccess.Borrow(out List<int> list);
            foreach(var a in answer) list.Add(a.from);
            return PackKey(list);
        }

        public int HandKey() {
            var _ = PoolMappingAccess.Borrow(out List<int> list);
            foreach(var h in hand) list.Add(h.value);
            return PackKey(list);
        }

        public int PlaceKey() {
            var _ = PoolMappingAccess.Borrow(out List<int> list);
            foreach(var h in hand) list.Add(h.place);
            return PackKey(list);
        }

        public bool CheckRecord() {
            var key = BetKey();
            var r = new GuessRecord(key);
            if (record.Contains(r)) return false;
            record.Add(r);
            DebugEx.Info($"{nameof(ActivityGuess)} answer {key}");
            return true;
        }

        public (bool, bool, List<GuessCheck>, Ref<List<RewardCommitData>>) CheckAnswer() {
            if (!AnswerReady) {
                DebugEx.Warning($"{nameof(ActivityGuess)} answer not complete {AnswerNext}<{answer.Count}");
                return default;
            }
            if (firstInType) {
                FirstInTypeHack();
                firstInType = false;
                FirstCheck = true;
                Game.Manager.guideMan.TriggerGuide();
            }
            check.Clear();
            var m = 0;
            for (var k = 0 ; k < answer.Count; ++k) {
                var a = answer[k];
                var mO = a.match;
                var mN = a.bet == a.value;
                var rC = false;
                a.match = mN;
                var cc = new GuessCheck(a, hand[a.from - 1], mO != mN, rC);
                if (mN) ++m;
                else {
                    var vh = 1 << (a.bet - 1);
                    rC = (a.recordB & vh) == 0;
                    if (rC) {
                        a.recordB |= vh;
                        a.record *= 10;
                        a.record += a.bet;
                    }
                    ToHand(k, out var ii, out _, auto_:true);
                }
                cc.changeR = rC;
                check.Add(cc);
            }
            CorrectCount = m;
            var complete = m >= answer.Count;
            var reward = complete ? LevelComplete() : default;
            if (!complete) CountHand();
            DebugEx.Info($"{nameof(ActivityGuess)} answer correct:{m} complete:{complete}");
            return (true, complete, check, reward);
        }

        public Ref<List<RewardCommitData>> LevelComplete() {
            var ret = PoolMappingAccess.Take<List<RewardCommitData>>(out var list);
            var rMan = Game.Manager.rewardMan;
            foreach(var r in level.reward) {
                var d = rMan.BeginReward(r.Id, r.Count, guess);
                list.Add(d);
            }
            NextLevel();
            return ret;
        }

        public void FirstInTypeHack() {
            var aa = answer[^1];
            var ba = aa.bet;
            var va = aa.value;
            aa.value = ba;
            var count = answer.Count - 1;
            for (var k = 0; k < count; ++k) {
                var a = answer[k];
                if (a.value == ba) {
                    a.value = va;
                    break;
                }
            }
            for (var k = 0; k < count; ++k) {
                var a = answer[k];
                if (a.bet == a.value) {
                    var o = answer[(k + 1) % count];
                    (o.value, a.value) = (a.value, o.value);
                }
            }
            DebugEx.Info($"{nameof(ActivityGuess)} hack: {AnswerKey()}");
        }

        public bool HasNextRound()
        {
            if (confR != null)
                return roundIndex < confR.IncludeGuessId.Count;
            return false;
        }

        #region score
        public RewardCommitData AddScore(int addScore)
        {
            var activeWorld = Game.Manager.mergeBoardMan.activeWorld;
            if (activeWorld != null)
            {
                //说明在棋盘内
                var curBoardId = activeWorld.activeBoard.boardId;
                if (curBoardId != confD.BoardId)
                {
                    return null;
                }
            }

            var reward = Game.Manager.rewardMan.BeginReward(confD.RequireScoreId, addScore, guess);
            return reward;
        }

        public void CheckScore() 
        {
            var cycle = ScorePhase >= confD1.NormalScore.Count;
            var max = cycle ? confD1.CycleScore : confD1.NormalScore[ScorePhase];
            if (Score < max) return;
            var reward = cycle ? confD1.CycleToken.ConvertToRewardConfig() : confD1.NormalToken[ScorePhase].ConvertToRewardConfig();
            var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.pachinko_energy);
            SetScoreCommit(commit);
            ScorePhase++;
            Score -= max;
        }

        public void SetScoreCommit(RewardCommitData data)
        {
            scoreCommit = data;
        }

        public (int, int) GetScoreShowNum()
        {
            var cycle = ScorePhase >= confD1.NormalScore.Count;
            var max = cycle ? confD1.CycleScore : confD1.NormalScore[ScorePhase];
            return (Score, max);
        }

        public RewardConfig GetScoreShowReward()
        {
            var cycle = ScorePhase >= confD1.NormalScore.Count;
            var str = cycle ? confD1.CycleToken : confD1.NormalToken[ScorePhase];
            return str.ConvertToRewardConfig();

        }

        public string BoardEntryAsset()
        {
            VisualMain.visual.AssetMap.TryGetValue("boardEntry", out var key);
            return key;
        }

        public bool BoardEntryVisible => HasNextRound();

        #endregion
    }
}

namespace FAT.MSG {
    public class ACTIVITY_GUESS_TOKEN : MessageBase<int, int> { }
    public class ACTIVITY_GUESS_SCORE : MessageBase<int, int> { }
    public class ACTIVITY_GUESS_MILESTONE : MessageBase<int, int> { }
    public class ACTIVITY_GUESS_ENTRY_REFRESH_RED_DOT : MessageBase { }
}

public partial class DataTracker {
    public static void GuessLevelComplete(FAT.ActivityGuess acti_) {
        var data = BorrowTrackObject();
        FillActivity(data, acti_);
        data["level_id"] = acti_.level.conf.Id;
        data["level_queue"] = acti_.levelIndexR + 1;
        data["round_num"]= acti_.roundIndex + 1;
        data["token_use"] = acti_.tokenCost;
        TrackObject(data, "guess_level_complete");
    }

    public static void GuessMilestone(FAT.ActivityGuess acti_, EventGuessMilestone conf_, int index1_, int count_) {
        var data = BorrowTrackObject();
        FillActivity(data, acti_);
        data["milestone_id"] = conf_.Id;
        data["milestone_queue"] = index1_;
        data["milestone_difficulty"] = conf_.Diff;
        data["milestone_num"] = count_;
        data["round_num"]= acti_.roundIndex + 1;
        data["is_final"] = index1_ == count_;
        TrackObject(data, "event_guess_milestone");
    }

    public static void GuessRestart(FAT.ActivityGuess acti_) {
        var data = BorrowTrackObject();
        FillActivity(data, acti_);
        data["round_num"]= acti_.roundIndex + 1;
        TrackObject(data, "event_guess_restart");
    }

    public static void GuessEnd(FAT.ActivityGuess acti_) {
        var data = BorrowTrackObject();
        FillActivity(data, acti_);
        data["round_total"]= acti_.roundIndex;
        data["token_expire_num"] = acti_.Token;
        TrackObject(data, "event_guess_end");
    }
}