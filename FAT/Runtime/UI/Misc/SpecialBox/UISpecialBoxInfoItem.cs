/*
 * @Author: qun.chao
 * @Date: 2024-07-06 14:50:10
 */
using UnityEngine;

namespace FAT
{

    public class UISpecialBoxInfoItem : UIGenericItemBase<int>
    {
        [SerializeField] private UIImageRes resIcon;

        protected override void UpdateOnDataChange()
        {
            var cfg = Game.Manager.objectMan.GetBasicConfig(mData);
            if (cfg != null)
            {
                resIcon.SetImage(cfg.Icon);
            }
        }
    }
}