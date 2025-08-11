
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using EL;
using TMPro;
using static EL.PoolMapping;

namespace FAT
{
    public class UIMineBoardEndNotice : UIBase
    {
        // Image字段
        private UIImageRes _Bg;
        private UIImageRes _TitleBg;
        private UIImageRes _DeepBg;

        // Text字段
        private TextProOnACircle _Title;
        private TextMeshProUGUI _SubTitle;
        private TextMeshProUGUI _DeepTxt1;
        private TextMeshProUGUI _DeepTxt2;
        private MineBoardActivity _activity;
        private UICommonItem _item;

        private Ref<List<RewardCommitData>> list;
        private MBRewardLayout.CommitList _result;
        protected override void OnCreate()
        {
            base.OnCreate();
            // Image绑定
            transform.Access("Content/Bg_img", out _Bg);
            transform.Access("Content/TitleBg_img", out _TitleBg);
            transform.Access("Content/DeepBg_img", out _DeepBg);
            // Text绑定
            transform.Access("Content/TitleBg_img/Title_txt", out _Title);
            transform.Access("Content/DescBg/SubTitle_txt", out _SubTitle);
            transform.Access("Content/DeepBg_img/DeepTxt1_txt", out _DeepTxt1);
            transform.Access("Content/DeepBg_img/DeepTxt2_txt", out _DeepTxt2);
            transform.AddButton("Content/CloseBtn", ClickBtn);
            transform.AddButton("Content/Confirm", ClickBtn);
            transform.Access("Content/DescBg/UICommonItem", out _item);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2) return;
            _activity = items[0] as MineBoardActivity;
            list = (Ref<List<RewardCommitData>>)items[1];
            _result = new() { list = list.obj };
            if (_result.list == null || _result.list.Count == 0)
            {
                _item.gameObject.SetActive(false);
                return;
            }
            _item.gameObject.SetActive(true);
            _item.Setup();
            _item.Refresh(list.obj.FirstOrDefault().rewardId, list.obj.FirstOrDefault().rewardCount);
        }

        protected override void OnPreOpen()
        {
            _activity.EndTheme.Refresh(_Title, "mainTitle");
            _DeepTxt2.text = ZString.Concat(Game.Manager.mineBoardMan.GetCurDepth(), I18N.Text("#SysComDesc892"));
        }

        private void ClickBtn()
        {
            if (_result.list != null && _result.list.Count > 0)
                UIFlyUtility.FlyReward(_result.list.FirstOrDefault(), _item.transform.position);
            Close();
        }

        protected override void OnPreClose()
        {
            list.Free();
            _result.list = null;
        }
    }
}
