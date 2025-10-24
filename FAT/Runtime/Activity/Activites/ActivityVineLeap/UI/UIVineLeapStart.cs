// ===================================================
// Author: mengqc
// Date: 2025/09/02
// ===================================================

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using fat.conf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIVineLeapStart : UIBase
    {
        // UI组件
        public GameObject startPanel;
        public GameObject choicePanel;
        public RectTransform choiceGroup;
        public TextMeshProUGUI startSubDesc;
        public Animator contentAnimator;
        public UITextState stateTfBtnStart;

        // 活动实例
        private ActivityVineLeap _activity;
        private Button _btnStart;
        private List<ChoiceItem> _choiceItems = new();
        private int _selectedIndex = -1;

        protected struct ChoiceItem
        {
            public int index;
            public RectTransform root;
            public Button button;
            public RectTransform frame;
            public RectTransform content;
            public GameObject[] selectEffects;
        }

        protected override void OnCreate()
        {
            // 添加按钮事件
            transform.AddButton("Content/StartPanel/btnConfirm", OnClickConfirm);

            startSubDesc.text = I18N.FormatText("#SysComDesc1752", I18N.Text("#SysComDesc1720"));
            _choiceItems.Clear();
            foreach (RectTransform child in choiceGroup)
            {
                var index = _choiceItems.Count;
                var button = child.GetComponent<Button>();
                button.onClick.AddListener(() => { Select(index); });
                var content = child.Access<RectTransform>("content");
                var frame = content.Access<RectTransform>("Frame");
                _choiceItems.Add(new ChoiceItem
                {
                    index = index,
                    root = child,
                    button = button,
                    frame = frame,
                    content = content,
                    selectEffects = new[]
                    {
                        frame.gameObject,
                        content.TryFind("fx_vineleap_choice_glow_a"),
                        content.TryFind("fx_vineleap_choice_glow_b")
                    }
                });
            }

            _btnStart = transform.AddButton("Content/ChoicePanel/btnStart", OnClickStart);
            transform.AddButton("Content/ChoicePanel/btnClose", Close);
        }

        protected override void OnParse(params object[] items)
        {
            _selectedIndex = -1;
            if (items.Length < 1) return;
            _activity = (ActivityVineLeap)items[0];
            var showChoice = items.Length == 2 && items[1] is bool && (bool)items[1];
            startPanel.SetActive(!showChoice);
            choicePanel.SetActive(showChoice);
            contentAnimator.SetTrigger(showChoice ? "Choice" : "Start");
            foreach (var choiceItem in _choiceItems)
            {
                var imgChest = choiceItem.content.Access<UIImageRes>("Box");
                imgChest.SetImage(_activity.GetChestIconByIndex(choiceItem.index));
            }

            UpdateView();
        }


        private void OnClickConfirm()
        {
            StartCoroutine(SwitchToChoice());
        }

        private IEnumerator SwitchToChoice()
        {
            choicePanel.SetActive(true);
            contentAnimator.SetTrigger("StartToChoice");
            yield return new WaitForSeconds(78f / 60);
            var showChoice = true;
            startPanel.SetActive(!showChoice);
            choicePanel.SetActive(showChoice);
        }

        private void UpdateView()
        {
            for (var i = 0; i < _choiceItems.Count; i++)
            {
                var child = _choiceItems[i];
                if (i == _selectedIndex)
                {
                    SetSelectVisible(child, true);
                }
                else
                {
                    SetSelectVisible(child, false);
                }
            }

            if (_selectedIndex > -1)
            {
                _btnStart.interactable = true;
                stateTfBtnStart.Enabled(true);
                GameUIUtility.SetDefaultShader(_btnStart.image);
            }
            else
            {
                _btnStart.interactable = false;
                stateTfBtnStart.Enabled(false);
                GameUIUtility.SetGrayShader(_btnStart.image);
            }
        }

        private void Select(int index)
        {
            _selectedIndex = index;
            UpdateView();
            var frame = _choiceItems[_selectedIndex].frame;
            var diffId = _activity.DiffConf.SelectDiffId[index];
            var groupCfg = EventVineLeapGroupVisitor.Get(diffId);
            UIManager.Instance.CloseWindow(UIConfig.UIVineLeapChoiceTip);
            UIManager.Instance.OpenWindow(UIConfig.UIVineLeapChoiceTip, frame.position, frame.rect.height / 2, _activity.GetRewardsById(groupCfg.MilestoneReward));
        }

        private void SetSelectVisible(ChoiceItem item, bool visible)
        {
            DOTween.Kill(item.content);
            if (visible)
            {
                item.content.DOAnchorPosY(20f, 0.2f);
            }
            else
            {
                item.content.DOAnchorPosY(0, 0.2f);
            }

            foreach (var effect in item.selectEffects)
            {
                effect.SetActive(visible);
            }
        }

        private void OnClickStart()
        {
            _activity.SetDifficultyIndex(_selectedIndex);
            Close();
            _activity.Open();
        }
    }
}