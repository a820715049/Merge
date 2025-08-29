using System.Collections.Generic;
using System.Linq;
using Config;
using EL;
using UnityEngine;

namespace FAT
{
    public class UISpinTip : UITipsBase
    {
        public Transform view;
        public Transform hide;
        public GameObject cell;
        private PackSpin _pack;
        private List<SpinCellData> _dataList = new();
        private List<MBSpinScrollCell> _cellList = new();
        protected override void OnParse(params object[] items)
        {
            _SetCurTipsWidth(930);
            _SetTipsPosInfo(items);
            _pack = items[2] as PackSpin;
        }

        protected override void OnPreOpen()
        {
            _CheckDataList();
            _CheckCellList();
            var index = 0;
            _pack.RefreshTotalWeight();
            for (var i = 0; i < _dataList.Count; i++)
            {
                _dataList[i].RefreshData(_pack.RewardIDList[i], _pack.GetRewardRate(_pack.RewardIDList[i]), _pack.CheckHasBought(_pack.RewardIDList[i]) ? 0 : 1);
            }
            var sortList = _dataList.OrderByDescending(e => e.rate).ThenByDescending(e => _pack.RewardIDList.IndexOf(e.id)).ToArray();
            for (var i = 0; i < sortList.Count(); i++)
            {
                _cellList[i].Refresh(sortList[i]);
            }
            _RefreshTipsPos(20f, false);
        }

        private void _CheckDataList()
        {
            if (_dataList.Count == _pack.ShowIndexList.Count) { return; }
            if (_dataList.Count < _pack.ShowIndexList.Count)
            {
                while (_dataList.Count != _pack.ShowIndexList.Count) { _dataList.Add(new SpinCellData()); }
            }
            else
            {
                while (_dataList.Count != _pack.ShowIndexList.Count) { _dataList.RemoveAt(0); }
            }
        }

        private void _CheckCellList()
        {
            if (_cellList.Count == _pack.ShowIndexList.Count) { return; }
            if (_cellList.Count < _pack.ShowIndexList.Count)
            {
                while (_cellList.Count != _pack.ShowIndexList.Count) { _cellList.Add(Instantiate(cell, view).GetComponent<MBSpinScrollCell>()); }
            }
            else
            {
                while (_cellList.Count != _pack.ShowIndexList.Count)
                {
                    _cellList[0].transform.SetParent(hide);
                    _cellList.RemoveAt(0);
                }
            }
        }
    }
    public class SpinCellData
    {
        public int id;
        public float rate;
        public int complete;
        public RewardConfig reward1;
        public RewardConfig reward2;
        public void RefreshData(int _id, float _rate, int _complete)
        {
            id = _id;
            rate = _rate;
            complete = _complete;
            var conf = fat.conf.Data.GetSpinPackRewardPool(_id);
            reward1 = null;
            reward2 = null;
            if (conf.RewardList.TryGetByIndex(0, out var ret1)) { reward1 = ret1.ConvertToRewardConfig(); }
            if (conf.RewardList.TryGetByIndex(1, out var ret2)) { reward2 = ret2.ConvertToRewardConfig(); }
        }
    }
}