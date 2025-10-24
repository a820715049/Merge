// ================================================
// File: UIFarmBoardGetSeed.cs
// Author: yueran.li
// Date: 2025/05/07 16:03:06 星期三
// Desc: 农场棋盘 黑五 获得货物界面
// ================================================


using System.Collections;
using DG.Tweening;
using EL;
using FAT.MSG;
using Spine.Unity;
using UnityEngine;

namespace FAT
{
    public class UIFarmBoardGetGoods : UIBase, INavBack
    {
        [SerializeField] private RectTransform rootImg;
        [SerializeField] private RectTransform rootSeed;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject rewardEff;
        public AnimationCurve showCurve;
        public float playStopTime = 1f; // stop 动画播放时间
        public float preWaitTime = 1f; // 卸货前等待时间
        public float postWaitTime = 1f; // 开走前等待时间
        [SerializeField] private RectTransform farmCar;
        [SerializeField] private GameObject farmCarEff;
        [SerializeField] private SkeletonGraphic carSpine;

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
            farmCar.anchoredPosition = new Vector2(2000, farmCar.anchoredPosition.y);
            rootImg.gameObject.SetActive(true);
            rewardEff.SetActive(false);
            farmCarEff.SetActive(false);
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
            // 卡车出现
            Game.Manager.audioMan.TriggerSound("FarmboardTUnlockAreaStartBF");

            rootImg.anchoredPosition = Vector2.zero;
            var to = rootImg.position;

            rootImg.position = startPos;

            animator.SetTrigger(UIManager.OpenAnimTrigger);
            yield return new WaitForSeconds(0.15f);

            rewardEff.SetActive(true);
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

            OnClickTween();
        }

        private Sequence carSeq;

        private void OnClickTween()
        {
            showPlaying = true;
            rootImg.gameObject.SetActive(false);
            rewardEff.SetActive(false);

            // 移动
            carSpine.AnimationState.SetAnimation(0, "move", true);

            carSeq?.Kill();
            carSeq = DOTween.Sequence();

            // 实际移动
            carSeq.Append(farmCar.DOAnchorPosX(0, 1f));

            // 提前播放停车动画
            carSeq.InsertCallback(playStopTime, () =>
            {
                // 停车
                carSpine.AnimationState.SetAnimation(0, "stop", false);
            });


            // 卸货前等待时间
            carSeq.AppendInterval(preWaitTime);

            carSeq.AppendCallback(() =>
            {
                // 卡车卸货
                Game.Manager.audioMan.TriggerSound("FarmboardUnlockAreaBF");
                // 卸货
                carSpine.AnimationState.SetAnimation(0, "out", false);
                // 烟雾特效
                farmCarEff.SetActive(true);

                // 显示货物
                var mainUI = UIManager.Instance.TryGetUI(_activity.VisualBoard.res.ActiveR);
                if (mainUI != null && mainUI is UIFarmBoardMain farmBoardMain)
                {
                    if (farmBoardMain.mbFarm != null && farmBoardMain.mbFarm is MBFarmBoardFarm_Goods goods)
                    {
                        // 获得第几个货物
                        var index = _activity.UnlockFarmlandNum;
                        var fieldSpine = goods.FieldSpineAry[index - 1];
                        fieldSpine.gameObject.SetActive(true);
                    }
                }
            });

            // 开走前等待时间
            carSeq.AppendInterval(postWaitTime);

            carSeq.AppendCallback(() =>
            {
                // 移动
                carSpine.AnimationState.SetAnimation(0, "move", true);
                // 烟雾特效
                farmCarEff.SetActive(false);
            });
            carSeq.Append(farmCar.DOAnchorPosX(-1200, 1f));

            // 界面关闭动画
            carSeq.AppendCallback(() => { animator.SetTrigger(UIManager.CloseAnimTrigger); });
            carSeq.AppendInterval(0.5f);

            carSeq.OnComplete(() =>
            {
                MessageCenter.Get<FARM_BOARD_SEED_CLOSE>().Dispatch();
                Close();
            });
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
            carSeq?.Kill();
        }
    }
}