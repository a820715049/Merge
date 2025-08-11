/*
 *@Author:chaoran.zhang
 *@Desc:热气球排行榜界面玩家信息
 *@Created Time:2024.07.10 星期三 15:42:42
 */

using DG.Tweening;
using EL;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using static fat.conf.Data;

namespace FAT
{
    public class RaceTrack : MonoBehaviour
    {
        [SerializeField] private GameObject _player;
        [SerializeField] private GameObject _avatar;
        [SerializeField] private Transform _start;
        [SerializeField] private Transform _end;
        [SerializeField] private Transform _final;
        [SerializeField] private Transform _reward;
        [SerializeField] private RacePlayerInfo _info;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private SkeletonGraphic _spine;
        public bool IsPlayer { get; private set; }
        public int LastScore { get; private set; }
        public bool LastEnable { get; private set; }
        public int Score => _info.Score;
        public Transform RewardNode => _reward;
        public int ID;

        public bool IsNull { get; private set; }
        public bool Enable => _info == null ? false : _info.Enable;
        public bool Finish => LastScore < 0 || LastScore >= RaceManager.GetInstance().Race.CurRaceRound.Score;

        public Transform GetRewardTrans()
        {
            return _reward;
        }

        public void InitBot(RacePlayerInfo info)
        {
            _info = info;
            IsPlayer = false;
            Init();
        }

        public void InitPlayer()
        {
            IsPlayer = true;
            _info = RaceManager.GetInstance().PlayerInfo;
            Init();
        }

        private void Init()
        {
            _player.transform.GetChild(0).gameObject.SetActive(false);
            if (IsPlayer)
            {
                LastScore = RaceManager.GetInstance().PlayerInfo.Score;
                gameObject.transform.GetChild(0).gameObject.SetActive(true);
                SetPlayerNode();
                SetPlayerScore();
                SetPlayerName();
            }
            else
            {
                if (_info == null)
                {
                    gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    IsNull = true;
                }

                else
                {
                    ID = _info.Id;
                    IsNull = false;
                    LastEnable = _info.Enable;
                    LastScore = _info.Score;
                    gameObject.transform.GetChild(0).gameObject.SetActive(_info.Enable);
                    SetPlayerNode();
                    SetPlayerScore();
                    SetPlayerName();
                }
            }
        }

        public void CheckShow()
        {
            if (_info == null)
            {
                gameObject.transform.GetChild(0).gameObject.SetActive(false);
                IsNull = true;
            }

            else
            {
                IsNull = false;
                gameObject.transform.GetChild(0).gameObject.SetActive(_info.Enable);
                SetPlayerNode();
                SetPlayerScore();
                SetPlayerName();
            }
        }

        private void SetPlayerNode()
        {
            if (LastScore >= 0)
                _player.transform.position = Vector3.Lerp(_start.position, _end.position,
                    (float)LastScore / (float)RaceManager.GetInstance().Race.CurRaceRound.Score);
            else
                _player.transform.position = _final.position;
        }

        private void SetPlayerName()
        {
            if (!IsPlayer)
                _avatar.GetComponent<UIImageRes>().SetImage(GetEventRaceRobotIcon(_info.Avatar).Icon);
            _name.text = IsPlayer ? I18N.Text("#SysComDesc459") : I18N.FormatText("#SysComDesc431", _info.Name);
        }

        private void SetPlayerScore()
        {
            _scoreText.text = LastScore.ToString();
        }

        public bool HasChange()
        {
            if (_info == null)
                return false;
            if (LastEnable != _info.Enable && !IsPlayer)
            {
                LastEnable = _info.Enable;
                return true;
            }

            return LastScore != _info.Score;
        }

        public void TryShow()
        {
            if(_info == null)
                return;
            if(IsPlayer)
                return;
            if (_info.Enable && gameObject.transform.GetChild(0).gameObject.activeInHierarchy != _info.Enable)
            {
                LastEnable = _info.Enable;
                gameObject.transform.GetChild(0).gameObject.SetActive(_info.Enable);
            }
        }

        public bool HasFinishAnim()
        {
            return LastScore != _info.Score &&
                   (_info.Score < 0 || _info.Score >= RaceManager.GetInstance().Race.CurRaceRound.Score);
        }

        public void HideEffect()
        {
            _player.transform.GetChild(0).gameObject.SetActive(false);
        }

        public void RefreshInfo()
        {
            _player.transform.GetChild(0).gameObject.SetActive(false);
            if (_info == null)
                return;
            LastScore = _info.Score;
            if (LastScore < 0 || LastScore >= RaceManager.GetInstance().Race.CurRaceRound.Score)
                _scoreText.gameObject.SetActive(false);
            else
            {
                _scoreText.gameObject.SetActive(true);
            }
        }

        public void PlayAnim(float time, bool play)
        {
            if (!_info.Enable)
                return;
            _scoreText.text = LastScore.ToString();
            Vector3 target;
            if (LastScore >= 0)
                if (LastScore >= RaceManager.GetInstance().Race.CurRaceRound.Score)
                    target = _final.position;
                else
                    target = Vector3.Lerp(_start.position, _end.position,
                        (float)LastScore / (float)RaceManager.GetInstance().Race.CurRaceRound.Score);
            else
                target = _final.position;
            if (play)
            {
                _spine.AnimationState.SetAnimation(0, "moveup", false).Complete += delegate(TrackEntry entry)
                {
                    _spine.AnimationState.SetAnimation(0, "idle", true);
                };
                _player.transform.DOMove(target, time).SetEase(Ease.InOutSine);
            }
            else
            {
                if (target != _final.position)
                    _player.transform.position = target;
                else
                {
                    _spine.AnimationState.SetAnimation(0, "moveup", false).Complete += delegate(TrackEntry entry)
                    {
                        _spine.AnimationState.SetAnimation(0, "idle", true);
                    };
                    _player.transform.DOMove(target, time).SetEase(Ease.InOutSine).onComplete += () =>
                    {
                        if (Finish)
                            _player.transform.GetChild(0).gameObject.SetActive(true);
                    };
                }
            }
        }
    }
}