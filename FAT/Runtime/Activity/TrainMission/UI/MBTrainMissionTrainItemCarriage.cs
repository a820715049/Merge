// ==================================================
// // File: MBTrainMissionProgressItem.cs
// // Author: liyueran
// // Date: 2025-07-29 15:07:45
// // Desc: $火车任务 火车车厢
// // ==================================================

using System;
using System.Collections.Generic;
using EL;
using FAT.Merge;
using FAT.MSG;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBTrainMissionTrainItemCarriage : MBTrainMissionTrainItem
    {
        public Animator animator;
        public Animator bgAnimator;

        public UIImageRes missionIcon;
        public GameObject coverMask;
        public GameObject check;
        public GameObject goBtn;
        public Animator goAnimator;

        public GameObject timeLimit;
        public TextMeshProUGUI timeLimitCd;

        public UIImageRes timeLimitIcon;
        public TextMeshProUGUI timeLimitCount;

        public GameObject countDown;

        private TrainMissionItemInfo _itemInfo;
        private List<RewardCommitData> _reward;
        [SerializeField] private int _state;


        #region Mono
        private void Start()
        {
            transform.AddButton("bgroot/bg/missionIcon", OnClickIcon).WithClickScale().FixPivot();
            transform.AddButton("bgroot/go", OnClickGO).WithClickScale().FixPivot();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<MSG.UI_BOARD_DIRTY>().AddListener(_OnMessageBoardDirty);
            MessageCenter.Get<MSG.UI_ON_ORDER_ITEM_CONSUMED>().AddListener(_OnMessageCommitItem);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<MSG.UI_BOARD_DIRTY>().RemoveListener(_OnMessageBoardDirty);
            MessageCenter.Get<MSG.UI_ON_ORDER_ITEM_CONSUMED>().RemoveListener(_OnMessageCommitItem);
        }
        #endregion


        public override void Init(TrainMissionActivity act, UITrainMissionTrainModule module, MBTrainMissionTrain train,
            int index)
        {
            base.Init(act, module, train, index);

            Train.TryGetItemInfo(Index, out _itemInfo);

            // 根据数据 处理车厢表现 
            SetCarriageView();
        }

        private void SetCarriageView()
        {
            if (_itemInfo == null)
            {
                return;
            }

            // 设置任务图标
            var conf = Game.Manager.objectMan.GetBasicConfig(_itemInfo.itemID);
            missionIcon.SetImage(conf.Icon);

            // 未完成 待提交 已完成 三种状态的处理
            // 1:未完成 2:可以完成 3:已完成 
            _state = Activity.CheckMissionState(Train.TrainOrder, Index);

            goBtn.SetActive(_state == 2);
            coverMask.SetActive(_state == 3);
            check.SetActive(_state == 3);

            timeLimit.SetActive(IsTimeLimitValid(out _, out _));
            countDown.SetActive(IsTimeLimitValid(out _, out _));
            // 限时任务显示
            if (IsTimeLimitValid(out _, out var info))
            {
                var limitConf = Game.Manager.objectMan.GetBasicConfig(info.rewardID);
                timeLimitIcon.SetImage(limitConf.Icon);
                timeLimitCount.SetText($"x{info.rewardCount}");
            }

            if (_state == 1)
            {
                // 未完成
                animator.SetTrigger("CarriageIdle");
            }
            else if (_state == 2)
            {
                // 待提交
                goAnimator.SetTrigger("Show");
                animator.SetTrigger("CarriageIdle");
            }
            else if (_state == 3)
            {
                // 已完成
                bgAnimator.SetTrigger("CloseIdle");
            }
        }

        private void ChangeCarriageView()
        {
            if (_itemInfo == null)
            {
                return;
            }

            // 设置任务图标
            var conf = Game.Manager.objectMan.GetBasicConfig(_itemInfo.itemID);
            missionIcon.SetImage(conf.Icon);

            // 未完成 待提交 已完成 三种状态的处理
            // 1:未完成 2:可以完成 3:已完成 
            var curState = Activity.CheckMissionState(Train.TrainOrder, Index);

            if (curState == 2)
            {
                // 有已完成待提交棋子
                Train.AutoScroll();
            }

            // 状态一致
            if (curState == _state)
            {
                return;
            }

            if (curState == 1)
            {
                goAnimator.SetTrigger("Hide");
                coverMask.SetActive(_state == 3);
                check.SetActive(_state == 3);

                timeLimit.SetActive(IsTimeLimitValid(out _, out _));
                countDown.SetActive(IsTimeLimitValid(out _, out _));

                // 未完成
                animator.SetTrigger("CarriageIdle");

                // 限时任务显示
                if (IsTimeLimitValid(out _, out var info))
                {
                    var limitConf = Game.Manager.objectMan.GetBasicConfig(info.rewardID);
                    timeLimitIcon.SetImage(limitConf.Icon);
                    timeLimitCount.SetText($"{info.rewardCount}");
                }
            }
            else if (curState == 2)
            { 
                Game.Manager.audioMan.TriggerSound("TrainItemEnable"); // 火车-棋盘检测到有可提交棋子
                
                // 待提交
                goBtn.SetActive(true);
                goAnimator.SetTrigger("Show");
                animator.SetTrigger("CarriageIdle");
            }


            _state = curState;
        }

        protected override void MoveIn(MBTrainMissionTrain.TrainType type)
        {
            if (!CheckBelongTrain(type))
            {
                return;
            }

            animator.SetTrigger("Move");
        }

        protected override void StopMove(MBTrainMissionTrain.TrainType type)
        {
            if (!CheckBelongTrain(type))
            {
                return;
            }

            animator.SetTrigger("Stop");
        }

        protected override void MoveOut(MBTrainMissionTrain.TrainType type)
        {
            if (!CheckBelongTrain(type))
            {
                return;
            }

            timeLimit.SetActive(IsTimeLimitValid(out _, out _));
            countDown.SetActive(IsTimeLimitValid(out _, out _));
            animator.SetTrigger("Move");
        }

        // 是否是限时任务
        private bool IsTimeLimitValid(out int end, out SpecialMissionInfo info, bool ignoreState = false)
        {
            var t = Game.Instance.GetTimestampSeconds();
            var checkConf = Train.TrainOrder.TryGetSpecialMission(Index, out end, out info);
            var checkCd = t <= end;
            var checkState = Activity.CheckMissionState(Train.TrainOrder, Index) != 3 || ignoreState;

            return checkConf && checkCd && checkState;
        }


        #region 事件
        private void OnClickIcon()
        {
            if (_state == 3)
            {
                return;
            }

            if (_state == 2)
            {
                OnClickGO();
                return;
            }

            // 打开详情界面
            UIManager.Instance.OpenWindow(UIConfig.UITrainMissionItemInfo, Activity, _itemInfo, _state);
        }

        // 提交任务
        public void OnClickGO()
        {
            // 检查是否是可完成状态
            if (Activity.CheckMissionState(Train.TrainOrder, Index) != 2)
            {
                return;
            }

            if (Train.consumeIdx != -1)
            {
                return;
            }


            Item itemInInventory = null;

            void WalkItem(Item item)
            {
                if (item.tid == _itemInfo.itemID) itemInInventory = item;
            }

            // 判断棋盘上没有棋子 && 背包里有棋子
            if (TrainMissionUtility.HasActiveItemInMainBoardAndInventory(_itemInfo.itemID) &&
                !TrainMissionUtility.HasActiveItemInMainBoard(_itemInfo.itemID))
            {
                var world = Activity.World;
                world.inventory.WalkAllItem(WalkItem);
                if (itemInInventory == null) return;

                var temp = PoolMapping.PoolMappingAccess.Take<List<Item>>(out var confirmList);
                confirmList.Add(itemInInventory);

                // 使用正式的弹窗 | UI关闭时释放temp
                UIManager.Instance.OpenWindow(UIConfig.UITrainMissionCompleteOrderBag, confirmList, new Action(() =>
                {
                    temp.Free();
                    ResolveCommit();
                    
                    // 通知其他车厢检查状态
                    MessageCenter.Get<MSG.UI_BOARD_DIRTY>().Dispatch();
                }));
            }
            else
            {
                ResolveCommit();
            }
        }

        private void ResolveCommit()
        {
            Train.consumeIdx = Index;
            _reward = Activity.CompleteTrainMission(Train.TrainOrder, Index);
        }

        // 完成车厢任后务表现
        private void _OnMessageCommitItem(int tid, Vector3 worldPos)
        {
            if (tid != _itemInfo.itemID || Train.consumeIdx != Index)
            {
                return;
            }

            Train.consumeIdx = -1;
            var allFinish = Train.TrainOrder.CheckAllFinish();

            // 飞棋子结束
            UIFlyUtility.FlyCompleteOrder(tid, 1, worldPos, 0f,
                () => transform.position, () =>
                {
                    coverMask.SetActive(true);
                    check.SetActive(true);
                    bgAnimator.SetTrigger("Close"); // 关闭车厢
                    goAnimator.SetTrigger("Hide"); // 关闭按钮
                    
                    Game.Manager.audioMan.TriggerSound("TrainItemSubmit"); // 火车-提交车厢棋子

                    // 飞完成当前任务的奖励
                    if (_reward != null)
                    {
                        for (var i = 0; i < _reward.Count; i++)
                        {
                            var reward = _reward[i];
                            if (i == _reward.Count - 1 && _reward.Count > 1)
                            {
                                // 限时奖励
                                UIFlyUtility.FlyReward(reward, timeLimit.transform.position);
                            }
                            else
                            {
                                UIFlyUtility.FlyReward(reward, transform.position);
                            }
                        }

                        _reward = null;
                    }

                    // 限时任务消失
                    if (IsTimeLimitValid(out _, out _, true))
                    {
                        animator.SetTrigger("CountHide");
                    }

                    // 判断是否整个火车任务完成
                    if (allFinish)
                    {
                        MessageCenter.Get<UI_TRAIN_MISSION_COMPLETE_TRAIN_MISSION>().Dispatch(Train.trainType, Index);
                    }
                    else
                    {
                        // 火车自动定位逻辑
                        Train.AutoScroll();
                    }
                });
        }

        private void RefreshCd()
        {
            if (!IsTimeLimitValid(out var end, out _))
            {
                timeLimit.SetActive(false);
                countDown.SetActive(false);
                return;
            }

            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, end - t);

            timeLimitCd.SetCountDown(diff, UIUtility.CdStyle.Colon);
        }

        // 棋盘发生变化时
        private void _OnMessageBoardDirty()
        {
            // 检查车厢状态
            ChangeCarriageView();
        }
        #endregion
    }
}