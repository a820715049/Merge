/**
 * @Author: zhangpengjian
 * @Date: 2025/1/14 10:44:04
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/1/14 10:44:04
 * Description: 卡册印章
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FAT.MSG;
using EL;
using System;
using System.Collections.Generic;
using Config;
using System.Collections;
using fat.rawdata;

namespace FAT
{
    using static MessageCenter;

    public class UICardStamp : UIBase
    {
        [SerializeField] private UICardStampCell[] cells;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnGo;
        [SerializeField] private TextProOnACircle title;
        [SerializeField] private TextMeshProUGUI desc1;
        [SerializeField] private TextMeshProUGUI desc2;
        [SerializeField] private TextMeshProUGUI btnDesc;
        [SerializeField] private TextMeshProUGUI cd;
        [SerializeField] private UIImageRes bg;
        [SerializeField] private UIImageRes titleBg;
        [SerializeField] private GameObject[] rewardIconGo;
        [SerializeField] private GameObject block;

        private ActivityStamp activityStamp;
        private Action WhenTick;
        private List<int> finishIndexList = new();
        private Dictionary<int, List<RewardCommitData>> finishRewardDict = new();
        private EventStampRound r;

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0 && items[0] is ActivityStamp activity)
            {
                activityStamp = activity;
                if (items.Length > 1 && items[1] != null)
                {
                    finishIndexList = items[1] as List<int>;
                }
                if (items.Length > 2 && items[2] != null)
                {
                    finishRewardDict = items[2] as Dictionary<int, List<RewardCommitData>>;
                }
                if (items.Length > 3 && items[3] != null)
                {
                    r = items[3] as EventStampRound;
                }
            }
        }

        protected override void OnPreOpen()
        {
            block.gameObject.SetActive(false);
            if (r == null)
            {
                r = activityStamp.GetCurRoundConfig();
            }
            if (r == null)
            {
                return;
            }
            RefreshTheme();
            RefreshData();
            TryStampAndCommitExtraReward();
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            Get<UI_SPECIAL_REWARD_FINISH>().AddListener(OnRandomBoxFinish);
        }

        private void TryStampAndCommitExtraReward()
        {
            if (finishIndexList.Count > 0)
            {
                StartCoroutine(CoShowStamp());
            }
        }

        private IEnumerator CoShowStamp()
        {
            block.gameObject.SetActive(true);
            if (finishIndexList.Count > 0)
            {
                cells[finishIndexList[0] - 1].PlayAnim();
            }
            yield return new WaitForSeconds(2f);
            if (r == null)
            {
                yield break;
            }
            if (finishIndexList.Count > 0 && finishRewardDict.Count > 0 && finishRewardDict.ContainsKey(finishIndexList[0]) && finishIndexList[0] != r.Level)
            {
                var listR = finishRewardDict[finishIndexList[0]];
                foreach (var reward in listR)
                {
                    Game.Manager.rewardMan.CommitReward(reward);
                }
            }
            block.gameObject.SetActive(false);
        }

        protected override void OnPreClose()
        {
            r = null;
            block.gameObject.SetActive(false);
            finishIndexList.Clear();
            finishRewardDict.Clear();
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            Get<UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnRandomBoxFinish);
        }

        private void OnRandomBoxFinish()
        {
            if (r == null)
            {
                Close();
                return;
            }
            if (finishIndexList.Count > 0 && finishIndexList[0] != r.Level)
            {
                return;
            }
            Close();
        }

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(OnClickClose);
            btnGo.onClick.AddListener(OnClickClose);
            WhenTick ??= RefreshCD;
            for (int i = 0; i < rewardIconGo.Length; i++)
            {
                var index = i;
                rewardIconGo[i].GetComponent<Button>().onClick.RemoveAllListeners();
                rewardIconGo[i].GetComponent<Button>().onClick.AddListener(() => OnClickReward(index));
            }
        }

        private void OnClickReward(int index)
        {
            if (r == null)
                return;
            if (r.LevelRewards.Count == 0)
                return;
            var reward = r.LevelRewards[index].ConvertToRewardConfig();
            var root = rewardIconGo[index].GetComponent<UIImageRes>().image.rectTransform;
            UIItemUtility.ShowItemTipsInfo(reward.Id, root.position, 4 + root.rect.size.y * 0.5f);
        }

        private void OnClickClose()
        {
            if (r == null)
            {
                Close();
                return;
            }
            if (finishIndexList.Count > 0 && finishIndexList[0] == r.Level && finishRewardDict.ContainsKey(finishIndexList[0]))
            {
                var listR = finishRewardDict[finishIndexList[0]];
                foreach (var reward in listR)
                {
                    Game.Manager.rewardMan.CommitReward(reward);
                }
            }
            else
            {
                Close();
            }
        }

        private void RefreshData()
        {
            if (r == null)
            {
                return;
            }
            for (int i = 0; i < rewardIconGo.Length; i++)
            {
                rewardIconGo[i].SetActive(false);
            }
            if (r.LevelRewards.Count > 0)
            {
                for (int i = 0; i < r.LevelRewards.Count; i++)
                {
                    var reward = r.LevelRewards[i].ConvertToRewardConfig();
                    if (reward != null)
                    {
                        rewardIconGo[i].SetActive(true);
                        rewardIconGo[i].GetComponent<UIImageRes>().SetImage(Game.Manager.rewardMan.GetRewardIcon(reward.Id, reward.Count));
                    }
                }
            }
            var idx = finishIndexList.Count > 0 && finishIndexList[0] >= r.Level ? finishIndexList[0] : activityStamp.GetCurFinishIndex();
            if (r.GiftRewards.Count <= r.Level)
            {
                for (int i = 0; i < r.Level; i++)
                {
                    var (hasExtraReward, icon) = _HasExtraReward(i);
                    cells[i].Setup(i + 1, finishIndexList.Count > 0 ? i + 1 < idx : i < idx, hasExtraReward, icon);
                }
            }
        }

        private (bool, AssetConfig) _HasExtraReward(int index)
        {
            if (r == null)
            {
                return (false, null);
            }
            foreach (var reward in r.GiftRewards)
            {
                if (reward.ConvertToInt3().Item3 == index + 1)
                {
                    return (true, Game.Manager.rewardMan.GetRewardIcon(reward.ConvertToInt3().Item1, reward.ConvertToInt3().Item2));
                }
            }
            return (false, null);
        }

        public void RefreshCD()
        {
            if (activityStamp.Valid && activityStamp.Countdown > 0)
            {
                UIUtility.CountDownFormat(cd, activityStamp.Countdown);
            }
            else
            {
                Close();
            }
        }

        private void RefreshTheme()
        {
            activityStamp.Visual.Refresh(bg, "bgImage");
            activityStamp.Visual.Refresh(titleBg, "titleBg");
            activityStamp.Visual.Refresh(title, "mainTitle");
            if (r == null)
            {
                return;
            }
            if (finishIndexList.Count > 0 && finishIndexList[0] == r.Level)
            {
                activityStamp.Visual.Refresh(desc1, "desc3");
                activityStamp.Visual.Refresh(desc2, "desc4");
                activityStamp.Visual.Refresh(btnDesc, "Btn2");
            }
            else
            {
                activityStamp.Visual.Refresh(desc1, "desc1");
                activityStamp.Visual.Refresh(desc2, "desc2");
                activityStamp.Visual.Refresh(btnDesc, "Btn1");
            }
        }
    }
}