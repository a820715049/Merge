/*
 *@Author:chaoran.zhang
 *@Desc:迷你棋盘奖励箱
 *@Created Time:2024.08.13 星期二 13:57:36
 */

using System;
using System.Collections;
using EL;
using FAT.Merge;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace FAT
{
    public class MBMiniBoardMultiReward : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private UIImageRes _reward;
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private GameObject _playBtn;
        [SerializeField] private GameObject _lock;

        public void SetUp()
        {
            if (!Game.Manager.miniBoardMultiMan.IsValid) return;
            if (Game.Manager.miniBoardMultiMan.World?.rewardCount > 0)
            {
                _root.gameObject.SetActive(true);
                ShowReward();
            }
            else
            {
                HideReward();
            }
        }

        public void ActivityEnd()
        {
            HideReward();
        }

        public void ClickReward()
        {
            if (Game.Manager.miniBoardMultiMan.CheckCanEnterNextRound() &&
                Game.Manager.miniBoardMultiMan.CheckHasNextBoard())
            {
                MessageCenter.Get<MSG.UI_CLICK_LOCK_REWARD>().Dispatch();
                return;
            }

            var world = BoardViewWrapper.GetCurrentWorld();
            BoardUtility.ClearSpawnRequest();
            BoardUtility.RegisterSpawnRequest(world.nextReward, _reward.transform.position);
            var item = world.activeBoard.SpawnRewardItemByIdx(0);
            if (item == null)
            {
                // clear
                BoardUtility.ClearSpawnRequest();
                //判断是否是卡包 若因为尝试点击开卡包而失败 则不弹提示(底层处理了可能因为连点导致连续开卡包的情况 这种情况不被允许)
                var nextItem = world.PeekRewardByIdx(0);
                if (nextItem > 0 && ItemUtility.IsCardPack(nextItem)) return;
                // 弹取出失败toast
                Game.Manager.commonTipsMan.ShowPopTips(Toast.BoardFullUi);
                Game.Manager.audioMan.TriggerSound("BoardFull");
            }
            else
            {
                // track
                DataTracker.gift_box_change.Track(item.tid, false);
                // 取出棋子播放音效
                Game.Manager.audioMan.TriggerSound("BoardReward");
            }

            ShowReward(true);
        }

        public void HideReward()
        {
            _root.SetActive(false);
            _playBtn.SetActive(false);
        }

        public void ShowReward(bool now = false)
        {
            if (!Game.Manager.miniBoardMultiMan.IsValid) return;
            if (Game.Manager.miniBoardMultiMan.CheckHasNextBoard() &&
                Game.Manager.miniBoardMultiMan.CheckCanEnterNextRound())
                _lock.SetActive(true);
            else
                _lock.SetActive(false);
            var item = Game.Manager.miniBoardMultiMan.World?.nextRewardItem;
            if (item == null)
            {
                _root.SetActive(false);
                if (now)
                    StartCoroutine(ShowBtn());
                else
                    _playBtn.SetActive(true);
                return;
            }

            _playBtn.SetActive(false);
            _root.SetActive(true);
            var obj = Game.Manager.objectMan.GetBasicConfig(item.config.Id);
            _reward.SetImage(obj.Icon);
            _num.text = Game.Manager.miniBoardMultiMan.World?.rewardCount.ToString();
        }

        public void RefreshReward(bool need)
        {
            ShowReward(true);
        }

        private IEnumerator ShowBtn()
        {
            yield return new WaitForSeconds(3.5f);
            var item = Game.Manager.miniBoardMultiMan.World?.nextRewardItem;
            if (item == null)
                _playBtn.SetActive(true);
        }
    }
}