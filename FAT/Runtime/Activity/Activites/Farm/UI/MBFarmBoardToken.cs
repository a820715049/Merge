// ==================================================
// File: MBFarmBoardFarm.cs
// Author: liyueran
// Date: 2025-05-09 17:05:51
// Desc: 农场主棋盘Token
// ==================================================

using EL;
using TMPro;
using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class MBFarmBoardToken : MonoBehaviour
    {
        [SerializeField] private UIImageRes tokenImg;
        public TextMeshProUGUI _tokenNum;
        [SerializeField] private MBFarmBoardFarm mbFarm;
        [SerializeField] private GameObject tokenAdd;

        // 活动实例
        private FarmBoardActivity _activity;

        public void SetUp()
        {
            transform.AddButton($"Bg", OnClickFarm).WithClickScale().FixPivot();
            _tokenNum.raycastTarget = false;
        }

        public void InitOnPreOpen(FarmBoardActivity act)
        {
            _activity = act;
            _tokenNum.text = $"x{_activity.TokenNum.ToString()}";
            var giftPack = Game.Manager.activity.LookupAny(EventType.FarmEndlessPack) as PackEndlessFarm;
            tokenAdd.SetActive(giftPack != null);
        }


        // 点击农田生成棋子
        private void OnClickFarm()
        {
            mbFarm.OnClickFarm();
        }

        public void OnTokenChange()
        {
            _tokenNum.text =  $"x{_activity.TokenNum.ToString()}";
        }
    }
}