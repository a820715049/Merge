// ==================================================
// File: MBFarmBoardAnimal.cs
// Author: liyueran
// Date: 2025-05-09 17:05:22
// Desc: 农场主棋盘动物
// ==================================================

using System;
using FAT.Merge;
using fat.rawdata;
using Spine.Unity;
using UnityEngine;

namespace FAT
{
    public class MBFarmBoardAnimal_Casher : MBFarmBoardAnimal
    {
        [SerializeField] private SkeletonGraphic animalTableSpine;
        [SerializeField] private SkeletonGraphic animalComputer01Spine;
        [SerializeField] private SkeletonGraphic animalComputer02Spine;
        [SerializeField] private SkeletonGraphic animalBoxSpine;

        // 电脑屏幕状态 用于控制tip
        private bool _computerState = false;
        public bool ComputerState => _computerState;

        public override void SetUp()
        {
            base.SetUp();
        }

        public override void InitOnPreOpen(FarmBoardActivity act)
        {
            base.InitOnPreOpen(act);
        }

        protected override void InitAnimalState()
        {
            // 判断奖励是否发完
            if (_activity.CheckIsInOutput())
            {
                CurAnimalState = AnimalState.Reward;

                // 物品框跳跃
                animalBoxSpine.AnimationState.AddAnimation(0, "idle", true, 0);

                // icon循环
                animalComputer01Spine.AnimationState.SetAnimation(0, "idle2", true);

                // 出小票
                animalTableSpine.AnimationState.SetAnimation(0, "punch", true);
            }
            else
            {
                CurAnimalState = AnimalState.Idle;
                animalTableSpine.AnimationState.SetAnimation(0, "idle", true);

                // 彩条循环
                animalComputer01Spine.AnimationState.SetAnimation(0, "idle1", true);
                animalBoxSpine.gameObject.SetActive(false);
            }

            // 小飞机动画
            animalComputer02Spine.AnimationState.SetAnimation(0, "idle", true);
        }


        public override void OnDragItemEndCustom(Item item)
        {
            base.OnDragItemEndCustom(item);

            // 关闭tips
            ComputerTransit(false, true, null);

            CurAnimalState = AnimalState.Cd;

            // 播放音效 cd
            Game.Manager.audioMan.TriggerSound("FarmboardCdBF");

            animalBoxSpine.gameObject.SetActive(true);
            // 物品框出现 
            animalBoxSpine.AnimationState.SetAnimation(0, "show", false).Complete += (_) =>
            {
                CurAnimalState = AnimalState.Reward;
            };
            // 物品框跳跃
            animalBoxSpine.AnimationState.AddAnimation(0, "idle", true, 0);

            // 出小票
            animalTableSpine.AnimationState.SetAnimation(0, "punch", true);
        }

        protected override void OnClickAnimal()
        {
            base.OnClickAnimal();

            // 判断当前的状态
            switch (CurAnimalState)
            {
                case AnimalState.Idle:
                    // Idle状态 判断是否存在目标棋子
                    // 点击显示气泡
                    ComputerTransit(true, false, () => { ComputerTransit(false, true); });
                    break;
                case AnimalState.Cd:
                    // cd状态
                    // 显示浮动文字
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.Charging, cdTrans.position);
                    break;
                case AnimalState.Reward:
                    // reward状态
                    // 点击发奖励
                    var ret = _activity.TryOutputItem(animalBtn.transform.position);
                    if (ret == 0 || !_activity.CheckIsInOutput())
                    {
                        // 发奖失败
                        CurAnimalState = AnimalState.Idle;
                        // 物品框消失
                        animalBoxSpine.AnimationState.SetAnimation(0, "hide", false);
                        // 桌子循环动画
                        animalTableSpine.AnimationState.AddAnimation(0, "idle", true, 0);
                        break;
                    }
                    else if (ret == 1)
                    {
                        // 播放音效 产出棋子
                        Game.Manager.audioMan.TriggerSound("FarmboardCdOutputBF");
                    }
                    else if (ret == 2)
                    {
                        // 棋盘已满
                        Game.Manager.commonTipsMan.ShowPopTips(Toast.BoardFull, animalBtn.transform.position);
                        Game.Manager.audioMan.TriggerSound("BoardFull");
                        break;
                    }

                    break;
            }
        }

        // 收银台动画
        public void ComputerTransit(bool show, bool idleLoop, Action onComplete = null)
        {
            if (show == _computerState)
            {
                return;
            }

            _computerState = show;

            if (show)
            {
                animalComputer01Spine.AnimationState.SetAnimation(0, "show", false); // 彩条-icon转换
                // icon循环
                animalComputer01Spine.AnimationState.AddAnimation(0, "idle2", idleLoop, 0).Complete += (_) =>
                {
                    if (!idleLoop)
                    {
                        onComplete?.Invoke();
                    }
                };
            }
            else
            {
                animalComputer01Spine.AnimationState.SetAnimation(0, "hide", false);
                animalComputer01Spine.AnimationState.AddAnimation(0, "idle1", idleLoop, 0); // 彩条循环
            }
        }
    }
}