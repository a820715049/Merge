/*
 *@Author:chaoran.zhang
 *@Desc:热气球活动排行榜界面UI，名次奖励使用的脚本文件
 *@Created Time:2024.07.15 星期一 03:31:10
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class RaceReward : MonoBehaviour
    {
        [SerializeField] private GameObject Reward;
        [SerializeField] private GameObject Bg;
        [SerializeField] private int Index;
        private Animator _rewardAnim;
        private Animator _bgAnim;
        private RaceTrack _follow;
        private bool _show;
        public bool HasInit = false;
        public bool Finish;
        public bool isFirst;
        public UIImageState _imageState;

        private void Awake()
        {
            if (!isFirst) { _rewardAnim = Reward.transform.GetChild(0).GetComponent<Animator>(); }
            _bgAnim = Bg.GetComponent<Animator>();
            transform.GetChild(1).Find("Btn").transform.GetComponent<Button>().onClick.AddListener(OnClickReward);
        }

        private void OnClickReward()
        {
            var rewardID = RaceManager.GetInstance().Race.CurRaceRound.RaceGetGift[Index];
            var config = fat.conf.Data.GetEventRaceReward(rewardID);
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, transform.GetChild(1).position, 50f, config.Reward);
        }

        public void ResetState()
        {
            _follow = null;
            Reward.SetActive(false);
            Bg.gameObject.SetActive(false);
            HasInit = false;
            Finish = false;
        }

        public void Init(RaceTrack track, bool show)
        {
            Reward.SetActive(false);
            Bg.gameObject.SetActive(false);
            if (track == null)
            {
                HasInit = false;
                return;
            }

            _follow = track;
            _show = show;
            Finish = _follow.Score < 0;
            gameObject.transform.SetParent(track.RewardNode, false);
            if (!show)
            {
                Reward.SetActive(false);
                Bg.SetActive(false);
                HasInit = true;
                return;
            }

            Reward.SetActive(!Finish);
            Bg.SetActive(Finish);
            if (isFirst)
            {
                Reward.transform.GetChild(0).gameObject.SetActive(RaceManager.GetInstance().Race.ConfD.SubType == 0);
                Reward.transform.GetChild(1).gameObject.SetActive(RaceManager.GetInstance().Race.ConfD.SubType == 1 &&
                    RaceManager.GetInstance().Race.Round == 5);
                Reward.transform.GetChild(2).gameObject.SetActive(RaceManager.GetInstance().Race.ConfD.SubType == 1 &&
                    RaceManager.GetInstance().Race.Round != 5);
                var select = RaceManager.GetInstance().Race.ConfD.SubType;
                if (select == 1)
                {
                    select = RaceManager.GetInstance().Race.Round != 5 ? 2 : 1;
                }
                _imageState.Select(select);
                _rewardAnim = Reward.transform.GetChild(select).GetComponent<Animator>();
            }
            HasInit = true;
        }

        public void CheckState()
        {
            if (Finish)
                return;
            if (!_show)
                return;
            if (_follow == null)
            {
                Reward.SetActive(false);
                Bg.gameObject.SetActive(false);
            }
            else
            {
                if (_follow.Enable && !_follow.Finish)
                {
                    Reward.SetActive(true);
                    Bg.gameObject.SetActive(false);
                }
            }
        }

        public bool HasChange(RaceTrack track)
        {
            if (!_show)
                return false;
            if (_follow == null)
                return false;
            if (Finish)
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

                if (!Finish && _follow.Finish)
                {
                    result = true;
                }
            }

            return result;
        }

        public IEnumerator PlayAnim()
        {
            if (Reward.activeInHierarchy)
            {
                _rewardAnim.SetTrigger("disappear");
                yield return new WaitForSeconds(0.3f);
                gameObject.transform.SetParent(_follow.RewardNode, false);
                if (!_follow.Finish)
                {
                    Game.Manager.audioMan.TriggerSound("HotAirRankChange");
                    _rewardAnim.SetTrigger("appear");
                }
                else
                {
                    Reward.gameObject.SetActive(false);
                    Bg.gameObject.SetActive(true);
                    switch (Index)
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
                    Finish = true;
                }
            }
            else
            {
                gameObject.transform.SetParent(_follow.RewardNode, false);
                yield return new WaitForSeconds(0.3f);
                Reward.SetActive(true);
            }
        }
    }
}
