// ===================================================
// Author: mengqc
// Date: 2025/09/02
// ===================================================

using System.Collections;
using System.Collections.Generic;
using BezierSolution;
using EL;
using fat.conf;
using FAT.MSG;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIVineLeapMain : UIBase
    {
        public MCVineScrollList vineScrollList;
        public TextMeshProUGUI tfDesc;
        public TextMeshProUGUI tfDesc2;
        public UIVineLeapStandPlace milestoneStandPos;
        public RectTransform runningProgress;
        public RectTransform hideRoot;
        public RectTransform playerMoveContent;
        public BezierSpline spline;
        public TextMeshProUGUI tfProgress;
        public UIImageRes imgChest;
        public Animator btnStartAnime;

        public ActivityVineLeap activity => _activity;
        public GameObject playerObj => _playerObj;

        private ActivityVineLeap _activity;
        private List<UICommonItem> _milestoneRewards;
        private TextMeshProUGUI _tfCd;
        private Button _btnStart;
        private VineLeapPlayerManager _playerManager = new();
        private GameObject _playerObj;

        protected override void OnCreate()
        {
            _tfCd = transform.Access<TextMeshProUGUI>("Content/TopContent/_cd/text");
            transform.AddButton("Content/BtnClose", OnClose);
            transform.AddButton("Content/BtnInfo", OnOpenHelp);
            var milestoneTrans = transform.Access<RectTransform>("Content/TopContent/Rewards/Tip/Group");
            _milestoneRewards = new List<UICommonItem>();
            foreach (Transform child in milestoneTrans)
            {
                var item = child.GetComponent<UICommonItem>();
                _milestoneRewards.Add(item);
            }

            _btnStart = transform.AddButton("Content/DownContent/btnStart", OnStart);
            _playerObj = hideRoot.GetChild(0).gameObject;
            _playerManager.Init(15, this);
#if UNITY_EDITOR
            transform.AddButton("Content/TopContent/Rewards/Chest", OnDebugAnime);
#endif
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            _activity = (ActivityVineLeap)items[0];
            var groupConf = _activity.GetCurGroupConf();
            imgChest.SetImage(_activity.GetChestIcon());
            tfDesc.text = I18N.FormatText("#SysComDesc1729", groupConf.LevelId.Count);
            UpdateMilestoneRewards();
            vineScrollList.SetLeafNum(groupConf.LevelId.Count);
            vineScrollList.SetClickAction(OnClickStepItem);
            UpdateStateArea();
            if (!IsVisitedResult() && _activity.LevelResult)
            {
                var preLv = _activity.CurLevel - 1;
                RefreshVineItems(preLv, true);
                vineScrollList.FocusToItem(preLv);
                vineScrollList.Item[preLv].progressBar.gameObject.SetActive(false);
            }
            else
            {
                vineScrollList.FocusToItem(_activity.CurLevel);
            }

            if (!IsCurStepRunning() && !IsVisitedResult() && _activity.LevelResult)
            {
                _btnStart.gameObject.SetActive(false);
            }
        }

        protected override void OnPostOpen()
        {
            base.OnPostOpen();
            if (IsCurStepRunning())
            {
                if (IsVisitedStart())
                {
                    _playerManager.InitPlayers(_activity.CurLevel, true);
                }
                else
                {
                    ShowStart(0.5f);
                }
            }
            else if (!IsVisitedResult())
            {
                if (_activity.LevelResult)
                {
                    _playerManager.InitPlayers(_activity.CurLevel - 1, true);
                    Game.StartCoroutine(_playerManager.MoveToNextLevel(_activity.CurLevel, _activity.GetResultRank()));
                }
                else
                {
                    _playerManager.InitPlayers(_activity.CurLevel, false);
                }

                _activity.SetVisitedResult();
            }
            else
            {
                _playerManager.InitPlayers(_activity.CurLevel, false);
            }

            MessageCenter.Get<MSG.VINELEAP_STEP_START>().AddListener(ShowStart);
            MessageCenter.Get<MSG.VINELEAP_STEP_END>().AddListener(OnStepEnd);
        }

        protected override void OnPreClose()
        {
            base.OnPreClose();
            MessageCenter.Get<MSG.VINELEAP_STEP_START>().RemoveListener(ShowStart);
            MessageCenter.Get<MSG.VINELEAP_STEP_END>().RemoveListener(OnStepEnd);
        }

        protected override void OnAddListener()
        {
            base.OnAddListener();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            base.OnRemoveListener();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void UpdateMilestoneRewards()
        {
            var milestoneRewards = _activity.GetMilestoneRewards();
            for (var i = 0; i < _milestoneRewards.Count; i++)
            {
                if (i >= milestoneRewards.Length)
                {
                    _milestoneRewards[i].gameObject.SetActive(false);
                }
                else
                {
                    _milestoneRewards[i].gameObject.SetActive(true);
                    _milestoneRewards[i].Refresh(milestoneRewards[i]);
                }
            }
        }

        private void RefreshVineItems(int lv, bool isCurStepRunning)
        {
            for (var i = 0; i < vineScrollList.Item.Count; i++)
            {
                var vineItem = vineScrollList.Item[i];
                var state = UIVineLeapStepItem.EVineLeapStepItemState.INACTIVE;
                if (i < lv)
                {
                    state = UIVineLeapStepItem.EVineLeapStepItemState.FINISHED;
                }
                else if (i == lv)
                {
                    state = UIVineLeapStepItem.EVineLeapStepItemState.ACTIVE;
                }

                vineItem.Refresh(i, state, _activity, isCurStepRunning);
            }
        }

        private bool IsVisitedResult()
        {
            return _activity.IsVisitedResult;
        }

        private bool IsVisitedStart()
        {
            return _activity.IsVisitedStart;
        }

        private bool IsCurStepRunning()
        {
            return _activity.IsCurStepRunning();
        }

        private void ShowStart()
        {
            ShowStart(0f);
        }

        private void ShowStart(float delay = 0)
        {
            UpdateStateArea();
            _playerManager.InitPlayers(_activity.CurLevel, false);
            _activity.SetVisitedStart();
            Game.StartCoroutine(_playerManager.ShowLevelStart(delay));
        }

        private void OnStepEnd(bool isWin)
        {
            UpdateStateArea();
            if (!isWin)
            {
                _playerManager.InitPlayers(_activity.CurLevel, false);
                _activity.VisualFailed.res.ActiveR.Open(_activity);
            }
        }

        public void UpdateStateArea()
        {
            RefreshVineItems(_activity.CurLevel, _activity.IsCurStepRunning());
            var levelCfg = _activity.GetCurLevelConf();
            if (_activity.IsCurStepRunning())
            {
                runningProgress.gameObject.SetActive(true);
                _btnStart.gameObject.SetActive(false);
                tfDesc2.text = I18N.FormatText("#SysComDesc1732", levelCfg.TotalNum);
                RefreshCD();
            }
            else
            {
                runningProgress.gameObject.SetActive(false);
                _btnStart.gameObject.SetActive(true);
                tfDesc2.text = I18N.Text("#SysComDesc1734");
            }
        }


        private void OnClose()
        {
            Exit();
        }

        public void Exit(bool ignoreFrom = false)
        {
            if (_activity.IsOpenWithLoading)
            {
                ActivityTransit.Exit(_activity, ResConfig, null, ignoreFrom); // 退出活动时默认返回主棋盘
            }
            else
            {
                Close();
            }
        }

        private void OnOpenHelp()
        {
            _activity.VisualHelp.res.ActiveR.Open(_activity);
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(_tfCd, _activity.Countdown);
            if (_activity.IsCurStepRunning())
            {
                var levelCfg = _activity.GetCurLevelConf();
                runningProgress.gameObject.SetActive(true);
                tfProgress.text = $"{levelCfg.TotalNum - activity.GetSeatsLeft()}/{levelCfg.TotalNum}";
            }
        }

        private void OnStart()
        {
            _activity.StartCurStep();
        }

        private void OnClickStepItem(int index, UIVineLeapStepItem item)
        {
            if (index != _activity.CurLevel) return;
            if (_activity.IsCurStepRunning())
            {
                var itemCfg = ObjTokenVisitor.Get(_activity.TokenId);
                var str = itemCfg == null ? "" : I18N.FormatText("#SysComDesc1731", $"<sprite name=\"{itemCfg.SpriteName}\">");
                UIManager.Instance.OpenWindow(UIConfig.UIVineLeapMsgTip, item.standPos.position, 0f, str,
                    false);
            }
            else
            {
                btnStartAnime.SetTrigger("Punch");
            }
        }

        private void OnDebugAnime()
        {
            Game.StartCoroutine(PlayDebugMoveNext());
        }

        private IEnumerator PlayDebugMoveNext()
        {
            UpdateStateArea();
            var curLv = _activity.CurLevel;
            var nextLv = curLv + 1;
            var preItem = vineScrollList.Item[curLv];
            preItem.Refresh(preItem.Index, UIVineLeapStepItem.EVineLeapStepItemState.INACTIVE, _activity, true);
            _playerManager.InitPlayers(curLv, true);
            yield return _playerManager.MoveToNextLevel(nextLv, Random.Range(1, 15));
        }
    }
}