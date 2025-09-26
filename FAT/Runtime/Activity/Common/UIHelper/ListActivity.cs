using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using EventType = fat.rawdata.EventType;
using static EL.MessageCenter;
using FAT.MSG;

namespace FAT
{
    public class ListActivity : MonoBehaviour
    {
        public enum Pos
        {
            TopLeft,
            TopRight,
            BottomLeft
        }

        [Serializable]
        public class Entry
        {
            public GameObject obj;
            public Transform root;
            public MapButton button;
            public UIVisualGroup visual;
            public UIImageRes icon;
            public UIImageRes img;
            public UIStateGroup iconState;
            public TextMeshProUGUI cd;
            public TextMeshProUGUI start;
            public TextMeshProUGUI actCd;
            public TextMeshProUGUI orderCd;
            public GameObject dot;
            public TextMeshProUGUI dotCount;
            public GameObject sale;
            public TextMeshProUGUI token;
            public TextMeshProUGUI innerTxt;
            public TextMeshProUGUI discount;
            public UITextState tokenState;
            public UIImageState frame;
            public UIImageRes up;
            public UIImageRes flag;
            public UICommonProgressBar progress;
            public ActivityLike activity;
            public IEntrySetup setup;
        }

        public abstract class IEntrySetup
        {
            public abstract void Clear(Entry e_);

            public virtual string TextCD(long diff_)
            {
                return UIUtility.CountDownFormat(diff_);
            }
        }

        public GameObject group;
        public List<Entry> list = new();
        public bool filterFirst;
        public Pos filterPos;
        private readonly List<ActivityLike> listAct = new();
        private Comparison<ActivityLike> SortPriority;
        private Action WhenUpdate;
        private Action<IMapBuilding> WhenFocus;
        private Action WhenTick;

        public static Entry ParseEntry(GameObject obj_)
        {
            var root = obj_.transform;
            var btn = root.GetComponent<MapButton>().WithClickScale().FixPivot();
            return new Entry
            {
                obj = obj_, root = root,
                button = btn,
                visual = root.Access<UIVisualGroup>(),
                icon = root.Access<UIImageRes>("icon"),
                img = root.Access<UIImageRes>("img"),
                iconState = root.Access<UIStateGroup>(),
                cd = root.Access<TextMeshProUGUI>("cd"),
                start = root.Access<TextMeshProUGUI>("img/start"),
                actCd = root.Access<TextMeshProUGUI>("img/actCd"),
                orderCd = root.Access<TextMeshProUGUI>("orderCd"),
                dot = root.TryFind("dotCount"),
                dotCount = root.Access<TextMeshProUGUI>("dotCount/Count"),
                sale = root.TryFind("icon/sale"),
                token = root.Access<TextMeshProUGUI>("icon/token"),
                innerTxt = root.Access<TextMeshProUGUI>("icon/innerTxt"),
                discount = root.Access<TextMeshProUGUI>("icon/discount"),
                tokenState = root.Access<UITextState>("icon/token"),
                frame = root.Access<UIImageState>("icon/frame"),
                up = root.Access<UIImageRes>("icon/_up"),
                flag = root.Access<UIImageRes>("icon/flag"),
                progress = root.Access<UICommonProgressBar>("progress", true)
            };
        }

        public void Start()
        {
            SetupEntry(list[0]);
        }

        public void OnEnable()
        {
            SortPriority ??= (a_, b_) => b_.Priority - a_.Priority;
            WhenUpdate ??= Refresh;
            WhenFocus ??= b => Visible(b == null);
            WhenTick ??= RefreshCD;
            Visible(!Game.Manager.mapSceneMan.Focus);
            Refresh();
            RefreshCD();
            Get<ACTIVITY_UPDATE>().AddListener(WhenUpdate);
            Get<MAP_FOCUS_CHANGE>().AddListener(WhenFocus);
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        public void OnDisable()
        {
            void ClearE(Entry e)
            {
                e.setup?.Clear(e);
                e.setup = null;
                e.activity = null;
            }
            foreach (var e in list) ClearE(e);
            Get<ACTIVITY_UPDATE>().RemoveListener(WhenUpdate);
            Get<MAP_FOCUS_CHANGE>().RemoveListener(WhenFocus);
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public void Visible(bool v_)
        {
            group.SetActive(v_);
        }

        public void Refresh()
        {
            void FilterFirst()
            {
                if (listAct.Count <= 0) return;
                var a = listAct[0];
                listAct.Clear();
                listAct.Add(a);
            }

            void ClearE(Entry e)
            {
                e.setup?.Clear(e);
                e.setup = null;
                e.activity = null;
            }

            listAct.Clear();
            var map = Game.Manager.activity.map;

            static Pos P(ActivityLike a_)
            {
                if (a_.Type is EventType.Invite) return Pos.BottomLeft;
                return a_.Visual.EntryOnLeft ? Pos.TopLeft : Pos.TopRight;
            }

            foreach (var (_, a) in map)
                if (a.EntryVisible && P(a) == filterPos)
                    listAct.Add(a);
            var count = listAct.Count;
            if (count == 0) goto end;
            listAct.Sort(SortPriority);
            if (filterFirst) FilterFirst();
            count = listAct.Count;
            var template = list[0];
            for (var n = list.Count; n < count; ++n)
            {
                var obj = Instantiate(template.obj, template.root.parent);
                var e = ParseEntry(obj);
                SetupEntry(e);
                list.Add(e);
            }

            for (var n = 0; n < count; ++n)
            {
                var p = listAct[n];
                var e = list[n];
                e.obj.SetActive(true);
                ClearE(e);
                RefreshEntry(e, p);
            }

        end:
            for (var n = count; n < list.Count; ++n)
            {
                var e = list[n];
                e.obj.SetActive(false);
                ClearE(e);
            }
        }

        public void RefreshCD()
        {
            var t = Game.TimestampNow();
            foreach (var e in list)
            {
                if (!e.obj.activeSelf) break;
                var diff = (long)Mathf.Max(0, e.activity.endTS - t);
                e.cd.text = e.setup?.TextCD(diff) ?? UIUtility.CountDownFormat(diff);
            }
        }

        public static void SetupEntry(Entry e_)
        {
            e_.button.WhenClick = () => PackClick(e_.activity);
        }

        public static void PackClick(ActivityLike pack_)
        {
            pack_.Open();
        }

        public static void RefreshEntry(Entry e_, ActivityLike p_)
        {
            e_.activity = p_;
            e_.icon.SetImage(p_.EntryIcon);
            e_.cd.gameObject.SetActive(true);
            e_.dot.SetActive(false);
            e_.dotCount.gameObject.SetActive(false);
            e_.sale.SetActive(_CheckIsShowSale(p_));
            e_.token.gameObject.SetActive(false);
            e_.innerTxt.gameObject.SetActive(false);
            e_.discount.gameObject.SetActive(false);
            e_.frame.gameObject.SetActive(false);
            e_.up.gameObject.SetActive(false);
            e_.flag.gameObject.SetActive(false);
            if (e_.img != null)
                e_.img.gameObject.SetActive(false);
            if (e_.progress != null)
                e_.progress.gameObject.SetActive(false);
            if (e_.orderCd != null)
                e_.orderCd.gameObject.SetActive(false);
            e_.setup = p_ switch
            {
                DecorateActivity pDeco => new EntryDecorate(e_, pDeco),
                ActivityTreasure pTresure => pTresure.SetupEntry(e_),
                PackRetention packRetention => packRetention.SetupEntry(e_),
                ActivityDigging digging => digging.SetupEntry(e_),
                ActivityInvite invite => invite.SetupEntry(e_),
                ActivityRanking ranking => ranking.SetupEntry(e_),
                PackGemThreeForOne gemThreeForOne => new EntryPackGemThreeForOne(e_, gemThreeForOne),
                ActivityOrderChallenge orderChallenge => orderChallenge.SetupEntry(e_),
                ActivityPachinko pachinko => new PachinkoEntry(e_),
                MiniBoardMultiActivity miniBoardMultiActivity => new MiniBoardMultiEntry(e_, miniBoardMultiActivity),
                ActivityGuess guess => guess.SetupEntry(e_),
                PackDiscount packDiscount => new PackDiscountEntry(e_, packDiscount),
                ActivityBingo bingo => new ActivityBingo.EntryWrapper(e_, bingo),
                MineBoardActivity mineBoard => new MineBoardEntry(e_, mineBoard),
                MineCartActivity mineCart => new MineCartEntry(e_, mineCart),
                ActivityFishing fishing => new FishingEntry(e_, fishing),
                FarmBoardActivity farm => new FarmBoardEntry(e_, farm),
                ActivityWeeklyTask weeklyTask => new WeeklyTaskEntry(e_, weeklyTask),
                BPActivity bpActivity => new BPEntry(e_, bpActivity),
                PackErgList packErgList => packErgList.SetupEntry(e_),
                ActivityRedeemShopLike redeemShop => new RedeemShopEntry(e_, redeemShop),
                TrainMissionActivity trainMission => new TrainMissionEntry(e_, trainMission),
                WishBoardActivity wishBoard => new WishBoardEntry(e_, wishBoard),
                ActivityWeeklyRaffle weeklyRaffle => new WeeklyRaffleEntry(e_, weeklyRaffle),
                PackSpin packSpin => new EntrySpin(e_, packSpin),
                PackLevel packLevel => new PackLevelEntry(e_, packLevel),
                FightBoardActivity fightBoard => new FightBoardEntry(e_, fightBoard),
                ActivityBingoTask bingoTask => new BingoTaskEntry(e_, bingoTask),
                ActivityMultiplierRanking multiplierRanking => new MultiplierRankingEntry(e_, multiplierRanking),
                ActivityPuzzle puzzle => new PuzzleEntry(e_, puzzle),
                ActivityOnlineReward onlineReward => new OnlineRewardEntry(e_, onlineReward),
                _ => null
            };
        }

        private static bool _CheckIsShowSale(ActivityLike p_)
        {
            if (p_ == null)
                return false;
            return p_.Type == EventType.MarketIapgift || p_.Type == EventType.OnePlusTwo;
        }
    }
}
