// ==================================================
// File: MBFarmBoardAnimal.cs
// Author: liyueran
// Date: 2025-05-09 17:05:22
// Desc: 农场主棋盘动物
// ==================================================

using EL;
using FAT.Merge;
using fat.rawdata;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBFarmBoardAnimal_Cow : MBFarmBoardAnimal
    {
       
        [SerializeField] private SkeletonGraphic animalSpine;
        [SerializeField] private SkeletonGraphic animalBoxSpine;

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
                animalBoxSpine.AnimationState.AddAnimation(0, "milk_idle", true, 0);
                // 吃东西循环动画
                animalSpine.AnimationState.SetAnimation(0, "eat", true);
            }
            else
            {
                CurAnimalState = AnimalState.Idle;
                animalSpine.AnimationState.SetAnimation(0, "sleep", true);
                animalBoxSpine.gameObject.SetActive(false);
            }
        }


        public override void OnDragItemEndCustom(Item item)
        {
            base.OnDragItemEndCustom(item);

            // 关闭tips
            MessageCenter.Get<MSG.UI_CLOSE_LAYER>().Dispatch(UIConfig.UIFarmBoardAnimalTips.layer);

            CurAnimalState = AnimalState.Cd;

            // 播放音效 cd
            Game.Manager.audioMan.TriggerSound("FarmboardCowCd");

            animalBoxSpine.gameObject.SetActive(true);
            // 物品框出现 
            animalBoxSpine.AnimationState.SetAnimation(0, "milk_show", false).Complete += (_) =>
            {
                CurAnimalState = AnimalState.Reward;
            };
            // 物品框跳跃
            animalBoxSpine.AnimationState.AddAnimation(0, "milk_idle", true, 0);

            // 睡觉->吃东西的转换动画
            animalSpine.AnimationState.SetAnimation(0, "sleep_eat", false).Complete +=
                (_) =>
                {
                    // 播放动画
                    // 吃东西循环动画
                    animalSpine.AnimationState.SetAnimation(0, "eat", true);
                };
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
                    var id = _activity.GetConsumeItemId();
                    UIConfig.UIFarmBoardAnimalTips.Open(bubbleTrans.position, 0f, id, _activity);
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
                        animalBoxSpine.AnimationState.SetAnimation(0, "milk_hide", false);
                        // 吃东西->睡觉转换动画
                        animalSpine.AnimationState.SetAnimation(0, "eat_sleep", false);
                        // 睡觉循环动画
                        animalSpine.AnimationState.AddAnimation(0, "sleep", true, 0);
                        break;
                    }
                    else if (ret == 1)
                    {
                        // 播放音效 产出棋子
                        Game.Manager.audioMan.TriggerSound("FarmboardCowOutput");
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
    }
}