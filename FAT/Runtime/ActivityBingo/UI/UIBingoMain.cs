/*
 * @Author: qun.chao
 * @Doc: https://centurygames.feishu.cn/wiki/IvlUwAEYaif2tEkYXPycx8gDn0e
 * @Date: 2025-02-28 17:43:32
 */
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using EL;
using fat.rawdata;
using Spine.Unity;
using System;
using Cysharp.Threading.Tasks;

namespace FAT
{
    public class UIBingoMain : UIBase, INavBack
    {
        // 同一个UI 兼具预告通知、分组选择、关卡内三种状态
        public enum State
        {
            None,
            Welcome,
            NextRound,
            Select,
            Main,
        }

        [Serializable]
        public class AnimGroup
        {
            public Animator selectSpawner;
            public Animator nextRound;
            public Animator talk;
            public Animator progress;
            public Animator spawner;
            public SkeletonGraphic spCurtain;
        }

        // 全部可切换组件
        [SerializeField] private GameObject[] objAll;
        // 预告弹窗状态时的组件
        [SerializeField] private GameObject[] objWelcome;
        // 新一轮弹窗状态时的组件
        [SerializeField] private GameObject[] objNextRound;
        // 选择生成器状态时的组件
        [SerializeField] private GameObject[] objSelect;
        // 玩法进行状态时的组件
        [SerializeField] private GameObject[] objMain;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnGo;
        [SerializeField] private Button btnNotReady;
        [SerializeField] private TextProOnACircle txtTitle;
        [SerializeField] private TextMeshProUGUI txtTalk;
        [SerializeField] private TextMeshProUGUI txtCD;
        [SerializeField] private MBBingoGroupSelector compSelector;
        [SerializeField] private MBBingoBoard compBoard;
        [SerializeField] private MBBingoSpawner compSpawner;
        [SerializeField] private MBBingoProgress compProgress;
        [SerializeField] private MBBingoRewardInfo compReward;
        [SerializeField] private GameObject goBlock;
        [SerializeField] private GameObject goCurtainBlock;
        [SerializeField] private AnimGroup animGroup;

        public ActivityBingo ActInst => actInst;
        private ActivityBingo actInst;
        private State curState = State.None;
        private bool showAsWelcome = false; // 是否以预告弹窗状态展示
        private int selectedGroupId = 0; // 当前选中的关卡组ID
        private bool ignoreHideWhenTransit = false; // 切换状态时不关闭组件(因为要播放消失动画)
        private UIBingoTransitionHelper transitionHelper = new();

        protected override void OnCreate()
        {
#if UNITY_EDITOR
            var debug = transform.Access<Button>("Debug");
            debug.onClick.AddListener(DebugMoveState);
#endif

            transitionHelper.InitOnce(animGroup);

            transform.Access<Button>("Mask").onClick.AddListener(Close);
            btnClose.onClick.AddListener(Close);
            btnGo.onClick.AddListener(OnBtnClickStart);
            btnNotReady.onClick.AddListener(OnBtnClickNotReady);
        }

        protected override void OnParse(params object[] items)
        {
            actInst = items[0] as ActivityBingo;
            showAsWelcome = items.Length > 1 && items[1] != null && (bool)items[1];
        }

        protected override void OnPreOpen()
        {
            selectedGroupId = 0;
            ignoreHideWhenTransit = false;

            compSelector.InitOnPreOpen(this);
            compBoard.InitOnPreOpen(this);
            compSpawner.InitOnPreOpen(this);
            compProgress.InitOnPreOpen(this);
            compReward.InitOnPreOpen(this);

            RefreshTheme();
            RefreshCD();
            SetState(ResolveCurrentStateWhenOpen());

            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(OnMessageSecondDriver);
        }

        protected override void OnPreClose()
        {
            actInst.ItemRes.ActiveR.Close();
            actInst.IsMain = false;
        }

        protected override void OnPostClose()
        {
            BoardViewManager.Instance.OnInventoryClose();

            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnMessageSecondDriver);

            compSelector.CleanupOnPostClose();
            compBoard.CleanupOnPostClose();
            compSpawner.CleanupOnPostClose();
            compProgress.CleanupOnPostClose();
            compReward.CleanupOnPostClose();

            actInst = null;
        }

        public async UniTask RefreshForNextLevel()
        {
            animGroup.spawner.SetTrigger("Hide");
            animGroup.progress.SetTrigger("Convert");
            await UniTask.WaitForSeconds(0.13f);
            compProgress.Refresh();
            compSpawner.Refresh();
            animGroup.spawner.SetTrigger("Show");
        }

        public void MoveToNextRound()
        {
            TransitState(State.NextRound);
        }

        private void RefreshTheme()
        {
            var visual = actInst.MainVisual;
            visual.Refresh(txtTitle, "mainTitle");
            visual.Refresh(txtCD, "time");
        }

        #region 预告弹窗
        private void ShowWelcome()
        {
            SetObjectState(objAll, false);
            SetObjectState(objWelcome, true);
            txtTalk.text = I18N.Text("#SysComDesc858");
            animGroup.spCurtain.AnimationState.SetAnimation(0, "idle", false);
            animGroup.talk.SetTrigger("Show");
        }
        #endregion

        #region 新一轮弹窗
        private void ShowNextRound()
        {
            SetObjectState(objAll, false);
            SetObjectState(objNextRound, true);
        }
        #endregion

        #region 关卡选择
        private void ShowSelect()
        {
            SetObjectState(objAll, false);
            SetObjectState(objSelect, true);
            txtTalk.text = I18N.Text("#SysComDesc859");
            compSelector.Refresh(OnBtnSelectGroup);
            var canvasGroup = animGroup.selectSpawner.GetComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            animGroup.selectSpawner.SetTrigger("Show");
            animGroup.spCurtain.AnimationState.SetAnimation(0, "idle", false);
        }
        #endregion

        #region 关卡内
        private void ShowMain()
        {
            actInst.IsMain = true;
            SetObjectState(objAll, false);
            SetObjectState(objMain, true);
            var canvasGroup = animGroup.selectSpawner.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            goCurtainBlock.SetActive(false);
            compBoard.Refresh();
            compSpawner.Refresh();
            compReward.Refresh();
            compProgress.Refresh();
        }
        #endregion

        private void SetObjectState(GameObject[] comps, bool show)
        {
            if (ignoreHideWhenTransit && !show)
                return;
            foreach (var comp in comps)
            {
                comp.SetActive(show);
            }
        }

        private void TryAutoSelect()
        {
            // 如果当前没有选择关卡组，且候选项目只有一个
            // 则直接选择第一个可选的关卡组
            if (!actInst.CheckGroupStart())
            {
                var groups = actInst.GetOptionalBingoGroup();
                if (groups.Count > 0)
                {
                    actInst.ChooseGroup(groups.Keys.First());
                }
            }
        }

        private State ResolveCurrentStateWhenOpen()
        {
            if (showAsWelcome)
            {
                showAsWelcome = false;
                return State.Welcome;
            }
            else
            {
                if (actInst.CheckGroupStart())
                {
                    return State.Main;
                }
                else
                {
                    return State.Select;
                }
            }
        }

        private void SetState(State st)
        {
            switch (st)
            {
                case State.Welcome:
                    ShowWelcome();
                    break;
                case State.NextRound:
                    ShowNextRound();
                    break;
                case State.Select:
                    ShowSelect();
                    break;
                case State.Main:
                    ShowMain();
                    break;
            }
            curState = st;
        }

        private void TransitState(State st)
        {
            ignoreHideWhenTransit = true;
            var beforeState = curState;
            SetState(st);
            transitionHelper.Transit(beforeState, st);
            ignoreHideWhenTransit = false;
        }

        private void OnBtnClickNotReady()
        {
            // 只有未选择group时会出现此按钮
            Game.Manager.commonTipsMan.ShowPopTips(Toast.ItemBingoChoose);
        }

        private void OnBtnClickStart()
        {
            var state = curState;
            switch (state)
            {
                case State.Welcome:
                    showAsWelcome = false;
                    if (actInst.CheckGroupStart())
                    {
                        TransitState(State.Main);
                    }
                    else
                    {
                        TransitState(State.Select);
                    }
                    break;
                case State.Select:
                    {
                        if (selectedGroupId > 0)
                        {
                            actInst.ChooseGroup(selectedGroupId);
                            TransitState(State.Main);
                            Game.Manager.guideMan.TriggerGuide();
                        }
                    }
                    break;
                case State.Main:
                    {
                        base.Close();
                        if (!UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain))
                        {
                            GameProcedure.SceneToMerge();
                        }
                    }
                    break;
                case State.NextRound:
                    TransitState(State.Main);
                    break;
            }
        }

        private void OnBtnSelectGroup(int id)
        {
            selectedGroupId = id;
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(txtCD, actInst.Countdown);
        }

        private void OnMessageSecondDriver()
        {
            RefreshCD();
        }

        void INavBack.OnNavBack()
        {
            if (!goBlock.activeSelf)
            {
                base.Close();
            }
        }

#if UNITY_EDITOR
        private void DebugMoveState()
        {
            var cur = this.curState;
            switch (cur)
            {
                case State.Welcome:
                    actInst.BingoGroupID = 0;
                    SetState(State.Select);
                    animGroup.talk.SetTrigger("Show");
                    animGroup.selectSpawner.SetTrigger("Show");
                    animGroup.spCurtain.AnimationState.SetAnimation(0, "idle", true);
                    break;
                case State.Select:
                    TryAutoSelect();
                    SetState(State.Main);
                    break;
                case State.Main:
                    SetState(State.NextRound);
                    animGroup.talk.SetTrigger("Show");
                    animGroup.spCurtain.AnimationState.SetAnimation(0, "idle", true);
                    break;
                case State.NextRound:
                    SetState(State.Welcome);
                    animGroup.spCurtain.AnimationState.SetAnimation(0, "idle", true);
                    break;
            }
        }
#endif
    }
}
