/*
 * @Author: qun.chao
 * @Date: 2025-03-04 11:43:00
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using FAT.Merge;
using Cysharp.Threading.Tasks;

namespace FAT
{
    public class UIBingoEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private UICommonItem finalReward;
        [SerializeField] private TextMeshProUGUI txtCD;
        [SerializeField] private GameObject goFlag;
        [SerializeField] private UICommonProgressBar progressBar;
        [SerializeField] private Animator animator;
        [SerializeField] private float time_punch = 0.75f;
        [SerializeField] private GameObject goNormal;
        [SerializeField] private GameObject goNotReady;
        [SerializeField] private TextMeshProUGUI txtCD_NotReady;

        private ActivityBingo actInst;

        public void Start()
        {
            transform.Access<Button>().WithClickScale().onClick.AddListener(OnClick);
        }

        public void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.BINGO_ITEM_COMPLETE_DIRTY>().AddListener(RefreshFlag);
            MessageCenter.Get<MSG.BINGO_ITEM_MAP_UPDATE>().AddListener(RefreshFlag);
            MessageCenter.Get<MSG.BINGO_PROGRESS_UPDATE>().AddListener(OnMessageBingoChange);
            MessageCenter.Get<MSG.BINGO_ENTER_NEXT_ROUND>().AddListener(OnMessageBingoChange);
            MessageCenter.Get<MSG.UI_BOARD_USE_ITEM>().AddListener(OnMessageBoardItemUse);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.BINGO_ITEM_COMPLETE_DIRTY>().RemoveListener(RefreshFlag);
            MessageCenter.Get<MSG.BINGO_ITEM_MAP_UPDATE>().RemoveListener(RefreshFlag);
            MessageCenter.Get<MSG.BINGO_PROGRESS_UPDATE>().RemoveListener(OnMessageBingoChange);
            MessageCenter.Get<MSG.BINGO_ENTER_NEXT_ROUND>().RemoveListener(OnMessageBingoChange);
            MessageCenter.Get<MSG.UI_BOARD_USE_ITEM>().RemoveListener(OnMessageBoardItemUse);
        }

        public void RefreshEntry(ActivityLike activity)
        {
            actInst = activity as ActivityBingo;
            RefreshGroup();
            RefreshProgress();
            RefreshReward();
            RefreshCD();
            RefreshFlag();
        }

        private void RefreshGroup()
        {
            if (actInst.GetBingoTotalNum() > 0)
            {
                goNormal.SetActive(true);
                goNotReady.SetActive(false);
            }
            else
            {
                goNormal.SetActive(false);
                goNotReady.SetActive(true);
            }
        }

        private void RefreshProgress()
        {
            progressBar.ForceSetup(0, actInst.GetBingoTotalNum(), actInst.GetBingoCount());
        }

        private void RefreshReward()
        {
            var (_, _, all) = ItemBingoUtility.GetBoardRewardInfo(actInst.ConfBingoID);
            finalReward.Refresh(all.Id, all.Count);
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(txtCD, actInst.Countdown);
            UIUtility.CountDownFormat(txtCD_NotReady, actInst.Countdown);
        }

        private void RefreshFlag()
        {
            if (goFlag == null || animator == null || actInst == null)
                return;
            goFlag.SetActive(actInst.CheckBingoComplete());
            animator.SetTrigger(goFlag.activeSelf ? "Sweep" : "Idle");
        }

        private void OnClick()
        {
            actInst?.Open();
        }

        private void OnMessageBoardItemUse(Item item)
        {
            if (actInst.CheckIndicator(item.tid, out _) != ItemIndType.Bingo)
                return;
            animator.SetTrigger("Punch");
            UniTask.Void(async () =>
            {
                await UniTask.WaitForSeconds(time_punch);
                RefreshFlag();
            });
        }

        private void OnMessageBingoChange()
        {
            RefreshGroup();
            RefreshProgress();
        }
    }
}