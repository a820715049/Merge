/**
 * @Author: qun.chao
 * @Date: 2024-04-17 18:08:08
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/12/20 15:01:44
 * Description: 寻宝宝箱坑位
 */

using UnityEngine;
using UnityEngine.UI;
using EL;
using static FAT.UITreasureHuntUtility;
using Spine.Unity;
using DG.Tweening;

namespace FAT
{
    public class MBTreasureSlot : MonoBehaviour
    {
        // 宝箱有确切朝向 必须按ui设计图摆放
        // 某个slot对应的prefab是确定的
        [SerializeField] private string resType;
        private Animator animator;
        private SkeletonGraphic skeletonAnimation;
        private bool useSpine;
        private AnimationCurve e;
        private float t;
        public int origSiblingIndex { get; private set; } = -1;
        private ITreasureHuntAnim animationController;
        private GameObject goInst;
        private State curState = State.None;
        private int upHeight = 3000;

        public void Setup(bool isSpine, AnimationCurve e, float t)
        {
            this.e = e;
            this.t = t;
            useSpine = isSpine;
            goInst = UITreasureHuntUtility.CreateTreasure(resType);
            if (goInst != null)
            {
                goInst.transform.SetParent(transform);
                goInst.transform.localScale = Vector3.one;
                goInst.transform.localPosition = Vector3.zero;
                if (isSpine)
                {
                    skeletonAnimation = goInst.GetComponentInChildren<SkeletonGraphic>();
                    animationController = new SpineController(skeletonAnimation);
                }
                else
                {
                    animator = goInst.GetComponentInChildren<Animator>();
                    animationController = new AnimatorController(animator);
                }
                var btn = goInst.transform.Find("ClickArea").GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(_OnBtnInteract);

                // 默认隐藏
                Hide();
            }
            if (origSiblingIndex < 0)
            {
                origSiblingIndex = transform.GetSiblingIndex();
                if (isSpine)
                {
                    if (int.TryParse(transform.name, out int index))
                    {
                        origSiblingIndex =  index;
                    }
                }
            }
        }

        public void Cleanup()
        {
            if (goInst != null)
            {
                UITreasureHuntUtility.ReleaseTreasure(resType, goInst);
                animator = null;
                goInst = null;
            }
        }

        public void Show()
        {
            if (goInst != null)
                goInst.SetActive(true);
            curState = State.None;
        }

        public void Hide()
        {
            if (goInst != null)
            {
                if (useSpine)
                {
                    SetState(State.Dead);
                }
                else
                {
                    goInst.SetActive(false);
                }
            }
        }

        public void SetState(State st)
        {
            curState = st;
            animationController.PlayAnimation(st.ToString(), () => {
                if (useSpine)
                {
                    goInst.transform.DOMoveY(goInst.transform.position.y + upHeight, t).SetEase(e);
                }
            });
        }

        public void SetIdleDelay(float delay)
        {
            Invoke(nameof(_TryBornToIdle), delay);
        }

        private void _TryBornToIdle()
        {
            if (curState == State.Born)
            {
                SetState(State.Idle);
            }
        }

        /* 场景定义的宝箱idx 尝试对此宝箱交互(Open) */
        private void _OnBtnInteract()
        {
            if (UITreasureHuntUtility.IsBlocking())
                return;
            if (!UITreasureHuntUtility.TryGetEventInst(out var act))
                return;
            if (act.bonusTokenReward != null)
            {
                return;
            }

            // 宝箱层级可能动态调整 当前sibling不可靠
            // 需要使用初始化index
            var idx = origSiblingIndex;
            if (act.HasOpen(idx))
                return;
            var levelRewards = UITreasureHuntUtility.tempLevelRewards;
            var progressRewards = UITreasureHuntUtility.tempProgressRewards;
            levelRewards.Clear();
            progressRewards.Clear();

            var preKeyNum = act.GetKeyNum();
            if (!act.TryOpenBox(idx, levelRewards, progressRewards, out var state))
            {
                if (state == ActivityTreasure.TreasureBoxState.KeyNotEnough)
                {
                    UITreasureHuntUtility.OnKeyNotEnough(transform.position);
                }
                return;
            }
            var postKeyNum = act.GetKeyNum();
            if (preKeyNum > 0 && postKeyNum <= 0 && state != ActivityTreasure.TreasureBoxState.Treasure)
            {
                // 钥匙用尽 且 没有过关 需要主动弹出礼包
                // 过关时如果钥匙为0 礼包会自动弹出
                UITreasureHuntUtility.RegisterToShowGiftShop();
            }

            // 调整层级保证宝箱可见
            if (!useSpine)
                transform.SetAsLastSibling();

            // 开箱
            SetState(State.Open);
            MessageCenter.Get<MSG.TREASURE_OPENBOX>().Dispatch(transform.position, idx);
            DebugEx.Info($"[treasurehunt] {preKeyNum}/{postKeyNum} {state}");
            var v = transform.position;
            if (useSpine)
            {
                v = new Vector3(v.x, v.y + 50, v.z);
            }
            StartCoroutine(UITreasureHuntUtility.CoOnTreasureOpened(state, v, levelRewards));
        }
    }
}