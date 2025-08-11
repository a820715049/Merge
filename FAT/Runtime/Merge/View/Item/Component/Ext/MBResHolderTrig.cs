/*
 * @Author: tang.yan
 * @Description: 触发式棋子特有 res holder 
 * @Date: 2025-03-24 17:03:38
 */

using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class MBResHolderTrig : MBResHolderBase
    {
        public float SpawnDelayTime = 0;    //产出棋子时 棋子延迟出现的时间
        public float ClickDelayTime = 0;    //点击阻挡时间  避免连点导致表现重叠
        public float DieDelayTime = 0;      //棋子死亡时 延迟缩小消失的时间 目的是为了和动画表现对上
        [SerializeField] private List<UIImageRes> coverIconList;
        [SerializeField] private List<GameObject> progressList;
        [SerializeField] private List<GameObject> fillList;
        private Item _curItem;
        private Coroutine _rewardCo;
        private Coroutine _soundCo;

        public override void OnInit(Item item)
        {
            _curItem = item;
            _InitShowInfo();
        }

        public override void OnClear()
        {
            _curItem = null;
            _ClearRewardCo();
            _ClearSoundCo();
        }
        
        public void OnTrigAutoSourceSucc()
        {
            _Refresh();
            _ClearSoundCo();
            _soundCo = StartCoroutine(_CoPlaySound());
        }

        private void _Refresh(bool isFirst = false)
        {
            if (_curItem == null)
                return;
            var com = _curItem.GetItemComponent<ItemTrigAutoSourceComponent>();
            if (com == null || com.Config == null) 
                return;
            var showIndex = com.HasTriggerCount() ? com.CurTriggerCount : com.TotalTriggerCount - 1;
            for (var i = 0; i < coverIconList.Count; i++)
            {
                coverIconList[i].gameObject.SetActive(i == showIndex);
                if (isFirst && com.Config.CoverPng.TryGetByIndex(i, out var asset))
                {
                    coverIconList[i].SetImage(asset);
                }
            }
            for (var i = 0; i < progressList.Count; i++)
            {
                progressList[i].gameObject.SetActive(i < com.TotalTriggerCount);
            }
            for (var i = 0; i < fillList.Count; i++)
            {
                fillList[i].gameObject.SetActive(i <= com.CurTriggerCount - 1);
            }
        }

        private void _InitShowInfo()
        {
            _Refresh(true);
        }

        public void DelayFlyRewardList(List<RewardCommitData> rewardList, Vector3 pos)
        {
            _ClearRewardCo();
            _rewardCo = StartCoroutine(_CoFlyRewardList(rewardList, pos));
        }

        private IEnumerator _CoFlyRewardList(List<RewardCommitData> rewardList, Vector3 pos)
        {
            yield return new WaitForSeconds(SpawnDelayTime);
            UIFlyUtility.FlyRewardList(rewardList, pos);
        }
        
        private void _ClearRewardCo()
        {
            if (_rewardCo != null)
            {
                StopCoroutine(_rewardCo);
                _rewardCo = null;
            }
        }
        
        private IEnumerator _CoPlaySound()
        {
            yield return new WaitForSeconds(SpawnDelayTime);
            Game.Manager.audioMan.TriggerSound("MineHitToken");
        }

        private void _ClearSoundCo()
        {
            if (_soundCo != null)
            {
                StopCoroutine(_soundCo);
                _soundCo = null;
            }
        }
    }
}