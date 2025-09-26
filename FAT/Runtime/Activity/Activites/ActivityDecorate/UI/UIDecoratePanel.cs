/*
 *@Author:chaoran.zhang
 *@Desc:装饰页活动面板
 *@Created Time:2024.05.23 星期四 15:38:55
 */

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIDecoratePanel : UIBase
    {
        [SerializeField] private GameObject _temp;
        [SerializeField] private Transform _root;
        [SerializeField] private GameObject _preview;
        private TextProOnACircle _title;
        private TextProOnACircle _titleBottom;
        private TextMeshProUGUI _leftTime;
        private RectTransform _progress;
        private TextMeshProUGUI _count;
        private MBRewardIcon _reward;
        private EventDecorateGroup _conf;
        private int _lastCount;
        private int _maxCount;
        private readonly List<DecorateLayout> _list = new List<DecorateLayout>();

        protected override void OnCreate()
        {
            _title = transform.Find("Content/Panel/Top/Title").GetComponent<TextProOnACircle>();
            _titleBottom = transform.Find("Content/Panel/Top/TitleBottom").GetComponent<TextProOnACircle>();
            _leftTime = transform.Find("Content/Panel/_cd/text").GetComponent<TextMeshProUGUI>();
            _progress = transform.Find("Content/Panel/Progress/Fill") as RectTransform;
            _count = transform.Find("Content/Panel/Progress/Num").GetComponent<TextMeshProUGUI>();
            _reward = transform.Find("Content/Panel/Progress/Reward").GetComponent<MBRewardIcon>();
            transform.AddButton("Content/Panel/Right/BtnClose", () => { base.Close(); });
            transform.AddButton("Content/Panel/Left/Help_Btn",
                (() => { UIManager.Instance.OpenWindow(Game.Manager.decorateMan.Activity.HelpUI.ActiveR); }));
            transform.AddButton("Content/Preview", ClickPreview);
        }

        protected override void OnVisible(bool v_)
        {
            if (!v_) return;
            Refresh();
        }

        protected override void OnPreOpen()
        {
            RefreshCD();
            if (_conf == null || Game.Manager.decorateMan.NeedRefreshUI)
                Init();
            else
            {
                Refresh();
            }

            UIManager.Instance.OpenWindow(UIConfig.UIDecorateRes);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            if (_preview != null)
                _preview.SetActive(Game.Manager.decorateMan.CheckCanPreview());
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            if (Game.Manager.decorateMan.AnimList.Count > 0)
            {
                UIManager.Instance.Block(false);
                DebugEx.FormatInfo("DecoratePanel Close,AnimList Num > 0,Set Block False");
            }
            UIManager.Instance.CloseWindow(UIConfig.UIDecorateRes);
        }

        private void Init()
        {
            _conf = Game.Manager.decorateMan.Activity.CurGroupConf;
            InitProgress();
            CreateLayout();
            Game.Manager.decorateMan.NeedRefreshUI = false;
            _title.SetText(I18N.Text(Game.Manager.decorateMan.Activity.confD.Name));
            _titleBottom.SetText(I18N.Text(Game.Manager.decorateMan.Activity.confD.Name));
            _reward.Refresh(_conf.MilestoneReward[0].ConvertToInt3().Item1,
                _conf.MilestoneReward[0].ConvertToInt3().Item2);
        }

        //初始化进度条
        private void InitProgress()
        {
            var total = _conf.DecorateNum;
            var count = Game.Manager.decorateMan.UnlockDecoration.Count;
            _maxCount = total;
            _lastCount = count;
            _progress.anchorMax = new Vector2((float)count / total, 1);
            _count.text = count + "/" + total;
        }

        //创建layout
        private void CreateLayout()
        {
            if (_root.childCount > 0)
                _root.DestroyAllChildren();
            _list.Clear();
            foreach (var kv in _conf.IncludeLvId)
            {
                var obj = GameObject.Instantiate(_temp, _root);
                obj.SetActive(true);
                var layout = obj.GetComponent<DecorateLayout>();
                _list.Add(layout);
                layout.Init(kv);
            }

            IEnumerator Wait()
            {
                yield return null;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_root as RectTransform);
                yield return null;
                var rect = _root.transform as RectTransform;
                var target = Mathf.Min(rect.rect.height - 1212, 519 * Game.Manager.decorateMan.Activity.CurLevel);
                rect.anchoredPosition = target * Vector2.up;
            }

            StartCoroutine(Wait());
        }

        private void Refresh()
        {
            _title.SetText(I18N.Text(Game.Manager.decorateMan.Activity.confD.Name));
            _titleBottom.SetText(I18N.Text(Game.Manager.decorateMan.Activity.confD.Name));
            if (Game.Manager.decorateMan.AnimList.Count > 0)
            {
                DebugEx.FormatInfo("DecoratePanel Open,AnimList Num > 0,Set Block True");
                UIManager.Instance.Block(true);
                Game.Manager.decorateMan.PlayAnim();
            }
            else
            {
                RefreshLayout();
                if (_lastCount != Game.Manager.decorateMan.UnlockDecoration.Count && _lastCount < _maxCount)
                {
                    _lastCount = Game.Manager.decorateMan.StartNewGroup
                        ? _maxCount
                        : Game.Manager.decorateMan.UnlockDecoration.Count;
                    Game.Manager.decorateMan.AnimList.Add((3, RefreshProgress));
                }

                if (Game.Manager.decorateMan.LevelReward.Count > 0 && !Game.Manager.decorateMan.StartNewGroup &&
                    !Game.Manager.decorateMan.AllEnd)
                    Game.Manager.decorateMan.AnimList.Add((4, RefreshContent));
                else
                {
                    if (!Game.Manager.decorateMan.StartNewGroup && !Game.Manager.decorateMan.AllEnd)
                    {
                        var rect = _root.transform as RectTransform;
                        var target = Mathf.Min(rect.rect.height - 1212,
                            519 * Game.Manager.decorateMan.Activity.CurLevel);
                        rect.anchoredPosition = target * Vector2.up;
                    }
                }

                Game.Manager.decorateMan.SortAnimList();
                if (Game.Manager.decorateMan.AnimList.Count > 0)
                {
                    DebugEx.FormatInfo("DecoratePanel Refresh,AnimList Num > 0,Set Block True");
                    UIManager.Instance.Block(true);
                    Game.Manager.decorateMan.PlayAnim();
                }
            }
        }

        //刷新进度条
        private void RefreshProgress()
        {
            var total = _conf.DecorateNum;
            var count = (Game.Manager.decorateMan.StartNewGroup || Game.Manager.decorateMan.AllEnd)
                ? _maxCount
                : Game.Manager.decorateMan.UnlockDecoration.Count;
            DOTween.To(() => _progress.anchorMax, x => _progress.anchorMax = x,
                new Vector2((float)count / total, 1),
                1f).OnComplete(() => MessageCenter.Get<MSG.DECORATE_ANIM_END>().Dispatch());
            _progress.GetComponent<Animator>().SetTrigger("Punch");
            _count.text = count + "/" + total;
            Game.Manager.audioMan.TriggerSound("DecorateProgress");
        }

        //滑动
        private void RefreshContent()
        {
            var rect = _root.transform as RectTransform;
            var target = Mathf.Min(rect.rect.height - 1212, 519 * Game.Manager.decorateMan.Activity.CurLevel);
            DOTween.To(() => rect.anchoredPosition, x => rect.anchoredPosition = x, target * Vector2.up, 0.5f)
                .OnComplete(() => MessageCenter.Get<MSG.DECORATE_ANIM_END>().Dispatch());
        }


        //刷新layout
        private void RefreshLayout()
        {
            foreach (var kv in _list)
            {
                kv.Refresh();
            }
        }

        private void RefreshCD()
        {
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, Game.Manager.decorateMan.Activity.endTS - t);
            UIUtility.CountDownFormat(_leftTime, diff);
        }

        public Transform FindFirstEnable()
        {
            Transform trans = null;
            foreach (var kv in _list)
            {
                trans = kv.FindFirstEnable();
                if (trans != null)
                    break;
            }

            return trans;
        }

        public void ClickPreview()
        {
            Close();
            UIManager.Instance.OpenWindow(UIConfig.UIDecorateOverview);
            GameProcedure.MergeToSceneArea(Game.Manager.decorateMan.Activity.CurArea, overview_: true);
        }
    }
}