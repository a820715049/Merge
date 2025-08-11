/*
 *@Author:chaoran.zhang
 *@Desc:热气球活动奖励tips
 *@Created Time:2024.07.18 星期四 10:58:52
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UICommonRewardTips : UITipsBase
    {
        [SerializeField] private HorizontalLayoutGroup layoutGroup;
        [SerializeField] private GameObject jokerGo;
        [SerializeField] private UIImageRes jokerBg;
        [SerializeField] private TMP_Text jokerText;
        [SerializeField] private TMP_Text jokerTitle;
        [SerializeField] private TMP_Text jokerBgTitle;
        [SerializeField] private List<UICommonItem> _cellList;
        [SerializeField] private TMP_Text title;
        private List<(int, string)> _rewardList = new();
        private Google.Protobuf.Collections.RepeatedField<string> reward;
        private float _baseOffset = 18f; //竖直方向固定偏移(下方箭头宽度的一半)
        private float _cellWidth = 152f; //面板中cell的宽度
        private float _cellHeight = 152f; //面板中cell的宽度
        private bool isShowTitle;

        protected override void OnCreate()
        {
            foreach (var cell in _cellList)
            {
                cell.Setup();
            }
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 3)
            {
                //设置tips位置参数
                _SetTipsPosInfo(items);
                //设置界面自定义参数
                reward = (Google.Protobuf.Collections.RepeatedField<string>)items[2];
                if (items.Length == 4)
                {
                    isShowTitle = (bool)items[3];
                }
                else
                {
                    isShowTitle = false;
                }
            }
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            _RefreshShowReward();
            //刷新tips位置
            _RefreshTipsPos(_baseOffset);
            title.gameObject.SetActive(isShowTitle);
        }

        protected override void OnPostClose()
        {
            base.OnPreClose();
        }

        private void _RefreshShowReward()
        {
            _rewardList.Clear();
            foreach (var reward in reward)
            {
                var item = reward.ConvertToInt3();
                _rewardList.Add((item.Item1,
                    Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(item.Item2, item.Item3).ToString()));
            }

            jokerGo.gameObject.SetActive(false);

            int index = 0;
            int length = _rewardList.Count;
            int showCellNum = 0; //记录最终显示的cell个数
            foreach (var cell in _cellList)
            {
                if (index < length)
                {
                    var data = _rewardList[index];
                    var cfg = Game.Manager.objectMan.GetBasicConfig(data.Item1);
                    if (cfg != null)
                    {
                        cell.gameObject.SetActive(true);
                        cell.Refresh(data.Item1, data.Item2);
                        showCellNum++;
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

            //计算面板最终宽度
            var padding = layoutGroup.padding;
            var finalWidth = padding.left + padding.right + showCellNum * _cellWidth +
                             layoutGroup.spacing * (showCellNum - 1);
            _SetCurTipsWidth(finalWidth);

            var height = padding.bottom + padding.top + _cellHeight;

            _SetCurTipsHeight(height);
        }
    }
}