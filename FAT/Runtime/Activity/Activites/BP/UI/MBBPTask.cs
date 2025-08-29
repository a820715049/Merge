// ==================================================
// // File: MBBPTask.cs
// // Author: liyueran
// // Date: 2025-06-18 16:06:56
// // Desc: $
// // ==================================================

using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class MBBPTask : MonoBehaviour
    {
        [SerializeField] private MBBPTaskItem taskItem;
        [SerializeField] private RectTransform content;

        private Dictionary<int, MBBPTaskItem> _taskItemMap = new(); // key: id
        private BPActivity _activity;

        private string taskItemKey = "bp_task_item";

        // PreOpen
        public void Initialize(BPActivity activity)
        {
            this._activity = activity;
            PreparePool();

            var taskDataList = _activity.GetBPTaskDataList();
            // 根据传入的逻辑层的数据 初始化生成 taskItem
            foreach (var taskData in taskDataList)
            {
                var id = taskData.Id;
                var data = taskData;
                GameObjectPoolManager.Instance.CreateObject(taskItemKey, content, obj =>
                {
                    obj.SetActive(true);
                    var item = obj.GetComponent<MBBPTaskItem>();
                    item.Init(_activity, data);
                    item.RefreshView();

                    _taskItemMap.Add(id, item); // TODO
                });
            }
        }

        private void PreparePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(taskItemKey))
            {
                return;
            }

            GameObjectPoolManager.Instance.PreparePool(taskItemKey, taskItem.gameObject);
        }

        public void Sort(int taskId)
        {
            var taskDataList = _activity.GetBPTaskDataList();
            // 重新获取逻辑层的顺序
            for (var i = 0; i < taskDataList.Count; i++) // TODO
            {
                var id = taskDataList[i].Id;
                // 根据数据 找到对应的 item
                if (TryGetTaskItem(id, out var item))
                {
                    // 根据逻辑层的顺序 判断索引与顺序是否一致 
                    if (item.transform.GetSiblingIndex() != i)
                    {
                        // 如果不一致更新在父节点下的顺序
                        item.transform.SetSiblingIndex(i);
                    }
                }
            }
        }

        // 根据id 获取item
        public bool TryGetTaskItem(int id, out MBBPTaskItem item)
        {
            return _taskItemMap.TryGetValue(id, out item);
        }

        // PostClose
        public void Release()
        {
            foreach (var item in _taskItemMap.Values)
            {
                GameObjectPoolManager.Instance.ReleaseObject(taskItemKey, item.gameObject);
            }

            _taskItemMap.Clear();
        }
    }
}