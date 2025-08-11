/*
 * @Author: pengjian.zhang
 * @Description: 订单礼盒Tips界面 
 * @Date: 2024-1-8 10:10:54
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Config;
using EL;

namespace FAT
{
    public class UIOrderBoxTips : UITipsBase
    {
        
        [SerializeField] private HorizontalLayoutGroup layoutGroup;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private RectTransform leftBoundary;
        [SerializeField] private RectTransform rightBoundary;
        [SerializeField] private RectTransform rightBoundaryWhenFull;   //订单礼盒面板区分两个宽度分别是奖励5个和其他；当奖励数量为5时 右边界节点
        [SerializeField] private RectTransform panel;
        [SerializeField] private List<UICommonItem> cellList = new List<UICommonItem>();
        private List<RewardConfig> _rewardConfigList = new List<RewardConfig>();
        private int _curOrderId; //目前正在查看的订单礼盒id
        private Vector3 _curWorldPos; //订单礼盒世界坐标
        private Vector3 _arrowResultPos = new Vector3(); //如果箭头超出左右边界后的最终箭头位置
        private int _layoutGroupSapcing = 60;  //默认1个或两个奖励时 layoutGroup的spacing
        private int _layoutGroupSapcing3 = 36;  //有3个奖励时 layoutGroup的spacing
        private int _layoutGroupSapcing4 = 12;  //有4个奖励时 layoutGroup的spacing
        private int _layoutGroupSapcing5 = 8;   //有5个奖励时 layoutGroup的spacing
        private int _orderBoxNum5Width = 862; //892的来源：美术规定奖励5个时固定宽度862 策划要求两边留有一定的安全区 不要贴边 +30偏移
        private int _orderBoxWidth = 712; //712的来源：美术规定奖励4个时固定宽度712 策划要求两边留有一定的安全区 不要贴边 +30偏移
        private int _tipsExtraWidth = 30; //策划要求两边留有一定的安全区 不要贴边 宽度上额外+30偏移
        private float _tipOffsetY = 70;   //订单礼盒奖励详情界面y轴偏移
        protected override void OnCreate()
        {
            foreach (var cell in cellList)
            {
                cell.Setup();
            }
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 2)
            {
                object[] tipsPosInfo = new object[2];
                tipsPosInfo[0] = items[0];
                tipsPosInfo[1] = _tipOffsetY;
                //设置tips位置参数
                _SetTipsPosInfo(tipsPosInfo);
                //设置界面自定义参数
                _curOrderId = (int)items[1];
                if (items[0] is Vector3 pos)
                {
                    _curWorldPos = new Vector3(pos.x, pos.y, pos.z);
                }
            }
        }

        protected override void OnPreOpen()
        {
            _RefreshShowReward();
            //刷新tips位置
            _RefreshTipsPos();
            //检测箭头是否越界
            _RefreshArrowPos();
        }

        private void _RefreshArrowPos()
        {
            if (arrow.position.x <= leftBoundary.position.x)
            {
                _arrowResultPos.Set(leftBoundary.position.x, arrow.position.y, arrow.position.z);
                arrow.position = _arrowResultPos;
                MessageCenter.Get<MSG.UI_ORDER_BOX_TIPS_POSITION_REFRESH>().Dispatch(_curWorldPos, arrow.position);
            }

            var resultRightBoundary = _rewardConfigList.Count > 4 ? rightBoundaryWhenFull : rightBoundary;
            if (arrow.position.x >= resultRightBoundary.position.x)
            {
                _arrowResultPos.Set(resultRightBoundary.position.x, arrow.position.y, arrow.position.z);
                arrow.position = _arrowResultPos;
                MessageCenter.Get<MSG.UI_ORDER_BOX_TIPS_POSITION_REFRESH>().Dispatch(_curWorldPos, arrow.position);
            }
        }
        
        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ORDER_ORDERBOX_END>().AddListener(_OnOrderBoxEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ORDER_ORDERBOX_END>().RemoveListener(_OnOrderBoxEnd);
        }

        protected override void OnPostClose()
        {
            
        }

        private void _OnOrderBoxEnd()
        {
            UIManager.Instance.CloseWindow(UIConfig.UIOrderBoxTips);
        }
        
        private void _RefreshShowReward()
        {
            BoardViewWrapper.TryGetOrderBoxDetail(_curOrderId, out _, out _, out var detail);
            if (detail == null)
                return;
            _rewardConfigList.Clear();
            int levelRate = 0;
            if (BoardViewWrapper.IsMainBoard())
            {
                levelRate = Game.Manager.mergeLevelMan.GetCurrentLevelRate();
            }
            var rewardMan = Game.Manager.rewardMan;
            foreach (var randId in detail.RandomReward)
            {
                var randReward = Game.Manager.configMan.GetRandomRewardConfigById(randId);
                foreach (var reward in randReward.Reward)
                {
                    //随机宝箱显示时支持按参数类型计算
                    var (_cfg_id, _cfg_count, _param) = reward.ConvertToInt3();
                    var (_id, _count) = rewardMan.CalcDynamicReward(_cfg_id, _cfg_count, levelRate, 0, _param);
                    var rewardData = new RewardConfig()
                    {
                        Id = _id,
                        Count = _count
                    };
                    _rewardConfigList.Add(rewardData);
                }
            }

            int index = 0;
            int length = _rewardConfigList.Count;
            //添加额外宽度 避免贴边
            _SetCurExtraWidth(_tipsExtraWidth);
            //效果图要求：不同个数预览时 奖励之间的间隙不同
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _orderBoxWidth);
            layoutGroup.spacing = _layoutGroupSapcing;
            _SetCurTipsWidth(_orderBoxWidth);

            if (length == 3)
            {
                layoutGroup.spacing = _layoutGroupSapcing3;
            }
            else if (length == 4)
            {
                layoutGroup.spacing = _layoutGroupSapcing4;
            }
            else if (length == 5)
            {
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _orderBoxNum5Width);
                layoutGroup.spacing = _layoutGroupSapcing5;
                _SetCurTipsWidth(_orderBoxNum5Width);
            }
            foreach (var cell in cellList)
            {
                if (index < length)
                {
                    var data = _rewardConfigList[index];
                    var cfg = Game.Manager.objectMan.GetBasicConfig(data.Id);
                    if (cfg != null)
                    {
                        cell.gameObject.SetActive(true);
                        cell.Refresh(data);
                    }
                    else
                    {
                        cell.gameObject.SetActive(false);
                    }
                }
                else
                {
                    cell.gameObject.SetActive(false);
                }
                index++;
            }
        }
    }
}