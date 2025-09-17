// ==================================================
// // File: MBTrainMissionSpawner.cs
// // Author: liyueran
// // Date: 2025-08-05 18:08:54
// // Desc: $火车任务 选组界面 spawner
// // ==================================================

using System;
using System.Collections.Generic;
using EL;
using UnityEngine;

namespace FAT
{
    public class MBTrainMissionChooseGroupSpawner : MonoBehaviour
    {
        public GameObject normal;
        public GameObject selected;
        public RectTransform root;

        private int _grouId;
        public int GrouId => _grouId;


        private TrainMissionActivity _activity;
        private UITrainMissionChooseGroup _ui;

        public void Init(TrainMissionActivity activity, UITrainMissionChooseGroup ui, int groupId, List<int> spawnerIds)
        {
            _activity = activity;
            _grouId = groupId;
            _ui = ui;

            // 根据数据显示对应的生成器
            var count = spawnerIds.Count;
            for (var i = 0; i < root.childCount; i++)
            {
                if (i >= count)
                {
                    root.GetChild(i).gameObject.SetActive(false);
                    continue;
                }

                // 根据id 找到玩家等级最高的生成器
                var id = spawnerIds[i];
                var spawnerId = BoardActivityUtility.GetHighestLevelItemIdInCategory(id, 1);

                var item = root.GetChild(i).GetComponent<UICommonItem>();
                item.Refresh(spawnerId, 1);
                item.ExtendTipsForMergeItem(spawnerId);
            }

            if (_grouId == _ui.SelectedGroup && _ui.SelectedGroup != -1)
            {
                SetSelectView();
            }
            else
            {
                SetCancelView();
            }
        }

        private void Start()
        {
            transform.AddButton("", OnSelect);
        }

        private void OnSelect()
        {
            _ui.OnClickSpawner(_grouId);
            SetSelectView();
        }

        public void OnCancel()
        {
            SetCancelView();
        }

        private void SetSelectView()
        {
            normal.SetActive(false);
            selected.SetActive(true);
        }

        private void SetCancelView()
        {
            normal.SetActive(true);
            selected.SetActive(false);
        }
    }
}