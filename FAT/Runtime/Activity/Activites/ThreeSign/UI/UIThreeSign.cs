/*
 * @Author: ange.shentu
 * @Description: 三天签到活动UI类
 * @Date: 2025-06-12 16:47:40
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using Config;
using static EL.PoolMapping;

namespace FAT
{
    public class UIThreeSign : UIBase
    {
        private const int TAB_STATE_CURRENT = 0;
        private const int TAB_STATE_PAST = 1;
        private const int TAB_STATE_FUTURE = 2;
        private class SignTab
        {
            public MBRewardLayout layout;
            public MapButton closeButton;
            private UIStateGroup m_stateGroup;
            private Transform m_lockImg;
            private Animation m_collected;
            private Image m_gray;
            public SignTab(Transform transform)
            {
                layout = transform.Access<MBRewardLayout>();
                closeButton = transform.Access<MapButton>("confirm");
                m_stateGroup = transform.Access<UIStateGroup>();
                m_lockImg = transform.Access<Transform>("eff_lock");
                m_collected = transform.Access<Animation>("collected");
                m_gray = transform.Access<Image>("gray");
            }
            public void SetState(int state)
            {
                if (state == TAB_STATE_CURRENT || state == TAB_STATE_PAST)
                {
                    m_stateGroup.Select(0);
                }
                else
                {
                    m_stateGroup.Select(1);
                }
                m_lockImg.gameObject.SetActive(state == TAB_STATE_FUTURE || state == TAB_STATE_CURRENT);
                m_collected.gameObject.SetActive(state == TAB_STATE_PAST);
                closeButton.gameObject.SetActive(state != TAB_STATE_PAST);
                m_gray.gameObject.SetActive(state != TAB_STATE_CURRENT);
            }
            public void OnClicked()
            {
                m_collected.gameObject.SetActive(true);
                //这两个Animation不用考虑复位问题，因为就算是第二天复用，这两个组件的状态也是播完的状态.
                m_collected.Play("UIThreeSignTab_collected_show");
                closeButton.enabled = false;
                Animation anim = closeButton.Access<Animation>();
                if (anim != null)
                {
                    anim.Play("UIThreeSignTab_confirm_close");
                }
            }
            public void RefreshRewards(List<RewardConfig> rewardConfig)
            {
                layout.Refresh(rewardConfig);
            }
            public void PlayUnlockAnim()
            {
                Animation anim = m_lockImg.Access<Animation>("ani");
                if (anim != null)
                {
                    anim.Play("eff_lock_open");
                }
            }
        }

        [SerializeField] private Transform[] m_tabs;
        [SerializeField] private float m_closeDelay = 0.75f;

        private SignTab[] m_signTabs;
        private ActivityThreeSign m_actInst;
        // 从弹窗传递过来的奖励数据（已经BeginReward的）
        private Ref<List<RewardCommitData>> m_popupRewards;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_signTabs = new SignTab[m_tabs.Length];
            for (int i = 0; i < m_tabs.Length; i++)
            {
                var tmp_i = i;
                m_signTabs[tmp_i] = new SignTab(m_tabs[tmp_i]);
                m_signTabs[tmp_i].closeButton.WhenClick = () => OnCloseButtonClick(tmp_i);
                m_signTabs[tmp_i].closeButton.WithClickScale();
            }
        }

        private void OnCloseButtonClick(int tabIndex)
        {
            if (tabIndex != m_actInst.TotalSignDay - 1)
            {
                return;
            }
            m_signTabs[tabIndex].OnClicked();

            // 如果有弹窗传递的奖励，飞行后关闭
            if (m_popupRewards.Valid && m_popupRewards.obj.Count > 0)
            {
                for (int i = 0; i < m_popupRewards.obj.Count; i++)
                {
                    var pos = m_signTabs[tabIndex].layout.list[i].icon.transform.position;
                    UIFlyUtility.FlyReward(m_popupRewards.obj[i], pos);
                }
                DelayClose();
            }
            else
            {
                Close();
            }
        }
        private void DelayClose()
        {
            StartCoroutine(DelayCloseCoroutine());
        }

        private IEnumerator DelayCloseCoroutine()
        {
            yield return new WaitForSeconds(m_closeDelay);
            Close();
        }

        protected override void OnParse(params object[] items)
        {
            m_actInst = items[0] as ActivityThreeSign;
            // 如果有第二个参数，说明是从弹窗传递的奖励数据（已BeginReward）
            if (items.Length > 1)
            {
                m_popupRewards = (Ref<List<RewardCommitData>>)items[1];
            }
        }

        protected override void OnPreOpen()
        {
            // 刷新三天的状态和奖励显示
            for (int i = 0; i < m_tabs.Length && i < 3; i++)
            {
                _RefreshDayState(i);
            }
        }
        protected override void OnPostOpen()
        {
            base.OnPostOpen();

            int currentSignDay = m_actInst.TotalSignDay - 1;
            if (currentSignDay < 0 || currentSignDay >= m_signTabs.Length)
                return;

            m_signTabs[currentSignDay].PlayUnlockAnim();
        }
        protected override void OnPreClose()
        {
            base.OnPreClose();
            if (m_popupRewards.Valid)
            {
                m_popupRewards.Free();
            }
        }
        private void _RefreshDayState(int dayIndex)
        {
            if (m_actInst == null)
                return;

            int currentSignDay = m_actInst.TotalSignDay - 1;
            // 设置签到状态
            if (currentSignDay > dayIndex)
            {
                // 已签到
                m_signTabs[dayIndex].SetState(TAB_STATE_PAST);
            }
            else if (currentSignDay == dayIndex)
            {
                // 当前可签到
                m_signTabs[dayIndex].SetState(TAB_STATE_CURRENT);
            }
            else
            {
                // 未来签到
                m_signTabs[dayIndex].SetState(TAB_STATE_FUTURE);
            }

            // 刷新奖励显示
            _RefreshDayRewards(dayIndex);
        }

        private void _RefreshDayRewards(int dayIndex)
        {
            if (m_actInst?.Conf?.Rewards == null || dayIndex >= m_actInst.Conf.Rewards.Count)
                return;

            if (m_actInst.Conf.Rewards.TryGetByIndex(dayIndex, out var rewardPoolId))
            {
                var poolConf = Game.Manager.configMan.GetEventThreeSignPoolConfig(rewardPoolId);
                if (poolConf != null)
                {
                    var signReward = PoolMappingAccess.Take(out List<RewardConfig> rewardConfigList);
                    foreach (var item in poolConf.Pool)
                    {
                        var rewardConf = item.ConvertToRewardConfig();
                        if (rewardConf != null)
                        {
                            rewardConfigList.Add(rewardConf);
                        }
                    }
                    m_signTabs[dayIndex].RefreshRewards(rewardConfigList);
                    // 这个Refresh在Delay false的时候是个同帧逻辑，所以使用完后立即回收
                    signReward.Free();
                }
            }
        }
    }
}
