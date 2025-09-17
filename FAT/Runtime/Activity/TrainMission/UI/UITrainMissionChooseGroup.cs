// ================================================
// File: UITrainOrderMain.cs
// Author: yueran.li
// Date: 2025/07/28 17:57:11 星期一
// Desc: 火车任务选组界面
// ================================================

using System.Collections.Generic;
using EL;
using FAT.Merge;
using FAT.MSG;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UITrainMissionChooseGroup : UIBase
    {
        public GameObject spawnerItem;
        private RectTransform _spawnerParent;
        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _desc;
        private UIImageState _btnState;
        private UITextState _btnTextState;

        // 活动实例 
        private TrainMissionActivity _activity;

        private Dictionary<int, MBTrainMissionChooseGroupSpawner> _spawners = new();
        private string PoolKeyGroupItem => $"train_mission_train_choose_group_item";

        private int _selectedGroup = -1;
        public int SelectedGroup => _selectedGroup;

        #region UI基础
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("Content/frame/Root/SpawnerRoot", out _spawnerParent);
            transform.Access("Content/frame/Top/cd/_cd", out _cd);
            transform.Access("Content/frame/Top/Title", out _title);
            transform.Access("Content/descBg/desc", out _desc);
            transform.Access("Content/frame/Root/BtnGo", out _btnState);
            transform.Access("Content/frame/Root/BtnGo/Text", out _btnTextState);
        }

        private void AddButton()
        {
            transform.AddButton("Content/frame/Top/BtnClose", Close).WithClickScale().FixPivot();
            transform.AddButton("Content/frame/Top/BtnInfo", OnClickInfo).WithClickScale().FixPivot();
            transform.AddButton("Content/frame/Root/BtnGo", OnClickGo).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (TrainMissionActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshCd();
            RefreshTheme();
            EnsurePool();
            SetBtnState(_selectedGroup == -1 ? 0 : 1);

            // 获取可选择的生成器
            Dictionary<int, List<int>> pairs = new();
            _activity.GetGroupInfo(pairs);

            foreach (var pair in pairs)
            {
                var groupId = pair.Key;
                var spawnerIds = pair.Value;

                var obj = GameObjectPoolManager.Instance.CreateObject(PoolKeyGroupItem, _spawnerParent);
                obj.SetActive(true);
                var spawner = obj.GetComponent<MBTrainMissionChooseGroupSpawner>();
                spawner.Init(_activity, this, groupId, spawnerIds);
                _spawners.Add(groupId, spawner);
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
        }

        protected override void OnPostOpen()
        {
        }


        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
            foreach (var spawner in _spawners.Values)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolKeyGroupItem, spawner.gameObject);
            }

            _spawners.Clear();
        }
        #endregion


        #region 事件
        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is TrainMissionActivity)
            {
                _selectedGroup = -1;
                Close();
            }
        }

        private void RefreshCd()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            _cd.SetCountDown(diff);
        }

        // 选组
        public void OnClickSpawner(int index)
        {
            _selectedGroup = index;
            SetBtnState(1);

            foreach (var spawner in _spawners.Values)
            {
                if (spawner.GrouId != index)
                {
                    spawner.OnCancel();
                }
            }
        }

        private void OnClickInfo()
        {
            UIManager.Instance.OpenWindow(_activity.VisualHelp.res.ActiveR, _activity);
        }

        private void OnClickGo()
        {
            // 判断是否已经选组
            if (_selectedGroup == -1)
            {
                // 请选择一组生成器
                Game.Manager.commonTipsMan.ShowPopTips(Toast.ItemBingoChoose);
                return;
            }

            _activity.ChooseGroup(_selectedGroup);
            _activity.Open();
            Close();
        }
        #endregion

        private void SetBtnState(int state)
        {
            _btnState.Select(state);
            _btnTextState.Select(state);
        }

        private void EnsurePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(PoolKeyGroupItem))
                return;
            GameObjectPoolManager.Instance.PreparePool(PoolKeyGroupItem, spawnerItem);
        }

        private void RefreshTheme()
        {
            _activity.VisualChooseGroup.visual.Refresh(_title, "mainTitle");
        }
    }
}