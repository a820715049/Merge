using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FAT
{
    public class UIRaceProStart : UIBase
    {
        [SerializeField] private List<MBRaceProTreasureLevel> treasureLevels;
        [SerializeField] private UIImageRes[] imageResArray;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TextMeshProUGUI cd;
        [SerializeField] private Transform player;
        
        private ActivityRaceExtend _activityRace;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Common/ConfirmBtn", OnClickConfirm);
            transform.AddButton("Content/Common/CancleBtn", OnClickCancel);
            transform.AddButton("Content/Common/InfoBtn", () => UIManager.Instance.OpenWindow(UIConfig.UIRaceHelp, transform.Find("Content/Common/InfoBtn").position, 0f));
        }

        protected override void OnParse(params object[] items)
        {
            _activityRace = (ActivityRaceExtend)items[0];
        }

        protected override void OnPreOpen()
        {
            Game.Manager.audioMan.TriggerSound("HotAirOpen");
            _activityRace.startPopup.visual.Refresh(title, "mainTitle1");
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            InitTreasure();
            RefreshCd();
        }

        protected override void OnPostOpen()
        {
            UpdateTreasure();
        }

        private void InitTreasure()
        {
            foreach (var t in treasureLevels)
            {
                t.InitState(_activityRace.phase);
            }

            player.transform.position = treasureLevels[_activityRace.phase].transform.position;
        }

        private void UpdateTreasure()
        {
            foreach (var t in treasureLevels)
            {
                t.UpdateState(_activityRace.phase);
            }

            player.transform.position = treasureLevels[_activityRace.phase].transform.position;
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
        }

        private void OnClickConfirm()
        {
            _activityRace.TryStartRound();
            Close();
        }

        private void OnClickCancel()
        {
            Close();
        }

        private void RefreshCd()
        {
            if (_activityRace == null)
            {
                Close();
                return;
            }

            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activityRace.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
            if (diff <= 0)
                Close();
        }
    }
}
