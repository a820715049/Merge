using DG.Tweening;
using EL;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace FAT
{
    public class RaceProTrack : MonoBehaviour
    {
        [SerializeField] private RectTransform player;
        [SerializeField] private RectTransform effectContent;
        [SerializeField] private GameObject avatar;
        [SerializeField] private Transform reward;
        [SerializeField] private TextMeshProUGUI nameTxt;
        
        [Header("参数")]
        public float startY;
        public float endY;
        public int id;
        
        private ActivityRaceExtend _activityRace;
        private RaceExtendPlayerBase _info;
        public bool IsPlayer { get; private set; }
        public int LastScore { get; private set; }
        public bool LastEnable { get; private set; }
        public int Score => _info.curScore;
        public Transform RewardNode => reward;
        public Transform EffectContent => effectContent;

        public bool IsNull { get; private set; }
        public bool Enable => _info is RaceExtendRobot { playerEnable: true };
        public bool Finish => LastScore < 0 || LastScore >= _activityRace.raceExtendRoundConfig.Score;
        public bool PlayerEnable => (_info is not RaceExtendRobot raceExtendPlayer) || raceExtendPlayer.playerEnable;

        public Transform GetRewardTrans()
        {
            return reward;
        }

        public void InitBot(ActivityRaceExtend activityRace, RaceExtendPlayerBase info)
        {
            _activityRace = activityRace;
            _info = info;
            IsPlayer = false;
            Init();
        }

        public void InitPlayer(ActivityRaceExtend activityRace)
        {
            _activityRace = activityRace;
            _info = activityRace.raceExtendManager.myself;
            IsPlayer = true;
            Init();
        }

        private void Init()
        {
            // player.transform.GetChild(1).gameObject.SetActive(false);
            if (IsPlayer)
            {
                LastScore = _info.showScore;
                gameObject.transform.GetChild(0).gameObject.SetActive(true);
                SetPlayerNode();
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
                    var raceExtendPlayer = _info as RaceExtendRobot;
                    id = raceExtendPlayer?.RobotID ?? -1;
                    IsNull = false;
                    LastEnable = PlayerEnable;
                    LastScore = _info.showScore;
                    gameObject.transform.GetChild(0).gameObject.SetActive(LastEnable);
                    SetPlayerNode();
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
                gameObject.transform.GetChild(0).gameObject.SetActive(PlayerEnable);
                SetPlayerNode();
                SetPlayerName();
            }
        }

        private void SetPlayerNode()
        {
            if (LastScore >= 0)
                player.sizeDelta = new Vector2(player.sizeDelta.x, Mathf.Lerp(startY, endY,
                    LastScore / (float)_activityRace.raceExtendRoundConfig.Score));
            else
                player.sizeDelta = new Vector2(player.sizeDelta.x, endY);
        }

        private void SetPlayerName()
        {
            if (!IsPlayer)
                avatar.GetComponent<UIImageRes>().SetImage(_info.avatar);
            nameTxt.text = IsPlayer ? I18N.Text("#SysComDesc459") : I18N.FormatText("#SysComDesc431", (_info as RaceExtendRobot).RobotID);
        }

        public bool HasChange()
        {
            if (_info == null)
                return false;
            var playerEnable = PlayerEnable;
            if (LastEnable != playerEnable && !IsPlayer)
            {
                LastEnable = playerEnable;
                return true;
            }

            return LastScore != _info.showScore;
        }

        public void TryShow()
        {
            if(_info == null)
                return;
            if(IsPlayer)
                return;
            var playerEnable = PlayerEnable;
            if (playerEnable && gameObject.transform.GetChild(0).gameObject.activeSelf != true)
            {
                LastEnable = true;
                gameObject.transform.GetChild(0).gameObject.SetActive(true);
            }
        }

        public bool HasFinishAnim()
        {
            return LastScore != _info.curScore &&
                   (_info.curScore < 0 || _info.curScore >= _activityRace.raceExtendRoundConfig.Score);
        }

        public void HideEffect()
        {
            // player.transform.GetChild(1).gameObject.SetActive(false);
        }

        public void RefreshInfo()
        {
            // player.transform.GetChild(1).gameObject.SetActive(false);
            if (_info == null)
                return;
            LastScore = _info.showScore;
        }

        public void PlayAnim(float time, bool play)
        {
            var playerEnable = PlayerEnable;
            if (!playerEnable)
                return;
            Vector2 target;
            bool end = false;
            if (LastScore >= 0)
                if (LastScore >= _activityRace.raceExtendRoundConfig.Score)
                {
                    target = new Vector2(player.sizeDelta.x, endY);
                    end = true;
                }
                else
                {
                    target = new Vector2(player.sizeDelta.x, Mathf.Lerp(startY, endY,
                        LastScore / (float)_activityRace.raceExtendRoundConfig.Score));
                }
            else
            {
                target = new Vector2(player.sizeDelta.x, endY);
                end = true;
            }
            
            if (play)
            {
                player.DOSizeDelta(target, time).SetEase(Ease.InOutSine);
            }
            else
            {
                if (!end)
                    player.sizeDelta = target;
                else
                {
                    player.DOSizeDelta(target, time).SetEase(Ease.InOutSine).onComplete += () =>
                    {
                        // 特效
                        // if (Finish)
                        //     player.transform.GetChild(1).gameObject.SetActive(true);
                    };
                }
            }
        }
    }
}