using EL;
using fat.rawdata;
using FAT.Merge;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBMineBoardReward : MonoBehaviour
    {
        private TextMeshProUGUI _rewardCount;
        private UIImageRes _rewardIcon;
        private Animator _animator;
        private GameObject _redPoint;
        private GameObject _root;
        public void Setup()
        {
            transform.Access("Root/BoardReward/Icon", out _rewardIcon);
            transform.Access("Root/Point/Num", out _rewardCount);
            _animator = transform.GetComponent<Animator>();
            _redPoint = transform.Find("Root/Point").gameObject;
            _root = transform.Find("Root").gameObject;
            transform.AddButton("Root/BoardReward", ClickReward);
        }

        public void Refresh()
        {

            if (Game.Manager.mineBoardMan.World.rewardCount > 0)
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

            var item = Game.Manager.mineBoardMan.World.nextRewardItem;
            if (item == null)
            {
                _root.SetActive(false);
                return;
            }

            _root.SetActive(true);
            var obj = Game.Manager.objectMan.GetBasicConfig(item.config.Id);
            _rewardIcon.SetImage(obj.Icon);
            if (Game.Manager.mineBoardMan.World.rewardCount > 1)
            {
                _rewardCount.text = Game.Manager.mineBoardMan.World.rewardCount.ToString();
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
            Refresh();
        }
    }
}