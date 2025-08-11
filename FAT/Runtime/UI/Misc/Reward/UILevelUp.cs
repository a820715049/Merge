/*
 * @Author: tang.yan
 * @Description: 角色升级界面 
 * @Date: 2023-11-20 16:11:58
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace FAT
{
    public class UILevelUp : UIBase
    {
        [Serializable]
        public class UILevelUpReward
        {
            [SerializeField] public GameObject rewardGo;
            [SerializeField] public UIImageRes rewardIcon;
            [SerializeField] public TMP_Text rewardNum;
        }
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button btnClaim;
        [SerializeField] private GameObject rewardGo;
        [SerializeField] private List<UILevelUpReward> rewardGroup;
        [SerializeField] private UILevelUpReward rewardProduce;
        [SerializeField] private Animator animator;

        private int _curProduceId;
        private bool _isPlayAnim;
        private Coroutine _coroutine = null;
        private List<RewardCommitData> _curRewardDataList = new List<RewardCommitData>();
        private Action _curCloseCb;

        protected override void OnCreate()
        {
            btnClaim.WithClickScale().FixPivot().onClick.AddListener(_OnBtnClaim);
        }

        protected override void OnParse(params object[] items)
        {
            _curRewardDataList.Clear();
            _curCloseCb = null;
            if (items.Length > 0)
            {
                if (items[0] is List<RewardCommitData> rewardList)
                {
                    _curRewardDataList.AddRange(rewardList);
                }
            }
            if (items.Length > 1)
            {
                _curCloseCb = items[1] as Action;
            }
        }

        protected override void OnPreOpen()
        {
            _CoPlayAnim("Show", 1.5f);
            _RefreshReward();
        }

        protected override void OnPostClose()
        {
            foreach (var reward in rewardGroup)
            {
                reward.rewardIcon.gameObject.SetActive(true);
            }
            _curCloseCb?.Invoke();
            _curCloseCb = null;
            Game.Manager.mapSceneMan.RefreshLocked();
        }

        private void _RefreshReward()
        {
            int curLevel = Game.Manager.mergeLevelMan.level;
            levelText.text = curLevel.ToString();
            _curProduceId = Game.Manager.bagMan.GetProduceIdByLevel(curLevel);
            rewardGo.SetActive(_curRewardDataList.Count > 0 || _curProduceId > 0);
            int index = 0;
            foreach (var uiReward in rewardGroup)
            {
                if (index < _curRewardDataList.Count)
                {
                    uiReward.rewardGo.SetActive(true);
                    uiReward.rewardNum.text = _curRewardDataList[index].rewardCount.ToString();
                    var image = Game.Manager.objectMan.GetBasicConfig(_curRewardDataList[index].rewardId)?.Icon.ConvertToAssetConfig();
                    if (image != null)
                    {
                        uiReward.rewardIcon.SetImage(image.Group, image.Asset);
                    }
                }
                else
                {
                    uiReward.rewardGo.SetActive(false);
                }
                index++;
            }
            if (_curProduceId > 0)
            {
                rewardProduce.rewardGo.SetActive(true);
                rewardProduce.rewardNum.text = "";
                var image = Game.Manager.objectMan.GetBasicConfig(_curProduceId)?.Icon.ConvertToAssetConfig();
                if (image != null)
                {
                    rewardProduce.rewardIcon.SetImage(image.Group, image.Asset);
                }
            }
            else
            {
                rewardProduce.rewardGo.SetActive(false);
            }
        }

        private void _OnBtnClaim()
        {
            if (_isPlayAnim)
                return;
            int index = 0;
            var from = Vector3.zero;
            foreach (var reward in _curRewardDataList)
            {
                if (index < rewardGroup.Count)
                {
                    from = rewardGroup[index].rewardIcon.transform.position - new Vector3(0,
                        (rewardGroup[index].rewardIcon.transform as RectTransform).sizeDelta.y / 2, 0);
                    UIFlyUtility.FlyReward(reward, from, null, 190);
                    rewardGroup[index].rewardIcon.gameObject.SetActive(false);
                }
                else
                {
                    UIFlyUtility.FlyReward(reward, btnClaim.transform.position);
                }
                index++;
            }
            if (_curProduceId > 0)
            {
                UIFlyFactory.GetFlyTarget(FlyType.Inventory, out var targetPos);
                from = rewardProduce.rewardIcon.transform.position - new Vector3(0, (rewardProduce.rewardIcon.transform as RectTransform).sizeDelta.y/2, 0);
                UIFlyUtility.FlyCustom(_curProduceId,1,from,targetPos,FlyStyle.Reward,UIFlyFactory.ResolveFlyType(_curProduceId), null, null,190f);
            }
            _CoPlayAnim("Hide", 0.5f, Close);
        }

        private void _CoPlayAnim(string trigger, float waitTime, Action cb = null)
        {
            if (_isPlayAnim)
                return;
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            _coroutine = StartCoroutine(_PlayAnim(trigger, waitTime, cb));
        }

        private IEnumerator _PlayAnim(string trigger, float waitTime, Action cb = null)
        {
            _isPlayAnim = true;
            animator.SetTrigger(trigger);
            yield return new WaitForSeconds(waitTime);
            _isPlayAnim = false;
            cb?.Invoke();
        }
    }
}