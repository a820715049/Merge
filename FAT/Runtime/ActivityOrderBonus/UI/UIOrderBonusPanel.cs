using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIOrderBonusPanel : UIBase
    {
        public TextMeshProUGUI cd;
        public UIStateGroup group;
        public GameObject lock1;
        public GameObject lock2;
        public GameObject lock3;
        private ActivityOrderBonus _act;

        protected override void OnCreate()
        {
            transform.AddButton("Content/Bg/PhaseNode/Phase1", () => ClickReward(0));
            transform.AddButton("Content/Bg/PhaseNode/Phase2", () => ClickReward(1));
            transform.AddButton("Content/Bg/PhaseNode/Phase3", () => ClickReward(2));
            transform.AddButton("Content/Bg/BtnConfirm", Close);
            transform.AddButton("Content/Bg/CloseBtn", Close);
        }
        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        protected override void OnParse(params object[] items)
        {
            _act = items[0] as ActivityOrderBonus;
            RefreshCD();
            group.Select(_act.phase > 0 ? 1 : 0);
            lock1.SetActive(_act.phase <= 0);
            lock2.SetActive(_act.phase <= 1);
            lock3.SetActive(_act.phase <= 2);
        }

        public void ClickReward(int id)
        {
            var pos = transform.Find("Content/Bg/PhaseNode").GetChild(id).position;
            UIManager.Instance.OpenWindow(UIConfig.UIOrderBonusTips, pos, 110f, id);
        }

        public void RefreshCD()
        {
            UIUtility.CountDownFormat(cd, _act.Countdown);
            if (_act.Countdown <= 0) Close();
        }
    }
}