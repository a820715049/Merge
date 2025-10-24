using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FAT
{
    public class RaceProReward : MonoBehaviour
    {
        [Header("组件")]
        [SerializeField] private GameObject reward;
        [SerializeField] private GameObject bg;
        [SerializeField] private Button clickBtn;
        [SerializeField] private int index;
        
        [Header("参数")]
        public bool hasInit;
        public bool finish;
        
        private ActivityRaceExtend _activityRace;
        private RaceProTrack _follow;
        private Animator _bgAnim;
        private bool _show;

        private void Awake()
        {
            _bgAnim = bg.GetComponent<Animator>();
            clickBtn.onClick.AddListener(OnClickReward);
        }

        private void OnClickReward()
        {
            var rewardID = _activityRace.raceExtendRoundConfig.RaceGetGift[index];
            var config = fat.conf.Data.GetRaceExtendReward(rewardID);
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, transform.GetChild(1).position, 50f, config.Reward);
        }

        public void ResetState()
        {
            _follow = null;
            reward.SetActive(false);
            bg.gameObject.SetActive(false);
            hasInit = false;
            finish = false;
        }

        public void Init(ActivityRaceExtend activityRace, RaceProTrack track, bool show)
        {
            _activityRace = activityRace;
            reward.SetActive(false);
            bg.gameObject.SetActive(false);
            if (track == null)
            {
                hasInit = false;
                return;
            }

            _follow = track;
            _show = show;
            finish = _follow.Score < 0;
            gameObject.transform.SetParent(track.RewardNode, false);
            if (!show)
            {
                reward.SetActive(false);
                bg.SetActive(false);
                hasInit = true;
                return;
            }

            reward.SetActive(!finish);
            bg.SetActive(finish);
            
            hasInit = true;
        }

        public void CheckState()
        {
            if (finish)
                return;
            if (!_show)
                return;
            if (_follow == null)
            {
                reward.SetActive(false);
                bg.gameObject.SetActive(false);
            }
            else
            {
                if (_follow.Enable && !_follow.Finish)
                {
                    reward.SetActive(true);
                    bg.gameObject.SetActive(false);
                }
            }
        }

        public bool HasChange(RaceProTrack track)
        {
            if (!_show)
                return false;
            if (_follow == null)
                return false;
            if (finish)
                return false;
            var result = false;
            if (_show)
            {
                if (_follow == null)
                {
                    _follow = track;
                    result = true;
                }

                if (_follow.GetHashCode() != track.GetHashCode())
                {
                    _follow = track;
                    result = true;
                }

                if (!finish && _follow.Finish)
                {
                    result = true;
                }
            }

            return result;
        }

        public IEnumerator PlayAnim()
        {
            if (reward.activeInHierarchy)
            {
                yield return new WaitForSeconds(0.3f);
                gameObject.transform.SetParent(_follow.RewardNode, false);
                if (!_follow.Finish)
                {
                    Game.Manager.audioMan.TriggerSound("HotAirRankChange");
                }
                else
                {
                    reward.gameObject.SetActive(false);
                    bg.gameObject.SetActive(true);
                    switch (index)
                    {
                        case 0:
                            {
                                Game.Manager.audioMan.TriggerSound("HotAirRank1");
                                break;
                            }
                        case 1:
                            {
                                Game.Manager.audioMan.TriggerSound("HotAirRank2");
                                break;
                            }
                        case 2:
                            {
                                Game.Manager.audioMan.TriggerSound("HotAirRank3");
                                break;
                            }
                    }
                    _bgAnim.SetTrigger("finish");
                    finish = true;
                }
            }
            else
            {
                gameObject.transform.SetParent(_follow.RewardNode, false);
                yield return new WaitForSeconds(0.3f);
                reward.SetActive(true);
            }
        }
    }
}
