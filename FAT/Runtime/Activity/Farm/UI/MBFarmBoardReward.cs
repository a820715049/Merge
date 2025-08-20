// ================================================
// File: MBFarmBoardReward.cs
// Author: yueran.li
// Date: 2025/04/25 19:01:13 星期五
// Desc: 农场奖励箱
// ================================================


using EL;
using FAT.Merge;
using fat.rawdata;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBFarmBoardReward : MonoBehaviour
    {
        private TextMeshProUGUI _rewardCount;
        private UIImageRes _rewardIcon;
        private Animator _animator;
        private GameObject _redPoint;
        private GameObject _root;

        private FarmBoardActivity _activity;

        public void Setup()
        {
            transform.Access("Root/BoardReward/Icon", out _rewardIcon);
            transform.Access("Root/dotCount/Count", out _rewardCount);
            _animator = transform.GetComponent<Animator>();
            _redPoint = transform.Find("Root/dotCount").gameObject;
            _root = transform.Find("Root").gameObject;
            transform.AddButton("Root/BoardReward", ClickReward);
        }

        public void Refresh(FarmBoardActivity _act)
        {
            _activity = _act;
            if (_activity.World.rewardCount > 0)
            {
                ShowReward();
            }
            else
            {
                _root.SetActive(false);
            }
        }

        private void ShowReward()
        {
            var item = _activity.World.nextRewardItem;
            if (item == null)
            {
                _root.SetActive(false);
                return;
            }

            _root.SetActive(true);
            var obj = Game.Manager.objectMan.GetBasicConfig(item.config.Id);
            _rewardIcon.SetImage(obj.Icon);
            if (_activity.World.rewardCount > 1)
            {
                _rewardCount.SetRedPoint(_activity.World.rewardCount);
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

            ShowReward();
        }

        public void FlyFeedBack(FlyableItemSlice slice)
        {
            if (slice.FlyType != FlyType.MergeItemFlyTarget) return;
            _animator.SetTrigger("Punch");
            Refresh(_activity);
        }
        
        public void RefreshWithPunch()
        {
            _animator.SetTrigger("Punch");
            Refresh(_activity);
        }
    }
}