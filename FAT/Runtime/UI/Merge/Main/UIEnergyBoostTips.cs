/*
 * @Author: tang.yan
 * @Description: 能量加倍tips界面 
 * @Date: 2024-01-09 19:01:10
 */

using UnityEngine;
using TMPro;
using EL;
using FAT.Merge;

namespace FAT
{
    public class UIEnergyBoostTips : UIBase
    {
        [SerializeField] private Animation anim;
        [SerializeField] private TMP_Text tipsText;

        private bool _isOpen;
        
        protected override void OnCreate()
        {
            
        }

        protected override void OnParse(params object[] items)
        {
            _isOpen = (bool)items[0];
        }

        protected override void OnPreOpen()
        {
            _RefreshUI();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_Check);
        }

        protected override void OnRefresh()
        {
            _RefreshUI();
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_Check);
        }

        protected override void OnPostClose()
        {
            
        }

        private void _RefreshUI()
        {
            tipsText.text = EnergyBoostUtility.GetEnergyBoostTipText();
            anim.Stop();
            var state = Env.Instance.GetEnergyBoostState();
            var anim_name = state switch
            {
                EnergyBoostState.X2 => "DoubleEnergy_appear",
                EnergyBoostState.X4 => "DoubleEnergy_appear_4x",
                _ => "DoubleEnergy_disappear",
            };
            anim.Play(anim_name);
        }

        private void _Check()
        {
            if (!anim.isPlaying)
            {
                anim.Stop();
                Close();
            }
        }
    }
}