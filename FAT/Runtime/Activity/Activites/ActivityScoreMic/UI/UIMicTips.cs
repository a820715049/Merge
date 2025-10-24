using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIMicTips : UITipsBase
    {
        [SerializeField] private UIImageRes[] itemIcon;
        [SerializeField] private TMP_Text[] itemText;
        
        private int _curId; //目前正在查看的id
        
        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 3)
            {
                //设置tips位置参数
                _SetTipsPosInfo(items);
                //设置界面自定义参数
                _curId = (int)items[2];
            }
        }

        protected override void OnPreOpen()
        {
            _RefreshTipsPos(18);
            _RefreshShowReward();
        }

        private void _RefreshShowReward()
        {
            var data = Game.Manager.configMan.GetComMergeTokenMultiplierConfig(_curId);
            for (var i = 0; i < data.Token.Count; i++)
            {
                var token = data.Token[i];
                var conf = Game.Manager.objectMan.GetBasicConfig(token);
                if (conf == null) continue;
                itemIcon[i].SetImage(conf.Icon);
                itemText[i].text = $"x{data.TokenMultiplier}";
            }
        }
    }
}