using EL;
using fat.rawdata;
using FAT.Merge;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBMineCartBoardReward : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rewardCount;
        [SerializeField] private UIImageRes _rewardIcon;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _redPoint;
        [SerializeField] private GameObject _root;
        public void Setup()
        {
            transform.AddButton("Root/BoardReward", ClickReward);
        }
        public void OnParse(MineCartActivity activity)
        {
            _activity = activity;
        }
        private MineCartActivity _activity;

        public void Refresh()
        {
            if (_activity.World.rewardCount > 0)
            {
                ShowReward();
            }
            else
            {
                _root.SetActive(false);
            }
        }
        // 中间状态强制刷新，用于锁定事件的时候做表现
        public void Refresh(Item item)
        {
            _root.SetActive(true);
            var obj = Game.Manager.objectMan.GetBasicConfig(item.config.Id);
            _rewardIcon.SetImage(obj.Icon);
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
            var count = _activity.World.rewardCount;
            _redPoint.SetActive(count > 0);
            _rewardCount.SetRedPoint(count);
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

        public void RefreshWithPunch()
        {
            _animator.SetTrigger("Punch");
            Refresh();
        }
    }
}
