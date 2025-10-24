// ===================================================
// Author: mengqc
// Date: 2025/09/02
// ===================================================

using System;
using DG.Tweening;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIVineLeapEntry : MonoBehaviour, IActivityBoardEntry
    {
        public GameObject root;
        public Slider progress;
        public TextMeshProUGUI tfProgress;
        public TextMeshProUGUI tfMember;
        public TextMeshProUGUI tfLimit;
        public TextMeshProUGUI tfCd;
        public GameObject dot;
        public GameObject texts;
        public UIImageRes tokenIcon;
        public Animator entryAnime;
        public Animator tfProgressAnime;
        public Animator progressAreaAnime;
        public Animator iconHitAnime;

        private ActivityVineLeap _activity;
        private Tween _progressTween;
        private int _curScore;
        private int _targetScore;

        public void Start()
        {
            var button = transform.Find("Root/Bg").GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(OnEntryClick);
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(OnTick);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(OnFlyIconFeedBack);
            MessageCenter.Get<MSG.VINELEAP_STEP_START>().AddListener(OnStepStart);
            MessageCenter.Get<MSG.VINELEAP_STEP_END>().AddListener(OnStepEnd);
            RefreshEntry(_activity);
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnTick);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(OnFlyIconFeedBack);
            MessageCenter.Get<MSG.VINELEAP_STEP_START>().RemoveListener(OnStepStart);
            MessageCenter.Get<MSG.VINELEAP_STEP_END>().RemoveListener(OnStepEnd);
        }

        private void OnTick()
        {
            RefreshCd();
        }

        private void OnStepStart()
        {
            RefreshEntry(_activity);
        }

        private void OnStepEnd(bool isWin)
        {
            RefreshEntry(_activity);
        }

        private void RefreshCd()
        {
            if (_activity.IsCurStepRunning())
            {
                tfMember.text = _activity.GetSeatsLeft().ToString();
            }

            UIUtility.CountDownFormat(tfCd, _activity.Countdown);
        }

        private void OnEntryClick()
        {
            if (!_activity.Active)
                return;
            _activity.Open();
        }

        private int GetTargetScore()
        {
            return _activity.GetCurLevelTargetScore();
        }

        public void RefreshEntry(ActivityLike activity = null)
        {
            tfProgress.color = new Color(1, 1, 1, 0);
            tfProgressAnime.enabled = false;
            ClearProgressTween();
            _activity = activity as ActivityVineLeap;
            if (_activity == null)
            {
                Visible(false);
                return;
            }

            if (!_activity.Active)
            {
                Visible(false);
                return;
            }

            Visible(true);

            if (_activity.IsCurStepRunning())
            {
                var tokenCfg = Game.Manager.objectMan.GetBasicConfig(_activity.TokenId);
                tokenIcon.SetImage(tokenCfg.Icon);
                progress.gameObject.SetActive(true);
                _curScore = _activity.GetTokenNum();
                _targetScore = _curScore;
                tfProgress.text = $"{_curScore}/{GetTargetScore()}";
                progress.value = (float)_curScore / GetTargetScore();
                dot.SetActive(false);
                tfLimit.gameObject.SetActive(true);
                entryAnime.SetTrigger("Idle");
            }
            else
            {
                tfLimit.gameObject.SetActive(false);
                tfMember.text = "YOU";
                dot.SetActive(true);
                progress.gameObject.SetActive(false);
                entryAnime.SetTrigger("Sleep");
            }

            RefreshCd();
        }

        private void Visible(bool v_)
        {
            root.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }

        private void ClearProgressTween()
        {
            if (_progressTween != null)
            {
                _progressTween.Kill();
                _progressTween = null;
            }
        }

        private void OnFlyIconFeedBack(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.VineLeapToken) return;
            ClearProgressTween();
            var old = _targetScore;
            var delta = item.Amount / item.SplitNum;
            if (item.CurIdx >= item.SplitNum)
            {
                _targetScore += Mathf.Max(0, item.Amount - delta * Mathf.Max(0, item.SplitNum - 1));
            }
            else
            {
                _targetScore += delta;
            }

            tfProgressAnime.enabled = true;
            tfProgressAnime?.SetTrigger("Punch");
            iconHitAnime?.SetTrigger("Punch");
            _progressTween = DOTween.To(() => _curScore, (v) =>
            {
                _curScore = v;
                tfProgress.text = $"{_curScore}/{GetTargetScore()}";
                progress.value = (float)_curScore / GetTargetScore();
            }, _targetScore, 0.2f).OnComplete(() =>
            {
                if (_activity.IsTokenFull())
                {
                    entryAnime.SetTrigger("Punch");
                }
                else
                {
                    progressAreaAnime.SetTrigger("Punch");
                }

                _progressTween = null;
            });
        }
    }
}