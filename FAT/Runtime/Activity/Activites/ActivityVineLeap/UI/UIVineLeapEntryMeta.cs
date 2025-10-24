// ===================================================
// Author: mengqc
// Date: 2025/09/18
// ===================================================

using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIVineLeapEntryMeta : MonoBehaviour
    {
        public TextMeshProUGUI tfNum;
        public TextMeshProUGUI tfLabel;
        public Animator animator;

        enum ViewState
        {
            None,
            Sleep,
            Working
        }

        private ListActivity.Entry _entry;
        private ActivityVineLeap _activity;
        private ViewState _animeState = ViewState.None;

        private void OnEnable()
        {
            _animeState = ViewState.None;
        }

        public void SetData(ListActivity.Entry entry, ActivityVineLeap activity)
        {
            _entry = entry;
            _activity = activity;
            Refresh();
        }

        public void Refresh()
        {
            if (_activity == null) return;
            if (_activity.IsCurStepRunning())
            {
                if (_animeState != ViewState.Working)
                {
                    animator?.SetTrigger("SleepHide");
                    _animeState = ViewState.Working;
                }

                _entry.dot.SetActive(false);
                tfLabel.gameObject.SetActive(true);
                tfLabel.text = I18N.Text("#SysComDesc1719");
                tfNum.text = _activity.GetSeatsLeft().ToString();
            }
            else
            {
                if (_animeState != ViewState.Sleep)
                {
                    animator?.SetTrigger("Sleep");
                    _animeState = ViewState.Sleep;
                }

                tfNum.text = "YOU";
                tfLabel.gameObject.SetActive(false);
                _entry.dot.SetActive(true);
            }
        }
    }
}