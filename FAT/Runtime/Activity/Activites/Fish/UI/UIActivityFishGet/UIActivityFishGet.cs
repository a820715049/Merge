// ================================================
// File: UIActivityFishGet.cs
// Author: yueran.li
// Date: 2025/04/15 16:55:35 星期二
// Desc: 钓鱼棋盘获得鱼界面
// ================================================

using System.Collections;
using System.Collections.Generic;
using Coffee.UIExtensions;
using DG.Tweening;
using EL;
using FAT.MSG;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIActivityFishGet : UIBase, INavBack
    {
        public class FishGetItemData
        {
            public FishInfo fishInfo;
            public int weight;
            public bool maxWeight;
            public bool newFish;
        }

        // UI
        private UIActivityFishGetItem _uiActivityFishGetItem;
        private GameObject newFish;
        private GameObject newRecord;
        private UIImageRes fishImg;
        private UIImageRes repeatIcon;
        private TextMeshProUGUI repeatCount;
        private TextMeshProUGUI title;
        private CanvasGroup group;
        private NonDrawingGraphic mask;
        [SerializeField] private UIParticle fishEff;
        [SerializeField] private RectTransform fishRect;
        [SerializeField] private Animator _animator;
        [SerializeField] private Animator _bg_animator;

        // 活动实例
        private ActivityFishing activityFish;
        private ActivityFishing.FishCaughtInfo _caughtInfo;

        // 重复转换奖励
        private bool isRepeatFish;
        private List<RewardCommitData> convertReward = new();

        // 动画
        public readonly int OpenAnimTrigger = Animator.StringToHash("Show");
        public readonly int CloseAnimTrigger = Animator.StringToHash("Hide");
        public readonly int RepeatAnimTrigger = Animator.StringToHash("Repeat");
        public readonly int RepeatCloseAnimTrigger = Animator.StringToHash("RepeatHide");
        [SerializeField] private float fishFlyDuration = 1f; // 飞行时间
        [SerializeField] private AnimationCurve customFlyPositionCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(1, 1, 2, 0));

        #region UI
        protected override void OnCreate()
        {
            transform.Access("Content/Group/Title", out title);
            transform.Access("Content/Group/Fish", out _uiActivityFishGetItem);
            transform.Access("Content/FishImgRoot/FishImg", out fishImg);
            transform.Access("Content/Group/Repeat/Item/Icon", out repeatIcon);
            transform.Access("Content/Group/Repeat/Item/Count", out repeatCount);
            transform.Access("Content/Group", out group);
            transform.Access("Mask", out mask);

            transform.AddButton("Mask", OnClick);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;
            activityFish = (ActivityFishing)items[0];
            _caughtInfo = (ActivityFishing.FishCaughtInfo)items[1];
        }

        protected override void OnPreOpen()
        {
            // 计算是否获得全部星星
            var caught = activityFish.GetFishCaughtCount(_caughtInfo.fishId);
            convertReward.Clear();
            isRepeatFish = activityFish.FillConvertRewards(convertReward) > 0;

            var fishInfo = activityFish.FishInfoList.FindEx(f => f.Id == _caughtInfo.fishId);
            fishImg.SetImage(fishInfo.Icon);

            // 判断是否是重复奖励
            if (isRepeatFish)
            {
                var itemId = convertReward[0].rewardId;
                var count = convertReward[0].rewardCount;
                repeatCount.text = count.ToString();
                var conf = Game.Manager.objectMan.GetBasicConfig(itemId);
                if (conf == null) return;
                repeatIcon.SetImage(conf.Icon);
            }

            var data = new FishGetItemData()
            {
                fishInfo = fishInfo,
                weight = _caughtInfo.curWeight,
                maxWeight = _caughtInfo.maxWeight < _caughtInfo.curWeight && caught > 1,
                newFish = caught == 1,
            };

            _uiActivityFishGetItem?.InitOnPreOpen(data);

            RestoreOriginalState();
            RefreshTheme();

            StartCoroutine(CoOnPreOpen());
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnPostOpen()
        {
            if (isRepeatFish)
            {
                StartCoroutine(CoFishToRepeat());
            }
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }
        #endregion


        private IEnumerator CoFishToRepeat()
        {
            yield return new WaitForSeconds(2f); // 这个时间为显示鱼的动画时间
            _bg_animator.SetTrigger(RepeatAnimTrigger);
            _animator.SetTrigger(RepeatAnimTrigger);
            Game.Manager.audioMan.TriggerSound("FishBoardFishTurn");
            yield return new WaitForSeconds(0.5f); // 这个时间为 转换重复奖励的动画时间
            UIManager.Instance.Block(false);
        }

        private void OnClick()
        {
            // 判断是否是3星后的重复奖励
            StartCoroutine(isRepeatFish ? CoOnClickRepeat() : CoOnClickFish());
        }

        private IEnumerator CoOnClickRepeat()
        {
            _bg_animator.SetTrigger(RepeatCloseAnimTrigger);
            _animator.SetTrigger(RepeatCloseAnimTrigger);
            yield return new WaitForSeconds(0.15f);

            Close();
            foreach (var commitData in convertReward)
            {
                UIFlyUtility.FlyReward(commitData, repeatIcon.transform.position);
            }

            MessageCenter.Get<FISHING_FISH_CAUGHT_REPEAT>().Dispatch();
        }

        private IEnumerator CoOnClickFish()
        {
            UIManager.Instance.Block(true); // 在钓鱼主棋盘 unlock
            _bg_animator.SetTrigger(CloseAnimTrigger);
            _animator.SetTrigger(CloseAnimTrigger);
            yield return new WaitForSeconds(0.15f);

            // 获取主界面中图鉴位置对应transform
            UIManager.Instance.TryGetCache(UIConfig.UIActivityFishMain, out var fishMain);
            if (fishMain == null) yield return null;
            RectTransform handBook = ((UIActivityFishMain)fishMain).FindFishItem(_caughtInfo.fishId);

            Sequence seq = DOTween.Sequence();
            seq.Append(fishRect.transform.DOMove(handBook.position, fishFlyDuration).SetEase(customFlyPositionCurve));
            seq.Join(fishRect.DOSizeDelta(new Vector2(120, 120), fishFlyDuration).SetEase(customFlyPositionCurve));
            seq.Join(DOTween.To(() => fishEff.scale,
                    x => fishEff.scale = x,
                    0, fishFlyDuration)
                .SetEase(customFlyPositionCurve));


            seq.OnComplete(() =>
            {
                Close();
                // 通知钓鱼主棋盘做图鉴的表现 重复鱼时不通知
                MessageCenter.Get<FISHING_FISH_CAUGHT_CLOSE>().Dispatch(_caughtInfo);
            });
        }

        private IEnumerator CoOnPreOpen()
        {
            _bg_animator.SetTrigger(OpenAnimTrigger);
            _animator.SetTrigger(OpenAnimTrigger);
            Game.Manager.audioMan.TriggerSound("FishBoardGetFish");
            yield return new WaitForSeconds(1.5f);

            if (!isRepeatFish)
            {
                UIManager.Instance.Block(false);
            }
        }

        // 用于恢复原始状态的辅助方法
        private void RestoreOriginalState()
        {
            group.alpha = 1;
            fishImg.image.color = new Color(1, 1, 1, 1);
            fishRect.sizeDelta = new Vector2(460, 460);
            fishRect.anchoredPosition = new Vector2(0, 180);
            fishEff.scale = 100;
        }

        private void RefreshTheme()
        {
            activityFish.VisualGet.visual.Refresh(title, "mainTitle");
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != activityFish || !expire) return;
            Close();
        }

        public void OnNavBack()
        {
            OnClick();
        }
    }
}