/**
 * @Author: zhangpengjian
 * @Date: 2025/7/10 16:17:05
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/10 16:17:05
 * Description: 沙堡里程碑活动入口
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections.Generic;
using fat.rawdata;


namespace FAT
{
    public class MBActivityCastleEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject group;
        [SerializeField] private TMP_Text cd;
        [SerializeField] private MBRewardProgress progress;
        [SerializeField] private UIImageRes rewardIcon;
        [SerializeField] private UIImageRes tokenIcon;
        [SerializeField] private Button rewardBtn;
        [SerializeField] private GameObject efx;
        [SerializeField] private float duration = 0.75f;

        private Action WhenCD;
        private ActivityCastle _activity;
        private bool hasChange = false;
        private CastleMilestoneGroup lastConfG;
        private List<RewardCommitData> reward = new();

        public void Start()
        {
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
            rewardBtn.onClick.AddListener(RewardClick);
        }

        private void RewardClick()
        {
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips,
                    rewardIcon.transform.position,
                    rewardIcon.transform.GetComponent<RectTransform>().rect.size.y * 0.5f,
                    _activity.confG.MilestoneReward);
        }

        public void OnEnable()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.CASTLE_MILESTONE_CHANGE>().AddListener(OnMilestoneChange);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
            MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLOSE>().AddListener(OnRewardClose);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(OnRewardClose);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(OnFlyFeedBack);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.CASTLE_MILESTONE_CHANGE>().RemoveListener(OnMilestoneChange);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
            MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLOSE>().RemoveListener(OnRewardClose);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnRewardClose);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(OnFlyFeedBack);
            if (UIManager.Instance.IsBlocking())
            {
                UIManager.Instance.Block(false);
            }
        }

        private void OnRewardClose()
        {
            if (lastConfG != null)
            {
                _activity.VisualMain.Popup(custom_: lastConfG);
                lastConfG = null;
                if (_activity.IsComplete())
                {
                    _activity.TryConvert();
                }
            }
        }

        private void OnFlyFeedBack(FlyableItemSlice slice)
        {
            if (slice.FlyType != FlyType.CastleToken) return;
            if (slice.CurIdx != 1) return;
            TryCommit();
        }

        private void TryCommit()
        {
            if (hasChange)
            {
                progress.Refresh(lastConfG.MilestoneScore, lastConfG.MilestoneScore, 0.5f, () => 
                {
                    efx.SetActive(true);
                    rewardIcon.SetImage(_activity.confG.MilestoneRewardIcon2);
                    UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, rewardIcon.transform.position, reward, lastConfG.MilestoneRewardIcon1, I18N.Text("#SysComDesc726"));
                    if (UIManager.Instance.IsBlocking())
                    {
                        UIManager.Instance.Block(false);
                    }
                    if (_activity.IsComplete())
                    {
                        Visible(false);
                    }
                    else
                    {
                        progress.Refresh(0, _activity.confG.MilestoneScore);
                        progress.Refresh(_activity.Score, _activity.confG.MilestoneScore, 0.5f, () =>
                        {
                            hasChange = false;
                            efx.SetActive(false);
                        });
                    }
                });
            }
            else
            {
                progress.Refresh(_activity.Score, _activity.confG.MilestoneScore, 0.5f);
            }
        }

        /// <summary>
        /// 寻宝活动入口
        /// </summary>
        public void RefreshEntry(ActivityLike activity)
        {
            hasChange = false;
            efx.SetActive(false);
            if (activity == null)
            {
                Visible(false);
                return;
            }
            if (activity is not ActivityCastle)
            {
                Visible(false);
                return;
            }
            _activity = (ActivityCastle)activity;
            var valid = _activity is { Valid: true } && !_activity.IsComplete();
            Visible(valid);
            if (!valid) return;
            RefreshCD();
            progress.Refresh(_activity.Score, _activity.confG.MilestoneScore);
            rewardIcon.SetImage(_activity.confG.MilestoneRewardIcon2);
            tokenIcon.SetImage(Game.Manager.objectMan.GetBasicConfig(_activity.conf.Token).Icon.ConvertToAssetConfig());
        }

        private void RefreshCD()
        {
            if (!group.activeSelf)
            {
                return;
            }
            var v = _activity.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
            {
                Visible(false);
                if (UIManager.Instance.IsBlocking())
                {
                    UIManager.Instance.Block(false);
                }
            }
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }

        private void EntryClick()
        {
            _activity.Open();
        }

        private void OnMilestoneChange(int score, List<RewardCommitData> rewardList, CastleMilestoneGroup confG)
        {
            hasChange = true;
            lastConfG = confG;
            reward.Clear();
            reward.AddRange(rewardList);
        }
    }
}