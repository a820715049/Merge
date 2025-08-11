/*
 * @Author: qun.chao
 * @Date: 2023-11-01 11:35:50
 */

using Coffee.UIExtensions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using fat.rawdata;
using EL;
using FAT.Merge;
using FAT.MSG;
using uTools;

namespace FAT
{
    public class MBMergeCompReward : MonoBehaviour
    {

        [SerializeField] private Button btnClaim;
        [SerializeField] private Merge.MBItemView topItem;
        [SerializeField] private TMPro.TMP_Text txtNum;
        [SerializeField] private UIParticle disappearEffect;
        private bool ignoreRefresh = false;

        public void Setup()
        {
            btnClaim.WithClickScale().FixPivot().onClick.AddListener(_OnBtnClaim);
        }

        public void InitOnPreOpen()
        {
            ignoreRefresh = false;
            _Refresh();
            BoardViewWrapper.GetCurrentWorld().onRewardListChange += _OnHandleRewardChange;
            BoardViewWrapper.GetCurrentWorld().onItemEvent += _OnItemEvent;
            BoardViewWrapper.SetParam(BoardViewWrapper.ParamType.CompRewardTrack, this);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().AddListener(_OnMessageLevelChange);
            MessageCenter.Get<MSG.GAME_MERGE_PRE_BEGIN_REWARD>().AddListener(_OnMessagePreBeginReward);
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().AddListener(_OnMessagePostCommitReward);
        }

        public void CleanupOnPostClose()
        {
            topItem.ClearData();
            BoardViewWrapper.GetCurrentWorld().onRewardListChange -= _OnHandleRewardChange;
            BoardViewWrapper.GetCurrentWorld().onItemEvent -= _OnItemEvent;
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().RemoveListener(_OnMessageLevelChange);
            MessageCenter.Get<MSG.GAME_MERGE_PRE_BEGIN_REWARD>().RemoveListener(_OnMessagePreBeginReward);
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().RemoveListener(_OnMessagePostCommitReward);
        }

        public Transform FirstRewardTrans()
        {
            return topItem.transform;
        }

        private void _RrefreshNum(int num)
        {
            txtNum.text = $"{num}";
            txtNum.transform.parent.gameObject.SetActive(num > 0);
        }

        private void _RrefreshRoot(bool show)
        {
            gameObject.SetActive(show);
        }

        private void _Refresh()
        {
            topItem.ClearData();
            var world = BoardViewWrapper.GetCurrentWorld();
            int rewardCount = world.rewardCount;
            _RrefreshNum(rewardCount);
            if (rewardCount < 1)
            {
                _RrefreshRoot(false);
            }
            else
            {
                _RrefreshRoot(true);
                topItem.SetData(world.PeekNextReward());
            }

            var tipAnimLevelLimit = Game.Manager.configMan.globalConfig.GiftBoxStopShaking;
            if (Game.Manager.mergeLevelMan.level >= tipAnimLevelLimit)
            {
                btnClaim.transform.GetComponent<uTools.TweenScale>().enabled = false;
                btnClaim.transform.localScale = Vector3.one;
            }
            else
            {
                btnClaim.transform.GetComponent<uTools.TweenScale>().enabled = true;
            }
        }

        private void _OnHandleRewardChange(bool add)
        {
            if (ignoreRefresh)
                return;
            _Refresh();
        }

        private void _OnBtnClaim()
        {
            var world = BoardViewWrapper.GetCurrentWorld();
            BoardUtility.ClearSpawnRequest();
            BoardUtility.RegisterSpawnRequest(world.nextReward, topItem.transform.position);
            MessageCenter.Get<GAME_CLAIM_REWARD>().Dispatch();

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
                // 礼物盒取出后不再自动选中
                // // 直接刷新选中
                // BoardViewManager.Instance.RefreshInfo(item.coord.x, item.coord.y);
                // track
                DataTracker.gift_box_change.Track(item.tid, false);
                // 取出棋子播放音效
                Game.Manager.audioMan.TriggerSound("BoardReward");
            }
        }

        private void _OnMessageLevelChange(int oldLevel)
        {
            _Refresh();
        }

        private void _OnMessagePreBeginReward(RewardCommitData data)
        {
            // 发棋子奖励和卡包 尝试在commit之后再刷新
            if (data.rewardType == ObjConfigType.MergeItem || data.rewardType == ObjConfigType.CardPack)
            {
                //若奖励来源为迷你棋盘相关 则无视
                if (data.reason == ReasonString.miniboard_getitem || Game.Manager.miniBoardMultiMan.CheckIsMiniBoardItem(data.rewardId))
                    return;
                ignoreRefresh = true;
            }
        }

        private void _OnMessagePostCommitReward(RewardCommitData data)
        {
            if ((data.rewardType == ObjConfigType.MergeItem || data.rewardType == ObjConfigType.CardPack) && ignoreRefresh)
            {
                //若奖励来源为迷你棋盘相关 则无视
                if (data.reason == ReasonString.miniboard_getitem || Game.Manager.miniBoardMultiMan.CheckIsMiniBoardItem(data.rewardId))
                    return;
                ignoreRefresh = false;
                transform.DOScale(1f, 0.1f).From(0.7f);
                _Refresh();
            }
        }

        private void _OnItemEvent(Item item, ItemEventType eventType)
        {
            if (eventType == ItemEventType.ItemEventRewardDisappear)
            {
                disappearEffect.Stop();
                disappearEffect.Play();
                Game.Manager.commonTipsMan.ShowPopTips(Toast.MiniBoardMultiEndItem);
            }
        }
    }
}