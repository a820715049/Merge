// ==================================================
// File: MBFarmBoardFarm.cs
// Author: liyueran
// Date: 2025-05-09 17:05:51
// Desc: 农场主棋盘农田
// ==================================================

using EL;
using FAT.MSG;
using fat.rawdata;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class MBFarmBoardFarm_Goods : MBFarmBoardFarm
    {
        [SerializeField] private SkeletonGraphic[] fieldSpineAry;
        [SerializeField] private SkeletonGraphic scanSpine;

        public SkeletonGraphic[] FieldSpineAry => fieldSpineAry;
        public override void SetUp()
        {
            base.SetUp();
        }

        public override void InitOnPreOpen(FarmBoardActivity act)
        {
            base.InitOnPreOpen(act);

            foreach (var fieldSpine in fieldSpineAry)
            {
                fieldSpine.AnimationState.SetAnimation(0, "idle", true);
            }

            // 扫描仪默认状态
            scanSpine.AnimationState.SetAnimation(0, "idle", false);

            // 读数据 根据里程碑判断 显隐已经解锁了几个农田
            FarmInit(_activity.UnlockFarmlandNum);
        }

        private void FarmInit(int count)
        {
            for (int i = 0; i < fieldSpineAry.Length; i++)
            {
                bool shouldShow = i < count;
                fieldSpineAry[i].gameObject.SetActive(shouldShow);
            }
        }

        public override void OnSeedClose()
        {
            base.OnSeedClose();

           

            // 判断是否可以滚动
            if (_activity.IsReadyToMove)
            {
                UIManager.Instance.Block(true);
            }
            
            var ui = (UIFarmBoardMain)UIManager.Instance.TryGetUI(_activity.VisualBoard.res.ActiveR);
            if (ui != null)
            {
                // 判断是否可以滚动
                if (_activity.IsReadyToMove)
                {
                    ui.StartMove();
                }
                else
                {
                    // 刷新锁的位置
                    ui.SetCloudLockPos();
                }
            }
        }

        // 点击农田生成棋子
        public override void OnClickFarm()
        {
            base.OnClickFarm();

            var ui = (UIFarmBoardMain)UIManager.Instance.TryGetUI(_activity.VisualBoard.res.ActiveR);
            if (ui != null)
            {
                ui.AutoGuideController.Interrupt();
            }

            if (_activity.TokenNum <= 0)
            {
                var giftPack = Game.Manager.activity.LookupAny(EventType.FarmEndlessPack) as PackEndlessFarm;
                if (giftPack == null)
                {
                    // 点击显示气泡
                    _activity.VisualTokenTip.res.ActiveR.Open(ui.mbtoken.transform.Find("TokenAdd").position, 0f,
                        _activity);
                }
                else
                {
                    // 弹出礼包
                    UIManager.Instance.OpenWindow(giftPack.Res.ActiveR, giftPack);
                    // 关闭tips
                    MessageCenter.Get<MSG.UI_CLOSE_LAYER>().Dispatch(UIConfig.UIFarmBoardAnimalTips.layer);
                    return;
                }
            }

            // 调用数据层接口
            var result = _activity.CollectFarmland(transform.position);
            if (result == 1)
            {
                //棋盘空间不足
                Game.Manager.commonTipsMan.ShowPopTips(Toast.BoardFullUi);
                Game.Manager.audioMan.TriggerSound("BoardFull");
            }
            else if (result == 2)
            {
                //奖励箱顶部棋子发往棋盘
            }
            else if (result == 3)
            {
                // 代币不足
            }
            else
            {
                // 播放音效 货架获取棋子
                Game.Manager.audioMan.TriggerSound("FarmboardGetTokenBF");

                // 扫描机特效动画
                animator.SetTrigger(Punch);
                // 扫描机动画
                scanSpine.AnimationState.SetAnimation(0, "punch", false);
                // 货物动画
                foreach (var fieldSpin in fieldSpineAry)
                {
                    fieldSpin.AnimationState.SetAnimation(0, "punch", false);
                }
            }
        }

        // 根据索引获得指定农田位置
        public override Transform GetFarmByIndex(int index)
        {
            var parent = fieldSpineAry[index].transform.parent.parent;
            var count = parent.childCount;
            return parent.GetChild(count - 1);
        }
    }
}