/*
 *@Author:chaoran.zhang
 *@Desc:热气球活动领奖UI
 *@Created Time:2024.07.09 星期二 17:41:42
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIExtensions;
using Config;
using EL;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIRaceReward : UIBase
    {
        [Serializable]
        private class UIRaceRewardItem
        {
            [SerializeField] public UICommonItem reward;
            [SerializeField] public UIParticle particle;
        }

        [SerializeField]
        [Tooltip("宝箱开启特效延迟出现时间")]
        private float effectShowDelayTime;

        [SerializeField] private GameObject boxGo;
        [SerializeField] private GameObject rewardGo;
        [SerializeField] private List<UIRaceRewardItem> rewardGroup;
        [SerializeField] private TMP_Text tipsText;
        [SerializeField] private TextMeshProUGUI desc;

        private RaceRewardData _curRaceRewardData;
        private int _curShowStage = 0;
        private Coroutine _coroutine = null;
        private GameObject _spinePrefab = null;
        private SkeletonGraphic _skeleton = null;
        private bool _isCurShowSpine = false;
        private bool _isPlaySpineAnim = false;
        private Coroutine _coShowEffect;

        protected override void OnCreate()
        {
            foreach (var r in rewardGroup)
            {
                r.reward.Setup();
            }

            transform.AddButton("Content/ClaimBtn", _OnBtnClaim);
        }

        protected override void OnPreOpen()
        {
            _curRaceRewardData = RaceManager.GetInstance().RewardData;
            _curShowStage = 0;
            _ChooseRewardSpine();
            _RefreshReward();
            _RefreshTips();

            IEnumerator show()
            {
                yield return new WaitForSeconds(0.3f);
                _TryShowRewardSpine();
            }

            StartCoroutine(show());
        }

        protected override void OnPostOpen()
        {

        }

        protected override void OnPreClose()
        {
            tipsText.gameObject.SetActive(false);
        }

        protected override void OnPostClose()
        {
            _ClearBoxSpine();
            MessageCenter.Get<MSG.RACE_REWARD_END>().Dispatch();
        }

        private void _ChooseRewardSpine()
        {
            boxGo.SetActive(true);
            if (_curRaceRewardData == null)
                return;
            if (RaceManager.GetInstance().Race.ConfD.SubType == 0)
            {
                _spinePrefab = boxGo.transform.GetChild(_curRaceRewardData.RewardID).gameObject;
            }
            else if (RaceManager.GetInstance().Race.ConfD.SubType == 1)
            {
                _spinePrefab = boxGo.transform.GetChild(RaceManager.GetInstance().Race.Round == 0 ? 4 : 3).gameObject;
            }
            _skeleton = _spinePrefab.transform.GetChild(0).GetChild(1).GetComponent<SkeletonGraphic>();
            desc.text = _curRaceRewardData.RewardID switch
            {
                0 => I18N.Text("#SysComDesc433"),
                1 => I18N.Text("#SysComDesc434"),
                2 => I18N.Text("#SysComDesc435"),
                _ => ""
            };
            _TryShowRewardSpine();
        }

        private void _TryShowRewardSpine()
        {
            if (_spinePrefab == null || _isCurShowSpine)
                return;
            _isCurShowSpine = true;
            _spinePrefab.SetActive(true);
            if (_skeleton != null)
            {
                Game.Manager.audioMan.TriggerSound("HotAirRewardOpen");
                _isPlaySpineAnim = true;
                _skeleton.AnimationState.SetAnimation(0, "box_show", false)
                    .Complete += delegate (TrackEntry entry)
                {
                    _isPlaySpineAnim = false;
                    _skeleton.AnimationState.SetAnimation(0, "box_idle", true);
                };
            }
        }

        private void _ClearBoxSpine()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }

            if (_coShowEffect != null)
            {
                StopCoroutine(_coShowEffect);
                _coShowEffect = null;
            }

            if (_spinePrefab != null)
            {
                _spinePrefab.SetActive(false);
                _spinePrefab = null;
            }

            if (_skeleton != null)
            {
                _skeleton.AnimationState.ClearTracks();
                _skeleton = null;
            }

            _isCurShowSpine = false;
            _isPlaySpineAnim = false;
        }

        private void _RefreshReward()
        {
            rewardGo.SetActive(false);
            if (_curRaceRewardData == null)
                return;
            var rewardList = _curRaceRewardData.Reward;
            //判断第一个奖励是否是万能卡，是的话则不显示
            var firstRewardId = rewardList.Count > 0 ? rewardList[0].rewardId : 0;
            int index = Game.Manager.objectMan.IsType(firstRewardId, ObjConfigType.CardJoker) ? 1 : 0;
            foreach (var uiReward in rewardGroup)
            {
                uiReward.particle.gameObject.SetActive(false);
                if (index < rewardList.Count)
                {
                    uiReward.reward.gameObject.SetActive(true);
                    uiReward.reward.Refresh(rewardList[index].rewardId, rewardList[index].rewardCount);
                }
                else
                {
                    uiReward.reward.gameObject.SetActive(false);
                }

                index++;
            }
        }

        private void _RefreshTips()
        {
            tipsText.gameObject.SetActive(true);
            string tips = "";
            if (_curShowStage == 0)
                tips = I18N.Text("#SysComDesc110");
            else if (_curShowStage == 1)
                tips = I18N.Text("#SysComDesc111");
            tipsText.text = tips;
        }

        private void _OnBtnClaim()
        {
            if (_curRaceRewardData == null)
            {
                Close();
                return;
            }

            if (_isPlaySpineAnim)
                return;
            if (_curShowStage == 0)
            {
                _isPlaySpineAnim = true;
                _coShowEffect = StartCoroutine(_CoShowOpenEffect());
                _skeleton.AnimationState.ClearTracks();
                _skeleton.AnimationState.SetAnimation(0, "box_open", false)
                    .Complete += delegate (TrackEntry entry)
                {
                    StartCoroutine(_coHideBoxGo());
                    _curShowStage = 1;
                    _RefreshTips();
                };
            }
            else if (_curShowStage == 1)
            {
                //先领取宝箱中的奖励
                int index = 0;
                foreach (var reward in _curRaceRewardData.Reward)
                {
                    if (index < rewardGroup.Count)
                    {
                        UIFlyUtility.FlyReward(reward, rewardGroup[index].reward.transform.position);
                    }
                    else
                    {
                        UIFlyUtility.FlyReward(reward, rewardGo.transform.position);
                    }

                    index++;
                }

                RaceManager.GetInstance().RewardData = null;
                //再关界面
                Close();
            }
        }

        private IEnumerator _coHideBoxGo()
        {
            yield return new WaitForSeconds(0.3f);
            boxGo.SetActive(false);
        }

        private IEnumerator _CoShowOpenEffect()
        {
            _spinePrefab.transform.GetChild(0).GetComponent<Animator>().SetTrigger("Hide");
            yield return new WaitForSeconds(effectShowDelayTime);
            rewardGo.SetActive(true);
            //等一帧再显示特效
            Game.Manager.audioMan.TriggerSound("HotAirRewardShow");
            yield return new WaitForSeconds(0.3f);
            foreach (var uiReward in rewardGroup)
            {
                uiReward.particle.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }

            //等宝箱打开特效播完才允许进行下一步
            yield return new WaitForSeconds(0.5f);
            _isPlaySpineAnim = false;
        }
    }
}
