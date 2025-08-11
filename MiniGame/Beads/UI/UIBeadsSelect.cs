/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏-选关界面 
 * @Date: 2024-10-05 12:06:52
 */

using System.Collections.Generic;
using UnityEngine;
using FAT;
using EL;
using fat.rawdata;
using UnityEngine.UI.Extensions;
using TMPro;

namespace MiniGame
{
    public class UIBeadsSelect : UIBase
    {
        [SerializeField] private TMP_Text gameName;
        [SerializeField] private UIBeadsSelectScrollRect selectScrollRect;
        private List<(int, int)> _indexList = new List<(int, int)>();   //小游戏关卡序号+关卡idlist

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/Root/BtnClose", base.Close).FixPivot();
            //临时在这里初始化小游戏manager 后面有更合适的位置时再挪
            MiniGameManager.Instance.InitManager();
        }

        protected override void OnParse(params object[] items) { }

        protected override void OnPreOpen()
        {
            _Refresh();
        }

        private void _Refresh(bool needJump = true, int targetIndex = -1)
        {
            var beadsConf = MiniGameManager.Instance.GetMiniGameConfByType(MiniGameType.MiniGameBeads);
            if (beadsConf == null) return;
            _indexList.Clear();
            gameName.text = I18N.Text(beadsConf.Name);
            //检查记录首个带红点的关卡id和最后一个已通关的关卡id
            var firstRPIndex = -1;
            var lastCompleteIndex = -1;
            for (int curIndex = 0; curIndex < beadsConf.LevelId.Count; curIndex++)
            {
                //当前关卡是否有红点
                var hasRP = MiniGameManager.Instance.CheckLevelHasRP(MiniGameType.MiniGameBeads, curIndex);
                if (hasRP && firstRPIndex < 0)
                {
                    firstRPIndex = curIndex;
                }
                //当前关卡是否通关
                var isComplete = MiniGameManager.Instance.IsLevelComplete(MiniGameType.MiniGameBeads, curIndex);
                if (isComplete)
                {
                    if (lastCompleteIndex < 0 || lastCompleteIndex < curIndex)
                        lastCompleteIndex = curIndex;
                }
                var levelId = beadsConf.LevelId[curIndex];
                _indexList.Add((curIndex, levelId));
            }
            //使用indexList刷新scroll
            selectScrollRect.UpdateData(_indexList);
            
            //如果不需要关卡定位 则return
            if (!needJump) return;
            //关卡列表定位逻辑
            //a.如果有关卡带红点：打开列表定位到首个带红点的关卡
            //b.如果没有红点，定位到该列表当前已通关的最后一关
            //c.如果有指定关卡序号 则直接跳转
            if (targetIndex >= 0)
            {
                selectScrollRect.JumpTo(targetIndex, Alignment.Upper);
                return;
            }
            if (firstRPIndex > 0)
            {
                selectScrollRect.JumpTo(firstRPIndex, Alignment.Upper);
            }
            else if (lastCompleteIndex > 0)
            {
                selectScrollRect.JumpTo(lastCompleteIndex, Alignment.Upper);
            }
            else
            {
                selectScrollRect.JumpTo(0, Alignment.Upper);
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.MINIGAME_RESULT>().AddListener(_OnGameFinish);
            MessageCenter.Get<MSG.MINIGAME_QUIT>().AddListener(_OnGameQuit);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.MINIGAME_RESULT>().RemoveListener(_OnGameFinish);
            MessageCenter.Get<MSG.MINIGAME_QUIT>().RemoveListener(_OnGameQuit);
        }

        protected override void OnPostClose()
        {
            _indexList.Clear();
            MiniGameManager.Instance.ClearLevelRP(MiniGameType.MiniGameBeads);
        }

        private void _OnGameFinish(int levelIndex, bool result)
        {
            _Refresh(result, levelIndex);
        }

        private void _OnGameQuit(int levelIndex)
        {
            _Refresh(false);
        }
    }
}
