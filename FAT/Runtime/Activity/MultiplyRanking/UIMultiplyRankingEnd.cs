/*
 * @Author: yanfuxing
 * @Date: 2025-07-22 15:40:09
 */
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMultiplyRankingEnd : UIBase
    {

        [SerializeField] private TextMeshProUGUI _cdText;
        [SerializeField] private MapButton _confirmBtn;
        [SerializeField] private MapButton _helpBtn;
        [SerializeField] private MapButton _progressBtn;
        [SerializeField] private MapButton _closeBtn;
        [SerializeField] private Button TurntableBtn;
        [SerializeField] private Transform TurntableTipTrans;
        [SerializeField] private Transform TurntableTrans;
        [SerializeField] private Transform TurntableArrow;
        [SerializeField] private UIMultiplyRankingListScroll _scroll;
        [SerializeField] private UIVisualGroup _vGroup;
        [SerializeField] private MBRewardProgress _mBprogress;
        [SerializeField] private MBRewardIcon _mBProgressReward;
        [SerializeField] private Transform _playerRankingInfoTrans;
        [SerializeField] private ActivityMultiplierRanking _activity;

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out _vGroup);
            var root = transform.Find("Content");
            _vGroup.Prepare(root.Access<TextMeshProUGUI>("title"), "mainTitle");
            _vGroup.Prepare(root.Access<TextMeshProUGUI>("confirm/text"), "confirm");
            _vGroup.CollectTrim();
        }
    }
}

