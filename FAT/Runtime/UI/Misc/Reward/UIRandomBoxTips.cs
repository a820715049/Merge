/*
 * @Author: tang.yan
 * @Description: 随机宝箱Tips界面
 * @doc 随机宝箱案子：https://centurygames.yuque.com/ywqzgn/ne0fhm/ev8gk5lizqglu3fc
 * @doc 随机宝箱优化改造案子：https://centurygames.yuque.com/ywqzgn/ne0fhm/epyoema5l99gfgv1
 * @Date: 2023-11-30 16:11:26
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Config;
using TMPro;
using EL;

namespace FAT
{
    public class UIRandomBoxTips : UITipsBase
    {
        
        [SerializeField] private HorizontalLayoutGroup layoutGroup;
        [SerializeField] private GameObject jokerGo;
        [SerializeField] private UIImageRes jokerBg;
        [SerializeField] private TMP_Text jokerText;
        [SerializeField] private TMP_Text jokerTitle;
        [SerializeField] private TMP_Text jokerBgTitle;
        [SerializeField] private List<UICommonItem> _cellList;
        private List<(int, string)> _rewardList = new();
        private int _curBoxId; //目前正在查看的宝箱id
        private float _baseOffset = 18f;    //竖直方向固定偏移(下方箭头宽度的一半)
        private float _cellWidth = 152f;   //面板中cell的宽度
        private float _cellHeight = 152f;   //面板中cell的宽度

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
                _curBoxId = (int)items[2];
            }
        }

        protected override void OnPreOpen()
        {
            _RefreshShowReward();
            //刷新tips位置
            _RefreshTipsPos(_baseOffset);
        }

        protected override void OnPostClose()
        {
            
        }

        private void _RefreshShowReward()
        {
            var boxConfig = Game.Manager.objectMan.GetRandomBoxConfig(_curBoxId);
            if (boxConfig == null)
                return;
            _rewardList.Clear();
            if (boxConfig.IsFixedFirst && boxConfig.FixedReward.Count > 0)
            {
                foreach (var reward in boxConfig.FixedReward)
                {
                    _rewardList.Add((reward.ConvertToRewardConfig().Id, reward.ConvertToRewardConfig().Count.ToString()));
                }
            }
            
            foreach (var info in boxConfig.Info)
            {
                var showReward = info.ConvertToRandomBoxShowReward();
                if (showReward != null)
                {
                    _rewardList.Add((showReward.Id, showReward.MinCount + "-" + showReward.MaxCount));
                }
            }
            
            if (!boxConfig.IsFixedFirst && boxConfig.FixedReward.Count > 0)
            {
                foreach (var reward in boxConfig.FixedReward)
                {
                    _rewardList.Add((reward.ConvertToRewardConfig().Id, reward.ConvertToRewardConfig().Count.ToString()));
                }
            }
            jokerGo.gameObject.SetActive(boxConfig.JokerReward > 0);
            var objConfig = Game.Manager.objectMan.GetCardJokerConfig(boxConfig.JokerReward);
            if (objConfig != null)
            {
                jokerBg.SetImage(objConfig.ChestTipsImage);
                var c = FontMaterialRes.Instance.GetFontMatResConf(objConfig.ChestTipsStyle);
                c?.ApplyFontMatResConfig(jokerText);
                jokerText.text = I18N.FormatText("#SysComDesc377", boxConfig.JokerProbShow);
                jokerTitle.text = I18N.Text(Game.Manager.objectMan.GetBasicConfig(boxConfig.JokerReward).Name);
                jokerBgTitle.text = I18N.Text(Game.Manager.objectMan.GetBasicConfig(boxConfig.JokerReward).Name);
            }

            int index = 0;
            int length = _rewardList.Count;
            int showCellNum = 0;  //记录最终显示的cell个数
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
            var finalWidth = padding.left + padding.right + showCellNum * _cellWidth + layoutGroup.spacing * (showCellNum - 1);
            _SetCurTipsWidth(finalWidth);
            
            var height = padding.bottom + padding.top + _cellHeight;
            if (boxConfig.JokerReward > 0)
            {
                var rt = jokerGo.transform as RectTransform;
                height += rt.rect.height;
            }
            _SetCurTipsHeight(height);
        }
        
        private void _OnTipsBtnClick(int groupIndex)
        {
            
        }
    }
}