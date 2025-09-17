// ================================================
// File: UITrainOrderMain.cs
// Author: yueran.li
// Date: 2025/07/28 17:57:11 星期一
// Desc: 火车任务里程碑发奖界面
// ================================================

using EL;
using FAT.MSG;
using TMPro;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UITrainMissionReward : UIBase
    {
        private UICommonItem _rewardItem;
        private NonDrawingGraphic _block;
        private TextProOnACircle _title;

        // 活动实例 
        private TrainMissionActivity _activity;
        private UITrainMissionMain _main;
        private RewardCommitData _reward;

        #region UI基础
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("block", out _block);
            transform.Access("Content/RewardItem", out _rewardItem);
            transform.Access("Content/Title", out _title);
        }

        private void AddButton()
        {
            transform.AddButton("Mask", OnClickClaim).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (TrainMissionActivity)items[0];
            _main = (UITrainMissionMain)items[1];
            _reward = (RewardCommitData)items[2];
        }

        protected override void OnPreOpen()
        {
            // 设置标题
            _title.SetText(I18N.Text("#SysComDesc543"));

            UIManager.Instance.OpenWindow(UIConfig.UICommonShowRes);

            // 显示奖励
            _rewardItem.Refresh(_reward.rewardId, _reward.rewardCount);


            Game.Manager.audioMan.TriggerSound("TrainCongrats"); // 火车-发奖界面弹出
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        protected override void OnPostOpen()
        {
        }


        protected override void OnPreClose()
        {
            if (IsBlock)
            {
                SetBlock(false);
            }
        }

        protected override void OnPostClose()
        {
            UIManager.Instance.CloseWindow(UIConfig.UICommonShowRes);
        }
        #endregion

        public bool IsBlock => _block.raycastTarget;

        public void SetBlock(bool value)
        {
            _block.raycastTarget = value;
        }


        #region 事件
        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is TrainMissionActivity)
            {
                Close();
            }
        }

        private void OnClickClaim()
        {
            if (_reward != null)
            {
                // todo 判断是否显示资源栏
                if (CheckShowRes(_reward.rewardId))
                {
                    (UIManager.Instance.TryGetUI(UIConfig.UICommonShowRes) as UICommonShowRes)?.ShowOtherRes();
                }

                // 飞奖励
                var from = _rewardItem.transform.position;
                UIFlyUtility.FlyReward(_reward, from);
            }

            SetBlock(true);

            // 判断是否有下一轮
            if (_activity.waitEnterNextChallenge && !_activity.waitRecycle)
            {
                UIManager.Instance.OpenWindow(_activity.VisualComplete.res.ActiveR, _activity, _main);
            }
            else if (_activity.waitRecycle)
            {
                _main.StartRecycle();
            }

            Close();
        }
        #endregion

        private bool CheckShowRes(int id)
        {
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.Coin))
                return true;
            if (id == Constant.kMergeEnergyObjId)
                return true;
            return false;
        }
    }
}