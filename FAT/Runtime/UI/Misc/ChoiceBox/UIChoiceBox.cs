/*
 * @Author: qun.chao
 * @Date: 2024-07-06 10:52:56
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FAT.Merge;
using EL;

namespace FAT
{
    public class UIChoiceBox : UIBase
    {
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnGrey;
        [SerializeField] private Transform itemRoot;

        private int boxItemId = -1;
        private int selectedItemId = -1;
        private List<int> curChoices;
        private Action<int> onConfirm;

        protected override void OnCreate()
        {
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var idx = i;
                var btn = itemRoot.GetChild(idx).GetComponent<Button>();
                btn.onClick.AddListener(() => OnSelectItem(idx));
            }
            btnConfirm.onClick.AddListener(OnBtnConfirm);
            btnClose.onClick.AddListener(base.Close);
        }

        protected override void OnParse(params object[] items)
        {
            var boxItem = items[0] as Item;
            var choices = items[1] as List<int>;
            onConfirm = items[2] as Action<int>;

            ShowTitle(boxItem);
            ShowChoices(choices);
        }

        protected override void OnPreOpen()
        {
            selectedItemId = -1;
            btnConfirm.gameObject.SetActive(false);
            btnGrey.gameObject.SetActive(true);
        }

        private void ShowTitle(Item box)
        {
            boxItemId = box.tid;
            var cfg = Game.Manager.objectMan.GetBasicConfig(box.tid);
            txtTitle.text = I18N.Text(cfg.Name);
        }

        private void ShowChoices(List<int> choices)
        {
            curChoices = choices;
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var itemTrans = itemRoot.GetChild(i);
                if (i < choices.Count)
                {
                    itemTrans.gameObject.SetActive(true);
                    ItemShow(itemTrans, choices[i]);
                    ItemSelect(itemTrans, false);
                }
                else
                {
                    itemTrans.gameObject.SetActive(false);
                }
            }
        }

        private void ItemShow(Transform item, int id)
        {
            var cfg = Game.Manager.objectMan.GetBasicConfig(id);
            item.FindEx<UIImageRes>("Icon").SetImage(cfg.Icon);
        }

        private void ItemSelect(Transform item, bool selected)
        {
            item.Find("Selected").gameObject.SetActive(selected);
        }

        private void OnSelectItem(int idx)
        {
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                ItemSelect(itemRoot.GetChild(i), idx == i);
            }
            if (idx < curChoices.Count)
            {
                selectedItemId = curChoices[idx];
            }
            btnConfirm.gameObject.SetActive(true);
            btnGrey.gameObject.SetActive(false);
        }

        private void OnBtnConfirm()
        {
            onConfirm?.Invoke(selectedItemId);
            DataTracker.TrackMergeActionChoiceBox(boxItemId, selectedItemId, curChoices);
            base.Close();
        }
    }
}