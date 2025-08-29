/**
 * @Author: zhangpengjian
 * @Date: 2025/7/24 14:56:50
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/24 14:56:50
 * Description: 耗体自选活动棋盘入口
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using System.Collections;
using DG.Tweening;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class UIActivityWishUponEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject group;
        [SerializeField] private TMP_Text cd;
        [SerializeField] private GameObject redGo;
        [SerializeField] private MBRewardProgress progress;
        [SerializeField] private TMP_Text addNum;
        [SerializeField] private TMP_Text addNumShow;
        [SerializeField] private Animator progressAnimator;
        [SerializeField] private Animator addNumAnimator;
        [SerializeField] private Animator animator;

        private int targetV;
        private float currentV;
        private int showAddNum;
        private Coroutine routine;

        private Action WhenCD;
        private ActivityWishUpon _activity;

        public void Start()
        {
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        public void OnEnable()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.WISH_UPON_ENERGY_UPDATE>().AddListener(Refresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);

        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.WISH_UPON_ENERGY_UPDATE>().RemoveListener(Refresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
        }

        /// <summary>
        /// 寻宝活动入口
        /// </summary>
        public void RefreshEntry(ActivityLike activity)
        {
            if (activity == null)
            {
                Visible(false);
                return;
            }
            if (activity is not ActivityWishUpon)
            {
                Visible(false);
                return;
            }
            _activity = (ActivityWishUpon)activity;
            var valid = _activity is { Valid: true};
            Visible(valid);
            if (!valid) return;
            RefreshCD();
            RefreshRedDot();
            progress.Refresh(_activity.EnergyCost, _activity.confD.Score);
            addNum.gameObject.SetActive(false);
            addNumShow.gameObject.SetActive(false);
            showAddNum = 0;
        }

        private void RefreshCD()
        {
            if (!group.activeSelf)
                return;
            var v = _activity.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
                Visible(false);
        }

        private void Visible(bool v_)
        {
            group.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }

        private void EntryClick()
        {
            _activity.Open();
        }

        private void Refresh(int oV_, int nV_)
        {
            Game.Manager.activity.LookupAny(EventType.WishUpon, out var activity);
            _activity = (ActivityWishUpon)activity;
            targetV = nV_;
            Visible(true);
            var change = nV_ - oV_;
            showAddNum += change;
            addNumShow.text = "+" + showAddNum;
            addNumShow.gameObject.SetActive(true);
            addNum.gameObject.SetActive(false);
            currentV = nV_ - showAddNum;
            if (routine != null)
            {
                StopCoroutine(routine);
            }
            routine = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            yield return new WaitForSeconds(0.5f);
            ProgressEffect();
            progress.Refresh(targetV, _activity.confD.Score, 0.5f, ()=>
            {
                RefreshRedDot();
            });
            currentV = targetV;
            yield return new WaitForSeconds(1.5f);
            routine = null;
        }

        private void ProgressEffect()
        {
            animator.SetTrigger("Punch");
            progressAnimator.SetTrigger("Punch");
            addNumAnimator.SetTrigger("Punch");
            addNumShow.gameObject.SetActive(false);
            addNum.text = "+" + showAddNum;
            addNum.gameObject.SetActive(true);
            showAddNum = 0;
        }

        private void RefreshRedDot()
        {
            redGo.SetActive(_activity.EnergyCost >= _activity.confD.Score);
        }
    }
}