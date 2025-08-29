// ==================================================
// // File: MBBingoTaskItem.cs
// // Author: liyueran
// // Date: 2025-07-16 11:07:46
// // Desc: bingoTask 格子Item
// // ==================================================

using System;
using DG.Tweening;
using EL;
using FAT.MSG;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBBingoTaskItem : MonoBehaviour
    {
        private class PreviewCellData
        {
            public int Score;
            public BingoState State;
        }

        [SerializeField] private GameObject cover;
        [SerializeField] private GameObject readyButton;
        [SerializeField] private CanvasGroup readyButtonCanvasGroup;
        [SerializeField] private GameObject progress;

        [SerializeField] private UIImageState _readyBg;
        [SerializeField] private UIImageRes _icon;
        [SerializeField] private RectTransform _mask;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private Image _commitImg;
        [SerializeField] private Image _bingoImg;
        [SerializeField] private Animator _animator;

        private ActivityBingoTask _activity;
        private BingoTaskCell _cell;
        private MBBingoTaskBoard _board;

        private PreviewCellData _previewCellData;

        #region Mono
        private void Start()
        {
            AddButton();
        }


        private void AddButton()
        {
            transform.AddButton("Content/ReadyBtn", OnClickCommit).WithClickScale().FixPivot();
            transform.AddButton("cover", OnClickCover);
            transform.AddButton("Content/Icon", OnClickItem);
        }

        private void OnEnable()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }


        private void OnDisable()
        {
            RecordPreView();

            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }
        #endregion

        public void Init(ActivityBingoTask activity, MBBingoTaskBoard board, BingoTaskCell cell)
        {
            this._activity = activity;
            this._cell = cell;
            this._board = board;

            RefreshTheme();

            RefreshView();
        }

        public void OnPostOpen()
        {
            if (_previewCellData == null)
            {
                SetCellView();
            }

            // 记录数据
            RecordPreView();
        }

        #region 状态控制
        public void RefreshView()
        {
            if (_previewCellData == null)
            {
                SetCellView();
            }
            else
            {
                UpdateView();
            }
        }

        private void SetCellView()
        {
            switch (_cell.state)
            {
                case BingoState.UnFinished:
                    SetCellUnFinished();
                    break;
                case BingoState.ToBeCompleted:
                    SetCellToBeCompleted();
                    break;
                case BingoState.Completed:
                    SetCellCompleted();
                    break;
                case BingoState.Bingo:
                    SetCellBingo();
                    break;
                case BingoState.Special:
                    SetCellLock();
                    break;
                default:
                    SetCellUnFinished();
                    break;
            }
        }

        private void UpdateView()
        {
            // 未完成状态
            if (_cell.state == BingoState.UnFinished)
            {
                // 从其他到未完成 或 状态没有变化
                if (_previewCellData.State != BingoState.UnFinished || _previewCellData.Score == _cell.score)
                {
                    // 上一个活动结束 新活动开始时 会把_previewCellData state 记录

                    // 动画完成后设置状态
                    SetCellUnFinished();
                }
                // 从未完成到未完成
                else if (_previewCellData.State == BingoState.UnFinished)
                {
                    _board.Main.SetBlock(true);

                    // 关闭不是UnFinished状态的 UI
                    readyButton.SetActive(false);
                    _commitImg.gameObject.SetActive(false);
                    _bingoImg.gameObject.SetActive(false);
                    cover.SetActive(false);

                    // 进度文本
                    _progressText.text = $"{_previewCellData.Score}/{_cell.target}";
                    // 进度条动画
                    var from = CalculateProgress(_previewCellData.Score);
                    var to = CalculateProgress(_cell.score);
                    PlayProgressAnim(from, to, () =>
                    {
                        _progressText.text = $"{_cell.score}/{_cell.target}";
                        SetCellUnFinished();
                        _board.Main.SetBlock(false);
                    });
                }
            }
            // 待提交状态
            else if (_cell.state == BingoState.ToBeCompleted)
            {
                // 从未完成到待提交
                if (_previewCellData.State == BingoState.UnFinished)
                {
                    readyButton.SetActive(false);
                    _board.Main.SetBlock(true);
                    // 进度文本
                    _progressText.text = $"{_previewCellData.Score}/{_cell.target}";
                    // 进度条动画
                    var from = CalculateProgress(_previewCellData.Score);
                    var to = CalculateProgress(_cell.score);
                    PlayProgressAnim(from, to, () =>
                    {
                        readyButton.SetActive(true);
                        _progressText.text = $"{_cell.score}/{_cell.target}";
                        // 按钮出现动画
                        PlayReadyAnim(() =>
                        {
                            SetCellToBeCompleted();
                            _board.Main.SetBlock(false);
                        });
                    });
                }
                else
                {
                    SetCellToBeCompleted();
                }
            }
            else
            {
                // 已完成/Bingo/未解锁
                SetCellView();
            }
        }

        private void SetCellUnFinished()
        {
            progress.SetActive(true);
            SetProgress();

            readyButton.SetActive(false);
            readyButtonCanvasGroup.alpha = 0;
            _commitImg.gameObject.SetActive(false);
            _bingoImg.gameObject.SetActive(false);
            cover.SetActive(false);
        }

        private void SetProgress()
        {
            var value = CalculateProgress(_cell.score);
            _mask.sizeDelta = new Vector2(value, _mask.sizeDelta.y);
            _progressText.text = $"{_cell.score}/{_cell.target}";
        }

        private void SetCellToBeCompleted()
        {
            _animator.SetTrigger("CompleteIdle");
            progress.SetActive(false);
            readyButton.SetActive(true);
            readyButtonCanvasGroup.alpha = 1;
            _commitImg.gameObject.SetActive(false);
            _bingoImg.gameObject.SetActive(false);
            cover.SetActive(false);
        }

        private void SetCellCompleted()
        {
            _animator.SetTrigger("YellowIdle");
            progress.SetActive(false);
            readyButton.SetActive(false);
            readyButtonCanvasGroup.alpha = 0;
            _commitImg.gameObject.SetActive(true);
            _bingoImg.gameObject.SetActive(false);
            cover.SetActive(false);
        }

        private void SetCellBingo()
        {
            _animator.SetTrigger("PurpleIdle");
            progress.SetActive(true);
            readyButton.SetActive(false);
            readyButtonCanvasGroup.alpha = 0;
            _commitImg.gameObject.SetActive(true);
            _bingoImg.gameObject.SetActive(true);
            _bingoImg.color = new Color(_bingoImg.color.r, _bingoImg.color.g, _bingoImg.color.b, 1);
            cover.SetActive(false);
        }

        private void SetCellLock()
        {
            progress.SetActive(true);
            SetProgress();
            readyButton.SetActive(false);
            readyButtonCanvasGroup.alpha = 0;
            _commitImg.gameObject.SetActive(false);
            _bingoImg.gameObject.SetActive(false);
            cover.SetActive(true);
        }

        // 判断当前的UI状态是不是bingo
        private bool NeedPlayBingoView()
        {
            var checkCover = cover.activeSelf;
            var checkBingo = _bingoImg.gameObject.activeSelf;

            if (checkBingo)
            {
                return false;
            }

            return !checkCover;
        }
        #endregion

        #region 动画
        // 播放进度条动画
        private void PlayProgressAnim(float from, float to, Action onComplete = null)
        {
            _mask.sizeDelta = new Vector2(from, _mask.sizeDelta.y);

            // 进度条动画
            DOTween.To(() => _mask.sizeDelta, x => _mask.sizeDelta = x,
                    new Vector2(to, _mask.sizeDelta.y), 0.5f)
                .OnUpdate(() =>
                {
                    var max = _mask.parent.GetComponent<RectTransform>().rect.width;
                    var percent = _mask.sizeDelta.x / max;
                    _progressText.text = $"{(int)(_cell.target * percent)}/{_cell.target}";
                })
                .OnComplete(() => { onComplete?.Invoke(); });
        }

        // 播放按钮出现动画
        private void PlayReadyAnim(Action onComplete = null)
        {
            readyButton.transform.localScale = Vector3.one * 0.8f;
            readyButtonCanvasGroup.alpha = 0;
            readyButton.gameObject.SetActive(true);
            readyButtonCanvasGroup.blocksRaycasts = false;

            // complete
            var seq = DOTween.Sequence();
            seq.AppendCallback(() => { _animator.SetTrigger("Complete"); });
            seq.AppendInterval(0.7f);
            seq.OnComplete(() =>
            {
                onComplete?.Invoke();
                readyButtonCanvasGroup.blocksRaycasts = true;
            });
            seq.Play();
        }

        // 播放提交动画
        public void PlayCommitAnim(Sequence seq, Action onComplete = null, Action onReady = null)
        {
            _commitImg.color = new Color(_commitImg.color.r, _commitImg.color.g, _commitImg.color.b, 0);
            _commitImg.gameObject.SetActive(true);
            _animator.ResetTrigger("Yellow");

            // yellow
            seq.AppendCallback(() => { onReady?.Invoke(); });
            seq.AppendCallback(() =>
            {
                _animator.SetTrigger("Yellow");
                Game.Manager.audioMan.TriggerSound("BingoTaskComplete");
            });
            seq.AppendCallback(() => { onComplete?.Invoke(); });
        }

        // 播放bingo动画
        public Sequence PlayBingoAnim(Action onComplete = null, Action onReady = null, float afterReady = 0,
            float beforeComplete = 0)
        {
            var seq = DOTween.Sequence();

            seq.AppendCallback(() =>
            {
                // 初始化状态 bingo动画准备
                _animator.ResetTrigger("Purple");
                _bingoImg.color = new Color(_bingoImg.color.r, _bingoImg.color.g, _bingoImg.color.b, 0);
            });

            // purple
            seq.AppendCallback(() => { onReady?.Invoke(); });

            seq.AppendInterval(afterReady);

            if (NeedPlayBingoView())
            {
                seq.AppendCallback(() =>
                {
                    // 播放动画
                    _bingoImg.gameObject.SetActive(true);
                    _animator.SetTrigger("Purple");
                });
            }

            seq.AppendInterval(beforeComplete);

            seq.AppendCallback(() => { onComplete?.Invoke(); });
            return seq;
        }
        #endregion


        private void RecordPreView()
        {
            if (_cell == null)
            {
                return;
            }

            _previewCellData ??= new();

            _previewCellData.Score = _cell.score;
            _previewCellData.State = _cell.state;
        }

        private float CalculateProgress(int value)
        {
            var percent = value * 1f / _cell.target;
            var max = _mask.parent.GetComponent<RectTransform>().rect.width;
            var result = (percent) * max;
            return Mathf.Clamp(result, 0f, max);
        }


        #region 事件
        // 点击提交按钮
        private void OnClickCommit()
        {
            var bingoResult = _activity.TryCompleteBingoCell(_cell.taskInfo.Index);
            if (bingoResult == BingoResult.None)
            {
                // 提交失败
                return;
            }

            MessageCenter.Get<UI_BINGO_TASK_COMPLETE_ITEM>().Dispatch(bingoResult, _cell.taskInfo.Index);
        }

        // 点击遮罩
        private void OnClickCover()
        {
            _board.Effect_CoverClick(transform.position);
        }

        // 点击单元格
        private void OnClickItem()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIBingoTaskItemTips, transform.position, 0f, _cell);
        }

        // 活动结束
        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != _activity)
            {
                return;
            }

            _previewCellData = null;
        }
        #endregion


        private void RefreshTheme()
        {
            _icon.SetImage(_cell.taskInfo.IconShow);
            SetReadyBg(_cell.taskInfo.Index);
        }

        // 设置单元格背景
        private void SetReadyBg(int index)
        {
            // 转换为0-based
            var row = (index) / 4; // 0-3行
            var col = (index) % 4; // 0-3列
            var value = (row + col) % 2;
            _readyBg.Select(value);
        }
    }
}