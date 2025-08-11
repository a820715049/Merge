/**
 * @Author: chaoran.zhang
 * @Date: Time:2024.04.22 星期一 11:49:35
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/9 18:57:59
 * Description: 通用全部奖励界面
 */

using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;
using Conf = fat.conf;

namespace FAT
{
    public class TotalRewardCell
    {
        public GameObject GameObject;
        public RewardCommitData CommitData;
        private UIImageRes _icon;
        private TextMeshProUGUI _count;

        public void Init(GameObject obj)
        {
            GameObject = obj;
            _icon = obj.transform.GetChild(0).GetComponent<UIImageRes>();
            _count = obj.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        }

        public void SetData(RewardCommitData conf)
        {
            CommitData = conf;
        }

        public void SetDetail()
        {
            _count.text = UIUtility.SpecialCountText(CommitData.rewardId, CommitData.rewardCount, out var countStr) ? countStr : CommitData.rewardCount.ToString();
            var basic = Conf.Data.GetObjBasic(CommitData.rewardId);
            _icon.SetImage(basic.Icon);
        }

        public void SetParent(Transform parent)
        {
            GameObject.transform.SetParent(parent);
        }

        public void DeInit()
        {
            CommitData = null;
        }

        public Vector3 FlyPos()
        {
            return _icon.transform.position;
        }
    }

    public class UITotalRewardPanel : UIBase
    {
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private GameObject rewardCell;
        [SerializeField] private Transform cellPool;
        [SerializeField] private Transform content;
        [SerializeField] private Transform mask;

        private List<TotalRewardCell> _total = new List<TotalRewardCell>();
        private List<TotalRewardCell> _current = new List<TotalRewardCell>();
        private List<RewardCommitData> _commitData = new List<RewardCommitData>();
        private const int MaxCell = 21;

        protected override void OnCreate()
        {
            transform.AddButton("Content/btnClick", OnClickMask);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(TryClaimTempBagReward);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(TryClaimTempBagReward);
        }

        private void TryClaimTempBagReward()
        {
            if (_commitData.Count > 0)
            {
                content.gameObject.SetActive(true);
                mask.gameObject.SetActive(true);
                transform.GetComponent<Animator>().SetTrigger("Show");
                PrepareCell();
                UpdateAllCell();
            }
        }

        private void OnClickMask()
        {
            foreach (var cell in _current)
            {
                cell.SetParent(cellPool);
            }
            FlyReward();
            ResetPanel();
            if (_commitData.Count > 0)
            {
                content.gameObject.SetActive(false);
                mask.gameObject.SetActive(false);
            }
            else
            {
                Close();
            }
            Game.Manager.specialRewardMan.CheckSpecialRewardFinish();
        }

        protected override void OnParse(params object[] items)
        {
            _commitData = items[0] as List<RewardCommitData>;
            PrepareCell();
            UpdateAllCell();
        }

        private void PrepareCell()
        {
            for (var i = 0; i < _commitData.Count && i < MaxCell; i++)
            {
                var data = _commitData[i];
                if (i < _total.Count)
                {
                    InitCell(data, _total[i]);
                }
                else
                {
                    CreateCell(data);
                }
            }
            if (_commitData.Count <= MaxCell)
            {
                _commitData.Clear();
            }
            else
            {
                _commitData.RemoveRange(0, MaxCell);
            }
        }

        private void CreateCell(RewardCommitData reward)
        {
            var obj = Instantiate(rewardCell, rewardRoot);
            var cell = new TotalRewardCell();
            cell.Init(obj);
            cell.SetData(reward);
            _total.Add(cell);
            _current.Add(cell);
        }

        private void InitCell(RewardCommitData reward, TotalRewardCell cell)
        {
            cell.SetData(reward);
            cell.SetParent(rewardRoot);
            _current.Add(cell);
        }

        private void UpdateAllCell()
        {
            foreach (var cell in _current)
            {
                cell.SetDetail();
            }
        }

        private void FlyReward()
        {
            foreach (var data in _current)
            {
                UIFlyUtility.FlyReward(data.CommitData, data.FlyPos(), null, 190f);
            }
        }

        private void ResetPanel()
        {
            foreach (var cell in _total)
            {
                cell.SetParent(cellPool);
            }
            _current.Clear();
        }
    }
}