// ==================================================
// // File: MBTrainMissionPreviewSpawner.cs
// // Author: liyueran
// // Date: 2025-08-06 11:08:07
// // Desc: $火车任务 预览界面 生成器
// // ==================================================

using System.Collections.Generic;
using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBTrainMissionPreviewSpawner : MonoBehaviour
    {
        public TextMeshProUGUI roundText;
        public UITextState roundTextState;
        public Image line;
        public UICommonItem spawner1;
        public UICommonItem spawner2;
        public UIImageState imageState;

        private TrainMissionActivity _activity;

        private int _index;
        private List<int> _spawnerIds = new List<int>();

        public void Init(TrainMissionActivity activity, int index, List<int> spawnerIds)
        {
            _activity = activity;
            this._index = index;
            this._spawnerIds = spawnerIds;

            Refresh();
        }

        private void Refresh()
        {
            roundText.SetText(I18N.FormatText("#SysComDesc1544", _index + 1));

            // 判断是不是正在进行的轮次
            // 图片
            imageState.Select(_index == _activity.challengeIndex ? 1 : 0);
            // 图片大小
            GetComponent<RectTransform>().sizeDelta =
                _index == _activity.challengeIndex ? new Vector2(852, 198) : new Vector2(834, 180);
            // 文字字体
            roundTextState.Select(_index == _activity.challengeIndex ? 1 : 0);
            // 点点颜色
            ColorUtility.TryParseHtmlString("#017D43", out var inRound);
            ColorUtility.TryParseHtmlString("#CC997C", out var otherRound);
            line.color = _index == _activity.challengeIndex ? inRound : otherRound;

            // 根据id 找到玩家等级最高的生成器
            var id1 = _spawnerIds[0];
            var spawnerId1 = BoardActivityUtility.GetHighestLevelItemIdInCategory(id1, 1);
            spawner1.Refresh(spawnerId1, 1);
            spawner1.ExtendTipsForMergeItem(spawnerId1);

            var id2 = _spawnerIds[1];
            var spawnerId2 = BoardActivityUtility.GetHighestLevelItemIdInCategory(id2, 1);
            spawner2.Refresh(spawnerId2, 1);
            spawner2.ExtendTipsForMergeItem(spawnerId2);
        }
    }
}