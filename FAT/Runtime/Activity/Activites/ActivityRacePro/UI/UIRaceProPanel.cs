using System.Collections;
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIRaceProPanel : UIBase
    {
        [Header("组件")]
        [SerializeField] private RaceProReward firstReward;
        [SerializeField] private RaceProReward secondReward;
        [SerializeField] private RaceProReward thirdReward;
        [SerializeField] private List<RaceProTrack> tracks = new();
        
        [Header("参数")]
        [SerializeField] private float _time; //上升动画时间
        [SerializeField] private float _time2; //奖励展示时间
        
        [SerializeField] private GameObject finishNode;
        [SerializeField] private TextMeshProUGUI score;
        [SerializeField] private TextMeshProUGUI cd;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private RaceProTrack playerTrack;
        
        private ActivityRaceExtend _activityRace;
        private List<RaceProTrack> _allTracks = new();
        private List<RaceProTrack> _needAnim = new();
        private bool _init = false;
        private bool _isPlayingAnim = false;
        private RaceProTrack _firstTrack;
        private RaceProTrack _secondTrack;
        private RaceProTrack _thirdTrack;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Close/CloseBtn", Close);
            transform.AddButton("Content/ConfirmBtn", Close);
            transform.AddButton("Content/InfoBtn", OpenHelp);
        }

        protected override void OnParse(params object[] items)
        {
            _activityRace = (ActivityRaceExtend)items[0];
        }

        protected override void OnPreOpen()
        {
            InitPanel();
            foreach (var track in _allTracks)
            {
                track.CheckShow();
            }

            RefreshCd();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
        }

        protected override void OnPostOpen()
        {
            RefreshInfo();
            CheckAnim();
            // _activityRace.Block = false;
        }

        //初始化面板状态
        private void InitPanel()
        {
            // _activityRace = RaceManager.GetInstance().Race;
            score.text = _activityRace.raceExtendRoundConfig.Score.ToString();
            _activityRace.mainPopup.visual.Refresh(title, "mainTitle1");
            //  || _activityRace.RefreshPanel 检测新一轮
            if (!_init)
            {
                Game.Manager.audioMan.TriggerSound("HotAirGameStart");
                finishNode.SetActive(false);
                firstReward.ResetState();
                secondReward.ResetState();
                thirdReward.ResetState();
                InitTrack();
                InitReward();
                SetReward();
                _init = true;
                // _activityRace.RefreshPanel = false;
            }
        }

        //检测是否已经结束
        private bool CheckEnd()
        {
            if (!_activityRace.hasStart && !_activityRace.CheckHasRoundReward())
            {
                finishNode.gameObject.SetActive(true);
                return true;
            }

            finishNode.gameObject.SetActive(false);
            return false;
        }

        private void RefreshInfo()
        {
            // RaceManager.GetInstance().RefreshScore();
        }

        //检测并播放动画
        private void CheckAnim()
        {
            _needAnim.Clear();
            var hasChange = false; //是否有积分变化，只有在积分有变化的情况下，才有后续对一系列动画，因此用这个作为判断依据
            var hasFinish = false; //是否有玩家需要播放获得奖励动画

            foreach (var track in _allTracks)
            {
                if (track.HasChange())
                {
                    hasChange = true;
                    _needAnim.Add(track);
                    if (track.HasFinishAnim())
                        hasFinish = true;
                }

                track.RefreshInfo();
            }

            if (!hasChange)
                return;
            RefreshReward();
            SetReward();
            Game.Instance.StartCoroutineGlobal(PlayChangeAnim(!hasFinish));
        }

        //初始化track信息
        private void InitTrack()
        {
            //玩家信息初始化
            playerTrack.InitPlayer(_activityRace);
            //机器人信息初始化
            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].InitBot(_activityRace, _activityRace.raceExtendManager.robots[i]);
            }

            _allTracks.Clear();
            _allTracks.AddRange(tracks);
            _allTracks.Add(playerTrack);
            _firstTrack = null;
            _secondTrack = null;
            _thirdTrack = null;
        }

        //刷新获奖信息
        private void RefreshReward()
        {
            _firstTrack = null;
            _secondTrack = null;
            _thirdTrack = null;
            var tempList = new List<RaceProTrack>();
            tempList.AddRange(_allTracks);
            foreach (var track in _allTracks)
            {
                if (track.IsNull || !track.Enable)
                    continue;
                switch (track.Score)
                {
                    case -1:
                        {
                            _firstTrack = track;
                            tempList.Remove(track);
                            break;
                        }
                    case -2:
                        {
                            _secondTrack = track;
                            tempList.Remove(track);
                            break;
                        }
                    case -3:
                        {
                            _thirdTrack = track;
                            tempList.Remove(track);
                            break;
                        }
                }
            }

            if (_firstTrack == null)
            {
                foreach (var track in tempList)
                {
                    if (track.IsNull || !track.Enable)
                        continue;
                    if (_firstTrack == null)
                        _firstTrack = track;
                    else if (_firstTrack.Score < track.Score)
                        _firstTrack = track;
                }

                tempList.Remove(_firstTrack);
            }

            if (_secondTrack == null)
            {
                foreach (var track in tempList)
                {
                    if (track.IsNull || !track.Enable)
                        continue;
                    if (_secondTrack == null)
                        _secondTrack = track;
                    else if (_secondTrack.Score < track.Score)
                        _secondTrack = track;
                }

                tempList.Remove(_secondTrack);
            }

            if (_thirdTrack == null)
            {
                foreach (var track in tempList)
                {
                    if (track.IsNull || !track.Enable)
                        continue;
                    if (_thirdTrack == null)
                        _thirdTrack = track;
                    else if (_thirdTrack.Score < track.Score)
                        _thirdTrack = track;
                }

                tempList.Remove(_thirdTrack);
            }
        }

        private void InitReward()
        {
            _firstTrack = null;
            _secondTrack = null;
            _thirdTrack = null;
            var tempList = new List<RaceProTrack>();
            tempList.AddRange(_allTracks);
            foreach (var track in _allTracks)
            {
                if (track.IsNull || !track.Enable)
                    continue;
                switch (track.LastScore)
                {
                    case -1:
                        {
                            _firstTrack = track;
                            tempList.Remove(track);
                            break;
                        }
                    case -2:
                        {
                            _secondTrack = track;
                            tempList.Remove(track);
                            break;
                        }
                    case -3:
                        {
                            _thirdTrack = track;
                            tempList.Remove(track);
                            break;
                        }
                }
            }

            if (_firstTrack == null)
            {
                foreach (var track in tempList)
                {
                    if (track.IsNull || !track.Enable)
                        continue;
                    if (_firstTrack == null)
                        _firstTrack = track;
                    else if (_firstTrack.LastScore < track.LastScore)
                        _firstTrack = track;
                }

                tempList.Remove(_firstTrack);
            }

            if (_secondTrack == null)
            {
                foreach (var track in tempList)
                {
                    if (track.IsNull || !track.Enable)
                        continue;
                    if (_secondTrack == null)
                        _secondTrack = track;
                    else if (_secondTrack.LastScore < track.LastScore)
                        _secondTrack = track;
                }

                tempList.Remove(_secondTrack);
            }

            if (_thirdTrack == null)
            {
                foreach (var track in tempList)
                {
                    if (track.IsNull || !track.Enable)
                        continue;
                    if (_thirdTrack == null)
                        _thirdTrack = track;
                    else if (_thirdTrack.LastScore < track.LastScore)
                        _thirdTrack = track;
                }

                tempList.Remove(_thirdTrack);
            }
        }

        private void SetReward()
        {
            if (_activityRace.raceExtendRoundConfig.RaceGetNum.Count == 1)
            {
                if (!firstReward.hasInit)
                    firstReward.Init(_firstTrack, true);
                if (!secondReward.hasInit)
                    secondReward.Init(_secondTrack, false);
                if (!thirdReward.hasInit)
                    thirdReward.Init(_thirdTrack, false);
            }
            else
            {
                if (!firstReward.hasInit)
                    firstReward.Init(_firstTrack, true);
                if (!secondReward.hasInit)
                    secondReward.Init(_secondTrack, true);
                if (!thirdReward.hasInit)
                    thirdReward.Init(_thirdTrack, true);
            }
        }

        private IEnumerator PlayChangeAnim(bool allPlay = true)
        {
            UIManager.Instance.Block(true);
            foreach (var track in _needAnim)
            {
                track.PlayAnim(_time, allPlay);
            }

            Game.Manager.audioMan.TriggerSound("HotAirRankUp");
            yield return new WaitForSeconds(_time - 0.4f);
            if (thirdReward.HasChange(_thirdTrack))
            {
                Game.Instance.StartCoroutineGlobal(thirdReward.PlayAnim());
                yield return new WaitForSeconds(0.3f);
            }

            if (secondReward.HasChange(_secondTrack))
            {
                Game.Instance.StartCoroutineGlobal(secondReward.PlayAnim());
                yield return new WaitForSeconds(0.3f);
            }

            if (firstReward.HasChange(_firstTrack))
            {
                Game.Instance.StartCoroutineGlobal(firstReward.PlayAnim());
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(_time2);
            foreach (var track in _allTracks)
            {
                track.HideEffect();
            }

            UIManager.Instance.Block(false);
            if (!_activityRace.hasStart)
            {
                if (_activityRace.CheckHasRoundReward())
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIRaceReward);
                    Close();
                }
                else
                {
                    CheckEnd();
                }
            }
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
        }

        private void RefreshCd()
        {
            if (_activityRace == null)
            {
                Close();
                return;
            }

            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activityRace.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
            if (diff <= 0)
            {
                if (!_isPlayingAnim)
                    Close();
            }
        }

        private void OpenHelp()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIRaceHelp, transform.Find("Content/InfoBtn").position, 0f);
        }

        private void Update()
        {
            if (IsOpening())
                return;
            foreach (var track in _allTracks)
            {
                track.TryShow();
            }
        }
    }
}