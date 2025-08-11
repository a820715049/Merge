// ================================================
// File: UIFarmBoardGetSeed.cs
// Author: yueran.li
// Date: 2025/05/07 16:03:06 星期三
// Desc: 农场棋盘获得种子界面
// ================================================


using System.Collections;
using DG.Tweening;
using EL;
using FAT.MSG;
using UnityEngine;

namespace FAT
{
    public class UIFarmBoardGetSeed : UIBase, INavBack
    {
        [SerializeField] private RectTransform rootImg;
        [SerializeField] private RectTransform rootSeed;
        [SerializeField] private Animator animator;
        public AnimationCurve showCurve;
        public AnimationCurve hideCurve;

        // 活动实例
        private FarmBoardActivity _activity;
        private Vector3 startPos;
        private bool showPlaying = false;

        #region UI
        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnClick);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;
            _activity = (FarmBoardActivity)items[0];

            startPos = (Vector3)items[1];
        }

        protected override void OnPreOpen()
        {
            UIManager.Instance.Block(true);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnPostOpen()
        {
            StartCoroutine(CoOnPostOpen());
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }
        #endregion

        private IEnumerator CoOnPostOpen()
        {
            rootImg.anchoredPosition = Vector2.zero;
            var to = rootImg.position;

            rootImg.position = startPos;

            animator.SetTrigger(UIManager.OpenAnimTrigger);
            yield return new WaitForSeconds(0.15f);

            var seq = DOTween.Sequence();
            seq.Append(rootImg.DOMove(to, 0.5f).SetEase(showCurve));
            seq.OnComplete(() => { UIManager.Instance.Block(false); });
            seq.Play();
        }

        private void OnClick()
        {
            if (showPlaying)
            {
                return;
            }
            StartCoroutine(CoOnClickTween());
        }

        private IEnumerator CoOnClickTween()
        {
            showPlaying = true;
            int farmIndex = _activity.UnlockFarmlandNum - 1;
            var mainUI = UIManager.Instance.TryGetUI(UIConfig.UIFarmBoardMain);
            if (mainUI != null)
            {
                var farmBoardMain = (UIFarmBoardMain)mainUI;
                animator.SetTrigger(UIManager.CloseAnimTrigger);
                var pos = farmBoardMain.mbFarm.GetFarmByIndex(farmIndex).position;
                rootSeed.position = pos;
                rootImg.DOMove(pos, 1f)
                    .SetEase(hideCurve)
                    .OnComplete(() =>
                    {
                        // 播放音效 倒肥料
                        Game.Manager.audioMan.TriggerSound("FarmboardFertilize");
                    });

                // 结束动画时间
                yield return new WaitForSeconds(2.2f);
                MessageCenter.Get<FARM_BOARD_SEED_CLOSE>().Dispatch();
                Close();
            }
            else
            {
                Close();
            }
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != _activity || !expire) return;
            Close();
        }

        public void OnNavBack()
        {
            OnClick();
        }

        protected override void OnPostClose()
        {
            showPlaying = false;
        }
    }
}