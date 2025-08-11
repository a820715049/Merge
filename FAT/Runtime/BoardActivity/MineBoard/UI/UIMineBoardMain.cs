
using System.Collections;
using System.Collections.Generic;
using Cysharp.Text;
using DG.Tweening;
using EL;
using FAT.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMineBoardMain : UIBase
    {
        public float Shake;
        public int DebugRow;
        public Transform TempNode;
        public Transform HideNode;
        public UIImageRes TempIcon;
        public AnimationCurve MoveCurve;
        public AnimationCurve MoveUp;
        public float MoveTime;
        public float BannerAnimTime;
        public MBBoardView _view;
        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _tokenNum;
        private TextMeshProUGUI _deepth;
        private MBMineBoardReward _reward;
        private MBMineProgress _progress;
        private MBMineHandbook _handbook;
        private MineBoardActivity _activity;
        private Animator _moveAnim;
        private RectMask2D _mask;
        private Image _boardEntry;
        //右侧挖矿1+1礼包
        private PackOnePlusOneMine _giftPack;
        private GameObject _giftPackGo;
        private UIImageRes _giftPackIcon;
        private TextMeshProUGUI _giftPackCd;
        private MBBoardAutoGuide _autoGuide;
        protected override void OnCreate()
        {
            RegiestComp();
            Setup();
            AddButton();
        }

        private void RegiestComp()
        {
            transform.Access("Content/Center/ProgressBg/_cd/text", out _cd);
            transform.Access("Content/Bottom/TokenNode/TokenNum", out _tokenNum);
            transform.Access("Content/Center/BoardNode/Bottom/Deepth", out _deepth);
            transform.Access("Content/Center/BoardRewardNode", out _reward);
            transform.Access("Content/Center/ProgressBg/Progress", out _progress);
            transform.Access("Content/Center/TopBg/MilestoneNode", out _handbook);
            transform.Access("Content/Bottom/EffectNode", out _moveAnim);
            transform.Access("Content/Center/BoardNode/CompBoard/Mask", out _mask);
            transform.FindEx("Content/Center/GiftNode", out _giftPackGo);
            transform.Access("Content/Center/GiftNode/Icon", out _giftPackIcon);
            transform.Access("Content/Center/GiftNode/cd", out _giftPackCd);
            transform.Access("Content/Bottom/FlyTarget/Entry", out _boardEntry);
            _autoGuide = GetComponent<MBBoardAutoGuide>();
        }

        private void Setup()
        {
            _reward.Setup();
            _progress.Setup();
            _handbook.Setup();
            _view.Setup();
        }

        private void AddButton()
        {
            transform.AddButton("Content/Top/HelpBtn", ClickHelp);
            transform.AddButton("Content/Top/CloseBtn", ClickClose);
            transform.AddButton("Content/Bottom/TokenNode", () => UIManager.Instance.OpenWindow(UIConfig.UIMineTokenTips, transform.Find("Content/Bottom/TokenNode").position, 50f));
            transform.AddButton("Content/Center/GiftNode", ClickPack);
        }

        private void ClickHelp()
        {
            UIManager.Instance.OpenWindow(_activity.HelpResAlt.ActiveR, _activity);
        }

        private void ClickClose()
        {
            Game.Manager.mineBoardMan.ExitMineBoard(_activity);
        }

        private void ClickPack()
        {
            if (_giftPack != null)
                UIManager.Instance.OpenWindow(_giftPack.Res.ActiveR, _giftPack);
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as MineBoardActivity;
            if (_activity == null) return;
            Game.Manager.screenPopup.Block(true, false);
            EnterBoard();
        }


        private void EnterBoard()
        {
            var world = Game.Manager.mineBoardMan.World;
            var tracer = Game.Manager.mineBoardMan.WorldTracer;
            BoardViewWrapper.PushWorld(world);
            RefreshScale(Game.Manager.mainMergeMan.mainBoardScale);
            _view.OnBoardEnter(world, tracer);
        }

        private void RefreshScale(float scale)
        {
            var root = _view.transform as RectTransform;
            root.localScale = new Vector3(scale, scale, scale);
            var move = transform.Find("Content/MoveNode/BoardNode/CompBoard");
            move.localScale = new Vector3(scale, scale, scale);
            (root.parent as RectTransform).sizeDelta = new Vector2(scale * root.sizeDelta.x, scale * root.sizeDelta.y);
            (move.parent as RectTransform).sizeDelta = new Vector2(scale * root.sizeDelta.x, scale * root.sizeDelta.y);

        }

        protected override void OnPreOpen()
        {
            _reward.Refresh();
            _progress.Refresh(_activity);
            _handbook.Refresh(_activity);
            _tokenNum.text = _activity.GetTokenNum().ToString();
            _deepth.text = Game.Manager.mineBoardMan.GetCurDepth().ToString();
            RefreshGiftPack();
            RefreshCD();
            MessageCenter.Get<MSG.UI_TOP_BAR_PUSH_STATE>().Dispatch(UIStatus.LayerState.AboveStatus);
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.UI_MINE_BOARD_UNLOCK_ITEM>().AddListener(_handbook.Unlock);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FlyFeedBack);
            MessageCenter.Get<MSG.GAME_MINE_BOARD_PROG_CHANGE>().AddListener(_progress.RegiestProgressInfo);
            MessageCenter.Get<MSG.UI_MINE_BOARD_MOVE_UP_READY>().AddListener(OnMoveUpReady);
            MessageCenter.Get<MSG.UI_MINE_BOARD_MOVE_UP_FINISH>().AddListener(OnMoveUp);
            MessageCenter.Get<MSG.UI_MINE_BOARD_MOVE_UP_COLLECT>().AddListener(OnMoveUpCollect);
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().AddListener(RefreshGiftPack);
            MessageCenter.Get<MSG.GAME_MINE_BOARD_TOKEN_CHANGE>().AddListener(OnTokenChange);
            MessageCenter.Get<MSG.FLY_ICON_START>().AddListener(CheckNewFly);
            MessageCenter.Get<MSG.GAME_BOARD_TOUCH>().AddListener(_autoGuide.Interrupt);
            MessageCenter.Get<MSG.GUIDE_OPEN>().AddListener(_autoGuide.Interrupt);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.UI_MINE_BOARD_UNLOCK_ITEM>().RemoveListener(_handbook.Unlock);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FlyFeedBack);
            MessageCenter.Get<MSG.GAME_MINE_BOARD_PROG_CHANGE>().RemoveListener(_progress.RegiestProgressInfo);
            MessageCenter.Get<MSG.UI_MINE_BOARD_MOVE_UP_READY>().RemoveListener(OnMoveUpReady);
            MessageCenter.Get<MSG.UI_MINE_BOARD_MOVE_UP_FINISH>().RemoveListener(OnMoveUp);
            MessageCenter.Get<MSG.UI_MINE_BOARD_MOVE_UP_COLLECT>().RemoveListener(OnMoveUpCollect);
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().RemoveListener(RefreshGiftPack);
            MessageCenter.Get<MSG.GAME_MINE_BOARD_TOKEN_CHANGE>().RemoveListener(OnTokenChange);
            MessageCenter.Get<MSG.FLY_ICON_START>().RemoveListener(CheckNewFly);
            MessageCenter.Get<MSG.GAME_BOARD_TOUCH>().RemoveListener(_autoGuide.Interrupt);
            MessageCenter.Get<MSG.GUIDE_OPEN>().RemoveListener(_autoGuide.Interrupt);
        }

        protected override void OnPreClose()
        {
            UIManager.Instance.CloseWindow(UIConfig.UIPopFlyTips);
            UIManager.Instance.CloseWindow(UIConfig.UIEnergyBoostTips);
            if (_commonResSeq != null) _commonResSeq.Kill();
            if (BoardViewWrapper.GetCurrentWorld() == null) return;
            _view.OnBoardLeave();
            BoardViewWrapper.PopWorld();
            _giftPack = null;
        }

        protected override void OnPostClose()
        {
            MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().Dispatch();
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
        }

        private bool _isPlayingMove;
        private void Update()
        {
            if (_isPlayingMove) return;
            BoardViewManager.Instance.Update(Time.deltaTime);
        }

        private void RefreshCD()
        {
            if (_activity == null) return;
            if (!_isPlayingMove) _autoGuide.SecondUpdate();
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            UIUtility.CountDownFormat(_cd, diff);
            if (_giftPack != null)
            {
                var diff1 = (long)Mathf.Max(0, _giftPack.endTS - t);
                UIUtility.CountDownFormat(_giftPackCd, diff1);
            }
            if (diff <= 1) ClickClose();
        }

        private void RefreshGiftPack()
        {
            _giftPack = Game.Manager.activity.LookupAny(fat.rawdata.EventType.MineOnePlusOne) as PackOnePlusOneMine;
            if (_giftPack != null)
            {
                _giftPackGo.SetActive(true);
                _giftPackIcon.SetImage(_giftPack.EntryIcon);
            }
            else
            {
                _giftPackGo.SetActive(false);
            }
        }

        private void FlyFeedBack(FlyableItemSlice slice)
        {
            if (slice.FlyType == FlyType.MergeItemFlyTarget) _reward.FlyFeedBack(slice);
            if (slice.FlyType == FlyType.MineScore) _progress.StartProgressAnim(slice);
            if (slice.FlyType == FlyType.MineToken) _tokenNum.text = _activity.GetTokenNum().ToString();
        }

        private void OnTokenChange(int tokenNum, int tokenId)
        {
            _tokenNum.text = _activity.GetTokenNum().ToString();
        }

        private void CheckNewFly(FlyableItemSlice slice)
        {
            if (_activity == null) return;
            if (slice.FlyType == FlyType.TapBonus || slice.FlyType == FlyType.FlyToMainBoard)
            {
                CheckTapBonus();
            }
            if (slice.FlyType == FlyType.Coin || slice.FlyType == FlyType.Gem || slice.FlyType == FlyType.Energy)
            {
                CheckCommonRes();
            }
        }

        private bool _isTapBonus;
        private void CheckTapBonus()
        {
            if (_isTapBonus) return;
            _isTapBonus = true;
            var seq = DOTween.Sequence();
            seq.Append(_boardEntry.DOFade(1, 0.5f));
            seq.AppendInterval(0.5f);
            seq.Append(_boardEntry.DOFade(0, 0.5f));
            seq.AppendCallback(() => _isTapBonus = false);
            seq.OnKill(() =>
            {
                var color = Color.white;
                color.a = 0;
                _boardEntry.color = color;
                _isTapBonus = false;
            });
            seq.Play();
        }

        private bool _isCheckCommonRes;
        private Sequence _commonResSeq;
        private void CheckCommonRes()
        {
            if (_isCheckCommonRes) return;
            _isCheckCommonRes = true;
            _commonResSeq = DOTween.Sequence();
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
            _commonResSeq.AppendInterval(1.5f);
            _commonResSeq.AppendCallback(() =>
            {
                MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
                _isCheckCommonRes = false;
                _commonResSeq = null;
            });
            _commonResSeq.OnKill(() =>
            {
                MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
                _isCheckCommonRes = false;
                _commonResSeq = null;
            });
            _commonResSeq.Play();
        }
        private void OnMoveUpReady()
        {
            _isPlayingMove = true;
            UIManager.Instance.Block(true);
            BoardViewManager.Instance.OnUserActive();
            IEnumerator coroutine()
            {
                yield return new WaitForSeconds(1.5f);
                Game.Manager.mineBoardMan.StartMoveUpBoard();
            }
            Game.Instance.StartCoroutineGlobal(coroutine());
        }

        private void OnMoveUpCollect(List<Item> collectItemList, int upRowCount)
        {
            _mask.enabled = true;
            PrepareTempIcon(collectItemList);
        }

        private void OnMoveUp(int upRowCount)
        {
            PrepareBg(upRowCount);
            _view.boardHolder.ReFillItem();
            StartMoveUp(upRowCount);
        }

        private void PrepareBg(int upRowCount)
        {

            var moveDis = upRowCount * _view.cellSize * Game.Manager.mainMergeMan.mainBoardScale;
            var root = _view.transform.Find("Mask/Root");
            var bg = _view.transform.Find("Mask/Root/BgBoard");
            var till = root.Find("BgBoard/Pattern").GetComponent<UITilling>();
            (bg.transform as RectTransform).sizeDelta = new Vector2(0f, _view.cellSize * upRowCount);
            root.localPosition += new Vector3(0, -moveDis, 0);
            till.SetTilling(new Vector2(Game.Manager.mergeBoardMan.activeWorld.activeBoard.size.x * 0.5f, (Game.Manager.mergeBoardMan.activeWorld.activeBoard.size.y + upRowCount) * 0.5f));
        }

        private List<MBMineTempIcon> _tempIcons = new List<MBMineTempIcon>();
        private void PrepareTempIcon(List<Item> collectItemList)
        {
            foreach (var item in collectItemList)
            {
                var tempIcon = HideNode.childCount > 1 ? HideNode.GetChild(1).GetComponent<MBMineTempIcon>() : Instantiate(TempIcon.gameObject, HideNode).GetComponent<MBMineTempIcon>();
                tempIcon.transform.SetParent(TempNode, false);
                tempIcon.SetImage(item, HideNode);
                _tempIcons.Add(tempIcon);
            }
        }

        private void StartMoveUp(int upRowCount)
        {
            _moveAnim.SetTrigger("Punch");
            _moveAnim.SetBool("Start", true);
            var str = upRowCount switch
            {
                1 => "MineBoardupOne",
                2 => "MineBoardupTwo",
                3 => "MineBoardupThree",
                4 => "MineBoardupFour",
                _ => "MineBoardupFour"
            };
            Game.Manager.audioMan.TriggerSound(str);
            var speed = _view.cellSize * Game.Manager.mainMergeMan.mainBoardScale / MoveTime;
            _tempIcons.ForEach(temp => temp.SetSpeed(speed));
            var moveDis = upRowCount * _view.cellSize * Game.Manager.mainMergeMan.mainBoardScale;
            var root = _view.transform.Find("Mask/Root");
            var seqY = DOTween.Sequence();
            seqY.Append(DOTween.To(() => root.localPosition, x => root.localPosition = x, root.localPosition + new Vector3(0, moveDis, 0), MoveTime * upRowCount).SetEase(MoveUp).SetOptions(AxisConstraint.Y));
            var seqX = DOTween.Sequence();
            for (int i = 0; i < upRowCount; i++)
            {
                seqX.Append(DOTween.To(() => root.localPosition, x => root.localPosition = x, root.localPosition + new Vector3(Shake, 0, 0), MoveTime).SetEase(MoveCurve).SetOptions(AxisConstraint.X));
                seqX.Join(DOTween.To(() => TempNode.localPosition, x => TempNode.localPosition = x, TempNode.localPosition + new Vector3(Shake, 0, 0), MoveTime).SetEase(MoveCurve).SetOptions(AxisConstraint.X));
            }
            seqY.AppendCallback(() =>
            {
                var till = root.Find("BgBoard/Pattern").GetComponent<UITilling>();
                var bg = _view.transform.Find("Mask/Root/BgBoard");
                till.SetTilling(new Vector2(Game.Manager.mergeBoardMan.activeWorld.activeBoard.size.x * 0.5f, Game.Manager.mergeBoardMan.activeWorld.activeBoard.size.y * 0.5f));
                (bg.transform as RectTransform).sizeDelta = Vector2.zero;
                root.localPosition = Vector3.zero;
                UIManager.Instance.Block(false);
                UIManager.Instance.OpenWindow(_activity.BannerResAlt.ActiveR, _activity, ZString.Format(I18N.Text("#SysComDesc909"), Game.Manager.mineBoardMan.GetCurDepth(), I18N.Text("#SysComDesc892")));
                _isPlayingMove = false;
                _moveAnim.SetBool("Start", false);
                _mask.enabled = false;
                _tempIcons.Clear();
                _view.boardHolder.ReFillItem();
                _deepth.text = Game.Manager.mineBoardMan.GetCurDepth().ToString();
            });

        }

        #region Debug
        public void DebugMoveUp()
        {
            UIManager.Instance.OpenWindow(_activity.HandBookResAlt.ActiveR, _activity);
            _activity.HandBookTheme.TextMap.TryGetValue("desc1", out var mainTitle);
            _activity.HandBookTheme.TextMap.TryGetValue("mainTitle", out var subTitle);
            IEnumerator bannerAnim()
            {
                yield return new WaitForSeconds(BannerAnimTime);
                UIManager.Instance.OpenWindow(_activity.BannerResAlt.ActiveR, _activity, ZString.Format(I18N.Text(mainTitle), I18N.Text(subTitle)), true);
            }
            StartCoroutine(bannerAnim());
        }
        #endregion
    }
}
