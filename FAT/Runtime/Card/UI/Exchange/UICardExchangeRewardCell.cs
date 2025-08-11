/*
 *@Author:chaoran.zhang
 *@Desc:集卡活动兑换界面宝箱cell
 *@Created Time:2024.09.09 星期一 17:57:34
 */

using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toast = fat.rawdata.Toast;

namespace FAT
{
    public class UICardExchangeRewardCell : MonoBehaviour
    {
        private UIImageRes _box;
        private UIImageState _boxState;
        private int _state = -1;
        private GameObject _buttonState1;
        private GameObject _buttonState2;
        private GameObject _buttonState3;
        private GameObject _lock;
        private CardStarExchangeData _data;
        private TextMeshProUGUI _cd;

        /// <summary>
        /// 组件初始化接口
        /// </summary>
        public void SetUp()
        {
            RegisterComp();
            AddButton();
        }

        /// <summary>
        /// 获取并维护各个组件
        /// </summary>
        private void RegisterComp()
        {
            transform.Access("BoxIcon", out _box);
            transform.Access("BoxIcon", out _boxState);
            transform.Access("BtnNode/State3/CD/cd_txt", out _cd);
            _buttonState1 = transform.Find("BtnNode").GetChild(0).gameObject;
            _buttonState2 = transform.Find("BtnNode").GetChild(1).gameObject;
            _buttonState3 = transform.Find("BtnNode").GetChild(2).gameObject;
            _lock = transform.Find("BoxIcon/Lock").gameObject;
        }

        /// <summary>
        /// 添加按钮点击函数
        /// </summary>
        private void AddButton()
        {
            //transform.Find("BoxIcon").GetComponent<Button>().clicked += OnInfoClick;
            transform.AddButton("BoxIcon", OnInfoClick);
            transform.AddButton("BtnNode/State1/ExchangeBtn", OnExchangeClick);
            transform.AddButton("BtnNode/State2/ExchangeBtn", OnExchangeClick);
            transform.AddButton("BtnNode/State3/ExchangeBtn", OnExchangeClick);
        }

        /// <summary>
        /// info按钮点击函数
        /// </summary>
        private void OnInfoClick()
        {
            var pos = transform.Find("BoxIcon").position;
            var offset = 85f;
            var reward = _data.GetConfig().Reward;
            Game.Manager.randomBoxMan.TryOpenRandomBoxTips(reward, pos, offset);
        }

        /// <summary>
        /// 兑换按钮点击函数
        /// </summary>
        private void OnExchangeClick()
        {
            switch (_state)
            {
                case 0:
                {
                    if (Game.Manager.cardMan.TryConsumeStarForReward(_data.StarExchangeId))
                        MessageCenter.Get<MSG.CARD_STAR_EXCHANGE>().Dispatch();
                    break;
                }
                case 1:
                {
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.CardNoStar);
                    break;
                }
                case 2:
                {
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.CardExchangeCd, _buttonState3.transform.position,
                        UIUtility.CountDownFormat(
                            _data.NextCanExchangeTime - Game.Instance.GetTimestampSeconds()));
                    break;
                }
            }
        }

        /// <summary>
        /// 初始化条目状态，包括：图片、按钮状态、CD文本等
        /// </summary>
        /// <param name="data">兑换条目相关数据</param>
        public void Init(CardStarExchangeData data)
        {
            _data = data;
            _box.SetImage(_data.GetBasicConfig().Icon);
            transform.Find("BtnNode/State1/ExchangeBtn/Root/Price").GetComponent<TextMeshProUGUI>().text =
                _data.GetConfig().CostStar.ToString();
            transform.Find("BtnNode/State2/ExchangeBtn/Root/Price").GetComponent<TextMeshProUGUI>().text =
                _data.GetConfig().CostStar.ToString();
            transform.Find("BtnNode/State3/ExchangeBtn/Root/Price").GetComponent<TextMeshProUGUI>().text =
                _data.GetConfig().CostStar.ToString();
            Refresh();
        }

        /// <summary>
        /// 刷新条目状态
        /// </summary>
        public void Refresh()
        {
            var cur = _state;
            SetState();
            if (cur != _state)
                RefreshCellUI();
            if (cur == 2)
                RefreshCD();
        }

        /// <summary>
        /// 判断当前购买状态
        /// </summary>
        private void SetState()
        {
            _state = Game.Manager.cardMan.CheckCanExchange(_data.StarExchangeId) ? 0 : 1;
            var time = Game.Instance.GetTimestampSeconds();
            if (time < _data.NextCanExchangeTime)
                _state = 2;
        }

        /// <summary>
        /// 根据购买状态刷新当前显示情况
        /// </summary>
        private void RefreshCellUI()
        {
            _lock.SetActive(_state == 2);
            _boxState.Setup(_state == 2 ? 1 : 0);
            _buttonState1.SetActive(_state == 0);
            _buttonState2.SetActive(_state == 1);
            _buttonState3.SetActive(_state == 2);
        }

        private void RefreshCD()
        {
            if (_state != 2)
                return;
            UIUtility.CountDownFormat(_cd, _data.NextCanExchangeTime -
                                           Game.Instance.GetTimestampSeconds());
            LayoutRebuilder.ForceRebuildLayoutImmediate(_cd.transform.parent as RectTransform);
        }

        /// <summary>
        /// 刷新布局
        /// </summary>
        public void Rebuild()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                _buttonState1.transform.Find("ExchangeBtn/Root") as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                _buttonState2.transform.Find("ExchangeBtn/Root") as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                _buttonState3.transform.Find("ExchangeBtn/Root") as RectTransform);
        }
    }
}