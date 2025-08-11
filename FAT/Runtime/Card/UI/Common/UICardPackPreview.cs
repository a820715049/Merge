using System.Collections.Generic;
using UnityEngine;
using EL;
using TMPro;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    public class UICardPackPreview : UITipsBase
    {
        public TextMeshProUGUI count;
        public TextMeshProUGUI desc;
        public CardStar star;
        public float offset = 18f;
        private int id;
        private ObjCardPack conf;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            var root = transform.Find("root");
            count = root.FindEx<TextMeshProUGUI>("count");
            desc = root.FindEx<TextMeshProUGUI>("desc");
            star = root.FindEx<CardStar>("star");
        }
#endif

        protected override void OnCreate()
        {
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 3)
            {
                //设置tips宽度,为保证距离边缘有9的距离，+18
                _SetCurExtraWidth(18);
                _SetCurTipsWidth(356);
                //设置tips位置参数
                _SetTipsPosInfo(items);
                //设置界面自定义参数
                id = (int)items[2];
                conf = Game.Manager.objectMan.GetCardPackConfig(id);
            }
        }

        protected override void OnPreOpen()
        {
            RefreshInfo();
            //刷新tips位置
            _RefreshTipsPos(offset);
        }

        public void RefreshInfo()
        {
            if (conf == null) return;
            count.text = $"x{conf.CardNum}";
            desc.text = I18N.FormatText("#SysComDesc234", conf.LeastNum);
            star.Setup(conf.LeastStar);
        }
    }
}