// ==================================================
// // File: UIBingoTaskEntry.cs
// // Author: liyueran
// // Date: 2025-07-18 15:07:50
// // Desc: bingoTask 入口
// // ==================================================

using System.Linq;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBingoTaskEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _dot;
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private TextMeshProUGUI _cd;

        private ActivityBingoTask _activity;

        public void Start()
        {
            var button = transform.Find("Root/Bg").GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        private void EntryClick()
        {
            if (!_activity.Active)
                return;
            _activity.Open();
        }

        private void OnEnable()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenRefresh);
            RefreshEntry(_activity);
        }

        private void OnDisable()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenRefresh);
        }


        private void WhenRefresh()
        {
            RefreshEntry(_activity);
        }

        public void RefreshEntry(ActivityLike activity = null)
        {
            _activity = activity as ActivityBingoTask;
            if (_activity == null)
            {
                Visible(false);
                return;
            }

            if (!_activity.Active)
            {
                Visible(false);
                return;
            }

            Visible(true);
            UIUtility.CountDownFormat(_cd, _activity.Countdown);

            if (_activity.WhetherShowRemind(out var redNum))
            {
                _dot.SetActive(true);
                _num.SetRedPoint(redNum);
            }
            else
            {
                _dot.SetActive(false);
            }
        }


        private void Visible(bool v_)
        {
            _root.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }
    }
}