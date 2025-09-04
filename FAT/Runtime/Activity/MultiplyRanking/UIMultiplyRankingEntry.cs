/*
 * @Author: yanfuxing
 * @Date: 2025-07-18 11:20:05
 */
using System;
using Cysharp.Threading.Tasks;
using EL;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMultiplyRankingEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _redPointNum;
        [SerializeField] private TextMeshProUGUI _rankNum;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private Button _entryBtn;
        [SerializeField] private GameObject _redPoint;
        [SerializeField] private Animator _animator;
        [SerializeField] private Button TurntableBtn;
        [SerializeField] private Transform TurntableTrans;
        [SerializeField] private Transform TurntableArrow;
        [SerializeField] private TextMeshProUGUI TurntableCdText;
        [SerializeField] private Transform _highSlotFx;
        [SerializeField] private UIStateGroup _rankStateGroup;
        [SerializeField] private Animator _arrowAnimator;
        [SerializeField] private Animator _timeFxAnimator;
        private ActivityMultiplierRanking _activity;
        private int _defaultSlotNum = 1; //默认档位
        private bool _isHadRefresh;
        private bool _blockRefresh;

        void Awake()
        {
            _entryBtn.onClick.AddListener(OnEntryClick);
            TurntableBtn.onClick.AddListener(OnTurntableClick);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FeedBack);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshTurntableCD);
            MessageCenter.Get<MSG.MULTIPLY_RANKING_ENTRY_REFRESH_RED_DOT>().AddListener(RefreshRedDot);
            MessageCenter.Get<MSG.MULTIPLY_RANKING_BLOCK_ENTRY_UPDATE>().AddListener(_BlockRefresh);
            MessageCenter.Get<MSG.MULTIPLY_RANKING_RANKING_CHANGE>().AddListener(_RefreshRanking);
        }

        private void _BlockRefresh()
        {
            _blockRefresh = true;
        }

        private void _RefreshRanking()
        {
            if (_blockRefresh) { return; }
            RefreshRankNumState();
        }

        public void RefreshEntry(ActivityLike activity)
        {
            _blockRefresh = false;
            RefreshRankingEntry(activity);
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
            var multiplierIndex = _activity.GetMultiplierIndex();
            int slotNum = multiplierIndex + 1;
            UniTask.Void(async () =>
            {
                await UniTask.WaitUntil(() => gameObject != null && gameObject.activeInHierarchy);
                RefreshTurntable(slotNum);
                _isHadRefresh = true;
            });
        }

        private void RefreshRankingEntry(ActivityLike activity)
        {
            if (activity == null || activity.Type != fat.rawdata.EventType.MultiplierRanking) return;
            _activity = activity as ActivityMultiplierRanking;
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
            _redPointNum.text = _activity.GetTokenNum().ToString();
            _redPoint.SetActive(_activity.GetTokenNum() > 0);
            RefreshRankNumState();
        }

        private void RefreshCD()
        {
            if (_activity == null) return;
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
        }

        private void FeedBack(FlyableItemSlice slice)
        {
            if (_activity == null) return;
            if (slice.FlyType != FlyType.MultiRankingToken) return;
            if (slice.CurIdx != 1) return;
            _blockRefresh = false;
            RefreshTurntableCD();
            if (_redPointNum != null)
            {
                _redPointNum.text = _activity.GetTokenNum().ToString();
            }
            if (_redPoint != null)
            {
                _redPoint.SetActive(_activity.GetTokenNum() > 0);
            }
            RefreshRankNumState();
            if (_animator != null)
            {
                _animator.SetTrigger("Punch");
            }
            var multiplierIndex = _activity.GetMultiplierIndex();
            int slotNum = multiplierIndex + 1;
            //判断是否发生变化
            RefreshTurntable(slotNum, true);
            if (_timeFxAnimator != null)
            {
                _timeFxAnimator.SetTrigger("Punch");
            }
        }

        private void RefreshTurntableCD()
        {
            if (_blockRefresh) return;
            if (_activity == null) return;
            var timeList = _activity.conf.MultiplierDur;
            if (timeList.Count > 0)
            {
                if (_activity.GetLeftMultiplierResetTime() <= 0)
                {
                    TurntableCdText.text = I18N.Text("#SysComDesc1482");
                    if (!_isHadRefresh)
                    {
                        _isHadRefresh = true;
                        RefreshTurntable(_defaultSlotNum, true);
                    }
                }
                else
                {
                    _isHadRefresh = false;
                    if (TurntableCdText != null)
                    {
                        UIUtility.CountDownFormat(TurntableCdText, _activity.GetLeftMultiplierResetTime());
                    }
                }
            }
        }

        private void OnEntryClick()
        {
            if (_activity == null) return;
            _activity.Open();
        }

        private void OnTurntableClick()
        {
            var itemRect = TurntableTrans.transform as RectTransform;
            var itemHeight = itemRect.rect.height * 0.5f;
            var tokenId = _activity.conf.Token;
            var str = UIUtility.FormatTMPString(tokenId);
            UIManager.Instance.OpenWindow(UIConfig.UIRankingEntryTips, TurntableTrans.position, itemHeight, str);
        }

        private void RefreshTurntable(int multiplierNum, bool isChasing = false)
        {
            RankingUIUtility.RefreshTurntableByNum(multiplierNum, TurntableTrans, isChasing);
            RankingUIUtility.PointerToSlot(multiplierNum, TurntableTrans, TurntableArrow, action: () => _arrowAnimator.enabled = multiplierNum >= TurntableTrans.childCount);
            RefreshHighSlotFx(multiplierNum);
        }

        private void RefreshHighSlotFx(int slotNum)
        {
            _highSlotFx.gameObject.SetActive(slotNum >= TurntableTrans.childCount);
        }

        private void RefreshRedDot()
        {
            if (_activity == null) return;
            _redPoint.SetActive(_activity.GetTokenNum() > 0);
        }

        private void RefreshRankNumState()
        {
            if (_activity == null) return;
            var rankNum = _activity.CurRank;
            var stateIndex = rankNum <= 3 ? rankNum - 1 : 3;
            _rankStateGroup.Select(stateIndex);
            _rankNum.text = _activity.CurRank.ToString();
        }

        private void Visible(bool v_)
        {
            _root.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }

        void OnDestroy()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FeedBack);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshTurntableCD);
            MessageCenter.Get<MSG.MULTIPLY_RANKING_ENTRY_REFRESH_RED_DOT>().RemoveListener(RefreshRedDot);
            MessageCenter.Get<MSG.MULTIPLY_RANKING_BLOCK_ENTRY_UPDATE>().RemoveListener(_BlockRefresh);
            MessageCenter.Get<MSG.MULTIPLY_RANKING_RANKING_CHANGE>().RemoveListener(_RefreshRanking);
        }
    }
}