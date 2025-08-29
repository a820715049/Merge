/*
 * @Author: qun.chao
 * @Date: 2025-07-22 16:11:38
 */
using EL;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace FAT
{
    public class UIClawOrderEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private TextMeshProUGUI txtCD;
        [SerializeField] private UICommonProgressBar progressBar;
        [SerializeField] private GameObject goDot;
        [SerializeField] private Animator ani;

        private ActivityClawOrder actInst;

        public void Start()
        {
            transform.Access<Button>().WithClickScale().onClick.AddListener(OnClick);
        }

        public void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.CLAWORDER_TOKEN_COMMIT>().AddListener(OnTokenCommitted);
            MessageCenter.Get<MSG.CLAWORDER_CHANGE>().AddListener(RefreshDot);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.CLAWORDER_TOKEN_COMMIT>().RemoveListener(OnTokenCommitted);
            MessageCenter.Get<MSG.CLAWORDER_CHANGE>().RemoveListener(RefreshDot);
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(txtCD, actInst.Countdown);
        }

        private void RefreshDot()
        {
            if (!goDot.activeSelf)
            {
                // 有未使用的抽奖次数
                if (actInst.CalcTotalDrawChanceCountByToken(actInst.CurToken) > actInst.DrawAttemptCount)
                {
                    goDot.SetActive(true);
                }
            }
            else
            {
                if (actInst.CalcTotalDrawChanceCountByToken(actInst.CurToken) <= actInst.DrawAttemptCount)
                {
                    goDot.SetActive(false);
                }
            }
        }

        private void RefreshProgress()
        {
            var nowToken = actInst.DisplayToken;
            var nextToken = actInst.FindNextTokenMilestone(nowToken);
            progressBar.ForceSetup(0, nextToken, nowToken);
        }

        private void OnTokenCommitted()
        {
            RefreshProgress();
            RefreshDot();
            ani.SetTrigger("Punch");
        }

        private void OnClick()
        {
            actInst?.Open();
        }

        void IActivityBoardEntry.RefreshEntry(ActivityLike activity)
        {
            actInst = activity as ActivityClawOrder;
            RefreshProgress();
            RefreshCD();
            RefreshDot();
        }
    }
}