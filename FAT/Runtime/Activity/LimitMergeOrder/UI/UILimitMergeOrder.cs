using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UILimitMergeOrder : UIBase
    {
        [SerializeField] private Button mask;
        [SerializeField] private MBRewardIcon icon;
        [SerializeField] private UIImageRes itemImg;
        [SerializeField] private TMP_Text cdText;
        private int boxItemId;
        private int _rewardBoxId;
        private ActivityLimitMergeOrder _activity;

        protected override void OnCreate()
        {
            mask.onClick.AddListener(OnClose);
            transform.AddButton("Content/CloseBtn", OnClose);
            transform.AddButton("Content/Confirm", OnClose);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }
            _activity = (ActivityLimitMergeOrder)items[0];
        }

        protected override void OnPreOpen()
        {
            if (_activity == null) return;
            // 刷奖励宝箱（展示随机宝箱本体缩略图）
            _rewardBoxId = _activity.GetCurrentRoundRewardBoxId();
            var rewardCfg = _rewardBoxId > 0 ? Game.Manager.mergeItemMan.GetOrderRewardConfig(_rewardBoxId) : null;
            if (rewardCfg != null && rewardCfg.Reward != null && rewardCfg.Reward.Count > 0)
            {
                var r = rewardCfg.Reward[0];
                var (cfgId, cfgCount, param) = r.ConvertToInt3();
                // 直接为随机宝箱类型
                if (Game.Manager.objectMan.IsType(cfgId, ObjConfigType.RandomBox))
                {
                    boxItemId = cfgId;
                }
                else
                {
                    // 计算动态奖励再判断是否为随机宝箱
                    var levelRate = Game.Manager.mergeLevelMan.GetCurrentLevelRate();
                    var (dynId, _) = Game.Manager.rewardMan.CalcDynamicReward(cfgId, cfgCount, levelRate, 0, param);
                    if (Game.Manager.objectMan.IsType(dynId, ObjConfigType.RandomBox))
                    {
                        boxItemId = dynId;
                    }
                }
            }
            if (boxItemId > 0)
            {
                icon.Refresh(boxItemId, 1);
                // 点击宝箱图标：弹出随机宝箱 Tips
                icon.RefreshClick((id, custom) =>
                 {
                     Game.Manager.randomBoxMan.TryOpenRandomBoxTips(boxItemId, icon.transform.position, 120f);
                     return true;
                 });
            }
            else
            {
                icon.RefreshEmpty();
            }
            // 刷需求棋子图标
            var itemId = _activity.GetCurrentRoundMaxRequiredItemId();
            if (itemId > 0)
            {
                var cfg = Game.Manager.objectMan.GetBasicConfig(itemId);
                if (cfg != null)
                {
                    itemImg.SetImage(cfg.Icon.ConvertToAssetConfig());
                }
            }
            RefreshCD();
        }
        override protected void OnPostOpen()
        {
            base.OnPostOpen();
            if (_activity.needPopRewardTip)
            {
                _activity.needPopRewardTip = false;
                Game.Manager.randomBoxMan.TryOpenRandomBoxTips(boxItemId, icon.transform.position, 120f);
            }
        }
        private void RefreshCD()
        {
            var left = _activity?.Countdown ?? 0;
            if (left <= 0)
            {
                Close();
                return;
            }
            UIUtility.CountDownFormat(cdText, left);
        }
        private void OnClose()
        {
            Close();
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is WishBoardActivity)
            {
                Close();
            }
        }
    }
}
