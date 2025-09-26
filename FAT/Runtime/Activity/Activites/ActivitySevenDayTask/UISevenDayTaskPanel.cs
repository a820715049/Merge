using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DG.Tweening;
using EL;
using fat.conf;
using fat.rawdata;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UISevenDayTaskPanel : UIBase
    {
        public TextMeshProUGUI cd;
        public float animationDelay;
        public float animationDuration;
        public float startScale;
        public float startAlpha;
        public float sortDuration;
        public AnimationCurve sclaeCurve;
        public AnimationCurve alphaCurve;
        public float UnlockInterval;
        public List<MBSevenDayLabel> labels;
        public Transform content;
        public Transform progressContent;
        public GameObject cell;
        public GameObject progressCell;
        public Animator progressAnim;
        public TextMeshProUGUI progressCount;
        public RectTransform progressMask;
        public Transform finalReward;
        public TextMeshProUGUI bottomTips;
        private ActivitySevenDayTask _activity;
        private List<MBSevenDayTaskCell> _cells = new();
        private List<MBSevenDayTaskCell> _showCells = new();
        private List<MBSevenDayMilestone> _mileNode = new();
        private SevenDayTaskGroup _curGroup;

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as ActivitySevenDayTask;
            for (var i = 0; i < _activity.detailConfig.TaskGroup.Count; i++)
            {
                if (_activity.WhetherUnlock(i)) { continue; }
                _ChooseLabel(i - 1);
                return;
            }
            _ChooseLabel(6);
        }

        protected override void OnPreOpen()
        {
            RefreshLabel();
            RefreshProgress();
            RefreshCD();
            var token = Game.Manager.objectMan.GetTokenConfig(_activity.eventConfig.TokenId);
            var sprite = "<sprite name=\"" + token.SpriteName + "\">";
            bottomTips.text = I18N.FormatText("#SysComDesc1594", sprite);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(RefreshProgress);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(RefreshProgress);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void RefreshProgress(FlyableItemSlice slice)
        {
            if (slice.FlyType != FlyType.SevenDayToken || slice.CurIdx != 1) { return; }
            progressAnim.SetTrigger("Punch");
            progressCount.text = _activity.TokenNum.ToString();
            var end = 40f;
            var last = 0f;
            var interval = 660f / _activity.detailConfig.PointsRwd.Count;
            foreach (var id in _activity.detailConfig.PointsRwd)
            {
                var conf = SevenDayTaskRwdVisitor.Get(id);
                if (_activity.TokenNum >= conf.Points)
                {
                    end += interval;
                    last = conf.Points;
                    continue;
                }
                else
                {
                    end += interval * (_activity.TokenNum - last) / (conf.Points - last);
                    break;
                }
            }
            UIManager.Instance.Block(true);
            progressMask.DOSizeDelta(new Vector2(end, 41), 0.5f).OnComplete(() =>
            {
                if (_activity.milestoneRewardCommitData.Count > 0)
                {
                    if (_activity.phase < _activity.detailConfig.PointsRwd.Count)
                    {
                        var mile = _mileNode[_activity.phase - 1];
                        UIFlyUtility.FlyReward(_activity.milestoneRewardCommitData[0], mile.transform.GetChild(0).transform.position);
                        mile.PlayComplete();
                        _activity.milestoneRewardCommitData.Clear();
                    }
                    else if (!_activity.hasFinishReward)
                    {
                        finalReward.Find("Complete").localScale = Vector3.one;
                        UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, finalReward.position, _activity.milestoneRewardCommitData, _activity.detailConfig.FinalBox, I18N.Text("#SysComDesc726"));
                        _activity.hasFinishReward = true;
                    }
                }
                UIManager.Instance.Block(false);

            });
        }

        private void RefreshProgress()
        {
            var final = SevenDayTaskRwdVisitor.Get(_activity.detailConfig.PointsRwd[^1]);
            finalReward.GetChild(1).GetComponent<UIImageRes>().SetImage(_activity.detailConfig.FinalBox);
            finalReward.GetChild(2).GetComponent<TextMeshProUGUI>().text = final.Points.ToString();
            finalReward.Find("Complete").localScale = _activity.phase >= _activity.detailConfig.PointsRwd.Count ? Vector3.one : Vector3.zero;
            progressCount.text = _activity.TokenNum.ToString();
            var interval = 660f / _activity.detailConfig.PointsRwd.Count;
            while (_mileNode.Count < _activity.detailConfig.PointsRwd.Count - 1)
            {
                var obj = Instantiate(progressCell, progressContent).GetComponent<MBSevenDayMilestone>();
                _mileNode.Add(obj);
            }

            for (var i = 0; i < _mileNode.Count; i++)
            {
                _mileNode[i].RefreshData(_activity.detailConfig.PointsRwd[i]);
                _mileNode[i].RefreshState(_activity.phase > i);
                _mileNode[i].transform.localPosition = new(-309 + interval * (i + 1), 0, 0);
            }
            var end = 40f;
            var last = 0;
            foreach (var id in _activity.detailConfig.PointsRwd)
            {
                var conf = SevenDayTaskRwdVisitor.Get(id);
                if (_activity.TokenNum >= conf.Points)
                {
                    end += interval;
                    last = conf.Points;
                    continue;
                }
                else
                {
                    end += interval * (_activity.TokenNum - last) / (conf.Points - last);
                    break;
                }
            }
            progressMask.sizeDelta = new(end, 41);
        }

        public void RefreshLabel()
        {
            foreach (var label in labels)
            {
                label.SetText();
                label.SetLock(_activity.WhetherUnlock(label.index));
                label.SetComplete(_activity.WhetherComplete(label.index) && _activity.WhetherUnlock(label.index));
                label.SetRedPoint(_activity.GetCanCompleteNum(label.index), _activity.WhetherUnlock(label.index));
            }
        }

        protected override void OnCreate()
        {
            transform.AddButton("Content/Bg/Close", Close);
            transform.AddButton("Content/Bg/Progress/FinalRoot/UIFinalReward/Icon", () =>
            {
                var final = SevenDayTaskRwdVisitor.Get(_activity.detailConfig.PointsRwd[^1]);
                UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, finalReward.transform.position, 50f, final.Reward);
            });
            foreach (var label in labels) { label.transform.AddButton("Unchose", () => _ChooseLabel(label.index)); }
        }

        private void _ChooseLabel(int index)
        {
            foreach (var label in labels)
            {
                if (label.index == index) { label.Choose(); }
                else { label.UnChoen(); }
            }
            _RefreshScroll(index);

        }

        private void _RefreshScroll(int index)
        {
            _curGroup = SevenDayTaskGroupVisitor.Get(_activity.detailConfig.TaskGroup[index]);
            var unlock = _activity.WhetherUnlock(index) && !_activity.WhetherPlayUnlock(index);
            while (_cells.Count < _curGroup.TaskInfo.Count)
            {
                var obj = Instantiate(cell, content).GetComponent<MBSevenDayTaskCell>();
                _cells.Add(obj);
                obj.transform.AddButton("CompleteBtn", () => ClickComplete(obj), false);
            }
            foreach (var cell in _cells)
            {
                cell.transform.localScale = Vector3.zero;
                cell.RefreshData(_curGroup.TaskInfo[_cells.IndexOf(cell)]);
                var canComplete = _activity.CheckTaskCanComplete(cell.info.Id);
                var hasComplete = _activity.CheckTaskHasComplete(cell.info.Id);
                cell.SetGroupId(_curGroup.Id);
                if (!unlock) { cell.SetLock(); }
                else if (hasComplete) { cell.animator.SetTrigger("Complete"); }
                else { cell.SetIdle(); }
                cell.RefreshState(canComplete, hasComplete);
                cell.RefreshProgress(Game.Manager.taskMan.FillTaskProgress(_activity, cell.info.TaskType), _activity.taskInfos[cell.info.Id]);
            }
            _SortCell();
            foreach (var cell in _cells)
            {
                cell.transform.localPosition = new(450, -_showCells.IndexOf(cell) * 160, 0);
            }
            StartCoroutine(_RefreshAnim(index));
        }

        private void _SortCell()
        {
            _showCells.Clear();
            _showCells.AddRange(_cells.OrderBy(x => !x.waitComplete).ThenBy(x => x.hasComplete).ThenBy(x => x.info.Sort));
        }

        private IEnumerator _RefreshAnim(int idx)
        {
            UIManager.Instance.Block(true);
            for (var i = 0; i < _showCells.Count; i++)
            {
                var cell = _showCells[i];
                cell.transform.DOScale(1, animationDuration).From(startScale);
                DOTween.To(() => cell.GetComponent<CanvasGroup>().alpha, x => cell.GetComponent<CanvasGroup>().alpha = x, 1, animationDuration).From(startAlpha);
                yield return new WaitForSeconds(animationDelay);
            }
            if (_activity.WhetherPlayUnlock(idx))
            {
                _activity.UnlockDay(idx);
                foreach (var cell in _showCells)
                {
                    cell.SetUnlock();
                    yield return new WaitForSeconds(UnlockInterval);
                }
                yield return new WaitForSeconds(1.5f);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }
            UIManager.Instance.Block(false);
        }

        private void ClickComplete(MBSevenDayTaskCell cell)
        {
            if (!_activity.Active) { return; }
            if (!_activity.TryCompleteTask(cell.info.Id, _curGroup)) { return; }
            cell.Claim(_activity.taskRewardCommitDatas);
            _activity.taskRewardCommitDatas.Clear();
            StartCoroutine(_SortAnim(cell));
            RefreshLabel();
        }

        private IEnumerator _SortAnim(MBSevenDayTaskCell cell)
        {
            UIManager.Instance.Block(true);
            yield return new WaitForSeconds(0.5f);
            var before = _showCells.IndexOf(cell);
            _SortCell();
            var now = _showCells.IndexOf(cell);
            if (before == now)
            {
                yield return new WaitForSeconds(1f);
                UIManager.Instance.Block(false);
                yield break;
            }
            else
            {
                cell.transform.DOScale(0, sortDuration);
                _showCells.Remove(cell);
                foreach (var item in _cells)
                {
                    if (item != cell)
                        item.transform.DOLocalMove(new(450, -_showCells.IndexOf(item) * 160, 0), sortDuration);
                }
                yield return new WaitForSeconds(sortDuration);
                _SortCell();
                cell.transform.DOScale(1, sortDuration);
                cell.transform.localPosition = new(450, -_showCells.IndexOf(cell) * 160, 0);
                foreach (var item in _cells)
                {
                    if (item != cell)
                        item.transform.DOLocalMove(new(450, -_showCells.IndexOf(item) * 160, 0), sortDuration);
                }
                yield return new WaitForSeconds(sortDuration);
                UIManager.Instance.Block(false);
            }
        }

        public void RefreshCD()
        {
            cd.text = UIUtility.CountDownFormat(_activity?.Countdown ?? 0);
            if (!_activity.Active)
            {
                if (!UIManager.Instance.IsBlocked) { Close(); }
            }
        }
    }
}