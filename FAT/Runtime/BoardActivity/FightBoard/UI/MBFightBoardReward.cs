/**
 * @Author: zhangpengjian
 * @Date: 2025/5/14 16:39:16
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/14 16:39:16
 * Description: 打怪棋盘奖励箱
 */

using System.Collections;
using EL;
using fat.rawdata;
using FAT.Merge;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBFightBoardReward : MonoBehaviour
    {
        private TextMeshProUGUI _rewardCount;
        private UIImageRes _rewardIcon;
        private Animator _animator;
        private GameObject _redPoint;
        private GameObject _root;
        private GameObject _playBtn;
        private FightBoardActivity _activity;
        
        public void Setup()
        {
            transform.Access("Root/BoardReward/Icon", out _rewardIcon);
            transform.Access("Root/Point/Num", out _rewardCount);
            _animator = transform.GetComponent<Animator>();
            _redPoint = transform.Find("Root/Point").gameObject;
            _playBtn = transform.Find("BtnClaim").gameObject;
            _root = transform.Find("Root").gameObject;
            transform.AddButton("Root/BoardReward", ClickReward);
        }

        public void Refresh(FightBoardActivity activity)
        {
            _activity = activity;
            if (_activity.World.rewardCount > 0)
            {
                ShowReward();
            }
            else
            {
                _root.SetActive(false);
                _playBtn.SetActive(true);
            }
        }

        private void ShowReward(bool now = false)
        {
            var item = _activity.World.nextRewardItem;
            if (item == null)
            {
                _root.SetActive(false);
                if (now)
                {
                    StartCoroutine(ShowBtn());
                }
                else
                {
                    _playBtn.SetActive(true);
                }
                return;
            }

            _playBtn.SetActive(false);
            _root.SetActive(true);
            var obj = Game.Manager.objectMan.GetBasicConfig(item.config.Id);
            _rewardIcon.SetImage(obj.Icon);
            if (_activity.World.rewardCount > 1)
            {
                _rewardCount.text = _activity.World.rewardCount.ToString();
                _redPoint.SetActive(true);
            }
            else
            {
                _redPoint.SetActive(false);
            }
        }

        private void ClickReward()
        {
            var world = BoardViewWrapper.GetCurrentWorld();
            BoardUtility.ClearSpawnRequest();
            BoardUtility.RegisterSpawnRequest(world.nextReward, _rewardIcon.transform.position);
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

        public void FlyFeedBack(FlyableItemSlice slice)
        {
            if (slice.FlyType != FlyType.MergeItemFlyTarget) return;
            _animator.SetTrigger("Punch");
            Refresh(_activity);
        }

        private IEnumerator ShowBtn()
        {
            yield return new WaitForSeconds(2f);
            var item = Game.Manager.miniBoardMultiMan.World?.nextRewardItem;
            if (item == null)
            {
                _playBtn.SetActive(true);
            }
        }

        public void RefreshWithPunch()
        {
            _animator.SetTrigger("Punch");
            Refresh(_activity);
        }
    }
}