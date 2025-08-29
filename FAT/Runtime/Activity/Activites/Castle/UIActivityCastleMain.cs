/**
 * @Author: zhangpengjian
 * @Date: 2025/7/10 16:17:17
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/10 16:17:17
 * Description: 沙堡里程碑活动主界面
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System.Collections.Generic;
using fat.rawdata;
using Spine.Unity;

namespace FAT
{
    public class UIActivityCastleMain : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnHelp;
        [SerializeField] private Button btnGo;
        [SerializeField] private Button btnReward;
        [SerializeField] private Button btnTip;
        [SerializeField] private Transform talkRoot;
        [SerializeField] private Transform helpRoot;
        [SerializeField] private UIImageRes bgIcon;
        [SerializeField] private UIImageRes tokenIcon;
        [SerializeField] private UIImageRes rewardIcon;
        [SerializeField] private UIImageRes castleIcon;
        [SerializeField] private MBRewardProgress progress;
        [SerializeField] private TextMeshProUGUI cd;
        [SerializeField] private TextMeshProUGUI talkText;
        [SerializeField] private TextMeshProUGUI endText;
        [SerializeField] private TextMeshProUGUI btnText;
        [SerializeField] private TextMeshProUGUI help1;
        [SerializeField] private TextMeshProUGUI help2;
        [SerializeField] private TextMeshProUGUI help3;
        [SerializeField] private GameObject cell;
        [SerializeField] private GameObject milestoneRoot;
        [SerializeField] private GameObject cdRoot;
        [SerializeField] private GameObject block;
        [SerializeField] private Transform cellRoot;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private SkeletonGraphic spine;

        private ActivityCastle _activity;
        private List<GameObject> cellList = new();
        private CastleMilestoneGroup _confG;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(OnClickClose);
            btnHelp.onClick.AddListener(OnClickHelp);
            btnGo.onClick.AddListener(OnClickGo);
            btnReward.onClick.AddListener(OnClickReward);
            btnTip.onClick.AddListener(OnClickTip);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.CASTLE_MILESTONE_CELL, cell);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(OnOneSecond);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnOneSecond);
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as ActivityCastle;
            if (items.Length > 1 && items[1] != null)
            {
                _confG = items[1] as CastleMilestoneGroup;
            }
        }

        protected override void OnPreOpen()
        {
            progress.Refresh(_activity.Score, _activity.confG.MilestoneScore);
            castleIcon.SetImage(_activity.confG.DisplayBg);
            rewardIcon.SetImage(_activity.confG.MilestoneRewardIcon1);
            tokenIcon.SetImage(Game.Manager.objectMan.GetBasicConfig(_activity.conf.Token).Icon.ConvertToAssetConfig());
            RefreshList();
            ScrollToCurrent();
            OnOneSecond();
            var id = _activity.conf.Token;
            var s = UIUtility.FormatTMPString(id);
            help1.SetText(I18N.FormatText("#SysComDesc1415", s));
            help2.SetText(I18N.FormatText("#SysComDesc1416", s));
            help3.SetText(I18N.FormatText("#SysComDesc1417", s));
            endText.text = _activity.IsComplete() ? I18N.Text("#SysComDesc1400") : I18N.Text("#SysComDesc1399");
            endText.gameObject.SetActive(_activity.Countdown <= 0 || _activity.IsComplete());
            milestoneRoot.gameObject.SetActive(_activity.Countdown > 0 && !_activity.IsComplete());
            spine.AnimationState.SetAnimation(0, "idle", true);
            if (_confG != null)
            {
                spine.AnimationState.SetAnimation(0, "win", true);
                Game.Manager.audioMan.TriggerSound("SandCastleComplete");
                talkText.text = I18N.Text(_confG.DisplayFinishText);
                btnText.text = I18N.Text("#SysComDesc1398");
                if (_activity.IsComplete())
                {
                    talkText.text = I18N.Text("#SysComDesc1404");
                    btnText.text = I18N.Text("#SysComDesc1397");
                }
                cellList[_activity.ScorePhase - 1].GetComponent<MBActivityCastleCell>().PlayCurrent2Finish();
            }
            else
            {
                talkText.text = I18N.Text(_activity.IsComplete() || _activity.Countdown <= 0 ? "#SysComDesc1402" : _activity.confG.DisplayOngoingText);
                btnText.text = I18N.Text(_activity.IsComplete() || _activity.Countdown <= 0 ? "#SysComDesc1396" : "#SysComDesc1398");
            }
        }

        protected override void OnPostClose()
        {
            _confG = null;
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.CASTLE_MILESTONE_CELL, item);
            }
            cellList.Clear();
        }

        private void OnClickClose()
        {
            Close();
        }

        private void OnClickHelp()
        {
            btnTip.gameObject.SetActive(true);
            helpRoot.gameObject.SetActive(true);
        }

        private void OnClickGo()
        {
            if (Game.Manager.mapSceneMan.scene.Active)
            {
                GameProcedure.SceneToMerge();
                Close();
            }
            else
            {
                Close();
            }
        }

        private void OnClickReward()
        {
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips,
                rewardIcon.transform.position,
                rewardIcon.transform.GetComponent<RectTransform>().rect.size.y * 0.5f,
                _activity.confG.MilestoneReward);
        }

        private void OnClickTip()
        {
            btnTip.gameObject.SetActive(false);
            helpRoot.gameObject.SetActive(false);
        }

        private void RefreshList()
        {
            var list = _activity.confD.MilestoneGroup;
            scroll.content.sizeDelta = new Vector2(47f + (136 + 30) * list.Count, scroll.content.sizeDelta.y);
            scroll.normalizedPosition = new Vector2(0f, scroll.normalizedPosition.y);
            for (int i = 0; i < list.Count; i++)
            {
                var conf = fat.conf.Data.GetCastleMilestoneGroup(list[i]);
                var cellSand = GameObjectPoolManager.Instance.CreateObject(PoolItemType.CASTLE_MILESTONE_CELL, cellRoot.transform);
                cellSand.GetComponent<MBActivityCastleCell>().UpdateContent(i, conf, _activity.ScorePhase, i == list.Count - 1);
                cellList.Add(cellSand);
            }
        }

        private void ScrollToCurrent()
        {
            if (_activity == null || cellList.Count == 0) return;
            
            // 计算当前阶段的位置
            int currentIndex = _activity.ScorePhase;
            currentIndex = Mathf.Clamp(currentIndex, 0, cellList.Count - 1);
            
            // 计算目标位置
            // 每个单元格宽度：136 + 30 = 166
            // 起始偏移：47f
            float cellWidth = 166f;
            float startOffset = 47f;
            float targetX = startOffset + currentIndex * cellWidth;
            
            // 计算滚动视图的宽度
            float scrollViewWidth = scroll.viewport.rect.width;
            float contentWidth = scroll.content.rect.width;
            
            // 计算归一化位置
            float normalizedX = Mathf.Clamp01((targetX - scrollViewWidth * 0.5f) / (contentWidth - scrollViewWidth));
            
            // 设置滚动位置
            scroll.normalizedPosition = new Vector2(normalizedX, scroll.normalizedPosition.y);
        }

        private void OnOneSecond()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            cdRoot.SetActive(diff > 0);
            if (diff <= 0)
            {
                endText.gameObject.SetActive(true);
                milestoneRoot.gameObject.SetActive(false);
                talkText.text = I18N.Text("#SysComDesc1402");
                btnText.text = I18N.Text("#SysComDesc1396");
                return;
            }
            UIUtility.CountDownFormat(cd, diff);
        }
    }
}
