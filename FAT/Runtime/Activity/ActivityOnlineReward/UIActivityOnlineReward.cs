/**FileHeader
 * @Author: zhangpengjian
 * @Date: 2025/8/20 14:05:04
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/8/20 14:09:10
 * @Description: 
 * @Copyright: Copyright (©)}) 2025 zhangpengjian. All rights reserved.
 */

using EL;
using FAT;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Collections;

namespace FAT
{
    [Serializable]
    public class RewardGroup
    {
        [SerializeField] public List<GameObject> rewardItems = new List<GameObject>();
    }

    public class UIActivityOnlineReward : UIBase
    {
        [SerializeField] private List<RewardGroup> rewardGroups;
        [SerializeField] private List<GameObject> rewardList;
        [SerializeField] private TMP_Text cd;
        [SerializeField] private TMP_Text rewardCD;
        [SerializeField] private Button closeBtn;
        [SerializeField] private Button claimBtn;
        [SerializeField] private Button cdBtn;
        [SerializeField] private GameObject block;
        [SerializeField] private GameObject efx;
        [SerializeField] private float delay;
        
        private ActivityOnlineReward _activity;
        private bool _isEnd;
        private bool _clickEnd;
        private bool _previousHasReward;  // 记录上一次的奖励状态

        protected override void OnParse(params object[] items)
        {
            _activity = (ActivityOnlineReward)items[0];
            if (items.Length > 1 && items[1] != null)
            {
                _isEnd = (bool)items[1];
            }
        }

        protected override void OnCreate()
        {
            closeBtn.WithClickScale().FixPivot().onClick.AddListener(OnClose);
            claimBtn.WithClickScale().FixPivot().onClick.AddListener(OnClaim);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(OnOneSecondDriver);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnOneSecondDriver);
        }

        private void OnOneSecondDriver()
        {
            OnOneSecond();
        }

        protected override void OnPreOpen()
        {
            efx.SetActive(false);
            block.SetActive(false);
            // 初始化时记录当前奖励状态，避免立即触发特效
            _previousHasReward = _activity != null && _activity.HasReward();
            OnOneSecond(true);
            GameUIUtility.SetGrayShader(cdBtn.image);
            cdBtn.interactable = false;
            var idx = _isEnd ? _activity.preOnlineIndex : _activity.onlineIndex;
            for (int i = 0; i < _activity.conf.IncludeReward.Count; i++)
            {
                var reward = _activity.conf.IncludeReward[i];
                var confD = fat.conf.EventOnlineDetailVisitor.GetOneByFilter((c) => c.Id == reward);
                for (int j = 0; j < rewardGroups[i].rewardItems.Count; j++)
                {
                    rewardGroups[i].rewardItems[j].gameObject.SetActive(false);
                }
                for (int j = 0; j < confD.Rewards.Count; j++)
                {
                    var rewardConfig = confD.Rewards[j].ConvertToRewardConfig();
                    rewardGroups[i].rewardItems[j].GetComponent<UICommonItem>().Refresh(rewardConfig, i == idx ? 17 : 13);
                    rewardGroups[i].rewardItems[j].gameObject.SetActive(true);
                }
            }

            for (int i = 0; i < rewardList.Count; i++)
            {
                rewardList[i].transform.GetChild(0).gameObject.SetActive(i != idx);
                rewardList[i].transform.GetChild(3).gameObject.SetActive(i < idx);
                rewardList[i].transform.GetChild(4).gameObject.SetActive(i > idx);
                rewardList[i].transform.GetChild(2).GetComponent<TextMeshProUGUI>().SetText(I18N.FormatText("#SysComDesc1568", i + 1));
                rewardList[i].transform.GetChild(2).GetComponent<UITextState>().Select(i == idx ? 1 : 0);
                rewardList[i].transform.GetChild(3).GetChild(2).gameObject.SetActive(false);
                rewardList[i].transform.GetChild(4).GetChild(0).GetChild(1).gameObject.SetActive(false);
            }
        }

        private void OnOneSecond(bool isInit = false)
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            cd.SetCountDown(diff);
            if (diff <= 0 && !_activity.HasReward())
            {
                Close();
                return;
            }

            bool currentHasReward = _activity.HasReward();
            if (currentHasReward && _activity.onlineIndex < _activity.conf.IncludeReward.Count)
            {
                claimBtn.gameObject.SetActive(!_clickEnd);
                // 只在从不可领取变为可领取的瞬间激活特效
                if (!isInit && !_previousHasReward && currentHasReward)
                {
                    efx.SetActive(true);
                }
                cdBtn.gameObject.SetActive(false);
            }
            else
            {
                if (_activity.onlineIndex >= _activity.conf.IncludeReward.Count || _isEnd || (_activity.Countdown <= 0 && !_activity.HasReward()))
                {
                    cdBtn.gameObject.SetActive(false);
                }
                else
                {
                    cdBtn.gameObject.SetActive(true);
                }
                claimBtn.gameObject.SetActive(false);
                var r = (long)Mathf.Max(0, _activity.onlineTs - t);
                rewardCD.SetCountDown(r);
            }
            
            // 更新之前的奖励状态
            if (!isInit)
            {
                _previousHasReward = currentHasReward;
            }
        }

        private void OnClose()
        {
            if (!_isEnd)
            {
                Close();
            }
            else
            {
                OnClaim();
            }
        }
        
        private void OnClaim()
        {
            if (!_isEnd)
            {
                _activity.ClaimReward();
            }
            block.SetActive(true);
            cdBtn.gameObject.SetActive(true);
            claimBtn.gameObject.SetActive(_isEnd ? false :_activity.HasReward());
            if (_isEnd)
            {
                _clickEnd = true;
            }
            StartCoroutine(PlayAnim());
            if (_activity.onlineIndex >= _activity.conf.IncludeReward.Count || _isEnd || (_activity.Countdown <= 0 && !_activity.HasReward()))
            {
                cdBtn.gameObject.SetActive(false);
                claimBtn.gameObject.SetActive(false);
                StartCoroutine(DelayClose());
            }
            else
            {
                StartCoroutine(DelayUnblock());
            }
        }

        private IEnumerator DelayUnblock()
        {
            yield return new WaitForSeconds(1.5f);
            block.SetActive(false);
        }

        protected override void OnPostClose()
        {
            _isEnd = false;
            _clickEnd = false;
        }

        private IEnumerator DelayClose()
        {
            yield return new WaitForSeconds(1.5f);
            block.SetActive(false);
            Close();
        }

        private IEnumerator PlayAnim()
        {
            // 处理多档位奖励的情况
            if (_activity.claimedIndexes.Count > 1)
            {
                // 多档位：播放所有档位的飞行动画
                int rewardIndex = 0;
                for (int claimIndex = 0; claimIndex < _activity.claimedIndexes.Count; claimIndex++)
                {
                    var claimedIdx = _activity.claimedIndexes[claimIndex];
                    var confD = fat.conf.EventOnlineDetailVisitor.GetOneByFilter((c) => c.Id == _activity.conf.IncludeReward[claimedIdx]);
                    
                    // 播放当前档位的奖励飞行动画
                    for (int j = 0; j < confD.Rewards.Count && rewardIndex < _activity.rewardList.Count; j++, rewardIndex++)
                    {
                        var pos = rewardGroups[claimedIdx].rewardItems[j].transform.position;
                        UIFlyUtility.FlyReward(_activity.rewardList[rewardIndex], pos);
                    }
                }
                
                yield return new WaitForSeconds(delay);
                
                // 更新所有已领取档位的UI状态
                var config = FontMaterialRes.Instance.GetFontMatResConf(13);
                var config2 = FontMaterialRes.Instance.GetFontMatResConf(17);
                
                for (int claimIndex = 0; claimIndex < _activity.claimedIndexes.Count; claimIndex++)
                {
                    var claimedIdx = _activity.claimedIndexes[claimIndex];
                    
                    // 应用字体配置
                    for (int j = 0; j < rewardGroups[claimedIdx].rewardItems.Count; j++)
                    {
                        config?.ApplyFontMatResConfig(rewardGroups[claimedIdx].rewardItems[j].transform.GetChild(1).GetComponent<TMP_Text>());
                    }
                    
                    // 更新UI状态
                    rewardList[claimedIdx].transform.GetChild(0).gameObject.SetActive(true);
                    rewardList[claimedIdx].transform.GetChild(3).gameObject.SetActive(true);
                    rewardList[claimedIdx].transform.GetChild(4).gameObject.SetActive(false);
                    rewardList[claimedIdx].transform.GetChild(3).GetChild(2).gameObject.SetActive(true);
                    rewardList[claimedIdx].transform.SetAsLastSibling();
                    rewardList[claimedIdx].transform.GetChild(3).GetComponent<Animation>().Play();
                    rewardList[claimedIdx].transform.GetChild(2).GetComponent<UITextState>().Select(0);
                }
                
                // 处理下一个档位的显示（如果存在）
                if (_activity.onlineIndex < _activity.conf.IncludeReward.Count)
                {
                    for (int j = 0; j < rewardGroups[_activity.onlineIndex].rewardItems.Count; j++)
                    {
                        config2?.ApplyFontMatResConfig(rewardGroups[_activity.onlineIndex].rewardItems[j].transform.GetChild(1).GetComponent<TMP_Text>());
                    }
                    
                    rewardList[_activity.onlineIndex].transform.SetAsLastSibling();
                    StartCoroutine(DelaySelect());
                    rewardList[_activity.onlineIndex].transform.GetChild(4).gameObject.SetActive(true);
                    rewardList[_activity.onlineIndex].transform.GetChild(4).GetChild(0).GetChild(1).gameObject.SetActive(true);
                    rewardList[_activity.onlineIndex].transform.GetChild(4).GetChild(0).GetComponent<Animation>().Play();
                }
            }
            else
            {
                // 单档位：保持原来的逻辑
                for (int i = 0; i < _activity.rewardList.Count; i++)
                {
                    var pos = rewardGroups[_activity.onlineIndex - 1].rewardItems[i].transform.position;
                    UIFlyUtility.FlyReward(_activity.rewardList[i], pos);
                }
                yield return new WaitForSeconds(delay);
                for (int i = 0; i < _activity.rewardList.Count; i++)
                {
                    var config = FontMaterialRes.Instance.GetFontMatResConf(13);
                    var config2 = FontMaterialRes.Instance.GetFontMatResConf(17);
                    for (int j = 0; j < rewardGroups[_activity.onlineIndex - 1].rewardItems.Count; j++)
                    {
                        config?.ApplyFontMatResConfig(rewardGroups[_activity.onlineIndex - 1].rewardItems[j].transform.GetChild(1).GetComponent<TMP_Text>());
                    }
                    if (_activity.onlineIndex < _activity.conf.IncludeReward.Count)
                    {
                        for (int j = 0; j < rewardGroups[_activity.onlineIndex].rewardItems.Count; j++)
                        {
                            config2?.ApplyFontMatResConfig(rewardGroups[_activity.onlineIndex].rewardItems[j].transform.GetChild(1).GetComponent<TMP_Text>());
                        }
                    }
                }
                rewardList[_activity.onlineIndex - 1].transform.GetChild(0).gameObject.SetActive(true);
                rewardList[_activity.onlineIndex - 1].transform.GetChild(3).gameObject.SetActive(true);
                rewardList[_activity.onlineIndex - 1].transform.GetChild(3).GetChild(2).gameObject.SetActive(true);
                rewardList[_activity.onlineIndex - 1].transform.SetAsLastSibling();
                rewardList[_activity.onlineIndex - 1].transform.GetChild(3).GetComponent<Animation>().Play();
                rewardList[_activity.onlineIndex - 1].transform.GetChild(2).GetComponent<UITextState>().Select(0);
                if (_activity.onlineIndex < _activity.conf.IncludeReward.Count)
                {
                    rewardList[_activity.onlineIndex].transform.SetAsLastSibling();
                    StartCoroutine(DelaySelect());
                    rewardList[_activity.onlineIndex].transform.GetChild(4).gameObject.SetActive(true);
                    rewardList[_activity.onlineIndex].transform.GetChild(4).GetChild(0).GetChild(1).gameObject.SetActive(true);
                    rewardList[_activity.onlineIndex].transform.GetChild(4).GetChild(0).GetComponent<Animation>().Play();
                }
            }
        }

        private IEnumerator DelaySelect()
        {
            yield return new WaitForSeconds(1f);
            rewardList[_activity.onlineIndex].transform.GetChild(2).GetComponent<UITextState>().Select(1);
            rewardList[_activity.onlineIndex].transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}
