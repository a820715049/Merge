/*
 * @Author: qun.chao
 * @Date: 2025-03-06 10:49:27
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class MBBingoProgress : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtRound;
        [SerializeField] private UICommonProgressBar progressBar;
        [SerializeField] private UICommonItem finalRewardItem;
        [SerializeField] private GameObject goCheck;
        [SerializeField] private Button btnSpawnerInfo;

        private UIBingoMain uiMain;
        private ActivityBingo actInst => uiMain.ActInst;

        private void Awake()
        {
            btnSpawnerInfo.onClick.AddListener(OnBtnClickSpawnerInfo);
        }

        public void InitOnPreOpen(UIBingoMain main)
        {
            uiMain = main;
            MessageCenter.Get<MSG.BINGO_PROGRESS_UPDATE>().AddListener(OnMessageBingoProgressChange);
        }

        public void CleanupOnPostClose()
        {
            uiMain = null;
            MessageCenter.Get<MSG.BINGO_PROGRESS_UPDATE>().RemoveListener(OnMessageBingoProgressChange);
        }

        public void Refresh()
        {
            if (actInst == null)
                return;
            var confGroup = ItemBingoUtility.GetGroupDetail(actInst.ConfBingoID);
            var round_max = confGroup.IncludeBoard.Count;
            var round_cur = actInst.GetBingoBoardIndex();
            // 轮次
            txtRound.text = I18N.FormatText("#SysComDesc863", round_cur, round_max);
            // 进度
            progressBar.ForceSetup(0, actInst.GetBingoTotalNum(), actInst.GetBingoCount());
            // 最终奖励
            RefreshReward();
        }

        private void RefreshReward()
        {
            // 进度达成后显示对勾 隐藏奖励
            var cur = actInst.GetBingoCount();
            var max = actInst.GetBingoTotalNum();
            if (cur >= max)
            {
                goCheck.SetActive(true);
                finalRewardItem.gameObject.SetActive(false);
            }
            else
            {
                goCheck.SetActive(false);
                finalRewardItem.gameObject.SetActive(true);
                var (_, _, all) = ItemBingoUtility.GetBoardRewardInfo(actInst.ConfBoardID);
                finalRewardItem.Refresh(all.Id, all.Count);
            }
        }

        private void OnMessageBingoProgressChange()
        {
            progressBar.SetProgress(actInst.GetBingoCount());
            RefreshReward();
        }

        private void OnBtnClickSpawnerInfo()
        {
            UIConfig.UIBingoSpawnerInfo.Open(actInst);
        }
    }
}