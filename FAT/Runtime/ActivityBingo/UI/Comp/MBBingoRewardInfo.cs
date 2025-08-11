/*
 * @Author: qun.chao
 * @Date: 2025-03-06 10:46:43
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class MBBingoRewardInfo : MonoBehaviour
    {
        [SerializeField] private UICommonItem itemStraight;
        [SerializeField] private UICommonItem itemSlash;
        [SerializeField] private UICommonItem itemAll;
        [SerializeField] private Button btnReward;
        [SerializeField] private Button info;

        private UIBingoMain uiMain;
        private ActivityBingo actInst => uiMain.ActInst;

        private void Start()
        {
            btnReward.onClick.AddListener(OnBtnClickReward);
            info.onClick.AddListener(OnBtnClickReward);
        }

        public void InitOnPreOpen(UIBingoMain main)
        {
            uiMain = main;
        }

        public void CleanupOnPostClose()
        {
            uiMain = null;
        }

        public void Refresh()
        {
            var (straight, slash, all) = BingoUtility.GetBoardRewardInfo(actInst.ConfBoardID);
            itemStraight.Refresh(straight.Id, straight.Count);
            itemSlash.Refresh(slash.Id, slash.Count);
            itemAll.Refresh(all.Id, all.Count);
        }

        private void OnBtnClickReward()
        {
            actInst.BingoRes.ActiveR.Open(actInst);
        }
    }
}