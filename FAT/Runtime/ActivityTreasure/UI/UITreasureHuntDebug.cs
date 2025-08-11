using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;
using Config;
using TMPro;
using EL;
using static fat.conf.Data;
using DG.Tweening;
using Random = System.Random;

namespace FAT
{
    public class UITreasureHuntDebug : UIBase
    {
        public TextMeshProUGUI score;
        public TextMeshProUGUI key;
        public TextMeshProUGUI showNum;
        public TextMeshProUGUI grpIdx;
        public TextMeshProUGUI levelIdx;
        public TextMeshProUGUI boxLeft;
        public TextMeshProUGUI openCount;
        public TextMeshProUGUI noAppear;
        public TextMeshProUGUI appear;
        public TextMeshProUGUI has;
        public TextMeshProUGUI cycle;
        public Button addScore;
        public Button openBox;
        public Button addKey;
        private ActivityTreasure activityTreasure;
        protected override void OnCreate()
        {
            transform.FindEx<Button>("Mask").onClick.AddListener(Close);
            addScore.onClick.AddListener(OnClick);
            openBox.onClick.AddListener(OnClick2);
            addKey.onClick.AddListener(OnClick3);
        }

        protected override void OnAddListener()
        {
            base.OnAddListener();
            MessageCenter.Get<MSG.TREASURE_SCORE_UPDATE>().AddListener(RefreshData2);
            MessageCenter.Get<MSG.TREASURE_OPENBOX_UPDATE>().AddListener(RefreshData);
            MessageCenter.Get<MSG.TREASURE_LEVEL_UPDATE>().AddListener(RefreshData);
            MessageCenter.Get<MSG.TREASURE_KEY_UPDATE>().AddListener(RefreshKey);
        }

        protected override void OnRemoveListener()
        {
            base.OnRemoveListener();
            MessageCenter.Get<MSG.TREASURE_SCORE_UPDATE>().RemoveListener(RefreshData2);
            MessageCenter.Get<MSG.TREASURE_OPENBOX_UPDATE>().RemoveListener(RefreshData);
            MessageCenter.Get<MSG.TREASURE_LEVEL_UPDATE>().RemoveListener(RefreshData);
            MessageCenter.Get<MSG.TREASURE_KEY_UPDATE>().RemoveListener(RefreshKey);
        }

        private void OnClick()
        {
            var score = 100;
            activityTreasure.AddScore(score);
        }

        private void OnClick2()
        {
            var l = new List<RewardConfig>();
            var l2 = new List<RewardConfig>();
            Random r = new Random();
            var ll = activityTreasure.GetCurrentTreasureLevel();
            var rr = r.Next(0, ll.RewardInfo.Count - 1);
            Debug.LogError(rr);
            activityTreasure.TryOpenBox(rr, l, l2, out var state);
            foreach (var VARIABLE in l2)
            {
                Debug.LogError(VARIABLE.Id);
                Debug.LogError(VARIABLE.Count);
            }
        }

        private void OnClick3()
        {
            activityTreasure.UpdateScoreOrTreasureKey(9, 100);
        }

        private void RefreshData()
        {

            score.text = activityTreasure.GetScore().ToString();

            grpIdx.text = activityTreasure.GetGroupIndex().ToString();
            levelIdx.text = activityTreasure.GetLevelIndex().ToString();
            var a = "";
            var b = "";
            var l = activityTreasure.GetCurrentLevelBoxes();
            foreach (var v in l)
            {
                a += v.Id + "/";
            }
            for (int i = 0; i < l.Count; i++)
            {
                var isO = activityTreasure.HasOpen(i);
                 b += isO ? 1 : 0 + "/";
            }

            boxLeft.text = a;
            has.text = b;
            openCount.text = activityTreasure.GetOpenCount().ToString();
            noAppear.text = activityTreasure.GetNoAppear().ToString();
            appear.text = activityTreasure.GetAppear().ToString();
            cycle.text = activityTreasure.GetScoreCycleCount().ToString();
        }

        private void RefreshData2(int a = 0, int b = 0)
        {
            var (c,d) = activityTreasure.GetScoreShowNum();
            showNum.text = c + "/" + d;
        }
        
        protected override void OnPreOpen()
        {
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.Treasure, out var activity);
            if (activity == null)
                return;
            activityTreasure = (ActivityTreasure)activity;
            RefreshData();
            RefreshData2();
            RefreshKey();
        }

        private void RefreshKey(int a = 0)
        {
            key.text = activityTreasure.GetKeyNum().ToString();
        }
        
        protected override void OnPreClose()
        {
        }
    }
}