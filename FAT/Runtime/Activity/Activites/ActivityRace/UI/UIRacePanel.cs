/*
 *@Author:chaoran.zhang
 *@Desc:热气球活动排行版界面
 *@Created Time:2024.07.10 星期三 14:04:21
 */

using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FAT
{
    public class UIRacePanel : UIBase
    {
        [SerializeField] private float _time; //上升动画时间
        [SerializeField] private float _time2; //奖励展示时间
        private TextMeshProUGUI _score;
        private TextMeshProUGUI _cd;
        private UIImageState _titleBg;
        private TextProOnACircle _title;
        private ActivityRace _race;
        private GameObject _finishNode;
        [SerializeField] private List<RaceTrack> tracks = new();
        private RaceTrack _playerTrack;
        private List<RaceTrack> _allTracks = new();
        private bool _init = false;
        private bool _isPlayingAnim = false;
        private RaceTrack _firstTrack;
        private RaceTrack _secondTrack;
        private RaceTrack _thirdTrack;
        private TextMeshProUGUI _round;
        private List<RaceTrack> _needAnim = new();
        [SerializeField] private RaceReward firstReward;
        [SerializeField] private RaceReward secondReward;
        [SerializeField] private RaceReward thirdReward;

        protected override void OnCreate()
        {
            _finishNode = transform.Find("Content/FinishNode").gameObject;
            _score = transform.Find("Content/TotalScore/ScoreText").GetComponent<TextMeshProUGUI>();
            _cd = transform.Find("Content/_cd/text").GetComponent<TextMeshProUGUI>();
            _titleBg = transform.Find("Content/TitleBg").GetComponent<UIImageState>();
            _title = transform.Find("Content/TitleBg/Title").GetComponent<TextProOnACircle>();
            _playerTrack = transform.Find("Content/TrackNode/TrackPlayer").GetComponent<RaceTrack>();
            transform.AddButton("Content/CloseBtn", Close);
            transform.AddButton("Content/FinishNode/ConfirmBtn", Close);
            _round = transform.Find("Content/Line2/Bg/RoundText").GetComponent<TextMeshProUGUI>();
            transform.AddButton("Content/InfoBtn", OpenHelp);
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
            _race.Block = false;
        }

        //初始化面板状态
        private void InitPanel()
        {
            _race = RaceManager.GetInstance().Race;
            _score.text = _race.CurRaceRound.Score.ToString();
            if (_race.Round >= 0)
                _round.text = I18N.FormatText("#SysComDesc402", _race.Round + 1);
            else
                _round.text = I18N.Text("#SysComDesc432");
            _titleBg.Select(_race.Round >= 0 ? 0 : 1);
            _race.RacePanelVisual.Refresh(_title, "mainTitle");
            if (!_init || _race.RefreshPanel)
            {
                Game.Manager.audioMan.TriggerSound("HotAirGameStart");
                _finishNode.SetActive(false);
                firstReward.ResetState();
                secondReward.ResetState();
                thirdReward.ResetState();
                InitTrack();
                InitReward();
                SetReward();
                _init = true;
                _race.RefreshPanel = false;
            }
        }

        //检测是否已经结束
        private bool CheckEnd()
        {
            if (_race.HasFinish && !_race.HasReward)
            {
                _finishNode.gameObject.SetActive(true);
                return true;
            }

            _finishNode.gameObject.SetActive(false);
            return false;
        }

        private void RefreshInfo()
        {
            RaceManager.GetInstance().RefreshScore();
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
            _playerTrack.InitPlayer();
            //机器人信息初始化
            for (int i = 0; i < tracks.Count; i++)
            {
                if (i + 1 <= RaceManager.GetInstance().BotInfos.Count)
                    tracks[i].InitBot(RaceManager.GetInstance().BotInfos[i]);
                else
                    tracks[i].InitBot(null);
            }

            _allTracks.Clear();
            _allTracks.AddRange(tracks);
            _allTracks.Add(_playerTrack);
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
            var tempList = new List<RaceTrack>();
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
            var tempList = new List<RaceTrack>();
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
            if (_race.CurRaceRound.RaceGetNum.Count == 1)
            {
                if (!firstReward.HasInit)
                    firstReward.Init(_firstTrack, true);
                if (!secondReward.HasInit)
                    secondReward.Init(_secondTrack, false);
                if (!thirdReward.HasInit)
                    thirdReward.Init(_thirdTrack, false);
            }
            else
            {
                if (!firstReward.HasInit)
                    firstReward.Init(_firstTrack, true);
                if (!secondReward.HasInit)
                    secondReward.Init(_secondTrack, true);
                if (!thirdReward.HasInit)
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
            if (RaceManager.GetInstance().Race.HasFinish)
            {
                if (RaceManager.GetInstance().Race.HasReward)
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
            if (RaceManager.GetInstance().Race == null)
            {
                Close();
                return;
            }

            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, RaceManager.GetInstance().Race.endTS - t);
            UIUtility.CountDownFormat(_cd, diff);
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
