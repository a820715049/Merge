/*
 *@Author:chaoran.zhang
 *@Desc:热气球活动入口
 *@Created Time:2024.07.15 星期一 10:02:53
 */

using System;
using System.Collections;
using EL;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class RaceEntry : MonoBehaviour
    {
        public GameObject startBtn;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private SkeletonGraphic _skeleton;
        [SerializeField] private Animator _animator;
        [SerializeField] private LayoutElement _element;
        [SerializeField] private UIImageState _numBg;
        [SerializeField] private UITextState _numState;
        [SerializeField] private Animator _numAnim;
        [SerializeField] private float _wait;
        [SerializeField] private float _start;
        private Coroutine _animCoroutine = null;
        private int _lastNum = 0;
        private int _interval = 0;
        private int _lastAnimNum = -1;
        private bool _isPlayNumAnim;
        private float _numAnimTime = 0f;

        public void Start()
        {
            var button = GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        private void EntryClick()
        {
            if (RaceManager.GetInstance().Race == null) return;
            if (RaceManager.GetInstance().Race.Block) return;
            if (!RaceManager.GetInstance().Race.HasStartRound)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIRaceStart);
                return;
            }
            else
            {
                UIManager.Instance.OpenWindow(UIConfig.UIRacePanel);
            }
            _lastNum = RaceManager.GetInstance().GetNum();
            _num.text = _lastNum.ToString();
            _numBg.Select(_lastNum - 1);
            _numState.Select(_lastNum - 1);
        }

        public void OnEnable()
        {
            //根据数据刷新UI
            RefreshEntry();
            MessageCenter.Get<MSG.ACTIVITY_ACTIVE>().AddListener(RefreshEntry);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(RefreshEntry);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.RACE_ROUND_START>().AddListener(StartRound);
            MessageCenter.Get<MSG.FLY_ICON_START>().AddListener(CheckNewFly);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(UpdateText);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.ACTIVITY_ACTIVE>().RemoveListener(RefreshEntry);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.RACE_ROUND_START>().RemoveListener(StartRound);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(RefreshEntry);
            MessageCenter.Get<MSG.FLY_ICON_START>().RemoveListener(CheckNewFly);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(UpdateText);
        }

        private void RefreshEntry(ActivityLike act = null, bool isNew = false)
        {
            if (RaceManager.GetInstance().Race == null)
            {
                Visible(false);
                return;
            }

            Visible(true);
            startBtn.SetActive(!RaceManager.GetInstance().Race.HasStartRound);
            //刷新倒计时
            RefreshCD();
            _lastNum = RaceManager.GetInstance().GetNum();
            _num.text = _lastNum.ToString();
            _numBg.Select(_lastNum - 1);
            _numState.Select(_lastNum - 1);
            _lastAnimNum = RaceManager.GetInstance().Race.Score;
            _numAnim.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _lastAnimNum.ToString();
        }

        private void StartRound(bool start)
        {
            RefreshEntry();
            if (!start)
                return;
            _lastAnimNum = RaceManager.GetInstance().Race.Score;
            _numAnim.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _lastAnimNum.ToString();
        }

        private void RefreshCD()
        {
            if (RaceManager.GetInstance().Race == null)
                return;
            _interval++;
            if (_interval >= RaceManager.GetInstance().Race.ConfD.RefreshTime)
            {
                //刷新排名
                if (_lastNum != RaceManager.GetInstance().GetNum())
                {
                    if (_lastNum > RaceManager.GetInstance().GetNum())
                    {
                        Game.Manager.audioMan.TriggerSound("HotAirGetPointUp");
                        _animator.SetTrigger("Punch");
                    }
                    else
                    {
                        Game.Manager.audioMan.TriggerSound("HotAirGetPointDown");
                        _animator.SetTrigger("down");
                    }
                }

                _lastNum = RaceManager.GetInstance().GetNum();
                _num.text = _lastNum.ToString();
                _numBg.Select(_lastNum - 1);
                _numState.Select(_lastNum - 1);
                _interval = 0;
                if (!_isPlayNumAnim)
                {
                    _numAnim.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _lastAnimNum.ToString();
                }
            }

            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, RaceManager.GetInstance().Race.endTS - t);
            UIUtility.CountDownFormat(_cd, diff);
            if (diff == 0) Visible(false);
        }

        private void Visible(bool v_)
        {
            for (var i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(v_);

            _element.ignoreLayout = !v_;
            GetComponent<NonDrawingGraphic>().raycastTarget = v_;
        }

        private IEnumerator PlaySpine(FlyableItemSlice item)
        {
            _isPlayNumAnim = true;
            _numAnimTime = 0.6f + UIFlyConfig.Instance.durationScatter +
                           UIFlyConfig.Instance.durationAdd * (item.SplitNum - 1) + _start;
            _numAnim.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _lastAnimNum.ToString();
            yield return new WaitForSeconds(UIFlyConfig.Instance.durationScatter +
                                            UIFlyConfig.Instance.durationAdd * (item.SplitNum - 1) + _start);
            _numAnim.SetTrigger("Punch");
            yield return new WaitForSeconds(0.3f);
            _numAnim.SetFloat("Speed", 0f);
            yield return new WaitUntil(() => !_isPlayNumAnim);
            _numAnim.SetFloat("Speed", 1f);
            yield return new WaitForSeconds(_wait);
            _skeleton.AnimationState.SetAnimation(0, "active", false).Complete += delegate (TrackEntry entry)
            {
                _skeleton.AnimationState.SetAnimation(0, "idle", false);
            };
            if (_lastNum != RaceManager.GetInstance().GetNum())
            {
                if (_lastNum > RaceManager.GetInstance().GetNum())
                {
                    Game.Manager.audioMan.TriggerSound("HotAirGetPointUp");
                    _animator.SetTrigger("Punch");
                }
                else
                {
                    Game.Manager.audioMan.TriggerSound("HotAirGetPointDown");
                    _animator.SetTrigger("down");
                }

                _lastNum = RaceManager.GetInstance().GetNum();
                _num.text = _lastNum.ToString();
                _numBg.Select(_lastNum - 1);
                _numState.Select(_lastNum - 1);
            }

            yield return new WaitForSeconds(0.3f);
            //RaceManager.GetInstance().Race?.TryShowStartNew();
            _numAnim.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _lastAnimNum.ToString();
            _animCoroutine = null;
        }

        private void Update()
        {
            if (_numAnimTime > 0)
                _numAnimTime -= Time.deltaTime;
            if (_numAnimTime <= 0)
                _isPlayNumAnim = false;
        }

        private void CheckNewFly(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.RaceToken)
                return;
            if (!_isPlayNumAnim && _animCoroutine == null)
            {
                _animCoroutine = Game.Instance.StartCoroutineGlobal(PlaySpine(item));
                return;
            }

            _numAnimTime += UIFlyConfig.Instance.durationFly + UIFlyConfig.Instance.durationAdd * (item.SplitNum - 1);
        }

        private void UpdateText(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.RaceToken)
                return;
            if (item.CurIdx >= item.SplitNum)
            {
                _lastAnimNum += item.Amount -
                                item.Amount / item.SplitNum * (item.SplitNum - 1);
                _numAnim.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _lastAnimNum.ToString();
            }
            else
            {
                _lastAnimNum += item.Amount / item.SplitNum;
                _numAnim.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _lastAnimNum.ToString();
            }
        }
    }
}
