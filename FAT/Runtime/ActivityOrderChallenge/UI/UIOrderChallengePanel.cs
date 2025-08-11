/*
 *@Author:chaoran.zhang
 *@Desc:连续订单挑战主界面UI表现逻辑
 *@Created Time:2024.11.11 星期一 11:11:52
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Cysharp.Text;
using DG.Tweening;
using EL;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace FAT
{
    public class UIOrderChallengePanel : UIBase
    {
        [SerializeField] private RectTransform _targetRoot;
        [SerializeField] private RectTransform _scaleRoot;
        [SerializeField] private Transform _standRoot;
        [SerializeField] private Transform _hideRoot;
        [SerializeField] private TextMeshProUGUI _roundNum;
        [SerializeField] private TextMeshProUGUI _playerNum;
        [SerializeField] private TextMeshProUGUI _rewardNum;
        [SerializeField] private UIImageRes _rewardIcon;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private GameObject _failNode;
        [SerializeField] private GameObject _timeNode;
        [SerializeField] private TextMeshProUGUI _levelTime;
        [SerializeField] private TextMeshProUGUI _levelTxt;
        [SerializeField] private Transform _timeBg;
        [SerializeField] private Animator _numAnimator;
        [SerializeField] private Animator _levelAnimator;
        [SerializeField] private float _dalay;
        [SerializeField] private float _textAnimDur;
        private GameObject _player;
        private readonly OrderChallengePlayerManager _playerManager = new();
        private readonly OrderChallengeStandManager _standManager = new();
        private ActivityOrderChallenge _activity;
        private int _level => _activity.CurLevelIndex;
        private int _lastLevel = -1;
        private bool _hasInit;
        private bool _start;
        private bool _fail;

        protected override void OnCreate()
        {
            InitButton();
            InitPlayerManager();
            InitStandManager();
        }

        private void InitButton()
        {
            transform.AddButton("Content/Top/Close_btn", Close);
            transform.AddButton("Content/Top/Info_btn", () => _activity.HelpRes.ActiveR.Open(_activity));
            transform.AddButton("Content/Go_btn", Close);
        }

        private void InitPlayerManager()
        {
            _player = _hideRoot.GetChild(0).gameObject;
            _playerManager.Init(15, _hideRoot, _standRoot, _player);
        }

        private void InitStandManager()
        {
            _standManager.Init(_standRoot);
            _standManager.AddClick(ClickStand);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0) _activity = items[0] as ActivityOrderChallenge;
            UIUtility.CountDownFormat(_cd, _activity?.Countdown ?? 0);
            Game.Manager.audioMan.TriggerSound("OrderChallengeActAmb");
            var totalSec = _activity?.GetCurrentOrderCD() ?? 0;
            var min = totalSec / 60;
            var sec = totalSec % 60;
            var minstr = min < 10 ? "0" + min : min.ToString();
            var secstr = sec < 10 ? "0" + sec : sec.ToString();
            _levelTime.text = minstr + ":" + secstr;
            _fail = items.Length > 3;
            if (_fail)
            {
                RefreshText();
                _timeNode.SetActive(false);
                _failNode.SetActive(true);
                if (!_hasInit) InitShow(false);
                _standManager.SetStandAnim(_level, false);
                ShowEnd();
                return;
            }

            _start = items.Length > 2;
            _failNode.SetActive(false);

            if (_start)
            {
                RefreshText();
                _timeNode.SetActive(false);
                _lastLevel = 0;
                _playerManager.ResetStand();
                _standManager.SetStandAnim(_level, _start);
                InitShow(true);
                UIManager.Instance.OpenWindow(UIConfig.UIOrderChallengeMatch, _activity, new Action(ShowStart));
                _start = false;
                return;
            }

            if (!_hasInit)
            {
                _timeNode.SetActive(false);
                RefreshText();
                if (items.Length > 1)
                {
                    _hasInit = true;
                    _playerManager.ShowStartStand(_standManager.GetStandByIndex(_level),
                        _activity.GetCurLevelShowNum(), _level, _activity);
                    _standManager.SetStandAnim(_level - 1, false);
                    _levelTxt.text = (_activity?.CurLevelIndex + 1).ToString();
                    _playerManager.CheckLastIcon(_activity, _standManager.GetStandByIndex(_level));
                    PlayAnim();
                    return;
                }
                else
                {
                    InitShow(_start);
                    _standManager.SetStandAnim(_level, _start);
                }

                _lastLevel = _level;
                var width = _standManager.GetStandByIndex(_level + 1).width;
                var height = _standManager.GetStandByIndex(_level + 1).height;
                var offsetX = (_timeNode.transform as RectTransform).rect.width / 2;
                _timeNode.transform.localPosition =
                    _standManager.GetStandByIndex(_level + 1).transform.localPosition.x > 0
                        ? _standManager.GetStandByIndex(_level + 1).transform.localPosition +
                          Vector3.left * (width / 2 + offsetX) +
                          Vector3.up * height / 2
                        : _standManager.GetStandByIndex(_level + 1).transform.localPosition +
                          Vector3.right * (width / 2 + 120f) + Vector3.up * height / 2;
                _timeBg.transform.eulerAngles =
                    _standManager.GetStandByIndex(_level + 1).transform.localPosition.x < 0
                        ? 180 * Vector3.up
                        : Vector3.zero;
                _timeNode.SetActive(_level < 6);
            }

            if (_lastLevel < _level)
            {
                _timeNode.SetActive(false);
                _levelTxt.text = (_activity?.CurLevelIndex + 1).ToString();
                _standManager.SetStandAnim(_level - 1, false);
                _playerManager.CheckLastIcon(_activity, _standManager.GetStandByIndex(_level));
                PlayAnim();
            }
            else
            {
                RefreshText();
                _playerManager.CheckIcon(_activity, _standManager.GetStandByIndex(_lastLevel + 1));
                _standManager.SetStandAnim(_level, false);
            }
        }

        private void InitShow(bool justStart)
        {
            _hasInit = true;
            _playerManager.ShowStartStand(_standManager.GetStandByIndex(justStart ? 0 : _level + 1),
                _activity.GetCurLevelShowNum(), _level, _activity);
        }

        private void RefreshText()
        {
            if (_activity == null) return;
            _rewardNum.text = _activity.curLevelTotalReward.Count.ToString();
            _rewardIcon.SetImage(Game.Manager.objectMan.GetBasicConfig(_activity.curLevelTotalReward.Id).Icon);
            _roundNum.text = _activity.CurLevelIndex + "/" + _activity.LevelCount;
            _playerNum.text = _activity.PlayerShowInfo;
            _levelTxt.text = (_activity.CurLevelIndex + 1).ToString();
        }

        private void PlayAnim()
        {
            _lastLevel = _level;
            _standManager.GetStandByIndex(_level + 1).RandomPos(_activity.GetCurLevelShowNum(), false);
            _playerManager.SortList(false);
            Game.Instance.StartCoroutineGlobal(_playerManager.MoveNextStand(_standManager.GetStandByIndex(_level + 1),
                _level + 1, _activity, _standManager.GetStandByIndex(_level), _timeNode));
            var width = _standManager.GetStandByIndex(_level + 1).width;
            var height = _standManager.GetStandByIndex(_level + 1).height;
            var offsetX = (_timeNode.transform as RectTransform).rect.width / 2;
            _timeNode.transform.localPosition = _standManager.GetStandByIndex(_level + 1).transform.localPosition.x > 0
                ? _standManager.GetStandByIndex(_level + 1).transform.localPosition +
                  Vector3.left * (width / 2 + offsetX) +
                  Vector3.up * height / 2
                : _standManager.GetStandByIndex(_level + 1).transform.localPosition +
                  Vector3.right * (width / 2 + 120f) + Vector3.up * height / 2;
            _timeBg.transform.eulerAngles = _standManager.GetStandByIndex(_level + 1).transform.localPosition.x < 0
                ? 180 * Vector3.up
                : Vector3.zero;

            IEnumerator enumerator()
            {
                yield return new WaitForSeconds(_dalay);
                Game.Manager.audioMan.TriggerSound("OrderChallengeActNum");
                _roundNum.text = _activity.CurLevelIndex + "/" + _activity.LevelCount;
                _levelAnimator.SetTrigger("Punch");
                int.TryParse(_playerNum.text.Split("/")[0], out var _last);
                DOTween.To(() => _last, x =>
                    {
                        _last = x;
                        _playerNum.text = _last + "/" + _activity.TotalNum;
                    }, _activity.GetFinalLeftNumWhenJump(), _textAnimDur)
                    .OnComplete(() => { _numAnimator.SetTrigger("Punch"); });
            }

            Game.Instance.StartCoroutineGlobal(enumerator());
        }

        private void ShowStart()
        {
            _standManager.GetStandByIndex(1).RandomPos(15, true);
            Game.Instance.StartCoroutineGlobal(_playerManager.ShowStartMove(_standManager.GetStandByIndex(1), 1,
                _standManager.GetStandByIndex(0), _timeNode));
            var width = _standManager.GetStandByIndex(1).width;
            var height = _standManager.GetStandByIndex(1).height;
            var offsetX = (_timeNode.transform as RectTransform).rect.width / 2;
            _timeNode.transform.localPosition = _standManager.GetStandByIndex(_level + 1).transform.localPosition.x > 0
                ? _standManager.GetStandByIndex(_level + 1).transform.localPosition +
                  Vector3.left * (width / 2 + offsetX) +
                  Vector3.up * height / 2
                : _standManager.GetStandByIndex(_level + 1).transform.localPosition +
                  Vector3.right * (width / 2 + 120f) + Vector3.up * height / 2;
            _timeBg.transform.eulerAngles = _standManager.GetStandByIndex(_level + 1).transform.localPosition.x < 0
                ? 180 * Vector3.up
                : Vector3.zero;
        }

        private void ShowEnd()
        {
            _lastLevel = _level;
            _standManager.GetStandByIndex(_level + 2).RandomPos(14, false);
            _playerManager.SortList(false);
            Game.Instance.StartCoroutineGlobal(_playerManager.ShowEnd(_standManager.GetStandByIndex(_level + 2),
                _level + 2, _activity, _standManager.GetStandByIndex(_level + 1)));
        }

        protected override void OnPostClose()
        {
            Game.Manager.audioMan.StopLoopSound();
            if (!_fail) return;
            _fail = false;
            _activity.TryOpenStart();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void RefreshCD()
        {
            if (_activity == null) return;
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
            var totalSec = _activity.GetCurrentOrderCD();
            if (totalSec < 0) { totalSec = 0; }
            var min = totalSec / 60;
            var sec = totalSec % 60;
            _levelTime.SetTextFormat("{0:D2}:{1:D2}", min, sec);
            if ((_activity.Countdown < 0 || totalSec <= 0) && !UIManager.Instance.IsBlocked) { Close(); }
        }

        private void ClickStand(int index, MBOrderChallengeStand stand)
        {
            var str = string.Empty;

            if (index == -1)
            {
                str = I18N.Text("#SysComDesc689");
                UIManager.Instance.OpenWindow(UIConfig.UIOrderChallengeTips, stand.transform.position, -40f, str,
                    false);
            }
            else if (index == _level)
            {
                str = I18N.FormatText("#SysComDesc691", _activity.PlayerShowInfo);
                UIManager.Instance.OpenWindow(UIConfig.UIOrderChallengeTips, stand.transform.position, 100f, str);
            }
            else if (index < _level)
            {
                return;
            }
            else
            {
                str = I18N.Text("#SysComDesc690");
                UIManager.Instance.OpenWindow(UIConfig.UIOrderChallengeTips, stand.transform.position, 20f, str);
            }
        }
    }

    public class OrderChallengePlayerManager
    {
        public int Count => _showList.Count;
        private Transform _showNode;
        private Transform _hideNode;
        private MBOrderChallengePlayer _me;
        private readonly List<MBOrderChallengePlayer> _showList = new();
        private readonly List<MBOrderChallengePlayer> _hideList = new();

        public void Init(int num, Transform hide, Transform stand, GameObject obj)
        {
            _hideNode = hide;
            _showNode = stand;
            _me = obj.GetComponent<MBOrderChallengePlayer>();
            _me.Init(true);
            for (var i = 0; i < num; i++)
            {
                var _temp = Object.Instantiate(obj, hide);
                _hideList.Add(_temp.GetComponent<MBOrderChallengePlayer>());
            }

            foreach (var player in _hideList) player.Init();
        }

        public void SortList(bool h)
        {
            if (h)
                _showList.Sort((a, b) => b._targetPos.x.CompareTo(a._targetPos.x));
            else
                _showList.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
        }

        public IEnumerator MoveNextStand(MBOrderChallengeStand stand, int index, ActivityOrderChallenge activity,
            MBOrderChallengeStand last, GameObject time)
        {
            var i = 0;
            _me.SetAnim(index);
            _me.SetTarget(stand.PlayerPos);
            Game.Instance.StartCoroutineGlobal(_me.MoveNext(stand, 0));
            Game.Manager.audioMan.TriggerSound("OrderChallengeActJump");
            yield return null;
            var outNum = activity.OutPlayerShowNum;
            var outList = new List<MBOrderChallengePlayer>();
            for (var j = 0; j < outNum; j++)
            {
                outList.Add(_showList[^1]);
                _showList.RemoveAt(_showList.Count - 1);
            }

            foreach (var player in _showList)
            {
                player.SetAnim(index);
                var delay = stand.interval - i * stand.sub;
                player.SetTarget(stand.GetPosByIndex(i));
                Game.Instance.StartCoroutineGlobal(player.MoveNext(stand,
                    stand.delay + (stand.interval + delay) * i / 2));
                i++;
            }

            foreach (var VARIABLE in outList)
                Game.Instance.StartCoroutineGlobal(VARIABLE.FailAnim(_showList.Count * stand.interval + stand.delay,
                    _hideNode));
            _hideList.AddRange(outList);
            Game.Instance.StartCoroutineGlobal(last.PlayHide(_showList.Count * stand.interval + stand.delay));

            IEnumerator check()
            {
                yield return new WaitForSeconds(_showList.Count * stand.interval + stand.delay + 3f);
                time.SetActive(index < 7);
                activity.TryOpenVictory();
            }

            Game.Instance.StartCoroutineGlobal(check());
        }

        public IEnumerator ShowStartMove(MBOrderChallengeStand stand, int index, MBOrderChallengeStand last,
            GameObject time)
        {
            var i = 0;
            _me.SetAnim(index);
            _me.SetTarget(stand.PlayerPos);
            Game.Instance.StartCoroutineGlobal(_me.MoveNext(stand, 0));
            Game.Manager.audioMan.TriggerSound("OrderChallengeActJump");
            yield return null;
            foreach (var player in _showList)
            {
                player.SetTarget(stand.GetPosByIndex(i));
                i++;
            }

            SortList(false);
            var j = 0;
            foreach (var player in _showList)
            {
                player.SetAnim(index);
                var delay = stand.interval - j * stand.sub;
                var max = Mathf.Floor(stand.interval / stand.sub) + 1;
                Game.Instance.StartCoroutineGlobal(player.MoveNext(stand,
                    stand.delay + (stand.interval + delay) * j / 2));
                j++;
            }

            Game.Instance.StartCoroutineGlobal(last.PlayHide(_showList.Count * stand.interval + stand.delay));
            yield return new WaitForSeconds(_showList.Count * stand.interval + stand.delay);
            time.SetActive(true);
        }

        public IEnumerator ShowEnd(MBOrderChallengeStand stand, int index, ActivityOrderChallenge activity,
            MBOrderChallengeStand last)
        {
            UIManager.Instance.Block(true);
            var i = 0;
            yield return null;
            var outNum = Random.Range(3, 6);
            var outList = new List<MBOrderChallengePlayer>();
            for (var j = 0; j < outNum; j++)
            {
                outList.Add(_showList[^1]);
                _showList.RemoveAt(_showList.Count - 1);
            }

            foreach (var player in _showList)
            {
                player.SetAnim(index);
                var delay = stand.interval - i * stand.sub;
                player.SetTarget(stand.GetPosByIndex(i));
                Game.Instance.StartCoroutineGlobal(player.MoveNext(stand,
                    (stand.interval + delay) * i / 2));
                i++;
            }

            foreach (var VARIABLE in outList)
                Game.Instance.StartCoroutineGlobal(VARIABLE.FailAnim(_showList.Count * stand.interval,
                    _hideNode));
            _hideList.AddRange(outList);

            Game.Manager.audioMan.TriggerSound("OrderChallengeLose");
            Game.Instance.StartCoroutineGlobal(_me.FailAnim(_showList.Count * stand.interval + 1.5f, _hideNode));
            Game.Instance.StartCoroutineGlobal(last.PlayHide(_showList.Count * stand.interval, false));
            yield return new WaitForSeconds(_showList.Count * stand.interval + stand.delay + 2.7f);
            UIManager.Instance.Block(false);
            UIManager.Instance.OpenWindow(UIConfig.UIOrderChallengeFail, activity);
        }

        public void ShowStartStand(MBOrderChallengeStand stand, int num, int level, ActivityOrderChallenge activity)
        {
            for (var i = 0; i < num - 1; i++)
            {
                _showList.Add(_hideList.First());
                _hideList.RemoveAt(0);
            }

            var j = 0;
            stand.RandomPos(level == 0 ? 15 : activity.GetCurLevelShowNum(), level == 0);
            foreach (var item in _showList)
            {
                item.transform.SetParent(_showNode);
                item.transform.localPosition = stand.GetPosByIndex(j++);
            }

            _me.transform.SetParent(_showNode);
            _me.transform.localPosition = stand.PlayerPos;
        }

        public void ResetStand()
        {
            foreach (var item in _showList)
            {
                item.transform.SetParent(_hideNode);
                _hideList.Add(item);
            }

            _showList.Clear();
            _me.transform.SetParent(_hideNode);
        }

        public void CheckIcon(ActivityOrderChallenge activity, MBOrderChallengeStand stand)
        {
            if (_showList.Count == activity.GetCurLevelShowNum() - 1)
                return;
            var need = activity.GetCurLevelShowNum() - _showList.Count;
            for (var i = 0; i < need; i++)
            {
                _hideList[0].transform.SetParent(_showNode);
                _hideList[0].transform.SetSiblingIndex(_showNode.childCount - 2);
                _hideList[0].transform.localPosition = stand.GetPosByIndex(_showList.Count);
                _showList.Add(_hideList[0]);
                _hideList.RemoveAt(0);
            }
        }

        public void CheckLastIcon(ActivityOrderChallenge activity, MBOrderChallengeStand stand)
        {
            if (_showList.Count == activity.GetLastLevelShowNum() - 1)
                return;
            var need = activity.GetCurLevelShowNum() - _showList.Count;
            for (var i = 0; i < need; i++)
            {
                _hideList[0].transform.SetParent(_showNode);
                _hideList[0].transform.SetSiblingIndex(_showNode.childCount - 2);
                _hideList[0].transform.localPosition = stand.GetPosByIndex(_showList.Count);
                _showList.Add(_hideList[0]);
                _hideList.RemoveAt(0);
            }
        }
    }

    public class OrderChallengeStandManager
    {
        private List<MBOrderChallengeStand> _list = new();

        public void Init(Transform root)
        {
            _list.Clear();
            for (var i = 0; i < root.childCount; i++)
                if (root.GetChild(i).GetComponent<MBOrderChallengeStand>() != null)
                    _list.Add(root.GetChild(i).GetComponent<MBOrderChallengeStand>());
            foreach (var stand in _list) stand.Init();
        }

        public MBOrderChallengeStand GetStandByIndex(int index)
        {
            return _list[index];
        }

        public void SetStandAnim(int index, bool start)
        {
            if (start)
            {
                foreach (var item in _list) item.SetAnim(true);
                return;
            }

            for (var i = 0; i < index + 1; i++) _list[i].SetAnim(false);
        }

        public void AddClick(Action<int, MBOrderChallengeStand> click)
        {
            foreach (var stand in _list) stand.AddClick(click);
        }
    }
}