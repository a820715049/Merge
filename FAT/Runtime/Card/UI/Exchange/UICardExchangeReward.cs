/*
 *@Author:chaoran.zhang
 *@Desc:卡册活动重复奖励兑换界面
 *@Created Time:2024.09.02 星期一 15:21:00
 */

using System.Collections;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class UICardExchangeReward : UIBase
    {
        private UIImageRes _topBG;
        private TextMeshProUGUI _starCount;
        private UICardExchangeRewardCell _cell1;
        private UICardExchangeRewardCell _cell2;
        private UICardExchangeRewardCell _cell3;
        private RectTransform _starRect;
        private RectTransform _rectCell1;
        private RectTransform _rectCell2;
        private RectTransform _rectCell3;


        protected override void OnCreate()
        {
            RegisterComp();
            Setup();
        }

        /// <summary>
        /// 注册并维护各个组件
        /// </summary>
        private void RegisterComp()
        {
            transform.AddButton("Content/CloseBtn", Close);
            transform.Access("Content/Panel/TopBg", out _topBG);
            transform.Access("Content/Panel/TopBg/ScoreBg/Root/Count", out _starCount);
            transform.Access("Content/Panel/SelectBg/Cell1", out _cell1);
            transform.Access("Content/Panel/SelectBg/Cell2", out _cell2);
            transform.Access("Content/Panel/SelectBg/Cell3", out _cell3);
            transform.Access("Content/Panel/TopBg/ScoreBg/Root", out _starRect);
            transform.Access("Content/Panel/SelectBg/Cell1/BtnNode/State3/CD", out _rectCell1);
            transform.Access("Content/Panel/SelectBg/Cell2/BtnNode/State3/CD", out _rectCell2);
            transform.Access("Content/Panel/SelectBg/Cell3/BtnNode/State3/CD", out _rectCell3);
        }

        /// <summary>
        /// 初始化入口，仅调用一次
        /// </summary>
        private void Setup()
        {
            _cell1.SetUp();
            _cell2.SetUp();
            _cell3.SetUp();
        }

        protected override void OnPreOpen()
        {
            InitCell();
            InitPanel();
            RefreshPanel();
        }

        protected override void OnPostOpen()
        {
            Rebuild();
        }

        /// <summary>
        /// 每次打开面板时进行一次初始化
        /// </summary>
        private void InitCell()
        {
            var list = Game.Manager.cardMan.GetCardRoundData().GetAllStarExchangeData();
            _cell1.Init(list[0]);
            _cell2.Init(list[1]);
            _cell3.Init(list[2]);
        }

        /// <summary>
        /// 初始化兑换面板本身直接维护的文本、图片等
        /// </summary>
        private void InitPanel()
        {
            Game.Manager.cardMan.GetCardActivity().VisualRestart.Refresh(_topBG, "exchangeBg");
            _starCount.text = Game.Manager.cardMan.GetTotalStarNum().ToString();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.CARD_STAR_EXCHANGE>().AddListener(RefreshPanel);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshPanel);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(OnActivityEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.CARD_STAR_EXCHANGE>().RemoveListener(RefreshPanel);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshPanel);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(OnActivityEnd);
        }


        /// <summary>
        /// 刷新面板显示、星星数量文本、各兑换条目的现实状态
        /// </summary>
        private void RefreshPanel()
        {
            _starCount.text = Game.Manager.cardMan.GetTotalStarNum().ToString();
            _cell1.Refresh();
            _cell2.Refresh();
            _cell3.Refresh();
        }

        /// <summary>
        /// 刷新layout排布
        /// </summary>
        private void Rebuild()
        {
            IEnumerator start()
            {
                yield return null;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_starRect);
                _cell1.Rebuild();
                _cell2.Rebuild();
                _cell3.Rebuild();
            }

            StartCoroutine(start());
        }

        /// <summary>
        /// 活动结束时自动关闭界面
        /// </summary>
        /// <param name="act"></param>
        /// <param name="expire"></param>
        private void OnActivityEnd(ActivityLike act, bool expire)
        {
            if (act.Lite.Type == EventType.CardAlbum)
                Close();
        }
    }
}