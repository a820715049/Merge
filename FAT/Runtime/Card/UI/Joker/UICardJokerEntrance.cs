/*
 * @Author: tang.yan
 * @Description: 万能卡入口界面 
 * @Date: 2024-03-27 20:03:26
 */
using EL;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class UICardJokerEntrance : UIBase
    {
        [SerializeField] private GameObject normalGo;
        [SerializeField] private GameObject goldGo;
        [SerializeField] private TMP_Text jokerName;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private Button closeBtn;

        private CardJokerData _curSelectJokerData;
        //是否显示下一张万能卡的入口
        private bool _isShowNext = false;
        //是否强制玩家选择(不显示关闭按钮)
        private bool _isForceChoose = false;
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", _OnBtnClose);
            transform.AddButton("Content/Root/Bottom/ChooseBtn", _OnBtnClaim);
            closeBtn.WithClickScale().FixPivot().onClick.AddListener(_OnBtnClose);
        }

        protected override void OnParse(params object[] items) { }

        protected override void OnPreOpen()
        {
            var roundData = Game.Manager.cardMan.GetCardRoundData();
            if (roundData == null) return;
            var jokerData = roundData.GetCurIndexJokerData();
            if (jokerData == null) return;
            _curSelectJokerData = jokerData;
            _curSelectJokerData.SetLockExpire(true);
            normalGo.SetActive(_curSelectJokerData.IsGoldCard == 0);
            goldGo.SetActive(_curSelectJokerData.IsGoldCard == 1);
            jokerName.text = I18N.Text(_curSelectJokerData.GetObjBasicConfig()?.Name ?? "");
            _RefreshTime();
            _RefreshExpire();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_RefreshTime);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshTime);
        }

        private void _RefreshTime()
        {
            //当活动结束或者数据非法时 直接关界面
            if (!Game.Manager.cardMan.CheckValid() || _curSelectJokerData == null)
            {
                Close();
                return;
            }
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _curSelectJokerData.ExpireTs - t);
            _isForceChoose = diff <= 0;
            if (!_isForceChoose)
                timeText.text = I18N.FormatText("#SysComDesc316", UIUtility.CountDownFormat(diff));
            else
            {
                timeText.text = I18N.Text("#SysComDesc568");
            }
            //当检查到时间上过期时 还要检查一下当前卡片是否值得被使用，如果不值得被使用则允许玩家暂时关闭界面 反之则不允许
            if (_isForceChoose)
            {
                var isUseful = Game.Manager.cardMan.GetCardRoundData()?.CheckIsUsefulJokerData(_curSelectJokerData) ?? false;
                if (!isUseful)
                    _isForceChoose = false;
            }
            _RefreshExpire();
        }

        private void _RefreshExpire()
        {
            closeBtn.gameObject.SetActive(!_isForceChoose);
            descText.text = _isForceChoose ? I18N.Text("#SysComDesc569") : I18N.Text("#SysComDesc317");
        }

        protected override void OnPostClose()
        {
            _curSelectJokerData = null;
            if (_isShowNext)
            {
                bool isLast = Game.Manager.cardMan.GetCardRoundData()?.ProcessNextJokerIndex() ?? true;
                if (!isLast)
                {
                    Game.Manager.cardMan.OpenJokerEntranceUI();
                }
            }
            _isShowNext = false;
        }
        
        //点击确定时 打开选卡界面
        private void _OnBtnClaim()
        {
            UIManager.Instance.OpenWindow(UIConfig.UICardJokerSelect);
            _isShowNext = false;
            Close();
        }

        //点击关界面时 取消卡片的锁定过期状态 同时递进当前index
        private void _OnBtnClose()
        {
            if (_isForceChoose) return;
            _curSelectJokerData.SetLockExpire(false);
            _isShowNext = true;
            Close();
        }
    }
}