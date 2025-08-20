/*
 *@Author:chaoran.zhang
 *@Desc:热气球活动新一轮开启弹板
 *@Created Time:2024.07.09 星期二 10:23:12
 */

using System.Collections;
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIRaceStart : UIBase
    {
        public GameObject NormalNode;
        public GameObject TreasureNode;
        public List<MBRaceTreasureLevel> TreasureLevels;
        private UIImageState _bg;
        private TextProOnACircle _title;
        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _subTitle;
        private ActivityRace _race;
        private TextMeshProUGUI _desc;

        protected override void OnCreate()
        {
            _bg = transform.Find("Content/Common/TitleBg").GetComponent<UIImageState>();
            _title = transform.Find("Content/Common/TitleBg/TitleText").GetComponent<TextProOnACircle>();
            _cd = transform.Find("Content/Common/_cd/text").GetComponent<TextMeshProUGUI>();
            _subTitle = transform.Find("Content/Common/Normal/Desc1").GetComponent<TextMeshProUGUI>();
            _desc = transform.Find("Content/Common/Normal/Desc2").GetComponent<TextMeshProUGUI>();
            transform.AddButton("Content/Common/ConfirmBtn", OnClickConfirm);
            transform.AddButton("Content/Common/CancleBtn", OnClickCancel);
            transform.AddButton("Content/Common/InfoBtn", () => UIManager.Instance.OpenWindow(UIConfig.UIRaceHelp, transform.Find("Content/Common/InfoBtn").position, 0f));

        }

        protected override void OnPreOpen()
        {
            Game.Manager.audioMan.TriggerSound("HotAirOpen");
            _race = RaceManager.GetInstance().Race;
            _race.StartVisual.Refresh(_title, "mainTitle");
            _bg.Setup(_race.Round >= 0 && _race.Round < _race.ConfD.NormalRoundId.Count ? 0 : 1);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            NormalNode.SetActive(_race.ConfD.SubType == 0);
            TreasureNode.SetActive(_race.ConfD.SubType == 1);
            transform.Find("Content/Common/InfoBtn").gameObject.SetActive(_race.ConfD.SubType == 1);
            switch (_race.ConfD.SubType)
            {
                case 0:
                    InitNormal();
                    break;
                case 1:
                    InitTreasure();
                    break;
                default:
                    break;
            }
            RefreshCD();
        }

        protected override void OnPostOpen()
        {
            if (_race.ConfD.SubType == 1)
            {
                UpdateTreasure();
            }
        }

        private void InitNormal()
        {
            if (_race.Round >= 0 && _race.Round < _race.ConfD.NormalRoundId.Count)
                _subTitle.text = I18N.FormatText("#SysComDesc402", _race.Round + 1);
            else
                _subTitle.text = I18N.Text("#SysComDesc432");
            _race.StartVisual.Refresh(_desc, _race.NextRoundRewardNum() > 1 ? "subTitle1" : "subTitle2");
        }

        private void InitTreasure()
        {
            for (var i = 0; i < TreasureLevels.Count; i++)
            {
                TreasureLevels[i].InitState(i, _race.Round);
            }
        }

        private void UpdateTreasure()
        {
            for (var i = 0; i < TreasureLevels.Count; i++)
            {
                TreasureLevels[i].UpdateState(_race.Round);
            }
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void OnClickConfirm()
        {
            RaceManager.GetInstance().StartNewRound();
            Close();
        }

        private void OnClickCancel()
        {
            Close();
        }

        private void RefreshCD()
        {
            if (RaceManager.GetInstance().Race == null)
            {
                Close();
                return;
            }

            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, RaceManager.GetInstance().Race.endTS - t);
            UIUtility.CountDownFormat(_cd, diff);
            if (diff <= 0)
                Close();
        }
    }
}
